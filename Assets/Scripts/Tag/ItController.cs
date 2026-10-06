using UnityEngine;
using Tag.Audio;
using Tag.Modes;
using TagArena.Movement;

namespace Tag.Gameplay
{
    /// <summary>Per-player It flag, i-frames during ragdoll, time-as-It accumulator, elimination.</summary>
    public class ItController : MonoBehaviour
    {
        [SerializeField] bool isIt;
        [SerializeField] Renderer accentRenderer;
        [SerializeField] Color itColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] Color runnerColor = new Color(0.3f, 0.7f, 1f);
        [SerializeField] Color eliminatedColor = new Color(0.25f, 0.25f, 0.25f, 0.55f);

        float _timeAsIt;
        int _tagsLanded;
        float _iFrameTimer;
        bool _eliminated;
        TagBackImmunity.Window _tagBack;
        ItController _tagBackFrom;
        int _tagPawnId;
        static int _nextTagPawnId = 1;
        MaterialPropertyBlock _mpb;
        PlayerMotor _motor;
        PlayerRagdoll _ragdollCached;
        Tag.Art.DummyLocomotor _loco;
        Tag.Art.DummyAvatarBinder _binder;
        PlayerInputReader _reader;
        Tag.Modes.DummyPatrol _patrol;
        bool _humanKnown;
        Rigidbody _rb;
        Collider _bodyCol;
        PunchTagTuning _lastPunchTuning;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsIt => isIt;
        public bool HasIFrames => _iFrameTimer > 0f;
        public float TimeAsIt => _timeAsIt;
        public int TagsLanded => _tagsLanded;
        public string PlayerId { get; set; }
        public bool IsEliminated => _eliminated;
        public bool IsAlive => !_eliminated;
        public bool CanBeTagged => !_eliminated && !isIt && !HasIFrames;
        public int TagPawnId
        {
            get
            {
                if (_tagPawnId == 0) _tagPawnId = _nextTagPawnId++;
                return _tagPawnId;
            }
        }
        public PlayerMotor Motor => _motor;

        /// <summary>Local human: has a reader and is not the dummy. Cached so the HUD does not search every frame.</summary>
        public bool LooksLocal()
        {
            if (_humanKnown) return _reader != null && _patrol == null;
            _humanKnown = true;
            _reader = GetComponent<PlayerInputReader>();
            _patrol = GetComponent<Tag.Modes.DummyPatrol>();
            return _reader != null && _patrol == null;
        }
        public float TagBackRemaining => _tagBack.Remaining;
        public float TagBackGlow01(float time) => TagBackImmunity.GlowPulse(_tagBack, time);

        /// <summary>A (this pawn) just lost It to <paramref name="newIt"/>. B cannot tag A back.</summary>
        public void BeginTagBackImmunity(ItController newIt, float seconds)
        {
            _tagBackFrom = newIt;
            int id = newIt != null ? newIt.TagPawnId : 0;
            _tagBack = TagBackImmunity.Open(id, seconds);
            if (GetComponent<Tag.Art.TagBackGlow>() == null)
                gameObject.AddComponent<Tag.Art.TagBackGlow>();
        }

        public bool BlocksTagBackFrom(ItController attacker)
        {
            if (attacker == null || _tagBackFrom == null || attacker != _tagBackFrom) return false;
            return TagBackImmunity.Blocks(_tagBack, attacker.TagPawnId);
        }

