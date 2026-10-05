using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual role swap only. A punch or tag that moves It holds a readable
    /// body for a short window, then eases back onto the gait.
    /// The new It raises one arm and beats that fist to the chest.
    /// The old It flinches and gives the role up. No root motion.
    /// Reach, hit timing, and the tag rules are not written here.
    /// TagLandFlash and ItMarker stay on their own paths.
    /// </summary>
    public static class BecomeItPose
    {
        public const bool RootMotion = false;

        /// <summary>How long the swap reads at chase-cam distance before the ease.</summary>
        public const float HoldSeconds = 0.40f;
        /// <summary>Smoothstep back onto the gait. The gait wins.</summary>
        public const float RecoverSeconds = 0.16f;
        /// <summary>The raised arm is fully on until here, then the fist comes in.</summary>
        public const float RaiseUntil = 0.14f;
        /// <summary>The chest beat is fully on here and holds through the rest of the window.</summary>
        public const float BeatFullAt = 0.30f;
        /// <summary>Fast enough that the raise arrives inside the hold.</summary>
        public const float Slew = 160f;

        public const float RaisePitchR = -142f;
        public const float RaiseYawR = 14f;
        public const float RaiseRollR = -6f;
        public const float RaiseElbowR = -4f;
        public const float RaisePitchL = 36f;
        public const float RaiseYawL = -58f;
        public const float RaiseRollL = 10f;
        public const float RaiseElbowL = -28f;
        public const float RaiseThighL = 12f;
        public const float RaiseThighR = 22f;
        public const float RaiseKneeL = -10f;
        public const float RaiseKneeR = -16f;
        public const float RaiseSpine = -32f;
        public const float RaiseSpineYaw = -20f;
        public const float RaiseHip = -6f;
        public const float RaiseHipYaw = 14f;
        public const float RaiseHead = -18f;
        public const float RaiseHeadYaw = -6f;

        public const float BeatPitchR = -46f;
        public const float BeatYawR = -38f;
        public const float BeatRollR = 22f;
        public const float BeatElbowR = -124f;
        public const float BeatPitchL = 28f;
        public const float BeatYawL = -64f;
        public const float BeatRollL = 8f;
        public const float BeatElbowL = -22f;
        public const float BeatThighL = 10f;
        public const float BeatThighR = 18f;
        public const float BeatKneeL = -12f;
        public const float BeatKneeR = -20f;
        public const float BeatSpine = 14f;
        public const float BeatSpineYaw = 10f;
        public const float BeatHip = 8f;
        public const float BeatHipYaw = -8f;
        public const float BeatHead = -4f;
        public const float BeatHeadYaw = 6f;

        public const float GivePitchL = 70f;
        public const float GiveYawL = 22f;
        public const float GiveRollL = 8f;
        public const float GiveElbowL = -78f;
        public const float GivePitchR = 66f;
        public const float GiveYawR = -18f;
        public const float GiveRollR = -8f;
        public const float GiveElbowR = -72f;
        public const float GiveThighL = 18f;
        public const float GiveThighR = 14f;
        public const float GiveKneeL = -38f;
        public const float GiveKneeR = -34f;
        public const float GiveSpine = 42f;
        public const float GiveSpineYaw = 4f;
        public const float GiveHip = 16f;
        public const float GiveHipYaw = -6f;
        public const float GiveHead = 32f;
        public const float GiveHeadYaw = 10f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float Hip, HipYaw, Spine, SpineYaw, Head, HeadYaw;
        }

        /// <summary>One arm up, chest open. The right arm is the claim, the same side as the strike.</summary>
        public static Sample Raise()
        {
            return new Sample
            {
                ThighL = RaiseThighL,
                ThighR = RaiseThighR,
                KneeL = RaiseKneeL,
                KneeR = RaiseKneeR,
                ArmPitchL = RaisePitchL,
                ArmPitchR = RaisePitchR,
                ArmYawL = RaiseYawL,
                ArmYawR = RaiseYawR,
                ArmRollL = RaiseRollL,
                ArmRollR = RaiseRollR,
                ElbowL = RaiseElbowL,
                ElbowR = RaiseElbowR,
                Hip = RaiseHip,
                HipYaw = RaiseHipYaw,
                Spine = RaiseSpine,
                SpineYaw = RaiseSpineYaw,
                Head = RaiseHead,
                HeadYaw = RaiseHeadYaw,
            };
        }

        /// <summary>That same fist folds to the chest. The other arm stays out.</summary>
        public static Sample ChestBeat()
        {
            return new Sample
            {
                ThighL = BeatThighL,
                ThighR = BeatThighR,
                KneeL = BeatKneeL,
                KneeR = BeatKneeR,
                ArmPitchL = BeatPitchL,
                ArmPitchR = BeatPitchR,
                ArmYawL = BeatYawL,
                ArmYawR = BeatYawR,
                ArmRollL = BeatRollL,
                ArmRollR = BeatRollR,
                ElbowL = BeatElbowL,
                ElbowR = BeatElbowR,
                Hip = BeatHip,
                HipYaw = BeatHipYaw,
                Spine = BeatSpine,
                SpineYaw = BeatSpineYaw,
                Head = BeatHead,
                HeadYaw = BeatHeadYaw,
            };
        }

        /// <summary>0 is the raised arm. 1 is the fist on the chest.</summary>
        public static Sample Claim(float beat01)
        {
            float u = beat01 < 0f ? 0f : (beat01 > 1f ? 1f : beat01);
            return Lerp(Raise(), ChestBeat(), u);
        }

        /// <summary>Old It. Shoulders drop, chin tucks, chest curls. Not a reach.</summary>
        public static Sample GiveUp()
        {
            return new Sample
            {
                ThighL = GiveThighL,
                ThighR = GiveThighR,
                KneeL = GiveKneeL,
                KneeR = GiveKneeR,
                ArmPitchL = GivePitchL,
                ArmPitchR = GivePitchR,
                ArmYawL = GiveYawL,
                ArmYawR = GiveYawR,
                ArmRollL = GiveRollL,
                ArmRollR = GiveRollR,
                ElbowL = GiveElbowL,
                ElbowR = GiveElbowR,
                Hip = GiveHip,
                HipYaw = GiveHipYaw,
                Spine = GiveSpine,
                SpineYaw = GiveSpineYaw,
                Head = GiveHead,
                HeadYaw = GiveHeadYaw,
            };
        }

        /// <summary>0 through the raise, 1 once the fist is on the chest, then it holds.</summary>
        public static float Beat01(float age)
        {
            if (age <= RaiseUntil) return 0f;
            if (age >= BeatFullAt) return 1f;
            float span = BeatFullAt - RaiseUntil;
            if (span <= 0.0001f) return 1f;
            return PoseHandoff.Ease((age - RaiseUntil) / span);
        }

        /// <summary>1 for the whole hold. Then a smoothstep down to 0 so the gait returns.</summary>
        public static float PoseWeight(float age)
        {
            if (age < 0f) return 0f;
            if (age <= HoldSeconds) return 1f;
            float t = age - HoldSeconds;
            if (RecoverSeconds <= 0.0001f || t >= RecoverSeconds) return 0f;
            return 1f - PoseHandoff.Ease(t / RecoverSeconds);
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (HoldSeconds < 0.30f || HoldSeconds > 0.50f) return false;
            if (RecoverSeconds < 0.12f || RecoverSeconds > 0.22f) return false;
            if (RaiseUntil < 0.08f || RaiseUntil > 0.22f) return false;
            if (BeatFullAt < RaiseUntil + 0.10f || BeatFullAt > HoldSeconds) return false;
            if (Slew < 120f || Slew > 200f) return false;

            if (Mathf.Abs(PoseWeight(0f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(PoseWeight(HoldSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(PoseWeight(HoldSeconds * 0.5f) - 1f) > 0.0001f) return false;
            if (PoseWeight(-0.01f) > 0.0001f) return false;
            if (PoseWeight(HoldSeconds + RecoverSeconds) > 0.0001f) return false;
            if (Mathf.Abs(PoseWeight(HoldSeconds + RecoverSeconds * 0.5f) - 0.5f) > 0.0001f) return false;
            float prevW = 2f;
            for (int i = 0; i <= 8; i++)
            {
                float w = PoseWeight(HoldSeconds + RecoverSeconds * i / 8f);
                if (w > prevW + 0.0001f) return false;
                prevW = w;
            }

            if (Beat01(0f) > 0.0001f || Beat01(RaiseUntil) > 0.0001f) return false;
            if (Mathf.Abs(Beat01(BeatFullAt) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Beat01(HoldSeconds) - 1f) > 0.0001f) return false;
            float midBeat = RaiseUntil + (BeatFullAt - RaiseUntil) * 0.5f;
            if (Mathf.Abs(Beat01(midBeat) - 0.5f) > 0.0001f) return false;
            float prevBeat = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float b = Beat01(Mathf.Lerp(RaiseUntil, BeatFullAt, i / 8f));
                if (b + 0.0001f < prevBeat) return false;
                prevBeat = b;
            }

            Sample raise = Raise();
            Sample beat = ChestBeat();
            Sample claim0 = Claim(0f);
            Sample claim1 = Claim(1f);
            Sample give = GiveUp();
            if (Mathf.Abs(claim0.ArmPitchR - raise.ArmPitchR) > 0.001f) return false;
            if (Mathf.Abs(claim1.ElbowR - beat.ElbowR) > 0.001f) return false;
            if (raise.ArmPitchR > -124f || raise.ArmPitchR < -150f) return false;
            if (raise.ElbowR < -16f) return false;
            if (raise.ArmPitchL < 20f) return false;
            if (raise.ArmPitchR > raise.ArmPitchL - 100f) return false;
            if (raise.Spine > -20f) return false;
            if (beat.ElbowR > -100f) return false;
            if (beat.ArmPitchR < -80f || beat.ArmPitchR > -30f) return false;
            if (beat.ElbowR > raise.ElbowR - 80f) return false;
            if (beat.ElbowL < -40f) return false;
            if (beat.ArmPitchL < 16f) return false;
            if (beat.Spine < 6f || beat.Spine < raise.Spine + 30f) return false;
            if (give.ArmPitchL < 40f || give.ArmPitchR < 40f) return false;
            if (give.Spine < 28f) return false;
            if (give.Head < 20f) return false;
            if (give.KneeL < -50f || give.KneeR < -50f) return false;
            if (give.ArmPitchR < 0f || raise.ArmPitchR > 0f) return false;
            if (Mathf.Abs(raise.ArmPitchR - VerbPoseClips.PunchStrikePitch) < 8f) return false;
            if (Mathf.Abs(give.ArmPitchR - VerbPoseClips.TagArmPitch) < 40f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample raise = Raise();
            Sample beat = ChestBeat();
            Sample give = GiveUp();
            float midBack = PoseWeight(HoldSeconds + RecoverSeconds * 0.5f);
            float midBeat = Beat01(RaiseUntil + (BeatFullAt - RaiseUntil) * 0.5f);
            return "become-it pose"
                + " hold=" + HoldSeconds.ToString("0.00")
                + " recover=" + RecoverSeconds.ToString("0.00")
                + " raisePitch=" + raise.ArmPitchR.ToString("0")
                + " raiseElbow=" + raise.ElbowR.ToString("0")
                + " beatElbow=" + beat.ElbowR.ToString("0")
                + " beatPitch=" + beat.ArmPitchR.ToString("0")
                + " beatSpine=" + beat.Spine.ToString("0")
                + " givePitch=" + give.ArmPitchR.ToString("0")
                + "/" + give.ArmPitchL.ToString("0")
                + " giveSpine=" + give.Spine.ToString("0")
                + " giveHead=" + give.Head.ToString("0")
                + " midBeat=" + midBeat.ToString("0.00")
                + " midBack=" + midBack.ToString("0.00")
                + " slew=" + Slew.ToString("0")
                + " gate=claim weight 1 for 0.." + HoldSeconds.ToString("0.00")
                + "s, one-arm raise then chest beat"
                + "; give-up weight 1 for the same window, shoulders down chin tucked"
                + "; recover smoothstep " + RecoverSeconds.ToString("0.00")
                + "s onto gait"
                + "; rings=TagLandFlash marker=ItMarker"
                + "; reach 1.55 hit timing tag rules untouched"
                + "; rootMotion=0";
        }

        static Sample Lerp(Sample a, Sample b, float u)
        {
            return new Sample
            {
                ThighL = Mathf.Lerp(a.ThighL, b.ThighL, u),
                ThighR = Mathf.Lerp(a.ThighR, b.ThighR, u),
                KneeL = Mathf.Lerp(a.KneeL, b.KneeL, u),
                KneeR = Mathf.Lerp(a.KneeR, b.KneeR, u),
                ArmPitchL = Mathf.Lerp(a.ArmPitchL, b.ArmPitchL, u),
                ArmPitchR = Mathf.Lerp(a.ArmPitchR, b.ArmPitchR, u),
                ArmYawL = Mathf.Lerp(a.ArmYawL, b.ArmYawL, u),
                ArmYawR = Mathf.Lerp(a.ArmYawR, b.ArmYawR, u),
                ArmRollL = Mathf.Lerp(a.ArmRollL, b.ArmRollL, u),
                ArmRollR = Mathf.Lerp(a.ArmRollR, b.ArmRollR, u),
                ElbowL = Mathf.Lerp(a.ElbowL, b.ElbowL, u),
                ElbowR = Mathf.Lerp(a.ElbowR, b.ElbowR, u),
                Hip = Mathf.Lerp(a.Hip, b.Hip, u),
                HipYaw = Mathf.Lerp(a.HipYaw, b.HipYaw, u),
                Spine = Mathf.Lerp(a.Spine, b.Spine, u),
                SpineYaw = Mathf.Lerp(a.SpineYaw, b.SpineYaw, u),
                Head = Mathf.Lerp(a.Head, b.Head, u),
                HeadYaw = Mathf.Lerp(a.HeadYaw, b.HeadYaw, u),
            };
        }
    }
}
