using Tag.Couch;
using Tag.Onboard;
using Tag.Settings;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Loading tips. Each line is a verb or a rule the match already uses.
    /// </summary>
    public static class MenuTips
    {
        public static readonly string[] Lines =
        {
            "Space jumps.",
            "Sprint, then slide.",
            "Push [WASD] or the left stick into a wall to climb.",
            "Press [Space] while pushing into a wall to wall jump.",
            "Air dash in the air.",
            "Punch to tag. It changes hands.",
            "Tag-back is 1 second.",
            "Double-click the grapple button to let go.",
            "Least It: least time as It. Next punch breaks a tie.",
            "Hot Potato: first to 2.",
            "Trail Tag: last one standing.",
            "Free play has no timer.",
            "Pads launch. Zips carry you."
        };

        public static int Count => Lines.Length;

        /// <summary>
        /// Main-menu grapple tip. The glyph is the bound key, not a baked word.
        /// </summary>
        public static string GrappleLine(ActionBinds binds)
        {
            if (binds == null) binds = ActionBinds.Current ?? ActionBinds.Defaults();
            return "Double-click [" + ActionBinds.Show(binds.GrappleKey) + "] to let go of the grapple.";
        }

        public static string At(int index)
        {
            int n = Lines.Length;
            if (n < 1) return "";
            int i = index;
            if (i < 0) i = 0;
            i %= n;
            return Lines[i];
        }

        /// <summary>One tip per seat. The next turn rotates every seat together.</summary>
        public static string For(int seat, int turn)
        {
            if (seat < 0) seat = 0;
            return At(seat + turn);
        }

        /// <summary>
        /// The same tip, with that seat's bound marks. A pad reads Left stick and A.
        /// A keyboard reads WASD and Space.
        /// </summary>
        public static string Shown(int seat, int turn)
        {
            if (seat < 0) seat = 0;
            int n = Lines.Length;
            if (n < 1) return "";
            int i = seat + turn;
            if (i < 0) i = 0;
            i %= n;
            int device = CouchPlay.DeviceOf(seat);
            if (device < CouchPlay.DeviceKeyboard)
                device = seat <= 0 ? CouchPlay.DeviceKeyboard : CouchPlay.DevicePad0 + seat - 1;
            if (device > CouchPlay.DevicePad3) device = CouchPlay.DevicePad3;
            ActionBinds binds = CouchPlay.BindsFor(device);
            if (binds == null) binds = ActionBinds.Defaults();
            bool pad = device != CouchPlay.DeviceKeyboard;
            if (i == 0) return Verb(PlayAction.Jump) + " [" + Mark(binds, PlayAction.Jump, pad) + "] to leave the ground.";
            if (i == 1) return Verb(PlayAction.Sprint) + " [" + Mark(binds, PlayAction.Sprint, pad) + "], then " + Later(PlayAction.Slide) + " [" + Mark(binds, PlayAction.Slide, pad) + "].";
            if (i == 2) return ClimbLine(binds, pad);
            if (i == 3) return WallJumpLine(binds, pad);
            if (i == 4) return Verb(PlayAction.AirDash) + " [" + Mark(binds, PlayAction.AirDash, pad) + "] in the air.";
            if (i == 5) return Verb(PlayAction.Punch) + " [" + Mark(binds, PlayAction.Punch, pad) + "]. It changes hands.";
            if (i == 7) return LetGoLine(binds, pad);
            return Lines[i];
        }

        /// <summary>
        /// Climb is the move wish into the wall. Cling is not its own button.
        /// </summary>
        static string ClimbLine(ActionBinds binds, bool pad)
        {
            if (pad) return "Push the left stick into a wall to climb.";
            return "Push [" + MoveWord(binds, false) + "] into a wall to climb.";
        }

        /// <summary>
        /// Wall jump is Jump pressed while that wish still holds, or inside cling grace.
        /// </summary>
        static string WallJumpLine(ActionBinds binds, bool pad)
        {
            string jump = Mark(binds, PlayAction.Jump, pad);
            if (pad) return "Press [" + jump + "] while pushing the left stick into a wall to wall jump.";
            return "Press [" + jump + "] while pushing [" + MoveWord(binds, false) + "] into a wall to wall jump.";
        }

        /// <summary>
        /// Second press inside the grapple window lets go. The pad glyph is the
        /// bound token, shown the same way as the keyboard key.
        /// </summary>
        static string LetGoLine(ActionBinds binds, bool pad)
        {
            if (binds == null) binds = ActionBinds.Defaults();
            string token = pad ? binds.GrapplePad : binds.GrappleKey;
            if (string.IsNullOrEmpty(token))
                token = pad ? ActionBinds.GrapplePadDefault : ActionBinds.GrappleKeyDefault;
            string mark = pad ? ActionBinds.PadWord(token) : ActionBinds.Show(token);
            return "Double-click [" + mark + "] to let go of the grapple.";
        }

        static string MoveWord(ActionBinds binds, bool pad)
        {
            if (binds == null) binds = ActionBinds.Defaults();
            string token = pad ? binds.Gamepad[(int)PlayAction.Move] : binds.Keyboard[(int)PlayAction.Move];
            return ActionBinds.Show(token);
        }

        static string Verb(PlayAction action)
        {
            return ActionBinds.Name(action);
        }

        static string Later(PlayAction action)
        {
            string name = ActionBinds.Name(action);
            if (string.IsNullOrEmpty(name)) return "";
            if (name.Length == 1) return name.ToLowerInvariant();
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        static string Mark(ActionBinds binds, PlayAction action, bool pad)
        {
            if (binds == null) binds = ActionBinds.Defaults();
            int n = (int)action;
            if (n < 0 || n >= binds.Keyboard.Length) return "";
            string token = pad ? binds.Gamepad[n] : binds.Keyboard[n];
            string chip = ActionBinds.Chip(token);
            if (SharesMove(binds, action, pad, chip))
                return ActionBinds.Show(token);
            return chip;
        }

        static bool SharesMove(ActionBinds binds, PlayAction action, bool pad, string chip)
        {
            if (action == PlayAction.Move || action == PlayAction.Look) return false;
            int move = (int)PlayAction.Move;
            int look = (int)PlayAction.Look;
            string moveToken = pad ? binds.Gamepad[move] : binds.Keyboard[move];
            string lookToken = pad ? binds.Gamepad[look] : binds.Keyboard[look];
            return chip == ActionBinds.Chip(moveToken) || chip == ActionBinds.Chip(lookToken);
        }

        public static bool Holds()
        {
            if (Count < 8) return false;
            if (At(0) != "Space jumps.") return false;
            if (At(Count) != At(0)) return false;
            if (At(9).IndexOf("first to 2", System.StringComparison.Ordinal) < 0) return false;
            if (For(0, 0) != At(0) || For(0, 1) != At(1)) return false;
            if (For(1, 0) == For(0, 0)) return false;
            if (At(2) != "Push [WASD] or the left stick into a wall to climb.") return false;
            if (Shown(0, 2).IndexOf("Push [WASD] into a wall", System.StringComparison.Ordinal) != 0) return false;
            if (Shown(2, 0).IndexOf("into a wall") < 0) return false;
            if (Shown(2, 0).IndexOf("[", System.StringComparison.Ordinal) >= 0) return false;
            string padLetGo = Shown(1, 6);
            if (padLetGo.IndexOf("[LT]", System.StringComparison.Ordinal) < 0) return false;
            if (padLetGo.IndexOf("leftTrigger", System.StringComparison.Ordinal) >= 0) return false;
            if (Shown(0, 7).IndexOf("[RMB]", System.StringComparison.Ordinal) < 0) return false;
            if (Shown(2, 0).IndexOf("Cling hold", System.StringComparison.Ordinal) >= 0) return false;
            if (Shown(3, 0).IndexOf("Cling hold", System.StringComparison.Ordinal) >= 0) return false;
            if (Shown(3, 0).IndexOf("wall jump") < 0) return false;
            string sprint = ActionBinds.Name(PlayAction.Sprint);
            if (Shown(1, 0).IndexOf(sprint + " [", System.StringComparison.Ordinal) != 0) return false;
            string slide = Later(PlayAction.Slide);
            if (slide.Length < 1 || Shown(1, 0).IndexOf(slide + " [", System.StringComparison.Ordinal) < 0) return false;
            if (Shown(0, 0).IndexOf(ActionBinds.Name(PlayAction.Jump) + " [", System.StringComparison.Ordinal) != 0) return false;
            ActionBinds kb = ActionBinds.Defaults();
            if (!MarkIsOwn(kb, false)) return false;
            if (!MarkIsOwn(kb, true)) return false;
            string glyph = ActionBinds.Show(kb.GrappleKey);
            if (GrappleLine(kb).IndexOf("[" + glyph + "]", System.StringComparison.Ordinal) < 0) return false;
            ActionBinds moved = ActionBinds.Defaults();
            moved.GrappleKey = "q";
            string shifted = GrappleLine(moved);
            if (shifted.IndexOf("[Q]", System.StringComparison.Ordinal) < 0) return false;
            if (shifted.IndexOf("RMB", System.StringComparison.Ordinal) >= 0) return false;
            return true;
        }

        /// <summary>
        /// Cling's short chip is the move control. The tip has to use the real hold.
        /// </summary>
        static bool MarkIsOwn(ActionBinds binds, bool pad)
        {
            string move = ActionBinds.Chip(pad ? binds.Gamepad[(int)PlayAction.Move] : binds.Keyboard[(int)PlayAction.Move]);
            string cling = Mark(binds, PlayAction.Cling, pad);
            if (cling == move) return false;
            string token = pad ? binds.Gamepad[(int)PlayAction.Cling] : binds.Keyboard[(int)PlayAction.Cling];
            if (cling != ActionBinds.Show(token)) return false;
            string jump = Mark(binds, PlayAction.Jump, pad);
            string sprint = Mark(binds, PlayAction.Sprint, pad);
            string slide = Mark(binds, PlayAction.Slide, pad);
            string air = Mark(binds, PlayAction.AirDash, pad);
            string punch = Mark(binds, PlayAction.Punch, pad);
            if (jump == move || sprint == move || slide == move || air == move || punch == move) return false;
            string look = ActionBinds.Chip(pad ? binds.Gamepad[(int)PlayAction.Look] : binds.Keyboard[(int)PlayAction.Look]);
            if (jump == look || cling == look || sprint == look || slide == look || air == look || punch == look) return false;
            return true;
        }
    }
}
