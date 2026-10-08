using System.Globalization;
using System.Text;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Every presentation pair the motor can actually enter, and the one-frame
    /// bone and visual-root step that pair produces. A gait-sized step stays on
    /// the exponential. A real pose gap uses a wider spring so the mesh stays
    /// inside the pass-1 limits. The capsule and the verb clocks are not written.
    /// </summary>
    public static class TransitionMatrix
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;
        public const float PoseLimit = 22f;
        public const float RootLimit = 0.09f;
        /// <summary>Exponential steps at or under this stay on the gait slew. A stride step is about 31°.</summary>
        public const float GaitAngle = 33f;
        public const float WideSettle = 0.12f;
        public const float WideReach = 0.16f;
        public const float WideRoot = 0.18f;
        /// <summary>Pops at or under this keep <see cref="SmoothMotion.PositionSeconds"/>.</summary>
        public const float RootGate = 0.74f;

        public const int Count = 16;

        /// <summary>
        /// Spring time for a bone that is <paramref name="angle"/> degrees off its target.
        /// 0 keeps the exponential. A stride step does. A verb-sized gap does not.
        /// </summary>
        public static float BoneSeconds(float angle, float slew)
        {
            float gap = angle < 0f ? -angle : angle;
            float seconds = SmoothMotion.SecondsForSlew(slew);
            if (seconds <= 0f)
            {
                if (gap <= GaitAngle) return 0f;
                return gap <= 180f ? WideSettle : WideReach;
            }
            if (gap <= 110f)
                return seconds;
            float wide = gap <= 180f ? WideSettle : WideReach;
            return seconds > wide ? seconds : wide;
        }

        /// <summary>
        /// Spring time for a visual lag of <paramref name="popMeters"/>. A respawn is unchanged.
        /// The 0.71 m mantle exit stays on the short spring. A larger absorbed pop uses the wide one.
        /// </summary>
        public static float RootSeconds(float popMeters)
        {
            float mag = popMeters < 0f ? -popMeters : popMeters;
            if (mag >= SmoothMotion.PopIgnore) return SmoothMotion.PositionSeconds;
            if (mag > RootGate) return WideRoot;
            return SmoothMotion.PositionSeconds;
        }

        public struct Pair
        {
            public int From;
            public int To;
            public bool Handoff;
            public float PoseBefore;
            public float PoseAfter;
            public float RootBefore;
            public float RootAfter;
        }

        public static string Name(int state)
        {
            switch (state)
            {
                case 0: return "ground";
                case 1: return "air";
                case 2: return "cling";
                case 3: return "climb";
                case 4: return "wallrun";
                case 5: return "slide";
                case 6: return "crouch";
                case 7: return "dash";
                case 8: return "lunge";
                case 9: return "punch";
                case 10: return "vault";
                case 11: return "mantle";
                case 12: return "zip";
                case 13: return "pad";
                case 14: return "grapple";
                default: return "stagger";
            }
        }

        public static Pair[] Run()
        {
            Bones[][] poses = Samples();
            float[] slew = Slews();
            int[] edges = Edges();
            int n = edges.Length / 2;
            var pairs = new Pair[n];
            for (int i = 0; i < n; i++)
            {
                int from = edges[i * 2];
                int to = edges[i * 2 + 1];
                float gap = WorstGap(poses[from], poses[to]);
                float pop = RootPop(from, to);
                pairs[i] = new Pair
                {
                    From = from,
                    To = to,
                    Handoff = IsHandoff(from, to),
                    PoseBefore = gap,
                    PoseAfter = MaxClose(gap, slew[to]),
                    RootBefore = pop,
                    RootAfter = MaxRoot(pop),
                };
            }
            return pairs;
        }

        public static bool Holds()
        {
            if (RootMotion || GameplayDelay != 0f) return false;
            if (BoneSeconds(GaitAngle, SmoothMotion.CycleSlew) != 0f) return false;
            if (Mathf.Abs(BoneSeconds(40f, SmoothMotion.CycleSlew) - WideSettle) > 0.001f) return false;
            if (Mathf.Abs(BoneSeconds(110f, 2400f) - SmoothMotion.ResponsiveSeconds) > 0.001f) return false;
            if (Mathf.Abs(BoneSeconds(240f, 1600f) - WideReach) > 0.001f) return false;
            if (Mathf.Abs(BoneSeconds(140f, 2400f) - WideSettle) > 0.001f) return false;
            if (Mathf.Abs(BoneSeconds(200f, SmoothMotion.CycleSlew) - WideReach) > 0.001f) return false;
            if (MaxClose(240f, 1600f) > PoseLimit) return false;
            if (MaxClose(110f, 2400f) > PoseLimit) return false;
            if (Mathf.Abs(RootSeconds(0.71f) - SmoothMotion.PositionSeconds) > 0.001f) return false;
            if (Mathf.Abs(RootSeconds(1.20f) - WideRoot) > 0.001f) return false;
            if (MaxRoot(0.71f) > RootLimit) return false;
            if (MaxRoot(1.20f) > RootLimit) return false;
            Pair[] pairs = Run();
            if (pairs == null || pairs.Length < 40) return false;
            for (int i = 0; i < pairs.Length; i++)
            {
                if (pairs[i].PoseAfter > PoseLimit) return false;
                if (pairs[i].RootAfter > RootLimit) return false;
                if (pairs[i].From == pairs[i].To) return false;
            }
            return true;
        }

        public static string ProofLine()
        {
            Pair[] pairs = Run();
            int worstP = 0;
            int worstR = 0;
            int over = 0;
            for (int i = 0; i < pairs.Length; i++)
            {
                if (pairs[i].PoseBefore > pairs[worstP].PoseBefore) worstP = i;
                if (pairs[i].RootBefore > pairs[worstR].RootBefore) worstR = i;
                if (pairs[i].PoseAfter > PoseLimit || pairs[i].RootAfter > RootLimit) over++;
            }
            CultureInfo c = CultureInfo.InvariantCulture;
            return "transition-matrix"
                + " pairs=" + pairs.Length.ToString(c)
                + " pose=" + Label(pairs[worstP])
                + " " + pairs[worstP].PoseBefore.ToString("0.0", c) + ">" + pairs[worstP].PoseAfter.ToString("0.0", c)
                + " root=" + Label(pairs[worstR])
                + " " + pairs[worstR].RootBefore.ToString("0.000", c) + ">" + pairs[worstR].RootAfter.ToString("0.000", c)
                + " over=" + over.ToString(c)
                + " gameplayDelay=0 rootMotion=0";
        }

        /// <summary>The ten pairs with the largest raw pose gap, after the wide spring.</summary>
        public static string TopLine()
        {
            Pair[] pairs = Run();
            var order = new int[pairs.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = 1; i < order.Length; i++)
            {
                int key = order[i];
                int j = i - 1;
                while (j >= 0 && pairs[order[j]].PoseBefore < pairs[key].PoseBefore)
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = key;
            }
            CultureInfo c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append("transition-top");
            int n = order.Length < 10 ? order.Length : 10;
            for (int i = 0; i < n; i++)
            {
                Pair p = pairs[order[i]];
                sb.Append(' ');
                sb.Append(i + 1);
                sb.Append('=');
                sb.Append(Label(p));
                sb.Append(' ');
                sb.Append(p.PoseBefore.ToString("0.0", c));
                sb.Append('>');
                sb.Append(p.PoseAfter.ToString("0.0", c));
                sb.Append(" root ");
                sb.Append(p.RootBefore.ToString("0.000", c));
                sb.Append('>');
                sb.Append(p.RootAfter.ToString("0.000", c));
            }
            return sb.ToString();
        }

        public static string Label(Pair p)
        {
            string mark = p.Handoff ? "*" : "";
            return Name(p.From) + ">" + Name(p.To) + mark;
        }

        /// <summary>Peak one-frame change while a gap closes. The unsprung step is the whole gap.</summary>
        public static float MaxClose(float degrees, float slew)
        {
            float gap = degrees < 0f ? -degrees : degrees;
            float seconds = BoneSeconds(gap, slew);
            if (seconds <= 0f)
            {
                float a = 1f - (float)System.Math.Exp(-slew * SmoothMotion.Dt);
                if (a < 0f) a = 0f;
                if (a > 1f) a = 1f;
                return gap * a;
            }
            float cur = 0f;
            float vel = 0f;
            float prev = 0f;
            float max = 0f;
            for (int i = 0; i < 48; i++)
            {
                float next = SmoothMotion.Smooth(cur, gap, ref vel, seconds, SmoothMotion.Dt);
                float d = next - prev;
                if (d < 0f) d = -d;
                if (d > max) max = d;
                prev = next;
                cur = next;
            }
            return max;
        }

        public static float MaxRoot(float pop)
        {
            float mag = pop < 0f ? -pop : pop;
            if (mag >= SmoothMotion.PopIgnore) return mag;
            if (mag <= 0.0001f) return 0f;
            float seconds = RootSeconds(mag);
            float lag = -mag;
            float vel = 0f;
            float prev = 0f;
            float max = 0f;
            for (int i = 0; i < 48; i++)
            {
                float shown = mag + lag;
                float d = shown - prev;
                if (d < 0f) d = -d;
                if (d > max) max = d;
                prev = shown;
                lag = SmoothMotion.Smooth(lag, 0f, ref vel, seconds, SmoothMotion.Dt);
            }
            return max;
        }

        static bool IsHandoff(int from, int to)
        {
            if (from == 3 && to == 4) return true;
            if (from == 4 && to == 3) return true;
            if (from == 10 && to == 11) return true;
            return false;
        }

        /// <summary>The vault arc ends on the stand point, so the exit write is not a pop.</summary>
        static float RootPop(int from, int to)
        {
            if (to == 0 && (from == 10 || from == 11)) return 0f;
            return 0f;
        }

        static float[] Slews()
        {
            return new float[]
            {
                SmoothMotion.CycleSlew,
                JumpPose.TakeoffSlew,
                WallPose.BlendSlew,
                SmoothMotion.CycleSlew,
                SmoothMotion.CycleSlew,
                CrouchPose.SlideHandoffSlew,
                CrouchPose.Slew,
                AirDashPose.Slew,
                LungePose.Slew,
                2400f,
                MantlePose.Slew,
                MantlePose.Slew,
                SmoothMotion.CycleSlew,
                SmoothMotion.CycleSlew,
                GrapplePose.LatchSlew,
                PunchStaggerPose.Slew,
            };
        }

        /// <summary>Motor edges, plus the climb/wall-run and vault/mantle pose handoffs.</summary>
        static int[] Edges()
        {
            return new int[]
            {
                0, 1, 1, 0,
                0, 5, 5, 0, 5, 1,
                0, 6, 6, 0, 1, 6, 6, 1, 6, 5, 5, 6,
                0, 3, 1, 3, 3, 1, 3, 0, 6, 3,
                1, 2, 2, 1, 0, 2, 2, 3, 3, 2, 2, 0, 6, 2,
                1, 4, 4, 1, 4, 0,
                3, 4, 4, 3,
                0, 10, 1, 10, 10, 11, 11, 0, 10, 0,
                1, 7, 7, 1, 7, 3, 7, 4,
                0, 8, 8, 0,
                0, 9, 9, 0, 1, 9, 9, 1,
                0, 12, 1, 12, 12, 1,
                0, 13, 13, 1,
                0, 14, 14, 0, 1, 14, 14, 1,
                0, 15, 15, 0, 1, 15, 15, 1, 9, 15,
            };
        }

        struct Bones
        {
            public float ArmL, ArmR, ThighL, ThighR, Spine, Head;
        }

        static float WorstGap(Bones[] a, Bones[] b)
        {
            float max = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                for (int j = 0; j < b.Length; j++)
                {
                    float d = Delta(a[i], b[j]);
                    if (d > max) max = d;
                }
            }
            return max;
        }

        static float Delta(Bones a, Bones b)
        {
            float m = Abs(a.ArmL - b.ArmL);
            m = Bigger(m, Abs(a.ArmR - b.ArmR));
            m = Bigger(m, Abs(a.ThighL - b.ThighL));
            m = Bigger(m, Abs(a.ThighR - b.ThighR));
            m = Bigger(m, Abs(a.Spine - b.Spine));
            m = Bigger(m, Abs(a.Head - b.Head));
            return m;
        }

        static float Abs(float v) => v < 0f ? -v : v;
        static float Bigger(float a, float b) => a > b ? a : b;

        static Bones[][] Samples()
        {
            var all = new Bones[Count][];
            all[0] = Ground();
            all[1] = Air();
            all[2] = new Bones[] { Wall(WallPose.Hold()) };
            all[3] = new Bones[]
            {
                Wall(WallPose.Climb(1f, WallPose.ClimbSpeedRef)),
                Wall(WallPose.Climb(-1f, WallPose.ClimbSpeedRef)),
                Wall(WallPose.Climb(0.4f, -WallPose.SlipSpeedRef)),
            };
            all[4] = new Bones[]
            {
                Wall(WallPose.RunCycle(0.4f, true)),
                Wall(WallPose.RunCycle(1.7f, true)),
                Wall(WallPose.RunCycle(3.1f, false)),
            };
            all[5] = new Bones[] { Slide() };
            all[6] = Crouch();
            all[7] = new Bones[] { Dash(AirDashPose.At(0f, 1f)), Dash(AirDashPose.At(1f, 0f)) };
            all[8] = new Bones[] { Lunge(LungePose.Burst()), Lunge(LungePose.Telegraph()) };
            all[9] = new Bones[] { Punch() };
            all[10] = new Bones[] { Mantle(MantlePose.At(0.08f, true)), Mantle(MantlePose.At(0.32f, true)) };
            all[11] = new Bones[] { Mantle(MantlePose.At(0.72f, true)), Mantle(MantlePose.At(1f, true)) };
            all[12] = new Bones[] { Zip(ZipPose.Hang()), Zip(ZipPose.JumpDrop()) };
            all[13] = new Bones[] { Launch(LaunchPose.At(24.7f)), Launch(LaunchPose.At(0f)), Launch(LaunchPose.At(-16f)) };
            all[14] = new Bones[] { Grapple(GrapplePose.Latch(0f)) };
            all[15] = new Bones[] { Stagger(PunchStaggerPose.Stumble()) };
            return all;
        }

        static Bones[] Ground()
        {
            float speed = GaitBlend.SprintSpeed;
            return new Bones[]
            {
                Jump(JumpPose.Stride(speed, 1f, 1.5707963f)),
                Jump(JumpPose.Stride(speed, -1f, 4.712389f)),
                Jump(JumpPose.Stride(speed, 0f, 0f)),
            };
        }

        static Bones[] Air()
        {
            return new Bones[]
            {
                Jump(JumpPose.At(24.7f, 0.02f, true)),
                Jump(JumpPose.At(0f, 0.2f, true)),
                Jump(JumpPose.At(-20f, 0.3f, true)),
            };
        }

        static Bones[] Crouch()
        {
            return new Bones[]
            {
                new Bones
                {
                    ArmL = CrouchPose.ArmPitch, ArmR = CrouchPose.ArmPitch,
                    ThighL = CrouchPose.Thigh, ThighR = CrouchPose.Thigh,
                    Spine = CrouchPose.Spine, Head = CrouchPose.Head,
                },
                new Bones
                {
                    ArmL = CrouchPose.ArmPitch, ArmR = CrouchPose.ArmPitch,
                    ThighL = CrouchPose.WalkThigh(1f, 0f), ThighR = CrouchPose.WalkThigh(0f, 1f),
                    Spine = CrouchPose.Spine, Head = CrouchPose.Head,
                },
            };
        }

        static Bones Slide()
        {
            return new Bones
            {
                ArmL = VerbPoseClips.SlideLeadArmPitch,
                ArmR = VerbPoseClips.SlideBalanceArmPitch,
                ThighL = VerbPoseClips.SlideLeadThigh,
                ThighR = VerbPoseClips.SlideTrailThigh,
                Spine = VerbPoseClips.SlideSpine,
                Head = VerbPoseClips.SlideHead,
            };
        }

        static Bones Punch()
        {
            return new Bones
            {
                ArmL = VerbPoseClips.PunchGuardPitchStrike,
                ArmR = VerbPoseClips.PunchStrikePitch,
                ThighL = VerbPoseClips.PunchRecoverLeadThigh,
                ThighR = VerbPoseClips.PunchRecoverTrailThigh,
                Spine = 18f,
                Head = -6f,
            };
        }

        static Bones Jump(JumpPose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = -6f,
            };
        }

        static Bones Wall(WallPose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = s.Head,
            };
        }

        static Bones Dash(AirDashPose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = s.Head,
            };
        }

        static Bones Lunge(LungePose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = s.Head,
            };
        }

        static Bones Mantle(MantlePose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = s.Head,
            };
        }

        static Bones Zip(ZipPose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = s.Head,
            };
        }

        static Bones Launch(LaunchPose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = s.Head,
            };
        }

        static Bones Grapple(GrapplePose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = s.Head,
            };
        }

        static Bones Stagger(PunchStaggerPose.Sample s)
        {
            return new Bones
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ThighL = s.ThighL, ThighR = s.ThighR,
                Spine = s.Spine, Head = s.Head,
            };
        }
    }
}
