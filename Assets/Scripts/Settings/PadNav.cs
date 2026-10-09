using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Settings
{
    /// <summary>
    /// One gamepad read per poll. D-pad and a flicked left stick are menu events.
    /// South confirms, East goes back, Start pauses, Select is the minimap button.
    /// </summary>
    public static class PadNav
    {
        public static bool Up { get; private set; }
        public static bool Down { get; private set; }
        public static bool Left { get; private set; }
        public static bool Right { get; private set; }
        public static bool Confirm { get; private set; }
        public static bool Back { get; private set; }
        public static bool Start { get; private set; }
        public static bool Select { get; private set; }

        /// <summary>1–4 when that pad pressed Start. 0 when the edge was the keyboard.</summary>
        public static int StartDevice;

        static float _nextStick;

        public static void ResetStatics()
        {
            Up = Down = Left = Right = Confirm = Back = Start = Select = false;
            StartDevice = 0;
            _nextStick = 0f;
        }

        public static void Poll()
        {
            Up = Down = Left = Right = Confirm = Back = Start = Select = false;
            StartDevice = 0;
#if ENABLE_INPUT_SYSTEM
            // current only sees whichever pad the runtime last touched, so P2–P4
            // Start never paused the table or opened that seat's settings.
            var pads = Gamepad.all;
            if (pads.Count > 0)
            {
                int n = pads.Count;
                for (int i = 0; i < n; i++)
                {
                    Gamepad pad = pads[i];
                    if (pad == null) continue;
                    if (pad.dpad.up.wasPressedThisFrame) Up = true;
                    if (pad.dpad.down.wasPressedThisFrame) Down = true;
                    if (pad.dpad.left.wasPressedThisFrame) Left = true;
                    if (pad.dpad.right.wasPressedThisFrame) Right = true;
                    if (pad.buttonSouth.wasPressedThisFrame) Confirm = true;
                    if (pad.buttonEast.wasPressedThisFrame) Back = true;
                    if (pad.startButton.wasPressedThisFrame)
                    {
                        Start = true;
                        if (StartDevice == 0) StartDevice = i + 1;
                    }
                    if (pad.selectButton.wasPressedThisFrame) Select = true;
                    Vector2 stick = pad.leftStick.ReadValue();
                    if (stick.sqrMagnitude < 0.16f)
                        _nextStick = 0f;
                    else if (Time.unscaledTime >= _nextStick)
                    {
                        if (stick.y > 0.55f) Up = true;
                        else if (stick.y < -0.55f) Down = true;
                        else if (stick.x < -0.55f) Left = true;
                        else if (stick.x > 0.55f) Right = true;
                        if (Up || Down || Left || Right)
                            _nextStick = Time.unscaledTime + 0.18f;
                    }
                }
            }
#endif
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0)) Confirm = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton1)) Back = true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton7))
            {
                Start = true;
                if (StartDevice == 0) StartDevice = 1;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton6)) Select = true;
        }
    }
}
