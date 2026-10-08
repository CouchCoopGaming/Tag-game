using Tag.Art;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Menu-only idle. Uses IdlePose for the breath and the weight shift,
    /// and the idle arm hang on the primitive body. No root motion.
    /// </summary>
    public sealed class MenuIdle : MonoBehaviour
    {
        Transform _hips, _spine, _head;
        Transform _armL, _armR, _foreL, _foreR;
        Transform _thighL, _thighR, _kneeL, _kneeR, _footL, _footR;
        Quaternion _hips0, _spine0, _head0;
        Quaternion _armL0, _armR0, _foreL0, _foreR0;
        Quaternion _thighL0, _thighR0, _kneeL0, _kneeR0, _footL0, _footR0;
        bool _primitive;
        bool _ready;
        bool _hold;
        float _blend;
        float _shift;
        float _breath;

        public void SetReady(bool ready)
        {
            _ready = ready;
        }

        /// <summary>Relaxed idle held on pose sample 0. No breath, no new clip.</summary>
        public void HoldRest()
        {
            _hold = true;
            _ready = false;
            _blend = 0f;
            Apply(IdlePose.At(0f, 0f), 0f);
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
            _footL = Find("Foot_L", "Foot.L");
            _footR = Find("Foot_R", "Foot.R");
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
            _footL0 = Rest(_footL);
            _footR0 = Rest(_footR);
        }

        void Update()
        {
            if (_hold)
            {
                Apply(IdlePose.At(0f, 0f), 0f);
                return;
            }
            float dt = Time.unscaledDeltaTime;
            float goal = _ready ? 1f : 0f;
            if (MenuVideo.ReduceMotion)
            {
                _blend = goal;
                Apply(IdlePose.At(0f, 0f), _blend);
                return;
            }
            if (dt > 0.05f) dt = 0.05f;
            _blend = Mathf.MoveTowards(_blend, goal, dt / 0.18f);
            _shift += IdlePose.ShiftRate * dt;
            _breath += IdlePose.BreathRate * dt;
            Apply(IdlePose.At(_shift, _breath), _blend);
        }

        void Apply(IdlePose.Sample s, float ready)
        {
            if (ready < 0f) ready = 0f;
            if (ready > 1f) ready = 1f;
            float idle = 1f - ready;
            Set(_hips, _hips0, 0f, 0f, s.HipRoll * idle);
            Set(_spine, _spine0, s.ChestPitch * idle + (-8f * ready), 0f, s.ChestRoll * idle);
            Set(_head, _head0, s.HeadPitch * idle + (-4f * ready), 0f, 0f);
            float hang = _primitive ? VerbPoseClips.IdleArmPitch : 0f;
            float yaw = _primitive ? VerbPoseClips.IdleArmYaw : 0f;
            float elbow = _primitive ? VerbPoseClips.IdleElbow : 0f;
            Set(_armL, _armL0, (hang + s.Shoulder) * idle + (-58f * ready), yaw * idle, 0f);
            Set(_armR, _armR0, (hang + s.Shoulder) * idle + (-58f * ready), -yaw * idle, 0f);
            Set(_foreL, _foreL0, elbow * idle + (42f * ready), 0f, 0f);
            Set(_foreR, _foreR0, elbow * idle + (42f * ready), 0f, 0f);
            Set(_thighL, _thighL0, s.ThighL * idle + (10f * ready), 0f, 0f);
            Set(_thighR, _thighR0, s.ThighR * idle + (10f * ready), 0f, 0f);
            Set(_kneeL, _kneeL0, s.KneeL * idle + (16f * ready), 0f, 0f);
            Set(_kneeR, _kneeR0, s.KneeR * idle + (16f * ready), 0f, 0f);
            Set(_footL, _footL0, s.FootL * idle, 0f, 0f);
            Set(_footR, _footR0, s.FootR * idle, 0f, 0f);
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
