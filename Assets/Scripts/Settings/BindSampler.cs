using Tag.Couch;
using Tag.Practice;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Tag.Settings
{
    /// <summary>
    /// Reads the tokens in <see cref="ActionBinds"/>. The legacy WASD / Space /
    /// mouse path stays in PlayerInputReader until a token is no longer the default.
    /// </summary>
    public static class BindSampler
    {
        public static bool Held(PlayAction action)
        {
            var binds = CouchPlay.BindsFor(CouchPlay.DeviceKeyboard);
            int i = (int)action;
            return KeyboardHit(binds, action, i, false) || HeldToken(binds.Gamepad[i], true);
        }

        public static bool Pressed(PlayAction action)
        {
            var binds = CouchPlay.BindsFor(CouchPlay.DeviceKeyboard);
            int i = (int)action;
            return KeyboardHit(binds, action, i, true) || PressedToken(binds.Gamepad[i], true);
        }

        /// <summary>
        /// The existing grapple verb. Keyboard token, then the first pad's token.
        /// A second press inside the grapple window releases. This method only reports the hold.
        /// </summary>
        public static bool GrappleHeld()
        {
            ActionBinds keys = CouchPlay.BindsFor(CouchPlay.DeviceKeyboard);
            if (keys == null) keys = ActionBinds.Defaults();
            string key = keys.GrappleKey;
            if (string.IsNullOrEmpty(key)) key = ActionBinds.GrappleKeyDefault;
            if (HeldToken(key, false)) return true;
            ActionBinds pad = CouchPlay.BindsFor(CouchPlay.DevicePad0);
            if (pad == null) pad = keys;
            string trigger = pad.GrapplePad;
            if (string.IsNullOrEmpty(trigger)) trigger = ActionBinds.GrapplePadDefault;
            return HeldToken(trigger, true);
        }

        public static Vector2 MoveVector()
        {
            var binds = CouchPlay.BindsFor(CouchPlay.DeviceKeyboard);
            Vector2 v = Vector2.zero;
            string kb = binds.Keyboard[(int)PlayAction.Move];
            if (kb == "wasd")
            {
                if (UnityEngine.Input.GetKey(KeyCode.D)) v.x += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.A)) v.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.W)) v.y += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S)) v.y -= 1f;
            }
            else if (kb == "arrows")
            {
                if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
            }
            v += PadMove(binds.Gamepad[(int)PlayAction.Move]);
            if (v.sqrMagnitude > 1f) v.Normalize();
            return v;
        }

        public static bool PracticeRestartDown()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.T)) return true;
            if (!PracticeInput.PadLive(PracticePadPlugged(), CouchPlay.NeedsRejoin))
                return false;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null && pad.buttonNorth.wasPressedThisFrame) return true;
#endif
            return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton3);
        }

        public static bool PracticeGhostDown()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.G)) return true;
            if (!PracticeInput.PadLive(PracticePadPlugged(), CouchPlay.NeedsRejoin))
                return false;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null && pad.leftStickButton.wasPressedThisFrame) return true;
#endif
            return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton8);
        }

        public static bool PracticeInputDown()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.I)) return true;
            if (!PracticeInput.PadLive(PracticePadPlugged(), CouchPlay.NeedsRejoin))
                return false;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null && pad.rightStickButton.wasPressedThisFrame) return true;
#endif
            return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton9);
        }

        static bool PracticePadPlugged()
        {
#if ENABLE_INPUT_SYSTEM
            return Gamepad.current != null;
#else
            return false;
#endif
        }

        public static Vector2 LookVector()
        {
            var binds = CouchPlay.BindsFor(CouchPlay.DeviceKeyboard);
            Vector2 v = Vector2.zero;
            if (binds.Keyboard[(int)PlayAction.Look] == "mouse")
                v = new Vector2(UnityEngine.Input.GetAxisRaw("Mouse X"), UnityEngine.Input.GetAxisRaw("Mouse Y"));
            v += PadLook(binds.Gamepad[(int)PlayAction.Look]);
            return v;
        }

        /// <summary>
        /// Token of the control that went down this frame, or null.
        /// F3 stays Trail Tag and F6 stays the frame overlay, so neither is sampled here.
        /// </summary>
        public static string AnyPressedToken()
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.buttonSouth.wasPressedThisFrame) return "buttonSouth";
                if (pad.buttonEast.wasPressedThisFrame) return "buttonEast";
                if (pad.buttonWest.wasPressedThisFrame) return "buttonWest";
                if (pad.buttonNorth.wasPressedThisFrame) return "buttonNorth";
                if (pad.leftShoulder.wasPressedThisFrame) return "leftShoulder";
                if (pad.rightShoulder.wasPressedThisFrame) return "rightShoulder";
                if (pad.leftTrigger.wasPressedThisFrame) return "leftTrigger";
                if (pad.rightTrigger.wasPressedThisFrame) return "rightTrigger";
                if (pad.leftStickButton.wasPressedThisFrame) return "leftStickPress";
                if (pad.rightStickButton.wasPressedThisFrame) return "rightStickPress";
                if (pad.startButton.wasPressedThisFrame) return "start";
                if (pad.selectButton.wasPressedThisFrame) return "select";
                if (pad.dpad.left.wasPressedThisFrame) return "dpadLeft";
                if (pad.dpad.right.wasPressedThisFrame) return "dpadRight";
                if (pad.dpad.up.wasPressedThisFrame) return "dpadUp";
                if (pad.dpad.down.wasPressedThisFrame) return "dpadDown";
            }
