using Tag.Couch;
using Tag.Onboard;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Ui.Menu
{
    public struct MenuEdge
    {
        public int Device;
        public int X;
        public int Y;
        public bool Confirm;
        public bool Back;
        public bool Start;
        public bool Join;
        public bool ShoulderL;
        public bool ShoulderR;
        public bool North;
    }

    /// <summary>
    /// One edge list per frame. Keyboard is device 0. Pads are devices 1-4.
    /// Each seat can move its own cursor. Gamepad.all is a struct; only Count is read.
    /// </summary>
    public static class MenuInput
    {
        public const int Cap = 5;

        public static readonly MenuEdge[] Edges = new MenuEdge[Cap];
        public static int Count;
        public static InputDeviceKind LastKind = InputDeviceKind.Keyboard;
        public static int LastDevice;

        static float _kbNext;
        static readonly float[] _padNext = new float[4];

        public static void Poll()
        {
            Count = 0;
            PollKeyboard();
            PollPads();
            if (UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetMouseButtonDown(1))
            {
                LastKind = InputDeviceKind.Keyboard;
                MenuAudio.NoteDevice(LastKind);
            }
        }

        public static bool AnyAdvance()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) return true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter))
                return true;
            if (UnityEngine.Input.GetMouseButtonDown(0)) return true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0)) return true;
            for (int i = 0; i < Count; i++)
            {
                if (Edges[i].Confirm || Edges[i].Start || Edges[i].Join) return true;
            }
            return false;
        }

        static void PollKeyboard()
        {
            int x = 0;
            int y = 0;
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || UnityEngine.Input.GetKeyDown(KeyCode.A)) x = -1;
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || UnityEngine.Input.GetKeyDown(KeyCode.D)) x = 1;
            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow) || UnityEngine.Input.GetKeyDown(KeyCode.W)) y = 1;
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.S)) y = -1;
            bool hold = x == 0 && y == 0;
            if (hold && Time.unscaledTime >= _kbNext)
            {
                if (UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.A)) x = -1;
                else if (UnityEngine.Input.GetKey(KeyCode.RightArrow) || UnityEngine.Input.GetKey(KeyCode.D)) x = 1;
                else if (UnityEngine.Input.GetKey(KeyCode.UpArrow) || UnityEngine.Input.GetKey(KeyCode.W)) y = 1;
                else if (UnityEngine.Input.GetKey(KeyCode.DownArrow) || UnityEngine.Input.GetKey(KeyCode.S)) y = -1;
                if (x != 0 || y != 0) _kbNext = Time.unscaledTime + 0.18f;
            }
            else if (!hold)
                _kbNext = Time.unscaledTime + 0.18f;

            bool confirm = UnityEngine.Input.GetKeyDown(KeyCode.Return)
                || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter)
                || UnityEngine.Input.GetKeyDown(KeyCode.Space);
            bool back = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
            bool join = KeyboardJoin();
            bool north = UnityEngine.Input.GetKeyDown(KeyCode.R);
            bool sl = UnityEngine.Input.GetKeyDown(KeyCode.Q);
            bool sr = UnityEngine.Input.GetKeyDown(KeyCode.E);
            if (x == 0 && y == 0 && !confirm && !back && !join && !north && !sl && !sr) return;
            Push(CouchPlay.DeviceKeyboard, x, y, confirm, back, false, join, sl, sr, north);
            LastKind = InputDeviceKind.Keyboard;
            LastDevice = CouchPlay.DeviceKeyboard;
            PadGlyph.Note(CouchPlay.DeviceKeyboard, "");
            MenuAudio.NoteDevice(LastKind);
        }

        static bool KeyboardJoin()
        {
            for (int k = (int)KeyCode.A; k <= (int)KeyCode.Z; k++)
            {
                if (UnityEngine.Input.GetKeyDown((KeyCode)k)) return true;
            }
            for (int k = (int)KeyCode.Alpha0; k <= (int)KeyCode.Alpha9; k++)
            {
                if (UnityEngine.Input.GetKeyDown((KeyCode)k)) return true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift) || UnityEngine.Input.GetKeyDown(KeyCode.RightShift)) return true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftControl) || UnityEngine.Input.GetKeyDown(KeyCode.LeftAlt)) return true;
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab)) return true;
            return false;
        }

        static void PollPads()
        {
#if ENABLE_INPUT_SYSTEM
            int n = Gamepad.all.Count;
            if (n > 4) n = 4;
            for (int i = 0; i < n; i++)
            {
                Gamepad pad = Gamepad.all[i];
                if (pad == null) continue;
                int x = 0;
                int y = 0;
                if (pad.dpad.left.wasPressedThisFrame) x = -1;
                else if (pad.dpad.right.wasPressedThisFrame) x = 1;
                if (pad.dpad.up.wasPressedThisFrame) y = 1;
                else if (pad.dpad.down.wasPressedThisFrame) y = -1;
                Vector2 stick = pad.leftStick.ReadValue();
                if (x == 0 && y == 0 && stick.sqrMagnitude >= 0.30f && Time.unscaledTime >= _padNext[i])
                {
                    if (stick.x < -0.55f) x = -1;
                    else if (stick.x > 0.55f) x = 1;
                    else if (stick.y > 0.55f) y = 1;
                    else if (stick.y < -0.55f) y = -1;
                    if (x != 0 || y != 0) _padNext[i] = Time.unscaledTime + 0.18f;
                }
                else if (stick.sqrMagnitude < 0.16f)
                    _padNext[i] = 0f;

                bool south = pad.buttonSouth.wasPressedThisFrame;
                bool east = pad.buttonEast.wasPressedThisFrame;
                bool start = pad.startButton.wasPressedThisFrame;
                bool north = pad.buttonNorth.wasPressedThisFrame;
                bool sl = pad.leftShoulder.wasPressedThisFrame;
                bool sr = pad.rightShoulder.wasPressedThisFrame;
                bool join = south || north || start || sl || sr
                    || pad.buttonWest.wasPressedThisFrame
                    || pad.selectButton.wasPressedThisFrame
                    || pad.dpad.up.wasPressedThisFrame
                    || pad.dpad.down.wasPressedThisFrame
                    || pad.dpad.left.wasPressedThisFrame
                    || pad.dpad.right.wasPressedThisFrame;
                if (x == 0 && y == 0 && !south && !east && !start && !north && !sl && !sr && !join) continue;
                Push(CouchPlay.DevicePad0 + i, x, y, south, east, start, join, sl, sr, north);
                LastKind = InputDeviceKind.Gamepad;
                LastDevice = CouchPlay.DevicePad0 + i;
                PadGlyph.Note(LastDevice, pad.name);
                MenuAudio.NoteDevice(LastKind);
            }
#else
            int x = 0;
            int y = 0;
            bool south = UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0);
            bool east = UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton1);
            bool start = UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton7);
            bool north = UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton3);
            bool sl = UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton4);
            bool sr = UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton5);
            bool join = south || north || start || sl || sr || UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton2);
            if (x != 0 || y != 0 || south || east || start || north || sl || sr || join)
            {
                Push(CouchPlay.DevicePad0, x, y, south, east, start, join, sl, sr, north);
                LastKind = InputDeviceKind.Gamepad;
                LastDevice = CouchPlay.DevicePad0;
                PadGlyph.Note(LastDevice, "");
                MenuAudio.NoteDevice(LastKind);
            }
#endif
        }

        static void Push(int device, int x, int y, bool confirm, bool back, bool start, bool join, bool sl, bool sr, bool north)
        {
            if (Count >= Cap) return;
            MenuEdge edge;
            edge.Device = device;
            edge.X = x;
            edge.Y = y;
            edge.Confirm = confirm;
            edge.Back = back;
            edge.Start = start;
            edge.Join = join;
            edge.ShoulderL = sl;
            edge.ShoulderR = sr;
            edge.North = north;
            Edges[Count] = edge;
            Count++;
        }
    }
}
