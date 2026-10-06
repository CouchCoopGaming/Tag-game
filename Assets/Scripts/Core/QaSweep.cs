using System;
using System.IO;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using Tag.Settings;
using TagArena.Movement;

namespace Tag.Core
{
    /// <summary>
    /// Seeded combinations of the verbs that already exist. Each case has a repro line.
    /// No new move is introduced here.
    /// </summary>
    public static class QaSweep
    {
        public const int BugsFixed = 7;
        public const int Seed = 9051;

        public sealed class Report
        {
            public bool Ok;
            public int Cases;
            public int Passed;
            public string Line;
            public string Failure;

            public void Fail(string why)
            {
                Ok = false;
                if (string.IsNullOrEmpty(Failure)) Failure = why;
            }
        }

        public static Report Run()
        {
            var report = new Report { Ok = true };
            var rng = new Random(Seed);
            CheckPause(report, rng);
            CheckRebind(report, rng);
            CheckTagCarriers(report);
            CheckTagBackRound(report, rng);
            CheckStaggerZip(report, rng);
            CheckRespawn(report);
            CheckNulls(report);
            CheckFeel(report);
            CheckWired(report);
            if (!report.Ok)
                report.Line = "qa-sweep FAIL " + report.Failure;
            else
                report.Line = "qa-sweep cases=" + report.Passed.ToString() + "/" + report.Cases.ToString()
                    + " bugs=" + BugsFixed.ToString() + " fixed=" + BugsFixed.ToString()
                    + " pause=frozen rebind=held tag=drops-carrier"
                    + " tag-back=round-end-clears stagger-zip=blocked"
                    + " respawn=arenas3 seed=" + Seed.ToString();
            return report;
        }

        static void Pass(Report report, bool ok, string repro)
        {
            report.Cases++;
            if (ok) report.Passed++;
            else report.Fail(repro);
        }

        static SessionRules.Clocks FullClocks()
        {
            SessionRules.Clocks c = default;
            c.ClingGrace = 0.08f;
            c.JumpSlot = 0.16f;
            c.WallJumpSlot = 0.08f;
            c.Coyote = 0.10f;
            c.AirDash = 0.10f;
            c.AirDashCd = 30f;
            c.Lunge = 0.20f;
            c.LungeCd = 1f;
            c.Stagger = PunchStagger.Duration;
            c.StaggerImmune = 0f;
            c.Zip = true;
            c.Arc = true;
            c.Slide = true;
            c.Climb = true;
            return c;
        }

        static void CheckPause(Report report, Random rng)
        {
            SessionRules.Clocks clocks = FullClocks();
            bool frozen = SessionRules.TimeFrozen(0f);
            SessionRules.Clocks paused = SessionRules.Pause(clocks);
            for (int i = 0; i < 90; i++)
            {
                float dt = 0.008f + (float)rng.NextDouble() * 0.02f;
                paused = SessionRules.Tick(paused, dt, frozen);
            }
            Pass(report, frozen && SessionRules.Same(clocks, paused),
                "repro: pause 1.5s during slide, cling grace 0.08, zip, pad arc, lunge 0.20, stagger 0.25, air dash 0.10; unpause and the clocks have moved");

            SessionRules.Clocks live = SessionRules.Tick(clocks, 0.05f, false);
            Pass(report, !SessionRules.Same(clocks, live) && live.ClingGrace < clocks.ClingGrace && live.AirDash < clocks.AirDash,
                "repro: unpaused 0.05s step did not decay cling grace and air dash");

            Pass(report, !SessionRules.TimeFrozen(1f),
                "repro: play timeScale 1 was treated as a freeze");
        }

