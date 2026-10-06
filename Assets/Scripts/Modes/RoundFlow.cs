using System;

namespace Tag.Modes
{
    /// <summary>
    /// Countdown, play, tag swap, results, next countdown.
    /// The headless proof steps this for seeded rounds against the dummy.
    /// Live UI asks it for the banner line and the clock. Feel numbers are not stored here.
    /// </summary>
    public static class RoundFlow
    {
        public const float CountdownSeconds = 3f;
        public const float ResultsArmSeconds = 0.25f;
        public const float TagBackSeconds = 1f;
        public const float TaggedBannerSeconds = 1.6f;
        public const int ProofRounds = 50;
        public const float ProofRoundSeconds = 6f;
        public const float ProofStep = 1f / 30f;

        public enum Phase
        {
            Countdown,
            Playing,
            Results
        }

        public sealed class State
        {
            public Phase Phase;
            public float PhaseTimer;
            public float Duration;
            public float Remaining;
            public float Elapsed;
            public float Arm;
            public int It = -1;
            public int OpeningIt;
            public int Local;
            public int Seed;
            public int RoundIndex;
            public int Tags;
            public int EndCount;
            public int Beep = -1;
            public int Beeps;
            public float LongestChase;
            public float Chase;
            public float TagBack;
            public int ImmuneWho = -1;
            public int ImmuneFrom = -1;
            public bool SameFrame;
            public bool Latch;
            public bool InputOpen;
            public string Banner = "";
            public float BannerTimer;
            public readonly string[] Names = { "You", "Dummy" };
            public readonly float[] TimeAsIt = new float[2];
            public readonly int[] TagsBy = new int[2];
        }

        public sealed class Report
        {
            public bool Ok = true;
            public int Rounds;
            public int Ended;
            public int Returned;
            public int Tags;
            public int Rejected;
            public int Eaten;
            public int Dead;
            public int DoubleEnd;
            public int Beeps;
            public float Longest;
            public string Failure = "";
            public string Line = "";

            public void Fail(string why)
            {
                Ok = false;
                if (Failure.Length == 0) Failure = why;
            }
        }

        public static State NewRound(float duration, int seed)
        {
            var s = new State();
            s.Phase = Phase.Countdown;
            s.PhaseTimer = CountdownSeconds;
            s.Duration = duration > 0.05f ? duration : 0.05f;
            s.Seed = seed;
            s.OpeningIt = Mod(seed, 2);
            s.Local = 0;
            s.It = -1;
            s.InputOpen = false;
            s.Beep = -1;
            return s;
        }

        public static void Tick(State s, float dt)
        {
            if (s == null) return;
            if (dt < 0f) dt = 0f;
            s.SameFrame = false;

            if (s.Phase == Phase.Countdown)
            {
                s.InputOpen = false;
                s.PhaseTimer -= dt;
                if (s.PhaseTimer > 0f)
                {
                    int sec = CeilToInt(s.PhaseTimer);
                    if (sec >= 1 && sec != s.Beep)
                    {
                        s.Beep = sec;
                        s.Beeps++;
                    }
                    return;
                }

                s.PhaseTimer = 0f;
                s.Phase = Phase.Playing;
                s.InputOpen = true;
                s.Remaining = s.Duration;
                s.Elapsed = 0f;
                s.Chase = 0f;
                s.It = s.OpeningIt;
                s.BannerTimer = 0f;
                s.Banner = s.It == s.Local ? "You're It" : ItLine(s);
                return;
            }

            if (s.Phase == Phase.Results)
            {
                s.InputOpen = false;
                s.Arm -= dt;
                if (s.Arm < 0f) s.Arm = 0f;
                return;
            }

            s.InputOpen = true;
            s.Remaining -= dt;
            s.Elapsed += dt;
            if (s.It >= 0 && s.It < 2)
            {
                s.TimeAsIt[s.It] += dt;
                s.Chase += dt;
            }
            if (s.TagBack > 0f)
            {
                s.TagBack -= dt;
                if (s.TagBack < 0f) s.TagBack = 0f;
            }
            if (s.BannerTimer > 0f)
            {
                s.BannerTimer -= dt;
                if (s.BannerTimer <= 0f)
                    s.Banner = s.It == s.Local ? "You're It" : ItLine(s);
            }
            if (s.Remaining <= 0f)
            {
                s.Remaining = 0f;
                End(s);
            }
        }

