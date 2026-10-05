using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual air-strafe lean only. JumpPose takeoff, rise, and fall stay the
    /// base. While airborne, a side wish that is adding horizontal speed rolls
    /// the chest and hips into that side and lets the arms counterbalance.
    /// The amount is the vertical-velocity fall blend, so the tuck stays the
    /// rise and the bank reads on the way down. Air dash, wall, grapple, and
    /// punch own the bones instead. No root motion, and the air math is not written.
    /// </summary>
    public static class AirStrafeLeanPose
    {
        public const bool RootMotion = false;

        /// <summary>Follows the fall blend. The air torso slew is slower than this.</summary>
        public const float Slew = 140f;

        /// <summary>Same gate the motor uses for the air-strafe bonus. The bonus is not written.</summary>
        public const float SideGate = 0.2f;
        /// <summary>Stick noise under this is not a strafe. A real side key is past it.</summary>
        public const float SideDeadzone = 0.05f;

        /// <summary>Chest roll, degrees. Modest next to a wall-run tilt, readable at the chase boom.</summary>
        public const float Roll = 18f;
        /// <summary>Hip roll into the same side, a little less so the chest leads.</summary>
        public const float HipLean = 12f;
        /// <summary>Head rolls against the chest so the face stays up.</summary>
        public const float HeadShare = 0.30f;

        /// <summary>High-side arm drops, low-side arm lifts. Added on the jump arms.</summary>
        public const float ArmCounterPitch = 20f;
        /// <summary>High-side arm reaches out, low-side arm tucks. Jump yaw space, before the right-arm negate.</summary>
        public const float ArmCounterYaw = 16f;
        /// <summary>Low-side elbow bends, high-side elbow opens.</summary>
        public const float ElbowCounter = 8f;

        public struct Sample
        {
            public float Roll, HipRoll, HeadRoll;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR;
            public float ElbowL, ElbowR;
        }

        /// <summary>+1 strafe right, -1 strafe left, 0 when the stick has no side.</summary>
        public static float Side(float moveX)
        {
            if (moveX > SideDeadzone) return 1f;
            if (moveX < -SideDeadzone) return -1f;
            return 0f;
        }

        /// <summary>
        /// True when one Quake air step would lengthen horizontal velocity.
        /// The step is not written back. Wish speed and accel stay on the motor.
        /// </summary>
        public static bool AddsSpeed(Vector3 hv, Vector3 wish, float wishSpeed, float accelPerSecond, float dt)
        {
            if (wish.sqrMagnitude <= 0.01f || wishSpeed <= 0f || accelPerSecond <= 0f || dt <= 0f)
                return false;
            float before = hv.magnitude;
            Vector3 next = KinematicStep.AirSteer(hv, wish, wishSpeed, accelPerSecond, dt);
            return next.magnitude > before + 0.0001f;
        }

        /// <summary>
        /// 0 through takeoff and the rise. 1 on the fall. Same curve as JumpPose.Extend,
        /// so the apex does not move with horizontal speed.
        /// </summary>
        public static float FallBlend(float verticalSpeed, float jumpAge)
        {
            if (jumpAge < 0f) return 0f;
            float take = JumpPose.TakeoffWeight(jumpAge);
            return JumpPose.Extend(verticalSpeed) * (1f - take);
        }

        /// <summary>Air dash, wall, grapple, and punch keep their poses. 0 while any of them owns the body.</summary>
        public static float Yield(bool airDash, bool wall, bool grapple, bool punch)
        {
            if (airDash || wall || grapple || punch) return 0f;
            return 1f;
        }

        /// <summary>
        /// Signed pose at full fall. Positive side is a right strafe: the right
        /// shoulder and hip drop, the left arm drops and reaches out, the right
        /// arm lifts and tucks. Multiply by <see cref="FallBlend"/>.
        /// </summary>
        public static Sample At(float side)
        {
            float s = side < 0f ? -1f : (side > 0f ? 1f : 0f);
            float roll = -Roll * s;
            return new Sample
            {
                Roll = roll,
                HipRoll = -HipLean * s,
                HeadRoll = -roll * HeadShare,
                ArmPitchL = ArmCounterPitch * s,
                ArmPitchR = -ArmCounterPitch * s,
                ArmYawL = ArmCounterYaw * s,
                ArmYawR = -ArmCounterYaw * s,
                ElbowL = ElbowCounter * s,
                ElbowR = -ElbowCounter * s,
            };
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (Slew < 100f || Slew > 180f) return false;
            if (Mathf.Abs(SideGate - 0.2f) > 0.001f) return false;
            if (SideDeadzone <= 0f || SideDeadzone >= SideGate) return false;
            if (Roll < 14f || Roll > 24f) return false;
            if (HipLean < 8f || HipLean >= Roll) return false;
            if (Roll >= WallPose.RunTilt) return false;
            if (HeadShare < 0.2f || HeadShare > 0.45f) return false;
            if (ArmCounterPitch < 12f || ArmCounterPitch > 28f) return false;
            if (ArmCounterYaw < 10f || ArmCounterYaw > 24f) return false;
            if (ElbowCounter < 4f || ElbowCounter > 14f) return false;

            if (Side(0f) != 0f || Side(SideDeadzone) != 0f || Side(-SideDeadzone) != 0f) return false;
            if (Mathf.Abs(Side(1f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Side(-1f) + 1f) > 0.0001f) return false;

            if (FallBlend(24.7f, -1f) > 0.0001f) return false;
            if (FallBlend(24.7f, 0.2f) > 0.0001f) return false;
            if (FallBlend(-16f, 0f) > 0.0001f) return false;
            if (Mathf.Abs(FallBlend(-16f, 0.5f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(FallBlend(JumpPose.FallVy, 0.5f) - 1f) > 0.0001f) return false;
            float apex = FallBlend(0f, 0.4f);
            if (Mathf.Abs(apex - JumpPose.Extend(0f)) > 0.0001f) return false;
            if (Mathf.Abs(JumpPose.ExtendAt(0f, 0f) - JumpPose.ExtendAt(0f, 24f)) > 0.0001f) return false;
            if (JumpPose.Extend(24.7f) > 0.0001f) return false;
            float prev = -1f;
            float[] vys = { 24f, 8f, 0f, -6f, -12f, -16f };
            for (int i = 0; i < vys.Length; i++)
            {
                float w = FallBlend(vys[i], 0.4f);
                if (w + 0.0001f < prev) return false;
                prev = w;
            }

            if (Yield(true, false, false, false) > 0.0001f) return false;
            if (Yield(false, true, false, false) > 0.0001f) return false;
            if (Yield(false, false, true, false) > 0.0001f) return false;
            if (Yield(false, false, false, true) > 0.0001f) return false;
            if (Mathf.Abs(Yield(false, false, false, false) - 1f) > 0.0001f) return false;

            Sample right = At(1f);
            Sample left = At(-1f);
            Sample none = At(0f);
            if (right.Roll >= 0f || right.HipRoll >= 0f) return false;
            if (Mathf.Abs(right.Roll - -Roll) > 0.01f) return false;
            if (Mathf.Abs(right.HipRoll - -HipLean) > 0.01f) return false;
            if (right.HeadRoll <= 0f) return false;
            if (Mathf.Abs(right.HeadRoll - Roll * HeadShare) > 0.01f) return false;
            if (right.ArmPitchL <= 0f || right.ArmPitchR >= 0f) return false;
            if (right.ArmYawL <= 0f || right.ArmYawR >= 0f) return false;
            if (right.ElbowL <= 0f || right.ElbowR >= 0f) return false;
            if (Mathf.Abs(left.Roll + right.Roll) > 0.01f) return false;
            if (Mathf.Abs(left.HipRoll + right.HipRoll) > 0.01f) return false;
            if (Mathf.Abs(left.ArmPitchL + right.ArmPitchL) > 0.01f) return false;
            if (Mathf.Abs(left.ArmYawR + right.ArmYawR) > 0.01f) return false;
            if (Mathf.Abs(none.Roll) > 0.01f || Mathf.Abs(none.HipRoll) > 0.01f) return false;
            if (Mathf.Abs(none.ArmPitchL) > 0.01f || Mathf.Abs(none.ArmYawL) > 0.01f) return false;

            const float dt = 1f / 60f;
            Vector3 forward = new Vector3(0f, 0f, 12f);
            if (!AddsSpeed(forward, new Vector3(1f, 0f, 0f), 12f, 30f, dt)) return false;
            if (AddsSpeed(forward, new Vector3(0f, 0f, 1f), 12f, 30f, dt)) return false;
            if (AddsSpeed(new Vector3(12f, 0f, 0f), new Vector3(1f, 0f, 0f), 12f, 30f, dt)) return false;
            Vector3 diag = WishAccel.PlanarWish(Vector3.forward, Vector3.right, new Vector2(1f, 1f));
            if (!AddsSpeed(forward, diag, 12f, 30f, dt)) return false;
            if (AddsSpeed(forward, Vector3.zero, 12f, 30f, dt)) return false;

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airAccel - 30f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airStrafeBonus - 1.35f) > 0.001f) return false;
            if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f) return false;
            if (cfg.slideBoost != 0f) return false;
            Vector3 plain = KinematicStep.AirSteer(forward, new Vector3(1f, 0f, 0f), 12f, cfg.airAccel, dt);
            Vector3 bonus = KinematicStep.AirSteer(forward, new Vector3(1f, 0f, 0f), 12f, cfg.airAccel * cfg.airStrafeBonus, dt);
            if (Mathf.Abs(plain.x - cfg.airAccel * dt) > 0.001f) return false;
            if (!(plain.magnitude > 12f)) return false;
            if (!(bonus.magnitude > plain.magnitude)) return false;
            if (Mathf.Abs(JumpPose.FallArmYaw - 58f) > 0.01f) return false;
            if (Mathf.Abs(JumpPose.FallThigh - 16f) > 0.01f) return false;
            return true;
        }

        public static string ProofLine()
        {
            const float dt = 1f / 60f;
            Vector3 forward = new Vector3(0f, 0f, 12f);
            bool side = AddsSpeed(forward, new Vector3(1f, 0f, 0f), 12f, 30f, dt);
            bool straight = AddsSpeed(forward, new Vector3(0f, 0f, 1f), 12f, 30f, dt);
            Vector3 diag = WishAccel.PlanarWish(Vector3.forward, Vector3.right, new Vector2(1f, 1f));
            bool diagGain = AddsSpeed(forward, diag, 12f, 30f, dt);
            Sample right = At(1f);
            return "air strafe lean"
                + " roll=" + Roll.ToString("0")
                + " hip=" + HipLean.ToString("0")
                + " headShare=" + HeadShare.ToString("0.00")
                + " armPitch=" + ArmCounterPitch.ToString("0")
                + " armYaw=" + ArmCounterYaw.ToString("0")
                + " elbow=" + ElbowCounter.ToString("0")
                + " rightRoll=" + right.Roll.ToString("0")
                + " rightHip=" + right.HipRoll.ToString("0")
                + " rightArmL=" + right.ArmPitchL.ToString("0")
                + " rightArmR=" + right.ArmPitchR.ToString("0")
                + " rise=" + FallBlend(24.7f, 0.2f).ToString("0.00")
                + " apex=" + FallBlend(0f, 0.4f).ToString("0.00")
                + " fall=" + FallBlend(-16f, 0.5f).ToString("0.00")
                + " takeoff=" + FallBlend(-16f, 0f).ToString("0.00")
                + " sideGain=" + (side ? "1" : "0")
                + " straightGain=" + (straight ? "1" : "0")
                + " diagGain=" + (diagGain ? "1" : "0")
                + " yield=airdash+wall+grapple+punch"
                + " blend=JumpPose.Extend(vy)*(1-takeoff)"
                + " slew=" + Slew.ToString("0")
                + " gate=side wish Move.x past " + SideDeadzone.ToString("0.00")
                + " and AirSteer lengthens hv; bonus gate |Move.y|<" + SideGate.ToString("0.00") + " is read only"
                + " rootMotion=0";
        }
    }
}
