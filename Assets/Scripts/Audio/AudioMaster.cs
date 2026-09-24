using UnityEngine;

namespace Tag.Audio
{
    /// <summary>
    /// Master volume + mute stub. Drives AudioListener.volume. Saved in PlayerPrefs.
    /// </summary>
    public static class AudioMaster
    {
        public const string VolumePrefsKey = "Tag.MasterVolume";
        public const string MutePrefsKey = "Tag.MasterMute";
        public const float DefaultVolume = 0.8f;

        static readonly float[] Steps = { 0f, 0.25f, 0.5f, 0.8f, 1f };
        static readonly string[] Names = { "Off", "Low", "Med", "Default", "Max" };

        public static float Volume { get; private set; } = DefaultVolume;
        public static bool Muted { get; private set; }

        public static string Label
        {
            get
            {
                Load();
                if (Muted) return "Muted";
                int i = IndexOf(Volume);
                return Names[i] + "  " + Volume.ToString("0.00");
            }
        }

        public static void Load()
        {
            Volume = Nearest(PlayerPrefs.GetFloat(VolumePrefsKey, DefaultVolume));
            Muted = PlayerPrefs.GetInt(MutePrefsKey, 0) != 0;
            Apply();
        }

        public static void CycleVolume(int dir)
        {
            Load();
            if (Muted)
            {
                Muted = false;
                PlayerPrefs.SetInt(MutePrefsKey, 0);
            }
            int i = IndexOf(Volume);
            int next = Mathf.Clamp(i + dir, 0, Steps.Length - 1);
            if (next == i && !Muted)
            {
                Apply();
                return;
            }
            Volume = Steps[next];
            PlayerPrefs.SetFloat(VolumePrefsKey, Volume);
            PlayerPrefs.Save();
            Apply();
            TagSfx.UiClick();
        }

        public static void ToggleMute()
        {
            Load();
            Muted = !Muted;
            PlayerPrefs.SetInt(MutePrefsKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            TagSfx.UiClick();
        }

        public static void Apply()
        {
            AudioListener.volume = Muted ? 0f : Mathf.Clamp01(Volume);
        }

        static int IndexOf(float v)
        {
            int best = 0;
            float bestD = Mathf.Abs(Steps[0] - v);
            for (int i = 1; i < Steps.Length; i++)
            {
                float d = Mathf.Abs(Steps[i] - v);
                if (d < bestD)
                {
                    best = i;
                    bestD = d;
                }
            }
            return best;
        }

        static float Nearest(float v) => Steps[IndexOf(v)];
    }
}