using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Dummy AI v1 (SP demo): chase+punch when It; flee when not.
    /// Feeds TagArena PlayerMotor via PlayerInputReader.ExternalControl (no RB velocity fight).
    /// </summary>
    public class DummyPatrol : MonoBehaviour
    {
        [SerializeField] float radius = 5.5f;
        [SerializeField] float turnSpeed = 220f;
        [SerializeField] float punchRange = 1.35f;
        [SerializeField] float punchConeDeg = 40f;
        [SerializeField] float itGraceSec = 1f;
        [SerializeField] float aggression = 0.85f;
        [SerializeField] float cooldownMin = 0.6f;
        [SerializeField] float cooldownMax = 0.9f;
        [SerializeField] float decisionHz = 5f;
        [SerializeField] Vector3 centerOffset = Vector3.zero;
        [SerializeField] float closeChaseRange = 3f;
        [SerializeField] float leadSeconds = 0.25f;
        [SerializeField] float faceAlignDeg = 18f;
        [SerializeField] float hotPotatoUrgencySec = 10f;
        [Tooltip("Forward wish strength while wandering (motor treats y>0.4 as sprint).")]
        [SerializeField] float wanderMoveY = 0.35f;
        [Tooltip("Forward wish while fleeing under Hot Potato urgency.")]
        [SerializeField] float fleeUrgencyMoveY = 1f;

        PlayerInputReader _input;
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
        float _itGraceTimer;
        bool _wasIt;

        void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            if (_input == null) _input = gameObject.AddComponent<PlayerInputReader>();
            _input.ExternalControl = true;

            _ragdoll = GetComponent<PlayerRagdoll>();
            _selfMotor = GetComponent<PlayerMotor>();
            // Motor must stay unlocked so locomotion + PunchHitbox can run.
            if (_selfMotor != null && _selfMotor.IsMotorLocked)
                _selfMotor.SetMotorLocked(false);

            // Never enable legacy CharacterController — TagArena is RB-only.
            var cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

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
            if (_it != null && _it.IsEliminated)
            {
                StopWish();
                return;
            }
            if (_ragdoll != null && _ragdoll.IsRagdolling)
            {
                StopWish();
                return;
            }
            if (_selfMotor != null && _selfMotor.IsMotorLocked)
            {
                // Stun / ragdoll proxy owns the lock — do not fight it.
                StopWish();
                return;
            }

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

            if (isIt)
                TickChase(dt);
            else
                TickFleeOrWander(dt);
        }

        void StopWish()
        {
            if (_input == null) return;
            _input.ExternalControl = true;
            _input.SetExternalMove(Vector2.zero, false);
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

        /// <summary>
        /// Body-relative wish: AI has no TP cam, so PlayerMotor uses transform as wish basis.
        /// Face first, then push forward — matches human TP (yaw then Move.y).
        /// </summary>
        void DriveWish(float moveY, bool sprint)
        {
            if (_input == null) return;
            _input.SetExternalMove(new Vector2(0f, Mathf.Clamp(moveY, -1f, 1f)), sprint);
        }

        void TickChase(float dt)
        {
            if (_target == null || !_target.IsAlive) Retarget();
            Vector3 moveDir = transform.forward;
            float moveY = 1f;
            bool sprint = true;

            if (_target != null)
            {
                Vector3 toAim = AimPoint(_target) - transform.position;
                toAim.y = 0f;
                FaceAndSteer(toAim, dt, out moveDir);

                Vector3 toBody = _target.transform.position - transform.position;
                toBody.y = 0f;
                float dist = toBody.magnitude;
                // Close range: keep sprinting in; motor owns accel (no velocity overwrite).
                if (dist <= closeChaseRange)
                    moveY = 1f;

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
                _angle += (6f / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * dt;
                moveY = wanderMoveY;
                sprint = false;
            }

            // Keep facing coherent even when moveDir came from FaceAndSteer.
            _ = moveDir;
            DriveWish(moveY, sprint);
        }

        bool HotPotatoUrgent()
        {
            if (_modes == null || _modes.SelectedMode != TagModeId.HotPotato)
                return false;
            float remain = _modes.Remaining;
            return remain > 0f && remain <= hotPotatoUrgencySec;
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
                bool urgent = HotPotatoUrgent();
                DriveWish(urgent ? fleeUrgencyMoveY : 1f, sprint: true);
            }
            else
            {
                _angle += (4f / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * dt;
                Vector3 target = _center + new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), 0f, Mathf.Sin(_angle * Mathf.Deg2Rad)) * radius;
                Vector3 to = target - transform.position;
                to.y = 0f;
                FaceAndSteer(to, dt, out moveDir);
                DriveWish(wanderMoveY, sprint: false);
            }
            _ = moveDir;
        }
    }
}
