using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Input
{
    /// <summary>
    /// Per-local-player input. playerIndex 0..3.
    /// P0: WASD+mouse / gamepad0 · P1: arrows+RCTRL punch / gamepad1 · P2/P3: gamepads.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] int playerIndex;
        [SerializeField] bool lockCursor = true;

        public int PlayerIndex { get => playerIndex; set => playerIndex = value; }
        public Vector2 Move { get; private set; }
        public Vector2 LookDelta { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SlidePressed { get; private set; }
        public bool PunchPressed { get; private set; }
        public bool AirDodgePressed { get; private set; }

        void OnEnable()
        {
            if (lockCursor && playerIndex == 0)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            if (lockCursor && playerIndex == 0)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void Update()
        {
            JumpPressed = SlidePressed = PunchPressed = AirDodgePressed = false;
            LookDelta = Vector2.zero;
            Move = Vector2.zero;
            SprintHeld = false;
#if ENABLE_INPUT_SYSTEM
            ReadNewInput();
#else
            ReadLegacyInput();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        void ReadNewInput()
        {
            // Gamepad slot
            Gamepad pad = null;
            if (Gamepad.all.Count > playerIndex)
                pad = Gamepad.all[playerIndex];

            if (playerIndex == 0 && Keyboard.current != null)
            {
                Vector2 move = Vector2.zero;
                if (Keyboard.current.wKey.isPressed) move.y += 1f;
                if (Keyboard.current.sKey.isPressed) move.y -= 1f;
                if (Keyboard.current.aKey.isPressed) move.x -= 1f;
                if (Keyboard.current.dKey.isPressed) move.x += 1f;
                Move = move;
                SprintHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
                if (Keyboard.current.spaceKey.wasPressedThisFrame) JumpPressed = true;
                if (Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.cKey.wasPressedThisFrame) SlidePressed = true;
                if (Keyboard.current.leftAltKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame) AirDodgePressed = true;
                if (Mouse.current != null)
                {
                    LookDelta += Mouse.current.delta.ReadValue() * 0.1f;
                    if (Mouse.current.leftButton.wasPressedThisFrame) PunchPressed = true;
                }
            }
            else if (playerIndex == 1 && Keyboard.current != null)
            {
                Vector2 move = Vector2.zero;
                if (Keyboard.current.upArrowKey.isPressed) move.y += 1f;
                if (Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
                if (Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
                if (Keyboard.current.rightArrowKey.isPressed) move.x += 1f;
                Move = move;
                SprintHeld = Keyboard.current.rightShiftKey.isPressed;
                if (Keyboard.current.rightCtrlKey.wasPressedThisFrame) JumpPressed = true;
                if (Keyboard.current.slashKey.wasPressedThisFrame || Keyboard.current.periodKey.wasPressedThisFrame) SlidePressed = true;
                if (Keyboard.current.rightAltKey.wasPressedThisFrame || Keyboard.current.quoteKey.wasPressedThisFrame) AirDodgePressed = true;
                if (Keyboard.current.rightBracketKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame) PunchPressed = true;
                // look: IJKL
                Vector2 look = Vector2.zero;
                if (Keyboard.current.iKey.isPressed) look.y += 1f;
                if (Keyboard.current.kKey.isPressed) look.y -= 1f;
                if (Keyboard.current.jKey.isPressed) look.x -= 1f;
                if (Keyboard.current.lKey.isPressed) look.x += 1f;
                LookDelta += look * 2.5f;
            }

            if (pad != null)
            {
                Vector2 stick = pad.leftStick.ReadValue();
                if (stick.sqrMagnitude > Move.sqrMagnitude) Move = stick;
                SprintHeld = SprintHeld || pad.leftStickButton.isPressed || pad.leftShoulder.isPressed;
                if (pad.buttonSouth.wasPressedThisFrame) JumpPressed = true;
                if (pad.buttonEast.wasPressedThisFrame) SlidePressed = true;
                if (pad.buttonWest.wasPressedThisFrame) PunchPressed = true;
                if (pad.rightShoulder.wasPressedThisFrame) AirDodgePressed = true;
                LookDelta += pad.rightStick.ReadValue() * 8f;
            }

            Move = Vector2.ClampMagnitude(Move, 1f);
        }
#endif

        void ReadLegacyInput()
        {
            if (playerIndex == 0)
            {
                Move = new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
                LookDelta = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
                SprintHeld = UnityEngine.Input.GetKey(KeyCode.LeftShift);
                JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space);
                SlidePressed = UnityEngine.Input.GetKeyDown(KeyCode.LeftControl) || UnityEngine.Input.GetKeyDown(KeyCode.C);
                PunchPressed = UnityEngine.Input.GetMouseButtonDown(0);
                AirDodgePressed = UnityEngine.Input.GetKeyDown(KeyCode.LeftAlt) || UnityEngine.Input.GetKeyDown(KeyCode.Q);
            }
            else if (playerIndex == 1)
            {
                float x = 0, y = 0;
                if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) x += 1;
                if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) x -= 1;
                if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) y += 1;
                if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) y -= 1;
                Move = new Vector2(x, y);
                JumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.RightControl);
                PunchPressed = UnityEngine.Input.GetKeyDown(KeyCode.Return);
            }
            Move = Vector2.ClampMagnitude(Move, 1f);
        }
    }
}
