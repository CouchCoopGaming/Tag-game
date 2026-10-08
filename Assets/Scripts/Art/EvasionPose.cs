using System.Globalization;
using System.IO;
using UnityEngine;
using Tag.Gameplay;

namespace Tag.Art
{
    /// <summary>
    /// In-place Hier clips for the four evasion moves. The capsule stays on the
    /// motor. Sample.Drop is a visual pelvis offset, not root motion.
    /// Spin and juke have a left and a right. Stutter shoulders stay square.
    /// </summary>
    public static class EvasionPose
    {
        public const bool RootMotion = false;
        public const float Drop = 0f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR, YawL, YawR;
            public float ArmL, ArmR, ArmYawL, ArmYawR, RollL, RollR;
            public float ElbowL, ElbowR, ElbowYawL, ElbowYawR;
            public float Hip, Spine, Head, Lean, HipYaw, SpineYaw, HeadYaw;
            public float FootL, FootR, Drop;
            public float Bank, Seat;
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (EvasionMoves.Duration(EvasionMoves.Kind.Spin) < 0.5f) return false;
            Sample spinR = MovePose(EvasionMoves.Kind.Spin, 1, EvasionMoves.SpinSeconds * 0.15f, EvasionMoves.SpinSeconds);
            Sample spinL = MovePose(EvasionMoves.Kind.Spin, -1, EvasionMoves.SpinSeconds * 0.15f, EvasionMoves.SpinSeconds);
            if (spinR.HeadYaw < 12f || spinL.HeadYaw > -12f) return false;
            if (spinR.HipYaw < 40f || spinL.HipYaw > -40f) return false;
            if (spinR.Drop > -0.05f || spinL.Lean > -8f) return false;
            if (spinR.ElbowL > -25f) return false;
            Sample juke = MovePose(EvasionMoves.Kind.Juke, 1, 0.02f, EvasionMoves.JukeSeconds);
            if (juke.HeadYaw >= 0f || juke.SpineYaw >= 0f) return false;
            if (juke.KneeR > -40f || juke.Drop > -0.12f) return false;
            Sample stutter = MovePose(EvasionMoves.Kind.Stutter, 1, 0.08f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Stutter));
            if (Mathf.Abs(stutter.HipYaw) > 0.01f || Mathf.Abs(stutter.SpineYaw) > 0.01f) return false;
            if (stutter.KneeL > -35f || stutter.KneeR > -35f) return false;
            if (stutter.Drop > -0.14f || stutter.Drop < -0.21f) return false;
            if (stutter.Hip < 10f) return false;
            Sample dive = MovePose(EvasionMoves.Kind.Dive, 1, EvasionMoves.DiveFlight * 0.9f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Dive));
            if (dive.Hip < 60f) return false;
            if (dive.Drop > -0.35f) return false;
            if (dive.Bank > 1f) return false;
            Sample roll = MovePose(EvasionMoves.Kind.Dive, 1, EvasionMoves.DiveFlight + EvasionMoves.DiveRecover * 0.55f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Dive));
            if (roll.Bank < 70f) return false;
            Sample exit = At(EvasionMoves.Kind.Stutter, 1, EvasionMoves.Duration(EvasionMoves.Kind.Stutter));
            if (Mathf.Abs(exit.Drop) > 0.02f) return false;
            return true;
        }

        public static void WriteKeys(TextWriter w)
        {
            CultureInfo c = CultureInfo.InvariantCulture;
            w.WriteLine("# evasion pose keys dt=1/30 source=Tag.Art.EvasionPose rootMotion=0");
            WriteClip(w, c, "stutter", EvasionMoves.Kind.Stutter, 1);
            WriteClip(w, c, "spinL", EvasionMoves.Kind.Spin, -1);
            WriteClip(w, c, "spinR", EvasionMoves.Kind.Spin, 1);
            WriteClip(w, c, "jukeL", EvasionMoves.Kind.Juke, -1);
            WriteClip(w, c, "jukeR", EvasionMoves.Kind.Juke, 1);
            WriteClip(w, c, "dive", EvasionMoves.Kind.Dive, 1);
        }

        public static Sample At(EvasionMoves.Kind kind, int sign, float time)
        {
            float entry = EvasionMoves.EntrySeconds;
            float exit = EvasionMoves.ExitSeconds;
            float move = EvasionMoves.MoveSeconds(kind);
            float total = entry + move + exit;
            if (time < 0f) time = 0f;
            if (time > total) time = total;
            bool reach = kind == EvasionMoves.Kind.Dive;
            Sample run = Run(time * 8f, reach);
            if (time <= entry)
            {
                Sample into = MovePose(kind, sign, 0f, move);
                float w = Smooth(entry > 0f ? time / entry : 1f);
                Sample intoBlend = Lerp(run, into, w);
                if (kind == EvasionMoves.Kind.Juke)
                    intoBlend.Drop = Mathf.Lerp(0f, into.Drop, w * w);
                return intoBlend;
            }
            if (time <= entry + move)
                return MovePose(kind, sign, time - entry, move);
            Sample end = MovePose(kind, sign, move, move);
            float u = exit > 0f ? (time - entry - move) / exit : 1f;
            Sample outBlend = Lerp(end, run, Smooth(u));
            if (kind == EvasionMoves.Kind.Stutter || kind == EvasionMoves.Kind.Juke || kind == EvasionMoves.Kind.Dive)
            {
                float up = u * 2.4f;
                if (up > 1f) up = 1f;
                float e = Smooth(up);
                float lift = kind == EvasionMoves.Kind.Dive ? end.Drop + 0.03f : end.Drop;
                outBlend.Drop = Mathf.Lerp(lift, 0f, e);
                outBlend.Seat = Mathf.Lerp(end.Seat, 0f, e);
            }
            return outBlend;
        }

        static void WriteClip(TextWriter w, CultureInfo c, string name, EvasionMoves.Kind kind, int sign)
        {
            float dur = EvasionMoves.Duration(kind);
            const float dt = 1f / 30f;
            int n = (int)(dur / dt + 0.5f);
            for (int i = 0; i <= n; i++)
            {
                float t = i * dt;
                if (t > dur) t = dur;
                Sample s = At(kind, sign, t);
                w.Write(name);
                w.Write('\t');
                w.Write(t.ToString("0.000", c));
                Field(w, c, s.ThighL); Field(w, c, s.ThighR); Field(w, c, s.KneeL); Field(w, c, s.KneeR);
                Field(w, c, s.YawL); Field(w, c, s.YawR);
                Field(w, c, s.ArmL); Field(w, c, s.ArmR); Field(w, c, s.ArmYawL); Field(w, c, s.ArmYawR);
                Field(w, c, s.RollL); Field(w, c, s.RollR);
                Field(w, c, s.ElbowL); Field(w, c, s.ElbowR);
                Field(w, c, s.Hip); Field(w, c, s.Spine); Field(w, c, s.Head); Field(w, c, s.Lean);
                Field(w, c, s.HipYaw); Field(w, c, s.SpineYaw);
                Field(w, c, s.FootL); Field(w, c, s.FootR); Field(w, c, s.Drop);
                Field(w, c, s.ElbowYawL); Field(w, c, s.ElbowYawR);
                Field(w, c, s.HeadYaw);
                Field(w, c, s.Bank); Field(w, c, s.Seat);
                w.WriteLine();
                if (t >= dur) break;
            }
        }

        static Sample Run(float phase, bool reach)
        {
            float s = Mathf.Sin(phase);
            Sample sample = new Sample();
            sample.ThighL = 18f * s;
            sample.ThighR = -18f * s;
            sample.KneeL = -22f - 8f * Mathf.Abs(s);
            sample.KneeR = sample.KneeL;
            sample.FootL = s > 0.2f ? 8f : 0f;
            sample.FootR = s < -0.2f ? 8f : 0f;
            if (reach)
            {
                sample.ArmL = 8f;
                sample.ArmR = 8f;
                sample.ElbowL = -20f;
                sample.ElbowR = -20f;
                sample.KneeL = -36f;
                sample.KneeR = -32f;
            }
            else
            {
                sample.ArmL = -22f * s;
                sample.ArmR = 22f * s;
                sample.ElbowL = -22f;
                sample.ElbowR = -22f;
            }
            sample.Head = -4f;
            sample.Hip = 8f;
            sample.Drop = 0f;
            return sample;
        }

        static Sample MovePose(EvasionMoves.Kind kind, int sign, float local, float move)
        {
            if (sign >= 0) sign = 1;
            else sign = -1;
            if (local < 0f) local = 0f;
            if (local > move) local = move;
            Sample s;
            if (kind == EvasionMoves.Kind.Stutter) s = Stutter(local, move);
            else if (kind == EvasionMoves.Kind.Spin) s = Spin(local, move, sign);
            else if (kind == EvasionMoves.Kind.Juke) s = Juke(local, move);
            else s = Dive(local);
            if (sign < 0 && (kind == EvasionMoves.Kind.Juke))
                s = Mirror(s);
            return s;
        }

        static Sample Stutter(float local, float move)
        {
            float brake = EvasionMoves.StutterBrake;
            float span = move - brake;
            if (span < 0.001f) span = 0.001f;
            float burst = local <= brake ? 0f : Smooth((local - brake) / span);
            // Alternating chops. The wave holds each plant, and the switch is short.
            // Drop seats the plant sole; it does not chase the airborne foot.
            float s = Mathf.Sin(local * 42f);
            float side = s / 0.25f;
            if (side > 1f) side = 1f;
            if (side < -1f) side = -1f;
            float w = Smooth((side + 1f) * 0.5f);
            Sample sample = new Sample();
            sample.Drop = Mathf.Lerp(-0.152f, -0.218f, burst);
            sample.Hip = Mathf.Lerp(18f, 26f, burst);
            sample.Head = Mathf.Lerp(-6f, -12f, burst);
            sample.ThighL = 6f + 6f * side;
            sample.ThighR = 6f - 6f * side;
            sample.KneeL = Mathf.Lerp(-68f, -50f, w);
            sample.KneeR = Mathf.Lerp(-50f, -68f, w);
            sample.FootL = Mathf.Lerp(4f, 12f, w);
            sample.FootR = Mathf.Lerp(12f, 4f, w);
            sample.ArmL = -20f * side;
            sample.ArmR = 20f * side;
            sample.ElbowL = -30f;
            sample.ElbowR = -30f;
            return sample;
        }

        static Sample Spin(float local, float move, int sign)
        {
            float u = move > 0f ? local / move : 1f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float yaw = u >= 0.999f ? 360f * sign : 360f * u * sign;
            float lead = u < 0.40f ? 36f * (1f - u / 0.40f) : 0f;
            float outDrive = u < 0.82f ? 0f : Smooth((u - 0.82f) / 0.18f);
            Sample s = new Sample();
            s.HipYaw = yaw;
            s.HeadYaw = lead * sign;
            s.Head = -8f;
            s.Hip = 8f;
            s.Lean = Mathf.Lerp(13f, 4f, outDrive) * sign;
            s.Drop = SpinDrop(local);
            float side = Mathf.Abs(Mathf.Sin(yaw * sign * 0.5f * 0.0174533f));
            // Left foot is the pivot. The free foot stays bent so only the pivot reaches.
            s.ThighL = Mathf.Lerp(14f, 8f, outDrive);
            s.ThighR = Mathf.Lerp(-4f, -8f, outDrive);
            s.KneeL = Mathf.Lerp(-44f, -36f, outDrive);
            s.KneeR = Mathf.Lerp(-82f - 8f * side, -48f, outDrive);
            s.FootL = 10f;
            s.FootR = 2f;
            s.ArmL = 6f;
            s.ArmR = 6f;
            s.RollL = Mathf.Lerp(-42f, -8f, outDrive);
            s.RollR = Mathf.Lerp(42f, 8f, outDrive);
            s.ElbowL = Mathf.Lerp(-64f, -28f, outDrive);
            s.ElbowR = Mathf.Lerp(-64f, -28f, outDrive);
            if (sign < 0)
            {
                float thigh = s.ThighL;
                float knee = s.KneeL;
                float foot = s.FootL;
                s.ThighL = s.ThighR;
                s.KneeL = s.KneeR;
                s.FootL = s.FootR;
                s.ThighR = thigh;
                s.KneeR = knee;
                s.FootR = foot;
                float arm = s.ArmL;
                float armYaw = s.ArmYawL;
                float roll = s.RollL;
                float elbow = s.ElbowL;
                s.ArmL = s.ArmR;
                s.ArmYawL = -s.ArmYawR;
                s.RollL = -s.RollR;
                s.ElbowL = s.ElbowR;
                s.ArmR = arm;
                s.ArmYawR = -armYaw;
                s.RollR = -roll;
                s.ElbowR = elbow;
            }
            return s;
        }

        static Sample Juke(float local, float move)
        {
            float u = move > 0f ? local / move : 1f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float fake = 1f - Smooth(u);
            float commit = Smooth(u);
            Sample s = new Sample();
            s.Drop = JukeDrop(local);
            s.Hip = Mathf.Lerp(14f, 20f, commit);
            s.Lean = Mathf.Lerp(-10f, 14f, commit);
            s.SpineYaw = -20f * fake + 12f * commit;
            s.HeadYaw = -28f * fake + 12f * commit;
            s.Head = -6f;
            s.YawL = 12f;
            s.YawR = -16f;
            s.ThighL = 4f;
            s.ThighR = 10f;
            s.KneeL = -72f;
            s.KneeR = -48f;
            s.FootL = 2f;
            s.FootR = 12f;
            s.ArmL = 14f * fake - 4f * commit;
            s.ArmR = -6f * fake - 14f * commit;
            s.ElbowL = -24f;
            s.ElbowR = -30f;
            return s;
        }

        static Sample Dive(float local)
        {
            float flight = EvasionMoves.DiveFlight;
            Sample push = new Sample();
            push.Drop = -0.10f;
            push.Hip = 34f;
            push.Head = -8f;
            push.ThighL = 18f;
            push.ThighR = 10f;
            push.KneeL = -46f;
            push.KneeR = -40f;
            push.FootL = 10f;
            push.FootR = 6f;
            push.ArmL = 8f;
            push.ArmR = 8f;
            push.ElbowL = -18f;
            push.ElbowR = -18f;
            Sample palm = Palm();
            if (local <= flight)
            {
                float u = Smooth(flight > 0f ? local / flight : 1f);
                return Lerp(push, palm, u);
            }
            float recover = EvasionMoves.DiveRecover;
            float ru = recover > 0f ? (local - flight) / recover : 1f;
            if (ru < 0f) ru = 0f;
            if (ru > 1f) ru = 1f;
            // Hands stay on the floor, then the shoulder roll, then the feet.
            if (ru < 0.14f)
                return palm;
            float rollU = (ru - 0.14f) / 0.86f;
            if (rollU < 0f) rollU = 0f;
            if (rollU > 1f) rollU = 1f;
            return RollAt(rollU);
        }

        /// <summary>Low dive. The fist's flat face is on the floor. An open palm turns the upper arm through the chest.</summary>
        static Sample Palm()
        {
            Sample s = new Sample();
            s.Drop = -0.628f;
            s.Hip = 74f;
            s.Spine = 6f;
            s.Head = -12f;
            s.ThighL = 14f;
            s.ThighR = 8f;
            s.KneeL = -28f;
            s.KneeR = -22f;
            s.FootL = 8f;
            s.FootR = 4f;
            SetArms(ref s, -15f, 0f, -8f);
            return s;
        }

        /// <summary>
        /// Landing-roll keys from the roll lane, banked on the right shoulder.
        /// Peak bank is 135°, which puts the shoulder down and keeps the head up.
        /// </summary>
        static Sample RollAt(float u)
        {
            Sample tuck = RollBones(26f, 18f, -26f, 12f, 8f, -74f, -68f);
            Sample sweep = RollBones(16f, 18f, -22f, 10f, 6f, -72f, -66f);
            Sample hand = RollBones(12f, 16f, -22f, 10f, 6f, -70f, -64f);
            Sample shoulder = RollBones(10f, 16f, -24f, 16f, 12f, -72f, -66f);
            Sample legs = RollBones(-4f, -6f, -22f, 22f, 16f, -60f, -54f);
            Sample plant = RollBones(12f, 8f, -8f, 20f, 12f, -32f, -18f);
            Sample rise = RollBones(6f, 2f, -2f, 18f, 8f, -20f, -14f);
            Sample s = Piece(u, tuck, sweep, hand, shoulder, legs, plant, rise);
            s.Bank = BankDegrees(u * 360f);
            s.Drop = -0.15f;
            s.Seat = RollSeat(u);
            return s;
        }

        static Sample RollBones(float hip, float spine, float head, float thL, float thR, float knL, float knR)
        {
            Sample s = new Sample();
            s.Hip = hip;
            s.Spine = spine;
            s.Head = head;
            s.ThighL = thL;
            s.ThighR = thR;
            s.KneeL = knL;
            s.KneeR = knR;
            s.FootL = 6f;
            s.FootR = 4f;
            // Same tuck that stays off the chest on the spin. The bank does the roll.
            s.ArmL = 6f;
            s.ArmR = 6f;
            s.RollL = -40f;
            s.RollR = 40f;
            s.ElbowL = -62f;
            s.ElbowR = -62f;
            return s;
        }

        static Sample Piece(float u, Sample a, Sample b, Sample c, Sample d, Sample e, Sample f, Sample g)
        {
            if (u < 0.18f) return Lerp(a, b, Smooth(u / 0.18f));
            if (u < 0.36f) return Lerp(b, c, Smooth((u - 0.18f) / 0.18f));
            if (u < 0.52f) return Lerp(c, d, Smooth((u - 0.36f) / 0.16f));
            if (u < 0.68f) return Lerp(d, e, Smooth((u - 0.52f) / 0.16f));
            if (u < 0.84f) return Lerp(e, f, Smooth((u - 0.68f) / 0.16f));
            return Lerp(f, g, Smooth((u - 0.84f) / 0.16f));
        }

        /// <summary>Same curve as the landing roll. 135° is the shoulder. Past that the head becomes the contact.</summary>
        public static float BankDegrees(float spin)
        {
            if (spin < 0f) spin = 0f;
            if (spin > 360f) spin = 360f;
            const float halfPi = 1.5707963f;
            if (spin <= 110f)
                return 135f * Mathf.Sin(spin / 110f * halfPi);
            return 135f * Mathf.Sin((360f - spin) / 250f * halfPi);
        }

        public static Vector3 RollAxis()
        {
            Vector3 a = new Vector3(0.62f, -0.10f, 0.78f);
            float m = Mathf.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
            return new Vector3(a.x / m, a.y / m, a.z / m);
        }

        public static Vector3 RollPivot()
        {
            return new Vector3(0.30f, 1.15f, 0.12f);
        }

        /// <summary>Where the visual origin moves when it orbits the lead shoulder. Not capsule motion.</summary>
        public static Vector3 OrbitDelta(float degrees)
        {
            Vector3 pivot = RollPivot();
            Vector3 axis = RollAxis();
            Vector3 v = new Vector3(-pivot.x, -pivot.y, -pivot.z);
            float rad = degrees * 0.017453292f;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            float dot = axis.x * v.x + axis.y * v.y + axis.z * v.z;
            float cx = axis.y * v.z - axis.z * v.y;
            float cy = axis.z * v.x - axis.x * v.z;
            float cz = axis.x * v.y - axis.y * v.x;
            float k = 1f - c;
            return new Vector3(
                pivot.x + v.x * c + cx * s + axis.x * dot * k,
                pivot.y + v.y * c + cy * s + axis.y * dot * k,
                pivot.z + v.z * c + cz * s + axis.z * dot * k);
        }

        static float RollSeat(float u)
        {
            // Measured on the banked mesh: extra root Y that puts the lowest vertex on the floor.
            float[] at = { 0.061f, 0.127f, 0.191f, 0.255f, 0.321f, 0.385f, 0.449f, 0.515f, 0.579f, 0.643f, 0.709f, 0.772f, 0.836f, 0.902f, 0.966f, 1f };
            float[] seat = { -0.228f, -0.697f, -0.932f, -0.971f, -0.975f, -0.967f, -0.952f, -0.932f, -0.893f, -0.843f, -0.839f, -0.580f, -0.248f, -0.009f, 0.123f, 0.140f };
            return Table(u, at, seat);
        }

        static float SpinDrop(float local)
        {
            // Pivot sole seated to 0.3 cm through the turn. Lean makes the gap move with yaw.
            float[] at = { 0f, 0.033f, 0.067f, 0.100f, 0.133f, 0.167f, 0.200f, 0.233f, 0.267f, 0.300f, 0.333f, 0.350f };
            float[] drop = { -0.110f, -0.083f, -0.054f, -0.036f, -0.034f, -0.048f, -0.074f, -0.103f, -0.124f, -0.123f, -0.073f, -0.050f };
            return Table(local, at, drop);
        }

        static float JukeDrop(float local)
        {
            // Outside sole seated to 0.3 cm. The inside foot stays up.
            float[] at = { 0f, 0.033f, 0.067f, 0.100f, 0.133f, 0.167f, 0.200f, 0.220f };
            float[] drop = { -0.166f, -0.161f, -0.151f, -0.143f, -0.144f, -0.151f, -0.159f, -0.163f };
            return Table(local, at, drop);
        }

        static float Table(float local, float[] at, float[] drop)
        {
            if (local <= at[0]) return drop[0];
            int last = at.Length - 1;
            if (local >= at[last]) return drop[last];
            int i = 1;
            while (i < last && at[i] < local) i++;
            float span = at[i] - at[i - 1];
            float t = span > 0.0001f ? (local - at[i - 1]) / span : 0f;
            return drop[i - 1] + (drop[i] - drop[i - 1]) * t;
        }

        static void SetArms(ref Sample s, float pitch, float yaw, float elbow)
        {
            s.ArmL = pitch;
            s.ArmR = pitch;
            s.ArmYawL = yaw;
            s.ArmYawR = -yaw;
            s.ElbowL = elbow;
            s.ElbowR = elbow;
        }

        static Sample Mirror(Sample s)
        {
            float thigh = s.ThighL;
            float knee = s.KneeL;
            float foot = s.FootL;
            float yaw = s.YawL;
            s.ThighL = s.ThighR;
            s.KneeL = s.KneeR;
            s.FootL = s.FootR;
            s.YawL = -s.YawR;
            s.ThighR = thigh;
            s.KneeR = knee;
            s.FootR = foot;
            s.YawR = -yaw;
            float arm = s.ArmL;
            float armYaw = s.ArmYawL;
            float roll = s.RollL;
            float elbow = s.ElbowL;
            float elbowYaw = s.ElbowYawL;
            s.ArmL = s.ArmR;
            s.ArmYawL = -s.ArmYawR;
            s.RollL = -s.RollR;
            s.ElbowL = s.ElbowR;
            s.ElbowYawL = -s.ElbowYawR;
            s.ArmR = arm;
            s.ArmYawR = -armYaw;
            s.RollR = -roll;
            s.ElbowR = elbow;
            s.ElbowYawR = -elbowYaw;
            s.HipYaw = -s.HipYaw;
            s.SpineYaw = -s.SpineYaw;
            s.HeadYaw = -s.HeadYaw;
            s.Lean = -s.Lean;
            return s;
        }

        static Sample Lerp(Sample a, Sample b, float w)
        {
            Sample s = new Sample();
            s.ThighL = Mathf.Lerp(a.ThighL, b.ThighL, w);
            s.ThighR = Mathf.Lerp(a.ThighR, b.ThighR, w);
            s.KneeL = Mathf.Lerp(a.KneeL, b.KneeL, w);
            s.KneeR = Mathf.Lerp(a.KneeR, b.KneeR, w);
            s.YawL = Mathf.Lerp(a.YawL, b.YawL, w);
            s.YawR = Mathf.Lerp(a.YawR, b.YawR, w);
            s.ArmL = Mathf.Lerp(a.ArmL, b.ArmL, w);
            s.ArmR = Mathf.Lerp(a.ArmR, b.ArmR, w);
            s.ArmYawL = Mathf.Lerp(a.ArmYawL, b.ArmYawL, w);
            s.ArmYawR = Mathf.Lerp(a.ArmYawR, b.ArmYawR, w);
            s.RollL = Mathf.Lerp(a.RollL, b.RollL, w);
            s.RollR = Mathf.Lerp(a.RollR, b.RollR, w);
            s.ElbowL = Mathf.Lerp(a.ElbowL, b.ElbowL, w);
            s.ElbowR = Mathf.Lerp(a.ElbowR, b.ElbowR, w);
            s.ElbowYawL = Mathf.Lerp(a.ElbowYawL, b.ElbowYawL, w);
            s.ElbowYawR = Mathf.Lerp(a.ElbowYawR, b.ElbowYawR, w);
            s.Hip = Mathf.Lerp(a.Hip, b.Hip, w);
            s.Spine = Mathf.Lerp(a.Spine, b.Spine, w);
            s.Head = Mathf.Lerp(a.Head, b.Head, w);
            s.Lean = Mathf.Lerp(a.Lean, b.Lean, w);
            s.HipYaw = LerpYaw(a.HipYaw, b.HipYaw, w);
            s.SpineYaw = LerpYaw(a.SpineYaw, b.SpineYaw, w);
            s.HeadYaw = LerpYaw(a.HeadYaw, b.HeadYaw, w);
            s.FootL = Mathf.Lerp(a.FootL, b.FootL, w);
            s.FootR = Mathf.Lerp(a.FootR, b.FootR, w);
            s.Drop = Mathf.Lerp(a.Drop, b.Drop, w);
            s.Bank = Mathf.Lerp(a.Bank, b.Bank, w);
            s.Seat = Mathf.Lerp(a.Seat, b.Seat, w);
            return s;
        }

        static float LerpYaw(float a, float b, float w)
        {
            float d = b - a;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return a + d * w;
        }

        static float Smooth(float u)
        {
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return u * u * (3f - 2f * u);
        }

        static void Field(TextWriter w, CultureInfo c, float v)
        {
            w.Write('\t');
            w.Write(v.ToString("0.000", c));
        }
    }
}
