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
            if (spinR.Drop > -0.05f || Mathf.Abs(spinL.Lean) > 0.01f || Mathf.Abs(spinR.Lean) > 0.01f) return false;
            if (spinR.ElbowL > -25f) return false;
            Sample juke = MovePose(EvasionMoves.Kind.Juke, 1, 0.02f, EvasionMoves.JukeSeconds);
            if (juke.HeadYaw >= 0f || Mathf.Abs(juke.SpineYaw) > 0.01f) return false;
            if (juke.KneeR > -40f || juke.Drop > -0.08f) return false;
            Sample stutter = MovePose(EvasionMoves.Kind.Stutter, 1, 0.08f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Stutter));
            if (Mathf.Abs(stutter.HipYaw) > 0.01f || Mathf.Abs(stutter.SpineYaw) > 0.01f) return false;
            if (stutter.KneeL > -35f || stutter.KneeR > -35f) return false;
            if (stutter.Drop > -0.08f || stutter.Drop < -0.14f) return false;
            if (stutter.Hip < 10f) return false;
            Sample dive = MovePose(EvasionMoves.Kind.Dive, 1, EvasionMoves.DiveFlight * 0.9f, EvasionMoves.MoveSeconds(EvasionMoves.Kind.Dive));
            if (dive.Hip < 12f) return false;
            if (dive.Head > -18f) return false;
            if (dive.ArmL > -20f || dive.Drop > -0.08f) return false;
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
                // The pelvis drops late, so the entry blend does not bury the feet.
                if (kind == EvasionMoves.Kind.Juke || kind == EvasionMoves.Kind.Stutter
                    || kind == EvasionMoves.Kind.Spin || kind == EvasionMoves.Kind.Dive)
                    intoBlend.Drop = Mathf.Lerp(0f, into.Drop, w * w * w);
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
            // Chops stay planted. Near a switch both soles use the plant, so neither foot hops.
            // The plant thigh reaches in front of the hips. Drop only seats the sole.
            float s = Mathf.Sin(local * 42f);
            float side = s / 0.25f;
            if (side > 1f) side = 1f;
            if (side < -1f) side = -1f;
            float split = (Mathf.Abs(side) - 0.20f) / 0.55f;
            if (split < 0f) split = 0f;
            if (split > 1f) split = 1f;
            split = Smooth(split);
            bool left = side >= 0f;
            Sample sample = new Sample();
            sample.Drop = -0.104f;
            sample.Hip = 14f;
            sample.Spine = 4f;
            sample.Head = Mathf.Lerp(-8f, -12f, burst);
            // Thigh stays under 50°. Past that the upper leg passes through the spine.
            // The knee is bent and the shin points forward, so the pelvis sits behind the foot.
            sample.ThighL = left ? 48f : Mathf.Lerp(48f, 12f, split);
            sample.ThighR = left ? Mathf.Lerp(48f, 12f, split) : 48f;
            sample.KneeL = left ? -55f : Mathf.Lerp(-55f, -80f, split);
            sample.KneeR = left ? Mathf.Lerp(-55f, -80f, split) : -55f;
            sample.FootL = left ? 12f : Mathf.Lerp(12f, 2f, split);
            sample.FootR = left ? Mathf.Lerp(12f, 2f, split) : 12f;
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
            s.Hip = 14f;
            s.Spine = 4f;
            s.Lean = 0f;
            s.Drop = SpinDrop(local);
            // Pivot thigh stays under 50° so it does not enter the spine. The free foot stays up.
            s.ThighL = 48f;
            s.ThighR = 10f;
            s.KneeL = SpinKnee(local);
            s.KneeR = -90f;
            s.FootL = 12f;
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
            s.Drop = JukeDrop(local);
            s.Hip = 14f;
            s.Spine = 6f;
            s.Head = -8f;
            s.Lean = Mathf.Lerp(10f, 14f, headU);
            s.SpineYaw = 0f;
            s.HeadYaw = Mathf.Lerp(-24f, 18f, headU);
            s.HipYaw = Mathf.Lerp(0f, 8f, hipU);
            s.YawL = 4f;
            s.YawR = -16f;
            s.ThighRollL = -4f;
            s.ThighRollR = 12f;
            s.ThighL = 8f;
            s.ThighR = 48f;
            s.KneeL = -90f;
            s.KneeR = -55f;
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
            // Takeoff holds through 0.30 s. The legs tuck up before the chest drops.
            // The roll-up is the seated crouch from 0.76 s, so every landing frame is that pose.
            float liftAt = 0.36f;
            float pushAt = 0.42f;
            float handsAt = 0.48f;
            float tuckAt = 0.52f;
            float shoulderAt = 0.60f;
            float bridgeAt = 0.68f;
            float hipAt = 0.74f;
            float crouchAt = 0.76f;
            Sample reach = DiveReach();
            Sample lift = DiveLift();
            Sample push = DivePush();
            Sample hands = DiveHands();
            Sample tuck = DiveTuck();
            Sample shoulder = DiveShoulder();
            Sample bridge = DiveBridge();
            Sample hip = DiveHip();
            Sample crouch = DiveCrouch();
            if (local <= 0.30f)
                return reach;
            if (local <= liftAt)
                return Lerp(reach, lift, Smooth((local - 0.30f) / (liftAt - 0.30f)));
            if (local <= pushAt)
                return Lerp(lift, push, Smooth((local - liftAt) / (pushAt - liftAt)));
            if (local <= handsAt)
                return Lerp(push, hands, Smooth((local - pushAt) / (handsAt - pushAt)));
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
            // Takeoff. Hips sit back, knee bent, shin forward. Not a straight leg leaned back.
            Sample s = new Sample();
            s.Drop = -0.093f;
            s.Hip = 14f;
            s.Spine = 4f;
            s.Head = -24f;
            s.Lean = 2f;
            s.ThighL = 48f;
            s.ThighR = 48f;
            s.KneeL = -50f;
            s.KneeR = -50f;
            s.FootL = 12f;
            s.FootR = 10f;
            s.ArmL = -30f;
            s.ArmR = -30f;
            s.ElbowL = -24f;
            s.ElbowR = -24f;
            return s;
        }

        /// <summary>Same thigh, more knee bend, so the foot lifts before it swings back.</summary>
        static Sample DiveLift()
        {
            Sample s = DiveReach();
            s.KneeL = -75f;
            s.KneeR = -75f;
            s.FootL = 8f;
            s.FootR = 8f;
            return s;
        }

        /// <summary>Legs leave the floor while the chest is still up, so the next drop does not bury the feet.</summary>
        static Sample DivePush()
        {
            Sample s = new Sample();
            s.Drop = -0.093f;
            s.Hip = 18f;
            s.Spine = 4f;
            s.Head = -28f;
            s.Lean = 4f;
            s.ThighL = 16f;
            s.ThighR = 12f;
            s.KneeL = -90f;
            s.KneeR = -84f;
            s.FootL = 6f;
            s.FootR = 4f;
            s.ArmL = -32f;
            s.ArmR = -32f;
            s.ElbowL = -30f;
            s.ElbowR = -30f;
            return s;
        }

        /// <summary>Face-down stretch. Forearms lead. The legs trail and stay below the hips.</summary>
        static Sample DiveHands()
        {
            Sample s = new Sample();
            s.Drop = -0.496f;
            s.Hip = 62f;
            s.Spine = 5f;
            s.Head = -34f;
            s.Lean = 8f;
            s.ThighL = 16f;
            s.ThighR = 12f;
            s.KneeL = -40f;
            s.KneeR = -34f;
            s.FootL = 4f;
            s.FootR = 2f;
            s.ArmL = -34f;
            s.ArmR = -34f;
            s.ArmYawL = 0f;
            s.ArmYawR = 0f;
            s.ElbowL = -32f;
            s.ElbowR = -32f;
            return s;
        }

        /// <summary>Chest rolls to the side. Still face down. No leg reaches overhead.</summary>
        static Sample DiveTuck()
        {
            Sample s = new Sample();
            s.Drop = -0.500f;
            s.Hip = 56f;
            s.Spine = 4f;
            s.Head = -20f;
            s.Lean = 26f;
            s.ThighL = 10f;
            s.ThighR = 6f;
            s.KneeL = -78f;
            s.KneeR = -72f;
            s.FootL = 4f;
            s.FootR = 2f;
            s.ArmL = -16f;
            s.ArmR = -10f;
            s.ElbowL = -42f;
            s.ElbowR = -36f;
            return s;
        }

        /// <summary>Shoulder takes the floor. Knees stay bent under the hips, not kicked up.</summary>
        static Sample DiveShoulder()
        {
            Sample s = new Sample();
            s.Drop = -0.480f;
            s.Hip = 46f;
            s.Spine = 2f;
            s.Head = -12f;
            s.Lean = 34f;
            s.ThighL = 8f;
            s.ThighR = 6f;
            s.KneeL = -82f;
            s.KneeR = -76f;
            s.FootL = 4f;
            s.FootR = 2f;
            s.ArmL = -4f;
            s.ArmR = 2f;
            s.ElbowL = -44f;
            s.ElbowR = -38f;
            return s;
        }

        /// <summary>Coming up off the shoulder toward the feet.</summary>
        static Sample DiveBridge()
        {
            Sample s = new Sample();
            s.Drop = -0.080f;
            s.Hip = 8f;
            s.Spine = 4f;
            s.Head = -10f;
            s.Lean = 8f;
            s.ThighL = 16f;
            s.ThighR = 12f;
            s.KneeL = -80f;
            s.KneeR = -74f;
            s.FootL = 6f;
            s.FootR = 4f;
            s.ArmL = 4f;
            s.ArmR = 2f;
            s.ElbowL = -40f;
            s.ElbowR = -36f;
            return s;
        }

        /// <summary>Hips moving back over the feet on the way up to the crouch.</summary>
        static Sample DiveHip()
        {
            Sample s = new Sample();
            s.Drop = -0.050f;
            s.Hip = 6f;
            s.Spine = 4f;
            s.Head = -8f;
            s.Lean = 3f;
            s.ThighL = 46f;
            s.ThighR = 46f;
            s.KneeL = -68f;
            s.KneeR = -68f;
            s.FootL = 14f;
            s.FootR = 12f;
            s.ArmL = 4f;
            s.ArmR = 2f;
            s.ElbowL = -40f;
            s.ElbowR = -36f;
            return s;
        }

        static Sample DiveCrouch()
        {
            Sample s = new Sample();
            s.Drop = -0.141f;
            s.Hip = 6f;
            s.Spine = 4f;
            s.Head = -8f;
            s.Lean = 2f;
            s.ThighL = 46f;
            s.ThighR = 46f;
            s.KneeL = -68f;
            s.KneeR = -68f;
            s.FootL = 14f;
            s.FootR = 12f;
            s.ArmL = 4f;
            s.ArmR = 2f;
            s.ElbowL = -40f;
            s.ElbowR = -36f;
            return s;
        }

        static float SpinKnee(float local)
        {
            // Pivot knee. Bent, shin forward. A table seats the sole once the turn is measured.
            float[] at = { 0f, 0.350f };
            float[] knee = { -55f, -55f };
            return Table(local, at, knee);
        }

        static float SpinDrop(float local)
        {
            // Pelvis height that seats the pivot sole. It is a bone drop, not a capsule offset.
            float[] at = { 0f, 0.350f };
            float[] drop = { -0.104f, -0.104f };
            return Table(local, at, drop);
        }

        static float JukeDrop(float local)
        {
            // Pelvis height that keeps the outside sole down while the trunk leans into the cut.
            float[] at = { 0f, 0.033f, 0.067f, 0.100f, 0.133f, 0.167f, 0.200f, 0.220f };
            float[] drop = { -0.0894f, -0.0893f, -0.0897f, -0.0906f, -0.0922f, -0.0944f, -0.0961f, -0.0961f };
            return Table(local, at, drop);
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
