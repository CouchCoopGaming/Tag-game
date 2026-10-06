using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual zip ride. Two hands hang under the cable, legs lead a little,
    /// and a small sway scales with ride speed. The catch and the let-go are
    /// their own poses. Nothing here writes velocity or the root.
    /// </summary>
    public static class ZipPose
    {
        public const bool RootMotion = false;
        public const float RideSpeed = 14f;
        public const float CatchSeconds = 0.12f;
        public const float ReleaseSeconds = 0.16f;
        /// <summary>Degrees of roll at full ride speed.</summary>
        public const float SwayDegrees = 7f;
        public const float SwayRate = 2.4f;

        public const float HangThighL = 34f;
        public const float HangThighR = 30f;
        public const float HangKneeL = -18f;
        public const float HangKneeR = -14f;
        public const float HangHip = -8f;
        public const float HangSpine = -6f;
        public const float HangHead = -4f;
        public const float HangElbow = -8f;
        public const float HangYaw = 8f;

        public const float DropThigh = -24f;
        public const float DropKnee = -10f;
        public const float DropHip = 8f;
        public const float DropSpine = 6f;
        public const float DropHead = -22f;
        public const float DropElbow = -28f;

        public const float ReleasePitch = 28f;
        public const float ReleaseYaw = 36f;
        public const float ReleaseElbow = -18f;
        public const float ReleaseThigh = 14f;
        public const float ReleaseKnee = -16f;
        public const float ReleaseHip = 4f;
        public const float ReleaseSpine = -8f;
        public const float ReleaseHead = 8f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head, LeanZ;
        }

        /// <summary>Both hands on the cable. Legs a little forward. Arms stay the cling reach.</summary>
        public static Sample Hang()
        {
            return new Sample
            {
                ThighL = HangThighL,
                ThighR = HangThighR,
                KneeL = HangKneeL,
                KneeR = HangKneeR,
                ArmPitchL = WallPose.ReachPitch,
                ArmPitchR = WallPose.ReachPitch,
                ArmYawL = HangYaw,
                ArmYawR = -HangYaw,
                ElbowL = HangElbow,
                ElbowR = HangElbow,
                Hip = HangHip,
                Spine = HangSpine,
                Head = HangHead,
                LeanZ = 0f,
            };
        }

        /// <summary>The jump onto the cable. Hands up, legs still trailing.</summary>
        public static Sample JumpDrop()
        {
            return new Sample
            {
                ThighL = DropThigh,
                ThighR = DropThigh + 4f,
                KneeL = DropKnee,
                KneeR = DropKnee,
                ArmPitchL = WallPose.ReachPitch,
                ArmPitchR = WallPose.ReachPitch,
                ArmYawL = HangYaw + 6f,
                ArmYawR = -(HangYaw + 6f),
                ElbowL = DropElbow,
                ElbowR = DropElbow,
                Hip = DropHip,
                Spine = DropSpine,
                Head = DropHead,
                LeanZ = 0f,
            };
        }

        /// <summary>Hands leave the cable. The body opens into the fall.</summary>
        public static Sample Release()
        {
            return new Sample
            {
                ThighL = ReleaseThigh,
                ThighR = ReleaseThigh,
                KneeL = ReleaseKnee,
                KneeR = ReleaseKnee,
                ArmPitchL = ReleasePitch,
                ArmPitchR = ReleasePitch,
                ArmYawL = ReleaseYaw,
                ArmYawR = -ReleaseYaw,
                ElbowL = ReleaseElbow,
                ElbowR = ReleaseElbow,
                Hip = ReleaseHip,
                Spine = ReleaseSpine,
                Head = ReleaseHead,
                LeanZ = 0f,
            };
        }

        /// <summary>0 at the grab, 1 once the hang owns the body.</summary>
        public static float CatchWeight(float age)
        {
            float u = 1f;
            if (CatchSeconds > 0.0001f)
                u = age / CatchSeconds;
            return Ease(u);
        }

        /// <summary>1 on the frame of the let-go, 0 once the fall has it.</summary>
        public static float ReleaseWeight(float age)
        {
            float u = 1f;
            if (ReleaseSeconds > 0.0001f)
                u = age / ReleaseSeconds;
            return 1f - Ease(u);
        }

        /// <summary>Roll in degrees. Full at the locked ride speed, quiet when the ride is slow.</summary>
        public static float Sway(float time, float speed)
        {
            float ride = RideSpeed > 0.001f ? speed / RideSpeed : 0f;
            if (ride < 0f) ride = 0f;
            if (ride > 1f) ride = 1f;
            return Mathf.Sin(time * SwayRate) * SwayDegrees * ride;
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(RideSpeed - 14f) > 0.001f) return false;
            Sample hang = Hang();
            Sample drop = JumpDrop();
            Sample let = Release();
            if (Mathf.Abs(hang.ArmPitchL - WallPose.ReachPitch) > 0.05f) return false;
            if (Mathf.Abs(hang.ArmPitchR - WallPose.ReachPitch) > 0.05f) return false;
            if (hang.ArmPitchL > -90f) return false;
            if (hang.ThighL < 24f || hang.ThighR < 20f) return false;
            if (drop.ThighL > -12f || drop.Head > -16f) return false;
            if (Mathf.Abs(drop.ThighL - hang.ThighL) < 40f) return false;
            if (let.ArmPitchL < 16f) return false;
            if (Mathf.Abs(let.ArmPitchL - hang.ArmPitchL) < 80f) return false;
            if (CatchWeight(0f) > 0.001f || Mathf.Abs(CatchWeight(CatchSeconds) - 1f) > 0.001f) return false;
            if (Mathf.Abs(ReleaseWeight(0f) - 1f) > 0.001f || ReleaseWeight(ReleaseSeconds) > 0.001f) return false;
            if (Mathf.Abs(Sway(0f, RideSpeed)) > 0.001f) return false;
            const float quarter = 1.5707963f;
            if (Mathf.Abs(Sway(quarter / SwayRate, RideSpeed) - SwayDegrees) > 0.05f) return false;
            if (Mathf.Abs(Sway(quarter / SwayRate, 0f)) > 0.001f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample hang = Hang();
            return "zip pose"
                + " hangPitch=" + hang.ArmPitchL.ToString("0")
                + " hangThigh=" + hang.ThighL.ToString("0")
                + " dropThigh=" + JumpDrop().ThighL.ToString("0")
                + " releasePitch=" + Release().ArmPitchL.ToString("0")
                + " sway=" + SwayDegrees.ToString("0")
                + " ride=" + RideSpeed.ToString("0")
                + " catch=" + CatchSeconds.ToString("0.00")
                + " release=" + ReleaseSeconds.ToString("0.00")
                + " rootMotion=0";
        }

        static float Ease(float u)
        {
            if (u <= 0f) return 0f;
            if (u >= 1f) return 1f;
            return u * u * (3f - 2f * u);
        }
    }
}
