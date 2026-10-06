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

        public static void PlayWorld(AudioClip clip, Vector3 pos, float volume, int priority, bool itLouder, float pitchScale)
        {
            float vol = volume * SfxGain();
            if (itLouder) vol *= VoiceBudget.ItFootstepGain;
            Play(clip, pos, vol, priority, pitchScale, true);
        }

        public static void PlayFlat(AudioClip clip, float volume, int priority, bool ui)
        {
            float vol = volume * (ui ? UiGain() : SfxGain());
            Play(clip, Vector3.zero, vol, priority, 1f, false);
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

        static void Play(AudioClip clip, Vector3 pos, float volume, int priority, float pitchScale, bool spatial)
        {
            if (clip == null || volume <= 0.001f) return;
            if (AudioMaster.Muted || AudioMaster.Volume <= 0.001f) return;
            Ensure();
            float pitch = pitchScale;
            if (spatial) pitch += Jitter(FootstepMap.PitchJitter);
            if (pitch < 0.5f) pitch = 0.5f;
            if (pitch > 1.6f) pitch = 1.6f;
            float dur = clip.length / pitch;
            int slot = VoiceBudget.Admit(priority, dur, Time.time);
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
