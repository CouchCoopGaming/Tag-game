using UnityEngine;

namespace Tag.Movement
{
    /// <summary>
    /// Systems Tag v1 movement numbers — baked defaults for Apex-adjacent kit.
    /// Create via Assets → Create → Tag → Movement Tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementTuning", menuName = "Tag/Movement Tuning", order = 0)]
    public class MovementTuning : ScriptableObject
    {
        [Header("Ground speeds (m/s)")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7.0f;

        [Header("Acceleration (seconds to full)")]
        public float accelTime = 0.18f;
        public float brakeTime = 0.12f;

        [Header("Turn rates (deg/s)")]
        public float turnRateWalk = 540f;
        public float turnRateSprint = 380f;

        [Header("Jump / momentum")]
        public float jumpApexHeight = 1.15f;
        public float gravity = 28f;
        public float jumpLaunchSpeed = 8.0f;
        public float coyoteTime = 0.120f;
        public float jumpBuffer = 0.140f;
        [Range(0f, 1f)] public float airControlPercent = 0.45f;
        [Tooltip("Planar speed retained on any grounded/coyote jump takeoff (walk, sprint, slide-exit). 1 = full carry.")]
        [Range(0f, 1f)] public float jumpHorizRetain = 1.0f;
        [Tooltip("Additional planar retain multiplier when sprint-held at takeoff (stacked with jumpHorizRetain).")]
        [Range(0f, 1f)] public float sprintJumpHorizRetain = 1.0f;
        [Tooltip("When airborne with move input, planar speed floor is max(wishSpeed, current*this). 1 = never bleed carried momentum toward walk.")]
        [Range(0f, 1f)] public float airMomentumPreserve = 1.0f;
        [Tooltip("Hard land if fall distance exceeds this × jump apex → apply horiz penalty.")]
        public float hardLandFallMultiple = 1.5f;
        [Tooltip("Horiz speed reduction fraction during hard-land window (0.15 → keep ×0.85). Never zeroes velocity.")]
        public float hardLandHorizPenalty = 0.15f;
        public float hardLandPenaltyDuration = 0.1f;

        [Header("Slide")]
        public float slideSpeedGate = 5.5f;
        public float slideDuration = 0.70f;
        public float slidePunchDuration = 0.15f;
        [Range(0f, 1f)] public float slideEndSpeedPercent = 0.55f;
        public float slideHeight = 0.9f;
        public float standHeight = 1.8f;
        public float slideJumpHorizBonus = 0.12f;
        public float slideCooldown = 0.080f;
        [Tooltip("If true, snap planar speed to slideSpeedGate on enter. Systems Tag v1: false — keep current horiz.")]
        public bool slideEnterWipe = false;

        [Header("Air dodge (juke) — Systems Tag v1")]
        [Tooltip("Charges available while airborne (recharge on ground).")]
        public int airDodgeCharges = 1;
        [Tooltip("Planar speed set on air dodge (m/s). Replaces horiz toward input, or facing if no input; keeps vertical.")]
        public float airDodgeSpeed = 6.5f;
        [Tooltip("No air control during lock (seconds). Systems: 130 ms.")]
        public float airDodgeLock = 0.130f;
        [Tooltip("I-frames vs punch hurtbox only (seconds). Systems: 100 ms.")]
        public float airDodgeIFrames = 0.100f;
        [Tooltip("Input buffer for air dodge (seconds). Systems: 80 ms.")]
        public float airDodgeBuffer = 0.080f;
        [Tooltip("Grounded footfalls required to recharge (optional; travel fallback preferred).")]
        public int airDodgeRechargeSteps = 3;
        [Tooltip("Grounded travel (m) to recharge one charge. Systems Tag v1 fallback: 1.8 m.")]
        public float airDodgeRechargeTravel = 1.8f;

        [Header("Wall run")]
        public float wallRunAttachSpeed = 5.0f;
        public float wallRunFaceAngleMax = 70f;
        public float wallRunVelAngleMax = 55f;
        public float wallRunMaxDuration = 1.25f;
        [Range(0f, 1f)] public float wallRunGravityScale = 0.35f;
        public float wallRunDetachOppositeHold = 0.100f;
        public float wallRunMinSpeed = 3.5f;
        public float sameWallCooldown = 0.350f;
        public int wallChainCap = 3;
        public float wallChainAttachMinAfterCap = 6.2f;

        [Header("Wall jump")]
        public float wallJumpOutSpeed = 6.5f;
        public float wallJumpUpSpeed = 5.5f;
        [Range(0f, 1f)] public float wallJumpSteerPercent = 0.20f;
        [Range(0f, 1f)] public float wallJumpAlongPercent = 0.50f;
        public float wallJumpFalloffWindow = 0.8f;
        public float wallJumpFalloffMult = 0.85f;
        public float wallJumpFalloffFloor = 0.55f;
        public float wallJumpBuffer = 0.100f;

        [Header("Vault")]
        public float vaultLowMin = 0.45f;
        public float vaultLowMax = 1.10f;
        public float vaultLowSpeedGate = 3.5f;
        public float vaultLowLock = 0.28f;
        [Range(0f, 1f)] public float vaultLowRetain = 0.95f;
        public float vaultHighMin = 1.10f;
        public float vaultHighMax = 1.70f;
        public float vaultHighSpeedGate = 4.0f;
        public float vaultHighLock = 0.40f;
        [Range(0f, 1f)] public float vaultHighRetain = 0.85f;
        public float vaultConeDegrees = 35f;
        public float vaultFailSpeedPenalty = 0.40f;
        public float vaultFailPenaltyDuration = 0.15f;
        [Range(0f, 1f)] public float vaultLipJumpWindow = 0.30f;

        [Header("Capsule")]
        public float capsuleRadius = 0.4f;

        /// <summary>Runtime fallback when no asset is assigned.</summary>
        public static MovementTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<MovementTuning>();
            t.name = "MovementTuning (Runtime)";
            return t;
        }
    }
}
