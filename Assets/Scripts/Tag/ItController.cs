using UnityEngine;
using Tag.Movement;

namespace Tag.Gameplay
{
    /// <summary>Per-player It flag, i-frames during ragdoll, time-as-It accumulator.</summary>
    public class ItController : MonoBehaviour
    {
        [SerializeField] bool isIt;
        [SerializeField] Renderer accentRenderer;
        [SerializeField] Color itColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] Color runnerColor = new Color(0.3f, 0.7f, 1f);

        float _timeAsIt;
        float _iFrameTimer;
        MaterialPropertyBlock _mpb;
        PlayerMotor _motor;
        PunchTagTuning _lastPunchTuning;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsIt => isIt;
        public bool HasIFrames => _iFrameTimer > 0f;
        public float TimeAsIt => _timeAsIt;
        public string PlayerId { get; set; }
        public bool CanBeTagged => !isIt && !HasIFrames;

        void Awake()
        {
            if (string.IsNullOrEmpty(PlayerId))
                PlayerId = gameObject.name;
            _mpb = new MaterialPropertyBlock();
            _motor = GetComponent<PlayerMotor>();
            if (accentRenderer == null)
                accentRenderer = GetComponentInChildren<Renderer>();
            ApplyVisual();
        }

        void Update()
        {
            if (isIt)
                _timeAsIt += Time.deltaTime;
            if (_iFrameTimer > 0f)
                _iFrameTimer -= Time.deltaTime;
        }

        public void SetIt(bool value)
        {
            bool wasIt = isIt;
            isIt = value;
            ApplyVisual();

            // Buff clears when losing It
            if (wasIt && !value && _motor != null)
            {
                if (_lastPunchTuning == null || _lastPunchTuning.speedBuffClearsOnLosingIt)
                    _motor.ClearSpeedBoost();
            }
        }

        public void ReceiveTagHit(Vector3 knock, PunchTagTuning tuning)
        {
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

        void ApplyVisual()
        {
            if (accentRenderer == null) return;
            Color c = isIt ? itColor : runnerColor;
            accentRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            accentRenderer.SetPropertyBlock(_mpb);
        }
    }
}
