using TagArena.Movement;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Chase stick for DummyRunner. The motor already owns the verbs.
    /// This only picks a facing and a body-space stick. It does not move the body,
    /// write a velocity, or raise a feel number.
    /// </summary>
    public enum OpponentChaseVerb
    {
        Close = 0,
        Sprint = 1,
        AirStrafe = 2,
        GapJump = 3,
        WallCling = 4,
        Lunge = 5
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
            if (s.LungeCommit && s.Grounded && !wallClose)
                return Make(OpponentChaseVerb.Lunge, aim, SprintMoveY, 0f, false, false, true);

            if (wallClose)
            {
                Vector3 clingFace = ClingDirection(s.WallNormal, aim);
                return Make(OpponentChaseVerb.WallCling, clingFace, SprintMoveY, 0f, sprintRange, false, false);
            }

            if (s.HoldLine)
                return Make(s.PlanarDistance > far ? OpponentChaseVerb.Sprint : OpponentChaseVerb.Close,
                    aim, sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, false, false);

            if (s.Grounded && s.GapAhead)
                return Make(OpponentChaseVerb.GapJump, aim, sprintRange ? SprintMoveY : CloseMoveY, 0f, sprintRange, true, false);

            if (TryAirStrafe(s, aim, sprintRange, out Vector3 strafeFace, out float moveY, out float strafe, out bool sprintHeld))
                return Make(OpponentChaseVerb.AirStrafe, strafeFace, moveY, strafe, sprintHeld, false, false);

            if (sprintRange)
                return Make(OpponentChaseVerb.Sprint, aim, SprintMoveY, 0f, true, false, false);

            return Make(OpponentChaseVerb.Close, aim, CloseMoveY, 0f, false, false, false);
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
