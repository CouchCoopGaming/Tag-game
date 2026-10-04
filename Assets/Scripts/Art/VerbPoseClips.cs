using System.Collections.Generic;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Named body clips. DummyLocomotor plays them from verb states.
    /// SlideBody is a low forward split: chest flat, lead leg out, trail leg back.
    /// PunchStrike cocks the fist beside the head, then extends one arm while the chest unwinds.
    /// TagCatch reaches both hands to one contact. Feel numbers are not in here.
    /// </summary>
    public static class VerbPoseClips
    {
        public const string SlideBody = "SlideBody";
        public const string PunchStrike = "PunchStrike";
        public const string TagCatch = "TagCatch";

        public const string StateSlide = "Slide";
        public const string StatePunchWindup = "PunchWindup";
        public const string StatePunchActive = "PunchActive";
        public const string StateTag = "Tag";

        // Hier Tan rest: hips +X pitches the chest toward +Z (the face).
        // Upper-leg rest is about 180° on X, so thigh pitch does NOT add to the hip.
        // Solved on that rig: hips near the ground, chest flat, lead leg straight
        // out front, trail leg straight back, head looking along the slide.
        // Crouch stays chest-up with both knees bent. slideBoost is not in here.
        public const float SlideHip = 78f;
        public const float SlideSpine = 8f;
        public const float SlideHead = -84f;
        public const float SlideLeadThigh = -158f;
        public const float SlideLeadYaw = 6f;
        public const float SlideLeadKnee = 4f;
        public const float SlideTrailThigh = -6f;
        public const float SlideTrailYaw = -6f;
        public const float SlideTrailKnee = 8f;
        public const float SlideArmPitch = 170f;
        public const float SlideArmYaw = -20f;
        public const float SlideArmRoll = 0f;
        public const float SlideElbow = -8f;
        // Extra foot pitch so the shoe stays along the ground once the shin is horizontal.
        public const float SlideLeadFoot = 54f;
        public const float SlideLeadFootRoll = -30f;
        public const float SlideTrailFoot = 102f;
        public const float SlideTrailFootRoll = 0f;
        // Visual mesh drop only. The capsule and slideBoost stay put.
        // 0.78 puts the hip bone near y=0.23 and both ankles on the ground.
        public const float SlideBodyDrop = 0.78f;
        // Enter and leave. About four frames. Crouch keeps its own 0.16s drop.
        public const float SlideBlendSeconds = 0.06f;

        // The crouch this slide must not match: both knees bent, elbows folded, chest up.
        public const float CrouchHip = 22f;
        public const float CrouchSpine = 10f;
        public const float CrouchThigh = 56f;
        public const float CrouchKnee = -68f;
        public const float CrouchElbow = -72f;
        public const float CrouchArmPitch = -36f;
        public const float CrouchArmYaw = 16f;
        public const float CrouchDrop = 0.14f;

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
        // Positive pitch is the back arm. The right arm strikes. The left arm trails.
        public const float PunchCockPitch = -78f;
        public const float PunchCockYaw = 64f;
        public const float PunchCockRoll = -24f;
        public const float PunchCockElbow = -104f;
        public const float PunchStrikePitch = -118f;
        public const float PunchStrikeYaw = 54f;
        public const float PunchStrikeRoll = -16f;
        public const float PunchStrikeElbow = -6f;
        public const float PunchGuardPitchCock = -28f;
        public const float PunchGuardYawCock = -20f;
        public const float PunchGuardElbowCock = -68f;
        public const float PunchGuardPitchStrike = 68f;
        public const float PunchGuardYawStrike = -34f;
        public const float PunchGuardElbowStrike = -36f;
        public const float PunchGuardRoll = 10f;
        // Coil, then unwind. Opposite signs so the chest does not stay twisted one way.
        public const float PunchCockHipYaw = -36f;
        public const float PunchStrikeHipYaw = 28f;
        public const float PunchCockSpineYaw = -48f;
        public const float PunchStrikeSpineYaw = 44f;
        public const float PunchHipPitch = 8f;
        public const float PunchSpinePitch = 8f;
        public const float PunchHeadPitch = -4f;
        public const float PunchCockHeadYaw = -18f;
        public const float PunchStrikeHeadYaw = 12f;
        // A stance, not a crouch. The knees stay far from CrouchKnee.
        public const float PunchCockLeadThigh = 10f;
        public const float PunchStrikeLeadThigh = 16f;
        public const float PunchCockTrailThigh = -8f;
        public const float PunchStrikeTrailThigh = -14f;
        public const float PunchCockLeadKnee = -16f;
        public const float PunchStrikeLeadKnee = -10f;
        public const float PunchCockTrailKnee = -8f;
        public const float PunchStrikeTrailKnee = -6f;

        // Both arms share one pitch and meet in front. Not an overhead V, not one fist.
        // The chest leans into the touch. The knees soften and stay clear of the crouch.
        public const float TagArmPitch = -96f;
        public const float TagArmYaw = 26f;
        public const float TagArmRoll = 6f;
        public const float TagElbow = -12f;
        public const float TagSpine = 14f;
        public const float TagHip = 4f;
        public const float TagSpineYaw = 0f;
        public const float TagHipYaw = 0f;
        public const float TagThigh = 14f;
        public const float TagKnee = -44f;
        public const float TagHead = 12f;

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
            if (state == StatePunchWindup || state == StatePunchActive) return PunchStrike;
            if (state == StateTag) return TagCatch;
            return null;
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
            return new Pose
            {
                UaL = bind.UaL * Quaternion.Euler(SlideArmPitch, SlideArmYaw, SlideArmRoll),
                UaR = bind.UaR * Quaternion.Euler(SlideArmPitch, -SlideArmYaw, -SlideArmRoll),
                LaL = bind.LaL * Quaternion.Euler(SlideElbow, 0f, 0f),
                LaR = bind.LaR * Quaternion.Euler(SlideElbow, 0f, 0f),
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

        /// <summary>0 is the fist beside the head. 1 is one long arm, chest unwound, the other arm back.</summary>
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

        public static Pose TagCatchPose(Bind bind)
        {
            return new Pose
            {
                UaL = bind.UaL * Quaternion.Euler(TagArmPitch, TagArmYaw, TagArmRoll),
                UaR = bind.UaR * Quaternion.Euler(TagArmPitch, -TagArmYaw, -TagArmRoll),
                LaL = bind.LaL * Quaternion.Euler(TagElbow, 0f, 0f),
                LaR = bind.LaR * Quaternion.Euler(TagElbow, 0f, 0f),
                UlL = bind.UlL * Quaternion.Euler(TagThigh, 0f, 0f),
                UlR = bind.UlR * Quaternion.Euler(TagThigh, 0f, 0f),
                LlL = bind.LlL * Quaternion.Euler(TagKnee, 0f, 0f),
                LlR = bind.LlR * Quaternion.Euler(TagKnee, 0f, 0f),
                Spine = bind.Spine * Quaternion.Euler(TagSpine, TagSpineYaw, 0f),
                Hips = bind.Hips * Quaternion.Euler(TagHip, TagHipYaw, 0f),
                Head = bind.Head * Quaternion.Euler(TagHead, 0f, 0f),
            };
        }

        public static List<string> SilhouetteFailures()
        {
            var fails = new List<string>();
            float chest = SlideHip + SlideSpine;
            float crouchChest = CrouchHip + CrouchSpine;
            // Hip 78 + spine 8 lays the chest flat. A crouch chest stays near 32.
            if (SlideHip < 74f || SlideHip > 86f || SlideSpine < 4f || SlideSpine > 14f || chest < 80f || chest > 96f)
                fails.Add("SlideBody chest is not a flat slide");
            if (chest < crouchChest + 40f)
                fails.Add("SlideBody chest matches the crouch");
            if (SlideHead > -75f || SlideHead < -95f)
                fails.Add("SlideBody head is not looking along the slide");
            if (SlideLeadThigh > -145f || SlideLeadThigh < -170f)
                fails.Add("SlideBody lead leg is not extended forward");
            if (Mathf.Abs(SlideLeadKnee) > 16f)
                fails.Add("SlideBody lead knee is folded like a crouch");
            if (SlideTrailThigh > 8f || SlideTrailThigh < -18f)
                fails.Add("SlideBody trail leg is not extended back");
            if (Mathf.Abs(SlideTrailKnee) > 20f)
                fails.Add("SlideBody trail knee is folded like a crouch");
            if (Mathf.Abs(SlideLeadThigh - SlideTrailThigh) < 120f)
                fails.Add("SlideBody legs match each other");
            if (Mathf.Abs(SlideLeadThigh - CrouchThigh) < 40f || Mathf.Abs(SlideTrailThigh - CrouchThigh) < 30f)
                fails.Add("SlideBody legs match the crouch");
            if (SlideArmPitch < 155f || SlideArmPitch > 185f)
                fails.Add("SlideBody arms are not a low forward line");
            if (Mathf.Abs(SlideArmYaw) < 8f)
                fails.Add("SlideBody arms stack on the chest");
            if (Mathf.Abs(SlideElbow) > 18f)
                fails.Add("SlideBody elbows are folded like a crouch");
            if (Mathf.Abs(SlideLeadFoot - SlideTrailFoot) < 30f)
                fails.Add("SlideBody feet share one angle");
            if (CrouchHip > 40f || Mathf.Abs(CrouchElbow) < 60f || Mathf.Abs(CrouchKnee) < 50f
                || CrouchArmPitch > -24f || CrouchArmPitch < -50f)
                fails.Add("crouch reference no longer reads as a crouch");
            if (Mathf.Abs(IdleArmPitch) > 24f || Mathf.Abs(IdleElbow) > 20f || Mathf.Abs(IdleKnee) > 8f
                || Mathf.Abs(IdleHip) > 8f || Mathf.Abs(IdleSpine) > 8f)
                fails.Add("idle reference no longer reads as a stand");
            if (SlideBodyDrop < 0.72f || SlideBodyDrop > 0.86f || SlideBodyDrop < CrouchDrop + 0.5f)
                fails.Add("SlideBody is not lower than the crouch");
            if (SlideBlendSeconds < 0.05f || SlideBlendSeconds > 0.10f)
                fails.Add("SlideBody blend is not a short snap");

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
            if (ClipForState(StateSlide) != SlideBody)
                fails.Add("Slide does not play SlideBody");
            if (ClipForState(StatePunchWindup) != PunchStrike || ClipForState(StatePunchActive) != PunchStrike)
                fails.Add("punch states do not play PunchStrike");
            if (ClipForState(StateTag) != TagCatch)
                fails.Add("Tag does not play TagCatch");
            return fails;
        }
    }
}