        public static void End(State s)
        {
            if (s == null || s.Phase == Phase.Results) return;
            if (s.Chase > s.LongestChase) s.LongestChase = s.Chase;
            s.EndCount++;
            s.Phase = Phase.Results;
            s.InputOpen = false;
            s.Arm = ResultsArmSeconds;
            s.Latch = false;
        }

        /// <summary>One It swap. Countdown, results, tag-back, and a second call this step are refused.</summary>
        public static bool TryTag(State s, int from, int to)
        {
            if (s == null) return false;
            if (s.Phase != Phase.Playing || !s.InputOpen || s.SameFrame)
                return false;
            if (from == to || from < 0 || to < 0 || from > 1 || to > 1)
                return false;
            if (s.It != from)
                return false;
            if (s.TagBack > 0f && from == s.ImmuneFrom && to == s.ImmuneWho)
                return false;

            s.SameFrame = true;
            if (s.Chase > s.LongestChase) s.LongestChase = s.Chase;
            s.Chase = 0f;
            s.It = to;
            s.Tags++;
            s.TagsBy[from]++;
            s.TagBack = TagBackSeconds;
            s.ImmuneWho = from;
            s.ImmuneFrom = to;
            if (from == s.Local)
            {
                s.Banner = "Tagged " + s.Names[to];
                s.BannerTimer = TaggedBannerSeconds;
            }
            else if (to == s.Local)
            {
                s.Banner = "You're It";
                s.BannerTimer = 0f;
            }
            else
            {
                s.Banner = ItLine(s);
                s.BannerTimer = 0f;
            }
            return true;
        }

        /// <summary>A press during countdown is kept for the first play step. A results press is dropped.</summary>
        public static void NotePress(State s, bool pressed)
        {
            if (s == null || !pressed) return;
            if (s.Phase == Phase.Countdown)
                s.Latch = true;
        }

        public static bool ConsumePress(State s)
        {
            if (s == null || !s.InputOpen || !s.Latch) return false;
            s.Latch = false;
            return true;
        }

        public static bool Advance(State s)
        {
            if (s == null || s.Phase != Phase.Results || s.Arm > 0f) return false;
            int next = s.RoundIndex + 1;
            int seed = s.Seed + 1;
            float duration = s.Duration;
            var fresh = NewRound(duration, seed);
            CopyFresh(s, fresh);
            s.RoundIndex = next;
            return true;
        }

        public static string BannerLine(bool localIsIt, bool taggedFlash, string taggedId, string itId)
        {
            if (taggedFlash && !string.IsNullOrEmpty(taggedId))
                return "Tagged " + taggedId;
            if (localIsIt)
                return "You're It";
            if (string.IsNullOrEmpty(itId))
                return "No one is It";
            return "It: " + itId;
        }

        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int whole = CeilToInt(seconds);
            if (whole < 0) whole = 0;
            int m = whole / 60;
            int r = whole % 60;
            return m.ToString() + ":" + (r < 10 ? "0" : "") + r.ToString();
        }

