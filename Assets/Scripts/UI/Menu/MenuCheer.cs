using Tag.Art;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Results poses. The winner claims with the existing raise and chest beat.
    /// The others give the round up, or stumble. No root motion.
    /// Reduce motion holds one frame of that pose.
    /// </summary>
    public sealed class MenuCheer : MonoBehaviour
    {
        Transform _hips, _spine, _head;
        Transform _armL, _armR, _foreL, _foreR;
        Transform _thighL, _thighR, _kneeL, _kneeR;
        Quaternion _hips0, _spine0, _head0;
        Quaternion _armL0, _armR0, _foreL0, _foreR0;
        Quaternion _thighL0, _thighR0, _kneeL0, _kneeR0;
        bool _win;
        bool _clap;
        bool _small;
        bool _still;

        public static void Slot(int rank, out float x, out float height)
        {
            if (rank <= 0) { x = 0.02f; height = 1.16f; return; }
            if (rank == 1) { x = -1.32f; height = 0.78f; return; }
            if (rank == 2) { x = 1.28f; height = 0.52f; return; }
            x = 2.38f;
            height = 0.30f;
        }

        public static void Dress(GameObject body, Color color)
        {
            if (body == null) return;
            Renderer[] parts = body.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                Renderer part = parts[i];
                if (part == null) continue;
                string n = part.gameObject.name;
                Color paint = color;
                if (n.IndexOf("Joint") >= 0 || n.IndexOf("Eye") >= 0)
                    paint = Color.Lerp(color, Color.black, 0.35f);
                part.sharedMaterial = DummyPrimitiveFactory.MakeMat(paint);
            }
        }

        public static void Play(GameObject body, bool win, bool clap, bool small)
        {
            if (body == null) return;
            MenuIdle idle = body.GetComponent<MenuIdle>();
            if (idle != null) idle.enabled = false;
            MenuCheer cheer = body.GetComponent<MenuCheer>();
            if (cheer == null) cheer = body.AddComponent<MenuCheer>();
            cheer.Begin(win, clap, small);
        }

        public void Begin(bool win, bool clap, bool small)
        {
            _win = win;
            _clap = clap && !win;
            _small = small && _clap;
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
            if (_win)
            {
                float u = _still ? 1f : 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * 2.1f));
                BecomeItPose.Sample s = BecomeItPose.Claim(u);
                Pose(s.ThighL, s.ThighR, s.KneeL, s.KneeR, s.ArmPitchL, s.ArmPitchR, s.ArmYawL, s.ArmYawR, s.ArmRollL, s.ArmRollR, s.ElbowL, s.ElbowR, s.Hip, s.HipYaw, s.Spine, s.SpineYaw, s.Head, s.HeadYaw);
                return;
            }
            if (_clap)
            {
                float elbow = _small ? -72f : -108f;
                float pitch = _small ? -34f : -50f;
                Pose(6f, 6f, -8f, -8f, pitch, pitch, -34f, 34f, 8f, -8f, elbow, elbow, 0f, 0f, -6f, 0f, -4f, 0f);
                return;
            }
            BecomeItPose.Sample give = BecomeItPose.GiveUp();
            Pose(give.ThighL, give.ThighR, give.KneeL, give.KneeR, give.ArmPitchL, give.ArmPitchR, give.ArmYawL, give.ArmYawR, give.ArmRollL, give.ArmRollR, give.ElbowL, give.ElbowR, give.Hip, give.HipYaw, give.Spine, give.SpineYaw, give.Head, give.HeadYaw);
        }

        void Pose(float thighL, float thighR, float kneeL, float kneeR, float armPitchL, float armPitchR, float armYawL, float armYawR, float armRollL, float armRollR, float elbowL, float elbowR, float hip, float hipYaw, float spine, float spineYaw, float head, float headYaw)
        {
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
