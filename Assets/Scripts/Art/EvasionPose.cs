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
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (EvasionMoves.Duration(EvasionMoves.Kind.Spin) < 0.5f) return false;
            Sample spinR = MovePose(EvasionMoves.Kind.Spin, 1, EvasionMoves.SpinSeconds * 0.15f, EvasionMoves.SpinSeconds);
            Sample spinL = MovePose(EvasionMoves.Kind.Spin, -1, EvasionMoves.SpinSeconds * 0.15f, EvasionMoves.SpinSeconds);
            if (spinR.HeadYaw < 12f || spinL.HeadYaw > -12f) return false;
            if (spinR.HipYaw < 40f || spinL.HipYaw > -40f) return false;
            if (spinR.Drop > -0.08f || spinL.Lean > -8f) return false;
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
            if (dive.Hip < 70f) return false;
            if (dive.Drop > -0.25f) return false;
            if (dive.ArmL > -40f || dive.ArmR > -40f) return false;
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
                return Lerp(run, into, Smooth(entry > 0f ? time / entry : 1f));
            }
            if (time <= entry + move)
                return MovePose(kind, sign, time - entry, move);
            Sample end = MovePose(kind, sign, move, move);
            float u = exit > 0f ? (time - entry - move) / exit : 1f;
            return Lerp(end, run, Smooth(u));
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
            // Five short plants on the balls. Hips sit 17 cm down, chest over the knees.
            float s = Mathf.Sin(local * 42f);
            Sample sample = new Sample();
            sample.Drop = -0.16f;
            sample.Hip = Mathf.Lerp(18f, 28f, burst);
            sample.Head = Mathf.Lerp(-6f, -14f, burst);
            sample.ThighL = 8f + 5f * s;
            sample.ThighR = 8f - 5f * s;
            sample.KneeL = -52f - 3f * (s > 0f ? s : 0f);
            sample.KneeR = -52f - 3f * (s < 0f ? -s : 0f);
            sample.FootL = 12f + 2f * (s > 0f ? s : 0f);
            sample.FootR = 12f + 2f * (s < 0f ? -s : 0f);
            sample.ArmL = -20f * s;
            sample.ArmR = 20f * s;
            sample.ElbowL = -28f;
            sample.ElbowR = -28f;
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
            s.Drop = Mathf.Lerp(-0.15f, -0.05f, outDrive);
            float side = Mathf.Abs(Mathf.Sin(yaw * sign * 0.5f * 0.0174533f));
            s.ThighL = Mathf.Lerp(12f, 8f, outDrive);
            s.ThighR = Mathf.Lerp(2f, -4f, outDrive);
            s.KneeL = Mathf.Lerp(-66f - 14f * side, -42f, outDrive);
            s.KneeR = Mathf.Lerp(-62f - 14f * side, -38f, outDrive);
            s.FootL = 4f;
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
            s.Drop = -0.16f;
            s.Hip = Mathf.Lerp(14f, 20f, commit);
            s.Lean = Mathf.Lerp(-10f, 14f, commit);
            s.SpineYaw = -20f * fake + 12f * commit;
            s.HeadYaw = -28f * fake + 12f * commit;
            s.Head = -6f;
            s.YawL = 12f;
            s.YawR = -16f;
            s.ThighL = 4f;
            s.ThighR = 10f;
            s.KneeL = -64f;
            s.KneeR = -54f;
            s.FootL = 4f;
            s.FootR = 10f;
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
            push.Drop = -0.08f;
            push.Hip = 26f;
            push.Head = -8f;
            push.ThighL = 16f;
            push.ThighR = 8f;
            push.KneeL = -48f;
            push.KneeR = -42f;
            push.FootL = 12f;
            push.FootR = 6f;
            push.ArmL = 12f;
            push.ArmR = 12f;
            push.ElbowL = -22f;
            push.ElbowR = -22f;
            Sample air = new Sample();
            air.Drop = -0.34f;
            air.Hip = 82f;
            air.Head = -12f;
            air.ThighL = 2f;
            air.ThighR = 0f;
            air.KneeL = -12f;
            air.KneeR = -10f;
            air.ArmL = -55f;
            air.ArmR = -55f;
            air.ArmYawL = 22f;
            air.ArmYawR = -22f;
            air.ElbowL = -8f;
            air.ElbowR = -8f;
            if (local <= flight)
            {
                float u = Smooth(flight > 0f ? local / flight : 1f);
                Sample flown = Lerp(push, air, u);
                float open = u < 0.50f ? 0f : Smooth((u - 0.50f) / 0.50f);
                float pitch = open <= 0f ? Mathf.Lerp(12f, -32f, u / 0.50f) : Mathf.Lerp(-32f, -55f, open);
                SetArms(ref flown, pitch, 22f * open, -22f);
                return flown;
            }
            Sample land = new Sample();
            land.Drop = -0.525f;
            land.Hip = 74f;
            land.Head = -6f;
            land.ThighL = 12f;
            land.ThighR = 8f;
            land.KneeL = -56f;
            land.KneeR = -50f;
            land.FootL = 4f;
            land.FootR = 2f;
            SetArms(ref land, -48f, 24f, -22f);
            Sample roll = land;
            roll.Drop = -0.50f;
            roll.Hip = 96f;
            roll.Head = -14f;
            roll.ThighL = 18f;
            roll.ThighR = 14f;
            roll.KneeL = -68f;
            roll.KneeR = -62f;
            SetArms(ref roll, -48f, 24f, -22f);
            float recover = EvasionMoves.DiveRecover;
            float ru = recover > 0f ? (local - flight) / recover : 1f;
            if (ru < 0f) ru = 0f;
            if (ru > 1f) ru = 1f;
            if (ru < 0.36f)
            {
                Sample down = Lerp(air, land, Smooth(ru / 0.36f));
                SetArms(ref down, Mathf.Lerp(-55f, -48f, Smooth(ru / 0.36f)), 24f, Mathf.Lerp(-8f, -22f, Smooth(ru / 0.36f)));
                return down;
            }
            if (ru < 0.55f)
                return Lerp(land, roll, Smooth((ru - 0.36f) / 0.19f));
            float pop = Smooth((ru - 0.55f) / 0.45f);
            Sample up = Run(1.4f + ru * 3f, true);
            up.Hip = 12f;
            up.Drop = 0f;
            Sample risen = Lerp(roll, up, pop);
            if (pop < 0.58f)
                SetArms(ref risen, -48f, 24f, -22f);
            else
                SetArms(ref risen, Mathf.Lerp(-48f, 8f, (pop - 0.58f) / 0.42f), 0f, -20f);
            return risen;
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
