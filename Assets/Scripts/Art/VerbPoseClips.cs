using System.Collections.Generic;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Named body clips. DummyLocomotor plays them from verb states.
    /// SlideBody is a low forward split: chest flat, lead leg out, trail leg back.
    /// PunchStrike is the cock then the strike.
    /// TagCatch is the tagged runner's guard. Feel numbers are not in here.
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
        public const float CrouchDrop = 0.14f;

        public const float PunchCockPitch = -40f;
        public const float PunchCockYaw = 80f;
        public const float PunchCockRoll = -55f;
        public const float PunchCockElbow = -110f;
        public const float PunchStrikePitch = -105f;
        public const float PunchStrikeYaw = 6f;
        public const float PunchStrikeRoll = -6f;
        public const float PunchStrikeElbow = -5f;

        public const float TagArmPitch = -125f;
        public const float TagArmYaw = 46f;
        public const float TagElbow = -12f;
        public const float TagSpine = -20f;
        public const float TagHip = -8f;
        public const float TagThigh = 30f;
        public const float TagKnee = -62f;

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

        /// <summary>0 is the cock beside the head. 1 is the long strike in front of the chest.</summary>
        public static Pose PunchStrikePose(Bind bind, float sample)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sample));
            float pitchR = Mathf.Lerp(PunchCockPitch, PunchStrikePitch, t);
            float yawR = Mathf.Lerp(PunchCockYaw, PunchStrikeYaw, t);
            float rollR = Mathf.Lerp(PunchCockRoll, PunchStrikeRoll, t);
            float elbowR = Mathf.Lerp(PunchCockElbow, PunchStrikeElbow, t);
            float pitchL = Mathf.Lerp(20f, 48f, t);
            float yawL = Mathf.Lerp(-18f, -30f, t);
            float elbowL = Mathf.Lerp(-28f, -16f, t);
            float hipYaw = Mathf.Lerp(-28f, 20f, t);
            float spineYaw = Mathf.Lerp(-40f, 36f, t);
            return new Pose
            {
                UaL = bind.UaL * Quaternion.Euler(pitchL, yawL, 16f),
                UaR = bind.UaR * Quaternion.Euler(pitchR, yawR, rollR),
                LaL = bind.LaL * Quaternion.Euler(elbowL, 0f, 0f),
                LaR = bind.LaR * Quaternion.Euler(elbowR, 0f, 0f),
                UlL = bind.UlL * Quaternion.Euler(Mathf.Lerp(10f, 16f, t), 0f, 0f),
                UlR = bind.UlR * Quaternion.Euler(Mathf.Lerp(-8f, -14f, t), 0f, 0f),
                LlL = bind.LlL * Quaternion.Euler(Mathf.Lerp(-14f, -10f, t), 0f, 0f),
                LlR = bind.LlR * Quaternion.Euler(-8f, 0f, 0f),
                Spine = bind.Spine * Quaternion.Euler(8f, spineYaw, 0f),
                Hips = bind.Hips * Quaternion.Euler(8f, hipYaw, 0f),
                Head = bind.Head * Quaternion.Euler(-4f, Mathf.Lerp(-12f, 8f, t), 0f),
            };
        }

        public static Pose TagCatchPose(Bind bind)
        {
            return new Pose
            {
                UaL = bind.UaL * Quaternion.Euler(TagArmPitch, TagArmYaw, 10f),
                UaR = bind.UaR * Quaternion.Euler(TagArmPitch, -TagArmYaw, -10f),
                LaL = bind.LaL * Quaternion.Euler(TagElbow, 0f, 0f),
                LaR = bind.LaR * Quaternion.Euler(TagElbow, 0f, 0f),
                UlL = bind.UlL * Quaternion.Euler(TagThigh, 0f, 0f),
                UlR = bind.UlR * Quaternion.Euler(TagThigh, 0f, 0f),
                LlL = bind.LlL * Quaternion.Euler(TagKnee, 0f, 0f),
                LlR = bind.LlR * Quaternion.Euler(TagKnee, 0f, 0f),
                Spine = bind.Spine * Quaternion.Euler(TagSpine, 0f, 0f),
                Hips = bind.Hips * Quaternion.Euler(TagHip, 0f, 0f),
                Head = bind.Head * Quaternion.Euler(10f, 0f, 0f),
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
            if (CrouchHip > 40f || Mathf.Abs(CrouchElbow) < 60f || Mathf.Abs(CrouchKnee) < 50f)
                fails.Add("crouch reference no longer reads as a crouch");
            if (SlideBodyDrop < 0.72f || SlideBodyDrop > 0.86f || SlideBodyDrop < CrouchDrop + 0.5f)
                fails.Add("SlideBody is not lower than the crouch");
            if (SlideBlendSeconds < 0.05f || SlideBlendSeconds > 0.10f)
                fails.Add("SlideBody blend is not a short snap");
            if (PunchCockElbow > -80f)
                fails.Add("PunchStrike cock is not bent beside the head");
            if (Mathf.Abs(PunchStrikeElbow) > 16f)
                fails.Add("PunchStrike strike elbow is folded");
            if (PunchStrikePitch > -90f || PunchStrikePitch < -140f)
                fails.Add("PunchStrike strike is not a forward line");
            if (Mathf.Abs(PunchStrikeYaw) > 20f)
                fails.Add("PunchStrike strike leaves the chest line");
            if (TagArmPitch > -100f)
                fails.Add("TagCatch arms are not up");
            if (Mathf.Abs(TagElbow) > 24f)
                fails.Add("TagCatch arms are folded");
            if (TagSpine >= 0f)
                fails.Add("TagCatch chest is not open");
            if (TagKnee > -40f)
                fails.Add("TagCatch knees do not buckle");
            if (Mathf.Abs(TagArmPitch - SlideArmPitch) < 20f && Mathf.Abs(TagElbow - SlideElbow) < 8f)
                fails.Add("TagCatch matches SlideBody");
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
