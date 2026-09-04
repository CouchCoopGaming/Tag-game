using Tag.Gameplay;
using Tag.Modes;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Dummy AI v0 (SP demo): chase+punch when It; flee when not. Same punch kit, no range cheat.
    /// Spec: tag-gdd/DUMMY-AI-v0.md
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DummyPatrol : MonoBehaviour
    {
        [SerializeField] float speed = 4.2f;
        [SerializeField] float radius = 5.5f;
        [SerializeField] float turnSpeed = 220f;
        [SerializeField] float chaseSpeedMul = 1.0f;
        [SerializeField] float punchRange = 1.2f;
        [SerializeField] float punchConeDeg = 40f;
        [SerializeField] float aggression = 0.85f;
        [SerializeField] float cooldownMin = 0.6f;
        [SerializeField] float cooldownMax = 0.9f;
        [SerializeField] float decisionHz = 5f;
        [SerializeField] Vector3 centerOffset = Vector3.zero;

        CharacterController _cc;
        ItController _it;
        PunchHitbox _punch;
        Vector3 _center;
        float _angle;
        float _gravity;
        float _cooldown;
        float _decisionTimer;
        ItController _target;
        TagModeController _modes;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _it = GetComponent<ItController>();
            if (_it == null) _it = gameObject.AddComponent<ItController>();
            if (string.IsNullOrEmpty(_it.PlayerId) || _it.PlayerId == "Player" || _it.PlayerId == gameObject.name)
                _it.PlayerId = "Dummy";

            _punch = GetComponent<PunchHitbox>();
            if (_punch == null) _punch = gameObject.AddComponent<PunchHitbox>();

            _center = transform.position + centerOffset;
            _angle = Random.Range(0f, 360f);
            _cooldown = Random.Range(cooldownMin, cooldownMax);
        }

        void Start()
        {
            _modes = TagModeController.Instance != null
                ? TagModeController.Instance
                : FindFirstObjectByType<TagModeController>();
            if (_modes != null)
                _modes.RegisterPlayer(_it);
        }

        void Update()
        {
            if (_it != null && _it.IsEliminated) return;
            if (_cc == null || !_cc.enabled) return;

            float dt = Time.deltaTime;
            _decisionTimer -= dt;
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = 1f / Mathf.Max(1f, decisionHz);
                Retarget();
            }

            bool isIt = _it != null && _it.IsIt;
            if (isIt)
                TickChase(dt);
            else
                TickFleeOrWander(dt);
        }

        float EffectiveAggression()
        {
            float a = aggression;
            if (_modes != null && _modes.SelectedMode == TagModeId.LeastIt)
                a = 1.0f; // NextPunch pressure
            if (_modes != null && _modes.SelectedMode == TagModeId.HotPotato)
                a = Mathf.Max(a, 0.95f); // prioritize punch
            return a;
        }

        void Retarget()
        {
            _target = null;
            float best = float.MaxValue;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (p == null || p == _it || !p.IsAlive || p.IsEliminated) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; _target = p; }
            }
        }

        void TickChase(float dt)
        {
            if (_target == null || !_target.IsAlive) Retarget();
            Vector3 moveDir = transform.forward;
            if (_target != null)
            {
                Vector3 to = _target.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * dt);
                    moveDir = transform.forward;
                }

                float dist = to.magnitude;
                float ang = Vector3.Angle(transform.forward, to.sqrMagnitude > 0.001f ? to.normalized : transform.forward);
                _cooldown -= dt;
                bool inCone = dist <= punchRange && ang <= punchConeDeg * 0.5f;
                if (inCone && _cooldown <= 0f && Random.value <= EffectiveAggression())
                {
                    _punch?.QueuePunch();
                    _cooldown = Random.Range(cooldownMin, cooldownMax);
                }
            }
            else
            {
                // No runner — keep orbiting
                _angle += (speed / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * dt;
            }

            ApplyMove(moveDir * (speed * chaseSpeedMul), dt);
        }

        void TickFleeOrWander(float dt)
        {
            Vector3 moveDir = transform.forward;
            // Flee It if present
            ItController threat = null;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (p != null && p.IsIt && p.IsAlive && p != _it) { threat = p; break; }
            }
            if (threat != null)
            {
                Vector3 away = transform.position - threat.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = -transform.forward;
                Quaternion look = Quaternion.LookRotation(away.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * dt);
                moveDir = transform.forward;
            }
            else
            {
                _angle += (speed / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * dt;
                Vector3 target = _center + new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), 0f, Mathf.Sin(_angle * Mathf.Deg2Rad)) * radius;
                Vector3 to = target - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * dt);
                }
                moveDir = transform.forward;
            }
            ApplyMove(moveDir * speed, dt);
        }

        void ApplyMove(Vector3 horiz, float dt)
        {
            if (_cc.isGrounded) _gravity = -2f;
            else _gravity += -20f * dt;
            horiz.y = _gravity;
            _cc.Move(horiz * dt);
        }
    }
}
