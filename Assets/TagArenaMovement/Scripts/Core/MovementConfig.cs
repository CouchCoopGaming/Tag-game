using UnityEngine;

namespace TagArena.Movement
{
    [CreateAssetMenu(menuName = "Tag Arena/Movement Config", fileName = "MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        [Header("Capsule")]
        public float standingHeight = 1.8f;
        public float crouchHeight = 1.05f;
        public float radius = 0.38f;
        public float skin = 0.02f;

        [Header("Grounding")]
        public float groundProbe = 0.28f;
        public float maxWalkableAngle = 48f;
        public float coyoteTime = 0.10f;
        public float jumpBuffer = 0.16f;
        public LayerMask groundMask = ~0;
        public LayerMask wallMask = ~0;

        [Header("Ground speeds (m/s)")]
        public float walkSpeed = 6.0f;
        public float sprintSpeed = 12.0f;
        public float crouchSpeed = 3.2f;
        public float groundAccel = 52f;
        public float groundDecel = 38f;
        public float slideEntrySpeed = 7.5f;

        [Header("Slide — Apex bloodline")]
        /// <summary>Legacy field; EnterSlide no longer adds impulse. Keep 0.</summary>
        public float slideBoost = 0f;
        /// <summary>Minimum planar speed to stay in slide while crouch is held.</summary>
        public float slideStaySpeed = 4.0f;
        /// <summary>Brief commit so crouch-edge noise does not cancel enter; releasing crouch after this exits.</summary>
        public float slideMinDuration = 0.12f;
        public float slideFlatFriction = 6.8f;
        public float slideDownhillAccel = 16f;
        public float slideUphillBrake = 18f;
        public float slideSteer = 22f;
        public float slideJumpWindow = 0.24f;
        public float slideJumpSpeedCap = 11.5f;
        public float slideHopRetain = 0.92f;

        [Header("Jump / fatigue — Apex bloodline")]
        /// <summary>~10x prior peak height at same gravity (v scales with sqrt(height)).</summary>
        public float jumpSpeed = 24.7f;
        public float jumpFatigueMin = 9.8f;
        public float jumpFatigueWindow = 0.75f;
        public float jumpFatigueFullAt = 0.15f;
        public float gravity = 22f;
        public float fallGravityMult = 1.50f;
        /// <summary>While airborne + crouch held, fall gravity is multiplied by this.</summary>
        public float airCrouchFallMult = 2.0f;
        public float maxFallSpeed = 52f;
        public float landStunSpeed = 28f;
        public float landStunDuration = 0.20f;

        [Header("Air control — Quake/Apex lurch + tap-strafe analog")]
        public float airAccel = 30f;
        public float airSpeedCap = 12.0f;
        public float airStrafeBonus = 1.35f;
        public float tapStrafeImpulse = 9.5f;
        public float tapStrafeCooldown = 0.08f;
        public bool enableTapStrafe = true;

        [Header("Ski — Tribes bloodline")]
        public float skiMinSlope = 6f;
        public float skiFriction = 0.15f;
        public float skiSteer = 19f;
        public float skiGravityScale = 1.12f;
        public float skiLaunchLeaveDot = 0.12f;
        public float skiMaxSpeed = 24f;
        public float skiAirDrag = 0.08f;
        public float highSpeedSteerFalloff = 28f;

        [Header("Jet — Tribes bloodline (OFF by default — not a core verb)")]
        public bool enableJet = false;
        public float jetEnergyMax = 100f;
        public float jetEnergyRegen = 22f;
        public float jetRegenDelay = 0.35f;
        public float jetDrain = 38f;
        public float jetUpForce = 26f;
        public float jetWishForce = 18f;
        public float jetHoverDamp = 8f;
        public float jetMinEnergy = 4f;
        public float gravityWhileJetting = 0.15f;

        [Header("Wall climb / mantle — Apex bloodline")]
        public float climbMaxHeight = 3.4f;
        /// <summary>Hard time cap — after this you slip down (no Spiderman stick).</summary>
        public float climbMaxTime = 0.85f;
        /// <summary>Climb up-speed begins decaying after this many seconds on the wall.</summary>
        public float climbDecayStart = 0.40f;
        // Slightly snappier vertical + glue for TP parkour (was 7.2 / 18 / 3.4).
        public float climbSpeed = 7.8f;
        public float climbAttachAngle = 55f;
        public float climbStickForce = 22f;
        public float climbSideSpeed = 3.7f;
        public float climbSlipSpeed = 3.4f;
        // Slightly taller / lower lips for mega-park rails + decks (was 2.35 / 0.55).
        public float mantleMaxLedgeHeight = 2.55f;
        public float mantleMinLedgeHeight = 0.45f;
        public float mantleDuration = 0.40f;
        // Extra settle onto thick Mega_ tops so TP does not hang on the lip.
        public float mantleForward = 0.95f;
        public float wallBounceSpeed = 9.2f;
        public float wallBounceUp = 7.0f;
        public float wallBounceGreenMin = 0.04f;
        public float wallBounceGreenMax = 0.22f;
        // Real seconds at mantle peak (bible). 0.10 party-fair; was 0.055 counted as u-fraction (~23ms).
        public float superGlideWindow = 0.10f;
        public float superGlideSpeed = 10.3f;

        [Header("Wall run — short arena parkour, not Titanfall infinite")]
        public bool enableWallRun = true;
        public float wallRunMaxTime = 0.62f;
        public float wallRunMinSpeed = 6.0f;
        public float wallRunSpeed = 9.5f;
        public float wallRunGravity = 6.5f;
        /// <summary>Extra gravity multiplier reached at wallRunMaxTime (slides you down).</summary>
        public float wallRunGravityEndMult = 3.2f;
        public float wallRunJumpOut = 8.0f;
        public float wallRunJumpUp = 6.2f;
        public float wallRunAttachAngle = 35f;

        [Header("Camera / feel")]
        public float fovIdle = 78f;
        public float fovSprint = 84f;
        public float fovSlide = 88f;
        public float fovSki = 92f;
        public float fovJet = 90f;
        public float tiltMax = 8f;
        public float bobAmp = 0.015f;

        [Header("Tag arena tuning")]
        public float runnerJetEnergyBonus = 20f;
        public float taggerSprintBonus = 0.55f;
        public float taggerLungeSpeed = 16f;
        public float taggerLungeDuration = 0.20f;
        public float taggerLungeCooldown = 1.0f;
        public float tagRadius = 1.15f;
    }
}
