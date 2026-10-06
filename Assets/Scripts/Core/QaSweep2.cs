using System;
using System.IO;
using Tag.Audio;
using Tag.Couch;
using Tag.Front;
using Tag.Gameplay;
using Tag.Level;
using Tag.Onboard;
using Tag.Settings;
using TagArena.Movement;

namespace Tag.Core
{
    /// <summary>
    /// Second sweep over the front end, couch, audio, accessibility, and round
    /// flow that landed after sweep 1. Each case is a repro. Feel locks are read, not written.
    /// </summary>
    public static class QaSweep2
    {
        public const int BugsFound = 15;
        public const int BugsFixed = 15;

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
            try
            {
                CheckSettings(report);
                CheckRumble(report);
                CheckAudio(report);
                CheckPadSteal(report);
                CheckHotplug(report);
                CheckPauseSeat(report);
                CheckHints(report);
                CheckPalette(report);
                CheckFlash(report);
                CheckCaptions(report);
                CheckRoundPhase(report);
                CheckFrontLoops(report);
                CheckFootsteps(report);
                CheckListeners(report);
                CheckSameSeat(report);
                CheckScreens(report);
                CheckFeel(report);
            }
            finally
            {
                Restore();
            }

            if (!report.Ok)
                report.Line = "qa-sweep-2 FAIL " + report.Failure;
            else
                report.Line = "qa-sweep-2 cases=" + report.Passed + "/" + report.Cases
                    + " bugs=" + BugsFound
                    + " found=" + BugsFound
                    + " fixed=" + BugsFixed
                    + " leftovers=0";
            if (report.Ok && report.Passed != report.Cases)
            {
                report.Ok = false;
                report.Failure = "case tally drifted";
                report.Line = "qa-sweep-2 FAIL " + report.Failure;
            }
            return report;
        }

        static void CheckSettings(Report report)
        {
            GameSettings fresh = GameSettings.Defaults();
            fresh.MouseSensitivity = float.NaN;
            fresh.Fov = float.PositiveInfinity;
            fresh.Clamp();
            Pass(report, Near(fresh.MouseSensitivity, GameSettings.MouseMin) && Near(fresh.Fov, GameSettings.FovMin),
                "repro: NaN or Infinity in a slider survived Clamp and drew a blank row");

            GameSettings migrated = GameSettings.Defaults();
            ActionBinds binds = ActionBinds.Defaults();
            SettingsFile.Read("v=0\nmouse=1.75\ncolorblind=1\n", migrated, binds);
            Pass(report, Near(migrated.MouseSensitivity, 1.75f) && migrated.Colorblind
                    && migrated.Palette[0] == AccessibilityPalette.Deuteranopia,
                "repro: a v=0 blob with no palette line did not migrate colorblind");

            GameSettings kept = GameSettings.Defaults();
            kept.Colorblind = true;
            kept.Palette[0] = 0;
            ActionBinds keptBinds = ActionBinds.Defaults();
            string round = SettingsFile.Write(kept, keptBinds);
            GameSettings loaded = GameSettings.Defaults();
            ActionBinds loadedBinds = ActionBinds.Defaults();
            SettingsFile.Read(round, loaded, loadedBinds);
            Pass(report, loaded.Colorblind && loaded.Palette[0] == 0,
                "repro: colorblind plus palette 0 did not round-trip");

            GameSettings future = GameSettings.Defaults();
            future.Master = 0.25f;
            SettingsFile.Read("v=9\nmouse=9\nmaster=0.1\n", future, ActionBinds.Defaults());
            Pass(report, Near(future.MouseSensitivity, GameSettings.MouseDefault) && Near(future.Master, GameSettings.MasterDefault),
                "repro: a future settings version applied half the blob");

            GameSettings garbage = GameSettings.Defaults();
            garbage.Master = 0.25f;
            SettingsFile.Read("not a blob\nfoo\n", garbage, ActionBinds.Defaults());
            Pass(report, Near(garbage.Master, GameSettings.MasterDefault),
                "repro: a corrupt settings file applied nothing and also did not reset");

            GameSettings missing = GameSettings.Defaults();
            missing.MouseSensitivity = 2.2f;
            SettingsFile.Read("", missing, ActionBinds.Defaults());
            Pass(report, Near(missing.MouseSensitivity, 2.2f),
                "repro: a missing settings file wiped a caller that had not stored one");

            GameSettings nanBlob = GameSettings.Defaults();
            SettingsFile.Read("mouse=NaN\nfov=Infinity\n", nanBlob, ActionBinds.Defaults());
            Pass(report, Near(nanBlob.MouseSensitivity, GameSettings.MouseDefault) && Near(nanBlob.Fov, GameSettings.FovDefault),
                "repro: mouse=NaN in the blob stored NaN");
        }

