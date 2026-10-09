using System;
using System.IO;
using Tag.Front;
using Tag.Level;
using Tag.MatchStats;
using Tag.Practice;
using Tag.Profiles;
using Tag.Settings;
using TagArena.Movement;

namespace Tag.Core
{
    /// <summary>
    /// Third sweep over practice, match stats, and local profiles.
    /// Each case is a repro that failed before the fix. Feel locks are not written.
    /// </summary>
    public static class QaSweep3
    {
        public const int BugsFound = 27;
        public const int BugsFixed = 27;

        public sealed class Report
        {
            public bool Ok = true;
            public int Cases;
            public int Passed;
            public string Line = "";
            public string Failure = "";

            public void Fail(string why)
            {
                Ok = false;
                if (Failure.Length == 0) Failure = why;
            }
        }

        public static Report Run()
        {
            var report = new Report();
            GameSettings backup = GameSettings.Defaults();
            if (GameSettings.Current != null) backup.CopyFrom(GameSettings.Current);
            else GameSettings.Current = GameSettings.Defaults();
            int savedArena = ParkArena.Id;
            bool savedChoice = ParkArena.HasExplicitChoice;
            ParkArena.Select(ParkArena.Mega);
            try
            {
                CheckCarriers(report);
                CheckGates(report);
                CheckPaused(report);
                CheckAbort(report);
                CheckGhostArena(report);
                CheckGhostProfile(report);
                CheckGhostCorrupt(report);
                CheckGhostOld(report);
                CheckRebind(report);
                CheckHotplug(report);
                CheckDummyIt(report);
                CheckLeavePractice(report);
                CheckRematch(report);
                CheckRoundRing(report);
                CheckRoster(report);
                CheckEarlyTag(report);
                CheckRespawnCut(report);
                CheckOnePawn(report);
                CheckAwards(report);
                CheckDeleteSeated(report);
                CheckRename(report);
                CheckScroll(report);
                CheckMigrate(report);
                CheckGuest(report);
                CheckItColor(report);
                CheckAtomic(report);
                CheckBinds(report);
            }
            finally
            {
                Restore(backup);
                ParkArena.Select(savedArena);
                ParkArena.HasExplicitChoice = savedChoice;
            }

            if (!report.Ok)
                report.Line = "qa-sweep-3 FAIL " + report.Failure;
            else
                report.Line = "qa-sweep-3 cases=" + report.Passed + "/" + report.Cases
                    + " bugs=" + BugsFound
                    + " found=" + BugsFound
                    + " fixed=" + BugsFixed
                    + " leftovers=0";
            if (report.Ok && report.Passed != report.Cases)
            {
                report.Ok = false;
                report.Failure = "case tally drifted";
                report.Line = "qa-sweep-3 FAIL " + report.Failure;
            }
            if (report.Ok && report.Cases != BugsFound)
            {
                report.Ok = false;
                report.Failure = "bug tally drifted";
                report.Line = "qa-sweep-3 FAIL " + report.Failure;
            }
            return report;
        }

        static void CheckCarriers(Report report)
        {
            SessionRules.Clocks clocks = default;
            clocks.Zip = true;
            clocks.Arc = true;
            clocks.Slide = true;
            clocks.Climb = true;
            clocks.Stagger = 0.25f;
            clocks.StaggerImmune = 0.50f;
            clocks.AirDash = 0.10f;
            clocks.Lunge = 0.20f;
            clocks.JumpSlot = 0.16f;
            clocks.WallJumpSlot = 0.16f;
            clocks.ClingGrace = 0.08f;
            clocks = SessionRules.ClearCarriers(clocks);
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            int halt = motor == null ? -1 : motor.IndexOf("public void Halt()", StringComparison.Ordinal);
            string slice = halt < 0 ? "" : motor.Substring(halt, Math.Min(700, motor.Length - halt));
            bool cleared = !clocks.Zip && !clocks.Arc && !clocks.Slide && !clocks.Climb
                && clocks.Stagger == 0f && clocks.StaggerImmune == 0f
                && clocks.AirDash == 0f && clocks.Lunge == 0f
                && clocks.JumpSlot == 0f && clocks.ClingGrace == 0f
                && slice.IndexOf("_stagger = default", StringComparison.Ordinal) >= 0
                && slice.IndexOf("_wallRunT = 0f", StringComparison.Ordinal) >= 0
                && slice.IndexOf("_slideT = 0f", StringComparison.Ordinal) >= 0
                && slice.IndexOf("_climbT = 0f", StringComparison.Ordinal) >= 0;
            Pass(report, cleared, "repro: restart mid-zip, pad, wall-run, or stagger left the carrier running");
        }