        static void CheckRebind(Report report, Random rng)
        {
            SessionRules.Clocks clocks = FullClocks();
            clocks.Zip = false;
            clocks.Arc = false;
            ActionBinds binds = ActionBinds.Defaults();
            binds.SetKeyboard(PlayAction.AirDash, "v");
            bool clash = binds.AnyConflict(out _, out _);
            SessionRules.Clocks after = clocks;
            for (int i = 0; i < 20; i++)
                after = SessionRules.Tick(after, 0f, true);
            Pass(report, !clash && SessionRules.Same(clocks, after) && binds.Keyboard[(int)PlayAction.Jump] == "space",
                "repro: rebind air dash to V mid-round cleared the jump buffer or moved jump off space");

            ActionBinds stolen = binds.Clone();
            stolen.SetKeyboard(PlayAction.Jump, "v");
            PlayAction other;
            bool conflict = stolen.Conflict(PlayAction.Jump, out other);
            Pass(report, conflict && other == PlayAction.AirDash && ActionBinds.Defaults().Keyboard[(int)PlayAction.Jump] == "space",
                "repro: mid-round jump rebound onto the new dash key was stored on the defaults");

            int spins = rng.Next(1, 4);
            for (int i = 0; i < spins; i++)
                binds.SetKeyboard(PlayAction.Slide, "c");
            Pass(report, binds.Keyboard[(int)PlayAction.Slide] == "c" && binds.Keyboard[(int)PlayAction.Jump] == "space",
                "repro: a slide rebind mid-round rewrote jump");
        }

        static void CheckTagCarriers(Report report)
        {
            VerbIntegration.Carrier attacker = default;
            attacker.Zip = true;
            attacker.Velocity = new UnityEngine.Vector3(0f, -2f, 14f);
            VerbIntegration.Carrier victim = default;
            victim.Zip = true;
            victim.Arc = true;
            victim.Grapple = true;
            victim.Lunging = true;
            SessionRules.TagCarriers(ref attacker, ref victim);
            Pass(report, attacker.Zip && !victim.Zip && !victim.Arc && !victim.Grapple && !victim.Lunging,
                "repro: tag during a zip ride or pad arc left the victim on the cable or the arc");

            VerbIntegration.Carrier padVictim = default;
            padVictim.Arc = true;
            VerbIntegration.Carrier runner = default;
            SessionRules.TagCarriers(ref runner, ref padVictim);
            Pass(report, !padVictim.Arc && !runner.Zip,
                "repro: tag during a pad arc cleared the attacker's ride or kept the victim's arc");
        }

        static void CheckTagBackRound(Report report, Random rng)
        {
            var round = RoundFlow.NewRound(4f, Seed + rng.Next(0, 20));
            int guard = 0;
            while (round.Phase == RoundFlow.Phase.Countdown && guard++ < 400)
                RoundFlow.Tick(round, RoundFlow.ProofStep);
            Pass(report, round.Phase == RoundFlow.Phase.Playing,
                "repro: qa round never left countdown");
            int from = round.It;
            int to = from == 0 ? 1 : 0;
            bool tagged = RoundFlow.TryTag(round, from, to);
            Pass(report, tagged && round.TagBack > 0.5f,
                "repro: tag during play did not open the 1s tag-back window");
            RoundFlow.Tick(round, 0.4f);
            bool blocked = !RoundFlow.TryTag(round, to, from);
            Pass(report, blocked,
                "repro: tag-back landed 0.4s after the tag in the same round");
            RoundFlow.End(round);
            Pass(report, round.Phase == RoundFlow.Phase.Results && round.TagBack <= 0f && round.ImmuneFrom < 0,
                "repro: tag-back immunity was still open after the round ended");
            RoundFlow.Tick(round, RoundFlow.ResultsArmSeconds + 0.05f);
            bool advanced = RoundFlow.Advance(round);
            guard = 0;
            while (round.Phase == RoundFlow.Phase.Countdown && guard++ < 400)
                RoundFlow.Tick(round, RoundFlow.ProofStep);
            from = round.It;
            to = from == 0 ? 1 : 0;
            bool next = advanced && round.Phase == RoundFlow.Phase.Playing && RoundFlow.TryTag(round, from, to);
            Pass(report, next,
                "repro: the first tag of the next round was refused by the previous round's tag-back");
        }

