using UnityEngine;
using Tag.Input;
using Tag.Movement;

namespace Tag.Gameplay
{
    public enum PunchPhase
    {
        Idle,
        Windup,
        Active,
        HitRecover,
        MissRecover
    }

    /// <summary>
    /// It-only dedicated melee. Active punch ONLY — NO passive overlap/aura tag.
    /// Prefer continuous cast during Active. Closest runner with LoS wins.
    /// </summary>
    public class PunchHitbox : MonoBehaviour
    {
        [SerializeField] PunchTagTuning tuning;
        [SerializeField] Transform aimOrigin;

        PlayerInputReader _input;
        ItController _it;
        PlayerMotor _motor;
        PlayerRagdoll _ragdoll;
        TagRoundController _round;

        public PunchPhase Phase { get; private set; } = PunchPhase.Idle;
        public bool IsPunching => Phase != PunchPhase.Idle;

        float _phaseTimer;
        float _bufferTimer;
        bool _hitThisSwing;
        readonly Collider[] _overlap = new Collider[24];
        Coroutine _buffCo;

        void Awake()
        {
            if (tuning == null)
                tuning = PunchTagTuning.CreateRuntimeDefaults();
            _input = GetComponent<PlayerInputReader>();
            _it = GetComponent<ItController>();
            _motor = GetComponent<PlayerMotor>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            if (aimOrigin == null)
                aimOrigin = transform;
        }

        void Start()
        {
            _round = FindFirstObjectByType<TagRoundController>();
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (_input != null && _input.PunchPressed)
                _bufferTimer = tuning.inputBuffer;
            else
                _bufferTimer = Mathf.Max(0f, _bufferTimer - dt);

            bool canStart =
                Phase == PunchPhase.Idle
                && _bufferTimer > 0f
                && _it != null && _it.IsIt
                && (_motor == null || !_motor.IsMotorLocked)
                && (_ragdoll == null || !_ragdoll.IsRagdolling)
                && !_it.HasIFrames;

            if (canStart)
                BeginWindup();

            switch (Phase)
            {
                case PunchPhase.Windup: TickWindup(dt); break;
                case PunchPhase.Active: TickActive(dt); break;
                case PunchPhase.HitRecover:
                case PunchPhase.MissRecover: TickRecover(dt); break;
            }
        }

        void BeginWindup()
        {
            _bufferTimer = 0f;
            _hitThisSwing = false;
            Phase = PunchPhase.Windup;
            _phaseTimer = tuning.windup;
            if (_motor != null)
            {
                _motor.SetPunchMoveScale(tuning.windupMoveSpeedScale);
                if (!tuning.allowSlideCancelDuringPunch)
                    _motor.SetSlideBlocked(true);
            }
        }

        void TickWindup(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer <= 0f)
            {
                Phase = PunchPhase.Active;
                _phaseTimer = tuning.active;
                if (_motor != null) _motor.SetPunchMoveScale(1f);
            }
        }

        void TickActive(float dt)
        {
            if (!_hitThisSwing && TryHitClosestRunner(out var victim, out var hitPoint))
            {
                _hitThisSwing = true;
                ResolveHit(victim, hitPoint);
                Phase = PunchPhase.HitRecover;
                _phaseTimer = tuning.hitRecover;
                return;
            }

            _phaseTimer -= dt;
            if (_phaseTimer <= 0f)
            {
                Phase = PunchPhase.MissRecover;
                _phaseTimer = tuning.missRecover;
            }
        }

