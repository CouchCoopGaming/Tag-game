using System;
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

        /// <summary>No ceiling within one jump. A real gap is shorter than this.</summary>
        public const float OpenCeiling = 8f;

        /// <summary>Downward speed that keeps a planted capsule on the floor. Same write the gentle band already uses.</summary>
        public const float PlantStickVy = -2f;

        /// <summary>
        /// Shorten an upward move so it stops a skin short of the ceiling.
        /// Overlap eases out by at most one skin this frame. The full overlap is not returned.
        /// </summary>
        public static float CeilingKissRise(float requestedRise, float gap, float skin)
        {
            float safe = skin < 0.02f ? 0.02f : skin;
            if (requestedRise < 0f) requestedRise = 0f;
            if (gap >= OpenCeiling) return requestedRise;
            float room = gap - safe;
            if (requestedRise <= room) return requestedRise;
            if (room >= 0f) return room;
            float depth = -room;
            return depth < safe ? -depth : -safe;
        }

        /// <summary>A ceiling hit spends upward speed. Falling is left alone. No position write.</summary>
        public static float CeilingBlockedVy(float vy, bool ceilingHit)
        {
            if (ceilingHit && vy > 0f) return 0f;
            return vy;
        }

        /// <summary>Recover speed after a kiss. Downward ease stays under a launch.</summary>
        public static float CapKissVy(float vy)
        {
            const float maxDown = 1.2f;
            if (vy < -maxDown) return -maxDown;
            return vy;
        }

        /// <summary>
        /// A ceiling inside the step height would turn the rise into a downward snap.
        /// Open air keeps the stair step.
        /// </summary>
        public static bool CeilingSuppressStep(float gap, float skin, float stepOffset)
        {
            float safe = skin < 0.02f ? 0.02f : skin;
            float step = stepOffset > 0f ? stepOffset : 0f;
            return gap < step + safe;
        }

        /// <summary>
        /// A fall that is still moving into the floor becomes the plant stick.
        /// Speed along the slope (into the surface near zero) is kept. A launch is kept.
        /// </summary>
        public static float SteepLandStick(float vy, float intoSurface)
        {
            if (vy > LaunchVy) return vy;
            if (intoSurface < -0.35f && vy < -0.5f) return PlantStickVy;
            return vy;
        }

        /// <summary>
        /// Contact, a bury inside 0.20 m, and a drop the step can absorb stay planted.
        /// A lip inside the probe but past the step is a fall, so the stick does not hover there.
        /// The band between the step and a slightly higher leave line does not flip every frame.
        /// </summary>
        public static bool StepEdgePlant(float feetGap, float stepOffset, float groundProbe, bool wasPlanted)
        {
            const float bury = 0.2f;
            float step = stepOffset > 0f ? stepOffset : 0f;
            if (feetGap < -bury) return false;
            if (feetGap > groundProbe) return false;
            float grab = step;
            float leave = step + 0.04f;
            if (wasPlanted) return feetGap <= leave;
            return feetGap <= grab;
        }

        /// <summary>
        /// Buried past the skin, move up by at most one skin this frame and stay under launch speed.
        /// A jump already faster than that ease is kept. Inside the skin, the velocity is unchanged.
        /// </summary>
        public static float BuriedEaseVy(float vy, float feetGap, float skin, float dt)
        {
            float safe = skin < 0.02f ? 0.02f : skin;
            if (feetGap >= -safe) return vy;
            if (dt < 1e-6f) return vy;
            float depth = -feetGap - safe;
            float distance = depth < safe ? depth : safe;
            float up = distance / dt;
            const float maxUp = 1.2f;
            if (up > maxUp) up = maxUp;
            if (vy >= up) return vy;
            return up;
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

    /// <summary>Second controller pass. Feel locks are not inputs. One Move stays in the motor.</summary>
    public static class EnginePass2
    {
        public static bool Holds()
        {
            const float skin = 0.02f;
            const float step = 0.20f;
            const float probe = 0.28f;
            const float dt = 1f / 60f;

            float kiss = KinematicStep.CeilingKissRise(0.40f, 0.05f, skin);
            if (Mathf.Abs(kiss - 0.03f) > 0.001f) return false;
            float open = KinematicStep.CeilingKissRise(0.40f, KinematicStep.OpenCeiling, skin);
            if (Mathf.Abs(open - 0.40f) > 0.001f) return false;
            float overlap = KinematicStep.CeilingKissRise(0.40f, -0.20f, skin);
            if (Mathf.Abs(overlap - (-skin)) > 0.001f) return false;
            if (overlap <= -0.20f) return false;
            if (Mathf.Abs(KinematicStep.CeilingBlockedVy(24.7f, true)) > 0.001f) return false;
            if (Mathf.Abs(KinematicStep.CeilingBlockedVy(24.7f, false) - 24.7f) > 0.001f) return false;
            if (Mathf.Abs(KinematicStep.CeilingBlockedVy(-4f, true) - (-4f)) > 0.001f) return false;
            float hiHz = KinematicStep.CapKissVy(-skin / (1f / 144f));
            if (hiHz < -1.2f - 0.001f || hiHz > -1.2f + 0.001f) return false;
            if (!(Mathf.Abs(hiHz) < KinematicStep.LaunchVy)) return false;
            if (!KinematicStep.CeilingSuppressStep(0.10f, skin, step)) return false;
            if (KinematicStep.CeilingSuppressStep(KinematicStep.OpenCeiling, skin, step)) return false;
            if (KinematicStep.StepOffset(1.8f, 0.38f, skin, true) > 0.0001f) return false;
            if (Mathf.Abs(KinematicStep.StepOffset(1.8f, 0.38f, skin, false) - step) > 0.001f) return false;

            if (Mathf.Abs(KinematicStep.SteepLandStick(-30f, -22f) - KinematicStep.PlantStickVy) > 0.001f) return false;
            if (Mathf.Abs(KinematicStep.SteepLandStick(-7f, -0.05f) - (-7f)) > 0.001f) return false;
            if (Mathf.Abs(KinematicStep.SteepLandStick(24.7f, -5f) - 24.7f) > 0.001f) return false;
            if (Mathf.Abs(KinematicStep.SteepLandStick(-0.2f, -0.2f) - (-0.2f)) > 0.001f) return false;

            const float castRadius = 0.38f * 0.92f;
            if (!KinematicStep.ProbeGrounded(true, 0.25f, probe, castRadius, false)) return false;
            if (KinematicStep.StepEdgePlant(0.25f, step, probe, true)) return false;
            if (!KinematicStep.StepEdgePlant(0.10f, step, probe, true)) return false;
            if (!KinematicStep.StepEdgePlant(0.188f, step, probe, false)) return false;
            if (!KinematicStep.StepEdgePlant(0.22f, step, probe, true)) return false;
            if (KinematicStep.StepEdgePlant(0.22f, step, probe, false)) return false;
            if (!KinematicStep.StepEdgePlant(-0.08f, step, probe, true)) return false;
            if (!KinematicStep.NearFeet(-0.08f, 0f, probe, 0.2f)) return false;

            float eased = KinematicStep.BuriedEaseVy(-2f, -0.15f, skin, dt);
            if (Mathf.Abs(eased - 1.2f) > 0.001f) return false;
            float easedStep = eased * dt;
            if (easedStep > skin + 0.001f) return false;
            if (easedStep >= 0.15f - 0.05f) return false;
            if (Mathf.Abs(KinematicStep.BuriedEaseVy(-2f, -0.01f, skin, dt) - (-2f)) > 0.001f) return false;
            if (Mathf.Abs(KinematicStep.BuriedEaseVy(24.7f, -0.15f, skin, dt) - 24.7f) > 0.001f) return false;
            if (!(eased < KinematicStep.LaunchVy)) return false;
            return true;
        }

        public static string ProofLine()
        {
            const float skin = 0.02f;
            const float dt = 1f / 60f;
            float kiss = KinematicStep.CeilingKissRise(0.40f, 0.05f, skin);
            float open = KinematicStep.CeilingKissRise(0.40f, KinematicStep.OpenCeiling, skin);
            float overlap = KinematicStep.CeilingKissRise(0.40f, -0.20f, skin);
            float eased = KinematicStep.BuriedEaseVy(-2f, -0.15f, skin, dt);
            bool curb = KinematicStep.StepEdgePlant(0.25f, 0.20f, 0.28f, true);
            bool stair = KinematicStep.StepEdgePlant(0.10f, 0.20f, 0.28f, true);
            bool band = KinematicStep.StepEdgePlant(0.22f, 0.20f, 0.28f, true);
            bool regrab = KinematicStep.StepEdgePlant(0.22f, 0.20f, 0.28f, false);
            float stepHeld = KinematicStep.StepOffset(1.8f, 0.38f, skin, KinematicStep.CeilingSuppressStep(0.10f, skin, 0.20f));
            bool teleport = overlap <= -0.20f || eased * dt > skin + 0.001f;
            return "engine-pass-2"
                + " ceiling-kiss rise=" + kiss.ToString("0.000")
                + " open=" + open.ToString("0.000")
                + " overlap=" + overlap.ToString("0.000")
                + " blockedVy=" + KinematicStep.CeilingBlockedVy(24.7f, true).ToString("0.0")
                + " stepHeld=" + stepHeld.ToString("0.00")
                + " steep-land-stick fall=" + KinematicStep.SteepLandStick(-30f, -22f).ToString("0.0")
                + " slide=" + KinematicStep.SteepLandStick(-7f, -0.05f).ToString("0.0")
                + " jump=" + KinematicStep.SteepLandStick(24.7f, -5f).ToString("0.0")
                + " step-edge curb=" + (curb ? "plant" : "fall")
                + " stair=" + (stair ? "plant" : "fall")
                + " band=" + (band ? "hold" : "fall")
                + " regrab=" + (regrab ? "plant" : "no")
                + " buried-ease up=" + eased.ToString("0.00")
                + " step=" + (eased * dt).ToString("0.000")
                + " teleport=" + (teleport ? "yes" : "no");
        }

        public static bool Wired(string motor, string probe)
        {
            if (string.IsNullOrEmpty(motor) || string.IsNullOrEmpty(probe)) return false;
            if (Count(motor, "_cc.Move(") != 1) return false;
            if (motor.IndexOf("SteepLandStick", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("BuriedEaseVy", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("CeilingKissRise", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("CeilingBlockedVy", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("CeilingSuppressStep", StringComparison.Ordinal) < 0) return false;
            if (probe.IndexOf("StepEdgePlant", StringComparison.Ordinal) < 0) return false;
            if (probe.IndexOf("CeilingGap", StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static int Count(string text, string needle)
        {
            int n = 0;
            int i = 0;
            while (i >= 0 && i < text.Length)
            {
                i = text.IndexOf(needle, i, StringComparison.Ordinal);
                if (i < 0) break;
                n++;
                i += needle.Length;
            }
            return n;
        }
    }
}
