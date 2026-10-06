using Tag.Level;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Chase stick for DummyRunner. The motor already owns the verbs.
    /// This only picks a facing and a body-space stick. It does not move the body,
    /// write a velocity, or raise a feel number.
    /// A gap jump commits only when the measured span fits inside horizontal speed
    /// times the jump hang, plus a small margin. Otherwise the chase clings, brakes,
    /// or turns onto a side route. A launch pad is a known edge: step on it only
    /// when the predicted landing is closer to the target. A zip line is the same kind of edge:
    /// take it only when the exit is closer to the target. Sprint, air strafe, and cling stay the default.
    /// </summary>
    public enum OpponentChaseVerb
    {
        Close = 0,
        Sprint = 1,
        AirStrafe = 2,
        GapJump = 3,
        WallCling = 4,
        Lunge = 5,
        PadTake = 6,
        ZipTake = 7
    }

    public struct OpponentChaseInput
    {
        public Vector3 Aim;
        public Vector3 Velocity;
        public Vector3 BodyForward;
        public Vector3 WallNormal;
        public float WallDistance;
        public bool Grounded;
        public bool GapAhead;
        public bool LungeCommit;
        public bool HoldLine;
        public float PlanarDistance;
        public float FarMeters;
        /// <summary>Open floor between the near lip and the next landing. 0 when the probe found no gap.</summary>
        public float GapSpan;
        /// <summary>Meters from the pawn to the near lip. Large when no lip was measured.</summary>
        public float LipDistance;
        /// <summary>How far the current horizontal speed carries a jump. The motor still writes the impulse.</summary>
        public float JumpReachMeters;
        /// <summary>Same hang time at sprint speed, or at the current speed when that is already faster.</summary>
        public float RunUpReachMeters;
        /// <summary>Signed side route when a gap will not clear. Positive is to the pawn's right. 0 is none.</summary>
        public float PathStrafe;
        /// <summary>Ground decel used only to decide when to brake. 0 leaves the brake to the lip window.</summary>
        public float GroundDecel;
        /// <summary>Target is over a void, the ledge will not clear, or the tell would leave the pawn in the air.</summary>
        public bool LungeBlocked;
        /// <summary>The motor left this face. Do not steer a cling back onto it.</summary>
        public bool SameWallClosed;
        /// <summary>A launch pad lies on the chase line.</summary>
        public bool PadAhead;
        /// <summary>Meters from the pawn to that pad.</summary>
        public float PadDistance;
        /// <summary>The pad's landing is closer to the target than staying off it.</summary>
        public bool PadHelps;
        /// <summary>Flat direction from the pawn onto the pad.</summary>
        public Vector3 PadAim;
        /// <summary>A zip line lies on the chase line, or the pawn is already under one.</summary>
        public bool ZipAhead;
        /// <summary>Meters from the pawn to that line.</summary>
        public float ZipDistance;
        /// <summary>The exit is closer to the target than staying off the line.</summary>
        public bool ZipHelps;
        /// <summary>Flat direction onto the entry, or along the cable when already under it.</summary>
        public Vector3 ZipAim;
    }

    public struct OpponentChaseWish
    {
        public Vector3 Face;
        public float MoveY;
        public float Strafe;
        public bool Sprint;
        public bool Jump;
        public bool Lunge;
        public bool AirDash;
        public OpponentChaseVerb Verb;
    }

    public static class OpponentChaseSteer
    {
        /// <summary>Outside the lunge band the chase sprints. Inside it, walk, so the 0.45 s tell can finish.</summary>
        public const float FarSprintMeters = 6f;
        /// <summary>Motor cling probe is about a meter. Commit before the capsule touches.</summary>
        public const float ClingCommitMeters = 2.2f;
        /// <summary>Same gate as PlayerMotor.ClingIntoWall. Jump is not cling.</summary>
        public const float ClingIntoWall = 0.25f;
        /// <summary>cos(20°). A tighter heading is a straight run, not a corner.</summary>
        public const float CornerDot = 0.9397f;
        /// <summary>Side stick the motor treats as air strafe: Move.x set and |Move.y| under 0.2.</summary>
        public const float AirStrafeMoveY = 0.12f;
        public const float AirStrafeBonusMaxY = 0.2f;
        /// <summary>Walk stick. GaitCap sprints when moveY is above 0.4, so close chase stays under that.</summary>
        public const float CloseMoveY = 0.35f;
        public const float SprintMoveY = 1f;
        /// <summary>Matches OpponentLungeTell.LeadSeconds. The press is illegal before this.</summary>
        public const float LungeLeadSeconds = 0.45f;
        /// <summary>Meters of landing past the measured span. A jump that only just meets the lip does not commit.</summary>
        public const float GapClearMargin = 0.40f;
        /// <summary>Commit the jump only this close to the near lip, so the arc leaves from the edge.</summary>
        public const float GapCommitLip = 1.25f;
        /// <summary>Narrower than this is a crack. The chase does not jump it.</summary>
        public const float GapMinSpan = 0.55f;
        /// <summary>Extra meters in front of the stopping distance. Braking starts before the lip.</summary>
        public const float BrakeMargin = 0.35f;
        /// <summary>Yaw applied when a side of the gap still has floor.</summary>
        public const float PathAroundDegrees = 55f;
        /// <summary>A tell or a lunge that ends this close to a missing landing is a strand.</summary>
        public const float LungeStrandMargin = 0.35f;
        /// <summary>FaceAndSteer cap. A corner is a turn, not a snap.</summary>
        public const float MaxYawDegPerSec = 150f;
        /// <summary>cos(52°). Steeper than walkable + 4, which is what the motor calls a wall.</summary>
        public const float WallUpDotMax = 0.6157f;
        const float AirStrafeMinSpeedSqr = 9f;
        const float FaceOnClingDot = 0.82f;

        public static bool IsWallNormal(Vector3 normal)
        {
            float mag = normal.magnitude;
            if (mag < 1e-5f) return false;
            return Mathf.Abs(normal.y / mag) <= WallUpDotMax;
        }

        /// <summary>
        /// True only after the tell has been up for the full lead, on the ground, in the window, and not winding a punch.
        /// Airborne is refused: that press is an air dash, and chase does not take it.
        /// </summary>
        public static bool LungePressLegal(float secondsSinceTellStart, bool tellStarted, bool grounded, bool inWindow, bool busy)
        {
            if (!tellStarted || !grounded || !inWindow || busy)
                return false;
            return secondsSinceTellStart + 0.0001f >= LungeLeadSeconds;
        }

        /// <summary>Time from the jump impulse back to the same height. Apex does not depend on horizontal speed.</summary>
        public static float JumpHangSeconds(float verticalImpulse, float gravity, float fallGravityMult)
        {
            if (verticalImpulse <= 0.01f || gravity <= 0.01f)
                return 0f;
            float fall = fallGravityMult < 0.01f ? 1f : fallGravityMult;
            float tUp = verticalImpulse / gravity;
            float height = verticalImpulse * tUp * 0.5f;
            float tDown = Mathf.Sqrt(Mathf.Max(0f, 2f * height / (gravity * fall)));
            return tUp + tDown;
        }

        public static float JumpApexMeters(float verticalImpulse, float gravity)
        {
            if (verticalImpulse <= 0.01f || gravity <= 0.01f)
                return 0f;
            return verticalImpulse * verticalImpulse / (2f * gravity);
        }

        /// <summary>Horizontal speed carried through the jump hang. This is the jump reach.</summary>
        public static float JumpReachMeters(float horizontalSpeed, float verticalImpulse, float gravity, float fallGravityMult)
        {
            float speed = horizontalSpeed > 0f ? horizontalSpeed : 0f;
            return speed * JumpHangSeconds(verticalImpulse, gravity, fallGravityMult);
        }

        public static float SpeedToward(Vector3 velocity, Vector3 aim)
        {
            Vector3 v = Flat(velocity);
            Vector3 a = Flat(aim);
            if (a.sqrMagnitude < 1e-6f)
                return v.magnitude;
            a.Normalize();
            float toward = Vector3.Dot(v, a);
            return toward > 0f ? toward : 0f;
        }

        public static bool ClearsGap(float reachMeters, float gapSpan)
        {
            if (gapSpan <= GapMinSpan)
                return false;
            return reachMeters + 0.0001f >= gapSpan + GapClearMargin;
        }

        public static float StopMeters(float speed, float decel)
        {
            float v = speed > 0f ? speed : 0f;
            float a = decel > 0.01f ? decel : 0.01f;
            return v * v / (2f * a);
        }

        /// <summary>
        /// True when the pawn is still moving toward a lip it should not jump, or is already in the commit window.
        /// </summary>
        public static bool ShouldBrakeForGap(float speedToward, float decel, float lipDistance)
        {
            if (lipDistance <= GapCommitLip)
                return true;
            if (decel <= 0.01f)
                return false;
            return lipDistance <= StopMeters(speedToward, decel) + BrakeMargin;
        }

        /// <summary>
        /// The motor's lunge burst, read so the chase can predict a landing.
        /// The speed, duration, and cooldown stay on the config.
        /// </summary>
        public static void ReadLungeBurst(MovementConfig cfg, out float speed, out float duration)
        {
            speed = 0f;
            duration = 0f;
            if (cfg == null)
                return;
            speed = cfg.taggerLungeSpeed;
            duration = cfg.taggerLungeDuration;
        }

        /// <summary>Approach distance across the lunge tell while the gait cap is the walk.</summary>
        public static float TellApproachMeters(float speed, float walkCap, float accel, float decel, float tellSeconds)
        {
            float v = speed > 0f ? speed : 0f;
            float cap = walkCap > 0f ? walkCap : 0f;
            float t = tellSeconds > 0f ? tellSeconds : 0f;
            float dist = 0f;
            if (v > cap && decel > 0.01f)
            {
                float brakeT = (v - cap) / decel;
                if (brakeT >= t)
                    return Mathf.Max(0f, v * t - 0.5f * decel * t * t);
                dist += (v + cap) * 0.5f * brakeT;
                t -= brakeT;
                v = cap;
            }
            else if (v < cap && accel > 0.01f)
            {
                float accelT = (cap - v) / accel;
                if (accelT >= t)
                    return Mathf.Max(0f, v * t + 0.5f * accel * t * t);
                dist += (v + cap) * 0.5f * accelT;
                t -= accelT;
                v = cap;
            }
            dist += v * t;
            return Mathf.Max(0f, dist);
        }

        /// <summary>
        /// The tell or the burst would leave the lip and the next floor is not under that path.
        /// No measured lip (negative or very large) stays on the deck.
        /// </summary>
        public static bool LungeStrands(
            float speed,
            float walkCap,
            float accel,
            float decel,
            float tellSeconds,
            float lungeSpeed,
            float lungeDuration,
            float ledgeMeters,
            float landingMeters)
        {
            if (ledgeMeters < 0f || ledgeMeters >= 80f)
                return false;
            float tell = TellApproachMeters(speed, walkCap, accel, decel, tellSeconds);
            float burst = (lungeSpeed > 0f ? lungeSpeed : 0f) * (lungeDuration > 0f ? lungeDuration : 0f);
            float margin = LungeStrandMargin;
            if (tell + margin > ledgeMeters && (landingMeters < 0f || landingMeters > tell + margin))
                return true;
            float end = tell + burst;
            if (end + margin > ledgeMeters && (landingMeters < 0f || landingMeters > end + margin))
                return true;
            return false;
        }

        public static bool LedgeBeyondReach(float gapSpan, float jumpReach)
        {
            return gapSpan > GapMinSpan && !ClearsGap(jumpReach, gapSpan);
        }

        public static bool LungeAllowed(bool targetOverVoid, bool ledgeBeyondReach, bool tellStrands)
        {
            return !targetOverVoid && !ledgeBeyondReach && !tellStrands;
        }

        public static Vector3 YawOffset(Vector3 forward, float degrees)
        {
            Vector3 v = Flat(forward);
            if (v.sqrMagnitude < 1e-6f)
                v = new Vector3(0f, 0f, 1f);
            v.Normalize();
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector3(v.x * c + v.z * s, 0f, -v.x * s + v.z * c);
        }

        public static OpponentChaseWish Decide(OpponentChaseInput s)
        {
            float far = s.FarMeters > 0.5f ? s.FarMeters : FarSprintMeters;
            bool sprintRange = s.PlanarDistance > far;
            Vector3 aim = Flat(s.Aim);
            if (aim.sqrMagnitude < 1e-6f)
                aim = Flat(s.BodyForward);
            if (aim.sqrMagnitude < 1e-6f)
                aim = new Vector3(0f, 0f, 1f);
            aim.Normalize();

            bool wallClose = IsWallNormal(s.WallNormal) && s.WallDistance <= ClingCommitMeters && s.WallDistance >= 0f;
            // Ground only. An airborne lunge press is the air dash, and chase does not take that.
            if (s.LungeCommit && s.Grounded && !wallClose && !s.LungeBlocked)
                return Make(OpponentChaseVerb.Lunge, aim, SprintMoveY, 0f, false, false, true);

            if (wallClose && s.SameWallClosed)
            {
                Vector3 along = AlongWall(s.WallNormal, aim);
                // Stick is body space. Build it from the facing they have now, or the
                // turn toward the tangent still pushes into the closed face.
                BodyStick(s.BodyForward, along, out float alongY, out float alongX);
                return Make(sprintRange ? OpponentChaseVerb.Sprint : OpponentChaseVerb.Close,
                    along, alongY, alongX, sprintRange, false, false);
            }

            if (wallClose)
            {
                Vector3 clingFace = ClingDirection(s.WallNormal, aim);
                return Make(OpponentChaseVerb.WallCling, clingFace, SprintMoveY, 0f, sprintRange, false, false);
            }

            if (s.HoldLine)
                return Make(s.PlanarDistance > far ? OpponentChaseVerb.Sprint : OpponentChaseVerb.Close,
                    aim, sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, false, false);

            // A helping pad wins on the ground. A pad that does not help does not hide a zip that does.
            VerbIntegration.ChaseEdge edge = VerbIntegration.Choose(
                true, s.Grounded, s.PadAhead, s.PadHelps, s.ZipAhead, s.ZipHelps);
            if (edge == VerbIntegration.ChaseEdge.Pad)
            {
                Vector3 onto = Flat(s.PadAim);
                if (onto.sqrMagnitude < 1e-6f)
                    onto = aim;
                else
                    onto.Normalize();
                return Make(OpponentChaseVerb.PadTake, onto, sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, false, false);
            }

            if (edge == VerbIntegration.ChaseEdge.Zip)
            {
                Vector3 onto = Flat(s.ZipAim);
                if (onto.sqrMagnitude < 1e-6f)
                    onto = aim;
                else
                    onto.Normalize();
                return Make(OpponentChaseVerb.ZipTake, onto, sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, false, false);
            }

            if (s.Grounded && s.PadAhead)
            {
                float side = Mathf.Abs(s.PathStrafe) > 0.2f ? s.PathStrafe : 1f;
                float yaw = side > 0f ? PathAroundDegrees : -PathAroundDegrees;
                Vector3 peel = YawOffset(aim, yaw);
                return Make(sprintRange ? OpponentChaseVerb.Sprint : OpponentChaseVerb.Close, peel,
                    sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, false, false);
            }

            if (s.ZipAhead)
            {
                float zipSide = Mathf.Abs(s.PathStrafe) > 0.2f ? s.PathStrafe : 1f;
                float zipYaw = zipSide > 0f ? PathAroundDegrees : -PathAroundDegrees;
                Vector3 zipPeel = YawOffset(aim, zipYaw);
                return Make(sprintRange ? OpponentChaseVerb.Sprint : OpponentChaseVerb.Close, zipPeel,
                    sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, false, false);
            }

            if (s.Grounded && s.GapAhead && s.GapSpan > GapMinSpan)
            {
                bool clearNow = ClearsGap(s.JumpReachMeters, s.GapSpan);
                bool clearRun = ClearsGap(Mathf.Max(s.JumpReachMeters, s.RunUpReachMeters), s.GapSpan);
                bool atLip = s.LipDistance >= 0f && s.LipDistance <= GapCommitLip;
                if (clearNow && atLip)
                    return Make(OpponentChaseVerb.GapJump, aim, sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, true, false);

                // Far enough, and a faster run still lands: keep sprinting up to the lip.
                if (!(clearRun && !atLip))
                {
                    if (Mathf.Abs(s.PathStrafe) > 0.2f)
                    {
                        float yaw = s.PathStrafe > 0f ? PathAroundDegrees : -PathAroundDegrees;
                        Vector3 face = YawOffset(aim, yaw);
                        return Make(sprintRange ? OpponentChaseVerb.Sprint : OpponentChaseVerb.Close, face,
                            sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, false, false);
                    }

                    float toward = SpeedToward(s.Velocity, aim);
                    if (atLip || ShouldBrakeForGap(toward, s.GroundDecel, s.LipDistance))
                        return Make(OpponentChaseVerb.Close, aim, -CloseMoveY, 0f, false, false, false);
                    return Make(OpponentChaseVerb.Close, aim, 0f, 0f, false, false, false);
                }
            }

            if (TryAirStrafe(s, aim, sprintRange, out Vector3 strafeFace, out float moveY, out float strafe, out bool sprintHeld))
                return Make(OpponentChaseVerb.AirStrafe, strafeFace, moveY, strafe, sprintHeld, false, false);

            if (sprintRange)
                return Make(OpponentChaseVerb.Sprint, aim, SprintMoveY, 0f, true, false, false);

            return Make(OpponentChaseVerb.Close, aim, CloseMoveY, 0f, false, false, false);
        }

        /// <summary>Body-space stick that travels along <paramref name="worldDir"/>.</summary>
        public static void BodyStick(Vector3 bodyForward, Vector3 worldDir, out float moveY, out float strafe)
        {
            Vector3 body = Flat(bodyForward);
            Vector3 dir = Flat(worldDir);
            if (body.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f)
            {
                moveY = 1f;
                strafe = 0f;
                return;
            }
            body.Normalize();
            dir.Normalize();
            Vector3 right = new Vector3(body.z, 0f, -body.x);
            moveY = Vector3.Dot(body, dir);
            strafe = Vector3.Dot(right, dir);
        }

        /// <summary>Along the face. The wish is not into the wall, so the motor will not cling.</summary>
        public static Vector3 AlongWall(Vector3 wallNormal, Vector3 aim)
        {
            Vector3 n = Flat(wallNormal);
            if (n.sqrMagnitude < 1e-6f)
                n = new Vector3(0f, 0f, 1f);
            n.Normalize();
            Vector3 tangent = new Vector3(n.z, 0f, -n.x);
            Vector3 aimFlat = Flat(aim);
            if (aimFlat.sqrMagnitude > 1e-6f && Vector3.Dot(tangent, aimFlat) < 0f)
                tangent = new Vector3(-tangent.x, -tangent.y, -tangent.z);
            return tangent;
        }

        /// <summary>Into-wall wish. Face-on is climb. A glance keeps a tangent so a wall run has a direction.</summary>
        public static Vector3 ClingDirection(Vector3 wallNormal, Vector3 aim)
        {
            Vector3 into = Flat(new Vector3(-wallNormal.x, -wallNormal.y, -wallNormal.z));
            if (into.sqrMagnitude < 1e-6f)
                into = Flat(aim);
            if (into.sqrMagnitude < 1e-6f)
                return new Vector3(0f, 0f, 1f);
            into.Normalize();

            Vector3 aimFlat = Flat(aim);
            if (aimFlat.sqrMagnitude < 1e-6f)
                return into;
            aimFlat.Normalize();

            float faceDot = Vector3.Dot(aimFlat, into);
            if (faceDot >= FaceOnClingDot)
                return into;

            Vector3 tangent = new Vector3(into.z, 0f, -into.x);
            float along = Vector3.Dot(tangent, aimFlat) >= 0f ? 1f : -1f;
            Vector3 mixed = into * 0.55f + tangent * (along * 0.84f);
            if (mixed.sqrMagnitude < 1e-6f)
                return into;
            mixed.Normalize();
            return mixed;
        }

        public static float IntoWallDot(Vector3 worldWish, Vector3 wallNormal)
        {
            Vector3 wish = Flat(worldWish);
            Vector3 normal = Flat(wallNormal);
            if (wish.sqrMagnitude < 1e-6f || normal.sqrMagnitude < 1e-6f)
                return 0f;
            wish.Normalize();
            normal.Normalize();
            return Vector3.Dot(wish, new Vector3(-normal.x, -normal.y, -normal.z));
        }

        public static Vector3 WorldWish(Vector3 face, float moveY, float strafe)
        {
            Vector3 forward = Flat(face);
            if (forward.sqrMagnitude < 1e-6f)
                forward = new Vector3(0f, 0f, 1f);
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);
            return WishAccel.PlanarWish(forward, right, new Vector2(strafe, moveY));
        }

        static bool TryAirStrafe(OpponentChaseInput s, Vector3 aim, bool sprintRange, out Vector3 face, out float moveY, out float strafe, out bool sprintHeld)
        {
            face = aim;
            moveY = sprintRange ? SprintMoveY : CloseMoveY;
            strafe = 0f;
            sprintHeld = sprintRange;
            if (s.Grounded)
                return false;

            Vector3 vel = Flat(s.Velocity);
            if (vel.sqrMagnitude < AirStrafeMinSpeedSqr)
                return false;
            vel.Normalize();
            if (Vector3.Dot(vel, aim) >= CornerDot)
                return false;

            Vector3 body = Flat(s.BodyForward);
            if (body.sqrMagnitude < 1e-6f)
                body = vel;
            body.Normalize();
            Vector3 bodyRight = new Vector3(body.z, 0f, -body.x);

            Vector3 faceRight = new Vector3(-aim.z, 0f, aim.x);
            Vector3 faceLeft = new Vector3(aim.z, 0f, -aim.x);
            face = Vector3.Dot(body, faceRight) >= Vector3.Dot(body, faceLeft) ? faceRight : faceLeft;

            strafe = Vector3.Dot(bodyRight, aim);
            moveY = Vector3.Dot(body, aim);
            if (!sprintRange && moveY > CloseMoveY)
                moveY = CloseMoveY;
            sprintHeld = sprintRange;
            return true;
        }

        static OpponentChaseWish Make(OpponentChaseVerb verb, Vector3 face, float moveY, float strafe, bool sprint, bool jump, bool lunge)
        {
            OpponentChaseWish w;
            w.Face = face;
            w.MoveY = moveY;
            w.Strafe = strafe;
            w.Sprint = sprint;
            w.Jump = jump;
            w.Lunge = lunge;
            w.AirDash = false;
            w.Verb = verb;
            return w;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
