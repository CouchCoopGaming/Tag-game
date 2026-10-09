using System;

namespace Tag.Gameplay
{
    /// <summary>
    /// Right-stick gestures for juke, spin, and an airborne dive.
    /// A flick is a deflection past 0.85 that is back near center, and
    /// stopped, within about 0.15 s. Sideways is a juke. Forward is a dive
    /// only while airborne; the same flick on the ground is look.
    /// A half circle is 150° of arc at more than 0.7 deflection inside 0.35 s.
    /// A held look, a pan, a runner track, and a tilt that stays out past
    /// 0.15 s never arm a move. Stutter is the right-trigger double-tap.
    /// </summary>
    public static class EvasionGestures
    {
        public const float FlickGate = 0.85f;
        public const float FlickReturn = 0.25f;
        public const float FlickWindow = 0.15f;
        public const float FlickSettle = 0.03f;
        public const float FlickMaxAge = 0.22f;
        public const float FlickStopSpeed = 3.0f;
        public const float SuppressReturn = 0.40f;
        public const float ArcDeflection = 0.7f;
        public const float ArcDegrees = 150f;
        public const float ArcWindow = 0.35f;
        public const float StutterWindow = 0.25f;
        public const float StutterLean = 0.25f;
        public const float StutterLateral = 0.40f;

        public struct State
        {
            public bool HasPrev;
            public bool Armed;
            public bool Suppress;
            public bool ArcLive;
            public bool HasCenter;
            public int Axis;
            public int Sign;
            public float ArmT;
            public float Settle;
            public float CenterT;
            public float ArcT;
            public float Arc;
            public float PrevA;
            public float PrevX, PrevY;
            public float Time;
            public float AccX, AccY;
        }

        public struct Result
        {
            public EvasionMoves.Kind Kind;
            public int Sign;
            public bool Commit;
            public float UndoX;
            public float UndoY;
        }

        public static void Reset(ref State s)
        {
            s = default;
        }

        public static Result Step(ref State s, float x, float y, float dt, bool airborne)
        {
            Result result = default;
            if (dt <= 0f) dt = 0.0001f;
            float m = Mag(x, y);
            float spd = 0f;
            if (s.HasPrev)
                spd = Mag(x - s.PrevX, y - s.PrevY) / dt;

            if (m > ArcDeflection)
            {
                float a = (float)Math.Atan2(y, x);
                if (!s.ArcLive)
                {
                    s.ArcLive = true;
                    s.ArcT = s.Time;
                    s.Arc = 0f;
                    s.PrevA = a;
                }
                else
                {
                    s.Arc += Wrap(a - s.PrevA);
                    s.PrevA = a;
                    if (s.Time - s.ArcT > ArcWindow)
                    {
                        s.ArcT = s.Time;
                        s.Arc = 0f;
                        s.PrevA = a;
                    }
                    else if (Abs(s.Arc) >= ArcDegrees * (float)Math.PI / 180f)
                    {
                        result.Kind = EvasionMoves.Kind.Spin;
                        result.Sign = s.Arc < 0f ? 1 : -1;
                        result.Commit = true;
                        result.UndoX = s.AccX;
                        result.UndoY = s.AccY;
                        ClearMotion(ref s);
                        s.HasPrev = true;
                        s.PrevX = x;
                        s.PrevY = y;
                        s.Time += dt;
                        return result;
                    }
                }
            }
            else
            {
                s.ArcLive = false;
                s.Arc = 0f;
            }

            if (s.Suppress)
            {
                if (m < SuppressReturn) s.Suppress = false;
                else
                {
                    if (s.ArcLive || m >= 0.15f)
                    {
                        s.AccX += x;
                        s.AccY += y;
                    }
                    Finish(ref s, x, y, dt);
                    return result;
                }
            }

            bool horiz = Abs(x) >= FlickGate && Abs(x) > Abs(y);
            bool forward = y >= FlickGate && y > Abs(x);
            if (!s.Armed)
            {
                if (horiz || forward)
                {
                    s.Armed = true;
                    s.Axis = forward && !horiz ? 2 : 1;
                    s.ArmT = s.Time;
                    s.Sign = s.Axis == 2 ? 1 : (x > 0f ? 1 : -1);
                    s.Settle = 0f;
                    s.HasCenter = false;
                }
            }
            else
            {
                float age = s.Time - s.ArmT;
                bool opposite = s.Axis == 2 ? (y < -FlickGate) : (x * s.Sign < -FlickGate);
                if (opposite)
                {
                    Disarm(ref s);
                    if (!s.ArcLive) s.AccX = s.AccY = 0f;
                }
                else if (!s.HasCenter && age > FlickWindow)
                {
                    Disarm(ref s);
                    if (s.ArcLive)
                    {
                        s.AccX += x;
                        s.AccY += y;
                    }
                    else s.AccX = s.AccY = 0f;
                    s.Suppress = true;
                }
                else if (age > FlickMaxAge)
                {
                    Disarm(ref s);
                    if (s.ArcLive)
                    {
                        s.AccX += x;
                        s.AccY += y;
                    }
                    else s.AccX = s.AccY = 0f;
                    s.Suppress = true;
                }
                else
                {
                    if (m < FlickReturn)
                    {
                        if (!s.HasCenter)
                        {
                            s.HasCenter = true;
                            s.CenterT = s.Time;
                        }
                        if (spd < FlickStopSpeed) s.Settle += dt;
                        else s.Settle = 0f;
                    }
                    else s.Settle = 0f;

                    if (s.HasCenter && s.Settle >= FlickSettle && (s.CenterT - s.ArmT) <= FlickWindow)
                    {
                        bool dive = s.Axis == 2;
                        if (dive && !airborne)
                        {
                            Disarm(ref s);
                        }
                        else
                        {
                            result.Kind = dive ? EvasionMoves.Kind.Dive : EvasionMoves.Kind.Juke;
                            result.Sign = s.Sign;
                            result.Commit = true;
                            result.UndoX = s.AccX;
                            result.UndoY = s.AccY;
                            ClearMotion(ref s);
                            Finish(ref s, x, y, dt);
                            return result;
                        }
                    }
                }
            }

            bool keep = !s.Suppress && (s.Armed || s.ArcLive || m >= 0.15f);
            if (keep)
            {
                s.AccX += x;
                s.AccY += y;
            }
            else if (!s.Armed && !s.ArcLive)
            {
                s.AccX = 0f;
                s.AccY = 0f;
            }
            Finish(ref s, x, y, dt);
            return result;
        }

