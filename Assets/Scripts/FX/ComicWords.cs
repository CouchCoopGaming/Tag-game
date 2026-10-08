using Tag.Settings;

namespace Tag.FX
{
    /// <summary>
    /// POP, POW, BAM, WHAM. Visual only.
    /// Pick(strength) follows the hit: a light tap is POP, a punch is POW,
    /// a punch at sprint speed is BAM, and a tag is WHAM.
    /// Pick(ref state, previous, tag) is the older biased roll. It does not repeat a word.
    /// </summary>
    public static class ComicWords
    {
        public const int Count = 4;
        public const float PopSeconds = 0.05f;
        public const float LifeSeconds = 0.45f;
        public const float HoldSeconds = 0.28f;
        public const float TiltDegrees = 12f;
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
            if (settings.Effects <= 0) return false;
            return true;
        }

        /// <summary>
        /// Extra size on the word during the shared overshoot. 1 the rest of the life,
        /// so the word and the burst are born together and then scale as one unit.
        /// </summary>
        public static float WordPunch(float age)
        {
            float peakAt = PopSeconds * 0.58f;
            const float extra = 0.18f;
            if (age <= 0f || age >= PopSeconds) return 1f;
            if (age < peakAt)
            {
                float t = age / peakAt;
                float e = t * t * (3f - 2f * t);
                return 1f + extra * e;
            }
            float settle = (age - peakAt) / (PopSeconds - peakAt);
            float down = settle * settle * (3f - 2f * settle);
            return 1f + extra * (1f - down);
        }

        /// <summary>
        /// Starts at 0, overshoots to 1.25, is back at 1 by 0.05 s, then shrinks away by 0.45 s.
        /// </summary>
        public static float Scale(float age)
        {
            if (age <= 0f || age >= LifeSeconds) return 0f;
            if (age < PopSeconds)
            {
                float u = age / PopSeconds;
                const float peakAt = 0.58f;
                const float peak = 1.25f;
                if (u < peakAt)
                {
                    float t = u / peakAt;
                    float e = t * t * (3f - 2f * t);
                    return peak * e;
                }
                float settle = (u - peakAt) / (1f - peakAt);
                float down = settle * settle * (3f - 2f * settle);
                return peak + (1f - peak) * down;
            }
            float span = LifeSeconds - PopSeconds;
            float v = (age - PopSeconds) / span;
            float ease = v * v * (3f - 2f * v);
            return 1f - ease;
        }

