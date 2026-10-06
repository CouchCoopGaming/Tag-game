using System;
using System.Globalization;
using System.Text;

namespace Tag.Practice
{
    /// <summary>
    /// Personal bests and checkpoint splits. Stored in the same settings blob
    /// as look and audio. Missing lines keep an empty board.
    /// </summary>
    public static class PracticeBests
    {
        public const int Slots = 16;
        public const int Splits = 8;

        static readonly string[] Ids = new string[Slots];
        static readonly float[] Times = new float[Slots];
        static readonly float[] Split = new float[Slots * Splits];
        static readonly int[] SplitCount = new int[Slots];

        public static void Clear()
        {
            for (int i = 0; i < Slots; i++)
            {
                Ids[i] = null;
                Times[i] = 0f;
                SplitCount[i] = 0;
            }
        }

        public static bool Has(string id)
        {
            return Slot(id, false) >= 0 && TimeOf(id) > 0f;
        }

        public static float TimeOf(string id)
        {
            int slot = Slot(id, false);
            if (slot < 0) return 0f;
            return Times[slot];
        }

        public static float SplitOf(string id, int index)
        {
            int slot = Slot(id, false);
            if (slot < 0 || index < 0 || index >= SplitCount[slot]) return 0f;
            return Split[slot * Splits + index];
        }

        public static int SplitsOf(string id)
        {
            int slot = Slot(id, false);
            if (slot < 0) return 0;
            return SplitCount[slot];
        }

        public static void Set(string id, float time, float[] splits, int count)
        {
            if (string.IsNullOrEmpty(id) || time <= 0f) return;
            int slot = Slot(id, true);
            if (slot < 0) return;
            Ids[slot] = id;
            Times[slot] = time;
            if (count < 0) count = 0;
            if (count > Splits) count = Splits;
            SplitCount[slot] = count;
            int o = slot * Splits;
            for (int i = 0; i < count; i++)
                Split[o + i] = splits != null && i < splits.Length ? splits[i] : 0f;
        }

        public static void SetTime(string id, float time)
        {
            if (string.IsNullOrEmpty(id)) return;
            int slot = Slot(id, true);
            if (slot < 0) return;
            Ids[slot] = id;
            Times[slot] = time;
        }

        public static void SetSplits(string id, string text)
        {
            if (string.IsNullOrEmpty(id)) return;
            int slot = Slot(id, true);
            if (slot < 0) return;
            Ids[slot] = id;
            int o = slot * Splits;
            int n = 0;
            if (!string.IsNullOrEmpty(text))
            {
                int i = 0;
                while (i < text.Length && n < Splits)
                {
                    int comma = text.IndexOf(',', i);
                    if (comma < 0) comma = text.Length;
                    string part = text.Substring(i, comma - i).Trim();
                    if (part.Length > 0 && float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                    {
                        Split[o + n] = v;
                        n++;
                    }
                    i = comma + 1;
                }
            }
            SplitCount[slot] = n;
        }

        public static void Write(StringBuilder text)
        {
            for (int s = 0; s < Slots; s++)
            {
                if (string.IsNullOrEmpty(Ids[s]) || Times[s] <= 0f) continue;
                text.Append("pb.");
                text.Append(Ids[s]);
                text.Append('=');
                text.Append(Times[s].ToString("0.000", CultureInfo.InvariantCulture));
                text.Append('\n');
                if (SplitCount[s] < 1) continue;
                text.Append("sp.");
                text.Append(Ids[s]);
                text.Append('=');
                int o = s * Splits;
                for (int i = 0; i < SplitCount[s]; i++)
                {
                    if (i > 0) text.Append(',');
                    text.Append(Split[o + i].ToString("0.000", CultureInfo.InvariantCulture));
                }
                text.Append('\n');
            }
        }

        static int Slot(string id, bool claim)
        {
            int free = -1;
            for (int i = 0; i < Slots; i++)
            {
                if (Ids[i] == id) return i;
                if (free < 0 && string.IsNullOrEmpty(Ids[i])) free = i;
            }
            return claim ? free : -1;
        }
    }
}
