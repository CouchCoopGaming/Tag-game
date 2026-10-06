using System;
using System.IO;
using Tag.Gameplay;
using Tag.Modes;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// How punch-stagger, the same-wall ban, launch pads, zip lines, and tag-back
    /// share one frame with the verbs that were already there. No new verb.
    /// Feel numbers stay in MovementConfig, PunchTagTuning, LaunchPad, and ZipLine.
    /// </summary>
    public static class VerbIntegration
    {
        public enum HitOrder
        {
            None = 0,
            Blocked = 1,
            Tagged = 2,
            Staggered = 3
        }

        public enum ChaseEdge
        {
            Hold = 0,
            Pad = 1,
            Zip = 2,
            Chase = 3
        }

        public struct ZipLeave
        {
            public Vector3 Velocity;
            public float Coyote;
            public float JumpSlot;
            public float WallJumpSlot;
            public bool Jumped;
        }

        public struct Carrier
        {
            public bool Zip;
            public bool Arc;
            public bool Grapple;
            public bool Lunging;
            public Vector3 Velocity;
        }

        /// <summary>Pad vertical replaces the entry vertical. A buffered jump is not added.</summary>
        public static Vector3 ApplyPadVelocity(Vector3 entry, float apex, float gravity, Vector3 horizontal, bool setHorizontal)
        {
            return LaunchPadRules.VelocitySet(entry, apex, gravity, horizontal, setHorizontal);
        }

        public static bool PadLeavesWall(bool climbing, bool wallRunning)
        {
            return climbing || wallRunning;
        }

        /// <summary>
        /// Ground opens the ban. A zip ride does not. Pad flight does not.
        /// The frame the arc ends on a deck does.
        /// </summary>
        public static bool ClearsWallBan(bool grounded, bool onWallState, bool zipRiding, bool launchArc)
        {
            if (!grounded || onWallState || zipRiding || launchArc) return false;
            return true;
        }

        public static bool EndLaunchArc(bool launchArc, bool grounded, float vy)
        {
            if (!launchArc) return false;
            return grounded && vy < KinematicStep.LaunchVy;
        }

        /// <summary>The rope yields while a zip or a pad owns the velocity.</summary>
        public static bool GrappleYields(bool zipRiding, bool launchQueued, bool launchArc)
        {
            return zipRiding || launchQueued || launchArc;
        }

        /// <summary>Stagger, a live pad, a vault, and a ragdoll refuse the grab. An air dash does not.</summary>
        public static bool ZipGrabBlocked(bool stagger, bool launchArc, bool launchQueued, bool motorLocked, bool vault, bool ragdoll, bool landStun)
        {
            return stagger || launchArc || launchQueued || motorLocked || vault || ragdoll || landStun;
        }

        /// <summary>
        /// Jump spends coyote and the buffer. A release or the end of the cable keeps both.
        /// Vertical is jumpSpeed, not jumpSpeed plus the cable.
        /// </summary>
        public static ZipLeave LeaveZip(Vector3 ride, float jumpSpeed, bool jumpPressed, bool end, float coyote, float jumpSlot, float wallJumpSlot)
        {
            ZipLeave leave;
            leave.Coyote = coyote;
            leave.JumpSlot = jumpSlot;
            leave.WallJumpSlot = wallJumpSlot;
            leave.Jumped = jumpPressed;
            if (jumpPressed)
            {
                leave.Velocity = ZipLineRules.JumpDrop(ride, jumpSpeed);
                leave.Coyote = 0f;
                leave.JumpSlot = 0f;
                leave.WallJumpSlot = 0f;
                return leave;
            }
            leave.Velocity = end ? ZipLineRules.EndDrop(ride) : ZipLineRules.ReleaseDrop(ride);
            return leave;
        }

        /// <summary>
        /// Tag-back wins the frame: no transfer and no stagger, and lunges stay.
        /// A real tag stuns the victim. A non-tag connect staggers and drops their lunge.
        /// </summary>
        public static HitOrder ResolveHit(bool blockedTag, bool isTag, bool staggerHit, bool attackerLunging, bool victimLunging, out bool attackerLunge, out bool victimLunge)
        {
            attackerLunge = attackerLunging;
            victimLunge = victimLunging;
            if (blockedTag)
                return HitOrder.Blocked;
            if (isTag)
            {
                victimLunge = false;
                return HitOrder.Tagged;
            }
            if (staggerHit)
            {
                victimLunge = false;
                return HitOrder.Staggered;
            }
            return HitOrder.None;
        }

        /// <summary>The old It keeps a zip or a pad arc. The victim's carrier exits.</summary>
        public static void TransferCarriers(ref Carrier oldIt, ref Carrier victim)
        {
            victim.Zip = false;
            victim.Arc = false;
            victim.Grapple = false;
            victim.Lunging = false;
            victim.Velocity = Vector3.zero;
        }

        /// <summary>
        /// No legal target holds. A helping pad wins on the ground.
        /// A pad that does not help does not hide a zip that does.
        /// The same inputs return the same edge.
        /// </summary>
        public static ChaseEdge Choose(bool legalTarget, bool grounded, bool padAhead, bool padHelps, bool zipAhead, bool zipHelps)
        {
            if (!legalTarget) return ChaseEdge.Hold;
            if (grounded && padAhead && padHelps) return ChaseEdge.Pad;
            if (zipAhead && zipHelps) return ChaseEdge.Zip;
            return ChaseEdge.Chase;
        }

        public static Vector3 FiniteOrZero(Vector3 v)
        {
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)) return Vector3.zero;
            if (float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)) return Vector3.zero;
            return v;
        }

        public static bool Finite(Vector3 v)
        {
            if (float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)) return false;
            if (float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)) return false;
            return true;
        }

        public static bool Holds()
        {
            int passed;
            int total;
            string fail;
            Tally(out passed, out total, out fail);
            if (passed != total) return false;
            if (!FeelLocks()) return false;
            if (!Wired()) return false;
            return true;
        }

        public static string ProofLine()
        {
            int passed;
            int total;
            string fail;
            Tally(out passed, out total, out fail);
            string line = "verb-integration cases=" + passed.ToString() + "/" + total.ToString()
                + " passed=" + passed.ToString()
                + " feel=" + (FeelLocks() ? "held" : "drift")
                + " wired=" + (Wired() ? "yes" : "no");
            if (fail != null && fail.Length > 0)
                line += " fail=" + fail;
            return line;
        }

        static void Tally(out int passed, out int total, out string fail)
        {
            bool[] cases = Cases(out string[] names);
            total = cases.Length;
            passed = 0;
            fail = "";
            for (int i = 0; i < cases.Length; i++)
            {
                if (cases[i]) passed++;
                else
                {
                    if (fail.Length > 0) fail += ",";
                    fail += names[i];
                }
            }
        }

        static bool[] Cases(out string[] names)
        {
            names = new string[]
            {
                "padCling",
                "padZip",
                "padGrapple",
                "padSlide",
                "padStagger",
                "padBuffer",
                "zipDash",
                "zipStagger",
                "zipGrapple",
                "zipJump",
                "zipRelease",
                "zipWall",
                "hitOrder",
                "tagZip",
                "tagPad",
                "padBan",
                "exits",
                "dummy",
                "lungeStagger",
                "oneMove"
            };
            var ok = new bool[names.Length];
            const float g = 22f;
            const float dt = 1f / 60f;
            float padVy = LaunchPadRules.VerticalSpeed(LaunchPadRules.DefaultApexMeters, g);
            Vector3 jumpEntry = new Vector3(3f, 24.7f, 4f);

            SameWallLimit.Face west = SameWallLimit.Make(7, new Vector3(1f, 0f, 0f), new Vector3(0f, 1.2f, 0f));
            SameWallLimit.Ban ban = default;
            if (PadLeavesWall(true, false))
                SameWallLimit.NoteLeave(ref ban, west);
            ok[0] = ban.Active
                && SameWallLimit.Blocks(ban, west)
                && !PadLeavesWall(false, false)
                && !ClearsWallBan(false, false, false, true)
                && Mathf.Abs(ApplyPadVelocity(jumpEntry, LaunchPadRules.DefaultApexMeters, g, Vector3.zero, false).y - padVy) < 0.001f;

            Vector3 fromZip = ApplyPadVelocity(new Vector3(1f, -3f, 2f), LaunchPadRules.DefaultApexMeters, g, new Vector3(0f, 0f, 6f), true);
            ok[1] = ZipGrabBlocked(false, false, true, false, false, false, false)
                && GrappleYields(true, true, false)
                && Mathf.Abs(fromZip.y - padVy) < 0.001f
                && Mathf.Abs(fromZip.z - 6f) < 0.001f
                && Finite(fromZip);

            ok[2] = GrappleYields(false, true, false)
                && Finite(ApplyPadVelocity(new Vector3(4f, 1f, 0f), LaunchPadRules.DefaultApexMeters, g, Vector3.zero, false));

            Vector3 slid = ApplyPadVelocity(new Vector3(8f, 1f, 0f), LaunchPadRules.DefaultApexMeters, g, Vector3.zero, false);
            ok[3] = Mathf.Abs(slid.x - 8f) < 0.001f
                && Mathf.Abs(slid.y - padVy) < 0.001f
                && Mathf.Abs(slid.y - (1f + padVy)) > 0.5f;

            ok[4] = Mathf.Abs(PunchStagger.Duration - 0.25f) < 0.001f
                && !ZipGrabBlocked(false, false, false, false, false, false, false)
                && Mathf.Abs(ApplyPadVelocity(Vector3.zero, LaunchPadRules.DefaultApexMeters, g, Vector3.zero, false).y - padVy) < 0.001f;

            Vector3 replaced = ApplyPadVelocity(jumpEntry, LaunchPadRules.DefaultApexMeters, g, Vector3.zero, false);
            float padApex = LaunchPadRules.SimulatedApex(12f, true, LaunchPadRules.DefaultApexMeters, g, 1.5f, 52f, dt);
            float jumpApex = 24.7f * 24.7f / (2f * g);
            float sumVy = 24.7f + padVy;
            float sumApex = sumVy * sumVy / (2f * g);
            ok[5] = Mathf.Abs(replaced.y - padVy) < 0.001f
                && Mathf.Abs(replaced.y - (24.7f + padVy)) > 1f
                && Mathf.Abs(replaced.x - 3f) < 0.001f
                && Mathf.Abs(padApex - LaunchPadRules.DefaultApexMeters) < 0.45f
                && Mathf.Abs(padApex - jumpApex) > 2f
                && Mathf.Abs(padApex - sumApex) > 2f;

            Vector3 ride = ZipLineRules.RideVelocity(new Vector3(15f, 4f, 0f), new Vector3(0f, -0.2f, 1f), ZipLineRules.DefaultRideSpeed);
            ok[6] = !ZipGrabBlocked(false, false, false, false, false, false, false)
                && Mathf.Abs(ride.magnitude - ZipLineRules.DefaultRideSpeed) < 0.02f
                && Mathf.Abs(ride.magnitude - (ZipLineRules.DefaultRideSpeed + 15f)) > 1f;

            ok[7] = ZipGrabBlocked(true, false, false, false, false, false, false)
                && ZipGrabBlocked(false, true, false, false, false, false, false)
                && ZipGrabBlocked(false, false, true, false, false, false, false);

            ok[8] = GrappleYields(true, false, false) && Finite(ride);

            ZipLeave jumped = LeaveZip(ride, 24.7f, true, false, 0.10f, 0.16f, 0.16f);
            ok[9] = jumped.Jumped
                && Mathf.Abs(jumped.Coyote) < 0.0001f
                && Mathf.Abs(jumped.JumpSlot) < 0.0001f
                && Mathf.Abs(jumped.WallJumpSlot) < 0.0001f
                && Mathf.Abs(jumped.Velocity.y - 24.7f) < 0.001f
                && Mathf.Abs(jumped.Velocity.y - (24.7f + ride.y)) > 0.5f
                && Mathf.Abs(jumped.Velocity.x - ride.x) < 0.001f
                && Mathf.Abs(jumped.Velocity.z - ride.z) < 0.001f
                && !KinematicStep.CoyoteJumpAllowed(false, jumped.Coyote);

            ZipLeave released = LeaveZip(ride, 24.7f, false, false, 0.08f, 0.12f, 0f);
            ZipLeave ended = LeaveZip(ride, 24.7f, false, true, 0.08f, 0.12f, 0f);
            ok[10] = !released.Jumped
                && Mathf.Abs(released.Coyote - 0.08f) < 0.0001f
                && Mathf.Abs(released.JumpSlot - 0.12f) < 0.0001f
                && Mathf.Abs(released.Velocity.y - ride.y) < 0.001f
                && !ended.Jumped
                && Mathf.Abs(ended.Coyote - 0.08f) < 0.0001f
                && Mathf.Abs(ended.JumpSlot - 0.12f) < 0.0001f
                && KinematicStep.CoyoteJumpAllowed(false, released.Coyote);

            SameWallLimit.Face east = SameWallLimit.Make(9, new Vector3(-1f, 0f, 0f), new Vector3(6f, 1.2f, 0f));
            SameWallLimit.Ban rideBan = default;
            SameWallLimit.NoteLeave(ref rideBan, west);
            bool still = SameWallLimit.Blocks(rideBan, west);
            bool other = !SameWallLimit.Blocks(rideBan, east);
            bool flightKeeps = !ClearsWallBan(false, false, true, false);
            bool groundedWould = !ClearsWallBan(true, false, true, false);
            ok[11] = still && other && flightKeeps && groundedWould && rideBan.Active;

            bool attackerLunge;
            bool victimLunge;
            HitOrder blocked = ResolveHit(true, false, true, true, true, out attackerLunge, out victimLunge);
            bool blockOk = blocked == HitOrder.Blocked && attackerLunge && victimLunge;
            HitOrder staggered = ResolveHit(false, false, true, true, true, out attackerLunge, out victimLunge);
            bool stagOk = staggered == HitOrder.Staggered && attackerLunge && !victimLunge;
            HitOrder tagged = ResolveHit(false, true, true, true, true, out attackerLunge, out victimLunge);
            bool tagOk = tagged == HitOrder.Tagged && attackerLunge && !victimLunge;
            ok[12] = blockOk && stagOk && tagOk;

            Carrier oldZip = default;
            oldZip.Zip = true;
            oldZip.Velocity = ride;
            Carrier victimZip = oldZip;
            victimZip.Grapple = true;
            TransferCarriers(ref oldZip, ref victimZip);
            TagBackImmunity.Window window = TagBackImmunity.Open(2, TagBackImmunity.DefaultSeconds);
            ok[13] = oldZip.Zip
                && !victimZip.Zip
                && !victimZip.Grapple
                && Finite(victimZip.Velocity)
                && victimZip.Velocity.sqrMagnitude < 0.0001f
                && TagBackImmunity.Blocks(window, 2)
                && !TagBackImmunity.Blocks(window, 3);

            Carrier oldPad = default;
            oldPad.Arc = true;
            oldPad.Velocity = new Vector3(0f, padVy, 0f);
            Carrier victimPad = oldPad;
            victimPad.Lunging = true;
            TransferCarriers(ref oldPad, ref victimPad);
            ok[14] = oldPad.Arc && !victimPad.Arc && !victimPad.Lunging && Finite(oldPad.Velocity) && Finite(victimPad.Velocity);

            bool flight = !ClearsWallBan(true, false, false, true);
            bool zipFlight = !ClearsWallBan(true, false, true, false);
            bool landed = ClearsWallBan(true, false, false, false);
            bool arcEnded = EndLaunchArc(true, true, 0f);
            bool arcHolds = !EndLaunchArc(true, false, padVy);
            ok[15] = flight && zipFlight && landed && arcEnded && arcHolds;

            Vector3 nan = FiniteOrZero(new Vector3(float.NaN, 1f, 0f));
            Vector3 inf = FiniteOrZero(new Vector3(0f, float.PositiveInfinity, 0f));
            bool both = GrappleYields(true, false, true);
            ok[16] = nan.sqrMagnitude < 0.0001f
                && inf.sqrMagnitude < 0.0001f
                && both
                && Finite(ride)
                && Finite(fromZip);

            ok[17] = DummyStable();
            ok[18] = staggered == HitOrder.Staggered && !victimLunge && blocked == HitOrder.Blocked;
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            ok[19] = motor != null && Count(motor, "_cc.Move(") == 1;
            return ok;
        }

        static bool DummyStable()
        {
            const int oldIt = 1;
            const int other = 2;
            int picked = TagBackImmunity.DummyPick(oldIt, other, true, false);
            if (picked != other) return false;
            int none = TagBackImmunity.DummyPick(oldIt, other, true, true);
            if (none != 0) return false;

            for (int i = 0; i < 8; i++)
            {
                if (Choose(false, true, true, true, true, true) != ChaseEdge.Hold) return false;
                if (Choose(false, i % 2 == 0, true, false, true, true) != ChaseEdge.Hold) return false;
                if (Choose(true, true, true, true, true, true) != ChaseEdge.Pad) return false;
                if (Choose(true, false, true, true, true, true) != ChaseEdge.Zip) return false;
                float strafe = i % 2 == 0 ? 1f : -1f;
                OpponentChaseInput zipOnly = Blank();
                zipOnly.Grounded = true;
                zipOnly.PadAhead = true;
                zipOnly.PadHelps = false;
                zipOnly.ZipAhead = true;
                zipOnly.ZipHelps = true;
                zipOnly.PathStrafe = strafe;
                zipOnly.PlanarDistance = 20f;
                OpponentChaseWish take = OpponentChaseSteer.Decide(zipOnly);
                if (take.Verb != OpponentChaseVerb.ZipTake || take.Jump || take.Lunge) return false;

                OpponentChaseInput both = zipOnly;
                both.PadHelps = true;
                OpponentChaseWish pad = OpponentChaseSteer.Decide(both);
                if (pad.Verb != OpponentChaseVerb.PadTake) return false;

                OpponentChaseInput miss = zipOnly;
                miss.ZipHelps = false;
                miss.ZipAhead = false;
                OpponentChaseWish peel = OpponentChaseSteer.Decide(miss);
                if (peel.Verb == OpponentChaseVerb.PadTake || peel.Verb == OpponentChaseVerb.ZipTake) return false;
            }
            return true;
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
            if (Mathf.Abs(PunchStagger.Duration - 0.25f) > 0.001f) return false;
            if (Mathf.Abs(PunchStagger.Immunity - 0.50f) > 0.001f) return false;
            if (PunchStagger.Knockback != 0f) return false;
            if (Mathf.Abs(LaunchPadRules.DefaultCooldown - 0.3f) > 0.001f) return false;
            if (Mathf.Abs(ZipLineRules.DefaultRideSpeed - 14f) > 0.001f) return false;
            if (Mathf.Abs(ZipLineRules.DefaultRegrabCooldown - 0.3f) > 0.001f) return false;
            if (Mathf.Abs(TagBackImmunity.DefaultSeconds - 1.0f) > 0.001f) return false;
            PunchTagTuning punch = ScriptableObject.CreateInstance<PunchTagTuning>();
            if (punch == null || Mathf.Abs(punch.reach - 1.55f) > 0.001f) return false;
            if (Mathf.Abs(punch.tagBackImmunity - 1.0f) > 0.001f) return false;
            string lunge = Read("Assets/Scripts/Art/OpponentLungeTell.cs");
            if (lunge == null || lunge.IndexOf("LeadSeconds = 0.45f", StringComparison.Ordinal) < 0)
                return false;
            return true;
        }

        static bool Wired()
        {
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            string steer = Read("Assets/Scripts/Modes/OpponentChaseSteer.cs");
            string patrol = Read("Assets/Scripts/Modes/DummyPatrol.cs");
            string zip = Read("Assets/Scripts/Level/ZipLineRules.cs");
            string self = Read("Assets/Scripts/Level/VerbIntegration.cs");
            string pad = Read("Assets/Scripts/Level/LaunchPad.cs");
            string line = Read("Assets/Scripts/Level/ZipLine.cs");
            string tuning = Read("Assets/Scripts/Tag/PunchTagTuning.cs");
            if (motor == null || steer == null || patrol == null || self == null || tuning == null)
                return false;
            if (Count(motor, "_cc.Move(") != 1) return false;
            if (motor.IndexOf("FiniteOrZero", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("ClearsWallBan", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("GrappleYields", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("ZipGrabBlocked", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("LeaveZip", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("ApplyPadVelocity", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("DropCarrierVerbs", StringComparison.Ordinal) < 0) return false;
            if (motor.IndexOf("_airJumpsFromFatigue", StringComparison.Ordinal) >= 0) return false;
            string apply = Method(motor, "Vector3 ApplyQueuedLaunch");
            if (apply.IndexOf("ClearWallBan", StringComparison.Ordinal) >= 0) return false;
            if (apply.IndexOf("BanLeftWall", StringComparison.Ordinal) < 0) return false;
            if (apply.IndexOf("_cc.Move", StringComparison.Ordinal) >= 0) return false;
            string zipApply = Method(motor, "Vector3 ApplyZipRide");
            if (zipApply.IndexOf("ClearWallBan", StringComparison.Ordinal) >= 0) return false;
            if (zipApply.IndexOf("LeaveZip", StringComparison.Ordinal) < 0) return false;
            if (self.IndexOf("JumpDrop", StringComparison.Ordinal) < 0) return false;
            if (self.IndexOf("ReleaseDrop", StringComparison.Ordinal) < 0) return false;
            if (self.IndexOf("EndDrop", StringComparison.Ordinal) < 0) return false;
            string begin = Method(motor, "bool BeginPunchStagger");
            if (begin.IndexOf("_lungeT = 0f", StringComparison.Ordinal) < 0) return false;
            if (begin.IndexOf("_launchArc = false", StringComparison.Ordinal) >= 0) return false;
            if (begin.IndexOf("_velocity", StringComparison.Ordinal) >= 0) return false;
            string lunge = Method(motor, "bool TryLunge");
            if (lunge.IndexOf("_stagger.Stagger", StringComparison.Ordinal) < 0) return false;
            if (steer.IndexOf("VerbIntegration.Choose", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf("VerbIntegration.Choose", StringComparison.Ordinal) < 0) return false;
            if (patrol.IndexOf(".Move(", StringComparison.Ordinal) >= 0) return false;
            if (tuning.IndexOf("Tooltip", StringComparison.Ordinal) < 0) return false;
            if (tuning.IndexOf("tagBackImmunity", StringComparison.Ordinal) < 0) return false;
            if (pad != null && pad.IndexOf("_cc.Move", StringComparison.Ordinal) >= 0) return false;
            if (line != null && line.IndexOf("_cc.Move", StringComparison.Ordinal) >= 0) return false;
            if (zip == null) return false;
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
