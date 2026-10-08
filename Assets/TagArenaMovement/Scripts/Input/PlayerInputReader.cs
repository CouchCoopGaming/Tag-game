using Tag.Couch;
using Tag.Gameplay;
using Tag.Settings;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TagArena.Movement
{
    /// <summary>
    /// Thin input adapter. Swap the body of Read() if you migrate to the new Input System.
    /// Keep raw + consumed flags separate so buffering (jump) is deterministic.
    /// When ExternalControl is true (AI), Read() leaves fields alone so DummyPatrol can drive them.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class PlayerInputReader : MonoBehaviour
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool SprintHeld;
        public bool CrouchHeld;
        public bool CrouchPressed;
        public bool JumpHeld;
        public bool JumpPressed;
        public bool SkiHeld;
        public bool JetHeld;
        public bool JetPressed;
        public bool LungePressed;
        public bool AirDashPressed;
        public bool PunchPressed;
        public bool TapForwardPulse;
        public bool LookFromGamepad;
        public bool LookIsStick;
        public Vector2 PadLookStick;
        public bool EvasionGrounded;
        public int EvasionId;
        public float EvasionPlanarSpeed;
        public float EvasionLateral;

        /// <summary>When true, Read() is a no-op - AI / tests own Move/Look/buttons.</summary>
        public bool ExternalControl;

        /// <summary>
        /// -1 keeps the solo read, where the keyboard and the current pad share one pawn.
        /// 0 is the keyboard couch pawn. 1–4 are pads. A pad never samples the keyboard.
        /// </summary>
        public int DriveDevice = -1;

        [Header("Legacy key map")]
        public KeyCode skiKey = KeyCode.LeftShift;
        public KeyCode jetKey = KeyCode.Mouse1;
        public KeyCode crouchKey = KeyCode.C;
        public KeyCode lungeKey = KeyCode.Mouse2;
        public KeyCode airDashKey = KeyCode.Q;
        public KeyCode punchKey = KeyCode.Mouse0;
        public KeyCode tapStrafePulseKey = KeyCode.W;
        public bool useShiftAsSprintWhenNotSkiing = true;

        float _prevCrouch;
        float _prevJump;
        float _prevSpace;
        float _prevJet;
        float _prevLunge;
        bool _prevW;
        float _prevMoveY;
        float _extPrevJump;
        bool _wasCursorLocked;
        // After pause/results unlock, locking the cursor in the same Update as Read can yaw+punch.
        // Drop look/punch for one locked frame so the resume click / residual mouse delta die first.
        int _lookPunchGateFrames;
        HoldSample _menuLatch;
        bool _resumeGate;
        bool _swallowResumeJump;
        bool _clearSprintOnResume;
        bool _clearClingOnResume;
        bool _couchWasLive;
        float _kbSpacePrev;
        int _readSerial = -1;
        EvasionGestures.State _gesture;
        EvasionGestures.TapState _stutterTap;

        void Awake()
        {
            ControlBinds.Load();
            airDashKey = ControlBinds.AirDash;
            punchKey = ControlBinds.Punch;
            SettingsRuntime.ArmInput();
        }

        void Update()
        {
            // Before PunchHitbox and the motor. The motor calls Read again; that call is a no-op.
            Read();
        }

        public void Read()
        {
            if (ExternalControl)
            {
                EvasionGestures.Reset(ref _gesture);
                EvasionGestures.ResetTap(ref _stutterTap);
                return;
            }
            int serial = Time.frameCount;
            if (_readSerial == serial) return;
            _readSerial = serial;
            if (DriveDevice >= 0)
            {
                ReadDriven();
                return;
            }

            airDashKey = ControlBinds.AirDash;
            punchKey = ControlBinds.Punch;

            // Pause freezes the clock but Update still runs. Results keep timeScale at 1
            // and unlock the cursor, so a Rematch click (Mouse0) would also punch.
            // Look is not scaled by deltaTime, so an unlocked cursor must not yaw either.
            bool cursorLocked = Cursor.lockState == CursorLockMode.Locked;
            bool playLive = Time.timeScale > 0f && cursorLocked;
            bool clingPhys = Input.GetKey(KeyCode.W) || Input.GetAxisRaw("Vertical") > 0.25f;
            bool sprintPhys = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftAlt);
            bool jumpPhys = JumpHeldNow();
            if (!playLive)
            {
                Move = Vector2.zero;
                Look = Vector2.zero;
                LookFromGamepad = false;
                SprintHeld = false;
                CrouchHeld = false;
                CrouchPressed = false;
                JumpHeld = false;
                JumpPressed = false;
                SkiHeld = false;
                JetHeld = false;
                JetPressed = false;
                LungePressed = false;
                AirDashPressed = false;
                PunchPressed = false;
                TapForwardPulse = false;
                // Latch the physical holds. Resume uses this so a released cling or
                // sprint cannot stay down, and the menu's jump cannot fire.
                _menuLatch = MenuHoldGate.WhileOpen(clingPhys, sprintPhys, jumpPhys);
                _resumeGate = true;
                // A hold that started in the menu must not look like a fresh press on resume.
                _prevCrouch = (Input.GetKey(crouchKey) || Input.GetKey(KeyCode.LeftControl)) ? 1f : 0f;
                _prevJump = jumpPhys ? 1f : 0f;
                _prevSpace = SpaceHeld() ? 1f : 0f;
                _kbSpacePrev = _prevSpace;
                _prevJet = (Input.GetKey(jetKey) || Input.GetMouseButton(1)) ? 1f : 0f;
                _prevW = Input.GetKey(tapStrafePulseKey);
                _prevMoveY = Input.GetAxisRaw("Vertical");
                _wasCursorLocked = false;
                PadLookStick = Vector2.zero;
                LookIsStick = false;
                EvasionGestures.Reset(ref _gesture);
                EvasionGestures.ResetTap(ref _stutterTap);
                return;
            }

            if (_resumeGate)
            {
                HoldResult step = MenuHoldGate.OnResume(_menuLatch, clingPhys, sprintPhys, jumpPhys);
                _resumeGate = false;
                _swallowResumeJump = true;
                _clearSprintOnResume = !step.Sprint;
                _clearClingOnResume = !step.Cling;
            }

            // Rising edge: menu/results just released play. Same-frame lock + Read would yaw/punch.
            if (!_wasCursorLocked)
            {
                _lookPunchGateFrames = Mathf.Max(_lookPunchGateFrames, 2);
                ResumeInputGate.Arm();
            }
            _wasCursorLocked = true;
            // Drop leftover menu focus before sampling Space. Jump owns that key in play.
            if (GUIUtility.keyboardControl != 0)
                GUIUtility.keyboardControl = 0;

            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Move.sqrMagnitude > 1f) Move.Normalize();

            Look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            bool w = Input.GetKey(tapStrafePulseKey);
            // W tap, or a stick that snaps forward in one frame. A slow push does not pulse.
            // Same air redirect. Impulse and cooldown stay on the motor.
            bool stickFlick = Move.y > 0.75f && _prevMoveY <= 0.45f;
            TapForwardPulse = (w && !_prevW) || stickFlick;
            _prevW = w;
            _prevMoveY = Move.y;

            CrouchHeld = Input.GetKey(crouchKey) || Input.GetKey(KeyCode.LeftControl);
            CrouchPressed = CrouchHeld && _prevCrouch <= 0f;
            _prevCrouch = CrouchHeld ? 1f : 0f;

            // Same press the gamepad Jump button uses. Space is already bound; a new
            // hold of that key still counts when the legacy axis was already high.
            bool spaceHeld = SpaceHeld();
            bool jumpHeld = Input.GetButton("Jump") || spaceHeld;
            JumpHeld = jumpHeld;
            JumpPressed = KinematicStep.JumpEdge(jumpHeld, spaceHeld, _prevJump > 0f, _prevSpace > 0f);
            _prevJump = jumpHeld ? 1f : 0f;
            _prevSpace = spaceHeld ? 1f : 0f;

            SkiHeld = Input.GetKey(skiKey);
            // Shift may also mean ski; PlayerMotor.WantsSki decides if ski engages.
            // When ski does not engage (flat jog), Shift still counts as sprint so run reads correctly.
            SprintHeld = useShiftAsSprintWhenNotSkiing
                ? Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftAlt)
                : Input.GetKey(KeyCode.LeftAlt);

            // Default: hold RMB / Left Shift+Space feel. Jet is dedicated.
            JetHeld = Input.GetKey(jetKey) || Input.GetMouseButton(1);
            JetPressed = JetHeld && _prevJet <= 0f;
            _prevJet = JetHeld ? 1f : 0f;

            LungePressed = Input.GetKeyDown(lungeKey) || Input.GetMouseButtonDown(2);
            // Q / Left Alt (docs); MMB also counts via LungePressed when airborne in motor.
            AirDashPressed = Input.GetKeyDown(airDashKey) || Input.GetKeyDown(KeyCode.LeftAlt);
            PunchPressed = Input.GetKeyDown(punchKey) || Input.GetKeyDown(KeyCode.E);
            LookFromGamepad = StickLookActive();
            ApplyReboundOverrides();
            OrKeyboardSpace();
            if (_swallowResumeJump)
            {
                JumpPressed = false;
                _swallowResumeJump = false;
                _prevJump = JumpHeld ? 1f : 0f;
                _prevSpace = JumpHeld ? 1f : 0f;
            }
            if (_clearSprintOnResume)
            {
                SprintHeld = false;
                _clearSprintOnResume = false;
            }
            if (_clearClingOnResume)
            {
                if (!clingPhys && Move.y > 0f) Move.y = 0f;
                _clearClingOnResume = false;
            }

            ShapeHumanMove();
            SampleSoloStick();

            if (_lookPunchGateFrames > 0 || ResumeInputGate.Blocking)
            {
                if (_lookPunchGateFrames > 0)
                    _lookPunchGateFrames--;
                SuppressResumeOneShots();
                EvasionGestures.Reset(ref _gesture);
                EvasionGestures.ResetTap(ref _stutterTap);
            }
            else
                ApplyEvasionStick();
        }

        /// <summary>
        /// Drop look and one-shot edges already latched this frame. Holds (move, sprint,
        /// jump-held) stay so resume does not zero locomotion. Safe for AI: no-op when
        /// ExternalControl is set.
        /// </summary>
        public void SuppressResumeOneShots()
        {
            if (ExternalControl) return;
            Look = Vector2.zero;
            CrouchPressed = false;
            JumpPressed = false;
            JetPressed = false;
            LungePressed = false;
            AirDashPressed = false;
            PunchPressed = false;
            TapForwardPulse = false;
            // Re-latch hold edges so a menu hold is not a fresh press next frame.
            _prevCrouch = (Input.GetKey(crouchKey) || Input.GetKey(KeyCode.LeftControl)) ? 1f : 0f;
            _prevJump = JumpHeldNow() ? 1f : 0f;
            _prevSpace = SpaceHeld() ? 1f : 0f;
            _kbSpacePrev = _prevSpace;
            _prevJet = (Input.GetKey(jetKey) || Input.GetMouseButton(1)) ? 1f : 0f;
            _prevW = Input.GetKey(tapStrafePulseKey);
            _prevMoveY = Move.y;
        }

        /// <summary>Optional explicit arm (pause/results clear). Rising-edge lock also arms.</summary>
        public void ArmLookPunchGate(int frames = 2)
        {
            if (frames < 1) frames = 1;
            _lookPunchGateFrames = Mathf.Max(_lookPunchGateFrames, frames);
            ResumeInputGate.Arm();
        }

        /// <summary>AI helper: set planar wish in body space and clear one-shot human buttons.</summary>
        public void SetExternalMove(Vector2 move, bool sprint, bool jump = false, bool lunge = false, bool airDash = false)
        {
            ExternalControl = true;
            Move = move.sqrMagnitude > 1f ? move.normalized : move;
            SprintHeld = sprint;
            Look = Vector2.zero;
            CrouchHeld = false;
            CrouchPressed = false;
            JumpHeld = jump;
            JumpPressed = jump && _extPrevJump <= 0f;
            _extPrevJump = jump ? 1f : 0f;
            SkiHeld = false;
            JetHeld = false;
            JetPressed = false;
            LungePressed = lunge;
            AirDashPressed = airDash;
            PunchPressed = false;
            TapForwardPulse = false;
        }

        void ApplyReboundOverrides()
        {
            ActionBinds binds = ActionBinds.Current;
            if (binds == null) return;
            if (!binds.UsesLegacy(PlayAction.Move))
                Move = BindSampler.MoveVector();
            if (!binds.UsesLegacy(PlayAction.Look))
            {
                Look = BindSampler.LookVector();
                LookFromGamepad = !binds.GamepadIsDefault(PlayAction.Look) || LookFromGamepad;
            }
            if (!binds.UsesLegacy(PlayAction.Jump))
            {
                bool held = BindSampler.Held(PlayAction.Jump);
                JumpHeld = held;
                JumpPressed = held && _prevJump <= 0f;
                _prevJump = held ? 1f : 0f;
                _prevSpace = held ? 1f : 0f;
            }
            if (!binds.UsesLegacy(PlayAction.Slide))
            {
                bool held = BindSampler.Held(PlayAction.Slide);
                CrouchPressed = held && _prevCrouch <= 0f;
                CrouchHeld = held;
                _prevCrouch = held ? 1f : 0f;
            }
            if (!binds.UsesLegacy(PlayAction.AirDash))
                AirDashPressed = BindSampler.Pressed(PlayAction.AirDash) || Input.GetKeyDown(KeyCode.LeftAlt);
            if (!binds.UsesLegacy(PlayAction.Punch))
                PunchPressed = BindSampler.Pressed(PlayAction.Punch) || Input.GetKeyDown(KeyCode.E);
            if (!binds.UsesLegacy(PlayAction.Sprint))
                SprintHeld = BindSampler.Held(PlayAction.Sprint);
            if (!binds.UsesLegacy(PlayAction.Cling) && BindSampler.Held(PlayAction.Cling))
            {
                if (Move.y < 0.85f) Move.y = 1f;
            }
        }

        static bool StickLookActive()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null && pad.rightStick.ReadValue().sqrMagnitude > 0.04f)
                return true;
