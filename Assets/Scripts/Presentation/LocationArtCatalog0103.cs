using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public static class LocationArtCoverage0103
    {
        public const string Unbound = "unbound";
        public const string ComponentsOnly = "components-only";
        public const string FullReviewed = "full-reviewed";
        public static bool IsValid(string value) => value == Unbound || value == ComponentsOnly || value == FullReviewed;
    }

    public sealed class LocationArtDefinition0103
    {
        public string AuthoredLocationId { get; }
        public string Name { get; }
        public string Region { get; }
        public string Terrain { get; }
        public string ThemeFamily { get; }
        public IReadOnlyList<string> RequiredLandmarks { get; }

        internal LocationArtDefinition0103(FullLocation0850 source)
        {
            AuthoredLocationId = source.id; Name = source.name; Region = source.region; Terrain = source.terrain;
            ThemeFamily = LocationArtCatalog0103.ThemeForTerrain(source.terrain);
            RequiredLandmarks = Array.AsReadOnly((source.pointsOfInterest ?? "").Split(';')
                .Select(p => p.Trim()).Where(p => p.Length > 0 && p != "—" && !p.Equals("None", StringComparison.OrdinalIgnoreCase)).ToArray());
        }
    }

    // This catalog describes authored content. A catalog row never means that
    // a Store package, rendered prefab or reviewed landmark is available.
    public static class LocationArtCatalog0103
    {
        private static Dictionary<string, LocationArtDefinition0103> definitions;
        public static IReadOnlyCollection<LocationArtDefinition0103> Definitions
        {
            get { EnsureLoaded(); return definitions.Values; }
        }

        private static void EnsureLoaded()
        {
            if (definitions != null) return;
            definitions = new Dictionary<string, LocationArtDefinition0103>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in FullContentCatalog0850.Locations)
                if (row != null && !string.IsNullOrEmpty(row.id)) definitions.Add(row.id, new LocationArtDefinition0103(row));
        }

        public static bool TryGet(string authoredId, out LocationArtDefinition0103 definition)
        {
            EnsureLoaded(); definition = null;
            return !string.IsNullOrEmpty(authoredId) && definitions.TryGetValue(authoredId, out definition);
        }

        public static string AuthoredIdFor(LocationState state)
        {
            // Legacy runtime L006 is Woodland Clearing, not authored Goblin Lookout.
            return state != null && TryGet(state.AuthoredLocationId, out var definition) ? definition.AuthoredLocationId : null;
        }

        public static LocationArtBindingCounts0103 CountBindings(ILocationArtLibrary0103 library)
        {
            var counts = new LocationArtBindingCounts0103 { CatalogLocations = Definitions.Count };
            foreach (var definition in Definitions)
                if (library != null && library.TryGetBinding(definition.AuthoredLocationId, out var binding, out _))
                {
                    counts.BoundLocations++;
                    if (binding.coverage == LocationArtCoverage0103.FullReviewed) counts.FullReviewedDeclared++;
                    else counts.ComponentsOnlyDeclared++;
                }
            counts.UnboundLocations = counts.CatalogLocations - counts.BoundLocations;
            return counts;
        }

        public static string ThemeForTerrain(string terrain)
        {
            switch (terrain)
            {
                case "Forest": case "Meadow": case "Plains": case "Hills": case "Enchanted Forest": return "temperate_outdoors";
                case "Ancient Forest": case "Grove": case "Spirit": case "Root": case "Fungal": case "Hollow": return "living_blighted_forest";
                case "Urban": case "Village": case "Settlement": return "settlements_and_city_districts";
                case "Cave": case "Cavern": case "Mine": return "caves_and_mines";
                case "Ruins": case "Ancient": case "Sacred": return "ruins_and_sacred_architecture";
                case "Crypt": case "Graveyard": case "Underground": return "crypts_and_underways";
                case "Fortress": case "Camp": return "fortresses_and_camps";
                case "Forge": case "Magma": case "Crystal": case "Heartstone": return "forges_and_crystal_core";
                case "Coast": case "Dock": case "Sea": case "Abyssal": return "coast_and_drowned_world";
                case "Desert": case "Solar": return "desert_and_solar";
                case "River": case "Bridge": case "Marsh": return "water_crossings_and_marsh";
                case "Road": return "roads_and_caravans";
                case "Mountain": return "mountains_and_cliffs";
                default: return "unclassified";
            }
        }
    }

    public sealed class LocationArtBindingCounts0103
    {
        public int CatalogLocations, BoundLocations, UnboundLocations, ComponentsOnlyDeclared, FullReviewedDeclared;
    }

    [Serializable] public sealed class LocationArtManifest0103
    {
        public int format;
        public string platform;
        public LocationArtBundle0103[] bundles = Array.Empty<LocationArtBundle0103>();
        public LocationArtProfile0103[] profiles = Array.Empty<LocationArtProfile0103>();
        public LocationArtBinding0103[] locations = Array.Empty<LocationArtBinding0103>();
    }

    [Serializable] public sealed class LocationArtBundle0103 { public string id, file, sha256; }
    [Serializable] public sealed class LocationArtPart0103
    {
        public string bundleId, assetName;
        public Vector3 position, rotation;
        public Vector3 scale = Vector3.one;
    }
    [Serializable] public sealed class LocationArtProfile0103
    {
        public string id, coverage;
        public string[] landmarks = Array.Empty<string>();
        public string[] reviewEvidence = Array.Empty<string>();
        public LocationArtPart0103[] parts = Array.Empty<LocationArtPart0103>();
    }
    [Serializable] public sealed class LocationArtBinding0103
    {
        public string authoredLocationId, profileId, coverage;
        public string[] missingLandmarks = Array.Empty<string>();
        public string[] reviewEvidence = Array.Empty<string>();
        public string[] replaceLegacy = Array.Empty<string>();
    }

    public interface ILocationArtLibrary0103 : IDisposable
    {
        bool TryGetBinding(string authoredId, out LocationArtBinding0103 binding, out LocationArtProfile0103 profile);
        GameObject LoadPart(LocationArtPart0103 part, out string failure);
    }
}
