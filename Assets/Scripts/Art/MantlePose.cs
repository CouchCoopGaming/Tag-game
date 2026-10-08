using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual mantle only. The same state the motor calls a vault.
    /// Hands plant on the lip, the lead knee drives up, the chest folds over,
    /// then the body settles into a land that can hand off to the gait or a crouch.
    /// Wall climb and the wall-jump push-off keep their own poses.
    /// Duration, height, and speed are not written here. No root motion.
    /// </summary>
    public static class MantlePose
    {
        public const bool RootMotion = false;

        /// <summary>Wall climb, wall run, or air into the plant. The vault wins.</summary>
        public const float EnterBlendSeconds = 0.10f;
        /// <summary>Vault into the gait or a crouch. A land thud keeps its own absorb.</summary>
        public const float ExitBlendSeconds = 0.10f;
        /// <summary>Tracks the beats inside the motor window. The window is not written.</summary>
        public const float Slew = 1600f;

        /// <summary>Hands on the lip. The knee has not driven yet.</summary>
        public const float PlantEnd = 0.22f;
        /// <summary>Lead knee is up. Hands are still on the lip.</summary>
        public const float KneeEnd = 0.55f;

        public const float PlantPitchL = -108f;
        public const float PlantPitchR = -100f;
        public const float PlantYaw = 18f;
        public const float PlantElbowL = -86f;
        public const float PlantElbowR = -82f;
        public const float PlantLeadThigh = 36f;
        public const float PlantTrailThigh = 14f;
        public const float PlantLeadKnee = -52f;
        public const float PlantTrailKnee = -22f;
        public const float PlantSpine = 26f;
        public const float PlantHip = 18f;
        public const float PlantHead = -16f;

        public const float KneePitchL = -112f;
        public const float KneePitchR = -104f;
        public const float KneeYaw = 14f;
        public const float KneeElbowL = -96f;
        public const float KneeElbowR = -90f;
        public const float KneeLeadThigh = 102f;
        public const float KneeTrailThigh = 10f;
        public const float KneeLeadKnee = -124f;
        public const float KneeTrailKnee = -18f;
        public const float KneeSpine = 30f;
        public const float KneeHip = 22f;
        public const float KneeHead = -8f;

        public const float OverPitchL = -58f;
        public const float OverPitchR = -50f;
        public const float OverYaw = 10f;
        public const float OverElbowL = -42f;
        public const float OverElbowR = -38f;
        public const float OverLeadThigh = 68f;
        public const float OverTrailThigh = 40f;
        public const float OverLeadKnee = -78f;
        public const float OverTrailKnee = -46f;
        public const float OverSpine = 68f;
        public const float OverHip = 46f;
        public const float OverHead = 10f;

        public const float LandPitch = -24f;
        public const float LandYaw = 12f;
        public const float LandElbow = -22f;
        public const float LandLeadThigh = 26f;
        public const float LandTrailThigh = 22f;
        public const float LandLeadKnee = -40f;
        public const float LandTrailKnee = -36f;
        public const float LandSpine = 12f;
        public const float LandHip = 8f;
        public const float LandHead = -4f;

        /// <summary>
        /// Played speed-vault keys. <see cref="At"/> stays the printed plant,
        /// knee, over, and land, which is what the handoff proof measures.
        /// The arms stay an uncrossed plant on the obstacle. Both legs swing
        /// out to their own sides and stay there until the hips have passed,
        /// then come home. The hip takes the pitch and the spine stays near
        /// eight degrees. On the plant and the landing the pelvis bone drops
        /// and the support knees bend, so the sit is not a straight leg leaned
        /// back. That drop is the hips bone, not the capsule and not the root.
        /// </summary>
        struct VaultKey
        {
            public float U;
            public float ThL, ThR, KnL, KnR, YawL, YawR;
            public float ArmL, ArmR, AyL, AyR, ElbL, ElbR;
            public float Hip, Spine, Head;
            public float PelvisDrop;
        }

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ThighYawL, ThighYawR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head;
            /// <summary>Meters the hips bone sits below its standing height. Not a root offset.</summary>
            public float PelvisDrop;
        }

        /// <summary>
        /// True only while the motor is in Mantle. Wall climb and the wall-jump
        /// push-off are other states, so they do not select this pose. Stale wall
        /// flags do not cancel the vault; the locomotor skips those samples instead.
        /// </summary>
        public static bool Owns(bool mantle, bool wallClimb, bool wallJumpPush)
        {
            if (!mantle) return false;
            // Climb and the push-off stay on WallPose. They do not turn this vault off.
            if (wallClimb) return true;
            if (wallJumpPush) return true;
            return true;
        }

        /// <summary>0 on the pose we came from, 1 on the vault. Sums to 1.</summary>
        public static void Enter(float age, out float fromW, out float toW)
        {
            PoseHandoff.Pair(age, EnterBlendSeconds, out fromW, out toW);
        }

        /// <summary>0 on the vault, 1 on the gait or the crouch. Sums to 1.</summary>
        public static void Exit(float age, out float fromW, out float toW)
        {
            PoseHandoff.Pair(age, ExitBlendSeconds, out fromW, out toW);
        }

        /// <summary>
        /// progress is the motor mantle fraction. 0 plants, then the lead knee,
        /// then the chest over the lip, then a soft land. leadLeft drives the left knee.
        /// </summary>
        public static Sample At(float progress, bool leadLeft)
        {
            float u = progress < 0f ? 0f : (progress > 1f ? 1f : progress);
            Sample a;
            Sample b;
            float t;
            if (u < PlantEnd)
            {
                a = Plant();
                b = Knee();
                t = PoseHandoff.Ease(u / PlantEnd);
            }
            else if (u < KneeEnd)
            {
                a = Knee();
                b = Over();
                t = PoseHandoff.Ease((u - PlantEnd) / (KneeEnd - PlantEnd));
            }
            else
            {
                a = Over();
                b = Land();
                float span = 1f - KneeEnd;
                t = span > 0.0001f ? PoseHandoff.Ease((u - KneeEnd) / span) : 1f;
            }

            Sample s = Lerp(a, b, t);
            if (!leadLeft) s = Mirror(s);
            return s;
        }

        /// <summary>
        /// The curve the vault plays, keyed on the motor fraction. Lead-left,
        /// then mirrored when the other knee leads.
        /// </summary>
        public static Sample Cleared(float motorU, bool leadLeft)
        {
            float u = motorU < 0f ? 0f : (motorU > 1f ? 1f : motorU);
            VaultKey[] keys = VaultKeys();
            int i = 0;
            while (i < keys.Length - 2 && u > keys[i + 1].U)
                i++;
            VaultKey a = keys[i];
            VaultKey b = keys[i + 1];
            float span = b.U - a.U;
            float t = span > 0.0001f ? (u - a.U) / span : 1f;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            Sample s = new Sample
            {
                ThighL = Mathf.Lerp(a.ThL, b.ThL, t),
                ThighR = Mathf.Lerp(a.ThR, b.ThR, t),
                KneeL = Mathf.Lerp(a.KnL, b.KnL, t),
                KneeR = Mathf.Lerp(a.KnR, b.KnR, t),
                ThighYawL = Mathf.Lerp(a.YawL, b.YawL, t),
                ThighYawR = Mathf.Lerp(a.YawR, b.YawR, t),
                ArmPitchL = Mathf.Lerp(a.ArmL, b.ArmL, t),
                ArmPitchR = Mathf.Lerp(a.ArmR, b.ArmR, t),
                ArmYawL = Mathf.Lerp(a.AyL, b.AyL, t),
                ArmYawR = Mathf.Lerp(a.AyR, b.AyR, t),
                ElbowL = Mathf.Lerp(a.ElbL, b.ElbL, t),
                ElbowR = Mathf.Lerp(a.ElbR, b.ElbR, t),
                Hip = Mathf.Lerp(a.Hip, b.Hip, t),
                Spine = Mathf.Lerp(a.Spine, b.Spine, t),
                Head = Mathf.Lerp(a.Head, b.Head, t),
                PelvisDrop = Mathf.Lerp(a.PelvisDrop, b.PelvisDrop, t),
            };
            if (!leadLeft) s = Mirror(s);
            return s;
        }

        static VaultKey[] VaultKeys()
        {
            return new VaultKey[]
            {
                Key(0.00f, 52f, 52f, -64f, -64f, -12f, 12f, -58f, -58f, -18f, 18f, -46f, -46f, 18f, 8f, -10f, 0.10f),
                Key(0.16f, 72f, 58f, -48f, -42f, -58f, 48f, -58f, -58f, -18f, 18f, -46f, -46f, 26f, 8f, -8f, 0f),
                Key(0.36f, 82f, 70f, -40f, -44f, -78f, 70f, -56f, -56f, -16f, 16f, -42f, -42f, 28f, 8f, -6f, 0f),
                Key(0.64f, 74f, 64f, -40f, -44f, -72f, 66f, -54f, -54f, -16f, 16f, -40f, -40f, 24f, 8f, -4f, 0f),
                Key(0.82f, 48f, 42f, -42f, -38f, -10f, 10f, -48f, -48f, -16f, 16f, -38f, -38f, 22f, 8f, -4f, 0f),
                Key(0.92f, 60f, 58f, -72f, -72f, -22f, 22f, -46f, -46f, -16f, 16f, -36f, -36f, 20f, 8f, -4f, 0.12f),
                Key(1.00f, 64f, 64f, -82f, -82f, -26f, 26f, -46f, -46f, -16f, 16f, -36f, -36f, 20f, 8f, -4f, 0.20f),
            };
        }

        static VaultKey Key(float u, float thL, float thR, float knL, float knR, float yawL, float yawR,
            float armL, float armR, float ayL, float ayR, float elbL, float elbR, float hip, float spine, float head,
            float pelvisDrop)
        {
            return new VaultKey
            {
                U = u,
                ThL = thL, ThR = thR, KnL = knL, KnR = knR, YawL = yawL, YawR = yawR,
                ArmL = armL, ArmR = armR, AyL = ayL, AyR = ayR, ElbL = elbL, ElbR = elbR,
                Hip = hip, Spine = spine, Head = head,
                PelvisDrop = pelvisDrop,
            };
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (EnterBlendSeconds < 0.08f || EnterBlendSeconds > 0.12f) return false;
            if (ExitBlendSeconds < 0.08f || ExitBlendSeconds > 0.12f) return false;
            if (Slew < 1200f) return false;
            if (!Owns(true, false, false)) return false;
            if (!Owns(true, true, false) || !Owns(true, false, true)) return false;
            if (Owns(false, false, false) || Owns(false, true, false) || Owns(false, false, true)) return false;

            Enter(0f, out float enterFrom, out float enterTo);
            if (enterFrom < 0.999f || enterTo > 0.0001f) return false;
            Enter(EnterBlendSeconds, out float enterGone, out float enterFull);
            if (enterGone > 0.0001f || enterFull < 0.999f) return false;
            Enter(EnterBlendSeconds * 0.5f, out float enterMidFrom, out float enterMidTo);
            if (Mathf.Abs(enterMidFrom + enterMidTo - 1f) > 0.0001f) return false;
            if (Mathf.Abs(enterMidTo - 0.5f) > 0.0001f) return false;
            Exit(0f, out float exitFrom, out float exitTo);
            if (exitFrom < 0.999f || exitTo > 0.0001f) return false;
            Exit(ExitBlendSeconds, out float exitGone, out float exitFull);
            if (exitGone > 0.0001f || exitFull < 0.999f) return false;
            float prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                Exit(ExitBlendSeconds * i / 8f, out float fromW, out float toW);
                if (Mathf.Abs(fromW + toW - 1f) > 0.0001f) return false;
                if (toW + 0.0001f < prev) return false;
                prev = toW;
            }

            Sample plant = At(0f, true);
            Sample knee = At(PlantEnd, true);
            Sample over = At(KneeEnd, true);
            Sample land = At(1f, true);
            Sample kneeR = At(PlantEnd, false);

            float plantGap = plant.ArmPitchL - plant.ArmPitchR;
            if (plantGap < 0f) plantGap = -plantGap;
            if (plantGap > 16f) return false;
            if (plant.ArmPitchL > -90f || plant.ArmPitchR > -90f) return false;
            if (plant.ElbowL > -70f || plant.ElbowR > -70f) return false;
            if (plant.ThighL > 50f || plant.ThighL < plant.ThighR) return false;
            if (plant.Spine > 36f || plant.Head > -8f) return false;

            if (knee.ThighL < 95f || knee.ThighR > 20f) return false;
            if (knee.ThighL - knee.ThighR < 70f) return false;
            if (knee.KneeL > -110f) return false;
            if (knee.ArmPitchL > -90f || knee.ArmPitchR > -90f) return false;
            float kneeGap = knee.ArmPitchL - knee.ArmPitchR;
            if (kneeGap < 0f) kneeGap = -kneeGap;
            if (kneeGap > 20f) return false;
            if (knee.ThighL <= plant.ThighL) return false;

            if (kneeR.ThighR < 95f || kneeR.ThighL > 20f) return false;
            if (kneeR.ThighR <= kneeR.ThighL) return false;

            float chest = over.Spine + over.Hip;
            if (chest < 100f || over.Spine < WallPose.ClimbSpine + 24f) return false;
            if (over.Hip < WallPose.ClimbHip + 12f) return false;
            if (over.Head < 4f) return false;
            if (over.ArmPitchL < -70f || over.ArmPitchR < -70f) return false;
            if (over.ArmPitchL <= plant.ArmPitchL) return false;
            if (over.ThighL <= over.ThighR) return false;

            if (land.KneeL < -55f || land.KneeL > -28f) return false;
            if (land.KneeR < -55f || land.KneeR > -28f) return false;
            float landGap = land.ThighL - land.ThighR;
            if (landGap < 0f) landGap = -landGap;
            if (landGap > 12f) return false;
            if (land.Spine > 20f || land.Hip > 16f) return false;
            if (land.ThighL >= knee.ThighL) return false;

            WallPose.Sample reach = WallPose.Climb(1f, WallPose.ClimbSpeedRef);
            float reachGap = reach.ArmPitchL - reach.ArmPitchR;
            if (reachGap < 0f) reachGap = -reachGap;
            if (reachGap < 48f) return false;
            if (plantGap > reachGap * 0.35f) return false;
            if (plant.ElbowL >= reach.ElbowL) return false;
            if (knee.ThighL <= WallPose.DriveThigh) return false;
            if (over.Spine <= reach.Spine) return false;
            if (over.Head <= reach.Head) return false;

            WallPose.Sample push = WallPose.PushOff(true);
            if (knee.ThighL < push.ThighR + 16f) return false;
            if (plant.ElbowL >= push.ElbowL) return false;
            if (over.Spine <= push.Spine) return false;
            float pushGap = push.ThighR - push.ThighL;
            if (pushGap < 20f) return false;
            if (landGap > pushGap * 0.5f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample plant = At(0f, true);
            Sample knee = At(PlantEnd, true);
            Sample over = At(KneeEnd, true);
            Sample land = At(1f, true);
            Enter(EnterBlendSeconds * 0.5f, out float fromW, out float toW);
            return "mantle pose"
                + " plantPitch=" + plant.ArmPitchL.ToString("0") + "/" + plant.ArmPitchR.ToString("0")
                + " plantElbow=" + plant.ElbowL.ToString("0")
                + " kneeLead=" + knee.ThighL.ToString("0") + "/" + knee.KneeL.ToString("0")
                + " kneeTrail=" + knee.ThighR.ToString("0")
                + " overSpine=" + over.Spine.ToString("0")
                + " overHip=" + over.Hip.ToString("0")
                + " overHead=" + over.Head.ToString("0")
                + " landThigh=" + land.ThighL.ToString("0") + "/" + land.ThighR.ToString("0")
                + " landKnee=" + land.KneeL.ToString("0") + "/" + land.KneeR.ToString("0")
                + " enter=" + EnterBlendSeconds.ToString("0.00")
                + " exit=" + ExitBlendSeconds.ToString("0.00")
                + " midEnter=" + fromW.ToString("0.00") + "+" + toW.ToString("0.00")
                + " slew=" + Slew.ToString("0")
                + " gate=plant hands then lead knee then chest over the lip then soft land"
                + "; enter smoothstep " + EnterBlendSeconds.ToString("0.00")
                + "s from wall climb, wall run, or air"
                + "; exit smoothstep " + ExitBlendSeconds.ToString("0.00")
                + "s into gait or crouch; land thud keeps its absorb"
                + "; wall climb and wall-jump push-off stay on their states"
                + "; duration height speed untouched"
                + "; rootMotion=0";
        }

        static Sample Plant()
        {
            return new Sample
            {
                ThighL = PlantLeadThigh,
                ThighR = PlantTrailThigh,
                KneeL = PlantLeadKnee,
                KneeR = PlantTrailKnee,
                ArmPitchL = PlantPitchL,
                ArmPitchR = PlantPitchR,
                ArmYawL = PlantYaw,
                ArmYawR = -PlantYaw + 2f,
                ElbowL = PlantElbowL,
                ElbowR = PlantElbowR,
                Hip = PlantHip,
                Spine = PlantSpine,
                Head = PlantHead,
            };
        }

        static Sample Knee()
        {
            return new Sample
            {
                ThighL = KneeLeadThigh,
                ThighR = KneeTrailThigh,
                KneeL = KneeLeadKnee,
                KneeR = KneeTrailKnee,
                ArmPitchL = KneePitchL,
                ArmPitchR = KneePitchR,
                ArmYawL = KneeYaw,
                ArmYawR = -KneeYaw,
                ElbowL = KneeElbowL,
                ElbowR = KneeElbowR,
                Hip = KneeHip,
                Spine = KneeSpine,
                Head = KneeHead,
            };
        }

        static Sample Over()
        {
            return new Sample
            {
                ThighL = OverLeadThigh,
                ThighR = OverTrailThigh,
                KneeL = OverLeadKnee,
                KneeR = OverTrailKnee,
                ArmPitchL = OverPitchL,
                ArmPitchR = OverPitchR,
                ArmYawL = OverYaw,
                ArmYawR = -OverYaw,
                ElbowL = OverElbowL,
                ElbowR = OverElbowR,
                Hip = OverHip,
                Spine = OverSpine,
                Head = OverHead,
            };
        }

        static Sample Land()
        {
            return new Sample
            {
                ThighL = LandLeadThigh,
                ThighR = LandTrailThigh,
                KneeL = LandLeadKnee,
                KneeR = LandTrailKnee,
                ArmPitchL = LandPitch,
                ArmPitchR = LandPitch + 2f,
                ArmYawL = LandYaw,
                ArmYawR = -LandYaw,
                ElbowL = LandElbow,
                ElbowR = LandElbow + 2f,
                Hip = LandHip,
                Spine = LandSpine,
                Head = LandHead,
            };
        }

        static Sample Mirror(Sample s)
        {
            return new Sample
            {
                ThighL = s.ThighR,
                ThighR = s.ThighL,
                KneeL = s.KneeR,
                KneeR = s.KneeL,
                ThighYawL = -s.ThighYawR,
                ThighYawR = -s.ThighYawL,
                ArmPitchL = s.ArmPitchR,
                ArmPitchR = s.ArmPitchL,
                ArmYawL = -s.ArmYawR,
                ArmYawR = -s.ArmYawL,
                ElbowL = s.ElbowR,
                ElbowR = s.ElbowL,
                Hip = s.Hip,
                Spine = s.Spine,
                Head = s.Head,
                PelvisDrop = s.PelvisDrop,
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
            };
        }
    }
}