        public void ClearTagBackImmunity()
        {
            _tagBack = default;
            _tagBackFrom = null;
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(PlayerId))
                PlayerId = gameObject.name;
            _mpb = new MaterialPropertyBlock();
            _motor = GetComponent<PlayerMotor>();
            if (GetComponent<Tag.Art.TagBackGlow>() == null)
                gameObject.AddComponent<Tag.Art.TagBackGlow>();
            _rb = GetComponent<Rigidbody>();
            _bodyCol = GetComponent<CapsuleCollider>();
            if (_bodyCol == null) _bodyCol = GetComponent<Collider>();
            // The scene accent is the pawn capsule (built-in mesh, Default-Material).
            // Under URP that mesh is magenta, and it is centered on the motor origin
            // so it sits halfway in the ground. Do not adopt it. Hier carries the color.
            ReleaseRootAccent();
            ApplyVisual();
        }

        /// <summary>Drop a tint target that is the pawn root capsule, not a mannequin part.</summary>
        public void ReleaseRootAccent()
        {
            if (accentRenderer != null && accentRenderer.transform == transform)
                accentRenderer = null;
        }

        void Update()
        {
            if (_eliminated) return;
            bool playing = RoundClockOpen();
            if (isIt && playing)
                _timeAsIt += Time.deltaTime;
            if (_iFrameTimer > 0f)
                _iFrameTimer -= Time.deltaTime;
            // Pause freezes with timeScale 0. Results and the next countdown do not keep the window.
            if (playing)
                _tagBack = TagBackImmunity.Tick(_tagBack, Time.deltaTime);
            else
                ClearTagBackImmunity();
            // The window is only from the specific new It. Once they are not It, the glow ends.
            if (_tagBack.Remaining <= 0f || _tagBackFrom == null || !_tagBackFrom.IsIt)
                ClearTagBackImmunity();
        }

        Tag.Art.DummyLocomotor BodyLoco()
        {
            if (_loco == null) _loco = GetComponentInChildren<Tag.Art.DummyLocomotor>();
            return _loco;
        }

        Tag.Art.DummyAvatarBinder BodyBinder()
        {
            if (_binder == null) _binder = GetComponent<Tag.Art.DummyAvatarBinder>();
            return _binder;
        }

        PlayerRagdoll BodyRagdoll()
        {
            if (_ragdollCached == null) _ragdollCached = GetComponent<PlayerRagdoll>();
            return _ragdollCached;
        }

        public static void ResetPawnIds()
        {
            _nextTagPawnId = 1;
        }

        public void SetIt(bool value)
        {
            if (_eliminated && value) return;
            bool wasIt = isIt;
            isIt = value;
            ApplyVisual();
            if (!wasIt && value)
            {
                ClearTagBackImmunity();
                AudioBus.Raise(AudioBus.Hook.Tag, transform.position);
                // Drive MoveAnimDriver / HUD listeners (legacy TryTag path was the only NotifyBecameIt caller).
                if (_motor != null)
                    _motor.NotifyBecameIt();
                // New It plays the claim pose. The tagged runner still guards in ReceiveTagHit.
                Tag.Art.DummyLocomotor loco = BodyLoco();
                if (loco != null) loco.PlayItClaim();
            }
            if (wasIt && !value && _motor != null)
            {
                if (_lastPunchTuning == null || _lastPunchTuning.speedBuffClearsOnLosingIt)
                    _motor.ClearSpeedBoost();
            }
        }

        public void ReceiveTagHit(Vector3 knock, PunchTagTuning tuning)
        {
            if (_eliminated) return;
            _lastPunchTuning = tuning;
            float dur = tuning != null ? tuning.ragdollDuration : 1.5f;
            if (tuning == null || tuning.ragdollHasIFrames)
                _iFrameTimer = dur;
            PlayerRagdoll ragdoll = BodyRagdoll();
            if (ragdoll != null)
                ragdoll.TriggerRagdoll(dur, knock);
            else if (_motor != null)
                _motor.BeginStunProxy(dur, knock);

            // Readable tag flinch on victim dummy (code-only pose pulse)
            Tag.Art.DummyLocomotor loco = BodyLoco();
            if (loco != null) loco.PlayTagFlinch();
            Tag.Art.DummyAvatarBinder binder = BodyBinder();
            if (binder != null) binder.PlayTagHitFeedback();
        }

        /// <summary>
        /// Non-tag punch. Stumble and drop sprint. No ragdoll and no impulse.
        /// Immunity after the stumble refuses a chain.
        /// </summary>
        public bool ReceivePunchStagger()
        {
            if (_eliminated) return false;
            if (_motor != null && !_motor.BeginPunchStagger())
                return false;
            var loco = GetComponentInChildren<Tag.Art.DummyLocomotor>();
            if (loco != null) loco.PlayPunchStagger();
            return true;
        }

        public void ResetScore()
        {
            _timeAsIt = 0f;
            _tagsLanded = 0;
            _iFrameTimer = 0f;
        }

        public void NoteTagLanded()
        {
            _tagsLanded++;
        }

        /// <summary>Results and countdown keep the snapshot. A missing match still accrues.</summary>
        static bool RoundClockOpen()
        {
            var modes = TagModeController.Instance;
            if (modes == null) return true;
            return modes.Phase == MatchPhase.Playing && modes.IsRunning;
        }

        public void ApplySpawnIFrames(float seconds)
        {
            if (seconds > 0f)
                _iFrameTimer = Mathf.Max(_iFrameTimer, seconds);
        }

        public void Eliminate() => Eliminate("eliminated");

        public void Eliminate(string reason)
        {
            if (_eliminated) return;
            _eliminated = true;
            isIt = false;
            _iFrameTimer = 0f;
            ClearTagBackImmunity();
            if (_motor != null) _motor.SetMotorLocked(true);
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            // Keep collider for world collision; motor lock stops control.
            ApplyVisual();
            Debug.Log($"[It] {PlayerId} eliminated ({reason})");
        }

        public void Revive() => ResetForRound();

        public void ResetForRound()
        {
            _eliminated = false;
            _timeAsIt = 0f;
            _iFrameTimer = 0f;
            ClearTagBackImmunity();
            isIt = false;
            if (_motor != null) _motor.SetMotorLocked(false);
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            Physics.SyncTransforms();
            if (accentRenderer != null && accentRenderer.transform != transform)
                accentRenderer.enabled = true;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (accentRenderer == null) return;
            Color c = _eliminated ? eliminatedColor : (isIt ? itColor : runnerColor);
            accentRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            accentRenderer.SetPropertyBlock(_mpb);
        }
    }
}
