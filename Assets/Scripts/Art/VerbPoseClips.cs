using System.Collections.Generic;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Named body clips. DummyLocomotor plays them from verb states.
    /// SlideBody is a feet-first baseball slide: torso leaned back, lead leg out, trail leg tucked, one arm back.
    /// PunchStrike coils the shoulder and the chest, then one arm reaches while the other counters.
    /// TagCatch gathers both shoulders, then both hands meet at one contact. Feel numbers are not in here.
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
        // Negative hip/spine leans the chest back. The lead thigh stays the solved
        // forward line. The trail thigh flexes up and the knee folds under.
        // The arm on the lead-leg side goes back, matching the run's back arm,
        // so the hands do not cross on the way in. Crouch stays chest-up.
        // slideBoost is not in here. No root motion.
        public const float SlideHip = -36f;
        public const float SlideSpine = -14f;
        public const float SlideHead = 54f;
        public const float SlideLeadThigh = -156f;
        public const float SlideLeadYaw = 8f;
        public const float SlideLeadKnee = 6f;
        public const float SlideTrailThigh = 98f;
        public const float SlideTrailYaw = 36f;
        public const float SlideTrailKnee = -108f;
        // Forward arm. Negative pitch reaches toward +Z. Kept as SlideArmPitch
        // so a site that has not split the arms still reaches forward.
        public const float SlideLeadArmPitch = -54f;
        public const float SlideLeadArmYaw = 22f;
        public const float SlideLeadElbow = -14f;
        // Balance arm. Positive pitch is behind the shoulder.
        public const float SlideBalanceArmPitch = 74f;
        public const float SlideBalanceArmYaw = 32f;
        public const float SlideBalanceElbow = -34f;
        public const float SlideArmPitch = SlideLeadArmPitch;
        public const float SlideArmYaw = SlideLeadArmYaw;
        public const float SlideArmRoll = 0f;
        public const float SlideElbow = SlideLeadElbow;
        // Lead shoe stays along the ground. Trail shoe follows the tucked shin
        // so it does not crank flat and skate.
        public const float SlideLeadFoot = 48f;
        public const float SlideLeadFootRoll = -12f;
        public const float SlideTrailFoot = 18f;
        public const float SlideTrailFootRoll = 8f;
        // Visual mesh drop only. The capsule and slideBoost stay put.
        public const float SlideBodyDrop = 0.82f;
        // Enter from run, leave to run or crouch, and a jump cancel.
        // Crouch keeps its own tenth-second blend. See CrouchPose.
        public const float SlideBlendSeconds = 0.10f;
        // The clip weight eases the run enter. The old wedge overlay is off
        // so it cannot snap underneath that weight.
        public const bool RunSlideOverlay = false;

        // The crouch this slide must not match: both knees bent, elbows folded, chest forward.
        // Same numbers as CrouchPose. The slide leans back. This one leans toward the face.
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
        // The cock is the pull-back. The strike is the long line for reach 1.55.
        public const float PunchCockPitch = -88f;
        public const float PunchCockYaw = 72f;
        public const float PunchCockRoll = -22f;
        public const float PunchCockElbow = -112f;
        public const float PunchStrikePitch = -130f;
        public const float PunchStrikeYaw = 46f;
        public const float PunchStrikeRoll = -14f;
        public const float PunchStrikeElbow = -2f;
        public const float PunchGuardPitchCock = -32f;
        public const float PunchGuardYawCock = -18f;
        public const float PunchGuardElbowCock = -78f;
        public const float PunchGuardPitchStrike = 84f;
        public const float PunchGuardYawStrike = -46f;
        public const float PunchGuardElbowStrike = -28f;
        public const float PunchGuardRoll = 12f;
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

        // Both arms share one pitch and meet in front. Not an overhead V, not one fist.
        // The chest leans into the touch. The knees soften and stay clear of the crouch.
        // The windup is a symmetric gather. No yaw, so it is not the punch coil.
        public const float TagWindupArmPitch = 58f;
        public const float TagWindupArmYaw = 36f;
        public const float TagWindupArmRoll = 8f;
        public const float TagWindupElbow = -96f;
        public const float TagWindupSpine = -12f;
        public const float TagWindupHip = -8f;
        public const float TagWindupSpineYaw = 0f;
        public const float TagWindupHipYaw = 0f;
        public const float TagWindupThigh = 8f;
        public const float TagWindupKnee = -20f;
        public const float TagWindupHead = -8f;
        public const float TagArmPitch = -108f;
        public const float TagArmYaw = 20f;
        public const float TagArmRoll = 4f;
        public const float TagElbow = -4f;
        public const float TagSpine = 18f;
        public const float TagHip = 6f;
        public const float TagSpineYaw = 0f;
        public const float TagHipYaw = 0f;
        public const float TagThigh = 18f;
        public const float TagKnee = -46f;
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
                + " gate=run-enter+hold:SmoothStep(_slidePose)/SlideBlendSeconds while sliding"
                + "; run-exit:slideLeave=1-SmoothStep(_dropVis) full weight, clip off"
                + "; crouch-exit:captured still/crouch-walk dt/SlideBlendSeconds, clip off"
                + "; jump-cancel:SmoothStep(_jumpFromSlideIn) dt/SlideBlendSeconds, jump wins, clip off";
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
            };
        }

        public static List<string> SilhouetteFailures()
        {
            var fails = new List<string>();
            float chest = SlideHip + SlideSpine;
            float crouchChest = CrouchHip + CrouchSpine;
            // Negative hip + spine reclines the chest. A crouch chest stays forward, near +46.
            if (SlideHip > -24f || SlideHip < -48f || SlideSpine > -6f || SlideSpine < -22f || chest > -36f || chest < -64f)
                fails.Add("SlideBody torso is not leaned back");
            if (chest > crouchChest - 50f)
                fails.Add("SlideBody chest matches the crouch");
            if (SlideHead < 42f || SlideHead > 68f)
                fails.Add("SlideBody head is not looking along the slide");
            if (SlideLeadThigh > -145f || SlideLeadThigh < -170f)
                fails.Add("SlideBody lead leg is not extended forward");
            if (Mathf.Abs(SlideLeadKnee) > 16f)
                fails.Add("SlideBody lead knee is folded like a crouch");
            if (SlideTrailThigh < 80f || SlideTrailThigh > 116f)
                fails.Add("SlideBody trail thigh is not tucked up");
            if (SlideTrailKnee > -96f || SlideTrailKnee < -124f)
                fails.Add("SlideBody trail leg is not tucked");
            if (Mathf.Abs(SlideTrailYaw) < 24f)
                fails.Add("SlideBody trail knee is not out");
            if (Mathf.Abs(SlideLeadThigh - SlideTrailThigh) < 180f)
                fails.Add("SlideBody legs match each other");
            if (Mathf.Abs(SlideLeadThigh - CrouchThigh) < 40f || Mathf.Abs(SlideTrailThigh - CrouchThigh) < 30f
                || Mathf.Abs(SlideTrailKnee - CrouchKnee) < 24f)
                fails.Add("SlideBody legs match the crouch");
            if (SlideLeadArmPitch > -40f || SlideLeadArmPitch < -72f)
                fails.Add("SlideBody lead arm is not forward");
            if (SlideBalanceArmPitch < 60f || SlideBalanceArmPitch > 90f)
                fails.Add("SlideBody balance arm is not back");
            if (SlideBalanceArmPitch - SlideLeadArmPitch < 110f)
                fails.Add("SlideBody arms match each other");
            if (Mathf.Abs(SlideLeadArmYaw) < 12f || Mathf.Abs(SlideBalanceArmYaw) < 18f)
                fails.Add("SlideBody arms stack on the chest");
            if (Mathf.Abs(SlideLeadElbow) > 22f)
                fails.Add("SlideBody lead elbow is folded like a crouch");
            if (Mathf.Abs(SlideBalanceElbow) < 22f || Mathf.Abs(SlideBalanceElbow) > 48f)
                fails.Add("SlideBody balance elbow is not a soft bend");
            if (Mathf.Abs(SlideLeadFoot - SlideTrailFoot) < 20f)
                fails.Add("SlideBody feet share one angle");
            if (CrouchHip > 40f || Mathf.Abs(CrouchElbow) < 60f || Mathf.Abs(CrouchKnee) < 50f
                || CrouchArmPitch > -24f || CrouchArmPitch < -50f)
                fails.Add("crouch reference no longer reads as a crouch");
            if (Mathf.Abs(IdleArmPitch) > 24f || Mathf.Abs(IdleElbow) > 20f || Mathf.Abs(IdleKnee) > 8f
                || Mathf.Abs(IdleHip) > 8f || Mathf.Abs(IdleSpine) > 8f)
                fails.Add("idle reference no longer reads as a stand");
            if (SlideBodyDrop < 0.74f || SlideBodyDrop > 0.86f || SlideBodyDrop < CrouchDrop + 0.5f)
                fails.Add("SlideBody is not lower than the crouch");
            if (SlideBlendSeconds < 0.08f || SlideBlendSeconds > 0.12f)
                fails.Add("SlideBody blend is not a clean tenth");

            // Punch: fist beside the head, then one long arm and a chest unwind. Not idle, not a crouch.
            if (PunchCockPitch > -65f || PunchCockPitch < -95f || PunchCockYaw < 50f || PunchCockYaw > 78f)
                fails.Add("PunchStrike cock is not beside the head");
            if (Mathf.Abs(PunchCockRoll) < 12f || Mathf.Abs(PunchCockRoll) > 36f)
                fails.Add("PunchStrike cock roll folds into the body");
            if (PunchCockElbow > -92f || PunchCockElbow < -120f)
                fails.Add("PunchStrike cock is not bent beside the head");
            if (Mathf.Abs(PunchCockPitch - IdleArmPitch) < 50f || Mathf.Abs(PunchCockElbow - IdleElbow) < 70f)
                fails.Add("PunchStrike cock matches the idle hang");
            if (PunchStrikePitch > -108f || PunchStrikePitch < -136f)
                fails.Add("PunchStrike strike is not a forward line");
            if (PunchStrikeYaw < 42f || PunchStrikeYaw > 68f)
                fails.Add("PunchStrike strike does not extend off the chest");
            if (Mathf.Abs(PunchStrikeRoll) < 8f || Mathf.Abs(PunchStrikeRoll) > 28f)
                fails.Add("PunchStrike strike roll folds the fist");
            if (Mathf.Abs(PunchStrikeElbow) > 12f)
                fails.Add("PunchStrike strike elbow is folded");
            if (Mathf.Abs(PunchStrikePitch - PunchCockPitch) < 24f || Mathf.Abs(PunchStrikeElbow - PunchCockElbow) < 70f)
                fails.Add("PunchStrike does not open from the cock");
            if (Mathf.Abs(PunchStrikePitch - IdleArmPitch) < 80f && Mathf.Abs(PunchStrikeYaw - IdleArmYaw) < 24f)
                fails.Add("PunchStrike is an arm twitch");
            if (Mathf.Abs(PunchStrikePitch - CrouchArmPitch) < 55f || Mathf.Abs(PunchStrikeElbow - CrouchElbow) < 48f
                || Mathf.Abs(PunchStrikeYaw - CrouchArmYaw) < 20f)
                fails.Add("PunchStrike matches the crouch");
            if (PunchCockHipYaw > -24f || PunchStrikeHipYaw < 18f || PunchCockHipYaw * PunchStrikeHipYaw >= 0f
                || Mathf.Abs(PunchStrikeHipYaw - PunchCockHipYaw) < 48f)
                fails.Add("PunchStrike hips do not unwind");
            if (PunchCockSpineYaw > -32f || PunchStrikeSpineYaw < 30f
                || Mathf.Abs(PunchStrikeSpineYaw - PunchCockSpineYaw) < 70f)
                fails.Add("PunchStrike chest does not twist");
            if (PunchGuardPitchStrike < 48f || PunchGuardPitchStrike - PunchStrikePitch < 150f)
                fails.Add("PunchStrike off arm is not back");
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

            // Tag: both arms to one contact. Soft knees. No twist, so it is not the punch or the crouch.
            float tagChest = TagHip + TagSpine;
            if (TagArmPitch > -88f || TagArmPitch < -112f)
                fails.Add("TagCatch arms are not a reach");
            if (TagArmYaw < 18f || TagArmYaw > 36f)
                fails.Add("TagCatch hands do not meet in front");
            if (Mathf.Abs(TagElbow) > 16f)
                fails.Add("TagCatch arms are folded");
            if (Mathf.Abs(TagArmPitch - IdleArmPitch) < 70f)
                fails.Add("TagCatch is an arm twitch");
            if (Mathf.Abs(TagArmPitch - CrouchArmPitch) < 40f || Mathf.Abs(TagElbow - CrouchElbow) < 40f)
                fails.Add("TagCatch arms match the crouch");
            if (tagChest < 12f || tagChest > 32f || TagSpine < 10f)
                fails.Add("TagCatch chest does not lean into the contact");
            if (TagSpineYaw != 0f || TagHipYaw != 0f)
                fails.Add("TagCatch twists like a punch");
            if (TagKnee > -36f || TagKnee < -52f)
                fails.Add("TagCatch knees do not buckle");
            if (Mathf.Abs(TagKnee - CrouchKnee) < 16f || Mathf.Abs(TagThigh - CrouchThigh) < 28f || Mathf.Abs(TagThigh) > 28f)
                fails.Add("TagCatch matches the crouch");
            if (Mathf.Abs(tagChest - chest) < 40f)
                fails.Add("TagCatch chest matches the slide");
            if (Mathf.Abs(TagArmPitch - SlideArmPitch) < 40f || Mathf.Abs(TagHead - SlideHead) < 40f)
                fails.Add("TagCatch matches SlideBody");
            if (Mathf.Abs(TagArmPitch - PunchStrikePitch) < 8f && Mathf.Abs(TagArmYaw - PunchStrikeYaw) < 12f)
                fails.Add("TagCatch matches PunchStrike");
            if (Mathf.Abs(PunchStrikePitch - PunchGuardPitchStrike) < 80f || Mathf.Abs(TagArmPitch - PunchGuardPitchStrike) < 40f)
                fails.Add("PunchStrike reads as a two-hand catch");
            if (TagWindupArmPitch < 40f || TagWindupArmPitch > 80f || TagWindupElbow > -80f)
                fails.Add("TagCatch windup is not a gather");
            if (TagWindupHipYaw != 0f || TagWindupSpineYaw != 0f)
                fails.Add("TagCatch windup twists like a punch");
            if (Mathf.Abs(TagWindupArmPitch - TagArmPitch) < 140f)
                fails.Add("TagCatch does not open from the gather");
            if (Mathf.Abs(TagWindupArmPitch - PunchCockPitch) < 100f)
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
            return fails;
        }
    }
}
