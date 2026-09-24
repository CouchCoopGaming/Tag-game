using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Thin input adapter. Swap the body of Read() if you migrate to the new Input System.
    /// Keep raw + consumed flags separate so buffering (jump) is deterministic.
    /// When ExternalControl is true (AI), Read() leaves fields alone so DummyPatrol can drive them.
    /// </summary>
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

        /// <summary>When true, Read() is a no-op — AI / tests own Move/Look/buttons.</summary>
        public bool ExternalControl;

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
        float _prevJet;
        float _prevLunge;
        bool _prevW;
        float _extPrevJump;

        void Awake()
        {
            ControlBinds.Load();
            airDashKey = ControlBinds.AirDash;
        }

        public void Read()
        {
            if (ExternalControl) return;

            // Pause freezes the clock but Update still runs. A menu click is Mouse0,
            // which is also punch, and look is not scaled by deltaTime.
            if (Time.timeScale <= 0f)
            {
                Move = Vector2.zero;
                Look = Vector2.zero;
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
                // A hold that started in the menu must not look like a fresh press on resume.
                _prevCrouch = (Input.GetKey(crouchKey) || Input.GetKey(KeyCode.LeftControl)) ? 1f : 0f;
                _prevJump = (Input.GetButton("Jump") || Input.GetKey(KeyCode.Space)) ? 1f : 0f;
                _prevJet = (Input.GetKey(jetKey) || Input.GetMouseButton(1)) ? 1f : 0f;
                _prevW = Input.GetKey(tapStrafePulseKey);
                return;
            }

            airDashKey = ControlBinds.AirDash;

            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (Move.sqrMagnitude > 1f) Move.Normalize();

            Look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            bool w = Input.GetKey(tapStrafePulseKey);
            TapForwardPulse = w && !_prevW;
            _prevW = w;

            CrouchHeld = Input.GetKey(crouchKey) || Input.GetKey(KeyCode.LeftControl);
            CrouchPressed = CrouchHeld && _prevCrouch <= 0f;
            _prevCrouch = CrouchHeld ? 1f : 0f;

            JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.Space);
            JumpPressed = JumpHeld && _prevJump <= 0f;
            _prevJump = JumpHeld ? 1f : 0f;

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
        }

        /// <summary>AI helper: set planar wish in body space and clear one-shot human buttons.</summary>
        public void SetExternalMove(Vector2 move, bool sprint, bool jump = false, bool lunge = false)
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
            AirDashPressed = false;
            PunchPressed = false;
            TapForwardPulse = false;
        }

        public void ConsumeJumpPress() => JumpPressed = false;
        public void ConsumeCrouchPress() => CrouchPressed = false;
        public void ConsumeTapPulse() => TapForwardPulse = false;
    }
}
