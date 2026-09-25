using System.Collections.Generic;
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
    /// Hot Potato: when fuse Remaining is low (warnSec ~10), It chases harder to dump the tag;
    /// non-It flees harder from the current It.
    /// Least It: when It, prefer chasing runners with low TimeAsIt (leaders) to push their clocks;
    /// when not It, bias flee/wander toward nearby non-It allies.
    /// </summary>
    public class DummyPatrol : MonoBehaviour
    {
        [SerializeField] float radius = 5.5f;
        [SerializeField] float turnSpeed = 220f;
        [Tooltip("Fallback when PunchHitbox/Tuning unavailable. Prefer syncing from PunchTagTuning.reach.")]
        [SerializeField] float punchRange = 1.56f; // hair longer so tip tags connect
        [Tooltip("Fallback full cone (deg). Prefer syncing from PunchTagTuning width/reach.")]
        [SerializeField] float punchConeDeg = 38f; // hair wider so lined-up tags land more often
        [SerializeField] float itGraceSec = 0.85f;
        [SerializeField] float aggression = 0.93f; // slightly hungrier chase punches
        [SerializeField] float cooldownMin = 0.5f;
        [SerializeField] float cooldownMax = 0.78f;
        [SerializeField] float decisionHz = 5.2f; // slightly snappier retargets
        [SerializeField] Vector3 centerOffset = Vector3.zero;
        [SerializeField] float closeChaseRange = 3.7f; // slightly earlier close-chase / punch pressure
        [SerializeField] float leadSeconds = 0.36f; // slight extra lead so intercept cuts read
        [SerializeField] float faceAlignDeg = 15f; // tighter face-up before punch commit
        [Tooltip("Fallback Hot Potato fuse warn window when HotPotatoTuning unavailable.")]
        [SerializeField] float hotPotatoUrgencySec = 10f;
        [Tooltip("Only flee when It is within this planar distance; otherwise wander.")]
        [SerializeField] float fleeThreatRange = 14.5f; // earlier kite start
        [Tooltip("Blend of lateral strafe into flee dir so pure radial chase is harder.")]
        [SerializeField] float fleeStrafeBias = 0.42f; // slightly stronger kite strafe so peel reads
        [Tooltip("Seconds of threat velocity lead when computing flee-from point.")]
        [SerializeField] float fleeLeadSeconds = 0.36f; // slight extra flee lead so kite peels read
        [Tooltip("Forward wish strength while wandering (motor treats y>0.4 as sprint).")]
        [SerializeField] float wanderMoveY = 0.35f;
        [Tooltip("Forward wish while fleeing under Hot Potato urgency.")]
        [SerializeField] float fleeUrgencyMoveY = 1f;
        [Tooltip("Extra planar flee range when Hot Potato fuse is in the warn window.")]
        [SerializeField] float fleeUrgencyThreatBonus = 6f;
        [Tooltip("Punch cooldown scale when It and Hot Potato fuse is urgent (lower = dump faster).")]
        [SerializeField] float chaseUrgencyCooldownScale = 0.62f;
        [Header("Trail Tag avoid")]
        [Tooltip("Only active when TagModeController SelectedMode is TrailTag.")]
        [SerializeField] float trailAvoidRange = 9.4f; // earlier trail peel start
        [Tooltip("Lateral bias on trail flee (same idea as fleeStrafeBias).")]
        [SerializeField] float trailAvoidStrafeBias = 0.45f;
        [Tooltip("How hard trail flee blends into chase/flee/wander wish (0=off).")]
        [SerializeField] float trailAvoidWeight = 0.82f; // slightly stronger trail peel
        [Header("Least It bias")]
        [Tooltip("When It in Least It: meters of chase cost per second of target TimeAsIt (higher = stronger preference for low-time leaders).")]
        [SerializeField] float leastItChaseTimeWeight = 0.75f;
        [Tooltip("When not It in Least It: blend flee/wander toward nearest non-It ally (0=off).")]
        [SerializeField] float leastItAllySeekWeight = 0.48f; // slightly stronger buddy seek when Least It

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
        float _jumpHoldT;
        float _jumpPulseCd;
        float _lungeGate;
        float _airDashGate;
        float _weave;
        float _weaveT;
        float _punchTell;
        readonly List<TrailSegment> _trailActiveScratch = new List<TrailSegment>();

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

            // Never enable legacy CharacterController - TagArena is RB-only.
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
                // Stun / ragdoll proxy owns the lock - do not fight it.
                StopWish();
                return;
            }

            // Countdown and the results card: stay on the pad. Chase starts when the round is live.
            var modes = _modes != null ? _modes : TagModeController.Instance;
            if (modes != null)
            {
                var phase = modes.Phase;
                if (phase == MatchPhase.Countdown || phase == MatchPhase.Results || phase == MatchPhase.Idle)
                {
                    _punchTell = 0f;
                    CancelPunchTelegraph();
                    StopWish();
                    return;
                }
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
            {
                // Just became It: drop flee target and pick prey immediately (don't wait for decisionHz).
                _itGraceTimer = Mathf.Max(0f, itGraceSec);
                Retarget();
            }
            else if (!isIt && _wasIt)
            {
                // Just lost It: retarget so flee locks onto the new It without waiting for decisionHz.
                Retarget();
            }
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
            // Elim / ragdoll / motor lock: drop a cocked arm so it does not linger frozen.
            _punchTell = 0f;
            CancelPunchTelegraph();
        }

        /// <summary>
        /// 0 = calm / not Hot Potato; 1 = fuse about to pop (Remaining near 0).
        /// Uses TagModeController.Remaining vs HotPotatoTuning.warnSec (fallback: hotPotatoUrgencySec).
        /// </summary>
        float HotPotatoFuseUrgency()
        {
            if (_modes == null || _modes.SelectedMode != TagModeId.HotPotato)
                return 0f;
            float remain = _modes.Remaining;
            if (remain <= 0f)
                return 0f;
            float warnSec = hotPotatoUrgencySec;
            var tuning = _modes.HotPotatoTuningAsset;
            if (tuning != null && tuning.warnSec > 0f)
                warnSec = tuning.warnSec;
            float warn = Mathf.Max(0.5f, warnSec);
            return 1f - Mathf.Clamp01(remain / warn);
        }

        bool HotPotatoUrgent() => HotPotatoFuseUrgency() > 0.01f;

        float EffectiveAggression()
        {
            float a = aggression;
            if (_modes != null && _modes.SelectedMode == TagModeId.LeastIt)
                a = 1.0f;
            if (_modes != null && _modes.SelectedMode == TagModeId.HotPotato)
            {
                a = Mathf.Max(a, 0.95f);
                // Desperate tag dump: near-certain punch when fuse is in the warn window.
                float u = HotPotatoFuseUrgency();
                if (u > 0f)
                    a = Mathf.Lerp(a, 1f, u);
            }
            return a;
        }

        float EffectiveLeadSeconds()
        {
            float lead = leadSeconds;
            float u = HotPotatoFuseUrgency();
            if (u > 0f && _it != null && _it.IsIt)
                lead = Mathf.Lerp(lead, lead * 1.35f, u);
            return lead;
        }

        float EffectiveFleeThreatRange()
        {
            float range = fleeThreatRange;
            float u = HotPotatoFuseUrgency();
            if (u > 0f)
                range += fleeUrgencyThreatBonus * u;
            return range;
        }

        float EffectiveFleeStrafeBias()
        {
            float bias = fleeStrafeBias;
            float u = HotPotatoFuseUrgency();
            // Less lateral wobble when fuse is low - commit to getting away from It.
            if (u > 0f)
                bias = Mathf.Lerp(bias, bias * 0.35f, u);
            return bias;
        }

        float EffectiveFleeLeadSeconds()
        {
            float lead = fleeLeadSeconds;
            float u = HotPotatoFuseUrgency();
            if (u > 0f)
                lead = Mathf.Lerp(lead, lead * 1.4f, u);
            return lead;
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
            bool selfIsIt = _it != null && _it.IsIt;
            bool leastIt = _modes != null && _modes.SelectedMode == TagModeId.LeastIt;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (p == null || p == _it || !p.IsAlive || p.IsEliminated) continue;
                // When chasing as It, dump onto nearest non-It (skip other Its if any).
                if (selfIsIt && p.IsIt) continue;
                // When fleeing, prefer locking onto the current It so lose-It Retarget is useful.
                if (!selfIsIt && !p.IsIt) continue;
                float dSq = (p.transform.position - transform.position).sqrMagnitude;
                float score = dSq;
                // Least It + It: prefer tagging leaders (low TimeAsIt) so their clocks rise.
                if (leastIt && selfIsIt && leastItChaseTimeWeight > 0.001f)
                {
                    float dist = Mathf.Sqrt(dSq);
                    score = dist + leastItChaseTimeWeight * Mathf.Max(0f, p.TimeAsIt);
                }
                if (score < best) { best = score; _target = p; }
            }
            if (_target == null) return;
            _targetMotor = _target.GetComponent<PlayerMotor>();
        }

        /// <summary>
        /// Least It only: planar unit toward nearest living non-It ally (pack up to avoid free tags).
        /// </summary>
        Vector3 LeastItAllySeekDir()
        {
            if (_modes == null || _modes.SelectedMode != TagModeId.LeastIt)
                return Vector3.zero;
            if (leastItAllySeekWeight <= 0.01f)
                return Vector3.zero;

            ItController bestAlly = null;
            float best = float.MaxValue;
            foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (p == null || p == _it || !p.IsAlive || p.IsEliminated || p.IsIt) continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; bestAlly = p; }
            }
            if (bestAlly == null) return Vector3.zero;
            Vector3 to = bestAlly.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return Vector3.zero;
            return to.normalized;
        }

        /// <summary>
        /// Trail Tag only: sample a snapshot of live TrailSegments via CopyActive (from PlayerTrailEmitter ribbons)
        /// and cache a weighted lateral flee wish. Cheap - runs at decisionHz.
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

            TrailSegment.CopyActive(_trailActiveScratch);
            foreach (var seg in _trailActiveScratch)
            {
                if (seg == null) continue;
                // Closest point on ribbon A-B (not collider midpoint) so long segments steer correctly.
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
            float lead = EffectiveLeadSeconds();
            if (vel.sqrMagnitude < 0.04f || lead <= 0f)
                return pos;
            // Cap the cut-off. Serialized lead (0.32s, more under a fuse) was a perfect intercept.

            return pos + vel * Mathf.Min(lead, 0.18f);
        }

        void FaceAndSteer(Vector3 desired, float dt, out Vector3 moveDir)
        {
            if (desired.sqrMagnitude < 0.001f)
            {
                moveDir = transform.forward;
                return;
            }
            desired.Normalize();
            // Always turn. The old 16 deg snap made a juke useless once they were lined up.
            Quaternion look = Quaternion.LookRotation(desired, Vector3.up);
            float rate = Mathf.Min(turnSpeed, 150f);
            if (Vector3.Angle(transform.forward, desired) <= faceAlignDeg)
                rate *= 0.65f;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rate * dt);
            moveDir = transform.forward;
        }

        /// <summary>
        /// Hold a lateral bias for about half a second so chase and flee are not a perfect line.
        /// Dropped inside close range so a punch can still line up.
        /// </summary>
        Vector3 ApplyWeave(Vector3 dir, float dt, bool distHold)
        {
            _weaveT -= dt;
            if (dir.sqrMagnitude < 0.001f)
                return dir;
            if (_weaveT <= 0f)
            {
                _weave = Random.Range(-0.58f, 0.58f); // slightly wider for readable chase juke
                _weaveT = Random.Range(0.38f, 0.72f); // slightly snappier weave retarget
            }
            if (!distHold)
                return dir;
            Vector3 side = Vector3.Cross(Vector3.up, dir.normalized);
            if (side.sqrMagnitude < 0.001f)
                return dir;
            return (dir.normalized + side * _weave).normalized;
        }

        /// <summary>
        /// Body-relative wish: AI has no TP cam, so PlayerMotor uses transform as wish basis.
        /// Face first, then push forward - matches human TP (yaw then Move.y).
        /// </summary>
        void DriveWish(float moveY, bool sprint, float strafe = 0f, bool jump = false, bool lunge = false, bool airDash = false)
        {
            if (_input == null) return;
            _input.SetExternalMove(new Vector2(strafe, Mathf.Clamp(moveY, -1f, 1f)), sprint, jump, lunge, airDash);
        }

        float NextPunchCooldown(float urgency)
        {
            float cMin = cooldownMin;
            float cMax = cooldownMax;
            if (urgency > 0f)
            {
                float scale = Mathf.Lerp(1f, Mathf.Clamp(chaseUrgencyCooldownScale, 0.35f, 1f), urgency);
                cMin *= scale;
                cMax *= scale;
            }
            return Random.Range(cMin, cMax);
        }

        void HoldPunchTelegraph()
        {
            var loco = GetComponentInChildren<Tag.Art.DummyLocomotor>();
            if (loco != null) loco.HoldPunchTelegraph();
        }

        void CancelPunchTelegraph()
        {
            var loco = GetComponentInChildren<Tag.Art.DummyLocomotor>();
            if (loco != null) loco.CancelPunchTelegraph();
        }

        /// <summary>
        /// Sample ground 1.4-3.2 m along planarDir. Positive = higher deck than feet.
        /// Lets chase/flee hop a playground lip after weave steers off the ideal line.
        /// </summary>
        float ProbeAheadDeckDy(Vector3 planarDir)
        {
            if (planarDir.sqrMagnitude < 0.01f) return 0f;
            planarDir.Normalize();
            float best = 0f;
            // Include a close sample so a lip under the fists (pressed against a 1 m deck) still registers.
            float[] dists = { 0.7f, 1.1f, 1.6f, 2.3f, 3.2f };
            for (int i = 0; i < dists.Length; i++)
            {
                Vector3 origin = transform.position + planarDir * dists[i] + Vector3.up * 2.8f;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5.5f, ~0, QueryTriggerInteraction.Ignore))
                {
                    float dy = hit.point.y - transform.position.y;
                    if (dy > best) best = dy;
                }
            }
            return best;
        }

        bool ConsumeHop(float dy, float dist, bool grounded, float minDy, float maxDist)
        {
            _jumpPulseCd -= Time.fixedDeltaTime;
            _jumpHoldT -= Time.fixedDeltaTime;
            // Clear lips often put the body <0.8 m from the deck face; allow closer when dy is a real step.
            float minDist = dy >= 1.0f ? 0.35f : 0.8f;
            if (dy > minDy && dist < maxDist && dist > minDist && grounded && _jumpPulseCd <= 0f)
            {
                _jumpHoldT = 0.42f;
                _jumpPulseCd = 0.9f;
            }
            return _jumpHoldT > 0f;
        }

        void TickChase(float dt)
        {
            if (_target == null || !_target.IsAlive) Retarget();
            Vector3 moveDir = transform.forward;
            float moveY = 1f;
            bool sprint = true;
            float urgency = HotPotatoFuseUrgency();

            if (_target != null)
            {
                Vector3 toAim = AimPoint(_target) - transform.position;
                toAim.y = 0f;
                toAim = BlendTrailAvoid(toAim);
                toAim = ApplyWeave(toAim, dt, distHold: toAim.magnitude > closeChaseRange);

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
                // Slightly wider decision cone when dumping a low fuse.
                float cone = EffectivePunchConeHalfDeg() * (1f + 0.2f * urgency);
                bool inCone = dist <= range && ang <= cone;
                // A hard strafe past the fist should whiff - not a guaranteed tag.
                Vector3 juke = TargetPlanarVelocity();
                float lateral = Mathf.Abs(Vector3.Dot(juke, transform.right));
                bool juked = lateral > 6.8f && Random.value < 0.78f; // easier cancel when prey strafes
                // Windup on the punch itself is 0.12s. Cock the arm first so the swing is readable,
                // and drop it if they leave the fist.
                if (_punchTell > 0f)
                {
                    if (!inCone || juked || _itGraceTimer > 0f)
                    {
                        _punchTell = 0f;
                        CancelPunchTelegraph();
                        // Whiff / leave-cone: brief arm-drop before they can cock again.
                        if (juked || !inCone)
                            _cooldown = Mathf.Max(_cooldown, 0.33f); // slightly longer arm drop so juke/leave-cone whiff reads
                        // Juke peel: refresh weave so they leave the punch line instead of re-cocking in place.
                        if (juked)
                        {
                            _weave = Random.Range(0.35f, 0.55f) * (Random.value < 0.5f ? -1f : 1f);
                            _weaveT = Random.Range(0.35f, 0.55f);
                        }
                    }
                    else
                    {
                        HoldPunchTelegraph();
                        _punchTell -= dt;
                        if (_punchTell <= 0f)
                        {
                            _punch?.QueuePunch();
                            _cooldown = NextPunchCooldown(urgency);
                        }
                    }
                }
                else if (inCone && !juked && _itGraceTimer <= 0f && _cooldown <= 0f && Random.value <= EffectiveAggression())
                {
                    _punchTell = Mathf.Lerp(0.36f, 0.22f, urgency); // urgent cock still long enough to read in TP
                    HoldPunchTelegraph();
                }
            }
            else
            {
                _punchTell = 0f;
                CancelPunchTelegraph();
                _angle += (6f / Mathf.Max(0.5f, radius)) * Mathf.Rad2Deg * dt;
                moveY = wanderMoveY;
                sprint = false;
                // Still peel off ribbons while hunting with no target.
                Vector3 peel = BlendTrailAvoid(transform.forward);
                FaceAndSteer(peel, dt, out moveDir);
            }

            // Keep facing coherent even when moveDir came from FaceAndSteer.
            _ = moveDir;
            if (_target == null)
            {
                DriveWish(moveY, sprint);
                return;
            }
            float chaseDy = _target.transform.position.y - transform.position.y;
            Vector3 chaseFlat = _target.transform.position - transform.position;
            chaseFlat.y = 0f;
            bool chaseGrounded = _selfMotor == null || _selfMotor.IsGrounded;
            // After weave, target dy alone can miss a lip between us; probe ahead along chase flat.
            float chaseLip = ProbeAheadDeckDy(chaseFlat);
            bool chaseJump = ConsumeHop(Mathf.Max(chaseDy, chaseLip), chaseFlat.magnitude, chaseGrounded, 0.7f, 9f);
            float chaseAng = chaseFlat.sqrMagnitude > 0.001f
                ? Vector3.Angle(transform.forward, chaseFlat.normalized)
                : 0f;
            _lungeGate -= dt;
            bool chaseLunge = false;
            float reach = EffectivePunchRange();
            float chaseDist = chaseFlat.magnitude;
            if (chaseDist > reach + 0.35f && chaseDist < reach + 4.2f && chaseAng <= 22f && _lungeGate <= 0f)
            {
                chaseLunge = true;
                _lungeGate = 1.35f;
            }
            float chaseStrafe = chaseDist > closeChaseRange
                ? Mathf.Sin(Time.time * 1.6f + transform.GetInstanceID() * 0.017f) * 0.18f
                : 0f;
            DriveWish(moveY, sprint, chaseStrafe, chaseJump, chaseLunge);
        }

        void TickFleeOrWander(float dt)
        {
            Vector3 moveDir = transform.forward;
            ItController threat = null;
            // Prefer Retarget lock (updated on lose-It) when it still points at a living It.
            if (_target != null && _target.IsIt && _target.IsAlive && !_target.IsEliminated)
                threat = _target;
            float bestThreat = float.MaxValue;
            if (threat == null)
            {
                foreach (var p in FindObjectsByType<ItController>(FindObjectsSortMode.None))
                {
                    if (p == null || !p.IsIt || !p.IsAlive || p == _it) continue;
                    float d = (p.transform.position - transform.position).sqrMagnitude;
                    if (d < bestThreat) { bestThreat = d; threat = p; }
                }
            }

            if (threat != null)
            {
                Vector3 threatPos = threat.transform.position;
                var threatMotor = threat.GetComponent<PlayerMotor>();
                float fleeLead = EffectiveFleeLeadSeconds();
                if (threatMotor != null && fleeLead > 0f)
                {
                    Vector3 tv = threatMotor.Velocity;
                    tv.y = 0f;
                    threatPos += tv * fleeLead;
                }

                Vector3 away = transform.position - threatPos;
                away.y = 0f;
                float threatDist = away.magnitude;
                float threatRange = EffectiveFleeThreatRange();
                if (threatDist > threatRange)
                {
                    // Far enough: resume patrol wander instead of endless radial flee.
                    Wander(dt, out moveDir);
                    _ = moveDir;
                    return;
                }

                if (away.sqrMagnitude < 0.01f) away = -transform.forward;
                else away.Normalize();
                // Hold a flank so the human It can cut the corner instead of chasing a perfect radial.
                away = ApplyWeave(away, dt, distHold: true);

                // Strafe bias: prefer current facing side so flee isn't pure radial (easier to cut off).
                // Under Hot Potato urgency, bias shrinks so flee commits away from It.
                Vector3 lateral = Vector3.Cross(Vector3.up, away);
                float strafe = EffectiveFleeStrafeBias();
                if (lateral.sqrMagnitude > 0.001f && strafe > 0.01f)
                {
                    lateral.Normalize();
                    if (Vector3.Dot(lateral, transform.right) < 0f) lateral = -lateral;
                    away = (away + lateral * Mathf.Clamp01(strafe)).normalized;
                }

                // Least It: peel toward a non-It ally so the pack clusters instead of solo runs.
                Vector3 allySeek = LeastItAllySeekDir();
                if (allySeek.sqrMagnitude > 0.01f)
                    away = (away + allySeek * Mathf.Clamp01(leastItAllySeekWeight)).normalized;

                away = BlendTrailAvoid(away);
                FaceAndSteer(away, dt, out moveDir);
                bool urgent = HotPotatoUrgent() || threatDist <= closeChaseRange * 1.6f;
                bool grounded = _selfMotor == null || _selfMotor.IsGrounded;
                // Probe flee heading for a real deck lip; mild panic hop only when It is close.
                float fleeLip = ProbeAheadDeckDy(away);
                float panicDy = threatDist < 5.5f ? 0.9f : 0f; // must clear ConsumeHop minDy (0.85)
                float hopDy = Mathf.Max(fleeLip, panicDy);
                float hopDist = Mathf.Clamp(threatDist, 1.2f, 8f);
                bool jump = ConsumeHop(hopDy, hopDist, grounded, 0.85f, 9f);
                // Hot Potato dump panic: one air dash while airborne if CD is clear (same 30s motor CD).
                _airDashGate -= dt;
                bool airDash = false;
                if (urgent && !grounded && _airDashGate <= 0f && _selfMotor != null
                    && _selfMotor.AirDashCooldownRemaining <= 0.05f)
                {
                    airDash = true;
                    _airDashGate = 1.1f;
                }
                DriveWish(urgent ? fleeUrgencyMoveY : 1f, sprint: true, jump: jump, airDash: airDash);
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
            Vector3 allySeek = LeastItAllySeekDir();
            if (allySeek.sqrMagnitude > 0.01f)
            {
                if (to.sqrMagnitude < 0.001f) to = allySeek;
                else to = (to.normalized + allySeek * Mathf.Clamp01(leastItAllySeekWeight * 0.6f)).normalized;
            }
            to = BlendTrailAvoid(to);
            FaceAndSteer(to, dt, out moveDir);
            DriveWish(wanderMoveY, sprint: false);
        }
    }
}