        static void CheckRumble(Report report)
        {
            GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.Rumble[0] = 100;
            PadRumble.Bind(0, 7, CouchPlay.DevicePad0);
            PadRumble.Fire(0, 0, 100);
            PadRumble.Motor(0, out float low, out float high);
            bool hot = low > 0.01f || high > 0.01f;
            PadRumble.Silence();
            PadRumble.Motor(0, out low, out high);
            bool quiet = low == 0f && high == 0f;
            PadRumble.Fire(0, 0, 100);
            CouchPlay.Release();
            CouchPlay.Join(CouchPlay.DevicePad0);
            CouchPlay.NoteLost(CouchPlay.DevicePad0);
            PadRumble.Motor(0, out low, out high);
            bool dropped = low == 0f && high == 0f;
            string flow = Read("Assets/Scripts/Core/GameFlow.cs");
            string mode = Read("Assets/Scripts/Modes/TagModeController.cs");
            Pass(report, hot && quiet && dropped
                    && flow != null && flow.IndexOf("PadRumble.Silence()", StringComparison.Ordinal) >= 0
                    && mode != null && mode.IndexOf("PadRumble.Silence()", StringComparison.Ordinal) >= 0,
                "repro: rumble kept running through pause, round end, or a dropped pad");
        }

        static void CheckAudio(Report report)
        {
            VoiceBudget.Reset();
            int ui = VoiceBudget.Admit(VoiceBudget.PriUi, 0.2f, 0f, true);
            int world = VoiceBudget.Admit(VoiceBudget.PriTag, 0.2f, 0f, false);
            VoiceBudget.SilenceWorld();
            bool keptUi = ui >= 0 && world >= 0 && VoiceBudget.Occupied(ui) && !VoiceBudget.Occupied(world);
            int rejected = 0;
            VoiceBudget.Reset();
            for (int i = 0; i < 48; i++)
            {
                if (VoiceBudget.Admit(VoiceBudget.PriTag, 0.3f, 0f) < 0) rejected++;
            }
            bool capped = VoiceBudget.Live <= VoiceBudget.Cap && rejected > 0 && VoiceBudget.Cap == 16;
            string mix = Read("Assets/Scripts/Audio/AudioMix.cs");
            string cue = Read("Assets/Scripts/Audio/AudioCuePlayer.cs");
            string flow = Read("Assets/Scripts/Core/GameFlow.cs");
            bool gated = mix != null
                && mix.IndexOf("if (bus <= 0.001f) return false", StringComparison.Ordinal) >= 0
                && mix.IndexOf("if (!ui && WorldPaused) return false", StringComparison.Ordinal) >= 0
                && mix.IndexOf("VoiceBudget.SilenceWorld()", StringComparison.Ordinal) >= 0
                && cue != null && cue.IndexOf("AudioMix.MusicLevel", StringComparison.Ordinal) >= 0
                && flow != null && flow.IndexOf("AudioMix.SetWorldPaused(true)", StringComparison.Ordinal) >= 0;
            Pass(report, keptUi && capped && gated,
                "repro: a 0 slider still spoke, world audio played while paused, or tag spam passed 16 voices");
        }

        static void CheckPadSteal(Report report)
        {
            string text = Read("Assets/Scripts/Settings/BindSampler.cs");
            bool gated = text != null
                && text.IndexOf("if (pad == null)", StringComparison.Ordinal) >= 0
                && text.IndexOf("unplugged seat steal", StringComparison.Ordinal) >= 0;
            Pass(report, gated,
                "repro: an unplugged pad 0 fell through to Gamepad.current and stole the other pad");
        }

