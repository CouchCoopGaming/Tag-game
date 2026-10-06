using Tag.Couch;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Onboard
{
    /// <summary>
    /// Draws the first-run hint and the contextual chip.
    /// Update and OnGUI only read cached strings. They do not pause or eat verbs.
    /// </summary>
    public class PlayPromptHud : MonoBehaviour
    {
        public static PlayPromptHud Instance { get; private set; }

        public Camera View;
        public int Seat = -1;
        public int DriveDevice = -1;

        PlayerMotor _motor;
        PlayerInputReader _input;
        PunchHitbox _punch;
        Camera _cam;
        GUIStyle _label;
        readonly ContextPrompts _context = new ContextPrompts();
        ContextSample _sample;
        int _wallJumps;
        bool _wasCling;
        bool _wasArc;
        bool _wasRide;
        bool _anchored;
        Vector3 _point;

        public static void ResetStatics()
        {
            Instance = null;
            OnboardingSession.ResetStatics();
            ControlGlyphs.ResetStatics();
            PromptText.ResetStatics();
            HowToPlay.ResetStatics();
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
            _motor = GetComponent<PlayerMotor>();
            _input = GetComponent<PlayerInputReader>();
            _punch = GetComponent<PunchHitbox>();
            BootStyle();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (_motor == null) _motor = GetComponent<PlayerMotor>();
            if (_input == null) _input = GetComponent<PlayerInputReader>();
            if (_punch == null) _punch = GetComponent<PunchHitbox>();
            _cam = Camera.main;
            if (_motor != null) _wallJumps = _motor.WallJumpCount;
            OnboardingStore.Load(OnboardingSession.Live);
            BootStyle();
        }

        void BootStyle()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label);
            _label.fontSize = 16;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleLeft;
            _label.normal.textColor = Color.white;
        }

        void Update()
        {
            if (Instance == this)
                Tag.Core.FrameMeter.AddHud(Tag.Core.FrameMeter.HudOps);
            if (_motor == null || _input == null) return;
            if (Instance != this)
            {
                TickContext();
                return;
            }
            if (_cam == null) _cam = Camera.main;
            if (DriveDevice < 0) WatchDevice();
            if (Input.GetKeyDown(KeyCode.F12))
            {
                OnboardingSession.Live.Skip();
                OnboardingStore.Save(OnboardingSession.Live);
            }
            TickLearn();
            TickContext();
        }

        void WatchDevice()
        {
            bool pad = PadActive();
            bool key = KeyActive();
            if (pad && !key) ControlGlyphs.Note(InputDeviceKind.Gamepad);
            else if (key && !pad) ControlGlyphs.Note(InputDeviceKind.Keyboard);
        }

        static bool KeyActive()
        {
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) return true;
            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            if (mx > 0.01f || mx < -0.01f || my > 0.01f || my < -0.01f) return true;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) return true;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftControl)) return true;
            if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.F)) return true;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)) return true;
            return false;
        }

        static bool PadActive()
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.leftStick.ReadValue().sqrMagnitude > 0.06f) return true;
                if (pad.rightStick.ReadValue().sqrMagnitude > 0.06f) return true;
                if (pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.buttonWest.isPressed || pad.buttonNorth.isPressed) return true;
                if (pad.leftShoulder.isPressed || pad.rightShoulder.isPressed) return true;
                if (pad.startButton.isPressed || pad.selectButton.isPressed) return true;
                if (pad.dpad.up.isPressed || pad.dpad.down.isPressed || pad.dpad.left.isPressed || pad.dpad.right.isPressed) return true;
            }
