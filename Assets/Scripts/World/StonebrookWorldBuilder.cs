using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Ascendance
{
    public sealed class StonebrookWorldBuilder : MonoBehaviour
    {
        private Transform worldRoot;
        private readonly List<GameObject> walkableGeometry = new();
        private readonly List<GameObject> obstacleGeometry = new();
        private RuntimeNavMeshBaker navMeshBaker0862;
        private bool navigationReady0862;
        private bool presentationSpawnReady0862;
        private LocationArtPresenter0103 locationArt0103;
        private readonly List<LocationArtLegacyRendererGroup0103> legacyHouseArt0103 = new();

        public bool NavigationReady0862 => navigationReady0862;
        public bool PresentationSpawnReady0862 => presentationSpawnReady0862;

        public void Build()
        {
            navigationReady0862 = false;
            presentationSpawnReady0862 = false;
            legacyHouseArt0103.Clear();
            worldRoot = new GameObject("Stonebrook_World").transform;
            BuildLightingAndAtmosphere();
            BuildTerrain();
            BuildVillage();
            StonebrookAmbientFX.Create(worldRoot);
            BuildCamera();
            StartCoroutine(BuildNavigationAndSpawn());
        }

        private IEnumerator BuildNavigationAndSpawn()
        {
            yield return null;
            navMeshBaker0862 = GetComponent<RuntimeNavMeshBaker>();
            if (navMeshBaker0862 == null) navMeshBaker0862 = gameObject.AddComponent<RuntimeNavMeshBaker>();
            var operation = navMeshBaker0862.Build(walkableGeometry, obstacleGeometry, new Bounds(Vector3.zero, new Vector3(78f, 14f, 78f)));
            if (operation != null)
            {
                yield return operation;
                navigationReady0862 = navMeshBaker0862.Activate0862();
            }
            else
            {
                navigationReady0862 = false;
            }

            // Runtime Quest setup can request presentation repair during manager/session setup,
            // before Stonebrook has built navigation. Defer those requests until this point so
            // Hero NavMeshAgents are never created against a missing/empty NavMesh.
            presentationSpawnReady0862 = true;
            yield return null;

            SpawnLocationMarkers();
            locationArt0103 = LocationArtPresenter0103.Install(AscendanceGameManager.Instance, worldRoot, legacyHouseArt0103);
            SpawnHeroes();
            SpawnEnemies();
            yield return null;
            ValidateAndRepairCharacterSpawns();
            foreach (var piece in AscendanceGameManager.Instance.HeroCreatedPieces) HeroPieceAvatar.Create(piece);
            foreach (var obj in AscendanceGameManager.Instance.EncounterObjects) EncounterObjectAvatar.Create(obj);
            foreach (var obj in AscendanceGameManager.Instance.QuestObjects0829)
                if (!obj.Rescued && !obj.Destroyed) QuestObjectAvatar0829.Create(obj);
        }

        private void BuildLightingAndAtmosphere()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.31f);
            RenderSettings.ambientEquatorColor = new Color(0.17f, 0.13f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.055f, 0.05f, 0.045f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0060f;
            RenderSettings.fogColor = new Color(0.27f, 0.31f, 0.40f);

            var sun = new GameObject("Dusk Sun");
            sun.transform.SetParent(worldRoot);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(1f, 0.69f, 0.47f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.82f;
            sun.transform.rotation = Quaternion.Euler(36f, -42f, 0f);
            RenderSettings.sun = light;

            var fill = new GameObject("Cool Moon Fill");
            fill.transform.SetParent(worldRoot);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.28f;
            fillLight.color = new Color(0.35f, 0.47f, 0.78f);
            fill.transform.rotation = Quaternion.Euler(52f, 132f, 0f);
        }

        private void BuildTerrain()
        {
            var ground = CreatePrimitive("Ground", PrimitiveType.Plane, Vector3.zero, new Vector3(6.6f, 1f, 6.6f), Color.white, true);
            ground.GetComponent<Renderer>().sharedMaterial = StonebrookSurfaceMaterials.Ground();
            walkableGeometry.Add(ground);

            // Dirt/stone roads that visually connect the Ascendance Location graph.
            var roadA = CreatePrimitive("Stonebrook Road", PrimitiveType.Cube, new Vector3(0f, 0.055f, 4.4f), new Vector3(7.2f, 0.09f, 1.55f), Color.white, true);
            roadA.GetComponent<Renderer>().sharedMaterial = StonebrookSurfaceMaterials.Road();
            walkableGeometry.Add(roadA);
            var roadB = CreatePrimitive("Forest Road", PrimitiveType.Cube, new Vector3(-10.3f, 0.052f, 5.4f), new Vector3(8.4f, 0.085f, 1.30f), Color.white, true);
            roadB.GetComponent<Renderer>().sharedMaterial = StonebrookSurfaceMaterials.Road();
            roadB.transform.rotation = Quaternion.Euler(0f, -8f, 0f);
            walkableGeometry.Add(roadB);

            CreateCobblestones(new Vector3(-2f, 0.14f, 3.9f), 32);

            var river = CreatePrimitive("River", PrimitiveType.Cube, new Vector3(12f, -0.28f, 9f), new Vector3(5f, 0.30f, 18f), Color.white, false);
            river.GetComponent<Renderer>().sharedMaterial = StonebrookSurfaceMaterials.Water();
            AddBoxObstacle("River Obstacle South", new Vector3(12f, 0.6f, 1.9f), new Vector3(5.0f, 1.3f, 4.0f));
            AddBoxObstacle("River Obstacle North", new Vector3(12f, 0.6f, 12.8f), new Vector3(5.0f, 1.3f, 10.8f));

            GameObject bridge;
            if (!ProductionAssetResolver.TrySpawnEnvironment("Bridge", worldRoot, new Vector3(10.7f, 0.23f, 5.1f), Quaternion.identity, Vector3.one, out bridge))
            {
                bridge = CreatePrimitive("Stone Bridge", PrimitiveType.Cube, new Vector3(10.7f, 0.23f, 5.1f), new Vector3(5.6f, 0.32f, 2.6f), new Color(0.31f, 0.29f, 0.27f), true);
                CreateBridgeRails(new Vector3(10.7f, 0.23f, 5.1f));
                walkableGeometry.Add(bridge);
            }
            else
            {
                // Keep navigation independent from the imported bridge render mesh.
                AddWalkableBox("Bridge Navigation Surface", new Vector3(10.7f, 0.23f, 5.1f), new Vector3(5.6f, 0.32f, 2.6f));
            }

            CreateFenceLine(new Vector3(-10f, 0f, -1.5f), 6, Vector3.right, 2.2f);
            CreateFenceLine(new Vector3(4.5f, 0f, 8.5f), 5, Vector3.right, 2.2f);

            for (int i = 0; i < 24; i++)
            {
                float x = -27f + (i % 12) * 5.1f;
                float z = -6f + (i / 12) * 25f;
                if (Mathf.Abs(x) < 9f && z < 8f) continue;
                CreateTree(new Vector3(x, 0f, z), 0.80f + (i % 4) * 0.12f);
            }

            for (int i = 0; i < 18; i++)
            {
                float angle = i * 0.83f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * (16f + (i % 3) * 3f), 0f, Mathf.Sin(angle) * (13f + (i % 4) * 2f) + 5f);
                CreateRock(p, 0.45f + (i % 3) * 0.18f);
            }

            CreateBush(new Vector3(-11.5f, 0f, 1.2f));
            CreateBush(new Vector3(-13.8f, 0f, 8.4f));
            CreateBush(new Vector3(7.8f, 0f, 8.0f));
            CreateBush(new Vector3(15.3f, 0f, 5.7f));
        }

        private void BuildVillage()
        {
            CreateHouse(new Vector3(-4.2f, 1.1f, -3.3f), new Vector3(4.2f, 2.2f, 3.5f), new Color(0.41f, 0.30f, 0.20f));
            CreateHouse(new Vector3(3.6f, 1.1f, -4.2f), new Vector3(4.6f, 2.2f, 3.3f), new Color(0.37f, 0.29f, 0.20f));
            CreateHouse(new Vector3(7.0f, 1.1f, 1.2f), new Vector3(3.6f, 2.2f, 3.0f), new Color(0.45f, 0.33f, 0.22f));
            CreateHouse(new Vector3(-7.7f, 1.1f, 3.0f), new Vector3(4.0f, 2.2f, 3.0f), new Color(0.40f, 0.31f, 0.22f));

            CreateWell(new Vector3(-0.8f, 0f, 1.2f));
            CreateCart(new Vector3(3.0f, 0f, 3.7f));
            CreateCrates(new Vector3(-6.0f, 0f, 0.2f));
            CreateCrates(new Vector3(6.0f, 0f, 5.6f));

            CreateBrazier(new Vector3(0.8f, 0.5f, 2.1f));
            CreateBrazier(new Vector3(8.7f, 0.5f, 4.0f));
            CreateBrazier(new Vector3(-6.0f, 0.5f, 5.0f));

            var sign = CreatePrimitive("Stonebrook Sign", PrimitiveType.Cube, new Vector3(4.7f, 1.0f, 6.0f), new Vector3(2.7f, 0.25f, 0.18f), new Color(0.25f, 0.13f, 0.055f), false);
            WorldBillboardLabel.Create(sign.transform, "STONEBROOK", new Vector3(0f, 1.2f, 0f), new Color(0.93f, 0.73f, 0.35f), 34);
        }

        private void BuildCamera()
        {
            Camera camera = ShellCameraBootstrap.EnsureCamera(false);
            camera.transform.position = new Vector3(-0.5f, 10.6f, -12.6f);
            camera.transform.rotation = Quaternion.Euler(34f, 38f, 0f);
            camera.fieldOfView = 44f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 350f;
            camera.backgroundColor = new Color(0.12f, 0.17f, 0.27f);
            if (camera.GetComponent<IsometricCameraController>() == null)
                camera.gameObject.AddComponent<IsometricCameraController>();
        }

        private void SpawnLocationMarkers()
        {
            foreach (var kv in AscendanceGameManager.Instance.Locations)
            {
                var state = kv.Value;
                var root = new GameObject("LocationMarker_" + state.Id);
                root.transform.SetParent(worldRoot);
                root.transform.position = state.Position;

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "Marker Disc";
                marker.transform.SetParent(root.transform, false);
                marker.transform.localPosition = new Vector3(0f, 0.06f, 0f);
                marker.transform.localScale = new Vector3(1.70f, 0.025f, 1.70f);
                root.AddComponent<LocationMarker>().Initialize(state.Id, marker.GetComponent<Renderer>(), marker.GetComponent<Collider>());
                WorldBillboardLabel.Create(root.transform, state.Name, new Vector3(0f, 0.48f, 0f), new Color(0.78f, 0.88f, 1f), 26);
            }
        }

        public void EnsureRuntimeLocationSpawns0856()
        {
            var gm=AscendanceGameManager.Instance;if(gm==null||worldRoot==null)return;
            foreach(var state in gm.Locations.Values)
            {
                if(state==null||GameObject.Find("LocationMarker_"+state.Id)!=null)continue;
                var root=new GameObject("LocationMarker_"+state.Id);root.transform.SetParent(worldRoot);root.transform.position=state.Position;
                var marker=GameObject.CreatePrimitive(PrimitiveType.Cylinder);marker.name="Marker Disc";marker.transform.SetParent(root.transform,false);marker.transform.localPosition=new Vector3(0f,0.06f,0f);marker.transform.localScale=new Vector3(1.70f,0.025f,1.70f);
                root.AddComponent<LocationMarker>().Initialize(state.Id,marker.GetComponent<Renderer>(),marker.GetComponent<Collider>());
                WorldBillboardLabel.Create(root.transform,state.Name,new Vector3(0f,0.48f,0f),new Color(0.78f,0.88f,1f),26);
            }
            locationArt0103?.Reconcile();
        }

        private void SpawnHeroes()
        {
            var colors = new Dictionary<string, Color>
            {
                ["Garruk"] = new Color(0.46f, 0.14f, 0.055f),
                ["Rowan"] = new Color(0.10f, 0.34f, 0.14f),
                ["Aldric"] = new Color(0.07f, 0.22f, 0.54f),
                ["Elara"] = new Color(0.73f, 0.71f, 0.63f),
                ["Lyra"] = new Color(0.26f, 0.08f, 0.32f), ["Vaelis"] = new Color(0.16f, 0.18f, 0.52f),
                ["Seraphine"] = new Color(0.72f, 0.58f, 0.18f), ["Thorne"] = new Color(0.17f, 0.42f, 0.16f),
                ["Cassian"] = new Color(0.42f, 0.12f, 0.40f), ["Kael"] = new Color(0.55f, 0.42f, 0.20f),
                ["Mira"] = new Color(0.42f, 0.26f, 0.10f), ["Malachar"] = new Color(0.60f, 0.12f, 0.06f),
                ["Veyra"] = new Color(0.22f, 0.07f, 0.30f), ["Mordren"] = new Color(0.18f, 0.18f, 0.22f),
                ["Lucien"] = new Color(0.20f, 0.38f, 0.62f), ["Asha"] = new Color(0.12f, 0.48f, 0.46f)
            };

            foreach (var hero in AscendanceGameManager.Instance.ParticipatingHeroes)
                SpawnHeroSafe(hero, colors);
        }

        private void SpawnHeroSafe(HeroState hero, Dictionary<string, Color> colors)
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null || hero == null || gm.HeroAvatars.ContainsKey(hero.Id)) return;

            GameObject root = null;
            try
            {
                root = new GameObject(hero.DisplayName);
                root.transform.SetParent(worldRoot);
                Vector3 location = gm.Locations[hero.LocationId].Position;
                Vector3 desiredPosition = location + FormationOffset(hero.Id);
                NavMeshAgent agent = null;

                // Add NavMeshAgent only after a completed bake and only while standing on a
                // sampled NavMesh position. If navigation is unavailable, HeroAvatar already
                // has direct fallback movement and remains fully playable.
                if (navigationReady0862 && NavMesh.SamplePosition(desiredPosition, out var spawnHit0862, 4f, NavMesh.AllAreas))
                {
                    root.transform.position = spawnHit0862.position;
                    agent = root.AddComponent<NavMeshAgent>();
                    agent.speed = 5.2f;
                    agent.angularSpeed = 720f;
                    agent.acceleration = 32f;
                    agent.radius = hero.Id == "Garruk" ? 0.50f : 0.42f;
                    agent.height = hero.Id == "Garruk" ? 2.65f : 2.4f;
                    agent.stoppingDistance = 0.08f;
                    agent.autoBraking = true;
                    agent.updateRotation = true;
                }
                else
                {
                    root.transform.position = desiredPosition;
                }

                Color primary = colors.TryGetValue(hero.Id, out var c) ? c : Color.Lerp(Color.gray, Color.white, 0.18f);
                CharacterPresentationController presentation = null;
                try
                {
                    presentation = CharacterPresentationController.BuildHero(root.transform, hero.Id, primary);
                }
                catch (System.Exception presentationException)
                {
                    Debug.LogError($"[ASCENDANCE] Hero presentation failed for {hero.Id}; using gameplay fallback. {presentationException.Message}");
                }

                var avatar = root.AddComponent<HeroAvatar>();
                avatar.Initialize(hero.Id, primary, presentation, agent);
                if (hero.Downed) avatar.ShowDowned();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ASCENDANCE] Failed to spawn Hero {hero.Id}: {ex}");
                if (root != null) Destroy(root);
            }
        }

        private void SpawnEnemies()
        {
            int index = 0;
            foreach (var enemy in AscendanceGameManager.Instance.Enemies.Values)
            {
                SpawnEnemySafe(enemy, index);
                index++;
            }
        }

        private void SpawnEnemySafe(EnemyState enemy, int index)
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null || enemy == null || gm.EnemyAvatars.ContainsKey(enemy.Id)) return;

            GameObject root = null;
            try
            {
                root = new GameObject(enemy.DisplayName + "_" + enemy.Id);
                root.transform.SetParent(worldRoot);
                root.transform.position = gm.Locations[enemy.LocationId].Position + EnemyOffset(enemy.Id, index);

                CharacterPresentationController presentation = null;
                try
                {
                    presentation = CharacterPresentationController.BuildEnemy(root.transform, string.IsNullOrEmpty(enemy.TemplateId) ? enemy.Id : enemy.TemplateId, enemy.IsBoss);
                }
                catch (System.Exception presentationException)
                {
                    Debug.LogError($"[ASCENDANCE] Enemy presentation failed for {enemy.Id}; using gameplay fallback. {presentationException.Message}");
                }

                var avatar = root.AddComponent<EnemyAvatar>();
                avatar.Initialize(enemy.Id, enemy.IsBoss, presentation);
                if (enemy.Defeated) avatar.SetDefeated();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ASCENDANCE] Failed to spawn Enemy {enemy.Id}: {ex}");
                if (root != null) Destroy(root);
            }
        }

        public void EnsureRuntimeEnemySpawns()
        {
            // Quest setup fires while GameBootstrap is still applying manager/session state.
            // Initial presentation is spawned once navigation setup has completed.
            if (!presentationSpawnReady0862 || worldRoot == null) return;
            ValidateAndRepairCharacterSpawns();
        }

        private void ValidateAndRepairCharacterSpawns()
        {
            var gm = AscendanceGameManager.Instance;
            if (gm == null) return;

            var colors = new Dictionary<string, Color>
            {
                ["Garruk"] = new Color(0.46f, 0.14f, 0.055f),
                ["Rowan"] = new Color(0.10f, 0.34f, 0.14f),
                ["Aldric"] = new Color(0.07f, 0.22f, 0.54f),
                ["Elara"] = new Color(0.73f, 0.71f, 0.63f),
                ["Lyra"] = new Color(0.26f, 0.08f, 0.32f), ["Vaelis"] = new Color(0.16f, 0.18f, 0.52f),
                ["Seraphine"] = new Color(0.72f, 0.58f, 0.18f), ["Thorne"] = new Color(0.17f, 0.42f, 0.16f),
                ["Cassian"] = new Color(0.42f, 0.12f, 0.40f), ["Kael"] = new Color(0.55f, 0.42f, 0.20f),
                ["Mira"] = new Color(0.42f, 0.26f, 0.10f), ["Malachar"] = new Color(0.60f, 0.12f, 0.06f),
                ["Veyra"] = new Color(0.22f, 0.07f, 0.30f), ["Mordren"] = new Color(0.18f, 0.18f, 0.22f),
                ["Lucien"] = new Color(0.20f, 0.38f, 0.62f), ["Asha"] = new Color(0.12f, 0.48f, 0.46f)
            };

            var activeHeroIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var hero in gm.ParticipatingHeroes) activeHeroIds.Add(hero.Id);

            // Quest/campaign transitions may change the active party without rebuilding the scene.
            // Remove presentation objects for Heroes who are no longer participating before
            // validating counts, then create any newly participating Hero pieces.
            foreach (var heroId in new List<string>(gm.HeroAvatars.Keys))
            {
                if (activeHeroIds.Contains(heroId)) continue;
                if (gm.HeroAvatars.TryGetValue(heroId, out var staleAvatar) && staleAvatar != null)
                    Destroy(staleAvatar.gameObject);
                gm.HeroAvatars.Remove(heroId);
            }

            foreach (var hero in gm.ParticipatingHeroes)
                if (!gm.HeroAvatars.ContainsKey(hero.Id)) SpawnHeroSafe(hero, colors);

            // Runtime/validator Quest transitions can remove EnemyState entries while their
            // presentation registrations are still alive. Prune those stale avatars before
            // creating or validating the current Quest presentation so visual state mirrors
            // the authoritative Enemy dictionary exactly.
            foreach (var enemyId in new List<string>(gm.EnemyAvatars.Keys))
            {
                if (gm.Enemies.ContainsKey(enemyId)) continue;
                if (gm.EnemyAvatars.TryGetValue(enemyId, out var staleEnemyAvatar) && staleEnemyAvatar != null)
                    Destroy(staleEnemyAvatar.gameObject);
                gm.EnemyAvatars.Remove(enemyId);
            }

            int index = 0;
            foreach (var enemy in gm.Enemies.Values)
            {
                if (!gm.EnemyAvatars.ContainsKey(enemy.Id)) SpawnEnemySafe(enemy, index);
                index++;
            }
            foreach(var obj in gm.QuestObjects0829)if(!obj.Rescued&&!obj.Destroyed)QuestObjectAvatar0829.Create(obj);

            int livingEnemies = 0;
            foreach (var enemy in gm.Enemies.Values)
                if (!enemy.Defeated) livingEnemies++;

            gm.AddLog($"SPAWN CHECK — Heroes {gm.HeroAvatars.Count}/{gm.PartySize} · Enemies {gm.EnemyAvatars.Count}/{gm.Enemies.Count} ({livingEnemies} living).");
            if (gm.HeroAvatars.Count == gm.PartySize && gm.EnemyAvatars.Count == gm.Enemies.Count)
                gm.AddLog("SPAWN READY — current Quest party and enemy presentation registered.");

            if (gm.HeroAvatars.Count != gm.PartySize)
                Debug.LogError($"[ASCENDANCE] PARTY SPAWN INCOMPLETE — expected {gm.PartySize}, found {gm.HeroAvatars.Count}.");
            if (gm.EnemyAvatars.Count != gm.Enemies.Count)
                Debug.LogError($"[ASCENDANCE] ENEMY SPAWN INCOMPLETE — expected {gm.Enemies.Count}, found {gm.EnemyAvatars.Count}.");
        }

        private static Vector3 EnemyOffset(string id, int index)
        {
            if (id == "Captain") return new Vector3(2.9f, 0f, 1.6f);
            if (id == "RaiderA") return new Vector3(1.6f, 0f, 2.2f);
            return new Vector3(1.9f + index * 0.35f, 0f, 1.0f);
        }

        private void CreateHouse(Vector3 position, Vector3 scale, Color wallColor)
        {
            // Capture only renderers produced by this house call. Procedural
            // roofs/windows are siblings, so a subtree name search is unsafe.
            var before = new HashSet<Renderer>(worldRoot.GetComponentsInChildren<Renderer>(true));
            try
            {
                Vector3 productionScale = new Vector3(
                    Mathf.Max(0.70f, scale.x / 4.2f),
                    Mathf.Max(0.80f, scale.y / 2.2f),
                    Mathf.Max(0.75f, scale.z / 3.5f));
                if (ProductionAssetResolver.TrySpawnEnvironment("House", worldRoot, new Vector3(position.x, 0f, position.z), Quaternion.identity, productionScale, out var productionHouse))
                {
                    // Production visuals never participate directly in runtime NavMesh baking.
                    // A cheap blocker proxy preserves the authored footprint without requiring
                    // Read/Write access on the imported render mesh.
                    AddBoxObstacle("House Navigation Blocker", new Vector3(position.x, scale.y * 0.5f, position.z), new Vector3(scale.x * 0.94f, Mathf.Max(2.0f, scale.y), scale.z * 0.94f));
                    return;
                }

                var house = CreatePrimitive("Timber House", PrimitiveType.Cube, position, scale, wallColor, true);
                AddObstacleTo(house);

                Color beam = new Color(0.18f, 0.09f, 0.035f);
                float y = position.y + scale.y * 0.36f;
                for (int x = -1; x <= 1; x += 2)
                {
                    CreatePrimitive("Timber Beam", PrimitiveType.Cube, new Vector3(position.x + x * scale.x * 0.43f, y, position.z - scale.z * 0.51f), new Vector3(0.14f, scale.y * 0.88f, 0.10f), beam, true);
                }
                CreatePrimitive("Cross Beam", PrimitiveType.Cube, new Vector3(position.x, y, position.z - scale.z * 0.51f), new Vector3(scale.x * 0.92f, 0.12f, 0.10f), beam, true);

                Color roofColor = new Color(0.16f, 0.055f, 0.032f);
                var roofLeft = CreatePrimitive("Roof Left", PrimitiveType.Cube, position + Vector3.up * (scale.y * 0.70f) + Vector3.left * (scale.x * 0.22f), new Vector3(scale.x * 0.62f, 0.24f, scale.z * 1.12f), roofColor, true);
                roofLeft.transform.rotation = Quaternion.Euler(0f, 0f, -31f);
                var roofRight = CreatePrimitive("Roof Right", PrimitiveType.Cube, position + Vector3.up * (scale.y * 0.70f) + Vector3.right * (scale.x * 0.22f), new Vector3(scale.x * 0.62f, 0.24f, scale.z * 1.12f), roofColor, true);
                roofRight.transform.rotation = Quaternion.Euler(0f, 0f, 31f);

                CreatePrimitive("Door", PrimitiveType.Cube, new Vector3(position.x, 0.78f, position.z - scale.z * 0.52f), new Vector3(0.72f, 1.45f, 0.10f), new Color(0.20f, 0.10f, 0.04f), false);
                CreateWindow(new Vector3(position.x - scale.x * 0.28f, 1.35f, position.z - scale.z * 0.525f));
                CreateWindow(new Vector3(position.x + scale.x * 0.28f, 1.35f, position.z - scale.z * 0.525f));
            }
            finally
            {
                var houseRenderers = new List<Renderer>();
                foreach (var renderer in worldRoot.GetComponentsInChildren<Renderer>(true))
                    if (!before.Contains(renderer) && renderer.gameObject.name != "House Navigation Blocker") houseRenderers.Add(renderer);
                legacyHouseArt0103.Add(new LocationArtLegacyRendererGroup0103("stonebrook-house-" + legacyHouseArt0103.Count,
                    new Bounds(position, scale), houseRenderers));
            }
        }

        private void CreateWindow(Vector3 position)
        {
            var window = CreatePrimitive("Warm Window", PrimitiveType.Cube, position, new Vector3(0.46f, 0.54f, 0.08f), new Color(0.90f, 0.47f, 0.11f), false);
            var renderer = window.GetComponent<Renderer>();
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", new Color(0.78f, 0.28f, 0.04f));
        }

        private void CreateTree(Vector3 position, float scale)
        {
            if (ProductionAssetResolver.TrySpawnEnvironment("Tree", worldRoot, position, Quaternion.Euler(0f, position.x * 13f + position.z * 7f, 0f), Vector3.one * scale, out var productionTree))
            {
                // Block only the trunk footprint; the crown is visual and should not create
                // an oversized navigation exclusion zone.
                AddBoxObstacle("Tree Navigation Blocker", position + Vector3.up * (1.15f * scale), new Vector3(0.62f * scale, 2.30f * scale, 0.62f * scale));
                return;
            }

            var trunk = CreatePrimitive("Tree Trunk", PrimitiveType.Cylinder, position + Vector3.up * (1.15f * scale), new Vector3(0.30f * scale, 1.15f * scale, 0.30f * scale), new Color(0.17f, 0.085f, 0.038f), true);
            RegisterObstacleGeometry(trunk);
            Color leaves = new Color(0.045f, 0.20f + 0.025f * scale, 0.065f);
            CreatePrimitive("Tree Crown", PrimitiveType.Sphere, position + Vector3.up * (3.05f * scale), new Vector3(1.60f * scale, 1.45f * scale, 1.60f * scale), leaves, true);
            CreatePrimitive("Tree Crown", PrimitiveType.Sphere, position + new Vector3(-0.75f, 3.20f, 0.35f) * scale, new Vector3(0.95f, 1.05f, 0.95f) * scale, leaves * 0.92f, true);
            CreatePrimitive("Tree Crown", PrimitiveType.Sphere, position + new Vector3(0.70f, 3.10f, -0.25f) * scale, new Vector3(1.0f, 1.0f, 1.0f) * scale, leaves * 1.05f, true);
        }

        private void CreateBush(Vector3 position)
        {
            if (ProductionAssetResolver.TrySpawnEnvironment("Bush", worldRoot, position, Quaternion.identity, Vector3.one, out _)) return;
            CreatePrimitive("Bush", PrimitiveType.Sphere, position + Vector3.up * 0.42f, new Vector3(0.85f, 0.52f, 0.72f), new Color(0.055f, 0.24f, 0.075f), true);
            CreatePrimitive("Bush", PrimitiveType.Sphere, position + new Vector3(0.52f, 0.32f, 0.18f), new Vector3(0.62f, 0.40f, 0.55f), new Color(0.06f, 0.27f, 0.08f), true);
        }

        private void CreateRock(Vector3 position, float scale)
        {
            if (ProductionAssetResolver.TrySpawnEnvironment("Rock", worldRoot, position, Quaternion.Euler(0f, position.x * 11f, 12f), Vector3.one * scale, out _)) return;
            var rock = CreatePrimitive("Rock", PrimitiveType.Sphere, position + Vector3.up * 0.25f * scale, new Vector3(0.85f, 0.48f, 0.72f) * scale, new Color(0.28f, 0.29f, 0.28f), true);
            rock.transform.rotation = Quaternion.Euler(0f, position.x * 11f, 12f);
        }

        private void CreateFenceLine(Vector3 start, int posts, Vector3 direction, float spacing)
        {
            Vector3 normalized = direction.normalized;
            Quaternion rotation = Quaternion.LookRotation(normalized, Vector3.up);
            for (int i = 0; i < posts - 1; i++)
            {
                Vector3 p = start + normalized * spacing * i;
                Vector3 mid = p + normalized * spacing * 0.5f;
                if (ProductionAssetResolver.TrySpawnEnvironment("Fence", worldRoot, mid, rotation, new Vector3(1f, 1f, spacing / 2.1f), out _))
                    continue;

                CreatePrimitive("Fence Post", PrimitiveType.Cube, p + Vector3.up * 0.65f, new Vector3(0.18f, 1.3f, 0.18f), new Color(0.23f, 0.13f, 0.06f), true);
                var rail = CreatePrimitive("Fence Rail", PrimitiveType.Cube, mid + Vector3.up * 0.75f, new Vector3(0.12f, 0.12f, spacing), new Color(0.23f, 0.13f, 0.06f), true);
                rail.transform.rotation = rotation;
            }

            if (posts > 0)
            {
                Vector3 last = start + normalized * spacing * (posts - 1);
                CreatePrimitive("Fence End Post", PrimitiveType.Cube, last + Vector3.up * 0.65f, new Vector3(0.18f, 1.3f, 0.18f), new Color(0.23f, 0.13f, 0.06f), true);
            }
        }

        private void CreateBridgeRails(Vector3 center)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float z = center.z + side * 1.15f;
                for (int i = -2; i <= 2; i++)
                    CreatePrimitive("Bridge Post", PrimitiveType.Cube, new Vector3(center.x + i * 1.05f, 0.82f, z), new Vector3(0.12f, 1.1f, 0.12f), new Color(0.20f, 0.14f, 0.08f), true);
                CreatePrimitive("Bridge Rail", PrimitiveType.Cube, new Vector3(center.x, 1.08f, z), new Vector3(5.0f, 0.12f, 0.12f), new Color(0.20f, 0.14f, 0.08f), true);
            }
        }

        private void CreateWell(Vector3 position)
        {
            if (ProductionAssetResolver.TrySpawnEnvironment("Well", worldRoot, position, Quaternion.identity, Vector3.one, out _)) return;
            CreatePrimitive("Well Base", PrimitiveType.Cylinder, position + Vector3.up * 0.42f, new Vector3(1.0f, 0.42f, 1.0f), new Color(0.30f, 0.28f, 0.25f), true);
            CreatePrimitive("Well Water", PrimitiveType.Cylinder, position + Vector3.up * 0.82f, new Vector3(0.72f, 0.04f, 0.72f), new Color(0.05f, 0.22f, 0.30f), false);
            CreatePrimitive("Well Post", PrimitiveType.Cube, position + new Vector3(-0.85f, 1.35f, 0f), new Vector3(0.14f, 1.7f, 0.14f), new Color(0.22f, 0.12f, 0.05f), true);
            CreatePrimitive("Well Post", PrimitiveType.Cube, position + new Vector3(0.85f, 1.35f, 0f), new Vector3(0.14f, 1.7f, 0.14f), new Color(0.22f, 0.12f, 0.05f), true);
            CreatePrimitive("Well Beam", PrimitiveType.Cube, position + Vector3.up * 2.05f, new Vector3(1.85f, 0.13f, 0.13f), new Color(0.22f, 0.12f, 0.05f), true);
        }

        private void CreateCart(Vector3 position)
        {
            if (ProductionAssetResolver.TrySpawnEnvironment("Wagon", worldRoot, position, Quaternion.identity, Vector3.one, out _)) return;
            CreatePrimitive("Cart Bed", PrimitiveType.Cube, position + Vector3.up * 0.62f, new Vector3(2.1f, 0.35f, 1.15f), new Color(0.29f, 0.16f, 0.07f), true);
            for (int s = -1; s <= 1; s += 2)
            {
                var wheel = CreatePrimitive("Cart Wheel", PrimitiveType.Cylinder, position + new Vector3(0f, 0.50f, s * 0.72f), new Vector3(0.62f, 0.10f, 0.62f), new Color(0.16f, 0.08f, 0.03f), true);
                wheel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            CreatePrimitive("Cart Shaft", PrimitiveType.Cube, position + new Vector3(1.75f, 0.55f, 0f), new Vector3(2.2f, 0.10f, 0.10f), new Color(0.25f, 0.13f, 0.05f), true);
        }

        private void CreateCrates(Vector3 position)
        {
            if (ProductionAssetResolver.TrySpawnEnvironment("Crates", worldRoot, position, Quaternion.identity, Vector3.one, out _)) return;
            CreatePrimitive("Crate", PrimitiveType.Cube, position + Vector3.up * 0.45f, new Vector3(0.82f, 0.82f, 0.82f), new Color(0.30f, 0.18f, 0.08f), true);
            CreatePrimitive("Crate", PrimitiveType.Cube, position + new Vector3(0.75f, 0.35f, 0.15f), new Vector3(0.64f, 0.64f, 0.64f), new Color(0.27f, 0.15f, 0.06f), true);
            CreatePrimitive("Barrel", PrimitiveType.Cylinder, position + new Vector3(-0.75f, 0.55f, 0.1f), new Vector3(0.46f, 0.55f, 0.46f), new Color(0.25f, 0.13f, 0.05f), true);
        }

        private void CreateCobblestones(Vector3 center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x = center.x + (i % 8) * 0.72f - 2.4f;
                float z = center.z + (i / 8) * 0.68f - 1.2f;
                var stone = CreatePrimitive("Cobblestone", PrimitiveType.Cube, new Vector3(x, center.y, z), new Vector3(0.55f, 0.045f, 0.45f), Color.white, true);
                stone.GetComponent<Renderer>().sharedMaterial = StonebrookSurfaceMaterials.Stone();
                stone.transform.rotation = Quaternion.Euler(0f, (i * 17) % 22 - 11f, 0f);
                Destroy(stone.GetComponent<Collider>());
            }
        }

        private void CreateBrazier(Vector3 position)
        {
            if (ProductionAssetResolver.TrySpawnEnvironment("Brazier", worldRoot, position, Quaternion.identity, Vector3.one, out _)) return;
            CreatePrimitive("Village Brazier", PrimitiveType.Cylinder, position, new Vector3(0.55f, 0.46f, 0.55f), new Color(0.26f, 0.14f, 0.06f), true);
            var flameMesh = CreatePrimitive("Flame", PrimitiveType.Sphere, position + Vector3.up * 1.15f, new Vector3(0.30f, 0.62f, 0.30f), new Color(1f, 0.28f, 0.035f), false);
            Destroy(flameMesh.GetComponent<Collider>());
            var flameRenderer = flameMesh.GetComponent<Renderer>();
            flameRenderer.material.EnableKeyword("_EMISSION");
            flameRenderer.material.SetColor("_EmissionColor", new Color(1f, 0.17f, 0.015f) * 2f);
            var light = flameMesh.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10f;
            light.intensity = 2.7f;
            light.color = new Color(1f, 0.36f, 0.09f);
            light.shadows = LightShadows.Soft;
            flameMesh.AddComponent<FireFlicker>().Initialize(light);
        }

        private void AddWalkableBox(string name, Vector3 position, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(worldRoot);
            go.transform.position = position;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            walkableGeometry.Add(go);
        }

        private void AddBoxObstacle(string name, Vector3 position, Vector3 size)
        {
            // Bake blockers directly into the runtime NavMesh instead of attaching
            // NavMeshObstacle components. This is more reliable across Unity 6.x.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(worldRoot);
            go.transform.position = position;
            go.transform.localScale = size;
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            RegisterObstacleGeometry(go);
        }

        private void AddObstacleTo(GameObject go)
        {
            RegisterObstacleGeometry(go);
        }

        private void RegisterObstacleGeometry(GameObject go)
        {
            if (go == null || obstacleGeometry.Contains(go)) return;
            obstacleGeometry.Add(go);
        }

        private static Vector3 FormationOffset(string heroId)
        {
            int index = 0;
            var gm = AscendanceGameManager.Instance;
            if (gm != null && gm.Heroes.TryGetValue(heroId, out var hero) && hero.PartyIndex >= 0) index = hero.PartyIndex;
            Vector3[] offsets = { new(-1.35f,0f,-1.0f), new(-0.45f,0f,-1.25f), new(0.45f,0f,-1.0f), new(1.35f,0f,-1.25f) };
            return offsets[Mathf.Clamp(index,0,offsets.Length-1)];
        }

        private GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool receiveShadows)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(worldRoot);
            go.transform.position = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
            renderer.receiveShadows = receiveShadows;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }
    }
}