        void TickRecover(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer <= 0f)
                EndPunch();
        }

        void EndPunch()
        {
            Phase = PunchPhase.Idle;
            if (_motor != null)
            {
                _motor.SetPunchMoveScale(1f);
                _motor.SetSlideBlocked(false);
            }
        }

        bool TryHitClosestRunner(out ItController victim, out Vector3 hitPoint)
        {
            victim = null;
            hitPoint = aimOrigin.position;

            Vector3 origin = aimOrigin.position + Vector3.up * tuning.midTorsoHeight;
            Vector3 forward = aimOrigin.forward;
            // Pitch clamp ±tolerance around forward
            float pitch = Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            if (Mathf.Abs(pitch) > tuning.pitchToleranceDeg)
            {
                float clamped = Mathf.Clamp(pitch, -tuning.pitchToleranceDeg, tuning.pitchToleranceDeg);
                Vector3 flat = new Vector3(forward.x, 0f, forward.z).normalized;
                forward = (Quaternion.AngleAxis(clamped, Vector3.Cross(flat, Vector3.up).normalized) * flat);
                // simpler: flatten excess pitch
                forward = Vector3.RotateTowards(
                    new Vector3(forward.x, 0f, forward.z).normalized,
                    aimOrigin.forward,
                    tuning.pitchToleranceDeg * Mathf.Deg2Rad,
                    0f);
            }

            Vector3 halfExtents = new Vector3(tuning.width * 0.5f, tuning.height * 0.5f, 0.05f);
            float best = float.MaxValue;
            ItController bestIt = null;
            Vector3 bestPt = origin;

            // Continuous box cast preferred
            if (tuning.preferContinuousCast)
            {
                if (Physics.BoxCast(origin, halfExtents, forward, out var boxHit,
                        Quaternion.LookRotation(forward, Vector3.up), tuning.reach,
                        tuning.runnerMask, QueryTriggerInteraction.Ignore))
                {
                    TryConsider(boxHit.collider, origin, boxHit.point, ref best, ref bestIt, ref bestPt);
                }
            }

            // Overlap volume as continuous sweep fallback / supplemental
            Vector3 center = origin + forward * (tuning.reach * 0.5f);
            Vector3 boxHalf = new Vector3(tuning.width * 0.5f, tuning.height * 0.5f, tuning.reach * 0.5f);
            int count = Physics.OverlapBoxNonAlloc(center, boxHalf, _overlap,
                Quaternion.LookRotation(forward, Vector3.up), tuning.runnerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                TryConsider(_overlap[i], origin, _overlap[i].bounds.center, ref best, ref bestIt, ref bestPt);

            victim = bestIt;
            hitPoint = bestPt;
            return victim != null;
        }

        void TryConsider(Collider col, Vector3 origin, Vector3 candidatePt,
            ref float best, ref ItController bestIt, ref Vector3 bestPt)
        {
            if (col == null) return;
            if (col.transform.root == transform.root) return;

            var other = col.GetComponentInParent<ItController>();
            if (other == null || other == _it) return;
            if (other.IsIt) return; // runners only
            if (!other.CanBeTagged) return;

            // LoS
            Vector3 losTarget = col.bounds.center;
            if (Physics.Linecast(origin, losTarget, out var losHit, tuning.losMask, QueryTriggerInteraction.Ignore))
            {
                var hitIt = losHit.collider.GetComponentInParent<ItController>();
                if (hitIt != other && !losHit.transform.IsChildOf(other.transform))
                    return;
            }

            float d = Vector3.Distance(origin, candidatePt);
            if (d < best)
            {
                best = d;
                bestIt = other;
                bestPt = candidatePt;
            }
        }

        void ResolveHit(ItController victim, Vector3 hitPoint)
        {
            Vector3 flat = hitPoint - aimOrigin.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.001f) flat = aimOrigin.forward;
            flat.Normalize();
            Vector3 knock = flat * tuning.knockbackHorizontal + Vector3.up * tuning.knockbackUp;

            // Transfer-It
            if (_round != null)
                _round.OnSuccessfulPunch(_it, victim);
            else if (_it != null && _it.IsIt)
            {
                _it.SetIt(false);
                victim.SetIt(true);
            }

            // Target ragdoll / kinematic stun proxy + i-frames
            victim.ReceiveTagHit(knock, tuning);

            // Puncher buff: +8% walk+sprint, no stack, refresh on hit
            if (_motor != null)
            {
                if (tuning.speedBuffRefreshOnHit || _buffCo == null)
                    _motor.ApplySpeedBoost(tuning.speedBuffPercent, tuning.speedBuffDuration);
                if (_buffCo != null) StopCoroutine(_buffCo);
                _buffCo = StartCoroutine(ClearBuffWhenDone(tuning.speedBuffDuration));
            }

            Debug.Log($"[Punch] {name} tagged {victim.name}");
        }

        System.Collections.IEnumerator ClearBuffWhenDone(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            _buffCo = null;
            // Motor clears via its own timer; this is a safety sync
        }

        void OnDrawGizmosSelected()
        {
            if (tuning == null) return;
            var t = aimOrigin != null ? aimOrigin : transform;
            Vector3 origin = t.position + Vector3.up * tuning.midTorsoHeight;
            Vector3 fwd = t.forward;
            Gizmos.color = Phase == PunchPhase.Active ? Color.red : new Color(1f, 0.85f, 0.1f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(origin + fwd * (tuning.reach * 0.5f), Quaternion.LookRotation(fwd), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(tuning.width, tuning.height, tuning.reach));
        }
    }
}