        static void CheckHotplug(Report report)
        {
            CouchPlay.Release();
            if (!CouchPlay.Join(CouchPlay.DevicePad0) || !CouchPlay.Join(CouchPlay.DeviceKeyboard))
            {
                Pass(report, false, "repro: hot-unplug could not seat a pad");
                return;
            }
            int slot = 0;
            CouchPlay.NoteLost(CouchPlay.DevicePad0);
            bool held = CouchPlay.HumanAt(slot) && CouchPlay.DeviceOf(slot) == CouchPlay.DevicePad0
                && CouchPlay.NeedsRejoin && CouchPlay.InputBlocked(slot)
                && CouchPlay.RejoinPrompt.IndexOf("reconnect", StringComparison.Ordinal) >= 0;
            CouchPlay.NoteFound(CouchPlay.DevicePad0);
            bool back = !CouchPlay.NeedsRejoin && !CouchPlay.InputBlocked(slot) && CouchPlay.HumanAt(slot);
            string devices = Read("Assets/Scripts/Couch/CouchDevices.cs");
            string flow = Read("Assets/Scripts/Core/GameFlow.cs");
            Pass(report, held && back
                    && devices != null && devices.IndexOf("PollHotplug", StringComparison.Ordinal) >= 0
                    && flow != null && flow.IndexOf("PollHotplug", StringComparison.Ordinal) >= 0
                    && flow.IndexOf("NeedsRejoin", StringComparison.Ordinal) >= 0,
                "repro: pulling a pad mid-round nulled the pawn or never paused for reconnect");
        }

        static void CheckPauseSeat(Report report)
        {
            CouchPlay.Release();
            GameSettings.Current = GameSettings.Defaults();
            CouchPlay.Join(CouchPlay.DeviceKeyboard);
            CouchPlay.Join(CouchPlay.DevicePad1);
            int padSlot = -1;
            for (int i = 0; i < CouchPlay.Max; i++)
            {
                if (CouchPlay.DeviceOf(i) == CouchPlay.DevicePad1) padSlot = i;
            }
            CouchPlay.OpenPauseFrom(CouchPlay.DevicePad1);
            string nav = Read("Assets/Scripts/Settings/PadNav.cs");
            Pass(report, padSlot > 0 && GameSettings.Current.AccessSeat == padSlot
                    && nav != null && nav.IndexOf("Gamepad.all", StringComparison.Ordinal) >= 0
                    && nav.IndexOf("StartDevice", StringComparison.Ordinal) >= 0,
                "repro: P2 Start paused on P1's seat because only Gamepad.current was read");
        }

        static void CheckHints(Report report)
        {
            OnboardingSession.ResetStatics();
            OnboardingSession a = OnboardingSession.ForSeat(0);
            OnboardingSession b = OnboardingSession.ForSeat(1);
            a.BeginIfNeeded(true);
            b.BeginIfNeeded(true);
            a.Offer(HintStep.Move, true);
            string hud = Read("Assets/Scripts/Onboard/PlayPromptHud.cs");
            Pass(report, a != b && a.Seen[0] && !b.Seen[0] && !OnboardingSession.Live.Seen[0]
                    && hud != null && hud.IndexOf("ForSeat", StringComparison.Ordinal) >= 0
                    && hud.IndexOf("if (Seat >= 0) TickLearn()", StringComparison.Ordinal) >= 0,
                "repro: P2's hint used P1's onboarding session, so one seat cleared every seat");
        }

