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
        PlayerInputReader _input;
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
        float _landHard;
        float _punchTelegraph;
        bool _wasGrounded = true;
        float _pushOff;
        bool _pushLeft;
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
        float _stopGait;
        float _stopRun;
        float _stopPlant;
        bool _stopPlanted;
        bool _stopPlantLeft;
        float _runVis;
        float _swayVis;
        float _idlePhase;
        bool _swayIdle;
        float _stepIn;
        float _dropVis;
        bool _dropSlide;
        float _diveVis;
        float _surfPhase;
        float _surfIn;
        bool _wasSurf;
        float _prevYaw;
        float _turnVis;
        bool _hasYaw;
        float _lookArmVis;
        float _grapplePose;
        float _wallExit;
        bool _exitLeadLeft;
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
            if (_input == null) _input = GetComponentInParent<PlayerInputReader>();
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
            bool onSurf = wallRun || climb;
            bool leavingSurf = _wasSurf && !onSurf;
            if (onSurf && !_wasSurf)
                _surfPhase = 0f;
            if (onSurf)
            {
                // Hand meets the surface, then the swing starts. A clock sine pops the arm.
                _surfIn = Mathf.MoveTowards(_surfIn, 1f, dt / 0.1f);
                _surfPhase += dt * (climb ? 7.5f : 9.5f);
            }
            else
                _surfIn = 0f;
            _wasSurf = onSurf;
            if (leavingSurf)
                _cycle = _exitLeadLeft ? Mathf.PI * 0.5f : Mathf.PI * 1.5f;
            // Jump holds a reach while rising. Fall trails the arms once drop speed builds.
            // The jet branch is separate and is not used here.
            float airRise = 0f;
            float airFall = 0f;
            float diveAmt = 0f;
            if (air && _motor != null)
            {
                float vy = _motor.Velocity.y;
                // Full tuck on a normal leave, full trail once the drop is clearly down.
                // The quiet band around zero is the apex hang. Jump height is unchanged.
                airRise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.2f, 9f, vy));
                airFall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-3.2f, -9f, vy));
                // Visual only. airCrouchFallMult stays 2. The dart starts as soon as the drop is readable.
                bool airCrouch = !jet && _input != null && _input.CrouchHeld;
                if (airCrouch)
                    diveAmt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-1.2f, -6.5f, vy));
            }
            // The crouch arrives with the fall. Leaving it eases, so the arms do not pop on the land.
            float diveStep = diveAmt >= _diveVis ? 1f : dt / 0.16f;
            _diveVis = Mathf.MoveTowards(_diveVis, diveAmt, diveStep);
            bool crouch = st == MoveState.Crouch;
            // Drop into the guard or the wedge, then rise back out. Speed is unchanged.
            _dropVis = Mathf.MoveTowards(_dropVis, sliding || crouch ? 1f : 0f, dt / 0.16f);
            if (sliding)
                _dropSlide = true;
            else if (crouch)
                _dropSlide = false;
            else if (_dropVis <= 0.001f)
                _dropSlide = false;
            // Stand-up from a slide: the feet enter the stride while the hips are still low.
            // slideBoost stays 0. The speed you already have carries.
            bool slideExit = _dropSlide && !sliding && !crouch;
            float footDrop = slideExit ? _dropVis * _dropVis : _dropVis;
            float hipDrop = slideExit ? Mathf.SmoothStep(0f, 1f, _dropVis) : _dropVis;
            bool skiing = st == MoveState.Ski;
            _skiBlend = Mathf.MoveTowards(_skiBlend, skiing ? 1f : 0f, dt / 0.22f);
            // Feet stay in the short glide while the hips are still pitched, then the run opens under them.
            float footSki = _skiBlend * (2f - _skiBlend);
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
                _landHard = t;
            }
            if (!grounded && _wasGrounded && _motor != null && _motor.Velocity.y > 1.5f)
            {
                // Push off the foot that was down. Jump height is unchanged.
                _pushLeft = Mathf.Cos(_cycle) < 0f;
                _pushOff = 1f;
            }
            else if (!grounded)
                _pushOff = Mathf.MoveTowards(_pushOff, 0f, dt / 0.12f);
            else
                _pushOff = 0f;
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
            // Keep a soft air/vault cycle so limbs stay energetic off the ground.
            // Walk and sprint ease length and tempo. The cycle keeps advancing, so a plant does not freeze.
            if (airDashing)
            {
                // The burst leads with the left thigh. Hold the stride there so the
                // landing does not skate onto the other foot. Dash time is unchanged.
                _runVis = runAmt;
                _cycle = Mathf.PI * 0.5f;
                _stopGait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                _stopRun = _runVis;
            }
            else if (grounded && speed > 0.35f && !sliding && !crouch)
            {
                _runVis = Mathf.MoveTowards(_runVis, runAmt, dt / 0.2f);
                float cadence = Mathf.Lerp(7.2f, 11.2f, _runVis);
                float rate = Mathf.Lerp(cadence, 5.2f, footSki);
                _cycle += dt * rate;
                _stopGait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                _stopRun = _runVis;
            }
            else if (sliding)
            {
                // Keep the stride that entered the slide. Closing it skates the exit.
                // slideBoost stays 0. Speed is unchanged.
                _stopGait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                _stopRun = _runVis;
            }
            else if (air && !jet)
            {
                if (_armRecover > 0f)
                {
                    // Stay on the dash lead until the feet are back on the ground.
                    _runVis = runAmt;
                    _cycle = Mathf.PI * 0.5f;
                }
                else
                {
                    _runVis = runAmt;
                    _cycle += dt * Mathf.Lerp(5.5f, 9f, runAmt);
                }
            }
            else if (crouch && grounded && speed > 0.35f)
            {
                // Short shuffle under the hips. The guard stays low. Speed is unchanged.
                _cycle += dt * 6.2f;
                _runVis = runAmt;
            }
            else if (!jet)
            {
                // Close onto a stride where the sine is 0. Rounding to an integer left a leg stuck out,
                // which read as a skate stop. Speed is unchanged.
                float plant = Mathf.PI * Mathf.Round(_cycle / Mathf.PI);
                _cycle = Mathf.MoveTowards(_cycle, plant, dt * 4.2f);
                if (!sliding && !crouch)
                {
                    _stopGait = Mathf.MoveTowards(_stopGait, 0f, dt / 0.28f);
                    _runVis = Mathf.MoveTowards(_runVis, 0f, dt / 0.28f);
                    _stopRun = _runVis;
                }
                else
                    _runVis = runAmt;
            }
            else
                _runVis = runAmt;

            bool stepping = grounded && speed > 0.35f && !sliding && !crouch;
            if (air)
                _stepIn = 1f;
            else if (stepping)
                _stepIn = Mathf.MoveTowards(_stepIn, 1f, dt / 0.32f);
            else if (crouch)
            {
                float remain = Mathf.Abs(_cycle - Mathf.PI * Mathf.Round(_cycle / Mathf.PI));
                if (remain < 0.25f)
                    _stepIn = Mathf.MoveTowards(_stepIn, 0f, dt / 0.12f);
            }
            else if (speed <= 0.35f)
            {
                float remain = Mathf.Abs(_cycle - Mathf.PI * Mathf.Round(_cycle / Mathf.PI));
                if (remain < 0.25f)
                    _stepIn = Mathf.MoveTowards(_stepIn, 0f, dt / 0.12f);
            }

            // Hold the plant and the lift, then cross zero faster - a sine reads as skating.
            // The first step uses the raw sine so it pushes off the plant instead of skating.
            float sinRaw = Mathf.Sin(_cycle);
            float sinShaped = Mathf.Sign(sinRaw) * Mathf.Pow(Mathf.Abs(sinRaw), 0.40f);
            float sinC = Mathf.Lerp(sinRaw, sinShaped, stepping ? Mathf.SmoothStep(0f, 1f, _stepIn) : 1f);
            float breath = Mathf.Sin(Time.time * 2.1f) * 2.4f;
            float punchProg = _punch != null ? _punch.PhaseProgress : 0f;

            // Spine / hips lean by state - jet reads clearly in TP
            float leanX = lunging || dashing ? Mathf.Lerp(28f, 48f, dashAmt) : jet ? -22f : wallRun ? 22f : climb ? -16f : mantle ? Mathf.Lerp(42f, 22f, _motor != null ? _motor.MantleProgress : 0.5f) : air ? 18f : breath;
            float leanZ = wallRun ? (_motor != null && _motor.WallLeft ? 32f : -32f) : 0f;
            float idleW = 0f;
            bool atRest = grounded && !dashing && !sliding && !crouch && !jet && !wallRun && !climb && !mantle && !air && !lunging && flinchAmt < 0.04f && claimAmt < 0.04f;
            if (atRest)
                idleW = 1f - Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
            // After the feet close, the last hip roll eases into the idle sway.
            // A clock sine pops the hips. Holding them flat until the sway starts reads as a freeze.
            float strideRemain = Mathf.Abs(_cycle - Mathf.PI * Mathf.Round(_cycle / Mathf.PI));
            bool stopping = atRest && speed <= 0.35f;
            if (!stopping)
            {
                _stopPlanted = false;
                _stopPlant = 0f;
            }
            else if (!_stopPlanted)
            {
                // The back foot is the one that stays. The front foot finishes the close.
                _stopPlanted = true;
                _stopPlantLeft = sinC < 0f;
            }
            if (stopping)
                _stopPlant = Mathf.MoveTowards(_stopPlant, 1f, dt / 0.12f);
            float closeRoll = 0f;
            if (stopping && !_swayIdle)
                closeRoll = sinC * Mathf.Lerp(3.2f, 5.5f, _stopRun) * Mathf.Max(_stopGait, Mathf.Clamp01(strideRemain / 0.55f));
            if (!stopping)
                _swayIdle = false;
            else if (!_swayIdle && strideRemain < 0.22f)
            {
                _swayIdle = true;
                float n = Mathf.Clamp(_swayVis / 5f, -1f, 1f);
                float a = Mathf.Asin(n);
                // Leave a peak toward center so the idle sway continues the settle.
                _idlePhase = n >= 0f ? Mathf.PI - a : a;
            }
            if (_swayIdle)
                _idlePhase += dt * 0.8f;
            float swayTarget = !atRest ? 0f : _swayIdle ? Mathf.Sin(_idlePhase) * 5f : closeRoll;
            _swayVis = Mathf.MoveTowards(_swayVis, swayTarget, dt * 28f);
            if (idleW > 0.02f)
                leanX = breath * (1f + idleW);
            if (atRest && (stopping || idleW > 0.02f || Mathf.Abs(_swayVis) > 0.2f))
                leanZ = _swayVis;
            if (_skiBlend > 0.02f && !dashing && !sliding && !jet)
                leanX = Mathf.Lerp(leanX, 26f, _skiBlend);
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
            _hipsT = _hips0 * Quaternion.Euler(lunging || dashing ? Mathf.Lerp(18f, 28f, dashAmt) : gliding ? Mathf.Lerp(8f, 22f, glideAmt) : bouncing ? 14f : mantle ? Mathf.Lerp(18f, 8f, mantleAmt) : jet ? -10f : climb ? 12f : air ? 8f : 0f, 0f, -leanZ * 0.55f);
            if (_skiBlend > 0.02f && !dashing && !sliding && !jet)
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(14f, 0f, 0f), _skiBlend);
            _headT = _head0 * Quaternion.Euler(lunging || dashing ? Mathf.Lerp(16f, 22f, dashAmt) : gliding ? Mathf.Lerp(-4f, 8f, glideAmt) : bouncing ? 10f : jet ? -8f : air ? -6f : -breath * 0.4f, 0f, 0f);
            float swayFade = Mathf.Max(idleW, atRest ? Mathf.Clamp01(Mathf.Abs(_swayVis) / 5f) : 0f);
            if (swayFade > 0.02f)
                _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-breath * 0.5f, 0f, -_swayVis * 0.35f), swayFade);

            Transform yawSrc = _motor != null ? _motor.transform : transform;
            float yawNow = yawSrc.eulerAngles.y;
            if (!_hasYaw)
            {
                _prevYaw = yawNow;
                _hasYaw = true;
            }
            float yawRate = dt > 0.0001f ? Mathf.DeltaAngle(_prevYaw, yawNow) / dt : 0f;
            _prevYaw = yawNow;
            // Positive yaw is a right turn. Visual only. Look speed is unchanged.
            bool canTurn = grounded && !air && !sliding && !crouch && !dashing && !jet && !wallRun && !climb && !mantle;
            float turnTarget = canTurn ? Mathf.Clamp(yawRate / 280f, -1f, 1f) : 0f;
            _turnVis = Mathf.MoveTowards(_turnVis, turnTarget, dt / 0.1f);
            if (Mathf.Abs(_turnVis) > 0.12f && canTurn)
            {
                // Same roll on the chest and the hips. A counter-roll reads as a twist at the waist.
                float w = Mathf.Clamp01(Mathf.Abs(_turnVis));
                Quaternion lean = Quaternion.Euler(0f, 0f, _turnVis * 4.5f);
                _spineT = Quaternion.Slerp(_spineT, _spineT * lean, w);
                _hipsT = Quaternion.Slerp(_hipsT, _hipsT * lean, w);
            }

            // Arms - slight outward A-pose only (large +Z was V-ing hands into the butt)
            float armZ = Mathf.Lerp(4f, 8f, _runVis);
            // Camera pitch only. The look gate and the sensitivity stay on the camera.
            float lookPitch = 0f;
            Transform lookCam = _motor != null ? _motor.cam : null;
            if (lookCam != null && lookCam.parent != null)
            {
                float x = lookCam.parent.localEulerAngles.x;
                if (x > 180f) x -= 360f;
                lookPitch = Mathf.Clamp(x, -25f, 55f);
            }
            _lookArmVis = Mathf.MoveTowards(_lookArmVis, lookPitch, dt * 240f);
            float lungeAmt = lunging && _motor != null ? _motor.LungeProgress : 0f;
            // 1 at the start of an air dash or lunge, 0 at the end. The pulse tail keeps easing after the burst.
            float dashStretchPose = 1f;
            if (lunging || dashing)
            {
                if (airDashing && _motor != null)
                {
                    // Hold the whip for the whole burst. A curve that dies early never
                    // reaches the chest and arms inside 0.1s. Duration and cooldown are unchanged.
                    dashStretchPose = 1f;
                    _dashRecover = 1f;
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
                // Pitch only. Extra roll on the Hier A-pose folds the hands into the pelvis.
                if (airDashing || (_airDashArms && !lunging))
                {
                    // Wide and back, clear of the fall trail, so the short burst reads from behind.
                    // The same pose eases out after the burst. It does not whip a second time.
                    _uaLT = Quaternion.Slerp(
                        _uaL0 * Quaternion.Euler(-16f, 0f, armZ),
                        _uaL0 * Quaternion.Euler(108f, 32f, armZ),
                        dashStretchPose);
                    _uaRT = Quaternion.Slerp(
                        _uaR0 * Quaternion.Euler(-12f, 0f, -armZ),
                        _uaR0 * Quaternion.Euler(108f, -32f, -armZ),
                        dashStretchPose);
                    _laLT = Quaternion.Slerp(
                        _laL0 * Quaternion.Euler(-14f, 0f, 0f),
                        _laL0 * Quaternion.Euler(-22f, 0f, 0f),
                        dashStretchPose);
                    _laRT = Quaternion.Slerp(
                        _laR0 * Quaternion.Euler(-12f, 0f, 0f),
                        _laR0 * Quaternion.Euler(-22f, 0f, 0f),
                        dashStretchPose);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(58f, 0f, 0f), dashStretchPose);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), dashStretchPose);
                }
                else
                {
                    // Early frames hold the whip. The back half settles toward a hang so the run does not pop in.
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
                // One hand meets the surface, then they trade. The swing eases in
                // so the grab does not pop. Pitch and the mild A flare only.
                float climbLive = Mathf.Sin(_surfPhase);
                float upLive = (climbLive + 1f) * 0.5f;
                float up = Mathf.Lerp(0.8f, upLive, _surfIn);
                float down = 1f - up;
                _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-52f, -118f, up), Mathf.Lerp(12f, 18f, up), armZ);
                _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-52f, -118f, down), Mathf.Lerp(-12f, -18f, down), -armZ);
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
                // Wall hand presses into the surface, then travels with the stride.
                // The outer arm stays a long line. Pitch and the mild A flare only.
                bool left = _motor != null && _motor.WallLeft;
                float wallLive = Mathf.Sin(_surfPhase);
                float pressLive = (wallLive + 1f) * 0.5f;
                float press = Mathf.Lerp(0.55f, pressLive, _surfIn);
                float outerFwd = 1f - press;
                float yaw = Mathf.Lerp(18f, 34f, _surfIn);
                float wallPitch = Mathf.Lerp(-48f, -72f, press);
                float wallElbow = Mathf.Lerp(-16f, -8f, press);
                float outerArm = Mathf.Lerp(-36f, Mathf.Lerp(-28f, -84f, 1f - pressLive), _surfIn);
                float outerElbow = Mathf.Lerp(-16f, -10f, outerFwd);
                if (left)
                {
                    _uaLT = _uaL0 * Quaternion.Euler(wallPitch, yaw, armZ);
                    _uaRT = _uaR0 * Quaternion.Euler(outerArm, -10f, -armZ);
                    _laLT = _laL0 * Quaternion.Euler(wallElbow, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(outerElbow, 0f, 0f);
                }
                else
                {
                    _uaRT = _uaR0 * Quaternion.Euler(wallPitch, -yaw, -armZ);
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
                    // The cock arrives in the first beat and holds, so the tell reads before the strike.
                    // Yaw carries the elbow out. Roll stays the mild A. Timing stays the authored 0.12s windup.
                    float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
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
                    // Hold the connect, then ease onward so the fist does not snap when the phase ends.
                    // A tag eases into the new It's claim. A clean hit still eases into the run.
                    // Timing is unchanged. Pitch on the connect stays above a torso wrap.
                    float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, punchProg));
                    Quaternion connectR = _uaR0 * Quaternion.Euler(-118f, 52f, -22f);
                    Quaternion connectL = _uaL0 * Quaternion.Euler(-32f, 18f, armZ);
                    if (claimAmt > 0.04f)
                    {
                        _uaRT = Quaternion.Slerp(connectR, _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), settle);
                        _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-18f, 0f, 0f), _laR0 * Quaternion.Euler(-12f, 0f, 0f), settle);
                        _uaLT = Quaternion.Slerp(connectL, _uaL0 * Quaternion.Euler(-128f, 8f, armZ), settle);
                        _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-22f, 0f, 0f), _laL0 * Quaternion.Euler(-10f, 0f, 0f), settle);
                        _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spine0 * Quaternion.Euler(-22f, -16f, 0f), settle);
                        _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hips0 * Quaternion.Euler(4f, 0f, 0f), settle);
                    }
                    else
                    {
                        // The arm opposite the front knee goes back to the stride while the
                        // fist is still out. The hips leave the punch twist with that arm.
                        // Windup time is unchanged.
                        float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
                        float idle = 1f - gait;
                        float amp = Mathf.Lerp(36f, 64f, gait);
                        float outY = Mathf.Lerp(12f, 8f, gait);
                        float roll = Mathf.Lerp(0f, armZ, gait);
                        float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                        float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                        float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                        float armBreath = breath * 0.55f * idle;
                        Quaternion runL = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll);
                        Quaternion runR = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll);
                        float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                        float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                        float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                        float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                        float plant = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg));
                        _uaRT = Quaternion.Slerp(connectR, runR, settle);
                        _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-18f, 0f, 0f), _laR0 * Quaternion.Euler(elbowR, 0f, 0f), settle);
                        _uaLT = Quaternion.Slerp(connectL, runL, plant);
                        _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-22f, 0f, 0f), _laL0 * Quaternion.Euler(elbowL, 0f, 0f), plant);
                        _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spineT, plant);
                        _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hipsT, plant);
                    }
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
            else if (air)
            {
                // Rise: a long line up and out. Fall: both arms trail back, wide of the torso.
                // The tuck pitch is unchanged. Look speed is unchanged.
                // Apex hangs out to the sides so the top reads before the trail. Mild A only.
                float airW = Mathf.Clamp01(airRise + airFall);
                float riseShare = airW > 0.001f ? airRise / (airRise + airFall) : 0f;
                float lookUp = Mathf.Clamp(-_lookArmVis, 0f, 25f);
                float lookDown = Mathf.Clamp(_lookArmVis, 0f, 55f);
                float fallPitch = 72f + lookDown * 0.05f - lookUp * 0.2f;
                float fallYaw = 16f + lookDown * 0.08f;
                Quaternion upL = _uaL0 * Quaternion.Euler(-112f, 16f, armZ);
                Quaternion upR = _uaR0 * Quaternion.Euler(-112f, -16f, -armZ);
                Quaternion downL = _uaL0 * Quaternion.Euler(fallPitch, fallYaw, armZ);
                Quaternion downR = _uaR0 * Quaternion.Euler(fallPitch, -fallYaw, -armZ);
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
                if (_diveVis > 0.02f)
                {
                    // Air crouch: knees up and arms in, short of the jump tuck and the ground guard.
                    // The chest stays nose-down so it is not the apex hang. Fall speed is unchanged.
                    float d = _diveVis;
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-16f, 12f, armZ), d);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-16f, -12f, -armZ), d);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-58f, 0f, 0f), d);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-58f, 0f, 0f), d);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(46f, 0f, 0f), d);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(24f, 0f, 0f), d);
                    _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(12f, 0f, 0f), d);
                }
            }
            else
            {
                // Opposite the legs. sinC>0 puts the left thigh forward, so the right arm reaches
                // and the left arm stays back. Same-side swing reads as a skate from the chase cam.
                // Rearward travel stays short so the hands do not fold into the pelvis. No extra roll.
                // _stopGait holds the last stride while the feet close, so a brake does not pop the arms idle.
                float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
                float idle = 1f - gait;
                float amp = Mathf.Lerp(36f, 64f, gait);
                // Idle hang sits slightly forward and out. The outward yaw stays on through the
                // stride so the hands do not drop into the hips as the walk starts.
                // Roll stays 0 at rest and only picks up the mild A once the stride is moving.
                float outY = Mathf.Lerp(12f, 8f, gait);
                float roll = Mathf.Lerp(0f, armZ, gait);
                // The reach opposite the front knee opens a little wider. The back arm keeps the
                // shorter yaw so the hand stays out of the hip. Rearward pitch stays short.
                float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                // A turn opens both hands a little more. No extra roll, so they stay off the hips.
                float turnOut = Mathf.Abs(_turnVis) * 5f;
                yL += turnOut;
                yR += turnOut;
                // The reach follows the look. Look up lifts it. Look down stays short and out,
                // so the hand does not enter the hip. The back arm stays the plant.
                float lookUp = Mathf.Clamp(-_lookArmVis, 0f, 25f);
                float lookDown = Mathf.Clamp(_lookArmVis, 0f, 55f);
                float lookAdd = lookDown * 0.1f - lookUp * 0.5f;
                float lookOut = lookDown * 0.1f;
                float reachL = Mathf.Clamp01(-sinC) * gait;
                float reachR = Mathf.Clamp01(sinC) * gait;
                yL += lookOut * reachL;
                yR += lookOut * reachR;
                // Both hands rise a little with the breath. Yaw stays out, and roll stays 0 at rest,
                // so the sway does not fold the hands into the hips.
                float armBreath = breath * 0.55f * idle;
                float pitchL = RunArmPitch(-sinC, amp) - 12f * idle + armBreath + lookAdd * reachL;
                float pitchR = RunArmPitch(sinC, amp) - 12f * idle + armBreath + lookAdd * reachR;
                // Stop and the first step. Hands stay forward and out so they do not drift into the hips.
                float stopBlend = (!stepping && !air && !sliding && !crouch) ? _stopGait : 0f;
                float startBlend = (stepping && !air)
                    ? (1f - Mathf.SmoothStep(0f, 1f, _stepIn)) * Mathf.Clamp01(walkAmt + runAmt)
                    : 0f;
                float armHold = Mathf.Clamp01(Mathf.Max(stopBlend, startBlend));
                if (armHold > 0.02f)
                {
                    pitchL = Mathf.Lerp(pitchL, Mathf.Min(pitchL, -6f), armHold);
                    pitchR = Mathf.Lerp(pitchR, Mathf.Min(pitchR, -6f), armHold);
                    yL += 5f * armHold;
                    yR += 5f * armHold;
                }
                _uaLT = _uaL0 * Quaternion.Euler(pitchL, yL, roll);
                _uaRT = _uaR0 * Quaternion.Euler(pitchR, -yR, -roll);
                // Long line on the reach. The elbow fold sits on the back arm, short of the hip.
                // The trail knee is unchanged and stays straight.
                float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                if (armHold > 0.02f)
                {
                    elbowL = Mathf.Lerp(elbowL, Mathf.Max(elbowL, -12f), armHold);
                    elbowR = Mathf.Lerp(elbowR, Mathf.Max(elbowR, -12f), armHold);
                }
                _laLT = _laL0 * Quaternion.Euler(elbowL, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(elbowR, 0f, 0f);
                if (footSki > 0.001f)
                {
                    // Short swing while the feet are still in the glide, so the arms do not skate past the hips.
                    float skateL = RunArmPitch(-sinC, 16f);
                    float skateR = RunArmPitch(sinC, 16f);
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ), footSki);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ), footSki);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-12f, 0f, 0f), footSki);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-12f, 0f, 0f), footSki);
                }
                if (_dropVis > 0.02f)
                {
                    // The stride drops into the wedge or the guard. A slide stand-up
                    // brings the arms back with the feet, under the hips.
                    float d = footDrop;
                    if (_dropSlide)
                    {
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-70f, 28f, armZ), d);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-64f, -28f, -armZ), d);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-8f, 0f, 0f), d);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-6f, 0f, 0f), d);
                    }
                    else
                    {
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), d);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), d);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-72f, 0f, 0f), d);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-72f, 0f, 0f), d);
                    }
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
                if (_airDashArms && !airDashing && !lunging)
                {
                    // The burst is over. The same lead foot reaches into the stride
                    // under the hips. Collapsing the split reads as a skate.
                    float stride = Mathf.Lerp(0.96f, 1.16f, _runVis);
                    float reach = Mathf.Lerp(34f, 58f, Mathf.Clamp01(Mathf.Max(walkAmt, runAmt))) * stride;
                    float kneeAmt = Mathf.Lerp(48f, 90f, _runVis);
                    float w = 1f - Mathf.Clamp01(dashStretchPose);
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(reach, 0f, 0f), w);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-reach * 0.58f, 0f, 0f), w);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(2f + kneeAmt), 0f, 0f), w);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-2f, 0f, 0f), w);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0, w);
                }
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
                // The leg opposite the reaching hand steps up. Same phase as the hands, so the grab does not pop.
                float climbPhase = Mathf.Sin(_surfPhase);
                float up = Mathf.Lerp(0.8f, (climbPhase + 1f) * 0.5f, _surfIn);
                float kneePhase = climbPhase * _surfIn;
                _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(62f, 14f, up), 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(14f, 62f, up), 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-(6f + Mathf.Max(0f, -kneePhase) * 72f), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-(6f + Mathf.Max(0f, kneePhase) * 72f), 0f, 0f);
            }
            else if (wallRun)
            {
                // Outer leg steps with the same phase as the wall hand.
                bool left = _motor != null && _motor.WallLeft;
                float wallPhase = Mathf.Lerp(0f, Mathf.Sin(_surfPhase), _surfIn);
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
                if (_diveVis > 0.02f)
                {
                    // Knees come up enough to read as a crouch. Still short of the jump tuck and the ground guard.
                    float d = _diveVis;
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(34f, 0f, 0f), d);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(34f, 0f, 0f), d);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-50f, 0f, 0f), d);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-50f, 0f, 0f), d);
                }
                if (_pushOff > 0.02f && _diveVis < 0.2f)
                {
                    // The planted foot pushes. The other knee comes up, then the tuck.
                    float p = _pushOff;
                    if (_pushLeft)
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(-8f, 0f, 0f), p);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-6f, 0f, 0f), p);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(48f, 0f, 0f), p);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-62f, 0f, 0f), p);
                    }
                    else
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-8f, 0f, 0f), p);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-6f, 0f, 0f), p);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(48f, 0f, 0f), p);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-62f, 0f, 0f), p);
                    }
                }
            }
            else
            {
                // Recovery leg takes the knee. The back thigh stays shorter than the front reach
                // so the pair does not meet straight under the hips. Stance knee stays nearly straight.
                float stride = Mathf.Lerp(0.96f, 1.16f, _runVis);
                float reach = Mathf.Lerp(34f, 58f, Mathf.Max(Mathf.Max(walkAmt, runAmt), _stopGait)) * stride;
                float frontL = Mathf.Max(0f, sinC);
                float frontR = Mathf.Max(0f, -sinC);
                float thighL = (frontL - frontR * 0.58f) * reach;
                float thighR = (frontR - frontL * 0.58f) * reach;
                _ulLT = _ulL0 * Quaternion.Euler(thighL, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(thighR, 0f, 0f);
                float kneeAmt = Mathf.Lerp(48f, 90f, _runVis);
                float kneeL = -(2f + frontL * kneeAmt);
                float kneeR = -(2f + frontR * kneeAmt);
                _llLT = _llL0 * Quaternion.Euler(kneeL, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(kneeR, 0f, 0f);
                // First step pushes off the foot that stays down. The other leg reaches into the stride.
                float plantW = stepping ? 1f - Mathf.SmoothStep(0f, 1f, _stepIn) : 0f;
                if (plantW > 0.04f && footSki < 0.35f)
                {
                    if (Mathf.Cos(_cycle) >= 0f)
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-6f, 0f, 0f), plantW);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-4f, 0f, 0f), plantW);
                    }
                    else
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(-6f, 0f, 0f), plantW);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-4f, 0f, 0f), plantW);
                    }
                }
                if (stopping && _stopPlant > 0.02f && footSki < 0.35f && _dropVis < 0.35f)
                {
                    // Last foot under the hip before the idle sway. The other foot finishes the close.
                    float p = _stopPlant;
                    if (_stopPlantLeft)
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(-4f, 0f, 0f), p);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-4f, 0f, 0f), p);
                    }
                    else
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-4f, 0f, 0f), p);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-4f, 0f, 0f), p);
                    }
                }
                if (footSki > 0.001f)
                {
                    // Short glide under the hips. The run stride opens as the hips level.
                    float sFrontL = Mathf.Max(0f, sinC);
                    float sFrontR = Mathf.Max(0f, -sinC);
                    float sThighL = (sFrontL - sFrontR * 0.5f) * 32f;
                    float sThighR = (sFrontR - sFrontL * 0.5f) * 32f;
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(sThighL, 0f, 0f), footSki);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(sThighR, 0f, 0f), footSki);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(6f + sFrontL * 32f), 0f, 0f), footSki);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(6f + sFrontR * 32f), 0f, 0f), footSki);
                }
                if (_dropVis > 0.02f)
                {
                    // The stride drops into the pose. On a stand-up the feet come back
                    // under the hips while the chest is still low. The lead foot stays forward.
                    float d = footDrop;
                    if (_dropSlide)
                    {
                        bool leadLeft = sinC >= 0f;
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), d);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), d);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), d);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), d);
                    }
                    else if (crouch && speed > 0.35f)
                    {
                        // Short steps under the hips. Both knees stay bent, so it is not a run or a skate.
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), d);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), d);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), d);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), d);
                    }
                    else
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(56f, 0f, 0f), d);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(56f, 0f, 0f), d);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-68f, 0f, 0f), d);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-68f, 0f, 0f), d);
                    }
                }
                if (Mathf.Abs(_turnVis) > 0.18f && footSki < 0.35f)
                {
                    // Outside foot plants. Positive turn is to the right, so the left foot stays down.
                    float w = Mathf.Clamp01((Mathf.Abs(_turnVis) - 0.15f) / 0.55f);
                    if (_turnVis > 0f)
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(6f, 0f, 0f), w);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-4f, 0f, 0f), w);
                    }
                    else
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(6f, 0f, 0f), w);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-4f, 0f, 0f), w);
                    }
                }
            }

            if (_dropVis > 0.02f && !air && !dashing && !lunging && !jet && !wallRun && !climb && !mantle && !punching)
            {
                // Chest and hips follow the drop, then rise back into the stride.
                // A slide stand-up eases the hips so they do not pop flat.
                float d = _dropSlide ? hipDrop : _dropVis;
                float chest = _dropSlide ? 62f : 10f;
                float hip = _dropSlide ? 50f : 22f;
                float head = _dropSlide ? -12f : -6f;
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(chest, 0f, 0f), d);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(hip, 0f, 0f), d);
                _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(head, 0f, 0f), d);
            }

            if (wallRun || climb)
            {
                _wallExit = 1f;
                if (climb)
                {
                    float up = Mathf.Lerp(0.8f, (Mathf.Sin(_surfPhase) + 1f) * 0.5f, _surfIn);
                    _exitLeadLeft = up < 0.5f;
                }
                else
                    _exitLeadLeft = _motor == null || !_motor.WallLeft;
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
                // Hands keep the full exit. Hips and feet ease into the stride
                // so the wall roll does not pop. Exit time is unchanged.
                _wallExit = Mathf.MoveTowards(_wallExit, 0f, dt / 0.18f);
                float w = _wallExit;
                float body = Mathf.SmoothStep(0f, 1f, w);
                _uaLT = Quaternion.Slerp(_uaLT, _exitUaL, w);
                _uaRT = Quaternion.Slerp(_uaRT, _exitUaR, w);
                _laLT = Quaternion.Slerp(_laLT, _exitLaL, w);
                _laRT = Quaternion.Slerp(_laRT, _exitLaR, w);
                _ulLT = Quaternion.Slerp(_ulLT, _exitUlL, body);
                _ulRT = Quaternion.Slerp(_ulRT, _exitUlR, body);
                _llLT = Quaternion.Slerp(_llLT, _exitLlL, body);
                _llRT = Quaternion.Slerp(_llRT, _exitLlR, body);
                _spineT = Quaternion.Slerp(_spineT, _exitSpine, body);
                _hipsT = Quaternion.Slerp(_hipsT, _exitHips, body);
            }

            if (_landSquash > 0.08f && grounded && !sliding && !dashing)
            {
                // A short hop bends the knees and stays in the stride. The arms-out flare
                // is for a hard landing. A stand eases both knees into the idle breath,
                // and the arms leave the flare so they do not lock. Hold time is unchanged.
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_landSquash));
                float hard = Mathf.SmoothStep(0f, 1f, _landHard);
                float moving = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                float standing = 1f - moving;
                float kneeBase = Mathf.Lerp(k * 0.62f, k, hard);
                float kRelease = Mathf.Lerp(kneeBase, kneeBase * kneeBase, moving);
                float kneeStand = kneeBase * kneeBase;
                float kL = sinC >= 0f ? Mathf.Lerp(kneeBase, kneeStand, standing) : kRelease;
                float kR = sinC >= 0f ? kRelease : Mathf.Lerp(kneeBase, kneeStand, standing);
                float armK = Mathf.Lerp(kRelease, kneeStand * kneeStand, standing) * hard;
                float hipMove = Mathf.Lerp(kneeBase * 0.2f, kRelease, hard);
                float hipK = Mathf.Lerp(hipMove, hipMove * hipMove, standing);
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(48f, 0f, 0f), kL);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(40f, 0f, 0f), kR);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-78f, 0f, 0f), kL);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-70f, 0f, 0f), kR);
                if (_diveVis > 0.02f)
                {
                    // The air crouch eases into the landing. The flare comes in as the dart leaves,
                    // so a soft hop still does not flare and the arms do not pop.
                    float hand = _diveVis;
                    float flareIn = armK * (1f - hand);
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-16f, 12f, armZ), hand);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-16f, -12f, -armZ), hand);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-58f, 0f, 0f), hand);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-58f, 0f, 0f), hand);
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-40f, 18f, armZ), flareIn);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-40f, -18f, -armZ), flareIn);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-22f, 0f, 0f), flareIn);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-22f, 0f, 0f), flareIn);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(46f, 0f, 0f), hand);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(24f, 0f, 0f), hand);
                    _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(12f, 0f, 0f), hand);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(26f, 0f, 0f), hipK * (1f - hand));
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(18f, 0f, 0f), hipK * (1f - hand));
                }
                else
                {
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-40f, 18f, armZ), armK);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-40f, -18f, -armZ), armK);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-22f, 0f, 0f), armK);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-22f, 0f, 0f), armK);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(18f, 0f, 0f), hipK);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(26f, 0f, 0f), hipK);
                }
            }

            bool pulling = _grapple != null && _grapple.IsPulling;
            _grapplePose = Mathf.MoveTowards(_grapplePose, pulling ? 1f : 0f, dt / 0.12f);
            if (_grapplePose > 0.04f && !punching)
            {
                // Experimental rope only. Both arms reach as a long line. The chest and the
                // hips settle together, with no yaw, so the line does not twist.
                // Legs stay long so it is not a jump tuck. The gate stays off unless
                // the component is added and enableGrapple is turned on.
                float g = Mathf.SmoothStep(0f, 1f, _grapplePose);
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-96f, 16f, armZ), g);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-96f, -16f, -armZ), g);
                _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-14f, 0f, 0f), g);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-14f, 0f, 0f), g);
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(8f, 0f, 0f), g);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(6f, 0f, 0f), g);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-8f, 0f, 0f), g);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-8f, 0f, 0f), g);
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(-12f, 0f, 0f), g);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(6f, 0f, 0f), g);
            }

            if (flinchAmt > 0f)
            {
                // Tagged runner: a long V in front of the chest. Both knees bend at the hit, so it
                // stays distinct from the new It's one-knee claim. While running, the hands, the
                // chest, and the hips leave together. Standing, they ease into the idle breath
                // so they do not freeze and then pop. Flinch time is unchanged. Mild A only.
                float f = flinchAmt;
                float moving = grounded ? Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)) : 0f;
                float standing = grounded ? 1f - moving : 0f;
                float fRelease = Mathf.Lerp(f, f * f, moving);
                float fHands = Mathf.Lerp(fRelease, f * f, standing);
                float fL = sinC >= 0f ? f : fRelease;
                float fR = sinC >= 0f ? fRelease : f;
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-78f, 22f, armZ), fHands);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-78f, -22f, -armZ), fHands);
                _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-16f, 0f, 0f), fHands);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-16f, 0f, 0f), fHands);
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(22f, 0f, 0f), fL);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(22f, 0f, 0f), fR);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-48f, 0f, 0f), fL);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-48f, 0f, 0f), fR);
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(22f, 0f, 0f), fHands);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(8f, 0f, 0f), fHands);
            }
            if (claimAmt > 0f)
            {
                // New It: one arm up, the other out, chest open. Not the tagged runner's matching V.
                // While moving, the hands and the chest ease into the stride. The raised knee
                // stays on the claim so it does not freeze and then pop. Claim time is unchanged.
                // During HitRecover the punch block eases the fist into that claim. Applying it
                // again here would snap the connect away. The knee still comes up immediately.
                float c = claimAmt;
                float moving = grounded ? Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)) : 0f;
                float cRelease = Mathf.Lerp(c, c * c, moving);
                bool punchHandoff = punching && phase == PunchPhase.HitRecover;
                if (!punchHandoff)
                {
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-128f, 8f, armZ), cRelease);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), cRelease);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-10f, 0f, 0f), cRelease);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-12f, 0f, 0f), cRelease);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(-22f, -16f, 0f), cRelease);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(4f, 0f, 0f), cRelease);
                }
                _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(10f, 0f, 0f), c);
                _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(52f, 0f, 0f), c);
                _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-6f, 0f, 0f), c);
                _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-64f, 0f, 0f), c);
            }

            float slew = bouncing || gliding || jet || punching || lunging || dashing || mantle || wallRun || climb || sliding || flinchAmt > 0.04f || claimAmt > 0.04f ? 42f : crouch ? 24f : air ? 18f : 20f;
            // 0.1s air dash never reached the whip pose at slew 42.
            bool punchWind = punching && phase == PunchPhase.Windup;
            bool handoff = flinchAmt > 0.2f || claimAmt > 0.2f;
            bool grappleTell = _grapplePose > 0.04f && !punching;
            // A hop is short. Slew 18 never reached the tuck or the trail before the landing.
            bool apexHang = air && airRise < 0.2f && airFall < 0.2f;
            bool airDive = _diveVis > 0.12f;
            bool airTell = air && (airRise > 0.12f || airFall > 0.12f || apexHang || airDive);
            float armSlewL = airDashing ? 78f : punchWind ? 90f : handoff ? 72f : grappleTell ? 36f : airTell ? 64f : (punching || lunging || dashing ? 42f : slew);
            float armSlewR = airDashing ? 78f : punchWind ? 90f : handoff ? 72f : grappleTell ? 36f : airTell ? 64f : (punching || lunging || dashing ? 46f : slew);
            // Run knees have to arrive inside one stride or the flex never shows.
            bool runCycle = grounded && !air && !sliding && !crouch && !dashing && !lunging && speed > 2f;
            // Buckle has to arrive during the short absorb, then follow the ease back into the stride.
            float legSlew = airDashing ? 78f : grappleTell ? 36f : airTell ? 64f : (_landSquash > 0.05f ? 46f : runCycle ? 44f : slew);
            float torsoSlew = airDashing ? 78f : grappleTell ? 36f : airTell ? 64f : slew;
            if (!(lunging || dashing))
                _airDashArms = false;
            if (!dashing && !lunging && _armRecover > 0f)
                _armRecover = Mathf.MoveTowards(_armRecover, 0f, dt);
            // After the burst, ease the arms into the fall or the run. Slew 64 snaps them into a second throw.
            // The legs still take the stride once the feet are on the ground.
            if (_armRecover > 0f && !airDashing && !dashing && !lunging && !punchWind && !handoff && !grappleTell)
            {
                armSlewL = 16f;
                armSlewR = 16f;
                torsoSlew = 16f;
                legSlew = grounded ? 44f : 16f;
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
            // Keep the last bounce while the feet close. Cutting it with speed freezes the hips, then the idle sway pops.
            float bobGait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
            float bob = grounded ? step * 0.085f * bobGait : air ? step * 0.02f : 0f;
            if (_dropVis > 0.02f && !air && !jet)
                bob = Mathf.Lerp(bob, _dropSlide ? -0.32f : -0.14f, _dropSlide ? hipDrop : _dropVis);
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
