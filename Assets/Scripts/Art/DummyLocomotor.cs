using Tag.Gameplay;
using Tag.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Readable third-person dummy motion. Drives Dummy_Runner / Dummy_It / HiPoly bones
    /// (UpperArm_L/R, UpperLeg_L/R, ...) or primitive limbs of the same names.
    /// No AnimationClips required — shippable playtest tells.
    /// </summary>
    public class DummyLocomotor : MonoBehaviour
    {
        PlayerMotor _motor;
        PunchHitbox _punch;
        CharacterController _cc;

        Transform _hips, _spine, _head;
        Transform _upperArmL, _upperArmR, _lowerArmL, _lowerArmR;
        Transform _upperLegL, _upperLegR, _lowerLegL, _lowerLegR;
        Quaternion _hips0, _spine0, _head0;
        Quaternion _uaL0, _uaR0, _laL0, _laR0;
        Quaternion _ulL0, _ulR0, _llL0, _llR0;
        Vector3 _root0;
        bool _bound;
        float _cycle;
        float _landSquash;
        bool _wasGrounded = true;

        // Smoothed pose targets
        Quaternion _spineT, _hipsT, _headT;
        Quaternion _uaLT, _uaRT, _laLT, _laRT;
        Quaternion _ulLT, _ulRT, _llLT, _llRT;

        public void Bind(Transform visualRoot, PlayerMotor motor, PunchHitbox punch, CharacterController cc)
        {
            _motor = motor;
            _punch = punch;
            _cc = cc;
            _root0 = transform.localPosition;
            Cache(visualRoot);
        }

        void LateUpdate()
        {
            if (!_bound) Cache(transform);
            if (!_bound) return;

            float dt = Time.deltaTime;
            float speed = HorizontalSpeed();
            bool grounded = IsGrounded();
            bool sliding = _motor != null && _motor.IsSliding;
            bool airDash = _motor != null && _motor.IsAirDodgeLocked;
            bool wallRun = _motor != null && _motor.IsWallRunning;
            bool vault = _motor != null && _motor.IsVaulting;
            bool air = !grounded;
            bool punching = _punch != null && _punch.IsPunching;
            var phase = _punch != null ? _punch.Phase : PunchPhase.Idle;

            if (grounded && !_wasGrounded)
                _landSquash = 1f;
            _wasGrounded = grounded;
            _landSquash = Mathf.MoveTowards(_landSquash, 0f, dt * 4.5f);

            float walkAmt = Mathf.Clamp01(speed / 5.5f);
            float runAmt = Mathf.InverseLerp(5.2f, 8.5f, speed);
            float cadence = Mathf.Lerp(6.4f, 10.2f, runAmt);
            if (grounded && speed > 0.35f && !sliding)
                _cycle += dt * cadence;
            else if (!airDash)
                _cycle = Mathf.MoveTowards(_cycle, Mathf.Round(_cycle), dt * 8f);

            float swing = Mathf.Sin(_cycle) * Mathf.Lerp(10f, 42f, Mathf.Max(walkAmt, runAmt));
            if (!grounded) swing *= 0.2f;
            if (sliding) swing *= 0.12f;
            if (airDash) swing = 0f;

            float breath = Mathf.Sin(Time.time * 2.1f) * 2.4f;
            float punchT = PunchWeight(phase);

            // Spine / hips / head
            float leanX = sliding ? 34f : airDash ? -18f : wallRun ? 12f : air ? 10f : breath;
            if (punching) leanX += (phase == PunchPhase.Windup ? -4f : 10f) * punchT;
            leanX += _landSquash * 8f;
            float leanZ = wallRun ? 18f : 0f;
            _spineT = _spine0 * Quaternion.Euler(leanX, 0f, leanZ);
            _hipsT = _hips0 * Quaternion.Euler(sliding ? 12f : _landSquash * 6f, 0f, -leanZ * 0.35f);
            _headT = _head0 * Quaternion.Euler(sliding ? 14f : airDash ? -8f : -breath * 0.4f, 0f, 0f);

            // Arms — run cycle
            float armZ = Mathf.Lerp(8f, 14f, runAmt);
            _uaLT = _uaL0 * Quaternion.Euler(swing, 0f, armZ);
            _uaRT = _uaR0 * Quaternion.Euler(-swing, 0f, -armZ);
            _laLT = _laL0 * Quaternion.Euler(Mathf.Max(0f, -swing) * 0.6f + 8f * walkAmt, 0f, 0f);
            _laRT = _laR0 * Quaternion.Euler(Mathf.Max(0f, swing) * 0.6f + 8f * walkAmt, 0f, 0f);

            if (airDash)
            {
                _uaLT = _uaL0 * Quaternion.Euler(55f, -25f, 20f);
                _uaRT = _uaR0 * Quaternion.Euler(55f, 25f, -20f);
                _laLT = _laL0 * Quaternion.Euler(25f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(25f, 0f, 0f);
            }
            else if (vault)
            {
                _uaLT = _uaL0 * Quaternion.Euler(-40f, 0f, 25f);
                _uaRT = _uaR0 * Quaternion.Euler(-40f, 0f, -25f);
            }

            // Punch: windup pulls back, active shoots forward (right arm)
            if (punchT > 0f)
            {
                if (phase == PunchPhase.Windup)
                {
                    _uaRT = _uaR0 * Quaternion.Euler(35f + 25f * punchT, -30f * punchT, 20f);
                    _laRT = _laR0 * Quaternion.Euler(40f * punchT, 0f, 0f);
                }
                else
                {
                    _uaRT = _uaR0 * Quaternion.Euler(-25f - 85f * punchT, 22f * punchT, -14f);
                    _laRT = _laR0 * Quaternion.Euler(8f + 30f * punchT, 0f, 0f);
                }
                // slight opposite arm brace
                _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-15f * punchT, 10f, 15f), 0.65f);
            }

            // Legs
            float legL = sliding ? 20f : airDash ? -40f : air ? -32f : -swing * 0.9f;
            float legR = sliding ? 20f : airDash ? -40f : air ? -32f : swing * 0.9f;
            _ulLT = _ulL0 * Quaternion.Euler(legL, 0f, 0f);
            _ulRT = _ulR0 * Quaternion.Euler(legR, 0f, 0f);
            float kneeL = sliding || air || airDash ? 58f : Mathf.Max(0f, swing) * 0.75f;
            float kneeR = sliding || air || airDash ? 58f : Mathf.Max(0f, -swing) * 0.75f;
            _llLT = _llL0 * Quaternion.Euler(kneeL, 0f, 0f);
            _llRT = _llR0 * Quaternion.Euler(kneeR, 0f, 0f);

            float slew = airDash || punching ? 22f : 14f;
            ApplySlew(dt, slew);

            float bob = grounded && !sliding ? Mathf.Abs(Mathf.Sin(_cycle * 2f)) * 0.035f * walkAmt : 0f;
            if (sliding) bob = -0.14f;
            if (airDash) bob = 0.02f;
            bob -= _landSquash * 0.08f;
            transform.localPosition = _root0 + new Vector3(0f, bob, 0f);
            float squashY = 1f - _landSquash * 0.08f;
            float squashXZ = 1f + _landSquash * 0.05f;
            transform.localScale = new Vector3(squashXZ, squashY, squashXZ);
        }

        void ApplySlew(float dt, float rate)
        {
            float k = 1f - Mathf.Exp(-rate * dt);
            if (_spine != null) _spine.localRotation = Quaternion.Slerp(_spine.localRotation, _spineT, k);
            if (_hips != null) _hips.localRotation = Quaternion.Slerp(_hips.localRotation, _hipsT, k);
            if (_head != null) _head.localRotation = Quaternion.Slerp(_head.localRotation, _headT, k);
            if (_upperArmL != null) _upperArmL.localRotation = Quaternion.Slerp(_upperArmL.localRotation, _uaLT, k);
            if (_upperArmR != null) _upperArmR.localRotation = Quaternion.Slerp(_upperArmR.localRotation, _uaRT, k);
            if (_lowerArmL != null) _lowerArmL.localRotation = Quaternion.Slerp(_lowerArmL.localRotation, _laLT, k);
            if (_lowerArmR != null) _lowerArmR.localRotation = Quaternion.Slerp(_lowerArmR.localRotation, _laRT, k);
            if (_upperLegL != null) _upperLegL.localRotation = Quaternion.Slerp(_upperLegL.localRotation, _ulLT, k);
            if (_upperLegR != null) _upperLegR.localRotation = Quaternion.Slerp(_upperLegR.localRotation, _ulRT, k);
            if (_lowerLegL != null) _lowerLegL.localRotation = Quaternion.Slerp(_lowerLegL.localRotation, _llLT, k);
            if (_lowerLegR != null) _lowerLegR.localRotation = Quaternion.Slerp(_lowerLegR.localRotation, _llRT, k);
        }

        float HorizontalSpeed()
        {
            if (_motor != null) return _motor.HorizontalSpeed;
            if (_cc != null)
            {
                var v = _cc.velocity;
                return new Vector3(v.x, 0f, v.z).magnitude;
            }
            return 0f;
        }

        bool IsGrounded()
        {
            if (_motor != null) return _motor.IsGrounded;
            return _cc == null || _cc.isGrounded;
        }

        static float PunchWeight(PunchPhase phase)
        {
            switch (phase)
            {
                case PunchPhase.Windup: return 0.7f;
                case PunchPhase.Active: return 1f;
                case PunchPhase.HitRecover: return 0.4f;
                case PunchPhase.MissRecover: return 0.25f;
                default: return 0f;
            }
        }

        void Cache(Transform root)
        {
            if (root == null) return;
            _hips = FindBone(root, "Hips", "hip", "Pelvis");
            _spine = FindBone(root, "Spine", "spine", "Torso", "Chest");
            _head = FindBone(root, "Head", "head");
            _upperArmL = FindBone(root, "UpperArm_L", "LeftUpperArm", "upperarm_l", "Shoulder_L");
            _upperArmR = FindBone(root, "UpperArm_R", "RightUpperArm", "upperarm_r", "Shoulder_R");
            _lowerArmL = FindBone(root, "LowerArm_L", "LeftLowerArm", "lowerarm_l");
            _lowerArmR = FindBone(root, "LowerArm_R", "RightLowerArm", "lowerarm_r");
            _upperLegL = FindBone(root, "UpperLeg_L", "LeftUpperLeg", "upperleg_l");
            _upperLegR = FindBone(root, "UpperLeg_R", "RightUpperLeg", "upperleg_r");
            _lowerLegL = FindBone(root, "LowerLeg_L", "LeftLowerLeg", "lowerleg_l");
            _lowerLegR = FindBone(root, "LowerLeg_R", "RightLowerLeg", "lowerleg_r");

            // HiPoly procedural dummies use mesh part names — drive those if no armature bones.
            if (_upperArmL == null) _upperArmL = FindBone(root, "UpperArm_L");
            if (_upperArmL == null && _upperLegL == null)
            {
                // Fall back to any child named like limbs from DummyPrimitiveFactory / HiPoly export
                _upperArmL = FindBone(root, "Arm_L");
                _upperArmR = FindBone(root, "Arm_R");
            }

            if (_upperArmL == null && _upperLegL == null && _spine == null)
                return;

            _hips0 = Rest(_hips);
            _spine0 = Rest(_spine);
            _head0 = Rest(_head);
            _uaL0 = Rest(_upperArmL);
            _uaR0 = Rest(_upperArmR);
            _laL0 = Rest(_lowerArmL);
            _laR0 = Rest(_lowerArmR);
            _ulL0 = Rest(_upperLegL);
            _ulR0 = Rest(_upperLegR);
            _llL0 = Rest(_lowerLegL);
            _llR0 = Rest(_lowerLegR);

            _spineT = _spine0; _hipsT = _hips0; _headT = _head0;
            _uaLT = _uaL0; _uaRT = _uaR0; _laLT = _laL0; _laRT = _laR0;
            _ulLT = _ulL0; _ulRT = _ulR0; _llLT = _llL0; _llRT = _llR0;
            _bound = true;

            foreach (var a in root.GetComponentsInChildren<Animator>())
            {
                if (a.runtimeAnimatorController == null)
                    a.enabled = false;
            }
        }

        static Quaternion Rest(Transform t) => t != null ? t.localRotation : Quaternion.identity;

        static Transform FindBone(Transform root, params string[] names)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(t.name, names[i], System.StringComparison.OrdinalIgnoreCase))
                        return t;
                }
            }
            return null;
        }
    }
}