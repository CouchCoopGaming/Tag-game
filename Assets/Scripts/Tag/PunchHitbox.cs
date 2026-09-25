using UnityEngine;
using Tag.Audio;
using TagArena.Movement;
using Tag.Modes;
using Tag.Art;

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
        TagModeController _mode;
        TagRoundController _roundLegacy;

        public PunchPhase Phase { get; private set; } = PunchPhase.Idle;
        public bool IsPunching => Phase != PunchPhase.Idle;
        /// <summary>0 at phase start, 1 when the phase timer expires.</summary>
        public float PhaseProgress =>
            _phaseDuration <= 0.0001f ? 1f : 1f - Mathf.Clamp01(_phaseTimer / _phaseDuration);

        /// <summary>AI / external: arm punch buffer (same path as input).</summary>
        public void QueuePunch()
        {
            if (tuning == null) return;
            _bufferTimer = Mathf.Max(_bufferTimer, tuning.inputBuffer);
        }

        /// <summary>Active hitbox reach (PunchTagTuning.reach). Used by DummyPatrol swing gating.</summary>
        public float Reach => tuning != null ? tuning.reach : 1.55f;
        /// <summary>Active hitbox width (PunchTagTuning.width). Used with Reach for AI cone.</summary>
        public float Width => tuning != null ? tuning.width : 0.85f;

        float _phaseTimer;
        float _phaseDuration;
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
            _mode = TagModeController.Instance != null
                ? TagModeController.Instance
                : FindFirstObjectByType<TagModeController>();
            if (_mode == null)
                _roundLegacy = FindFirstObjectByType<TagRoundController>();
        }

        void Update()
        {
            // Pause freezes the clock. Results stay at timeScale 1 with the cursor unlocked.
            // Either way a swing that started on the menu click must not finish into gameplay.
            if (Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked || ResumeInputGate.Blocking)
            {
                DropSwing();
                return;
            }
            float dt = Time.deltaTime;

            if (_input != null && _input.PunchPressed)
                _bufferTimer = tuning.inputBuffer;
            else
                _bufferTimer = Mathf.Max(0f, _bufferTimer - dt);

            // Local It: cock the fist while the punch buffer is armed (same tell AI uses).
            if (Phase == PunchPhase.Idle && _bufferTimer > 0f &&
                _it != null && _it.IsIt && !_it.IsEliminated)
                HoldLocalPunchTell();

            bool canStart =
                Phase == PunchPhase.Idle
                && _bufferTimer > 0f
                && _it != null && _it.IsIt && !_it.IsEliminated
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
            HoldLocalPunchTell();
            Phase = PunchPhase.Windup;
            _phaseDuration = tuning.windup;
            _phaseTimer = _phaseDuration;
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
                _phaseDuration = tuning.active;
                _phaseTimer = _phaseDuration;
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
                _phaseDuration = tuning.hitRecover;
                _phaseTimer = _phaseDuration;
                return;
            }

            _phaseTimer -= dt;
            if (_phaseTimer <= 0f)
            {
                Phase = PunchPhase.MissRecover;
                // Soft fail: quieter/higher TagSfx + light cam nudge (connect keeps strong kick)
                TagSfx.PunchMiss(transform.position);
                var tpsMiss = GetComponentInChildren<TpsMoveCamera>(true);
                if (tpsMiss != null)
                    tpsMiss.AddKick(new Vector3(0f, 0.025f, -0.06f));
                _phaseDuration = tuning.missRecover;
                _phaseTimer = _phaseDuration;
            }
        }

        void TickRecover(float dt)
        {
            _phaseTimer -= dt;
            if (_phaseTimer <= 0f)
                EndPunch();
        }

        void HoldLocalPunchTell()
        {
            var loco = GetComponentInChildren<DummyLocomotor>();
            if (loco != null) loco.HoldPunchTelegraph();
        }

        /// <summary>Drop a swing that started on the results click so it does not carry into countdown.</summary>
        public void ForceEnd() => DropSwing();

        void DropSwing()
        {
            _bufferTimer = 0f;
            if (Phase != PunchPhase.Idle)
                EndPunch();
            var loco = GetComponentInChildren<DummyLocomotor>();
            if (loco != null) loco.CancelPunchTelegraph();
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
            // Pitch clamp +/-tolerance around planar forward
            Vector3 flatFwd = new Vector3(aimOrigin.forward.x, 0f, aimOrigin.forward.z).normalized;
            if (flatFwd.sqrMagnitude < 0.001f) flatFwd = transform.forward;
            Vector3 forward = Vector3.RotateTowards(
                flatFwd, aimOrigin.forward, tuning.pitchToleranceDeg * Mathf.Deg2Rad, 0f);

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
            if (other.IsEliminated) return;
            if (other.IsIt) return; // runners only
            if (!other.CanBeTagged) return;

            // Systems Tag v1: air-dodge i-frames vs punch hurtbox only
            var victimMotor = other.GetComponent<PlayerMotor>();
            if (victimMotor != null && victimMotor.HasAirDodgeIFrames) return;

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
            if (_mode != null)
                _mode.OnSuccessfulPunch(_it, victim);
            else if (_roundLegacy != null)
                _roundLegacy.OnSuccessfulPunch(_it, victim);
            else if (_it != null && _it.IsIt)
            {
                _it.SetIt(false);
                victim.SetIt(true);
            }

            // Punch impact (TagSfx has Resources clip + procedural fallback); become-It chirp from SetIt(true)
            TagSfx.PunchConnect(transform.position);

            // Readable TP punch connect: stronger camera kick + FOV punch on attacker
            var tps = GetComponentInChildren<TpsMoveCamera>(true);
            if (tps != null)
                tps.AddKick(new Vector3(0f, 0.14f, -0.38f));
            // Victim's chase cam, lighter than the attacker's. No hitstop — nothing else freezes time.
            var victimCam = victim.GetComponentInChildren<TpsMoveCamera>(true);
            if (victimCam != null && victimCam != tps)
                victimCam.AddKick(new Vector3(0.04f, 0.08f, -0.18f));

            // Target ragdoll / kinematic stun proxy + i-frames; hit pulse fires on It visual swap
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
