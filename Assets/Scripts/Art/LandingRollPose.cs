using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Parkour shoulder roll when a landing is near terminal fall speed.
    /// The visual root turns a full circle about the lead-shoulder diagonal.
    /// The capsule, the velocity, and the camera stay put.
    /// </summary>
    public static class LandingRollPose
    {
        public const bool RootMotion = false;
        public const bool AddsStun = false;
        public const bool ChangesVelocity = false;

        public const float Terminal = 56.16f;
        public const float Fraction = 0.65f;
        /// <summary>Downward speed, m/s. 0.65 * terminal fall.</summary>
        public const float Threshold = Terminal * Fraction;
        public const float Gravity = 22f;
        public const float FallMult = 1.62f;
        /// <summary>Planar speed under this plays the crouch absorb instead of the shoulder roll.</summary>
        public const float AbsorbPlanar = 1.25f;
        public const float SoftFloor = 3f;
        /// <summary>Light hop below this. Medium knee bend below the next. Heavy hands after that, until the roll.</summary>
        public const float LightCeil = 12f;
        public const float MediumCeil = 24f;

        public const float Seconds = 0.52f;
        public const float AbsorbSeconds = 0.32f;
        /// <summary>Shoulder-contact beat. Dust and the audio hook fire here.</summary>
        public const float DustAt = 0.18f;
        /// <summary>No constant mesh dip. The roll settles onto the floor instead.</summary>
        public const float DipMeters = 0f;
        public const float SpinDegrees = 360f;

        public struct Figure
        {
            public Vector3 Hip, Chest, Head, Shoulder, Hand, Knee, Foot, OffHand;
            public float MinY;
            public float Shift;
        }

        public static float DropMeters()
        {
            float v = Threshold;
            float g = Gravity * FallMult;
            return (v * v) / (2f * g);
        }

        public static bool Triggered(float downwardSpeed)
        {
            return downwardSpeed >= Threshold;
        }

        public static bool Stationary(float planarSpeed)
        {
            float s = planarSpeed < 0f ? -planarSpeed : planarSpeed;
            return s < AbsorbPlanar;
        }

        /// <summary>0.25 light, 0.55 medium, 1 heavy. The roll gate is not a tier.</summary>
        public static float TierScale(float impact)
        {
            if (impact < LightCeil) return 0.25f;
            if (impact < MediumCeil) return 0.55f;
            return 1f;
        }

        /// <summary>Strafe picks the lead shoulder. A straight landing alternates.</summary>
        public static bool LeadLeft(float lateral, int alternate)
        {
            if (lateral < -0.45f) return true;
            if (lateral > 0.45f) return false;
            return alternate == 1;
        }

        public static Vector3 Axis(bool shoulderLeft)
        {
            float x = shoulderLeft ? -0.86f : 0.86f;
            Vector3 a = new Vector3(x, -0.30f, 0.42f);
            a.Normalize();
            return a;
        }

        public static Vector3 Pivot(bool shoulderLeft)
        {
            // Lead shoulder, so the hips and the head orbit that diagonal.
            float x = shoulderLeft ? -0.22f : 0.22f;
            return new Vector3(x, 1.15f, 0.08f);
        }

        public static float SpinAt(float u)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return SpinDegrees * u;
        }

        /// <summary>Presentation does not rewrite velocity.</summary>
        public static Vector3 KeepVelocity(Vector3 velocity)
        {
            return velocity;
        }

        public static VerbExitSample RollAt(float u, bool shoulderLeft)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            VerbExitSample s = Piece(u,
                0f, Tuck(),
                0.18f, Sweep(),
                0.36f, HandDown(),
                0.52f, Shoulder(),
                0.68f, LegsOver(),
                0.84f, Plant(),
                1f, Rise());
            s.RootSpin = SpinAt(u);
            s.RootPitch = 0f;
            s.Drop = 0f;
            if (shoulderLeft)
                s = VerbExitSample.Mirror(s);
            return s;
        }

        public static VerbExitSample AbsorbAt(float u)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            VerbExitSample deep = HandsDown();
            VerbExitSample up = Rise();
            up.ThighL = 16f;
            up.ThighR = 10f;
            up.KneeL = -14f;
            up.KneeR = -10f;
            up.Head = -4f;
            if (u < 0.42f)
                return VerbExitSample.Lerp(deep, deep, 1f);
            return VerbExitSample.Lerp(deep, up, PoseHandoff.Ease((u - 0.42f) / 0.58f));
        }

        public static VerbExitSample LandAt(float u, float tierScale)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            VerbExitSample a;
            VerbExitSample b;
            VerbExitSample c;
            if (tierScale < 0.4f)
                Light(out a, out b, out c);
            else if (tierScale < 0.75f)
                Medium(out a, out b, out c);
            else
                Heavy(out a, out b, out c);
            float e = PoseHandoff.Ease(u);
            VerbExitSample s = e < 0.5f
                ? VerbExitSample.Lerp(a, b, e * 2f)
                : VerbExitSample.Lerp(b, c, (e - 0.5f) * 2f);
            return s;
        }

        public static Figure PoseFigure(VerbExitSample s, float spin, bool shoulderLeft)
        {
            float leadPitch = shoulderLeft ? s.ArmPitchL : s.ArmPitchR;
            float leadElbow = shoulderLeft ? s.ElbowL : s.ElbowR;
            float offPitch = shoulderLeft ? s.ArmPitchR : s.ArmPitchL;
            float offElbow = shoulderLeft ? s.ElbowR : s.ElbowL;
            float thigh = shoulderLeft ? s.ThighL : s.ThighR;
            float knee = shoulderLeft ? s.KneeL : s.KneeR;
            float side = shoulderLeft ? -1f : 1f;

            float squat = -knee / 140f;
            if (squat < 0f) squat = 0f;
            if (squat > 1f) squat = 1f;
            // A spinning roll keeps the hip up so the orbit reads. A plant squats.
            if (spin > 1f && spin < 359f)
                squat *= 0.25f;
            float hipY = 0.96f + (0.42f - 0.96f) * squat;
            Vector3 hip = new Vector3(0f, hipY, 0f);
            float spine = s.Spine * Mathf.Deg2Rad;
            Vector3 chest = new Vector3(
                side * s.SpineRoll * 0.003f,
                hipY + Mathf.Cos(spine) * 0.30f,
                Mathf.Sin(spine) * 0.30f);
            float hr = (s.Spine + s.Head) * Mathf.Deg2Rad;
            Vector3 head = new Vector3(
                chest.x,
                chest.y + Mathf.Cos(hr) * 0.13f,
                chest.z + Mathf.Sin(hr) * 0.13f);
            Vector3 shoulder = new Vector3(side * 0.26f, chest.y + 0.02f, chest.z + 0.02f);
            Vector3 hand = LimbEnd(shoulder, leadPitch, leadElbow, 0.24f, 0.20f, side * 0.06f);
            Vector3 off = LimbEnd(
                new Vector3(-side * 0.20f, chest.y, chest.z),
                offPitch, offElbow, 0.22f, 0.18f, -side * 0.04f);
            float rt = thigh * Mathf.Deg2Rad;
            Vector3 kneeV = new Vector3(
                side * 0.08f,
                hipY - Mathf.Cos(rt) * 0.42f,
                Mathf.Sin(rt) * 0.42f);
            float rk = (thigh + knee) * Mathf.Deg2Rad;
            Vector3 foot = new Vector3(
                kneeV.x,
                kneeV.y - Mathf.Cos(rk) * 0.40f,
                kneeV.z + Mathf.Sin(rk) * 0.40f);

            Vector3 axis = Axis(shoulderLeft);
            Vector3 pivot = Pivot(shoulderLeft);
            Figure f;
            f.Hip = SpinPoint(hip, pivot, axis, spin);
            f.Chest = SpinPoint(chest, pivot, axis, spin);
            f.Head = SpinPoint(head, pivot, axis, spin);
            f.Shoulder = SpinPoint(shoulder, pivot, axis, spin);
            f.Hand = SpinPoint(hand, pivot, axis, spin);
            f.Knee = SpinPoint(kneeV, pivot, axis, spin);
            f.Foot = SpinPoint(foot, pivot, axis, spin);
            f.OffHand = SpinPoint(off, pivot, axis, spin);
            f.MinY = f.Hip.y;
            f.MinY = Lower(f.MinY, f.Chest.y);
            f.MinY = Lower(f.MinY, f.Head.y);
            f.MinY = Lower(f.MinY, f.Shoulder.y);
            f.MinY = Lower(f.MinY, f.Hand.y);
            f.MinY = Lower(f.MinY, f.Knee.y);
            f.MinY = Lower(f.MinY, f.Foot.y);
            f.MinY = Lower(f.MinY, f.OffHand.y);
            f.Shift = -f.MinY;
            return f;
        }

        public static float FloorShift(VerbExitSample s, float spin, bool shoulderLeft)
        {
            return PoseFigure(s, spin, shoulderLeft).Shift;
        }

        public static Vector3 OrbitDelta(Vector3 pivot, Vector3 axis, float degrees)
        {
            return SpinPoint(Vector3.zero, pivot, axis, degrees);
        }

        public static Vector3 SpinPoint(Vector3 p, Vector3 pivot, Vector3 axis, float degrees)
        {
            Vector3 v = p - pivot;
            float rad = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float dot = axis.x * v.x + axis.y * v.y + axis.z * v.z;
            float cx = axis.y * v.z - axis.z * v.y;
            float cy = axis.z * v.x - axis.x * v.z;
            float cz = axis.x * v.y - axis.y * v.x;
            float k = 1f - c;
            Vector3 r = new Vector3(
                v.x * c + cx * s + axis.x * dot * k,
                v.y * c + cy * s + axis.y * dot * k,
                v.z * c + cz * s + axis.z * dot * k);
            return pivot + r;
        }

        static Vector3 LimbEnd(Vector3 root, float pitch, float elbow, float upper, float lower, float xBias)
        {
            float ra = -pitch * Mathf.Deg2Rad;
            Vector3 ad = new Vector3(xBias, -Mathf.Cos(ra), Mathf.Sin(ra));
            Vector3 el = root + ad * upper;
            float rb = -(pitch + elbow) * Mathf.Deg2Rad;
            Vector3 fd = new Vector3(xBias * 0.4f, -Mathf.Cos(rb), Mathf.Sin(rb));
            return el + fd * lower;
        }

        static float Lower(float a, float b)
        {
            return b < a ? b : a;
        }

        static VerbExitSample Piece(float u,
            float u0, VerbExitSample a,
            float u1, VerbExitSample b,
            float u2, VerbExitSample c,
            float u3, VerbExitSample d,
            float u4, VerbExitSample e,
            float u5, VerbExitSample f,
            float u6, VerbExitSample g)
        {
            if (u <= u1) return VerbExitSample.Lerp(a, b, Span(u, u0, u1));
            if (u <= u2) return VerbExitSample.Lerp(b, c, Span(u, u1, u2));
            if (u <= u3) return VerbExitSample.Lerp(c, d, Span(u, u2, u3));
            if (u <= u4) return VerbExitSample.Lerp(d, e, Span(u, u3, u4));
            if (u <= u5) return VerbExitSample.Lerp(e, f, Span(u, u4, u5));
            return VerbExitSample.Lerp(f, g, Span(u, u5, u6));
        }

        static float Span(float u, float a, float b)
        {
            float d = b - a;
            if (d < 0.0001f) return 1f;
            return PoseHandoff.Ease((u - a) / d);
        }

        static VerbExitSample Tuck()
        {
            VerbExitSample s = default;
            s.Hip = 24f;
            s.Spine = 18f;
            s.Head = -34f;
            s.ThighL = 48f;
            s.ThighR = 42f;
            s.KneeL = -78f;
            s.KneeR = -72f;
            s.ArmPitchL = -36f;
            s.ArmPitchR = -28f;
            s.ElbowL = -96f;
            s.ElbowR = -88f;
            s.SpineRoll = 6f;
            return s;
        }

        static VerbExitSample Sweep()
        {
            VerbExitSample s = default;
            s.Hip = 18f;
            s.Spine = 22f;
            s.Head = -28f;
            s.ThighL = 64f;
            s.ThighR = 58f;
            s.KneeL = -96f;
            s.KneeR = -90f;
            s.ArmPitchL = -48f;
            s.ArmPitchR = -102f;
            s.ArmYawR = -16f;
            s.ElbowL = -80f;
            s.ElbowR = -22f;
            s.SpineRoll = 10f;
            return s;
        }

        static VerbExitSample HandDown()
        {
            VerbExitSample s = default;
            s.Hip = 12f;
            s.Spine = 16f;
            s.Head = -22f;
            s.ThighL = 88f;
            s.ThighR = 80f;
            s.KneeL = -112f;
            s.KneeR = -104f;
            s.ArmPitchL = -30f;
            s.ArmPitchR = -58f;
            s.ElbowL = -70f;
            s.ElbowR = -64f;
            s.SpineRoll = 16f;
            return s;
        }

        static VerbExitSample Shoulder()
        {
            VerbExitSample s = default;
            s.Hip = 8f;
            s.Spine = 4f;
            s.Head = -36f;
            s.ThighL = 112f;
            s.ThighR = 104f;
            s.KneeL = -116f;
            s.KneeR = -108f;
            s.ArmPitchL = -24f;
            s.ArmPitchR = 10f;
            s.ElbowL = -64f;
            s.ElbowR = -78f;
            s.SpineRoll = 26f;
            s.HipRoll = -8f;
            return s;
        }

        static VerbExitSample LegsOver()
        {
            VerbExitSample s = default;
            s.Hip = -6f;
            s.Spine = -8f;
            s.Head = -18f;
            s.ThighL = 118f;
            s.ThighR = 108f;
            s.KneeL = -104f;
            s.KneeR = -96f;
            s.ArmPitchL = -16f;
            s.ArmPitchR = 14f;
            s.ElbowL = -48f;
            s.ElbowR = -56f;
            s.SpineRoll = 18f;
            return s;
        }

        static VerbExitSample Plant()
        {
            VerbExitSample s = default;
            s.Hip = 10f;
            s.Spine = 6f;
            s.Head = -6f;
            s.ThighL = 36f;
            s.ThighR = 22f;
            s.KneeL = -28f;
            s.KneeR = -18f;
            s.ArmPitchL = -18f;
            s.ArmPitchR = -8f;
            s.ElbowL = -30f;
            s.ElbowR = -22f;
            s.SpineRoll = 8f;
            return s;
        }

        static VerbExitSample Rise()
        {
            VerbExitSample s = default;
            s.Hip = 4f;
            s.Spine = 2f;
            s.Head = 0f;
            s.ThighL = 22f;
            s.ThighR = 8f;
            s.KneeL = -16f;
            s.KneeR = -10f;
            s.ArmPitchL = -16f;
            s.ArmPitchR = -8f;
            s.ElbowL = -12f;
            s.ElbowR = -10f;
            return s;
        }

        /// <summary>Both palms on the ground, chin in, hips down.</summary>
        static VerbExitSample HandsDown()
        {
            VerbExitSample s = default;
            s.Hip = 46f;
            s.Spine = 70f;
            s.Head = -32f;
            s.ThighL = 100f;
            s.ThighR = 96f;
            s.KneeL = -136f;
            s.KneeR = -132f;
            s.ArmPitchL = -52f;
            s.ArmPitchR = -48f;
            s.ElbowL = -12f;
            s.ElbowR = -14f;
            s.HipYaw = 6f;
            return s;
        }

        static void Light(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = default;
            a.Hip = 6f;
            a.Spine = 4f;
            a.Head = -2f;
            a.ThighL = 14f;
            a.ThighR = 12f;
            a.KneeL = -16f;
            a.KneeR = -14f;
            a.ArmPitchL = -8f;
            a.ArmPitchR = 6f;
            a.ElbowL = -10f;
            a.ElbowR = -8f;
            b = a;
            b.KneeL = -24f;
            b.KneeR = -22f;
            b.ThighL = 20f;
            b.ThighR = 16f;
            b.Head = 2f;
            b.ArmPitchL = -18f;
            b.ArmPitchR = 14f;
            b.HipYaw = 6f;
            c = default;
            c.ThighL = 12f;
            c.ThighR = 6f;
            c.KneeL = -8f;
            c.KneeR = -6f;
            c.ArmPitchL = -10f;
            c.ArmPitchR = -6f;
            c.Head = 0f;
        }

        static void Medium(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = default;
            a.Hip = 12f;
            a.Spine = 10f;
            a.Head = -6f;
            a.ThighL = 28f;
            a.ThighR = 24f;
            a.KneeL = -36f;
            a.KneeR = -32f;
            a.ArmPitchL = -16f;
            a.ArmPitchR = -12f;
            a.ElbowL = -20f;
            a.ElbowR = -18f;
            b = a;
            b.Hip = 20f;
            b.Spine = 18f;
            b.Head = -10f;
            b.ThighL = 46f;
            b.ThighR = 40f;
            b.KneeL = -64f;
            b.KneeR = -58f;
            b.ArmPitchL = -28f;
            b.ArmPitchR = -22f;
            b.ElbowL = -24f;
            b.ElbowR = -20f;
            b.HipYaw = -8f;
            c = default;
            c.Hip = 4f;
            c.ThighL = 16f;
            c.ThighR = 8f;
            c.KneeL = -12f;
            c.KneeR = -8f;
            c.ArmPitchL = -12f;
            c.ArmPitchR = -8f;
            c.Head = -2f;
        }

        static void Heavy(out VerbExitSample a, out VerbExitSample b, out VerbExitSample c)
        {
            a = default;
            a.Hip = 28f;
            a.Spine = 36f;
            a.Head = -16f;
            a.ThighL = 64f;
            a.ThighR = 58f;
            a.KneeL = -88f;
            a.KneeR = -82f;
            a.ArmPitchL = -36f;
            a.ArmPitchR = -32f;
            a.ElbowL = -20f;
            a.ElbowR = -18f;
            b = a;
            b.Hip = 40f;
            b.Spine = 56f;
            b.Head = -26f;
            b.ThighL = 86f;
            b.ThighR = 80f;
            b.KneeL = -118f;
            b.KneeR = -112f;
            b.ArmPitchL = -64f;
            b.ArmPitchR = -58f;
            b.ElbowL = -16f;
            b.ElbowR = -18f;
            b.HipYaw = 8f;
            c = default;
            c.Hip = 8f;
            c.Spine = 4f;
            c.ThighL = 20f;
            c.ThighR = 12f;
            c.KneeL = -16f;
            c.KneeR = -12f;
            c.ArmPitchL = -14f;
            c.ArmPitchR = -10f;
            c.Head = -4f;
        }

        public static bool Holds()
        {
            if (RootMotion || AddsStun || ChangesVelocity) return false;
            if (Mathf.Abs(Terminal - 56.16f) > 0.001f) return false;
            if (Mathf.Abs(Fraction - 0.65f) > 0.001f) return false;
            if (Mathf.Abs(Threshold - Terminal * Fraction) > 0.001f) return false;
            if (Mathf.Abs(Gravity - 22f) > 0.001f) return false;
            if (Mathf.Abs(FallMult - 1.62f) > 0.001f) return false;
            if (Triggered(Threshold - 0.05f)) return false;
            if (!Triggered(Threshold)) return false;
            if (!Triggered(Terminal)) return false;
            if (Triggered(10f) || Triggered(0f)) return false;
            float drop = DropMeters();
            if (drop < 18.2f || drop > 19.2f) return false;
            if (!Stationary(0f) || !Stationary(1.24f)) return false;
            if (Stationary(AbsorbPlanar) || Stationary(8f)) return false;
            if (Seconds < 0.45f || Seconds > 0.60f) return false;
            if (AbsorbSeconds < 0.15f || AbsorbSeconds > 0.35f) return false;
            if (DipMeters > 0.001f) return false;
            if (Mathf.Abs(SpinDegrees - 360f) > 0.1f) return false;

            Vector3 v = new Vector3(4.5f, -Threshold, 1.25f);
            Vector3 kept = KeepVelocity(v);
            if (kept.x != v.x || kept.y != v.y || kept.z != v.z) return false;

            Vector3 axis = Axis(false);
            if (axis.x < 0.5f || axis.z < 0.2f) return false;
            if (Axis(true).x > -0.5f) return false;
            if (Mathf.Abs(SpinAt(0f)) > 0.01f) return false;
            if (Mathf.Abs(SpinAt(1f) - 360f) > 0.01f) return false;
            if (SpinAt(0.5f) < 160f) return false;

            if (!Clear(0f, false)) return false;
            if (!Clear(0.18f, false)) return false;
            if (!Clear(0.36f, false)) return false;
            if (!Clear(0.52f, false)) return false;
            if (!Clear(0.68f, false)) return false;
            if (!Clear(0.84f, false)) return false;
            if (!Clear(1f, false)) return false;
            if (!Clear(0.52f, true)) return false;

            VerbExitSample tuck = RollAt(0f, false);
            VerbExitSample shoulder = RollAt(0.52f, false);
            VerbExitSample over = RollAt(0.68f, false);
            VerbExitSample rise = RollAt(1f, false);
            if (tuck.Head > -20f || tuck.KneeL > -60f) return false;
            if (shoulder.SpineRoll < 18f || shoulder.RootSpin < 170f) return false;
            if (over.ThighL < 90f || over.RootSpin < 220f) return false;
            if (rise.RootSpin < 350f || rise.ThighL > 40f) return false;
            VerbExitSample left = RollAt(0.52f, true);
            if (left.SpineRoll > -18f) return false;
            Figure contact = PoseFigure(shoulder, shoulder.RootSpin, false);
            if (contact.Shoulder.y - contact.MinY > 0.12f) return false;
            if (contact.Head.y < contact.Shoulder.y - 0.02f) return false;
            Figure leftC = PoseFigure(left, left.RootSpin, true);
            if (leftC.Shoulder.y - leftC.MinY > 0.12f) return false;

            VerbExitSample absorb = AbsorbAt(0.2f);
            if (absorb.KneeL > -120f || absorb.ArmPitchL > -40f || absorb.ArmPitchR > -40f) return false;
            Figure hands = PoseFigure(absorb, 0f, false);
            float handGap = hands.Hand.y - hands.Foot.y;
            if (handGap < 0f) handGap = -handGap;
            float offGap = hands.OffHand.y - hands.Foot.y;
            if (offGap < 0f) offGap = -offGap;
            if (handGap > 0.2f || offGap > 0.25f) return false;
            if (!ClearSample(absorb, 0f, false)) return false;

            if (TierScale(4f) > 0.3f || TierScale(16f) < 0.5f || TierScale(30f) < 0.9f) return false;
            VerbExitSample hop = LandAt(0.5f, 0.25f);
            VerbExitSample mid = LandAt(0.5f, 0.55f);
            VerbExitSample heavy = LandAt(0.5f, 1f);
            if (-hop.KneeL >= -mid.KneeL) return false;
            if (-mid.KneeL >= -heavy.KneeL) return false;
            if (heavy.ArmPitchL > -40f) return false;
            if (hop.ArmPitchL < -30f) return false;
            if (VerbExitSample.Gap(hop, heavy) < 80f) return false;
            if (VerbExitSample.Gap(over, absorb) < 80f) return false;

            if (!LeadLeft(-1f, 0)) return false;
            if (LeadLeft(1f, 1)) return false;
            if (!LeadLeft(0f, 1)) return false;
            if (LeadLeft(0f, 0)) return false;

            if (ChaseCam.FovPop != 0f || ChaseCam.Shake != 0f || ChaseCam.SlowMo != 0f) return false;
            return true;
        }

        static bool Clear(float u, bool left)
        {
            VerbExitSample s = RollAt(u, left);
            return ClearSample(s, s.RootSpin, left);
        }

        static bool ClearSample(VerbExitSample s, float spin, bool left)
        {
            Figure f = PoseFigure(s, spin, left);
            float floor = f.MinY + f.Shift;
            if (floor < -0.002f || floor > 0.002f) return false;
            if (f.Head.y + f.Shift < -0.002f) return false;
            if (f.Hand.y + f.Shift < -0.002f) return false;
            if (f.Foot.y + f.Shift < -0.002f) return false;
            if (f.OffHand.y + f.Shift < -0.002f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Vector3 v = new Vector3(4.5f, -Threshold, 1.25f);
            Vector3 kept = KeepVelocity(v);
            float delta = Mathf.Abs(kept.x - v.x) + Mathf.Abs(kept.y - v.y) + Mathf.Abs(kept.z - v.z);
            VerbExitSample shoulder = RollAt(0.52f, false);
            Figure contact = PoseFigure(shoulder, shoulder.RootSpin, false);
            float gap = contact.Shoulder.y - contact.MinY;
            if (gap < 0f) gap = -gap;
            return "landing-roll"
                + " threshold=" + Threshold.ToString("0.00")
                + " fraction=" + Fraction.ToString("0.00")
                + " terminal=" + Terminal.ToString("0.00")
                + " drop=" + DropMeters().ToString("0.00") + "m"
                + " seconds=" + Seconds.ToString("0.00")
                + " absorb=" + AbsorbSeconds.ToString("0.00")
                + " absorb-planar=" + AbsorbPlanar.ToString("0.00")
                + " velocity-delta=" + delta.ToString("0.00")
                + " stun=0"
                + " camera-roll=0"
                + " dip=" + DipMeters.ToString("0.00") + "m"
                + " spin=" + SpinDegrees.ToString("0")
                + " floor=" + (contact.MinY + contact.Shift).ToString("0.00")
                + " shoulder=" + gap.ToString("0.00");
        }
    }
}
