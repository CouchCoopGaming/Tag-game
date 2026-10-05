using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Visual bunny-hop chain only. A jump that is already buffered, or that
    /// fires within about one frame of the landing, skips the land absorb so
    /// the thud does not pop against the next takeoff. The stride phase keeps
    /// advancing. The air-strafe lean already on the fall is handed into the
    /// next airborne beat. Coyote, the jump buffer, jump speed, and the apex
    /// are not written. No root motion.
    /// </summary>
    public static class BunnyHopPose
    {
        public const bool RootMotion = false;

        /// <summary>
        /// About one frame at 60 Hz. A jump later than this keeps the thud.
        /// Shorter than coyote (0.10) and the jump buffer (0.16). Those stay put.
        /// </summary>
        public const float ChainSeconds = 1f / 60f;

        /// <summary>
        /// Seconds since the landing sample are inside the chain window.
        /// The window is at least one pose frame, and about one 60 Hz frame.
        /// Negative means there is no landing to chain from.
        /// </summary>
        public static bool InWindow(float secondsSinceLand, float dt)
        {
            if (secondsSinceLand < 0f) return false;
            float window = ChainSeconds;
            if (dt > window) window = dt;
            return secondsSinceLand <= window + 0.0001f;
        }

        /// <summary>
        /// True when the landing sample already consumed a jump, or a jump
        /// fires while that landing is still inside <see cref="InWindow"/>.
        /// </summary>
        public static bool Chain(bool buffered, float secondsSinceLand, bool jumpFires, float dt)
        {
            if (!InWindow(secondsSinceLand, dt)) return false;
            if (buffered) return true;
            return jumpFires;
        }

        /// <summary>
        /// 0 skips the land absorb. The landing sample stays quiet so the next
        /// frame can still chain. After the window, a stay-down landing is 1.
        /// </summary>
        public static float Absorb(bool chain, float secondsSinceLand)
        {
            if (chain) return 0f;
            if (secondsSinceLand >= 0f && secondsSinceLand <= 0.0001f) return 0f;
            return 1f;
        }

        /// <summary>
        /// Air cadence step. A chain does not plant the cycle onto a multiple of pi.
        /// A non-chain returns the phase unchanged so the gait keeps its own step.
        /// </summary>
        public static float Phase(float cycleRadians, float airRate, float dt, bool chain)
        {
            if (!chain || dt <= 0f || airRate <= 0f) return cycleRadians;
            return cycleRadians + airRate * dt;
        }

        /// <summary>
        /// Lean weight for the next airborne beat. A chain keeps the bank that
        /// was already on until this jump's own fall blend catches up. A normal
        /// jump uses the fall blend alone, so the rise stays unbanked.
        /// </summary>
        public static float LeanHand(float carried, float fallBlend, bool chain)
        {
            float next = fallBlend < 0f ? 0f : fallBlend;
            if (!chain) return next;
            float keep = carried < 0f ? 0f : carried;
            return keep > next ? keep : next;
        }

        public static bool Holds()
        {
            if (RootMotion) return false;
            if (ChainSeconds <= 0f || ChainSeconds >= 0.05f) return false;
            const float dt = 1f / 60f;
            if (Mathf.Abs(ChainSeconds - dt) > 0.0001f) return false;

            if (!InWindow(0f, dt) || !InWindow(dt, dt)) return false;
            if (InWindow(-1f, dt) || InWindow(0.05f, dt)) return false;
            if (InWindow(dt * 2f, dt)) return false;

            if (!Chain(true, 0f, false, dt)) return false;
            if (!Chain(false, 0f, true, dt)) return false;
            if (!Chain(false, dt, true, dt)) return false;
            if (Chain(false, 0f, false, dt)) return false;
            if (Chain(false, 0.05f, true, dt)) return false;
            if (Chain(true, 0.05f, true, dt)) return false;
            if (Chain(false, -1f, true, dt)) return false;
            if (Chain(true, -1f, false, dt)) return false;

            if (Absorb(true, 0f) > 0.0001f) return false;
            if (Absorb(true, 0.05f) > 0.0001f) return false;
            if (Absorb(false, 0f) > 0.0001f) return false;
            if (Mathf.Abs(Absorb(false, 0.05f) - 1f) > 0.0001f) return false;
            if (Mathf.Abs(Absorb(false, -1f) - 1f) > 0.0001f) return false;

            const float phase0 = 1.2f;
            const float airRate = 9f;
            float phase1 = Phase(phase0, airRate, dt, true);
            if (Mathf.Abs((phase1 - phase0) - airRate * dt) > 0.0001f) return false;
            if (Mathf.Abs(Phase(phase0, airRate, dt, false) - phase0) > 0.0001f) return false;
            const float pi = 3.14159265f;
            float plant = pi * (phase0 >= pi ? 1f : 0f);
            if (Mathf.Abs(phase1 - plant) < 0.05f) return false;

            if (Mathf.Abs(LeanHand(0.75f, 0f, true) - 0.75f) > 0.0001f) return false;
            if (Mathf.Abs(LeanHand(0.75f, 0.2f, true) - 0.75f) > 0.0001f) return false;
            if (Mathf.Abs(LeanHand(0.2f, 0.9f, true) - 0.9f) > 0.0001f) return false;
            if (LeanHand(0.75f, 0f, false) > 0.0001f) return false;
            if (Mathf.Abs(LeanHand(0.75f, 0.4f, false) - 0.4f) > 0.0001f) return false;
            if (Mathf.Abs(LeanHand(-1f, 0.4f, true) - 0.4f) > 0.0001f) return false;

            if (Mathf.Abs(JumpPose.ExtendAt(0f, 0f) - JumpPose.ExtendAt(0f, 24f)) > 0.0001f) return false;
            if (JumpPose.Extend(24.7f) > 0.0001f) return false;
            if (Mathf.Abs(AirStrafeLeanPose.FallBlend(0f, 0.4f) - JumpPose.Extend(0f)) > 0.0001f) return false;
            if (AirStrafeLeanPose.FallBlend(24.7f, 0.2f) > 0.0001f) return false;

            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            if (Mathf.Abs(cfg.coyoteTime - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpBuffer - 0.16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.clingReleaseGrace - 0.08f) > 0.001f) return false;
            if (Mathf.Abs(cfg.jumpSpeed - 24.7f) > 0.001f) return false;
            if (cfg.slideBoost != 0f) return false;
            if (Mathf.Abs(cfg.airDashDuration - 0.10f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashSpeed - 15f) > 0.001f) return false;
            if (Mathf.Abs(cfg.airDashCooldown - 30f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSpeed - 6.0f) > 0.001f) return false;
            if (Mathf.Abs(cfg.climbSlipSpeed - 3.7f) > 0.001f) return false;
            if (Mathf.Abs(cfg.wallRunSpeed - 9.5f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeSpeed - 16f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeDuration - 0.20f) > 0.001f) return false;
            if (Mathf.Abs(cfg.taggerLungeCooldown - 1f) > 0.001f) return false;
            if (Mathf.Abs(PunchTagPose.ReachMeters - 1.55f) > 0.001f) return false;
            if (ChainSeconds >= cfg.coyoteTime || ChainSeconds >= cfg.jumpBuffer) return false;
            return true;
        }

        public static string ProofLine()
        {
            const float dt = 1f / 60f;
            bool buffered = Chain(true, 0f, false, dt);
            bool next = Chain(false, dt, true, dt);
            bool late = Chain(false, 0.05f, true, dt);
            const float phase0 = 1.2f;
            float phase1 = Phase(phase0, 9f, dt, true);
            return "bunny-hop pose"
                + " window=" + ChainSeconds.ToString("0.000")
                + " absorbChain=" + Absorb(true, 0f).ToString("0.00")
                + " absorbLand=" + Absorb(false, 0f).ToString("0.00")
                + " absorbStay=" + Absorb(false, 0.05f).ToString("0.00")
                + " buffered=" + (buffered ? "1" : "0")
                + " next=" + (next ? "1" : "0")
                + " late=" + (late ? "1" : "0")
                + " phase0=" + phase0.ToString("0.00")
                + " phase1=" + phase1.ToString("0.00")
                + " leanTakeoff=" + LeanHand(0.75f, 0f, true).ToString("0.00")
                + " leanFall=" + LeanHand(0.75f, 1f, true).ToString("0.00")
                + " leanPlain=" + LeanHand(0.75f, 0f, false).ToString("0.00")
                + " apex=" + JumpPose.Extend(0f).ToString("0.00")
                + " coyote=0.10 buffer=0.16 jumpSpeed=24.7"
                + " gate=buffered or jump within one frame; stride phase advances; air-strafe lean hands into the next beat"
                + " rootMotion=0";
        }
    }
}
