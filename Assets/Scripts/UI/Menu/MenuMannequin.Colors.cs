using Tag.Profiles;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Costume colors. The menu lane and the match chrome both read these.
    /// Seat order is P1 red, P2 blue, P3 orange, P4 lavender.
    /// </summary>
    public static partial class MenuMannequin
    {
        public static string Normalize(string key)
        {
            if (string.IsNullOrEmpty(key)) return "Tan";
            for (int i = 0; i < LocalProfiles.HierNames.Length; i++)
            {
                if (LocalProfiles.HierNames[i] == key) return key;
            }
            return "Tan";
        }

        public static string NameOf(int index)
        {
            int n = LocalProfiles.HierNames.Length;
            if (n < 1) return "Tan";
            if (index < 0) index = 0;
            if (index >= n) index = n - 1;
            return LocalProfiles.HierNames[index];
        }

        public const int Circle = 0;
        public const int Triangle = 1;
        public const int Square = 2;
        public const int Diamond = 3;

        struct SeatRow
        {
            public string Body;
            public int Mark;
        }

        // One seat table. P1 circle, P2 triangle, P3 square, P4 diamond.
        static readonly SeatRow[] Seats =
        {
            new SeatRow { Body = "Red", Mark = Circle },
            new SeatRow { Body = "Blue", Mark = Triangle },
            new SeatRow { Body = "Orange", Mark = Square },
            new SeatRow { Body = "Lavender", Mark = Diamond },
        };

        /// <summary>P1 red, P2 blue, P3 orange, P4 lavender. The costume swatches, not a second table.</summary>
        public static Color SeatColor(int seat)
        {
            return Swatch(Seats[SeatIndex(seat)].Body);
        }

        /// <summary>The shape that sits with this seat. 0 circle, 1 triangle, 2 square, 3 diamond.</summary>
        public static int SeatMark(int seat)
        {
            return Seats[SeatIndex(seat)].Mark;
        }

        static int SeatIndex(int seat)
        {
            if (seat < 0) return 0;
            if (seat > 3) return 3;
            return seat;
        }

        public static Color Swatch(string key)
        {
            switch (Normalize(key))
            {
                case "Blue": return new Color(0.20f, 0.48f, 0.88f, 1f);
                case "Mint": return new Color(0.42f, 0.82f, 0.70f, 1f);
                case "Orange": return new Color(1.00f, 0.62f, 0.18f, 1f);
                case "Lavender": return new Color(0.82f, 0.70f, 0.98f, 1f);
                case "Red": return new Color(0.90f, 0.18f, 0.20f, 1f);
                default: return new Color(0.90f, 0.76f, 0.52f, 1f);
            }
        }
    }
}
