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

        static readonly string[] SeatBody = { "Red", "Blue", "Orange", "Lavender" };

        /// <summary>P1 red, P2 blue, P3 orange, P4 lavender. The costume swatches, not a second table.</summary>
        public static Color SeatColor(int seat)
        {
            int i = seat;
            if (i < 0) i = 0;
            if (i > 3) i = 3;
            return Swatch(SeatBody[i]);
        }

        public static Color Swatch(string key)
        {
            switch (Normalize(key))
            {
                case "Blue": return new Color(0.20f, 0.48f, 0.88f, 1f);
                case "Mint": return new Color(0.42f, 0.82f, 0.70f, 1f);
                case "Orange": return new Color(1.00f, 0.62f, 0.18f, 1f);
                case "Lavender": return new Color(0.90f, 0.82f, 1.00f, 1f);
                case "Red": return new Color(0.90f, 0.18f, 0.20f, 1f);
                default: return new Color(0.90f, 0.76f, 0.52f, 1f);
            }
        }
    }
}
