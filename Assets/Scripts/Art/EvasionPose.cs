using System.Globalization;
using System.IO;
using UnityEngine;
using Tag.Gameplay;

namespace Tag.Art
{
    /// <summary>
    /// In-place Hier clips for the four evasion moves. Entry blends out of the
    /// sprint stride, the move plays, and the exit blends back. Root motion stays off:
    /// the dump's drop is 0 and the motor owns translation.
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
            if (RootMotion || Drop != 0f) return false;
            if (EvasionMoves.Duration(EvasionMoves.Kind.Spin) < 0.5f) return false;
            Sample spinR = MovePose(EvasionMoves.Kind.Spin, 1, EvasionMoves.SpinSeconds * 0.2f, EvasionMoves.SpinSeconds);
            Sample spinL = MovePose(EvasionMoves.Kind.Spin, -1, EvasionMoves.SpinSeconds * 0.2f, EvasionMoves.SpinSeconds);
            if (spinR.HeadYaw < 8f || spinL.HeadYaw > -8f) return false;
            if (spinR.HipYaw < 40f || spinL.HipYaw > -40f) return false;
            if (spinR.ThighL < 3f || spinL.ThighR < 3f) return false;
            Sample juke = MovePose(EvasionMoves.Kind.Juke, 1, 0.02f, EvasionMoves.JukeSeconds);
            if (juke.HeadYaw >= 0f) return false;
            if (juke.YawL < 8f || juke.YawR > -8f) return false;
            if (Mathf.Abs(juke.SpineYaw) > 0.01f) return false;
            Sample stutter = MovePose(EvasionMoves.Kind.Stutter, 1, 0.05f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Stutter));
            if (Mathf.Abs(stutter.HipYaw) > 0.01f || Mathf.Abs(stutter.SpineYaw) > 0.01f || Mathf.Abs(stutter.HeadYaw) > 0.01f) return false;
            if (stutter.Hip < 3f) return false;
            Sample dive = MovePose(EvasionMoves.Kind.Dive, 1, EvasionMoves.DiveFlight * 0.85f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Dive));
            if (dive.Hip < 40f) return false;
            if (dive.ArmL > -40f || dive.ArmR > -40f) return false;
            if (dive.ElbowL < -8f || dive.ElbowR < -8f) return false;
            Sample exit = At(EvasionMoves.Kind.Stutter, 1, EvasionMoves.Duration(EvasionMoves.Kind.Stutter));
            if (Mathf.Abs(exit.Drop) > 0.0001f) return false;
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

        /// <summary>
        /// Short stride the Hier cuffs can hold. A sprint thigh past about 4° forward
        /// or 6° back, a knee past 2°, or a spine pitch of 1° deepens a rest overlap.
        /// The dive keeps the arms at the one forward pitch that clears the shoulder.
        /// </summary>
        static Sample Run(float phase, bool reach)
        {
            float s = Mathf.Sin(phase);
            Sample sample = new Sample();
            sample.ThighL = 4f * s;
            sample.ThighR = -4f * s;
            sample.KneeL = -2f;
            sample.KneeR = -2f;
            sample.FootL = s > 0.25f ? 3f : 0f;
            sample.FootR = s < -0.25f ? 3f : 0f;
            sample.ElbowL = reach ? 0f : -4f;
            sample.ElbowR = sample.ElbowL;
            sample.ArmL = reach ? -60f : 0f;
            sample.ArmR = sample.ArmL;
            sample.Head = reach ? -2f : -4f;
            sample.Hip = reach ? 0f : 2f;
            sample.Drop = Drop;
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
            s.Drop = Drop;
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
            // High cadence, a few degrees of thigh. The knee cuff clips past 2°.
            Sample s = Run(local * 22f, false);
            s.Hip = Mathf.Lerp(5f, 8f, burst);
            s.HipYaw = 0f;
            s.Spine = 0f;
            s.SpineYaw = 0f;
            s.HeadYaw = 0f;
            s.Lean = 0f;
            s.YawL = 0f;
            s.YawR = 0f;
            s.ArmL = 0f;
            s.ArmR = 0f;
            s.ArmYawL = 0f;
            s.ArmYawR = 0f;
            s.Head = Mathf.Lerp(-5f, -8f, burst);
            s.ElbowL = -6f;
            s.ElbowR = -6f;
            s.KneeL = -2f;
            s.KneeR = -2f;
            return s;
        }

        static Sample Spin(float local, float move, int sign)
        {
            float u = move > 0f ? local / move : 1f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float yaw = u >= 0.999f ? 360f * sign : 360f * u * sign;
            float lead = u < 0.45f ? 26f * (1f - u / 0.45f) : 0f;
            Sample s = new Sample();
            s.HipYaw = yaw;
            s.HeadYaw = lead * sign;
            s.Head = -6f;
            // Elbow past 2° deepens the cuff on part of the turn. This is the tuck that survives it.
            s.ElbowL = -2f;
            s.ElbowR = -2f;
            s.ThighL = 4f;
            s.ThighR = -5f;
            s.KneeL = -2f;
            s.KneeR = -2f;
            s.FootL = 4f;
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
            s.Hip = 6f;
            s.YawL = 8f;
            s.YawR = -8f;
            s.ThighL = 0f;
            s.ThighR = 0f;
            s.KneeL = -2f;
            s.KneeR = -2f;
            s.FootL = 4f;
            s.Head = -6f;
            s.HeadYaw = -22f * fake + 8f * commit;
            s.ElbowL = -4f;
            s.ElbowR = -4f;
            return s;
        }

        static Sample Dive(float local)
        {
            const float flat = 78f;
            Sample s = new Sample();
            // -60° is a clear shoulder angle. The pitches between a hanging arm and this reach are not.
            s.ArmL = -60f;
            s.ArmR = -60f;
            float flight = EvasionMoves.DiveFlight;
            if (local <= flight)
            {
                float u = Smooth(flight > 0f ? local / flight : 1f);
                s.Hip = Mathf.Lerp(0f, flat, u);
                s.Head = Mathf.Lerp(-2f, -8f, u);
                return s;
            }
            float recover = EvasionMoves.DiveRecover;
            float ru = recover > 0f ? (local - flight) / recover : 1f;
            if (ru < 0f) ru = 0f;
            if (ru > 1f) ru = 1f;
            s.Hip = Mathf.Lerp(flat, 4f, Smooth(ru));
            s.Head = Mathf.Lerp(-8f, -4f, ru);
            float amp = s.Hip < 20f ? 4f * (20f - s.Hip) / 16f : 0f;
            if (amp > 4f) amp = 4f;
            float stride = Mathf.Sin(ru * 6.2f);
            s.ThighL = amp * stride;
            s.ThighR = -amp * stride;
            if (amp > 0.5f)
            {
                s.KneeL = -2f;
                s.KneeR = -2f;
            }
            float foot = 3f * (amp / 4f);
            s.FootL = stride > 0.25f ? foot : 0f;
            s.FootR = stride < -0.25f ? foot : 0f;
            return s;
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
            s.Drop = Drop;
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
