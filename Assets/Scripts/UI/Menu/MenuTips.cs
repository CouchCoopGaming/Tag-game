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
            "Hold [WASD] against a wall to climb.",
            "Hold [WASD] + [Space] to wall jump.",
            "Air dash in the air.",
            "Punch to tag. It changes hands.",
            "Tag-back is 1 second.",
            "Double-click RMB to let go of the grapple.",
            "Least It: least time as It. Next punch breaks a tie.",
            "Hot Potato: first to 2.",
            "Trail Tag: last one standing.",
            "Free play has no timer.",
            "Pads launch. Zips carry you."
        };

        public static int Count => Lines.Length;

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
            if (i == 0) return "[" + Mark(binds, PlayAction.Jump, pad) + "] jumps.";
            if (i == 1) return "[" + Mark(binds, PlayAction.Sprint, pad) + "], then [" + Mark(binds, PlayAction.Slide, pad) + "].";
            if (i == 2) return "Hold [" + Mark(binds, PlayAction.Cling, pad) + "] against a wall to climb.";
            if (i == 3) return "Hold [" + Mark(binds, PlayAction.Cling, pad) + "] + [" + Mark(binds, PlayAction.Jump, pad) + "] to wall jump.";
            if (i == 4) return "[" + Mark(binds, PlayAction.AirDash, pad) + "] in the air.";
            if (i == 7)
            {
                string token = pad ? binds.GrapplePad : binds.GrappleKey;
                return "Double-click [" + ActionBinds.Chip(token) + "] to let go of the grapple.";
            }
            return Lines[i];
        }

        static string Mark(ActionBinds binds, PlayAction action, bool pad)
        {
            if (binds == null) binds = ActionBinds.Defaults();
            int n = (int)action;
            if (n < 0 || n >= binds.Keyboard.Length) return "";
            string token = pad ? binds.Gamepad[n] : binds.Keyboard[n];
            return ActionBinds.Chip(token);
        }

        public static bool Holds()
        {
            if (Count < 8) return false;
            if (At(0) != "Space jumps.") return false;
            if (At(Count) != At(0)) return false;
            if (At(9).IndexOf("first to 2", System.StringComparison.Ordinal) < 0) return false;
            if (For(0, 0) != At(0) || For(0, 1) != At(1)) return false;
            if (For(1, 0) == For(0, 0)) return false;
            if (At(2).IndexOf("into a wall", System.StringComparison.Ordinal) >= 0) return false;
            if (Shown(2, 0).IndexOf("against a wall") < 0) return false;
            if (Shown(3, 0).IndexOf("wall jump") < 0) return false;
            return true;
        }
    }
}
