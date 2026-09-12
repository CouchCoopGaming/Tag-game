using Tag.Gameplay;
using Tag.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Party-game third-person dummy tells. Drives Dummy_* / Dummy_*_Hi bones
    /// (Hips, Spine, Head, UpperArm_L/R, LowerArm_L/R, UpperLeg_L/R, LowerLeg_L/R)
    /// or primitive limbs of the same names. Punchy squash — not mocap.
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
        Vector3 _rootScale0 = Vector3.one;
        bool _bound;
        float _cycle;
        float _landSquash;
        float _jumpStretch;
        bool _wasGrounded = true;

        Quaternion _spineT, _hipsT, _headT;
        Quaternion _uaLT, _uaRT, _laLT, _laRT;
        Quaternion _ulLT, _ulRT, _llLT, _llRT;

        public void Bind(Transform visualRoot, PlayerMotor motor, PunchHitbox punch, CharacterController cc)
        {
            _motor = motor;
            _punch = punch;
            _cc = cc;
            _root0 = transform.localPosition;
            _rootScale0 = transform.localScale;
            Cache(visualRoot);
        }

        void LateUpdate()
        {
            if (!_bound) Cache(transform);
            if (!_bound) return;

            float dt = Time.deltaTime;
            float speed = HorizontalSpeed();
            float vertical = _motor != null ? _motor.VerticalSpeed : 0f;
            bool grounded = IsGrounded();
            bool sliding = _motor != null && _motor.IsSliding;
            bool airDash = _motor != null && _motor.IsAirDodgeLocked;
            bool wallRun = _motor != null && _motor.IsWallRunning;
            bool vault = _motor != null && _motor.IsVaulting;
            bool air = !grounded;
            var phase = _punch != null ? _punch.Phase : PunchPhase.Idle;
            bool punching = phase != PunchPhase.Idle;
            float punchT = _punch != null ? _punch.PhaseProgress : 0f;

            if (_wasGrounded && !grounded && vertical > 1.2f)
                _jumpStretch = 1f;
            if (!_wasGrounded && grounded)
                _landSquash = 1f;
            _wasGrounded = grounded;
            _landSquash = Mathf.MoveTowards(_landSquash, 0f, dt * 4.2f);
            _jumpStretch = Mathf.MoveTowards(_jumpStretch, 0f, dt * 5.5f);

            float walkAmt = Mathf.Clamp01(speed / 5.5f);
            float runAmt = Mathf.InverseLerp(5.2f, 8.5f, speed);
            float cadence = Mathf.Lerp(6.6f, 10.6f, runAmt);
            if (grounded && speed > 0.35f && !sliding)
                _cycle += dt * cadence;
            else if (!airDash)
                _cycle = Mathf.MoveTowards(_cycle, Mathf.Round(_cycle), dt * 8f);

            float swing = Mathf.Sin(_cycle) * Mathf.Lerp(12f, 52f, Mathf.Max(walkAmt, runAmt));
            if (!grounded) swing *= 0.18f;
            if (sliding) swing *= 0.1f;
            if (airDash) swing = 0f;

            float breath = Mathf.Sin(Time.time * 2.35f) * (walkAmt < 0.08f ? 5.2f : 2.2f);
            float bobWave = Mathf.Abs(Mathf.Sin(_cycle));

            float leanX = sliding ? 38f : airDash ? -20f : wallRun ? 12f : air ? (8f + Mathf.Clamp(-vertical, 0f, 10f) * 0.6f) : breath;
            if (punching)
                leanX += phase == PunchPhase.Windup ? -6f * punchT : 12f * punchT;
            leanX += _landSquash * 10f - _jumpStretch * 6f;
            float leanZ = wallRun ? 18f : 0f;
            float hipYaw = grounded && !sliding ? swing * 0.28f : 0f;
            if (punching)
                hipYaw += phase == PunchPhase.Windup ? -18f * punchT : 24f * punchT;

            _spineT = _spine0 * Quaternion.Euler(leanX, hipYaw * -0.45f, leanZ);
            _hipsT = _hips0 * Quaternion.Euler(sliding ? 16f : _landSquash * 8f - _jumpStretch * 4f, hipYaw, -leanZ * 0.35f);
            _headT = _head0 * Quaternion.Euler(sliding ? 16f : airDash ? -10f : -breath * 0.45f, hipYaw * 0.35f, 0f);

            float armZ = Mathf.Lerp(8f, 16f, runAmt);
            _uaLT = _uaL0 * Quaternion.Euler(swing, 0f, armZ);
            _uaRT = _uaR0 * Quaternion.Euler(-swing, 0f, -armZ);
            _laLT = _laL0 * Quaternion.Euler(Mathf.Max(0f, -swing) * 0.65f + 10f * walkAmt, 0f, 0f);
            _laRT = _laR0 * Quaternion.Euler(Mathf.Max(0f, swing) * 0.65f + 10f * walkAmt, 0f, 0f);

            if (airDash)
            {
                _uaLT = _uaL0 * Quaternion.Euler(62f, -28f, 22f);
                _uaRT = _uaR0 * Quaternion.Euler(62f, 28f, -22f);
                _laLT = _laL0 * Quaternion.Euler(28f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(28f, 0f, 0f);
            }
            else if (vault)
            {
                _uaLT = _uaL0 * Quaternion.Euler(-42f, 0f, 26f);
                _uaRT = _uaR0 * Quaternion.Euler(-42f, 0f, -26f);
            }
            else if (sliding)
            {
                _uaLT = _uaL0 * Quaternion.Euler(-8f, 0f, 44f);
                _uaRT = _uaR0 * Quaternion.Euler(-8f, 0f, -44f);
            }

            if (punching)
                ApplyPunch(phase, punchT);

            float rising = air ? Mathf.Clamp01(vertical / 8f) : 0f;
            float falling = air ? Mathf.Clamp01(-vertical / 12f) : 0f;
            float tuck = airDash ? 46f : air ? Mathf.Lerp(28f, 52f, falling) : 0f;
            float legL = sliding ? 22f : airDash ? -tuck : air ? -tuck : -swing * 0.95f;
            float legR = sliding ? 8f : airDash ? -tuck * 0.9f : air ? -tuck * 0.85f : swing * 0.95f;
            _ulLT = _ulL0 * Quaternion.Euler(legL, sliding ? 8f : 0f, 0f);
            _ulRT = _ulR0 * Quaternion.Euler(legR, sliding ? -8f : 0f, 0f);
            float kneeL = sliding ? 40f : air || airDash ? tuck + 12f : Mathf.Max(0f, swing) * 0.78f;
            float kneeR = sliding ? 18f : air || airDash ? tuck + 10f : Mathf.Max(0f, -swing) * 0.78f;
            _llLT = _llL0 * Quaternion.Euler(kneeL, 0f, 0f);
            _llRT = _llR0 * Quaternion.Euler(kneeR, 0f, 0f);

            if (air && !airDash && !punching)
            {
                _uaLT = _uaL0 * Quaternion.Euler(-22f * rising, 0f, 16f + falling * 10f);
                _uaRT = _uaR0 * Quaternion.Euler(-22f * rising, 0f, -16f - falling * 10f);
            }

            float slew = airDash ? 28f : punching ? 20f : 14f;
            ApplySlew(dt, slew);

            float bob = grounded && !sliding ? bobWave * 0.045f * walkAmt : 0f;
            if (sliding) bob = -0.16f;
            if (airDash) bob = 0.04f;
            bob -= _landSquash * 0.1f;
            bob += _jumpStretch * 0.04f;
            transform.localPosition = _root0 + new Vector3(0f, bob, 0f);

            float squashY = 1f - _landSquash * 0.12f + _jumpStretch * 0.1f;
            float squashXZ = 1f + _landSquash * 0.07f - _jumpStretch * 0.06f;
            if (walkAmt < 0.08f && grounded && !sliding)
            {
                float breathe = 1f + Mathf.Sin(Time.time * Mathf.PI * 2.3f) * 0.035f;
                squashY *= breathe;
                squashXZ /= breathe;
            }

            transform.localScale = new Vector3(
                _rootScale0.x * squashXZ,
                _rootScale0.y * squashY,
                _rootScale0.z * squashXZ);
        }

        void ApplyPunch(PunchPhase phase, float t)
        {
            float armX;
            float yaw;
            float fold;
            switch (phase)
            {
                case PunchPhase.Windup:
                    armX = Mathf.Lerp(8f, 48f, EaseOut(t));
                    yaw = Mathf.Lerp(0f, -32f, t);
                    fold = Mathf.Lerp(0f, 52f, t);
                    break;
                case PunchPhase.Active:
                    armX = Mathf.Lerp(12f, -98f, EaseOut(t));
                    yaw = Mathf.Lerp(-10f, 28f, EaseOut(t));
                    fold = Mathf.Lerp(28f, 4f, EaseOut(t));
                    break;
                default:
                    armX = Mathf.Lerp(-55f, 0f, EaseIn(t));
                    yaw = Mathf.Lerp(14f, 0f, t);
                    fold = Mathf.Lerp(16f, 0f, t);
                    break;
            }

            _uaRT = _uaR0 * Quaternion.Euler(armX, yaw * 0.4f, -14f);
            _laRT = _laR0 * Quaternion.Euler(fold, 0f, 0f);
            _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-18f * Mathf.Max(0.25f, t), 10f, 16f), 0.7f);
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

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        static float EaseIn(float t) => t * t;

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

        void Cache(Transform root)
        {
            if (root == null) return;
            _hips = FindBone(root, "Hips", "hip", "Pelvis", "mixamorig:Hips");
            _spine = FindBone(root, "Spine", "Spine1", "spine", "Torso", "Chest", "mixamorig:Spine");
            _head = FindBone(root, "Head", "head", "mixamorig:Head");
            _upperArmL = FindBone(root, "UpperArm_L", "LeftUpperArm", "upperarm_l", "Shoulder_L", "mixamorig:LeftArm");
            _upperArmR = FindBone(root, "UpperArm_R", "RightUpperArm", "upperarm_r", "Shoulder_R", "mixamorig:RightArm");
            _lowerArmL = FindBone(root, "LowerArm_L", "LeftLowerArm", "lowerarm_l", "mixamorig:LeftForeArm");
            _lowerArmR = FindBone(root, "LowerArm_R", "RightLowerArm", "lowerarm_r", "mixamorig:RightForeArm");
            _upperLegL = FindBone(root, "UpperLeg_L", "LeftUpperLeg", "upperleg_l", "mixamorig:LeftUpLeg");
            _upperLegR = FindBone(root, "UpperLeg_R", "RightUpperLeg", "upperleg_r", "mixamorig:RightUpLeg");
            _lowerLegL = FindBone(root, "LowerLeg_L", "LeftLowerLeg", "lowerleg_l", "mixamorig:LeftLeg");
            _lowerLegR = FindBone(root, "LowerLeg_R", "RightLowerLeg", "lowerleg_r", "mixamorig:RightLeg");

            if (_upperArmL == null) _upperArmL = FindBone(root, "Arm_L");
            if (_upperArmR == null) _upperArmR = FindBone(root, "Arm_R");

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
            if (_rootScale0 == Vector3.zero) _rootScale0 = transform.localScale;
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
