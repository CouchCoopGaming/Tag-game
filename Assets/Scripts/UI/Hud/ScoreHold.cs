using Tag.Couch;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Ui.Hud
{
    /// <summary>
    /// Tab on the keyboard, Select on a pad. Gamepad.all is a struct.
    /// </summary>
    public static class ScoreHold
    {
        public static bool Down(int device)
        {
            if (device < 0) return false;
            if (device == CouchPlay.DeviceKeyboard)
                return UnityEngine.Input.GetKey(KeyCode.Tab);
#if ENABLE_INPUT_SYSTEM
            int i = device - CouchPlay.DevicePad0;
            int n = Gamepad.all.Count;
            if (i < 0 || i >= n) return false;
            Gamepad pad = Gamepad.all[i];
            if (pad == null) return false;
            return pad.selectButton.isPressed;
#else
            return false;
#endif
        }
    }
}
