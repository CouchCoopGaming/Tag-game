using UnityEngine;

namespace Tag.Audio
{
    /// <summary>
    /// Master loudness stub. Default is full and unmuted, so existing tones stay the same.
    /// AudioListener.volume scales PlayClipAtPoint and every AudioSource.
    /// </summary>
    public static class MasterVolume
    {
        public const string LevelKey = "Tag.MasterVolume";
        public const string MuteKey = "Tag.Muted";

        static readonly float[] Steps = { 0.25f, 0.5f, 0.75f, 1f };
        static bool _loaded;

        public static float Level { get; private set; } = 1f;
        public static bool Muted { get; private set; }

        public static string Label => Muted
            ? "Muted (" + Mathf.RoundToInt(Level * 100f) + "%)"
            : Mathf.RoundToInt(Level * 100f) + "%";

        public static void Ensure()
        {
            if (_loaded) return;
            Load();
        }

        public static void Load()
        {
            _loaded = true;
            float raw = PlayerPrefs.GetFloat(LevelKey, 1f);
            Level = 1f;
            float best = 999f;
            for (int i = 0; i < Steps.Length; i++)
            {
                float d = Mathf.Abs(Steps[i] - raw);
                if (d < best)
                {
                    best = d;
                    Level = Steps[i];
                }
            }
            Muted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
            Apply();
        }

        public static void Apply()
        {
            AudioListener.volume = Muted ? 0f : Level;
        }

        public static void Cycle(int dir)
        {
            Ensure();
            int i = 0;
            float best = 999f;
            for (int n = 0; n < Steps.Length; n++)
            {
                float d = Mathf.Abs(Steps[n] - Level);
                if (d < best)
                {
                    best = d;
                    i = n;
                }
            }
            int next = Mathf.Clamp(i + dir, 0, Steps.Length - 1);
            if (next == i) return;
            Level = Steps[next];
            PlayerPrefs.SetFloat(LevelKey, Level);
            PlayerPrefs.Save();
            Apply();
            if (!Muted) TagSfx.UiClick();
        }

        public static void ToggleMute()
        {
            Ensure();
            Muted = !Muted;
            PlayerPrefs.SetInt(MuteKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            if (!Muted) TagSfx.UiClick();
        }
    }
}
