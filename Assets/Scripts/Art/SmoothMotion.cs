using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Render-only smoothing. Gameplay state, the capsule, and feel locks stay instant.
    /// A one-frame slew above the gait band is replaced with a critically damped spring.
    /// Position pops are absorbed on a visual offset. The capsule is not moved.
    /// </summary>
    public static class SmoothMotion
    {
        public const bool RootMotion = false;

        /// <summary>Jump, punch, dash, vault, wall. About 0.06 s to settle.</summary>
        public const float ResponsiveSeconds = 0.06f;
        /// <summary>Heavier pose catches and the camera duck. About 0.12 s.</summary>
        public const float SettleSeconds = 0.12f;
        /// <summary>Visual yaw toward the wall or the move direction.</summary>
        public const float YawSeconds = 0.10f;
        /// <summary>Visual mesh catch-up after a capsule correction.</summary>
        public const float PositionSeconds = 0.10f;
        /// <summary>Live cycles and authored blend curves. Under the spring band so the stride keeps its swing.</summary>
        public const float CycleSlew = 64f;
        /// <summary>Slews at or under this stay on the old exponential. Gait lives here.</summary>
        public const float ExponentialSlew = 70f;
        /// <summary>At and above this, the spring is the short responsive one.</summary>
        public const float ResponsiveSlew = 140f;
        /// <summary>A correction this large is a respawn. The mesh goes with the capsule.</summary>
        public const float PopIgnore = 1.25f;

        public const float Dt = 1f / 60f;

        /// <summary>Exponential rate whose three time-constants match <paramref name="seconds"/>.</summary>
        public static float Rate(float seconds)
        {
            if (seconds < 0.0001f) return 0f;
            return 3f / seconds;
        }

        /// <summary>0 keeps the exponential. Positive is the spring time for that slew.</summary>
        public static float SecondsForSlew(float speed)
        {
            if (speed < ExponentialSlew) return 0f;
            if (speed >= ResponsiveSlew) return ResponsiveSeconds;
            return SettleSeconds;
        }

        /// <summary>
        /// Critically damped step. Same shape as a SmoothDamp with no max-speed cap.
        /// Overshoot snaps to the target and clears the velocity.
        /// </summary>
        public static float Smooth(float current, float target, ref float vel, float smoothTime, float dt)
        {
            if (dt <= 0f) return current;
            if (smoothTime < 0.0001f) smoothTime = 0.0001f;
            float omega = 2f / smoothTime;
            float x = omega * dt;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = current - target;
            float temp = (vel + omega * change) * dt;
            vel = (vel - omega * temp) * exp;
            float output = target + (change + temp) * exp;
            if ((target - current > 0f) == (output > target))
            {
                output = target;
                vel = 0f;
            }
            return output;
        }

        public static Vector3 Decay(Vector3 offset, ref float vx, ref float vy, ref float vz, float seconds, float dt)
        {
            return new Vector3(
                Smooth(offset.x, 0f, ref vx, seconds, dt),
                Smooth(offset.y, 0f, ref vy, seconds, dt),
                Smooth(offset.z, 0f, ref vz, seconds, dt));
        }

        /// <summary>Signed yaw from planar A to planar B, degrees. Forward is +Z.</summary>
        public static float PlanarDelta(float ax, float az, float bx, float bz)
        {
            const float rad2deg = 57.29578f;
            float a = (float)Math.Atan2(ax, az) * rad2deg;
            float b = (float)Math.Atan2(bx, bz) * rad2deg;
            float d = b - a;
            while (d > 180f) d -= 360f;
            while (d < -180f) d += 360f;
            return d;
        }

        public struct Measure
        {
            public float PoseBefore;
            public float PoseAfter;
            public float PosBefore;
            public float PosAfter;
            public float YawBefore;
            public float YawAfter;
            public string PoseName;
            public string PosName;
            public string YawName;
        }

        public static Measure Run()
        {
            var m = new Measure();
            // Degrees the old slew closed in one frame, and the spring that replaces it.
            ConsiderPose(ref m, "airDash", 82f, 2800f);
            ConsiderPose(ref m, "punchArm", 110f, 2400f);
            ConsiderPose(ref m, "mantleThigh", 102f, 1600f);
            ConsiderPose(ref m, "crouchThigh", 64f, 1400f);
            ConsiderPose(ref m, "slideHandoff", 68f, 3600f);
            ConsiderPose(ref m, "grappleLatch", 90f, 2400f);
            ConsiderPose(ref m, "stagger", 48f, 280f);
            ConsiderPose(ref m, "aimChest", 36f, 240f);
            ConsiderPose(ref m, "stopPlant", 34f, 320f);
            ConsiderPose(ref m, "wallLean", 20f, 170f);
            ConsiderPose(ref m, "jumpTakeoff", 40f, 170f);
            ConsiderPose(ref m, "wallJump", 36f, 170f);
            ConsiderPose(ref m, "airLean", 22f, 140f);

            ConsiderPos(ref m, "step", 0.20f);
            ConsiderPos(ref m, "skin", 0.08f);
            ConsiderPos(ref m, "ledge", 0.35f);
            ConsiderPos(ref m, "mantleExit", 0.71f);

            ConsiderYaw(ref m, "wallAttach", 90f);
            ConsiderYaw(ref m, "hardTurn", 180f);
            return m;
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (ResponsiveSeconds < 0.05f || ResponsiveSeconds > 0.08f) return false;
            if (SettleSeconds < 0.10f || SettleSeconds > 0.15f) return false;
            if (YawSeconds < 0.08f || YawSeconds > 0.12f) return false;
            if (PositionSeconds < 0.08f || PositionSeconds > 0.12f) return false;
            if (SecondsForSlew(42f) != 0f) return false;
            if (Mathf.Abs(SecondsForSlew(170f) - ResponsiveSeconds) > 0.001f) return false;
            if (Mathf.Abs(SecondsForSlew(90f) - SettleSeconds) > 0.001f) return false;
            if (Mathf.Abs(SecondsForSlew(2800f) - ResponsiveSeconds) > 0.001f) return false;
            if (PopIgnore < 1f || PopIgnore > 2f) return false;

            Measure m = Run();
            if (!(m.PoseAfter < m.PoseBefore * 0.45f)) return false;
            if (!(m.PosAfter < m.PosBefore * 0.35f)) return false;
            if (!(m.YawAfter < m.YawBefore * 0.45f)) return false;
            if (m.PoseBefore < 40f) return false;
            if (m.PosBefore < 0.5f) return false;
            if (m.YawBefore < 90f) return false;
            // A step is hidden on the mesh. A respawn still moves the mesh with the capsule.
            if (Absorb(0.20f) > 0.001f) return false;
            if (Absorb(2.5f) < 2f) return false;
            if (CycleSlew >= ExponentialSlew) return false;
            if (SecondsForSlew(CycleSlew) != 0f) return false;
            if (SwingKept(WallPose.ClimbCadenceFull, CycleSlew, false) < 0.85f) return false;
            if (SwingKept(WallPose.RunCadenceFull, CycleSlew, false) < 0.75f) return false;
            if (PlantFrames(true) <= PlantFrames(false)) return false;
            if (SpikeShown(0.35f, 0f, 0f, 0f, 0f, false) > 0.001f) return false;
            if (SpikeShown(0.12f, 0f, 0f, 0.12f, 0f, false) < 0.10f) return false;
            if (SpikeShown(2.5f, 0f, 0f, 0f, 0f, false) < 2f) return false;
            if (!ResponsesSameFrame()) return false;
            return true;
        }

        /// <summary>Visual travel on the pop frame. Small corrections hide. A respawn does not.</summary>
        public static float Absorb(float popMeters)
        {
            float mag = popMeters < 0f ? -popMeters : popMeters;
            if (mag >= PopIgnore) return mag;
            return 0f;
        }

        public static string DetailLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            var sb = new System.Text.StringBuilder();
            sb.Append("smooth-detail");
            AppendPose(sb, c, "airDash", 82f, 2800f);
            AppendPose(sb, c, "punchArm", 110f, 2400f);
            AppendPose(sb, c, "mantleThigh", 102f, 1600f);
            AppendPose(sb, c, "crouchThigh", 64f, 1400f);
            AppendPose(sb, c, "slideHandoff", 68f, 3600f);
            AppendPose(sb, c, "grappleLatch", 90f, 2400f);
            AppendPose(sb, c, "stagger", 48f, 280f);
            AppendPose(sb, c, "wallLean", 20f, 170f);
            AppendPose(sb, c, "jumpTakeoff", 40f, 170f);
            AppendPose(sb, c, "airLean", 22f, 140f);
            AppendPos(sb, c, "step", 0.20f);
            AppendPos(sb, c, "ledge", 0.35f);
            AppendPos(sb, c, "mantleExit", 0.71f);
            AppendYaw(sb, c, "wallAttach", 90f);
            AppendYaw(sb, c, "hardTurn", 180f);
            sb.Append(" ledgeH=0.350>");
            sb.Append(SpikeShown(0.35f, 0f, 0f, 0f, 0f, false).ToString("0.000", c));
            sb.Append(" wallPush=0.120>");
            sb.Append(SpikeShown(0.12f, 0f, 0f, 0.12f, 0f, false).ToString("0.000", c));
            return sb.ToString();
        }

        static void AppendPose(System.Text.StringBuilder sb, CultureInfo c, string name, float degrees, float slew)
        {
            sb.Append(' ');
            sb.Append(name);
            sb.Append('=');
            sb.Append(MaxPose(degrees, slew, false).ToString("0.0", c));
            sb.Append('>');
            sb.Append(MaxPose(degrees, slew, true).ToString("0.0", c));
        }

        static void AppendPos(System.Text.StringBuilder sb, CultureInfo c, string name, float meters)
        {
            sb.Append(' ');
            sb.Append(name);
            sb.Append('=');
            sb.Append(MaxPos(meters, false).ToString("0.000", c));
            sb.Append('>');
            sb.Append(MaxPos(meters, true).ToString("0.000", c));
        }

        static void AppendYaw(System.Text.StringBuilder sb, CultureInfo c, string name, float degrees)
        {
            sb.Append(' ');
            sb.Append(name);
            sb.Append('=');
            sb.Append(degrees.ToString("0.0", c));
            sb.Append('>');
            sb.Append(MaxYaw(degrees).ToString("0.0", c));
        }

        public static string ProofLine()
        {
            Measure m = Run();
            CultureInfo c = CultureInfo.InvariantCulture;
            return "smooth-motion"
                + " poseBefore=" + m.PoseBefore.ToString("0.0", c)
                + " poseAfter=" + m.PoseAfter.ToString("0.0", c)
                + " pose=" + m.PoseName
                + " posBefore=" + m.PosBefore.ToString("0.000", c)
                + " posAfter=" + m.PosAfter.ToString("0.000", c)
                + " pos=" + m.PosName
                + " yawBefore=" + m.YawBefore.ToString("0.0", c)
                + " yawAfter=" + m.YawAfter.ToString("0.0", c)
                + " yaw=" + m.YawName
                + " blend=" + ResponsiveSeconds.ToString("0.00", c)
                + "/" + SettleSeconds.ToString("0.00", c)
                + " posSmooth=" + PositionSeconds.ToString("0.00", c)
                + " yawSmooth=" + YawSeconds.ToString("0.00", c)
                + " inputSameFrame=1"
                + " rootMotion=0 fovPop=0 shake=0 slowMo=0";
        }

        /// <summary>Sixteen frames, before on top, after underneath. Pose, position, yaw.</summary>
        public static void WriteStrip(string path)
        {
            const int frames = 16;
            const int cell = 36;
            const int rowH = 72;
            const int rows = 6;
            int w = frames * cell;
            int h = rows * rowH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 18, 22, 28);

            float[] dashB = SeriesPose(82f, 2800f, false, frames);
            float[] dashA = SeriesPose(82f, 2800f, true, frames);
            float[] wallB = SeriesPose(20f, 170f, false, frames);
            float[] wallA = SeriesPose(20f, 170f, true, frames);
            float[] posB = SeriesPos(0.71f, false, frames);
            float[] posA = SeriesPos(0.71f, true, frames);

            Plot(pix, w, h, 0, dashB, 82f, 220, 96, 84);
            Plot(pix, w, h, 1, dashA, 82f, 120, 196, 140);
            Plot(pix, w, h, 2, wallB, 20f, 220, 96, 84);
            Plot(pix, w, h, 3, wallA, 20f, 120, 196, 140);
            Plot(pix, w, h, 4, posB, 0.71f, 220, 96, 84);
            Plot(pix, w, h, 5, posA, 0.71f, 120, 196, 140);

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePpm(path, pix, w, h);
        }

        static void ConsiderPose(ref Measure m, string name, float degrees, float slew)
        {
            float before = MaxPose(degrees, slew, false);
            float after = MaxPose(degrees, slew, true);
            if (before > m.PoseBefore)
            {
                m.PoseBefore = before;
                m.PoseAfter = after;
                m.PoseName = name;
            }
        }

        static void ConsiderPos(ref Measure m, string name, float meters)
        {
            float before = MaxPos(meters, false);
            float after = MaxPos(meters, true);
            if (before > m.PosBefore)
            {
                m.PosBefore = before;
                m.PosAfter = after;
                m.PosName = name;
            }
        }

        static void ConsiderYaw(ref Measure m, string name, float degrees)
        {
            float before = degrees;
            float after = MaxYaw(degrees);
            if (before > m.YawBefore)
            {
                m.YawBefore = before;
                m.YawAfter = after;
                m.YawName = name;
            }
        }

        static float MaxPose(float degrees, float slew, bool sprung)
        {
            float[] s = SeriesPose(degrees, slew, sprung, 48);
            return MaxGap(s, 0f);
        }

        static float MaxPos(float pop, bool sprung)
        {
            float[] s = SeriesPos(pop, sprung, 48);
            return MaxGap(s, 0f);
        }

        static float MaxGap(float[] s, float start)
        {
            float max = 0f;
            float prev = start;
            for (int i = 0; i < s.Length; i++)
            {
                float d = s[i] - prev;
                if (d < 0f) d = -d;
                if (d > max) max = d;
                prev = s[i];
            }
            return max;
        }

        static float MaxYaw(float degrees)
        {
            float cur = 0f;
            float vel = 0f;
            float max = 0f;
            for (int i = 0; i < 48; i++)
            {
                float next = Smooth(cur, degrees, ref vel, YawSeconds, Dt);
                float d = next - cur;
                if (d < 0f) d = -d;
                if (d > max) max = d;
                cur = next;
            }
            return max;
        }

        static float[] SeriesPose(float degrees, float slew, bool sprung, int frames)
        {
            var s = new float[frames];
            float cur = 0f;
            float vel = 0f;
            float seconds = SecondsForSlew(slew);
            for (int i = 0; i < frames; i++)
            {
                if (!sprung || seconds <= 0f)
                {
                    float a = 1f - (float)Math.Exp(-slew * Dt);
                    if (a < 0f) a = 0f;
                    if (a > 1f) a = 1f;
                    cur = cur + (degrees - cur) * a;
                }
                else
                    cur = Smooth(cur, degrees, ref vel, seconds, Dt);
                s[i] = cur;
            }
            return s;
        }

        /// <summary>
        /// World visual position relative to the pre-pop spot.
        /// The old path jumps with the capsule. The new path stays put, then eases.
        /// </summary>
        static float[] SeriesPos(float pop, bool sprung, int frames)
        {
            var s = new float[frames];
            if (!sprung)
            {
                for (int i = 0; i < frames; i++)
                    s[i] = pop;
                return s;
            }
            float lag = -pop;
            float vel = 0f;
            for (int i = 0; i < frames; i++)
            {
                s[i] = pop + lag;
                lag = Smooth(lag, 0f, ref vel, PositionSeconds, Dt);
            }
            return s;
        }

        static void Plot(byte[] pix, int w, int h, int row, float[] series, float scale, byte r, byte g, byte b)
        {
            if (scale < 0.0001f) scale = 1f;
            int y0 = row * (h / 6);
            int y1 = y0 + (h / 6) - 1;
            int cell = w / series.Length;
            for (int i = 0; i < series.Length; i++)
            {
                float u = series[i] / scale;
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
                int top = y1 - (int)(u * (y1 - y0 - 4)) - 2;
                int x0 = i * cell + 3;
                int x1 = x0 + cell - 8;
                for (int y = top; y <= y1 - 2; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int p = (y * w + x) * 3;
                        if (p < 0 || p + 2 >= pix.Length) continue;
                        pix[p] = r;
                        pix[p + 1] = g;
                        pix[p + 2] = b;
                    }
                }
            }
        }

        static void Fill(byte[] pix, int w, int h, byte r, byte g, byte b)
        {
            for (int i = 0; i < w * h; i++)
            {
                pix[i * 3] = r;
                pix[i * 3 + 1] = g;
                pix[i * 3 + 2] = b;
            }
        }

        static void WritePpm(string path, byte[] pix, int w, int h)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] head = System.Text.Encoding.ASCII.GetBytes("P6\n" + w.ToString(CultureInfo.InvariantCulture) + " " + h.ToString(CultureInfo.InvariantCulture) + "\n255\n");
                fs.Write(head, 0, head.Length);
                fs.Write(pix, 0, pix.Length);
            }
        }

        /// <summary>Share of a unit swing that the mesh still shows after the filter settles.</summary>
        public static float SwingKept(float cadence, float slew, bool sprung)
        {
            float cur = 0f;
            float vel = 0f;
            float peak = 0f;
            float seconds = SecondsForSlew(slew);
            const float amp = 25f;
            for (int i = 0; i < 180; i++)
            {
                float t = i * Dt;
                float target = amp * (float)Math.Sin(cadence * t);
                if (!sprung || seconds <= 0f)
                {
                    vel = 0f;
                    float a = 1f - (float)Math.Exp(-slew * Dt);
                    cur = cur + (target - cur) * a;
                }
                else
                    cur = Smooth(cur, target, ref vel, seconds, Dt);
                if (t > 0.45f)
                {
                    float mag = cur < 0f ? -cur : cur;
                    if (mag > peak) peak = mag;
                }
            }
            return peak / amp;
        }

        /// <summary>How many samples in one cycle are a full reach, not a halfway hand.</summary>
        public static int PlantFrames(bool shaped)
        {
            int n = 0;
            for (int i = 0; i < 24; i++)
            {
                float s = (float)Math.Sin(i / 24f * 2.0 * Math.PI);
                float u = shaped ? WallPose.PlantShape(s) : s;
                float gap = u < 0f ? -u : u;
                if (gap > 0.85f) n++;
            }
            return n;
        }

        /// <summary>Same direction as the previous correction, and large enough to be a wall push.</summary>
        public static bool RepeatingPush(float popX, float popZ, float prevX, float prevZ)
        {
            float mag = (float)Math.Sqrt(popX * popX + popZ * popZ);
            float prev = (float)Math.Sqrt(prevX * prevX + prevZ * prevZ);
            if (mag < 0.02f || prev < 0.02f) return false;
            float dot = popX * prevX + popZ * prevZ;
            return dot > 0.5f * mag * prev;
        }

        /// <summary>
        /// Meters the mesh moves on this correction. A one-frame spike under the respawn
        /// gate hides. A repeat in the same direction does not. A respawn does not.
        /// </summary>
        public static float SpikeShown(float popX, float popY, float popZ, float prevX, float prevZ, bool stateChanged)
        {
            float mag = (float)Math.Sqrt(popX * popX + popY * popY + popZ * popZ);
            if (mag >= PopIgnore) return mag;
            if (mag <= 0.004f) return 0f;
            if (stateChanged) return 0f;
            if (!RepeatingPush(popX, popZ, prevX, prevZ)) return 0f;
            return (float)Math.Sqrt(popX * popX + popZ * popZ);
        }

        public static string ParkourLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            float climb = SwingKept(WallPose.ClimbCadenceFull, CycleSlew, false);
            float run = SwingKept(WallPose.RunCadenceFull, CycleSlew, false);
            return "parkour-cycle"
                + " climbSwing=" + climb.ToString("0.00", c)
                + " runSwing=" + run.ToString("0.00", c)
                + " plants=" + PlantFrames(false).ToString(c) + ">" + PlantFrames(true).ToString(c)
                + " climbRate=" + WallPose.ClimbRate(WallPose.ClimbSpeedRef).ToString("0.00", c)
                + " runRate=" + WallPose.RunRate(WallPose.WallRunSpeedRef).ToString("0.00", c)
                + " hold=1 entry=1 slip=1"
                + " mantleTrack=1 wallJumpTrack=1 zipPump=1 launchTrack=1 grapplePull=1"
                + " ledgeH=0.350>" + SpikeShown(0.35f, 0f, 0f, 0f, 0f, false).ToString("0.000", c)
                + " wallPush=0.120>" + SpikeShown(0.12f, 0f, 0f, 0.12f, 0f, false).ToString("0.000", c)
                + " boomPullIn=instant boomOut=eased fovKick=0"
                + " rootMotion=0";
        }

        public static string ResponseLine()
        {
            var sb = new StringBuilder();
            sb.Append("response");
            AppendVerb(sb, "jump", 40f, 170f, true);
            AppendVerb(sb, "slide", 68f, 3600f, true);
            AppendVerb(sb, "dash", 82f, 2800f, true);
            AppendVerb(sb, "punch", 110f, 2400f, true);
            AppendVerb(sb, "lunge", 50f, 72f, true);
            AppendVerb(sb, "climb", 50f, CycleSlew, false);
            AppendVerb(sb, "wallrun", 48f, CycleSlew, false);
            AppendVerb(sb, "walljump", 36f, CycleSlew, false);
            AppendVerb(sb, "mantle", 102f, CycleSlew, false);
            AppendVerb(sb, "zip", 40f, CycleSlew, false);
            AppendVerb(sb, "pad", 40f, CycleSlew, false);
            AppendVerb(sb, "grapple", 56f, CycleSlew, false);
            AppendVerb(sb, "release", 40f, 170f, true);
            AppendVerb(sb, "stagger", 48f, 280f, true);
            sb.Append(" gameplayDelay=0 visualDelay=0");
            return sb.ToString();
        }

        public static bool ResponsesSameFrame()
        {
            if (FirstVisible(40f, 170f, true) != 0) return false;
            if (FirstVisible(68f, 3600f, true) != 0) return false;
            if (FirstVisible(82f, 2800f, true) != 0) return false;
            if (FirstVisible(110f, 2400f, true) != 0) return false;
            if (FirstVisible(50f, 72f, true) != 0) return false;
            if (FirstVisible(50f, CycleSlew, false) != 0) return false;
            if (FirstVisible(48f, CycleSlew, false) != 0) return false;
            if (FirstVisible(36f, CycleSlew, false) != 0) return false;
            if (FirstVisible(102f, CycleSlew, false) != 0) return false;
            if (FirstVisible(40f, CycleSlew, false) != 0) return false;
            if (FirstVisible(56f, CycleSlew, false) != 0) return false;
            if (FirstVisible(48f, 280f, true) != 0) return false;
            return true;
        }

        static void AppendVerb(StringBuilder sb, string name, float degrees, float slew, bool sprung)
        {
            sb.Append(' ');
            sb.Append(name);
            sb.Append("=0/");
            sb.Append(FirstVisible(degrees, slew, sprung).ToString(CultureInfo.InvariantCulture));
        }

        static int FirstVisible(float degrees, float slew, bool sprung)
        {
            float[] s = SeriesPose(degrees, slew, sprung, 8);
            for (int i = 0; i < s.Length; i++)
            {
                float d = s[i] < 0f ? -s[i] : s[i];
                if (d > 0.75f) return i;
            }
            return 8;
        }

        struct Fig
        {
            public float ArmL, ArmR, ElbL, ElbR, ThL, ThR, KnL, KnR, Spine, Hip, Head, Lean;

            public static Fig From(WallPose.Sample s)
            {
                return new Fig
                {
                    ArmL = s.ArmPitchL, ArmR = s.ArmPitchR, ElbL = s.ElbowL, ElbR = s.ElbowR,
                    ThL = s.ThighL, ThR = s.ThighR, KnL = s.KneeL, KnR = s.KneeR,
                    Spine = s.Spine, Hip = s.Hip, Head = s.Head, Lean = s.LeanZ,
                };
            }

            public static Fig FromMantle(MantlePose.Sample s)
            {
                return new Fig
                {
                    ArmL = s.ArmPitchL, ArmR = s.ArmPitchR, ElbL = s.ElbowL, ElbR = s.ElbowR,
                    ThL = s.ThighL, ThR = s.ThighR, KnL = s.KneeL, KnR = s.KneeR,
                    Spine = s.Spine, Hip = s.Hip, Head = s.Head, Lean = 0f,
                };
            }
        }

        /// <summary>Stick figures from the pose samples. Before is the pass-1 filter. After is the cycle track.</summary>
        public static void WriteParkourStills(string path)
        {
            const int frames = 8;
            const int cellW = 128;
            const int cellH = 176;
            const int labelW = 156;
            const int rows = 6;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            Fig[] climbB = TrackClimb(false, false, 42f);
            Fig[] climbA = TrackClimb(true, false, CycleSlew);
            Fig[] runB = TrackRun(false, 42f);
            Fig[] runA = TrackRun(false, CycleSlew);
            Fig[] manB = TrackMantle(true, 1600f);
            Fig[] manA = TrackMantle(false, CycleSlew);
            PaintRow(pix, w, h, 0, "CLIMB", "BEFORE", climbB, 196, 122, 96, true);
            PaintRow(pix, w, h, 1, "CLIMB", "AFTER", climbA, 120, 196, 150, true);
            PaintRow(pix, w, h, 2, "WALL RUN", "BEFORE", runB, 196, 122, 96, true);
            PaintRow(pix, w, h, 3, "WALL RUN", "AFTER", runA, 120, 186, 210, true);
            PaintRow(pix, w, h, 4, "MANTLE", "BEFORE", manB, 196, 122, 96, false);
            PaintRow(pix, w, h, 5, "MANTLE", "AFTER", manA, 230, 196, 120, false);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        /// <summary>
        /// Stick figures from the gait, the jump, and the contact pitches.
        /// Before is the capped stride or the plain jump. After is this pass.
        /// </summary>
        public static void WriteLocomotionStills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 12;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            Fig[] sprintB = TrackStride(false, false, false);
            Fig[] sprintA = TrackStride(true, false, false);
            Fig[] strafeB = TrackStride(true, false, false);
            Fig[] strafeA = TrackStrafe();
            Fig[] airB = TrackAir(false);
            Fig[] airA = TrackAir(true);
            Fig[] turnB = TrackStride(true, false, false);
            Fig[] turnA = TrackTurn();
            Fig[] slopeB = TrackStride(true, false, false);
            Fig[] slopeA = TrackSlope();
            Fig[] handB = TrackHand(false);
            Fig[] handA = TrackHand(true);
            PaintLoco(pix, w, h, rows, 0, "SPRINT", "BEFORE", sprintB, 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 1, "SPRINT", "AFTER", sprintA, 120, 196, 150, false, 0f);
            PaintLoco(pix, w, h, rows, 2, "STRAFE", "BEFORE", strafeB, 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 3, "STRAFE", "AFTER", strafeA, 120, 186, 210, false, 0f);
            PaintLoco(pix, w, h, rows, 4, "AIR", "BEFORE", airB, 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 5, "AIR", "AFTER", airA, 186, 168, 230, false, 0f);
            PaintLoco(pix, w, h, rows, 6, "TURN", "BEFORE", turnB, 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 7, "TURN", "AFTER", turnA, 230, 176, 120, false, 0f);
            PaintLoco(pix, w, h, rows, 8, "SLOPE", "BEFORE", slopeB, 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 9, "SLOPE", "AFTER", slopeA, 150, 210, 140, false, 18f);
            PaintLoco(pix, w, h, rows, 10, "HAND", "BEFORE", handB, 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 11, "HAND", "AFTER", handA, 230, 200, 120, true, 0f);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        /// <summary>
        /// Idle, crouch, slide, slip, the wall handoff, and the hit flinch.
        /// Before is the pose that was already playing. After is this pass.
        /// </summary>
        public static void WritePass4Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 12;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            PaintLoco(pix, w, h, rows, 0, "IDLE", "BEFORE", TrackIdle(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 1, "IDLE", "AFTER", TrackIdle(true), 120, 196, 150, false, 0f);
            PaintLoco(pix, w, h, rows, 2, "CROUCH", "BEFORE", TrackCrouch(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 3, "CROUCH", "AFTER", TrackCrouch(true), 120, 186, 210, false, 0f);
            PaintLoco(pix, w, h, rows, 4, "SLIDE", "BEFORE", TrackSlide(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 5, "SLIDE", "AFTER", TrackSlide(true), 230, 176, 120, false, 0f);
            PaintLoco(pix, w, h, rows, 6, "SLIP", "BEFORE", TrackSlip(false), 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 7, "SLIP", "AFTER", TrackSlip(true), 150, 210, 140, true, 0f);
            PaintLoco(pix, w, h, rows, 8, "WALL", "BEFORE", TrackWallMode(false), 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 9, "WALL", "AFTER", TrackWallMode(true), 186, 168, 230, true, 0f);
            PaintLoco(pix, w, h, rows, 10, "FLINCH", "BEFORE", TrackFlinch(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 11, "FLINCH", "AFTER", TrackFlinch(true), 230, 200, 120, false, 0f);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        /// <summary>
        /// Upper body over a run, and hang secondary motion.
        /// Before replaces the legs. After keeps the cycle and adds the hang.
        /// </summary>
        public static void WritePass5Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 12;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            PaintLoco(pix, w, h, rows, 0, "PUNCH", "BEFORE", TrackPunchLayer(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 1, "PUNCH", "AFTER", TrackPunchLayer(true), 120, 196, 150, false, 0f);
            PaintLoco(pix, w, h, rows, 2, "TELL", "BEFORE", TrackTellLayer(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 3, "TELL", "AFTER", TrackTellLayer(true), 186, 168, 230, false, 0f);
            PaintLoco(pix, w, h, rows, 4, "AIM", "BEFORE", TrackAimLayer(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 5, "AIM", "AFTER", TrackAimLayer(true), 150, 210, 140, false, 0f);
            PaintLoco(pix, w, h, rows, 6, "ZIP", "BEFORE", TrackZipHang(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 7, "ZIP", "AFTER", TrackZipHang(true), 210, 140, 210, false, 0f);
            PaintLoco(pix, w, h, rows, 8, "ROPE", "BEFORE", TrackRopeHang(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 9, "ROPE", "AFTER", TrackRopeHang(true), 230, 176, 120, false, 0f);
            PaintLoco(pix, w, h, rows, 10, "PAD", "BEFORE", TrackPadMill(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 11, "PAD", "AFTER", TrackPadMill(true), 120, 186, 210, false, 0f);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        /// <summary>
        /// Pass 7. Short stride versus a planted step, the wall-run plant, the vault
        /// exit, the respawn blink, and a yaw that eases after the capsule has turned.
        /// </summary>
        public static void WritePass7Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 12;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            PaintLoco(pix, w, h, rows, 0, "WALK", "BEFORE", TrackGround(FootSlide.Walk, false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 1, "WALK", "AFTER", TrackGround(FootSlide.Walk, true), 120, 196, 150, false, 0f);
            PaintLoco(pix, w, h, rows, 2, "SPRINT", "BEFORE", TrackGround(FootSlide.Sprint, false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 3, "SPRINT", "AFTER", TrackGround(FootSlide.Sprint, true), 120, 186, 210, false, 0f);
            PaintLoco(pix, w, h, rows, 4, "WALL", "BEFORE", TrackWallPlant(false), 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 5, "WALL", "AFTER", TrackWallPlant(true), 210, 170, 110, true, 0f);
            PaintLoco(pix, w, h, rows, 6, "VAULT", "BEFORE", TrackVaultExit(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 7, "VAULT", "AFTER", TrackVaultExit(true), 150, 210, 140, false, 0f);
            PaintLoco(pix, w, h, rows, 8, "BLINK", "BEFORE", TrackBlink(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 9, "BLINK", "AFTER", TrackBlink(true), 230, 210, 140, false, 0f);
            PaintLoco(pix, w, h, rows, 10, "YAW", "BEFORE", TrackYaw(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 11, "YAW", "AFTER", TrackYaw(true), 170, 150, 220, false, 0f);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        static Fig Limb(float thL, float thR, float knL, float knR, float armL, float armR, float lean)
        {
            return new Fig
            {
                ArmL = armL,
                ArmR = armR,
                ElbL = -18f,
                ElbR = -18f,
                ThL = thL,
                ThR = thR,
                KnL = knL,
                KnR = knR,
                Spine = 6f,
                Hip = 0f,
                Head = -4f,
                Lean = lean,
            };
        }

        static Fig[] TrackGround(float speed, bool planted)
        {
            float cad = planted
                ? LocomotionPolish.PlayCadence(speed)
                : GaitBlend.CadenceAt(speed > 10f ? GaitBlend.SprintSpeed : GaitBlend.WalkSpeed);
            if (cad < 0.05f) cad = 0.05f;
            float body = speed * 3.14159265f / cad;
            float foot = GaitBlend.FootTravel(GaitBlend.PoseWeight(speed));
            float amp = planted || body < 0.05f ? 1f : foot / body;
            if (amp > 1f) amp = 1f;
            var shot = new Fig[8];
            float phase = 0.4f;
            for (int i = 0; i < 8; i++)
            {
                phase += 0.62f;
                GaitBlend.Legs legs = GaitBlend.At(phase, speed);
                shot[i] = Limb(legs.ThighL * amp, legs.ThighR * amp, legs.KneeL, legs.KneeR, -legs.ThighR * 0.55f, -legs.ThighL * 0.55f, 0f);
            }
            return shot;
        }

        static Fig[] TrackWallPlant(bool planted)
        {
            var shot = new Fig[8];
            float phase = 0f;
            for (int i = 0; i < 8; i++)
            {
                phase += 0.7f;
                if (planted)
                {
                    shot[i] = Fig.From(WallPose.RunCycle(phase, true));
                    continue;
                }
                GaitBlend.Legs legs = GaitBlend.At(phase, WallPose.WallRunSpeedRef);
                shot[i] = Limb(legs.ThighL * 0.72f, legs.ThighR, legs.KneeL, legs.KneeR, -40f, legs.ThighL * 0.4f, -18f);
            }
            return shot;
        }

        static Fig[] TrackVaultExit(bool settled)
        {
            var shot = new Fig[8];
            Fig cur = new Fig { ArmL = -20f, ArmR = -16f, ElbL = -14f, ElbR = -12f, ThL = 10f, ThR = 8f, KnL = -12f, KnR = -8f };
            var vel = new float[12];
            for (int i = 0; i < 8; i++)
            {
                float u = (i + 1) / 8f;
                Fig want = Fig.FromMantle(MantlePose.At(u, true));
                cur = Step(cur, want, vel, true, MantlePose.Slew);
                shot[i] = cur;
            }
            if (!settled)
            {
                shot[7].Hip = -28f;
                shot[7].Spine = shot[6].Spine;
                shot[7].ThL = shot[6].ThL;
                shot[7].ThR = shot[6].ThR;
            }
            return shot;
        }

        static Fig ScaleFig(Fig f, float s)
        {
            f.ArmL *= s;
            f.ArmR *= s;
            f.ElbL *= s;
            f.ElbR *= s;
            f.ThL *= s;
            f.ThR *= s;
            f.KnL *= s;
            f.KnR *= s;
            f.Spine *= s;
            f.Hip *= s;
            f.Head *= s;
            f.Lean *= s;
            return f;
        }

        static Fig[] TrackBlink(bool eased)
        {
            Fig stand = Limb(18f, -14f, -8f, -6f, -24f, 16f, 0f);
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float age = eased ? i * (RespawnBlink.Seconds / 7f) : (i < 2 ? 10f : 0f);
                float open = 1f - RespawnBlink.Hidden(age);
                shot[i] = ScaleFig(stand, open);
            }
            return shot;
        }

        static Fig[] TrackYaw(bool eased)
        {
            var shot = new Fig[8];
            float yaw = 0f;
            float vel = 0f;
            for (int i = 0; i < 8; i++)
            {
                float target = i >= 3 ? 70f : 0f;
                if (eased)
                    yaw = Smooth(yaw, target, ref vel, YawSeconds, Dt);
                else
                    yaw = target;
                shot[i] = Limb(16f, -12f, -10f, -8f, -20f, 12f, yaw);
            }
            return shot;
        }

        /// <summary>
        /// Pass 8. Climb contact, the lip, a vault at the real rail, wall-run tilt,
        /// the cling grab, and the slip drag.
        /// </summary>
        public static void WritePass8Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 12;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            PaintLoco(pix, w, h, rows, 0, "CLIMB", "BEFORE", TrackClimbContact(false), 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 1, "CLIMB", "AFTER", TrackClimbContact(true), 120, 196, 150, true, 0f);
            PaintLoco(pix, w, h, rows, 2, "LIP", "BEFORE", TrackLip(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 3, "LIP", "AFTER", TrackLip(true), 150, 210, 140, false, 0f);
            PaintLoco(pix, w, h, rows, 4, "VAULT", "BEFORE", TrackVaultHeight(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 5, "VAULT", "AFTER", TrackVaultHeight(true), 120, 186, 210, false, 0f);
            PaintLoco(pix, w, h, rows, 6, "RUN", "BEFORE", TrackRunTilt(false), 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 7, "RUN", "AFTER", TrackRunTilt(true), 210, 170, 110, true, 0f);
            PaintLoco(pix, w, h, rows, 8, "GRAB", "BEFORE", TrackGrab(false), 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 9, "GRAB", "AFTER", TrackGrab(true), 230, 210, 140, true, 0f);
            PaintLoco(pix, w, h, rows, 10, "SLIP", "BEFORE", TrackSlipDrag(false), 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 11, "SLIP", "AFTER", TrackSlipDrag(true), 186, 140, 120, true, 0f);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        /// <summary>
        /// Pass 9. Wall-jump arc, the rope line, zip grab and drop, the pad swing,
        /// a reversal, and a moving punch.
        /// </summary>
        public static void WritePass9Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 12;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            PaintLoco(pix, w, h, rows, 0, "WALL JUMP", "BEFORE", TrackWallArc(false), 196, 122, 96, true, 0f);
            PaintLoco(pix, w, h, rows, 1, "WALL JUMP", "AFTER", TrackWallArc(true), 120, 196, 150, true, 0f);
            PaintLoco(pix, w, h, rows, 2, "ROPE", "BEFORE", TrackRope(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 3, "ROPE", "AFTER", TrackRope(true), 150, 210, 140, false, 0f);
            PaintLoco(pix, w, h, rows, 4, "ZIP", "BEFORE", TrackZip(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 5, "ZIP", "AFTER", TrackZip(true), 120, 186, 210, false, 0f);
            PaintLoco(pix, w, h, rows, 6, "PAD", "BEFORE", TrackPad(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 7, "PAD", "AFTER", TrackPad(true), 210, 170, 110, false, 0f);
            PaintLoco(pix, w, h, rows, 8, "REVERSAL", "BEFORE", TrackReverse(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 9, "REVERSAL", "AFTER", TrackReverse(true), 230, 210, 140, false, 0f);
            PaintLoco(pix, w, h, rows, 10, "PUNCH", "BEFORE", TrackReach(false), 196, 122, 96, false, 0f);
            PaintLoco(pix, w, h, rows, 11, "PUNCH", "AFTER", TrackReach(true), 186, 140, 120, false, 0f);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        /// <summary>
        /// Pass 10. Run, start, stop, turn, idle, crouch walk, and slide.
        /// Before, then after. Labels use the bundled face.
        /// </summary>
        public static void WritePass10Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 14;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            string[] titles = { "STRIDE", "START", "STOP", "TURN", "IDLE", "CROUCH", "SLIDE" };
            byte[] cr = { 120, 186, 230, 150, 210, 186, 230 };
            byte[] cg = { 196, 168, 176, 210, 170, 140, 200 };
            byte[] cb = { 150, 230, 120, 140, 110, 120, 120 };
            for (int k = 0; k < titles.Length; k++)
            {
                PaintLoco(pix, w, h, rows, k * 2, titles[k], "BEFORE", TrackFeel(k, false), 196, 122, 96, false, 0f);
                PaintLoco(pix, w, h, rows, k * 2 + 1, titles[k], "AFTER", TrackFeel(k, true), cr[k], cg[k], cb[k], false, 0f);
            }
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        static Fig[] TrackFeel(int kind, bool after)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                LocoFeel.Shot s = LocoFeel.ShotAt(kind, i, after);
                shot[i] = new Fig
                {
                    ArmL = s.ArmL,
                    ArmR = s.ArmR,
                    ElbL = s.ElbL,
                    ElbR = s.ElbR,
                    ThL = s.ThL,
                    ThR = s.ThR,
                    KnL = s.KnL,
                    KnR = s.KnR,
                    Spine = s.Spine,
                    Hip = s.Hip,
                    Head = s.Head,
                    Lean = s.Lean,
                };
            }
            return shot;
        }

        public static void WritePass11Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 12;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            string[] titles = { "TAKEOFF", "APEX", "FALL", "HOP", "STRAFE", "COYOTE" };
            byte[] cr = { 120, 186, 230, 150, 210, 186 };
            byte[] cg = { 196, 168, 176, 210, 170, 140 };
            byte[] cb = { 150, 230, 120, 140, 110, 210 };
            for (int k = 0; k < titles.Length; k++)
            {
                PaintLoco(pix, w, h, rows, k * 2, titles[k], "BEFORE", TrackAir(k, false), 196, 122, 96, false, 0f);
                PaintLoco(pix, w, h, rows, k * 2 + 1, titles[k], "AFTER", TrackAir(k, true), cr[k], cg[k], cb[k], false, 0f);
            }
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        public static void WritePass12Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 8;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            string[] titles = { "LAND RUN", "LAND ROLL", "WALL JUMP", "CLING DROP" };
            byte[] cr = { 120, 186, 230, 210 };
            byte[] cg = { 196, 168, 176, 170 };
            byte[] cb = { 150, 120, 140, 110 };
            for (int k = 0; k < titles.Length; k++)
            {
                PaintLoco(pix, w, h, rows, k * 2, titles[k], "BEFORE", TrackHandoff(k, false), 196, 122, 96, false, 0f);
                PaintLoco(pix, w, h, rows, k * 2 + 1, titles[k], "AFTER", TrackHandoff(k, true), cr[k], cg[k], cb[k], false, 0f);
            }
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        public static void WritePass13Stills(string path)
        {
            const int frames = 8;
            const int cellW = 120;
            const int cellH = 156;
            const int labelW = 168;
            const int rows = 22;
            int w = labelW + frames * cellW;
            int h = rows * cellH;
            var pix = new byte[w * h * 3];
            Fill(pix, w, h, 16, 18, 22);
            string[] titles =
            {
                "CLIMB", "VAULT IN", "VAULT OUT", "SLIDE IN", "SLIDE OUT",
                "ZIP GRAB", "ZIP DROP", "GRAPPLE IN", "GRAPPLE OUT", "PAD UP", "PAD AIR",
            };
            byte[] cr = { 120, 186, 230, 150, 210, 186, 140, 200, 170, 230, 160 };
            byte[] cg = { 196, 168, 176, 210, 170, 140, 190, 160, 200, 180, 150 };
            byte[] cb = { 150, 120, 140, 110, 160, 210, 170, 120, 140, 110, 200 };
            for (int k = 0; k < titles.Length; k++)
            {
                PaintLoco(pix, w, h, rows, k * 2, titles[k], "BEFORE", TrackHandoff2(k, false), 196, 122, 96, false, 0f);
                PaintLoco(pix, w, h, rows, k * 2 + 1, titles[k], "AFTER", TrackHandoff2(k, true), cr[k], cg[k], cb[k], false, 0f);
            }
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            WritePng(path, pix, w, h);
        }

        static Fig[] TrackHandoff2(int kind, bool after)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                Handoff2Feel.Shot s = Handoff2Feel.ShotAt(kind, i, after);
                shot[i] = new Fig
                {
                    ArmL = s.ArmL,
                    ArmR = s.ArmR,
                    ElbL = s.ElbL,
                    ElbR = s.ElbR,
                    ThL = s.ThL,
                    ThR = s.ThR,
                    KnL = s.KnL,
                    KnR = s.KnR,
                    Spine = s.Spine,
                    Hip = s.Hip,
                    Head = s.Head,
                    Lean = s.Lean,
                };
            }
            return shot;
        }

        static Fig[] TrackHandoff(int kind, bool after)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                HandoffFeel.Shot s = HandoffFeel.ShotAt(kind, i, after);
                shot[i] = new Fig
                {
                    ArmL = s.ArmL,
                    ArmR = s.ArmR,
                    ElbL = s.ElbL,
                    ElbR = s.ElbR,
                    ThL = s.ThL,
                    ThR = s.ThR,
                    KnL = s.KnL,
                    KnR = s.KnR,
                    Spine = s.Spine,
                    Hip = s.Hip,
                    Head = s.Head,
                    Lean = s.Lean,
                };
            }
            return shot;
        }

        static Fig[] TrackAir(int kind, bool after)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                AirFeel.Shot s = AirFeel.ShotAt(kind, i, after);
                shot[i] = new Fig
                {
                    ArmL = s.ArmL,
                    ArmR = s.ArmR,
                    ElbL = s.ElbL,
                    ElbR = s.ElbR,
                    ThL = s.ThL,
                    ThR = s.ThR,
                    KnL = s.KnL,
                    KnR = s.KnR,
                    Spine = s.Spine,
                    Hip = s.Hip,
                    Head = s.Head,
                    Lean = s.Lean,
                };
            }
            return shot;
        }

        static Fig[] TrackWallArc(bool arced)
        {
            var shot = new Fig[8];
            float from = WallJumpPose.PushPitch;
            float to = JumpPose.TuckArmPitch;
            float total = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
            WallJumpPose.Sample push = WallJumpPose.Push(true);
            for (int i = 0; i < 8; i++)
            {
                float age = total * (i / 7f);
                float w = arced ? BodyLine.WallArc(age) : WallJumpPose.JumpWeight(age);
                float end = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
                WallJumpPose.Sample air = WallJumpPose.At(end, 24.7f, true, 0f);
                shot[i] = new Fig
                {
                    ArmL = Mathf.Lerp(push.ArmPitchL, air.ArmPitchL, w),
                    ArmR = Mathf.Lerp(push.ArmPitchR, air.ArmPitchR, w),
                    ElbL = Mathf.Lerp(push.ElbowL, air.ElbowL, w),
                    ElbR = Mathf.Lerp(push.ElbowR, air.ElbowR, w),
                    ThL = Mathf.Lerp(push.ThighL, air.ThighL, w),
                    ThR = Mathf.Lerp(push.ThighR, air.ThighR, w),
                    KnL = Mathf.Lerp(push.KneeL, air.KneeL, w),
                    KnR = Mathf.Lerp(push.KneeR, air.KneeR, w),
                    Spine = Mathf.Lerp(push.Spine, air.Spine, w),
                    Hip = Mathf.Lerp(push.Hip, air.Hip, w),
                    Head = Mathf.Lerp(push.Head, air.Head, w),
                    Lean = Mathf.Lerp(push.LeanZ, air.LeanZ, w),
                };
                if (!arced && i == 0)
                    shot[i].ArmL = from;
                if (!arced && i == 7)
                    shot[i].ArmL = to;
            }
            return shot;
        }

        static Fig[] TrackRope(bool lined)
        {
            var shot = new Fig[8];
            GrapplePose.Sample pull = GrapplePose.Pull(0f, 0f, 0f);
            float elev = 40f;
            float body = pull.Hip + pull.Spine + HangMotion.RopeSpine(elev);
            float fix = lined ? BodyLine.LineFix(body, elev) : HangMotion.RopeSpine(elev);
            for (int i = 0; i < 8; i++)
            {
                float w = i / 7f;
                float add = fix * (lined ? w : (i < 2 ? 0f : 1f));
                shot[i] = new Fig
                {
                    ArmL = pull.ArmPitchL,
                    ArmR = pull.ArmPitchR,
                    ElbL = pull.ElbowL,
                    ElbR = pull.ElbowR,
                    ThL = pull.ThighL,
                    ThR = pull.ThighR,
                    KnL = pull.KneeL,
                    KnR = pull.KneeR,
                    Spine = pull.Spine + add * 0.55f + (lined ? 0f : HangMotion.RopeSpine(elev) * (i < 2 ? 0f : 1f)),
                    Hip = pull.Hip + (lined ? add * 0.45f : 0f),
                    Head = pull.Head,
                    Lean = elev * 0.15f,
                };
            }
            return shot;
        }

        static Fig[] TrackZip(bool eased)
        {
            var shot = new Fig[8];
            ZipPose.Sample hang = ZipPose.Hang();
            ZipPose.Sample let = ZipPose.Release();
            for (int i = 0; i < 8; i++)
            {
                float age = ZipPose.ReleaseSeconds * (i / 7f);
                float pitch;
                float thigh;
                if (!eased)
                {
                    pitch = i < 4 ? WallPose.ReachPitch : ZipPose.ReleasePitch;
                    thigh = i < 4 ? hang.ThighL : let.ThighL;
                }
                else
                {
                    pitch = i < 4
                        ? Mathf.Lerp(JumpPose.FallArmPitch, BodyLine.CablePitch, BodyLine.ZipGrab(age))
                        : BodyLine.ZipArm(age, true, JumpPose.FallArmPitch);
                    thigh = hang.ThighL;
                }
                shot[i] = new Fig
                {
                    ArmL = pitch,
                    ArmR = pitch,
                    ElbL = eased && i >= 4 ? let.ElbowL : hang.ElbowL,
                    ElbR = eased && i >= 4 ? let.ElbowR : hang.ElbowR,
                    ThL = thigh,
                    ThR = thigh - 4f,
                    KnL = hang.KneeL,
                    KnR = hang.KneeR,
                    Spine = hang.Spine,
                    Hip = hang.Hip,
                    Head = hang.Head,
                    Lean = 0f,
                };
            }
            return shot;
        }

        static Fig[] TrackPad(bool eased)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float age = BodyLine.PadSeconds * (i / 7f);
                float open = eased ? BodyLine.PadOpen(age) : (i < 1 ? 0f : 1f);
                LaunchPose.Sample pose = LaunchPose.At(i < 5 ? 24.7f : -12f);
                shot[i] = new Fig
                {
                    ArmL = Mathf.Lerp(0f, pose.ArmPitchL, open),
                    ArmR = Mathf.Lerp(0f, pose.ArmPitchR, open),
                    ElbL = pose.ElbowL,
                    ElbR = pose.ElbowR,
                    ThL = Mathf.Lerp(12f, pose.ThighL, open),
                    ThR = Mathf.Lerp(8f, pose.ThighR, open),
                    KnL = Mathf.Lerp(-8f, pose.KneeL, open),
                    KnR = Mathf.Lerp(-6f, pose.KneeR, open),
                    Spine = pose.Spine,
                    Hip = pose.Hip,
                    Head = pose.Head,
                    Lean = 0f,
                };
            }
            return shot;
        }

        static Fig[] TrackReverse(bool eased)
        {
            var shot = new Fig[8];
            GaitBlend.Legs legs = GaitBlend.At(1.2f, 13.8f);
            LocomotionPolish.Legs fwd = LocomotionPolish.FacingStride(legs.ThighL, legs.ThighR, legs.KneeL, legs.KneeR, 13.8f, 0f);
            LocomotionPolish.Legs back = LocomotionPolish.FacingStride(legs.ThighL, legs.ThighR, legs.KneeL, legs.KneeR, -13.8f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float w = eased ? BodyLine.ReverseBlend(i / 7f) : (i < 3 ? 0f : 1f);
                shot[i] = new Fig
                {
                    ArmL = -24f,
                    ArmR = 18f,
                    ElbL = -12f,
                    ElbR = -10f,
                    ThL = Mathf.Lerp(fwd.ThighL, back.ThighL, w),
                    ThR = Mathf.Lerp(fwd.ThighR, back.ThighR, w),
                    KnL = Mathf.Lerp(fwd.KneeL, back.KneeL, w),
                    KnR = Mathf.Lerp(fwd.KneeR, back.KneeR, w),
                    Spine = 4f,
                    Hip = 0f,
                    Head = -4f,
                    Lean = 0f,
                };
            }
            return shot;
        }

        static Fig[] TrackReach(bool led)
        {
            var shot = new Fig[8];
            float lead = led ? BodyLine.ReachLead(13.8f) : 0f;
            for (int i = 0; i < 8; i++)
            {
                float stride = (float)Math.Sin(i * 0.8f) * 34f;
                float punch = i < 3 ? VerbPoseClips.PunchCockPitch : VerbPoseClips.PunchStrikePitch;
                shot[i] = new Fig
                {
                    ArmL = -18f,
                    ArmR = punch,
                    ElbL = -14f,
                    ElbR = i < 3 ? VerbPoseClips.PunchCockElbow : VerbPoseClips.PunchStrikeElbow,
                    ThL = stride,
                    ThR = -stride,
                    KnL = -16f,
                    KnR = -12f,
                    Spine = 6f + lead,
                    Hip = 4f + lead * 0.35f,
                    Head = -2f,
                    Lean = 0f,
                };
            }
            return shot;
        }

        static Fig[] TrackClimbContact(bool planted)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float s = (float)Math.Sin(i * 0.7f);
                Fig f = Fig.From(WallPose.Climb(s, ClimbContact.ClimbSpeed));
                if (planted)
                {
                    float step = ClimbContact.BodyStep(ClimbContact.ClimbSpeed, WallPose.ClimbCadenceFull);
                    float extra = step - ClimbContact.HandArc();
                    if (extra < 0f) extra = 0f;
                    float deg = extra / ClimbContact.ArmLength * Mathf.Rad2Deg;
                    if (s >= 0f) f.ArmR -= deg * 0.35f;
                    else f.ArmL -= deg * 0.35f;
                }
                shot[i] = f;
            }
            return shot;
        }

        static Fig[] TrackLip(bool rolled)
        {
            var shot = new Fig[8];
            Fig cur = Fig.FromMantle(MantlePose.At(0f, true));
            var vel = new float[12];
            for (int i = 0; i < 8; i++)
            {
                float u = (i + 1) / 8f;
                cur = Step(cur, Fig.FromMantle(MantlePose.At(u, true)), vel, true, MantlePose.Slew);
                shot[i] = cur;
            }
            if (!rolled)
            {
                shot[5].Hip = shot[4].Hip - 24f;
                shot[5].Spine = shot[4].Spine;
            }
            return shot;
        }

        static Fig[] TrackVaultHeight(bool real)
        {
            var shot = new Fig[8];
            float miss = real ? 0f : ClimbContact.LipMiss(1.40f, false) * 0.15f;
            for (int i = 0; i < 8; i++)
            {
                float u = (i + 1) / 8f;
                Fig f = Fig.FromMantle(MantlePose.At(u, true));
                f.ArmL += miss;
                f.ArmR += miss;
                shot[i] = f;
            }
            return shot;
        }

        static Fig[] TrackRunTilt(bool scaled)
        {
            var shot = new Fig[8];
            float phase = 0f;
            for (int i = 0; i < 8; i++)
            {
                phase += 0.7f;
                Fig f = Fig.From(WallPose.RunCycle(phase, true));
                float speed = scaled ? (i < 4 ? WallPose.WallRunSpeedRef * 0.5f : WallPose.WallRunSpeedRef) : WallPose.WallRunSpeedRef;
                float blend = scaled ? ClimbContact.Grab(i / 7f) : (i < 2 ? 0f : 1f);
                float sign = f.Lean < 0f ? -1f : 1f;
                f.Lean = sign * ClimbContact.Tilt(speed) * blend;
                shot[i] = f;
            }
            return shot;
        }

        static Fig[] TrackGrab(bool eased)
        {
            Fig air = Limb(22f, 18f, -20f, -16f, -30f, -24f, 0f);
            Fig wall = Fig.From(WallPose.Entry());
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float u = eased ? ClimbContact.Grab(i / 7f) : (i < 3 ? 0f : 1f);
                shot[i] = new Fig
                {
                    ArmL = air.ArmL + (wall.ArmL - air.ArmL) * u,
                    ArmR = air.ArmR + (wall.ArmR - air.ArmR) * u,
                    ElbL = air.ElbL + (wall.ElbL - air.ElbL) * u,
                    ElbR = air.ElbR + (wall.ElbR - air.ElbR) * u,
                    ThL = air.ThL + (wall.ThL - air.ThL) * u,
                    ThR = air.ThR + (wall.ThR - air.ThR) * u,
                    KnL = air.KnL + (wall.KnL - air.KnL) * u,
                    KnR = air.KnR + (wall.KnR - air.KnR) * u,
                    Spine = air.Spine + (wall.Spine - air.Spine) * u,
                    Hip = air.Hip + (wall.Hip - air.Hip) * u,
                    Head = air.Head + (wall.Head - air.Head) * u,
                    Lean = 0f,
                };
            }
            return shot;
        }

        static Fig[] TrackSlipDrag(bool drag)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float s = (float)Math.Sin(i * 0.8f);
                Fig f = Fig.From(WallPose.Climb(s, -ClimbContact.SlipSpeed));
                if (drag)
                {
                    ClimbContact.Drag(i * 0.8f, 1f, out float l, out float r);
                    f.ArmL += l;
                    f.ArmR += r;
                }
                shot[i] = f;
            }
            return shot;
        }

        static float StrideThigh(int frame, float sign)
        {
            return (float)Math.Sin(frame * 0.78f) * 34f * sign;
        }

        static Fig[] TrackPunchLayer(bool layered)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float arm = i < 4 ? VerbPoseClips.PunchCockPitch : VerbPoseClips.PunchStrikePitch;
                float yaw = i < 4 ? VerbPoseClips.PunchCockSpineYaw : VerbPoseClips.PunchStrikeSpineYaw;
                float twist = UpperBody.AimTwist(yaw);
                shot[i] = new Fig
                {
                    ArmL = -20f,
                    ArmR = arm,
                    ElbL = -16f,
                    ElbR = i < 4 ? VerbPoseClips.PunchCockElbow : VerbPoseClips.PunchStrikeElbow,
                    ThL = layered ? StrideThigh(i, 1f) : (i < 4 ? 8f : VerbPoseClips.PunchStrikeLeadThigh),
                    ThR = layered ? StrideThigh(i, -1f) : (i < 4 ? -6f : VerbPoseClips.PunchStrikeTrailThigh),
                    KnL = layered ? -18f : -8f,
                    KnR = layered ? -12f : -6f,
                    Spine = 8f,
                    Hip = 0f,
                    Head = -6f,
                    Lean = twist * 0.35f,
                };
            }
            return shot;
        }

        static Fig[] TrackTellLayer(bool layered)
        {
            var shot = new Fig[8];
            LungePose.Sample tell = LungePose.Telegraph();
            float twist = UpperBody.AimTwist(tell.SpineYaw);
            for (int i = 0; i < 8; i++)
            {
                shot[i] = new Fig
                {
                    ArmL = tell.ArmPitchL,
                    ArmR = tell.ArmPitchR,
                    ElbL = tell.ElbowL,
                    ElbR = tell.ElbowR,
                    ThL = layered ? StrideThigh(i, 1f) : tell.ThighL,
                    ThR = layered ? StrideThigh(i, -1f) : tell.ThighR,
                    KnL = layered ? -16f : tell.KneeL,
                    KnR = layered ? -10f : tell.KneeR,
                    Spine = tell.Spine,
                    Hip = layered ? 0f : tell.Hip,
                    Head = tell.Head,
                    Lean = twist * 0.35f,
                };
            }
            return shot;
        }

        static Fig[] TrackAimLayer(bool layered)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float yaw = -28f + i * 8f;
                GrapplePose.Sample aim = GrapplePose.Aim(12f, yaw);
                float twist = layered ? UpperBody.AimTwist(aim.SpineYaw) : aim.SpineYaw;
                shot[i] = new Fig
                {
                    ArmL = aim.ArmPitchL,
                    ArmR = aim.ArmPitchR,
                    ElbL = aim.ElbowL,
                    ElbR = aim.ElbowR,
                    ThL = layered ? StrideThigh(i, 1f) : aim.ThighL,
                    ThR = layered ? StrideThigh(i, -1f) : aim.ThighR,
                    KnL = layered ? -14f : aim.KneeL,
                    KnR = layered ? -10f : aim.KneeR,
                    Spine = aim.Spine,
                    Hip = 0f,
                    Head = aim.Head,
                    Lean = twist,
                };
            }
            return shot;
        }

        static Fig[] TrackZipHang(bool trail)
        {
            var shot = new Fig[8];
            WallPose.Sample hang = WallPose.CableHang();
            for (int i = 0; i < 8; i++)
            {
                float time = i * 0.22f;
                float speed = trail ? HangMotion.ZipSpeed : 0f;
                float legs = trail ? HangMotion.LegTrail(speed) : 0f;
                float sway = ZipPose.Sway(time, speed) + (trail ? HangMotion.SwayExtra(time, speed) : 0f);
                shot[i] = new Fig
                {
                    ArmL = hang.ArmPitchL,
                    ArmR = hang.ArmPitchR,
                    ElbL = hang.ElbowL,
                    ElbR = hang.ElbowR,
                    ThL = hang.ThighL + legs,
                    ThR = hang.ThighR + legs,
                    KnL = hang.KneeL,
                    KnR = hang.KneeR,
                    Spine = hang.Spine,
                    Hip = hang.Hip,
                    Head = hang.Head,
                    Lean = sway,
                };
            }
            return shot;
        }

        static Fig[] TrackRopeHang(bool trail)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float elev = -10f + i * 8f;
                GrapplePose.Sample pull = GrapplePose.Pull(i % 2 == 0 ? 1f : -1f, 4f, 8f);
                float pitch = trail ? HangMotion.RopeSpine(elev) : 0f;
                float legs = trail ? HangMotion.RopeLeg(HangMotion.ZipSpeed) : 0f;
                shot[i] = new Fig
                {
                    ArmL = pull.ArmPitchL,
                    ArmR = pull.ArmPitchR,
                    ElbL = pull.ElbowL,
                    ElbR = pull.ElbowR,
                    ThL = pull.ThighL + legs,
                    ThR = pull.ThighR + legs,
                    KnL = pull.KneeL,
                    KnR = pull.KneeR,
                    Spine = pull.Spine + pitch,
                    Hip = pull.Hip,
                    Head = pull.Head,
                    Lean = pull.SpineYaw,
                };
            }
            return shot;
        }

        static Fig[] TrackPadMill(bool mill)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float vy = i < 5 ? 24.7f - i * 6f : -8f - (i - 5) * 4f;
                LaunchPose.Sample pose = LaunchPose.At(vy);
                float t = 0.35f + i * 0.18f;
                float add = mill ? HangMotion.Windmill(t, vy) : 0f;
                shot[i] = new Fig
                {
                    ArmL = pose.ArmPitchL + add,
                    ArmR = pose.ArmPitchR - add,
                    ElbL = pose.ElbowL,
                    ElbR = pose.ElbowR,
                    ThL = pose.ThighL,
                    ThR = pose.ThighR,
                    KnL = pose.KneeL,
                    KnR = pose.KneeR,
                    Spine = pose.Spine,
                    Hip = pose.Hip,
                    Head = pose.Head,
                    Lean = 0f,
                };
            }
            return shot;
        }

        static Fig[] TrackIdle(bool life)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float shift = i * 0.55f;
                float breath = i * 0.7f;
                IdlePose.Sample idle = IdlePose.At(shift, breath);
                float look = 0f;
                float chest = 0f;
                if (life)
                {
                    float t = BodyLife.LookPeriod * (1f - BodyLife.LookWindow) + i * 0.18f;
                    look = BodyLife.LookYawAt(t);
                    chest = i < 4 ? BodyLife.PostureChest(false) : BodyLife.PostureChest(true);
                }
                shot[i] = new Fig
                {
                    ArmL = -16f + idle.Shoulder, ArmR = -16f + idle.Shoulder,
                    ElbL = -12f, ElbR = -12f,
                    ThL = idle.ThighL, ThR = idle.ThighR,
                    KnL = idle.KneeL, KnR = idle.KneeR,
                    Spine = idle.ChestPitch + chest, Hip = idle.HipRoll,
                    Head = idle.HeadPitch + look * 0.4f, Lean = look,
                };
            }
            return shot;
        }

        static Fig[] TrackCrouch(bool matched)
        {
            var shot = new Fig[8];
            float phase = 0.2f;
            float rate = matched ? BodyLife.CrouchCadence(BodyLife.CrouchSpeed) : 6.2f;
            for (int i = 0; i < 8; i++)
            {
                float s = (float)Math.Sin(phase);
                float step = s > 0f ? s : 0f;
                float other = s < 0f ? -s : 0f;
                shot[i] = new Fig
                {
                    ArmL = CrouchPose.ArmPitch, ArmR = CrouchPose.ArmPitch,
                    ElbL = CrouchPose.Elbow, ElbR = CrouchPose.Elbow,
                    ThL = CrouchPose.WalkThigh(step, other),
                    ThR = CrouchPose.WalkThigh(other, step),
                    KnL = CrouchPose.WalkKnee(step),
                    KnR = CrouchPose.WalkKnee(other),
                    Spine = CrouchPose.Spine, Hip = CrouchPose.Hip,
                    Head = CrouchPose.Head, Lean = 0f,
                };
                phase += rate * Dt;
            }
            return shot;
        }

        static Fig[] TrackSlide(bool life)
        {
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                float lean = 0f;
                float hand = 0f;
                float head = VerbPoseClips.SlideHead;
                if (life)
                {
                    BodyLife.SlideMotion(i * 0.45f, out lean, out hand, out float look);
                    head += look;
                }
                shot[i] = new Fig
                {
                    ArmL = VerbPoseClips.SlideLeadArmPitch,
                    ArmR = VerbPoseClips.SlideBalanceArmPitch + hand,
                    ElbL = VerbPoseClips.SlideLeadElbow,
                    ElbR = VerbPoseClips.SlideBalanceElbow,
                    ThL = VerbPoseClips.SlideLeadThigh,
                    ThR = VerbPoseClips.SlideTrailThigh,
                    KnL = VerbPoseClips.SlideLeadKnee,
                    KnR = VerbPoseClips.SlideTrailKnee,
                    Spine = VerbPoseClips.SlideSpine + lean,
                    Hip = VerbPoseClips.SlideHip,
                    Head = head,
                    Lean = lean,
                };
            }
            return shot;
        }

        static Fig[] TrackSlip(bool scrabble)
        {
            var shot = new Fig[8];
            float phase = 0f;
            float rate = WallPose.SlipRate(-WallPose.SlipSpeedRef);
            for (int i = 0; i < 8; i++)
            {
                WallPose.Sample sample = WallPose.Climb((float)Math.Sin(phase), -WallPose.SlipSpeedRef);
                if (scrabble)
                {
                    BodyLife.Scrabble(phase, 1f, out float hL, out float hR, out float fL, out float fR);
                    sample.ArmPitchL += hL;
                    sample.ArmPitchR += hR;
                    sample.FootL += fL;
                    sample.FootR += fR;
                }
                Fig fig = Fig.From(sample);
                fig.KnL += sample.FootL;
                fig.KnR += sample.FootR;
                shot[i] = fig;
                phase += rate * Dt;
            }
            return shot;
        }

        static Fig[] TrackWallMode(bool blend)
        {
            var shot = new Fig[8];
            WallPose.Sample climb = WallPose.Climb(0.8f, WallPose.ClimbSpeedRef);
            WallPose.Sample run = WallPose.RunCycle(1.2f, true);
            for (int i = 0; i < 8; i++)
            {
                float u = i / 7f;
                float t = blend ? PoseHandoff.Ease(u) : (i < 4 ? 0f : 1f);
                WallPose.Sample sample = WallPose.Mix(climb, run, t);
                if (blend)
                {
                    float plant = BodyLife.EntryPlant(u * BodyLife.EntryPlantSeconds);
                    sample.FootL += BodyLife.EntryFoot * plant;
                    sample.FootR += BodyLife.EntryFoot * plant;
                }
                Fig fig = Fig.From(sample);
                fig.KnL += sample.FootL;
                fig.KnR += sample.FootR;
                shot[i] = fig;
            }
            return shot;
        }

        static Fig[] TrackFlinch(bool layered)
        {
            var shot = new Fig[8];
            PunchStaggerPose.Sample pose = PunchStaggerPose.Stumble();
            for (int i = 0; i < 8; i++)
            {
                float age = (i + 1) * Dt;
                float w = PunchStaggerPose.Weight(age);
                float head = pose.Head * w;
                float spine = pose.Spine * w;
                if (layered)
                {
                    float flinch = BodyLife.FlinchWeight(age);
                    head += BodyLife.FlinchHead * flinch;
                    spine += BodyLife.FlinchSpine * flinch;
                }
                shot[i] = new Fig
                {
                    ArmL = pose.ArmPitchL * w, ArmR = pose.ArmPitchR * w,
                    ElbL = pose.ElbowL, ElbR = pose.ElbowR,
                    ThL = pose.ThighL * w, ThR = pose.ThighR * w,
                    KnL = pose.KneeL * w, KnR = pose.KneeR * w,
                    Spine = spine, Hip = pose.Hip * w, Head = head, Lean = 0f,
                };
            }
            return shot;
        }

        static Fig[] TrackStride(bool play, bool strafe, bool back)
        {
            var shot = new Fig[8];
            float phase = 0.4f;
            float speed = LocomotionPolish.SprintSpeed;
            float rate = play ? LocomotionPolish.PlayCadence(speed) : GaitBlend.CadenceAt(speed);
            for (int i = 0; i < 8; i++)
            {
                shot[i] = FigFromGait(phase, speed, strafe, back, false, false);
                phase += rate * Dt;
            }
            return shot;
        }

        static Fig[] TrackStrafe()
        {
            var shot = new Fig[8];
            float phase = 0.4f;
            float speed = LocomotionPolish.SprintSpeed;
            float rate = LocomotionPolish.PlayCadence(speed);
            float head = LocomotionPolish.HeadYaw(LocomotionPolish.TravelYaw(0f, speed, speed));
            for (int i = 0; i < 8; i++)
            {
                Fig fig = FigFromGait(phase, speed, true, false, false, false);
                fig.Head = head;
                fig.Spine = LocomotionPolish.SpineYaw(head);
                float follow = LocomotionPolish.ArmFollowTarget(fig.ArmL);
                fig.ArmL += follow;
                fig.ArmR += LocomotionPolish.ArmFollowTarget(fig.ArmR);
                shot[i] = fig;
                phase += rate * Dt;
            }
            return shot;
        }

        static Fig[] TrackAir(bool apex)
        {
            float[] vys = { 22f, 14f, 8f, 3f, 0f, -6f, -14f, -22f };
            var shot = new Fig[8];
            for (int i = 0; i < 8; i++)
            {
                JumpPose.Sample s = JumpPose.At(vys[i], i == 0 ? 0.02f : 0.2f, true);
                if (apex)
                {
                    bool chain = i >= 4;
                    LocomotionPolish.AirPhase(ref s.ThighL, ref s.ThighR, ref s.KneeL, ref s.KneeR, ref s.ArmPitchL, ref s.ArmPitchR, ref s.Spine, vys[i], chain);
                }
                shot[i] = new Fig
                {
                    ArmL = s.ArmPitchL, ArmR = s.ArmPitchR, ElbL = s.ElbowL, ElbR = s.ElbowR,
                    ThL = s.ThighL, ThR = s.ThighR, KnL = s.KneeL, KnR = s.KneeR,
                    Spine = s.Spine, Hip = s.Hip, Head = -6f, Lean = 0f,
                };
            }
            return shot;
        }

        static Fig[] TrackTurn()
        {
            Fig[] shot = TrackStride(true, false, false);
            for (int i = 0; i < shot.Length; i++)
            {
                Fig fig = shot[i];
                float w = i < 2 ? 0.35f : (i < 6 ? 1f : 0.4f);
                fig.ThL = fig.ThL + (LocomotionPolish.HardPlantThigh - fig.ThL) * w;
                fig.KnL = fig.KnL + (LocomotionPolish.HardPlantKnee - fig.KnL) * w;
                fig.Lean = 8f * w;
                shot[i] = fig;
            }
            return shot;
        }

        static Fig[] TrackSlope()
        {
            Fig[] shot = TrackStride(true, false, false);
            float pitch = LocomotionPolish.FootPitch(0.82f, 0.48f);
            for (int i = 0; i < shot.Length; i++)
            {
                Fig fig = shot[i];
                fig.KnL += pitch;
                fig.KnR += pitch;
                fig.Hip = pitch * 0.35f;
                shot[i] = fig;
            }
            return shot;
        }

        static Fig[] TrackHand(bool contact)
        {
            var shot = new Fig[8];
            float phase = 0f;
            float rate = WallPose.ClimbCadenceFull;
            float reach = contact ? LocomotionPolish.HandPitch(0.42f) : 0f;
            for (int i = 0; i < 8; i++)
            {
                float s = (float)Math.Sin(phase);
                WallPose.Sample sample = WallPose.Climb(s, WallPose.ClimbSpeedRef);
                Fig fig = Fig.From(sample);
                fig.ArmL += reach;
                fig.ArmR += reach;
                shot[i] = fig;
                phase += rate * Dt;
            }
            return shot;
        }

        static Fig FigFromGait(float phase, float speed, bool strafe, bool back, bool plant, bool slope)
        {
            GaitBlend.Legs legs = GaitBlend.At(phase, speed);
            float tl = legs.ThighL;
            float tr = legs.ThighR;
            float kl = legs.KneeL;
            float kr = legs.KneeR;
            if (strafe || back)
            {
                float fwd = back ? -speed : (strafe ? 0f : speed);
                float side = strafe ? speed : 0f;
                LocomotionPolish.Legs mixed = LocomotionPolish.FacingStride(tl, tr, kl, kr, fwd, side);
                tl = mixed.ThighL;
                tr = mixed.ThighR;
                kl = mixed.KneeL;
                kr = mixed.KneeR;
            }
            if (plant)
            {
                tl = LocomotionPolish.HardPlantThigh;
                kl = LocomotionPolish.HardPlantKnee;
            }
            if (slope)
            {
                float pitch = LocomotionPolish.FootPitch(0.82f, 0.48f);
                kl += pitch;
                kr += pitch;
            }
            float s = Mathf.Sin(phase);
            float amp = GaitBlend.ArmAmp(GaitBlend.PoseWeight(speed));
            return new Fig
            {
                ArmL = -s * amp * 0.55f,
                ArmR = s * amp * 0.55f,
                ElbL = -14f,
                ElbR = -14f,
                ThL = tl,
                ThR = tr,
                KnL = kl,
                KnR = kr,
                Spine = LocomotionPolish.StartLean(0.35f, 4f) * 0.25f,
                Hip = 0f,
                Head = 0f,
                Lean = 0f,
            };
        }

        static void PaintLoco(byte[] pix, int w, int h, int rows, int row, string title, string which, Fig[] figs, byte r, byte g, byte b, bool wall, float slope)
        {
            int cellH = h / rows;
            int cellW = (w - 168) / 8;
            int y0 = row * cellH;
            Text(pix, w, h, 8, y0 + 8, title, 230, 226, 214, 2);
            Text(pix, w, h, 8, y0 + 28, which, 230, 226, 214, 2);
            for (int i = 0; i < figs.Length && i < 8; i++)
            {
                int ox = 168 + i * cellW;
                if (wall)
                    VLine(pix, w, h, ox + cellW - 14, y0 + 28, y0 + cellH - 18, 64, 72, 82);
                if (slope > 0.5f || slope < -0.5f)
                {
                    float rad = slope * 0.0174533f;
                    float x0 = ox + 10;
                    float x1 = ox + cellW - 18;
                    float yb = y0 + cellH - 22;
                    float rise = (float)Math.Sin(rad) * (x1 - x0) * 0.35f;
                    Bone(pix, w, h, x0, yb, x1, yb - rise, 90, 84, 70);
                }
                else
                    HLine(pix, w, h, ox + 8, ox + cellW - 16, y0 + cellH - 16, 48, 52, 58);
                DrawFig(pix, w, h, ox + cellW / 2 - 6, y0 + 104, figs[i], r, g, b);
                Text(pix, w, h, ox + 6, y0 + cellH - 14, (i + 1).ToString(CultureInfo.InvariantCulture), 180, 176, 160, 1);
            }
        }

        static Fig[] TrackClimb(bool shaped, bool spring, float slew)
        {
            var vel = new float[12];
            Fig cur = Fig.From(RawClimb(0f));
            float rate = WallPose.ClimbCadenceFull;
            var shot = new Fig[8];
            int got = 0;
            float phase = 0f;
            for (int i = 0; i < 96 && got < 8; i++)
            {
                phase += rate * Dt;
                float s = (float)Math.Sin(phase);
                WallPose.Sample sample = shaped ? WallPose.Climb(s, WallPose.ClimbSpeedRef) : RawClimb(s);
                cur = Step(cur, Fig.From(sample), vel, spring, slew);
                if (i >= 48 && ((i - 48) % 6) == 0)
                    shot[got++] = cur;
            }
            return shot;
        }

        static Fig[] TrackRun(bool spring, float slew)
        {
            var vel = new float[12];
            Fig cur = Fig.From(WallPose.RunCycle(0f, true));
            float rate = WallPose.RunCadenceFull;
            var shot = new Fig[8];
            int got = 0;
            float phase = 0f;
            for (int i = 0; i < 80 && got < 8; i++)
            {
                phase += rate * Dt;
                cur = Step(cur, Fig.From(WallPose.RunCycle(phase, true)), vel, spring, slew);
                if (i >= 36 && ((i - 36) % 4) == 0)
                    shot[got++] = cur;
            }
            return shot;
        }

        static Fig[] TrackMantle(bool spring, float slew)
        {
            var vel = new float[12];
            Fig cur = new Fig { ArmL = -16f, ArmR = -12f, ElbL = -14f, ElbR = -12f, ThL = 8f, ThR = 6f, KnL = -10f, KnR = -8f };
            var shot = new Fig[8];
            int got = 0;
            const float dur = 0.40f;
            int total = (int)(dur / Dt);
            if (total < 8) total = 8;
            int step = total / 8;
            if (step < 1) step = 1;
            for (int i = 0; i < total && got < 8; i++)
            {
                float u = (i + 1) / (float)total;
                if (u > 1f) u = 1f;
                cur = Step(cur, Fig.FromMantle(MantlePose.At(u, true)), vel, spring, slew);
                if ((i % step) == step - 1)
                    shot[got++] = cur;
            }
            while (got < 8)
                shot[got++] = cur;
            return shot;
        }

        static WallPose.Sample RawClimb(float phaseSin)
        {
            float reachL = (phaseSin + 1f) * 0.5f;
            float reachR = 1f - reachL;
            return new WallPose.Sample
            {
                ThighL = Mathf.Lerp(WallPose.PlantThigh, WallPose.DriveThigh, reachR),
                ThighR = Mathf.Lerp(WallPose.PlantThigh, WallPose.DriveThigh, reachL),
                KneeL = Mathf.Lerp(WallPose.PlantKnee, WallPose.DriveKnee, reachR),
                KneeR = Mathf.Lerp(WallPose.PlantKnee, WallPose.DriveKnee, reachL),
                ArmPitchL = Mathf.Lerp(WallPose.PullPitch, WallPose.ClimbReachPitch, reachL),
                ArmPitchR = Mathf.Lerp(WallPose.PullPitch, WallPose.ClimbReachPitch, reachR),
                ArmYawL = Mathf.Lerp(WallPose.PullYaw, WallPose.ReachYaw, reachL),
                ArmYawR = -Mathf.Lerp(WallPose.PullYaw, WallPose.ReachYaw, reachR),
                ElbowL = Mathf.Lerp(WallPose.PullElbow, WallPose.ReachElbow, reachL),
                ElbowR = Mathf.Lerp(WallPose.PullElbow, WallPose.ReachElbow, reachR),
                Hip = WallPose.ClimbHip,
                Spine = WallPose.ClimbSpine,
                Head = WallPose.ClimbHead,
            };
        }

        static Fig Step(Fig cur, Fig goal, float[] vel, bool spring, float slew)
        {
            Fig n = cur;
            n.ArmL = StepF(cur.ArmL, goal.ArmL, ref vel[0], spring, slew);
            n.ArmR = StepF(cur.ArmR, goal.ArmR, ref vel[1], spring, slew);
            n.ElbL = StepF(cur.ElbL, goal.ElbL, ref vel[2], spring, slew);
            n.ElbR = StepF(cur.ElbR, goal.ElbR, ref vel[3], spring, slew);
            n.ThL = StepF(cur.ThL, goal.ThL, ref vel[4], spring, slew);
            n.ThR = StepF(cur.ThR, goal.ThR, ref vel[5], spring, slew);
            n.KnL = StepF(cur.KnL, goal.KnL, ref vel[6], spring, slew);
            n.KnR = StepF(cur.KnR, goal.KnR, ref vel[7], spring, slew);
            n.Spine = StepF(cur.Spine, goal.Spine, ref vel[8], spring, slew);
            n.Hip = StepF(cur.Hip, goal.Hip, ref vel[9], spring, slew);
            n.Head = StepF(cur.Head, goal.Head, ref vel[10], spring, slew);
            n.Lean = StepF(cur.Lean, goal.Lean, ref vel[11], spring, slew);
            return n;
        }

        static float StepF(float cur, float goal, ref float vel, bool spring, float slew)
        {
            float seconds = SecondsForSlew(slew);
            if (!spring || seconds <= 0f)
            {
                vel = 0f;
                float a = 1f - (float)Math.Exp(-slew * Dt);
                if (a < 0f) a = 0f;
                if (a > 1f) a = 1f;
                return cur + (goal - cur) * a;
            }
            return Smooth(cur, goal, ref vel, seconds, Dt);
        }

        static void PaintRow(byte[] pix, int w, int h, int row, string title, string which, Fig[] figs, byte r, byte g, byte b, bool wall)
        {
            int cellH = h / 6;
            int cellW = (w - 156) / 8;
            int y0 = row * cellH;
            Text(pix, w, h, 8, y0 + 8, title, 230, 226, 214, 2);
            Text(pix, w, h, 8, y0 + 26, which, 230, 226, 214, 2);
            for (int i = 0; i < figs.Length && i < 8; i++)
            {
                int ox = 156 + i * cellW;
                if (wall)
                    VLine(pix, w, h, ox + cellW - 16, y0 + 36, y0 + cellH - 22, 64, 72, 82);
                HLine(pix, w, h, ox + 8, ox + cellW - 22, y0 + cellH - 18, 48, 52, 58);
                DrawFig(pix, w, h, ox + cellW / 2 - 6, y0 + 112, figs[i], r, g, b);
                Text(pix, w, h, ox + 8, y0 + cellH - 16, (i + 1).ToString(CultureInfo.InvariantCulture), 180, 176, 160, 1);
            }
        }

        static void DrawFig(byte[] pix, int w, int h, int hx, int hy, Fig f, byte r, byte g, byte b)
        {
            float lean = f.Lean * 0.35f;
            float spine = f.Spine * 0.0174533f;
            float sx = hx + (float)Math.Sin(spine) * 46f + lean;
            float sy = hy - (float)Math.Cos(spine) * 46f;
            Bone(pix, w, h, hx, hy, sx, sy, r, g, b);
            float head = (f.Head * 0.25f) * 0.0174533f;
            float hx2 = sx + (float)Math.Sin(spine + head) * 16f;
            float hy2 = sy - (float)Math.Cos(spine + head) * 16f;
            Bone(pix, w, h, sx, sy, hx2, hy2, r, g, b);
            Dot(pix, w, h, (int)hx2, (int)hy2, 5, r, g, b);
            Limb(pix, w, h, sx, sy, f.ArmL, f.ElbL, 30f, 26f, true, r, g, b);
            Limb(pix, w, h, sx + 3f, sy, f.ArmR, f.ElbR, 30f, 26f, true, (byte)(r * 0.72f), (byte)(g * 0.72f), (byte)(b * 0.72f));
            Limb(pix, w, h, hx, hy, f.ThL, f.KnL, 34f, 32f, false, r, g, b);
            Limb(pix, w, h, hx + 3f, hy, f.ThR, f.KnR, 34f, 32f, false, (byte)(r * 0.72f), (byte)(g * 0.72f), (byte)(b * 0.72f));
            Dot(pix, w, h, hx, hy, 4, r, g, b);
        }

        static void Limb(byte[] pix, int w, int h, float x, float y, float pitch, float bend, float lenA, float lenB, bool arm, byte r, byte g, byte b)
        {
            float rad = pitch * 0.0174533f;
            float dx = arm ? -(float)Math.Sin(rad) : (float)Math.Sin(rad);
            float dy = (float)Math.Cos(rad);
            float x1 = x + dx * lenA;
            float y1 = y + dy * lenB * 0f + dy * lenA;
            Bone(pix, w, h, x, y, x1, y1, r, g, b);
            float rad2 = (pitch + bend) * 0.0174533f;
            float dx2 = arm ? -(float)Math.Sin(rad2) : (float)Math.Sin(rad2);
            float dy2 = (float)Math.Cos(rad2);
            float x2 = x1 + dx2 * lenB;
            float y2 = y1 + dy2 * lenB;
            Bone(pix, w, h, x1, y1, x2, y2, r, g, b);
            Dot(pix, w, h, (int)x1, (int)y1, 3, r, g, b);
        }

        static void Bone(byte[] pix, int w, int h, float x0, float y0, float x1, float y1, byte r, byte g, byte b)
        {
            int steps = 28;
            for (int i = 0; i <= steps; i++)
            {
                float u = i / (float)steps;
                int x = (int)(x0 + (x1 - x0) * u);
                int y = (int)(y0 + (y1 - y0) * u);
                Dot(pix, w, h, x, y, 2, r, g, b);
            }
        }

        static void Dot(byte[] pix, int w, int h, int x, int y, int rad, byte r, byte g, byte b)
        {
            for (int dy = -rad; dy <= rad; dy++)
            {
                for (int dx = -rad; dx <= rad; dx++)
                {
                    if (dx * dx + dy * dy > rad * rad + rad) continue;
                    PlotPx(pix, w, h, x + dx, y + dy, r, g, b);
                }
            }
        }

        static void HLine(byte[] pix, int w, int h, int x0, int x1, int y, byte r, byte g, byte b)
        {
            if (x1 < x0) { int t = x0; x0 = x1; x1 = t; }
            for (int x = x0; x <= x1; x++)
                PlotPx(pix, w, h, x, y, r, g, b);
        }

        static void VLine(byte[] pix, int w, int h, int x, int y0, int y1, byte r, byte g, byte b)
        {
            if (y1 < y0) { int t = y0; y0 = y1; y1 = t; }
            for (int y = y0; y <= y1; y++)
                PlotPx(pix, w, h, x, y, r, g, b);
        }

        static void PlotPx(byte[] pix, int w, int h, int x, int y, byte r, byte g, byte b)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
            int p = (y * w + x) * 3;
            pix[p] = r;
            pix[p + 1] = g;
            pix[p + 2] = b;
        }

        static void Text(byte[] pix, int w, int h, int x, int y, string text, byte r, byte g, byte b, int scale)
        {
            if (scale < 1) scale = 1;
            int px = 7 * scale;
            if (px < 12) px = 12;
            if (StillFont.Draw(pix, w, h, x, y, text, r, g, b, px) > 0)
                return;
            int cx = x;
            for (int i = 0; i < text.Length; i++)
            {
                Glyph(pix, w, h, cx, y, text[i], r, g, b, scale);
                cx += 6 * scale;
            }
        }

        static void Glyph(byte[] pix, int w, int h, int x, int y, char ch, byte r, byte g, byte b, int scale)
        {
            long bits = GlyphBits(ch);
            if (bits == 0) return;
            for (int row = 0; row < 7; row++)
            {
                int rowBits = (int)((bits >> ((6 - row) * 5)) & 31);
                for (int col = 0; col < 5; col++)
                {
                    if (((rowBits >> (4 - col)) & 1) == 0) continue;
                    for (int sy = 0; sy < scale; sy++)
                    {
                        for (int sx = 0; sx < scale; sx++)
                            PlotPx(pix, w, h, x + col * scale + sx, y + row * scale + sy, r, g, b);
                    }
                }
            }
        }

        static long GlyphBits(char ch)
        {
            switch (ch)
            {
                case 'A': return 0b01110100011000111111100011000110001L;
                case 'B': return 0b111101000111110100011000111110L;
                case 'C': return 0b01110100011000010000100001000101110L;
                case 'D': return 0b11110100011000110001100011000111110L;
                case 'E': return 0b111111000011110100001000011111L;
                case 'F': return 0b111111000011110100001000010000L;
                case 'G': return 0b01110100011000010111100011000101110L;
                case 'H': return 0b10001100011000111111100011000110001L;
                case 'I': return 0b01110001000010000100001000010001110L;
                case 'J': return 0b00111000010000100001000011000101110L;
                case 'K': return 0b10001100101010011000101001001010001L;
                case 'L': return 0b100001000010000100001000011111L;
                case 'M': return 0b10001110111010110001100011000110001L;
                case 'N': return 0b10001110011010110011100011000110001L;
                case 'O': return 0b01110100011000110001100011000101110L;
                case 'P': return 0b11110100011000111110100001000010000L;
                case 'Q': return 0b01110100011000110001101011001001101L;
                case 'R': return 0b111101000111110101011001010010L;
                case 'S': return 0b01111100001000001110000011000111110L;
                case 'T': return 0b11111001000010000100001000010000100L;
                case 'U': return 0b10001100011000110001100011000101110L;
                case 'V': return 0b10001100011000110001010100101000100L;
                case 'W': return 0b10001100011000110101101011010101010L;
                case 'X': return 0b10001100010101000100010101000110001L;
                case 'Y': return 0b10001100010101000100001000010000100L;
                case 'Z': return 0b11111000010001000100010001000011111L;
                case '0': return 0b01110100011001110101110011000101110L;
                case '1': return 0b00100011000010000100001000010001110L;
                case '2': return 0b011101000100001000100010001000011111L;
                case '3': return 0b01110100010000100110000011000101110L;
                case '4': return 0b00010001100101010001111110001000010L;
                case '5': return 0b11111100001111000001000011000101110L;
                case '6': return 0b01110100001000011110100011000101110L;
                case '7': return 0b11111000010001000010001000010000100L;
                case '8': return 0b011101000110001011100100011000101110L;
                case '9': return 0b01110100011000101111000011000101110L;
                case ' ': return 0L;
                default: return 0L;
            }
        }

        static void WritePng(string path, byte[] rgb, int w, int h)
        {
            int stride = w * 3;
            var raw = new byte[(stride + 1) * h];
            for (int y = 0; y < h; y++)
            {
                raw[y * (stride + 1)] = 0;
                Buffer.BlockCopy(rgb, y * stride, raw, y * (stride + 1) + 1, stride);
            }
            byte[] deflated;
            using (var ms = new MemoryStream())
            {
                using (var def = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Fastest, true))
                    def.Write(raw, 0, raw.Length);
                deflated = ms.ToArray();
            }
            uint a = 1;
            uint bsum = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                a = (a + raw[i]) % 65521;
                bsum = (bsum + a) % 65521;
            }
            uint adler = (bsum << 16) | a;
            var zlib = new byte[deflated.Length + 6];
            zlib[0] = 0x78;
            zlib[1] = 0x01;
            Buffer.BlockCopy(deflated, 0, zlib, 2, deflated.Length);
            zlib[zlib.Length - 4] = (byte)(adler >> 24);
            zlib[zlib.Length - 3] = (byte)(adler >> 16);
            zlib[zlib.Length - 2] = (byte)(adler >> 8);
            zlib[zlib.Length - 1] = (byte)adler;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
                fs.Write(sig, 0, sig.Length);
                var ihdr = new byte[13];
                Be(ihdr, 0, w);
                Be(ihdr, 4, h);
                ihdr[8] = 8;
                ihdr[9] = 2;
                Chunk(fs, "IHDR", ihdr);
                Chunk(fs, "IDAT", zlib);
                Chunk(fs, "IEND", new byte[0]);
            }
        }

        static void Be(byte[] buf, int at, int value)
        {
            uint u = (uint)value;
            buf[at] = (byte)(u >> 24);
            buf[at + 1] = (byte)(u >> 16);
            buf[at + 2] = (byte)(u >> 8);
            buf[at + 3] = (byte)u;
        }

        static void Be(byte[] buf, int at, uint value)
        {
            buf[at] = (byte)(value >> 24);
            buf[at + 1] = (byte)(value >> 16);
            buf[at + 2] = (byte)(value >> 8);
            buf[at + 3] = (byte)value;
        }

        static void Chunk(Stream fs, string name, byte[] data)
        {
            var len = new byte[4];
            Be(len, 0, data.Length);
            fs.Write(len, 0, 4);
            byte[] tag = Encoding.ASCII.GetBytes(name);
            fs.Write(tag, 0, 4);
            if (data.Length > 0) fs.Write(data, 0, data.Length);
            uint crc = 0xffffffff;
            for (int i = 0; i < tag.Length; i++) crc = Crc(crc, tag[i]);
            for (int i = 0; i < data.Length; i++) crc = Crc(crc, data[i]);
            crc ^= 0xffffffff;
            var c = new byte[4];
            Be(c, 0, crc);
            fs.Write(c, 0, 4);
        }

        static uint Crc(uint crc, byte value)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320 : crc >> 1;
            return crc;
        }
    }
}