        static void CheckGates(Report report)
        {
            var gates = new PracticeGate[3];
            gates[1].X = 0f;
            gates[1].Y = 0.9f;
            gates[1].Z = 0f;
            gates[1].R = 5f;
            gates[2].X = 1f;
            gates[2].Y = 0.9f;
            gates[2].Z = 0f;
            gates[2].R = 5f;
            int once = PracticeGates.Step(gates, 1, 0f, 0.9f, 0f, true);
            int twice = PracticeGates.Step(gates, once, 0f, 0.9f, 0f, true);
            string sim = Read("Assets/Scripts/Practice/PracticeSim.cs");
            string play = Read("Assets/Scripts/Practice/PracticePlay.cs");
            bool one = once == 2 && twice == 3
                && sim != null && sim.IndexOf("PracticeGates.Step", StringComparison.Ordinal) >= 0
                && play != null && play.IndexOf("PracticeGates.Step", StringComparison.Ordinal) >= 0
                && sim.IndexOf("while (Next <", StringComparison.Ordinal) < 0;
            Pass(report, one, "repro: overlapping checkpoints both counted on one sample");
        }

        static void CheckPaused(Report report)
        {
            bool refused = !PracticeScore.Commit(true, false, true, 2f, 0f);
            string play = Read("Assets/Scripts/Practice/PracticePlay.cs");
            string mode = Read("Assets/Scripts/Modes/TagModeController.cs");
            string rumble = Read("Assets/Scripts/Settings/PadRumbleOutput.cs");
            bool wired = play != null && play.IndexOf("timeScale", StringComparison.Ordinal) >= 0
                && play.IndexOf("PracticeScore.Commit", StringComparison.Ordinal) >= 0
                && mode != null && mode.IndexOf("PadRumble.Silence()", StringComparison.Ordinal) >= 0
                && mode.IndexOf("AudioMix.SetWorldPaused(true)", StringComparison.Ordinal) >= 0
                && mode.IndexOf("AudioMix.SetWorldPaused(false)", StringComparison.Ordinal) >= 0
                && rumble != null && rumble.IndexOf("CaptionFeed.Decay", StringComparison.Ordinal) >= 0;
            Pass(report, refused && wired, "repro: a paused practice run still sealed a best, or pause dropped rumble, captions, or world audio");
        }

        static void CheckAbort(Report report)
        {
            bool abort = !PracticeScore.Commit(true, true, false, 1f, 0f);
            bool worse = !PracticeScore.Commit(true, false, false, 5f, 4f);
            bool better = PracticeScore.Commit(true, false, false, 3f, 4f);
            bool open = !PracticeScore.Commit(false, false, false, 1f, 0f);
            string sim = Read("Assets/Scripts/Practice/PracticeSim.cs");
            bool used = sim != null && sim.IndexOf("PracticeScore.Commit", StringComparison.Ordinal) >= 0;
            Pass(report, abort && worse && better && open && used, "repro: abort or a slower run overwrote the personal best");
        }

