using Tag.Experimental;
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
        ExperimentalGrapple _grapple;

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
        float _landHold;
        float _punchTelegraph;
        bool _wasGrounded = true;
        float _bouncePulse;
        bool _bounceWallLeft;
        float _glidePulse;
        float _dashPulse;
        float _dashRecover;
        float _armRecover;
        bool _airDashArms;
        float _dashTrailT;
        float _tagFlinch;
        float _itClaim;
        float _skiBlend;
        float _grapplePose;
        float _wallExit;
        Quaternion _exitUaL, _exitUaR, _exitLaL, _exitLaR;
        Quaternion _exitUlL, _exitUlR, _exitLlL, _exitLlR;
        Quaternion _exitSpine, _exitHips;
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
            if (_grapple == null) _grapple = GetComponentInParent<ExperimentalGrapple>();
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
            // Jump holds a reach while rising. Fall trails the arms once drop speed builds.
            // The jet branch is separate and is not used here.
            float airRise = 0f;
            float airFall = 0f;
            if (air && _motor != null)
            {
                float vy = _motor.Velocity.y;
                // Full tuck on a normal leave, full trail once the drop is clearly down.
                // The quiet band around zero is the apex hang. Jump height is unchanged.
                airRise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.2f, 9f, vy));
                airFall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-3.2f, -9f, vy));
            }
            bool crouch = st == MoveState.Crouch;
            bool skiing = st == MoveState.Ski;
            _skiBlend = Mathf.MoveTowards(_skiBlend, skiing ? 1f : 0f, dt / 0.16f);
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
                _landSquash = Mathf.Clamp(Mathf.Lerp(0.55f, 1.35f, t * t), 0.55f, 1.35f);
                // Brief absorb, then the pose eases into the run instead of popping off.
                _landHold = Mathf.Lerp(0.05f, 0.11f, t);
            }
            _wasGrounded = grounded;
            if (_landHold > 0f)
                _landHold = Mathf.Max(0f, _landHold - dt);
            else
            {
                // ~0.4s from a full buckle back to the stride.
                _landSquash = Mathf.MoveTowards(_landSquash, 0f, dt * 3.1f);
            }
            // Bible WallBounce ~0.22s kick flash - brief TP limb tell after OnWallBounced.
            _bouncePulse = Mathf.MoveTowards(_bouncePulse, 0f, dt / 0.22f);
            bool bouncing = _bouncePulse > 0.04f;
            float bounceAmt = Mathf.Clamp01(_bouncePulse);
            // Bible SuperGlide ~0.28s flat body + crouch hips - TP launch tell.
            _glidePulse = Mathf.MoveTowards(_glidePulse, 0f, dt / 0.28f);
            bool gliding = _glidePulse > 0.04f;
            float glideAmt = Mathf.Clamp01(_glidePulse);

            bool airDashing = _motor != null && _motor.IsAirDashing;
            _tagFlinch = Mathf.MoveTowards(_tagFlinch, 0f, dt / 0.45f);
            _itClaim = Mathf.MoveTowards(_itClaim, 0f, dt / 0.52f);
            bool dashing = _dashPulse > 0.04f || lunging || airDashing;
            float dashAmt = Mathf.Max(
                Mathf.Clamp01(_dashPulse),
                lunging && _motor != null ? _motor.LungeProgress : 0f,
                airDashing && _motor != null ? _motor.AirDashProgress : 0f);
            float flinchAmt = Mathf.Clamp01(_tagFlinch);
            float claimAmt = Mathf.Clamp01(_itClaim);

            float walkAmt = Mathf.Clamp01(speed / 5.5f);
            float runAmt = Mathf.InverseLerp(5.5f, 11.5f, speed);
            // Human-ish run cadence - knees drive the cycle, not ice-skate lock
            float cadence = Mathf.Lerp(7.2f, 11.2f, runAmt);
            // Keep a soft air/vault cycle so limbs stay energetic off the ground
            if (grounded && speed > 0.35f && !sliding && !crouch)
            {
                float rate = Mathf.Lerp(cadence, 5.2f, _skiBlend);
                _cycle += dt * rate;
            }
            else if (air && !jet)
                _cycle += dt * Mathf.Lerp(5.5f, 9f, runAmt);
            else if (!jet)
                _cycle = Mathf.MoveTowards(_cycle, Mathf.Round(_cycle), dt * 8f);

            // Hold the plant and the lift, then cross zero faster - a sine reads as skating.
            float sinRaw = Mathf.Sin(_cycle);
            float sinC = Mathf.Sign(sinRaw) * Mathf.Pow(Mathf.Abs(sinRaw), 0.40f);
            float breath = Mathf.Sin(Time.time * 2.1f) * 2.4f;
            float punchProg = _punch != null ? _punch.PhaseProgress : 0f;

            // Spine / hips lean by state - jet reads clearly in TP
            float leanX = lunging || dashing ? Mathf.Lerp(28f, 48f, dashAmt) : sliding ? 76f : crouch ? 28f : jet ? -22f : wallRun ? 22f : climb ? -16f : mantle ? Mathf.Lerp(42f, 22f, _motor != null ? _motor.MantleProgress : 0.5f) : air ? 18f : breath;
            float leanZ = wallRun ? (_motor != null && _motor.WallLeft ? 32f : -32f) : 0f;
            if (_skiBlend > 0.02f && !dashing && !sliding && !jet)
                leanX = Mathf.Lerp(leanX, 26f, _skiBlend);
            if (flinchAmt > 0.04f)
                leanX = Mathf.Lerp(leanX, 22f, flinchAmt);
            if (claimAmt > 0.04f)
                leanX = Mathf.Lerp(leanX, -12f, claimAmt);
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
            _hipsT = _hips0 * Quaternion.Euler(lunging || dashing ? Mathf.Lerp(18f, 28f, dashAmt) : gliding ? Mathf.Lerp(8f, 22f, glideAmt) : bouncing ? 14f : mantle ? Mathf.Lerp(18f, 8f, mantleAmt) : sliding ? 50f : crouch ? 14f : jet ? -10f : climb ? 12f : air ? 8f : 0f, 0f, -leanZ * 0.55f);
            if (_skiBlend > 0.02f && !dashing && !sliding && !jet)
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(14f, 0f, 0f), _skiBlend);
            _headT = _head0 * Quaternion.Euler(lunging || dashing ? Mathf.Lerp(16f, 22f, dashAmt) : gliding ? Mathf.Lerp(-4f, 8f, glideAmt) : bouncing ? 10f : sliding ? 18f : crouch ? 6f : jet ? -8f : air ? -6f : -breath * 0.4f, 0f, 0f);

            // Arms - slight outward A-pose only (large +Z was V-ing hands into the butt)
            float armZ = Mathf.Lerp(4f, 8f, runAmt);
            float lungeAmt = lunging && _motor != null ? _motor.LungeProgress : 0f;
            // 1 at the start of an air dash or lunge, 0 at the end. The pulse tail keeps easing after the burst.
            float dashStretchPose = 1f;
            if (lunging || dashing)
            {
                if (airDashing && _motor != null)
                {
                    float raw = _motor.AirDashProgress;
                    dashStretchPose = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.InverseLerp(0.08f, 0.62f, raw)));
                    _dashRecover = dashStretchPose;
                    _airDashArms = true;
                    _armRecover = 0.28f;
                }
                else if (lunging)
                {
                    float raw = lungeAmt;
                    dashStretchPose = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.InverseLerp(0.08f, 0.62f, raw)));
                    _dashRecover = dashStretchPose;
                }
                else if (_airDashArms)
                {
                    // The burst already settled toward a hang. The leftover pulse is still high,
                    // and feeding it back in throws the arms into a second whip.
                    _dashRecover = Mathf.MoveTowards(_dashRecover, 0f, dt / 0.12f);
                    dashStretchPose = _dashRecover;
                }
                else
                {
                    float raw = Mathf.Clamp01(_dashPulse);
                    dashStretchPose = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.InverseLerp(0.08f, 0.62f, raw)));
                }
                // Early frames hold the whip. The back half settles toward a hang so the run does not pop in.
                // Pitch only. Extra roll on the Hier A-pose folds the hands into the pelvis.
                _uaLT = Quaternion.Slerp(
                    _uaL0 * Quaternion.Euler(-16f, 0f, armZ),
                    _uaL0 * Quaternion.Euler(96f, -6f, armZ),
                    dashStretchPose);
                _uaRT = Quaternion.Slerp(
                    _uaR0 * Quaternion.Euler(-12f, 0f, -armZ),
                    _uaR0 * Quaternion.Euler(70f, 6f, -armZ),
                    dashStretchPose);
                _laLT = Quaternion.Slerp(
                    _laL0 * Quaternion.Euler(-14f, 0f, 0f),
                    _laL0 * Quaternion.Euler(-58f, 0f, 0f),
                    dashStretchPose);
                _laRT = Quaternion.Slerp(
                    _laR0 * Quaternion.Euler(-12f, 0f, 0f),
                    _laR0 * Quaternion.Euler(-46f, 0f, 0f),
                    dashStretchPose);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(14f, 0f, 0f), _spineT, Mathf.Lerp(0.4f, 1f, dashStretchPose));
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(8f, 0f, 0f), _hipsT, Mathf.Lerp(0.4f, 1f, dashStretchPose));
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
                // One hand reaches, the other pulls. Both stay a long line so the hand
                // can be followed through the swap. A bent elbow at the bottom of the
                // pull used to vanish into the chest. Pitch and the mild A flare only.
                float climbPhase = Mathf.Sin(Time.time * 7.5f);
                float up = (climbPhase + 1f) * 0.5f;
                float down = (-climbPhase + 1f) * 0.5f;
                _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-52f, -118f, up), Mathf.Lerp(10f, 16f, up), armZ);
                _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-52f, -118f, down), Mathf.Lerp(-10f, -16f, down), -armZ);
                _laLT = _laL0 * Quaternion.Euler(Mathf.Lerp(-18f, -8f, up), 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(Mathf.Lerp(-18f, -8f, down), 0f, 0f);
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
                // Wall hand presses along the wall with the outer stride instead of locking.
                // The outer arm opposes that leg and stays a long line. Pitch and the mild A flare only.
                bool left = _motor != null && _motor.WallLeft;
                float wallPhase = Mathf.Sin(Time.time * 9.5f);
                float press = (wallPhase + 1f) * 0.5f;
                float outerFwd = 1f - press;
                float wallPitch = Mathf.Lerp(-42f, -70f, press);
                float wallElbow = Mathf.Lerp(-18f, -10f, press);
                float outerArm = Mathf.Lerp(-28f, -84f, outerFwd);
                float outerElbow = Mathf.Lerp(-16f, -10f, outerFwd);
                if (left)
                {
                    _uaLT = _uaL0 * Quaternion.Euler(wallPitch, 16f, armZ);
                    _uaRT = _uaR0 * Quaternion.Euler(outerArm, -10f, -armZ);
                    _laLT = _laL0 * Quaternion.Euler(wallElbow, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(outerElbow, 0f, 0f);
                }
                else
                {
                    _uaRT = _uaR0 * Quaternion.Euler(wallPitch, -16f, -armZ);
                    _uaLT = _uaL0 * Quaternion.Euler(outerArm, 10f, armZ);
                    _laRT = _laR0 * Quaternion.Euler(wallElbow, 0f, 0f);
                    _laLT = _laL0 * Quaternion.Euler(outerElbow, 0f, 0f);
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
                    // Cock beside the head. The old back-pitch and heavy roll put the fist through the chest.
                    // Yaw carries the elbow out. Roll stays the mild A. Timing stays the authored 0.12s windup.
                    _uaRT = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                    _laRT = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                    _hipsT = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f); // clearer windup hip twist in TP
                    _spineT = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ); // clearer windup spine twist in TP
                }
                else if (phase == PunchPhase.Active)
                {
                    float e = Mathf.Lerp(0.8f, 1f, punchProg);
                    // Long line in front of the chest. A bent elbow disappears at chase distance.
                    // Pitch stays above -150 so the fist does not wrap through the torso.
                    _uaRT = _uaR0 * Quaternion.Euler(-118f, 58f * e, -22f);
                    _laRT = _laR0 * Quaternion.Euler(-18f * e, 0f, 0f);
                    _hipsT = _hips0 * Quaternion.Euler(18f, 22f * e, 0f);
                    _spineT = _spine0 * Quaternion.Euler(leanX + 16f, 42f * e, leanZ);
                }
                else if (phase == PunchPhase.HitRecover)
                {
                    // Hold the connect, then ease into the run so the fist does not snap back when the phase ends.
                    // Timing is unchanged. Pitch on the connect stays above a torso wrap.
                    float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, punchProg));
                    float gait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                    float idle = 1f - gait;
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    Quaternion runL = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle, outY, roll);
                    Quaternion runR = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle, -outY, -roll);
                    float elbowR = Mathf.Lerp(-18f, -8f, idle) - Mathf.Max(0f, sinC) * Mathf.Lerp(28f, 58f, runAmt);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-118f, 52f, -22f), runR, settle);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-18f, 0f, 0f), _laR0 * Quaternion.Euler(elbowR, 0f, 0f), settle);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-32f, 18f, armZ), runL, settle);
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spineT, settle);
                    _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hipsT, settle);
                }
                else // MissRecover - limp whiff: less extension, quicker drop vs HitRecover hold
                {
                    // Cubed ease + soft shoulder sag so a whiff drops faster vs HitRecover hold.
                    float r = Mathf.Lerp(0.62f, 0.02f, punchProg * punchProg * punchProg); // fuller limp drop at end of MissRecover
                    _uaRT = _uaR0 * Quaternion.Euler(-18f - 40f * r, 10f * r, -8f); // softer limp shoulder so whiff reads in TP
                    _laRT = _laR0 * Quaternion.Euler(-14f * r, 0f, 0f);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX + 6f * r, 0f, leanZ), 0.35f);
                }
            }
            else if (sliding)
            {
                // Flat wedge: a long low line. A bent elbow disappears into the chest at chase distance.
                // Pitch and the mild A flare only.
                _uaLT = _uaL0 * Quaternion.Euler(-74f, -6f, armZ);
                _uaRT = _uaR0 * Quaternion.Euler(-68f, 6f, -armZ);
                _laLT = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-10f, 0f, 0f);
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
                // Rise: a long line up and out. Fall: both arms trail back.
                // Apex hangs out to the sides so the top reads before the trail. Mild A only.
                float airW = Mathf.Clamp01(airRise + airFall);
                float riseShare = airW > 0.001f ? airRise / (airRise + airFall) : 0f;
                Quaternion upL = _uaL0 * Quaternion.Euler(-112f, 16f, armZ);
                Quaternion upR = _uaR0 * Quaternion.Euler(-112f, -16f, -armZ);
                Quaternion downL = _uaL0 * Quaternion.Euler(72f, 6f, armZ);
                Quaternion downR = _uaR0 * Quaternion.Euler(72f, -6f, -armZ);
                Quaternion hangL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion hangR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                _uaLT = Quaternion.Slerp(hangL, Quaternion.Slerp(downL, upL, riseShare), airW);
                _uaRT = Quaternion.Slerp(hangR, Quaternion.Slerp(downR, upR, riseShare), airW);
                Quaternion elbowUp = Quaternion.Euler(-12f, 0f, 0f);
                Quaternion elbowDown = Quaternion.Euler(-16f, 0f, 0f);
                Quaternion elbowHang = Quaternion.Euler(-12f, 0f, 0f);
                Quaternion elbow = Quaternion.Slerp(elbowHang, Quaternion.Slerp(elbowDown, elbowUp, riseShare), airW);
                _laLT = _laL0 * elbow;
                _laRT = _laR0 * elbow;
                _spineT = Quaternion.Slerp(
                    _spine0 * Quaternion.Euler(-6f, 0f, 0f),
                    Quaternion.Slerp(_spine0 * Quaternion.Euler(26f, 0f, 0f), _spine0 * Quaternion.Euler(-8f, 0f, 0f), riseShare),
                    airW);
                _hipsT = Quaternion.Slerp(
                    _hips0 * Quaternion.Euler(6f, 0f, 0f),
                    Quaternion.Slerp(_hips0 * Quaternion.Euler(8f, 0f, 0f), _hips0 * Quaternion.Euler(4f, 0f, 0f), riseShare),
                    airW);
            }
            else
            {
                // Opposite the legs. sinC>0 puts the left thigh forward, so the right arm reaches
                // and the left arm stays back. Same-side swing reads as a skate from the chase cam.
                // Rearward travel stays short so the hands do not fold into the pelvis. No extra roll.
                float gait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                float idle = 1f - gait;
                float amp = Mathf.Lerp(36f, 64f, gait);
                // Idle hang sits slightly forward and out. The outward yaw stays on through the
                // stride so the hands do not drop into the hips as the walk starts.
                // Roll stays 0 at rest and only picks up the mild A once the stride is moving.
                float outY = Mathf.Lerp(12f, 8f, gait);
                float roll = Mathf.Lerp(0f, armZ, gait);
                _uaLT = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle, outY, roll);
                _uaRT = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle, -outY, -roll);
                float elbowL = Mathf.Lerp(-18f, -8f, idle) - Mathf.Max(0f, -sinC) * Mathf.Lerp(28f, 58f, runAmt);
                float elbowR = Mathf.Lerp(-18f, -8f, idle) - Mathf.Max(0f, sinC) * Mathf.Lerp(28f, 58f, runAmt);
                _laLT = _laL0 * Quaternion.Euler(elbowL, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(elbowR, 0f, 0f);
                if (_skiBlend > 0.02f)
                {
                    // Skate is a wide balance, not the run's opposing swing. The blend eases both ways.
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-22f, 30f, armZ), _skiBlend);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-22f, -30f, -armZ), _skiBlend);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-12f, 0f, 0f), _skiBlend);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-12f, 0f, 0f), _skiBlend);
                }
            }

            if (_punchTelegraph > 0.02f && !punching)
            {
                // Dummy It cocks before QueuePunch. Same pose as the windup, clear of the chest.
                // The real windup is still only 0.12s; this is the hold pose before QueuePunch.
                float k = Mathf.Clamp01(_punchTelegraph / 0.2f);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-58f, 46f, -armZ), k);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-68f, 0f, 0f), k);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(14f + 8f, -30f, 0f), k); // match windup hip twist
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX + 12f, -36f, leanZ), k); // match windup spine twist
            }

            // Legs
            if (lunging || dashing)
            {
                _ulLT = Quaternion.Slerp(
                    _ulL0 * Quaternion.Euler(16f, 0f, 0f),
                    _ulL0 * Quaternion.Euler(72f, 0f, 0f),
                    dashStretchPose);
                _ulRT = Quaternion.Slerp(
                    _ulR0 * Quaternion.Euler(-6f, 0f, 0f),
                    _ulR0 * Quaternion.Euler(-34f, 0f, 0f),
                    dashStretchPose);
                _llLT = Quaternion.Slerp(
                    _llL0 * Quaternion.Euler(-14f, 0f, 0f),
                    _llL0 * Quaternion.Euler(-62f, 0f, 0f),
                    dashStretchPose);
                _llRT = Quaternion.Slerp(
                    _llR0 * Quaternion.Euler(-8f, 0f, 0f),
                    _llR0 * Quaternion.Euler(-18f, 0f, 0f),
                    dashStretchPose);
            }
            else if (sliding)
            {
                // Flat chase silhouette: lead knee under the chest, trail leg straight behind.
                _ulLT = _ulL0 * Quaternion.Euler(74f, 6f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(-28f, -4f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-94f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
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
                // The leg opposite the reaching hand steps up. That knee bends. The plant leg stays long.
                float climbPhase = Mathf.Sin(Time.time * 7.5f);
                float up = (climbPhase + 1f) * 0.5f;
                _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(62f, 14f, up), 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(14f, 62f, up), 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-(6f + Mathf.Max(0f, -climbPhase) * 72f), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-(6f + Mathf.Max(0f, climbPhase) * 72f), 0f, 0f);
            }
            else if (wallRun)
            {
                // Outer leg steps. Its knee bends only on the way forward. The wall-side leg stays long.
                bool left = _motor != null && _motor.WallLeft;
                float wallPhase = Mathf.Sin(Time.time * 9.5f);
                float outerThigh = 10f + wallPhase * 38f;
                float outerKnee = -(6f + Mathf.Max(0f, wallPhase) * 68f);
                if (left)
                {
                    _ulLT = _ulL0 * Quaternion.Euler(16f, 0f, 0f);
                    _ulRT = _ulR0 * Quaternion.Euler(outerThigh, 0f, 0f);
                    _llLT = _llL0 * Quaternion.Euler(-8f, 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(outerKnee, 0f, 0f);
                }
                else
                {
                    _ulRT = _ulR0 * Quaternion.Euler(16f, 0f, 0f);
                    _ulLT = _ulL0 * Quaternion.Euler(outerThigh, 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(-8f, 0f, 0f);
                    _llLT = _llL0 * Quaternion.Euler(outerKnee, 0f, 0f);
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
                // Tuck on the way up so a hop reads. Lengthen on the way down so a fall is not a skate.
                float airW = Mathf.Clamp01(airRise + airFall);
                float riseShare = airW > 0.001f ? airRise / (airRise + airFall) : 0f;
                Quaternion tuckL = _ulL0 * Quaternion.Euler(58f, 0f, 0f);
                Quaternion tuckR = _ulR0 * Quaternion.Euler(54f, 0f, 0f);
                Quaternion longL = _ulL0 * Quaternion.Euler(4f, 0f, 0f);
                Quaternion longR = _ulR0 * Quaternion.Euler(4f, 0f, 0f);
                Quaternion hangThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion hangThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                _ulLT = Quaternion.Slerp(hangThighL, Quaternion.Slerp(longL, tuckL, riseShare), airW);
                _ulRT = Quaternion.Slerp(hangThighR, Quaternion.Slerp(longR, tuckR, riseShare), airW);
                Quaternion kneeTuckL = _llL0 * Quaternion.Euler(-90f, 0f, 0f);
                Quaternion kneeTuckR = _llR0 * Quaternion.Euler(-86f, 0f, 0f);
                Quaternion kneeLongL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion kneeLongR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion kneeHangL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion kneeHangR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _llLT = Quaternion.Slerp(kneeHangL, Quaternion.Slerp(kneeLongL, kneeTuckL, riseShare), airW);
                _llRT = Quaternion.Slerp(kneeHangR, Quaternion.Slerp(kneeLongR, kneeTuckR, riseShare), airW);
            }
            else
            {
                // Recovery leg takes the knee. The back thigh stays shorter than the front reach
                // so the pair does not meet straight under the hips. Stance knee stays nearly straight.
                float stride = Mathf.Lerp(0.96f, 1.16f, runAmt);
                float reach = Mathf.Lerp(34f, 58f, Mathf.Max(walkAmt, runAmt)) * stride;
                float frontL = Mathf.Max(0f, sinC);
                float frontR = Mathf.Max(0f, -sinC);
                float thighL = (frontL - frontR * 0.58f) * reach;
                float thighR = (frontR - frontL * 0.58f) * reach;
                _ulLT = _ulL0 * Quaternion.Euler(thighL, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(thighR, 0f, 0f);
                float kneeAmt = Mathf.Lerp(48f, 90f, runAmt);
                float kneeL = -(2f + frontL * kneeAmt);
                float kneeR = -(2f + frontR * kneeAmt);
                _llLT = _llL0 * Quaternion.Euler(kneeL, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(kneeR, 0f, 0f);
                if (_skiBlend > 0.02f)
                {
                    float sFrontL = Mathf.Max(0f, sinC);
                    float sFrontR = Mathf.Max(0f, -sinC);
                    float sThighL = (sFrontL - sFrontR * 0.4f) * 16f;
                    float sThighR = (sFrontR - sFrontL * 0.4f) * 16f;
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(sThighL, 0f, 0f), _skiBlend);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(sThighR, 0f, 0f), _skiBlend);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(4f + sFrontL * 14f), 0f, 0f), _skiBlend);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(4f + sFrontR * 14f), 0f, 0f), _skiBlend);
                }
            }

            if (wallRun || climb)
            {
                _wallExit = 1f;
                _exitUaL = _uaLT;
                _exitUaR = _uaRT;
                _exitLaL = _laLT;
                _exitLaR = _laRT;
                _exitUlL = _ulLT;
                _exitUlR = _ulRT;
                _exitLlL = _llLT;
                _exitLlR = _llRT;
                _exitSpine = _spineT;
                _exitHips = _hipsT;
            }
            else if (dashing || punching || sliding || jet || mantle)
                _wallExit = 0f;
            else if (_wallExit > 0f)
            {
                // Leaving a wall run or a climb used to swap onto the run or the fall in one frame.
                _wallExit = Mathf.MoveTowards(_wallExit, 0f, dt / 0.18f);
                float w = _wallExit;
                _uaLT = Quaternion.Slerp(_uaLT, _exitUaL, w);
                _uaRT = Quaternion.Slerp(_uaRT, _exitUaR, w);
                _laLT = Quaternion.Slerp(_laLT, _exitLaL, w);
                _laRT = Quaternion.Slerp(_laRT, _exitLaR, w);
                _ulLT = Quaternion.Slerp(_ulLT, _exitUlL, w);
                _ulRT = Quaternion.Slerp(_ulRT, _exitUlR, w);
                _llLT = Quaternion.Slerp(_llLT, _exitLlL, w);
                _llRT = Quaternion.Slerp(_llRT, _exitLlR, w);
                _spineT = Quaternion.Slerp(_spineT, _exitSpine, w);
                _hipsT = Quaternion.Slerp(_hipsT, _exitHips, w);
            }

            if (_landSquash > 0.08f && grounded && !sliding && !dashing)
            {
                // Ease into the run targets already in _ulLT. SmoothStep keeps the last bit from popping.
                // Balance arms come out so the absorb reads at chase distance. Mild A flare only.
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_landSquash));
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(48f, 0f, 0f), k);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(40f, 0f, 0f), k);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-78f, 0f, 0f), k);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-70f, 0f, 0f), k);
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-40f, 18f, armZ), k);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-40f, -18f, -armZ), k);
                _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-22f, 0f, 0f), k);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-22f, 0f, 0f), k);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(18f, 0f, 0f), k);
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(26f, 0f, 0f), k);
            }

            bool pulling = _grapple != null && _grapple.IsPulling;
            _grapplePose = Mathf.MoveTowards(_grapplePose, pulling ? 1f : 0f, dt / 0.12f);
            if (_grapplePose > 0.04f && !punching)
            {
                // Experimental rope only. Both arms reach as a long line. Legs stay long so it is not a jump tuck.
                // The gate stays off unless the component is added and enableGrapple is turned on.
                float g = _grapplePose;
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-96f, 16f, armZ), g);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-96f, -16f, -armZ), g);
                _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-14f, 0f, 0f), g);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-14f, 0f, 0f), g);
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(8f, 0f, 0f), g);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(6f, 0f, 0f), g);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-8f, 0f, 0f), g);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-8f, 0f, 0f), g);
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(-12f, 0f, 0f), g);
            }

            if (flinchAmt > 0.04f)
            {
                // Tagged runner: a long V in front of the chest. A bent elbow disappears at chase distance.
                // Both knees still bend, so it stays distinct from the new It's one-knee claim. Mild A only.
                float f = flinchAmt;
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-78f, 22f, armZ), f);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-78f, -22f, -armZ), f);
                _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-16f, 0f, 0f), f);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-16f, 0f, 0f), f);
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(22f, 0f, 0f), f);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(22f, 0f, 0f), f);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-48f, 0f, 0f), f);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-48f, 0f, 0f), f);
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(22f, 0f, 0f), f);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(8f, 0f, 0f), f);
            }
            if (claimAmt > 0.04f)
            {
                // New It: one arm up, the other out, chest open. Not the tagged runner's matching V.
                // Pitch stays above a torso wrap. Mild A on the raised arm only.
                float c = claimAmt;
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-128f, 8f, armZ), c);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), c);
                _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-10f, 0f, 0f), c);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-12f, 0f, 0f), c);
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(10f, 0f, 0f), c);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(52f, 0f, 0f), c);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-6f, 0f, 0f), c);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-64f, 0f, 0f), c);
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(-22f, -16f, 0f), c);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(4f, 0f, 0f), c);
            }

            float slew = bouncing || gliding || jet || punching || lunging || dashing || mantle || wallRun || climb || sliding || flinchAmt > 0.04f || claimAmt > 0.04f ? 42f : crouch ? 24f : air ? 18f : 20f;
            // 0.1s air dash never reached the whip pose at slew 42.
            bool punchWind = punching && phase == PunchPhase.Windup;
            bool handoff = flinchAmt > 0.2f || claimAmt > 0.2f;
            bool grappleTell = _grapplePose > 0.2f && !punching;
            // A hop is short. Slew 18 never reached the tuck or the trail before the landing.
            bool apexHang = air && airRise < 0.2f && airFall < 0.2f;
            bool airTell = air && (airRise > 0.12f || airFall > 0.12f || apexHang);
            float armSlewL = airDashing ? 78f : punchWind ? 90f : handoff || grappleTell ? 72f : airTell ? 64f : (punching || lunging || dashing ? 42f : slew);
            float armSlewR = airDashing ? 78f : punchWind ? 90f : handoff || grappleTell ? 72f : airTell ? 64f : (punching || lunging || dashing ? 46f : slew);
            // Run knees have to arrive inside one stride or the flex never shows.
            bool runCycle = grounded && !air && !sliding && !crouch && !dashing && !lunging && speed > 2f;
            // Buckle has to arrive during the short absorb, then follow the ease back into the stride.
            float legSlew = airDashing ? 78f : grappleTell ? 72f : airTell ? 64f : (_landSquash > 0.05f ? 46f : runCycle ? 44f : slew);
            float torsoSlew = grappleTell ? 72f : airTell ? 64f : slew;
            if (!(lunging || dashing))
                _airDashArms = false;
            if (!dashing && !lunging && _armRecover > 0f)
                _armRecover = Mathf.MoveTowards(_armRecover, 0f, dt);
            // After the burst, ease into the fall or the run. Slew 64 snaps the arms into a second throw.
            if (_armRecover > 0f && !airDashing && !dashing && !lunging && !punchWind && !handoff && !grappleTell)
            {
                armSlewL = 16f;
                armSlewR = 16f;
                legSlew = 16f;
                torsoSlew = 16f;
            }
            Slew(ref _spine, _spineT, torsoSlew, dt);
            Slew(ref _hips, _hipsT, torsoSlew, dt);
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
            if (sliding) bob = -0.32f; else if (crouch) bob = -0.14f;
            else if (jet) bob = 0.05f + Mathf.Sin(Time.time * 6.5f) * 0.02f;
            if (_landSquash > 0f) bob -= 0.14f * _landSquash;
            if (dashing) bob += 0.04f * dashAmt;
            if (flinchAmt > 0.04f) bob -= 0.1f * flinchAmt;
            transform.localPosition = _root0 + new Vector3(0f, bob, 0f);
            float squash = 1f - 0.14f * _landSquash;
            // Air-dash: strong stretch then brief squash; tag flinch compresses
            float dashStretch = airDashing ? 0.32f : 0.18f;
            float dashSquash = airDashing ? 0.16f : 0.1f;
            float stretchY = 1f + dashStretch * dashAmt - 0.16f * flinchAmt + 0.06f * claimAmt;
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
                _dashTrailT = 0.33f; // slightly longer air-dash ribbon read
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
            _dashTrail.time = 0.33f;
            _dashTrail.minVertexDistance = 0.04f;
            _dashTrail.widthMultiplier = 0.38f; // slightly wider so air-dash ribbon reads in TP
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

        /// <summary>Tagged runner guard. The new It uses <see cref="PlayItClaim"/>.</summary>
        public void PlayTagFlinch()
        {
            _tagFlinch = 1f;
        }

        /// <summary>New It raises both arms. Separate from the tagged runner's guard.</summary>
        public void PlayItClaim()
        {
            _itClaim = 1f;
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
