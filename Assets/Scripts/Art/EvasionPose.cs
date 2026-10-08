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
            public float ThighL, ThighR, KneeL, KneeR, YawL, YawR, ThighRollL, ThighRollR;
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
            if (dive.Hip < 45f) return false;
            if (dive.Head > -18f) return false;
            if (dive.ArmL > -20f || dive.Drop > -0.28f) return false;
            if (dive.Bank > 0.01f || dive.Bank < -0.01f) return false;
            Sample roll = MovePose(EvasionMoves.Kind.Dive, 1, EvasionMoves.DiveFlight + EvasionMoves.DiveRecover * 0.55f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Dive));
            if (roll.KneeL > -70f || roll.KneeR > -70f) return false;
            if (roll.Hip > 70f) return false;
            Sample late = MovePose(EvasionMoves.Kind.Dive, 1, EvasionMoves.DiveFlight + EvasionMoves.DiveRecover * 0.92f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Dive));
            if (late.KneeL > -45f || late.KneeR > -45f) return false;
            if (late.ElbowL > -25f || late.ElbowR > -25f) return false;
            if (late.ArmL > 24f || late.ArmR > 24f) return false;
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
            if (kind == EvasionMoves.Kind.Stutter || kind == EvasionMoves.Kind.Juke || kind == EvasionMoves.Kind.Spin || kind == EvasionMoves.Kind.Dive)
            {
                float up = u * 2.4f;
                if (up > 1f) up = 1f;
                float e = Smooth(up);
                float lift = kind == EvasionMoves.Kind.Dive ? end.Drop + 0.03f : end.Drop;
                outBlend.Drop = Mathf.Lerp(lift, 0f, e);
                outBlend.Seat = Mathf.Lerp(end.Seat, 0f, e);
            }
            if (kind == EvasionMoves.Kind.Spin)
            {
                // The exit arm blend walks an upper arm through the chest. The run arms are clear.
                outBlend.ArmL = run.ArmL;
                outBlend.ArmR = run.ArmR;
                outBlend.ArmYawL = run.ArmYawL;
                outBlend.ArmYawR = run.ArmYawR;
                outBlend.RollL = run.RollL;
                outBlend.RollR = run.RollR;
                outBlend.ElbowL = run.ElbowL;
                outBlend.ElbowR = run.ElbowR;
                outBlend.ElbowYawL = run.ElbowYawL;
                outBlend.ElbowYawR = run.ElbowYawR;
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
                Field(w, c, s.ThighRollL); Field(w, c, s.ThighRollR);
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
            if (sign > 0) sign = 1;
            else if (sign < 0) sign = -1;
            else sign = 0;
            if (local < 0f) local = 0f;
            if (local > move) local = move;
            Sample s;
            if (kind == EvasionMoves.Kind.Stutter) s = Stutter(local, move);
            else if (kind == EvasionMoves.Kind.Spin) s = Spin(local, move, sign);
            else if (kind == EvasionMoves.Kind.Juke) s = Juke(local, move);
            else s = Dive(local);
            if (sign < 0 && (kind == EvasionMoves.Kind.Juke || kind == EvasionMoves.Kind.Stutter))
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
            sample.FootL = Mathf.Lerp(4f, 14f, w);
            sample.FootR = Mathf.Lerp(14f, 4f, w);
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
            Sample s = new Sample();
            s.HipYaw = yaw;
            s.HeadYaw = lead * sign;
            s.Head = -12f;
            s.Hip = 16f;
            s.Spine = 8f;
            s.Lean = 16f * sign;
            s.Drop = -0.125f;
            // Left foot is the pivot. The knee keeps that sole down while the hips stay low.
            s.ThighL = 22f;
            s.ThighR = 10f;
            s.KneeL = SpinKnee(local);
            s.KneeR = -96f;
            s.FootL = 14f;
            s.FootR = 2f;
            // Inside arm folds across the chest. Outside arm stays close, elbow bent.
            s.ArmL = -6f;
            s.ArmR = 8f;
            s.ArmYawL = 0f;
            s.ArmYawR = -4f;
            s.RollL = 10f;
            s.RollR = 12f;
            s.ElbowL = -70f;
            s.ElbowR = -52f;
            s.ElbowYawL = 8f;
            s.ElbowYawR = -6f;
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
            }
            return s;
        }

        static Sample Juke(float local, float move)
        {
            // Head and trunk reach the new direction, then the hips follow one frame later.
            float headDone = 0.12f;
            float hipStart = 0.087f;
            float headU = Smooth(headDone > 0f ? local / headDone : 1f);
            float hipU = local <= hipStart ? 0f : Smooth((local - hipStart) / (move - hipStart));
            Sample s = new Sample();
            s.Drop = -0.1765f;
            s.Hip = Mathf.Lerp(18f, 22f, hipU);
            s.Spine = 6f;
            s.Head = -8f;
            s.Lean = Mathf.Lerp(12f, 18f, headU);
            s.SpineYaw = Mathf.Lerp(-18f, 16f, headU);
            s.HeadYaw = Mathf.Lerp(-24f, 18f, headU);
            s.HipYaw = Mathf.Lerp(0f, 14f, hipU);
            s.YawL = 6f;
            s.YawR = -26f;
            s.ThighRollL = -4f;
            s.ThighRollR = 24f;
            s.ThighL = -6f;
            s.ThighR = 4f;
            s.KneeL = -92f;
            s.KneeR = JukeKnee(local);
            s.FootL = 2f;
            s.FootR = 14f;
            s.ArmL = Mathf.Lerp(8f, -12f, headU);
            s.ArmR = Mathf.Lerp(-4f, -18f, headU);
            s.ElbowL = -30f;
            s.ElbowR = -38f;
            return s;
        }

        static Sample Dive(float local)
        {
            float flight = EvasionMoves.DiveFlight;
            float handsAt = flight + 0.02f;
            float tuckAt = flight + 0.12f;
            float shoulderAt = flight + 0.24f;
            float bridgeAt = flight + 0.35f;
            float hipAt = flight + 0.42f;
            float crouchAt = flight + 0.55f;
            Sample reach = DiveReach();
            Sample hands = DiveHands();
            Sample tuck = DiveTuck();
            Sample shoulder = DiveShoulder();
            Sample bridge = DiveBridge();
            Sample hip = DiveHip();
            Sample crouch = DiveCrouch();
            if (local <= handsAt)
                return Lerp(reach, hands, Smooth(handsAt > 0f ? local / handsAt : 1f));
            if (local <= tuckAt)
                return Lerp(hands, tuck, Smooth((local - handsAt) / (tuckAt - handsAt)));
            if (local <= shoulderAt)
                return Lerp(tuck, shoulder, Smooth((local - tuckAt) / (shoulderAt - tuckAt)));
            if (local <= bridgeAt)
                return Lerp(shoulder, bridge, Smooth((local - shoulderAt) / (bridgeAt - shoulderAt)));
            if (local <= hipAt)
                return Lerp(bridge, hip, Smooth((local - bridgeAt) / (hipAt - bridgeAt)));
            if (local <= crouchAt)
                return Lerp(hip, crouch, Smooth((local - hipAt) / (crouchAt - hipAt)));
            return crouch;
        }

        static Sample DiveReach()
        {
            Sample s = new Sample();
            s.Drop = -0.16f;
            s.Hip = 36f;
            s.Spine = 4f;
            s.Head = -12f;
            s.Lean = 4f;
            s.ThighL = 16f;
            s.ThighR = 12f;
            s.KneeL = -42f;
            s.KneeR = -36f;
            s.FootL = 10f;
            s.FootR = 8f;
            s.ArmL = -22f;
            s.ArmR = -22f;
            s.ElbowL = -18f;
            s.ElbowR = -18f;
            return s;
        }

        /// <summary>Forward reach. Hands meet the floor and the chin is in. The chest stays up.</summary>
        static Sample DiveHands()
        {
            Sample s = new Sample();
            s.Drop = -0.470f;
            s.Hip = 58f;
            s.Spine = 10f;
            s.Head = -34f;
            s.Lean = 8f;
            s.ThighL = 30f;
            s.ThighR = 22f;
            s.KneeL = -72f;
            s.KneeR = -64f;
            s.FootL = 6f;
            s.FootR = 4f;
            s.ArmL = -36f;
            s.ArmR = -36f;
            s.ElbowL = -16f;
            s.ElbowR = -16f;
            return s;
        }

        /// <summary>Arms leave the floor before the shoulder takes the contact.</summary>
        static Sample DiveTuck()
        {
            Sample s = new Sample();
            s.Drop = -0.46f;
            s.Hip = 78f;
            s.Spine = 4f;
            s.Head = -24f;
            s.Lean = 22f;
            s.ThighL = 12f;
            s.ThighR = 8f;
            s.KneeL = -140f;
            s.KneeR = -132f;
            s.FootL = 2f;
            s.FootR = 0f;
            s.ArmL = 70f;
            s.ArmR = 60f;
            s.ElbowL = 20f;
            s.ElbowR = 16f;
            return s;
        }

        /// <summary>Right shoulder on the floor. The hands stay up off it.</summary>
        static Sample DiveShoulder()
        {
            Sample s = new Sample();
            s.Drop = -0.808f;
            s.Hip = 95f;
            s.Spine = 2f;
            s.Head = -16f;
            s.Lean = 30f;
            s.ThighL = 10f;
            s.ThighR = 6f;
            s.KneeL = -150f;
            s.KneeR = -142f;
            s.FootL = 2f;
            s.FootR = 0f;
            s.ArmL = 70f;
            s.ArmR = 60f;
            s.ElbowL = 20f;
            s.ElbowR = 16f;
            return s;
        }

        /// <summary>Between the shoulder and the opposite hip. The drop keeps the thigh off the floor.</summary>
        static Sample DiveBridge()
        {
            Sample s = new Sample();
            s.Drop = -0.510f;
            s.Hip = 42f;
            s.Spine = 2f;
            s.Head = -12f;
            s.Lean = -8f;
            s.ThighL = 30f;
            s.ThighR = 17f;
            s.KneeL = -154f;
            s.KneeR = -146f;
            s.FootL = 2f;
            s.FootR = 2f;
            s.ArmL = 22f;
            s.ArmR = 18f;
            s.ElbowL = -32f;
            s.ElbowR = -27f;
            return s;
        }

        /// <summary>Opposite hip. Both knees stay tucked so the thigh, not the foot, is down.</summary>
        static Sample DiveHip()
        {
            Sample s = new Sample();
            s.Drop = -0.481f;
            s.Hip = 26f;
            s.Spine = 2f;
            s.Head = -10f;
            s.Lean = -20f;
            s.ThighL = 36f;
            s.ThighR = 20f;
            s.KneeL = -155f;
            s.KneeR = -148f;
            s.FootL = 4f;
            s.FootR = 4f;
            s.ArmL = 8f;
            s.ArmR = 6f;
            s.ElbowL = -48f;
            s.ElbowR = -40f;
            return s;
        }

        static Sample DiveCrouch()
        {
            Sample s = new Sample();
            s.Drop = -0.30f;
            s.Hip = 28f;
            s.Spine = 8f;
            s.Head = -8f;
            s.Lean = 3f;
            s.ThighL = 16f;
            s.ThighR = 10f;
            s.KneeL = -64f;
            s.KneeR = -58f;
            s.FootL = 14f;
            s.FootR = 12f;
            s.ArmL = 4f;
            s.ArmR = 2f;
            s.ElbowL = -38f;
            s.ElbowR = -34f;
            return s;
        }

        static float SpinKnee(float local)
        {
            // Second measure. The pivot knee keeps the sole down at a fixed hip height.
            float[] at = { 0f, 0.033f, 0.067f, 0.100f, 0.133f, 0.167f, 0.200f, 0.233f, 0.267f, 0.300f, 0.350f };
            float[] knee = { -40.9f, -51.6f, -63.5f, -69.2f, -67.2f, -59.5f, -50.8f, -43.8f, -38.7f, -36.0f, -36.8f };
            return Table(local, at, knee);
        }

        static float JukeKnee(float local)
        {
            // Outside knee. It lifts the sole as the trunk leans in, so the hip height can stay put.
            float[] at = { 0f, 0.033f, 0.067f, 0.100f, 0.133f, 0.167f, 0.200f, 0.220f };
            float[] knee = { -43.9f, -45.1f, -47.9f, -50.0f, -50.6f, -50.6f, -50.5f, -50.4f };
            return Table(local, at, knee);
        }

        static float Table(float local, float[] at, float[] value)
        {
            if (local <= at[0]) return value[0];
            int last = at.Length - 1;
            if (local >= at[last]) return value[last];
            int i = 1;
            while (i < last && at[i] < local) i++;
            float span = at[i] - at[i - 1];
            float t = span > 0.0001f ? (local - at[i - 1]) / span : 0f;
            return value[i - 1] + (value[i] - value[i - 1]) * t;
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
            float rollThigh = s.ThighRollL;
            s.ThighRollL = -s.ThighRollR;
            s.ThighR = thigh;
            s.KneeR = knee;
            s.FootR = foot;
            s.YawR = -yaw;
            s.ThighRollR = -rollThigh;
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
            s.ThighRollL = Mathf.Lerp(a.ThighRollL, b.ThighRollL, w);
            s.ThighRollR = Mathf.Lerp(a.ThighRollR, b.ThighRollR, w);
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
