using UnityEngine;

namespace Tag.Audio
{
    /// <summary>
    /// Placeholder SFX: prefer Resources/Audio clips when present, else tiny procedural tones
    /// via AudioClip.Create (no asset store purchases). Modest volumes.
    /// </summary>
    public static class TagSfx
    {
        const float DefaultVol = 0.45f;
        const int SampleRate = 22050;

        static AudioClip _punch;
        static AudioClip _tag;
        static AudioClip _ski;
        static AudioClip _jet;
        static AudioClip _land;
        static AudioClip _slide;
        static AudioClip _jump;
        static AudioClip _miss;
        static AudioClip _lunge;
        static AudioClip _airDash;
        static AudioClip _trail;
        static AudioClip _roundStart;
        static AudioClip _roundEnd;
        static AudioClip _roundWin;
        static AudioClip _roundLose;
        static AudioClip _uiClick;
        static AudioClip _uiConfirm;
        static AudioSource _flat;

        public static AudioClip Punch => _punch ??= Resolve("SFX/sfx_punch_hit", () => MakeImpact(180f, 0.07f, 0.55f));
        public static AudioClip Tag => _tag ??= Resolve("SFX/sfx_tag_transfer", () => MakeChirp(520f, 780f, 0.12f, 0.4f));
        public static AudioClip Ski => _ski ??= Resolve("SFX/sfx_slide", () => MakeNoiseWhoosh(0.14f, 0.35f, 900f));
        public static AudioClip Jet => _jet ??= Resolve("SFX/sfx_air_dodge", () => MakeNoiseWhoosh(0.11f, 0.32f, 1400f));
        public static AudioClip Land => _land ??= MakeThud(90f, 0.09f, 0.5f);
        public static AudioClip Slide => _slide ??= Resolve("SFX/sfx_slide", () => MakeNoiseWhoosh(0.12f, 0.38f, 700f));
        public static AudioClip Jump => _jump ??= MakeBlip(320f, 0.06f, 0.28f);
        public static AudioClip Miss => _miss ??= Resolve("SFX/sfx_punch_miss", () => MakeBlip(300f, 0.045f, 0.14f));
        public static AudioClip Lunge => _lunge ??= Resolve("SFX/sfx_air_dodge", () => MakeNoiseWhoosh(0.13f, 0.42f, 1100f));
        public static AudioClip AirDash => _airDash ??= MakeNoiseWhoosh(0.08f, 0.36f, 1800f);
        public static AudioClip TrailElimClip => _trail ??= MakeChirp(880f, 220f, 0.16f, 0.45f);
        public static AudioClip RoundStartClip => _roundStart ??= MakeChirp(440f, 880f, 0.18f, 0.4f);
        public static AudioClip RoundEndClip => _roundEnd ??= MakeChirp(520f, 180f, 0.22f, 0.4f);
        public static AudioClip RoundWinClip => _roundWin ??= MakeChirp(660f, 990f, 0.2f, 0.42f);
        public static AudioClip RoundLoseClip => _roundLose ??= MakeThud(70f, 0.16f, 0.45f);
        public static AudioClip UiClickClip => _uiClick ??= MakeBlip(680f, 0.04f, 0.22f);
        public static AudioClip UiConfirmClip => _uiConfirm ??= MakeChirp(520f, 740f, 0.08f, 0.28f);

        public static AudioSource EnsureSource(GameObject host)
        {
            if (host == null) return null;
            var src = host.GetComponent<AudioSource>();
            if (src == null) src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0.7f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.maxDistance = 28f;
            src.volume = 0.75f;
            return src;
        }

        public static void Play(AudioSource src, AudioClip clip, float vol = DefaultVol)
        {
            if (clip == null) return;
            vol = Mathf.Clamp01(vol);
            if (src != null)
            {
                src.pitch = 1f + Random.Range(-0.04f, 0.04f);
                src.PlayOneShot(clip, vol);
                return;
            }
            AudioSource.PlayClipAtPoint(clip, Vector3.zero, vol);
        }