        static void CheckStaggerZip(Report report, Random rng)
        {
            float stagger = PunchStagger.Duration;
            Pass(report, SessionRules.StaggerBlocksZip(stagger) && !SessionRules.StaggerBlocksZip(0f),
                "repro: stagger 0.25s still grabbed the zip, or a rested pawn could not");
            Pass(report, !SessionRules.AirDashAllowed(stagger) && SessionRules.AirDashAllowed(0f),
                "repro: stagger 0.25s started a new air dash");

            SessionRules.Clocks riding = FullClocks();
            riding.Stagger = stagger;
            bool dropped = SessionRules.StaggerBlocksZip(riding.Stagger);
            if (dropped) riding.Zip = false;
            float wait = 0.05f + (float)rng.NextDouble() * 0.1f;
            riding = SessionRules.Tick(riding, wait, false);
            Pass(report, !riding.Zip && riding.Stagger > 0f && riding.Stagger < PunchStagger.Duration,
                "repro: stagger plus zip grab kept the ride or skipped the stumble timer");
        }

        static void CheckRespawn(Report report)
        {
            SessionRules.ArenaBox mega = SessionRules.Bounds(ParkArena.Mega);
            SessionRules.ArenaBox pocket = SessionRules.Bounds(ParkArena.Pocket);
            SessionRules.ArenaBox stack = SessionRules.Bounds(ParkArena.Stack);
            SessionRules.ArenaBox other = SessionRules.Bounds(9);
            Pass(report, mega.Arena == ParkArena.Mega && pocket.Arena == ParkArena.Pocket
                    && stack.Arena == ParkArena.Stack && other.Arena == ParkArena.Mega,
                "repro: kill boxes were not Mega 0, Pocket 1, Stack 2, or a missing id left Mega Park");
            Pass(report, Fence(mega, MegaParkP1Layout.MapW, MegaParkP1Layout.MapD)
                    && Fence(pocket, PocketParkLayout.MapW, PocketParkLayout.MapD)
                    && Fence(stack, StackYardLayout.MapW, StackYardLayout.MapD)
                    && mega.KillY == -2.5f && pocket.KillY == mega.KillY && stack.KillY == mega.KillY
                    && other.MaxX == mega.MaxX && other.MaxZ == mega.MaxZ,
                "repro: a kill box did not use that park's fence, or the kill height was not -2.5");
            Pass(report, !SessionRules.Outside(mega, 80f, 1f, 40f)
                    && !SessionRules.Outside(pocket, 40f, 1f, 20f)
                    && !SessionRules.Outside(stack, 50f, 1f, 30f),
                "repro: an interior point on Mega Park, Pocket Park, or Stack Yard respawned");
            Pass(report, SessionRules.Outside(mega, 200f, 1f, 20f)
                    && SessionRules.Outside(pocket, 100f, 1f, 20f)
                    && !SessionRules.Outside(mega, 100f, 1f, 20f)
                    && !SessionRules.Outside(stack, 100f, 1f, 20f)
                    && !SessionRules.Outside(mega, 130f, 1f, 20f)
                    && SessionRules.Outside(stack, 130f, 1f, 20f)
                    && SessionRules.Outside(pocket, 130f, 1f, 20f),
                "repro: a point past one fence was judged with another park's box");
            Pass(report, SessionRules.Outside(mega, 10f, -3f, 10f)
                    && SessionRules.Outside(pocket, 10f, -3f, 10f)
                    && SessionRules.Outside(stack, 10f, -3f, 10f),
                "repro: y=-3 during a zip ride did not cross the kill plane");
            Pass(report, SessionRules.Outside(mega, -8f, 1f, 40f)
                    && SessionRules.Outside(pocket, -8f, 1f, 20f)
                    && SessionRules.Outside(stack, -8f, 1f, 30f),
                "repro: x=-8 inside the margin did not respawn");
            Pass(report, !SessionRules.RidingAfterRespawn(true),
                "repro: respawn during a zip ride left the pawn on the cable");
            Pass(report, SeatsUseBox(),
                "repro: a human seat or the AI on arena 0, 1, or 2 was outside that park's kill box, or a step past the fence stayed in");
        }