#endif
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0)) return "buttonSouth";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton1)) return "buttonEast";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton2)) return "buttonWest";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton3)) return "buttonNorth";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton4)) return "leftShoulder";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton5)) return "rightShoulder";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton6)) return "select";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton7)) return "start";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton8)) return "leftStickPress";
            if (UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton9)) return "rightStickPress";

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) return "space";
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift)) return "leftShift";
            if (UnityEngine.Input.GetKeyDown(KeyCode.RightShift)) return "rightShift";
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftControl)) return "leftCtrl";
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftAlt)) return "leftAlt";
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) return "escape";
            if (UnityEngine.Input.GetKeyDown(KeyCode.Comma)) return "comma";
            if (UnityEngine.Input.GetKeyDown(KeyCode.Q)) return "q";
            if (UnityEngine.Input.GetKeyDown(KeyCode.E)) return "e";
            if (UnityEngine.Input.GetKeyDown(KeyCode.C)) return "c";
            if (UnityEngine.Input.GetKeyDown(KeyCode.V)) return "v";
            if (UnityEngine.Input.GetKeyDown(KeyCode.F)) return "f";
            if (UnityEngine.Input.GetKeyDown(KeyCode.R)) return "r";
            if (UnityEngine.Input.GetKeyDown(KeyCode.M)) return "m";
            if (UnityEngine.Input.GetKeyDown(KeyCode.N)) return "n";
            if (UnityEngine.Input.GetKeyDown(KeyCode.P)) return "p";
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab)) return "tab";
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) return "alpha1";
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) return "alpha2";
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) return "alpha3";
            if (UnityEngine.Input.GetMouseButtonDown(0)) return "mouseLeft";
            if (UnityEngine.Input.GetMouseButtonDown(1)) return "mouseRight";
            if (UnityEngine.Input.GetMouseButtonDown(2)) return "mouseMiddle";
            return null;
        }

        public static bool PadButtonDown()
        {
            string token = AnyPressedToken();
            if (string.IsNullOrEmpty(token)) return false;
            return token == "buttonSouth" || token == "buttonEast" || token == "buttonWest"
                || token == "buttonNorth" || token == "leftShoulder" || token == "rightShoulder"
                || token == "start" || token == "select" || token == "leftTrigger" || token == "rightTrigger"
                || token.StartsWith("dpad")
                || token.EndsWith("Press");
        }

        static bool HeldToken(string token, bool pad)
        {
            if (string.IsNullOrEmpty(token) || ActionBinds.SharesMove(token)) return false;
            if (pad) return PadHeld(token);
            switch (token)
            {
                case "space": return UnityEngine.Input.GetKey(KeyCode.Space);
                case "leftShift": return UnityEngine.Input.GetKey(KeyCode.LeftShift);
                case "rightShift": return UnityEngine.Input.GetKey(KeyCode.RightShift);
                case "leftCtrl": return UnityEngine.Input.GetKey(KeyCode.LeftControl);
                case "leftAlt": return UnityEngine.Input.GetKey(KeyCode.LeftAlt);
                case "escape": return UnityEngine.Input.GetKey(KeyCode.Escape);
                case "q": return UnityEngine.Input.GetKey(KeyCode.Q);
                case "e": return UnityEngine.Input.GetKey(KeyCode.E);
                case "c": return UnityEngine.Input.GetKey(KeyCode.C);
                case "v": return UnityEngine.Input.GetKey(KeyCode.V);
                case "f": return UnityEngine.Input.GetKey(KeyCode.F);
                case "m": return UnityEngine.Input.GetKey(KeyCode.M);
                case "r": return UnityEngine.Input.GetKey(KeyCode.R);
                case "p": return UnityEngine.Input.GetKey(KeyCode.P);
                case "tab": return UnityEngine.Input.GetKey(KeyCode.Tab);
                case "alpha1": return UnityEngine.Input.GetKey(KeyCode.Alpha1);
                case "alpha2": return UnityEngine.Input.GetKey(KeyCode.Alpha2);
                case "alpha3": return UnityEngine.Input.GetKey(KeyCode.Alpha3);
                case "mouseLeft": return UnityEngine.Input.GetMouseButton(0);
                case "mouseRight": return UnityEngine.Input.GetMouseButton(1);
                case "mouseMiddle": return UnityEngine.Input.GetMouseButton(2);
                default: return false;
            }
        }

        static bool PressedToken(string token, bool pad)
        {
            if (string.IsNullOrEmpty(token) || ActionBinds.SharesMove(token)) return false;
            if (pad) return PadPressed(token);
            switch (token)
            {
                case "space": return UnityEngine.Input.GetKeyDown(KeyCode.Space);
                case "leftShift": return UnityEngine.Input.GetKeyDown(KeyCode.LeftShift);
                case "leftCtrl": return UnityEngine.Input.GetKeyDown(KeyCode.LeftControl);
                case "leftAlt": return UnityEngine.Input.GetKeyDown(KeyCode.LeftAlt);
                case "escape": return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
                case "q": return UnityEngine.Input.GetKeyDown(KeyCode.Q);
                case "e": return UnityEngine.Input.GetKeyDown(KeyCode.E);
                case "c": return UnityEngine.Input.GetKeyDown(KeyCode.C);
                case "v": return UnityEngine.Input.GetKeyDown(KeyCode.V);
                case "f": return UnityEngine.Input.GetKeyDown(KeyCode.F);
                case "m": return UnityEngine.Input.GetKeyDown(KeyCode.M);
                case "alpha1": return UnityEngine.Input.GetKeyDown(KeyCode.Alpha1);
                case "alpha2": return UnityEngine.Input.GetKeyDown(KeyCode.Alpha2);
                case "alpha3": return UnityEngine.Input.GetKeyDown(KeyCode.Alpha3);
                case "mouseLeft": return UnityEngine.Input.GetMouseButtonDown(0);
                case "mouseRight": return UnityEngine.Input.GetMouseButtonDown(1);
                case "mouseMiddle": return UnityEngine.Input.GetMouseButtonDown(2);
                case "p": return UnityEngine.Input.GetKeyDown(KeyCode.P);
                case "tab": return UnityEngine.Input.GetKeyDown(KeyCode.Tab);
                case "r": return UnityEngine.Input.GetKeyDown(KeyCode.R);
                default: return false;
            }
        }

        static bool PadHeld(string token)
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null)
            {
                switch (token)
                {
                    case "buttonSouth": return pad.buttonSouth.isPressed;
                    case "buttonEast": return pad.buttonEast.isPressed;
                    case "buttonWest": return pad.buttonWest.isPressed;
                    case "buttonNorth": return pad.buttonNorth.isPressed;
                    case "leftShoulder": return pad.leftShoulder.isPressed;
                    case "rightShoulder": return pad.rightShoulder.isPressed;
                    case "leftTrigger": return pad.leftTrigger.isPressed;
                    case "rightTrigger": return pad.rightTrigger.isPressed;
                    case "start": return pad.startButton.isPressed;
                    case "select": return pad.selectButton.isPressed;
                    case "dpadLeft": return pad.dpad.left.isPressed;
                    case "dpadRight": return pad.dpad.right.isPressed;
                    case "dpadUp": return pad.dpad.up.isPressed;
                    case "dpadDown": return pad.dpad.down.isPressed;
                }
            }
