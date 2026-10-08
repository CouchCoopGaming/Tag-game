using System.Globalization;
using System.Text;

namespace Tag.FX
{
    /// <summary>
    /// Per-effect switches for the FX kit. Defaults are on.
    /// The pause settings row count stays 21. This blob is its own file.
    /// </summary>
    public static class FxKitOptions
    {
        public const int Land = 0;
        public const int Grapple = 1;
        public const int Immunity = 2;
        public const int Stagger = 3;
        public const int Launch = 4;
        public const int Wall = 5;
        public const int TagFlash = 6;
        public const int Count = 7;

        public static readonly bool[] On = new bool[Count];

        /// <summary>
        /// Speed lines on air dash and grapple pull. Off unless turned on,
        /// so a 4-way split does not gain another moving layer. Not one of the seven rows.
        /// </summary>
        public static bool SpeedLines;
        static readonly string[] Keys =
        {
            "land", "grapple", "immune", "stagger", "launch", "wall", "tag"
        };
        static readonly string[] Names =
        {
            "Landing impact",
            "Grapple hook",
            "Immunity glow",
            "Punch stagger",
            "Launch pad",
            "Wall run",
            "Tag flash"
        };
        static readonly string[] OnText = new string[Count];
        static readonly string[] OffText = new string[Count];
        public const string Title = "FX kit";

        static FxKitOptions()
        {
            Reset();
        }

        public static void Reset()
        {
            for (int i = 0; i < Count; i++)
                On[i] = true;
            SpeedLines = false;
            Rebuild();
        }

        public static void ToggleSpeedLines()
        {
            SpeedLines = !SpeedLines;
        }

        public static bool Enabled(int slot)
        {
            if (slot < 0 || slot >= Count) return false;
            return On[slot];
        }

        public static string Label(int slot)
        {
            if (slot < 0 || slot >= Count) return Title;
            return On[slot] ? OnText[slot] : OffText[slot];
        }

        public static void Toggle(int slot)
        {
            if (slot < 0 || slot >= Count) return;
            On[slot] = !On[slot];
            Rebuild();
        }

        public static string Write()
        {
            var text = new StringBuilder();
            text.Append("v=1\n");
            for (int i = 0; i < Count; i++)
            {
                text.Append(Keys[i]);
                text.Append('=');
                text.Append(On[i] ? '1' : '0');
                text.Append('\n');
            }
            text.Append("lines=");
            text.Append(SpeedLines ? '1' : '0');
            text.Append('\n');
            return text.ToString();
        }

        public static void Read(string blob)
        {
            if (string.IsNullOrEmpty(blob)) return;
            int i = 0;
            while (i < blob.Length)
            {
                int nl = blob.IndexOf('\n', i);
                if (nl < 0) nl = blob.Length;
                int eq = -1;
                for (int c = i; c < nl; c++)
                {
                    if (blob[c] == '=')
                    {
                        eq = c;
                        break;
                    }
                }
                if (eq > i)
                {
                    int slot = Match(blob, i, eq);
                    if (slot >= 0 && eq + 1 < nl)
                        On[slot] = blob[eq + 1] != '0';
                    else if (SameKey(blob, i, eq, "lines") && eq + 1 < nl)
                        SpeedLines = blob[eq + 1] != '0';
                }
                i = nl + 1;
            }
            Rebuild();
        }

        static bool SameKey(string blob, int start, int end, string key)
        {
            int len = end - start;
            if (key.Length != len) return false;
            for (int c = 0; c < len; c++)
            {
                if (blob[start + c] != key[c]) return false;
            }
            return true;
        }

        static int Match(string blob, int start, int end)
        {
            int len = end - start;
            for (int s = 0; s < Count; s++)
            {
                string key = Keys[s];
                if (key.Length != len) continue;
                bool same = true;
                for (int c = 0; c < len; c++)
                {
                    if (blob[start + c] != key[c])
                    {
                        same = false;
                        break;
                    }
                }
                if (same) return s;
            }
            return -1;
        }

        static void Rebuild()
        {
            for (int i = 0; i < Count; i++)
            {
                OnText[i] = Names[i] + "  On";
                OffText[i] = Names[i] + "  Off";
            }
        }

        public static string ProofBits()
        {
            int n = 0;
            for (int i = 0; i < Count; i++)
            {
                if (On[i]) n++;
            }
            return n.ToString(CultureInfo.InvariantCulture);
        }
    }
}
