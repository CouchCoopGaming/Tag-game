using System;

namespace Tag.Settings
{
    /// <summary>
    /// Keyboard and gamepad tokens for the actions that already exist.
    /// Jump's default keyboard token stays space. Cling's default is the
    /// move-into-wall hold, which shares Move and is not a second key.
    /// Comma stays mute and N stays music mute, so those tokens conflict.
    /// </summary>
    public enum PlayAction
    {
        Move = 0,
        Look = 1,
        Jump = 2,
        Cling = 3,
        Slide = 4,
        AirDash = 5,
        Punch = 6,
        Sprint = 7,
        Pause = 8,
        Minimap = 9,
        Arena1 = 10,
        Arena2 = 11,
        Count = 12
    }

    public sealed class ActionBinds
    {
        public readonly string[] Keyboard = new string[(int)PlayAction.Count];
        public readonly string[] Gamepad = new string[(int)PlayAction.Count];

        public static ActionBinds Current = Defaults();

        public static ActionBinds Defaults()
        {
            var b = new ActionBinds();
            Set(b, PlayAction.Move, "wasd", "leftStick");
            Set(b, PlayAction.Look, "mouse", "rightStick");
            Set(b, PlayAction.Jump, "space", "buttonSouth");
            Set(b, PlayAction.Cling, "holdIntoWall", "leftStickHold");
            Set(b, PlayAction.Slide, "leftCtrl", "buttonEast");
            Set(b, PlayAction.AirDash, "q", "rightShoulder");
            Set(b, PlayAction.Punch, "mouseLeft", "buttonWest");
            Set(b, PlayAction.Sprint, "leftShift", "leftShoulder");
            Set(b, PlayAction.Pause, "escape", "start");
            Set(b, PlayAction.Minimap, "m", "select");
            Set(b, PlayAction.Arena1, "alpha1", "dpadLeft");
            Set(b, PlayAction.Arena2, "alpha2", "dpadRight");
            return b;
        }

        public static string Name(PlayAction action)
        {
            switch (action)
            {
                case PlayAction.Move: return "Move";
                case PlayAction.Look: return "Look";
                case PlayAction.Jump: return "Jump";
                case PlayAction.Cling: return "Cling hold";
                case PlayAction.Slide: return "Slide";
                case PlayAction.AirDash: return "Air dash";
                case PlayAction.Punch: return "Punch / tag";
                case PlayAction.Sprint: return "Sprint";
                case PlayAction.Pause: return "Pause";
                case PlayAction.Minimap: return "Minimap";
                case PlayAction.Arena1: return "Arena 1";
                case PlayAction.Arena2: return "Arena 2";
                default: return "Action";
            }
        }

        public string RowLabel(PlayAction action)
        {
            int i = (int)action;
            return Name(action) + "   " + Show(Keyboard[i]) + "   /   " + Show(Gamepad[i]);
        }

        public static string Show(string token)
        {
            switch (token)
            {
                case "wasd": return "WASD";
                case "arrows": return "Arrows";
                case "mouse": return "Mouse";
                case "mouseLeft": return "LMB";
                case "mouseRight": return "RMB";
                case "mouseMiddle": return "MMB";
                case "space": return "Space";
                case "holdIntoWall": return "Hold into wall";
                case "leftCtrl": return "Ctrl";
                case "leftShift": return "Shift";
                case "leftAlt": return "Alt";
                case "escape": return "Esc";
                case "alpha1": return "1";
                case "alpha2": return "2";
                case "comma": return "Comma";
                case "q": return "Q";
                case "e": return "E";
                case "m": return "M";
                case "leftStick": return "Left stick";
                case "rightStick": return "Right stick";
                case "leftStickHold": return "Left stick hold";
                case "buttonSouth": return "South";
                case "buttonEast": return "East";
                case "buttonWest": return "West";
                case "buttonNorth": return "North";
                case "leftShoulder": return "LB";
                case "rightShoulder": return "RB";
                case "start": return "Start";
                case "select": return "Select";
                case "dpadLeft": return "D-pad left";
                case "dpadRight": return "D-pad right";
                case "dpadUp": return "D-pad up";
                case "dpadDown": return "D-pad down";
                default: return string.IsNullOrEmpty(token) ? "—" : token;
            }
        }

        public bool KeyboardIsDefault(PlayAction action)
        {
            return Keyboard[(int)action] == Defaults().Keyboard[(int)action];
        }

        public bool GamepadIsDefault(PlayAction action)
        {
            return Gamepad[(int)action] == Defaults().Gamepad[(int)action];
        }

        public bool UsesLegacy(PlayAction action)
        {
            return KeyboardIsDefault(action) && GamepadIsDefault(action);
        }

        public void SetKeyboard(PlayAction action, string token)
        {
            Keyboard[(int)action] = token ?? "";
        }

        public void SetGamepad(PlayAction action, string token)
        {
            Gamepad[(int)action] = token ?? "";
        }

        public void ResetToDefaults()
        {
            var d = Defaults();
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                Keyboard[i] = d.Keyboard[i];
                Gamepad[i] = d.Gamepad[i];
            }
        }

        public ActionBinds Clone()
        {
            var copy = new ActionBinds();
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                copy.Keyboard[i] = Keyboard[i];
                copy.Gamepad[i] = Gamepad[i];
            }
            return copy;
        }

        /// <summary>
        /// True when this action's token is reserved or matches another action
        /// on the same device. The cling hold aliases Move, so it does not clash.
        /// </summary>
        public bool Conflict(PlayAction action, out PlayAction other)
        {
            int i = (int)action;
            if (Reserved(Keyboard[i]) || Reserved(Gamepad[i]))
            {
                other = action;
                return true;
            }
            for (int j = 0; j < (int)PlayAction.Count; j++)
            {
                if (j == i) continue;
                if (Clash(Keyboard[i], Keyboard[j]) || Clash(Gamepad[i], Gamepad[j]))
                {
                    other = (PlayAction)j;
                    return true;
                }
            }
            other = action;
            return false;
        }

        public bool AnyConflict(out PlayAction a, out PlayAction b)
        {
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                if (Conflict((PlayAction)i, out var other) && other != (PlayAction)i)
                {
                    a = (PlayAction)i;
                    b = other;
                    return true;
                }
                if (Reserved(Keyboard[i]) || Reserved(Gamepad[i]))
                {
                    a = (PlayAction)i;
                    b = (PlayAction)i;
                    return true;
                }
            }
            a = PlayAction.Move;
            b = PlayAction.Move;
            return false;
        }

        public static bool Reserved(string token)
        {
            return token == "comma" || token == "n";
        }

        public static bool SharesMove(string token)
        {
            return token == "holdIntoWall" || token == "leftStickHold";
        }

        static bool Clash(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            if (!string.Equals(a, b, StringComparison.Ordinal)) return false;
            if (SharesMove(a)) return false;
            return true;
        }

        static void Set(ActionBinds b, PlayAction action, string keyboard, string gamepad)
        {
            b.Keyboard[(int)action] = keyboard;
            b.Gamepad[(int)action] = gamepad;
        }
    }
}
