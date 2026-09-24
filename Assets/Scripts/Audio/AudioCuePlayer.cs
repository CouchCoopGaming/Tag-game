using UnityEngine;

namespace Tag.Audio
{
    /// <summary>Null-safe one-shots + music bed. Loads clips from Resources/Audio mirroring Assets/Audio.</summary>
    public class AudioCuePlayer : MonoBehaviour
    {
        public static AudioCuePlayer Instance { get; private set; }

        [SerializeField] float pitchJitter = 0.05f;
        AudioSource _sfx;
        AudioSource _ui;
        AudioSource _music;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false;
            _sfx.volume = 0.7f;
            _ui = gameObject.AddComponent<AudioSource>();
            _ui.playOnAwake = false;
            _ui.volume = 0.65f;
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.volume = 0.35f;
        }

        public static AudioCuePlayer Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("AudioCuePlayer");
            return go.AddComponent<AudioCuePlayer>();
        }

        public void PlaySfx(string resourcesPath, Vector3? pos = null)
        {
            AudioMaster.Apply();
            var clip = Load(resourcesPath);
            if (clip == null)
            {
                PlayProcedural(resourcesPath, pos);
                return;
            }
            _sfx.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            if (pos.HasValue)
                AudioSource.PlayClipAtPoint(clip, pos.Value, 0.55f);
            else
                _sfx.PlayOneShot(clip);
        }

        public void PlayUi(string resourcesPath)
        {
            AudioMaster.Apply();
            var clip = Load(resourcesPath);
            if (clip == null)
            {
                PlayProcedural(resourcesPath, null);
                return;
            }
            _ui.pitch = 1f;
            _ui.PlayOneShot(clip);
        }

        /// <summary>
        /// Resources clips are optional. Same events already have TagSfx procedural tones.
        /// </summary>
        static void PlayProcedural(string resourcesPath, Vector3? pos)
        {
            Vector3 p = pos ?? Vector3.zero;
            switch (resourcesPath)
            {
                case "SFX/sfx_punch_hit": TagSfx.PunchConnect(p); break;
                case "SFX/sfx_punch_miss": TagSfx.PunchMiss(p); break;
                case "SFX/sfx_ragdoll": TagSfx.LandAt(p, 0.5f); break;
                case "SFX/sfx_slide": TagSfx.PlayAt(TagSfx.Slide, p, 0.4f); break;
                case "SFX/sfx_air_dodge": TagSfx.PlayAirDash(p); break;
                case "SFX/sfx_tag_transfer": TagSfx.BecomeIt(p); break;
                case "SFX/sfx_trail_elim": TagSfx.TrailElim(p); break;
                case "SFX/sfx_round_start": TagSfx.RoundStart(); break;
                case "SFX/sfx_round_end": TagSfx.RoundEnd(); break;
                case "SFX/sfx_round_win": TagSfx.RoundWin(); break;
                case "SFX/sfx_round_lose": TagSfx.RoundLose(); break;
                case "UI/ui_click": TagSfx.UiClick(); break;
                case "UI/ui_confirm": TagSfx.UiConfirm(); break;
            }
        }

        public void PlayMusic(string resourcesPath)
        {
            AudioMaster.Apply();
            var clip = Load(resourcesPath);
            if (clip == null) return;
            if (_music.clip == clip && _music.isPlaying) return;
            _music.clip = clip;
            _music.loop = true;
            _music.volume = 0.35f;
            _music.Play();
        }

        public void StopMusic()
        {
            if (_music != null) _music.Stop();
        }

        static AudioClip Load(string path)
        {
            // Prefer Resources/Audio/... ; Editor can also load from Assets/Audio via Resources copy
            var c = Resources.Load<AudioClip>("Audio/" + path);
            if (c != null) return c;
            c = Resources.Load<AudioClip>(path);
            return c;
        }

        // Convenience
        public void PunchHit(Vector3 p) => PlaySfx("SFX/sfx_punch_hit", p);
        public void PunchMiss(Vector3 p) => PlaySfx("SFX/sfx_punch_miss", p);
        public void Ragdoll(Vector3 p) => PlaySfx("SFX/sfx_ragdoll", p);
        public void Slide(Vector3 p) => PlaySfx("SFX/sfx_slide", p);
        public void AirDodge(Vector3 p) => PlaySfx("SFX/sfx_air_dodge", p);
        public void TagTransfer(Vector3 p) => PlaySfx("SFX/sfx_tag_transfer", p);
        public void TrailElim(Vector3 p) => PlaySfx("SFX/sfx_trail_elim", p);
        public void RoundStart() => PlaySfx("SFX/sfx_round_start");
        public void RoundEnd() => PlaySfx("SFX/sfx_round_end");
        public void RoundWin() => PlaySfx("SFX/sfx_round_win");
        public void RoundLose() => PlaySfx("SFX/sfx_round_lose");
        public void UiClick() => PlayUi("UI/ui_click");
        public void UiConfirm() => PlayUi("UI/ui_confirm");
        public void PlaygroundMusic() => PlayMusic("Music/music_playground_bed_loop");
    }
}

