using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Parkour shoulder roll when a landing is near terminal fall speed.
    /// Chin tucks, the lead arm stays bent into a hoop, and the visual root
    /// turns a full circle from that shoulder across the back to the far hip.
    /// The shape follows the grade-B roll clips (usable with care). The capsule,
    /// the velocity, and the camera stay put.
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
            // Diagonal from the lead shoulder toward the opposite hip, mostly along the ground.
            float x = shoulderLeft ? -0.62f : 0.62f;
            Vector3 a = new Vector3(x, -0.10f, 0.78f);
            a.Normalize();
            return a;
        }

        public static Vector3 Pivot(bool shoulderLeft)
        {
            // Lead-shoulder height. The orbit stays on the shoulder instead of the hip.
            float x = shoulderLeft ? -0.30f : 0.30f;
            return new Vector3(x, 1.15f, 0.12f);
        }

        /// <summary>
        /// Visual bank, degrees. The lead shoulder is down by the contact beat
        /// (spin ~104°, t=0.15 of 0.52 s), the turn continues across the back,
        /// and 360° stands the rise pose on its feet. It does not hold a headstand.
        /// </summary>
        public static float BankDegrees(float spin)
        {
            if (spin < 0f) spin = 0f;
            if (spin > 360f) spin = 360f;
            // Peak on the lead shoulder. Past this the head, not the back, becomes
            // the contact on this rig, so the turn comes back up onto the feet.
            const float halfPi = 1.5707963f;
            if (spin <= 110f)
                return 122f * Mathf.Sin(spin / 110f * halfPi);
            return 122f * Mathf.Sin((360f - spin) / 250f * halfPi);
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
            float side = shoulderLeft ? -1f : 1f;
            Figure f;
            f.Hip = RollPoint(0, spin, side);
            f.Head = RollPoint(1, spin, side);
            f.Shoulder = RollPoint(2, spin, side);
            f.Hand = RollPoint(3, spin, side);
            f.OffHand = RollPoint(4, spin, side);
            f.Foot = RollPoint(5, spin, side);
            f.Knee = RollPoint(6, spin, side);
            f.Chest = RollPoint(7, spin, side);
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

        /// <summary>Low diagonal roll. Hips stay under 0.9 m and never stack over the head.</summary>
        static Vector3 RollPoint(int id, float spin, float side)
        {
            if (spin < 0f) spin = 0f;
            if (spin > 360f) spin = 360f;
            float a, b, t;
            SpanKey(spin, out a, out b, out t);
            Vector3 p = LerpV(Key(id, a), Key(id, b), t);
            p.x *= side;
            return p;
        }

        static void SpanKey(float spin, out float a, out float b, out float t)
        {
            if (spin <= 0f) { a = 0f; b = 100f; t = 0f; return; }
            if (spin >= 360f) { a = 260f; b = 360f; t = 1f; return; }
            if (spin < 100f) { a = 0f; b = 100f; }
            else if (spin < 187.2f) { a = 100f; b = 187.2f; }
            else if (spin < 260f) { a = 187.2f; b = 260f; }
            else { a = 260f; b = 360f; }
            t = (spin - a) / (b - a);
        }

        static Vector3 LerpV(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        static Vector3 Key(int id, float at)
        {
            // 0 hip, 1 head, 2 shoulder, 3 hand, 4 off hand, 5 foot, 6 knee, 7 chest.
            if (at < 50f)
            {
                if (id == 0) return new Vector3(0f, 0.52f, 0f);
                if (id == 1) return new Vector3(0f, 0.95f, 0.12f);
                if (id == 2) return new Vector3(0.22f, 0.62f, 0.08f);
                if (id == 3) return new Vector3(0.16f, 0.02f, 0.28f);
                if (id == 4) return new Vector3(-0.16f, 0.04f, 0.22f);
                if (id == 5) return new Vector3(0.08f, 0.00f, -0.05f);
                if (id == 6) return new Vector3(0.10f, 0.22f, 0.08f);
                return new Vector3(0f, 0.72f, 0.06f);
            }
            if (at < 150f)
            {
                if (id == 0) return new Vector3(0f, 0.46f, 0.05f);
                if (id == 1) return new Vector3(-0.10f, 0.58f, 0.16f);
                if (id == 2) return new Vector3(0.24f, 0.22f, 0.10f);
                if (id == 3) return new Vector3(0.20f, 0.00f, 0.32f);
                if (id == 4) return new Vector3(-0.18f, 0.20f, 0.10f);
                if (id == 5) return new Vector3(0.06f, 0.16f, -0.12f);
                if (id == 6) return new Vector3(0.04f, 0.28f, -0.02f);
                return new Vector3(0.02f, 0.50f, 0.10f);
            }
            if (at < 220f)
            {
                if (id == 0) return new Vector3(-0.04f, 0.42f, -0.06f);
                if (id == 1) return new Vector3(-0.20f, 0.30f, 0.12f);
                if (id == 2) return new Vector3(0.18f, 0.00f, 0.04f);
                if (id == 3) return new Vector3(0.14f, 0.06f, 0.22f);
                if (id == 4) return new Vector3(-0.16f, 0.22f, 0.02f);
                if (id == 5) return new Vector3(0.02f, 0.32f, -0.16f);
                if (id == 6) return new Vector3(0.00f, 0.34f, -0.08f);
                return new Vector3(-0.02f, 0.36f, 0.04f);
            }
            if (at < 300f)
            {
                if (id == 0) return new Vector3(0.02f, 0.44f, 0.02f);
                if (id == 1) return new Vector3(-0.12f, 0.40f, 0.10f);
                if (id == 2) return new Vector3(0.16f, 0.18f, 0.02f);
                if (id == 3) return new Vector3(0.08f, 0.20f, 0.12f);
                if (id == 4) return new Vector3(-0.10f, 0.28f, 0.04f);
                if (id == 5) return new Vector3(0.10f, 0.04f, 0.16f);
                if (id == 6) return new Vector3(0.08f, 0.20f, 0.08f);
                return new Vector3(0f, 0.42f, 0.06f);
            }
            if (id == 0) return new Vector3(0f, 0.84f, 0f);
            if (id == 1) return new Vector3(0f, 1.35f, 0.04f);
            if (id == 2) return new Vector3(0.22f, 0.95f, 0f);
            if (id == 3) return new Vector3(0.28f, 0.70f, 0.10f);
            if (id == 4) return new Vector3(-0.26f, 0.72f, 0.05f);
            if (id == 5) return new Vector3(0.08f, 0.00f, 0.12f);
            if (id == 6) return new Vector3(0.10f, 0.42f, 0.06f);
            return new Vector3(0f, 1.05f, 0.02f);
        }

        public static void CurveLimits(out float maxHip, out float minHead, out float maxInvert)
        {
            maxHip = 0f;
            minHead = 99f;
            maxInvert = -90f;
            for (int i = 0; i <= 180; i++)
            {
                float spin = i * 2f;
                Figure f = PoseFigure(default, spin, false);
                float hip = f.Hip.y + f.Shift;
                float head = f.Head.y + f.Shift;
                if (hip > maxHip) maxHip = hip;
                if (head < minHead) minHead = head;
                float dx = f.Hip.x - f.Head.x;
                float dy = f.Hip.y - f.Head.y;
                float dz = f.Hip.z - f.Head.z;
                float horiz = Mathf.Sqrt(dx * dx + dz * dz);
                float elev;
                if (horiz < 0.0001f) elev = dy > 0f ? 90f : -90f;
                else elev = Mathf.Atan(dy / horiz) * Mathf.Rad2Deg;
                if (elev > maxInvert) maxInvert = elev;
            }
        }

        public static float FloorShift(VerbExitSample s, float spin, bool shoulderLeft)
        {
            // Stick-figure settle, plus the drop that seats the shoulder orbit on the floor.
            return PoseFigure(s, spin, shoulderLeft).Shift + MeshSeat(spin);
        }

        /// <summary>Extra root drop, metres. Seats the shoulder-orbit mesh on the floor.</summary>
        public static float MeshSeat(float spin)
        {
            if (spin < 0f) spin = 0f;
            if (spin > 360f) spin = 360f;
            // Measured at 30 fps on the banked mesh: the drop that puts the lowest vertex on the floor.
            float[] at = { 0f, 23f, 46f, 69f, 92f, 116f, 139f, 161f, 185f, 208f, 231f, 254f, 277f, 300f, 323f, 346f, 360f };
            float[] drop = { -0.56f, -0.43f, -0.42f, -0.59f, -0.78f, -0.85f, -0.84f, -0.82f, -0.76f, -0.67f, -0.58f, -0.43f, -0.29f, -0.20f, -0.06f, 0.03f, 0.04f };
            int i = 1;
            while (i < at.Length && at[i] < spin) i++;
            if (i >= at.Length) return drop[drop.Length - 1];
            float span = at[i] - at[i - 1];
            float t = span > 0.001f ? (spin - at[i - 1]) / span : 0f;
            return drop[i - 1] + (drop[i] - drop[i - 1]) * t;
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
            s.Hip = 28f;
            s.Spine = 22f;
            s.Head = -40f;
            s.ThighL = 70f;
            s.ThighR = 64f;
            s.KneeL = -100f;
            s.KneeR = -94f;
            s.ArmPitchL = -42f;
            s.ArmPitchR = -40f;
            s.ArmYawL = -28f;
            s.ArmYawR = 32f;
            s.ElbowL = -20f;
            s.ElbowR = -18f;
            s.ThighRollL = -50f;
            s.ThighRollR = 50f;
            s.SpineRoll = 8f;
            return s;
        }

        static VerbExitSample Sweep()
        {
            VerbExitSample s = default;
            s.Hip = 22f;
            s.Spine = 26f;
            s.Head = -36f;
            s.ThighL = 70f;
            s.ThighR = 64f;
            s.KneeL = -100f;
            s.KneeR = -94f;
            s.ArmPitchL = -28f;
            s.ArmPitchR = -36f;
            s.ArmYawL = -24f;
            s.ArmYawR = 28f;
            s.ElbowL = -20f;
            s.ElbowR = -18f;
            s.ThighRollL = -50f;
            s.ThighRollR = 50f;
            s.SpineRoll = 14f;
            return s;
        }

        static VerbExitSample HandDown()
        {
            VerbExitSample s = default;
            s.Hip = 14f;
            s.Spine = 18f;
            s.Head = -32f;
            s.ThighL = 66f;
            s.ThighR = 58f;
            s.KneeL = -96f;
            s.KneeR = -88f;
            s.ArmPitchL = -18f;
            s.ArmPitchR = -32f;
            s.ArmYawL = -20f;
            s.ArmYawR = 24f;
            s.ElbowL = -20f;
            s.ElbowR = -18f;
            s.ThighRollL = -48f;
            s.ThighRollR = 48f;
            s.SpineRoll = 20f;
            return s;
        }

        static VerbExitSample Shoulder()
        {
            VerbExitSample s = default;
            s.Hip = 8f;
            s.Spine = 6f;
            s.Head = -38f;
            s.ThighL = 46f;
            s.ThighR = 40f;
            s.KneeL = -78f;
            s.KneeR = -72f;
            s.ArmPitchL = -12f;
            s.ArmPitchR = -20f;
            s.ArmYawL = -16f;
            s.ArmYawR = 18f;
            s.ElbowL = -28f;
            s.ElbowR = -24f;
            s.ThighRollL = -36f;
            s.ThighRollR = 36f;
            s.SpineRoll = 28f;
            s.HipRoll = -10f;
            return s;
        }

        static VerbExitSample LegsOver()
        {
            VerbExitSample s = default;
            s.Hip = -4f;
            s.Spine = -6f;
            s.Head = -22f;
            s.ThighL = 48f;
            s.ThighR = 42f;
            s.ThighRollL = -36f;
            s.ThighRollR = 36f;
            s.KneeL = -70f;
            s.KneeR = -64f;
            s.ArmPitchL = -10f;
            s.ArmPitchR = -8f;
            s.ElbowL = -52f;
            s.ElbowR = -64f;
            s.ThighRollL = -36f;
            s.ThighRollR = 36f;
            s.SpineRoll = 18f;
            return s;
        }

        static VerbExitSample Plant()
        {
            VerbExitSample s = default;
            s.Hip = 12f;
            s.Spine = 8f;
            s.Head = -8f;
            s.ThighL = 38f;
            s.ThighR = 22f;
            s.KneeL = -32f;
            s.KneeR = -18f;
            s.ArmPitchL = -20f;
            s.ArmPitchR = -12f;
            s.ElbowL = -36f;
            s.ElbowR = -28f;
            s.SpineRoll = 8f;
            return s;
        }

        static VerbExitSample Rise()
        {
            VerbExitSample s = default;
            s.Hip = 6f;
            s.Spine = 2f;
            s.Head = -2f;
            s.ThighL = 24f;
            s.ThighR = 10f;
            s.KneeL = -18f;
            s.KneeR = -12f;
            s.ArmPitchL = -18f;
            s.ArmPitchR = -10f;
            s.ElbowL = -16f;
            s.ElbowR = -14f;
            return s;
        }

        /// <summary>Both palms on the ground, chin in, hips down.</summary>
        static VerbExitSample HandsDown()
        {
            VerbExitSample s = default;
            s.Hip = 28f;
            s.Spine = 36f;
            s.Head = -32f;
            s.ThighL = 72f;
            s.ThighR = 68f;
            s.ThighRollL = -24f;
            s.ThighRollR = 24f;
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
            if (over.ThighL < 36f || over.ThighL > 60f || over.RootSpin < 220f) return false;
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
            CurveLimits(out float maxHip, out float minHead, out float maxInvert);
            if (maxHip > 0.90f) return false;
            if (minHead <= 0f) return false;
            if (maxInvert > 60f) return false;
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
            CurveLimits(out float maxHip, out float minHead, out float maxInvert);
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
                + " shoulder=" + gap.ToString("0.00")
                + " hip=" + maxHip.ToString("0.00")
                + " head=" + minHead.ToString("0.00")
                + " invert=" + maxInvert.ToString("0.0");
        }
    }
}
