using Tag.Experimental;
using Tag.Gameplay;
using Tag.Level;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Editor play scene. One Hier dummy per verb, driven by the real motor and
    /// the pose stack. Slow motion scales time only while this component is alive.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class MotionGallery : MonoBehaviour
    {
        const int Run = 0;
        const int Climb = 1;
        const int Wall = 2;
        const int Vault = 3;
        const int Mantle = 4;
        const int Slide = 5;
        const int Dash = 6;
        const int Punch = 7;
        const int Zip = 8;
        const int Pad = 9;
        const int Grapple = 10;
        const int Stagger = 11;
        const int IdleIt = 12;
        const int IdleRun = 13;
        const int SoftA = 14;
        const int SoftB = 15;
        const int WallRef = 16;
        const int TicTac = 17;
        const int CatLeap = 18;
        const int Count = 19;
        const float Gap = 8f;

        sealed class Slot
        {
            public int Kind;
            public PlayerMotor Motor;
            public PlayerInputReader Input;
            public DummyLocomotor Loco;
            public OpponentLungeTell Tell;
            public Vector3 Home;
            public float Yaw;
            public bool Pivoted;
            public int Beat;
        }

        Slot[] _slots;
        float[] _clock;
        float[] _period;
        Transform[] _labels;
        Camera _cam;
        GameObject _slowMark;
        Vector3 _focus;
        float _orbit;
        float _yaw = 20f;
        float _pitch = 18f;
        bool _fly;
        bool _slow;
        bool _built;

        void Start()
        {
            _cam = Camera.main;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            MovementConfig cfg = Resources.Load<MovementConfig>("TagArena/MovementConfig");
            if (cfg == null)
            {
                Debug.LogError("Motion gallery is missing TagArena/MovementConfig");
                return;
            }

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "GalleryFloor";
            floor.transform.position = new Vector3((Count - 1) * Gap * 0.5f, -0.5f, 6f);
            floor.transform.localScale = new Vector3(Count * Gap + 24f, 1f, 48f);

            _slots = new Slot[Count];
            _clock = new float[Count];
            _period = new float[Count];
            _labels = new Transform[Count];
            _focus = new Vector3((Count - 1) * Gap * 0.5f, 1.2f, 4f);

            BuildSlot(0, Run, "GalleryRun", "RUN", "Blue", false, cfg, 3.6f);
            BuildSlot(1, Climb, "GalleryClimb", "CLIMB", "Mint", false, cfg, 4.5f);
            BuildSlot(2, Wall, "GalleryWall", "WALL", "Lavender", false, cfg, 4.2f);
            BuildSlot(3, Vault, "GalleryVault", "VAULT", "Tan", false, cfg, 3.2f);
            BuildSlot(4, Mantle, "GalleryMantle", "MANTLE", "Red", false, cfg, 3.6f);
            BuildSlot(5, Slide, "GallerySlide", "SLIDE", "Blue", false, cfg, 3.2f);
            BuildSlot(6, Dash, "GalleryDash", "DASH", "Mint", false, cfg, 3.2f);
            BuildSlot(7, Punch, "GalleryPunch", "PUNCH", "Orange", false, cfg, 3.2f);
            BuildSlot(8, Zip, "GalleryZip", "ZIP", "Lavender", false, cfg, 4.2f);
            BuildSlot(9, Pad, "GalleryPad", "PAD", "Tan", false, cfg, 3.6f);
            BuildSlot(10, Grapple, "GalleryGrapple", "GRAPPLE", "Red", false, cfg, 3.4f);
            BuildSlot(11, Stagger, "GalleryStagger", "STAGGER", "Blue", false, cfg, 2.6f);
            BuildSlot(12, IdleIt, "GalleryIt", "IDLE IT", "Orange", true, cfg, 8f);
            BuildSlot(13, IdleRun, "GalleryRunner", "IDLE RUN", "Mint", false, cfg, 8f);
            BuildPreview(SoftA, "GallerySoft03", "SOFT 03", "Tan", StorrorClips.Seconds[StorrorClips.SoftLandA]);
            BuildPreview(SoftB, "GallerySoft04", "SOFT 04", "Blue", StorrorClips.Seconds[StorrorClips.SoftLandB]);
            BuildPreview(WallRef, "GalleryWallRef", "WALL RUN", "Lavender", StorrorClips.Seconds[StorrorClips.WallRun]);
            BuildPreview(TicTac, "GalleryTicTac", "TIC TAC", "Mint", StorrorClips.Seconds[StorrorClips.WallJump]);
            BuildPreview(CatLeap, "GalleryCatLeap", "CAT LEAP", "Red", StorrorClips.Seconds[StorrorClips.Cling]);
            DressStages();
            _slowMark = MakeLabel(_focus + new Vector3(0f, 6f, 0f), "SLOW");
            _slowMark.SetActive(false);
            MakeLabel(_focus + new Vector3(0f, 7.2f, 0f), "ORBIT   F FLY   T SLOW");
            _built = true;
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.T))
                ToggleSlow();
            if (UnityEngine.Input.GetKeyDown(KeyCode.F))
                _fly = !_fly;
            TickCamera();
            if (!_built || _slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
                Drive(i);
            if (_slowMark != null)
                _slowMark.SetActive(_slow);
        }

        void ToggleSlow()
        {
            _slow = !_slow;
            Time.timeScale = _slow ? 0.25f : 1f;
        }

        void TickCamera()
        {
            if (_cam == null) return;
            if (_fly)
            {
                _yaw += UnityEngine.Input.GetAxis("Mouse X") * 2.2f;
                _pitch -= UnityEngine.Input.GetAxis("Mouse Y") * 1.6f;
                if (_pitch > 80f) _pitch = 80f;
                if (_pitch < -80f) _pitch = -80f;
                _cam.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                Vector3 wish = Vector3.zero;
                if (UnityEngine.Input.GetKey(KeyCode.W)) wish.z += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S)) wish.z -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.A)) wish.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D)) wish.x += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.E)) wish.y += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.Q)) wish.y -= 1f;
                _cam.transform.position += _cam.transform.rotation * wish * (12f * Time.deltaTime);
            }
            else
            {
                _orbit += Time.deltaTime * 8f;
                float rad = _orbit * 0.0174532924f;
                float x = _focus.x + (float)System.Math.Sin(rad) * 46f;
                float z = _focus.z + (float)System.Math.Cos(rad) * 34f;
                _cam.transform.position = new Vector3(x, 12f, z);
                _cam.transform.LookAt(_focus);
            }
            FaceLabels();
        }

        void FaceLabels()
        {
            if (_labels == null || _cam == null) return;
            for (int i = 0; i < _labels.Length; i++)
            {
                Transform label = _labels[i];
                if (label == null) continue;
                Vector3 to = label.position - _cam.transform.position;
                if (to.sqrMagnitude < 0.04f) continue;
                label.rotation = Quaternion.LookRotation(to);
            }
            if (_slowMark != null)
            {
                Vector3 to = _slowMark.transform.position - _cam.transform.position;
                if (to.sqrMagnitude > 0.04f)
                    _slowMark.transform.rotation = Quaternion.LookRotation(to);
            }
        }

        void Drive(int i)
        {
            Slot s = _slots[i];
            if (s == null) return;
            if (s.Kind >= SoftA)
            {
                _clock[i] += Time.deltaTime;
                if (_clock[i] >= _period[i])
                    _clock[i] = 0f;
                if (s.Loco != null)
                {
                    float u = _period[i] > 0.001f ? _clock[i] / _period[i] : 0f;
                    s.Loco.SetStorrorPreview(s.Kind - SoftA, u);
                }
                return;
            }
            if (s.Motor == null || s.Input == null) return;
            _clock[i] += Time.deltaTime;
            if (_clock[i] >= _period[i] || s.Motor.transform.position.y < -3f)
            {
                _clock[i] = 0f;
                s.Pivoted = false;
                s.Beat = -1;
                s.Yaw = 0f;
                s.Motor.Place(s.Home, "gallery");
                s.Motor.transform.rotation = Quaternion.identity;
            }
            float t = _clock[i];
            Apply(s, t);
        }

        void Apply(Slot s, float t)
        {
            if (s.Kind == Run)
            {
                if (t >= 2.5f && !s.Pivoted)
                {
                    s.Pivoted = true;
                    s.Yaw = 180f;
                    s.Motor.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                }
                bool sprint = t < 1.05f || (t >= 1.45f && t < 2.35f) || t >= 2.5f;
                float y = t < 1.05f || t >= 2.5f ? 1f : (t < 1.45f ? 0f : 1f);
                if (t >= 1.45f && t < 2.35f) y = -1f;
                s.Input.SetExternalMove(new Vector2(0f, y), sprint, false, false, false);
                return;
            }
            if (s.Kind == Climb)
            {
                // Forward climbs the wall. The last beat pulls back so the slip at 3.7 plays.
                bool slip = t >= 3.15f && t < 4.05f;
                float y = slip ? -1f : 1f;
                s.Input.SetExternalMove(new Vector2(0f, y), false, false, false, false);
                return;
            }
            if (s.Kind == Wall)
            {
                float wrap = t - 1.4f * (float)System.Math.Floor(t / 1.4f);
                bool jump = wrap < 0.12f;
                s.Input.SetExternalMove(new Vector2(-0.65f, 1f), true, jump, false, false);
                return;
            }
            if (s.Kind == Vault || s.Kind == Mantle || s.Kind == Pad)
            {
                s.Input.SetExternalMove(new Vector2(0f, 1f), true, false, false, false);
                return;
            }
            if (s.Kind == Slide)
            {
                s.Input.SetExternalMove(new Vector2(0f, 1f), true, false, false, false);
                s.Input.CrouchHeld = t < 2.15f;
                return;
            }
            if (s.Kind == Dash)
            {
                bool jump = t < 0.16f || (t > 1.55f && t < 1.72f);
                bool dash = (t >= 0.2f && t < 0.32f) || (t >= 1.78f && t < 1.9f);
                s.Input.SetExternalMove(new Vector2(0f, 1f), true, jump, false, dash);
                return;
            }
            if (s.Kind == Punch)
            {
                bool lunge = t >= 1.45f && t < 1.58f;
                s.Input.SetExternalMove(new Vector2(0f, 1f), true, false, lunge, false);
                s.Input.PunchPressed = t >= 0.22f && t < 0.34f;
                if (s.Tell != null)
                {
                    if (t >= 0.95f && t < 1.45f)
                        s.Tell.Show((t - 0.95f) / 0.5f);
                    else
                        s.Tell.Hide();
                }
                return;
            }
            if (s.Kind == Zip)
            {
                bool jump = t < 0.18f;
                s.Input.SetExternalMove(new Vector2(0f, 1f), true, jump, false, false);
                return;
            }
            if (s.Kind == Grapple)
            {
                float yaw = t < 0.7f ? 90f : 0f;
                s.Motor.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                bool fire = t < 0.7f || (t >= 0.85f && t < 2.05f) || (t >= 2.35f && t < 2.46f) || (t >= 2.5f && t < 2.62f);
                s.Input.SetExternalMove(new Vector2(0f, 0.2f), false, false, false, false);
                s.Input.JetHeld = fire;
                return;
            }
            if (s.Kind == Stagger)
            {
                s.Input.SetExternalMove(new Vector2(0f, 0.35f), false, false, false, false);
                int beat = t < 1.15f ? 0 : 1;
                if (beat != s.Beat && s.Loco != null)
                {
                    s.Beat = beat;
                    if (beat == 0) s.Loco.PlayPunchStagger();
                    else s.Loco.PlayTagFlinch();
                }
                return;
            }
            s.Input.SetExternalMove(Vector2.zero, false, false, false, false);
        }

        void BuildPreview(int index, int kind, string pawnName, string label, string color, float period)
        {
            float x = index * Gap;
            var home = new Vector3(x, 0f, 0f);
            var go = new GameObject(pawnName);
            var loco = go.AddComponent<DummyLocomotor>();
            DummyPrimitiveFactory.Build(go.transform, false, color);
            go.transform.position = home;
            go.transform.rotation = Quaternion.identity;
            var slot = new Slot();
            slot.Kind = kind;
            slot.Loco = loco;
            slot.Home = home;
            slot.Beat = -1;
            _slots[index] = slot;
            _period[index] = period;
            _labels[index] = MakeLabel(home + new Vector3(0f, 2.55f, 0f), label).transform;
        }

        void BuildSlot(int index, int kind, string pawnName, string label, string color, bool asIt, MovementConfig cfg, float period)
        {
            float x = index * Gap;
            var home = new Vector3(x, 0.08f, 0f);
            var go = new GameObject(pawnName);
            go.SetActive(false);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<CapsuleCollider>();
            var cc = go.AddComponent<CharacterController>();
            cc.height = cfg.standingHeight;
            cc.radius = cfg.radius;
            cc.center = new Vector3(0f, cfg.standingHeight * 0.5f, 0f);
            var input = go.AddComponent<PlayerInputReader>();
            go.AddComponent<SurfaceProbe>();
            var motor = go.AddComponent<PlayerMotor>();
            motor.cfg = cfg;
            motor.cam = null;
            ItController it = null;
            if (kind == Punch || kind == IdleIt || kind == IdleRun)
                it = go.AddComponent<ItController>();
            if (kind == Punch)
                go.AddComponent<PunchHitbox>();
            OpponentLungeTell tell = null;
            if (kind == Punch)
                tell = go.AddComponent<OpponentLungeTell>();
            if (kind == Grapple)
            {
                var hook = go.AddComponent<ExperimentalGrapple>();
                hook.enableGrapple = true;
            }
            var loco = go.AddComponent<DummyLocomotor>();
            DummyPrimitiveFactory.Build(go.transform, asIt, color);
            go.transform.position = home;
            go.transform.rotation = Quaternion.identity;
            go.SetActive(true);
            if (it != null)
                it.SetIt(asIt || kind == IdleIt);
            var slot = new Slot();
            slot.Kind = kind;
            slot.Motor = motor;
            slot.Input = input;
            slot.Loco = loco;
            slot.Tell = tell;
            slot.Home = home;
            slot.Beat = -1;
            _slots[index] = slot;
            _period[index] = period;
            _labels[index] = MakeLabel(home + new Vector3(0f, 2.55f, 0f), label).transform;
        }

        void DressStages()
        {
            Slot climb = _slots[Climb];
            if (climb != null)
            {
                Box("ClimbWall", climb.Home + new Vector3(0f, 1.7f, 1.55f), new Vector3(3.2f, 3.4f, 0.45f));
                Box("ClimbLip", climb.Home + new Vector3(0f, 2.15f, 2.35f), new Vector3(3.2f, 0.28f, 1.5f));
            }
            Slot wall = _slots[Wall];
            if (wall != null)
                Box("WallRun", wall.Home + new Vector3(-1.05f, 2.2f, 6f), new Vector3(0.4f, 4.4f, 16f));
            Slot vault = _slots[Vault];
            if (vault != null)
                Box("VaultRail", vault.Home + new Vector3(0f, 0.5f, 2.1f), new Vector3(2.4f, 1.0f, 0.45f));
            Slot mantle = _slots[Mantle];
            if (mantle != null)
                Box("MantleLedge", mantle.Home + new Vector3(0f, 1.05f, 2.2f), new Vector3(2.6f, 2.1f, 1.4f));
            Slot zip = _slots[Zip];
            if (zip != null)
            {
                ZipLine.Build(zip.Motor.transform.parent, zip.Home + new Vector3(0f, 3.1f, 0.6f), zip.Home + new Vector3(0f, 2.4f, 14f), 14f, 0.3f);
            }
            Slot pad = _slots[Pad];
            if (pad != null)
                LaunchPad.Build(pad.Motor.transform.parent, pad.Home + new Vector3(0f, 0.02f, 1.4f), 6f, Vector3.forward, 8f, 0.3f);
            Slot hook = _slots[Grapple];
            if (hook != null)
                Box("GrappleWall", hook.Home + new Vector3(0f, 2.2f, 7f), new Vector3(4f, 4.4f, 0.4f));
        }

        static Transform Box(string name, Vector3 center, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = center;
            go.transform.localScale = scale;
            return go.transform;
        }

        static GameObject MakeLabel(Vector3 world, string text)
        {
            var go = new GameObject("Label");
            go.transform.position = world;
            var mesh = go.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
                mesh.font = font;
            mesh.text = text;
            mesh.characterSize = 0.18f;
            mesh.fontSize = 64;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            return go;
        }
    }
}
