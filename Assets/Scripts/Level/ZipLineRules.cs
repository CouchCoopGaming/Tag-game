using System;
using System.IO;
using Tag.Art;
using Tag.Gameplay;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Kinematic zip-line ride. Speed along the cable is the line's ride speed.
    /// Entry speed is ignored. Cling is the existing move hold. Jump replaces
    /// vertical with jumpSpeed and keeps the ride's horizontal. A release or the
    /// end of the cable keeps the ride velocity. The same-wall ban is not cleared.
    /// </summary>
    public static class ZipLineRules
    {
        public const float DefaultRideSpeed = 14f;
        public const float DefaultRegrabCooldown = 0.3f;
        /// <summary>Capsule center sits this far under the cable while riding.</summary>
        public const float HangDrop = 1.15f;
        /// <summary>Horizontal reach of the grab volume around the cable.</summary>
        public const float GrabRadius = 0.85f;
        /// <summary>Extra meters below the hang the volume still counts as a grab.</summary>
        public const float GrabBelow = 1.15f;
        /// <summary>Above the cable, only a small band still counts.</summary>
        public const float GrabAbove = 0.45f;
        /// <summary>Flatter than this, point order is the ride. Otherwise the low end wins.</summary>
        public const float LevelEpsilon = 0.05f;
        /// <summary>Same gate as wall cling. A neutral stick is not a hold. Jump is not cling.</summary>
        public const float ClingHold = 0.25f;
        /// <summary>A landing this much closer counts as helping the chase.</summary>
        public const float HelpMarginMeters = 0.35f;

        public static bool ClingHeld(Vector2 move)
        {
            return move.sqrMagnitude > ClingHold * ClingHold;
        }

        public static bool CooldownOpen(float now, float readyAt)
        {
            return now + 0.0001f >= readyAt;
        }

        public static float ArmCooldown(float now, float cooldown)
        {
            float cd = cooldown > 0f ? cooldown : 0f;
            return now + cd;
        }

        /// <summary>Same line stays shut until the cooldown. A different line is open.</summary>
        public static bool RegrabBlocked(int lineId, int lastLineId, float now, float readyAt)
        {
            if (lineId == 0 || lineId != lastLineId) return false;
            return !CooldownOpen(now, readyAt);
        }

        /// <summary>
        /// Downhill when the ends differ in height. A level cable rides from A toward B.
        /// </summary>
        public static Vector3 RideDirection(Vector3 pointA, Vector3 pointB)
        {
            Vector3 aToB = pointB - pointA;
            if (aToB.sqrMagnitude < 1e-8f)
                return new Vector3(0f, 0f, 1f);
            if (pointB.y < pointA.y - LevelEpsilon)
                return aToB.normalized;
            if (pointA.y < pointB.y - LevelEpsilon)
                return (pointA - pointB).normalized;
            return aToB.normalized;
        }

        public static Vector3 ExitPoint(Vector3 pointA, Vector3 pointB)
        {
            Vector3 dir = RideDirection(pointA, pointB);
            Vector3 aToB = pointB - pointA;
            if (Vector3.Dot(aToB, dir) >= 0f)
                return pointB;
            return pointA;
        }

        public static Vector3 EntryPoint(Vector3 pointA, Vector3 pointB)
        {
            Vector3 exit = ExitPoint(pointA, pointB);
            if ((exit - pointB).sqrMagnitude < 1e-8f)
                return pointA;
            return pointB;
        }

        /// <summary>
        /// One velocity set along the cable. <paramref name="entry"/> is not added
        /// and does not scale the result.
        /// </summary>
        public static Vector3 RideVelocity(Vector3 entry, Vector3 direction, float rideSpeed)
        {
            if (direction.sqrMagnitude < 1e-8f || rideSpeed <= 0f)
                return new Vector3(0f, 0f, 0f);
            return direction.normalized * rideSpeed;
        }

        /// <summary>Releasing cling. The ride velocity is the fall. No jump is added.</summary>
        public static Vector3 ReleaseDrop(Vector3 rideVelocity)
        {
            return rideVelocity;
        }

        /// <summary>The end of the cable. Same carry as a cling release.</summary>
        public static Vector3 EndDrop(Vector3 rideVelocity)
        {
            return rideVelocity;
        }

        /// <summary>
        /// Jump off the hang. Vertical is jumpSpeed, not jumpSpeed plus the cable.
        /// Horizontal stays the ride.
        /// </summary>
        public static Vector3 JumpDrop(Vector3 rideVelocity, float jumpSpeed)
        {
            Vector3 v = rideVelocity;
            v.y = jumpSpeed;
            return v;
        }

        public static bool InGrabVolume(Vector3 pawn, Vector3 pointA, Vector3 pointB)
        {
            Vector3 ab = pointB - pointA;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-6f) return false;
            float t = Vector3.Dot(pawn - pointA, ab) / lenSq;
            if (t < -0.02f || t > 1.02f) return false;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            Vector3 on = pointA + ab * t;
            Vector3 delta = pawn - on;
            float lateral = new Vector3(delta.x, 0f, delta.z).magnitude;
            if (lateral > GrabRadius) return false;
            if (delta.y > GrabAbove) return false;
            if (-delta.y > HangDrop + GrabBelow) return false;
            return true;
        }

        public static float DistanceToSegment(Vector3 pawn, Vector3 pointA, Vector3 pointB)
        {
            Vector3 ab = pointB - pointA;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-6f) return (pawn - pointA).magnitude;
            float t = Vector3.Dot(pawn - pointA, ab) / lenSq;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            Vector3 on = pointA + ab * t;
            return (pawn - on).magnitude;
        }

        /// <summary>Meters from the entry toward the exit, measured on the cable axis.</summary>
        public static float AlongMeters(Vector3 pawn, Vector3 entry, Vector3 exit)
        {
            Vector3 span = exit - entry;
            float len = span.magnitude;
            if (len < 1e-4f) return 0f;
            return Vector3.Dot(pawn - entry, span) / len;
        }

        public static bool ReachedEnd(Vector3 pawn, Vector3 entry, Vector3 exit, float rideSpeed, float dt)
        {
            Vector3 span = exit - entry;
            float len = span.magnitude;
            if (len < 0.2f) return true;
            float remain = len - AlongMeters(pawn, entry, exit);
            float step = rideSpeed * (dt > 0f ? dt : 0.016f);
            return remain <= step + 0.12f;
        }

        public static bool ExitHelps(Vector3 pawn, Vector3 exit, Vector3 target)
        {
            float now = (target - pawn).magnitude;
            float after = (target - exit).magnitude;
            return after + HelpMarginMeters < now;
        }

        /// <summary>
        /// Along-cable speed stays on the ride. The lateral term only pulls the
        /// body onto the hang under the cable. Entry speed is not an argument.
        /// </summary>
        public static Vector3 HangVelocity(Vector3 pawn, Vector3 entry, Vector3 exit, float rideSpeed, float dt)
        {
            Vector3 span = exit - entry;
            Vector3 dir = RideDirection(entry, exit);
            Vector3 ride = RideVelocity(Vector3.zero, dir, rideSpeed);
            float len = span.magnitude;
            if (len < 1e-4f || dt < 1e-5f)
                return ride;
            float t = Vector3.Dot(pawn - entry, span) / (len * len);
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            float step = (rideSpeed * dt) / len;
            float tNext = t + step;
            if (tNext > 1f) tNext = 1f;
            Vector3 hangNext = entry + span * tNext - new Vector3(0f, HangDrop, 0f);
            Vector3 vel = (hangNext - pawn) * (1f / dt);
            float along = Vector3.Dot(vel, dir);
            Vector3 lateral = vel - dir * along;
            float lat = lateral.magnitude;
            if (lat > 24f)
                lateral = lateral * (24f / lat);
            return ride + lateral;
        }

        /// <summary>
        /// Discrete apex from a vertical set. The first step moves that speed.
        /// Later steps use rise gravity only. Horizontal is not an input, so a
        /// carried ride cannot add height.
        /// </summary>
        public static float SimulatedApex(float jumpSpeed, float gravity, float fallGravityMult, float maxFall, float dt)
        {
            float vy = jumpSpeed;
            float y = vy * dt;
            float peak = y;
            for (int i = 0; i < 800; i++)
            {
                if (vy <= 0f) break;
                float g = KinematicStep.AirGravity(vy, gravity, fallGravityMult);
                vy = KinematicStep.IntegrateVertical(vy, g, dt, maxFall);
                y += vy * dt;
                if (y > peak) peak = y;
            }
            return peak;
        }

        public static bool Holds()
        {
            if (!FeelLocks()) return false;
            if (!SpeedFixed()) return false;
            if (!DropsHold()) return false;
            if (!ApexMatchesGround()) return false;
            if (!EndCarries()) return false;
            if (!CooldownHolds()) return false;
            if (!DirectionHolds()) return false;
            if (!VolumeHolds()) return false;
            if (!DummyUsesLine()) return false;
            if (!PoseHangs()) return false;
            if (!DoesNotClearWall()) return false;
            if (!Wired()) return false;
            return true;
        }

        public static string ProofLine()
        {
            const float dt = 1f / 60f;
            const float g = 22f;
            const float fall = 1.5f;
            Vector3 dir = RideDirection(new Vector3(0f, 8f, 0f), new Vector3(0f, 5f, 12f));
            float s0 = RideVelocity(new Vector3(0f, 0f, 0f), dir, DefaultRideSpeed).magnitude;
            float s12 = RideVelocity(new Vector3(12f, 4f, 0f), dir, DefaultRideSpeed).magnitude;
            float s168 = RideVelocity(new Vector3(16.8f, 24.7f, 1f), dir, DefaultRideSpeed).magnitude;
            bool fixedSpeed = Mathf.Abs(s0 - DefaultRideSpeed) < 0.02f
                && Mathf.Abs(s0 - s12) < 0.001f
                && Mathf.Abs(s0 - s168) < 0.001f;
            Vector3 ride = RideVelocity(new Vector3(16.8f, 9f, 0f), dir, DefaultRideSpeed);
            Vector3 release = ReleaseDrop(ride);
            bool clingDrop = Mathf.Abs(release.x - ride.x) < 0.001f
                && Mathf.Abs(release.y - ride.y) < 0.001f
                && Mathf.Abs(release.z - ride.z) < 0.001f
                && Mathf.Abs(release.y - 24.7f) > 1f;
            float groundApex = SimulatedApex(24.7f, g, fall, 52f, dt);
            float jumpApex = SimulatedApex(JumpDrop(ride, 24.7f).y, g, fall, 52f, dt);
            bool apexMatch = Mathf.Abs(jumpApex - groundApex) < 0.001f;
            Vector3 end = EndDrop(ride);
            bool endCarry = Mathf.Abs(end.x - ride.x) < 0.001f
                && Mathf.Abs(end.y - ride.y) < 0.001f
                && Mathf.Abs(end.z - ride.z) < 0.001f;
            float ready = ArmCooldown(0f, DefaultRegrabCooldown);
            bool cd = CooldownOpen(0f, 0f)
                && !CooldownOpen(0.1f, ready)
                && CooldownOpen(0.3f, ready)
                && RegrabBlocked(4, 4, 0.1f, ready)
                && !RegrabBlocked(9, 4, 0.1f, ready);
            DummyFlags(out bool take, out bool skip);
            return "zip line"
                + " speed0=" + s0.ToString("0.00")
                + " speed12=" + s12.ToString("0.00")
                + " speed16.8=" + s168.ToString("0.00")
                + " fixed=" + (fixedSpeed ? "yes" : "no")
                + " clingDrop=" + (clingDrop ? "carry" : "no")
                + " jumpApex=" + jumpApex.ToString("0.00")
                + " groundApex=" + groundApex.ToString("0.00")
                + " apexMatch=" + (apexMatch ? "yes" : "no")
                + " endDrop=" + (endCarry ? "carry" : "no")
                + " regrab=" + (cd ? DefaultRegrabCooldown.ToString("0.00") : "no")
                + " dummyTake=" + (take ? "yes" : "no")
                + " dummySkip=" + (skip ? "yes" : "no");
        }

        static bool SpeedFixed()
        {
            Vector3 downhill = RideDirection(new Vector3(0f, 8f, 0f), new Vector3(0f, 5f, 12f));
            Vector3 e0 = new Vector3(0f, 0f, 0f);
            Vector3 e12 = new Vector3(12f, 3f, -4f);
            Vector3 e168 = new Vector3(16.8f, 24.7f, 2f);
            Vector3 v0 = RideVelocity(e0, downhill, DefaultRideSpeed);
            Vector3 v12 = RideVelocity(e12, downhill, DefaultRideSpeed);
            Vector3 v168 = RideVelocity(e168, downhill, DefaultRideSpeed);
            if (Mathf.Abs(v0.magnitude - DefaultRideSpeed) > 0.02f) return false;
            if (Mathf.Abs(v0.x - v12.x) > 0.001f || Mathf.Abs(v0.y - v12.y) > 0.001f || Mathf.Abs(v0.z - v12.z) > 0.001f)
                return false;
            if (Mathf.Abs(v0.x - v168.x) > 0.001f || Mathf.Abs(v0.y - v168.y) > 0.001f || Mathf.Abs(v0.z - v168.z) > 0.001f)
                return false;
            if (Mathf.Abs(v0.x - e168.x) < 0.5f && Mathf.Abs(v0.z - e168.z) < 0.5f) return false;
            if (v0.y >= -0.01f) return false;
            Vector3 flat = RideDirection(new Vector3(0f, 4f, 0f), new Vector3(0f, 4f, 10f));
            Vector3 level = RideVelocity(e168, flat, DefaultRideSpeed);
            if (Mathf.Abs(level.magnitude - DefaultRideSpeed) > 0.02f) return false;
            if (Mathf.Abs(level.y) > 0.02f) return false;
            return true;
        }

        static bool DropsHold()
        {
            Vector3 dir = RideDirection(new Vector3(0f, 6f, 0f), new Vector3(0f, 3f, 10f));
            Vector3 ride = RideVelocity(new Vector3(16.8f, 24.7f, 0f), dir, DefaultRideSpeed);
            Vector3 release = ReleaseDrop(ride);
            if (Mathf.Abs(release.x - ride.x) > 0.001f) return false;
            if (Mathf.Abs(release.y - ride.y) > 0.001f) return false;
            if (Mathf.Abs(release.z - ride.z) > 0.001f) return false;
            if (Mathf.Abs(release.y - 24.7f) < 1f) return false;
            if (!ClingHeld(new Vector2(0f, 1f))) return false;
            if (!ClingHeld(new Vector2(0.3f, 0f))) return false;
            if (ClingHeld(new Vector2(0f, 0f))) return false;
            if (ClingHeld(new Vector2(0.1f, 0.1f))) return false;
            return true;
        }

        static bool ApexMatchesGround()
        {
            const float dt = 1f / 60f;
            const float g = 22f;
            const float fall = 1.5f;
            Vector3 dir = RideDirection(new Vector3(0f, 9f, 0f), new Vector3(0f, 2f, 14f));
            float ground = SimulatedApex(24.7f, g, fall, 52f, dt);
            float[] entries = { 0f, 12f, 16.8f };
            for (int i = 0; i < entries.Length; i++)
            {
                Vector3 ride = RideVelocity(new Vector3(entries[i], entries[i], 0f), dir, DefaultRideSpeed);
                Vector3 jumped = JumpDrop(ride, 24.7f);
                if (Mathf.Abs(jumped.y - 24.7f) > 0.001f) return false;
                if (Mathf.Abs(jumped.x - ride.x) > 0.001f || Mathf.Abs(jumped.z - ride.z) > 0.001f) return false;
                if (Mathf.Abs(jumped.y - (24.7f + ride.y)) < 0.5f && ride.y < -0.2f) return false;
                float apex = SimulatedApex(jumped.y, g, fall, 52f, dt);
                if (Mathf.Abs(apex - ground) > 0.001f) return false;
            }
            float continuous = 24.7f * 24.7f / (2f * g);
            if (Mathf.Abs(ground - continuous) > 0.45f) return false;
            return true;
        }

        static bool EndCarries()
        {
            Vector3 entry = new Vector3(0f, 6f, 0f);
            Vector3 exit = new Vector3(0f, 4f, 18f);
            Vector3 dir = RideDirection(entry, exit);
            Vector3 ride = RideVelocity(new Vector3(12f, 1f, 0f), dir, DefaultRideSpeed);
            Vector3 dropped = EndDrop(ride);
            if (Mathf.Abs(dropped.x - ride.x) > 0.001f) return false;
            if (Mathf.Abs(dropped.y - ride.y) > 0.001f) return false;
            if (Mathf.Abs(dropped.z - ride.z) > 0.001f) return false;
            if (Mathf.Abs(dropped.magnitude) < 1f) return false;
            if (ReachedEnd(entry, entry, exit, DefaultRideSpeed, 1f / 60f)) return false;
            if (!ReachedEnd(exit, entry, exit, DefaultRideSpeed, 1f / 60f)) return false;
            Vector3 almost = exit - dir * 0.05f;
            if (!ReachedEnd(almost, entry, exit, DefaultRideSpeed, 1f / 60f)) return false;
            return true;
        }

        static bool CooldownHolds()
        {
            if (Mathf.Abs(DefaultRegrabCooldown - 0.3f) > 0.001f) return false;
            if (!CooldownOpen(1f, 1f)) return false;
            float ready = ArmCooldown(2f, DefaultRegrabCooldown);
            if (Mathf.Abs(ready - 2.3f) > 0.001f) return false;
            if (CooldownOpen(2.1f, ready)) return false;
            if (!CooldownOpen(2.3f, ready)) return false;
            if (!RegrabBlocked(3, 3, 2.1f, ready)) return false;
            if (RegrabBlocked(3, 3, 2.3f, ready)) return false;
            if (RegrabBlocked(8, 3, 2.1f, ready)) return false;
            return true;
        }

        static bool DirectionHolds()
        {
            Vector3 high = new Vector3(0f, 8f, 0f);
            Vector3 low = new Vector3(0f, 2f, 10f);
            Vector3 down = RideDirection(high, low);
            if (down.z <= 0.5f || down.y >= 0f) return false;
            Vector3 swapped = RideDirection(low, high);
            if (Vector3.Dot(swapped, down) < 0.99f) return false;
            Vector3 exit = ExitPoint(low, high);
            if (Mathf.Abs(exit.y - 2f) > 0.001f) return false;
            Vector3 level = RideDirection(new Vector3(0f, 3f, 0f), new Vector3(4f, 3f, 0f));
            if (level.x <= 0.5f) return false;
            if (Mathf.Abs(level.y) > 0.02f) return false;
            return true;
        }

        static bool VolumeHolds()
        {
            Vector3 a = new Vector3(0f, 4f, 0f);
            Vector3 b = new Vector3(0f, 4f, 10f);
            Vector3 hang = new Vector3(0f, 4f - HangDrop, 5f);
            if (!InGrabVolume(hang, a, b)) return false;
            Vector3 ledge = new Vector3(0f, 4f - HangDrop - 0.8f, 5f);
            if (!InGrabVolume(ledge, a, b)) return false;
            Vector3 far = new Vector3(3f, 4f - HangDrop, 5f);
            if (InGrabVolume(far, a, b)) return false;
            Vector3 above = new Vector3(0f, 4f + 1.2f, 5f);
            if (InGrabVolume(above, a, b)) return false;
            return true;
        }

        static bool DummyUsesLine()
        {
            DummyFlags(out bool take, out bool skip);
            if (!take || !skip) return false;
            Vector3 pawn = Vector3.zero;
            Vector3 exitSide = new Vector3(8f, 2f, 0f);
            Vector3 exitFar = new Vector3(0f, 2f, 20f);
            Vector3 targetNear = new Vector3(0f, 0f, 10f);
            Vector3 targetFar = new Vector3(0f, 0f, 30f);
            if (ExitHelps(pawn, exitSide, targetNear)) return false;
            if (!ExitHelps(pawn, exitFar, targetFar)) return false;
            if (ExitHelps(pawn, new Vector3(0f, 2f, -12f), targetFar)) return false;
            return true;
        }

        static void DummyFlags(out bool take, out bool skip)
        {
            take = false;
            skip = false;
            OpponentChaseInput go = Blank();
            go.ZipAhead = true;
            go.ZipHelps = true;
            go.ZipDistance = 4f;
            go.ZipAim = new Vector3(0f, 0f, 1f);
            go.PlanarDistance = 20f;
            OpponentChaseWish onto = OpponentChaseSteer.Decide(go);
            take = onto.Verb == OpponentChaseVerb.ZipTake && !onto.Jump && !onto.Lunge && !onto.AirDash
                && onto.MoveY > 0.2f;

            OpponentChaseInput air = go;
            air.Grounded = false;
            air.Velocity = new Vector3(0f, 2f, 0f);
            OpponentChaseWish airTake = OpponentChaseSteer.Decide(air);
            take = take && airTake.Verb == OpponentChaseVerb.ZipTake && !airTake.Jump;

            OpponentChaseInput around = go;
            around.ZipHelps = false;
            around.PathStrafe = 1f;
            OpponentChaseWish peel = OpponentChaseSteer.Decide(around);
            Vector3 face = peel.Face;
            face.y = 0f;
            Vector3 aim = new Vector3(0f, 0f, 1f);
            float dot = face.sqrMagnitude > 1e-6f ? Vector3.Dot(face.normalized, aim) : 1f;
            skip = peel.Verb != OpponentChaseVerb.ZipTake && !peel.Jump && !peel.Lunge && dot < 0.95f;

            OpponentChaseInput plain = Blank();
            plain.PlanarDistance = 12f;
            OpponentChaseWish sprint = OpponentChaseSteer.Decide(plain);
            if (sprint.Verb != OpponentChaseVerb.Sprint || sprint.Jump)
                take = false;
        }

        static OpponentChaseInput Blank()
        {
            OpponentChaseInput s = default;
            s.Aim = new Vector3(0f, 0f, 1f);
            s.BodyForward = new Vector3(0f, 0f, 1f);
            s.WallDistance = 999f;
            s.Grounded = true;
            s.PlanarDistance = 12f;
            s.FarMeters = 6f;
            s.LipDistance = 999f;
            return s;
        }

        static bool PoseHangs()
        {
            WallPose.Sample hang = WallPose.CableHang();
            if (Mathf.Abs(hang.ArmPitchL - WallPose.ReachPitch) > 0.05f) return false;
            if (Mathf.Abs(hang.ArmPitchR - WallPose.ReachPitch) > 0.05f) return false;
            if (hang.ArmPitchL > -90f || hang.ArmPitchR > -90f) return false;
            if (WallPose.RootMotion) return false;
            return true;
        }

        static bool DoesNotClearWall()
        {
            SameWallLimit.Face west = SameWallLimit.Make(7, new Vector3(1f, 0f, 0f), new Vector3(0f, 1.2f, 0f));
            SameWallLimit.Ban ban = default;
            SameWallLimit.NoteLeave(ref ban, west);
            if (!ban.Active || !SameWallLimit.Blocks(ban, west)) return false;
            // A ride does not call NoteLand. The ban is still the face just left.
            if (!SameWallLimit.Blocks(ban, west)) return false;
            SameWallLimit.NoteLand(ref ban);
            if (ban.Active) return false;
            return true;
        }

        static bool FeelLocks()
        {
            MovementConfig cfg = ScriptableObject.CreateInstance<MovementConfig>();
            if (cfg == null) return false;
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
            if (Mathf.Abs(cfg.gravity - 22f) > 0.001f) return false;
            PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
            if (punch == null || Mathf.Abs(punch.reach - 1.55f) > 0.001f) return false;
            return true;
        }

        static bool Wired()
        {
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            string line = Read("Assets/Scripts/Level/ZipLine.cs");
            string steer = Read("Assets/Scripts/Modes/OpponentChaseSteer.cs");
            string patrol = Read("Assets/Scripts/Modes/DummyPatrol.cs");
            string loco = Read("Assets/Scripts/Art/DummyLocomotor.cs");
            string pose = Read("Assets/Scripts/Art/WallPose.cs");
            if (motor == null || line == null || steer == null || patrol == null || loco == null || pose == null)
                return false;
            if (Count(motor, "_cc.Move(") != 1) return false;
            if (motor.IndexOf("animator.applyRootMotion = false", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("ApplyZipRide", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("TryBeginZip", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("ZipRiding", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("SetZipChase", StringComparison.Ordinal) < 0) return false;
            string apply = Method(motor, "Vector3 ApplyZipRide");
            if (apply.Length < 40) return false;
            if (apply.IndexOf("ClearWallBan", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("NoteLand", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("_cc.Move", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("AddForce", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("Rigidbody", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("jumpSpeed", StringComparison.Ordinal) < 0) return false;
            if (apply.IndexOf("JumpDrop", StringComparison.Ordinal) < 0) return false;
            if (apply.IndexOf("ReleaseDrop", StringComparison.Ordinal) < 0) return false;
            if (apply.IndexOf("EndDrop", StringComparison.Ordinal) < 0) return false;
            string begin = Method(motor, "bool BeginPunchStagger");
            if (begin.IndexOf("ReleaseZip", StringComparison.Ordinal) < 0) return false;
            if (begin.IndexOf("_velocity", StringComparison.Ordinal) >= 0) return false;
            if (begin.IndexOf("jumpSpeed", StringComparison.Ordinal) >= 0) return false;
            string gates = Method(motor, "void TickWallContactGates");
            if (gates.IndexOf("_zipRiding", StringComparison.Ordinal) < 0) return false;
            if (gates.IndexOf("ClearWallBan", StringComparison.Ordinal) < 0) return false;
            if (line.IndexOf("pointA", StringComparison.Ordinal) < 0) return false;
            if (line.IndexOf("pointB", StringComparison.Ordinal) < 0) return false;
            if (line.IndexOf("rideSpeed", StringComparison.Ordinal) < 0) return false;
            if (line.IndexOf("regrabCooldown", StringComparison.Ordinal) < 0) return false;
            if (line.IndexOf("isTrigger = true", StringComparison.Ordinal) < 0) return false;
            if (line.IndexOf("AddForce", StringComparison.Ordinal) >= 0) return false;
            if (line.IndexOf("Rigidbody", StringComparison.Ordinal) >= 0) return false;
            if (line.IndexOf("applyRootMotion", StringComparison.Ordinal) >= 0) return false;
            if (steer.IndexOf("ZipTake", StringComparison.Ordinal) < 0) return false;
            if (steer.IndexOf("ZipHelps", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("ZipLine.QueryChase", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("SetZipChase", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("ZipAhead", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("ZipHelps", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("ZipRiding", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("CableHang", StringComparison.Ordinal) < 0) return false;
            if (pose.IndexOf("CableHang", StringComparison.Ordinal) < 0) return false;
            if (pose.IndexOf("ReachPitch", StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static string Read(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        static int Count(string hay, string needle)
        {
            int n = 0;
            int i = 0;
            while (i >= 0 && i < hay.Length)
            {
                i = hay.IndexOf(needle, i, StringComparison.Ordinal);
                if (i < 0) break;
                n++;
                i += needle.Length;
            }
            return n;
        }

        static string Method(string source, string signature)
        {
            int start = source.IndexOf(signature, StringComparison.Ordinal);
            if (start < 0) return "";
            int brace = source.IndexOf('{', start);
            if (brace < 0) return "";
            int depth = 0;
            for (int i = brace; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}')
                {
                    depth--;
                    if (depth == 0) return source.Substring(brace, i - brace + 1);
                }
            }
            return "";
        }
    }
}
