using System;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Face-button names for the pad on a seat. Detection reads the product
    /// string once, when that pad is used, and keeps the family.
    /// </summary>
    public static class PadGlyph
    {
        public const int Keyboard = 0;
        public const int Xbox = 1;
        public const int PlayStation = 2;
        public const int Switch = 3;
        public const int Generic = 4;
        public const int Families = 5;

        static readonly int[] FamilyOf = { Keyboard, Generic, Generic, Generic, Generic };

        static readonly string[,] Lines =
        {
            { "Arrows   move", "Space   confirm", "Esc   back" },
            { "Stick   move", "A   confirm", "B   back" },
            { "Stick   move", "Cross   confirm", "Circle   back" },
            { "Stick   move", "B   confirm", "A   back" },
            { "Stick   move", "South   confirm", "East   back" }
        };

        static readonly string[] JoinLine =
        {
            "Press Space to join",
            "Press A to join",
            "Press Cross to join",
            "Press B to join",
            "Press a button to join"
        };

        public static int FromProduct(string name)
        {
            if (string.IsNullOrEmpty(name)) return Generic;
            if (Has(name, "xbox") || Has(name, "xinput")) return Xbox;
            if (Has(name, "dual") || Has(name, "playstation") || Has(name, "sony")) return PlayStation;
            if (Has(name, "switch") || Has(name, "nintendo") || Has(name, "pro controller") || Has(name, "npad")) return Switch;
            return Generic;
        }

        public static void Note(int device, string product)
        {
            if (device < 0 || device >= FamilyOf.Length) return;
            if (device == 0)
            {
                FamilyOf[0] = Keyboard;
                return;
            }
            FamilyOf[device] = FromProduct(product);
        }

        public static int Family(int device)
        {
            if (device < 0 || device >= FamilyOf.Length) return Generic;
            return FamilyOf[device];
        }

        public static string Line(int family, int slot)
        {
            int f = family;
            if (f < 0 || f >= Families) f = Generic;
            int s = slot;
            if (s < 0) s = 0;
            if (s > 2) s = 2;
            return Lines[f, s];
        }

        public static string Join(int family)
        {
            int f = family;
            if (f < 0 || f >= Families) f = Generic;
            return JoinLine[f];
        }

        public static bool Samples()
        {
            if (FromProduct(null) != Generic) return false;
            if (FromProduct("") != Generic) return false;
            if (FromProduct("Xbox Wireless Controller") != Xbox) return false;
            if (FromProduct("XInputControllerWindows") != Xbox) return false;
            if (FromProduct("DualSense Wireless Controller") != PlayStation) return false;
            if (FromProduct("Sony DualShock 4") != PlayStation) return false;
            if (FromProduct("Pro Controller") != Switch) return false;
            if (FromProduct("Nintendo Switch Pro") != Switch) return false;
            if (FromProduct("NPad") != Switch) return false;
            if (FromProduct("Wireless Controller") != Generic) return false;
            Note(0, "keyboard");
            if (Family(0) != Keyboard) return false;
            Note(1, "Xbox One Controller");
            if (Family(1) != Xbox) return false;
            Note(2, "DualSense");
            if (Family(2) != PlayStation) return false;
            if (Line(Keyboard, 0) != "Arrows   move") return false;
            if (Line(Xbox, 1) != "A   confirm") return false;
            if (Line(PlayStation, 2) != "Circle   back") return false;
            if (Line(Switch, 1) != "B   confirm") return false;
            if (Line(Switch, 2) != "A   back") return false;
            if (Line(Generic, 1) != "South   confirm") return false;
            if (Join(Keyboard) != "Press Space to join") return false;
            if (Join(Generic) != "Press a button to join") return false;
            return true;
        }

        static bool Has(string name, string needle)
        {
            return name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