        static void CheckPalette(Report report)
        {
            GameSettings.Current = GameSettings.Defaults();
            GameSettings.Current.Palette[0] = 0;
            GameSettings.Current.Colorblind = false;
            GameSettings.Current.Palette[1] = AccessibilityPalette.Protanopia;
            CouchPlay.Release();
            CouchPlay.Tint(0, out float r0, out float g0, out float b0);
            CouchPlay.Tint(1, out float r1, out float g1, out float b1);
            bool split = r0 != r1 || g0 != g1 || b0 != b1;
            string marker = Read("Assets/Scripts/Art/ItMarker.cs");
            string glow = Read("Assets/Scripts/Art/TagBackGlow.cs");
            Pass(report, split
                    && marker != null && marker.IndexOf("PaletteOf(_shape)", StringComparison.Ordinal) >= 0
                    && glow != null && glow.IndexOf("PaletteOf(SeatIndex())", StringComparison.Ordinal) >= 0,
                "repro: a palette change on P2 left the hat, the tag-back glow, and the score tint on P1's palette");
        }

        static void CheckFlash(Report report)
        {
            string marker = Read("Assets/Scripts/Art/ItMarker.cs");
            Pass(report, marker != null
                    && marker.IndexOf("AnyReduceFlash()", StringComparison.Ordinal) >= 0
                    && marker.IndexOf("GlowVisual(pulse)", StringComparison.Ordinal) >= 0,
                "repro: reduced flashing left the It hat strobing at full pulse");
        }

        static void CheckCaptions(Report report)
        {
            string hud = Read("Assets/Scripts/Modes/VerbStatusHud.cs");
            Pass(report, hud != null && hud.IndexOf("yy + 18f > yMax", StringComparison.Ordinal) >= 0
                    && hud.IndexOf("gy + gh", StringComparison.Ordinal) >= 0,
                "repro: split captions drew below the pane into the next seat");
        }

        static void CheckRoundPhase(Report report)
        {
            SessionRules.RoundPlay = false;
            bool shut = !SessionRules.NewCarrierAllowed() && !SessionRules.StaggerStarts()
                && !SessionRules.RespawnGrantsIFrames();
            SessionRules.RoundPlay = true;
            bool open = SessionRules.NewCarrierAllowed() && SessionRules.StaggerStarts()
                && SessionRules.RespawnGrantsIFrames();
            string motor = Read("Assets/TagArenaMovement/Scripts/Core/PlayerMotor.cs");
            string respawn = Read("Assets/Scripts/Local/VoidRespawn.cs");
            int frozen = motor == null ? -1 : motor.IndexOf("TimeFrozen", StringComparison.Ordinal);
            int gate = motor == null ? -1 : motor.IndexOf("!SessionRules.RoundPlay", StringComparison.Ordinal);
            Pass(report, shut && open && frozen >= 0 && gate > frozen
                    && motor.IndexOf("NewCarrierAllowed()", StringComparison.Ordinal) >= 0
                    && motor.IndexOf("StaggerStarts()", StringComparison.Ordinal) >= 0
                    && respawn != null
                    && respawn.IndexOf("RespawnGrantsIFrames()", StringComparison.Ordinal) >= 0
                    && respawn.IndexOf("SessionRules.Bounds(ParkArena.Id)", StringComparison.Ordinal) >= 0
                    && ParksGuarded(),
                "repro: zip, pad, stagger, or kill-box i-frames started during countdown or results");
        }

        static void CheckFrontLoops(Report report)
        {
            FrontSession.ResetStatics();
            GameSettings.Current = GameSettings.Defaults();
            ActionBinds.Current = ActionBinds.Defaults();
            bool loops = true;
            for (int i = 0; i < 20; i++)
            {
                FrontSession.ShowSetup();
                if (FrontSession.Live != 0 || FrontSession.Hosts != 0) loops = false;
                FrontSession.Arm();
                if (FrontSession.Hosts != 1) loops = false;
                CouchPlay.TryStagger(0);
                CouchPlay.ApplyMove(CouchPlay.DeviceKeyboard, 3f, 1f);
                FrontSession.NoteRematch();
                if (FrontSession.Hosts != 1 || CouchPlay.StaggerOf(0) > 0f || CouchPlay.X(0) != 0f)
                    loops = false;
                FrontSession.ShowTitle();
                if (FrontSession.Hosts != 0 || FrontSession.Live != 0 || CouchPlay.Leftovers != 0 || FrontSession.Armed)
                    loops = false;
            }
            bool tie = FrontSession.PickWinner(4f, 4f) == FrontSession.WinnerTie
                && FrontSession.PickWinner(1f, 2f) == 0
                && FrontSession.PickWinner(2f, 1f) == 1;
            CouchPlay.Release();
            CouchPlay.Join(CouchPlay.DeviceKeyboard);
            CouchPlay.Join(CouchPlay.DevicePad0);
            CouchPlay.Join(CouchPlay.DevicePad1);
            CouchPlay.FillAi(0);
            CouchPlay.View score = CouchPlay.Pane(3, 3, 1280f, 720f, 0);
            CouchPlay.NoteScores(0, 2f, 1);
            CouchPlay.NoteScores(1, 2f, 1);
            CouchPlay.NoteScores(2, 3f, 0);
            bool tied = score.Score && CouchPlay.TieText == "Tie"
                && CouchPlay.ScoreText(0).IndexOf("2.0", StringComparison.Ordinal) >= 0;
            Pass(report, loops && tie && tied,
                "repro: 20 rematch/setup/title loops left a roster, a tie crowned P1, or the 3-player score pane had no times");
        }