#endif
            switch (token)
            {
                case "buttonSouth": return UnityEngine.Input.GetKey(KeyCode.JoystickButton0);
                case "buttonEast": return UnityEngine.Input.GetKey(KeyCode.JoystickButton1);
                case "buttonWest": return UnityEngine.Input.GetKey(KeyCode.JoystickButton2);
                case "buttonNorth": return UnityEngine.Input.GetKey(KeyCode.JoystickButton3);
                case "leftShoulder": return UnityEngine.Input.GetKey(KeyCode.JoystickButton4);
                case "rightShoulder": return UnityEngine.Input.GetKey(KeyCode.JoystickButton5);
                case "select": return UnityEngine.Input.GetKey(KeyCode.JoystickButton6);
                case "start": return UnityEngine.Input.GetKey(KeyCode.JoystickButton7);
                default: return false;
            }
        }

        static bool PadPressed(string token)
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null)
            {
                switch (token)
                {
                    case "buttonSouth": return pad.buttonSouth.wasPressedThisFrame;
                    case "buttonEast": return pad.buttonEast.wasPressedThisFrame;
                    case "buttonWest": return pad.buttonWest.wasPressedThisFrame;
                    case "buttonNorth": return pad.buttonNorth.wasPressedThisFrame;
                    case "leftShoulder": return pad.leftShoulder.wasPressedThisFrame;
                    case "rightShoulder": return pad.rightShoulder.wasPressedThisFrame;
                    case "leftTrigger": return pad.leftTrigger.wasPressedThisFrame;
                    case "rightTrigger": return pad.rightTrigger.wasPressedThisFrame;
                    case "start": return pad.startButton.wasPressedThisFrame;
                    case "select": return pad.selectButton.wasPressedThisFrame;
                    case "dpadLeft": return pad.dpad.left.wasPressedThisFrame;
                    case "dpadRight": return pad.dpad.right.wasPressedThisFrame;
                    case "dpadUp": return pad.dpad.up.wasPressedThisFrame;
                    case "dpadDown": return pad.dpad.down.wasPressedThisFrame;
                }
            }