        static void CheckGhostArena(Report report)
        {
            PracticeCatalog.Load();
            PracticeGhost.Clear();
            PracticeGhost.ClearSaved();
            string[] routes =
            {
                "mega-beginner", "mega-wall", "mega-toy",
                "pocket-beginner", "pocket-toy",
                "stack-beginner", "stack-toy",
            };
            int[] home = { ParkArena.Mega, ParkArena.Mega, ParkArena.Mega, ParkArena.Pocket, ParkArena.Pocket, ParkArena.Stack, ParkArena.Stack };
            for (int i = 0; i < routes.Length; i++)
            {
                PracticeGhost.Clear();
                PracticeGhost.Offer(0f, 1f, 2f, 3f, 0f, PracticeVerb.Jump);
                PracticeGhost.Offer(0.05f, 4f, 5f, 6f, 0f, PracticeVerb.Jump);
                PracticeGhost.Keep(routes[i]);
            }
            bool matched = true;
            for (int i = 0; i < routes.Length; i++)
            {
                if (!PracticeCatalog.Playable(routes[i], home[i])) matched = false;
                if (!PracticeCatalog.Playable(routes[i], ParkArena.NameOf(home[i]))) matched = false;
                for (int arena = ParkArena.Mega; arena <= ParkArena.Stack; arena++)
                {
                    if (arena == home[i]) continue;
                    if (PracticeCatalog.Playable(routes[i], arena)) matched = false;
                }
            }
            bool away = !PracticeCatalog.Playable("mega-beginner", "PARK");
            bool unknown = !PracticeCatalog.Playable("mega-beginner", 9);
            string view = Read("Assets/Scripts/Practice/PracticePlay.cs");
            bool wired = view != null && view.IndexOf("Playable", StringComparison.Ordinal) >= 0;
            Pass(report, matched && away && unknown && wired, "repro: the practice ghost kept playing after the arena changed");
        }

        static void CheckGhostProfile(Report report)
        {
            LocalProfiles.Clear();
            PracticeBests.Clear();
            PracticeGhost.Clear();
            PracticeGhost.ClearSaved();
            int ada = LocalProfiles.Create("Ada");
            int bea = LocalProfiles.Create("Bea");
            bool seated = LocalProfiles.TrySeat(0, ada);
            string[] routes =
            {
                "mega-beginner", "mega-wall", "mega-toy",
                "pocket-beginner", "pocket-toy",
                "stack-beginner", "stack-toy",
            };
            for (int i = 0; i < routes.Length; i++)
            {
                PracticeBests.Set(routes[i], 3.5f + i, null, 0);
                PracticeGhost.Clear();
                PracticeGhost.Offer(0f, 1f, 2f, 3f, 0f, 1);
                PracticeGhost.Offer(0.05f, 4f, 5f, 6f, 0f, 1);
                PracticeGhost.Keep(routes[i]);
            }
            LocalProfiles.TrySeat(0, bea);
            bool hidden = true;
            bool stored = true;
            for (int i = 0; i < routes.Length; i++)
            {
                if (PracticeBests.TimeOf(routes[i]) > 0f || PracticeGhost.HasReplay(routes[i])) hidden = false;
                if (Math.Abs(LocalProfiles.PbTime(ada, routes[i]) - (3.5f + i)) > 0.001f
                    || LocalProfiles.GhostSamples(ada, routes[i]) < 2)
                    stored = false;
            }
            LocalProfiles.TrySeat(0, ada);
            bool back = true;
            for (int i = 0; i < routes.Length; i++)
            {
                if (Math.Abs(PracticeBests.TimeOf(routes[i]) - (3.5f + i)) > 0.001f
                    || !PracticeGhost.HasReplay(routes[i]))
                    back = false;
            }
            Pass(report, seated && hidden && stored && back, "repro: switching profiles left the previous ghost on the course");
        }

        static void CheckGhostCorrupt(Report report)
        {
            PracticeGhost.ClearSaved();
            bool nan = !PracticeGhost.Read("bad-nan", "2;NaN,1,2,0,1;3,4,5,0,1");
            bool huge = !PracticeGhost.Read("bad-huge", "999;1,2,3,0,1");
            bool partial = !PracticeGhost.Read("bad-short", "2;1,2,3,0,1");
            bool fat = !PracticeGhost.Read("bad-fat", new string('x', PracticeGhost.Cap * 128 + 8));
            bool empty = PracticeGhost.SavedSamples("bad-nan") == 0
                && PracticeGhost.SavedSamples("bad-huge") == 0
                && PracticeGhost.SavedSamples("bad-short") == 0
                && PracticeGhost.SavedSamples("bad-fat") == 0;
            bool good = PracticeGhost.Read("mega-beginner", "2;1,2,3,0,1;4,5,6,0.5,2")
                && PracticeGhost.SavedSamples("mega-beginner") == 2;
            Pass(report, nan && huge && partial && fat && empty && good, "repro: a corrupt or oversized ghost file claimed a replay slot");
        }

