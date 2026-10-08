using Tag.Settings;

namespace Tag.FX
{
    /// <summary>
    /// POP, POW, BAM, WHAM. A punch leans small. A tag leans big.
    /// The same word never plays twice in a row. Visual only.
    /// </summary>
    public static class ComicWords
    {
        public const int Count = 4;
        public const float PopSeconds = 0.05f;
        public const float LifeSeconds = 0.45f;
        public const float HoldSeconds = 0.28f;
        public const int Pop = 0;
        public const int Pow = 1;
        public const int Bam = 2;
        public const int Wham = 3;

        public static readonly string[] Text = { "POP!", "POW!", "BAM!", "WHAM!" };

        /// <summary>5-wide rows, 7 rows, low bits. P O W B A M ! H</summary>
        public static readonly byte[] GlyphP = { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 };
        public static readonly byte[] GlyphO = { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E };
        public static readonly byte[] GlyphW = { 0x11, 0x11, 0x11, 0x15, 0x15, 0x15, 0x0A };
        public static readonly byte[] GlyphB = { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E };
        public static readonly byte[] GlyphA = { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 };
        public static readonly byte[] GlyphM = { 0x11, 0x1B, 0x15, 0x11, 0x11, 0x11, 0x11 };
        public static readonly byte[] GlyphBang = { 0x04, 0x04, 0x04, 0x04, 0x04, 0x00, 0x04 };
        public static readonly byte[] GlyphH = { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 };

        public static byte[] Glyph(int index)
        {
            switch (index)
            {
                case 0: return GlyphP;
                case 1: return GlyphO;
                case 2: return GlyphW;
                case 3: return GlyphB;
                case 4: return GlyphA;
                case 5: return GlyphM;
                case 6: return GlyphBang;
                default: return GlyphH;
            }
        }

        public static int LetterCount(int word)
        {
            if (word == Wham) return 5;
            return 4;
        }

        public static int Letter(int word, int place)
        {
            if (word == Pop) return place == 0 || place == 2 ? 0 : place == 1 ? 1 : 6;
            if (word == Pow) return place == 0 ? 0 : place == 1 ? 1 : place == 2 ? 2 : 6;
            if (word == Bam) return place == 0 ? 3 : place == 1 ? 4 : place == 2 ? 5 : 6;
            if (place == 0) return 2;
            if (place == 1) return 7;
            if (place == 2) return 4;
            if (place == 3) return 5;
            return 6;
        }

        public static bool Visible(GameSettings settings)
        {
            if (settings == null) return true;
            if (!settings.ComicWords) return false;
            if (settings.AnyReduceFlash()) return false;
            return true;
        }

        /// <summary>0.15 at the hit, full size by 0.05 s, then it holds.</summary>
        public static float Scale(float age)
        {
            if (age <= 0f) return 0.15f;
            if (age < PopSeconds)
            {
                float u = age / PopSeconds;
                float e = u * u * (3f - 2f * u);
                return 0.15f + 0.85f * e;
            }
            return 1f;
        }

        public static float Alpha(float age)
        {
            if (age < 0f || age >= LifeSeconds) return 0f;
            if (age < PopSeconds) return age / PopSeconds;
            if (age < PopSeconds + HoldSeconds) return 1f;
            float span = LifeSeconds - PopSeconds - HoldSeconds;
            if (span < 0.01f) return 0f;
            float u = (age - PopSeconds - HoldSeconds) / span;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return 1f - u;
        }

        public static void ColorOf(int word, out float r, out float g, out float b)
        {
            if (word == Pow) { r = 1f; g = 0.46f; b = 0.08f; return; }
            if (word == Bam) { r = 0.95f; g = 0.12f; b = 0.18f; return; }
            if (word == Wham) { r = 0.62f; g = 0.18f; b = 0.95f; return; }
            r = 1f; g = 0.86f; b = 0.12f;
        }

