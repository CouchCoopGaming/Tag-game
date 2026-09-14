using Tag.Gameplay;
using Tag.Trail;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Dummy AI v1 (SP demo): chase+punch when It; flee when not.
    /// Feeds TagArena PlayerMotor via PlayerInputReader.ExternalControl (no RB velocity fight).
    /// Trail Tag: samples nearby TrailSegments and blends a lateral flee wish into steering.
    /// </summary>
    public class DummyPatrol : MonoBehaviour
    {
        [SerializeField] float radius = 5.5f;
        [SerializeField] float turnSpeed = 220f;
        [Tooltip("Fallback when PunchHitbox/Tuning unavailable. Prefer syncing from PunchTagTuning.reach.")]
        [SerializeField] float punchRange = 1.55f;
        [Tooltip("Fallback full cone (deg). Prefer syncing from PunchTagTuning width/reach.")]
        [SerializeField] float punchConeDeg = 36f;
        [SerializeField] float itGraceSec = 0.85f;
        [SerializeField] float aggression = 0.92f;
        [SerializeField] float cooldownMin = 0.5f;
        [SerializeField] float cooldownMax = 0.78f;
        [SerializeField] float decisionHz = 5f;
        [SerializeField] Vector3 centerOffset = Vector3.zero;
        [SerializeField] float closeChaseRange = 3.5f;
        [SerializeField] float leadSeconds = 0.32f;
        [SerializeField] float faceAlignDeg = 16f;
        [SerializeField] float hotPotatoUrgencySec = 10f;
        [Tooltip("Only flee when It is within this planar distance; otherwise wander.")]
        [SerializeField] float fleeThreatRange = 14f;
        [Tooltip("Blend of lateral strafe into flee dir so pure radial chase is harder.")]
        [SerializeField] float fleeStrafeBias = 0.35f;
        [Tooltip("Seconds of threat velocity lead when computing flee-from point.")]
        [SerializeField] float fleeLeadSeconds = 0.35f;
        [Tooltip("Forward wish strength while wandering (motor treats y>0.4 as sprint).")]
        [SerializeField] float wanderMoveY = 0.35f;
        [Tooltip("Forward wish while fleeing under Hot Potato urgency.")]
        [SerializeField] float fleeUrgencyMoveY = 1f;
        [Header("Trail Tag avoid")]
        [Tooltip("Only active when TagModeController SelectedMode is TrailTag.")]
        [SerializeField] float trailAvoidRange = 8f;
        [Tooltip("Lateral bias on trail flee (same idea as fleeStrafeBias).")]
        [SerializeField] float trailAvoidStrafeBias = 0.45f;
        [Tooltip("How hard trail flee blends into chase/flee/wander wish (0=off).")]
        [SerializeField] float trailAvoidWeight = 0.7f;

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
        Vector3 _trailFleeWish;

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
                RefreshTrailFleeWish();
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

        /// <summary>Match PunchHitbox / PunchTagTuning.reach so AI swings when the hitbox can connect.</summary>
        float EffectivePunchRange()
        {
            if (_punch != null)
                return _punch.Reach;
            return punchRange;
        }

        /// <summary>Half-angle from hitbox width/reach; slight pad so AI queues near the box edge.</summary>
        float EffectivePunchConeHalfDeg()
        {
            if (_punch != null)
            {
                float r = Mathf.Max(0.05f, _punch.Reach);
                float half = Mathf.Atan((_punch.Width * 0.5f) / r) * Mathf.Rad2Deg;
                return half * 1.15f; // small decision pad vs geometric box
            }
            return punchConeDeg * 0.5f;
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

        /// <summary>
        /// Trail Tag only: sample live TrailSegments in range (from PlayerTrailEmitter ribbons)
        /// and cache a weighted lateral flee wish. Cheap — runs at decisionHz.
        /// </summary>
        void RefreshTrailFleeWish()
        {
            _trailFleeWish = Vector3.zero;
            if (_modes == null || _modes.SelectedMode != TagModeId.TrailTag)
                return;
            if (trailAvoidRange <= 0.01f || trailAvoidWeight <= 0.01f)
                return;

            float range = trailAvoidRange;
            float rangeSq = range * range;
            Vector3 pos = transform.position;
            Vector3 sum = Vector3.zero;
            int hits = 0;

            foreach (var seg in FindObjectsByType<TrailSegment>(FindObjectsSortMode.None))
            {
                if (seg == null) continue;
                // Closest point on ribbon A–B (not collider midpoint) so long segments steer correctly.
                Vector3 delta = pos - seg.ClosestPointOnSegment(pos);
                delta.y = 0f;
                float dsq = delta.sqrMagnitude;
                if (dsq > rangeSq || dsq < 0.0001f) continue;

                float d = Mathf.Sqrt(dsq);
                float w = 1f - (d / range);
                Vector3 away = delta / d;
                Vector3 lateral = Vector3.Cross(Vector3.up, away);
                if (lateral.sqrMagnitude > 0.001f)
                {
                    lateral.Normalize();
                    if (Vector3.Dot(lateral, transform.right) < 0f) lateral = -lateral;
                    away = (away + lateral * Mathf.Clamp01(trailAvoidStrafeBias)).normalized;
                }
                sum += away * w;
                hits++;
            }

            if (hits > 0 && sum.sqrMagnitude > 0.0001f)
                _trailFleeWish = sum.normalized;
        }

        Vector3 BlendTrailAvoid(Vector3 desired)
        {
            if (_trailFleeWish.sqrMagnitude < 0.0001f)
                return desired;
            float w = Mathf.Clamp01(trailAvoidWeight);
            if (desired.sqrMagnitude < 0.0001f)
                return _trailFleeWish;
            return (desired.normalized + _trailFleeWish * w).normalized;
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
                toAim = BlendTrailAvoid(toAim);
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
                float range = EffectivePunchRange();
                bool inCone = dist <= range && ang <= EffectivePunchConeHalfDeg();
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
                // Still peel off ribbons while hunting with no target.
                Vector3 peel = BlendTrailAvoid(transform.forward);
                FaceAndSteer(peel, dt, out moveDir);
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
            float bestThreat = float.MaxValue;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (p == null || !p.IsIt || !p.IsAlive || p == _it) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < bestThreat) { bestThreat = d; threat = p; }
            }

            if (threat != null)
            {
                Vector3 threatPos = threat.transform.position;
                var threatMotor = threat.GetComponent<PlayerMotor>();
                if (threatMotor != null && fleeLeadSeconds > 0f)
                {
                    Vector3 tv = threatMotor.Velocity;
                    tv.y = 0f;
                    threatPos += tv * fleeLeadSeconds;
                }

                Vector3 away = transform.position - threatPos;
                away.y = 0f;
                float threatDist = away.magnitude;
                if (threatDist > fleeThreatRange)
                {
                    // Far enough: resume patrol wander instead of endless radial flee.
                    Wander(dt, out moveDir);
                    _ = moveDir;
                    return;
                }

                if (away.sqrMagnitude < 0.01f) away = -transform.forward;
                else away.Normalize();

                // Strafe bias: prefer current facing side so flee isn't pure radial (easier to cut off).
                Vector3 lateral = Vector3.Cross(Vector3.up, away);
                if (lateral.sqrMagnitude > 0.001f)
                {
                    lateral.Normalize();
                    if (Vector3.Dot(lateral, transform.right) < 0f) lateral = -lateral;
                    away = (away + lateral * Mathf.Clamp01(fleeStrafeBias)).normalized;
                }

                away = BlendTrailAvoid(away);
                FaceAndSteer(away, dt, out moveDir);
                bool urgent = HotPotatoUrgent() || threatDist <= closeChaseRange * 1.6f;
                DriveWish(urgent ? fleeUrgencyMoveY : 1f, sprint: true);
            }
            else
            {
                Wander(dt, out moveDir);
            }
            _ = moveDir;
        }

        void Wander(float dt, out Vector3 moveDir)
        {
            _angle += (4f / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * dt;
            Vector3 target = _center + new Vector3(Mathf.Cos(_angle * Mathf.Deg2Rad), 0f, Mathf.Sin(_angle * Mathf.Deg2Rad)) * radius;
            Vector3 to = target - transform.position;
            to.y = 0f;
            to = BlendTrailAvoid(to);
            FaceAndSteer(to, dt, out moveDir);
            DriveWish(wanderMoveY, sprint: false);
        }
    }
}
