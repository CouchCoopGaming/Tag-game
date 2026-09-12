using Tag.Gameplay;
using Tag.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Readable third-person dummy motion. Drives Dummy_Runner / Dummy_It bones
    /// (UpperArm_L/R, UpperLeg_L/R, …) or primitive limbs of the same names.
    /// No AnimationClips in the kit — this is the shippable playtest tell.
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
            bool air = !grounded;
            bool punching = _punch != null && _punch.IsPunching;
            var phase = _punch != null ? _punch.Phase : PunchPhase.Idle;

            float walkAmt = Mathf.Clamp01(speed / 5.5f);
            float runAmt = Mathf.InverseLerp(5.2f, 8.5f, speed);
            float cadence = Mathf.Lerp(6.4f, 9.6f, runAmt);
            if (grounded && speed > 0.35f)
                _cycle += dt * cadence;
            else
                _cycle = Mathf.MoveTowards(_cycle, Mathf.Round(_cycle), dt * 8f);

            float swing = Mathf.Sin(_cycle) * Mathf.Lerp(8f, 38f, Mathf.Max(walkAmt, runAmt));
            if (!grounded) swing *= 0.25f;
            if (sliding) swing *= 0.15f;

            float breath = Mathf.Sin(Time.time * 2.1f) * 2.2f;
            float punchT = PunchWeight(phase);

            if (_spine != null)
            {
                float lean = sliding ? 32f : air ? 8f : breath;
                if (punching) lean += 6f * punchT;
                _spine.localRotation = _spine0 * Quaternion.Euler(lean, 0f, 0f);
            }

            if (_hips != null)
            {
                float dip = sliding ? 10f : 0f;
                _hips.localRotation = _hips0 * Quaternion.Euler(dip, 0f, 0f);
            }

            if (_head != null)
                _head.localRotation = _head0 * Quaternion.Euler(sliding ? 12f : -breath * 0.35f, 0f, 0f);

            // Opposite arm / leg run cycle
            SetLocal(_upperArmL, _uaL0, swing, 0f, 10f);
            SetLocal(_upperArmR, _uaR0, -swing, 0f, -10f);
            SetLocal(_lowerArmL, _laL0, Mathf.Max(0f, -swing) * 0.55f, 0f, 0f);
            SetLocal(_lowerArmR, _laR0, Mathf.Max(0f, swing) * 0.55f, 0f, 0f);

            float leg = sliding ? 18f : air ? -28f : -swing * 0.85f;
            float legR = sliding ? 18f : air ? -28f : swing * 0.85f;
            SetLocal(_upperLegL, _ulL0, leg, 0f, 0f);
            SetLocal(_upperLegR, _ulR0, legR, 0f, 0f);
            float kneeL = sliding || air ? 55f : Mathf.Max(0f, swing) * 0.7f;
            float kneeR = sliding || air ? 55f : Mathf.Max(0f, -swing) * 0.7f;
            SetLocal(_lowerLegL, _llL0, kneeL, 0f, 0f);
            SetLocal(_lowerLegR, _llR0, kneeR, 0f, 0f);

            if (punchT > 0f && _upperArmR != null)
            {
                // Forward punch tell on the right arm (readable from the boom cam).
                var punchRot = Quaternion.Euler(-20f - 75f * punchT, 18f * punchT, -12f);
                _upperArmR.localRotation = _uaR0 * punchRot;
                if (_lowerArmR != null)
                    _lowerArmR.localRotation = _laR0 * Quaternion.Euler(10f + 25f * punchT, 0f, 0f);
            }

            float bob = grounded && !sliding ? Mathf.Abs(Mathf.Sin(_cycle * 2f)) * 0.03f * walkAmt : 0f;
            if (sliding) bob = -0.12f;
            transform.localPosition = _root0 + new Vector3(0f, bob, 0f);
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
                case PunchPhase.Windup: return 0.45f;
                case PunchPhase.Active: return 1f;
                case PunchPhase.HitRecover: return 0.35f;
                case PunchPhase.MissRecover: return 0.2f;
                default: return 0f;
            }
        }

        static void SetLocal(Transform t, Quaternion rest, float x, float y, float z)
        {
            if (t == null) return;
            t.localRotation = rest * Quaternion.Euler(x, y, z);
        }

        void Cache(Transform root)
        {
            if (root == null) return;
            _hips = FindBone(root, "Hips");
            _spine = FindBone(root, "Spine");
            _head = FindBone(root, "Head");
            _upperArmL = FindBone(root, "UpperArm_L", "LeftUpperArm", "upperarm_l");
            _upperArmR = FindBone(root, "UpperArm_R", "RightUpperArm", "upperarm_r");
            _lowerArmL = FindBone(root, "LowerArm_L", "LeftLowerArm", "lowerarm_l");
            _lowerArmR = FindBone(root, "LowerArm_R", "RightLowerArm", "lowerarm_r");
            _upperLegL = FindBone(root, "UpperLeg_L", "LeftUpperLeg", "upperleg_l");
            _upperLegR = FindBone(root, "UpperLeg_R", "RightUpperLeg", "upperleg_r");
            _lowerLegL = FindBone(root, "LowerLeg_L", "LeftLowerLeg", "lowerleg_l");
            _lowerLegR = FindBone(root, "LowerLeg_R", "RightLowerLeg", "lowerleg_r");

            if (_upperArmL == null && _upperLegL == null)
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
