using UnityEngine;

namespace Tag.Movement
{
    /// <summary>
    /// Party movement numbers — Apex-inspired, punchier for giant arenas.
    /// Create via Assets → Create → Tag → Movement Tuning.
    /// See Docs/MOVEMENT.md for Apex refs vs Tag defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementTuning", menuName = "Tag/Movement Tuning", order = 0)]
    public class MovementTuning : ScriptableObject
    {
        [Header("Ground speeds (m/s)")]
        [Tooltip("Tag party walk. Apex ref ~5.07.")]
        public float walkSpeed = 5.5f;
        [Tooltip("Tag party sprint. Apex ref ~7.59; punchier for giant arenas.")]
        public float sprintSpeed = 9.0f;

        [Header("Sprint")]
        [Tooltip("If true, full stick / WASD auto-sprints (party default). Light analog stick still walks.")]
        public bool autoSprint = true;
        [Tooltip("Move magnitude (0–1) at or above this counts as sprint when autoSprint is on.")]
        [Range(0.1f, 1f)] public float autoSprintThreshold = 0.55f;

        [Header("Acceleration (seconds to full)")]
        public float accelTime = 0.12f;
        public float brakeTime = 0.10f;

        [Header("Turn rates (deg/s)")]
        public float turnRateWalk = 540f;
        public float turnRateSprint = 420f;

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

        [Header("Slide (Apex-style: crouch while fast)")]
        [Tooltip("If true, holding crouch/slide while at or above the speed gate starts a slide.")]
        public bool slideFromSpeed = true;
        [Tooltip("Minimum planar speed (m/s) to start a slide. Between walk and sprint so you must be running.")]
        public float slideSpeedGate = 6.5f;
        [Tooltip("Planar speed punched to on slide enter when slideBoostToPeak is on. Apex ref ~11.45.")]
        public float slidePeakSpeed = 12.0f;
        [Tooltip("If true, slide enter uses max(current, peak) so a sprint always reads as a fast slide.")]
        public bool slideBoostToPeak = true;
        public float slideDuration = 0.70f;
        public float slidePunchDuration = 0.15f;
        [Range(0f, 1f)] public float slideEndSpeedPercent = 0.55f;
        public float slideHeight = 0.9f;
        public float standHeight = 1.8f;
        public float slideJumpHorizBonus = 0.12f;
        public float slideCooldown = 0.080f;
        [Tooltip("If true, snap planar speed to slideSpeedGate on enter. Party default: false (boost-to-peak instead).")]
        public bool slideEnterWipe = false;

        [Header("Air dodge (universal short dash)")]
        [Tooltip("Charges available while airborne (recharge on land).")]
        public int airDodgeCharges = 1;
        [Tooltip("Planar burst speed on air dodge (m/s). Party target 14–16.")]
        public float airDodgeSpeed = 15.0f;
        [Tooltip("No air control during lock (seconds). Party target 0.12–0.18.")]
        public float airDodgeLock = 0.150f;
        [Tooltip("I-frames vs punch hurtbox only (seconds). Party target 0.10–0.15.")]
        public float airDodgeIFrames = 0.120f;
        [Tooltip("Input buffer for air dodge (seconds).")]
        public float airDodgeBuffer = 0.080f;
        [Tooltip("If true, landing restores all air-dodge charges immediately (party default).")]
        public bool airDodgeRefreshOnLand = true;
        [Tooltip("Grounded footfalls required to recharge when refresh-on-land is off.")]
        public int airDodgeRechargeSteps = 3;
        [Tooltip("Grounded travel (m) to recharge one charge when refresh-on-land is off.")]
        public float airDodgeRechargeTravel = 1.8f;
        [Tooltip("Soft clamp: if speed × lock would exceed this distance (m), scale speed down. 0 = off.")]
        public float airDodgeMaxDistance = 2.5f;

        [Header("Camera")]
        [Tooltip("Third-person boom so the dummy (and slide/dash) is readable. Off = eye-height pivot.")]
        public bool thirdPerson = true;
        public Vector3 thirdPersonOffset = new Vector3(0f, 2.1f, -5.8f);
        public float thirdPersonLookAtHeight = 1.35f;
        public float thirdPersonProbeRadius = 0.22f;
        public float thirdPersonMinDistance = 0.45f;

        [Header("Debug")]
        [Tooltip("Show on-screen m/s + state HUD (toggle F3 in play).")]
        public bool showDebugHud = true;

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

        [Header("Slopes (CC probe — PARK 20° ramps)")]
        [Tooltip("Walkable slope degrees. PARK ramps are ~20°. Steeper = slide down.")]
        public float slopeLimit = 45f;
        [Tooltip("SphereCast extra distance below the capsule (m). Helps CC.isGrounded flicker on ramps.")]
        public float slopeProbeExtra = 0.28f;
        [Tooltip("Extra downward speed on walkable slopes so sprinting up a ramp does not bunny-hop.")]
        public float slopeStickSpeed = 14f;
        [Tooltip("Planar slide speed when the surface is steeper than slopeLimit.")]
        public float steepSlopeSlideSpeed = 8f;
        [Tooltip("Layers the ground probe hits. Default everything.")]
        public LayerMask groundMask = ~0;

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
