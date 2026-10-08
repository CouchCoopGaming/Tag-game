using Tag.Art;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Menu-only idle. Uses IdlePose for the breath and the weight shift,
    /// and the idle arm hang on the primitive body. Ready leans into the
    /// existing ready pose and hops once with MenuPolish.Hop.
    /// </summary>
    public sealed class MenuIdle : MonoBehaviour
    {
        Transform _hips, _spine, _head;
        Transform _armL, _armR, _foreL, _foreR;
        Transform _thighL, _thighR, _kneeL, _kneeR;
        Quaternion _hips0, _spine0, _head0;
        Quaternion _armL0, _armR0, _foreL0, _foreR0;
        Quaternion _thighL0, _thighR0, _kneeL0, _kneeR0;
        bool _primitive;
        bool _ready;
        float _blend;
        float _shift;
        float _breath;
        float _hop;
        float _restY;
        bool _restYSet;

        public void SetReady(bool ready)
        {
            if (ready && !_ready) _hop = 1f;
            if (!ready) _hop = 0f;
            _ready = ready;
        }

        public void Capture(bool primitive)
        {
            _primitive = primitive;
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
            _restY = transform.localPosition.y;
            _restYSet = true;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float goal = _ready ? 1f : 0f;
            if (MenuVideo.ReduceMotion)
            {
                _blend = goal;
                _hop = 0f;
                Pose(MenuAlive.Lerp(MenuAlive.Idle(0f, 0f), MenuAlive.Ready(), _blend));
                Lift(0f);
                return;
            }
            if (dt > 0.05f) dt = 0.05f;
            _blend = Mathf.MoveTowards(_blend, goal, dt / 0.18f);
            if (_hop > 0f)
            {
                _hop -= dt / 0.36f;
                if (_hop < 0f) _hop = 0f;
            }
            _shift += IdlePose.ShiftRate * dt;
            _breath += IdlePose.BreathRate * dt;
            Pose(MenuAlive.Lerp(MenuAlive.Idle(_shift, _breath), MenuAlive.Ready(), _blend));
            Lift(MenuPolish.Hop(_hop));
        }

        void Lift(float hop)
        {
            if (!_restYSet) return;
            Vector3 p = transform.localPosition;
            p.y = _restY + hop;
            transform.localPosition = p;
        }

        void Pose(MenuAlive.Angles a)
        {
            transform.localRotation = Quaternion.Euler(a.RootPitch, a.RootYaw, a.RootRoll);
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
