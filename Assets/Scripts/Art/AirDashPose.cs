using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual air dash only. For the motor's 0.10 s window the body goes
    /// horizontal and the arms trail back. After that window the body hands
    /// off to the air stride, or to the fall pose once vertical speed is down.
    /// Ribbons and the ready wink stay on their own tells.
    /// Speed, duration, and cooldown are not written here. No root motion.
    /// </summary>
    public static class AirDashPose
    {
        public const bool RootMotion = false;

        /// <summary>Matches the locked dash window. This pose does not set it.</summary>
        public const float WindowSeconds = 0.10f;
        /// <summary>Dash into the air stride, or into the fall beat. The stride or the fall wins.</summary>
        public const float HandoffSeconds = 0.10f;
        /// <summary>Fast enough that the lean is held inside the window, not still arriving as it ends.</summary>
        public const float Slew = 2800f;

        /// <summary>Falling at and below this vertical speed takes the fall beat. The apex stays a stride.</summary>
        public const float FallGate = -1f;

        /// <summary>Chest toward the face when the dash is straight ahead. Hip plus spine is near horizontal. A back dash flips the sign.</summary>
        public const float LeanHip = 58f;
        public const float LeanSpine = 24f;
        public const float LeanHead = 6f;
        /// <summary>Roll into a side dash. Positive is pawn-right.</summary>
        public const float LeanRoll = 34f;
        public const float HipRollShare = 0.55f;

        /// <summary>Arms trail behind the shoulders. Positive pitch is back on this rig.</summary>
        public const float BackPitch = 70f;
        public const float BackYaw = 10f;
        public const float BackElbow = -18f;
        /// <summary>Legs trail. Negative thigh is behind the hip.</summary>
        public const float TrailThigh = -16f;
        public const float TrailKnee = -12f;

        public struct Sample
        {
            public float ThighL, ThighR, KneeL, KneeR;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
            public float Hip, Spine, Head, LeanZ;
        }

        /// <summary>
        /// localRight and localForward are the dash direction in pawn space.
        /// Each is about -1..1. Forward leans the chest toward the face.
        /// </summary>
        public static Sample At(float localRight, float localForward)
        {
            float fwd = ClampSigned(localForward);
            float side = ClampSigned(localRight);
            return new Sample
            {
                ThighL = TrailThigh,
                ThighR = TrailThigh,
                KneeL = TrailKnee,
                KneeR = TrailKnee,
                ArmPitchL = BackPitch,
                ArmPitchR = BackPitch,
                ArmYawL = BackYaw,
                ArmYawR = BackYaw,
                ElbowL = BackElbow,
                ElbowR = BackElbow,
                Hip = LeanHip * fwd,
                Spine = LeanSpine * fwd,
                Head = LeanHead * fwd,
                LeanZ = LeanRoll * side,
            };
        }

        /// <summary>1 for the whole window, then down across the handoff. Negative age is not a dash.</summary>
        public static float DashWeight(float age)
        {
            if (age < 0f) return 0f;
            if (age <= WindowSeconds) return 1f;
            float u = (age - WindowSeconds) / HandoffSeconds;
            return 1f - PoseHandoff.Ease(u);
        }

        /// <summary>0 during the window. 1 once the handoff has finished. Sums with <see cref="DashWeight"/>.</summary>
        public static float NextWeight(float age)
        {
            if (age < 0f) return 0f;
            return 1f - DashWeight(age);
        }

        /// <summary>True once vertical speed has left the apex stride.</summary>
        public static bool ToFall(float verticalSpeed)
        {
            return verticalSpeed <= FallGate;
        }

        /// <summary>
        /// 0 on the stride, through the apex. 1 on the full fall beat at <see cref="JumpPose.FallVy"/>.
        /// The gate is not a snap: the body eases across that span.
        /// </summary>
        public static float FallBlend(float verticalSpeed)
        {
            if (verticalSpeed >= FallGate) return 0f;
            float span = JumpPose.FallVy - FallGate;
            if (span > -0.0001f && span < 0.0001f) return 1f;
            float u = (verticalSpeed - FallGate) / span;
            return PoseHandoff.Ease(u);
        }

        /// <summary>Air stride while rising or at the apex. Falls ease into the full fall beat.</summary>
        public static JumpPose.Sample Exit(float verticalSpeed, float planarSpeed, float sinC, float cycleRadians)
        {
            JumpPose.Sample stride = JumpPose.Stride(planarSpeed, sinC, cycleRadians);
            JumpPose.Sample fall = JumpPose.At(JumpPose.FallVy, 1f, true);
            return Lerp(stride, fall, FallBlend(verticalSpeed));
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Mathf.Abs(WindowSeconds - 0.10f) > 0.001f) return false;
            if (HandoffSeconds < 0.08f || HandoffSeconds > 0.12f) return false;
            if (Slew < 2000f) return false;
            if (Mathf.Abs(DashWeight(0f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(DashWeight(WindowSeconds) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(DashWeight(WindowSeconds * 0.5f) - 1f) > 0.0001f) return false;
            if (DashWeight(-0.01f) > 0.0001f) return false;
            if (NextWeight(-0.01f) > 0.0001f) return false;
            if (DashWeight(WindowSeconds + HandoffSeconds) > 0.0001f) return false;
            if (Mathf.Abs(NextWeight(WindowSeconds + HandoffSeconds) - 1f) > 0.0001f) return false;
            float prev = 2f;
            for (int i = 0; i <= 8; i++)
            {
                float age = WindowSeconds + HandoffSeconds * i / 8f;
                float dashW = DashWeight(age);
                float nextW = NextWeight(age);
                if (Mathf.Abs(dashW + nextW - 1f) > 0.0001f) return false;
                if (dashW > prev + 0.0001f) return false;
                prev = dashW;
            }

            if (Mathf.Abs(DashWeight(WindowSeconds + HandoffSeconds * 0.5f) - 0.5f) > 0.0001f) return false;

            Sample ahead = At(0f, 1f);
            Sample back = At(0f, -1f);
            Sample right = At(1f, 0f);
            Sample left = At(-1f, 0f);
            if (ahead.Hip < 40f || ahead.Spine < 24f) return false;
            if (back.Hip > -40f || back.Spine > -24f) return false;
            if (Mathf.Abs(ahead.LeanZ) > 0.01f || Mathf.Abs(back.LeanZ) > 0.01f) return false;
            if (right.LeanZ < 30f || left.LeanZ > -30f) return false;
            if (Mathf.Abs(right.Hip) > 0.01f || Mathf.Abs(left.Hip) > 0.01f) return false;
            Sample diag = At(0.6f, 0.8f);
            if (diag.Hip <= 0f || diag.LeanZ <= 0f) return false;

            if (ahead.Hip + ahead.Spine < 75f || ahead.Hip + ahead.Spine > 95f) return false;
            if (ahead.ElbowL < -40f || ahead.ElbowR < -40f) return false;
            if (Mathf.Abs(ahead.ElbowL - BackElbow) > 0.01f) return false;
            if (Mathf.Abs(ahead.ElbowR - BackElbow) > 0.01f) return false;
            if (ahead.ArmYawL > 16f || ahead.ArmYawR > 16f) return false;
            if (ahead.ArmPitchL < 40f || ahead.ArmPitchR < 40f) return false;
            if (Mathf.Abs(ahead.ArmPitchL - ahead.ArmPitchR) > 0.01f) return false;
            if (ahead.ThighL > -8f || ahead.KneeL < -30f) return false;

            if (ToFall(8f) || ToFall(0f) || !ToFall(-4f) || !ToFall(-16f)) return false;
            if (FallBlend(8f) > 0.0001f || FallBlend(0f) > 0.0001f || FallBlend(FallGate) > 0.0001f) return false;
            if (FallBlend(JumpPose.FallVy) < 0.999f || FallBlend(-16f) < 0.999f) return false;
            float midVy = (FallGate + JumpPose.FallVy) * 0.5f;
            if (Mathf.Abs(FallBlend(midVy) - 0.5f) > 0.0001f) return false;
            float prevFall = -1f;
            for (int i = 0; i <= 8; i++)
            {
                float vy = FallGate + (JumpPose.FallVy - FallGate) * i / 8f;
                float fb = FallBlend(vy);
                if (fb + 0.0001f < prevFall) return false;
                prevFall = fb;
            }
            const float cycle = 0.9f;
            float sinC = Mathf.Sin(cycle);
            JumpPose.Sample stride = Exit(8f, 12f, sinC, cycle);
            JumpPose.Sample fall = Exit(-16f, 12f, sinC, cycle);
            if (fall.ArmYawL > -50f) return false;
            if (fall.ThighL > 25f) return false;
            if (stride.ArmYawL < fall.ArmYawL) return false;
            if (Mathf.Abs(stride.ThighL - stride.ThighR) < 4f) return false;
            return true;
        }

        public static string ProofLine()
        {
            Sample ahead = At(0f, 1f);
            Sample right = At(1f, 0f);
            float mid = DashWeight(WindowSeconds + HandoffSeconds * 0.5f);
            return "air dash pose"
                + " window=" + WindowSeconds.ToString("0.00")
                + " leanHip=" + LeanHip.ToString("0")
                + " leanSpine=" + LeanSpine.ToString("0")
                + " leanRoll=" + LeanRoll.ToString("0")
                + " backPitch=" + BackPitch.ToString("0")
                + " backYaw=" + BackYaw.ToString("0")
                + " backElbow=" + BackElbow.ToString("0")
                + " trailThigh=" + TrailThigh.ToString("0")
                + " trailKnee=" + TrailKnee.ToString("0")
                + " flat=" + (LeanHip + LeanSpine).ToString("0")
                + " aheadHip=" + ahead.Hip.ToString("0")
                + " rightRoll=" + right.LeanZ.ToString("0")
                + " handoff=" + HandoffSeconds.ToString("0.00")
                + " midDash=" + mid.ToString("0.00")
                + " fallGate=" + FallGate.ToString("0")
                + " slew=" + Slew.ToString("0")
                + " gate=committed weight 1 for 0.." + WindowSeconds.ToString("0.00")
                + "s lean sign follows pawn-local dash dir"
                + "; body horizontal-ish, arms back, legs trailing"
                + "; handoff smoothstep " + HandoffSeconds.ToString("0.00")
                + "s to air stride while vy>" + FallGate.ToString("0")
                + " else ease into JumpPose fall by vy " + JumpPose.FallVy.ToString("0")
                + "; tell=AirDashTell wink=AirDashCooldownTell"
                + "; speed duration cooldown untouched"
                + "; rootMotion=0";
        }

        static float ClampSigned(float v)
        {
            if (v < -1f) return -1f;
            if (v > 1f) return 1f;
            return v;
        }

        static JumpPose.Sample Lerp(JumpPose.Sample a, JumpPose.Sample b, float t)
        {
            return new JumpPose.Sample
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
            };
        }
    }
}
