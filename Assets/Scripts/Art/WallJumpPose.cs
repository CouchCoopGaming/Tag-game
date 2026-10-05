using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual wall-jump push-off only. Cling plus Jump, including a jump during
    /// cling grace, holds an athletic shove, then eases into the jump rise and fall.
    /// Impulse, cling grace, climb, slip, wall-run, and gravity are not written.
    /// DummyRunner uses the same locomotor, so this beat is on that path too.
    /// </summary>
    public static class WallJumpPose
    {
        public const bool RootMotion = false;

        /// <summary>How long the shove holds before the ease. Inside 0.12–0.18s.</summary>
        public const float BeatSeconds = 0.15f;
        /// <summary>Smoothstep into JumpPose rise and fall after the hold.</summary>
        public const float EaseSeconds = 0.12f;
        /// <summary>Degrees per second. The shove arrives on the fire frame.</summary>
        public const float Slew = 170f;

        /// <summary>Foot that stayed on the wall. Nearly straight, trailing.</summary>
        public const float PlantThigh = -28f;
        public const float PlantKnee = -6f;
        /// <summary>Free knee up. Higher than the jump tuck so the shove still reads.</summary>
        public const float DriveThigh = 98f;
        public const float DriveKnee = -108f;
        /// <summary>Wall-side arm shoves back. Positive pitch is behind the chest.</summary>
        public const float PushPitch = 46f;
        public const float PushYaw = 32f;
        public const float PushElbow = -4f;
        /// <summary>Free arm reaches up the arc. Not the climb reach and not the tuck.</summary>
        public const float ReachPitch = -72f;
        public const float ReachYaw = 16f;
        public const float ReachElbow = -20f;
        /// <summary>Chest opens off the wall. Opposite the climb curl.</summary>
        public const float Spine = -26f;
        public const float Hip = -12f;
        /// <summary>Look up the launch.</summary>
        public const float Head = -18f;
        /// <summary>Roll off the wall. Wall-on-the-left is negative, same sign as the run.</summary>
        public const float Lean = 28f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head, LeanZ;
        }

        /// <summary>
        /// Wall jump is cling plus Jump on a climb or a wall run.
        /// A Jump during cling grace still counts while the probe is on the wall.
        /// A released cling with no grace, a spent grace, and a jump with no wall do not.
        /// </summary>
        public static bool Fires(bool clingHeld, bool jumpPressed, bool onWallSurf, float graceRemaining, bool wallContact)
        {
            if (!jumpPressed) return false;
            if (onWallSurf && clingHeld) return true;
            return wallContact && graceRemaining > 0f;
        }

        /// <summary>Smoothstep. 0 at the start of the ease, 1 at the end.</summary>
        public static float Ease(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return u * u * (3f - 2f * u);
        }

        /// <summary>0 during the hold, 1 once the ease into the jump has finished. Negative age is off.</summary>
        public static float JumpWeight(float age)
        {
            if (age < 0f) return 0f;
            if (age <= BeatSeconds) return 0f;
            if (EaseSeconds <= 0.0001f) return 1f;
            return Ease((age - BeatSeconds) / EaseSeconds);
        }

        /// <summary>1 during the hold, 0 when the jump owns the bones. Negative age is off.</summary>
        public static float PushWeight(float age)
        {
            if (age < 0f) return 0f;
            return 1f - JumpWeight(age);
        }

        /// <summary>The ease has finished. The jump rise and fall can own the stack.</summary>
        public static bool Settled(float age) => JumpWeight(age) >= 0.999f;

        /// <summary>pushW + jumpW = 1 for age &gt;= 0. Age 0 is the shove. The jump wins after the ease.</summary>
        public static void IntoJump(float age, out float pushW, out float jumpW)
        {
            jumpW = JumpWeight(age);
            pushW = age < 0f ? 0f : 1f - jumpW;
        }

        /// <summary>plantLeft keeps the left shoe on the wall and lifts the right knee.</summary>
        public static Sample Push(bool plantLeft)
        {
            float thighL = plantLeft ? PlantThigh : DriveThigh;
            float thighR = plantLeft ? DriveThigh : PlantThigh;
            float kneeL = plantLeft ? PlantKnee : DriveKnee;
            float kneeR = plantLeft ? DriveKnee : PlantKnee;
            float pitchL;
            float pitchR;
            float yawL;
            float yawR;
            float elbowL;
            float elbowR;
            float lean;
            if (plantLeft)
            {
                pitchL = PushPitch;
                pitchR = ReachPitch;
                yawL = PushYaw;
                yawR = -ReachYaw;
                elbowL = PushElbow;
                elbowR = ReachElbow;
                lean = -Lean;
            }
            else
            {
                pitchL = ReachPitch;
                pitchR = PushPitch;
                yawL = ReachYaw;
                yawR = -PushYaw;
                elbowL = ReachElbow;
                elbowR = PushElbow;
                lean = Lean;
            }

            return new Sample
            {
                ThighL = thighL,
                ThighR = thighR,
                KneeL = kneeL,
                KneeR = kneeR,
                ArmPitchL = pitchL,
                ArmPitchR = pitchR,
                ArmYawL = yawL,
                ArmYawR = yawR,
                ElbowL = elbowL,
                ElbowR = elbowR,
                Hip = Hip,
                Spine = Spine,
                Head = Head,
                LeanZ = lean,
            };
        }

        /// <summary>
        /// Hold the shove, then Ease into JumpPose rise and fall.
        /// The ground takeoff crouch stays off. Planar speed does not move the apex.
        /// </summary>
        public static Sample At(float age, float verticalSpeed, bool plantLeft, float planarSpeed)
        {
            Sample push = Push(plantLeft);
            float jumpW = JumpWeight(age);
            if (jumpW <= 0f) return push;
            JumpPose.Sample air = JumpPose.At(verticalSpeed + planarSpeed * 0f, JumpPose.TakeoffSeconds, !plantLeft);
            Sample jump = FromJump(air);
            if (jumpW >= 1f) return jump;
            return Lerp(push, jump, jumpW);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (BeatSeconds < 0.12f || BeatSeconds > 0.18f) return false;
            if (EaseSeconds < 0.10f || EaseSeconds > 0.14f) return false;
            if (Slew < 120f) return false;
            if (Ease(0f) > 0.0001f || Mathf.Abs(Ease(1f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Ease(0.5f) - 0.5f) > 0.0001f) return false;

            if (JumpWeight(-0.01f) > 0.0001f || PushWeight(-0.01f) > 0.0001f) return false;
            if (Mathf.Abs(PushWeight(0f) - 1f) > 0.0001f || JumpWeight(0f) > 0.0001f) return false;
            if (Mathf.Abs(PushWeight(BeatSeconds) - 1f) > 0.0001f || JumpWeight(BeatSeconds) > 0.0001f) return false;
            float end = BeatSeconds + EaseSeconds;
            if (PushWeight(end) > 0.0001f || Mathf.Abs(JumpWeight(end) - 1f) > 0.0001f) return false;
            if (!Settled(end) || Settled(BeatSeconds)) return false;
            if (Mathf.Abs(JumpWeight(BeatSeconds + EaseSeconds * 0.5f) - 0.5f) > 0.0001f) return false;
            float prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float age = BeatSeconds + EaseSeconds * (i / 8f);
                IntoJump(age, out float pushW, out float jumpW);
                if (Mathf.Abs(pushW + jumpW - 1f) > 0.0001f) return false;
                if (jumpW + 0.0001f < prev) return false;
                prev = jumpW;
            }

            if (Mathf.Abs(WallPose.ClingGraceSeconds - 0.08f) > 0.001f) return false;
            if (Mathf.Abs(WallPose.ClimbSpeedRef - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(WallPose.SlipSpeedRef - 3.7f) > 0.001f) return false;
            if (Mathf.Abs(WallPose.WallRunSpeedRef - 9.5f) > 0.001f) return false;
            if (JumpPose.Extend(24.7f) > 0.0001f) return false;
            if (Mathf.Abs(JumpPose.ExtendAt(0f, 0f) - JumpPose.ExtendAt(0f, 24f)) > 0.0001f) return false;

            float frame = 1f / 60f;
            float grace = WallPose.ClingGraceSeconds;
            if (!Fires(true, true, true, 0f, true)) return false;
            if (!Fires(false, true, false, grace, true)) return false;
            if (!Fires(false, true, false, grace - frame, true)) return false;
            if (Fires(false, true, true, 0f, true)) return false;
            if (Fires(true, false, true, grace, true)) return false;
            if (Fires(true, true, false, 0f, true)) return false;
            if (Fires(true, true, false, grace, false)) return false;
            if (Fires(false, true, false, 0f, true)) return false;

            Sample pushL = Push(true);
            Sample pushR = Push(false);
            if (pushL.ThighR <= pushL.ThighL + 80f) return false;
            if (pushL.KneeR >= pushL.KneeL - 70f) return false;
            if (pushL.ArmPitchL <= pushL.ArmPitchR + 80f) return false;
            if (pushL.ArmPitchL < 20f || pushL.ArmPitchR > -50f) return false;
            if (pushL.Spine > -16f || pushL.Hip > -6f) return false;
            if (pushL.Head > -10f) return false;
            if (pushL.LeanZ > -20f) return false;
            if (pushR.ThighL <= pushR.ThighR + 80f) return false;
            if (pushR.ArmPitchR <= pushR.ArmPitchL + 80f) return false;
            if (pushR.LeanZ < 20f) return false;
            if (Mathf.Abs(pushL.ArmPitchL - WallPose.PushArmPitch) < 40f) return false;
            if (!(DriveThigh > JumpPose.TuckThigh && DriveThigh > JumpPose.FallThigh)) return false;
            if (PushPitch <= 0f) return false;
            if (ReachPitch <= JumpPose.TuckArmPitch) return false;

            Sample held = At(0f, 24.7f, true, 12f);
            if (Mathf.Abs(held.ThighR - pushL.ThighR) > 0.05f) return false;
            if (Mathf.Abs(held.ArmPitchL - pushL.ArmPitchL) > 0.05f) return false;
            if (Mathf.Abs(held.LeanZ - pushL.LeanZ) > 0.05f) return false;

            JumpPose.Sample riseExpect = JumpPose.At(24.7f, JumpPose.TakeoffSeconds, true);
            Sample rise = At(end, 24.7f, true, 0f);
            Sample riseFast = At(end, 24.7f, false, 24f);
            if (Mathf.Abs(rise.ThighL - riseExpect.ThighL) > 0.05f) return false;
            if (Mathf.Abs(rise.ThighR - riseExpect.ThighR) > 0.05f) return false;
            if (Mathf.Abs(rise.KneeL - riseExpect.KneeL) > 0.05f) return false;
            if (Mathf.Abs(rise.ArmPitchL - riseExpect.ArmPitchL) > 0.05f) return false;
            if (Mathf.Abs(rise.ArmYawL - riseExpect.ArmYawL) > 0.05f) return false;
            if (Mathf.Abs(rise.ArmYawR - (-riseExpect.ArmYawR)) > 0.05f) return false;
            if (Mathf.Abs(rise.Spine - riseExpect.Spine) > 0.05f) return false;
            if (Mathf.Abs(rise.Hip - riseExpect.Hip) > 0.05f) return false;
            if (Mathf.Abs(rise.ThighL - riseFast.ThighL) > 0.05f) return false;
            if (Mathf.Abs(rise.ArmPitchL - riseFast.ArmPitchL) > 0.05f) return false;
            if (rise.LeanZ > 0.05f || rise.LeanZ < -0.05f) return false;
            if (rise.Head != WallPose.ReleaseHead) return false;

            Sample apex = At(end, 0f, true, 0f);
            Sample apexFast = At(end, 0f, true, 24f);
            if (Mathf.Abs(apex.ThighL - apexFast.ThighL) > 0.05f) return false;
            if (Mathf.Abs(apex.ArmYawL - apexFast.ArmYawL) > 0.05f) return false;
            if (apex.ThighL <= JumpPose.FallThigh || apex.ThighL >= JumpPose.TuckThigh) return false;

            JumpPose.Sample fallExpect = JumpPose.At(JumpPose.FallVy, JumpPose.TakeoffSeconds, true);
            Sample fall = At(end, JumpPose.FallVy, true, 9f);
            if (Mathf.Abs(fall.ThighL - fallExpect.ThighL) > 0.05f) return false;
            if (Mathf.Abs(fall.ArmYawL - JumpPose.FallArmYaw) > 0.05f) return false;
            if (Mathf.Abs(fall.ArmYawR - (-JumpPose.FallArmYaw)) > 0.05f) return false;
            if (Mathf.Abs(fall.ArmPitchL - JumpPose.FallArmPitch) > 0.05f) return false;
            if (fall.ArmYawL <= pushL.ArmYawL + 16f) return false;
            if (Mathf.Abs(fall.ThighL - rise.ThighL) < 30f) return false;

            Sample mid = At(BeatSeconds + EaseSeconds * 0.5f, 24.7f, true, 0f);
            if (mid.ThighR >= pushL.ThighR || mid.ThighR <= rise.ThighR) return false;
            if (mid.ArmPitchL <= rise.ArmPitchL || mid.ArmPitchL >= pushL.ArmPitchL) return false;
            return true;
        }

        public static string ProofLine()
        {
            float frame = 1f / 60f;
            float grace = WallPose.ClingGraceSeconds;
            float end = BeatSeconds + EaseSeconds;
            bool cling = Fires(true, true, true, 0f, true);
            bool graceFire = Fires(false, true, false, grace, true);
            bool frameFire = Fires(false, true, false, grace - frame, true);
            bool spent = Fires(false, true, false, 0f, true);
            bool noJump = Fires(true, false, true, grace, true);
            Sample push = Push(true);
            Sample rise = At(end, 24.7f, true, 0f);
            Sample fall = At(end, JumpPose.FallVy, true, 0f);
            IntoJump(BeatSeconds, out float holdPush, out float holdJump);
            IntoJump(BeatSeconds + EaseSeconds * 0.5f, out float midPush, out float midJump);
            return "wall-jump-pose"
                + " beat=" + BeatSeconds.ToString("0.00")
                + " ease=" + EaseSeconds.ToString("0.00")
                + " pushPitch=" + PushPitch.ToString("0")
                + " reachPitch=" + ReachPitch.ToString("0")
                + " pushYaw=" + PushYaw.ToString("0")
                + " reachYaw=" + ReachYaw.ToString("0")
                + " drive=" + DriveThigh.ToString("0") + "/" + DriveKnee.ToString("0")
                + " plant=" + PlantThigh.ToString("0") + "/" + PlantKnee.ToString("0")
                + " spine=" + Spine.ToString("0")
                + " hip=" + Hip.ToString("0")
                + " head=" + Head.ToString("0")
                + " lean=" + Lean.ToString("0")
                + " hold=" + holdPush.ToString("0.00") + "+" + holdJump.ToString("0.00")
                + " mid=" + midPush.ToString("0.00") + "+" + midJump.ToString("0.00")
                + " shoveThigh=" + push.ThighR.ToString("0")
                + " shovePitch=" + push.ArmPitchL.ToString("0")
                + " riseThigh=" + rise.ThighL.ToString("0")
                + " fallYaw=" + fall.ArmYawL.ToString("0")
                + " cling=" + (cling ? "1" : "0")
                + " graceFire=" + (graceFire ? "1" : "0")
                + " frameFire=" + (frameFire ? "1" : "0")
                + " spent=" + (spent ? "1" : "0")
                + " noJump=" + (noJump ? "1" : "0")
                + " gate=cling+Jump on climb or wall run, and Jump during cling grace " + grace.ToString("0.00") + " while the probe is on the wall"
                + "; beat holds " + BeatSeconds.ToString("0.00") + "s then Ease(t/" + EaseSeconds.ToString("0.00") + ") into JumpPose rise/fall"
                + "; apex ignores planar speed"
                + "; slew=" + Slew.ToString("0")
                + "; shared=DummyLocomotor"
                + "; rootMotion=0"
                + "; impulse unchanged";
        }

        static Sample FromJump(JumpPose.Sample jump)
        {
            return new Sample
            {
                ThighL = jump.ThighL,
                ThighR = jump.ThighR,
                KneeL = jump.KneeL,
                KneeR = jump.KneeR,
                ArmPitchL = jump.ArmPitchL,
                ArmPitchR = jump.ArmPitchR,
                ArmYawL = jump.ArmYawL,
                ArmYawR = -jump.ArmYawR,
                ElbowL = jump.ElbowL,
                ElbowR = jump.ElbowR,
                Hip = jump.Hip,
                Spine = jump.Spine,
                Head = WallPose.ReleaseHead,
                LeanZ = 0f,
            };
        }

        static Sample Lerp(Sample a, Sample b, float t)
        {
            return new Sample
            {
                ThighL = Mathf.Lerp(a.ThighL, b.ThighL, t),
                ThighR = Mathf.Lerp(a.ThighR, b.ThighR, t),
                KneeL = Mathf.Lerp(a.KneeL, b.KneeL, t),
                KneeR = Mathf.Lerp(a.KneeR, b.KneeR, t),
                ArmPitchL = Mathf.Lerp(a.ArmPitchL, b.ArmPitchL, t),
                ArmPitchR = Mathf.Lerp(a.ArmPitchR, b.ArmPitchR, t),
                ArmYawL = Mathf.Lerp(a.ArmYawL, b.ArmYawL, t),
                ArmYawR = Mathf.Lerp(a.ArmYawR, b.ArmYawR, t),
                ElbowL = Mathf.Lerp(a.ElbowL, b.ElbowL, t),
                ElbowR = Mathf.Lerp(a.ElbowR, b.ElbowR, t),
                Hip = Mathf.Lerp(a.Hip, b.Hip, t),
                Spine = Mathf.Lerp(a.Spine, b.Spine, t),
                Head = Mathf.Lerp(a.Head, b.Head, t),
                LeanZ = Mathf.Lerp(a.LeanZ, b.LeanZ, t),
            };
        }
    }
}