#endif
            switch (token)
            {
                case "buttonSouth": return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton0);
                case "buttonEast": return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton1);
                case "buttonWest": return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton2);
                case "buttonNorth": return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton3);
                case "leftShoulder": return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton4);
                case "rightShoulder": return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton5);
                case "select": return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton6);
                case "start": return UnityEngine.Input.GetKeyDown(KeyCode.JoystickButton7);
                default: return false;
            }
        }

        static Vector2 PadMove(string token)
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (token == "leftStick") return pad.leftStick.ReadValue();
                if (token == "dpad") return pad.dpad.ReadValue();
                if (token == "rightStick") return pad.rightStick.ReadValue();
            }
#endif
            return Vector2.zero;
        }

        static Vector2 PadLook(string token)
        {
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad == null) return Vector2.zero;
            if (token == "rightStick") return pad.rightStick.ReadValue();
            if (token == "leftStick") return pad.leftStick.ReadValue();
#endif
            return Vector2.zero;
        }

        /// <summary>Keyboard tokens only, or one pad. The other device is not sampled.</summary>
        public static bool HeldDevice(ActionBinds binds, PlayAction action, int device)
        {
            if (binds == null) return false;
            int i = (int)action;
            if (i < 0 || i >= binds.Keyboard.Length) return false;
            if (device <= 0) return KeyboardHit(binds, action, i, false);
            return PadHeldAt(device - 1, binds.Gamepad[i]);
        }

        public static bool PressedDevice(ActionBinds binds, PlayAction action, int device)
        {
            if (binds == null) return false;
            int i = (int)action;
            if (i < 0 || i >= binds.Keyboard.Length) return false;
            if (device <= 0) return KeyboardHit(binds, action, i, true);
            return PadPressedAt(device - 1, binds.Gamepad[i]);
        }

        /// <summary>
        /// Primary key, then Jump's second key when one is stored.
        /// The alt string is the one already on the table. This does not build a new one.
        /// </summary>
        static bool KeyboardHit(ActionBinds binds, PlayAction action, int i, bool pressed)
        {
            string token = binds.Keyboard[i];
            if (pressed)
            {
                if (PressedToken(token, false)) return true;
            }
            else if (HeldToken(token, false)) return true;
            if (action != PlayAction.Jump) return false;
            string alt = binds.JumpAlt;
            if (string.IsNullOrEmpty(alt)) return false;
            return pressed ? PressedToken(alt, false) : HeldToken(alt, false);
        }

        public static Vector2 MoveDevice(ActionBinds binds, int device)
        {
            if (binds == null) return Vector2.zero;
            if (device <= 0) return KeyboardMove(binds.Keyboard[(int)PlayAction.Move]);
            return PadMoveAt(device - 1, binds.Gamepad[(int)PlayAction.Move]);
        }

        public static Vector2 LookDevice(ActionBinds binds, int device)
        {
            if (binds == null) return Vector2.zero;
            if (device <= 0)
            {
                if (binds.Keyboard[(int)PlayAction.Look] != "mouse") return Vector2.zero;
                return new Vector2(UnityEngine.Input.GetAxisRaw("Mouse X"), UnityEngine.Input.GetAxisRaw("Mouse Y"));
            }
            return PadLookAt(device - 1, binds.Gamepad[(int)PlayAction.Look]);
        }

        static Vector2 KeyboardMove(string token)
        {
            Vector2 v = Vector2.zero;
            bool arrows = token == "arrows";
            bool wasd = token == "wasd" || token == "holdIntoWall" || string.IsNullOrEmpty(token);
            if (wasd)
            {
                if (UnityEngine.Input.GetKey(KeyCode.D)) v.x += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.A)) v.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.W)) v.y += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S)) v.y -= 1f;
            }
            if (arrows || token == "arrows")
            {
                if (UnityEngine.Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
            }
            if (v.sqrMagnitude > 1f) v.Normalize();
            return v;
        }

        static bool PadHeldAt(int index, string token)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = PadAt(index);
            // A missing slot is empty. Falling through to Gamepad.current let an
            // unplugged seat steal whatever pad the runtime still called current.
            if (pad == null)
                return false;
            switch (token)
            {
                case "buttonSouth": return pad.buttonSouth.isPressed;
                case "buttonEast": return pad.buttonEast.isPressed;
                case "buttonWest": return pad.buttonWest.isPressed;
                case "buttonNorth": return pad.buttonNorth.isPressed;
                case "leftShoulder": return pad.leftShoulder.isPressed;
                case "rightShoulder": return pad.rightShoulder.isPressed;
                case "leftTrigger": return pad.leftTrigger.isPressed;
                case "rightTrigger": return pad.rightTrigger.isPressed;
                case "start": return pad.startButton.isPressed;
                case "select": return pad.selectButton.isPressed;
                case "dpadLeft": return pad.dpad.left.isPressed;
                case "dpadRight": return pad.dpad.right.isPressed;
                case "dpadUp": return pad.dpad.up.isPressed;
                case "dpadDown": return pad.dpad.down.isPressed;
                case "leftStickHold": return pad.leftStick.ReadValue().sqrMagnitude > 0.04f;
            }
            return false;
