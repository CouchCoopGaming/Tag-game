using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Dummy AI v0 (SP demo): chase+punch when It; flee when not.
    /// Drives Rigidbody while PlayerMotor stays locked (TagArena cutover).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DummyPatrol : MonoBehaviour
    {
        [SerializeField] float speed = 7.4f;
        [SerializeField] float radius = 5.5f;
        [SerializeField] float turnSpeed = 220f;
        [SerializeField] float chaseSpeedMul = 1.12f;
        [Tooltip("If true, chase/flee scale off the player's sprint so dummy stays in the 9 m/s class.")]
        [SerializeField] bool matchPlayerSprint = true;
        [SerializeField] float sprintChaseMul = 0.88f;
        [SerializeField] float sprintFleeMul = 0.80f;
        [SerializeField] float punchRange = 1.2f;
        [SerializeField] float punchConeDeg = 40f;
        [SerializeField] float itGraceSec = 1f;
        [SerializeField] float aggression = 0.85f;
        [SerializeField] float cooldownMin = 0.6f;
        [SerializeField] float cooldownMax = 0.9f;
        [SerializeField] float decisionHz = 5f;
        [SerializeField] Vector3 centerOffset = Vector3.zero;
        [SerializeField] float closeChaseRange = 3f;
        [SerializeField] float closeChaseSpeedMul = 1.15f;
        [SerializeField] float leadSeconds = 0.25f;
        [SerializeField] float faceAlignDeg = 18f;
        [SerializeField] float hotPotatoUrgencySec = 10f;
        [SerializeField] float fleeUrgencyMul = 1.18f;
        [SerializeField] float gravity = 20f;

        Rigidbody _rb;
        PlayerRagdoll _ragdoll;
        PlayerMotor _selfMotor;
        ItController _it;
        PunchHitbox _punch;
        Vector3 _center;
        float _angle;
        float _cooldown;
        float _decisionTimer;
        ItController _target;
        PlayerMotor _targetMotor;
        TagModeController _modes;
        PlayerMotor _playerMotorRef;
        float _itGraceTimer;
        bool _wasIt;
        bool _grounded;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            _selfMotor = GetComponent<PlayerMotor>();
            if (_selfMotor != null)
                _selfMotor.SetMotorLocked(true);

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

        void FixedUpdate()
        {
            if (_it != null && _it.IsEliminated) return;
            if (_ragdoll != null && _ragdoll.IsRagdolling) return;
            if (_selfMotor != null && !_selfMotor.IsMotorLocked)
                _selfMotor.SetMotorLocked(true);
            if (_rb == null) return;

            float dt = Time.fixedDeltaTime;
            _decisionTimer -= dt;
            if (_decisionTimer <= 0f)
            {
                _decisionTimer = 1f / Mathf.Max(1f, decisionHz);
                Retarget();
            }

            bool isIt = _it != null && _it.IsIt;
            if (isIt && !_wasIt)
                _itGraceTimer = Mathf.Max(0f, itGraceSec);
            _wasIt = isIt;

            ProbeGround();

            if (isIt)
                TickChase(dt);
            else
                TickFleeOrWander(dt);
        }

        void ProbeGround()
        {
            _grounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore);
        }

        float EffectiveAggression()
        {
            float a = aggression;
            if (_modes != null && _modes.SelectedMode == TagModeId.LeastIt)
                a = 1.0f;
            if (_modes != null && _modes.SelectedMode == TagModeId.HotPotato)
                a = Mathf.Max(a, 0.95f);
            return a;
        }

        void Retarget()
        {
            _target = null;
            _targetMotor = null;
            float best = float.MaxValue;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (p == null || p == _it || !p.IsAlive || p.IsEliminated) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; _target = p; }
            }
            if (_target == null) return;
            _targetMotor = _target.GetComponent<PlayerMotor>();
        }

        Vector3 TargetPlanarVelocity()
        {
            Vector3 v = Vector3.zero;
            if (_targetMotor != null)
                v = _targetMotor.Velocity;
            v.y = 0f;
            return v;
        }

        Vector3 AimPoint(ItController target)
        {
            Vector3 pos = target.transform.position;
            Vector3 vel = TargetPlanarVelocity();
            if (vel.sqrMagnitude < 0.04f || leadSeconds <= 0f)
                return pos;
            return pos + vel * leadSeconds;
        }

        void FaceAndSteer(Vector3 desired, float dt, out Vector3 moveDir)
        {
            if (desired.sqrMagnitude < 0.001f)
            {
                moveDir = transform.forward;
                return;
            }
            desired.Normalize();
            float ang = Vector3.Angle(transform.forward, desired);
            if (ang <= faceAlignDeg)
            {
                transform.rotation = Quaternion.LookRotation(desired, Vector3.up);
                moveDir = desired;
            }
            else
            {
                Quaternion look = Quaternion.LookRotation(desired, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * dt);
                moveDir = transform.forward;
            }
        }

        void TickChase(float dt)
        {
            if (_target == null || !_target.IsAlive) Retarget();
            Vector3 moveDir = transform.forward;
            float speedMul = chaseSpeedMul;

            if (_target != null)
            {
                Vector3 toAim = AimPoint(_target) - transform.position;
                toAim.y = 0f;
                FaceAndSteer(toAim, dt, out moveDir);

                Vector3 toBody = _target.transform.position - transform.position;
                toBody.y = 0f;
                float dist = toBody.magnitude;
                if (dist <= closeChaseRange)
                    speedMul *= closeChaseSpeedMul;

                float ang = Vector3.Angle(transform.forward, toBody.sqrMagnitude > 0.001f ? toBody.normalized : transform.forward);
                _cooldown -= dt;
                if (_itGraceTimer > 0f)
                    _itGraceTimer -= dt;
                bool inCone = dist <= punchRange && ang <= punchConeDeg * 0.5f;
                if (inCone && _itGraceTimer <= 0f && _cooldown <= 0f && Random.value <= EffectiveAggression())
                {
                    _punch?.QueuePunch();
                    _cooldown = Random.Range(cooldownMin, cooldownMax);
                }
            }
            else
            {
                _angle += (speed / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * dt;
            }

            ApplyMove(moveDir * (ChaseSpeed() * speedMul), dt);
        }

        float HotPotatoFleeMul()
        {
            if (_modes == null || _modes.SelectedMode != TagModeId.HotPotato)
                return 1f;
            float remain = _modes.Remaining;
            if (remain > 0f && remain <= hotPotatoUrgencySec)
                return fleeUrgencyMul;
            return 1f;
        }

        void TickFleeOrWander(float dt)
        {
            Vector3 moveDir = transform.forward;
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
                FaceAndSteer(away, dt, out moveDir);
            }
            else
            {
                _angle += (speed / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * dt;
                Vector3 target = _center + new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), 0f, Mathf.Sin(_angle * Mathf.Deg2Rad)) * radius;
                Vector3 to = target - transform.position;
                to.y = 0f;
                FaceAndSteer(to, dt, out moveDir);
            }
            ApplyMove(moveDir * (FleeSpeed() * HotPotatoFleeMul()), dt);
        }

        float PartySprint()
        {
            if (!matchPlayerSprint) return speed;
            if (_playerMotorRef == null)
            {
                foreach (var m in FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None))
                {
                    if (m == null || m.GetComponent<DummyPatrol>() != null) continue;
                    _playerMotorRef = m;
                    break;
                }
            }
            if (_playerMotorRef != null) return _playerMotorRef.SprintSpeed;
            return speed > 0.1f ? speed : 9f;
        }

        float ChaseSpeed() => matchPlayerSprint ? PartySprint() * sprintChaseMul : speed;
        float FleeSpeed() => matchPlayerSprint ? PartySprint() * sprintFleeMul : speed;

        void ApplyMove(Vector3 horiz, float dt)
        {
            float vy = _rb.linearVelocity.y;
            if (_grounded && vy < 0.1f) vy = -0.5f;
            else vy -= gravity * dt;
            horiz.y = 0f;
            _rb.linearVelocity = new Vector3(horiz.x, vy, horiz.z);
        }
    }
}