        /// <summary>
        /// Tags spend most rolls on BAM and WHAM. Punches spend most rolls on POP and POW.
        /// A hit that would repeat the last word steps to the other word in that pair.
        /// </summary>
        public static int Pick(ref uint state, int previous, bool tag)
        {
            state = state * 1664525u + 1013904223u;
            int word = (int)((state >> 16) & 3u);
            state = state * 1664525u + 1013904223u;
            uint bias = (state >> 16) & 3u;
            if (tag)
            {
                if (word < 2 && bias != 0u) word += 2;
            }
            else if (word >= 2 && bias != 0u)
                word -= 2;
            if (previous >= 0 && word == previous)
            {
                if (tag) word = previous == Bam ? Wham : Bam;
                else word = previous == Pop ? Pow : Pop;
            }
            if (word < 0) word = 0;
            if (word > 3) word = 3;
            return word;
        }

        public static bool Holds()
        {
            if (Count != 4) return false;
            if (PopSeconds < 0.04f || PopSeconds > 0.06f) return false;
            if (LifeSeconds < 0.40f || LifeSeconds > 0.50f) return false;
            if (Scale(0f) > 0.20f || Scale(PopSeconds) < 0.98f) return false;
            if (Alpha(0.10f) < 0.99f) return false;
            if (Alpha(LifeSeconds) > 0.001f) return false;
            if (Alpha(LifeSeconds - 0.02f) <= 0f) return false;

            uint state = 1u;
            int prev = -1;
            int tagBig = 0;
            int repeats = 0;
            for (int i = 0; i < 200; i++)
            {
                int word = Pick(ref state, prev, true);
                if (word == prev) repeats++;
                if (word >= Bam) tagBig++;
                prev = word;
            }
            if (repeats != 0 || tagBig < 140) return false;

            state = 1u;
            prev = -1;
            int punchSmall = 0;
            for (int i = 0; i < 200; i++)
            {
                int word = Pick(ref state, prev, false);
                if (word == prev) repeats++;
                if (word <= Pow) punchSmall++;
                prev = word;
            }
            if (repeats != 0 || punchSmall < 140) return false;

            GameSettings settings = GameSettings.Defaults();
            if (!settings.ComicWords) return false;
            if (!Visible(settings)) return false;
            settings.ReduceFlash[0] = true;
            if (Visible(settings)) return false;
            settings.ReduceFlash[0] = false;
            settings.Nudge(GameSettings.RowComic, 1);
            if (settings.ComicWords || Visible(settings)) return false;
            string label = settings.RowLabel(GameSettings.RowComic);
            if (label.IndexOf("Comic", System.StringComparison.Ordinal) < 0) return false;
            string blob = SettingsFile.Write(settings, ActionBinds.Defaults());
            GameSettings loaded = GameSettings.Defaults();
            SettingsFile.Read(blob, loaded, ActionBinds.Defaults());
            if (loaded.ComicWords) return false;
            loaded.Nudge(GameSettings.RowComic, 1);
            if (!loaded.ComicWords) return false;
            return true;
        }

        public static string ProofLine()
        {
            uint state = 9u;
            int prev = -1;
            int repeats = 0;
            int tagBig = 0;
            int punchSmall = 0;
            uint tagState = state;
            int tagPrev = -1;
            for (int i = 0; i < 200; i++)
            {
                int word = Pick(ref tagState, tagPrev, true);
                if (word == tagPrev) repeats++;
                if (word >= Bam) tagBig++;
                tagPrev = word;
            }
            uint punchState = state;
            int punchPrev = -1;
            for (int i = 0; i < 200; i++)
            {
                int word = Pick(ref punchState, punchPrev, false);
                if (word == punchPrev) repeats++;
                if (word <= Pow) punchSmall++;
                punchPrev = word;
            }
            return "comic-words"
                + " words=4"
                + " repeat=" + repeats.ToString()
                + " tagBig=" + tagBig.ToString()
                + " punchSmall=" + punchSmall.ToString()
                + " pop=" + PopSeconds.ToString("0.00")
                + " life=" + LifeSeconds.ToString("0.00")
                + " toggle=1";
        }
    }
}