        public struct TapState
        {
            public bool Held;
            public bool Live;
            public float FirstT;
            public float Time;
        }

        public struct TapResult
        {
            public bool Commit;
            public int Sign;
        }

        /// <summary>
        /// RT double-tap. The second press's left-stick lean picks the side.
        /// A centered stick uses the lateral move, and a straight run plants in place.
        /// LT is not read.
        /// </summary>
        public static TapResult StutterTap(ref TapState s, bool held, float lean, float lateral, float dt)
        {
            TapResult result = default;
            if (dt <= 0f) dt = 0.0001f;
            bool rise = held && !s.Held;
            if (rise)
            {
                if (s.Live && (s.Time - s.FirstT) <= StutterWindow)
                {
                    result.Commit = true;
                    result.Sign = StutterSign(lean, lateral);
                    s.Live = false;
                }
                else
                {
                    s.Live = true;
                    s.FirstT = s.Time;
                }
            }
            if (s.Live && (s.Time - s.FirstT) > StutterWindow)
                s.Live = false;
            s.Held = held;
            s.Time += dt;
            return result;
        }

        public static void ResetTap(ref TapState s)
        {
            s = default;
        }

        static int StutterSign(float lean, float lateral)
        {
            if (lean >= StutterLean) return 1;
            if (lean <= -StutterLean) return -1;
            if (lateral >= StutterLateral) return 1;
            if (lateral <= -StutterLateral) return -1;
            return 0;
        }

        public struct Report
        {
            public int Camera;
            public int Fp;
            public int Moves;
            public int Fn;
            public int StutterFp;
            public int StutterFpN;
            public int StutterFn;
            public int StutterFnN;
            public int DiveFn;
            public int DiveFnN;
            public bool SwallowOk;
        }

        public static bool Holds()
        {
            Report r = Measure();
            return r.Fp == 0 && r.Fn == 0 && r.Camera > 0 && r.Moves > 0 && r.SwallowOk
                && r.StutterFp == 0 && r.StutterFn == 0 && r.StutterFpN > 0 && r.StutterFnN > 0
                && r.DiveFn == 0 && r.DiveFnN > 0;
        }

