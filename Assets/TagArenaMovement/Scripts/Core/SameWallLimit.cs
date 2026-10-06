using System;
using System.IO;
using Tag.Art;
using Tag.Gameplay;
using Tag.Modes;
using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// After a pawn leaves a wall, that face stays closed until the pawn lands
    /// or touches a different face. A flat run of colliders is one face when the
    /// normals agree. A corner, or a wall facing the other way, is a new face.
    /// Cling grace is not written here.
    /// </summary>
    public static class SameWallLimit
    {
        /// <summary>
        /// Normals within this angle are one face. A corner sits well past it.
        /// </summary>
        public const float NormalToleranceDeg = 35f;
        /// <summary>Seam slop. A recessed or parallel wall sits farther off the plane.</summary>
        public const float PlaneSlopMeters = 0.50f;

        public struct Face
        {
            public int ColliderId;
            public Vector3 Normal;
            public Vector3 Point;
            public bool Valid;
        }

        public struct Ban
        {
            public Face Left;
            public bool Active;
        }

        public struct Proof
        {
            public bool SameRefused;
            public bool DifferentAllowed;
            public bool LandResets;
            public bool ChainAllowed;
            public bool SeamSame;
            public bool CornerDifferent;
            public bool DummyRespects;
            public bool SlideOff;
            public bool GraceHeld;
        }

        public static Face Make(int colliderId, Vector3 normal, Vector3 point)
        {
            Face face;
            face.ColliderId = colliderId;
            face.Point = point;
            float mag = normal.magnitude;
            if (mag < 1e-5f)
            {
                face.Normal = Vector3.zero;
                face.Valid = false;
                return face;
            }
            face.Normal = normal * (1f / mag);
            face.Valid = true;
            return face;
        }

        /// <summary>
        /// Same collider and an agreeing normal are one face of that collider.
        /// Different colliders with agreeing normals are one face when the contacts
        /// share a plane, so a flat wall built from several boxes stays one surface.
        /// </summary>
        public static bool SameFace(Face a, Face b)
        {
            if (!a.Valid || !b.Valid) return false;
            if (!NormalsAgree(a.Normal, b.Normal)) return false;
            if (a.ColliderId != 0 && a.ColliderId == b.ColliderId) return true;
            float plane = Mathf.Abs(Vector3.Dot(b.Point - a.Point, a.Normal));
            return plane <= PlaneSlopMeters;
        }

        public static void NoteLeave(ref Ban ban, Face face)
        {
            if (!face.Valid) return;
            ban.Left = face;
            ban.Active = true;
        }

        public static void NoteLand(ref Ban ban)
        {
            ban = default;
        }

        /// <summary>A different face ends the ban. The same face keeps it.</summary>
        public static void NoteTouch(ref Ban ban, Face contact)
        {
            if (!ban.Active || !contact.Valid) return;
            if (!SameFace(ban.Left, contact))
                ban = default;
        }

        public static bool Blocks(Ban ban, Face contact)
        {
            return ban.Active && SameFace(ban.Left, contact);
        }

        public static bool Holds()
        {
            Proof proof = Evaluate();
            if (!proof.SameRefused || !proof.DifferentAllowed || !proof.LandResets || !proof.ChainAllowed)
                return false;
            if (!proof.SeamSame || !proof.CornerDifferent || !proof.DummyRespects || !proof.SlideOff)
                return false;
            if (!proof.GraceHeld) return false;
            if (!FeelLocks()) return false;
            if (!Wired()) return false;
            return true;
        }

        public static string ProofLine()
        {
            Proof proof = Evaluate();
            return "same-wall-limit"
                + " same wall " + (proof.SameRefused ? "refused" : "open")
                + ", different wall " + (proof.DifferentAllowed ? "allowed" : "refused")
                + ", land " + (proof.LandResets ? "resets" : "stuck")
                + ", facing-wall chain " + (proof.ChainAllowed ? "allowed" : "blocked")
                + ", seam=" + (proof.SeamSame ? "same" : "split")
                + ", corner=" + (proof.CornerDifferent ? "different" : "same")
                + ", grace=" + WallPose.ClingGraceSeconds.ToString("0.00")
                + ", slideOff=" + (proof.SlideOff ? "hands" : "grab")
                + ", dummy=" + (proof.DummyRespects ? "respects" : "reclings")
                + ", tol=" + NormalToleranceDeg.ToString("0")
                + ", rootMotion=0";
        }

        public static Proof Evaluate()
        {
            Proof proof = default;
            Face west = Make(7, new Vector3(1f, 0f, 0f), new Vector3(0f, 1.2f, 0f));
            // Fifteen degrees off +X, still on the same plane. A second box in a flat wall.
            Face seam = Make(8, new Vector3(0.9659f, 0f, 0.2588f), new Vector3(0.1f, 1.4f, 5f));
            Face corner = Make(7, new Vector3(0f, 0f, 1f), new Vector3(0.2f, 1.2f, 0.2f));
            Face east = Make(9, new Vector3(-1f, 0f, 0f), new Vector3(6f, 1.2f, 0f));
            Face offset = Make(10, new Vector3(1f, 0f, 0f), new Vector3(3f, 1.2f, 0f));

            proof.SeamSame = SameFace(west, seam);
            proof.CornerDifferent = !SameFace(west, corner) && !SameFace(west, east) && !SameFace(west, offset);

            Ban ban = default;
            NoteLeave(ref ban, west);
            proof.SameRefused = Blocks(ban, west) && Blocks(ban, seam);

            Ban open = ban;
            proof.DifferentAllowed = !Blocks(open, corner) && !Blocks(open, east) && !Blocks(open, offset);
            NoteTouch(ref open, corner);
            proof.DifferentAllowed = proof.DifferentAllowed && !open.Active && !Blocks(open, corner);

            Ban landed = ban;
            NoteLand(ref landed);
            proof.LandResets = !landed.Active && !Blocks(landed, west) && !Blocks(landed, seam);

            Ban chain = default;
            NoteLeave(ref chain, west);
            bool westClosed = Blocks(chain, west) && Blocks(chain, seam);
            NoteTouch(ref chain, east);
            bool eastTouched = !chain.Active && !Blocks(chain, east);
            NoteLeave(ref chain, east);
            bool backToWest = !Blocks(chain, west) && !Blocks(chain, seam);
            bool eastClosed = Blocks(chain, east);
            proof.ChainAllowed = westClosed && eastTouched && backToWest && eastClosed;

            OpponentChaseInput closed = default;
            closed.WallNormal = new Vector3(1f, 0f, 0f);
            closed.WallDistance = 1.1f;
            closed.Aim = new Vector3(-1f, 0f, 0.1f);
            closed.BodyForward = new Vector3(-1f, 0f, 0f);
            closed.SameWallClosed = true;
            closed.PlanarDistance = 12f;
            closed.Grounded = false;
            closed.Velocity = new Vector3(0f, 2f, 0f);
            OpponentChaseWish refused = OpponentChaseSteer.Decide(closed);
            float into = OpponentChaseSteer.IntoWallDot(
                OpponentChaseSteer.WorldWish(closed.BodyForward, refused.MoveY, refused.Strafe), closed.WallNormal);
            proof.DummyRespects = refused.Verb != OpponentChaseVerb.WallCling
                && !refused.Jump
                && !refused.Lunge
                && into <= OpponentChaseSteer.ClingIntoWall;

            OpponentChaseInput fresh = closed;
            fresh.SameWallClosed = false;
            OpponentChaseWish cling = OpponentChaseSteer.Decide(fresh);
            proof.DifferentAllowed = proof.DifferentAllowed && cling.Verb == OpponentChaseVerb.WallCling;

            WallPose.Sample grab = WallPose.Climb(1f, WallPose.ClimbSpeedRef);
            WallPose.Sample off = WallPose.SlideOff();
            float handGap = Mathf.Abs(off.ArmPitchL - off.ArmPitchR);
            proof.SlideOff = handGap < 8f
                && off.ArmPitchL > grab.ArmPitchL + 80f
                && off.Spine < grab.Spine - 20f
                && off.ArmPitchL > 0f
                && !WallPose.RootMotion;

            proof.GraceHeld = Mathf.Abs(WallPose.ClingGraceSeconds - 0.08f) < 0.001f;
            return proof;
        }

        static bool NormalsAgree(Vector3 a, Vector3 b)
        {
            float am = a.magnitude;
            float bm = b.magnitude;
            if (am < 1e-5f || bm < 1e-5f) return false;
            float dot = Vector3.Dot(a, b) / (am * bm);
            float limit = Mathf.Cos(NormalToleranceDeg * Mathf.Deg2Rad);
            return dot + 0.0001f >= limit;
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
            PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
            if (punch == null || Mathf.Abs(punch.reach - 1.55f) > 0.001f) return false;
            return true;
        }

        static bool Wired()
        {
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            string steer = Read("Assets/Scripts/Modes/OpponentChaseSteer.cs");
            string patrol = Read("Assets/Scripts/Modes/DummyPatrol.cs");
            string loco = Read("Assets/Scripts/Art/DummyLocomotor.cs");
            string pose = Read("Assets/Scripts/Art/WallPose.cs");
            if (motor == null || steer == null || patrol == null || loco == null || pose == null)
                return false;
            if (motor.IndexOf("BanLeftWall", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("SameWallLimit", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("ClingRefused", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("WouldRefuseCling", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("cfg.clingReleaseGrace", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("WallReattachDelay", StringComparison.Ordinal) >= 0) return false;
            if (Count(motor, "_cc.Move(") != 1) return false;
            string ban = Method(motor, "void BanLeftWall");
            if (ban.IndexOf("clingGrace", StringComparison.Ordinal) >= 0) return false;
            if (ban.IndexOf("jumpSpeed", StringComparison.Ordinal) >= 0) return false;
            if (ban.IndexOf("_cc.Move", StringComparison.Ordinal) >= 0) return false;
            if (steer.IndexOf("SameWallClosed", StringComparison.Ordinal) < 0) return false;
            if (steer.IndexOf("AlongWall", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("WouldRefuseCling", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("SameWallClosed", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("ClingRefused", StringComparison.Ordinal) < 0) return false;
            if (loco.IndexOf("WallPose.SlideOff", StringComparison.Ordinal) < 0) return false;
            if (pose.IndexOf("SlideOff", StringComparison.Ordinal) < 0) return false;
            if (pose.IndexOf("RootMotion = false", StringComparison.Ordinal) < 0) return false;
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