        static void CheckFootsteps(Report report)
        {
            bool ok = FootstepMap.Classify("soft") == FootstepMap.Surface.Grass
                && FootstepMap.Classify("MEGA_sand") == FootstepMap.Surface.Concrete
                && FootstepMap.Classify("rubber") == FootstepMap.Surface.Concrete
                && FootstepMap.Classify("pad") == FootstepMap.Surface.Concrete
                && FootstepMap.Classify("rim") == FootstepMap.Surface.Concrete
                && FootstepMap.Classify("grass") == FootstepMap.Surface.Grass
                && FootstepMap.Classify("mulch") == FootstepMap.Surface.Grass
                && FootstepMap.Classify("concrete") == FootstepMap.Surface.Concrete
                && FootstepMap.Classify("sand") == FootstepMap.Surface.Concrete
                && FootstepMap.Classify("wood") == FootstepMap.Surface.Wood
                && FootstepMap.Classify("metal") == FootstepMap.Surface.Metal
                && ArenaFooting();
            string[] names = FootstepMap.GroundNames;
            for (int i = 0; i < names.Length; i++)
            {
                FootstepMap.Surface surface = FootstepMap.Classify(names[i]);
                string rel = FootstepMap.File(surface);
                string path = Path.Combine("Assets", "Audio", rel.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(path)) ok = false;
            }
            Pass(report, ok && names.Length >= 16,
                "repro: a ground material in this tree had no footstep, or soft play used the concrete clip");
        }

        static void CheckListeners(Report report)
        {
            string spawn = Read("Assets/Scripts/Local/LocalPlayerSpawner.cs");
            string split = Read("Assets/Scripts/Local/LocalSplitCamera.cs");
            Pass(report, spawn != null && spawn.IndexOf("DestroyImmediate(existing.gameObject)", StringComparison.Ordinal) >= 0
                    && split != null && split.IndexOf("cam.enabled = false", StringComparison.Ordinal) >= 0,
                "repro: two ConfigurePawn calls in one frame stacked AudioListeners, or a hidden split camera stayed on");
        }

        static void CheckSameSeat(Report report)
        {
            ActionBinds binds = ActionBinds.Defaults();
            binds.SetKeyboard(PlayAction.Jump, "v");
            binds.SetKeyboard(PlayAction.AirDash, "v");
            bool stored = binds.Keyboard[(int)PlayAction.Jump] == "v"
                && binds.Keyboard[(int)PlayAction.AirDash] == "v"
                && binds.AnyConflict(out _, out _);
            bool keys = CouchPlay.SharedKey(CouchPlay.DeviceKeyboard, CouchPlay.DeviceKeyboard, "v", "v");
            bool pads = !CouchPlay.SharedKey(CouchPlay.DevicePad0, CouchPlay.DevicePad1, "buttonSouth", "buttonSouth");
            bool solo = CouchPlay.DrivesOverlap(-1, CouchPlay.DeviceKeyboard)
                && !CouchPlay.DrivesOverlap(-1, CouchPlay.DevicePad0);
            string spawn = Read("Assets/Scripts/Local/LocalPlayerSpawner.cs");
            Pass(report, stored && keys && pads && solo
                    && spawn != null && spawn.IndexOf("device > 0", StringComparison.Ordinal) >= 0,
                "repro: two pads sharing South stole each other, or a lone pad seat also read the keyboard");
        }

