using System.Collections.Generic;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Named body clips. DummyLocomotor plays them from verb states.
    /// SlideBody is a baseball slide: torso leaned back, lead leg extended, trail leg tucked, one hand on the ground.
    /// PunchStrike coils beside the head, reaches one arm on a chest-high line, then folds that fist back to the ribs.
    /// TagCatch gathers both hands at the chest, then both arms claim one contact in front. Feel numbers are not in here.
    /// </summary>
    public static class VerbPoseClips
    {
        public const string SlideBody = "SlideBody";
        public const string PunchStrike = "PunchStrike";
        public const string TagCatch = "TagCatch";

        public const string StateSlide = "Slide";
        public const string StatePunchWindup = "PunchWindup";
        public const string StatePunchActive = "PunchActive";
        public const string StatePunchRecover = "PunchRecover";
        public const string StateTag = "Tag";

        // Hier Tan rest: hips +X pitches the chest toward +Z (the face).
        // Upper-leg rest is about 180° on X, so thigh pitch does NOT add to the hip.
        // Positive hip/spine pitches the chest forward. A baseball slide uses the
        // negative sign: chest leaned back about 36°. Positive thigh is the lead
        // leg toward +Z. Negative knee bends. The trail knee folds under.
        // The free arm stays forward. The other hand trails back near the ground.
        // Crouch stays a symmetric forward guard. slideBoost is not in here. No root motion.
        // The pose does not take speed, so the read holds at slide-entry and at the end of decay.
        public const float SlideHip = -22f;
        public const float SlideSpine = -14f;
        public const float SlideHead = 50f;
        public const float SlideLeadThigh = 68f;
        public const float SlideLeadYaw = 8f;
        public const float SlideLeadKnee = -10f;
        public const float SlideTrailThigh = 40f;
        public const float SlideTrailYaw = 24f;
        public const float SlideTrailKnee = -130f;
        // Free arm. Negative pitch reaches toward +Z. Kept as SlideArmPitch
        // so a site that has not split the arms still reaches forward.
        public const float SlideLeadArmPitch = -36f;
        public const float SlideLeadArmYaw = 22f;
        public const float SlideLeadElbow = -28f;
        // Trail hand. Positive pitch is behind the shoulder and down, near the ground.
        public const float SlideBalanceArmPitch = 48f;
        public const float SlideBalanceArmYaw = 22f;
        public const float SlideBalanceElbow = -36f;
        public const float SlideArmPitch = SlideLeadArmPitch;
        public const float SlideArmYaw = SlideLeadArmYaw;
        public const float SlideArmRoll = 0f;
        public const float SlideElbow = SlideLeadElbow;
        // Lead shoe stays along the extended shin: -(thigh + knee).
        // The trail shoe follows the tuck. Feet are local. They move with the motor.
        public const float SlideLeadFoot = -58f;
        public const float SlideLeadFootRoll = -6f;
        public const float SlideTrailFoot = 16f;
        public const float SlideTrailFootRoll = 8f;
        // Visual mesh drop only. PoseHipY 1.05 minus this is the crouch-capsule center
        // (crouchHeight 1.05 * 0.5). The pelvis stays inside the crouched controller
        // and off the ground. The capsule and slideBoost stay put.
        public const float SlideBodyDrop = 0.525f;
        // Enter from run, leave to run or crouch, and a jump cancel.
        // Locked at a tenth by the pose handoff and the chase-cam catch. See CrouchPose.
        public const float SlideBlendSeconds = 0.10f;
        // The clip weight eases the run enter. The old wedge overlay is off
        // so it cannot snap underneath that weight.
        public const bool RunSlideOverlay = false;

        // The crouch this slide must not match: both knees bent, elbows folded, chest forward.
        // Same numbers as CrouchPose. The slide is the same direction of lean, lower,
        // with one planted shin and one free trail leg.
        public const float CrouchHip = 28f;
        public const float CrouchSpine = 18f;
        public const float CrouchThigh = 64f;
        public const float CrouchKnee = -80f;
        public const float CrouchElbow = -90f;
        public const float CrouchArmPitch = -44f;
        public const float CrouchArmYaw = 18f;
        public const float CrouchDrop = 0.22f;

        // Idle hang on this rig: a little forward, a little out, elbows nearly straight.
        // A change that stays inside this hang is an arm twitch.
        public const float IdleArmPitch = -12f;
        public const float IdleArmYaw = 12f;
        public const float IdleElbow = -10f;
        public const float IdleHip = 0f;
        public const float IdleSpine = 0f;
        public const float IdleKnee = 0f;

        // Hier upper arm: negative pitch reaches toward +Z (the face). Yaw carries the
        // elbow out from the chest. Pitch below about -150 wraps the fist through the
        // torso. Extra roll folds the hand into the pelvis, so the strike roll stays mild.
        // Positive pitch is the back arm. The right arm strikes. The left arm counters.
        // The cock pulls the fist in beside the head. A wide positive yaw is a float, not a cock.
        // The strike is the long chest-high line for reach 1.55. The recover folds that line
        // back to the ribs. It is not a second cock and it is not the idle hang.
        public const float PunchCockPitch = -80f;
        public const float PunchCockYaw = -16f;
        public const float PunchCockRoll = -16f;
        public const float PunchCockElbow = -110f;
        /// <summary>Yaw on the cocked forearm so the fist misses the head. Not in the hand-position proof.</summary>
        public const float PunchFistYaw = 40f;
        public const float PunchStrikePitch = -74f;
        public const float PunchStrikeYaw = 4f;
        public const float PunchStrikeRoll = -10f;
        public const float PunchStrikeElbow = -4f;
        public const float PunchGuardPitchCock = -26f;
        public const float PunchGuardYawCock = -12f;
        public const float PunchGuardElbowCock = -70f;
        public const float PunchGuardPitchStrike = 84f;
        public const float PunchGuardYawStrike = -18f;
        public const float PunchGuardElbowStrike = -36f;
        public const float PunchGuardRoll = 10f;
        public const float PunchRecoverPitch = -28f;
        public const float PunchRecoverYaw = -6f;
        public const float PunchRecoverRoll = -12f;
        public const float PunchRecoverElbow = -84f;
        public const float PunchRecoverGuardPitch = -18f;
        public const float PunchRecoverGuardYaw = -10f;
        public const float PunchRecoverGuardElbow = -52f;
        public const float PunchRecoverHipYaw = 6f;
        public const float PunchRecoverSpineYaw = 8f;
        public const float PunchRecoverHeadYaw = 4f;
        public const float PunchRecoverLeadThigh = 10f;
        public const float PunchRecoverTrailThigh = -8f;
        public const float PunchRecoverLeadKnee = -14f;
        public const float PunchRecoverTrailKnee = -10f;
        // Coil, then unwind. Opposite signs so the chest does not stay twisted one way.
        public const float PunchCockHipYaw = -44f;
        public const float PunchStrikeHipYaw = 36f;
        public const float PunchCockSpineYaw = -58f;
        public const float PunchStrikeSpineYaw = 52f;
        public const float PunchHipPitch = 10f;
        public const float PunchSpinePitch = 6f;
        public const float PunchHeadPitch = -6f;
        public const float PunchCockHeadYaw = -22f;
        public const float PunchStrikeHeadYaw = 18f;
        // A stance, not a crouch. The knees stay far from CrouchKnee.
        public const float PunchCockLeadThigh = 14f;
        public const float PunchStrikeLeadThigh = 20f;
        public const float PunchCockTrailThigh = -12f;
        public const float PunchStrikeTrailThigh = -18f;
        public const float PunchCockLeadKnee = -18f;
        public const float PunchStrikeLeadKnee = -8f;
        public const float PunchCockTrailKnee = -12f;
        public const float PunchStrikeTrailKnee = -6f;

        // Both arms share one pitch. The gather bends them in front of the chest.
        // The claim opens that bend to one contact. Stored yaw is the inward amount:
        // the pose mirrors it, so the hands meet instead of floating out into a V.
        // No hip or spine yaw, so it is not the punch coil. Not an overhead V, not one fist.
        public const float TagWindupArmPitch = -36f;
        public const float TagWindupArmYaw = 14f;
        public const float TagWindupArmRoll = 6f;
        public const float TagWindupElbow = -96f;
        public const float TagWindupSpine = 4f;
        public const float TagWindupHip = 2f;
        public const float TagWindupSpineYaw = 0f;
        public const float TagWindupHipYaw = 0f;
        public const float TagWindupThigh = 8f;
        public const float TagWindupKnee = -18f;
        public const float TagWindupHead = -4f;
        public const float TagArmPitch = -60f;
        public const float TagArmYaw = 22f;
        public const float TagArmRoll = 4f;
        public const float TagElbow = -8f;
        public const float TagSpine = 16f;
        public const float TagHip = 6f;
        public const float TagSpineYaw = 0f;
        public const float TagHipYaw = 0f;
        public const float TagThigh = 16f;
        public const float TagKnee = -42f;
        public const float TagHead = 8f;

        public struct Bind
        {
            public Quaternion UaL, UaR, LaL, LaR;
            public Quaternion UlL, UlR, LlL, LlR;
            public Quaternion FtL, FtR;
            public Quaternion Spine, Hips, Head;
        }

        public struct Pose
        {
            public Quaternion UaL, UaR, LaL, LaR;
            public Quaternion UlL, UlR, LlL, LlR;
            public Quaternion FtL, FtR;
            public Quaternion Spine, Hips, Head;
        }

        public static string ClipForState(string state)
        {
            if (state == StateSlide) return SlideBody;
            if (state == StatePunchWindup || state == StatePunchActive || state == StatePunchRecover) return PunchStrike;
            if (state == StateTag) return TagCatch;
            return null;
        }

        /// <summary>
        /// Lead-leg side keeps the back arm from the run. Yaw is positive-out;
        /// the right bone mirrors it.
        /// </summary>
        public static void SlideArmOffsets(bool leadLeft, out float pitchL, out float yawL, out float elbowL, out float pitchR, out float yawR, out float elbowR)
        {
            if (leadLeft)
            {
                pitchL = SlideBalanceArmPitch;
                yawL = SlideBalanceArmYaw;
                elbowL = SlideBalanceElbow;
                pitchR = SlideLeadArmPitch;
                yawR = SlideLeadArmYaw;
                elbowR = SlideLeadElbow;
            }
            else
            {
                pitchL = SlideLeadArmPitch;
                yawL = SlideLeadArmYaw;
                elbowL = SlideLeadElbow;
                pitchR = SlideBalanceArmPitch;
                yawR = SlideBalanceArmYaw;
                elbowR = SlideBalanceElbow;
            }
        }

        public static Pose SlideBodyPose(Bind bind, bool leadLeft)
        {
            float thighL = leadLeft ? SlideLeadThigh : SlideTrailThigh;
            float thighR = leadLeft ? SlideTrailThigh : SlideLeadThigh;
            float kneeL = leadLeft ? SlideLeadKnee : SlideTrailKnee;
            float kneeR = leadLeft ? SlideTrailKnee : SlideLeadKnee;
            float yawL = leadLeft ? SlideLeadYaw : SlideTrailYaw;
            float yawR = leadLeft ? -SlideTrailYaw : -SlideLeadYaw;
            float footL = leadLeft ? SlideLeadFoot : SlideTrailFoot;
            float footR = leadLeft ? SlideTrailFoot : SlideLeadFoot;
            float footRollL = leadLeft ? SlideLeadFootRoll : SlideTrailFootRoll;
            float footRollR = leadLeft ? -SlideTrailFootRoll : -SlideLeadFootRoll;
            SlideArmOffsets(leadLeft, out float pitchArmL, out float yawArmL, out float elbowArmL, out float pitchArmR, out float yawArmR, out float elbowArmR);
            return new Pose
            {
                UaL = bind.UaL * Quaternion.Euler(pitchArmL, yawArmL, SlideArmRoll),
                UaR = bind.UaR * Quaternion.Euler(pitchArmR, -yawArmR, -SlideArmRoll),
                LaL = bind.LaL * Quaternion.Euler(elbowArmL, 0f, 0f),
                LaR = bind.LaR * Quaternion.Euler(elbowArmR, 0f, 0f),
                UlL = bind.UlL * Quaternion.Euler(thighL, yawL, 0f),
                UlR = bind.UlR * Quaternion.Euler(thighR, yawR, 0f),
                LlL = bind.LlL * Quaternion.Euler(kneeL, 0f, 0f),
                LlR = bind.LlR * Quaternion.Euler(kneeR, 0f, 0f),
                FtL = bind.FtL * Quaternion.Euler(footL, 0f, footRollL),
                FtR = bind.FtR * Quaternion.Euler(footR, 0f, footRollR),
                Spine = bind.Spine * Quaternion.Euler(SlideSpine, 0f, 0f),
                Hips = bind.Hips * Quaternion.Euler(SlideHip, 0f, 0f),
                Head = bind.Head * Quaternion.Euler(SlideHead, 0f, 0f),
            };
        }

        public static string SlideProofLine()
        {
            return "slide pose"
                + " hip=" + SlideHip.ToString("0.#")
                + " spine=" + SlideSpine.ToString("0.#")
                + " head=" + SlideHead.ToString("0.#")
                + " leadThigh=" + SlideLeadThigh.ToString("0.#")
                + " leadKnee=" + SlideLeadKnee.ToString("0.#")
                + " trailThigh=" + SlideTrailThigh.ToString("0.#")
                + " trailKnee=" + SlideTrailKnee.ToString("0.#")
                + " trailYaw=" + SlideTrailYaw.ToString("0.#")
                + " leadArm=" + SlideLeadArmPitch.ToString("0.#")
                + " balanceArm=" + SlideBalanceArmPitch.ToString("0.#")
                + " drop=" + SlideBodyDrop.ToString("0.##")
                + " blend=" + SlideBlendSeconds.ToString("0.##")
                + " read=back-lean+lead-extended+trail-tuck"
                + " gate=run-enter+hold:SmoothStep(_slidePose)/SlideBlendSeconds while sliding"
                + "; run-exit:slideLeave=1-SmoothStep(_dropVis) full weight, clip off"
                + "; crouch-exit:captured still/crouch-walk dt/SlideBlendSeconds, clip off"
                + "; jump-cancel:SmoothStep(_jumpFromSlideIn) dt/SlideBlendSeconds, jump wins, clip off";
        }

        /// <summary>
        /// Recover shape. 0 is still the strike. 1 is the fist folded back to the ribs.
        /// The chest unwinds. The free arm comes off the backswing. Not a second cock.
        /// </summary>
        public static float RecoverOpen(float progress)
        {
            float p = Mathf.Clamp01(progress);
            float u = (p - 0.12f) / 0.50f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            return Mathf.SmoothStep(0f, 1f, u);
        }

        /// <summary>
        /// Clip weight during recover. The chamber stays on until it has arrived,
        /// then the beat weight eases it onto the gait. Phase time is unchanged.
        /// </summary>
        public static float RecoverKeep(float progress, float beatWeight)
        {
            float p = Mathf.Clamp01(progress);
            float u = (p - 0.66f) / 0.34f;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float release = Mathf.SmoothStep(0f, 1f, u);
            float beat = beatWeight < 0f ? 0f : (beatWeight > 1f ? 1f : beatWeight);
            return Mathf.Lerp(1f, beat, release);
        }

        /// <summary>0 is the shoulder-and-chest coil. 1 is one long arm, chest unwound, the free arm back.</summary>
        public static Pose PunchStrikePose(Bind bind, float sample)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sample));
            float pitchR = Mathf.Lerp(PunchCockPitch, PunchStrikePitch, t);
            float yawR = Mathf.Lerp(PunchCockYaw, PunchStrikeYaw, t);
            float rollR = Mathf.Lerp(PunchCockRoll, PunchStrikeRoll, t);
            float elbowR = Mathf.Lerp(PunchCockElbow, PunchStrikeElbow, t);
            float pitchL = Mathf.Lerp(PunchGuardPitchCock, PunchGuardPitchStrike, t);
            float yawL = Mathf.Lerp(PunchGuardYawCock, PunchGuardYawStrike, t);
            float elbowL = Mathf.Lerp(PunchGuardElbowCock, PunchGuardElbowStrike, t);
            float hipYaw = Mathf.Lerp(PunchCockHipYaw, PunchStrikeHipYaw, t);
            float spineYaw = Mathf.Lerp(PunchCockSpineYaw, PunchStrikeSpineYaw, t);
            return new Pose
            {
                UaL = bind.UaL * Quaternion.Euler(pitchL, yawL, PunchGuardRoll),
                UaR = bind.UaR * Quaternion.Euler(pitchR, yawR, rollR),
                LaL = bind.LaL * Quaternion.Euler(elbowL, 0f, 0f),
                LaR = bind.LaR * Quaternion.Euler(elbowR, 0f, 0f),
                UlL = bind.UlL * Quaternion.Euler(Mathf.Lerp(PunchCockLeadThigh, PunchStrikeLeadThigh, t), 0f, 0f),
                UlR = bind.UlR * Quaternion.Euler(Mathf.Lerp(PunchCockTrailThigh, PunchStrikeTrailThigh, t), 0f, 0f),
                LlL = bind.LlL * Quaternion.Euler(Mathf.Lerp(PunchCockLeadKnee, PunchStrikeLeadKnee, t), 0f, 0f),
                LlR = bind.LlR * Quaternion.Euler(Mathf.Lerp(PunchCockTrailKnee, PunchStrikeTrailKnee, t), 0f, 0f),
                Spine = bind.Spine * Quaternion.Euler(PunchSpinePitch, spineYaw, 0f),
                Hips = bind.Hips * Quaternion.Euler(PunchHipPitch, hipYaw, 0f),
                Head = bind.Head * Quaternion.Euler(PunchHeadPitch, Mathf.Lerp(PunchCockHeadYaw, PunchStrikeHeadYaw, t), 0f),
                FtL = bind.FtL,
                FtR = bind.FtR,
            };
        }

        /// <summary>0 holds the strike. 1 is the rib chamber. Same verb as PunchStrike.</summary>
        public static Pose PunchRecoverPose(Bind bind, float progress)
        {
            float t = RecoverOpen(progress);
            float pitchR = Mathf.Lerp(PunchStrikePitch, PunchRecoverPitch, t);
            float yawR = Mathf.Lerp(PunchStrikeYaw, PunchRecoverYaw, t);
            float rollR = Mathf.Lerp(PunchStrikeRoll, PunchRecoverRoll, t);
            float elbowR = Mathf.Lerp(PunchStrikeElbow, PunchRecoverElbow, t);
            float pitchL = Mathf.Lerp(PunchGuardPitchStrike, PunchRecoverGuardPitch, t);
            float yawL = Mathf.Lerp(PunchGuardYawStrike, PunchRecoverGuardYaw, t);
            float elbowL = Mathf.Lerp(PunchGuardElbowStrike, PunchRecoverGuardElbow, t);
            float hipYaw = Mathf.Lerp(PunchStrikeHipYaw, PunchRecoverHipYaw, t);
            float spineYaw = Mathf.Lerp(PunchStrikeSpineYaw, PunchRecoverSpineYaw, t);
            return new Pose
            {
                UaL = bind.UaL * Quaternion.Euler(pitchL, yawL, PunchGuardRoll),
                UaR = bind.UaR * Quaternion.Euler(pitchR, yawR, rollR),
                LaL = bind.LaL * Quaternion.Euler(elbowL, 0f, 0f),
                LaR = bind.LaR * Quaternion.Euler(elbowR, 0f, 0f),
                UlL = bind.UlL * Quaternion.Euler(Mathf.Lerp(PunchStrikeLeadThigh, PunchRecoverLeadThigh, t), 0f, 0f),
                UlR = bind.UlR * Quaternion.Euler(Mathf.Lerp(PunchStrikeTrailThigh, PunchRecoverTrailThigh, t), 0f, 0f),
                LlL = bind.LlL * Quaternion.Euler(Mathf.Lerp(PunchStrikeLeadKnee, PunchRecoverLeadKnee, t), 0f, 0f),
                LlR = bind.LlR * Quaternion.Euler(Mathf.Lerp(PunchStrikeTrailKnee, PunchRecoverTrailKnee, t), 0f, 0f),
                FtL = bind.FtL,
                FtR = bind.FtR,
                Spine = bind.Spine * Quaternion.Euler(PunchSpinePitch, spineYaw, 0f),
                Hips = bind.Hips * Quaternion.Euler(PunchHipPitch, hipYaw, 0f),
                Head = bind.Head * Quaternion.Euler(PunchHeadPitch, Mathf.Lerp(PunchStrikeHeadYaw, PunchRecoverHeadYaw, t), 0f),
            };
        }

        /// <summary>The active catch. Both hands at the contact.</summary>
        public static Pose TagCatchPose(Bind bind) => TagCatchPose(bind, 1f);

        /// <summary>0 is both shoulders gathered. 1 is both hands at the contact. No torso twist.</summary>
        public static Pose TagCatchPose(Bind bind, float sample)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sample));
            float pitch = Mathf.Lerp(TagWindupArmPitch, TagArmPitch, t);
            float yaw = Mathf.Lerp(TagWindupArmYaw, TagArmYaw, t);
            float roll = Mathf.Lerp(TagWindupArmRoll, TagArmRoll, t);
            float elbow = Mathf.Lerp(TagWindupElbow, TagElbow, t);
            float thigh = Mathf.Lerp(TagWindupThigh, TagThigh, t);
            float knee = Mathf.Lerp(TagWindupKnee, TagKnee, t);
            float spine = Mathf.Lerp(TagWindupSpine, TagSpine, t);
            float hip = Mathf.Lerp(TagWindupHip, TagHip, t);
            float head = Mathf.Lerp(TagWindupHead, TagHead, t);
            return new Pose
            {
                UaL = bind.UaL * Quaternion.Euler(pitch, yaw, roll),
                UaR = bind.UaR * Quaternion.Euler(pitch, -yaw, -roll),
                LaL = bind.LaL * Quaternion.Euler(elbow, 0f, 0f),
                LaR = bind.LaR * Quaternion.Euler(elbow, 0f, 0f),
                UlL = bind.UlL * Quaternion.Euler(thigh, 0f, 0f),
                UlR = bind.UlR * Quaternion.Euler(thigh, 0f, 0f),
                LlL = bind.LlL * Quaternion.Euler(knee, 0f, 0f),
                LlR = bind.LlR * Quaternion.Euler(knee, 0f, 0f),
                Spine = bind.Spine * Quaternion.Euler(spine, TagSpineYaw, 0f),
                Hips = bind.Hips * Quaternion.Euler(hip, TagHipYaw, 0f),
                Head = bind.Head * Quaternion.Euler(head, 0f, 0f),
                FtL = bind.FtL,
                FtR = bind.FtR,
            };
        }

        public static List<string> SilhouetteFailures()
        {
            var fails = new List<string>();
            float chest = SlideHip + SlideSpine;
            float crouchChest = CrouchHip + CrouchSpine;
            // Negative hip + spine leans the chest back. The head pitches forward
            // off that lean so the face looks along the slide.
            if (SlideHip > -16f || SlideHip < -28f || SlideSpine > -10f || SlideSpine < -20f || chest > -30f || chest < -40f)
                fails.Add("SlideBody torso is not leaned back");
            if (chest > 0f || Mathf.Abs(chest - crouchChest) < 50f)
                fails.Add("SlideBody chest matches the crouch");
            if (SlideHead < 44f || SlideHead > 58f || chest + SlideHead < 8f || chest + SlideHead > 22f)
                fails.Add("SlideBody head is not looking forward");
            if (SlideLeadThigh < 60f || SlideLeadThigh > 78f)
                fails.Add("SlideBody lead leg is not extended");
            if (SlideLeadKnee < -20f || SlideLeadKnee > -4f)
                fails.Add("SlideBody lead knee is not extended");
            if (Mathf.Abs(SlideLeadYaw) < 4f || Mathf.Abs(SlideLeadYaw) > 16f)
                fails.Add("SlideBody lead knee crosses the other leg");
            if (SlideTrailThigh < 28f || SlideTrailThigh > 52f)
                fails.Add("SlideBody trail leg is not tucked under");
            if (SlideTrailKnee > -115f || SlideTrailKnee < -142f)
                fails.Add("SlideBody trail knee is not tucked");
            if (Mathf.Abs(SlideTrailYaw) < 16f || Mathf.Abs(SlideTrailYaw) > 32f)
                fails.Add("SlideBody trail leg clips the pelvis");
            if (Mathf.Abs(SlideLeadThigh - SlideTrailThigh) < 20f)
                fails.Add("SlideBody legs match each other");
            if (Mathf.Abs(SlideTrailKnee - CrouchKnee) < 30f || Mathf.Abs(SlideLeadKnee - CrouchKnee) < 40f)
                fails.Add("SlideBody legs match the crouch");
            if (Mathf.Abs(-(SlideLeadThigh + SlideLeadKnee) - SlideLeadFoot) > 8f)
                fails.Add("SlideBody lead sole is not along the shin");
            if (SlideLeadArmPitch > -24f || SlideLeadArmPitch < -48f)
                fails.Add("SlideBody free arm is not forward");
            if (SlideBalanceArmPitch < 36f || SlideBalanceArmPitch > 60f)
                fails.Add("SlideBody trail hand is not back");
            if (SlideBalanceArmPitch - SlideLeadArmPitch < 70f)
                fails.Add("SlideBody arms match each other");
            if (Mathf.Abs(SlideLeadArmYaw) < 18f || Mathf.Abs(SlideBalanceArmYaw) < 18f)
                fails.Add("SlideBody arms stack on the chest");
            if (Mathf.Abs(SlideLeadArmYaw) > 40f || Mathf.Abs(SlideBalanceArmYaw) > 40f)
                fails.Add("SlideBody arms float wide");
            if (SlideLeadElbow > -18f || SlideLeadElbow < -44f)
                fails.Add("SlideBody free elbow is clipped");
            if (SlideBalanceElbow > -22f || SlideBalanceElbow < -48f)
                fails.Add("SlideBody trail elbow is clipped");
            if (Mathf.Abs(SlideLeadFoot - SlideTrailFoot) < 16f)
                fails.Add("SlideBody feet share one angle");
            if (CrouchHip > 40f || Mathf.Abs(CrouchElbow) < 60f || Mathf.Abs(CrouchKnee) < 50f
                || CrouchArmPitch > -24f || CrouchArmPitch < -50f)
                fails.Add("crouch reference no longer reads as a crouch");
            if (Mathf.Abs(IdleArmPitch) > 24f || Mathf.Abs(IdleElbow) > 20f || Mathf.Abs(IdleKnee) > 8f
                || Mathf.Abs(IdleHip) > 8f || Mathf.Abs(IdleSpine) > 8f)
                fails.Add("idle reference no longer reads as a stand");
            float pelvis = 1.05f - SlideBodyDrop;
            if (SlideBodyDrop < 0.50f || SlideBodyDrop > 0.56f || SlideBodyDrop < CrouchDrop + 0.16f
                || Mathf.Abs(pelvis - 0.525f) > 0.02f)
                fails.Add("SlideBody pelvis does not match the crouch capsule");
            if (SlideBlendSeconds < 0.08f || SlideBlendSeconds > 0.12f)
                fails.Add("SlideBody blend is not a clean tenth");
            foreach (string hit in ClearanceFailures())
                fails.Add(hit);

            // Punch: fist beside the head, then one chest-high line, then a rib chamber.
            // A wide positive cock yaw is a float. Pitch below -140 wraps the fist through the torso.
            if (PunchCockPitch > -72f || PunchCockPitch < -92f || PunchCockYaw > -8f || PunchCockYaw < -28f)
                fails.Add("PunchStrike cock is not beside the head");
            if (Mathf.Abs(PunchCockRoll) < 8f || Mathf.Abs(PunchCockRoll) > 28f)
                fails.Add("PunchStrike cock roll folds into the body");
            if (PunchCockElbow > -96f || PunchCockElbow < -120f)
                fails.Add("PunchStrike cock is not bent beside the head");
            if (Mathf.Abs(PunchCockPitch - IdleArmPitch) < 50f || Mathf.Abs(PunchCockElbow - IdleElbow) < 70f)
                fails.Add("PunchStrike cock matches the idle hang");
            if (PunchStrikePitch > -64f || PunchStrikePitch < -90f)
                fails.Add("PunchStrike strike is not a forward line");
            if (PunchStrikeYaw < 0f || PunchStrikeYaw > 16f)
                fails.Add("PunchStrike strike does not extend off the chest");
            if (Mathf.Abs(PunchStrikeRoll) < 6f || Mathf.Abs(PunchStrikeRoll) > 22f)
                fails.Add("PunchStrike strike roll folds the fist");
            if (Mathf.Abs(PunchStrikeElbow) > 8f)
                fails.Add("PunchStrike strike elbow is folded");
            if (Mathf.Abs(PunchStrikeElbow - PunchCockElbow) < 70f)
                fails.Add("PunchStrike does not open from the cock");
            if (Mathf.Abs(PunchStrikePitch - IdleArmPitch) < 48f)
                fails.Add("PunchStrike is an arm twitch");
            if (Mathf.Abs(PunchStrikeElbow - CrouchElbow) < 48f)
                fails.Add("PunchStrike matches the crouch");
            if (PunchCockHipYaw > -24f || PunchStrikeHipYaw < 18f || PunchCockHipYaw * PunchStrikeHipYaw >= 0f
                || Mathf.Abs(PunchStrikeHipYaw - PunchCockHipYaw) < 48f)
                fails.Add("PunchStrike hips do not unwind");
            if (PunchCockSpineYaw > -32f || PunchStrikeSpineYaw < 30f
                || Mathf.Abs(PunchStrikeSpineYaw - PunchCockSpineYaw) < 70f)
                fails.Add("PunchStrike chest does not twist");
            if (PunchGuardPitchStrike < 70f || PunchGuardPitchStrike - PunchStrikePitch < 140f)
                fails.Add("PunchStrike off arm is not back");
            if (PunchRecoverElbow > -60f || PunchRecoverPitch > -16f || PunchRecoverPitch < PunchStrikePitch)
                fails.Add("PunchStrike recover is not a rib chamber");
            if (Mathf.Abs(PunchRecoverElbow - PunchCockElbow) < 16f || PunchRecoverPitch < PunchCockPitch)
                fails.Add("PunchStrike recover is a second cock");
            if (Mathf.Abs(PunchGuardElbowStrike) < 22f)
                fails.Add("PunchStrike off arm is a second punch");
            if (Mathf.Abs(PunchGuardPitchCock) > 48f || Mathf.Abs(PunchGuardElbowCock) < 50f)
                fails.Add("PunchStrike cock guard leaves the ribs");
            if (Mathf.Abs(PunchCockLeadKnee) > 22f || Mathf.Abs(PunchStrikeLeadKnee) > 22f
                || Mathf.Abs(PunchCockTrailKnee) > 22f || Mathf.Abs(PunchStrikeTrailKnee) > 22f
                || Mathf.Abs(PunchCockLeadKnee - CrouchKnee) < 40f || Mathf.Abs(PunchStrikeTrailKnee - CrouchKnee) < 40f)
                fails.Add("PunchStrike knees match the crouch");
            float punchChest = PunchHipPitch + PunchSpinePitch;
            if (Mathf.Abs(PunchHipPitch) > 18f || Mathf.Abs(PunchSpinePitch) > 16f || punchChest > 28f
                || Mathf.Abs(punchChest - chest) < 40f)
                fails.Add("PunchStrike chest matches the slide");

            // Tag: both hands gather at the chest, then both arms claim one contact.
            // Stored yaw pulls inward. No twist, so it is not the punch or the crouch.
            float tagChest = TagHip + TagSpine;
            if (TagArmPitch > -52f || TagArmPitch < -76f)
                fails.Add("TagCatch arms are not a reach");
            if (TagArmYaw < 12f || TagArmYaw > 32f)
                fails.Add("TagCatch hands do not meet in front");
            if (Mathf.Abs(TagElbow) > 16f || Mathf.Abs(TagElbow) < 4f)
                fails.Add("TagCatch arms are folded");
            if (Mathf.Abs(TagArmPitch - IdleArmPitch) < 40f)
                fails.Add("TagCatch is an arm twitch");
            if (Mathf.Abs(TagArmPitch - CrouchArmPitch) < 20f && Mathf.Abs(TagElbow - CrouchElbow) < 40f)
                fails.Add("TagCatch arms match the crouch");
            if (tagChest < 12f || tagChest > 32f || TagSpine < 10f)
                fails.Add("TagCatch chest does not lean into the contact");
            if (TagSpineYaw != 0f || TagHipYaw != 0f)
                fails.Add("TagCatch twists like a punch");
            if (TagKnee > -36f || TagKnee < -52f)
                fails.Add("TagCatch knees do not buckle");
            if (Mathf.Abs(TagKnee - CrouchKnee) < 16f || Mathf.Abs(TagThigh - CrouchThigh) < 28f || Mathf.Abs(TagThigh) > 28f)
                fails.Add("TagCatch matches the crouch");
            if (Mathf.Abs(tagChest - chest) < 30f)
                fails.Add("TagCatch chest matches the slide");
            if (Mathf.Abs(TagArmPitch - SlideArmPitch) < 20f || Mathf.Abs(TagHead - SlideHead) < 40f)
                fails.Add("TagCatch matches SlideBody");
            if (Mathf.Abs(TagArmPitch - PunchStrikePitch) < 8f && Mathf.Abs(TagElbow - PunchStrikeElbow) < 8f)
                fails.Add("TagCatch matches PunchStrike");
            if (Mathf.Abs(PunchStrikePitch - PunchGuardPitchStrike) < 80f || Mathf.Abs(TagArmPitch - PunchGuardPitchStrike) < 40f)
                fails.Add("PunchStrike reads as a two-hand catch");
            if (TagWindupArmPitch > -20f || TagWindupArmPitch < -50f || TagWindupElbow > -80f)
                fails.Add("TagCatch windup is not a gather");
            if (TagWindupHipYaw != 0f || TagWindupSpineYaw != 0f)
                fails.Add("TagCatch windup twists like a punch");
            if (TagArmPitch > TagWindupArmPitch - 16f || Mathf.Abs(TagElbow - TagWindupElbow) < 60f)
                fails.Add("TagCatch does not open from the gather");
            if (TagWindupHipYaw != 0f || PunchCockHipYaw == 0f)
                fails.Add("TagCatch windup matches the punch coil");
            if (Mathf.Abs(TagWindupKnee) > 28f || Mathf.Abs(TagWindupKnee - CrouchKnee) < 30f)
                fails.Add("TagCatch windup matches the crouch");
            if (ClipForState(StateSlide) != SlideBody)
                fails.Add("Slide does not play SlideBody");
            if (ClipForState(StatePunchWindup) != PunchStrike || ClipForState(StatePunchActive) != PunchStrike
                || ClipForState(StatePunchRecover) != PunchStrike)
                fails.Add("punch states do not play PunchStrike");
            if (ClipForState(StateTag) != TagCatch)
                fails.Add("Tag does not play TagCatch");
            foreach (string hit in PunchTagClearance())
                fails.Add(hit);
            return fails;
        }

        /// <summary>
        /// Grounded baseball slide. Torso back, lead leg extended, trail leg tucked,
        /// one hand near the ground. The same shape at entry speed and at the end of
        /// decay. Feet stay on the skeleton, so they only move with the motor.
        /// DummyRunner plays the same SlideBody. slideBoost and root motion stay out.
        /// </summary>
        public static string PolishProofLine()
        {
            float sole = PlantSoleY();
            float lift = TrailSoleY() - sole;
            float pelvis = 1.05f - SlideBodyDrop;
            return "slide-pose-polish"
                + " torso=back"
                + " chest=" + (SlideHip + SlideSpine).ToString("0")
                + " head=" + SlideHead.ToString("0")
                + " lead=extended"
                + " thigh=" + SlideLeadThigh.ToString("0")
                + " knee=" + SlideLeadKnee.ToString("0")
                + " trail=tuck"
                + " trailThigh=" + SlideTrailThigh.ToString("0")
                + " trailKnee=" + SlideTrailKnee.ToString("0")
                + " hand=trail"
                + " drop=" + SlideBodyDrop.ToString("0.00")
                + " pelvis=" + pelvis.ToString("0.00")
                + " blend=" + SlideBlendSeconds.ToString("0.00")
                + " sole=" + sole.ToString("0.00")
                + " trailLift=" + lift.ToString("0.00")
                + " intersect=" + ClearanceFailures().Count.ToString("0")
                + " dummy=SlideBody"
                + " speed=held"
                + " feet=local"
                + " slideBoost=0"
                + " rootMotion=0";
        }

        public static bool PolishHolds()
        {
            if (ClearanceFailures().Count != 0) return false;
            if (SlideBlendSeconds < 0.08f || SlideBlendSeconds > 0.12f) return false;
            float chest = SlideHip + SlideSpine;
            if (chest > -30f || chest < -40f) return false;
            if (SlideLeadThigh <= 0f || SlideLeadKnee >= -2f || SlideLeadKnee < -24f) return false;
            if (SlideTrailKnee > -100f) return false;
            if (SlideBalanceArmPitch <= 0f || SlideLeadArmPitch >= 0f) return false;
            float sole = PlantSoleY();
            float trail = TrailSoleY();
            if (sole < -0.01f || sole > 0.05f) return false;
            if (trail < sole + 0.02f || trail > 0.16f) return false;
            float pelvis = 1.05f - SlideBodyDrop;
            if (Mathf.Abs(pelvis - 0.525f) > 0.02f || pelvis < 0.20f) return false;
            return true;
        }

        /// <summary>
        /// Punch and TagCatch read. Windup beside the head, strike a chest-high line,
        /// recover a rib chamber, gather at the chest, claim at one contact.
        /// Hands stay under the head and inside a shoulder-width flare. Reach and
        /// the phase windows are not retuned. DummyRunner plays the same two clips.
        /// </summary>
        public static string PunchTagPolishProofLine()
        {
            Vector3 cock = StrikeHand(PunchCockPitch, PunchCockYaw, PunchCockElbow);
            Vector3 strike = StrikeHand(PunchStrikePitch, PunchStrikeYaw, PunchStrikeElbow);
            Vector3 home = StrikeHand(PunchRecoverPitch, PunchRecoverYaw, PunchRecoverElbow);
            Vector3 gather = TagHand(TagWindupHip + TagWindupSpine, TagWindupArmPitch, TagWindupArmYaw, TagWindupElbow);
            Vector3 claim = TagHand(TagHip + TagSpine, TagArmPitch, TagArmYaw, TagElbow);
            return "punch-tag-polish"
                + " windup=beside-head"
                + " cockY=" + cock.y.ToString("0.00")
                + " strike=forward-line"
                + " strikeZ=" + strike.z.ToString("0.00")
                + " recover=rib-chamber"
                + " homeZ=" + home.z.ToString("0.00")
                + " tagGather=chest"
                + " gatherZ=" + gather.z.ToString("0.00")
                + " tagClaim=meet"
                + " claimZ=" + claim.z.ToString("0.00")
                + " claimX=" + claim.x.ToString("0.00")
                + " reach=" + PunchTagPose.ReachMeters.ToString("0.00")
                + " windupS=0.12"
                + " activeS=0.10"
                + " hitRecoverS=0.15"
                + " missRecoverS=0.32"
                + " intersect=" + PunchTagClearance().Count.ToString("0")
                + " dummy=shared"
                + " rootMotion=0";
        }

        public static bool PunchTagPolishHolds()
        {
            if (PunchTagClearance().Count != 0) return false;
            if (Mathf.Abs(PunchTagPose.ReachMeters - 1.55f) > 0.001f) return false;
            if (PunchTagPose.RootMotion) return false;
            if (RecoverOpen(0f) > 0.02f || RecoverOpen(1f) < 0.98f || RecoverOpen(0.5f) < 0.7f) return false;
            if (RecoverKeep(0f, 0f) < 0.98f || RecoverKeep(1f, 0f) > 0.02f || RecoverKeep(0.5f, 0.2f) < 0.9f) return false;
            if (ClipForState(StatePunchWindup) != PunchStrike || ClipForState(StatePunchRecover) != PunchStrike) return false;
            if (ClipForState(StateTag) != TagCatch) return false;
            if (!DummyPosePaths.Allows("DummyRunner", DummyPosePaths.Punch)) return false;
            if (!DummyPosePaths.Allows("DummyRunner", DummyPosePaths.Tag)) return false;
            Vector3 cock = StrikeHand(PunchCockPitch, PunchCockYaw, PunchCockElbow);
            Vector3 strike = StrikeHand(PunchStrikePitch, PunchStrikeYaw, PunchStrikeElbow);
            Vector3 home = StrikeHand(PunchRecoverPitch, PunchRecoverYaw, PunchRecoverElbow);
            Vector3 gather = TagHand(TagWindupHip + TagWindupSpine, TagWindupArmPitch, TagWindupArmYaw, TagWindupElbow);
            Vector3 claim = TagHand(TagHip + TagSpine, TagArmPitch, TagArmYaw, TagElbow);
            if (strike.z < cock.z + 0.25f) return false;
            if (home.z > strike.z - 0.08f) return false;
            if (home.y > cock.y - 0.12f) return false;
            if (claim.z < gather.z + 0.20f) return false;
            if (Mathf.Abs(claim.x) > Mathf.Abs(gather.x)) return false;
            return true;
        }

        // Hier lengths. Thigh pitch does not add to the hip. Positive thigh is +Z.
        const float PoseHipY = 1.05f;
        const float PoseHipX = 0.118f;
        const float PoseUl = 0.54f;
        const float PoseLl = 0.49f;
        const float PoseUa = 0.37f;
        const float PoseLa = 0.33f;
        const float PoseShX = 0.235f;
        const float PoseShZ = -0.06f;

        static Vector3 Rx(Vector3 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            return new Vector3(v.x, v.y * c - v.z * s, v.y * s + v.z * c);
        }

        static Vector3 Ry(Vector3 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);
            return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        static void Limb(float sx, float thigh, float yawOut, float knee, out Vector3 hip, out Vector3 kneeP, out Vector3 ankle)
        {
            hip = new Vector3(sx * PoseHipX, PoseHipY, -0.02f);
            Vector3 down = new Vector3(0f, -1f, 0f);
            Vector3 d = Ry(Rx(down, -thigh), yawOut * sx).normalized;
            kneeP = hip + d * PoseUl;
            Vector3 ds = Ry(Rx(down, -(thigh + knee)), yawOut * sx).normalized;
            ankle = kneeP + ds * PoseLl;
        }

        static void Arm(float sx, float chestPitch, float pitch, float yawOut, float elbow, out Vector3 shoulder, out Vector3 elbowP, out Vector3 hand)
        {
            float spineToSh = 1.40f - PoseHipY;
            Vector3 up = Rx(new Vector3(0f, 1f, 0f), chestPitch);
            Vector3 basis = new Vector3(0f, PoseHipY, 0f) + up * spineToSh;
            shoulder = basis + Rx(new Vector3(sx * PoseShX, 0f, PoseShZ), chestPitch);
            float outA = 24f * Mathf.Deg2Rad;
            float fwdA = 10f * Mathf.Deg2Rad;
            Vector3 rest = new Vector3(sx * Mathf.Sin(outA), -Mathf.Cos(outA), Mathf.Sin(fwdA));
            Vector3 d = Ry(Rx(rest, pitch), yawOut * sx).normalized;
            elbowP = shoulder + d * PoseUa;
            Vector3 fd = Rx(d, elbow).normalized;
            hand = elbowP + fd * PoseLa;
        }

        // yawOut follows the euler Y the pose writes: right uses it directly, left is mirrored by sx.
        static Vector3 StrikeHand(float pitch, float eulerY, float elbow)
        {
            Arm(1f, PunchHipPitch + PunchSpinePitch, pitch, eulerY, elbow, out _, out _, out Vector3 hand);
            return hand;
        }

        static Vector3 TagHand(float chestPitch, float pitch, float storedYaw, float elbow)
        {
            // Pose writes -storedYaw on the right bone. That inward yaw is the claim.
            Arm(1f, chestPitch, pitch, -storedYaw, elbow, out _, out _, out Vector3 hand);
            return hand;
        }

        static List<string> PunchTagClearance()
        {
            var fails = new List<string>();
            float chest = PunchHipPitch + PunchSpinePitch;
            void Hand(string name, float sx, float chestPitch, float pitch, float eulerY, float elbow, float minY, float maxY, float minZ, float maxZ, float maxAbsX)
            {
                float yawOut = eulerY * sx;
                Arm(sx, chestPitch, pitch, yawOut, elbow, out Vector3 sh, out Vector3 el, out Vector3 hd);
                if (hd.y < minY || hd.y > maxY)
                    fails.Add(name + " hand floats");
                if (hd.z < minZ || hd.z > maxZ)
                    fails.Add(name + " hand is not on its beat");
                if (Mathf.Abs(hd.x) > maxAbsX)
                    fails.Add(name + " arm is wide");
                if (hd.y > 1.82f || el.y > 1.88f)
                    fails.Add(name + " arm is overhead");
                Vector3 chestC = new Vector3(0f, PoseHipY, 0f) + Rx(new Vector3(0f, 1f, 0f), chestPitch) * 0.28f;
                float worst = 1e9f;
                for (int s = 3; s <= 10; s++)
                {
                    Vector3 p = sh + (el - sh) * (s / 10f);
                    float dist = (p - chestC).magnitude;
                    if (dist < worst) worst = dist;
                }
                if (worst < 0.10f)
                    fails.Add(name + " arm clips the chest");
            }

            Hand("punch windup", 1f, chest, PunchCockPitch, PunchCockYaw, PunchCockElbow, 1.48f, 1.78f, 0.05f, 0.42f, 0.48f);
            Hand("punch guard", -1f, chest, PunchGuardPitchCock, PunchGuardYawCock, PunchGuardElbowCock, 0.85f, 1.45f, 0.05f, 0.70f, 0.75f);
            Hand("punch strike", 1f, chest, PunchStrikePitch, PunchStrikeYaw, PunchStrikeElbow, 1.15f, 1.55f, 0.52f, 0.85f, 0.66f);
            Hand("punch off", -1f, chest, PunchGuardPitchStrike, PunchGuardYawStrike, PunchGuardElbowStrike, 0.70f, 1.35f, -0.80f, -0.25f, 0.55f);
            Hand("punch recover", 1f, chest, PunchRecoverPitch, PunchRecoverYaw, PunchRecoverElbow, 1.05f, 1.55f, 0.28f, 0.58f, 0.62f);
            float gatherChest = TagWindupHip + TagWindupSpine;
            float claimChest = TagHip + TagSpine;
            Hand("tag gather", 1f, gatherChest, TagWindupArmPitch, -TagWindupArmYaw, TagWindupElbow, 1.15f, 1.58f, 0.25f, 0.58f, 0.52f);
            Hand("tag gather L", -1f, gatherChest, TagWindupArmPitch, TagWindupArmYaw, TagWindupElbow, 1.15f, 1.58f, 0.25f, 0.58f, 0.52f);
            Hand("tag claim", 1f, claimChest, TagArmPitch, -TagArmYaw, TagElbow, 1.10f, 1.55f, 0.60f, 0.95f, 0.42f);
            Hand("tag claim L", -1f, claimChest, TagArmPitch, TagArmYaw, TagElbow, 1.10f, 1.55f, 0.60f, 0.95f, 0.42f);

            Vector3 strike = StrikeHand(PunchStrikePitch, PunchStrikeYaw, PunchStrikeElbow);
            Vector3 cock = StrikeHand(PunchCockPitch, PunchCockYaw, PunchCockElbow);
            Vector3 home = StrikeHand(PunchRecoverPitch, PunchRecoverYaw, PunchRecoverElbow);
            if (strike.z < cock.z + 0.25f)
                fails.Add("punch strike does not leave the cock");
            if (home.z > strike.z - 0.08f || home.y > cock.y - 0.12f)
                fails.Add("punch recover does not leave the strike");
            Vector3 gather = TagHand(gatherChest, TagWindupArmPitch, TagWindupArmYaw, TagWindupElbow);
            Vector3 claim = TagHand(claimChest, TagArmPitch, TagArmYaw, TagElbow);
            if (claim.z < gather.z + 0.20f)
                fails.Add("tag claim does not leave the gather");
            if (Mathf.Abs(claim.x) > Mathf.Abs(gather.x) - 0.04f)
                fails.Add("tag claim hands do not meet");
            if (Mathf.Abs(gather.x) < 0.12f)
                fails.Add("tag gather hands clip");
            return fails;
        }

        static Vector3 Drop(Vector3 p) => new Vector3(p.x, p.y - SlideBodyDrop, p.z);

        public static float PlantSoleY()
        {
            Limb(-1f, SlideLeadThigh, SlideLeadYaw, SlideLeadKnee, out _, out _, out Vector3 ankle);
            return ankle.y - 0.035f - SlideBodyDrop;
        }

        public static float TrailSoleY()
        {
            Limb(1f, SlideTrailThigh, SlideTrailYaw, SlideTrailKnee, out _, out _, out Vector3 ankle);
            return ankle.y - 0.03f - SlideBodyDrop;
        }

        static float SegDist(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            float best = 1e9f;
            for (int i = 0; i <= 10; i++)
            {
                Vector3 p = a + (b - a) * (i / 10f);
                for (int j = 0; j <= 10; j++)
                {
                    Vector3 q = c + (d - c) * (j / 10f);
                    float dist = (p - q).magnitude;
                    if (dist < best) best = dist;
                }
            }
            return best;
        }

        static List<string> ClearanceFailures()
        {
            var fails = new List<string>();
            float chestPitch = SlideHip + SlideSpine;
            Limb(-1f, SlideLeadThigh, SlideLeadYaw, SlideLeadKnee, out Vector3 lHip, out Vector3 lKnee, out Vector3 lAnkle);
            Limb(1f, SlideTrailThigh, SlideTrailYaw, SlideTrailKnee, out Vector3 tHip, out Vector3 tKnee, out Vector3 tAnkle);
            Arm(1f, chestPitch, SlideLeadArmPitch, SlideLeadArmYaw, SlideLeadElbow, out Vector3 fSh, out Vector3 fEl, out Vector3 fHand);
            Arm(-1f, chestPitch, SlideBalanceArmPitch, SlideBalanceArmYaw, SlideBalanceElbow, out Vector3 bSh, out Vector3 bEl, out Vector3 bHand);

            var names = new List<string>();
            var a = new List<Vector3>();
            var b = new List<Vector3>();
            var r = new List<float>();
            void Add(string name, Vector3 p, Vector3 q, float radius)
            {
                names.Add(name);
                a.Add(Drop(p));
                b.Add(Drop(q));
                r.Add(radius);
            }
            Add("leadThigh", lHip, lKnee, 0.078f);
            Add("leadShin", lKnee, lAnkle, 0.048f);
            Add("trailThigh", tHip, tKnee, 0.078f);
            Add("trailShin", tKnee, tAnkle, 0.048f);
            Add("fwdArm", fSh, fEl, 0.058f);
            Add("fwdFore", fEl, fHand, 0.038f);
            Add("backArm", bSh, bEl, 0.058f);
            Add("backFore", bEl, bHand, 0.038f);

            for (int i = 0; i < names.Count; i++)
            {
                for (int j = i + 1; j < names.Count; j++)
                {
                    bool linked =
                        (names[i] == "leadThigh" && names[j] == "leadShin")
                        || (names[i] == "trailThigh" && names[j] == "trailShin")
                        || (names[i] == "fwdArm" && names[j] == "fwdFore")
                        || (names[i] == "backArm" && names[j] == "backFore");
                    if (linked) continue;
                    float dist = SegDist(a[i], b[i], a[j], b[j]);
                    float need = r[i] + r[j] - 0.012f;
                    if (dist < need)
                        fails.Add("SlideBody " + names[i] + " intersects " + names[j]);
                }
                float low = 1e9f;
                for (int s = 0; s <= 10; s++)
                {
                    Vector3 p = a[i] + (b[i] - a[i]) * (s / 10f);
                    float under = p.y - r[i];
                    if (under < low) low = under;
                }
                if (low < -0.02f)
                    fails.Add("SlideBody " + names[i] + " is in the ground");
            }

            Vector3 pelvis = Drop(new Vector3(0f, PoseHipY, 0f));
            Vector3 chest = Drop(new Vector3(0f, PoseHipY, 0f) + Rx(new Vector3(0f, 1f, 0f), chestPitch) * 0.30f);
            Vector3 headDir = Rx(new Vector3(0f, 1f, 0f), chestPitch + SlideHead);
            Vector3 head = Drop(new Vector3(0f, PoseHipY, 0f) + Rx(new Vector3(0f, 1f, 0f), chestPitch) * 0.36f + headDir * 0.16f);
            void Sphere(string name, Vector3 center, float radius, bool thighSocket, bool shoulderRoot)
            {
                if (center.y - radius < -0.02f)
                    fails.Add("SlideBody " + name + " is in the ground");
                for (int i = 0; i < names.Count; i++)
                {
                    bool thigh = names[i] == "leadThigh" || names[i] == "trailThigh";
                    bool upper = names[i] == "fwdArm" || names[i] == "backArm";
                    float worst = 1e9f;
                    int start = 0;
                    if (thighSocket && thigh) start = 4;
                    if (shoulderRoot && upper) start = 4;
                    for (int s = start; s <= 10; s++)
                    {
                        Vector3 p = a[i] + (b[i] - a[i]) * (s / 10f);
                        float dist = (p - center).magnitude;
                        if (dist < worst) worst = dist;
                    }
                    float allow = (thighSocket && thigh) || (shoulderRoot && upper) ? 0.02f : 0.01f;
                    if (worst < radius + r[i] - allow)
                        fails.Add("SlideBody " + name + " intersects " + names[i]);
                }
            }
            Sphere("pelvis", pelvis, 0.115f, true, false);
            Sphere("chest", chest, 0.125f, false, true);
            Sphere("head", head, 0.11f, false, false);

            if (PlantSoleY() < -0.01f)
                fails.Add("SlideBody lead sole is buried");
            if (TrailSoleY() < PlantSoleY())
                fails.Add("SlideBody trail foot is not free");
            return fails;
        }
    }
}
