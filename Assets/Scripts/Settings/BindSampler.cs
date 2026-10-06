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
            var binds = ActionBinds.Current;
            int i = (int)action;
            return HeldToken(binds.Keyboard[i], false) || HeldToken(binds.Gamepad[i], true);
        }

        public static bool Pressed(PlayAction action)
        {
            var binds = ActionBinds.Current;
            int i = (int)action;
            return PressedToken(binds.Keyboard[i], false) || PressedToken(binds.Gamepad[i], true);
        }

        public static Vector2 MoveVector()
        {
            var binds = ActionBinds.Current;
            Vector2 v = Vector2.zero;
            string kb = binds.Keyboard[(int)PlayAction.Move];
            if (kb == "wasd")
            {
                if (Input.GetKey(KeyCode.D)) v.x += 1f;
                if (Input.GetKey(KeyCode.A)) v.x -= 1f;
                if (Input.GetKey(KeyCode.W)) v.y += 1f;
                if (Input.GetKey(KeyCode.S)) v.y -= 1f;
            }
            else if (kb == "arrows")
            {
                if (Input.GetKey(KeyCode.RightArrow)) v.x += 1f;
                if (Input.GetKey(KeyCode.LeftArrow)) v.x -= 1f;
                if (Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
                if (Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
            }
            v += PadMove(binds.Gamepad[(int)PlayAction.Move]);
            if (v.sqrMagnitude > 1f) v.Normalize();
            return v;
        }

        public static Vector2 LookVector()
        {
            var binds = ActionBinds.Current;
            Vector2 v = Vector2.zero;
            if (binds.Keyboard[(int)PlayAction.Look] == "mouse")
                v = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            v += PadLook(binds.Gamepad[(int)PlayAction.Look]);
            return v;
        }

        /// <summary>Token of the control that went down this frame, or null.</summary>
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
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) return "buttonSouth";
            if (Input.GetKeyDown(KeyCode.JoystickButton1)) return "buttonEast";
            if (Input.GetKeyDown(KeyCode.JoystickButton2)) return "buttonWest";
            if (Input.GetKeyDown(KeyCode.JoystickButton3)) return "buttonNorth";
            if (Input.GetKeyDown(KeyCode.JoystickButton4)) return "leftShoulder";
            if (Input.GetKeyDown(KeyCode.JoystickButton5)) return "rightShoulder";
            if (Input.GetKeyDown(KeyCode.JoystickButton6)) return "select";
            if (Input.GetKeyDown(KeyCode.JoystickButton7)) return "start";
            if (Input.GetKeyDown(KeyCode.JoystickButton8)) return "leftStickPress";
            if (Input.GetKeyDown(KeyCode.JoystickButton9)) return "rightStickPress";

            if (Input.GetKeyDown(KeyCode.Space)) return "space";
            if (Input.GetKeyDown(KeyCode.LeftShift)) return "leftShift";
            if (Input.GetKeyDown(KeyCode.RightShift)) return "rightShift";
            if (Input.GetKeyDown(KeyCode.LeftControl)) return "leftCtrl";
            if (Input.GetKeyDown(KeyCode.LeftAlt)) return "leftAlt";
            if (Input.GetKeyDown(KeyCode.Escape)) return "escape";
            if (Input.GetKeyDown(KeyCode.Comma)) return "comma";
            if (Input.GetKeyDown(KeyCode.Q)) return "q";
            if (Input.GetKeyDown(KeyCode.E)) return "e";
            if (Input.GetKeyDown(KeyCode.C)) return "c";
            if (Input.GetKeyDown(KeyCode.V)) return "v";
            if (Input.GetKeyDown(KeyCode.F)) return "f";
            if (Input.GetKeyDown(KeyCode.R)) return "r";
            if (Input.GetKeyDown(KeyCode.M)) return "m";
            if (Input.GetKeyDown(KeyCode.N)) return "n";
            if (Input.GetKeyDown(KeyCode.P)) return "p";
            if (Input.GetKeyDown(KeyCode.Tab)) return "tab";
            if (Input.GetKeyDown(KeyCode.Alpha1)) return "alpha1";
            if (Input.GetKeyDown(KeyCode.Alpha2)) return "alpha2";
            if (Input.GetMouseButtonDown(0)) return "mouseLeft";
            if (Input.GetMouseButtonDown(1)) return "mouseRight";
            if (Input.GetMouseButtonDown(2)) return "mouseMiddle";
            return null;
        }

        public static bool PadButtonDown()
        {
            string token = AnyPressedToken();
            if (string.IsNullOrEmpty(token)) return false;
            return token == "buttonSouth" || token == "buttonEast" || token == "buttonWest"
                || token == "buttonNorth" || token == "leftShoulder" || token == "rightShoulder"
                || token == "start" || token == "select" || token.StartsWith("dpad")
                || token.EndsWith("Press");
        }

        static bool HeldToken(string token, bool pad)
        {
            if (string.IsNullOrEmpty(token) || ActionBinds.SharesMove(token)) return false;
            if (pad) return PadHeld(token);
            switch (token)
            {
                case "space": return Input.GetKey(KeyCode.Space);
                case "leftShift": return Input.GetKey(KeyCode.LeftShift);
                case "rightShift": return Input.GetKey(KeyCode.RightShift);
                case "leftCtrl": return Input.GetKey(KeyCode.LeftControl);
                case "leftAlt": return Input.GetKey(KeyCode.LeftAlt);
                case "escape": return Input.GetKey(KeyCode.Escape);
                case "q": return Input.GetKey(KeyCode.Q);
                case "e": return Input.GetKey(KeyCode.E);
                case "c": return Input.GetKey(KeyCode.C);
                case "v": return Input.GetKey(KeyCode.V);
                case "f": return Input.GetKey(KeyCode.F);
                case "m": return Input.GetKey(KeyCode.M);
                case "r": return Input.GetKey(KeyCode.R);
                case "p": return Input.GetKey(KeyCode.P);
                case "tab": return Input.GetKey(KeyCode.Tab);
                case "alpha1": return Input.GetKey(KeyCode.Alpha1);
                case "alpha2": return Input.GetKey(KeyCode.Alpha2);
                case "mouseLeft": return Input.GetMouseButton(0);
                case "mouseRight": return Input.GetMouseButton(1);
                case "mouseMiddle": return Input.GetMouseButton(2);
                default: return false;
            }
        }

        static bool PressedToken(string token, bool pad)
        {
            if (string.IsNullOrEmpty(token) || ActionBinds.SharesMove(token)) return false;
            if (pad) return PadPressed(token);
            switch (token)
            {
                case "space": return Input.GetKeyDown(KeyCode.Space);
                case "leftShift": return Input.GetKeyDown(KeyCode.LeftShift);
                case "leftCtrl": return Input.GetKeyDown(KeyCode.LeftControl);
                case "leftAlt": return Input.GetKeyDown(KeyCode.LeftAlt);
                case "escape": return Input.GetKeyDown(KeyCode.Escape);
                case "q": return Input.GetKeyDown(KeyCode.Q);
                case "e": return Input.GetKeyDown(KeyCode.E);
                case "c": return Input.GetKeyDown(KeyCode.C);
                case "v": return Input.GetKeyDown(KeyCode.V);
                case "f": return Input.GetKeyDown(KeyCode.F);
                case "m": return Input.GetKeyDown(KeyCode.M);
                case "alpha1": return Input.GetKeyDown(KeyCode.Alpha1);
                case "alpha2": return Input.GetKeyDown(KeyCode.Alpha2);
                case "mouseLeft": return Input.GetMouseButtonDown(0);
                case "mouseRight": return Input.GetMouseButtonDown(1);
                case "mouseMiddle": return Input.GetMouseButtonDown(2);
                case "p": return Input.GetKeyDown(KeyCode.P);
                case "tab": return Input.GetKeyDown(KeyCode.Tab);
                case "r": return Input.GetKeyDown(KeyCode.R);
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
                case "buttonSouth": return Input.GetKey(KeyCode.JoystickButton0);
                case "buttonEast": return Input.GetKey(KeyCode.JoystickButton1);
                case "buttonWest": return Input.GetKey(KeyCode.JoystickButton2);
                case "buttonNorth": return Input.GetKey(KeyCode.JoystickButton3);
                case "leftShoulder": return Input.GetKey(KeyCode.JoystickButton4);
                case "rightShoulder": return Input.GetKey(KeyCode.JoystickButton5);
                case "select": return Input.GetKey(KeyCode.JoystickButton6);
                case "start": return Input.GetKey(KeyCode.JoystickButton7);
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
                case "buttonSouth": return Input.GetKeyDown(KeyCode.JoystickButton0);
                case "buttonEast": return Input.GetKeyDown(KeyCode.JoystickButton1);
                case "buttonWest": return Input.GetKeyDown(KeyCode.JoystickButton2);
                case "buttonNorth": return Input.GetKeyDown(KeyCode.JoystickButton3);
                case "leftShoulder": return Input.GetKeyDown(KeyCode.JoystickButton4);
                case "rightShoulder": return Input.GetKeyDown(KeyCode.JoystickButton5);
                case "select": return Input.GetKeyDown(KeyCode.JoystickButton6);
                case "start": return Input.GetKeyDown(KeyCode.JoystickButton7);
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
    }
}