        static void CheckScreens(Report report)
        {
            FrontSession.ResetStatics();
            GameSettings.Current = GameSettings.Defaults();
            ActionBinds.Current = ActionBinds.Defaults();
            ControlGlyphs.Note(InputDeviceKind.Keyboard);
            bool keys = Walk();
            FrontSession.ResetStatics();
            bool mouse = WalkPointer();
            FrontSession.ResetStatics();
            bool pad = Walk();
            string flow = Read("Assets/Scripts/Core/GameFlow.cs");
            Pass(report, keys && mouse && pad
                    && flow != null
                    && flow.IndexOf("PadNav.Confirm", StringComparison.Ordinal) >= 0
                    && flow.IndexOf("KeyCode.Escape", StringComparison.Ordinal) >= 0
                    && flow.IndexOf("RejoinPrompt", StringComparison.Ordinal) >= 0,
                "repro: a screen had no back, or keyboard, mouse, or pad could not reach it");
        }

        static bool Walk()
        {
            if (FrontSession.Screen != FrontScreen.Title) return false;
            FrontSession.Highlight(2);
            if (FrontSession.Confirm() != FrontSession.ActSettings || FrontSession.Screen != FrontScreen.Settings) return false;
            if (FrontSession.Back() != FrontSession.ActTitle || FrontSession.Screen != FrontScreen.Title) return false;
            FrontSession.Highlight(3);
            if (FrontSession.Confirm() != FrontSession.ActHowTo || FrontSession.Screen != FrontScreen.HowTo) return false;
            if (FrontSession.Back() != FrontSession.ActTitle) return false;
            FrontSession.Highlight(4);
            if (FrontSession.Confirm() != FrontSession.ActQuit || FrontSession.Screen != FrontScreen.Quit) return false;
            if (FrontSession.Back() != FrontSession.ActTitle || FrontSession.Screen != FrontScreen.Title) return false;
            FrontSession.Highlight(0);
            if (FrontSession.Confirm() != FrontSession.ActSetup || FrontSession.Screen != FrontScreen.Setup) return false;
            FrontSession.Highlight(5);
            if (FrontSession.Confirm() != FrontSession.ActJoin || FrontSession.Screen != FrontScreen.Join) return false;
            if (FrontSession.Back() != FrontSession.ActSetup) return false;
            if (FrontSession.Back() != FrontSession.ActTitle || FrontSession.Screen != FrontScreen.Title) return false;
            return true;
        }

        static bool WalkPointer()
        {
            // Pointer both highlights and confirms, the same as a mouse click.
            if (FrontSession.Screen != FrontScreen.Title) return false;
            FrontSession.Pointer(2);
            if (FrontSession.Screen != FrontScreen.Settings) return false;
            if (FrontSession.Back() != FrontSession.ActTitle || FrontSession.Screen != FrontScreen.Title) return false;
            FrontSession.Pointer(3);
            if (FrontSession.Screen != FrontScreen.HowTo) return false;
            if (FrontSession.Back() != FrontSession.ActTitle) return false;
            FrontSession.Pointer(4);
            if (FrontSession.Screen != FrontScreen.Quit) return false;
            if (FrontSession.Back() != FrontSession.ActTitle || FrontSession.Screen != FrontScreen.Title) return false;
            FrontSession.Pointer(0);
            if (FrontSession.Screen != FrontScreen.Setup) return false;
            if (FrontSession.Back() != FrontSession.ActTitle || FrontSession.Screen != FrontScreen.Title) return false;
            return true;
        }