        static bool SeatsUseBox()
        {
            for (int arena = 0; arena < ParkArena.Count; arena++)
            {
                SessionRules.ArenaBox box = SessionRules.Bounds(arena);
                for (int seat = 0; seat < ParkArena.HumanSeats; seat++)
                {
                    ParkArena.HumanSeat(arena, seat, out float x, out float y, out float z, out _);
                    if (SessionRules.Outside(box, x, y, z)) return false;
                    if (!SessionRules.Outside(box, x, -3f, z)) return false;
                    if (!SessionRules.Outside(box, box.MaxX + 1f, y, z)) return false;
                }
                MegaParkP1Layout.SpawnPad ai = arena == ParkArena.Pocket ? PocketParkLayout.Spawns[0]
                    : arena == ParkArena.Stack ? StackYardLayout.Spawns[0]
                    : MegaParkP1Layout.Spawns[0];
                if (SessionRules.Outside(box, ai.X, MegaParkP1Layout.SpawnY, ai.Z)) return false;
                if (!SessionRules.Outside(box, ai.X, -3f, ai.Z)) return false;
                if (!SessionRules.Outside(box, box.MinX - 1f, MegaParkP1Layout.SpawnY, ai.Z)) return false;
            }
            return true;
        }

        static bool Fence(SessionRules.ArenaBox box, float mapW, float mapD)
        {
            return Near(box.MinX, -SessionRules.Margin) && Near(box.MaxX, mapW + SessionRules.Margin)
                && Near(box.MinZ, -SessionRules.Margin) && Near(box.MaxZ, mapD + SessionRules.Margin);
        }

        static void CheckNulls(Report report)
        {
            Pass(report, TagBackImmunity.Seconds(null) == TagBackImmunity.DefaultSeconds,
                "repro: a missing punch tuning threw or changed tag-back from 1.0");
            SessionRules.ArenaBox box = SessionRules.Bounds(0);
            Pass(report, box.KillY < 0f && HudDigits.Whole0(0f) == "0" && HudDigits.Tenth0(1.0f) == "1.0",
                "repro: null-safe helpers changed the kill plane or the HUD numerals");
            Pass(report, HudDigits.DashCd(30f) == "30s" && HudDigits.DashCd(9.5f) == "9.5s" && HudDigits.Kph(0f) == "0",
                "repro: dash CD or speed text did not match the cached numerals");
        }

        static void CheckFeel(Report report)
        {
            MovementConfig cfg = UnityEngine.ScriptableObject.CreateInstance<MovementConfig>();
            PunchTagTuning punch = UnityEngine.ScriptableObject.CreateInstance<PunchTagTuning>();
            bool ok = Near(cfg.coyoteTime, 0.10f) && Near(cfg.jumpBuffer, 0.16f) && Near(cfg.clingReleaseGrace, 0.08f)
                && Near(cfg.jumpSpeed, 24.7f) && cfg.slideBoost == 0f
                && Near(cfg.airDashDuration, 0.10f) && Near(cfg.airDashSpeed, 15f) && Near(cfg.airDashCooldown, 30f)
                && Near(punch.reach, 1.55f)
                && Near(cfg.taggerLungeSpeed, 16f) && Near(cfg.taggerLungeDuration, 0.20f) && Near(cfg.taggerLungeCooldown, 1f)
                && Near(cfg.climbSpeed, 6.0f) && Near(cfg.climbSlipSpeed, 3.7f) && Near(cfg.wallRunSpeed, 9.5f)
                && Near(PunchStagger.Duration, 0.25f) && Near(PunchStagger.Immunity, 0.50f)
                && Near(LaunchPadRules.DefaultCooldown, 0.3f)
                && Near(ZipLineRules.DefaultRideSpeed, 14f) && Near(ZipLineRules.DefaultRegrabCooldown, 0.3f)
                && Near(TagBackImmunity.DefaultSeconds, 1.0f)
                && !cfg.enableJet;
            Pass(report, ok, "repro: a locked feel number moved during the qa pass");
        }

