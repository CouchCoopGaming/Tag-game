using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Procedural parkour body driven by TagArena MoveState (Apex-Tribes).
    /// No AnimationClips required - readable limb tells for third-person views.
    /// </summary>
    public class DummyLocomotor : MonoBehaviour
    {
        PlayerMotor _motor;
        PunchHitbox _punch;

        Transform _hips, _spine, _head;
        Transform _upperArmL, _upperArmR, _lowerArmL, _lowerArmR;
        Transform _upperLegL, _upperLegR, _lowerLegL, _lowerLegR;
        Quaternion _hips0, _spine0, _head0;
        Quaternion _uaL0, _uaR0, _laL0, _laR0;
        Quaternion _ulL0, _ulR0, _llL0, _llR0;
        Vector3 _root0;
        bool _bound;
        bool _loggedBindFail;
        float _cycle;
        float _landSquash;
        float _punchTelegraph;
        bool _wasGrounded = true;
        float _bouncePulse;
        bool _bounceWallLeft;
        float _glidePulse;
        float _dashPulse;
        float _dashTrailT;
        float _tagFlinch;
        bool _wasLunging;
        bool _wasAirDashing;
        bool _wasJetting;
        PlayerMotor _bounceHooked;
        TrailRenderer _dashTrail;

        Quaternion _spineT, _hipsT, _headT;
        Quaternion _uaLT, _uaRT, _laLT, _laRT;
        Quaternion _ulLT, _ulRT, _llLT, _llRT;

        public void Bind(Transform visualRoot, PlayerMotor motor, PunchHitbox punch, CharacterController ccIgnored = null)
        {
            _motor = motor;
            HookBounce();
            _punch = punch;
            _root0 = transform.localPosition;
            Cache(visualRoot);
            if (!_bound && !_loggedBindFail)
            {
                _loggedBindFail = true;
                Debug.LogWarning($"[DummyLocomotor] Bone bind failed on '{(visualRoot != null ? visualRoot.name : "null")}' - no hierarchical UpperArm/UpperLeg.");
            }
        }

        /// <summary>
        /// True when the visual has a hierarchical limb rig (LowerArm under UpperArm).
        /// Flat sibling mesh mannequins (HiPoly FBX / Dummy_Runner) return false.
        /// </summary>
        public static bool HasBindableBones(Transform root)
        {
            if (root == null) return false;
            var upperArm = FindBone(root, "UpperArm_L", "UpperArm.L", "LeftArm", "LeftUpperArm", "mixamorig:LeftArm", "Arm_L", "upperarm_l", "Upper_Arm_L");
            var lowerArm = FindBone(root, "LowerArm_L", "LowerArm.L", "LeftForeArm", "LeftLowerArm", "mixamorig:LeftForeArm", "ForeArm_L", "lowerarm_l", "Lower_Arm_L");
            var upperLeg = FindBone(root, "UpperLeg_L", "UpperLeg.L", "LeftUpLeg", "LeftUpperLeg", "mixamorig:LeftUpLeg", "Thigh_L", "upperleg_l", "Upper_Leg_L");
            if (upperArm == null || upperLeg == null) return false;
            // Require hierarchy so procedural swing actually moves the distal limb
            if (lowerArm != null && lowerArm.IsChildOf(upperArm) && lowerArm != upperArm)
                return true;
            // Or explicit Hips/Spine empties with arms as descendants (primitive factory)
            var hips = FindBone(root, "Hips", "Pelvis", "mixamorig:Hips", "hip", "Root");
            var spine = FindBone(root, "Spine", "Torso", "Spine1", "mixamorig:Spine", "Chest");
            if (hips != null && upperLeg.IsChildOf(hips))
                return true;
            if (spine != null && upperArm.IsChildOf(spine) && upperArm != spine)
                return true;
            return false;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            _punchTelegraph = Mathf.MoveTowards(_punchTelegraph, 0f, dt);
            if (_motor == null) _motor = GetComponentInParent<PlayerMotor>();
            HookBounce();
            // Cyan dash tell must run even when the limb rig failed to bind.
            TickAirDashTell(dt);
            if (!_bound) Cache(transform);
            if (!_bound) return;
            if (_punch == null) _punch = GetComponentInParent<PunchHitbox>();

            float speed = _motor != null ? _motor.HorizontalSpeed : 0f;
            bool grounded = _motor == null || _motor.IsGrounded;
            var st = _motor != null ? _motor.State : MoveState.Idle;
            bool sliding = st == MoveState.Slide;
            bool jet = st == MoveState.Jet || (_motor != null && _motor.Jetting);
            bool wallRun = st == MoveState.WallRun;
            bool climb = st == MoveState.WallClimb;
            bool mantle = st == MoveState.Mantle;
            bool air = st == MoveState.Air || (!grounded && !climb && !wallRun && !mantle);
            bool crouch = st == MoveState.Crouch;
            bool punching = _punch != null && _punch.IsPunching;
            bool lunging = _motor != null && _motor.IsLunging;
            var phase = _punch != null ? _punch.Phase : PunchPhase.Idle;

            if (grounded && !_wasGrounded)
            {
                // Soft landings = mild squash; hard (near landStunSpeed) = punchier. Clamped.
                float impact = _motor != null ? _motor.LastLandImpactSpeed : 10f;
                float soft = 5f;
                float hard = 24f;
                if (_motor != null && _motor.cfg != null)
                    hard = Mathf.Max(soft + 1f, _motor.cfg.landStunSpeed);
                float t = Mathf.Clamp01(Mathf.InverseLerp(soft, hard, impact));
                // Ease-in so mid falls stay readable but terminal velocity punches.
                // Slightly stronger mid-band so a park hop-off reads without waiting for stun speed.
                _landSquash = Mathf.Clamp(Mathf.Lerp(0.28f, 1.40f, t * t), 0.28f, 1.40f); // park hop-off punchier mid squash
            }
            _wasGrounded = grounded;
            float recover = Mathf.Lerp(8.6f, 5.6f, Mathf.Clamp01(_landSquash)); // snappier park hop-off settle
            _landSquash = Mathf.MoveTowards(_landSquash, 0f, dt * recover);
            // Bible WallBounce ~0.22s kick flash - brief TP limb tell after OnWallBounced.
            _bouncePulse = Mathf.MoveTowards(_bouncePulse, 0f, dt / 0.22f);
            bool bouncing = _bouncePulse > 0.04f;
            float bounceAmt = Mathf.Clamp01(_bouncePulse);
            // Bible SuperGlide ~0.28s flat body + crouch hips - TP launch tell.
            _glidePulse = Mathf.MoveTowards(_glidePulse, 0f, dt / 0.28f);
            bool gliding = _glidePulse > 0.04f;
            float glideAmt = Mathf.Clamp01(_glidePulse);

            bool airDashing = _motor != null && _motor.IsAirDashing;
            _tagFlinch = Mathf.MoveTowards(_tagFlinch, 0f, dt / 0.36f); // slightly longer so tag recoil reads in TP
            bool dashing = _dashPulse > 0.04f || lunging || airDashing;
            float dashAmt = Mathf.Max(
                Mathf.Clamp01(_dashPulse),
                lunging && _motor != null ? _motor.LungeProgress : 0f,
                airDashing && _motor != null ? _motor.AirDashProgress : 0f);
            float flinchAmt = Mathf.Clamp01(_tagFlinch);

            float walkAmt = Mathf.Clamp01(speed / 5.5f);
            float runAmt = Mathf.InverseLerp(5.5f, 11.5f, speed);
            // Human-ish run cadence - knees drive the cycle, not ice-skate lock
            float cadence = Mathf.Lerp(8.0f, 14.5f, runAmt);
            // Keep a soft air/vault cycle so limbs stay energetic off the ground
            if (grounded && speed > 0.35f && !sliding && !crouch)
                _cycle += dt * cadence;
            else if (air && !jet)
                _cycle += dt * Mathf.Lerp(5.5f, 9f, runAmt);
            else if (!jet)
                _cycle = Mathf.MoveTowards(_cycle, Mathf.Round(_cycle), dt * 8f);

            // Hold the plant and the lift, then cross zero faster - a sine reads as skating.
            float sinRaw = Mathf.Sin(_cycle);
            float sinC = Mathf.Sign(sinRaw) * Mathf.Pow(Mathf.Abs(sinRaw), 0.55f);
            float cosC = Mathf.Cos(_cycle);
            // Natural hang/swing - keep amplitude human (not arms-into-butt flares)
            float swing = sinC * Mathf.Lerp(28f, 52f, Mathf.Max(walkAmt, runAmt));
            if (air) swing *= 0.72f;
            if (sliding) swing *= 0.08f; else if (crouch) swing *= 0.18f;
            if (jet) swing = 0f;


            float breath = Mathf.Sin(Time.time * 2.1f) * 2.4f;
            float punchProg = _punch != null ? _punch.PhaseProgress : 0f;

            // Spine / hips lean by state - jet reads clearly in TP
            float leanX = lunging || dashing ? Mathf.Lerp(28f, 48f, dashAmt) : sliding ? 48f : crouch ? 28f : jet ? -22f : wallRun ? 22f : climb ? -16f : mantle ? Mathf.Lerp(42f, 22f, _motor != null ? _motor.MantleProgress : 0.5f) : air ? 18f : breath;
            float leanZ = wallRun ? (_motor != null && _motor.WallLeft ? 32f : -32f) : 0f;
            if (flinchAmt > 0.04f)
            {
                leanX = Mathf.Lerp(leanX, -32f, flinchAmt); // stronger tuck so tag recoil reads in TP
                leanZ = Mathf.Lerp(leanZ, Mathf.Sin(Time.time * 40f) * 20f, flinchAmt); // clearer tag flinch shake in TP
            }
            if (bouncing)
            {
                // Kick wall: spine opens opposite the wall normal (WallLeft = wall on left).
                leanX = Mathf.Lerp(leanX, 28f, bounceAmt);
                leanZ = Mathf.Lerp(leanZ, _bounceWallLeft ? -38f : 38f, bounceAmt);
            }
            if (gliding)
            {
                // Flat launch silhouette - hips read a crouch even if capsule stands.
                leanX = Mathf.Lerp(leanX, 42f, glideAmt);
                leanZ = Mathf.Lerp(leanZ, 0f, glideAmt);
            }
            _spineT = _spine0 * Quaternion.Euler(leanX, 0f, leanZ);
            float mantleAmt = mantle && _motor != null ? _motor.MantleProgress : 0f;
            _hipsT = _hips0 * Quaternion.Euler(lunging || dashing ? Mathf.Lerp(18f, 28f, dashAmt) : gliding ? Mathf.Lerp(8f, 22f, glideAmt) : bouncing ? 14f : mantle ? Mathf.Lerp(18f, 8f, mantleAmt) : sliding ? 28f : crouch ? 14f : jet ? -10f : climb ? 12f : air ? 8f : 0f, 0f, -leanZ * 0.55f);
            _headT = _head0 * Quaternion.Euler(lunging || dashing ? Mathf.Lerp(16f, 22f, dashAmt) : gliding ? Mathf.Lerp(-4f, 8f, glideAmt) : bouncing ? 10f : sliding ? 18f : crouch ? 6f : jet ? -8f : air ? -6f : -breath * 0.4f, 0f, 0f);

            // Arms - slight outward A-pose only (large +Z was V-ing hands into the butt)
            float armZ = Mathf.Lerp(4f, 8f, runAmt);
            float lungeAmt = lunging && _motor != null ? _motor.LungeProgress : 0f;
            if (lunging || dashing)
            {
                // MMB dash / air-dodge tell: hard whip + stretch early, settle late
                float snap = Mathf.Lerp(0.55f, 1f, Mathf.Max(lungeAmt, dashAmt));
                // Keep whip readable but limit +Z flare (large +Z V's hands into the butt on Hier/primitive).
                _uaLT = _uaL0 * Quaternion.Euler(78f * snap, -18f, armZ + 12f);
                _uaRT = _uaR0 * Quaternion.Euler(78f * snap, 18f, -armZ - 12f);
                _laLT = _laL0 * Quaternion.Euler(-38f - 28f * snap, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-38f - 28f * snap, 0f, 0f);
            }
            else if (jet)
            {
                // Jet pack tell + forward reach so thrust reads in TP (no invisible grapple)
                float throb = 0.65f + 0.35f * Mathf.Sin(Time.time * 18f);
                _uaLT = _uaL0 * Quaternion.Euler(18f * throb, 16f, 62f);
                _uaRT = _uaR0 * Quaternion.Euler(-48f * throb, -18f, -58f);
                _laLT = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-48f, 0f, 0f);
            }
            else if (climb)
            {
                // Hand-over-hand reach - louder than idle freeze; opposite phase to legs.
                float climbSwing = Mathf.Sin(Time.time * 7.5f) * 48f;
                _uaLT = _uaL0 * Quaternion.Euler(-138f + climbSwing, 16f, 12f);
                _uaRT = _uaR0 * Quaternion.Euler(-138f - climbSwing, -16f, -12f);
                _laLT = _laL0 * Quaternion.Euler(-55f - Mathf.Abs(climbSwing) * 0.12f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-55f - Mathf.Abs(climbSwing) * 0.12f, 0f, 0f);
            }
            else if (mantle)
            {
                // Progress pull-up - plant: syncs with motor mantle arc (not free Time.sin).
                float m = _motor != null ? _motor.MantleProgress : 0.5f;
                float reach = Mathf.Lerp(-155f, -78f, m);
                float flare = Mathf.Lerp(32f, 14f, m);
                _uaLT = _uaL0 * Quaternion.Euler(reach, Mathf.Lerp(20f, 8f, m), flare);
                _uaRT = _uaR0 * Quaternion.Euler(reach - 4f, Mathf.Lerp(-20f, -8f, m), -flare);
                _laLT = _laL0 * Quaternion.Euler(Mathf.Lerp(-62f, -28f, m), 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(Mathf.Lerp(-62f, -28f, m), 0f, 0f);
            }
            else if (wallRun)
            {
                // Wall-side arm plants toward wall; outer arm balances forward
                bool left = _motor != null && _motor.WallLeft;
                if (left)
                {
                    _uaLT = _uaL0 * Quaternion.Euler(-60f, 28f, 48f);
                    _uaRT = _uaR0 * Quaternion.Euler(-38f, -8f, -30f);
                    _laLT = _laL0 * Quaternion.Euler(-58f, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(-28f, 0f, 0f);
                }
                else
                {
                    _uaLT = _uaL0 * Quaternion.Euler(-38f, 8f, 30f);
                    _uaRT = _uaR0 * Quaternion.Euler(-60f, -28f, -48f);
                    _laLT = _laL0 * Quaternion.Euler(-28f, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(-58f, 0f, 0f);
                }
            }
            else if (gliding)
            {
                // Flat forward reach - reads as mantle-glide launch, not air flail
                float g = glideAmt;
                _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-20f, -72f, g), 12f * g, Mathf.Lerp(14f, 38f, g));
                _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-20f, -72f, g), -12f * g, Mathf.Lerp(-14f, -38f, g));
                _laLT = _laL0 * Quaternion.Euler(Mathf.Lerp(-18f, -36f, g), 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(Mathf.Lerp(-18f, -36f, g), 0f, 0f);
            }
            else if (bouncing)
            {
                // Brief push-off: wall-side arm plants/kicks, outer flings open
                float k = bounceAmt;
                if (_bounceWallLeft)
                {
                    _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-20f, -78f, k), 22f * k, 42f * k);
                    _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-20f, -48f, k), -18f * k, -36f * k);
                    _laLT = _laL0 * Quaternion.Euler(-52f * k, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(-24f * k, 0f, 0f);
                }
                else
                {
                    _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-20f, -48f, k), 18f * k, 36f * k);
                    _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-20f, -78f, k), -22f * k, -42f * k);
                    _laLT = _laL0 * Quaternion.Euler(-24f * k, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(-52f * k, 0f, 0f);
                }
            }
            else if (punching)
            {
                // Clear windup -> connect pose (beyond HitRecover) so tags read in TP
                _uaLT = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                _laLT = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                if (phase == PunchPhase.Windup)
                {
                    float w = Mathf.Lerp(0.55f, 1f, punchProg);
                    // Fist behind the spine vanishes in the chase cam. Flare the elbow out beside the head.
                    // Timing stays the authored 0.12s windup; a bit more elbow yaw so the cock reads in TP.
                    _uaRT = _uaR0 * Quaternion.Euler(28f * w, -26f * w, -56f - 16f * w); // extra elbow yaw for TP cock read
                    _laRT = _laR0 * Quaternion.Euler(-40f - 72f * w, 0f, 0f);
                    _hipsT = _hips0 * Quaternion.Euler(14f + 8f * w, -28f * w, 0f); // clearer windup hip twist in TP
                    _spineT = _spine0 * Quaternion.Euler(leanX + 12f * w, -32f * w, leanZ);
                }
                else if (phase == PunchPhase.Active)
                {
                    float e = Mathf.Lerp(0.8f, 1f, punchProg);
                    _uaRT = _uaR0 * Quaternion.Euler(-55f - 130f * e, 52f * e, -34f); // slightly more elbow flare so Active reads in TP
                    _laRT = _laR0 * Quaternion.Euler(-72f * e, 0f, 0f);
                    _hipsT = _hips0 * Quaternion.Euler(18f, 16f * e, 0f);
                    _spineT = _spine0 * Quaternion.Euler(leanX + 18f, 28f * e, leanZ);
                }
                else if (phase == PunchPhase.HitRecover)
                {
                    // Hold the connect: arm stays punched out + slight overshoot, then eases toward idle faster late.
                    float r = Mathf.Lerp(1.2f, 0.35f, punchProg * punchProg);
                    _uaRT = _uaR0 * Quaternion.Euler(-70f - 110f * r, 54f * r, -38f); // match Active flare so HitRecover still reads in TP
                    _laRT = _laR0 * Quaternion.Euler(-78f * r, 0f, 0f);
                    _uaLT = _uaL0 * Quaternion.Euler(-32f, 18f, armZ + 22f);
                    _spineT = _spine0 * Quaternion.Euler(leanX + 14f * r, 18f * r, leanZ);
                }
                else // MissRecover - limp whiff: less extension, quicker drop vs HitRecover hold
                {
                    // Cubed ease + soft shoulder sag so a whiff drops faster vs HitRecover hold.
                    float r = Mathf.Lerp(0.58f, 0.04f, punchProg * punchProg * punchProg);
                    _uaRT = _uaR0 * Quaternion.Euler(-18f - 40f * r, 10f * r, -8f); // softer limp shoulder so whiff reads in TP
                    _laRT = _laR0 * Quaternion.Euler(-14f * r, 0f, 0f);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX + 6f * r, 0f, leanZ), 0.35f);
                }
            }
            else if (sliding)
            {
                // Dive: arms forward/low - never behind the hips
                _uaLT = _uaL0 * Quaternion.Euler(-48f, -8f, armZ + 12f);
                _uaRT = _uaR0 * Quaternion.Euler(-28f, 10f, -armZ - 8f);
                _laLT = _laL0 * Quaternion.Euler(-42f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
            }
            else if (crouch)
            {
                // Low guard - hands forward of thighs
                _uaLT = _uaL0 * Quaternion.Euler(-18f, -4f, armZ + 4f);
                _uaRT = _uaR0 * Quaternion.Euler(-18f, 4f, -armZ - 4f);
                _laLT = _laL0 * Quaternion.Euler(-28f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-28f, 0f, 0f);
            }
            else if (air)
            {
                // Air / vault limb tells: residual run energy + open arms (slight loft for crest leave)
                float airKick = sinC * Mathf.Lerp(28f, 48f, runAmt);
                _uaLT = _uaL0 * Quaternion.Euler(-32f - airKick * 0.55f, 0f, 8f);
                _uaRT = _uaR0 * Quaternion.Euler(-32f + airKick * 0.55f, 0f, -8f);
                _laLT = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
            }
            else
            {
                // Mostly forward. A full -swing plus extra Z roll put both hands back into the hips.
                // Rest pose already carries the outward A; do not stack more roll on the run.
                float amp = Mathf.Lerp(24f, 46f, Mathf.Max(walkAmt, runAmt));
                _uaLT = _uaL0 * Quaternion.Euler(RunArmPitch(sinC, amp), 0f, 0f);
                _uaRT = _uaR0 * Quaternion.Euler(RunArmPitch(-sinC, amp), 0f, 0f);
                float elbowL = -18f - Mathf.Max(0f, sinC) * Mathf.Lerp(22f, 48f, runAmt);
                float elbowR = -18f - Mathf.Max(0f, -sinC) * Mathf.Lerp(22f, 48f, runAmt);
                _laLT = _laL0 * Quaternion.Euler(elbowL, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(elbowR, 0f, 0f);
            }

            if (_punchTelegraph > 0.02f && !punching)
            {
                // Dummy It cocks before QueuePunch. Match the flared windup elbow so the tell reads in TP.
                // The real windup is still only 0.12s; this is the hold pose before QueuePunch.
                float k = Mathf.Clamp01(_punchTelegraph / 0.2f);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(28f, -26f, -72f), k); // match windup elbow yaw
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-112f, 0f, 0f), k);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(14f + 8f, -28f, 0f), k); // match windup hip twist
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX + 12f, -32f, leanZ), k);
            }

            // Legs
            if (lunging || dashing)
            {
                float stride = Mathf.Lerp(0.7f, 1.15f, Mathf.Max(lungeAmt, dashAmt));
                _ulLT = _ulL0 * Quaternion.Euler(68f * stride, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(-38f * stride, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-58f * stride, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-22f * stride, 0f, 0f);
            }
            else if (sliding)
            {
                // Lead tucked, trail extended - reads as a slide, not a squat
                _ulLT = _ulL0 * Quaternion.Euler(82f, 8f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(28f, -6f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-72f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-18f, 0f, 0f);
            }
            else if (crouch)
            {
                _ulLT = _ulL0 * Quaternion.Euler(62f, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(58f, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-52f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-48f, 0f, 0f);
            }
            else if (jet)
            {
                // Knees slightly extended - hover, not a tuck
                float hover = Mathf.Sin(Time.time * 6.5f) * 5f;
                _ulLT = _ulL0 * Quaternion.Euler(14f + hover, 0f, 8f);
                _ulRT = _ulR0 * Quaternion.Euler(12f - hover, 0f, -8f);
                _llLT = _llL0 * Quaternion.Euler(-10f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-10f, 0f, 0f);
            }
            else if (mantle)
            {
                // Tuck early, lead-leg plant late - readable vault in TP
                float m = _motor != null ? _motor.MantleProgress : 0.5f;
                _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(72f, 28f, m), 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(58f, 42f, m), 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(Mathf.Lerp(-82f, -22f, m), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(Mathf.Lerp(-64f, -38f, m), 0f, 0f);
            }
            else if (climb)
            {
                // Opposite to arms: drive / plant - vertical climb silhouette in TP.
                float stride = Mathf.Sin(Time.time * 7.5f) * 28f;
                _ulLT = _ulL0 * Quaternion.Euler(52f - stride, 0f, 8f);
                _ulRT = _ulR0 * Quaternion.Euler(52f + stride, 0f, -8f);
                _llLT = _llL0 * Quaternion.Euler(-58f + stride * 0.45f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-58f - stride * 0.45f, 0f, 0f);
            }
            else if (wallRun)
            {
                bool left = _motor != null && _motor.WallLeft;
                float stride = Mathf.Sin(Time.time * 9.5f) * 24f;
                if (left)
                {
                    _ulLT = _ulL0 * Quaternion.Euler(38f + stride * 0.35f, 0f, 14f);
                    _ulRT = _ulR0 * Quaternion.Euler(18f - stride, 0f, -8f);
                    _llLT = _llL0 * Quaternion.Euler(-42f, 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(-28f - Mathf.Abs(stride) * 0.3f, 0f, 0f);
                }
                else
                {
                    _ulLT = _ulL0 * Quaternion.Euler(18f + stride, 0f, 8f);
                    _ulRT = _ulR0 * Quaternion.Euler(38f - stride * 0.35f, 0f, -14f);
                    _llLT = _llL0 * Quaternion.Euler(-28f - Mathf.Abs(stride) * 0.3f, 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(-42f, 0f, 0f);
                }
            }
            else if (gliding)
            {
                // Crouch-hip tuck in air - bible: crouch in hips even if capsule stands
                float g = glideAmt;
                _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(18f, 58f, g), 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(16f, 52f, g), 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(Mathf.Lerp(-18f, -48f, g), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(Mathf.Lerp(-16f, -44f, g), 0f, 0f);
            }
            else if (bouncing)
            {
                // Wall-side leg kicks the face; outer tucks - readable off-wall impulse
                float k = bounceAmt;
                if (_bounceWallLeft)
                {
                    _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(18f, 62f, k), 0f, 12f * k);
                    _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(16f, 28f, k), 0f, -6f * k);
                    _llLT = _llL0 * Quaternion.Euler(Mathf.Lerp(-22f, -58f, k), 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(Mathf.Lerp(-18f, -28f, k), 0f, 0f);
                }
                else
                {
                    _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(16f, 28f, k), 0f, 6f * k);
                    _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(18f, 62f, k), 0f, -12f * k);
                    _llLT = _llL0 * Quaternion.Euler(Mathf.Lerp(-18f, -28f, k), 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(Mathf.Lerp(-22f, -58f, k), 0f, 0f);
                }
            }
            else if (air)
            {
                float airKick = sinC * Mathf.Lerp(32f, 55f, runAmt);
                _ulLT = _ulL0 * Quaternion.Euler(22f + airKick, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(18f - airKick, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-35f - Mathf.Abs(airKick) * 0.35f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-28f - Mathf.Abs(airKick) * 0.25f, 0f, 0f);
            }
            else
            {
                // Human run: thigh stride + recovery-leg knee bend (stance more extended)
                float stride = Mathf.Lerp(0.98f, 1.28f, runAmt);
                float thighL = swing * stride;
                float thighR = -swing * stride;
                _ulLT = _ulL0 * Quaternion.Euler(thighL, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(thighR, 0f, 0f);
                float kneeAmt = Mathf.Lerp(32f, 74f, runAmt);
                float baseFlex = Mathf.Lerp(10f, 18f, runAmt);
                // Forward thigh (sin>0 left) flexes; trailing extends
                float kneeL = -(baseFlex + Mathf.Max(0f, sinC) * kneeAmt + Mathf.Max(0f, -cosC) * kneeAmt * 0.25f);
                float kneeR = -(baseFlex + Mathf.Max(0f, -sinC) * kneeAmt + Mathf.Max(0f, cosC) * kneeAmt * 0.25f);
                _llLT = _llL0 * Quaternion.Euler(kneeL, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(kneeR, 0f, 0f);
            }

            if (_landSquash > 0.08f && grounded && !sliding && !dashing)
            {
                // Recovery the squash scale never showed: knees buckle, arms out for balance.
                float k = Mathf.Clamp01(_landSquash);
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(42f, 0f, 8f), k);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(36f, 0f, -8f), k);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-62f, 0f, 0f), k);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-48f, 0f, 0f), k);
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-22f, 10f, 22f), k);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-22f, -10f, -22f), k);
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(26f, 0f, 0f), k);
            }

            if (flinchAmt > 0.04f)
            {
                // Tagged victim recoils: open arms + crumpled torso/legs
                float f = flinchAmt;
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-70f, 28f, 48f), f);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-70f, -28f, -48f), f);
                _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-40f, 0f, 0f), f);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-40f, 0f, 0f), f);
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(48f, 0f, 10f), f);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(48f, 0f, -10f), f);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-52f, 0f, 0f), f);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-52f, 0f, 0f), f);
            }

            float slew = bouncing || gliding || jet || punching || lunging || dashing || mantle || wallRun || climb || sliding || flinchAmt > 0.04f ? 42f : crouch ? 24f : air ? 18f : 20f;
            // 0.1s air dash never reached the whip pose at slew 42.
            bool punchWind = punching && phase == PunchPhase.Windup;
            float armSlewL = airDashing ? 78f : punchWind ? 90f : (punching || lunging || dashing ? 42f : slew);
            float armSlewR = airDashing ? 78f : punchWind ? 90f : (punching || lunging || dashing ? 46f : slew);
            float legSlew = airDashing ? 78f : slew;
            Slew(ref _spine, _spineT, slew, dt);
            Slew(ref _hips, _hipsT, slew, dt);
            Slew(ref _head, _headT, slew, dt);
            Slew(ref _upperArmL, _uaLT, armSlewL, dt);
            Slew(ref _upperArmR, _uaRT, armSlewR, dt);
            Slew(ref _lowerArmL, _laLT, armSlewL, dt);
            Slew(ref _lowerArmR, _laRT, armSlewR, dt);
            Slew(ref _upperLegL, _ulLT, legSlew, dt);
            Slew(ref _upperLegR, _ulRT, legSlew, dt);
            Slew(ref _lowerLegL, _llLT, legSlew, dt);
            Slew(ref _lowerLegR, _llRT, legSlew, dt);


            float step = Mathf.Pow(Mathf.Abs(sinRaw), 1.7f);
            float bob = grounded ? step * 0.085f * Mathf.Max(walkAmt, runAmt) : air ? step * 0.02f : 0f;
            if (sliding) bob = -0.22f; else if (crouch) bob = -0.14f;
            else if (jet) bob = 0.05f + Mathf.Sin(Time.time * 6.5f) * 0.02f;
            if (_landSquash > 0f) bob -= 0.14f * _landSquash;
            if (dashing) bob += 0.04f * dashAmt;
            if (flinchAmt > 0.04f) bob -= 0.1f * flinchAmt;
            transform.localPosition = _root0 + new Vector3(0f, bob, 0f);
            float squash = 1f - 0.14f * _landSquash;
            // Air-dash: strong stretch then brief squash; tag flinch compresses
            float dashStretch = airDashing ? 0.32f : 0.18f;
            float dashSquash = airDashing ? 0.16f : 0.1f;
            float stretchY = 1f + dashStretch * dashAmt - 0.16f * flinchAmt;
            float stretchXZ = 1f - dashSquash * dashAmt + 0.12f * flinchAmt;
            transform.localScale = new Vector3(stretchXZ / squash, squash * stretchY, stretchXZ / squash);
        }

        /// <summary>
        /// Negative pitch is forward on the hanging arm. Rearward recovery stays short
        /// so the hands do not fold back into the hips.
        /// </summary>
        static float RunArmPitch(float phase, float amp)
        {
            float fwd = Mathf.Max(0f, phase) * amp;
            float back = Mathf.Max(0f, -phase) * amp * 0.22f;
            return -(fwd - back);
        }

        void TickAirDashTell(float dt)
        {
            bool lunging = _motor != null && _motor.IsLunging;
            bool airDashing = _motor != null && _motor.IsAirDashing;
            bool jet = _motor != null && (_motor.State == MoveState.Jet || _motor.Jetting);
            if (lunging && !_wasLunging) _dashPulse = 1f;
            if (airDashing && !_wasAirDashing)
            {
                _dashPulse = 1f;
                _dashTrailT = 0.30f; // slightly longer air-dash ribbon read
                EnsureDashTrail();
            }
            if (jet && !_wasJetting) _dashPulse = Mathf.Max(_dashPulse, 0.85f);
            _wasLunging = lunging;
            _wasAirDashing = airDashing;
            _wasJetting = jet;
            _dashPulse = Mathf.MoveTowards(_dashPulse, 0f, dt / 0.18f);
            if (_dashTrailT > 0f) _dashTrailT = Mathf.MoveTowards(_dashTrailT, 0f, dt);
            if (_dashTrail != null)
                _dashTrail.emitting = _dashTrailT > 0.01f || airDashing;
        }

        void EnsureDashTrail()
        {
            if (_dashTrail != null) return;
            var go = new GameObject("AirDashTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.9f, -0.15f);
            _dashTrail = go.AddComponent<TrailRenderer>();
            _dashTrail.time = 0.30f;
            _dashTrail.minVertexDistance = 0.04f;
            _dashTrail.widthMultiplier = 0.36f;
            _dashTrail.emitting = false;
            _dashTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _dashTrail.receiveShadows = false;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.55f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            _dashTrail.colorGradient = grad;
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader);
            var c = new Color(0.45f, 0.9f, 1f, 0.9f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            _dashTrail.sharedMaterial = mat;
        }

        /// <summary>Called while the dummy is committed to a punch but has not swung yet.</summary>
        public void HoldPunchTelegraph()
        {
            _punchTelegraph = 0.2f;
        }

        public void CancelPunchTelegraph()
        {
            _punchTelegraph = 0f;
        }

        /// <summary>Victim tag / punch connect flinch - called from ItController / binder.</summary>
        public void PlayTagFlinch()
        {
            _tagFlinch = 1f;
        }

        void HookBounce()
        {
            if (_motor == _bounceHooked) return;
            if (_bounceHooked != null)
            {
                _bounceHooked.OnWallBounced -= HandleWallBounced;
                _bounceHooked.OnSuperGlide -= HandleSuperGlide;
                _bounceHooked.OnAirDashed -= HandleAirDashed;
            }
            _bounceHooked = _motor;
            if (_bounceHooked != null)
            {
                _bounceHooked.OnWallBounced += HandleWallBounced;
                _bounceHooked.OnSuperGlide += HandleSuperGlide;
                _bounceHooked.OnAirDashed += HandleAirDashed;
            }
        }

        void HandleAirDashed()
        {
            _dashPulse = 1f;
            _dashTrailT = 0.22f;
            EnsureDashTrail();
            if (_dashTrail != null) _dashTrail.emitting = true;
        }

        void HandleWallBounced()
        {
            _bouncePulse = 1f;
            _bounceWallLeft = _motor != null && _motor.WallLeft;
        }

        void HandleSuperGlide()
        {
            _glidePulse = 1f;
        }

        void OnDisable()
        {
            if (_bounceHooked != null)
            {
                _bounceHooked.OnWallBounced -= HandleWallBounced;
                _bounceHooked.OnSuperGlide -= HandleSuperGlide;
                _bounceHooked.OnAirDashed -= HandleAirDashed;
                _bounceHooked = null;
            }
        }

        static void Slew(ref Transform t, Quaternion target, float speed, float dt)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, target, 1f - Mathf.Exp(-speed * dt));
        }

        void Cache(Transform root)
        {
            if (root == null) return;
            _hips = FindBone(root, "Hips", "Pelvis", "mixamorig:Hips", "hip");
            _spine = FindBone(root, "Spine", "Torso", "Spine1", "mixamorig:Spine", "Chest");
            _head = FindBone(root, "Head", "mixamorig:Head", "head");
            _upperArmL = FindBone(root, "UpperArm_L", "UpperArm.L", "LeftArm", "LeftUpperArm", "mixamorig:LeftArm", "Arm_L", "upperarm_l", "Upper_Arm_L");
            _upperArmR = FindBone(root, "UpperArm_R", "UpperArm.R", "RightArm", "RightUpperArm", "mixamorig:RightArm", "Arm_R", "upperarm_r", "Upper_Arm_R");
            _lowerArmL = FindBone(root, "LowerArm_L", "LowerArm.L", "LeftForeArm", "LeftLowerArm", "mixamorig:LeftForeArm", "ForeArm_L", "lowerarm_l", "Lower_Arm_L");
            _lowerArmR = FindBone(root, "LowerArm_R", "LowerArm.R", "RightForeArm", "RightLowerArm", "mixamorig:RightForeArm", "ForeArm_R", "lowerarm_r", "Lower_Arm_R");
            _upperLegL = FindBone(root, "UpperLeg_L", "UpperLeg.L", "LeftUpLeg", "LeftUpperLeg", "mixamorig:LeftUpLeg", "Thigh_L", "upperleg_l", "Upper_Leg_L");
            _upperLegR = FindBone(root, "UpperLeg_R", "UpperLeg.R", "RightUpLeg", "RightUpperLeg", "mixamorig:RightUpLeg", "Thigh_R", "upperleg_r", "Upper_Leg_R");
            _lowerLegL = FindBone(root, "LowerLeg_L", "LowerLeg.L", "LeftLeg", "LeftLowerLeg", "mixamorig:LeftLeg", "Calf_L", "lowerleg_l", "Lower_Leg_L");
            _lowerLegR = FindBone(root, "LowerLeg_R", "LowerLeg.R", "RightLeg", "RightLowerLeg", "mixamorig:RightLeg", "Calf_R", "lowerleg_r", "Lower_Leg_R");
            _bound = _upperArmL != null || _upperLegL != null || _spine != null;
            if (!_bound) return;
            if (_hips) _hips0 = _hips.localRotation;
            if (_spine) _spine0 = _spine.localRotation;
            if (_head) _head0 = _head.localRotation;
            if (_upperArmL) _uaL0 = _upperArmL.localRotation;
            if (_upperArmR) _uaR0 = _upperArmR.localRotation;
            if (_lowerArmL) _laL0 = _lowerArmL.localRotation;
            if (_lowerArmR) _laR0 = _lowerArmR.localRotation;
            if (_upperLegL) _ulL0 = _upperLegL.localRotation;
            if (_upperLegR) _ulR0 = _upperLegR.localRotation;
            if (_lowerLegL) _llL0 = _lowerLegL.localRotation;
            if (_lowerLegR) _llR0 = _lowerLegR.localRotation;
            _spineT = _spine0; _hipsT = _hips0; _headT = _head0;
            _uaLT = _uaL0; _uaRT = _uaR0; _laLT = _laL0; _laRT = _laR0;
            _ulLT = _ulL0; _ulRT = _ulR0; _llLT = _llL0; _llRT = _llR0;
        }

        static Transform FindBone(Transform root, params string[] names)
        {
            if (root == null || names == null || names.Length == 0) return null;
            var all = root.GetComponentsInChildren<Transform>(true);

            // Exact match (any alias)
            for (int n = 0; n < names.Length; n++)
            {
                string want = names[n];
                if (string.IsNullOrEmpty(want)) continue;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].name == want) return all[i];
                }
            }

            // Case-insensitive exact
            for (int n = 0; n < names.Length; n++)
            {
                string want = names[n];
                if (string.IsNullOrEmpty(want)) continue;
                for (int i = 0; i < all.Length; i++)
                {
                    if (string.Equals(all[i].name, want, System.StringComparison.OrdinalIgnoreCase))
                        return all[i];
                }
            }

            // Fuzzy: strip common prefixes, then contains token
            for (int n = 0; n < names.Length; n++)
            {
                string token = NormalizeBoneToken(names[n]);
                if (token.Length < 3) continue;
                Transform best = null;
                int bestScore = int.MaxValue;
                for (int i = 0; i < all.Length; i++)
                {
                    string tn = NormalizeBoneToken(all[i].name);
                    if (tn == token) return all[i];
                    if (tn.Contains(token))
                    {
                        int score = tn.Length - token.Length;
                        if (score < bestScore) { bestScore = score; best = all[i]; }
                    }
                }
                if (best != null) return best;
            }

            return null;
        }

        static string NormalizeBoneToken(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            // strip mixamorig: / Armature / spaces
            int colon = name.LastIndexOf(':');
            if (colon >= 0 && colon + 1 < name.Length) name = name.Substring(colon + 1);
            name = name.Replace(" ", "").Replace(".", "").Replace("-", "");
            return name.ToLowerInvariant();
        }
    }
}