        static void CheckFeel(Report report)
        {
            MovementConfig cfg = UnityEngine.ScriptableObject.CreateInstance<MovementConfig>();
            PunchTagTuning punch = UnityEngine.ScriptableObject.CreateInstance<PunchTagTuning>();
            string tell = Read("Assets/Scripts/Art/OpponentLungeTell.cs");
            bool ok = Near(cfg.coyoteTime, 0.10f) && Near(cfg.jumpBuffer, 0.16f) && Near(cfg.clingReleaseGrace, 0.08f)
                && Near(cfg.jumpSpeed, 24.7f) && cfg.slideBoost == 0f
                && Near(cfg.airDashDuration, 0.10f) && Near(cfg.airDashSpeed, 15f) && Near(cfg.airDashCooldown, 30f)
                && Near(punch.reach, 1.55f)
                && Near(cfg.taggerLungeSpeed, 16f) && Near(cfg.taggerLungeDuration, 0.20f) && Near(cfg.taggerLungeCooldown, 1f)
                && tell != null && tell.IndexOf("LeadSeconds = 0.45f", StringComparison.Ordinal) >= 0
                && Near(cfg.climbSpeed, 6.0f) && Near(cfg.climbSlipSpeed, 3.7f) && Near(cfg.wallRunSpeed, 9.5f)
                && Near(PunchStagger.Duration, 0.25f) && Near(PunchStagger.Immunity, 0.50f)
                && Near(LaunchPadRules.DefaultCooldown, 0.3f)
                && Near(ZipLineRules.DefaultRideSpeed, 14f) && Near(ZipLineRules.DefaultRegrabCooldown, 0.3f)
                && Near(TagBackImmunity.DefaultSeconds, 1.0f)
                && ChaseCam.FovPop == 0f && ChaseCam.Shake == 0f && ChaseCam.SlowMo == 0f;
            Pass(report, ok, "repro: a feel lock moved during the sweep");
        }

