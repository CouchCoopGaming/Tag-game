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
        public float walkSpeed = 4.4f;
        public float sprintSpeed = 7.6f;
        public float crouchSpeed = 2.8f;
        public float groundAccel = 48f;
        public float groundDecel = 36f;
        public float slideEntrySpeed = 5.6f;

        [Header("Slide — Apex bloodline")]
        public float slideBoost = 3.8f;
        public float slideMinDuration = 0.26f;
        public float slideFlatFriction = 5.6f;
        public float slideDownhillAccel = 14f;
        public float slideUphillBrake = 18f;
        public float slideSteer = 22f;
        public float slideJumpWindow = 0.24f;
        public float slideJumpSpeedCap = 11.5f;
        public float slideHopRetain = 0.92f;

        [Header("Jump / fatigue — Apex bloodline")]
        public float jumpSpeed = 7.8f;
        public float jumpFatigueMin = 3.1f;
        public float jumpFatigueWindow = 0.75f;
        public float jumpFatigueFullAt = 0.15f;
        public float gravity = 22f;
        public float fallGravityMult = 1.50f;
        public float maxFallSpeed = 42f;
        public float landStunSpeed = 28f;
        public float landStunDuration = 0.20f;

        [Header("Air control — Quake/Apex lurch + tap-strafe analog")]
        public float airAccel = 28f;
        public float airSpeedCap = 7.6f;
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
        public float skiMaxSpeed = 38f;
        public float skiAirDrag = 0.08f;
        public float highSpeedSteerFalloff = 28f;

        [Header("Jet — Tribes bloodline")]
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
        public float climbMaxHeight = 6.0f;
        public float climbSpeed = 7.2f;
        public float climbAttachAngle = 55f;
        public float climbStickForce = 18f;
        public float climbSideSpeed = 3.4f;
        public float climbSlipSpeed = 2.2f;
        public float mantleMaxLedgeHeight = 2.35f;
        public float mantleMinLedgeHeight = 0.55f;
        public float mantleDuration = 0.42f;
        public float mantleForward = 0.85f;
        public float wallBounceSpeed = 9.2f;
        public float wallBounceUp = 7.0f;
        public float wallBounceGreenMin = 0.04f;
        public float wallBounceGreenMax = 0.22f;
        public float superGlideWindow = 0.055f;
        public float superGlideSpeed = 10.3f;

        [Header("Wall run — short arena parkour, not Titanfall infinite")]
        public bool enableWallRun = true;
        public float wallRunMaxTime = 0.85f;
        public float wallRunMinSpeed = 6.0f;
        public float wallRunSpeed = 9.5f;
        public float wallRunGravity = 4.5f;
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
