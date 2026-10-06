using System.Text;
using Tag.Core;
using Tag.Settings;

namespace Tag.MatchStats
{
    /// <summary>
    /// Per-player match story. Every counter lives in a buffer allocated once.
    /// A frame only writes numbers. Strings for the results card are built when the match seals.
    /// </summary>
    public static class MatchBook
    {
        public const int Cap = 8;
        public const int StatCount = 17;
        public const float PunchReach = 1.55f;

        public const string HotPotato = "Hot Potato";
        public const string Slipperiest = "Slipperiest";
        public const string SkyWalker = "Sky Walker";
        public const string WallCrawler = "Wall Crawler";

        public static int Count;
        public static bool Sealed;
        public static int AwardCount;

        public static readonly string[] Name = new string[Cap];
        public static readonly int[] TagsMade = new int[Cap];
        public static readonly int[] TimesTagged = new int[Cap];
        public static readonly float[] TimeAsIt = new float[Cap];
        public static readonly float[] LongestSurvival = new float[Cap];
        public static readonly float[] Distance = new float[Cap];
        public static readonly float[] TopSpeed = new float[Cap];
        public static readonly float[] AirTime = new float[Cap];
        public static readonly int[] WallRuns = new int[Cap];
        public static readonly int[] WallJumps = new int[Cap];
        public static readonly int[] Pads = new int[Cap];
        public static readonly int[] Zips = new int[Cap];
        public static readonly int[] AirDashes = new int[Cap];
        public static readonly int[] PunchesLanded = new int[Cap];
        public static readonly int[] PunchesWhiffed = new int[Cap];
        public static readonly int[] Staggers = new int[Cap];
        public static readonly int[] NearMisses = new int[Cap];
        public static readonly int[] TagBacksBlocked = new int[Cap];
        public static readonly string[] Card = new string[Cap];
        public static readonly string[] AwardLine = new string[3];
        public static readonly int[] AwardMask = new int[3];

        static readonly float[] Streak = new float[Cap];
        static readonly float[] Px = new float[Cap];
        static readonly float[] Py = new float[Cap];
        static readonly float[] Pz = new float[Cap];
        static readonly bool[] HasPos = new bool[Cap];
        static readonly float[] Scratch = new float[Cap];
        static readonly StringBuilder Sb = new StringBuilder(192);

        public static int It = -1;

        public static void ResetMatch()
        {
            Count = 0;
            Sealed = false;
            AwardCount = 0;
            It = -1;
            for (int i = 0; i < Cap; i++)
            {
                Name[i] = null;
                Card[i] = null;
                TagsMade[i] = 0;
                TimesTagged[i] = 0;
                TimeAsIt[i] = 0f;
                LongestSurvival[i] = 0f;
                Distance[i] = 0f;
                TopSpeed[i] = 0f;
                AirTime[i] = 0f;
                WallRuns[i] = 0;
                WallJumps[i] = 0;
                Pads[i] = 0;
                Zips[i] = 0;
                AirDashes[i] = 0;
                PunchesLanded[i] = 0;
                PunchesWhiffed[i] = 0;
                Staggers[i] = 0;
                NearMisses[i] = 0;
                TagBacksBlocked[i] = 0;
                Streak[i] = 0f;
                HasPos[i] = false;
            }
            for (int i = 0; i < AwardLine.Length; i++)
            {
                AwardLine[i] = null;
                AwardMask[i] = 0;
            }
        }

        public static void Open(string[] names, int n)
        {
            if (n < 0) n = 0;
            if (n > Cap) n = Cap;
            Count = n;
            Sealed = false;
            for (int i = 0; i < n; i++)
            {
                string id = names != null && i < names.Length && !string.IsNullOrEmpty(names[i]) ? names[i] : Fallback(i);
                Name[i] = id;
            }
        }

        public static void Open(int n)
        {
            if (n < 0) n = 0;
            if (n > Cap) n = Cap;
            Count = n;
            Sealed = false;
        }

        public static void EnsureCount(int n)
        {
            if (n > Cap) n = Cap;
            if (n > Count) Count = n;
        }

