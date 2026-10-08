using Tag.Settings;
using UnityEngine;

namespace Tag.Audio
{
    /// <summary>
    /// One pool of voices. World cues are 3D out to about 40 m.
    /// UI and round cues are 2D. The It pawn's footsteps sit a little hotter.
    /// </summary>
    public static class AudioMix
    {
        static AudioSource[] _voices;
        static GameObject _root;
        static uint _rng = 0xC0FFEEu;

        public static bool WorldPaused { get; private set; }

        /// <summary>
        /// Master and mute are the listener. Sfx, UI, and music are the bus gains
        /// PlayWorld and PlayFlat already multiply. There is no AudioMixer asset.
        /// </summary>
        public static void ApplyBuses()
        {
            Tag.Ui.Menu.OptionApply.SnapBuses(GameSettings.Current ?? GameSettings.Defaults());
        }

        public static void SetWorldPaused(bool paused)
        {
            WorldPaused = paused;
            if (!paused) return;
            VoiceBudget.SilenceWorld();
            if (_voices == null) return;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (VoiceBudget.Occupied(i)) continue;
                AudioSource src = _voices[i];
                if (src != null && src.isPlaying) src.Stop();
            }
        }

        /// <summary>Master, mute, the bus slider, and pause. UI is the only bus that plays while paused.</summary>
        public static bool WouldPlay(float bus, bool ui)
        {
            if (AudioMaster.Muted || AudioMaster.Volume <= 0.001f) return false;
            if (bus <= 0.001f) return false;
            if (!ui && WorldPaused) return false;
            return true;
        }

        public static float MusicLevel(float music, bool musicMuted)
        {
            if (!WouldPlay(musicMuted ? 0f : music, false)) return 0f;
            return music < 0f ? 0f : music;
        }

        public static void PlayWorld(AudioClip clip, Vector3 pos, float volume, int priority, bool itLouder, float pitchScale)
        {
            float vol = volume * SfxGain();
            if (itLouder) vol *= VoiceBudget.ItFootstepGain;
            Play(clip, pos, vol, priority, pitchScale, true, false);
        }

        public static void PlayFlat(AudioClip clip, float volume, int priority, bool ui)
        {
            float vol = volume * (ui ? UiGain() : SfxGain());
            Play(clip, Vector3.zero, vol, priority, 1f, false, ui);
        }

        public static void Pump()
        {
            if (_voices == null) return;
            float now = Time.time;
            for (int i = 0; i < _voices.Length; i++)
            {
                if (!VoiceBudget.Occupied(i)) continue;
                AudioSource src = _voices[i];
                bool done = src == null || !src.isPlaying || now >= VoiceBudget.End(i);
                if (!done) continue;
                if (src != null && src.isPlaying) src.Stop();
                VoiceBudget.Free(i);
            }
        }

        static void Play(AudioClip clip, Vector3 pos, float volume, int priority, float pitchScale, bool spatial, bool ui)
        {
            if (clip == null || volume <= 0.001f) return;
            if (!WouldPlay(1f, ui)) return;
            Ensure();
            float pitch = pitchScale;
            if (spatial) pitch += Jitter(FootstepMap.PitchJitter);
            if (pitch < 0.5f) pitch = 0.5f;
            if (pitch > 1.6f) pitch = 1.6f;
            float dur = clip.length / pitch;
            int slot = VoiceBudget.Admit(priority, dur, Time.time, ui);
            if (slot < 0) return;
            AudioSource src = _voices[slot];
            if (src == null)
            {
                VoiceBudget.Free(slot);
                return;
            }
            src.transform.position = pos;
            if (spatial)
            {
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 1.5f;
                src.maxDistance = VoiceBudget.HearMeters;
            }
            else
            {
                src.spatialBlend = 0f;
            }
            src.dopplerLevel = 0f;
            src.pitch = pitch;
            src.volume = volume > 1f ? 1f : volume;
            src.loop = false;
            src.clip = clip;
            src.Play();
        }

        static void Ensure()
        {
            if (_voices != null) return;
            _root = new GameObject("AudioVoices");
            Object.DontDestroyOnLoad(_root);
            _voices = new AudioSource[VoiceBudget.Cap];
            for (int i = 0; i < _voices.Length; i++)
            {
                var go = new GameObject("Voice");
                go.transform.SetParent(_root.transform, false);
                AudioSource src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 1.5f;
                src.maxDistance = VoiceBudget.HearMeters;
                src.dopplerLevel = 0f;
                _voices[i] = src;
            }
        }

        static float SfxGain()
        {
            GameSettings s = GameSettings.Current;
            return s != null ? s.Sfx : 1f;
        }

        static float UiGain()
        {
            GameSettings s = GameSettings.Current;
            return s != null ? s.Ui : 1f;
        }

        static float Jitter(float amount)
        {
            _rng = _rng * 1664525u + 1013904223u;
            float u = ((_rng >> 8) & 0xFFFFFFu) / 16777215f;
            return (u * 2f - 1f) * amount;
        }
    }
}
