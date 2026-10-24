using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ascendance
{
    public static class AscendanceSettings
    {
        private const string FullscreenKey = "Ascendance.Fullscreen";
        private const string WidthKey = "Ascendance.Width";
        private const string HeightKey = "Ascendance.Height";
        private const string VolumeKey = "Ascendance.Volume";

        public static bool Fullscreen => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        public static int Width => PlayerPrefs.GetInt(WidthKey, 1920);
        public static int Height => PlayerPrefs.GetInt(HeightKey, 1080);
        public static float Volume => PlayerPrefs.GetFloat(VolumeKey, 0.8f);

        public static void ApplySaved()
        {
            AudioListener.volume = Mathf.Clamp01(Volume);
            Screen.SetResolution(Width, Height, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        public static void SaveAndApply(bool fullscreen, int width, int height, float volume)
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(WidthKey, width);
            PlayerPrefs.SetInt(HeightKey, height);
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
            ApplySaved();
        }

        public static List<Vector2Int> AvailableResolutions()
        {
            var list = Screen.resolutions
                .Select(r => new Vector2Int(r.width, r.height))
                .Distinct()
                .Where(v => v.x >= 1024 && v.y >= 576)
                .OrderBy(v => v.x).ThenBy(v => v.y)
                .ToList();

            if (list.Count == 0)
            {
                list.Add(new Vector2Int(1280, 720));
                list.Add(new Vector2Int(1600, 900));
                list.Add(new Vector2Int(1920, 1080));
            }
            return list;
        }
    }
}