#endif
            if (Input.GetKey(KeyCode.JoystickButton0) || Input.GetKey(KeyCode.JoystickButton1)) return true;
            if (Input.GetKey(KeyCode.JoystickButton2) || Input.GetKey(KeyCode.JoystickButton3)) return true;
            if (Input.GetKey(KeyCode.JoystickButton4) || Input.GetKey(KeyCode.JoystickButton5)) return true;
            if (Input.GetKey(KeyCode.JoystickButton6) || Input.GetKey(KeyCode.JoystickButton7)) return true;
            return false;
        }

        void TickLearn()
        {
            bool window = LearnNow();
            OnboardingSession session = OnboardingSession.Live;
            session.BeginIfNeeded(window);
            if (!session.Active) return;

            bool cleared = false;
            if (_input.Move.sqrMagnitude > 0.04f) cleared |= session.Offer(HintStep.Move, true);
            if (_motor.State == MoveState.Sprint) cleared |= session.Offer(HintStep.Sprint, true);
            if (_input.JumpPressed) cleared |= session.Offer(HintStep.Jump, true);
            if (_motor.IsSliding) cleared |= session.Offer(HintStep.Slide, true);
            if (_motor.State == MoveState.WallClimb) cleared |= session.Offer(HintStep.ClingClimb, true);
            if (_motor.WallJumpCount != _wallJumps)
            {
                _wallJumps = _motor.WallJumpCount;
                cleared |= session.Offer(HintStep.WallJump, true);
            }
            if (_motor.IsAirDashing) cleared |= session.Offer(HintStep.AirDash, true);
            bool punching = _input.PunchPressed || (_punch != null && _punch.IsPunching);
            if (punching) cleared |= session.Offer(HintStep.Punch, true);
            if (cleared) OnboardingStore.Save(session);
        }

        void TickContext()
        {
            float dist;
            Vector3 point;
            bool face = _motor.AirborneClingFace(out dist, out point);
            _sample.NearCling = face && dist <= ContextPrompts.ClingClose;

            Vector3 pawn = _motor.transform.position;
            Vector3 padPoint;
            _sample.NearPad = LaunchPad.PromptNear(pawn, ContextPrompts.PadRange, out padPoint);

            Vector3 zipPoint;
            _sample.NearZip = !_motor.ZipRiding && ZipLine.PromptNear(pawn, ContextPrompts.ZipRange, out zipPoint);

            bool ride = _motor.ZipRiding;
            _sample.RidingZip = ride;
            if (ride)
            {
                _point = pawn;
                _anchored = true;
            }
            else if (_sample.NearZip)
            {
                _point = zipPoint;
                _anchored = true;
            }
            else if (_sample.NearCling)
            {
                _point = point;
                _anchored = true;
            }
            else if (_sample.NearPad)
            {
                _point = padPoint;
                _anchored = true;
            }

            bool cling = _motor.State == MoveState.WallClimb || _motor.State == MoveState.WallRun;
            _sample.DidCling = cling && !_wasCling;
            _wasCling = cling;

            bool arc = _motor.LaunchArc;
            _sample.DidPad = arc && !_wasArc;
            _wasArc = arc;

            _sample.DidZipGrab = ride && !_wasRide;
            _sample.DidZipDrop = !ride && _wasRide;
            _wasRide = ride;

            if (!LearnNow())
            {
                _sample.NearCling = false;
                _sample.NearPad = false;
                _sample.NearZip = false;
                _sample.RidingZip = false;
            }
            _context.Tick(Time.deltaTime, _sample);
        }

        static bool LearnNow()
        {
            if (Time.timeScale <= 0f) return false;
            TagModeController modes = TagModeController.Instance;
            if (modes == null) return true;
            return OnboardingSession.LearnWindow(
                modes.Phase == MatchPhase.Countdown,
                modes.Phase == MatchPhase.Playing);
        }

        void OnGUI()
        {
            if (_label == null || _motor == null) return;
            if (Time.timeScale <= 0f) return;
            PromptText.Ensure();
            _label.fontSize = Screen.height >= 1000 ? 16 : 14;
            OnboardingSession session = OnboardingSession.Live;
            if (session.Active) DrawHint(session);
            if (_context.Alpha > 0.02f && _context.Kind != ContextKind.None) DrawChip();
        }

        void DrawHint(OnboardingSession session)
        {
            if (DriveDevice >= 0 && View != null)
            {
                DrawCouchHint(session);
                return;
            }
            float sw = Screen.width;
            float sh = Screen.height;
            VerbHudLayout.Box bar = VerbHudLayout.HintBar(sw, sh);
            GUI.Box(new Rect(bar.X, bar.Y, bar.W, bar.H), "");
            GUI.Label(new Rect(bar.X + 10f, bar.Y + 4f, bar.W - 92f, bar.H - 8f), PromptText.Hint(session.Current), _label);
            if (GUI.Button(new Rect(bar.Right - 76f, bar.Y + 10f, 64f, bar.H - 20f), "Skip"))
            {
                session.Skip();
                OnboardingStore.Save(session);
            }
        }

        void DrawCouchHint(OnboardingSession session)
        {
            Rect area = View.pixelRect;
            if (area.width < 8f) return;
            float x = area.x + 8f;
            float y = Screen.height - area.y - 44f;
            float w = area.width - 16f;
            if (w > 420f) w = 420f;
            float h = 36f;
            GUI.Box(new Rect(x, y, w, h), "");
            GUI.Label(new Rect(x + 8f, y + 4f, w - 16f, h - 8f), CouchPlay.Hint(DriveDevice, session.Current), _label);
        }

        void DrawChip()
        {
            if (DriveDevice >= 0 && View != null)
            {
                DrawCouchChip();
                return;
            }
            float sw = Screen.width;
            float sh = Screen.height;
            VerbHudLayout.Box chip = VerbHudLayout.ContextChip(sw, sh);
            float x = chip.X;
            float y = chip.Y;
            float w = chip.W;
            float h = chip.H;
            if (_anchored && _cam != null)
            {
                Vector3 sp = _cam.WorldToScreenPoint(_point);
                if (sp.z > 0.1f)
                {
                    x = sp.x - w * 0.5f;
                    y = sh - sp.y - h * 0.5f;
                    VerbHudLayout.PushMarker(sw, sh, ref x, ref y, w, h);
                }
            }
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _context.Alpha);
            GUI.Box(new Rect(x, y, w, h), "");
            if (PromptText.IconOnly(_context.Kind))
                DrawPadIcon(x, y);
            else
                GUI.Label(new Rect(x + 8f, y, w - 16f, h), PromptText.ContextLine(_context.Kind), _label);
            GUI.color = prev;
        }

        void DrawCouchChip()
        {
            Rect area = View.pixelRect;
            float x = area.x + 12f;
            float y = Screen.height - (area.y + area.height * 0.62f);
            float w = 220f;
            if (w > area.width - 24f) w = area.width - 24f;
            float h = 32f;
            string line = CouchPlay.ContextLine(DriveDevice, _context.Kind);
            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _context.Alpha);
            GUI.Box(new Rect(x, y, w, h), "");
            if (line.Length > 0)
                GUI.Label(new Rect(x + 8f, y, w - 16f, h), line, _label);
            else
                DrawPadIcon(x, y);
            GUI.color = prev;
        }

        static void DrawPadIcon(float x, float y)
        {
            GUI.DrawTexture(new Rect(x + 10f, y + 20f, 22f, 5f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + 16f, y + 10f, 10f, 12f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + 14f, y + 8f, 4f, 6f), Texture2D.whiteTexture);
        }
    }

    public static class OnboardingStore
    {
        public const string Key = "Tag.OnboardMask";

        public static void Save(OnboardingSession session)
        {
            if (session == null) return;
            PlayerPrefs.SetInt(Key, session.Pack());
            PlayerPrefs.Save();
        }

        public static void Load(OnboardingSession session)
        {
            if (session == null) return;
            session.Unpack(PlayerPrefs.GetInt(Key, 0));
        }
    }
}
