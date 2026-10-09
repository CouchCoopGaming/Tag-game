using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
        /// Title parade. A march and a step, leaned as a whole body.
        /// A full stride and a vault knee sink on this rig. No root travel.
        /// Reduce motion holds one frame.
    /// </summary>
    public sealed class MenuStride : MonoBehaviour
    {
        Transform _hips, _spine, _head;
        Transform _armL, _armR, _foreL, _foreR;
        Transform _thighL, _thighR, _kneeL, _kneeR;
        Quaternion _hips0, _spine0, _head0;
        Vector3 _hipsAt0;
        Quaternion _armL0, _armR0, _foreL0, _foreR0;
        Quaternion _thighL0, _thighR0, _kneeL0, _kneeR0;
        bool _vault;
        float _phase;
        bool _still;
        Quaternion _basis = Quaternion.identity;
        bool _hasBasis;

        public void Begin(bool vault, float phase)
        {
            _vault = vault;
            _phase = phase;
            _still = MenuVideo.ReduceMotion;
            if (!_hasBasis)
            {
                _basis = transform.localRotation;
                _hasBasis = true;
            }
            Cache();
            Apply(0f);
        }

        void Update()
        {
            if (_still) return;
            Apply(Time.unscaledTime);
        }

        void Apply(float t)
        {
            float age = t + _phase;
            MenuAlive.Angles a = _vault
                ? MenuAlive.Step(_still ? 0.5f : age)
                : MenuAlive.Run(_still ? 0.4f : age);
            Pose(a);
        }

        void Pose(MenuAlive.Angles a)
        {
            // The body lean rides on the hips, pivoting at the root (the feet), so
            // the root itself stays upright (FigureStandsUp: root within 5 deg).
            if (_hips == null)
                transform.localRotation = _basis * Quaternion.Euler(a.RootPitch, a.RootYaw, a.RootRoll);
            else
                transform.localRotation = _basis * Quaternion.Euler(0f, a.RootYaw, 0f);
            Set(_hips, _hips0, a.Hip, a.HipYaw, a.HipRoll);
            Set(_spine, _spine0, a.Spine, a.SpineYaw, a.SpineRoll);
            Set(_head, _head0, a.Head, a.HeadYaw, 0f);
            Set(_armL, _armL0, a.ArmPitchL, a.ArmYawL, a.ArmRollL);
            Set(_armR, _armR0, a.ArmPitchR, a.ArmYawR, a.ArmRollR);
            Set(_foreL, _foreL0, a.ElbowL, 0f, 0f);
            Set(_foreR, _foreR0, a.ElbowR, 0f, 0f);
            Set(_thighL, _thighL0, a.ThighL, 0f, 0f);
            Set(_thighR, _thighR0, a.ThighR, 0f, 0f);
            Set(_kneeL, _kneeL0, a.KneeL, 0f, 0f);
            Set(_kneeR, _kneeR0, a.KneeR, 0f, 0f);
            Lean(a.RootPitch, a.RootRoll);
        }

        void Lean(float pitch, float roll)
        {
            if (_hips == null) return;
            _hips.localPosition = _hipsAt0;
            Quaternion frame = transform.rotation;
            Quaternion lean = frame * Quaternion.Euler(pitch, 0f, roll) * Quaternion.Inverse(frame);
            Vector3 pivot = transform.position;
            _hips.position = pivot + lean * (_hips.position - pivot);
            _hips.rotation = lean * _hips.rotation;
        }

        void Cache()
        {
            _hips = Find("Hips", "Pelvis");
            _spine = Find("Spine", "Chest", "Torso");
            _head = Find("Head");
            _armL = Find("UpperArm_L", "UpperArm.L");
            _armR = Find("UpperArm_R", "UpperArm.R");
            _foreL = Find("LowerArm_L", "LowerArm.L");
            _foreR = Find("LowerArm_R", "LowerArm.R");
            _thighL = Find("UpperLeg_L", "UpperLeg.L");
            _thighR = Find("UpperLeg_R", "UpperLeg.R");
            _kneeL = Find("LowerLeg_L", "LowerLeg.L");
            _kneeR = Find("LowerLeg_R", "LowerLeg.R");
            _hips0 = Rest(_hips);
            _hipsAt0 = _hips != null ? _hips.localPosition : Vector3.zero;
            _spine0 = Rest(_spine);
            _head0 = Rest(_head);
            _armL0 = Rest(_armL);
            _armR0 = Rest(_armR);
            _foreL0 = Rest(_foreL);
            _foreR0 = Rest(_foreR);
            _thighL0 = Rest(_thighL);
            _thighR0 = Rest(_thighR);
            _kneeL0 = Rest(_kneeL);
            _kneeR0 = Rest(_kneeR);
        }

        static Quaternion Rest(Transform t)
        {
            return t != null ? t.localRotation : Quaternion.identity;
        }

        static void Set(Transform t, Quaternion rest, float x, float y, float z)
        {
            if (t == null) return;
            t.localRotation = rest * Quaternion.Euler(x, y, z);
        }

        Transform Find(params string[] names)
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].name == names[n]) return all[i];
                }
            }
            return null;
        }
    }
}