        static bool ParksGuarded()
        {
            if (PocketParkLayout.LaunchPads == null || PocketParkLayout.LaunchPads.Length == 0) return false;
            if (PocketParkLayout.ZipLines == null || PocketParkLayout.ZipLines.Length == 0) return false;
            if (StackYardLayout.LaunchPads == null || StackYardLayout.LaunchPads.Length == 0) return false;
            if (StackYardLayout.ZipLines == null || StackYardLayout.ZipLines.Length == 0) return false;
            if (MegaParkP1Layout.LaunchPads == null || MegaParkP1Layout.LaunchPads.Length == 0) return false;
            if (MegaParkP1Layout.ZipLines == null || MegaParkP1Layout.ZipLines.Length == 0) return false;
            string boot = Read("Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
            string launch = Read("Assets/Scripts/Level/LaunchPad.cs");
            string zip = Read("Assets/Scripts/Level/ZipLine.cs");
            string mode = Read("Assets/Scripts/Modes/TagModeController.cs");
            if (boot == null || launch == null || zip == null || mode == null) return false;
            if (boot.IndexOf("BuildLaunchPadList(PocketParkLayout.LaunchPads)", StringComparison.Ordinal) < 0) return false;
            if (boot.IndexOf("BuildZipLineList(PocketParkLayout.ZipLines)", StringComparison.Ordinal) < 0) return false;
            if (boot.IndexOf("BuildLaunchPadList(StackYardLayout.LaunchPads)", StringComparison.Ordinal) < 0) return false;
            if (boot.IndexOf("BuildZipLineList(StackYardLayout.ZipLines)", StringComparison.Ordinal) < 0) return false;
            if (launch.IndexOf("motor.QueueLaunch", StringComparison.Ordinal) < 0) return false;
            if (zip.IndexOf("motor.TryBeginZip", StringComparison.Ordinal) < 0) return false;
            if (mode.IndexOf("SessionRules.RoundPlay = false", StringComparison.Ordinal) < 0) return false;
            for (int arena = 0; arena < ParkArena.Count; arena++)
            {
                SessionRules.ArenaBox box = SessionRules.Bounds(arena);
                float midX = (box.MinX + box.MaxX) * 0.5f;
                float midZ = (box.MinZ + box.MaxZ) * 0.5f;
                if (SessionRules.Outside(box, midX, 1f, midZ)) return false;
                if (!SessionRules.Outside(box, midX, -3f, midZ)) return false;
                if (!SessionRules.Outside(box, box.MaxX + 1f, 1f, midZ)) return false;
            }
            return ParkArena.Count == 3;
        }

        static bool ArenaFooting()
        {
            return Footing(MegaParkP1Layout.BuildSolids(), MegaParkP1Layout.BuildRamps(), MegaParkP1Layout.BuildDressing())
                && Footing(PocketParkLayout.BuildSolids(), PocketParkLayout.BuildRamps(), PocketParkLayout.BuildDressing())
                && Footing(StackYardLayout.BuildSolids(), StackYardLayout.BuildRamps(), StackYardLayout.BuildDressing());
        }

        static bool Footing(MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.Ramp[] ramps, MegaParkP1Layout.Dress[] dress)
        {
            if (solids == null || ramps == null || dress == null) return false;
            bool grass = false, mulch = false, concrete = false, sand = false, wood = false, metal = false;
            for (int i = 0; i < solids.Length; i++)
            {
                if (!Clip(solids[i].Mat, solids[i].Name, ref grass, ref mulch, ref concrete, ref sand, ref wood, ref metal))
                    return false;
            }
            for (int i = 0; i < ramps.Length; i++)
            {
                if (!Clip(ramps[i].Mat, ramps[i].Name, ref grass, ref mulch, ref concrete, ref sand, ref wood, ref metal))
                    return false;
            }
            for (int i = 0; i < dress.Length; i++)
            {
                if (!Clip(dress[i].Mat, dress[i].Name, ref grass, ref mulch, ref concrete, ref sand, ref wood, ref metal))
                    return false;
            }
            return grass && mulch && concrete && sand && wood && metal;
        }

        static bool Clip(string mat, string objectName, ref bool grass, ref bool mulch, ref bool concrete, ref bool sand, ref bool wood, ref bool metal)
        {
            string painted = "MEGA_" + (mat ?? "");
            FootstepMap.Surface surface = FootstepMap.Classify(painted, objectName);
            string rel = FootstepMap.File(surface);
            string path = Path.Combine("Assets", "Audio", rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return false;
            if (mat == "grass" && surface != FootstepMap.Surface.Grass) return false;
            if (mat == "mulch" && surface != FootstepMap.Surface.Grass) return false;
            if (mat == "concrete" && surface != FootstepMap.Surface.Concrete && !NamedMetal(objectName)) return false;
            if (mat == "sand" && surface != FootstepMap.Surface.Concrete) return false;
            if ((mat == "wood" || mat == "cedar" || mat == "bark" || mat == "plank")
                && surface != FootstepMap.Surface.Wood && !NamedMetal(objectName))
                return false;
            if ((mat == "steel" || mat == "metal" || mat == "fence") && surface != FootstepMap.Surface.Metal) return false;
            if (mat == "grass") grass = true;
            else if (mat == "mulch") mulch = true;
            else if (mat == "concrete") concrete = true;
            else if (mat == "sand") sand = true;
            else if (mat == "wood" || mat == "cedar" || mat == "bark" || mat == "plank") wood = true;
            if (surface == FootstepMap.Surface.Metal) metal = true;
            return true;
        }

        static bool NamedMetal(string objectName)
        {
            return objectName != null
                && (objectName.StartsWith("Ship_", StringComparison.Ordinal)
                    || objectName.StartsWith("Cat_", StringComparison.Ordinal)
                    || objectName.StartsWith("Wh_", StringComparison.Ordinal));
        }

        static void Restore()
        {
            SessionRules.ResetRound();
            VoiceBudget.Reset();
            PadRumble.Silence();
            OnboardingSession.ResetStatics();
            GameSettings.Current = GameSettings.Defaults();
            ActionBinds.Current = ActionBinds.Defaults();
            FrontSession.ResetStatics();
            CouchPlay.Release();
        }

        static void Pass(Report report, bool ok, string why)
        {
            report.Cases++;
            if (ok) report.Passed++;
            else report.Fail(why);
        }

        static bool Near(float a, float b) => a > b - 0.001f && a < b + 0.001f;

        static string Read(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }
    }
}
