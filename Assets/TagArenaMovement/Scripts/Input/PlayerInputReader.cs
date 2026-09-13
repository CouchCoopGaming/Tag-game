using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Thin input adapter. Swap the body of Read() if you migrate to the new Input System.
    /// Keep raw + consumed flags separate so buffering (jump) is deterministic.
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
        public bool PunchPressed;
        public bool TapForwardPulse;

        [Header("Legacy key map")]
        public KeyCode skiKey = KeyCode.LeftShift;
        public KeyCode jetKey = KeyCode.Mouse1;
        public KeyCode crouchKey = KeyCode.C;
        public KeyCode lungeKey = KeyCode.Mouse2;
        public KeyCode punchKey = KeyCode.Mouse0;
        public KeyCode tapStrafePulseKey = KeyCode.W;
        public bool useShiftAsSprintWhenNotSkiing = true;

        float _prevCrouch;
        float _prevJump;
        float _prevJet;
        float _prevLunge;
        bool _prevW;

        public void Read()
        {
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
            SprintHeld = useShiftAsSprintWhenNotSkiing
                ? (Input.GetKey(KeyCode.LeftShift) && !SkiHeld) || Input.GetKey(KeyCode.LeftAlt)
                : Input.GetKey(KeyCode.LeftAlt);

            // Default: hold RMB / Left Shift+Space feel. Jet is dedicated.
            JetHeld = Input.GetKey(jetKey) || Input.GetMouseButton(1);
            JetPressed = JetHeld && _prevJet <= 0f;
            _prevJet = JetHeld ? 1f : 0f;

            LungePressed = Input.GetKeyDown(lungeKey) || Input.GetMouseButtonDown(2);
            PunchPressed = Input.GetKeyDown(punchKey) || Input.GetKeyDown(KeyCode.E);
        }

        public void ConsumeJumpPress() => JumpPressed = false;
        public void ConsumeCrouchPress() => CrouchPressed = false;
        public void ConsumeTapPulse() => TapForwardPulse = false;
    }
}
