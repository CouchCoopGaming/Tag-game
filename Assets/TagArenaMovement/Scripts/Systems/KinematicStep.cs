using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Shared kinematic steps. Air speed is capped along the wish, not on the whole vector.
    /// Ground speed above the gait cap bleeds unless this frame jumps (bunny hop).
    /// </summary>
    public static class KinematicStep
    {
        public static float GaitCap(bool crouch, bool sprintHeld, float moveY, float crouchSpeed, float sprintSpeed, float walkSpeed)
        {
            if (crouch) return crouchSpeed;
            if (sprintHeld || moveY > 0.4f) return sprintSpeed;
            return walkSpeed;
        }

        /// <summary>
        /// Quake air accelerate. <paramref name="wishSpeed"/> limits the component along the wish.
        /// Turning the wish off the current velocity can raise total horizontal speed.
        /// <paramref name="accelPerSecond"/> is meters per second squared; it is multiplied by dt.
        /// </summary>
        public static Vector3 AirSteer(Vector3 hv, Vector3 wish, float wishSpeed, float accelPerSecond, float dt)
        {
            if (wish.sqrMagnitude <= 0.01f || wishSpeed <= 0f) return hv;
            float accel = accelPerSecond / Mathf.Max(wishSpeed, 1f);
            return WishAccel.Accelerate(hv, wish, wishSpeed, accel, dt);
        }

        /// <summary>
        /// Ground accelerate toward the gait cap. Speed already above that cap bleeds by
        /// <paramref name="groundDecel"/> unless <paramref name="hopSkipsFriction"/> (jump this frame).
        /// Under the cap this is the previous ground accelerate, including its angle add.
        /// </summary>
        public static Vector3 GroundSteer(Vector3 hv, Vector3 wish, float maxSpeed, float groundAccel, float groundDecel, float dt, bool hopSkipsFriction)
        {
            if (wish.sqrMagnitude > 0.01f)
            {
                bool over = hv.magnitude > maxSpeed;
                if (over && !hopSkipsFriction)
                    hv = WishAccel.Friction(hv, groundDecel / Mathf.Max(hv.magnitude, 1f), dt);
                float preserved = hv.magnitude;
                hv = WishAccel.Accelerate(hv, wish, maxSpeed, groundAccel / Mathf.Max(maxSpeed, 1f), dt);
                if (over && preserved > 0.0001f && hv.magnitude > preserved)
                    hv *= preserved / hv.magnitude;
                return hv;
            }

            return WishAccel.Friction(hv, groundDecel / Mathf.Max(hv.magnitude, 1f), dt);
        }

        public static float AirGravity(float vy, float gravity, float fallGravityMult)
        {
            return gravity * (vy < 0f ? fallGravityMult : 1f);
        }

        public static float IntegrateVertical(float vy, float gravityThisFrame, float dt, float fallCap)
        {
            vy -= gravityThisFrame * dt;
            if (vy < -fallCap) vy = -fallCap;
            return vy;
        }

        /// <summary>TickTimers order: decay before the jump check. A grounded frame refreshes after this.</summary>
        public static float DecayCoyote(float coyote, float dt)
        {
            if (coyote > 0f) coyote -= dt;
            return coyote;
        }

        public static bool CoyoteJumpAllowed(bool grounded, float coyote)
        {
            return grounded || coyote > 0f;
        }

        /// <summary>
        /// Above this, vertical speed is a launch. Ground stick uses the same gate.
        /// Coyote time is not an input.
        /// </summary>
        public const float LaunchVy = 1.5f;

        /// <summary>
        /// Grounded step stays at 0.2 m, and never above the radius or the skin.
        /// A launch or a wall contact uses 0. CharacterController treats an upward
        /// move that fits inside stepOffset as a step, then snaps the body back down.
        /// The same step also hops a capsule up a wall.
        /// </summary>
        public static float StepOffset(float height, float radius, float skin, bool suppress)
        {
            if (suppress) return 0f;
            float safeSkin = skin < 0f ? 0f : skin;
            float cap = height > 0f ? height : 0f;
            if (radius > 0f && cap > radius) cap = radius;
            float belowCap = height - radius - safeSkin;
            if (belowCap > safeSkin && cap > belowCap) cap = belowCap;
            const float desired = 0.2f;
            float step = desired;
            if (step > cap) step = cap;
            if (step < safeSkin) step = safeSkin;
            if (step < 0f) step = 0f;
            return step;
        }

        /// <summary>
        /// On the ground, walls are not floors. During climb, wall-run, and vault the
        /// motor's into-wall stick still needs the steep limit, or the controller rejects it.
        /// </summary>
        public static float SlopeLimit(float maxWalkableAngle, bool wallContact)
        {
            if (wallContact) return 90f;
            float angle = maxWalkableAngle;
            if (angle < 1f) angle = 1f;
            if (angle > 60f) angle = 60f;
            return angle;
        }

        /// <summary>
        /// SphereCast that starts inside the floor returns no hit. That is contact.
        /// The hit path keeps the old distance test. A far miss stays air.
        /// </summary>
        public static bool ProbeGrounded(bool castHit, float castDistance, float groundProbe, float radius, bool feetNear)
        {
            if (castHit)
                return castDistance <= groundProbe + radius * 0.15f;
            return feetNear;
        }

        /// <summary>Feet gap vs a walkable surface. Positive is floor below the origin. Negative is penetration.</summary>
        public static bool NearFeet(float feetY, float surfaceY, float groundProbe, float maxPenetration)
        {
            float gap = feetY - surfaceY;
            float pen = maxPenetration < 0f ? 0f : maxPenetration;
            return gap <= groundProbe && gap >= -pen;
        }

        /// <summary>
        /// One missed probe does not flap into air. A launch leaves immediately.
        /// The following miss is a real walk-off. Coyote is not refreshed here.
        /// </summary>
        public static bool StableGround(bool rawGrounded, bool previousRawGrounded, float verticalSpeed)
        {
            if (rawGrounded) return true;
            if (verticalSpeed > LaunchVy) return false;
            return previousRawGrounded;
        }

        /// <summary>
        /// One press for keyboard Space and the gamepad Jump button.
        /// spaceHeld is that key. jumpHeld includes it and the pad.
        /// A new Space hold still counts when the jump axis was already high, so the axis cannot swallow the key.
        /// The same hold reported again does not jump a second time.
        /// </summary>
        public static bool JumpEdge(bool jumpHeld, bool spaceHeld, bool prevJumpHeld, bool prevSpaceHeld)
        {
            bool spaceEdge = spaceHeld && !prevSpaceHeld;
            bool padEdge = jumpHeld && !prevJumpHeld;
            return spaceEdge || padEdge;
        }

        /// <summary>
        /// Ski, jet, and slide still cap the whole planar vector at ski max.
        /// Every other mode has no total-speed cap. Air speed is limited along the wish only.
        /// </summary>
        public static float LocomotionPlanarCap(float skiMaxSpeed, bool skiJetOrSlide)
        {
            return skiJetOrSlide ? skiMaxSpeed : float.PositiveInfinity;
        }

        /// <summary>
        /// Horizontal rope against a world hit. Vertical speed is not an input and not an output.
        /// Slack: the rope is taut once distance reaches the length latched at attach, minus slack.
        /// Outward planar speed is removed. The leftover is the tangent around that hit, so a
        /// different hit or a different incoming speed is a different path. A miss is not this
        /// function's problem: callers pass no rope, and no arc is invented here.
        /// Straight away from the hit, the horizontal result is zero. That cancels travel.
        /// </summary>
        public static Vector3 GrappleHorizontal(Vector3 horiz, Vector3 pawn, Vector3 anchor, float ropeLength, float slack)
        {
            horiz.y = 0f;
            if (ropeLength <= 0.05f) return horiz;

            Vector3 to = anchor - pawn;
            float dist = to.magnitude;
            float limit = ropeLength - (slack > 0f ? slack : 0f);
            if (limit < 0.05f) limit = 0.05f;
            if (dist < limit) return horiz;

            Vector3 planar = new Vector3(to.x, 0f, to.z);
            float planarDist = planar.magnitude;
            // Under the hit there is no tangent. Cancel horizontal travel.
            if (planarDist < 0.05f) return Vector3.zero;

            Vector3 inward = planar * (1f / planarDist);
            float outward = -Vector3.Dot(horiz, inward);
            if (outward <= 0f) return horiz;
            return horiz + inward * outward;
        }
    }

    /// <summary>Proofs for the controller, probe, and one-frame ground hold. Feel locks are not inputs.</summary>
    public static class MoveGrounding
    {
        public static bool StepEatsRise(float stepOffset, float rise)
        {
            return rise > 0f && rise <= stepOffset + 0.0001f;
        }

        public static bool Holds()
        {
            const float height = 1.8f;
            const float radius = 0.38f;
            const float skin = 0.02f;
            const float probeRadius = radius * 0.92f;
            float fatigueRise = 9.8f / 60f;
            float freshHiHz = 24.7f / 144f;
            if (!StepEatsRise(0.2f, fatigueRise)) return false;
            if (!StepEatsRise(0.2f, freshHiHz)) return false;
            float rising = KinematicStep.StepOffset(height, radius, skin, true);
            if (StepEatsRise(rising, fatigueRise) || StepEatsRise(rising, freshHiHz)) return false;
            float groundedStep = KinematicStep.StepOffset(height, radius, skin, false);
            if (Mathf.Abs(groundedStep - 0.2f) > 0.001f) return false;
            float crouchStep = KinematicStep.StepOffset(1.05f, radius, skin, false);
            if (Mathf.Abs(crouchStep - 0.2f) > 0.001f) return false;
            // A one-way height cap used to leave the step small after the capsule grew back.
            float stuck = 0.8f * 0.2f;
            if (!(groundedStep > stuck + 0.02f)) return false;

            float slope = KinematicStep.SlopeLimit(48f, false);
            if (Mathf.Abs(slope - 48f) > 0.001f) return false;
            if (!(90f > slope)) return false;
            if (Mathf.Abs(KinematicStep.SlopeLimit(48f, true) - 90f) > 0.001f) return false;
            if (KinematicStep.SlopeLimit(90f, false) > 60f) return false;
            if (KinematicStep.StepOffset(height, radius, skin, true) > 0.0001f) return false;

            if (!KinematicStep.ProbeGrounded(true, 0.07f, 0.28f, probeRadius, false)) return false;
            if (KinematicStep.ProbeGrounded(true, 0.50f, 0.28f, probeRadius, false)) return false;
            if (!KinematicStep.ProbeGrounded(false, 0f, 0.28f, probeRadius, true)) return false;
            if (KinematicStep.ProbeGrounded(false, 0f, 0.28f, probeRadius, false)) return false;
            if (!KinematicStep.NearFeet(0.02f, 0f, 0.28f, 0.2f)) return false;
            if (KinematicStep.NearFeet(1.2f, 0f, 0.28f, 0.2f)) return false;
            if (!KinematicStep.NearFeet(-0.08f, 0f, 0.28f, 0.2f)) return false;

            if (!KinematicStep.StableGround(false, true, -2f)) return false;
            if (KinematicStep.StableGround(false, true, 24.7f)) return false;
            if (KinematicStep.StableGround(false, false, -2f)) return false;
            if (!KinematicStep.StableGround(true, false, -2f)) return false;
            float coyote = KinematicStep.DecayCoyote(0.10f, 1f / 60f);
            if (Mathf.Abs(coyote - (0.10f - 1f / 60f)) > 0.0001f) return false;
            if (Mathf.Abs(KinematicStep.LaunchVy - 1.5f) > 0.001f) return false;
            return true;
        }

        public static string ProofLine()
        {
            float fatigueRise = 9.8f / 60f;
            float freshHiHz = 24.7f / 144f;
            float rising = KinematicStep.StepOffset(1.8f, 0.38f, 0.02f, true);
            float groundedStep = KinematicStep.StepOffset(1.8f, 0.38f, 0.02f, false);
            float coyote = KinematicStep.DecayCoyote(0.10f, 1f / 60f);
            return "controller step"
                + " grounded=" + groundedStep.ToString("0.00")
                + " rising=" + rising.ToString("0.00")
                + " fatigue60=" + fatigueRise.ToString("0.000")
                + " fresh144=" + freshHiHz.ToString("0.000")
                + " eats0.2=" + (StepEatsRise(0.2f, fatigueRise) ? "yes" : "no")
                + " eatsRising=" + (StepEatsRise(rising, fatigueRise) ? "yes" : "no")
                + " slopeGround=" + KinematicStep.SlopeLimit(48f, false).ToString("0")
                + " slopeWall=" + KinematicStep.SlopeLimit(48f, true).ToString("0")
                + " stepWall=" + KinematicStep.StepOffset(1.8f, 0.38f, 0.02f, true).ToString("0.00")
                + " wall90rejected=" + (90f > KinematicStep.SlopeLimit(48f, false) ? "yes" : "no")
                + "\nprobe overlap"
                + " nearMiss=ground"
                + " farMiss=air"
                + " cast0.07=ground"
                + " cast0.50=air"
                + "\nground flap"
                + " oneMiss=hold"
                + " launch=leave"
                + " secondMiss=air"
                + " coyoteAfter1f=" + coyote.ToString("0.0000")
                + " window=0.10";
        }
    }
}
