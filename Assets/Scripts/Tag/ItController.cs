using UnityEngine;
using Tag.Movement;

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
        float _iFrameTimer;
        bool _eliminated;
        MaterialPropertyBlock _mpb;
        PlayerMotor _motor;
        CharacterController _cc;
        PunchTagTuning _lastPunchTuning;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsIt => isIt;
        public bool HasIFrames => _iFrameTimer > 0f;
        public float TimeAsIt => _timeAsIt;
        public string PlayerId { get; set; }
        public bool IsEliminated => _eliminated;
        public bool IsAlive => !_eliminated;
        public bool CanBeTagged => !_eliminated && !isIt && !HasIFrames;

        void Awake()
        {
            if (string.IsNullOrEmpty(PlayerId))
                PlayerId = gameObject.name;
            _mpb = new MaterialPropertyBlock();
            _motor = GetComponent<PlayerMotor>();
            _cc = GetComponent<CharacterController>();
            if (accentRenderer == null)
                accentRenderer = GetComponentInChildren<Renderer>();
            ApplyVisual();
        }

        void Update()
        {
            if (_eliminated) return;
            if (isIt)
                _timeAsIt += Time.deltaTime;
            if (_iFrameTimer > 0f)
                _iFrameTimer -= Time.deltaTime;
        }

        public void SetIt(bool value)
        {
            if (_eliminated && value) return;
            bool wasIt = isIt;
            isIt = value;
            ApplyVisual();
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
            var ragdoll = GetComponent<PlayerRagdoll>();
            if (ragdoll != null)
                ragdoll.TriggerRagdoll(dur, knock);
            else if (_motor != null)
                _motor.BeginStunProxy(dur, knock);
        }

        public void ResetScore()
        {
            _timeAsIt = 0f;
            _iFrameTimer = 0f;
        }

        public void Eliminate() => Eliminate("eliminated");

        public void Eliminate(string reason)
        {
            if (_eliminated) return;
            _eliminated = true;
            isIt = false;
            _iFrameTimer = 0f;
            if (_motor != null) _motor.SetMotorLocked(true);
            if (_cc == null) _cc = GetComponent<CharacterController>();
            if (_cc != null) _cc.enabled = false;
            ApplyVisual();
            Debug.Log($"[It] {PlayerId} eliminated ({reason})");
        }

        public void Revive() => ResetForRound();

        public void ResetForRound()
        {
            _eliminated = false;
            _timeAsIt = 0f;
            _iFrameTimer = 0f;
            isIt = false;
            if (_motor != null) _motor.SetMotorLocked(false);
            if (_cc == null) _cc = GetComponent<CharacterController>();
            if (_cc != null)
            {
                _cc.enabled = true;
                Physics.SyncTransforms();
            }
            if (accentRenderer != null) accentRenderer.enabled = true;
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