        public static Report RunSeeded(int rounds, int seed)
        {
            var report = new Report();
            if (rounds < 1) rounds = 1;
            report.Rounds = rounds;
            CheckEdges(report);
            if (!report.Ok) return Finish(report);

            var s = NewRound(ProofRoundSeconds, seed);
            bool sawYoureIt = false;
            bool sawTagged = false;
            for (int i = 0; i < rounds; i++)
            {
                if (s.Phase != Phase.Countdown)
                {
                    report.Dead++;
                    report.Fail("round " + i + " did not start on countdown");
                    break;
                }

                if (TryTag(s, 0, 1))
                {
                    report.Dead++;
                    report.Fail("tag landed during countdown");
                    break;
                }
                report.Rejected++;

                NotePress(s, true);
                var rng = new Random(seed + i * 17);
                float wait = 0.45f + (float)rng.NextDouble() * 0.9f;
                float since = 0f;
                bool sawPlay = false;
                bool ended = false;
                int guard = 0;
                while (guard++ < 20000)
                {
                    Phase before = s.Phase;
                    Tick(s, ProofStep);
                    if (before == Phase.Countdown && s.Phase == Phase.Playing)
                    {
                        sawPlay = true;
                        if (s.Banner == "You're It") sawYoureIt = true;
                        if (!ConsumePress(s))
                        {
                            report.Eaten++;
                            report.Fail("countdown press was eaten");
                        }
                        if (ConsumePress(s))
                        {
                            report.Eaten++;
                            report.Fail("play consumed the latch twice");
                        }
                    }

                    if (s.Phase == Phase.Playing)
                    {
                        since += ProofStep;
                        if (since >= wait)
                        {
                            since = 0f;
                            wait = 0.45f + (float)rng.NextDouble() * 0.9f;
                            int from = s.It;
                            int to = from == 0 ? 1 : 0;
                            int tagsBefore = s.Tags;
                            bool ok = TryTag(s, from, to);
                            bool again = TryTag(s, from, to);
                            if (again || s.Tags > tagsBefore + 1)
                            {
                                report.DoubleEnd++;
                                report.Fail("tag swapped twice in one step");
                            }
                            if (ok)
                            {
                                report.Tags++;
                                if (s.Banner.StartsWith("Tagged ", StringComparison.Ordinal))
                                    sawTagged = true;
                                if (s.Banner == "You're It") sawYoureIt = true;
                                Tick(s, 0.02f);
                                if (s.Phase == Phase.Playing && TryTag(s, to, from))
                                {
                                    report.Dead++;
                                    report.Fail("tag-back landed inside 1s");
                                }
                                else report.Rejected++;
                            }
                            else report.Rejected++;
                        }
                    }

                    if (s.Phase == Phase.Results)
                    {
                        ended = true;
                        if (s.EndCount != 1)
                        {
                            report.DoubleEnd++;
                            report.Fail("round end fired " + s.EndCount);
                        }
                        Tick(s, 0.05f);
                        if (s.EndCount != 1)
                        {
                            report.DoubleEnd++;
                            report.Fail("results tick ended the round again");
                        }
                        if (TryTag(s, 0, 1))
                        {
                            report.Dead++;
                            report.Fail("tag landed on the results card");
                        }
                        else report.Rejected++;

                        NotePress(s, true);
                        float sum = s.TimeAsIt[0] + s.TimeAsIt[1];
                        if (Math.Abs(sum - s.Elapsed) > 0.02f)
                        {
                            report.Dead++;
                            report.Fail("time as It drifted from the round clock");
                        }
                        if (s.LongestChase <= 0.05f)
                        {
                            report.Dead++;
                            report.Fail("longest chase was empty");
                        }
                        if (s.LongestChase > report.Longest) report.Longest = s.LongestChase;
                        report.Beeps += s.Beeps;
                        Tick(s, ResultsArmSeconds + 0.05f);
                        int index = s.RoundIndex;
                        if (!Advance(s))
                        {
                            report.Dead++;
                            report.Fail("results did not return to countdown");
                            break;
                        }
                        if (!Advance(s) && s.RoundIndex != index + 1)
                        {
                            report.DoubleEnd++;
                            report.Fail("second advance moved the round");
                        }
                        if (s.Phase != Phase.Countdown || s.Latch || s.InputOpen || s.EndCount != 0
                            || s.Tags != 0 || s.TimeAsIt[0] != 0f || s.TimeAsIt[1] != 0f
                            || s.PhaseTimer < CountdownSeconds - 0.001f)
                        {
                            report.Dead++;
                            report.Fail("countdown was not clean");
                            break;
                        }
                        report.Returned++;
                        break;
                    }
                }

                if (!sawPlay || !ended || guard >= 20000)
                {
                    report.Dead++;
                    report.Fail("round " + i + " did not finish");
                    break;
                }
                report.Ended++;
            }

            if (s.Phase != Phase.Countdown)
            {
                report.Dead++;
                report.Fail("session did not rest on countdown");
            }
            if (!sawYoureIt || !sawTagged)
            {
                report.Dead++;
                report.Fail("You're It / Tagged banner did not both show");
            }
            if (report.Ended != rounds || report.Returned != rounds)
                report.Fail("not every round ended back on countdown");
            if (report.Eaten != 0 || report.Dead != 0 || report.DoubleEnd != 0)
                report.Fail("round flow was not clean");
            if (report.Beeps != rounds * 3)
                report.Fail("countdown beeps were not one per second");

            return Finish(report);
        }

