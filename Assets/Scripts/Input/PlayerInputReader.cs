using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Input
{
    /// <summary>
    /// Input wrapper. Prefers New Input System when available; falls back to legacy Input.
    /// Controls: WASD/stick move, Shift sprint, Ctrl/C slide, Space jump, Mouse look, LMB/R punch,
    /// Left Alt/Q or gamepad RB air dodge (airborne only — gated in PlayerMotor).
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] bool lockCursor = true;

        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SlidePressed { get; private set; }
        public bool PunchPressed { get; private set; }
        public bool AirDodgePressed { get; private set; }

        void OnEnable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void Update()
        {
            JumpPressed = false;
            SlidePressed = false;
            PunchPressed = false;
            AirDodgePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null || Gamepad.current != null || Mouse.current != null)
            {
                ReadNewInput();
                return;
            }
#endif
            ReadLegacyInput();
        }

#if ENABLE_INPUT_SYSTEM
        void ReadNewInput()
        {
            Vector2 move = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) move.y += 1f;
                if (Keyboard.current.sKey.isPressed) move.y -= 1f;
                if (Keyboard.current.aKey.isPressed) move.x -= 1f;
                if (Keyboard.current.dKey.isPressed) move.x += 1f;
                SprintHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
                bool jumpDown = Keyboard.current.spaceKey.wasPressedThisFrame;
                bool slideDown = Keyboard.current.leftCtrlKey.wasPressedThisFrame
                                 || Keyboard.current.cKey.wasPressedThisFrame;
                bool airDodgeDown = Keyboard.current.leftAltKey.wasPressedThisFrame
                                    || Keyboard.current.qKey.wasPressedThisFrame;
                if (jumpDown) JumpPressed = true;
                if (slideDown) SlidePressed = true;
                if (airDodgeDown) AirDodgePressed = true;
            }
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > move.sqrMagnitude) move = stick;
                SprintHeld = SprintHeld || Gamepad.current.leftStickButton.isPressed || Gamepad.current.leftShoulder.isPressed;
                if (Gamepad.current.buttonSouth.wasPressedThisFrame) JumpPressed = true;
                if (Gamepad.current.buttonEast.wasPressedThisFrame) SlidePressed = true;
                if (Gamepad.current.buttonWest.wasPressedThisFrame) PunchPressed = true;
                if (Gamepad.current.rightShoulder.wasPressedThisFrame) AirDodgePressed = true;
                LookDelta = Gamepad.current.rightStick.ReadValue() * 8f;
            }
            else
            {
                LookDelta = Vector2.zero;
            }
            if (Mouse.current != null)
            {
                LookDelta += Mouse.current.delta.ReadValue() * 0.1f;
                if (Mouse.current.leftButton.wasPressedThisFrame) PunchPressed = true;
            }
            Move = Vector2.ClampMagnitude(move, 1f);
        }
#endif

        void ReadLegacyInput()
        {
            Move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
            Move = Vector2.ClampMagnitude(Move, 1f);
            LookDelta = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
            SprintHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
            JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space);
            SlidePressed = UnityEngine.Input.GetKeyDown(KeyCode.LeftControl) || UnityEngine.Input.GetKeyDown(KeyCode.C);
            PunchPressed = UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetKeyDown(KeyCode.R);
            AirDodgePressed = UnityEngine.Input.GetKeyDown(KeyCode.LeftAlt) || UnityEngine.Input.GetKeyDown(KeyCode.Q);
        }
    }
}
