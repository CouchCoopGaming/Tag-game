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
        static AudioClip _uiBack;
        static AudioClip _uiMove;
        static AudioClip _thunk;
        static AudioClip _slideLoop;
        static AudioClip _climb;
        static AudioClip _patter;
        static AudioClip[] _hooks;

        public static AudioClip Punch => _punch ??= HookClip(AudioBus.Hook.PunchHit);
        public static AudioClip Tag => _tag ??= HookClip(AudioBus.Hook.Tag);
        public static AudioClip Ski => _ski ??= HookClip(AudioBus.Hook.SlideStart);
        public static AudioClip Jet => _jet ??= HookClip(AudioBus.Hook.AirDash);
        public static AudioClip Land => _land ??= HookClip(AudioBus.Hook.LandSoft);
        public static AudioClip Slide => _slide ??= HookClip(AudioBus.Hook.SlideStart);
        public static AudioClip SlideLoop => _slideLoop ??= HookClip(AudioBus.Hook.SlideLoop);
        public static AudioClip Jump => _jump ??= HookClip(AudioBus.Hook.Jump);
        public static AudioClip Miss => _miss ??= HookClip(AudioBus.Hook.PunchWhiff);
        public static AudioClip Lunge => _lunge ??= HookClip(AudioBus.Hook.AirDash);
        public static AudioClip AirDash => _airDash ??= HookClip(AudioBus.Hook.AirDash);
        public static AudioClip TrailElimClip => _trail ??= Resolve("SFX/sfx_trail_elim", () => MakeChirp(880f, 220f, 0.16f, 0.45f));
        public static AudioClip RoundStartClip => _roundStart ??= Resolve("SFX/sfx_round_start", () => MakeChirp(440f, 880f, 0.18f, 0.4f));
        public static AudioClip RoundEndClip => _roundEnd ??= HookClip(AudioBus.Hook.RoundEnd);
        public static AudioClip RoundWinClip => _roundWin ??= Resolve("SFX/sfx_round_win", () => MakeChirp(660f, 990f, 0.2f, 0.42f));
        public static AudioClip RoundLoseClip => _roundLose ??= Resolve("SFX/sfx_round_lose", () => MakeThud(70f, 0.16f, 0.45f));
        public static AudioClip UiClickClip => _uiClick ??= Resolve("UI/ui_click", () => MakeBlip(720f, 0.035f, 0.22f));
        public static AudioClip UiConfirmClip => _uiConfirm ??= Resolve("UI/ui_confirm", () => MakeChirp(520f, 780f, 0.08f, 0.28f));
        public static AudioClip UiBackClip => _uiBack ??= Resolve("UI/ui_back", () => MakeChirp(620f, 320f, 0.07f, 0.24f));
        public static AudioClip UiMoveClip => _uiMove ??= Resolve("UI/ui_move", () => MakeBlip(640f, 0.032f, 0.18f));
        public static AudioClip Thunk => _thunk ??= HookClip(AudioBus.Hook.ClingGrab);
        public static AudioClip ClimbScuff => _climb ??= Resolve("SFX/sfx_climb_scuff", () => MakeNoiseWhoosh(0.045f, 0.3f, 1400f));
        public static AudioClip WallPatter => _patter ??= Resolve("SFX/sfx_wallrun", () => MakeNoiseWhoosh(0.032f, 0.22f, 2400f));

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
            if (src != null && src.spatialBlend > 0.1f)
                AudioMix.PlayWorld(clip, src.transform.position, vol, VoiceBudget.PriSlide, false, 1f);
            else
                AudioMix.PlayFlat(clip, vol, VoiceBudget.PriUi, true);
        }

        public static void PlayAt(AudioClip clip, Vector3 pos, float vol = DefaultVol, int priority = -1)
        {
            if (priority < 0) priority = VoiceBudget.PriJump;
            AudioMix.PlayWorld(clip, pos, vol, priority, false, 1f);
        }

        public static void PlayHook(AudioBus.Hook hook, Vector3 pos)
        {
            int i = (int)hook;
            if (i < 0 || i >= ClipCatalog.Count) return;
            AudioClip clip = HookClip(hook);
            if (ClipCatalog.Flat(i))
                AudioMix.PlayFlat(clip, ClipCatalog.Volumes[i], ClipCatalog.Priorities[i], false);
            else
                AudioMix.PlayWorld(clip, pos, ClipCatalog.Volumes[i], ClipCatalog.Priorities[i], false, 1f);
        }

        public static AudioClip HookClip(AudioBus.Hook hook)
        {
            int i = (int)hook;
            if (_hooks == null) _hooks = new AudioClip[ClipCatalog.Count];
            if (i < 0 || i >= _hooks.Length) return null;
            if (_hooks[i] == null)
                _hooks[i] = Resolve(ClipCatalog.Files[i], () => Fallback(hook));
            return _hooks[i];
        }

        static AudioClip[] _steps;

        public static AudioClip StepClip(int surface)
        {
            if (_steps == null) _steps = new AudioClip[FootstepMap.SurfaceCount];
            if (surface < 0 || surface >= _steps.Length) surface = 0;
            if (_steps[surface] == null)
                _steps[surface] = Resolve(FootstepMap.File((FootstepMap.Surface)surface), () => MakeThud(140f + surface * 30f, 0.05f, 0.4f));
            return _steps[surface];
        }

        public static void PunchConnect(Vector3 pos) => PlayHook(AudioBus.Hook.PunchHit, pos);
        /// <summary>Whiff: air only, no body impact.</summary>
        public static void PunchMiss(Vector3 pos) => PlayHook(AudioBus.Hook.PunchWhiff, pos);
        public static void BecomeIt(Vector3 pos) => PlayHook(AudioBus.Hook.Tag, pos);
        /// <summary>Tag-back immunity: a glass shimmer, not a second punch.</summary>
        public static void TagBackThunk(Vector3 pos) => PlayHook(AudioBus.Hook.TagBackBlocked, pos);
        public static void SkiStart(AudioSource src) => Play(src, Ski, 0.4f);
        public static void JetStart(AudioSource src) => Play(src, Jet, 0.38f);
        public static void LandImpact(AudioSource src) => Play(src, Land, 0.42f);
        public static void LungeWhoosh(Vector3 pos) => PlayAt(Lunge, pos, 0.42f, VoiceBudget.PriDash);
        public static void PlayAirDash(Vector3 pos) => PlayHook(AudioBus.Hook.AirDash, pos);
        public static void TrailElim(Vector3 pos) => PlayAt(TrailElimClip, pos, 0.5f, VoiceBudget.PriTag);
        public static void LandAt(Vector3 pos, float vol = 0.32f) => PlayAt(Land, pos, vol, vol >= 0.4f ? VoiceBudget.PriLandHard : VoiceBudget.PriLandSoft);
        public static void RoundStart() => AudioMix.PlayFlat(RoundStartClip, 0.48f, VoiceBudget.PriRound, false);
        public static void RoundEnd() => PlayHook(AudioBus.Hook.RoundEnd, Vector3.zero);
        public static void RoundWin() => AudioMix.PlayFlat(RoundWinClip, 0.5f, VoiceBudget.PriRound, false);
        public static void RoundLose() => AudioMix.PlayFlat(RoundLoseClip, 0.5f, VoiceBudget.PriRound, false);
        public static void RoundTick() => AudioMix.PlayFlat(RoundTickClip, 0.36f, VoiceBudget.PriRound, false);
        public static void UiClick() => AudioMix.PlayFlat(UiClickClip, 0.36f, VoiceBudget.PriUi, true);
        public static void UiConfirm() => AudioMix.PlayFlat(UiConfirmClip, 0.4f, VoiceBudget.PriUi, true);
        public static void UiBack() => AudioMix.PlayFlat(UiBackClip, 0.36f, VoiceBudget.PriUi, true);
        public static void UiMove() => AudioMix.PlayFlat(UiMoveClip, 0.28f, VoiceBudget.PriUi, true);
        public static void CountdownBeep() => PlayHook(AudioBus.Hook.CountdownBeep, Vector3.zero);

        static AudioClip _roundTick;
        static AudioClip RoundTickClip => _roundTick ??= Resolve("SFX/sfx_round_tick", () => MakeBlip(660f, 0.04f, 0.26f));

        /// <summary>2D one-shot on the SFX bus. UI clicks use the UI methods.</summary>
        public static void PlayFlat(AudioClip clip, float vol = DefaultVol)
        {
            AudioMix.PlayFlat(clip, vol, VoiceBudget.PriRound, false);
        }

        static AudioClip Fallback(AudioBus.Hook hook)
        {
            switch (hook)
            {
                case AudioBus.Hook.Jump: return MakeBlip(420f, 0.07f, 0.35f);
                case AudioBus.Hook.LandSoft: return MakeThud(96f, 0.1f, 0.35f);
                case AudioBus.Hook.LandHard: return MakeThud(52f, 0.14f, 0.55f);
                case AudioBus.Hook.SlideStart:
                case AudioBus.Hook.SlideLoop:
                case AudioBus.Hook.SlideEnd: return MakeNoiseWhoosh(0.12f, 0.35f, 900f);
                case AudioBus.Hook.ClingGrab: return MakeThud(170f, 0.06f, 0.4f);
                case AudioBus.Hook.WallJump: return MakeNoiseWhoosh(0.1f, 0.4f, 1200f);
                case AudioBus.Hook.AirDash: return MakeNoiseWhoosh(0.07f, 0.4f, 2000f);
                case AudioBus.Hook.PunchWhiff: return MakeNoiseWhoosh(0.06f, 0.25f, 3000f);
                case AudioBus.Hook.PunchHit: return MakeImpact(145f, 0.08f, 0.6f);
                case AudioBus.Hook.Tag: return MakeChirp(880f, 1318f, 0.2f, 0.45f);
                case AudioBus.Hook.TagBackBlocked: return MakeChirp(1960f, 2480f, 0.22f, 0.28f);
                case AudioBus.Hook.Stagger: return MakeThud(78f, 0.09f, 0.45f);
                case AudioBus.Hook.PadLaunch: return MakeChirp(160f, 80f, 0.18f, 0.45f);
                case AudioBus.Hook.ZipGrab: return MakeImpact(980f, 0.05f, 0.35f);
                case AudioBus.Hook.ZipLoop: return MakeBlip(510f, 0.2f, 0.2f);
                case AudioBus.Hook.ZipDrop: return MakeChirp(520f, 160f, 0.1f, 0.3f);
                case AudioBus.Hook.CountdownBeep: return MakeBlip(880f, 0.05f, 0.3f);
                default: return MakeChirp(523f, 196f, 0.24f, 0.4f);
            }
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