        public static void PlayAt(AudioClip clip, Vector3 pos, float vol = DefaultVol)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, pos, Mathf.Clamp01(vol));
        }

        public static void PunchConnect(Vector3 pos) => PlayAt(Punch, pos, 0.62f);
        /// <summary>Soft fail: quieter + slightly higher than PunchConnect.</summary>
        public static void PunchMiss(Vector3 pos)
        {
            var clip = Miss;
            if (clip == null) return;
            var go = new GameObject("TagSfx_Miss");
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0.65f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.maxDistance = 22f;
            src.pitch = 1.15f + Random.Range(-0.04f, 0.04f);
            src.volume = 0.28f;
            src.clip = clip;
            src.Play();
            Object.Destroy(go, clip.length / Mathf.Max(0.5f, src.pitch) + 0.08f);
        }
        public static void BecomeIt(Vector3 pos) => PlayAt(Tag, pos, 0.48f);
        public static void SkiStart(AudioSource src) => Play(src, Ski, 0.4f);
        public static void JetStart(AudioSource src) => Play(src, Jet, 0.38f);
        public static void LandImpact(AudioSource src) => Play(src, Land, 0.42f);
        public static void LungeWhoosh(Vector3 pos) => PlayAt(Lunge, pos, 0.42f);
        public static void PlayAirDash(Vector3 pos) => PlayAt(AirDash, pos, 0.4f);
        public static void TrailElim(Vector3 pos) => PlayAt(TrailElimClip, pos, 0.5f);
        public static void LandAt(Vector3 pos, float vol = 0.32f) => PlayAt(Land, pos, vol);
        public static void RoundStart() => PlayFlat(RoundStartClip, 0.45f);
        public static void RoundEnd() => PlayFlat(RoundEndClip, 0.45f);
        public static void RoundWin() => PlayFlat(RoundWinClip, 0.48f);
        public static void RoundLose() => PlayFlat(RoundLoseClip, 0.48f);
        public static void UiClick() => PlayFlat(UiClickClip, 0.4f);
        public static void UiConfirm() => PlayFlat(UiConfirmClip, 0.42f);

        /// <summary>2D bed so round/UI tones are not played at world origin on a mega park.</summary>
        public static void PlayFlat(AudioClip clip, float vol = DefaultVol)
        {
            if (clip == null) return;
            if (_flat == null)
            {
                var go = new GameObject("TagSfx2D");
                Object.DontDestroyOnLoad(go);
                _flat = go.AddComponent<AudioSource>();
                _flat.playOnAwake = false;
                _flat.spatialBlend = 0f;
            }
            Play(_flat, clip, vol);
        }

        static AudioClip Resolve(string resourcesPath, System.Func<AudioClip> procedural)
        {
            var c = Resources.Load<AudioClip>("Audio/" + resourcesPath);
            if (c == null) c = Resources.Load<AudioClip>(resourcesPath);
            return c != null ? c : procedural();
        }

        static AudioClip MakeBlip(float hz, float dur, float amp)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(SampleRate * dur));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = 1f - (i / (float)n);
                env *= env;
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * amp * env;
            }
            return Build("sfx_proc_blip_" + (int)hz, data);
        }

        static AudioClip MakeChirp(float hz0, float hz1, float dur, float amp)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(SampleRate * dur));
            var data = new float[n];
            double phase = 0.0;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float hz = Mathf.Lerp(hz0, hz1, u);
                phase += 2.0 * Mathf.PI * hz / SampleRate;
                float env = Mathf.Sin(Mathf.PI * u);
                data[i] = (float)(System.Math.Sin(phase) * amp * env);
            }
            return Build("sfx_proc_chirp", data);
        }

        static AudioClip MakeThud(float hz, float dur, float amp)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(SampleRate * dur));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-18f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * hz * t);
                float noise = (Random.value * 2f - 1f) * 0.35f;
                data[i] = (tone * 0.7f + noise * 0.3f) * amp * env;
            }
            return Build("sfx_proc_thud", data);
        }

        static AudioClip MakeImpact(float hz, float dur, float amp)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(SampleRate * dur));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-28f * t);
                float click = i < 3 ? (Random.value * 2f - 1f) : 0f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * hz * t) * 0.6f + click * 0.5f) * amp * env;
            }
            return Build("sfx_proc_impact", data);
        }

        static AudioClip MakeNoiseWhoosh(float dur, float amp, float cutoffHint)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(SampleRate * dur));
            var data = new float[n];
            float prev = 0f;
            float alpha = Mathf.Clamp01(cutoffHint / 4000f);
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                float env = Mathf.Sin(Mathf.PI * u);
                float noise = Random.value * 2f - 1f;
                prev = prev + alpha * (noise - prev);
                data[i] = prev * amp * env;
            }
            return Build("sfx_proc_whoosh_" + (int)cutoffHint, data);
        }

        static AudioClip Build(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
