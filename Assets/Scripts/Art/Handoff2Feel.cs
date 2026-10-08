using System.Globalization;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual handoffs for the second set. Climb into the vault, the vault
    /// itself, the slide, the zip, the grapple, and the launch pad.
    /// Nothing here writes a Move, a timer the motor reads, or the root.
    /// </summary>
    public static class Handoff2Feel
    {
        public const bool RootMotion = false;
        public const float GameplayDelay = 0f;
        public const float Dt = 1f / 60f;
        public const float MantleWindow = 0.40f;

        public const float ClimbSeconds = 0.18f;
        public const float VaultOutSeconds = 0.18f;
        public const float ZipGrabSeconds = 0.28f;
        public const float ZipDropSeconds = 0.36f;
        public const float PadSeconds = 0.34f;
        public const float PadVyStep = 0.45f;
        public const float LatchSeconds = 0.18f;
        public const float ReleaseSeconds = 0.24f;
        /// <summary>Longer than the 0.22s head drop, so the trail knee stays under 15 degrees.</summary>
        public const float SlideSeconds = 0.26f;

        /// <summary>0 at the run, 1 in the wedge. The head drop stays on <see cref="LocoFeel.SlideWeight"/>.</summary>
        public static float SlideOpen(float age, bool entering)
        {
            float u = SlideSeconds > 0.0001f ? age / SlideSeconds : 1f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float e = Raised(u);
            return entering ? e : 1f - e;
        }

        public struct Shot
        {
            public float ArmL, ArmR, ElbL, ElbR, ThL, ThR, KnL, KnR, Spine, Hip, Head, Lean;
        }

        /// <summary>0 on the climb, 1 on the vault. Raised, so the first step is small.</summary>
        public static float ClimbOpen(float u)
        {
            return Raised(u);
        }

        /// <summary>
        /// Motor mantle fraction to the fraction <see cref="MantlePose.At"/> should see.
        /// Each beat stays on its motor time. The ease inside the beat becomes linear.
        /// </summary>
        public static float VaultShown(float motorU)
        {
            float u = motorU < 0f ? 0f : (motorU > 1f ? 1f : motorU);
            float a;
            float b;
            if (u < MantlePose.PlantEnd)
            {
                a = 0f;
                b = MantlePose.PlantEnd;
            }
            else if (u < MantlePose.KneeEnd)
            {
                a = MantlePose.PlantEnd;
                b = MantlePose.KneeEnd;
            }
            else
            {
                a = MantlePose.KneeEnd;
                b = 1f;
            }
            float span = b - a;
            float m = span > 0.0001f ? (u - a) / span : 1f;
            return a + span * InvEase(m);
        }

        /// <summary>0 on the vault land, 1 on the gait.</summary>
        public static float VaultOutOpen(float u)
        {
            return Raised(u);
        }

        /// <summary>0 in the air, 1 on the cable. Raised across a longer catch.</summary>
        public static float ZipGrab(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= ZipGrabSeconds) return 1f;
            return Raised(age / ZipGrabSeconds);
        }

        /// <summary>0 on the cable, 1 in the air. Linear, so the let-go does not accent.</summary>
        public static float ZipDrop(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= ZipDropSeconds) return 1f;
            return age / ZipDropSeconds;
        }

        /// <summary>0 at the walk, 1 in the swing. Raised, so the pad does not start at full speed.</summary>
        public static float PadOpen(float age)
        {
            if (age <= 0f) return 0f;
            if (age >= PadSeconds) return 1f;
            return Raised(age / PadSeconds);
        }

        /// <summary>0 as the latch starts, 1 once the hands have arrived.</summary>
        public static float LatchOpen(float u)
        {
            return Raised(u);
        }

        /// <summary>0 on the pull, 1 in the fall.</summary>
        public static float ReleaseOpen(float u)
        {
            return Raised(u);
        }

        /// <summary>
        /// Age to pass into <see cref="GrapplePose.Release"/> so its ease matches <see cref="ReleaseOpen"/>.
        /// </summary>
        public static float ReleaseShown(float u)
        {
            return GrapplePose.ReleaseBlendSeconds * InvEase(ReleaseOpen(u));
        }

        public static Shot ShotAt(int kind, int frame, bool after)
        {
            float u = frame / 7f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            if (kind == 0) return ClimbShot(u, after);
            if (kind == 1) return VaultShot(u, after);
            if (kind == 2) return VaultOutShot(u, after);
            if (kind == 3) return SlideShot(u, after, true);
            if (kind == 4) return SlideShot(u, after, false);
            if (kind == 5) return ZipGrabShot(u, after);
            if (kind == 6) return ZipDropShot(u, after);
            if (kind == 7) return GrappleInShot(u, after);
            if (kind == 8) return GrappleOutShot(u, after);
            if (kind == 9) return PadUpShot(u, after);
            return PadAirShot(u, after);
        }

        public static string ProofLine()
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            return "handoff2"
                + " climb=" + Pair(ClimbStep(false), ClimbStep(true), c)
                + " vault=" + Pair(VaultStep(false), VaultStep(true), c)
                + " vaultOut=" + Pair(VaultOutStep(false), VaultOutStep(true), c)
                + " slideIn=" + Pair(SlideStep(false), SlideStep(true), c)
                + " slideOut=" + Pair(SlideStep(false), SlideStep(true), c)
                + " zipGrab=" + Pair(ZipGrabStep(false), ZipGrabStep(true), c)
                + " zipDrop=" + Pair(ZipDropStep(false), ZipDropStep(true), c)
                + " grappleIn=" + Pair(GrappleInStep(false), GrappleInStep(true), c)
                + " grappleOut=" + Pair(GrappleOutStep(false), GrappleOutStep(true), c)
                + " padUp=" + Pair(PadUpStep(false), PadUpStep(true), c)
                + " padAir=" + Pair(PadAirStep(false), PadAirStep(true), c)
                + " gameplayDelay=0 rootMotion=0";
        }

        public static bool Holds()
        {
            bool motion = RootMotion;
            float delay = GameplayDelay;
            if (motion || delay != 0f) return false;
            if (Mathf.Abs(VaultShown(0f)) > 0.0001f) return false;
            if (Mathf.Abs(VaultShown(MantlePose.PlantEnd) - MantlePose.PlantEnd) > 0.0001f) return false;
            if (Mathf.Abs(VaultShown(MantlePose.KneeEnd) - MantlePose.KneeEnd) > 0.0001f) return false;
            if (Mathf.Abs(VaultShown(1f) - 1f) > 0.0001f) return false;
            if (ZipGrab(0f) > 0.0001f || Mathf.Abs(ZipGrab(ZipGrabSeconds) - 1f) > 0.0001f) return false;
            if (ZipDrop(0f) > 0.0001f || Mathf.Abs(ZipDrop(ZipDropSeconds) - 1f) > 0.0001f) return false;
            if (PadOpen(0f) > 0.0001f || Mathf.Abs(PadOpen(PadSeconds) - 1f) > 0.0001f) return false;
            if (ReleaseShown(0f) > 0.0001f) return false;
            if (Mathf.Abs(ReleaseShown(1f) - GrapplePose.ReleaseBlendSeconds) > 0.0001f) return false;
            if (ClimbOpen(0f) > 0.0001f || Mathf.Abs(ClimbOpen(1f) - 1f) > 0.0001f) return false;
            if (!Under(ClimbStep(false), ClimbStep(true))) return false;
            if (!Under(VaultStep(false), VaultStep(true))) return false;
            if (!Under(VaultOutStep(false), VaultOutStep(true))) return false;
            if (!Under(SlideStep(false), SlideStep(true))) return false;
            if (!Under(ZipGrabStep(false), ZipGrabStep(true))) return false;
            if (!Under(ZipDropStep(false), ZipDropStep(true))) return false;
            if (!Under(GrappleInStep(false), GrappleInStep(true))) return false;
            if (!Under(GrappleOutStep(false), GrappleOutStep(true))) return false;
            if (!Under(PadUpStep(false), PadUpStep(true))) return false;
            if (!Under(PadAirStep(false), PadAirStep(true))) return false;
            return true;
        }

        static bool Under(float before, float after)
        {
            if (after >= 22f || after <= 0.05f) return false;
            if (before > 15f && after >= before) return false;
            return true;
        }

        struct Bones
        {
            public float ArmL, ArmR, ElbL, ElbR, ThL, ThR, KnL, KnR, Spine, Hip, Head;

            public static Bones Wall(WallPose.Sample s)
            {
                return new Bones
                {
                    ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                    ElbL = s.ElbowL, ElbR = s.ElbowR,
                    ThL = s.ThighL, ThR = s.ThighR,
                    KnL = s.KneeL, KnR = s.KneeR,
                    Spine = s.Spine, Hip = s.Hip, Head = s.Head,
                };
            }

            public static Bones Mantle(MantlePose.Sample s)
            {
                return new Bones
                {
                    ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                    ElbL = s.ElbowL, ElbR = s.ElbowR,
                    ThL = s.ThighL, ThR = s.ThighR,
                    KnL = s.KneeL, KnR = s.KneeR,
                    Spine = s.Spine, Hip = s.Hip, Head = s.Head,
                };
            }

            public static Bones Lerp(Bones a, Bones b, float t)
            {
                return new Bones
                {
                    ArmL = a.ArmL + (b.ArmL - a.ArmL) * t,
                    ArmR = a.ArmR + (b.ArmR - a.ArmR) * t,
                    ElbL = a.ElbL + (b.ElbL - a.ElbL) * t,
                    ElbR = a.ElbR + (b.ElbR - a.ElbR) * t,
                    ThL = a.ThL + (b.ThL - a.ThL) * t,
                    ThR = a.ThR + (b.ThR - a.ThR) * t,
                    KnL = a.KnL + (b.KnL - a.KnL) * t,
                    KnR = a.KnR + (b.KnR - a.KnR) * t,
                    Spine = a.Spine + (b.Spine - a.Spine) * t,
                    Hip = a.Hip + (b.Hip - a.Hip) * t,
                    Head = a.Head + (b.Head - a.Head) * t,
                };
            }

            public static float Gap(Bones a, Bones b)
            {
                float m = Abs(a.ArmL - b.ArmL);
                m = Max(m, Abs(a.ArmR - b.ArmR));
                m = Max(m, Abs(a.ElbL - b.ElbL));
                m = Max(m, Abs(a.ElbR - b.ElbR));
                m = Max(m, Abs(a.ThL - b.ThL));
                m = Max(m, Abs(a.ThR - b.ThR));
                m = Max(m, Abs(a.KnL - b.KnL));
                m = Max(m, Abs(a.KnR - b.KnR));
                m = Max(m, Abs(a.Spine - b.Spine));
                m = Max(m, Abs(a.Hip - b.Hip));
                m = Max(m, Abs(a.Head - b.Head));
                return m;
            }
        }

        static float ClimbStep(bool eased)
        {
            float peak = 0f;
            peak = ClimbBoth(1f, eased, peak);
            peak = ClimbBoth(0f, eased, peak);
            peak = ClimbBoth(-1f, eased, peak);
            return peak;
        }

        static float ClimbBoth(float phase, bool eased, float peak)
        {
            Bones from = Bones.Wall(WallPose.Climb(phase, WallPose.ClimbSpeedRef));
            peak = Max(peak, ClimbRun(from, true, eased));
            return Max(peak, ClimbRun(from, false, eased));
        }

        static float ClimbRun(Bones from, bool leadLeft, bool eased)
        {
            float seconds = eased ? ClimbSeconds : MantlePose.EnterBlendSeconds;
            Bones prev = from;
            float peak = 0f;
            float motor = 0f;
            float snap = 0f;
            int n = (int)(0.55f / Dt) + 2;
            for (int i = 0; i < n; i++)
            {
                motor += Dt / MantleWindow;
                if (motor > 1f) motor = 1f;
                snap += Dt / seconds;
                if (snap > 1f) snap = 1f;
                float w = eased ? ClimbOpen(snap) : PoseHandoff.Ease(snap);
                float shown = eased ? VaultShown(motor) : motor;
                Bones vault = Bones.Mantle(MantlePose.At(shown, leadLeft));
                Bones now = Bones.Lerp(from, vault, w);
                peak = Max(peak, Bones.Gap(prev, now));
                prev = now;
                if (snap >= 1f && motor >= MantlePose.KneeEnd) break;
            }
            return peak;
        }

        static float VaultStep(bool eased)
        {
            float peak = 0f;
            for (int lead = 0; lead < 2; lead++)
            {
                bool left = lead == 0;
                Bones prev = Bones.Mantle(MantlePose.At(0f, left));
                int n = (int)(MantleWindow / Dt) + 1;
                for (int i = 1; i <= n; i++)
                {
                    float motor = i * Dt / MantleWindow;
                    if (motor > 1f) motor = 1f;
                    float shown = eased ? VaultShown(motor) : motor;
                    Bones now = Bones.Mantle(MantlePose.At(shown, left));
                    peak = Max(peak, Bones.Gap(prev, now));
                    prev = now;
                    if (motor >= 1f) break;
                }
            }
            return peak;
        }

        static float VaultOutStep(bool eased)
        {
            float span = VaultOutSpan();
            float seconds = eased ? VaultOutSeconds : MantlePose.ExitBlendSeconds;
            return WeightPeak(span, seconds, eased ? 1 : 0);
        }

        static float VaultOutSpan()
        {
            float span = 0f;
            for (int lead = 0; lead < 2; lead++)
            {
                Bones land = Bones.Mantle(MantlePose.At(1f, lead == 0));
                for (int i = 0; i <= 16; i++)
                {
                    float phase = i / 16f * 6.2831853f;
                    GaitBlend.Legs legs = GaitBlend.At(phase, 13.8f);
                    span = Max(span, Abs(land.ThL - legs.ThighL));
                    span = Max(span, Abs(land.ThR - legs.ThighR));
                    span = Max(span, Abs(land.KnL - legs.KneeL));
                    span = Max(span, Abs(land.KnR - legs.KneeR));
                }
                float amp = GaitBlend.ArmAmp(GaitBlend.PoseWeight(13.8f));
                span = Max(span, Abs(land.ArmL - (-amp)));
                span = Max(span, Abs(land.ArmR - amp * 0.22f));
            }
            return span;
        }

        static float SlideStep(bool eased)
        {
            float span = 0f;
            for (int i = 0; i <= 24; i++)
            {
                float phase = i / 24f * 6.2831853f;
                GaitBlend.Legs legs = GaitBlend.At(phase, 13.8f);
                span = Max(span, Abs(VerbPoseClips.SlideTrailKnee - legs.KneeL));
                span = Max(span, Abs(VerbPoseClips.SlideTrailKnee - legs.KneeR));
                span = Max(span, Abs(VerbPoseClips.SlideLeadKnee - legs.KneeL));
                span = Max(span, Abs(VerbPoseClips.SlideLeadKnee - legs.KneeR));
                span = Max(span, Abs(VerbPoseClips.SlideTrailThigh - legs.ThighL));
                span = Max(span, Abs(VerbPoseClips.SlideTrailThigh - legs.ThighR));
                span = Max(span, Abs(VerbPoseClips.SlideLeadThigh - legs.ThighL));
                span = Max(span, Abs(VerbPoseClips.SlideLeadThigh - legs.ThighR));
            }
            float seconds = eased ? SlideSeconds : VerbPoseClips.SlideBlendSeconds;
            return WeightPeak(span, seconds, eased ? 1 : 0);
        }

        static float ZipGrabStep(bool eased)
        {
            float span = Abs(BodyLine.CablePitch - JumpPose.FallArmPitch);
            if (!eased)
            {
                float prev = JumpPose.FallArmPitch;
                float peak = 0f;
                int n = (int)(ZipPose.CatchSeconds / Dt) + 3;
                for (int i = 0; i <= n; i++)
                {
                    float pitch = Lerp(JumpPose.FallArmPitch, BodyLine.CablePitch, BodyLine.ZipGrab(i * Dt));
                    peak = Max(peak, Abs(pitch - prev));
                    prev = pitch;
                }
                return peak;
            }
            return WeightPeak(span, ZipGrabSeconds, 1);
        }

        static float ZipDropStep(bool eased)
        {
            if (!eased) return BodyLine.ZipDropStep(true);
            float span = Abs(BodyLine.CablePitch - JumpPose.FallArmPitch);
            span = Max(span, Abs(BodyLine.CablePitch - AirFeel.BraceArmPitch));
            return WeightPeak(span, ZipDropSeconds, 2);
        }

        static float GrappleInStep(bool eased)
        {
            float span = Abs(GrapplePose.LatchPitchR - GrapplePose.AimPitchR);
            span = Max(span, Abs(GrapplePose.LatchPitchL - GrapplePose.AimPitchL));
            span = Max(span, Abs(GrapplePose.LatchElbowR - GrapplePose.AimElbowR));
            span = Max(span, Abs(GrapplePose.LatchElbowL - GrapplePose.AimElbowL));
            span = Max(span, Abs(GrapplePose.LatchSpine - GrapplePose.AimSpine));
            if (!eased) return span;
            return WeightPeak(span, LatchSeconds, 1);
        }

        static float GrappleOutStep(bool eased)
        {
            float span = Abs(GrapplePose.PullPitchR - JumpPose.FallArmPitch);
            span = Max(span, Abs(GrapplePose.PullPitchL - JumpPose.FallArmPitch));
            span = Max(span, Abs(GrapplePose.PullSpine - JumpPose.FallSpine));
            float seconds = eased ? ReleaseSeconds : GrapplePose.ReleaseBlendSeconds;
            return WeightPeak(span, seconds, eased ? 1 : 0);
        }

        static float PadUpStep(bool eased)
        {
            if (!eased) return BodyLine.PadStep(true);
            return WeightPeak(Abs(LaunchPose.SwingArmPitch), PadSeconds, 1);
        }

        static float PadAirStep(bool capped)
        {
            float vy = 24.7f;
            float vis = vy;
            LaunchPose.Sample prev = LaunchPose.At(vis);
            float peak = 0f;
            for (int i = 0; i < 220; i++)
            {
                float g = vy > 0f ? 22f : 22f * 1.62f;
                vy -= g * Dt;
                vis = capped ? MoveTowards(vis, vy, PadVyStep) : vy;
                LaunchPose.Sample now = LaunchPose.At(vis);
                peak = Max(peak, Abs(now.ArmPitchL - prev.ArmPitchL));
                peak = Max(peak, Abs(now.ArmYawL - prev.ArmYawL));
                peak = Max(peak, Abs(now.ThighL - prev.ThighL));
                peak = Max(peak, Abs(now.KneeL - prev.KneeL));
                prev = now;
                if (vy < -16f && vis < -16f) break;
            }
            return peak;
        }

        /// <summary>curve 0 is smoothstep, 1 is raised cosine, 2 is linear.</summary>
        static float WeightPeak(float span, float seconds, int curve)
        {
            float prev = 0f;
            float peak = 0f;
            float age = 0f;
            int n = (int)(seconds / Dt) + 2;
            for (int i = 0; i <= n; i++)
            {
                float u = seconds > 0.0001f ? age / seconds : 1f;
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
                float w = curve == 1 ? Raised(u) : (curve == 0 ? PoseHandoff.Ease(u) : u);
                float now = w * span;
                peak = Max(peak, Abs(now - prev));
                prev = now;
                age += Dt;
                if (u >= 1f) break;
            }
            return peak;
        }

        static Shot ClimbShot(float u, bool after)
        {
            float seconds = after ? ClimbSeconds : MantlePose.EnterBlendSeconds;
            float age = u * (after ? 0.28f : 0.16f);
            float snap = seconds > 0.0001f ? age / seconds : 1f;
            if (snap > 1f) snap = 1f;
            float motor = age / MantleWindow;
            if (motor > 1f) motor = 1f;
            float w = after ? ClimbOpen(snap) : PoseHandoff.Ease(snap);
            float shown = after ? VaultShown(motor) : motor;
            Bones from = Bones.Wall(WallPose.Climb(1f, WallPose.ClimbSpeedRef));
            Bones to = Bones.Mantle(MantlePose.At(shown, true));
            return ToShot(Bones.Lerp(from, to, w));
        }

        static Shot VaultShot(float u, bool after)
        {
            float shown = after ? VaultShown(u) : u;
            return ToShot(Bones.Mantle(MantlePose.At(shown, true)));
        }

        static Shot VaultOutShot(float u, bool after)
        {
            float w = after ? VaultOutOpen(u) : PoseHandoff.Ease(u);
            Bones land = Bones.Mantle(MantlePose.At(1f, true));
            GaitBlend.Legs legs = GaitBlend.At(1.5707963f, 13.8f);
            Bones run = land;
            run.ThL = legs.ThighL;
            run.ThR = legs.ThighR;
            run.KnL = legs.KneeL;
            run.KnR = legs.KneeR;
            float amp = GaitBlend.ArmAmp(1f);
            run.ArmL = -amp * 0.55f;
            run.ArmR = amp * 0.2f;
            return ToShot(Bones.Lerp(land, run, w));
        }

        static Shot SlideShot(float u, bool after, bool entering)
        {
            float w = after ? SlideOpen(u * SlideSeconds, entering) : (entering ? PoseHandoff.Ease(u) : 1f - PoseHandoff.Ease(u));
            if (!entering && !after) w = 1f - PoseHandoff.Ease(u);
            GaitBlend.Legs legs = GaitBlend.At(1.5707963f, 13.8f);
            float thighL = Lerp(legs.ThighL, VerbPoseClips.SlideLeadThigh, entering ? w : 1f - w);
            float thighR = Lerp(legs.ThighR, VerbPoseClips.SlideTrailThigh, entering ? w : 1f - w);
            float kneeL = Lerp(legs.KneeL, VerbPoseClips.SlideLeadKnee, entering ? w : 1f - w);
            float kneeR = Lerp(legs.KneeR, VerbPoseClips.SlideTrailKnee, entering ? w : 1f - w);
            if (!after && !entering)
            {
                float leave = PoseHandoff.Ease(u);
                thighL = Lerp(VerbPoseClips.SlideLeadThigh, legs.ThighL, leave);
                thighR = Lerp(VerbPoseClips.SlideTrailThigh, legs.ThighR, leave);
                kneeL = Lerp(VerbPoseClips.SlideLeadKnee, legs.KneeL, leave);
                kneeR = Lerp(VerbPoseClips.SlideTrailKnee, legs.KneeR, leave);
            }
            return new Shot
            {
                ThL = thighL, ThR = thighR, KnL = kneeL, KnR = kneeR,
                ArmL = -20f, ArmR = 16f, ElbL = -16f, ElbR = -16f,
                Spine = Lerp(8f, VerbPoseClips.SlideSpine, entering ? w : (after ? w : PoseHandoff.Ease(1f - u))),
                Hip = Lerp(4f, VerbPoseClips.SlideHip, entering ? w : (after ? 1f - w : PoseHandoff.Ease(u))),
                Head = 8f,
            };
        }

        static Shot ZipGrabShot(float u, bool after)
        {
            float age = u * (after ? ZipGrabSeconds : ZipPose.CatchSeconds);
            float w = after ? ZipGrab(age) : BodyLine.ZipGrab(age);
            float pitch = Lerp(JumpPose.FallArmPitch, BodyLine.CablePitch, w);
            return new Shot
            {
                ArmL = pitch, ArmR = pitch, ElbL = -12f, ElbR = -12f,
                ThL = 20f, ThR = 16f, KnL = -16f, KnR = -14f,
                Spine = -4f, Hip = -4f, Head = -4f,
            };
        }

        static Shot ZipDropShot(float u, bool after)
        {
            float pitch;
            if (after)
                pitch = Lerp(BodyLine.CablePitch, JumpPose.FallArmPitch, ZipDrop(u * ZipDropSeconds));
            else
                pitch = BodyLine.ZipArm(u * ZipPose.ReleaseSeconds, true, JumpPose.FallArmPitch);
            return new Shot
            {
                ArmL = pitch, ArmR = pitch, ElbL = -14f, ElbR = -14f,
                ThL = 18f, ThR = 14f, KnL = -16f, KnR = -14f,
                Spine = -2f, Hip = 0f, Head = 4f,
            };
        }

        static Shot GrappleInShot(float u, bool after)
        {
            float w = after ? LatchOpen(u) : (u < 0.15f ? 0f : 1f);
            return new Shot
            {
                ArmL = Lerp(GrapplePose.AimPitchL, GrapplePose.LatchPitchL, w),
                ArmR = Lerp(GrapplePose.AimPitchR, GrapplePose.LatchPitchR, w),
                ElbL = Lerp(GrapplePose.AimElbowL, GrapplePose.LatchElbowL, w),
                ElbR = Lerp(GrapplePose.AimElbowR, GrapplePose.LatchElbowR, w),
                ThL = GrapplePose.AimThigh,
                ThR = GrapplePose.AimThigh,
                KnL = GrapplePose.AimKnee,
                KnR = GrapplePose.AimKnee,
                Spine = Lerp(GrapplePose.AimSpine, GrapplePose.LatchSpine, w),
                Hip = Lerp(GrapplePose.AimHip, GrapplePose.LatchHip, w),
                Head = Lerp(GrapplePose.AimHead, GrapplePose.LatchHead, w),
            };
        }

        static Shot GrappleOutShot(float u, bool after)
        {
            float w = after ? ReleaseOpen(u) : PoseHandoff.Ease(u);
            return new Shot
            {
                ArmL = Lerp(GrapplePose.PullPitchL, JumpPose.FallArmPitch, w),
                ArmR = Lerp(GrapplePose.PullPitchR, JumpPose.FallArmPitch, w),
                ElbL = Lerp(GrapplePose.PullElbowL, JumpPose.FallElbow, w),
                ElbR = Lerp(GrapplePose.PullElbowR, JumpPose.FallElbow, w),
                ThL = Lerp(GrapplePose.StrideThighBack, JumpPose.FallThigh, w),
                ThR = Lerp(GrapplePose.StrideThighFwd, JumpPose.FallThigh, w),
                KnL = -16f, KnR = -16f,
                Spine = Lerp(GrapplePose.PullSpine, JumpPose.FallSpine, w),
                Hip = Lerp(GrapplePose.PullHip, 4f, w),
                Head = Lerp(GrapplePose.PullHead, 8f, w),
            };
        }

        static Shot PadUpShot(float u, bool after)
        {
            float age = u * (after ? PadSeconds : BodyLine.PadSeconds);
            float w = after ? PadOpen(age) : BodyLine.PadOpen(age);
            float pitch = Lerp(0f, LaunchPose.SwingArmPitch, w);
            return new Shot
            {
                ArmL = pitch, ArmR = pitch,
                ElbL = LaunchPose.SwingElbow, ElbR = LaunchPose.SwingElbow,
                ThL = Lerp(12f, LaunchPose.SwingThigh, w),
                ThR = Lerp(8f, LaunchPose.SwingThigh - 4f, w),
                KnL = Lerp(-8f, LaunchPose.SwingKnee, w),
                KnR = Lerp(-6f, LaunchPose.SwingKnee, w),
                Spine = LaunchPose.SwingSpine, Hip = LaunchPose.SwingHip, Head = LaunchPose.SwingHead,
            };
        }

        static Shot PadAirShot(float u, bool after)
        {
            float vy = Lerp(8f, -12f, u);
            float vis = after ? MoveTowards(0f, vy < 0f ? vy : 0f, PadVyStep * (1f + u * 8f)) : vy;
            if (!after) vis = vy;
            LaunchPose.Sample s = LaunchPose.At(vis);
            return new Shot
            {
                ArmL = s.ArmPitchL, ArmR = s.ArmPitchR,
                ElbL = s.ElbowL, ElbR = s.ElbowR,
                ThL = s.ThighL, ThR = s.ThighR,
                KnL = s.KneeL, KnR = s.KneeR,
                Spine = s.Spine, Hip = s.Hip, Head = s.Head,
            };
        }

        static Shot ToShot(Bones b)
        {
            return new Shot
            {
                ArmL = b.ArmL, ArmR = b.ArmR, ElbL = b.ElbL, ElbR = b.ElbR,
                ThL = b.ThL, ThR = b.ThR, KnL = b.KnL, KnR = b.KnR,
                Spine = b.Spine, Hip = b.Hip, Head = b.Head,
            };
        }

        static string Pair(float before, float after, CultureInfo c)
        {
            return before.ToString("0.0", c) + ">" + after.ToString("0.0", c);
        }

        static float Raised(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return 0.5f * (1f - Mathf.Cos(u * 3.14159265f));
        }

        static float InvEase(float e)
        {
            if (e <= 0f) return 0f;
            if (e >= 1f) return 1f;
            float u = e;
            for (int i = 0; i < 8; i++)
            {
                float y = u * u * (3f - 2f * u);
                float d = 6f * u * (1f - u);
                if (d < 0.0001f) break;
                u -= (y - e) / d;
                if (u < 0f) u = 0f;
                if (u > 1f) u = 1f;
            }
            return u;
        }

        static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        static float MoveTowards(float current, float target, float maxDelta)
        {
            float d = target - current;
            if (d > maxDelta) return current + maxDelta;
            if (d < -maxDelta) return current - maxDelta;
            return target;
        }

        static float Abs(float v)
        {
            return v < 0f ? -v : v;
        }

        static float Max(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}
