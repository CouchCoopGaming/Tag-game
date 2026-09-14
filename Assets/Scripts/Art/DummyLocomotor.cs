using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Procedural parkour body driven by TagArena MoveState (Apex×Tribes).
    /// No AnimationClips required — readable limb tells for third-person views.
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
        bool _wasGrounded = true;

        Quaternion _spineT, _hipsT, _headT;
        Quaternion _uaLT, _uaRT, _laLT, _laRT;
        Quaternion _ulLT, _ulRT, _llLT, _llRT;

        public void Bind(Transform visualRoot, PlayerMotor motor, PunchHitbox punch, CharacterController ccIgnored = null)
        {
            _motor = motor;
            _punch = punch;
            _root0 = transform.localPosition;
            Cache(visualRoot);
            if (!_bound && !_loggedBindFail)
            {
                _loggedBindFail = true;
                Debug.LogWarning($"[DummyLocomotor] Bone bind failed on '{(visualRoot != null ? visualRoot.name : "null")}' — no hierarchical UpperArm/UpperLeg.");
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
            if (!_bound) Cache(transform);
            if (!_bound) return;
            if (_motor == null) _motor = GetComponentInParent<PlayerMotor>();
            if (_punch == null) _punch = GetComponentInParent<PunchHitbox>();

            float dt = Time.deltaTime;
            float speed = _motor != null ? _motor.HorizontalSpeed : 0f;
            bool grounded = _motor == null || _motor.IsGrounded;
            var st = _motor != null ? _motor.State : MoveState.Idle;
            bool sliding = st == MoveState.Slide;
            bool skiing = st == MoveState.Ski || (_motor != null && _motor.Skiing);
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
                _landSquash = Mathf.Clamp(Mathf.Lerp(0.2f, 1.25f, t * t), 0.2f, 1.25f);
            }
            _wasGrounded = grounded;
            float recover = Mathf.Lerp(7.5f, 5f, Mathf.Clamp01(_landSquash));
            _landSquash = Mathf.MoveTowards(_landSquash, 0f, dt * recover);

            float walkAmt = Mathf.Clamp01(speed / 5.5f);
            float runAmt = Mathf.InverseLerp(5.2f, 9.5f, speed);
            float cadence = Mathf.Lerp(6.8f, 12.2f, runAmt);
            if (skiing) cadence = Mathf.Lerp(8f, 14f, runAmt);
            // Keep a soft air/vault cycle so limbs stay energetic off the ground
            if (grounded && speed > 0.35f && !sliding && !crouch)
                _cycle += dt * cadence;
            else if (air && !jet)
                _cycle += dt * Mathf.Lerp(5.5f, 9f, runAmt);
            else if (!jet)
                _cycle = Mathf.MoveTowards(_cycle, Mathf.Round(_cycle), dt * 8f);

            // Louder third-person limb cycles (walk/run must read clearly)
            float swing = Mathf.Sin(_cycle) * Mathf.Lerp(30f, 82f, Mathf.Max(walkAmt, runAmt));
            if (skiing) swing *= 0.35f;
            if (air) swing *= 0.72f; // retain vault/run energy instead of nearly freezing
            if (sliding) swing *= 0.08f; else if (crouch) swing *= 0.18f;
            if (jet) swing = 0f;

            float breath = Mathf.Sin(Time.time * 2.1f) * 2.4f;
            float punchProg = _punch != null ? _punch.PhaseProgress : 0f;

            // Spine / hips lean by state — jet/ski read clearly in TP (jet wins over ski tuck)
            float leanX = lunging ? Mathf.Lerp(22f, 36f, _motor != null ? _motor.LungeProgress : 1f) : sliding ? 48f : crouch ? 28f : jet ? -18f : skiing ? 16f : wallRun ? 22f : climb ? -8f : mantle ? 34f : air ? 18f : breath;
            float leanZ = wallRun ? (_motor != null && _motor.WallLeft ? 32f : -32f) : skiing && !jet ? Mathf.Sin(_cycle * 0.5f) * 14f : 0f;
            _spineT = _spine0 * Quaternion.Euler(leanX, 0f, leanZ);
            _hipsT = _hips0 * Quaternion.Euler(lunging ? 16f : sliding ? 28f : crouch ? 14f : jet ? -8f : skiing ? 10f : air ? 8f : 0f, 0f, -leanZ * 0.55f);
            _headT = _head0 * Quaternion.Euler(lunging ? 14f : sliding ? 18f : crouch ? 6f : jet ? -6f : skiing ? 10f : air ? -6f : -breath * 0.4f, 0f, 0f);

            // Arms
            float armZ = Mathf.Lerp(14f, 30f, runAmt);
            float lungeAmt = lunging && _motor != null ? _motor.LungeProgress : 0f;
            if (lunging)
            {
                // MMB dash tell wins over jet: hard whip early, settle late
                float snap = Mathf.Lerp(0.45f, 1f, lungeAmt);
                _uaLT = _uaL0 * Quaternion.Euler(62f * snap, -14f, armZ + 22f);
                _uaRT = _uaR0 * Quaternion.Euler(62f * snap, 14f, -armZ - 22f);
                _laLT = _laL0 * Quaternion.Euler(-28f - 18f * snap, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-28f - 18f * snap, 0f, 0f);
            }
            else if (jet)
            {
                // Pack tell: arms out, off-hand further back
                _uaLT = _uaL0 * Quaternion.Euler(24f, 10f, 52f);
                _uaRT = _uaR0 * Quaternion.Euler(62f, -12f, -46f);
                _laLT = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-32f, 0f, 0f);
            }
            else if (climb)
            {
                float climbSwing = Mathf.Sin(Time.time * 8f) * 40f;
                _uaLT = _uaL0 * Quaternion.Euler(-120f + climbSwing, 10f, 20f);
                _uaRT = _uaR0 * Quaternion.Euler(-120f - climbSwing, -10f, -20f);
                _laLT = _laL0 * Quaternion.Euler(-40f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-40f, 0f, 0f);
            }
            else if (mantle)
            {
                // Pull-up tell: both arms reach high then settle
                float pull = Mathf.Sin(Time.time * 11f) * 10f;
                _uaLT = _uaL0 * Quaternion.Euler(-145f + pull, 18f, 28f);
                _uaRT = _uaR0 * Quaternion.Euler(-145f - pull, -18f, -28f);
                _laLT = _laL0 * Quaternion.Euler(-55f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-55f, 0f, 0f);
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
            else if (punching)
            {
                // Guard left; right arm windup cock -> hard extend -> recover
                _uaLT = _uaL0 * Quaternion.Euler(-18f, 8f, armZ + 10f);
                _laLT = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                if (phase == PunchPhase.Windup)
                {
                    float w = Mathf.Lerp(0.25f, 1f, punchProg);
                    _uaRT = _uaR0 * Quaternion.Euler(42f + 48f * w, -32f * w, 16f);
                    _laRT = _laR0 * Quaternion.Euler(-70f * w, 0f, 0f);
                }
                else if (phase == PunchPhase.Active)
                {
                    float e = Mathf.Lerp(0.75f, 1f, punchProg);
                    _uaRT = _uaR0 * Quaternion.Euler(-40f - 115f * e, 34f * e, -22f);
                    _laRT = _laR0 * Quaternion.Euler(-55f * e, 0f, 0f);
                }
                else // HitRecover / MissRecover
                {
                    float r = Mathf.Lerp(1f, 0.2f, punchProg);
                    _uaRT = _uaR0 * Quaternion.Euler(-30f - 70f * r, 18f * r, -12f);
                    _laRT = _laR0 * Quaternion.Euler(-28f * r, 0f, 0f);
                }
            }
            else if (sliding)
            {
                // Compact dive silhouette: lead arm plants low, trail braces back
                _uaLT = _uaL0 * Quaternion.Euler(52f, -18f, armZ + 22f);
                _uaRT = _uaR0 * Quaternion.Euler(-28f, 22f, -armZ - 8f);
                _laLT = _laL0 * Quaternion.Euler(-58f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
            }
            else if (crouch)
            {
                // Low guard squat - quieter than slide, still readable in TP
                _uaLT = _uaL0 * Quaternion.Euler(22f, -6f, armZ + 8f);
                _uaRT = _uaR0 * Quaternion.Euler(22f, 6f, -armZ - 8f);
                _laLT = _laL0 * Quaternion.Euler(-28f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-28f, 0f, 0f);
            }
            else if (skiing)
            {
                // Quiet tuck — not a walk cycle; carve lives in hips/legs
                _uaLT = _uaL0 * Quaternion.Euler(32f, -10f, 16f);
                _uaRT = _uaR0 * Quaternion.Euler(32f, 10f, -16f);
                _laLT = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
            }
            else if (air)
            {
                // Air / vault limb tells: residual run energy + open arms (slight loft for crest leave)
                float airKick = Mathf.Sin(_cycle) * Mathf.Lerp(28f, 48f, runAmt);
                _uaLT = _uaL0 * Quaternion.Euler(-32f - airKick * 0.55f, 0f, armZ + 14f);
                _uaRT = _uaR0 * Quaternion.Euler(-32f + airKick * 0.55f, 0f, -armZ - 14f);
                _laLT = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
            }
            else
            {
                _uaLT = _uaL0 * Quaternion.Euler(-swing, 0f, armZ);
                _uaRT = _uaR0 * Quaternion.Euler(swing, 0f, -armZ);
                _laLT = _laL0 * Quaternion.Euler(Mathf.Min(0f, -swing * 0.55f), 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(Mathf.Min(0f, swing * 0.55f), 0f, 0f);
            }

            // Legs
            if (lunging)
            {
                float stride = Mathf.Lerp(0.55f, 1f, lungeAmt);
                _ulLT = _ulL0 * Quaternion.Euler(52f * stride, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(-22f * stride, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-44f * stride, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-14f * stride, 0f, 0f);
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
                // Knees slightly extended — hover, not a tuck
                float hover = Mathf.Sin(Time.time * 6.5f) * 5f;
                _ulLT = _ulL0 * Quaternion.Euler(14f + hover, 0f, 8f);
                _ulRT = _ulR0 * Quaternion.Euler(12f - hover, 0f, -8f);
                _llLT = _llL0 * Quaternion.Euler(-10f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-10f, 0f, 0f);
            }
            else if (skiing)
            {
                // Knees flexed stance + carve roll — ski, not a slide compact
                float carve = Mathf.Sin(_cycle * 0.5f);
                _ulLT = _ulL0 * Quaternion.Euler(40f + swing * 0.22f, 0f, 12f + carve * 8f);
                _ulRT = _ulR0 * Quaternion.Euler(40f - swing * 0.22f, 0f, -12f - carve * 8f);
                _llLT = _llL0 * Quaternion.Euler(-34f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-34f, 0f, 0f);
            }
            else if (mantle)
            {
                // Tucked then extend — readable vault silhouette
                _ulLT = _ulL0 * Quaternion.Euler(62f, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-75f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-58f, 0f, 0f);
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
            else if (air)
            {
                float airKick = Mathf.Sin(_cycle) * Mathf.Lerp(32f, 55f, runAmt);
                _ulLT = _ulL0 * Quaternion.Euler(22f + airKick, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(18f - airKick, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-35f - Mathf.Abs(airKick) * 0.35f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-28f - Mathf.Abs(airKick) * 0.25f, 0f, 0f);
            }
            else
            {
                _ulLT = _ulL0 * Quaternion.Euler(swing * 1.35f, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(-swing * 1.35f, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(Mathf.Min(0f, -Mathf.Abs(swing) * 0.85f), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(Mathf.Min(0f, -Mathf.Abs(swing) * 0.85f), 0f, 0f);
            }

            float slew = jet || punching || lunging || mantle || wallRun || sliding ? 34f : skiing || crouch ? 24f : air ? 18f : 16f;
            Slew(ref _spine, _spineT, slew, dt);
            Slew(ref _hips, _hipsT, slew, dt);
            Slew(ref _head, _headT, slew, dt);
            Slew(ref _upperArmL, _uaLT, punching || lunging ? 36f : slew, dt);
            Slew(ref _upperArmR, _uaRT, punching || lunging ? 40f : slew, dt);
            Slew(ref _lowerArmL, _laLT, punching || lunging ? 36f : slew, dt);
            Slew(ref _lowerArmR, _laRT, punching || lunging ? 40f : slew, dt);
            Slew(ref _upperLegL, _ulLT, slew, dt);
            Slew(ref _upperLegR, _ulRT, slew, dt);
            Slew(ref _lowerLegL, _llLT, slew, dt);
            Slew(ref _lowerLegR, _llRT, slew, dt);

            float bob = grounded ? Mathf.Abs(Mathf.Sin(_cycle)) * 0.055f * walkAmt : air ? Mathf.Abs(Mathf.Sin(_cycle)) * 0.02f : 0f;
            if (sliding) bob = -0.22f; else if (crouch) bob = -0.14f;
            else if (jet) bob = 0.05f + Mathf.Sin(Time.time * 6.5f) * 0.02f;
            else if (skiing) bob = -0.10f;
            if (_landSquash > 0f) bob -= 0.12f * _landSquash;
            transform.localPosition = _root0 + new Vector3(0f, bob, 0f);
            float squash = 1f - 0.12f * _landSquash;
            transform.localScale = new Vector3(1f / squash, squash, 1f / squash);
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
            _upperArmL = FindBone(root, "UpperArm_L", "UpperArm.L", "LeftArm", "LeftUpperArm", "mixamorig:LeftArm", "Arm_L", "upperarm_l");
            _upperArmR = FindBone(root, "UpperArm_R", "UpperArm.R", "RightArm", "RightUpperArm", "mixamorig:RightArm", "Arm_R", "upperarm_r");
            _lowerArmL = FindBone(root, "LowerArm_L", "LowerArm.L", "LeftForeArm", "LeftLowerArm", "mixamorig:LeftForeArm", "ForeArm_L", "lowerarm_l");
            _lowerArmR = FindBone(root, "LowerArm_R", "LowerArm.R", "RightForeArm", "RightLowerArm", "mixamorig:RightForeArm", "ForeArm_R", "lowerarm_r");
            _upperLegL = FindBone(root, "UpperLeg_L", "UpperLeg.L", "LeftUpLeg", "LeftUpperLeg", "mixamorig:LeftUpLeg", "Thigh_L", "upperleg_l");
            _upperLegR = FindBone(root, "UpperLeg_R", "UpperLeg.R", "RightUpLeg", "RightUpperLeg", "mixamorig:RightUpLeg", "Thigh_R", "upperleg_r");
            _lowerLegL = FindBone(root, "LowerLeg_L", "LowerLeg.L", "LeftLeg", "LeftLowerLeg", "mixamorig:LeftLeg", "Calf_L", "lowerleg_l");
            _lowerLegR = FindBone(root, "LowerLeg_R", "LowerLeg.R", "RightLeg", "RightLowerLeg", "mixamorig:RightLeg", "Calf_R", "lowerleg_r");
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
