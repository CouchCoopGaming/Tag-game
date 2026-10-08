using Tag.Art;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Results poses. The winner leans back. The last place bows.
    /// A raised arm and a clap sink on this rig. No root travel.
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
        Renderer[] _rend;
        int _rendCount;
        Transform _shadow;
        float _floor;
        bool _hasFloor;

        public static void Slot(int rank, out float x, out float height)
        {
            if (rank <= 0) { x = 0.00f; height = 0.72f; return; }
            if (rank == 1) { x = -1.46f; height = 0.50f; return; }
            if (rank == 2) { x = 1.50f; height = 0.34f; return; }
            x = 2.92f;
            height = 0.20f;
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

        /// <summary>World Y of the block top. Soles stay within 1 cm of it.</summary>
        public void Floor(float worldY)
        {
            _floor = worldY;
            _hasFloor = true;
            EnsureShadow();
            Ground();
        }

        void Update()
        {
            if (_still) return;
            Apply(Time.unscaledTime);
        }

        void Apply(float t)
        {
            float time = _still ? 1.2f : t;
            MenuAlive.Angles a;
            if (_win) a = MenuAlive.Cheer(time, 1f);
            else if (_clap) a = MenuAlive.Cheer(time, _small ? 0.3f : 0.55f);
            else a = MenuAlive.Slump(time);
            Pose(a);
            Ground();
        }

        void EnsureShadow()
        {
            if (_shadow != null) return;
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ContactShadow";
            Transform stand = transform.parent;
            if (stand != null && stand.name == "FacePivot") stand = stand.parent;
            if (stand == null) stand = transform;
            quad.transform.SetParent(stand, true);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(0.72f, 0.42f, 1f);
            Collider col = quad.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            Renderer rend = quad.GetComponent<Renderer>();
            if (rend != null)
                rend.sharedMaterial = DummyPrimitiveFactory.MakeMat(new Color(0.02f, 0.03f, 0.05f, 0.55f), 0.4f, 0f);
            _shadow = quad.transform;
        }

        void Ground()
        {
            if (!_hasFloor || _rend == null) return;
            float lowest = 1000f;
            float footLow = 1000f;
            bool any = false;
            bool foot = false;
            float midX = transform.position.x;
            float midZ = transform.position.z;
            for (int i = 0; i < _rendCount; i++)
            {
                Renderer rend = _rend[i];
                if (rend == null) continue;
                string n = rend.gameObject.name;
                if (n == "ContactShadow" || n == "MenuHat") continue;
                float y = rend.bounds.min.y;
                if (y < lowest) lowest = y;
                any = true;
                if (n.IndexOf("Foot") < 0) continue;
                if (y < footLow) footLow = y;
                foot = true;
            }
            if (!any) return;
            if (foot) lowest = footLow;
            float target = _floor + 0.005f;
            float dy = target - lowest;
            float scale = 1f;
            if (transform.parent != null)
            {
                float sy = transform.parent.lossyScale.y;
                if (sy > 0.001f) scale = sy;
            }
            Vector3 p = transform.localPosition;
            p.y += dy / scale;
            transform.localPosition = p;
            if (_shadow == null) return;
            Vector3 s = _shadow.position;
            s.x = midX;
            s.y = _floor + 0.008f;
            s.z = midZ;
            _shadow.position = s;
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
            _rend = GetComponentsInChildren<Renderer>(true);
            _rendCount = _rend != null ? _rend.Length : 0;
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