        /// <summary>A few degrees of wobble that dies before the hold. Radians.</summary>
        public static float Wobble(float age)
        {
            if (age <= 0f) return 0f;
            float settle = PopSeconds * 3f;
            if (age >= settle) return 0f;
            float decay = 1f - age / settle;
            return (float)System.Math.Sin(age * 48f) * 0.07f * decay;
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

        /// <summary>Random tilt in ±12 degrees, in radians.</summary>
        public static float TiltRadians(uint bits)
        {
            float u = (bits & 255u) / 255f;
            return (u * 2f - 1f) * TiltDegrees * 0.017453292f;
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

        public const int Tap = 0;
        public const int Punch = 1;
        public const int SprintPunch = 2;
        public const int TagHit = 3;
        public const float PunchSpeed = 6.9f;
        public const float SprintPunchSpeed = 13.8f;

        /// <summary>
        /// A tag is WHAM. A punch at sprint speed is BAM. A punch at walk speed
        /// or faster is POW. Slower than a walk, including a light tap, is POP.
        /// </summary>
        public static int Strength(bool tag, float speed)
        {
            if (tag) return TagHit;
            if (speed >= SprintPunchSpeed) return SprintPunch;
            if (speed >= PunchSpeed) return Punch;
            return Tap;
        }

        /// <summary>
        /// Light tap POP, punch POW, sprint punch BAM, tag WHAM.
        /// The biased roll is unchanged.
        /// </summary>
        public static int Pick(int strength)
        {
            if (strength <= Tap) return Pop;
            if (strength == Punch) return Pow;
            if (strength == SprintPunch) return Bam;
            return Wham;
        }

        public static bool Holds()
        {
            if (Count != 4) return false;
            if (PopSeconds < 0.04f || PopSeconds > 0.06f) return false;
            if (LifeSeconds < 0.40f || LifeSeconds > 0.50f) return false;
            if (Scale(0f) > 0.20f || Scale(PopSeconds) < 0.98f) return false;
            if (Scale(PopSeconds * 0.58f) < 1.15f) return false;
            float wob = Wobble(0.03f);
            if (wob < 0.02f || wob > 0.12f) return false;
            if (Wobble(0f) != 0f || Wobble(LifeSeconds) != 0f) return false;
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
            loaded.Effects = 0;
            if (Visible(loaded)) return false;
            loaded.Effects = 2;
            if (!Visible(loaded)) return false;
            float hi = TiltRadians(255u);
            float lo = TiltRadians(0u);
            float lim = TiltDegrees * 0.017453292f + 0.0001f;
            if (hi > lim || lo < -lim || hi <= 0f || lo >= 0f) return false;
            if (Pick(Strength(false, 0f)) != Pop) return false;
            if (Pick(Strength(false, 6.8f)) != Pop) return false;
            if (Pick(Strength(false, PunchSpeed)) != Pow) return false;
            if (Pick(Strength(false, 9f)) != Pow) return false;
            if (Pick(Strength(false, SprintPunchSpeed)) != Bam) return false;
            if (Pick(Strength(false, 20f)) != Bam) return false;
            if (Pick(Strength(true, 0f)) != Wham) return false;
            if (Pick(Strength(true, SprintPunchSpeed)) != Wham) return false;
            if (WordScale(0f) != 0f || WordScale(PopSeconds) != 0f) return false;
            if (WordScale(PopSeconds + 0.044f) < 1.10f) return false;
            if (WordScale(0.20f) < 0.98f || WordScale(LifeSeconds) < 0.98f) return false;
            if (!PoolsHold()) return false;
            if (WordCount != 36) return false;
            if (!AtlasHolds()) return false;
            if (!StylesHold()) return false;
            if (ClampAxis(1f, 0.25f) != 0.75f) return false;
            if (ClampAxis(0f, 0.25f) != 0f) return false;
            if (ClampAxis(-1f, 0.25f) != -0.75f) return false;
            string proof = ProofLine();
            if (proof.IndexOf("words=36", System.StringComparison.Ordinal) < 0) return false;
            if (proof.IndexOf("contact=14", System.StringComparison.Ordinal) < 0) return false;
            if (proof.IndexOf("whiff=13", System.StringComparison.Ordinal) < 0) return false;
            if (proof.IndexOf("land=5", System.StringComparison.Ordinal) < 0) return false;
            if (proof.IndexOf("crash=4", System.StringComparison.Ordinal) < 0) return false;
            if (proof.IndexOf("repeat=0,0,0,0", System.StringComparison.Ordinal) < 0) return false;
            return true;
        }

        /// <summary>
        /// Pull a viewport axis back inside. pad is the quad's half-extent in the same space.
        /// </summary>
        public static float ClampAxis(float center, float pad)
        {
            float limit = 1f - pad;
            if (limit < 0f) limit = 0f;
            if (center < -limit) return -limit;
            if (center > limit) return limit;
            return center;
        }

        public const int EvPunch = 0;
        public const int EvTag = 1;
        public const int EvTransfer = 2;
        public const int EvWhiff = 3;
        public const int EvLand = 4;
        public const int EvLaunch = 5;
        public const int EvZip = 6;
        public const int EvGrapple = 7;
        public const int EvWall = 8;
        public const int EvStagger = 9;
        public const int EvCount = 10;

        /// <summary>
        /// Each event has 3–6 words. A roll never repeats the previous word.
        /// The atlas cell for a pool index is AtlasOf.
        /// </summary>
        public static int PoolCount(int ev)
        {
            return Pool(ev).Length;
        }

        public static string PoolWord(int ev, int index)
        {
            string[] pool = Pool(ev);
            if (index < 0 || index >= pool.Length) return pool[0];
            return pool[index];
        }

        public static void Accent(int ev, out float r, out float g, out float b)
        {
            if (ev == EvTag) { r = 0.62f; g = 0.18f; b = 0.95f; return; }
            if (ev == EvTransfer) { r = 0.95f; g = 0.22f; b = 0.45f; return; }
            if (ev == EvWhiff) { r = 0.45f; g = 0.72f; b = 0.95f; return; }
            if (ev == EvLand) { r = 0.62f; g = 0.42f; b = 0.22f; return; }
            if (ev == EvLaunch) { r = 0.95f; g = 0.78f; b = 0.12f; return; }
            if (ev == EvZip) { r = 0.20f; g = 0.82f; b = 0.85f; return; }
            if (ev == EvGrapple) { r = 0.15f; g = 0.55f; b = 0.48f; return; }
            if (ev == EvWall) { r = 0.90f; g = 0.28f; b = 0.16f; return; }
            if (ev == EvStagger) { r = 0.55f; g = 0.62f; b = 0.28f; return; }
            r = 1f; g = 0.46f; b = 0.08f;
        }

        /// <summary>
        /// Word pop, after the burst has already scaled in. Peaks at 1.15, settles at 1.
        /// The burst curve and the 0.45 s life are unchanged.
        /// </summary>
        public static float WordScale(float age)
        {
            float start = PopSeconds;
            const float span = 0.08f;
            if (age <= start) return 0f;
            float u = (age - start) / span;
            if (u >= 1f) return 1f;
            const float peakAt = 0.55f;
            const float peak = 1.15f;
            if (u < peakAt)
            {
                float t = u / peakAt;
                float e = t * t * (3f - 2f * t);
                return peak * e;
            }
            float settle = (u - peakAt) / (1f - peakAt);
            float down = settle * settle * (3f - 2f * settle);
            return peak + (1f - peak) * down;
        }

        /// <summary>
        /// Punch strength narrows the punch pool. Other events use the whole pool.
        /// The biased four-word roll is unchanged.
        /// </summary>
        public static int PickEvent(ref uint state, int ev, int strength, string previous)
        {
            string[] pool = Pool(ev);
            int start = 0;
            int span = pool.Length;
            if (ev == EvPunch)
            {
                // Five words: tap is the light end, a sprint punch is the heavy end.
                if (strength <= Tap) { start = 0; span = 3; }
                else if (strength == Punch) { start = 1; span = 4; }
                else { start = pool.Length - 3; span = 3; }
            }
            state = state * 1664525u + 1013904223u;
            int pick = start + (int)((state >> 16) % (uint)span);
            if (!string.IsNullOrEmpty(previous) && pool[pick] == previous)
                pick = start + ((pick - start + 1) % span);
            return pick;
        }

        static bool PoolsHold()
        {
            string previous = null;
            uint state = 5u;
            for (int ev = 0; ev < EvCount; ev++)
            {
                int n = PoolCount(ev);
                if (n < 3 || n > 6) return false;
                for (int i = 0; i < n; i++)
                {
                    string word = PoolWord(ev, i);
                    if (string.IsNullOrEmpty(word) || word[word.Length - 1] != '!') return false;
                }
                for (int k = 0; k < 24; k++)
                {
                    int strength = k % 3;
                    int pick = PickEvent(ref state, ev, strength, previous);
                    string word = PoolWord(ev, pick);
                    if (word == previous) return false;
                    previous = word;
                }
            }
            if (PickEvent(ref state, EvPunch, Tap, null) < 0) return false;
            if (PoolWord(EvPunch, 0) != "POP!") return false;
            if (PoolWord(EvTag, 0) != "WHAM!") return false;
            if (PoolWord(EvGrapple, 0) != "THWIP!") return false;
            if (PoolWord(EvStagger, 0) != "OOF!") return false;
            return true;
        }

        static readonly string[] PunchWords = { "POP!", "POW!", "SMACK!", "WHACK!", "BAM!" };
        static readonly string[] TagWords = { "WHAM!", "BONK!", "KAPOW!" };
        static readonly string[] TransferWords = { "TAG!", "GOTCHA!", "MINE!" };
        static readonly string[] WhiffWords = { "WHIFF!", "SWISH!", "WHOOSH!", "MISS!" };
        static readonly string[] LandWords = { "THUD!", "WHUMP!", "THUMP!", "BOOM!", "KRUNCH!" };
        static readonly string[] LaunchWords = { "BOING!", "SPROING!", "POING!" };
        static readonly string[] ZipWords = { "ZING!", "ZIP!", "WHIZZ!" };
        static readonly string[] GrappleWords = { "THWIP!", "FWIP!", "ZWIP!" };
        static readonly string[] WallWords = { "KRAK!", "SLAM!", "SPLAT!", "THWACK!" };
        static readonly string[] StaggerWords = { "OOF!", "UGH!", "OUCH!" };

        public const int WordCount = 36;
        public const int CatContact = 0;
        public const int CatWhiff = 1;
        public const int CatLand = 2;
        public const int CatCrash = 3;
        public const int CatCount = 4;

        // Atlas cells. The first four stay POP, POW, BAM, WHAM.
        static readonly int[] PunchAtlas = { 0, 1, 4, 5, 2 };
        static readonly int[] TagAtlas = { 3, 7, 8 };
        static readonly int[] TransferAtlas = { 9, 10, 11 };
        static readonly int[] WhiffAtlas = { 12, 13, 14, 35 };
        static readonly int[] LandAtlas = { 15, 16, 17, 33, 34 };
        static readonly int[] LaunchAtlas = { 18, 19, 20 };
        static readonly int[] ZipAtlas = { 21, 22, 23 };
        static readonly int[] GrappleAtlas = { 24, 25, 26 };
        static readonly int[] WallAtlas = { 27, 28, 29, 6 };
        static readonly int[] StaggerAtlas = { 30, 31, 32 };

        public static int AtlasOf(int ev, int poolIndex)
        {
            int[] map = AtlasMap(ev);
            if (poolIndex < 0 || poolIndex >= map.Length) return map[0];
            return map[poolIndex];
        }

        static int[] AtlasMap(int ev)
        {
            if (ev == EvTag) return TagAtlas;
            if (ev == EvTransfer) return TransferAtlas;
            if (ev == EvWhiff) return WhiffAtlas;
            if (ev == EvLand) return LandAtlas;
            if (ev == EvLaunch) return LaunchAtlas;
            if (ev == EvZip) return ZipAtlas;
            if (ev == EvGrapple) return GrappleAtlas;
            if (ev == EvWall) return WallAtlas;
            if (ev == EvStagger) return StaggerAtlas;
            return PunchAtlas;
        }

        static bool AtlasHolds()
        {
            var seen = new bool[WordCount];
            for (int ev = 0; ev < EvCount; ev++)
            {
                int[] map = AtlasMap(ev);
                if (map.Length != PoolCount(ev)) return false;
                for (int i = 0; i < map.Length; i++)
                {
                    int id = map[i];
                    if (id < 0 || id >= WordCount || seen[id]) return false;
                    seen[id] = true;
                    if (PoolWord(ev, i) != AtlasWord(id)) return false;
                }
            }
            for (int i = 0; i < WordCount; i++)
            {
                if (!seen[i]) return false;
            }
            return AtlasWord(0) == "POP!" && AtlasWord(2) == "BAM!" && AtlasWord(3) == "WHAM!";
        }

        public static string AtlasWord(int index)
        {
            switch (index)
            {
                case 0: return "POP!";
                case 1: return "POW!";
                case 2: return "BAM!";
                case 3: return "WHAM!";
                case 4: return "SMACK!";
                case 5: return "WHACK!";
                case 6: return "THWACK!";
                case 7: return "BONK!";
                case 8: return "KAPOW!";
                case 9: return "TAG!";
                case 10: return "GOTCHA!";
                case 11: return "MINE!";
                case 12: return "WHIFF!";
                case 13: return "SWISH!";
                case 14: return "WHOOSH!";
                case 15: return "THUD!";
                case 16: return "WHUMP!";
                case 17: return "THUMP!";
                case 18: return "BOING!";
                case 19: return "SPROING!";
                case 20: return "POING!";
                case 21: return "ZING!";
                case 22: return "ZIP!";
                case 23: return "WHIZZ!";
                case 24: return "THWIP!";
                case 25: return "FWIP!";
                case 26: return "ZWIP!";
                case 27: return "KRAK!";
                case 28: return "SLAM!";
                case 29: return "SPLAT!";
                case 30: return "OOF!";
                case 31: return "UGH!";
                case 32: return "OUCH!";
                case 33: return "BOOM!";
                case 34: return "KRUNCH!";
                case 35: return "MISS!";
                default: return "OUCH!";
            }
        }

        static string[] Pool(int ev)
        {
            if (ev == EvTag) return TagWords;
            if (ev == EvTransfer) return TransferWords;
            if (ev == EvWhiff) return WhiffWords;
            if (ev == EvLand) return LandWords;
            if (ev == EvLaunch) return LaunchWords;
            if (ev == EvZip) return ZipWords;
            if (ev == EvGrapple) return GrappleWords;
            if (ev == EvWall) return WallWords;
            if (ev == EvStagger) return StaggerWords;
            return PunchWords;
        }

        public static string ProofLine()
        {
            int[] counts = new int[CatCount];
            for (int i = 0; i < WordCount; i++)
                counts[CategoryOf(i)]++;
            string cats = "";
            for (int c = 0; c < CatCount; c++)
            {
                string previous = null;
                int hits = 0;
                uint roll = (uint)(11 + c * 3);
                int n = counts[c];
                for (int k = 0; k < 40; k++)
                {
                    roll = roll * 1664525u + 1013904223u;
                    int pick = (int)((roll >> 16) % (uint)n);
                    string word = CategoryWord(c, pick);
                    if (!string.IsNullOrEmpty(previous) && word == previous)
                        pick = (pick + 1) % n;
                    word = CategoryWord(c, pick);
                    if (word == previous) hits++;
                    previous = word;
                }
                if (c > 0) cats += ",";
                cats += hits.ToString();
            }
            return "comic-words"
                + " words=" + WordCount.ToString()
                + " contact=" + counts[CatContact].ToString()
                + " whiff=" + counts[CatWhiff].ToString()
                + " land=" + counts[CatLand].ToString()
                + " crash=" + counts[CatCrash].ToString()
                + " repeat=" + cats
                + " pop=" + PopSeconds.ToString("0.00")
                + " life=" + LifeSeconds.ToString("0.00")
                + " toggle=1";
        }

        /// <summary>
        /// 0 contact, 1 whiff, 2 hard land, 3 crash. Every atlas cell is one of these.
        /// </summary>
        public static int CategoryOf(int atlas)
        {
            if (atlas == 6 || atlas == 27 || atlas == 28 || atlas == 29) return CatCrash;
            if (atlas == 15 || atlas == 16 || atlas == 17 || atlas == 33 || atlas == 34) return CatLand;
            if (atlas == 12 || atlas == 13 || atlas == 14 || atlas == 35) return CatWhiff;
            if (atlas >= 18 && atlas <= 26) return CatWhiff;
            return CatContact;
        }

        public static int CategoryCount(int cat)
        {
            int n = 0;
            for (int i = 0; i < WordCount; i++)
            {
                if (CategoryOf(i) == cat) n++;
            }
            return n;
        }

        public static string CategoryWord(int cat, int nth)
        {
            int seen = 0;
            for (int i = 0; i < WordCount; i++)
            {
                if (CategoryOf(i) != cat) continue;
                if (seen == nth) return AtlasWord(i);
                seen++;
            }
            return AtlasWord(0);
        }

        /// <summary>
        /// Each word has its own tilt in ±8–25°, plus a lean, a size, and a small arc.
        /// Neighbours never share a tilt.
        /// </summary>
        public static void StyleOf(int atlas, out float tiltDeg, out float skew, out float size, out float arc, out float wide, out float tall)
        {
            int i = atlas;
            if (i < 0) i = 0;
            int mag = 8 + ((i * 17) % 18);
            int sign = (i & 1) == 0 ? 1 : -1;
            tiltDeg = sign * mag;
            skew = ((i % 5) - 2) * 0.09f;
            size = 0.88f + (i % 7) * 0.04f;
            arc = ((i % 3) - 1) * 0.12f;
            int cat = CategoryOf(i);
            if (cat == CatWhiff)
            {
                wide = 1.14f;
                tall = 0.82f;
                skew += 0.10f * sign;
            }
            else if (cat == CatLand)
            {
                wide = 1.28f;
                tall = 0.74f;
            }
            else if (cat == CatCrash)
            {
                wide = 1.06f;
                tall = 1.08f;
                skew += 0.06f * -sign;
            }
            else
            {
                wide = 1f;
                tall = 1f;
            }
        }

        static bool StylesHold()
        {
            float prev = 0f;
            for (int i = 0; i < WordCount; i++)
            {
                float tilt, skew, size, arc, wide, tall;
                StyleOf(i, out tilt, out skew, out size, out arc, out wide, out tall);
                float abs = tilt < 0f ? -tilt : tilt;
                if (abs < 8f || abs > 25f) return false;
                if (i > 0 && tilt == prev) return false;
                if (size < 0.8f || size > 1.2f) return false;
                prev = tilt;
            }
            if (CategoryCount(CatContact) != 14) return false;
            if (CategoryCount(CatWhiff) != 13) return false;
            if (CategoryCount(CatLand) != 5) return false;
            if (CategoryCount(CatCrash) != 4) return false;
            return true;
        }
    }
}
