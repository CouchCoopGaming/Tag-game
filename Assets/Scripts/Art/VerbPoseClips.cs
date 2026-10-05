using System.Collections.Generic;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Named body clips. DummyLocomotor plays them from verb states.
    /// SlideBody is a low athletic crouch: torso forward, lead foot planted, free leg trailing.
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
        // Positive hip/spine pitches the chest forward. Positive thigh is the
        // forward plant, same sign as the run. Negative thigh trails behind.
        // Negative knee bends. The arm on the lead-leg side stays back and low,
        // matching the run's back arm, so the hands do not cross on the way in.
        // Crouch stays a symmetric guard. slideBoost is not in here. No root motion.
        public const float SlideHip = 36f;
        public const float SlideSpine = 24f;
        public const float SlideHead = -34f;
        public const float SlideLeadThigh = 78f;
        public const float SlideLeadYaw = 10f;
        public const float SlideLeadKnee = -96f;
        public const float SlideTrailThigh = -52f;
        public const float SlideTrailYaw = 18f;
        public const float SlideTrailKnee = -14f;
        // Forward arm. Negative pitch reaches toward +Z. Kept as SlideArmPitch
        // so a site that has not split the arms still reaches forward.
        // Soft elbow and a modest reach, so the hand does not spear the chest.
        public const float SlideLeadArmPitch = -32f;
        public const float SlideLeadArmYaw = 28f;
        public const float SlideLeadElbow = -30f;
        // Balance arm. Positive pitch is behind the shoulder, kept low so it
        // does not float up past the head.
        public const float SlideBalanceArmPitch = 28f;
        public const float SlideBalanceArmYaw = 26f;
        public const float SlideBalanceElbow = -36f;
        public const float SlideArmPitch = SlideLeadArmPitch;
        public const float SlideArmYaw = SlideLeadArmYaw;
        public const float SlideArmRoll = 0f;
        public const float SlideElbow = SlideLeadElbow;
        // Lead shoe levels on the plant: -(thigh + knee). Trail shoe points
        // along the free shin instead of cranking flat.
        public const float SlideLeadFoot = 18f;
        public const float SlideLeadFootRoll = -8f;
        public const float SlideTrailFoot = 40f;
        public const float SlideTrailFootRoll = 6f;
        // Visual mesh drop only. Seats the plant sole. The capsule and slideBoost stay put.
        public const float SlideBodyDrop = 0.42f;
        // Enter from run, leave to run or crouch, and a jump cancel.
        // Crouch keeps its own tenth-second blend. See CrouchPose.
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
                + " read=forward-plant+free-trail"
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
            // Positive hip + spine pitches the chest toward the face. The head counters
            // so the face stays along the slide instead of in the knees.
            if (SlideHip < 28f || SlideHip > 44f || SlideSpine < 16f || SlideSpine > 32f || chest < 50f || chest > 70f)
                fails.Add("SlideBody torso is not pitched forward");
            if (chest < crouchChest + 8f)
                fails.Add("SlideBody chest matches the crouch");
            if (SlideHead > -26f || SlideHead < -44f || chest + SlideHead < 14f || chest + SlideHead > 36f)
                fails.Add("SlideBody head is not looking along the slide");
            if (SlideLeadThigh < 68f || SlideLeadThigh > 90f)
                fails.Add("SlideBody lead leg is not a plant");
            if (SlideLeadKnee > -82f || SlideLeadKnee < -112f)
                fails.Add("SlideBody lead knee is not planted");
            if (Mathf.Abs(SlideLeadYaw) < 6f || Mathf.Abs(SlideLeadYaw) > 16f)
                fails.Add("SlideBody lead knee crosses the other leg");
            if (SlideTrailThigh > -40f || SlideTrailThigh < -68f)
                fails.Add("SlideBody trail leg is not free behind");
            if (SlideTrailKnee > -6f || SlideTrailKnee < -24f)
                fails.Add("SlideBody trail knee is tucked");
            if (Mathf.Abs(SlideTrailYaw) < 12f || Mathf.Abs(SlideTrailYaw) > 26f)
                fails.Add("SlideBody trail leg clips the pelvis");
            if (Mathf.Abs(SlideLeadThigh - SlideTrailThigh) < 110f)
                fails.Add("SlideBody legs match each other");
            if (Mathf.Abs(SlideTrailThigh - CrouchThigh) < 90f || Mathf.Abs(SlideTrailKnee - CrouchKnee) < 48f)
                fails.Add("SlideBody legs match the crouch");
            if (Mathf.Abs(-(SlideLeadThigh + SlideLeadKnee) - SlideLeadFoot) > 8f)
                fails.Add("SlideBody lead sole is not planted");
            if (SlideLeadArmPitch > -20f || SlideLeadArmPitch < -48f)
                fails.Add("SlideBody lead arm is not forward");
            if (SlideBalanceArmPitch < 16f || SlideBalanceArmPitch > 42f)
                fails.Add("SlideBody balance arm is floating");
            if (SlideBalanceArmPitch - SlideLeadArmPitch < 48f)
                fails.Add("SlideBody arms match each other");
            if (Mathf.Abs(SlideLeadArmYaw) < 18f || Mathf.Abs(SlideBalanceArmYaw) < 18f)
                fails.Add("SlideBody arms stack on the chest");
            if (Mathf.Abs(SlideLeadArmYaw) > 40f || Mathf.Abs(SlideBalanceArmYaw) > 40f)
                fails.Add("SlideBody arms float wide");
            if (SlideLeadElbow > -18f || SlideLeadElbow < -44f)
                fails.Add("SlideBody lead elbow is clipped");
            if (SlideBalanceElbow > -22f || SlideBalanceElbow < -48f)
                fails.Add("SlideBody balance elbow is clipped");
            if (Mathf.Abs(SlideLeadFoot - SlideTrailFoot) < 16f)
                fails.Add("SlideBody feet share one angle");
            if (CrouchHip > 40f || Mathf.Abs(CrouchElbow) < 60f || Mathf.Abs(CrouchKnee) < 50f
                || CrouchArmPitch > -24f || CrouchArmPitch < -50f)
                fails.Add("crouch reference no longer reads as a crouch");
            if (Mathf.Abs(IdleArmPitch) > 24f || Mathf.Abs(IdleElbow) > 20f || Mathf.Abs(IdleKnee) > 8f
                || Mathf.Abs(IdleHip) > 8f || Mathf.Abs(IdleSpine) > 8f)
                fails.Add("idle reference no longer reads as a stand");
            if (SlideBodyDrop < 0.36f || SlideBodyDrop > 0.50f || SlideBodyDrop < CrouchDrop + 0.16f)
                fails.Add("SlideBody is not a low crouch");
            if (SlideBlendSeconds < 0.08f || SlideBlendSeconds > 0.12f)
                fails.Add("SlideBody blend is not a clean tenth");
            foreach (string hit in ClearanceFailures())
                fails.Add(hit);

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
            if (Mathf.Abs(tagChest - chest) < 30f)
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

        /// <summary>
        /// Grounded slide read. Torso forward, lead sole planted, trail foot free
        /// and low, limbs clear of each other and of the ground. DummyRunner plays
        /// the same SlideBody. slideBoost and root motion stay out of this.
        /// </summary>
        public static string PolishProofLine()
        {
            float sole = PlantSoleY();
            float lift = TrailSoleY() - sole;
            return "slide-pose-polish"
                + " torso=forward"
                + " chest=" + (SlideHip + SlideSpine).ToString("0")
                + " head=" + SlideHead.ToString("0")
                + " lead=plant"
                + " thigh=" + SlideLeadThigh.ToString("0")
                + " knee=" + SlideLeadKnee.ToString("0")
                + " trail=free"
                + " trailThigh=" + SlideTrailThigh.ToString("0")
                + " trailKnee=" + SlideTrailKnee.ToString("0")
                + " arms=low"
                + " drop=" + SlideBodyDrop.ToString("0.00")
                + " blend=" + SlideBlendSeconds.ToString("0.00")
                + " sole=" + sole.ToString("0.00")
                + " trailLift=" + lift.ToString("0.00")
                + " intersect=" + ClearanceFailures().Count.ToString("0")
                + " dummy=SlideBody"
                + " slideBoost=0"
                + " rootMotion=0";
        }

        public static bool PolishHolds()
        {
            if (ClearanceFailures().Count != 0) return false;
            if (SlideBlendSeconds < 0.08f || SlideBlendSeconds > 0.12f) return false;
            if (SlideHip + SlideSpine <= 0f) return false;
            if (SlideLeadThigh <= 0f || SlideTrailThigh >= 0f) return false;
            if (SlideLeadKnee >= -40f || SlideTrailKnee <= -40f) return false;
            float sole = PlantSoleY();
            float trail = TrailSoleY();
            if (sole < -0.01f || sole > 0.05f) return false;
            if (trail < sole + 0.02f || trail > 0.14f) return false;
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
