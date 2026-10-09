using Tag.Level;
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
        public int PlanTick;
        public bool LungeArmed;
        public short ParkMark;
    }

    /// <summary>One Mega Park route the pawn can commit to. Lower cost wins.</summary>
    public struct ParkOption
    {
        public int Id;
        public float Seconds;
        public Vector3 Aim;
        public EnemyVerb Verb;
        public bool BreaksLos;
        public bool Toy;
        public bool Counter;
        public bool Cling;
        public bool Bar;
        public bool Grapple;
        public bool Toward;
        public int Wall;
        public short Mark;
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
        /// <summary>Retarget clock. Path searches do not run faster than this unless someone moves PathMoveRefresh.</summary>
        public const float DecisionHz = 5.2f;
        /// <summary>A pad, zip, or loop sample is stale after this much planar travel, so a chase still sees the toy.</summary>
        public const float PathMoveRefresh = 1.25f;
        public static int LoopProjectQueries;
        public static int LoopProjectMisses;
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

        static Vector3[] _megaLoopPts;

        public static void ResetLoopSearch()
        {
            LoopProjectQueries = 0;
            LoopProjectMisses = 0;
            _lpN = 0;
            _lpArena = -2;
        }

        public static Vector3 LoopPoint(bool mega, int index)
        {
            if (ParkArena.IsStack)
            {
                MegaParkP1Layout.Pt[] stackLoop = StackYardLayout.AiLoop;
                int stack = index % stackLoop.Length;
                if (stack < 0) stack += stackLoop.Length;
                MegaParkP1Layout.Pt stackPt = stackLoop[stack];
                return new Vector3(stackPt.X, StackYardLayout.SpawnY, stackPt.Z);
            }

            if (ParkArena.IsPocket)
            {
                MegaParkP1Layout.Pt[] pocketLoop = PocketParkLayout.AiLoop;
                int pocket = index % pocketLoop.Length;
                if (pocket < 0) pocket += pocketLoop.Length;
                MegaParkP1Layout.Pt pocketPt = pocketLoop[pocket];
                return new Vector3(pocketPt.X, PocketParkLayout.SpawnY, pocketPt.Z);
            }

            if (mega)
            {
                MegaParkP1Layout.Pt[] loop = MegaParkP1Layout.LoopCcw;
                int n = loop.Length;
                if (n <= 0) return new Vector3(8f, 0f, 8f);
                if (_megaLoopPts == null || _megaLoopPts.Length != n)
                {
                    _megaLoopPts = new Vector3[n];
                    for (int i = 0; i < n; i++)
                    {
                        MegaParkP1Layout.Pt src = loop[i];
                        _megaLoopPts[i] = new Vector3(src.X, src.Y, src.Z);
                    }
                }
                int slot = index % n;
                if (slot < 0) slot += n;
                return _megaLoopPts[slot];
            }

            float scale = 10f;
            int box = index % 4;
            if (box < 0) box += 4;
            if (box == 0) return new Vector3(14f * scale, 0f, 18f * scale);
            if (box == 1) return new Vector3(48f * scale, 0f, 18f * scale);
            if (box == 2) return new Vector3(48f * scale, 0f, 36f * scale);
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

        static readonly ParkOption[] ParkScratch = new ParkOption[96];

        /// <summary>How often the pawn may replan. Low difficulty holds a stale route longer.</summary>
        public static float PlanInterval(float difficulty, bool isIt, float threatDist, float tagBack)
        {
            float d = ClampDifficulty(difficulty);
            if (!isIt && tagBack > 0f) return 0.18f;
            if (isIt) return Mathf.Lerp(0.72f, 0.26f, d);
            if (threatDist < Mathf.Lerp(16f, 9f, d))
                return Mathf.Lerp(0.48f, 0.20f, d);
            return Mathf.Lerp(0.56f, 0.30f, d);
        }

        /// <summary>Whether to start the 0.45 s lunge tell. The tell length itself does not scale.</summary>
        public static bool ArmLungeTell(float difficulty, int salt)
        {
            float gate = Mathf.Lerp(0.16f, 1f, ClampDifficulty(difficulty));
            return Hash01(salt * 17 + 11) < gate;
        }

        /// <summary>
        /// Pick a Mega Park aim. Costs are flat distance and loop arc, not a grid search.
        /// The pawn still moves with the player motor. Hops stay empty.
        /// </summary>
        public static bool PlanPark(
            ref EnemyMemory memory,
            float dt,
            float difficulty,
            bool isIt,
            Vector3 self,
            Vector3 perceived,
            Vector3 perceivedVel,
            float threatDist,
            bool los,
            bool sameWall,
            int threatWall,
            float tagBack,
            float sprint,
            float crouch,
            float wallRun,
            out Vector3 aim,
            out EnemyVerb verb,
            MegaParkP1Layout.ParkHop[] hops,
            out int hopCount)
        {
            aim = perceived;
            verb = isIt ? EnemyVerb.Close : EnemyVerb.Evade;
            hopCount = 0;
            if (hops != null && hops.Length > 0) hops[0].Mark = -1;
            if (sprint < 1f) sprint = 13.8f;
            MegaParkP1Layout.WarmParkRoutes();
            float d = ClampDifficulty(difficulty);
            memory.PlanTick++;
            memory.ParkMark = -1;
            float lead = isIt ? Mathf.Lerp(0.05f, 0.50f, d) : 0f;
            Vector3 leadVec = perceivedVel * lead;
            float leadMag = Mathf.Sqrt(leadVec.x * leadVec.x + leadVec.z * leadVec.z);
            float gapNow = FlatMeters(self.x, self.z, perceived.x, perceived.z);
            float maxLead = Mathf.Max(2.2f, gapNow * Mathf.Lerp(0.10f, 0.38f, d));
            if (leadMag > maxLead && leadMag > 0.01f)
                leadVec = leadVec * (maxLead / leadMag);
            Vector3 goal = perceived + leadVec;
            goal.x = ClampRange(goal.x, 2f, ParkArena.MapW - 2f);
            goal.z = ClampRange(goal.z, 2f, ParkArena.MapD - 2f);

            int n = 0;
            int salt = memory.PlanTick * 3 + memory.Sample * 5;
            float gap = FlatMeters(self.x, self.z, goal.x, goal.z);
            float directT = Mathf.Max(gap, 0.5f) / sprint;

            if (isIt)
            {
                ParkScratch[n++] = Opt(2, directT, goal, EnemyVerb.Close, false, false, false, false, false, false, false, 0, -1);
            }
            else
            {
                Vector3 away = FleeAim(self, perceived, d, salt);
                float awayM = FlatMeters(self.x, self.z, away.x, away.z);
                bool awayBreak = !MegaParkP1Layout.ParkLos(away.x, away.z, perceived.x, perceived.z);
                Vector3 evadeAim = away;
                if (ParkArena.IsPocket)
                {
                    float blend = PocketParkLayout.EvadeLoopBlend(d);
                    if (blend > 0f)
                    {
                        float toward = LoopChaseSign(self.x, self.z, perceived.x, perceived.z);
                        float meters = Mathf.Lerp(14f, 26f, d);
                        LoopOffset(self.x, self.z, -toward * meters, out float fx, out float fz);
                        evadeAim.x = away.x + (fx - away.x) * blend;
                        evadeAim.z = away.z + (fz - away.z) * blend;
                    }
                }
                else if (ParkArena.IsStack)
                {
                    float blend = StackYardLayout.EvadeLoopBlend(d);
                    if (blend > 0f)
                    {
                        float toward = LoopChaseSign(self.x, self.z, perceived.x, perceived.z);
                        float meters = Mathf.Lerp(14f, 26f, d);
                        LoopOffset(self.x, self.z, -toward * meters, out float fx, out float fz);
                        evadeAim.x = away.x + (fx - away.x) * blend;
                        evadeAim.z = away.z + (fz - away.z) * blend;
                    }
                }
                ParkScratch[n++] = Opt(2, Mathf.Max(awayM, 1f) / sprint, evadeAim, EnemyVerb.Evade, awayBreak, false, false, false, false, false, false, 0, -1);
            }

            float towardSign = LoopChaseSign(self.x, self.z, perceived.x, perceived.z);
            float fleeSign = -towardSign;
            float loopMeters = isIt ? 16f : Mathf.Lerp(14f, 26f, d);
            float loopSign = isIt ? towardSign : fleeSign;
            if (isIt && Hash01(salt * 3 + 1) < (1f - d) * 0.48f)
                loopSign = -loopSign;
            LoopOffset(self.x, self.z, loopSign * loopMeters, out float lx, out float lz);
            float arc = LoopArc(self.x, self.z, perceived.x, perceived.z);
            float loopT = isIt ? Mathf.Max(arc, 4f) / sprint : 3.6f;
            ParkScratch[n++] = Opt(1, loopT, new Vector3(lx, self.y, lz), EnemyVerb.Loop, false, false, false, false, false, false, false, 0, -1);

            if (!isIt)
            {
                LoopOffset(self.x, self.z, towardSign * 14f, out float tx, out float tz);
                ParkScratch[n++] = Opt(4, 5.5f, new Vector3(tx, self.y, tz), EnemyVerb.Loop, false, false, false, false, false, false, true, 0, -1);
                if (MegaParkP1Layout.FindCover(self.x, self.z, perceived.x, perceived.z, out float cx, out float cz))
                    ParkScratch[n++] = Opt(3, 5.8f, new Vector3(cx, self.y, cz), EnemyVerb.Cover, true, false, false, false, false, false, false, 0, -1);
            }

            int marks = MegaParkP1Layout.ParkMarkCount;
            for (int i = 0; i < marks && n < ParkScratch.Length; i++)
            {
                MegaParkP1Layout.ParkMark m = MegaParkP1Layout.ParkMarks[i];
                float mount = FlatMeters(self.x, self.z, m.X, m.Z);
                if (mount > 38f) continue;
                float exitThreat = FlatMeters(m.ExitX, m.ExitZ, perceived.x, perceived.z);
                float after = FlatMeters(m.ExitX, m.ExitZ, goal.x, goal.z);
                bool toy = m.Kind == MegaParkP1Layout.HopPad || m.Kind == MegaParkP1Layout.HopZip;
                bool grapple = m.Kind == MegaParkP1Layout.HopGrapple;
                bool cling = m.Kind == MegaParkP1Layout.HopCling;
                bool bar = m.Kind == MegaParkP1Layout.HopBar;
                bool counter = m.Kind == MegaParkP1Layout.HopCounter;
                float eta = isIt
                    ? mount / sprint + m.Ride + after / sprint
                    : 4.6f + mount / 90f;
                bool closer = after + 3.5f < gap;
                bool helpsIt = isIt && (eta + 0.2f < directT || (toy && closer && mount < 24f) || (grapple && closer && mount < 20f));
                bool breaks = !isIt && !MegaParkP1Layout.ParkLos(m.ExitX, m.ExitZ, perceived.x, perceived.z);
                bool helpsRun = !isIt && (exitThreat > threatDist + 2.2f || breaks || counter);
                if (cling && sameWall && m.Wall != 0 && m.Wall == threatWall) helpsRun = false;
                if (isIt && !helpsIt) continue;
                if (!isIt && !helpsRun) continue;
                if (!isIt && !breaks && !counter && exitThreat + 1f < threatDist) continue;
                float ax = toy || cling ? m.X : m.ExitX;
                float az = toy || cling ? m.Z : m.ExitZ;
                if (toy)
                {
                    ax = m.X;
                    az = m.Z;
                }
                else if (cling || bar || grapple || counter)
                {
                    ax = m.ExitX;
                    az = m.ExitZ;
                }
                ParkScratch[n++] = Opt(
                    10 + i,
                    eta,
                    new Vector3(ax, self.y, az),
                    VerbForMark(m.Kind),
                    breaks,
                    toy,
                    counter,
                    cling,
                    bar,
                    grapple,
                    false,
                    m.Wall,
                    (short)i);
            }

            if (n <= 0)
            {
                aim = ContainAim(isIt ? goal : FleeAim(self, perceived, d, salt));
                return false;
            }

            int pick = 0;
            float best = 1e9f;
            int held = -1;
            float heldCost = 0f;
            float chord = FlatMeters(self.x, self.z, goal.x, goal.z);
            bool smartCut = isIt && los && chord + 1.2f < arc && d >= 0.35f;
            bool wasteCut = isIt && !los && Hash01(salt * 9 + 4) < (1f - d) * 0.55f;
            bool allowCut = smartCut || wasteCut || (isIt && threatDist < 12f);
            for (int i = 0; i < n; i++)
            {
                float c = ScorePark(d, isIt, los, sameWall, threatWall, threatDist, tagBack, allowCut, salt, ParkScratch[i]);
                ParkScratch[i].Seconds = c;
                if (c < best)
                {
                    best = c;
                    pick = i;
                }
                if (ParkScratch[i].Id == memory.RouteId)
                {
                    held = i;
                    heldCost = c;
                }
            }

            int committed = CommitRoute(ref memory, dt, ParkScratch[pick].Id, best, held >= 0 ? heldCost : best, held >= 0);
            int chosen = pick;
            for (int i = 0; i < n; i++)
            {
                if (ParkScratch[i].Id == committed)
                {
                    chosen = i;
                    break;
                }
            }

            ParkOption route = ParkScratch[chosen];
            aim = ContainAim(route.Aim);
            verb = route.Verb;
            memory.ParkMark = route.Mark;
            return true;
        }

        /// <summary>Keep a steer point inside the active park fence. The fence is 33 m tall on the map edge.</summary>
        public static Vector3 ContainAim(Vector3 p)
        {
            p.x = ClampRange(p.x, 1.2f, ParkArena.MapW - 1.2f);
            p.z = ClampRange(p.z, 1.2f, ParkArena.MapD - 1.2f);
            if (p.y > MegaParkP1Layout.FenceTop) p.y = 0f;
            if (p.y < MegaParkP1Layout.KillPlaneY) p.y = 0f;
            return p;
        }

        static Vector3 FleeAim(Vector3 self, Vector3 perceived, float d, int salt)
        {
            float ax = self.x - perceived.x;
            float az = self.z - perceived.z;
            float am = Mathf.Sqrt(ax * ax + az * az);
            if (am < 0.05f)
            {
                ax = 1f;
                az = 0f;
                am = 1f;
            }
            ax /= am;
            az /= am;
            float side = Hash01(salt * 13 + 2) < 0.5f ? 1f : -1f;
            float awayW = Mathf.Lerp(0.05f, 0.35f, d);
            float dirx = ax * awayW + (-az) * side * (1f - awayW);
            float dirz = az * awayW + ax * side * (1f - awayW);
            float mag = Mathf.Sqrt(dirx * dirx + dirz * dirz);
            if (mag < 0.05f) mag = 1f;
            float reach = Mathf.Lerp(12f, 24f, d);
            float x = self.x + dirx / mag * reach;
            float z = self.z + dirz / mag * reach;
            x = ClampRange(x, 2.2f, ParkArena.MapW - 2.2f);
            z = ClampRange(z, 2.2f, ParkArena.MapD - 2.2f);
            return new Vector3(x, self.y, z);
        }

        static float FlatMeters(float x0, float z0, float x1, float z1)
        {
            float dx = x1 - x0;
            float dz = z1 - z0;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static float ClampRange(float v, float lo, float hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        static ParkOption Opt(int id, float seconds, Vector3 aim, EnemyVerb verb, bool los, bool toy, bool counter, bool cling, bool bar, bool grapple, bool toward, int wall, short mark)
        {
            ParkOption o;
            o.Id = id;
            o.Seconds = seconds;
            o.Aim = aim;
            o.Verb = verb;
            o.BreaksLos = los;
            o.Toy = toy;
            o.Counter = counter;
            o.Cling = cling;
            o.Bar = bar;
            o.Grapple = grapple;
            o.Toward = toward;
            o.Wall = wall;
            o.Mark = mark;
            return o;
        }

        static EnemyVerb VerbForMark(byte kind)
        {
            if (kind == MegaParkP1Layout.HopPad) return EnemyVerb.Pad;
            if (kind == MegaParkP1Layout.HopZip) return EnemyVerb.Zip;
            if (kind == MegaParkP1Layout.HopGrapple) return EnemyVerb.Grapple;
            if (kind == MegaParkP1Layout.HopCling) return EnemyVerb.Cling;
            if (kind == MegaParkP1Layout.HopBar) return EnemyVerb.Slide;
            if (kind == MegaParkP1Layout.HopCounter) return EnemyVerb.Cover;
            return EnemyVerb.Close;
        }

        static float ScorePark(float d, bool isIt, bool los, bool sameWall, int threatWall, float threatDist, float tagBack, bool allowCut, int salt, ParkOption o)
        {
            float cost = o.Seconds;
            if (isIt)
            {
                bool finish = threatDist < 11f && (o.Verb == EnemyVerb.Close || o.Verb == EnemyVerb.Sprint);
                if (!allowCut && o.Verb != EnemyVerb.Loop && !finish)
                    cost += 34f;
                if (o.Verb == EnemyVerb.Loop)
                    cost += d * 8f;
                else if (!o.Toy && !o.Grapple)
                    cost -= d * 1.4f;
                if (o.Toy)
                    cost += 1.8f - d * 1.2f;
                if (o.Grapple)
                    cost += 1.2f;
                if (finish)
                    cost -= 16f;
                if (threatDist < 8f && (o.Toy || o.Grapple || o.Cling || o.Bar || o.Counter))
                    cost += 10f;
            }
            else
            {
                float calmP = d < 0.7f
                    ? Mathf.Lerp(0.05f, 0.32f, d / 0.7f)
                    : Mathf.Lerp(0.32f, 0.90f, (d - 0.7f) / 0.22f);
                if (calmP > 0.90f) calmP = 0.90f;
                bool calm = Hash01(salt * 5 + 7) < calmP;
                if (o.Toward)
                    cost += 2f + d * 14f;
                if (calm && o.Verb != EnemyVerb.Loop)
                    cost += 6.5f;
                if (!calm && o.Verb == EnemyVerb.Loop && !o.Toward)
                    cost += 6.5f;
                if (o.Verb == EnemyVerb.Evade)
                    cost += d * 4.5f;
                if (o.BreaksLos || o.Verb == EnemyVerb.Cover)
                    cost -= d * 2.2f;
                if (o.Counter || o.Bar || o.Cling)
                    cost -= d * 2.4f;
                if (!calm && (o.Cling || o.Bar) && o.Seconds < 4.85f)
                    cost -= 1.6f;
                if (o.Toy)
                    cost -= d * 2.8f;
                if (o.Grapple)
                    cost -= d * 1.6f;
                if (!calm && (o.Toy || o.Grapple || o.Counter || o.Cling || o.Bar || o.Verb == EnemyVerb.Cover))
                    cost += 3.5f;
                if (tagBack > 0f && !o.Toward && (o.Toy || o.BreaksLos || o.Verb == EnemyVerb.Evade || o.Verb == EnemyVerb.Loop))
                    cost -= 3.5f;
                if (los && (o.BreaksLos || o.Cling || o.Bar))
                    cost -= d * 2.5f;
                cost += (Hash01(salt + o.Id * 29) - 0.5f) * (1f - d) * 3.5f;
            }
            if (sameWall && o.Cling && o.Wall != 0 && o.Wall == threatWall)
                cost += 90f;
            return cost;
        }

        public static Vector3 LoopSteer(Vector3 self, Vector3 other, bool toward, float meters)
        {
            float sign = LoopChaseSign(self.x, self.z, other.x, other.z);
            if (!toward) sign = -sign;
            LoopOffset(self.x, self.z, sign * meters, out float x, out float z);
            return new Vector3(x, self.y, z);
        }

        public static void LoopOffset(float x, float z, float meters, out float ox, out float oz)
        {
            float len = SteerLoopLength();
            float t = LoopProject(x, z) + meters;
            t %= len;
            if (t < 0f) t += len;
            LoopAt(t, out ox, out oz);
        }

        static float LoopChaseSign(float x, float z, float ox, float oz)
        {
            float len = SteerLoopLength();
            float a = LoopProject(x, z);
            float b = LoopProject(ox, oz);
            float ccw = b - a;
            if (ccw < 0f) ccw += len;
            return ccw <= len * 0.5f ? 1f : -1f;
        }

        static float LoopArc(float x0, float z0, float x1, float z1)
        {
            float len = SteerLoopLength();
            float d = Mathf.Abs(LoopProject(x0, z0) - LoopProject(x1, z1));
            if (d > len * 0.5f) d = len - d;
            return d;
        }

        static int _lpN;
        static int _lpArena = -2;
        static float _lpX0, _lpZ0, _lpT0;
        static float _lpX1, _lpZ1, _lpT1;

        static MegaParkP1Layout.Pt[] SteerLoop()
        {
            if (ParkArena.IsStack) return StackYardLayout.AiLoop;
            return ParkArena.IsPocket ? PocketParkLayout.AiLoop : MegaParkP1Layout.LoopCcw;
        }

        static float SteerLoopLength()
        {
            if (ParkArena.IsStack) return StackYardLayout.AiLoopLength;
            return ParkArena.IsPocket ? PocketParkLayout.AiLoopLength : MegaParkP1Layout.LoopLengthM;
        }

        static float LoopProject(float x, float z)
        {
            int arena = ParkArena.Id;
            if (arena != _lpArena)
            {
                _lpN = 0;
                _lpArena = arena;
            }
            LoopProjectQueries++;
            if (_lpN >= 1 && x == _lpX0 && z == _lpZ0) return _lpT0;
            if (_lpN >= 2 && x == _lpX1 && z == _lpZ1) return _lpT1;
            LoopProjectMisses++;
            float t = ProjectLoop(x, z);
            _lpX1 = _lpX0;
            _lpZ1 = _lpZ0;
            _lpT1 = _lpT0;
            _lpX0 = x;
            _lpZ0 = z;
            _lpT0 = t;
            if (_lpN < 2) _lpN++;
            return t;
        }

        static float ProjectLoop(float x, float z)
        {
            MegaParkP1Layout.Pt[] loop = SteerLoop();
            float best = 1e9f;
            float bestT = 0f;
            float t0 = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                MegaParkP1Layout.Pt a = loop[i];
                MegaParkP1Layout.Pt b = loop[(i + 1) % loop.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float seg = Mathf.Sqrt(dx * dx + dz * dz);
                float u = 0f;
                if (seg > 0.001f)
                {
                    u = ((x - a.X) * dx + (z - a.Z) * dz) / (seg * seg);
                    if (u < 0f) u = 0f;
                    if (u > 1f) u = 1f;
                }
                float px = a.X + dx * u;
                float pz = a.Z + dz * u;
                float lat = (x - px) * (x - px) + (z - pz) * (z - pz);
                if (lat < best)
                {
                    best = lat;
                    bestT = t0 + seg * u;
                }
                t0 += seg;
            }
            return bestT;
        }

        static void LoopAt(float t, out float x, out float z)
        {
            MegaParkP1Layout.Pt[] loop = SteerLoop();
            float len = SteerLoopLength();
            if (t < 0f) t = 0f;
            if (t > len) t = len;
            float t0 = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                MegaParkP1Layout.Pt a = loop[i];
                MegaParkP1Layout.Pt b = loop[(i + 1) % loop.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float seg = Mathf.Sqrt(dx * dx + dz * dz);
                if (t <= t0 + seg + 0.0001f || i == loop.Length - 1)
                {
                    float u = seg > 0.001f ? (t - t0) / seg : 0f;
                    if (u < 0f) u = 0f;
                    if (u > 1f) u = 1f;
                    x = a.X + dx * u;
                    z = a.Z + dz * u;
                    return;
                }
                t0 += seg;
            }
            x = loop[0].X;
            z = loop[0].Z;
        }
    }
}
