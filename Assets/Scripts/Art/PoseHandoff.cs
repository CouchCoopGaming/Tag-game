using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual handoffs between pose layers. Each edge has one smoothstep,
    /// and the weights on that edge sum to 1 so one layer owns the bones.
    /// Nothing here writes velocity, cling, or the root.
    /// </summary>
    public static class PoseHandoff
    {
        public const bool RootMotion = false;

        /// <summary>Slide cancel into the jump. Jump wins.</summary>
        public const float SlideJumpSeconds = VerbPoseClips.SlideBlendSeconds;
        /// <summary>Wall release into the fall beat. Fall wins.</summary>
        public const float WallFallSeconds = WallPose.ReleaseBlendSeconds;
        /// <summary>Punch interrupted by an air dash. Dash wins. Dash time is unchanged.</summary>
        public const float PunchDashSeconds = 0.10f;
        /// <summary>Punch interrupted by a jump. Jump wins. Jump height is unchanged.</summary>
        public const float PunchJumpSeconds = 0.10f;
        /// <summary>Grapple release into the fall beat. Fall wins.</summary>
        public const float GrappleFallSeconds = GrapplePose.ReleaseBlendSeconds;
        /// <summary>Landing thud into the gait. Gait wins once the absorb has eased.</summary>
        public const float LandGaitSeconds = GaitBlend.IdleBlendSeconds;
        /// <summary>Wall-jump push-off into the air stride. Stride wins.</summary>
        public const float WallStrideSeconds = WallPose.PushBlendSeconds;
        /// <summary>Crouch into SlideBody. Slide wins. Crouch blend time is unchanged.</summary>
        public const float CrouchSlideSeconds = 0.10f;
        /// <summary>Air dash into the air stride or the fall beat. Stride or fall wins.</summary>
        public const float DashAirSeconds = 0.10f;
        /// <summary>Wall climb, wall run, or air into the vault. The vault wins.</summary>
        public const float MantleEnterSeconds = MantlePose.EnterBlendSeconds;
        /// <summary>Vault into the gait or a crouch. A land thud keeps its absorb.</summary>
        public const float MantleExitSeconds = MantlePose.ExitBlendSeconds;
        /// <summary>Crouch into the gait, and the gait into a crouch. One curve both ways.</summary>
        public const float CrouchGaitSeconds = CrouchPose.BlendSeconds;
        /// <summary>Vault into the land absorb. The land wins, then land to gait keeps its own ease.</summary>
        public const float MantleLandSeconds = MantlePose.ExitBlendSeconds;
        /// <summary>Lunge coil into the stretch. The burst wins. Burst time is unchanged.</summary>
        public const float LungeBurstSeconds = 0.08f;
        /// <summary>Lunge coil or stretch back onto the gait. The gait wins.</summary>
        public const float LungeRecoverSeconds = LungePose.RecoverSeconds;
        /// <summary>Claim or give-up back onto the gait. The gait wins.</summary>
        public const float BecomeGaitSeconds = BecomeItPose.RecoverSeconds;
        /// <summary>Punch and the role swap on the same frame. The swap wins.</summary>
        public const float PunchBecomeSeconds = 0.10f;
        /// <summary>Hard-brake plant into the idle weight shift. Idle wins.</summary>
        public const float StopIdleSeconds = StopPlantPose.WindowSeconds;
        /// <summary>Idle weight shift and the pivot. One curve both ways.</summary>
        public const float IdlePivotSeconds = PivotPose.BlendSeconds;
        /// <summary>Air-strafe lean into the air dash, and back out on the dash exit. The dash wins. Dash time is unchanged.</summary>
        public const float LeanDashSeconds = 0.08f;
        /// <summary>Air-strafe lean into a punch or a grapple. The verb wins. Reach and the pull are unchanged.</summary>
        public const float LeanVerbSeconds = 0.10f;
        /// <summary>Skipped land into the next takeoff. The takeoff wins. Jump height is unchanged.</summary>
        public const float HopTakeoffSeconds = JumpPose.TakeoffSeconds;
        /// <summary>Aim chest and the role swap. The swap wins. Chest and head only.</summary>
        public const float AimBecomeSeconds = 0.10f;

        /// <summary>Smoothstep. 0 at the start of the blend, 1 at the end.</summary>
        public static float Ease(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return u * u * (3f - 2f * u);
        }

        /// <summary>Winner share of a 0..1 timer. The other layer keeps 1 - this.</summary>
        public static float ToWeight(float timer01)
        {
            return Ease(timer01);
        }

        /// <summary>fromW + toW = 1. toW is the winner and rises with age.</summary>
        public static void Pair(float age, float seconds, out float fromW, out float toW)
        {
            float u = 1f;
            if (seconds > 0.0001f)
                u = age / seconds;
            toW = Ease(u);
            fromW = 1f - toW;
        }

        /// <summary>
        /// Wall, push-off, and air stride. They sum to 1.
        /// Age 0 is the wall. The push-off leads the middle. The stride wins.
        /// </summary>
        public static void WallJump01(float timer01, out float wallW, out float pushW, out float strideW)
        {
            float s = Ease(timer01);
            float stay = 1f - s;
            wallW = stay * stay;
            pushW = 2f * s * stay;
            strideW = s * s;
        }

        /// <summary>Gait and crouch. Sum to 1. timer 0 is the pose we left.</summary>
        public static void CrouchGait(float timer01, out float fromW, out float toW)
        {
            toW = Ease(timer01);
            fromW = 1f - toW;
        }

        /// <summary>Crouch and SlideBody. Sum to 1. Slide wins as the timer rises.</summary>
        public static void CrouchSlide(float timer01, out float crouchW, out float slideW)
        {
            slideW = Ease(timer01);
            crouchW = 1f - slideW;
        }

        /// <summary>
        /// Dash, air stride, and fall. They sum to 1.
        /// The window is the dash. After it, vertical speed picks stride or fall.
        /// </summary>
        public static void DashExit(float age, float verticalSpeed, out float dashW, out float strideW, out float fallW)
        {
            dashW = AirDashPose.DashWeight(age);
            float rest = 1f - dashW;
            float fall = AirDashPose.FallBlend(verticalSpeed);
            fallW = rest * fall;
            strideW = rest - fallW;
        }

        /// <summary>Vault and the land absorb. Sum to 1. The land wins.</summary>
        public static void MantleLand(float timer01, out float mantleW, out float landW)
        {
            landW = Ease(timer01);
            mantleW = 1f - landW;
        }

        /// <summary>
        /// Gait, coil, and stretch. They sum to 1.
        /// tellWeight is the coil we were holding. The stretch wins.
        /// </summary>
        public static void LungeEnter(float burstAge, float tellWeight, out float gaitW, out float tellW, out float burstW)
        {
            float from = tellWeight < 0f ? 0f : (tellWeight > 1f ? 1f : tellWeight);
            float u = 1f;
            if (LungeBurstSeconds > 0.0001f)
                u = burstAge / LungeBurstSeconds;
            float s = Ease(u);
            float stay = 1f - s;
            gaitW = stay * (1f - from);
            tellW = stay * from;
            burstW = s;
        }

        /// <summary>Punch and the role swap. Sum to 1. The swap wins.</summary>
        public static void PunchBecome(float age, out float punchW, out float becomeW)
        {
            Pair(age, PunchBecomeSeconds, out punchW, out becomeW);
        }

        /// <summary>
        /// Hard-brake plant and the idle weight shift. Sum to 1.
        /// The plant leads. Idle wins. A gentle slow does not use this edge.
        /// </summary>
        public static void StopIdle(float timer01, out float plantW, out float idleW)
        {
            StopPlantPose.IntoIdle(timer01, out plantW, out idleW);
        }

        /// <summary>
        /// Idle weight shift and the pivot. Sum to 1.
        /// timer 0 is idle. Pivot wins as the timer rises. The same curve runs backward.
        /// </summary>
        public static void IdlePivot(float timer01, out float idleW, out float pivotW)
        {
            pivotW = Ease(timer01);
            idleW = 1f - pivotW;
        }

        /// <summary>
        /// Air-strafe lean and the air dash. Sum to 1.
        /// timer 0 is the lean. The dash wins.
        /// </summary>
        public static void LeanDash(float timer01, out float leanW, out float dashW)
        {
            dashW = Ease(timer01);
            leanW = 1f - dashW;
        }

        /// <summary>
        /// Dash weight from <see cref="AirDashPose.DashWeight"/> and the lean that should return.
        /// leanW + jumpW + dashW = 1. The jump share is the exit pose with no bank.
        /// </summary>
        public static void LeanDashExit(float dashW, float leanWant, out float leanW, out float jumpW)
        {
            float dash = dashW < 0f ? 0f : (dashW > 1f ? 1f : dashW);
            float want = leanWant < 0f ? 0f : (leanWant > 1f ? 1f : leanWant);
            float rest = 1f - dash;
            leanW = rest * want;
            jumpW = rest - leanW;
        }

        /// <summary>
        /// Air-strafe lean and a punch or grapple. Sum to 1.
        /// timer 0 is the lean. The verb wins. The same curve runs backward.
        /// </summary>
        public static void LeanVerb(float timer01, out float leanW, out float verbW)
        {
            verbW = Ease(timer01);
            leanW = 1f - verbW;
        }

        /// <summary>
        /// Skipped land and the next takeoff. Sum to 1.
        /// Age 0 keeps the airborne pose. The takeoff wins. A stay-down landing is not this edge.
        /// </summary>
        public static void HopTakeoff(float age, out float airW, out float takeW)
        {
            float u = 1f;
            if (HopTakeoffSeconds > 0.0001f)
                u = age / HopTakeoffSeconds;
            takeW = Ease(u);
            airW = 1f - takeW;
        }

        /// <summary>
        /// Aim chest and the role swap. Sum to 1.
        /// timer 0 is the aim. The swap wins. The same curve runs backward.
        /// </summary>
        public static void AimBecome(float timer01, out float aimW, out float becomeW)
        {
            becomeW = Ease(timer01);
            aimW = 1f - becomeW;
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(SlideJumpSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(WallFallSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(PunchDashSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(PunchJumpSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(GrappleFallSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(LandGaitSeconds - GaitBlend.IdleBlendSeconds) > 0.001f) return false;
            if (LandGaitSeconds > JumpLandTell.FlashSeconds) return false;
            if (Mathf.Abs(WallStrideSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(CrouchSlideSeconds - CrouchPose.SlideHandoffSeconds) > 0.001f) return false;
            if (CrouchSlideSeconds < 0.08f || CrouchSlideSeconds > 0.12f) return false;
            if (Mathf.Abs(DashAirSeconds - AirDashPose.HandoffSeconds) > 0.001f) return false;
            if (DashAirSeconds < 0.08f || DashAirSeconds > 0.12f) return false;
            if (Mathf.Abs(MantleEnterSeconds - MantlePose.EnterBlendSeconds) > 0.001f) return false;
            if (MantleEnterSeconds < 0.08f || MantleEnterSeconds > 0.12f) return false;
            if (Mathf.Abs(MantleExitSeconds - MantlePose.ExitBlendSeconds) > 0.001f) return false;
            if (MantleExitSeconds < 0.08f || MantleExitSeconds > 0.12f) return false;
            if (Mathf.Abs(CrouchGaitSeconds - CrouchPose.BlendSeconds) > 0.001f) return false;
            if (CrouchGaitSeconds < 0.08f || CrouchGaitSeconds > 0.12f) return false;
            if (Mathf.Abs(MantleLandSeconds - MantlePose.ExitBlendSeconds) > 0.001f) return false;
            if (MantleLandSeconds < 0.08f || MantleLandSeconds > 0.12f) return false;
            if (LungeBurstSeconds < 0.06f || LungeBurstSeconds > 0.12f) return false;
            if (LungeBurstSeconds >= LungePose.BurstSeconds) return false;
            if (Mathf.Abs(LungeRecoverSeconds - LungePose.RecoverSeconds) > 0.001f) return false;
            if (Mathf.Abs(BecomeGaitSeconds - BecomeItPose.RecoverSeconds) > 0.001f) return false;
            if (Mathf.Abs(PunchBecomeSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(StopIdleSeconds - StopPlantPose.WindowSeconds) > 0.001f) return false;
            if (StopIdleSeconds < 0.12f || StopIdleSeconds > 0.20f) return false;
            if (Mathf.Abs(IdlePivotSeconds - PivotPose.BlendSeconds) > 0.001f) return false;
            if (IdlePivotSeconds < 0.10f || IdlePivotSeconds > 0.18f) return false;
            if (LeanDashSeconds < 0.06f || LeanDashSeconds >= AirDashPose.WindowSeconds) return false;
            if (Mathf.Abs(LeanVerbSeconds - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(HopTakeoffSeconds - JumpPose.TakeoffSeconds) > 0.001f) return false;
            if (HopTakeoffSeconds >= 0.10f) return false;
            if (Mathf.Abs(AimBecomeSeconds - 0.10f) > 0.001f) return false;
            if (Ease(0f) > 0.0001f || Mathf.Abs(Ease(1f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Ease(0.5f) - 0.5f) > 0.0001f) return false;
            if (Mathf.Abs(ToWeight(0f)) > 0.0001f || Mathf.Abs(ToWeight(1f) - 1f) > 0.0001f) return false;

            float prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float age = SlideJumpSeconds * i / 8f;
                Pair(age, SlideJumpSeconds, out float fromW, out float toW);
                if (Mathf.Abs(fromW + toW - 1f) > 0.0001f) return false;
                if (toW + 0.0001f < prev) return false;
                prev = toW;
            }

            Pair(0f, WallFallSeconds, out float wallFrom, out float fallTo);
            Pair(WallFallSeconds, WallFallSeconds, out float wallGone, out float fallFull);
            if (wallFrom < 0.999f || fallTo > 0.0001f) return false;
            if (wallGone > 0.0001f || fallFull < 0.999f) return false;

            WallJump01(0f, out float w0, out float p0, out float s0);
            if (w0 < 0.999f || p0 > 0.0001f || s0 > 0.0001f) return false;
            WallJump01(1f, out float w1, out float p1, out float s1);
            if (s1 < 0.999f || w1 > 0.0001f || p1 > 0.0001f) return false;
            WallJump01(0.5f, out float wM, out float pM, out float sM);
            if (Mathf.Abs(wM + pM + sM - 1f) > 0.0001f) return false;
            if (pM + 0.0001f < wM || pM + 0.0001f < sM) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                WallJump01(i / 8f, out float w, out float p, out float s);
                if (Mathf.Abs(w + p + s - 1f) > 0.0001f) return false;
                if (s + 0.0001f < prev) return false;
                prev = s;
            }

            CrouchGait(0f, out float gaitFull, out float crouchOff);
            CrouchGait(1f, out float gaitOff, out float crouchFull);
            if (gaitFull < 0.999f || crouchOff > 0.0001f) return false;
            if (gaitOff > 0.0001f || crouchFull < 0.999f) return false;
            CrouchSlide(0f, out float crouchStay, out float slideOff);
            CrouchSlide(1f, out float crouchGone, out float slideFull);
            if (crouchStay < 0.999f || slideOff > 0.0001f) return false;
            if (crouchGone > 0.0001f || slideFull < 0.999f) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                CrouchGait(i / 8f, out float fromW, out float toW);
                if (Mathf.Abs(fromW + toW - 1f) > 0.0001f) return false;
                if (toW + 0.0001f < prev) return false;
                prev = toW;
            }

            DashExit(0f, 8f, out float dash0, out float stride0, out float fall0);
            if (dash0 < 0.999f || stride0 > 0.0001f || fall0 > 0.0001f) return false;
            float handoffEnd = AirDashPose.WindowSeconds + DashAirSeconds;
            DashExit(handoffEnd, 0f, out float dashS, out float strideS, out float fallS);
            if (dashS > 0.0001f || strideS < 0.999f || fallS > 0.0001f) return false;
            DashExit(handoffEnd, JumpPose.FallVy, out float dashF, out float strideF, out float fallF);
            if (dashF > 0.0001f || strideF > 0.0001f || fallF < 0.999f) return false;
            DashExit(AirDashPose.WindowSeconds + DashAirSeconds * 0.5f, 0f, out float dashM, out float strideM, out float fallM);
            if (Mathf.Abs(dashM + strideM + fallM - 1f) > 0.0001f) return false;
            if (Mathf.Abs(dashM - 0.5f) > 0.0001f || Mathf.Abs(strideM - 0.5f) > 0.0001f) return false;

            MantleLand(0f, out float vaultFull, out float landOff);
            MantleLand(1f, out float vaultGone, out float landFull);
            if (vaultFull < 0.999f || landOff > 0.0001f) return false;
            if (vaultGone > 0.0001f || landFull < 0.999f) return false;

            LungeEnter(0f, 1f, out float lg0, out float lt0, out float lb0);
            if (lg0 > 0.0001f || lt0 < 0.999f || lb0 > 0.0001f) return false;
            LungeEnter(LungeBurstSeconds, 1f, out float lg1, out float lt1, out float lb1);
            if (lg1 > 0.0001f || lt1 > 0.0001f || lb1 < 0.999f) return false;
            LungeEnter(0f, 0.4f, out float lgP, out float ltP, out float lbP);
            if (Mathf.Abs(lgP - 0.6f) > 0.0001f || Mathf.Abs(ltP - 0.4f) > 0.0001f || lbP > 0.0001f) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                LungeEnter(LungeBurstSeconds * i / 8f, 1f, out float g, out float t, out float b);
                if (Mathf.Abs(g + t + b - 1f) > 0.0001f) return false;
                if (b + 0.0001f < prev) return false;
                prev = b;
            }

            PunchBecome(0f, out float punchFull, out float becomeOff);
            PunchBecome(PunchBecomeSeconds, out float punchGone, out float becomeFull);
            if (punchFull < 0.999f || becomeOff > 0.0001f) return false;
            if (punchGone > 0.0001f || becomeFull < 0.999f) return false;
            StopIdle(0f, out float stopFull, out float idleOff);
            StopIdle(StopPlantPose.IdleAt, out float stopHeld, out float idleEarly);
            StopIdle(1f, out float stopGone, out float idleWon);
            if (stopFull < 0.999f || idleOff > 0.0001f) return false;
            if (stopHeld < 0.999f || idleEarly > 0.0001f) return false;
            if (stopGone > 0.0001f || idleWon < 0.999f) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                StopIdle(i / 8f, out float plantW, out float idleW);
                if (Mathf.Abs(plantW + idleW - 1f) > 0.0001f) return false;
                if (idleW + 0.0001f < prev) return false;
                prev = idleW;
            }

            if (Mathf.Abs(LungePose.RecoverWeight(0f, 1f) + 0f - 1f) > 0.0001f) return false;
            if (LungePose.RecoverWeight(LungeRecoverSeconds, 1f) > 0.0001f) return false;
            if (Mathf.Abs(BecomeItPose.PoseWeight(BecomeItPose.HoldSeconds) - 1f) > 0.0001f) return false;
            if (BecomeItPose.PoseWeight(BecomeItPose.HoldSeconds + BecomeGaitSeconds) > 0.0001f) return false;

            IdlePivot(0f, out float idleFull, out float pivotOff);
            IdlePivot(1f, out float idleGone, out float pivotFull);
            if (idleFull < 0.999f || pivotOff > 0.0001f) return false;
            if (idleGone > 0.0001f || pivotFull < 0.999f) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                IdlePivot(i / 8f, out float idleW, out float pivotW);
                if (Mathf.Abs(idleW + pivotW - 1f) > 0.0001f) return false;
                if (pivotW + 0.0001f < prev) return false;
                prev = pivotW;
            }

            LeanDash(0f, out float leanFull, out float dashOff);
            LeanDash(1f, out float leanGone, out float dashFull);
            if (leanFull < 0.999f || dashOff > 0.0001f) return false;
            if (leanGone > 0.0001f || dashFull < 0.999f) return false;
            LeanDashExit(1f, 1f, out float leanAtDash, out float jumpAtDash);
            LeanDashExit(0f, 1f, out float leanAfter, out float jumpAfter);
            LeanDashExit(0.5f, 0.4f, out float leanMidExit, out float jumpMidExit);
            if (leanAtDash > 0.0001f || jumpAtDash > 0.0001f) return false;
            if (Mathf.Abs(leanAfter - 1f) > 0.0001f || jumpAfter > 0.0001f) return false;
            if (Mathf.Abs(leanMidExit + jumpMidExit + 0.5f - 1f) > 0.0001f) return false;
            if (Mathf.Abs(leanMidExit - 0.2f) > 0.0001f || Mathf.Abs(jumpMidExit - 0.3f) > 0.0001f) return false;

            LeanVerb(0f, out float leanVerbFull, out float verbOff);
            LeanVerb(1f, out float leanVerbGone, out float verbFull);
            if (leanVerbFull < 0.999f || verbOff > 0.0001f) return false;
            if (leanVerbGone > 0.0001f || verbFull < 0.999f) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                LeanVerb(i / 8f, out float leanW, out float verbW);
                if (Mathf.Abs(leanW + verbW - 1f) > 0.0001f) return false;
                if (verbW + 0.0001f < prev) return false;
                prev = verbW;
            }

            HopTakeoff(0f, out float airFull, out float takeOff);
            HopTakeoff(HopTakeoffSeconds, out float airGone, out float takeFull);
            if (airFull < 0.999f || takeOff > 0.0001f) return false;
            if (airGone > 0.0001f || takeFull < 0.999f) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                HopTakeoff(HopTakeoffSeconds * i / 8f, out float airW, out float takeW);
                if (Mathf.Abs(airW + takeW - 1f) > 0.0001f) return false;
                if (takeW + 0.0001f < prev) return false;
                prev = takeW;
            }

            AimBecome(0f, out float aimFull, out float becomeAimOff);
            AimBecome(1f, out float aimGone, out float becomeAimFull);
            if (aimFull < 0.999f || becomeAimOff > 0.0001f) return false;
            if (aimGone > 0.0001f || becomeAimFull < 0.999f) return false;
            prev = -1f;
            for (int i = 0; i <= 8; i++)
            {
                AimBecome(i / 8f, out float aimW, out float becomeW);
                if (Mathf.Abs(aimW + becomeW - 1f) > 0.0001f) return false;
                if (becomeW + 0.0001f < prev) return false;
                prev = becomeW;
            }

            return true;
        }

        /// <summary>Third set. The newest layers, after the second handoff line.</summary>
        public static string ProofLine3()
        {
            IdlePivot(0.5f, out float idleW, out float pivotW);
            LeanDash(0.5f, out float leanW, out float dashW);
            HopTakeoff(HopTakeoffSeconds * 0.5f, out float airW, out float takeW);
            AimBecome(0.5f, out float aimW, out float becomeW);
            return "pose handoff 3"
                + " stop->idle " + StopIdleSeconds.ToString("0.00") + "s idle wins"
                + " idle<->pivot " + IdlePivotSeconds.ToString("0.00") + "s idle or pivot wins"
                + " lean<->dash " + LeanDashSeconds.ToString("0.00") + "s dash wins"
                + " lean->punch/grapple " + LeanVerbSeconds.ToString("0.00") + "s punch or grapple wins"
                + " hop skip->takeoff " + HopTakeoffSeconds.ToString("0.00") + "s takeoff wins"
                + " aim<->become " + AimBecomeSeconds.ToString("0.00") + "s become wins"
                + " dummy=shared"
                + " mid=" + idleW.ToString("0.00") + "+" + pivotW.ToString("0.00")
                + " lean=" + leanW.ToString("0.00") + "+" + dashW.ToString("0.00")
                + " hop=" + airW.ToString("0.00") + "+" + takeW.ToString("0.00")
                + " aim=" + aimW.ToString("0.00") + "+" + becomeW.ToString("0.00")
                + " sum=1 smoothstep";
        }

        /// <summary>Second set. The newer verb layers, after the first handoff line.</summary>
        public static string ProofLine2()
        {
            LungeEnter(LungeBurstSeconds * 0.5f, 1f, out float gaitW, out float tellW, out float burstW);
            DashExit(AirDashPose.WindowSeconds + DashAirSeconds * 0.5f, 0f, out float dashW, out float strideW, out float fallW);
            return "pose handoff 2"
                + " crouch<->gait " + CrouchGaitSeconds.ToString("0.00") + "s gait or crouch wins"
                + " crouch->slide " + CrouchSlideSeconds.ToString("0.00") + "s slide wins"
                + " dash->fall/stride " + DashAirSeconds.ToString("0.00") + "s stride or fall wins"
                + " mantle->gait " + MantleExitSeconds.ToString("0.00") + "s gait wins"
                + " mantle->land " + MantleLandSeconds.ToString("0.00") + "s land wins"
                + " lunge tell->burst " + LungeBurstSeconds.ToString("0.00") + "s burst wins"
                + " lunge->gait " + LungeRecoverSeconds.ToString("0.00") + "s gait wins"
                + " become->gait " + BecomeGaitSeconds.ToString("0.00") + "s gait wins"
                + " punch->become " + PunchBecomeSeconds.ToString("0.00") + "s become wins"
                + " stop->idle " + StopIdleSeconds.ToString("0.00") + "s idle wins"
                + " dummy=shared"
                + " mid=" + tellW.ToString("0.00") + "+" + burstW.ToString("0.00") + "+" + gaitW.ToString("0.00")
                + " dash=" + dashW.ToString("0.00") + "+" + strideW.ToString("0.00") + "+" + fallW.ToString("0.00")
                + " sum=1 smoothstep";
        }

        public static string ProofLine()
        {
            WallJump01(0.5f, out float wallW, out float pushW, out float strideW);
            return "pose handoff"
                + " slide->jump " + SlideJumpSeconds.ToString("0.00") + "s jump wins"
                + " wall->fall " + WallFallSeconds.ToString("0.00") + "s fall wins"
                + " punch->dash " + PunchDashSeconds.ToString("0.00") + "s dash wins"
                + " punch->jump " + PunchJumpSeconds.ToString("0.00") + "s jump wins"
                + " grapple->fall " + GrappleFallSeconds.ToString("0.00") + "s fall wins"
                + " land->gait " + LandGaitSeconds.ToString("0.00") + "s gait wins"
                + " walljump->stride " + WallStrideSeconds.ToString("0.00") + "s stride wins"
                + " crouch->slide " + CrouchSlideSeconds.ToString("0.00") + "s slide wins"
                + " dash->air " + DashAirSeconds.ToString("0.00") + "s stride or fall wins"
                + " climb/run/air->mantle " + MantleEnterSeconds.ToString("0.00") + "s mantle wins"
                + " mantle->gait " + MantleExitSeconds.ToString("0.00") + "s gait or crouch wins"
                + " mid=" + wallW.ToString("0.00") + "+" + pushW.ToString("0.00") + "+" + strideW.ToString("0.00")
                + " sum=1 smoothstep";
        }
    }
}