        static void CheckGhostOld(Report report)
        {
            PracticeGhost.ClearSaved();
            bool v0 = PracticeGhost.Read("old-route", "v0;2;1,2,3,0;4,5,6,1");
            bool loaded = v0 && PracticeGhost.Load("old-route") && PracticeGhost.Count == 2 && PracticeGhost.Pose[0] == 0;
            bool v1 = PracticeGhost.Read("v1-route", "v1;1;8,9,10,0.25,3") && PracticeGhost.SavedSamples("v1-route") == 1;
            bool newer = !PracticeGhost.Read("v9-route", "v9;1;1,2,3,0,1") && PracticeGhost.SavedSamples("v9-route") == 0;
            Pass(report, loaded && v1 && newer, "repro: an older ghost file failed to migrate, or a newer prefix was applied in part");
        }

        static void CheckRebind(Report report)
        {
            ActionBinds binds = ActionBinds.Defaults();
            binds.SetKeyboard(PlayAction.Jump, "e");
            binds.SetGamepad(PlayAction.AirDash, "buttonWest");
            bool jump = PracticeInput.ActionDown(binds, (int)PlayAction.Jump, false, "e")
                && !PracticeInput.ActionDown(binds, (int)PlayAction.Jump, false, "space");
            bool dash = PracticeInput.ActionDown(binds, (int)PlayAction.AirDash, true, "buttonWest")
                && !PracticeInput.ActionDown(binds, (int)PlayAction.AirDash, true, "leftStickPress");
            string play = Read("Assets/Scripts/Practice/PracticePlay.cs");
            bool wired = play != null && play.IndexOf("HeldDevice", StringComparison.Ordinal) >= 0;
            Pass(report, jump && dash && wired, "repro: the practice input line ignored a rebind and showed the default key");
        }

        static void CheckHotplug(Report report)
        {
            bool dead = !PracticeInput.PadLive(false, false) && !PracticeInput.PadLive(true, true);
            bool live = PracticeInput.PadLive(true, false);
            string sampler = Read("Assets/Scripts/Settings/BindSampler.cs");
            int at = sampler == null ? -1 : sampler.IndexOf("PracticeRestartDown", StringComparison.Ordinal);
            int end = at < 0 ? -1 : sampler.IndexOf("LookVector", at, StringComparison.Ordinal);
            string slice = at < 0 || end < at ? "" : sampler.Substring(at, end - at);
            bool kept = slice.IndexOf("KeyCode.T", StringComparison.Ordinal) >= 0
                && slice.IndexOf("buttonNorth", StringComparison.Ordinal) >= 0
                && slice.IndexOf("KeyCode.G", StringComparison.Ordinal) >= 0
                && slice.IndexOf("JoystickButton8", StringComparison.Ordinal) >= 0
                && slice.IndexOf("KeyCode.I", StringComparison.Ordinal) >= 0
                && slice.IndexOf("JoystickButton9", StringComparison.Ordinal) >= 0
                && slice.IndexOf("PadLive", StringComparison.Ordinal) >= 0;
            Pass(report, dead && live && kept, "repro: an unplugged gamepad still fired practice restart, ghost, or the input line");
        }

        static void CheckDummyIt(Report report)
        {
            bool blocked = !PracticeSession.FilterIt(true, true) && PracticeSession.FilterIt(false, true);
            string it = Read("Assets/Scripts/Tag/ItController.cs");
            bool wired = it != null && it.IndexOf("FilterIt", StringComparison.Ordinal) >= 0;
            Pass(report, blocked && wired, "repro: the practice dummy became It");
        }

        static void CheckLeavePractice(Report report)
        {
            GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.Arena = 0;
            GameSettings.Current.AiOpponents = 3;
            GameSettings.Current.DifficultyTier = 2;
            GameSettings.Current.RoundLengthIndex = 1;
            GameSettings.Current.RoundsPerMatch = 2;
            GameSettings.Current.Clamp();
            PracticeSession.ResetStatics();
            PracticeSession.Open();
            PracticeSession.Arena = 1;
            PracticeSession.Arm();
            bool during = GameSettings.Current.Arena == 1;
            GameSettings.Current.AiOpponents = 0;
            PracticeSession.Stop();
            bool back = GameSettings.Current.Arena == 0
                && GameSettings.Current.AiOpponents == 3
                && GameSettings.Current.DifficultyTier == 2
                && GameSettings.Current.RoundLengthIndex == 1
                && GameSettings.Current.RoundsPerMatch == 2;
            Pass(report, during && back, "repro: leaving practice kept the practice arena and match setup");
        }

