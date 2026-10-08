using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual punch and tag only. Three beats at chase-cam distance.
    /// Windup cocks the fist beside the head. Active is the chest-high line for the locked reach.
    /// Recovery folds that line into a rib chamber, then eases onto the gait.
    /// TagCatch gathers both hands at the chest, then both arms claim one contact.
    /// Phase times, reach, and the hit window are not written here. No root motion.
    /// </summary>
    public static class PunchTagPose
    {
        public const bool RootMotion = false;

        /// <summary>Locked PunchTagTuning.reach. The strike arm is the long line for this span.</summary>
        public const float ReachMeters = 1.55f;

        /// <summary>Slew while a punch or a tag clip is on, so the short beats arrive.</summary>
        public const float PoseSlew = 170f;

        /// <summary>Coil share of the tag flinch life. The 0.45s decay is unchanged.</summary>
        public const float TagWindupEnd = 0.22f;

        /// <summary>Reach share ends here. The rest of the flinch returns to the run.</summary>
        public const float TagActiveEnd = 0.58f;

        public struct Beat
        {
            public float Sample;
            public float Weight;
            public string State;
        }

        /// <summary>Coil. Sample stays on the pull-back for the whole windup.</summary>
        public static Beat PunchWindup(float progress)
        {
            return new Beat
            {
                Sample = 0f,
                Weight = 1f,
                State = VerbPoseClips.StatePunchWindup,
            };
        }

        /// <summary>Reach. Opens from the coil to the long arm across the active window.</summary>
        public static Beat PunchActive(float progress)
        {
            return new Beat
            {
                Sample = Commit(progress),
                Weight = 1f,
                State = VerbPoseClips.StatePunchActive,
            };
        }

        /// <summary>Return. The reach shape fades onto the run or the idle. Hit and miss share it.</summary>
        public static Beat PunchRecover(float progress)
        {
            float p = Mathf.Clamp01(progress);
            return new Beat
            {
                Sample = 1f,
                Weight = 1f - Mathf.SmoothStep(0f, 1f, p),
                State = VerbPoseClips.StatePunchRecover,
            };
        }

        /// <summary>
        /// TagCatch over the existing flinch. 1 is the frame the tag arms.
        /// The gather, the two-hand reach, then the weight back to the run.
        /// </summary>
        public static Beat Tag(float flinch)
        {
            float u = 1f - Mathf.Clamp01(flinch);
            if (u <= TagWindupEnd)
            {
                return new Beat
                {
                    Sample = 0f,
                    Weight = 1f,
                    State = VerbPoseClips.StateTag,
                };
            }

            if (u <= TagActiveEnd)
            {
                float p = (u - TagWindupEnd) / (TagActiveEnd - TagWindupEnd);
                return new Beat
                {
                    Sample = Commit(p),
                    Weight = 1f,
                    State = VerbPoseClips.StateTag,
                };
            }

            float back = (u - TagActiveEnd) / (1f - TagActiveEnd);
            if (back < 0f) back = 0f;
            if (back > 1f) back = 1f;
            return new Beat
            {
                Sample = 1f,
                Weight = 1f - Mathf.SmoothStep(0f, 1f, back),
                State = VerbPoseClips.StateTag,
            };
        }

        /// <summary>0 at the start of a beat, 1 at the end. Steep early, so the reach commits.</summary>
        public static float Commit(float progress)
        {
            float u = Mathf.Clamp01(progress);
            return 1f - (1f - u) * (1f - u);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(ReachMeters - 1.55f) > 0.001f) return false;
            if (PoseSlew < 120f) return false;
            if (TagWindupEnd < 0.12f || TagWindupEnd > 0.30f) return false;
            if (TagActiveEnd < TagWindupEnd + 0.20f || TagActiveEnd > 0.70f) return false;

            Tag.Gameplay.PunchTagTuning tuning = ScriptableObject.CreateInstance<Tag.Gameplay.PunchTagTuning>();
            if (Mathf.Abs(tuning.reach - ReachMeters) > 0.001f) return false;
            if (Mathf.Abs(tuning.windup - 0.12f) > 0.001f) return false;
            if (Mathf.Abs(tuning.active - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(tuning.hitRecover - 0.15f) > 0.001f) return false;
            if (Mathf.Abs(tuning.missRecover - 0.32f) > 0.001f) return false;

            Beat wind = PunchWindup(0f);
            Beat windEnd = PunchWindup(1f);
            if (wind.Sample > 0.001f || windEnd.Sample > 0.001f) return false;
            if (Mathf.Abs(wind.Weight - 1f) > 0.001f || Mathf.Abs(windEnd.Weight - 1f) > 0.001f) return false;
            if (wind.State != VerbPoseClips.StatePunchWindup) return false;

            Beat active0 = PunchActive(0f);
            Beat activeMid = PunchActive(0.5f);
            Beat active1 = PunchActive(1f);
            if (active0.Sample > 0.001f || Mathf.Abs(active0.Weight - 1f) > 0.001f) return false;
            if (Mathf.Abs(active0.Sample - windEnd.Sample) > 0.001f) return false;
            if (Mathf.Abs(active0.Weight - windEnd.Weight) > 0.001f) return false;
            if (activeMid.Sample < 0.70f) return false;
            if (Mathf.Abs(active1.Sample - 1f) > 0.001f || Mathf.Abs(active1.Weight - 1f) > 0.001f) return false;
            if (active1.State != VerbPoseClips.StatePunchActive) return false;

            Beat hit0 = PunchRecover(0f);
            Beat miss0 = PunchRecover(0f);
            if (Mathf.Abs(hit0.Sample - active1.Sample) > 0.001f || Mathf.Abs(hit0.Weight - 1f) > 0.001f) return false;
            if (Mathf.Abs(miss0.Weight - 1f) > 0.001f || miss0.State != VerbPoseClips.StatePunchRecover) return false;
            float prevW = 2f;
            for (int i = 0; i <= 10; i++)
            {
                Beat step = PunchRecover(i / 10f);
                if (step.Weight > prevW + 0.0001f) return false;
                if (i > 0 && prevW - step.Weight > 0.25f) return false;
                prevW = step.Weight;
            }

            Beat hit1 = PunchRecover(1f);
            Beat miss1 = PunchRecover(1f);
            if (hit1.Weight > 0.001f || miss1.Weight > 0.001f) return false;
            if (hit0.State != VerbPoseClips.StatePunchRecover) return false;

            Beat tag0 = Tag(1f);
            Beat tagCoil = Tag(1f - TagWindupEnd);
            if (tag0.Sample > 0.001f || Mathf.Abs(tag0.Weight - 1f) > 0.001f) return false;
            if (tagCoil.Sample > 0.001f || tag0.State != VerbPoseClips.StateTag) return false;
            Beat tagReach = Tag(1f - TagActiveEnd);
            if (tagReach.Sample < 0.95f || Mathf.Abs(tagReach.Weight - 1f) > 0.001f) return false;
            Beat tagFade = Tag(0.04f);
            if (tagFade.Weight > 0.08f) return false;
            prevW = 2f;
            for (int i = 0; i <= 10; i++)
            {
                float u = TagActiveEnd + (1f - TagActiveEnd) * (i / 10f);
                Beat step = Tag(1f - u);
                if (step.Weight > prevW + 0.0001f) return false;
                if (i > 0 && prevW - step.Weight > 0.25f) return false;
                prevW = step.Weight;
            }

            if (VerbPoseClips.PunchCockPitch > -32f || VerbPoseClips.PunchCockYaw > -16f || VerbPoseClips.PunchCockSpineYaw > -50f) return false;
            if (VerbPoseClips.PunchStrikePitch > -64f || VerbPoseClips.PunchStrikePitch < -90f || Mathf.Abs(VerbPoseClips.PunchStrikeElbow) > 8f) return false;
            if (VerbPoseClips.PunchGuardPitchStrike < 72f) return false;
            if (VerbPoseClips.PunchGuardPitchStrike - VerbPoseClips.PunchStrikePitch < 140f) return false;
            if (VerbPoseClips.PunchRecoverElbow > -60f || VerbPoseClips.RecoverOpen(0f) > 0.02f) return false;
            if (VerbPoseClips.TagWindupArmPitch > -20f || VerbPoseClips.TagWindupHipYaw != 0f) return false;
            if (VerbPoseClips.TagArmPitch > -52f || Mathf.Abs(VerbPoseClips.TagElbow) > 12f) return false;
            if (Mathf.Abs(VerbPoseClips.TagArmPitch - VerbPoseClips.PunchStrikePitch) < 8f) return false;
            if (VerbPoseClips.ClipForState(VerbPoseClips.StatePunchRecover) != VerbPoseClips.PunchStrike) return false;
            if (VerbPoseClips.ClipForState(VerbPoseClips.StateTag) != VerbPoseClips.TagCatch) return false;
            if (VerbPoseClips.SilhouetteFailures().Count > 0) return false;
            return true;
        }

        public static string ProofLine()
        {
            Beat wind = PunchWindup(0f);
            Beat active = PunchActive(1f);
            Beat recover = PunchRecover(1f);
            Beat tagCoil = Tag(1f);
            Beat tagReach = Tag(1f - TagActiveEnd);
            Beat tagHome = Tag(0.04f);
            return "punch tag pose"
                + " reach=" + ReachMeters.ToString("0.00")
                + " cockPitch=" + VerbPoseClips.PunchCockPitch.ToString("0")
                + " cockYaw=" + VerbPoseClips.PunchCockYaw.ToString("0")
                + " cockRoll=" + VerbPoseClips.PunchCockRoll.ToString("0")
                + " cockElbow=" + VerbPoseClips.PunchCockElbow.ToString("0")
                + " cockHipYaw=" + VerbPoseClips.PunchCockHipYaw.ToString("0")
                + " cockSpineYaw=" + VerbPoseClips.PunchCockSpineYaw.ToString("0")
                + " strikePitch=" + VerbPoseClips.PunchStrikePitch.ToString("0")
                + " strikeYaw=" + VerbPoseClips.PunchStrikeYaw.ToString("0")
                + " strikeRoll=" + VerbPoseClips.PunchStrikeRoll.ToString("0")
                + " strikeElbow=" + VerbPoseClips.PunchStrikeElbow.ToString("0")
                + " guardCock=" + VerbPoseClips.PunchGuardPitchCock.ToString("0")
                + "/" + VerbPoseClips.PunchGuardElbowCock.ToString("0")
                + " guardStrike=" + VerbPoseClips.PunchGuardPitchStrike.ToString("0")
                + "/" + VerbPoseClips.PunchGuardYawStrike.ToString("0")
                + "/" + VerbPoseClips.PunchGuardElbowStrike.ToString("0")
                + " strikeHipYaw=" + VerbPoseClips.PunchStrikeHipYaw.ToString("0")
                + " strikeSpineYaw=" + VerbPoseClips.PunchStrikeSpineYaw.ToString("0")
                + " tagGather=" + VerbPoseClips.TagWindupArmPitch.ToString("0")
                + "/" + VerbPoseClips.TagWindupArmYaw.ToString("0")
                + "/" + VerbPoseClips.TagWindupElbow.ToString("0")
                + " tagReach=" + VerbPoseClips.TagArmPitch.ToString("0")
                + "/" + VerbPoseClips.TagArmYaw.ToString("0")
                + "/" + VerbPoseClips.TagElbow.ToString("0")
                + " tagChest=" + (VerbPoseClips.TagHip + VerbPoseClips.TagSpine).ToString("0")
                + " tagKnee=" + VerbPoseClips.TagKnee.ToString("0")
                + " windSample=" + wind.Sample.ToString("0.00")
                + " windWeight=" + wind.Weight.ToString("0.00")
                + " activeSample=" + active.Sample.ToString("0.00")
                + " recoverWeight=" + recover.Weight.ToString("0.00")
                + " tagCoilSample=" + tagCoil.Sample.ToString("0.00")
                + " tagReachSample=" + tagReach.Sample.ToString("0.00")
                + " tagHomeWeight=" + tagHome.Weight.ToString("0.00")
                + " recoverOpen=" + VerbPoseClips.RecoverOpen(1f).ToString("0.00")
                + " slew=" + PoseSlew.ToString("0")
                + " gate=punch-windup:sample 0 weight 1 for the whole 0.12s, fist beside the head, not a wide float"
                + "; punch-active:Commit(phase) sample 0→1 weight 1, chest-high line, free arm back, reach " + ReachMeters.ToString("0.00")
                + "; punch-recover:sample 1, beat weight 1-SmoothStep across hit 0.15s and miss 0.32s, clip eases strike into the rib chamber then off"
                + "; tag-windup:flinch life 0.." + TagWindupEnd.ToString("0.00") + " sample 0 weight 1, both hands gathered at the chest, no yaw"
                + "; tag-active:life " + TagWindupEnd.ToString("0.00") + ".." + TagActiveEnd.ToString("0.00") + " Commit sample, weight 1, both hands meet at the contact"
                + "; tag-recover:life " + TagActiveEnd.ToString("0.00") + "..1 weight 1-SmoothStep, gate flinch>0.04 unchanged"
                + "; clips PunchStrike and TagCatch; HitConfirmTell and LungeTell untouched"
                + "; rootMotion=0";
        }
    }
}
