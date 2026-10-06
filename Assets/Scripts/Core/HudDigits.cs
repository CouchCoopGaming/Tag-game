namespace Tag.Core
{
    /// <summary>
    /// Prebuilt HUD numerals. OnGUI reads these instead of formatting a string every frame.
    /// </summary>
    public static class HudDigits
    {
        const int WholeMax = 400;
        const int TenthMax = 1200;
        const int HundredthMax = 200;

        static readonly string[] Whole = BuildWhole();
        static readonly string[] Tenth = BuildTenth();
        static readonly string[] Hundredth = BuildHundredth();
        static readonly string[] WholeS = Suffix(Whole, "s");
        static readonly string[] TenthS = Suffix(Tenth, "s");
        static readonly string[] Meters = Suffix(Whole, "m");

        public static string Whole0(float value) => Pick(Whole, value, 1f);
        public static string Tenth0(float value) => Pick(Tenth, value, 10f);
        public static string Hundredth0(float value) => Pick(Hundredth, value, 100f);
        public static string WholeSeconds(float value) => Pick(WholeS, value, 1f);
        public static string TenthSeconds(float value) => Pick(TenthS, value, 10f);
        public static string Meters0(float value) => Pick(Meters, value, 1f);

        public static string Kph(float metersPerSecond)
        {
            float kph = metersPerSecond * 3.6f;
            if (kph < 0f) kph = 0f;
            return Whole0(kph);
        }

        /// <summary>Dash label: whole seconds at 10s and up, one decimal under that.</summary>
        public static string DashCd(float rem)
        {
            if (rem >= 10f) return WholeSeconds(rem);
            return TenthSeconds(rem);
        }

        static string Pick(string[] table, float value, float scale)
        {
            if (value < 0f) value = 0f;
            int i = (int)(value * scale + 0.5f);
            if (i >= table.Length) i = table.Length - 1;
            return table[i];
        }

        static string[] BuildWhole()
        {
            var t = new string[WholeMax + 1];
            for (int i = 0; i < t.Length; i++)
                t[i] = i.ToString();
            return t;
        }

        static string[] BuildTenth()
        {
            var t = new string[TenthMax + 1];
            for (int i = 0; i < t.Length; i++)
            {
                int whole = i / 10;
                int frac = i % 10;
                t[i] = whole.ToString() + "." + frac.ToString();
            }
            return t;
        }

        static string[] BuildHundredth()
        {
            var t = new string[HundredthMax + 1];
            for (int i = 0; i < t.Length; i++)
            {
                int whole = i / 100;
                int frac = i % 100;
                string fracText = frac < 10 ? "0" + frac.ToString() : frac.ToString();
                t[i] = whole.ToString() + "." + fracText;
            }
            return t;
        }

        static string[] Suffix(string[] source, string suffix)
        {
            var t = new string[source.Length];
            for (int i = 0; i < source.Length; i++)
                t[i] = source[i] + suffix;
            return t;
        }
    }
}