        static void CheckRematch(Report report)
        {
            MatchBook.ResetMatch();
            MatchBook.Open(2);
            MatchBook.TagsMade[0] = 6;
            MatchHighlight.Reset();
            OfferOne(0f, 1);
            MatchHighlight.MarkTag();
            int before = MatchHighlight.SnapCount;
            MatchBook.BeginRematch();
            string front = Read("Assets/Scripts/Front/FrontSession.cs");
            string live = Read("Assets/Scripts/Front/FrontLive.cs");
            bool cleared = MatchBook.Count == 0 && MatchBook.TagsMade[0] == 0 && MatchHighlight.SnapCount == 0 && before > 0;
            string flow = Read("Assets/Scripts/Core/GameFlow.cs");
            bool wired = front != null && front.IndexOf("BeginRematch", StringComparison.Ordinal) >= 0
                && flow != null && flow.IndexOf("BeginMatch", StringComparison.Ordinal) >= 0
                && live != null && live.IndexOf("void BeginMatch", StringComparison.Ordinal) >= 0;
            Pass(report, cleared && wired, "repro: rematch kept the previous match book and highlight");
        }

        static void CheckRoundRing(Report report)
        {
            MatchBook.ResetMatch();
            MatchBook.Open(1);
            MatchBook.TagsMade[0] = 2;
            MatchHighlight.Reset();
            OfferOne(0f, 1);
            OfferOne(4f, 1);
            MatchHighlight.MarkTag();
            int snap = MatchHighlight.SnapCount;
            MatchHighlight.NewRound();
            string mode = Read("Assets/Scripts/Match/MatchLive.cs");
            bool kept = MatchBook.TagsMade[0] == 2 && snap >= 1 && MatchHighlight.Filled == 0 && MatchHighlight.SnapCount == snap;
            bool wired = mode != null && mode.IndexOf("NewRound", StringComparison.Ordinal) >= 0;
            Pass(report, kept && wired, "repro: a tag in the first seconds of the next round replayed across the round boundary");
        }

        static void CheckRoster(Report report)
        {
            MatchBook.ResetMatch();
            MatchBook.TagsMade[0] = 4;
            int[] first = { 11 };
            MatchBook.SyncRoster(1, first);
            bool fresh = MatchBook.TagsMade[0] == 4 && MatchBook.Count == 1;
            MatchBook.TagsMade[0] = 4;
            MatchBook.TagsMade[1] = 9;
            int[] both = { 11, 22 };
            MatchBook.SyncRoster(2, both);
            int[] left = { 11 };
            MatchBook.SyncRoster(1, left);
            bool dropped = MatchBook.Count == 1 && MatchBook.TagsMade[0] == 4 && MatchBook.TagsMade[1] == 0;
            int[] swap = { 99 };
            MatchBook.SyncRoster(1, swap);
            bool replaced = MatchBook.TagsMade[0] == 0;
            string live = Read("Assets/Scripts/Match/MatchLive.cs");
            bool wired = live != null && live.IndexOf("SyncRoster", StringComparison.Ordinal) >= 0;
            Pass(report, fresh && dropped && replaced && wired, "repro: a seat that left or was replaced kept the previous body's stats");
        }

        static void CheckEarlyTag(Report report)
        {
            MatchHighlight.Reset();
            OfferOne(2f, PracticeVerb.Punch);
            MatchHighlight.MarkTag();
            MatchHighlight.BeginPlayback();
            bool at = MatchHighlight.At(0, 0f, out float x, out _, out _, out _, out byte pose);
            bool plays = MatchHighlight.Playing && MatchHighlight.SnapCount == 1 && at && x == 2f && pose == PracticeVerb.Punch;
            bool len = Math.Abs(MatchHighlight.Length() - (1f / MatchHighlight.Hz)) < 0.0001f;
            Pass(report, plays && len, "repro: a tag in the first moments of the match did not play the highlight");
        }