        public static string ProofLine()
        {
            Report r = Measure();
            return "evasion-gestures cameraFP=" + r.Fp.ToString()
                + "/" + r.Camera.ToString()
                + " moveFN=" + r.Fn.ToString()
                + "/" + r.Moves.ToString()
                + " swallow=commit"
                + " diveFN=" + r.DiveFn.ToString() + "/" + r.DiveFnN.ToString()
                + " stutterFP=" + r.StutterFp.ToString() + "/" + r.StutterFpN.ToString()
                + " stutterFN=" + r.StutterFn.ToString() + "/" + r.StutterFnN.ToString();
        }

        public static Report Measure()
        {
            Report report = new Report();
            report.SwallowOk = true;
            float[] rates = { 1f / 60f, 1f / 30f, 1f / 120f };
            for (int i = 0; i < rates.Length; i++)
            {
                float dt = rates[i];
                RunCamera(ref report, dt);
                RunMoves(ref report, dt);
                RunDive(ref report, dt);
            }
            report.SwallowOk = report.SwallowOk && SwallowHolds();
            RunStutter(ref report);
            return report;
        }

        static void RunStutter(ref Report report)
        {
            float[] rates = { 1f / 60f, 1f / 30f, 1f / 120f };
            for (int i = 0; i < rates.Length; i++)
            {
                float dt = rates[i];
                StutterFalse(ref report, dt, 0.50f, new float[] { 0f }, 0.06f, 0f, 0f);
                StutterFalse(ref report, dt, 0.70f, new float[] { 0f }, 0.60f, 0f, 0f);
                StutterFalse(ref report, dt, 0.70f, new float[] { 0f, 0.40f }, 0.05f, 0f, 0f);
                StutterFalse(ref report, dt, 1.20f, new float[] { 0f, 0.32f, 0.64f, 0.96f }, 0.04f, 0f, 0f);
                StutterFalse(ref report, dt, 0.80f, new float[] { 0f, 0.50f }, 0.20f, 0.4f, 0f);
                StutterTrue(ref report, dt, 0.12f, 0f, 0f, 0);
                StutterTrue(ref report, dt, 0.18f, 0.60f, 0f, 1);
                StutterTrue(ref report, dt, 0.22f, -0.70f, 0f, -1);
                StutterTrue(ref report, dt, 0.16f, 0f, 1.5f, 1);
                StutterTrue(ref report, dt, 0.14f, 0.10f, -2f, -1);
            }
        }

        static void StutterFalse(ref Report report, float dt, float end, float[] rises, float pulse, float lean, float lateral)
        {
            report.StutterFpN++;
            int sign;
            int fires = PlayTaps(dt, end, rises, pulse, lean, lateral, out sign);
            if (fires != 0) report.StutterFp++;
        }

        static void StutterTrue(ref Report report, float dt, float gap, float lean, float lateral, int want)
        {
            report.StutterFnN++;
            int sign;
            int fires = PlayTaps(dt, gap + 0.20f, new float[] { 0f, gap }, 0.05f, lean, lateral, out sign);
            if (fires != 1 || sign != want) report.StutterFn++;
        }

        static int PlayTaps(float dt, float end, float[] rises, float pulse, float lean, float lateral, out int sign)
        {
            TapState s = default;
            int fires = 0;
            sign = 0;
            int n = (int)Math.Round(end / dt);
            if (n < 2) n = 2;
            for (int i = 0; i < n; i++)
            {
                float t = i * dt;
                bool held = false;
                for (int k = 0; k < rises.Length; k++)
                {
                    if (t >= rises[k] && t < rises[k] + pulse) held = true;
                }
                TapResult r = StutterTap(ref s, held, lean, lateral, dt);
                if (!r.Commit) continue;
                fires++;
                sign = r.Sign;
            }
            return fires;
        }

