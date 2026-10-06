using Tag.Front;
using Tag.Settings;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Couch
{
    /// <summary>
    /// Join-screen edges. A keyboard key seats the one keyboard player.
    /// Any button on a pad seats that pad. Back on a device leaves it.
    /// Menu confirm stays on Enter, Space, and South once that device is seated.
    /// </summary>
    public static class CouchDevices
    {
        public static bool EatBack;
        public static bool EatConfirm;

        /// <summary>Play and pause only. A pad that disappears keeps its seat and asks to rejoin.</summary>
        public static void PollHotplug()
        {
#if ENABLE_INPUT_SYSTEM
            if (FrontSession.Screen == FrontScreen.Join) return;
            var pads = Gamepad.all;
            int n = pads == null ? 0 : pads.Count;
            if (n > 4) n = 4;
            for (int i = 0; i < 4; i++)
            {
                int device = CouchPlay.DevicePad0 + i;
                if (!CouchPlay.Joined(device)) continue;
                bool present = pads != null && i < n && pads[i] != null;
                if (!present) CouchPlay.NoteLost(device);
                else CouchPlay.NoteFound(device);
            }
#endif
        }

        public static void Poll()
        {
            EatBack = false;
            EatConfirm = false;
            if (FrontSession.Screen != FrontScreen.Join) return;
            if (KeyboardJoinEdge())
                CouchPlay.Join(CouchPlay.DeviceKeyboard);
            if (Input.GetKeyDown(KeyCode.Escape) && CouchPlay.Joined(CouchPlay.DeviceKeyboard))
            {
                CouchPlay.Leave(CouchPlay.DeviceKeyboard);
                EatBack = true;
            }
            PollPads();
        }

        static bool KeyboardJoinEdge()
        {
            for (int k = (int)KeyCode.A; k <= (int)KeyCode.Z; k++)
            {
                if (Input.GetKeyDown((KeyCode)k)) return true;
            }
            for (int k = (int)KeyCode.Alpha0; k <= (int)KeyCode.Alpha9; k++)
            {
                if (Input.GetKeyDown((KeyCode)k)) return true;
            }
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) return true;
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftAlt)) return true;
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E)) return true;
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.C)) return true;
            if (Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.P)) return true;
            return false;
        }

        static void PollPads()
        {
#if ENABLE_INPUT_SYSTEM
            var pads = Gamepad.all;
            int n = pads.Count;
            if (n > 4) n = 4;
            for (int i = 0; i < n; i++)
            {
                Gamepad pad = pads[i];
                if (pad == null) continue;
                int device = CouchPlay.DevicePad0 + i;
                if (pad.buttonEast.wasPressedThisFrame)
                {
                    if (CouchPlay.Joined(device))
                    {
                        CouchPlay.Leave(device);
                        EatBack = true;
                    }
                    continue;
                }
                bool any = pad.buttonSouth.wasPressedThisFrame
                    || pad.buttonWest.wasPressedThisFrame
                    || pad.buttonNorth.wasPressedThisFrame
                    || pad.leftShoulder.wasPressedThisFrame
                    || pad.rightShoulder.wasPressedThisFrame
                    || pad.startButton.wasPressedThisFrame
                    || pad.selectButton.wasPressedThisFrame
                    || pad.dpad.up.wasPressedThisFrame
                    || pad.dpad.down.wasPressedThisFrame
                    || pad.dpad.left.wasPressedThisFrame
                    || pad.dpad.right.wasPressedThisFrame;
                if (!any) continue;
                if (!CouchPlay.Joined(device))
                {
                    CouchPlay.Join(device);
                    if (pad.buttonSouth.wasPressedThisFrame) EatConfirm = true;
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.JoystickButton1) && CouchPlay.Joined(CouchPlay.DevicePad0))
            {
                CouchPlay.Leave(CouchPlay.DevicePad0);
                EatBack = true;
            }
            else if (PadPressed() && !CouchPlay.Joined(CouchPlay.DevicePad0))
            {
                CouchPlay.Join(CouchPlay.DevicePad0);
                if (Input.GetKeyDown(KeyCode.JoystickButton0)) EatConfirm = true;
            }
#endif
        }

        static bool PadPressed()
        {
            return Input.GetKeyDown(KeyCode.JoystickButton0)
                || Input.GetKeyDown(KeyCode.JoystickButton2)
                || Input.GetKeyDown(KeyCode.JoystickButton3)
                || Input.GetKeyDown(KeyCode.JoystickButton4)
                || Input.GetKeyDown(KeyCode.JoystickButton5)
                || Input.GetKeyDown(KeyCode.JoystickButton6)
                || Input.GetKeyDown(KeyCode.JoystickButton7);
        }
    }
}
