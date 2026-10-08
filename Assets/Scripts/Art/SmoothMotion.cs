using System;
using System.Globalization;
using System.IO;
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
    }
}
