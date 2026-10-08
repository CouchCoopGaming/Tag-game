using Tag.Art;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Title parade. Run uses the existing stride. Vault uses the existing mantle.
    /// No root motion. Reduce motion holds the first frame.
    /// </summary>
    public sealed class MenuStride : MonoBehaviour
    {
        Transform _hips, _spine, _head;
        Transform _armL, _armR, _foreL, _foreR;
        Transform _thighL, _thighR, _kneeL, _kneeR;
        Quaternion _hips0, _spine0, _head0;
        Quaternion _armL0, _armR0, _foreL0, _foreR0;
        Quaternion _thighL0, _thighR0, _kneeL0, _kneeR0;
        bool _vault;
        float _phase;
        bool _still;

        public void Begin(bool vault, float phase)
        {
            _vault = vault;
            _phase = phase;
            _still = MenuVideo.ReduceMotion;
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
            if (_vault)
            {
                float u = _still ? 0.4f : Mathf.Repeat(age * 0.28f, 1f);
                MantlePose.Sample s = MantlePose.At(u, true);
                Pose(s.ThighL, s.ThighR, s.KneeL, s.KneeR, s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, 0f, 0f, s.ElbowL, s.ElbowR, s.Hip, 0f, s.Spine, 0f, s.Head, 0f);
                return;
            }
            float cycle = _still ? 0.4f : age * 5.5f;
            JumpPose.Sample run = JumpPose.Stride(12f, Mathf.Sin(cycle), cycle);
            Pose(run.ThighL, run.ThighR, run.KneeL, run.KneeR, run.ArmPitchL, run.ArmPitchR, run.ArmYawL, run.ArmYawR, 0f, 0f, run.ElbowL, run.ElbowR, run.Hip, 0f, run.Spine, 0f, 0f, 0f);
        }

        void Pose(float thighL, float thighR, float kneeL, float kneeR, float armPitchL, float armPitchR, float armYawL, float armYawR, float armRollL, float armRollR, float elbowL, float elbowR, float hip, float hipYaw, float spine, float spineYaw, float head, float headYaw)
        {
            float show = 0.012f;
            thighL *= show; thighR *= show; kneeL *= show; kneeR *= show;
            armPitchL *= show; armPitchR *= show; armYawL *= show; armYawR *= show;
            armRollL *= show; armRollR *= show; elbowL *= show; elbowR *= show;
            hip *= show; hipYaw *= show; spine *= show; spineYaw *= show; head *= show; headYaw *= show;
            Set(_hips, _hips0, hip, hipYaw, 0f);
            Set(_spine, _spine0, spine, spineYaw, 0f);
            Set(_head, _head0, head, headYaw, 0f);
            Set(_armL, _armL0, armPitchL, armYawL, armRollL);
            Set(_armR, _armR0, armPitchR, armYawR, armRollR);
            Set(_foreL, _foreL0, elbowL, 0f, 0f);
            Set(_foreR, _foreR0, elbowR, 0f, 0f);
            Set(_thighL, _thighL0, thighL, 0f, 0f);
            Set(_thighR, _thighR0, thighR, 0f, 0f);
            Set(_kneeL, _kneeL0, kneeL, 0f, 0f);
            Set(_kneeR, _kneeR0, kneeR, 0f, 0f);
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
