using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Presentation only. Resolution, fullscreen, vsync, and quality are not
    /// gameplay feel. Reduce motion skips menu tweens. Nothing here is a motor number.
    /// </summary>
    public static class MenuVideo
    {
        public const string KeyW = "Tag.Ui.Width";
        public const string KeyFull = "Tag.Ui.Full";
        public const string KeyVSync = "Tag.Ui.VSync";
        public const string KeyQuality = "Tag.Ui.Quality";
        public const string KeyMotion = "Tag.Ui.ReduceMotion";

        public static readonly int[] Widths = { 1280, 1600, 1920, 2560 };
        public static readonly int[] Heights = { 720, 900, 1080, 1440 };

        public static int Index = 2;
        public static bool Full = true;
        public static bool VSync = true;
        public static int Quality;
        public static bool ReduceMotion;

        public static void Load()
        {
            ReduceMotion = PlayerPrefs.GetInt(KeyMotion, 0) == 1;
            if (!PlayerPrefs.HasKey(KeyW)) return;
            int w = PlayerPrefs.GetInt(KeyW, 1920);
            Index = 2;
            for (int i = 0; i < Widths.Length; i++)
            {
                if (Widths[i] == w) Index = i;
            }
            Full = PlayerPrefs.GetInt(KeyFull, 1) == 1;
            VSync = PlayerPrefs.GetInt(KeyVSync, 1) == 1;
            Quality = PlayerPrefs.GetInt(KeyQuality, 0);
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetInt(KeyW, Widths[Index]);
            PlayerPrefs.SetInt(KeyFull, Full ? 1 : 0);
            PlayerPrefs.SetInt(KeyVSync, VSync ? 1 : 0);
            PlayerPrefs.SetInt(KeyQuality, Quality);
            PlayerPrefs.SetInt(KeyMotion, ReduceMotion ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void CycleRes(int dir)
        {
            Index += dir < 0 ? -1 : 1;
            if (Index < 0) Index = 0;
            if (Index >= Widths.Length) Index = Widths.Length - 1;
            Save();
            Apply();
        }

        public static void ToggleFull()
        {
            Full = !Full;
            Save();
            Apply();
        }

        public static void ToggleVSync()
        {
            VSync = !VSync;
            Save();
            Apply();
        }

        public static void CycleQuality(int dir)
        {
            int n = QualityNames();
            if (n < 1) n = 1;
            Quality += dir < 0 ? -1 : 1;
            if (Quality < 0) Quality = 0;
            if (Quality >= n) Quality = n - 1;
            Save();
            Apply();
        }

        public static void ToggleMotion()
        {
            ReduceMotion = !ReduceMotion;
            Save();
        }

        public static string ResLabel()
        {
            return Widths[Index].ToString() + " x " + Heights[Index].ToString();
        }

        public static string QualityLabel()
        {
            string[] names = QualitySettings.names;
            if (names == null || names.Length == 0) return "Default";
            int i = Quality;
            if (i < 0) i = 0;
            if (i >= names.Length) i = names.Length - 1;
            return names[i];
        }

        public static int QualityNames()
        {
            string[] names = QualitySettings.names;
            if (names == null) return 0;
            return names.Length;
        }

        public static void Apply()
        {
            try
            {
                QualitySettings.vSyncCount = VSync ? 1 : 0;
                int n = QualityNames();
                if (n > 0)
                {
                    int q = Quality;
                    if (q < 0) q = 0;
                    if (q >= n) q = n - 1;
                    QualitySettings.SetQualityLevel(q, true);
                }
                int i = Index;
                if (i < 0) i = 0;
                if (i >= Widths.Length) i = Widths.Length - 1;
                Screen.SetResolution(Widths[i], Heights[i], Full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
            catch (System.Exception)
            {
                // Batch mode and the headless compile host have no display.
            }
        }
    }
}