        static void CheckEdges(Report report)
        {
            var s = NewRound(30f, 1);
            if (TryTag(s, 0, 1)) report.Fail("edge tag during countdown");
            NotePress(s, true);
            int guard = 0;
            while (s.Phase == Phase.Countdown && guard++ < 500)
                Tick(s, 0.05f);
            if (s.Phase != Phase.Playing || !ConsumePress(s))
                report.Fail("edge latch did not survive countdown");
            s.It = 0;
            s.TagBack = 0f;
            s.SameFrame = false;
            if (!TryTag(s, 0, 1)) report.Fail("edge tag did not swap");
            if (TryTag(s, 1, 0)) report.Fail("edge same-step tag was accepted");
            s.TagBack = 0f;
            if (TryTag(s, 1, 0)) report.Fail("edge same-step tag ignored immunity and still fired");
            Tick(s, 0.016f);
            s.TagBack = TagBackSeconds;
            s.ImmuneFrom = 1;
            s.ImmuneWho = 0;
            if (TryTag(s, 1, 0)) report.Fail("edge tag-back was open inside 1s");
            s.TagBack = 0f;
            if (!TryTag(s, 1, 0)) report.Fail("edge tag stayed blocked after the step");
            s.Remaining = 0.01f;
            Tick(s, 0.05f);
            int ends = s.EndCount;
            Tick(s, 0.05f);
            if (s.Phase != Phase.Results || ends != 1 || s.EndCount != 1)
                report.Fail("edge round end was not single");
            NotePress(s, true);
            if (s.Latch) report.Fail("results press leaked into the next round");
        }

        static Report Finish(Report report)
        {
            if (report.Dead != 0 || report.Eaten != 0 || report.DoubleEnd != 0)
                report.Ok = false;
            report.Line = "round-flow"
                + " rounds=" + report.Rounds
                + " ended=" + report.Ended
                + " countdown=" + report.Returned
                + " tags=" + report.Tags
                + " rejected=" + report.Rejected
                + " eaten=" + report.Eaten
                + " dead=" + report.Dead
                + " doubleEnd=" + report.DoubleEnd
                + " beeps=" + report.Beeps
                + " longest=" + report.Longest.ToString("0.00");
            return report;
        }

        static void CopyFresh(State into, State fresh)
        {
            into.Phase = fresh.Phase;
            into.PhaseTimer = fresh.PhaseTimer;
            into.Duration = fresh.Duration;
            into.Remaining = fresh.Remaining;
            into.Elapsed = fresh.Elapsed;
            into.Arm = fresh.Arm;
            into.It = fresh.It;
            into.OpeningIt = fresh.OpeningIt;
            into.Local = fresh.Local;
            into.Seed = fresh.Seed;
            into.Tags = fresh.Tags;
            into.EndCount = fresh.EndCount;
            into.Beep = fresh.Beep;
            into.Beeps = fresh.Beeps;
            into.LongestChase = fresh.LongestChase;
            into.Chase = fresh.Chase;
            into.TagBack = fresh.TagBack;
            into.ImmuneWho = fresh.ImmuneWho;
            into.ImmuneFrom = fresh.ImmuneFrom;
            into.SameFrame = fresh.SameFrame;
            into.Latch = fresh.Latch;
            into.InputOpen = fresh.InputOpen;
            into.Banner = fresh.Banner;
            into.BannerTimer = fresh.BannerTimer;
            into.TimeAsIt[0] = 0f;
            into.TimeAsIt[1] = 0f;
            into.TagsBy[0] = 0;
            into.TagsBy[1] = 0;
        }

        static string ItLine(State s)
        {
            if (s.It < 0 || s.It > 1) return "No one is It";
            return "It: " + s.Names[s.It];
        }

        static int CeilToInt(float v)
        {
            if (v <= 0f) return 0;
            int n = (int)v;
            if (v > n) n++;
            return n;
        }

        static int Mod(int value, int m)
        {
            int r = value % m;
            return r < 0 ? r + m : r;
        }
    }
}