        public static void SetName(int i, string name)
        {
            if (i < 0 || i >= Cap) return;
            Name[i] = string.IsNullOrEmpty(name) ? Fallback(i) : name;
        }

        public static void SetIt(int index)
        {
            It = index;
        }

        /// <summary>Edges are rising edges for the frame. Passing true every frame would count every frame.</summary>
        public static void NoteMotion(int i, float x, float y, float z, float speed, float dt, bool isIt, bool airborne, bool wallRunEdge, bool wallJumpEdge, bool padEdge, bool zipEdge, bool dashEdge)
        {
            if (i < 0 || i >= Count) return;
            if (dt < 0f) dt = 0f;
            if (HasPos[i])
            {
                float dx = x - Px[i];
                float dy = y - Py[i];
                float dz = z - Pz[i];
                Distance[i] += (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            Px[i] = x;
            Py[i] = y;
            Pz[i] = z;
            HasPos[i] = true;
            if (speed < 0f) speed = 0f;
            if (speed > TopSpeed[i]) TopSpeed[i] = speed;
            if (isIt)
            {
                TimeAsIt[i] += dt;
                if (Streak[i] > LongestSurvival[i]) LongestSurvival[i] = Streak[i];
                Streak[i] = 0f;
            }
            else
            {
                Streak[i] += dt;
                if (Streak[i] > LongestSurvival[i]) LongestSurvival[i] = Streak[i];
            }
            if (airborne) AirTime[i] += dt;
            if (wallRunEdge) WallRuns[i]++;
            if (wallJumpEdge) WallJumps[i]++;
            if (padEdge) Pads[i]++;
            if (zipEdge) Zips[i]++;
            if (dashEdge) AirDashes[i]++;
        }

        public static void NoteTag(int from, int to)
        {
            if (from < 0 || to < 0 || from >= Count || to >= Count || from == to) return;
            TagsMade[from]++;
            TimesTagged[to]++;
            if (Streak[to] > LongestSurvival[to]) LongestSurvival[to] = Streak[to];
            Streak[to] = 0f;
            Streak[from] = 0f;
            It = to;
            MatchHighlight.MarkTag();
        }

        public static void NoteLanded(int i)
        {
            if (i < 0 || i >= Count) return;
            PunchesLanded[i]++;
        }

        public static void NoteWhiff(int i)
        {
            if (i < 0 || i >= Count) return;
            PunchesWhiffed[i]++;
        }

        public static void NoteStagger(int i)
        {
            if (i < 0 || i >= Count) return;
            Staggers[i]++;
        }

        public static void NoteBlocked(int i)
        {
            if (i < 0 || i >= Count) return;
            TagBacksBlocked[i]++;
        }

        public static void NoteNear(int i, float dist)
        {
            if (i < 0 || i >= Count) return;
            NearMisses[i]++;
            MatchHighlight.ConsiderMiss(dist);
        }

        public static void Seal()
        {
            for (int i = 0; i < Count; i++)
            {
                if (Streak[i] > LongestSurvival[i]) LongestSurvival[i] = Streak[i];
            }
            Pick();
            BuildCards();
            Sealed = true;
            MatchHighlight.BeginPlayback();
        }

        /// <summary>
        /// Every seat that shares the high value. A tie keeps every seat, including seats after the first.
        /// A zero high value awards nobody.
        /// </summary>
        public static int Leaders(float[] values, int n)
        {
            if (values == null || n < 1) return 0;
            if (n > values.Length) n = values.Length;
            float best = values[0];
            for (int i = 1; i < n; i++)
            {
                if (values[i] > best) best = values[i];
            }
            if (best <= 0.0001f) return 0;
            int mask = 0;
            for (int i = 0; i < n; i++)
            {
                if (values[i] >= best - 0.0001f)
                    mask |= 1 << i;
            }
            return mask;
        }

        /// <summary>HUD scale from settings. The basis is already couch-sized, so the small step stays readable.</summary>
        public static int CouchFont(int basis, float hud)
        {
            if (basis < 8) basis = 8;
            if (hud < 0.75f) hud = 0.75f;
            if (hud > 1.5f) hud = 1.5f;
            int size = (int)(basis * hud + 0.5f);
            if (size < 16) size = 16;
            return size;
        }

        static void Pick()
        {
            AwardCount = 0;
            Consider(HotPotato, TagsMade);
            Consider(Slipperiest, NearMisses);
            ConsiderFloat(SkyWalker, AirTime);
            Consider(WallCrawler, WallRuns);
        }

        static void Consider(string title, int[] values)
        {
            if (AwardCount >= 3) return;
            for (int i = 0; i < Count; i++)
                Scratch[i] = values[i];
            int mask = Leaders(Scratch, Count);
            if (mask == 0) return;
            Push(title, mask);
        }

        static void ConsiderFloat(string title, float[] values)
        {
            if (AwardCount >= 3) return;
            for (int i = 0; i < Count; i++)
                Scratch[i] = values[i];
            int mask = Leaders(Scratch, Count);
            if (mask == 0) return;
            Push(title, mask);
        }

        static void Push(string title, int mask)
        {
            AwardMask[AwardCount] = mask;
            AwardLine[AwardCount] = title + "  " + NamesFor(mask);
            AwardCount++;
        }

        static string NamesFor(int mask)
        {
            Sb.Clear();
            bool any = false;
            for (int i = 0; i < Count; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                if (any) Sb.Append(" + ");
                any = true;
                Sb.Append(AccessibilityPalette.Glyph(i & 3));
                Sb.Append(' ');
                Sb.Append(Name[i] ?? Fallback(i));
            }
            return Sb.ToString();
        }

        static void BuildCards()
        {
            for (int i = 0; i < Count; i++)
            {
                Sb.Clear();
                Sb.Append(AccessibilityPalette.Glyph(i & 3));
                Sb.Append(' ');
                Sb.Append(Name[i] ?? Fallback(i));
                Sb.Append('\n');
                Sb.Append("tags ");
                Sb.Append(HudDigits.Whole0(TagsMade[i]));
                Sb.Append("   tagged ");
                Sb.Append(HudDigits.Whole0(TimesTagged[i]));
                Sb.Append('\n');
                Sb.Append("It ");
                Sb.Append(HudDigits.Tenth0(TimeAsIt[i]));
                Sb.Append("s   live ");
                Sb.Append(HudDigits.Tenth0(LongestSurvival[i]));
                Sb.Append("s");
                Sb.Append('\n');
                Sb.Append("run ");
                Sb.Append(HudDigits.Meters0(Distance[i]));
                Sb.Append("   top ");
                Sb.Append(HudDigits.Whole0(TopSpeed[i]));
                Sb.Append('\n');
                Sb.Append("air ");
                Sb.Append(HudDigits.Tenth0(AirTime[i]));
                Sb.Append("s   walls ");
                Sb.Append(HudDigits.Whole0(WallRuns[i]));
                Sb.Append('\n');
                Sb.Append("pad ");
                Sb.Append(HudDigits.Whole0(Pads[i]));
                Sb.Append("  zip ");
                Sb.Append(HudDigits.Whole0(Zips[i]));
                Sb.Append("  dash ");
                Sb.Append(HudDigits.Whole0(AirDashes[i]));
                Sb.Append('\n');
                Sb.Append("hit ");
                Sb.Append(HudDigits.Whole0(PunchesLanded[i]));
                Sb.Append("  miss ");
                Sb.Append(HudDigits.Whole0(PunchesWhiffed[i]));
                Sb.Append("  stag ");
                Sb.Append(HudDigits.Whole0(Staggers[i]));
                Sb.Append('\n');
                Sb.Append("slip ");
                Sb.Append(HudDigits.Whole0(NearMisses[i]));
                Sb.Append("  block ");
                Sb.Append(HudDigits.Whole0(TagBacksBlocked[i]));
                Card[i] = Sb.ToString();
            }
        }

        static string Fallback(int i)
        {
            if (i < 0) return "P1";
            if (i > 7) return "P8";
            return HudDigits.Whole0(i + 1);
        }
    }
}
