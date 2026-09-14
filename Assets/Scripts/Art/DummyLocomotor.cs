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
            var phase = _punch != null ? _punch.Phase : PunchPhase.Idle;

            if (grounded && !_wasGrounded) _landSquash = 1f;
            _wasGrounded = grounded;
            _landSquash = Mathf.MoveTowards(_landSquash, 0f, dt * 4.5f);

            float walkAmt = Mathf.Clamp01(speed / 5.5f);
            float runAmt = Mathf.InverseLerp(5.2f, 9.5f, speed);
            float cadence = Mathf.Lerp(6.4f, 11.5f, runAmt);
            if (skiing) cadence = Mathf.Lerp(8f, 14f, runAmt);
            if (grounded && speed > 0.35f && !sliding && !crouch)
                _cycle += dt * cadence;
            else if (!jet)
                _cycle = Mathf.MoveTowards(_cycle, Mathf.Round(_cycle), dt * 8f);

            float swing = Mathf.Sin(_cycle) * Mathf.Lerp(18f, 58f, Mathf.Max(walkAmt, runAmt));
            if (skiing) swing *= 0.35f;
            if (!grounded) swing *= 0.25f;
            if (sliding || crouch) swing *= 0.12f;
            if (jet) swing = 0f;

            float breath = Mathf.Sin(Time.time * 2.1f) * 2.4f;
            float punchT = PunchWeight(phase);

            // Spine / hips lean by state
            float leanX = sliding || crouch ? 38f : skiing ? 22f : jet ? -12f : wallRun ? 14f : climb ? -8f : mantle ? 25f : air ? 10f : breath;
            float leanZ = wallRun ? (_motor != null && _motor.WallLeft ? 18f : -18f) : skiing ? Mathf.Sin(_cycle * 0.5f) * 6f : 0f;
            _spineT = _spine0 * Quaternion.Euler(leanX, 0f, leanZ);
            _hipsT = _hips0 * Quaternion.Euler(sliding || crouch ? 20f : skiing ? 12f : 0f, 0f, -leanZ * 0.5f);
            _headT = _head0 * Quaternion.Euler(sliding ? 14f : jet ? 8f : -breath * 0.4f, 0f, 0f);

            // Arms
            float armZ = Mathf.Lerp(8f, 16f, runAmt);
            if (jet)
            {
                _uaLT = _uaL0 * Quaternion.Euler(40f, 0f, 35f);
                _uaRT = _uaR0 * Quaternion.Euler(40f, 0f, -35f);
                _laLT = _laL0 * Quaternion.Euler(-20f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-20f, 0f, 0f);
            }
            else if (climb)
            {
                float climbSwing = Mathf.Sin(Time.time * 8f) * 40f;
                _uaLT = _uaL0 * Quaternion.Euler(-120f + climbSwing, 10f, 20f);
                _uaRT = _uaR0 * Quaternion.Euler(-120f - climbSwing, -10f, -20f);
                _laLT = _laL0 * Quaternion.Euler(-40f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-40f, 0f, 0f);
            }
            else if (punching)
            {
                _uaLT = _uaL0 * Quaternion.Euler(-10f, 0f, armZ);
                _uaRT = _uaR0 * Quaternion.Euler(-25f - 85f * punchT, 22f * punchT, -14f);
                _laLT = _laL0;
                _laRT = _laR0 * Quaternion.Euler(-30f * punchT, 0f, 0f);
            }
            else
            {
                _uaLT = _uaL0 * Quaternion.Euler(-swing, 0f, armZ);
                _uaRT = _uaR0 * Quaternion.Euler(swing, 0f, -armZ);
                _laLT = _laL0 * Quaternion.Euler(Mathf.Min(0f, -swing * 0.4f), 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(Mathf.Min(0f, swing * 0.4f), 0f, 0f);
            }

            // Legs
            if (sliding || crouch)
            {
                _ulLT = _ulL0 * Quaternion.Euler(70f, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(55f, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-40f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-25f, 0f, 0f);
            }
            else if (skiing)
            {
                _ulLT = _ulL0 * Quaternion.Euler(25f + swing * 0.15f, 0f, 8f);
                _ulRT = _ulR0 * Quaternion.Euler(25f - swing * 0.15f, 0f, -8f);
                _llLT = _llL0 * Quaternion.Euler(-15f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-15f, 0f, 0f);
            }
            else
            {
                _ulLT = _ulL0 * Quaternion.Euler(swing * 1.1f, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(-swing * 1.1f, 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(Mathf.Min(0f, -Mathf.Abs(swing) * 0.6f), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(Mathf.Min(0f, -Mathf.Abs(swing) * 0.6f), 0f, 0f);
            }

            float slew = jet || punching || mantle ? 24f : 14f;
            Slew(ref _spine, _spineT, slew, dt);
            Slew(ref _hips, _hipsT, slew, dt);
            Slew(ref _head, _headT, slew, dt);
            Slew(ref _upperArmL, _uaLT, slew, dt);
            Slew(ref _upperArmR, _uaRT, slew, dt);
            Slew(ref _lowerArmL, _laLT, slew, dt);
            Slew(ref _lowerArmR, _laRT, slew, dt);
            Slew(ref _upperLegL, _ulLT, slew, dt);
            Slew(ref _upperLegR, _ulRT, slew, dt);
            Slew(ref _lowerLegL, _llLT, slew, dt);
            Slew(ref _lowerLegR, _llRT, slew, dt);

            float bob = grounded ? Mathf.Abs(Mathf.Sin(_cycle)) * 0.04f * walkAmt : 0f;
            if (sliding || crouch) bob = -0.18f;
            if (_landSquash > 0f) bob -= 0.08f * _landSquash;
            transform.localPosition = _root0 + new Vector3(0f, bob, 0f);
            float squash = 1f - 0.08f * _landSquash;
            transform.localScale = new Vector3(1f / squash, squash, 1f / squash);
        }

        static void Slew(ref Transform t, Quaternion target, float speed, float dt)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, target, 1f - Mathf.Exp(-speed * dt));
        }

        float PunchWeight(PunchPhase phase)
        {
            switch (phase)
            {
                case PunchPhase.Windup: return 0.35f;
                case PunchPhase.Active: return 1f;
                case PunchPhase.HitRecover:
                case PunchPhase.MissRecover: return 0.45f;
                default: return 0f;
            }
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
