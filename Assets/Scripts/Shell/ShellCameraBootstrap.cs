using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ascendance
{
    /// <summary>
    /// Guarantees Display 1 always has an enabled camera during scene transitions.
    /// MainMenuUI and StonebrookWorldBuilder then configure/reuse this camera.
    /// </summary>
    public static class ShellCameraBootstrap
    {
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (subscribed) return;
            subscribed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "MainMenu" && scene.name != "Stonebrook") return;
            EnsureCamera(scene.name == "MainMenu");
        }

        public static Camera EnsureCamera(bool menuStyle = false)
        {
            Camera camera = Camera.main;

            if (camera == null)
            {
                // Camera.main ignores disabled cameras, so recover one if a scene contains it.
                var all = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
                if (all != null && all.Length > 0)
                    camera = all[0];
            }

            if (camera == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                camera = go.AddComponent<Camera>();
            }

            if (!camera.gameObject.activeSelf)
                camera.gameObject.SetActive(true);

            camera.enabled = true;
            camera.targetDisplay = 0;
            camera.targetTexture = null;
            camera.cullingMask = ~0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = menuStyle
                ? new Color(0.015f, 0.022f, 0.032f, 1f)
                : new Color(0.12f, 0.17f, 0.27f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 350f;

            if (camera.GetComponent<AudioListener>() == null && Object.FindAnyObjectByType<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();

            return camera;
        }
    }
}
