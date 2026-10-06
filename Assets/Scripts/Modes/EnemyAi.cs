using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Solo opponent brain for DummyRunner. It writes the same stick and buttons a player
    /// writes. It does not set a speed, teleport, or read the target's next input.
    /// Difficulty scales reaction delay, aim error, and how often optional verbs fire.
    /// Route and edge picks stick until a new one wins by a margin.
    /// </summary>
    public enum EnemyVerb
    {
        Hold = 0,
        Sprint = 1,
        Close = 2,
        Jump = 3,
        Cling = 4,
        WallRun = 5,
        WallJump = 6,
        Slide = 7,
        AirDash = 8,
        Grapple = 9,
        Pad = 10,
        Zip = 11,
        Lunge = 12,
        Punch = 13,
        Evade = 14,
        Loop = 15,
        Cover = 16
    }

    public struct EnemyMemory
    {
        public bool HasPercept;
        public int TargetId;
        public Vector3 PerceivedPos;
        public Vector3 PerceivedVel;
        public float ReactLeft;
        public int Sample;
        public float AimSign;
        public int RouteId;
        public float RouteHold;
        public float RouteScore;
        public int EdgeId;
        public float EdgeHold;
        public float EdgeScore;
        public int Flips;
        public float VerbClock;
        public float WallJumpSuppress;
        public float SlideHold;
        public float GrappleHold;
        public int Waypoint;
        public int CoverSide;
        public float CoverHold;
        public int StickTarget;
        public float StickTargetHold;
    }

    /// <summary>What the pawn can see this frame. Velocities are current, not future commands.</summary>
    public struct EnemySense
    {
        public float Difficulty;
        public float Dt;
        public bool IsIt;
        public Vector3 SelfPos;
        public Vector3 SelfVel;
        public Vector3 Forward;
        public bool Grounded;
        public bool Airborne;
        public float PlanarSpeed;
        public bool AirDashReady;
        public bool SameWallClosed;
        public bool OnWall;
        public float WallTime;
        public Vector3 WallNormal;
        public bool HasTarget;
        public int TargetId;
        public Vector3 TargetPos;
        public Vector3 TargetVel;
        public bool TargetTagBackBlocked;
        public float SelfTagBackRemaining;
        public bool LineOfSight;
        public bool ThreatClosing;
        public bool Cornered;
        public float PlanarDistance;
        public float PunchReach;
        public float AngleDeg;
        public bool GapAhead;
        public bool GapClear;
        public bool PadAhead;
        public bool PadHelps;
        public Vector3 PadAim;
        public bool ZipAhead;
        public bool ZipHelps;
        public Vector3 ZipAim;
        public bool GrappleLatch;
        public bool GrappleOutward;
        public bool GrappleProbe;
        public bool MegaPark;
        public Vector3 LoopAim;
        public Vector3 CoverAim;
        public float TargetHeightDelta;
        public float LeadBonus;
        public bool ForceVerbs;
        public bool SlideOpen;
        public int RouteCandidate;
        public float RouteCandidateScore;
        public float RouteHeldScore;
        public bool RouteHeldValid;
    }

    /// <summary>Player-equivalent output. The motor still owns the step.</summary>
    public struct EnemyOverlay
    {
        public Vector3 Face;
        public float MoveX;
        public float MoveY;
        public bool Sprint;
        public bool Jump;
        public bool Crouch;
        public bool Grapple;
        public bool Dash;
        public bool WallJump;
        public bool Punch;
        public bool Lunge;
        public EnemyVerb Verb;
        public int Waypoint;
    }

    public static class EnemyAi
    {
        public const float DefaultDifficulty = 0.5f;
        public const float ReactSlow = 0.35f;
        public const float ReactFast = 0.12f;
        public const int MinDummies = 1;
        public const int MaxDummies = 3;
        public const float RouteHoldSeconds = 0.55f;
        public const float RouteMargin = 1.25f;
        public const float EdgeHoldSeconds = 0.45f;
        public const string MegaRootName = "MegaPark";

        public static float ClampDifficulty(float difficulty)
        {
            return Mathf.Clamp01(difficulty);
        }

        public static float ReactionDelay(float difficulty)
        {
            return Mathf.Lerp(ReactSlow, ReactFast, ClampDifficulty(difficulty));
        }

        public static float AimErrorMeters(float difficulty)
        {
            return Mathf.Lerp(1.40f, 0.12f, ClampDifficulty(difficulty));
        }

        public static float VerbRate(float difficulty)
        {
            return Mathf.Lerp(0.34f, 0.96f, ClampDifficulty(difficulty));
        }

        public static float LeadSeconds(float difficulty)
        {
            return Mathf.Lerp(0.08f, 0.22f, ClampDifficulty(difficulty));
        }

        public static int ClampDummyCount(int count)
        {
            if (count < MinDummies) return MinDummies;
            if (count > MaxDummies) return MaxDummies;
            return count;
        }

        /// <summary>DummyRunner and its solo clones. The player gate stays closed.</summary>
        public static bool AllowRope(bool ai, string pawnName)
        {
            if (!ai || string.IsNullOrEmpty(pawnName)) return false;
            if (pawnName == "DummyRunner") return true;
            return pawnName.StartsWith("DummyRunner_");
        }

        public static bool LungeCommitLegal(float tellSeconds, float leadSeconds, bool window, bool grounded, bool blocked)
        {
            if (!window || !grounded || blocked) return false;
            float lead = leadSeconds > 0.01f ? leadSeconds : 0.45f;
            return tellSeconds + 0.0001f >= lead;
        }

        /// <summary>
        /// Stale sample of where the target is and how they are moving now.
        /// The lead uses that sample. It does not read their next stick.
        /// </summary>
        public static Vector3 DelayedAim(
            ref EnemyMemory memory,
            float difficulty,
            float dt,
            Vector3 self,
            Vector3 targetPos,
            Vector3 targetVel,
            int targetId,
            float leadBonus)
        {
            if (dt < 0f) dt = 0f;
            if (memory.TargetId != targetId)
            {
                memory.TargetId = targetId;
                memory.HasPercept = false;
                memory.ReactLeft = 0f;
            }

            if (!memory.HasPercept)
            {
                memory.HasPercept = true;
                memory.PerceivedPos = targetPos;
                memory.PerceivedVel = Vector3.zero;
                memory.ReactLeft = ReactionDelay(difficulty);
                memory.AimSign = 1f;
                return targetPos;
            }

            memory.ReactLeft -= dt;
            if (memory.ReactLeft <= 0f)
            {
                memory.PerceivedPos = targetPos;
                memory.PerceivedVel = Flat(targetVel);
                memory.ReactLeft = ReactionDelay(difficulty);
                memory.Sample++;
                memory.AimSign = Hash01(memory.Sample * 13 + targetId * 7) < 0.5f ? -1f : 1f;
            }

            float lead = LeadSeconds(difficulty) + (leadBonus > 0f ? leadBonus : 0f);
            if (lead < 0f) lead = 0f;
            if (lead > 0.40f) lead = 0.40f;
            Vector3 point = memory.PerceivedPos + memory.PerceivedVel * lead;
            Vector3 to = point - self;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist < 0.05f) return point;
            Vector3 side = new Vector3(-to.z / dist, 0f, to.x / dist);
            float scale = dist < 2.2f ? dist / 2.2f : 1f;
            return point + side * (memory.AimSign * AimErrorMeters(difficulty) * scale);
        }

        public static EnemyOverlay Decorate(ref EnemyMemory memory, EnemySense sense, OpponentChaseWish wish)
        {
            EnemyOverlay overlay = FromWish(wish);
            if (sense.Dt > 0f && memory.WallJumpSuppress > 0f)
                memory.WallJumpSuppress -= sense.Dt;
            if (sense.Dt > 0f && memory.SlideHold > 0f)
                memory.SlideHold -= sense.Dt;
            if (sense.Dt > 0f && memory.GrappleHold > 0f)
                memory.GrappleHold -= sense.Dt;

            if (sense.IsIt && sense.TargetTagBackBlocked)
            {
                overlay.Face = sense.Forward.sqrMagnitude > 0.001f ? Flat(sense.Forward) : overlay.Face;
                overlay.MoveX = 0f;
                overlay.MoveY = 0f;
                overlay.Sprint = false;
                overlay.Jump = false;
                overlay.Crouch = false;
                overlay.Grapple = false;
                overlay.Dash = false;
                overlay.WallJump = false;
                overlay.Punch = false;
                overlay.Lunge = false;
                overlay.Verb = EnemyVerb.Hold;
                return overlay;
            }

            if (sense.SameWallClosed && (overlay.Verb == EnemyVerb.Cling || overlay.Verb == EnemyVerb.WallRun || overlay.Verb == EnemyVerb.WallJump))
            {
                overlay.Jump = false;
                overlay.WallJump = false;
                overlay.Verb = overlay.Sprint ? EnemyVerb.Sprint : EnemyVerb.Close;
            }
            else if (wish.Verb == OpponentChaseVerb.WallCling && !sense.SameWallClosed)
            {
                float into = OpponentChaseSteer.IntoWallDot(wish.Face, sense.WallNormal);
                overlay.Verb = into >= 0.82f ? EnemyVerb.Cling : EnemyVerb.WallRun;
                bool leave = sense.OnWall && sense.WallTime > 0.28f && sense.TargetHeightDelta > 1.1f;
                if (leave && memory.WallJumpSuppress <= 0f && Allowed(ref memory, sense, 3))
                {
                    overlay.WallJump = true;
                    overlay.Jump = true;
                    overlay.Verb = EnemyVerb.WallJump;
                    memory.WallJumpSuppress = 0.50f;
                }
            }

            bool straight = Mathf.Abs(wish.Strafe) < 0.25f && wish.MoveY > 0.4f;
            bool far = sense.PlanarDistance > sense.PunchReach + 4.2f;
            bool slideOk = sense.SlideOpen && sense.Grounded && sense.PlanarSpeed >= 7.5f && straight && far
                && wish.Verb == OpponentChaseVerb.Sprint && !wish.Lunge;
            if (memory.SlideHold > 0f && slideOk)
            {
                overlay.Crouch = true;
                overlay.Verb = EnemyVerb.Slide;
            }
            else if (slideOk && Allowed(ref memory, sense, 5))
            {
                memory.SlideHold = 0.30f;
                overlay.Crouch = true;
                overlay.Verb = EnemyVerb.Slide;
            }

            if (sense.Airborne && sense.AirDashReady && !wish.Lunge && !overlay.WallJump
                && (wish.Verb == OpponentChaseVerb.AirStrafe || (sense.GapAhead && !sense.GapClear))
                && Allowed(ref memory, sense, 7))
            {
                overlay.Dash = true;
                overlay.Crouch = false;
                overlay.Verb = EnemyVerb.AirDash;
            }

            bool grappleOk = !wish.Lunge && wish.Verb != OpponentChaseVerb.GapJump && !overlay.Dash;
            bool grappleHit = grappleOk && sense.GrappleLatch && sense.GrappleOutward;
            bool grappleMiss = grappleOk && sense.GrappleProbe && !sense.GrappleLatch && sense.Difficulty < 0.45f;
            if (memory.GrappleHold > 0f && grappleOk)
            {
                overlay.Grapple = true;
                overlay.Verb = EnemyVerb.Grapple;
            }
            else if ((grappleHit && Allowed(ref memory, sense, 11)) || (grappleMiss && Allowed(ref memory, sense, 13)))
            {
                // A miss still holds RMB. The hook latches nothing.
                memory.GrappleHold = 0.40f;
                overlay.Grapple = true;
                overlay.Verb = EnemyVerb.Grapple;
            }

            if (wish.Lunge)
            {
                overlay.Lunge = true;
                overlay.Dash = false;
                overlay.Grapple = false;
                overlay.Crouch = false;
                overlay.Verb = EnemyVerb.Lunge;
            }

            if (sense.IsIt && !sense.TargetTagBackBlocked && sense.PlanarDistance <= sense.PunchReach && sense.AngleDeg <= 28f && !wish.Lunge)
            {
                overlay.Punch = true;
                overlay.Verb = EnemyVerb.Punch;
            }

            return overlay;
        }

        public static EnemyOverlay Evade(ref EnemyMemory memory, EnemySense sense)
        {
            EnemyOverlay overlay;
            overlay.Face = sense.Forward.sqrMagnitude > 0.001f ? Flat(sense.Forward) : new Vector3(0f, 0f, 1f);
            overlay.MoveX = 0f;
            overlay.MoveY = 1f;
            overlay.Sprint = true;
            overlay.Jump = false;
            overlay.Crouch = false;
            overlay.Grapple = false;
            overlay.Dash = false;
            overlay.WallJump = false;
            overlay.Punch = false;
            overlay.Lunge = false;
            overlay.Verb = EnemyVerb.Loop;
            overlay.Waypoint = memory.Waypoint;

            if (sense.Dt < 0f) sense.Dt = 0f;
            if (memory.CoverHold > 0f) memory.CoverHold -= sense.Dt;
            if (memory.SlideHold > 0f) memory.SlideHold -= sense.Dt;
            if (memory.GrappleHold > 0f) memory.GrappleHold -= sense.Dt;
            if (memory.CoverSide == 0) memory.CoverSide = 1;

            int edge = PickEscapeEdge(ref memory, sense);
            if (edge == 1 && sense.PadAim.sqrMagnitude > 0.001f)
            {
                overlay.Face = Flat(sense.PadAim).normalized;
                overlay.Verb = EnemyVerb.Pad;
            }
            else if (edge == 2 && sense.ZipAim.sqrMagnitude > 0.001f)
            {
                overlay.Face = Flat(sense.ZipAim).normalized;
                overlay.Verb = EnemyVerb.Zip;
            }
            else if (edge == 3 && sense.CoverAim.sqrMagnitude > 0.001f)
            {
                overlay.Face = Flat(sense.CoverAim).normalized;
                overlay.Verb = EnemyVerb.Cover;
            }
            else
            {
                int wp = NextWaypoint(sense.SelfPos, sense.Forward, sense.MegaPark, memory.Waypoint);
                memory.Waypoint = wp;
                overlay.Waypoint = wp;
                Vector3 to = LoopPoint(sense.MegaPark, wp) - sense.SelfPos;
                if (to.sqrMagnitude < 0.25f)
                    to = LoopPoint(sense.MegaPark, wp + 1) - sense.SelfPos;
                overlay.Face = Flat(to).sqrMagnitude > 0.001f ? Flat(to).normalized : overlay.Face;
                overlay.Verb = EnemyVerb.Loop;
            }

            if (sense.SelfTagBackRemaining > 0f)
            {
                overlay.Punch = false;
                overlay.Verb = overlay.Verb == EnemyVerb.Hold ? EnemyVerb.Evade : overlay.Verb;
                if (overlay.Verb == EnemyVerb.Loop || overlay.Verb == EnemyVerb.Cover)
                    overlay.Verb = EnemyVerb.Evade;
            }

            bool slideFlee = sense.Grounded && sense.PlanarSpeed >= 7.5f && sense.ThreatClosing;
            if (memory.SlideHold > 0f && slideFlee)
            {
                overlay.Crouch = true;
                if (overlay.Verb == EnemyVerb.Loop || overlay.Verb == EnemyVerb.Evade || overlay.Verb == EnemyVerb.Cover)
                    overlay.Verb = EnemyVerb.Slide;
            }
            else if (slideFlee && Allowed(ref memory, sense, 19))
            {
                memory.SlideHold = 0.30f;
                overlay.Crouch = true;
                if (overlay.Verb == EnemyVerb.Loop || overlay.Verb == EnemyVerb.Evade || overlay.Verb == EnemyVerb.Cover)
                    overlay.Verb = EnemyVerb.Slide;
            }

            if (sense.Airborne && sense.AirDashReady && sense.ThreatClosing && Allowed(ref memory, sense, 23))
            {
                overlay.Dash = true;
                overlay.Crouch = false;
                overlay.Verb = EnemyVerb.AirDash;
            }

            if (!overlay.Dash && sense.GrappleLatch && sense.GrappleOutward && sense.ThreatClosing
                && (memory.GrappleHold > 0f || Allowed(ref memory, sense, 29)))
            {
                if (memory.GrappleHold <= 0f) memory.GrappleHold = 0.40f;
                overlay.Grapple = true;
                overlay.Verb = EnemyVerb.Grapple;
            }

            bool escape = sense.SelfTagBackRemaining > 0f || edge == 1 || edge == 2;
            if (!escape && sense.Cornered && sense.PlanarDistance <= sense.PunchReach * 1.25f)
            {
                overlay.Punch = true;
                overlay.MoveY = 0.35f;
                overlay.Sprint = false;
                overlay.Verb = EnemyVerb.Punch;
            }

            if (overlay.Face.sqrMagnitude < 0.001f)
                overlay.Face = new Vector3(0f, 0f, 1f);
            return overlay;
        }

        public static int CommitRoute(ref EnemyMemory memory, float dt, int candidate, float candidateScore, float heldScore, bool heldValid)
        {
            if (dt < 0f) dt = 0f;
            if (memory.RouteHold > 0f) memory.RouteHold -= dt;
            if (memory.RouteId == 0 || !heldValid)
            {
                memory.RouteId = candidate;
                memory.RouteScore = candidateScore;
                memory.RouteHold = RouteHoldSeconds;
                return memory.RouteId;
            }

            if (memory.RouteHold > 0f)
                return memory.RouteId;

            if (candidate != memory.RouteId && candidateScore + RouteMargin < memory.RouteScore)
            {
                memory.Flips++;
                memory.RouteId = candidate;
                memory.RouteScore = candidateScore;
                memory.RouteHold = RouteHoldSeconds;
            }
            else
            {
                memory.RouteScore = heldScore;
                memory.RouteHold = RouteHoldSeconds;
            }

            return memory.RouteId;
        }

        public static int CommitTarget(ref EnemyMemory memory, float dt, int current, float currentDist, int best, float bestDist, bool currentOk)
        {
            if (dt < 0f) dt = 0f;
            if (memory.StickTargetHold > 0f)
                memory.StickTargetHold -= dt;
            if (!currentOk || current == 0)
            {
                memory.StickTarget = best;
                memory.StickTargetHold = EdgeHoldSeconds;
                return best;
            }

            if (memory.StickTargetHold > 0f && memory.StickTarget == current)
                return current;

            if (best != current && bestDist + 2.5f < currentDist)
            {
                memory.Flips++;
                memory.StickTarget = best;
                memory.StickTargetHold = EdgeHoldSeconds;
                return best;
            }

            memory.StickTarget = current;
            memory.StickTargetHold = EdgeHoldSeconds;
            return current;
        }

        public static Vector3 LoopPoint(bool mega, int index)
        {
            float scale = mega ? 14f : 10f;
            int slot = index % 4;
            if (slot < 0) slot += 4;
            if (slot == 0) return new Vector3(14f * scale, 0f, 18f * scale);
            if (slot == 1) return new Vector3(48f * scale, 0f, 18f * scale);
            if (slot == 2) return new Vector3(48f * scale, 0f, 36f * scale);
            return new Vector3(14f * scale, 0f, 36f * scale);
        }

        public static int NextWaypoint(Vector3 self, Vector3 forward, bool mega, int current)
        {
            if (current < 0) current = 0;
            Vector3 point = LoopPoint(mega, current);
            Vector3 to = point - self;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > 2.5f)
            {
                Vector3 fwd = Flat(forward);
                if (fwd.sqrMagnitude < 0.01f || Vector3.Dot(fwd, to) > -0.15f)
                    return current;
            }

            return current + 1;
        }

        static int PickEscapeEdge(ref EnemyMemory memory, EnemySense sense)
        {
            if (memory.EdgeHold > 0f) memory.EdgeHold -= sense.Dt;
            int best = 4;
            float bestScore = 10f;
            if (sense.ThreatClosing && sense.Grounded && sense.PadAhead && sense.PadHelps)
            {
                best = 1;
                bestScore = 1f;
            }
            else if (sense.ThreatClosing && sense.ZipAhead && sense.ZipHelps)
            {
                best = 2;
                bestScore = 2f;
            }
            else if (sense.LineOfSight && sense.CoverAim.sqrMagnitude > 0.001f)
            {
                best = 3;
                bestScore = 3f;
            }

            if (memory.EdgeId == 0)
            {
                memory.EdgeId = best;
                memory.EdgeScore = bestScore;
                memory.EdgeHold = EdgeHoldSeconds;
                return best;
            }

            if (memory.EdgeHold > 0f)
                return memory.EdgeId;

            if (best != memory.EdgeId && bestScore + 0.75f < memory.EdgeScore)
            {
                memory.Flips++;
                memory.EdgeId = best;
                memory.EdgeScore = bestScore;
                memory.EdgeHold = EdgeHoldSeconds;
                return best;
            }

            memory.EdgeHold = EdgeHoldSeconds;
            return memory.EdgeId;
        }

        static bool Allowed(ref EnemyMemory memory, EnemySense sense, int salt)
        {
            if (sense.ForceVerbs) return true;
            memory.VerbClock += 1f;
            float roll = Hash01((int)memory.VerbClock * 17 + salt * 97);
            return roll < VerbRate(sense.Difficulty);
        }

        static EnemyOverlay FromWish(OpponentChaseWish wish)
        {
            EnemyOverlay overlay;
            overlay.Face = wish.Face.sqrMagnitude > 0.001f ? wish.Face : new Vector3(0f, 0f, 1f);
            overlay.MoveX = wish.Strafe;
            overlay.MoveY = wish.MoveY;
            overlay.Sprint = wish.Sprint;
            overlay.Jump = wish.Jump;
            overlay.Crouch = false;
            overlay.Grapple = false;
            overlay.Dash = false;
            overlay.WallJump = false;
            overlay.Punch = false;
            overlay.Lunge = wish.Lunge;
            overlay.Verb = MapVerb(wish.Verb);
            overlay.Waypoint = 0;
            return overlay;
        }

        static EnemyVerb MapVerb(OpponentChaseVerb verb)
        {
            if (verb == OpponentChaseVerb.Sprint) return EnemyVerb.Sprint;
            if (verb == OpponentChaseVerb.AirStrafe) return EnemyVerb.Close;
            if (verb == OpponentChaseVerb.GapJump) return EnemyVerb.Jump;
            if (verb == OpponentChaseVerb.WallCling) return EnemyVerb.Cling;
            if (verb == OpponentChaseVerb.Lunge) return EnemyVerb.Lunge;
            if (verb == OpponentChaseVerb.PadTake) return EnemyVerb.Pad;
            if (verb == OpponentChaseVerb.ZipTake) return EnemyVerb.Zip;
            return EnemyVerb.Close;
        }

        static float Hash01(int n)
        {
            uint x = (uint)n * 747796405u + 2891336453u;
            uint shift = (x >> 28) + 4u;
            x = ((x >> (int)shift) ^ x) * 277803737u;
            x = (x >> 22) ^ x;
            return (x & 0xFFFFFFu) / 16777215f;
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