#endif
            return false;
        }

        void ReadDriven()
        {
            bool live = Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked;
            if (!live || CouchPlay.InputBlockedDevice(DriveDevice))
            {
                Move = Vector2.zero;
                Look = Vector2.zero;
                LookFromGamepad = false;
                SprintHeld = false;
                CrouchHeld = false;
                CrouchPressed = false;
                JumpHeld = false;
                JumpPressed = false;
                SkiHeld = false;
                JetHeld = false;
                JetPressed = false;
                LungePressed = false;
                AirDashPressed = false;
                PunchPressed = false;
                TapForwardPulse = false;
                _couchWasLive = false;
                _kbSpacePrev = SpaceHeld() ? 1f : 0f;
                PadLookStick = Vector2.zero;
                LookIsStick = false;
                EvasionGestures.Reset(ref _gesture);
                EvasionGestures.ResetTap(ref _stutterTap);
                return;
            }

            bool swallow = !_couchWasLive;
            _couchWasLive = true;
            ActionBinds binds = CouchPlay.BindsFor(DriveDevice);
            bool pad = DriveDevice > 0;
            Move = BindSampler.MoveDevice(binds, DriveDevice);
            Look = BindSampler.LookDevice(binds, DriveDevice);
            LookFromGamepad = pad && Look.sqrMagnitude > 0.0004f;
            PadLookStick = pad ? Look : Vector2.zero;
            LookIsStick = pad;

            bool crouch = BindSampler.HeldDevice(binds, PlayAction.Slide, DriveDevice);
            CrouchPressed = crouch && _prevCrouch <= 0f;
            CrouchHeld = crouch;
            _prevCrouch = crouch ? 1f : 0f;

            bool jump = BindSampler.HeldDevice(binds, PlayAction.Jump, DriveDevice);
            JumpHeld = jump;
            JumpPressed = jump && _prevJump <= 0f;
            _prevJump = jump ? 1f : 0f;
            _prevSpace = jump ? 1f : 0f;
            OrKeyboardSpace();

            SprintHeld = BindSampler.HeldDevice(binds, PlayAction.Sprint, DriveDevice);
            AirDashPressed = BindSampler.PressedDevice(binds, PlayAction.AirDash, DriveDevice);
            PunchPressed = BindSampler.PressedDevice(binds, PlayAction.Punch, DriveDevice);
            // Same rope verb. Keyboard seat is RMB. Pad seat is LT. Not a new action.
            JetHeld = pad ? BindSampler.LeftTriggerHeld(DriveDevice) : BindSampler.MouseRightHeld();
            JetPressed = JetHeld && _prevJet <= 0f;
            _prevJet = JetHeld ? 1f : 0f;
            if (BindSampler.HeldDevice(binds, PlayAction.Cling, DriveDevice) && Move.y < 0.85f)
                Move.y = 1f;

            bool forward = Move.y > 0.75f && _prevMoveY <= 0.45f;
            TapForwardPulse = forward;
            _prevMoveY = Move.y;
            ShapeHumanMove();

            if (swallow)
            {
                Look = Vector2.zero;
                JumpPressed = false;
                CrouchPressed = false;
                AirDashPressed = false;
                PunchPressed = false;
                TapForwardPulse = false;
                EvasionGestures.Reset(ref _gesture);
                EvasionGestures.ResetTap(ref _stutterTap);
            }
            else
                ApplyEvasionStick();
        }

        /// <summary>
        /// Keyboard player 1 always jumps on Space, even when Jump was rebound.
        /// A pad seat never samples the keyboard. The edge is button-only.
        /// </summary>
        void OrKeyboardSpace()
        {
            if (DriveDevice > 0) return;
            bool space = SpaceHeld();
            bool edge = space && _kbSpacePrev <= 0f;
            _kbSpacePrev = space ? 1f : 0f;
            if (space) JumpHeld = true;
            if (edge) JumpPressed = true;
        }

        void SampleSoloStick()
        {
            PadLookStick = Vector2.zero;
            LookIsStick = false;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null)
                PadLookStick = pad.rightStick.ReadValue();