#else
            if (index != 0) return false;
            return PadHeld(token);
#endif
        }

        static bool PadPressedAt(int index, string token)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = PadAt(index);
            if (pad == null)
                return false;
            switch (token)
            {
                case "buttonSouth": return pad.buttonSouth.wasPressedThisFrame;
                case "buttonEast": return pad.buttonEast.wasPressedThisFrame;
                case "buttonWest": return pad.buttonWest.wasPressedThisFrame;
                case "buttonNorth": return pad.buttonNorth.wasPressedThisFrame;
                case "leftShoulder": return pad.leftShoulder.wasPressedThisFrame;
                case "rightShoulder": return pad.rightShoulder.wasPressedThisFrame;
                case "leftTrigger": return pad.leftTrigger.wasPressedThisFrame;
                case "rightTrigger": return pad.rightTrigger.wasPressedThisFrame;
                case "start": return pad.startButton.wasPressedThisFrame;
                case "select": return pad.selectButton.wasPressedThisFrame;
                case "dpadLeft": return pad.dpad.left.wasPressedThisFrame;
                case "dpadRight": return pad.dpad.right.wasPressedThisFrame;
                case "dpadUp": return pad.dpad.up.wasPressedThisFrame;
                case "dpadDown": return pad.dpad.down.wasPressedThisFrame;
            }
            return false;
#else
            if (index != 0) return false;
            return PadPressed(token);
#endif
        }

        static Vector2 PadMoveAt(int index, string token)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = PadAt(index);
            if (pad != null)
            {
                if (token == "leftStick" || token == "leftStickHold") return pad.leftStick.ReadValue();
                if (token == "rightStick") return pad.rightStick.ReadValue();
                if (token == "dpad") return pad.dpad.ReadValue();
            }
#endif
            return Vector2.zero;
        }

        static Vector2 PadLookAt(int index, string token)
        {
#if ENABLE_INPUT_SYSTEM
            Gamepad pad = PadAt(index);
            if (pad == null) return Vector2.zero;
            if (token == "rightStick") return pad.rightStick.ReadValue();
            if (token == "leftStick") return pad.leftStick.ReadValue();
#endif
            return Vector2.zero;
        }

#if ENABLE_INPUT_SYSTEM
        static Gamepad PadAt(int index)
        {
            if (index < 0) return null;
            var all = Gamepad.all;
            if (index >= all.Count) return null;
            return all[index];
        }
#endif
    }
}