        static void CheckRespawnCut(Report report)
        {
            ParkArena.Select(ParkArena.Mega);
            MatchHighlight.Reset();
            OfferOne(0f, PracticeVerb.Jump);
            OfferOne(100f, PracticeVerb.Punch);
            MatchHighlight.MarkTag();
            bool at = MatchHighlight.At(0, 0f, out float x, out _, out _, out _, out byte pose);
            bool cut = MatchHighlight.SnapCount == 1 && at && x == 100f && pose == PracticeVerb.Punch;
            Pass(report, cut, "repro: the highlight lerped through a respawn teleport");
        }

        static void CheckOnePawn(Report report)
        {
            MatchBook.ResetMatch();
            MatchBook.Open(2);
            MatchHighlight.Reset();
            OfferOne(6f, PracticeVerb.Sprint);
            MatchHighlight.MarkTag();
            bool human = MatchHighlight.At(0, 0f, out float x, out _, out _, out _, out _);
            bool ghost = MatchHighlight.At(1, 0f, out float zx, out float zy, out float zz, out _, out _);
            Pass(report, human && x == 6f && !ghost && zx == 0f && zy == 0f && zz == 0f,
                "repro: one player versus AI drew a zero ghost in the empty seat");
        }

        static void CheckAwards(Report report)
        {
            MatchBook.ResetMatch();
            MatchBook.Open(4);
            MatchBook.Seal();
            bool none = MatchBook.AwardCount == 0;
            MatchBook.ResetMatch();
            MatchBook.Open(4);
            for (int i = 0; i < 4; i++)
            {
                MatchBook.SetName(i, "Abcdefghijkl");
                MatchBook.TagsMade[i] = 1;
            }
            MatchBook.Seal();
            int font = MatchBook.CouchFont(30, 1.5f);
            int rows = MatchBook.AwardRows(MatchBook.AwardLine[0], 420f, font);
            string paint = Read("Assets/Scripts/Match/MatchResults.cs");
            bool wired = paint != null && paint.IndexOf("AwardRows", StringComparison.Ordinal) >= 0;
            Pass(report, none && MatchBook.AwardCount >= 1 && rows >= 2 && font >= 40 && wired,
                "repro: all-zero stats crowned someone, or a 4-way 12-character award clipped at large text");
        }

        static void CheckDeleteSeated(Report report)
        {
            GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.Palette[0] = 1;
            GameSettings.Current.Captions[0] = true;
            GameSettings.Current.Rumble[0] = 40;
            GameSettings.Current.Colorblind = true;
            LocalProfiles.Clear();
            int id = LocalProfiles.Create("Ada");
            LocalProfiles.SetAccess(id, 3, 1.5f, false, 10);
            bool seated = LocalProfiles.TrySeat(0, id);
            PracticeBests.Set("mega-beginner", 2f, null, 0);
            bool applied = GameSettings.Current.Palette[0] == 3 && !GameSettings.Current.Captions[0];
            LocalProfiles.NoteRoster(0, id);
            bool armed = LocalProfiles.ArmDelete(id) && LocalProfiles.Delete();
            bool restored = GameSettings.Current.Palette[0] == 1
                && GameSettings.Current.Captions[0]
                && GameSettings.Current.Rumble[0] == 40
                && GameSettings.Current.Colorblind
                && LocalProfiles.ProfileAt(0) < 0
                && PracticeBests.TimeOf("mega-beginner") <= 0f;
            string card = LocalProfiles.CardOf(id);
            Pass(report, seated && applied && armed && restored && string.IsNullOrEmpty(card),
                "repro: deleting the seated profile left its palette, rumble, and ghost on the seat");
        }