        static void RunCamera(ref Report report, float dt)
        {
            Count(ref report, true, false, false, dt, TraceSlow(1f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceSlow(-1f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceSnap(1f, 0.50f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceSnap(-1f, 0.45f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceAcross(0.18f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceAcross(0.30f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceAcross(0.80f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceAcross(1.20f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceSnapRelease(dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceLook(1f, true, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceLook(-1f, true, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceLook(1f, false, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceLook(-1f, false, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceUpDown(dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceTrack(dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceArc(90f, -120f, 0.25f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceArc(90f, -180f, 0.55f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceArc(10f, -80f, 0.25f, dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceSlowReturn(dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, false, dt, TraceWobble(dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, true, dt, TraceTiltSlow(dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, true, dt, TraceTiltHeld(dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, true, dt, TraceTiltFastHold(dt), EvasionMoves.Kind.None, 0);
            Count(ref report, true, false, true, dt, TraceTiltLateRelease(dt), EvasionMoves.Kind.None, 0);
        }

        static void RunMoves(ref Report report, float dt)
        {
            Count(ref report, false, true, false, dt, TraceFlick(1, 0.05f, 0.07f, dt), EvasionMoves.Kind.Juke, 1);
            Count(ref report, false, true, false, dt, TraceFlick(-1, 0.05f, 0.07f, dt), EvasionMoves.Kind.Juke, -1);
            Count(ref report, false, true, false, dt, TraceFlick(1, 0.03f, 0.05f, dt), EvasionMoves.Kind.Juke, 1);
            Count(ref report, false, true, false, dt, TraceFlick(1, 0.04f, 0.10f, dt), EvasionMoves.Kind.Juke, 1);
            Count(ref report, false, true, false, dt, TraceArc(90f, -170f, 0.28f, dt), EvasionMoves.Kind.Spin, 1);
            Count(ref report, false, true, false, dt, TraceArc(90f, 170f, 0.28f, dt), EvasionMoves.Kind.Spin, -1);
            Count(ref report, false, true, false, dt, TraceArc(0f, -160f, 0.30f, dt), EvasionMoves.Kind.Spin, 1);
            Count(ref report, false, true, false, dt, TraceArc(180f, 160f, 0.30f, dt), EvasionMoves.Kind.Spin, -1);
        }

        static void RunDive(ref Report report, float dt)
        {
            Count(ref report, false, false, true, dt, TraceDive(0.05f, 0.07f, dt), EvasionMoves.Kind.Dive, 1);
            Count(ref report, false, false, true, dt, TraceDive(0.03f, 0.05f, dt), EvasionMoves.Kind.Dive, 1);
            Count(ref report, false, false, true, dt, TraceDive(0.04f, 0.10f, dt), EvasionMoves.Kind.Dive, 1);
            Count(ref report, false, false, true, dt, TraceDive(0.06f, 0.08f, dt), EvasionMoves.Kind.Dive, 1);
        }

        static bool SwallowHolds()
        {
            if (!NetLook(TraceFlick(1, 0.05f, 0.07f, 1f / 60f), 1f / 60f, false)) return false;
            if (!NetLook(TraceArc(90f, -170f, 0.28f, 1f / 60f), 1f / 60f, false)) return false;
            if (!NetLook(TraceDive(0.05f, 0.07f, 1f / 60f), 1f / 60f, true)) return false;
            float[] pan = TraceSnap(1f, 0.40f, 1f / 60f);
            return PanKept(pan, 1f / 60f);
        }

        static bool NetLook(float[] samples, float dt, bool airborne)
        {
            State s = default;
            float lookX = 0f;
            float lookY = 0f;
            bool committed = false;
            int n = samples.Length / 2;
            for (int i = 0; i < n; i++)
            {
                float x = samples[i * 2];
                float y = samples[i * 2 + 1];
                Result r = Step(ref s, x, y, dt, airborne);
                if (r.Commit)
                {
                    lookX += -r.UndoX;
                    lookY += -r.UndoY;
                    committed = true;
                    break;
                }
                lookX += x;
                lookY += y;
            }
            if (!committed) return false;
            return Abs(lookX) < 0.05f && Abs(lookY) < 0.05f;
        }

        static bool PanKept(float[] samples, float dt)
        {
            State s = default;
            float stick = 0f;
            float look = 0f;
            int n = samples.Length / 2;
            for (int i = 0; i < n; i++)
            {
                float x = samples[i * 2];
                float y = samples[i * 2 + 1];
                Result r = Step(ref s, x, y, dt, false);
                if (r.Commit) return false;
                stick += x;
                look += x;
            }
            return Abs(stick - look) < 0.0001f;
        }

        static void Count(ref Report report, bool camera, bool move, bool airborne, float dt, float[] samples, EvasionMoves.Kind kind, int sign)
        {
            if (camera) report.Camera++;
            if (move) report.Moves++;
            bool dive = kind == EvasionMoves.Kind.Dive;
            if (dive) report.DiveFnN++;
            State s = default;
            bool hit = false;
            int n = samples.Length / 2;
            for (int i = 0; i < n; i++)
            {
                Result r = Step(ref s, samples[i * 2], samples[i * 2 + 1], dt, airborne);
                if (!r.Commit) continue;
                hit = true;
                if (camera) report.Fp++;
                if (move && (r.Kind != kind || r.Sign != sign)) report.Fn++;
                if (dive && (r.Kind != kind || r.Sign != sign)) report.DiveFn++;
                break;
            }
            if (move && !hit) report.Fn++;
            if (dive && !hit) report.DiveFn++;
        }

        static float[] TraceSlow(float sign, float dt)
        {
            float[] a = Lerp(0f, 0f, 0.95f * sign, 0f, 0.70f, dt);
            float[] b = Hold(0.95f * sign, 0f, 0.40f, dt);
            return Cat(a, b);
        }

        static float[] TraceSnap(float sign, float hold, float dt)
        {
            float[] a = Lerp(0f, 0f, sign, 0f, 0.04f, dt);
            float[] b = Hold(sign, 0f, hold, dt);
            return Cat(a, b);
        }

        static float[] TraceSnapRelease(float dt)
        {
            float[] a = TraceSnap(1f, 0.25f, dt);
            float[] b = Lerp(1f, 0f, 0f, 0f, 0.08f, dt);
            float[] c = Hold(0f, 0f, 0.10f, dt);
            return Cat(a, Cat(b, c));
        }

        static float[] TraceAcross(float seconds, float dt)
        {
            return Lerp(-1f, 0f, 1f, 0f, seconds, dt);
        }

        static float[] TraceLook(float sign, bool flickBack, float dt)
        {
            float[] a = Lerp(0f, 0f, 0f, 0.96f * sign, 0.08f, dt);
            if (!flickBack) return Cat(a, Hold(0f, 0.96f * sign, 0.40f, dt));
            float[] b = Lerp(0f, 0.96f * sign, 0f, 0f, 0.07f, dt);
            return Cat(a, Cat(b, Hold(0f, 0f, 0.10f, dt)));
        }

        static float[] TraceUpDown(float dt)
        {
            float[] a = Lerp(0f, 0f, 0f, 0.95f, 0.12f, dt);
            float[] b = Hold(0f, 0.95f, 0.15f, dt);
            float[] c = Lerp(0f, 0.95f, 0f, -0.95f, 0.30f, dt);
            float[] d = Hold(0f, -0.95f, 0.20f, dt);
            return Cat(a, Cat(b, Cat(c, d)));
        }

        static float[] TraceTrack(float dt)
        {
            int n = Math.Max(2, (int)Math.Round(1.2f / dt));
            float[] o = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float t = i * dt;
                o[i * 2] = 0.62f * (float)Math.Sin(t * 2.4f);
                o[i * 2 + 1] = 0.25f * (float)Math.Sin(t * 1.7f);
            }
            return o;
        }

        static float[] TraceSlowReturn(float dt)
        {
            float[] a = Lerp(0f, 0f, 0.95f, 0f, 0.40f, dt);
            float[] b = Lerp(0.95f, 0f, 0f, 0f, 0.40f, dt);
            return Cat(a, Cat(b, Hold(0f, 0f, 0.10f, dt)));
        }

        static float[] TraceWobble(float dt)
        {
            float[] a = Lerp(0f, 0f, 0.95f, 0.10f, 0.40f, dt);
            int n = a.Length / 2;
            for (int i = 0; i < n; i++)
                a[i * 2 + 1] += 0.10f * (float)Math.Sin(i * 0.35f);
            return Cat(a, Hold(0.95f, 0.05f, 0.25f, dt));
        }

        static float[] TraceTiltSlow(float dt)
        {
            float[] a = Lerp(0f, 0f, 0f, 0.95f, 0.70f, dt);
            return Cat(a, Hold(0f, 0.95f, 0.40f, dt));
        }

        static float[] TraceTiltHeld(float dt)
        {
            float[] a = Lerp(0f, 0f, 0f, 0.96f, 0.12f, dt);
            return Cat(a, Hold(0f, 0.96f, 0.50f, dt));
        }

        static float[] TraceTiltFastHold(float dt)
        {
            float[] a = Lerp(0f, 0f, 0f, 1f, 0.04f, dt);
            return Cat(a, Hold(0f, 1f, 0.50f, dt));
        }

        static float[] TraceTiltLateRelease(float dt)
        {
            float[] a = Lerp(0f, 0f, 0f, 1f, 0.04f, dt);
            float[] b = Hold(0f, 1f, 0.25f, dt);
            float[] c = Lerp(0f, 1f, 0f, 0f, 0.08f, dt);
            return Cat(a, Cat(b, Cat(c, Hold(0f, 0f, 0.12f, dt))));
        }

        static float[] TraceDive(float outT, float backT, float dt)
        {
            float[] a = Lerp(0f, 0f, 0f, 0.95f, outT, dt);
            float[] b = Lerp(0f, 0.95f, 0f, 0f, backT, dt);
            return Cat(a, Cat(b, Hold(0f, 0f, 0.08f, dt)));
        }

        static float[] TraceFlick(int sign, float outT, float backT, float dt)
        {
            float edge = 0.95f * sign;
            float[] a = Lerp(0f, 0f, edge, 0.04f, outT, dt);
            float[] b = Lerp(edge, 0.04f, 0f, 0f, backT, dt);
            return Cat(a, Cat(b, Hold(0f, 0f, 0.08f, dt)));
        }

        static float[] TraceArc(float startDeg, float sweepDeg, float seconds, float dt)
        {
            int n = Math.Max(2, (int)Math.Round(seconds / dt));
            float[] o = new float[n * 2];
            float start = startDeg * (float)Math.PI / 180f;
            float sweep = sweepDeg * (float)Math.PI / 180f;
            for (int i = 0; i < n; i++)
            {
                float u = n == 1 ? 1f : i / (float)(n - 1);
                float a = start + sweep * u;
                o[i * 2] = 0.92f * (float)Math.Cos(a);
                o[i * 2 + 1] = 0.92f * (float)Math.Sin(a);
            }
            return o;
        }

        static float[] Lerp(float x0, float y0, float x1, float y1, float seconds, float dt)
        {
            int n = Math.Max(2, (int)Math.Round(seconds / dt));
            float[] o = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                o[i * 2] = x0 + (x1 - x0) * u;
                o[i * 2 + 1] = y0 + (y1 - y0) * u;
            }
            return o;
        }

        static float[] Hold(float x, float y, float seconds, float dt)
        {
            int n = Math.Max(1, (int)Math.Round(seconds / dt));
            float[] o = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                o[i * 2] = x;
                o[i * 2 + 1] = y;
            }
            return o;
        }

        static float[] Cat(float[] a, float[] b)
        {
            float[] o = new float[a.Length + b.Length];
            for (int i = 0; i < a.Length; i++) o[i] = a[i];
            for (int i = 0; i < b.Length; i++) o[a.Length + i] = b[i];
            return o;
        }

        static void Disarm(ref State s)
        {
            s.Armed = false;
            s.HasCenter = false;
            s.Settle = 0f;
            s.Axis = 0;
        }

        static void ClearMotion(ref State s)
        {
            Disarm(ref s);
            s.Suppress = false;
            s.ArcLive = false;
            s.Arc = 0f;
            s.AccX = 0f;
            s.AccY = 0f;
        }

        static void Finish(ref State s, float x, float y, float dt)
        {
            s.HasPrev = true;
            s.PrevX = x;
            s.PrevY = y;
            s.Time += dt;
        }

        static float Mag(float x, float y)
        {
            return (float)Math.Sqrt(x * x + y * y);
        }

        static float Abs(float v)
        {
            return v < 0f ? -v : v;
        }

        static float Wrap(float d)
        {
            float pi = (float)Math.PI;
            while (d > pi) d -= pi * 2f;
            while (d < -pi) d += pi * 2f;
            return d;
        }
    }
}