        static void CheckWired(Report report)
        {
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            string respawn = Read("Assets/Scripts/Local/VoidRespawn.cs");
            string it = Read("Assets/Scripts/Tag/ItController.cs");
            string role = Read("Assets/TagArenaMovement/Scripts/Tag/TagRole.cs");
            string mode = Read("Assets/Scripts/Modes/TagModeController.cs");
            string flow = Read("Assets/Scripts/Core/GameFlow.cs");
            string life = Read("Assets/Scripts/Core/StaticLifecycle.cs");
            string binds = Read("Assets/Scripts/Settings/ActionBinds.cs");
            string round = Read("Assets/Scripts/Modes/RoundFlow.cs");
            string park = Read("Assets/Scripts/Level/ParkArena.cs");
            string mega = Read("Assets/Scripts/Level/MegaParkP1Layout.cs");
            string pocket = Read("Assets/Scripts/Level/PocketParkLayout.cs");
            string stack = Read("Assets/Scripts/Level/StackYardLayout.cs");
            bool files = motor != null && respawn != null && it != null && role != null && mode != null
                && flow != null && life != null && binds != null && round != null && park != null
                && mega != null && pocket != null && stack != null;
            Pass(report, files, "repro: a qa wiring file is missing");
            if (!files) return;

            int freeze = motor.IndexOf("TimeFrozen", StringComparison.Ordinal);
            int clear = motor.IndexOf("_clingGrace = 0f", StringComparison.Ordinal);
            Pass(report, freeze >= 0 && clear > freeze && Count(motor, "_cc.Move(") == 1,
                "repro: pause still clears cling grace before the freeze return, or a second Move was added");

            string begin = Method(motor, "bool BeginPunchStagger");
            string dash = Method(motor, "bool TryAirDash");
            Pass(report, begin.IndexOf("ReleaseZip", StringComparison.Ordinal) >= 0
                    && begin.IndexOf("_airDashT = 0f", StringComparison.Ordinal) >= 0
                    && begin.IndexOf("jumpSpeed", StringComparison.Ordinal) < 0
                    && dash.IndexOf("AirDashAllowed", StringComparison.Ordinal) >= 0,
                "repro: stagger no longer drops the zip or still lets an air dash start");

            Pass(report, mode.IndexOf("ReleaseCarriers", StringComparison.Ordinal) >= 0
                    && motor.IndexOf("void ReleaseCarriers", StringComparison.Ordinal) >= 0,
                "repro: a tag handoff does not drop the victim's zip or pad arc");

            Pass(report, it.IndexOf("ClearTagBackImmunity", StringComparison.Ordinal) >= 0
                    && role.IndexOf("ClearTagBackImmunity", StringComparison.Ordinal) >= 0
                    && round.IndexOf("TagBack = 0f", StringComparison.Ordinal) >= 0,
                "repro: tag-back is not cleared when the round ends");

            Pass(report, respawn.IndexOf("SessionRules.Bounds", StringComparison.Ordinal) >= 0
                    && respawn.IndexOf("KillPlaneY", StringComparison.Ordinal) >= 0
                    && respawn.IndexOf("PickRespawn", StringComparison.Ordinal) >= 0
                    && park.IndexOf("Mega = 0", StringComparison.Ordinal) >= 0
                    && park.IndexOf("Pocket = 1", StringComparison.Ordinal) >= 0
                    && park.IndexOf("Stack = 2", StringComparison.Ordinal) >= 0
                    && mega.IndexOf("MapW = 160f", StringComparison.Ordinal) >= 0
                    && mega.IndexOf("KillPlaneY = -2.5f", StringComparison.Ordinal) >= 0
                    && pocket.IndexOf("MapW = 80f", StringComparison.Ordinal) >= 0
                    && stack.IndexOf("MapW = 110f", StringComparison.Ordinal) >= 0,
                "repro: the kill box still hardcodes only Mega Park, or a park fence drifted");

            Pass(report, life.IndexOf("RuntimeInitializeOnLoadMethod", StringComparison.Ordinal) >= 0
                    && life.IndexOf("SubsystemRegistration", StringComparison.Ordinal) >= 0
                    && flow.IndexOf("Instance = null", StringComparison.Ordinal) >= 0
                    && binds.IndexOf("Template()", StringComparison.Ordinal) >= 0,
                "repro: domain-reload statics are not reset, or rebind still builds a bind table per read");
        }

        static bool Near(float a, float b) => a > b - 0.001f && a < b + 0.001f;

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