        static void CheckRename(Report report)
        {
            LocalProfiles.Clear();
            int ann = LocalProfiles.Create("Ann");
            int bea = LocalProfiles.Create("Bea");
            bool blank = LocalProfiles.ArmRename(bea) && !LocalProfiles.Rename("   ") && LocalProfiles.NameOf(bea) == "Bea";
            bool again = !LocalProfiles.Rename("Ann");
            bool dup = LocalProfiles.ArmRename(bea) && !LocalProfiles.Rename("ann") && LocalProfiles.NameOf(bea) == "Bea";
            bool trimmed = LocalProfiles.ArmRename(bea) && LocalProfiles.Rename("  Bea2 ") && LocalProfiles.NameOf(bea) == "Bea2";
            bool kept = LocalProfiles.NameOf(ann) == "Ann";
            Pass(report, blank && again && dup && trimmed && kept, "repro: rename accepted a blank, whitespace, or duplicate name");
        }

        static void CheckScroll(Report report)
        {
            LocalProfiles.Clear();
            string[] names = { "Ann", "Bea", "Cid", "Dee", "Eve", "Fay", "Gus", "Hal" };
            for (int i = 0; i < names.Length; i++)
                LocalProfiles.Create(names[i]);
            for (int step = 0; step < 9; step++)
            {
                int pick = LocalProfiles.Cycle(0, 1);
                if (pick == LocalProfiles.Guest) LocalProfiles.SeatGuest(0);
                else if (pick == LocalProfiles.None) LocalProfiles.ClearSeat(0);
                else LocalProfiles.TrySeat(0, pick);
            }
            int idx = LocalProfiles.ListIndex(0);
            int scroll = LocalProfiles.ListScrollOf(0);
            bool window = scroll > 0 && idx >= scroll && idx < scroll + LocalProfiles.ListWindow && LocalProfiles.ListWindow < 10;
            Pass(report, window, "repro: nine profiles on a pad could not scroll the join list");
        }

        static void CheckMigrate(Report report)
        {
            var settings = GameSettings.Defaults();
            var binds = ActionBinds.Defaults();
            SettingsFile.Read("mouse=1.5\nkb.Jump=q\npb.mega-beginner=4.250\n", settings, binds);
            bool bare = LocalProfiles.Count == 1 && Math.Abs(PracticeBests.TimeOf("mega-beginner") - 4.25f) < 0.001f;
            SettingsFile.Read("v=0\ncolorblind=1\ncaptions=1\nhud=1.1\n", settings, binds);
            bool v0 = LocalProfiles.Count == 1 && settings.Colorblind
                && settings.Palette[0] == AccessibilityPalette.Deuteranopia
                && settings.Captions[0];
            SettingsFile.Read("v=1\nhud=1.2\npalette=2\nkb.Jump=e\n", settings, binds);
            bool v1 = LocalProfiles.Count == 1 && LocalProfiles.FirstCard() != null
                && settings.Palette[0] == 2;
            SettingsFile.Read("v=9\nmouse=9\n", settings, binds);
            bool future = LocalProfiles.Count == 0;
            Pass(report, bare && v0 && v1 && future, "repro: an older settings blob did not migrate into a profile");
        }

        static void CheckGuest(Report report)
        {
            LocalProfiles.Clear();
            int id = LocalProfiles.Create("Ada");
            LocalProfiles.TrySeat(0, id);
            LocalProfiles.NoteRoster(0, id);
            LocalProfiles.SeatGuest(0);
            MatchBook.ResetMatch();
            MatchBook.Open(1);
            MatchBook.TagsMade[0] = 5;
            MatchBook.TimeAsIt[0] = 1f;
            MatchBook.TimeAsIt[0] = 1f;
            LocalProfiles.Absorb();
            string card = LocalProfiles.CardOf(id);
            bool clean = card != null && card.IndexOf("tags 0", StringComparison.Ordinal) >= 0
                && card.IndexOf("tags 5", StringComparison.Ordinal) < 0
                && card.IndexOf("wins 0", StringComparison.Ordinal) >= 0;
            Pass(report, clean, "repro: guest stats were written onto the profile that seat used to hold");
        }