#endif
        }

        /// <summary>
        /// Juke, spin, and the stutter double-tap. Only while the evasion flag is on.
        /// The commit sample of a raised stick move replaces stick look.
        /// RT is the stutter. LT, LB, and RB are not read here.
        /// </summary>
        void ApplyEvasionStick()
        {
            if (!EvasionMoves.Enabled)
            {
                EvasionGestures.Reset(ref _gesture);
                EvasionGestures.ResetTap(ref _stutterTap);
                return;
            }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            EvasionGestures.Result step = EvasionGestures.Step(ref _gesture, PadLookStick.x, PadLookStick.y, dt);
            if (step.Commit && EvasionGrounded)
            {
                if (EvasionMoves.TryRaise(EvasionId, step.Kind, step.Sign, EvasionPlanarSpeed, true) && LookIsStick)
                {
                    Look = new Vector2(-step.UndoX, -step.UndoY);
                    LookFromGamepad = true;
                }
            }
            EvasionGestures.TapResult tap = EvasionGestures.StutterTap(
                ref _stutterTap, SampleRightTrigger(), Move.x, EvasionLateral, dt);
            if (!tap.Commit || !EvasionGrounded) return;
            EvasionMoves.TryRaise(EvasionId, EvasionMoves.Kind.Stutter, tap.Sign, EvasionPlanarSpeed, true);
        }

        bool SampleRightTrigger()
        {
            if (DriveDevice > 0) return BindSampler.RightTriggerHeld(DriveDevice);
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null) return pad.rightTrigger.isPressed;
#endif
            return false;
        }

        void ShapeHumanMove()
        {
            if (ExternalControl) return;
            Move = StickQuality.Shape(Move);
        }

        public void ConsumeJumpPress() => JumpPressed = false;

        /// <summary>
        /// Legacy Jump axis (keyboard space and joystick button 3) plus the keyboard
        /// control already bound on Gameplay/Jump in Assets/Input/Tag.inputactions
        /// (Keyboard/space). No second binding.
        /// </summary>
        static bool JumpHeldNow()
        {
            return Input.GetButton("Jump") || SpaceHeld();
        }

        static bool SpaceHeld()
        {
            if (Input.GetKey(KeyCode.Space) || Input.GetKeyDown(KeyCode.Space))
                return true;
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.isPressed || keyboard.spaceKey.wasPressedThisFrame))
                return true;
#endif
            return false;
        }

        public void ConsumeCrouchPress() => CrouchPressed = false;
        public void ConsumeTapPulse() => TapForwardPulse = false;
    }
}
