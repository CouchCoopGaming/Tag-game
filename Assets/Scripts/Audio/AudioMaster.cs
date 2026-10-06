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
        public const string MusicMutePrefsKey = "Tag.MusicMute";
        public const string MusicVolumePrefsKey = "Tag.MusicVolume";
        public const float DefaultVolume = 0.8f;
        public const float DefaultMusicVolume = 0.35f;

        static readonly float[] Steps = { 0f, 0.25f, 0.5f, 0.8f, 1f };
        static readonly string[] Names = { "Off", "Low", "Med", "Default", "Max" };
        static readonly float[] MusicSteps = { 0f, 0.15f, 0.35f, 0.55f, 1f };
        static readonly string[] MusicNames = { "Off", "Low", "Default", "High", "Max" };

        public static float Volume { get; private set; } = DefaultVolume;
        public static float MusicVolume { get; private set; } = DefaultMusicVolume;
        public static bool Muted { get; private set; }
        public static bool MusicMuted { get; private set; }

        public static string Label
        {
            get
            {
                Load();
                if (Muted) return "Muted";
                int i = IndexOf(Volume);
                string core = Names[i] + "  " + Volume.ToString("0.00");
                return MusicMuted ? core + "  music off" : core;
            }
        }

        public static string MusicLabel
        {
            get
            {
                Load();
                if (MusicMuted) return "Muted";
                int i = IndexOf(MusicVolume, MusicSteps);
                return MusicNames[i] + "  " + MusicVolume.ToString("0.00");
            }
        }

        public static void Load()
        {
            Volume = Nearest(PlayerPrefs.GetFloat(VolumePrefsKey, DefaultVolume));
            MusicVolume = Nearest(PlayerPrefs.GetFloat(MusicVolumePrefsKey, DefaultMusicVolume), MusicSteps);
            Muted = PlayerPrefs.GetInt(MutePrefsKey, 0) != 0;
            MusicMuted = PlayerPrefs.GetInt(MusicMutePrefsKey, 0) != 0;
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
            Tag.Settings.SettingsRuntime.NoteAudio(Volume, Muted);
            TagSfx.UiClick();
        }

        /// <summary>
        /// One listener for M/N. Pause and the audio card used to be the only callers,
        /// so play, Boot, and results ignored the keys the HUD lists.
        /// </summary>
        public static void PollMuteHotkeys()
        {
            // A rebind listen owns the next key. Comma stays mute. M is the minimap.
            if (Tag.Settings.SettingsMenuUi.Capturing) return;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Comma)) ToggleMute();
            if (UnityEngine.Input.GetKeyDown(KeyCode.N)) ToggleMusicMute();
        }

        public static void ToggleMute()
        {
            Load();
            Muted = !Muted;
            PlayerPrefs.SetInt(MutePrefsKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            Tag.Settings.SettingsRuntime.NoteAudio(Volume, Muted);
            TagSfx.UiClick();
        }

        /// <summary>Settings blob is the source of truth after boot. Steps are not re-snapped.</summary>
        public static void ApplyFromSettings(float volume, bool muted)
        {
            Volume = Mathf.Clamp01(volume);
            Muted = muted;
            PlayerPrefs.SetFloat(VolumePrefsKey, Volume);
            PlayerPrefs.SetInt(MutePrefsKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        public static void ToggleMusicMute()
        {
            Load();
            MusicMuted = !MusicMuted;
            PlayerPrefs.SetInt(MusicMutePrefsKey, MusicMuted ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            TagSfx.UiClick();
        }

        public static void CycleMusic(int dir)
        {
            Load();
            if (MusicMuted)
            {
                MusicMuted = false;
                PlayerPrefs.SetInt(MusicMutePrefsKey, 0);
                PlayerPrefs.Save();
            }
            int i = IndexOf(MusicVolume, MusicSteps);
            int next = Mathf.Clamp(i + dir, 0, MusicSteps.Length - 1);
            if (next == i && !MusicMuted)
            {
                Apply();
                return;
            }
            MusicVolume = MusicSteps[next];
            PlayerPrefs.SetFloat(MusicVolumePrefsKey, MusicVolume);
            PlayerPrefs.Save();
            Apply();
            Tag.Settings.SettingsRuntime.NoteMusic(MusicVolume);
            TagSfx.UiClick();
        }

        /// <summary>Settings music slider. Does not snap to a step.</summary>
        public static void ApplyMusicFromSettings(float music)
        {
            MusicVolume = Mathf.Clamp01(music);
            PlayerPrefs.SetFloat(MusicVolumePrefsKey, MusicVolume);
            PlayerPrefs.Save();
            Apply();
        }

        public static void ResetStatics()
        {
            Volume = DefaultVolume;
            MusicVolume = DefaultMusicVolume;
            Muted = false;
            MusicMuted = false;
        }

        public static void Apply()
        {
            AudioListener.volume = Muted ? 0f : Mathf.Clamp01(Volume);
            if (AudioCuePlayer.Instance != null)
                AudioCuePlayer.Instance.RefreshVolumes();
        }

        static int IndexOf(float v) => IndexOf(v, Steps);

        static int IndexOf(float v, float[] steps)
        {
            int best = 0;
            float bestD = Mathf.Abs(steps[0] - v);
            for (int i = 1; i < steps.Length; i++)
            {
                float d = Mathf.Abs(steps[i] - v);
                if (d < bestD)
                {
                    best = i;
                    bestD = d;
                }
            }
            return best;
        }

        static float Nearest(float v) => Steps[IndexOf(v)];

        static float Nearest(float v, float[] steps) => steps[IndexOf(v, steps)];
    }
}