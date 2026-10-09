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
        Arena3 = 12,
        Count = 13
    }

    public sealed class ActionBinds
    {
        public readonly string[] Keyboard = new string[(int)PlayAction.Count];
        public readonly string[] Gamepad = new string[(int)PlayAction.Count];

        /// <summary>
        /// Extra keyboard jump. Space stays the primary token. Empty means no second key.
        /// The menu writes this. SetKeyboard does not.
        /// </summary>
        public string JumpAlt = "";

        /// <summary>Existing grapple verb. RMB pulls. A second press within 0.28 s releases.</summary>
        public const string GrappleKeyDefault = "mouseRight";

        /// <summary>Pad default for that same verb. LT is not used by another action.</summary>
        public const string GrapplePadDefault = "leftTrigger";

        public string GrappleKey = GrappleKeyDefault;
        public string GrapplePad = GrapplePadDefault;

        /// <summary>Bumps when a token changes so prompt glyphs can refresh without scanning.</summary>
        public int Revision { get; private set; }

        public static ActionBinds Current = Defaults();

        static ActionBinds _template;

        /// <summary>One table for default compares. Per-frame rebind checks must not allocate.</summary>
        static ActionBinds Template()
        {
            if (_template != null) return _template;
            _template = new ActionBinds();
            Fill(_template);
            return _template;
        }

        public static ActionBinds Defaults()
        {
            var b = new ActionBinds();
            ActionBinds template = Template();
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                b.Keyboard[i] = template.Keyboard[i];
                b.Gamepad[i] = template.Gamepad[i];
            }
            b.JumpAlt = template.JumpAlt ?? "";
            b.GrappleKey = string.IsNullOrEmpty(template.GrappleKey) ? GrappleKeyDefault : template.GrappleKey;
            b.GrapplePad = string.IsNullOrEmpty(template.GrapplePad) ? GrapplePadDefault : template.GrapplePad;
            return b;
        }

        static void Fill(ActionBinds b)
        {
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
            Set(b, PlayAction.Arena3, "alpha3", "dpadUp");
            b.GrappleKey = GrappleKeyDefault;
            b.GrapplePad = GrapplePadDefault;
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
                case PlayAction.Arena3: return "Arena 3";
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
                case "alpha3": return "3";
                case "comma": return "Comma";
                case "q": return "Q";
                case "e": return "E";
                case "c": return "C";
                case "v": return "V";
                case "f": return "F";
                case "r": return "R";
                case "p": return "P";
                case "n": return "N";
                case "tab": return "Tab";
                case "rightShift": return "RShift";
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

        /// <summary>
        /// Xbox face names for the controls list. The token itself does not change.
        /// </summary>
        /// <summary>
        /// Short mark for a loading tip. Cling's keyboard token is the move hold,
        /// drawn as WASD. The pad hold is the left stick. Face buttons use A B X Y.
        /// </summary>
        public static string Chip(string token)
        {
            if (token == "holdIntoWall") return "WASD";
            if (token == "leftStickHold") return "Left stick";
            return PadWord(token);
        }

        public static string PadWord(string token)
        {
            switch (token)
            {
                case "buttonSouth": return "A";
                case "buttonEast": return "B";
                case "buttonWest": return "X";
                case "buttonNorth": return "Y";
                case "leftTrigger": return "LT";
                case "rightTrigger": return "RT";
                default: return Show(token);
            }
        }

        public bool KeyboardIsDefault(PlayAction action)
        {
            return Keyboard[(int)action] == Template().Keyboard[(int)action];
        }

        public bool GamepadIsDefault(PlayAction action)
        {
            return Gamepad[(int)action] == Template().Gamepad[(int)action];
        }

        public static bool KnownKeyboard(string token)
        {
            switch (token)
            {
                case "space":
                case "leftShift":
                case "rightShift":
                case "leftCtrl":
                case "leftAlt":
                case "escape":
                case "q":
                case "e":
                case "c":
                case "v":
                case "f":
                case "m":
                case "r":
                case "p":
                case "tab":
                case "alpha1":
                case "alpha2":
                case "alpha3":
                case "mouseLeft":
                case "mouseRight":
                case "mouseMiddle":
                    return true;
                default:
                    return false;
            }
        }

        public bool UsesLegacy(PlayAction action)
        {
            if (action == PlayAction.Jump && !string.IsNullOrEmpty(JumpAlt)) return false;
            return KeyboardIsDefault(action) && GamepadIsDefault(action);
        }

        public void SetJumpAlt(string token)
        {
            JumpAlt = token ?? "";
            Revision++;
        }

        public void SetGrappleKey(string token)
        {
            GrappleKey = string.IsNullOrEmpty(token) ? GrappleKeyDefault : token;
            Revision++;
        }

        public void SetGrapplePad(string token)
        {
            GrapplePad = string.IsNullOrEmpty(token) ? GrapplePadDefault : token;
            Revision++;
        }

        public void SetKeyboard(PlayAction action, string token)
        {
            Keyboard[(int)action] = token ?? "";
            Revision++;
        }

        public void SetGamepad(PlayAction action, string token)
        {
            Gamepad[(int)action] = token ?? "";
            Revision++;
        }

        public void ResetToDefaults()
        {
            var d = Defaults();
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                Keyboard[i] = d.Keyboard[i];
                Gamepad[i] = d.Gamepad[i];
            }
            JumpAlt = d.JumpAlt ?? "";
            GrappleKey = string.IsNullOrEmpty(d.GrappleKey) ? GrappleKeyDefault : d.GrappleKey;
            GrapplePad = string.IsNullOrEmpty(d.GrapplePad) ? GrapplePadDefault : d.GrapplePad;
            Revision++;
        }

        public ActionBinds Clone()
        {
            var copy = new ActionBinds();
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                copy.Keyboard[i] = Keyboard[i];
                copy.Gamepad[i] = Gamepad[i];
            }
            copy.JumpAlt = JumpAlt ?? "";
            copy.GrappleKey = string.IsNullOrEmpty(GrappleKey) ? GrappleKeyDefault : GrappleKey;
            copy.GrapplePad = string.IsNullOrEmpty(GrapplePad) ? GrapplePadDefault : GrapplePad;
            return copy;
        }

        /// <summary>
        /// How many default pairs share a sampled button.
        /// Keyboard extras that the pawn still ORs in count. Each of the four pads counts.
        /// Cling's hold shares Move, so that pair is not a clash. Alt is not on Air dash or Sprint.
        /// </summary>
        public static int DefaultConflicts()
        {
            ActionBinds shipped = Defaults();
            int pairs = SamplePairs(shipped, true);
            pairs += SamplePairs(shipped, false) * 4;
            return pairs;
        }

        static int SamplePairs(ActionBinds binds, bool keyboard)
        {
            int slots = (int)PlayAction.Count + 1;
            int n = 0;
            for (int i = 0; i < slots; i++)
            {
                Sample(binds, i, keyboard, out string a, out string extraA);
                for (int j = i + 1; j < slots; j++)
                {
                    Sample(binds, j, keyboard, out string b, out string extraB);
                    if (SameButton(a, b) || SameButton(a, extraB) || SameButton(extraA, b) || SameButton(extraA, extraB))
                        n++;
                }
            }
            return n;
        }

        static void Sample(ActionBinds binds, int slot, bool keyboard, out string primary, out string extra)
        {
            extra = "";
            if (slot >= (int)PlayAction.Count)
            {
                primary = keyboard ? binds.GrappleKey : binds.GrapplePad;
                return;
            }
            primary = keyboard ? binds.Keyboard[slot] : binds.Gamepad[slot];
            if (!keyboard || string.IsNullOrEmpty(primary)) return;
            var action = (PlayAction)slot;
            if (action == PlayAction.Slide && primary == "leftCtrl") extra = "c";
            else if (action == PlayAction.Punch && primary != "e") extra = "e";
            else if (action == PlayAction.Jump && !string.IsNullOrEmpty(binds.JumpAlt)) extra = binds.JumpAlt;
        }

        static bool SameButton(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            if (!string.Equals(a, b, StringComparison.Ordinal)) return false;
            if (SharesMove(a)) return false;
            return true;
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