        static void CheckItColor(Report report)
        {
            LocalProfiles.Clear();
            GameSettings.Current = GameSettings.Defaults();
            int id = LocalProfiles.Create("Solo");
            LocalProfiles.SetColor(id, 0);
            LocalProfiles.TrySeat(0, id);
            int swatch = LocalProfiles.SeatColor(0);
            bool moved = AccessibilityPalette.ClearsIt(0, swatch);
            AccessibilityPalette.It(0, out float ir, out float ig, out float ib);
            AccessibilityPalette.ItAgainst(0, swatch, out float sr, out float sg, out float sb);
            AccessibilityPalette.ItAgainst(0, 0, out float ar, out float ag, out float ab);
            bool shifted = ar != ir || ag != ig || ab != ib;
            bool surfaces = AccessibilityPalette.ClearsSurfaces(sr, sg, sb)
                && AccessibilityPalette.ClearsSurfaces(ar, ag, ab);
            string marker = Read("Assets/Scripts/Art/ItMarker.cs");
            string hud = Read("Assets/Scripts/Modes/VerbStatusHud.cs");
            bool wired = marker != null && marker.IndexOf("ItAgainst", StringComparison.Ordinal) >= 0
                && hud != null && hud.IndexOf("ItAgainst", StringComparison.Ordinal) >= 0;
            Pass(report, moved && shifted && surfaces && wired, "repro: the It crown used the same color as the seated swatch");
        }

        static void CheckAtomic(Report report)
        {
            string path = Path.Combine(Path.GetTempPath(), "qa-sweep-3-settings.json");
            string tmp = path + ".tmp";
            try
            {
                File.WriteAllText(path, "good");
                File.WriteAllText(tmp, "partial");
                bool survived = SettingsFile.ReadStable(path) == "good";
                bool wrote = SettingsFile.CommitText(path, "next");
                bool replaced = SettingsFile.ReadStable(path) == "next" && !File.Exists(tmp);
                string runtime = Read("Assets/Scripts/Settings/SettingsRuntime.cs");
                bool wired = runtime != null && runtime.IndexOf("CommitText", StringComparison.Ordinal) >= 0
                    && runtime.IndexOf("ReadStable", StringComparison.Ordinal) >= 0;
                Pass(report, survived && wrote && replaced && wired, "repro: a crash mid-save truncated the settings file");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        static void CheckBinds(Report report)
        {
            LocalProfiles.Clear();
            int a = LocalProfiles.Create("Ann");
            int b = LocalProfiles.Create("Bea");
            LocalProfiles.TrySeat(0, a);
            LocalProfiles.TrySeat(1, b);
            ActionBinds edited = LocalProfiles.BindsOf(a).Clone();
            edited.SetKeyboard(PlayAction.Jump, "e");
            LocalProfiles.StoreBinds(0, edited);
            ActionBinds seat0 = LocalProfiles.BindsForSeat(0);
            ActionBinds seat1 = LocalProfiles.BindsForSeat(1);
            bool isolated = seat0 != null && seat1 != null
                && !ReferenceEquals(seat0, seat1)
                && !ReferenceEquals(seat0, edited)
                && seat0.Keyboard[(int)PlayAction.Jump] == "e"
                && seat1.Keyboard[(int)PlayAction.Jump] != "e";
            seat0.SetKeyboard(PlayAction.Jump, "q");
            bool still = seat1.Keyboard[(int)PlayAction.Jump] != "q";
            string menu = Read("Assets/Scripts/Settings/SettingsMenuUi.cs");
            bool wired = menu != null && menu.IndexOf("StoreBinds", StringComparison.Ordinal) >= 0;
            Pass(report, isolated && still && wired, "repro: a rebind on one seat changed the other seat's profile binds");
        }

        static void OfferOne(float x, int pose)
        {
            var xs = new[] { x };
            var ys = new[] { 0f };
            var zs = new[] { 0f };
            var yaw = new[] { 0f };
            var poses = new[] { (byte)pose };
            MatchHighlight.Offer(0.05f, 1, xs, ys, zs, yaw, poses);
        }

        static void Restore(GameSettings backup)
        {
            LocalProfiles.Clear();
            PracticeSession.ResetStatics();
            PracticeBests.Clear();
            PracticeGhost.Clear();
            PracticeGhost.ClearSaved();
            MatchBook.ResetMatch();
            MatchHighlight.Reset();
            if (GameSettings.Current == null) GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.CopyFrom(backup);
        }

        static void Pass(Report report, bool ok, string why)
        {
            report.Cases++;
            if (ok) report.Passed++;
            else report.Fail(why);
        }

        static string Read(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }
    }
}
