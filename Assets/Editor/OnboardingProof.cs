using System;
using System.IO;
using Tag.Core;
using Tag.Modes;
using Tag.Onboard;
using Tag.Settings;

public struct OnboardingReport
{
    public bool Ok;
    public string Line;
    public string Failure;

    public void Fail(string reason)
    {
        Ok = false;
        if (string.IsNullOrEmpty(Failure)) Failure = reason;
    }
}

/// <summary>
/// A scripted new player walks the hint list. Each hint clears on its action.
/// Presses still count when they do not match, and after skip. Glyphs follow a rebind.
/// </summary>
public static class OnboardingProof
{
    public static OnboardingReport Run()
    {
        var report = new OnboardingReport { Ok = true };
        try
        {
            CheckScript(report);
            CheckGlyphs(report);
            CheckContext(report);
            CheckCard(report);
            CheckHud(report);
            CheckMenus(report);
            CheckWired(report);
            CheckHot(report);
        }
        finally
        {
            Restore();
        }

        if (!report.Ok)
            report.Line = "onboarding FAIL " + report.Failure;
        else
            report.Line = "onboarding hints=8 cleared=8 blocked=0 eaten=0"
                + " glyph=rebind device=live arenas=" + ArenaRegistry.Count.ToString()
                + " key3=registry hud=clear retire=" + ContextPrompts.RetireAfter.ToString()
                + " hot<=9";
        return report;
    }

    static void CheckScript(OnboardingReport report)
    {
        var idle = new OnboardingSession();
        idle.BeginIfNeeded(false);
        if (idle.Active || idle.BlocksInput || idle.Pauses || idle.EatsInput)
            report.Fail("tips ran outside countdown and play");

        var countdown = new OnboardingSession();
        countdown.BeginIfNeeded(OnboardingSession.LearnWindow(true, false));
        if (!countdown.Active || countdown.BlocksInput)
            report.Fail("countdown did not show a non-blocking hint");

        var session = new OnboardingSession();
        session.BeginIfNeeded(OnboardingSession.LearnWindow(true, true));
        if (!session.Active || session.Pauses || session.EatsInput || session.BlocksInput)
            report.Fail("first play did not start the hint sequence");
        if (session.Offer(HintStep.Jump, true) || session.Current != (int)HintStep.Move || session.Accepted != 1)
            report.Fail("jump cleared the move hint or the press was eaten");

        HintStep[] order =
        {
            HintStep.Move, HintStep.Sprint, HintStep.Jump, HintStep.Slide,
            HintStep.ClingClimb, HintStep.WallJump, HintStep.AirDash, HintStep.Punch
        };
        for (int i = 0; i < order.Length; i++)
        {
            if (session.Current != (int)order[i] || session.BlocksInput)
            {
                report.Fail("hint " + i.ToString() + " was not the current step");
                return;
            }
            if (!session.Offer(order[i], true))
            {
                report.Fail("matching action did not clear hint " + i.ToString());
                return;
            }
        }
        if (session.Active || !session.AllSeen() || session.Accepted != 9 || session.BlocksInput)
            report.Fail("the scripted player did not clear every hint");

        int mask = session.Pack();
        var loaded = new OnboardingSession();
        loaded.Unpack(mask);
        loaded.BeginIfNeeded(true);
        if (loaded.Active || !loaded.AllSeen())
            report.Fail("seen flags did not persist");

        var fresh = new OnboardingSession();
        fresh.BeginIfNeeded(true);
        fresh.Offer(HintStep.Move, true);
        var again = new OnboardingSession();
        again.Unpack(fresh.Pack());
        again.BeginIfNeeded(true);
        if (!again.Seen[(int)HintStep.Move] || again.Current != (int)HintStep.Sprint)
            report.Fail("a cleared move hint came back");

        again.Skip();
        var skipped = new OnboardingSession();
        skipped.Unpack(again.Pack());
        skipped.BeginIfNeeded(true);
        if (skipped.Active || !skipped.Skipped)
            report.Fail("skip did not persist");
        int before = skipped.Accepted;
        skipped.Offer(HintStep.Jump, true);
        if (skipped.Accepted != before + 1 || skipped.BlocksInput || skipped.Pauses)
            report.Fail("skip ate the jump press");

        OnboardingSession.Live.Unpack(again.Pack());
        OnboardingSession.Live.Replay();
        if (!OnboardingSession.Live.Active || OnboardingSession.Live.Skipped || OnboardingSession.Live.Current != 0)
            report.Fail("replay tips did not restart the sequence");
        if (OnboardingSession.Live.BlocksInput || OnboardingSession.Live.EatsInput)
            report.Fail("replay blocked input");
    }

    static void CheckGlyphs(OnboardingReport report)
    {
        ActionBinds.Current = ActionBinds.Defaults();
        ControlGlyphs.Note(InputDeviceKind.Keyboard);
        PromptText.ResetStatics();
        if (ControlGlyphs.Glyph(PlayAction.Jump) != "Space")
            report.Fail("keyboard jump glyph is not Space");
        if (PromptText.Hint((int)HintStep.Jump).IndexOf("[Space]", StringComparison.Ordinal) < 0)
            report.Fail("jump hint did not show Space");
        if (PromptText.Hint((int)HintStep.Move).IndexOf("[WASD]", StringComparison.Ordinal) < 0)
            report.Fail("move hint did not show WASD");
        if (PromptText.Hint((int)HintStep.ClingClimb).IndexOf("climb", StringComparison.Ordinal) < 0)
            report.Fail("cling hint dropped the climb");

        ControlGlyphs.Note(InputDeviceKind.Gamepad);
        PromptText.ResetStatics();
        if (ControlGlyphs.Glyph(PlayAction.Jump) != "South")
            report.Fail("gamepad jump glyph is not South");
        if (PromptText.Hint((int)HintStep.Jump).IndexOf("[South]", StringComparison.Ordinal) < 0)
            report.Fail("jump hint did not follow the pad");

        ControlGlyphs.Note(InputDeviceKind.Keyboard);
        ActionBinds.Current.SetKeyboard(PlayAction.Jump, "e");
        PromptText.ResetStatics();
        if (ControlGlyphs.Glyph(PlayAction.Jump) != "E")
            report.Fail("rebind did not change the jump glyph");
        if (PromptText.Hint((int)HintStep.Jump).IndexOf("[E]", StringComparison.Ordinal) < 0)
            report.Fail("jump hint kept the old key after a rebind");
        if (PromptText.Hint((int)HintStep.WallJump).IndexOf("[E]", StringComparison.Ordinal) < 0)
            report.Fail("wall-jump hint did not use the jump rebind");

        ControlGlyphs.Note(InputDeviceKind.Gamepad);
        PromptText.ResetStatics();
        if (ControlGlyphs.Glyph(PlayAction.Jump) != "South")
            report.Fail("device switch ignored the pad after a keyboard rebind");
        if (PromptText.ContextLine(ContextKind.ZipDrop).IndexOf("[South]", StringComparison.Ordinal) < 0)
            report.Fail("zip drop glyph did not follow the pad");
    }

    static void CheckContext(OnboardingReport report)
    {
        ActionBinds.Current = ActionBinds.Defaults();
        ControlGlyphs.Note(InputDeviceKind.Keyboard);
        PromptText.ResetStatics();

        var cling = new ContextPrompts();
        var sample = new ContextSample { NearCling = true };
        cling.Tick(0.05f, sample);
        if (cling.Kind != ContextKind.Cling || cling.Alpha < 0.99f || cling.BlocksInput || cling.Pauses)
            report.Fail("cling prompt did not show");
        string hold = PromptText.ContextLine(ContextKind.Cling);
        if (hold.IndexOf("Hold [", StringComparison.Ordinal) < 0 || hold.IndexOf("Hold into wall", StringComparison.Ordinal) < 0)
            report.Fail("cling prompt is not Hold [cling]");

        cling.Tick(ContextPrompts.HoldSeconds + ContextPrompts.FadeSeconds + 0.05f, sample);
        if (cling.Kind != ContextKind.None || cling.Alpha > 0.001f)
            report.Fail("cling prompt did not fade");
        cling.Tick(0.1f, sample);
        if (cling.Kind != ContextKind.None)
            report.Fail("cling prompt ignored the throttle");

        var pad = new ContextPrompts();
        pad.Tick(ContextPrompts.ThrottleSeconds + 0.05f, new ContextSample { NearPad = true });
        if (pad.Kind != ContextKind.Pad || !PromptText.IconOnly(ContextKind.Pad))
            report.Fail("pad prompt is not an icon");
        if (PromptText.ContextLine(ContextKind.Pad).Length != 0)
            report.Fail("pad prompt used a jump label");

        var zip = new ContextPrompts();
        zip.Tick(0.05f, new ContextSample { NearZip = true });
        if (zip.Kind != ContextKind.ZipGrab)
            report.Fail("zip prompt did not show in range");
        string grab = PromptText.ContextLine(ContextKind.ZipGrab);
        if (grab.IndexOf("Hold [", StringComparison.Ordinal) < 0 || grab.IndexOf("to grab", StringComparison.Ordinal) < 0)
            report.Fail("zip prompt is not Hold [cling] to grab");
        if (grab.IndexOf("Jump", StringComparison.Ordinal) >= 0)
            report.Fail("zip grab used the jump word");

        var ride = new ContextPrompts();
        ride.Tick(0.05f, new ContextSample { RidingZip = true, NearZip = true });
        if (ride.Kind != ContextKind.ZipDrop)
            report.Fail("riding a zip did not show the drop prompt");
        string drop = PromptText.ContextLine(ContextKind.ZipDrop);
        if (drop.IndexOf("Jump [", StringComparison.Ordinal) < 0 || drop.IndexOf("to drop", StringComparison.Ordinal) < 0)
            report.Fail("zip drop is not Jump to drop");

        var retired = new ContextPrompts();
        var near = new ContextSample { NearCling = true };
        retired.Tick(0.05f, near);
        for (int i = 0; i < ContextPrompts.RetireAfter; i++)
        {
            near.DidCling = true;
            retired.Tick(0.01f, near);
            near.DidCling = false;
        }
        if (!retired.Retired(ContextKind.Cling))
            report.Fail("cling uses did not reach the retire count");
        near.DidCling = false;
        retired.Tick(ContextPrompts.HoldSeconds + ContextPrompts.FadeSeconds + 0.05f, near);
        retired.Tick(ContextPrompts.ThrottleSeconds + 0.05f, near);
        if (retired.Kind != ContextKind.None || retired.Alpha > 0.001f)
            report.Fail("cling prompt returned after it was retired");
    }

    static void CheckCard(OnboardingReport report)
    {
        ActionBinds.Current = ActionBinds.Defaults();
        ControlGlyphs.Note(InputDeviceKind.Keyboard);
        HowToPlay.ResetStatics();
        HowToPlay.Ensure(InputDeviceKind.Keyboard, ArenaRegistry.Count);
        if (ArenaRegistry.Count != 2)
            report.Fail("arena registry drifted from the two saved arenas");

        bool rules = false;
        bool stagger = false;
        bool jump = false;
        bool park = false;
        bool mega = false;
        bool third = false;
        for (int i = 0; i < HowToPlay.Count; i++)
        {
            string line = HowToPlay.Line(i);
            if (line.IndexOf("Tag-back immunity 1.0 s", StringComparison.Ordinal) >= 0) rules = true;
            if (line.IndexOf("0.25 s", StringComparison.Ordinal) >= 0 && line.IndexOf("0.50 s", StringComparison.Ordinal) >= 0)
                stagger = true;
            if (line.IndexOf("Jump  [Space]", StringComparison.Ordinal) >= 0) jump = true;
            if (line.IndexOf("1  PARK", StringComparison.Ordinal) >= 0) park = true;
            if (line.IndexOf("2  Mega Park", StringComparison.Ordinal) >= 0) mega = true;
            if (line.IndexOf("3  ", StringComparison.Ordinal) >= 0) third = true;
        }
        if (!rules || !stagger || !jump || !park || !mega || third)
            report.Fail("how to play card missed a rule, a verb, or invented an arena key");

        HowToPlay.Rebuild(ActionBinds.Current, InputDeviceKind.Keyboard, 3);
        bool key3 = false;
        for (int i = 0; i < HowToPlay.Count; i++)
        {
            if (HowToPlay.Line(i) == "3  Arena") key3 = true;
        }
        if (!key3)
            report.Fail("a third registry entry would not show key 3");

        ActionBinds.Current.SetKeyboard(PlayAction.AirDash, "v");
        HowToPlay.Rebuild(ActionBinds.Current, InputDeviceKind.Keyboard, ArenaRegistry.Count);
        bool dash = false;
        for (int i = 0; i < HowToPlay.Count; i++)
        {
            if (HowToPlay.Line(i).IndexOf("Air dash  [V]", StringComparison.Ordinal) >= 0) dash = true;
        }
        if (!dash)
            report.Fail("how to play kept the old air-dash key");
    }

    static void CheckHud(OnboardingReport report)
    {
        if (!VerbHudLayout.Separated(1920f, 1080f) || !VerbHudLayout.Separated(1280f, 720f))
            report.Fail("prompts overlap the hud at 1080p or 720p");
    }

    static void CheckMenus(OnboardingReport report)
    {
        MenuCursor cursor = MenuGraph.PauseRoot();
        for (int i = 0; i < 8; i++)
            cursor = MenuGraph.Apply(cursor, PadEvent.Right);
        if (cursor.Menu != MenuId.Pause || cursor.Row != 8)
            report.Fail("pause card did not reach how to play");
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.HowToPlay)
            report.Fail("confirm did not open how to play");
        cursor = MenuGraph.Apply(cursor, PadEvent.Back);
        if (cursor.Menu != MenuId.Pause || cursor.Row != MenuGraph.ParentRow(MenuId.HowToPlay))
            report.Fail("how to play did not return to its pause row");

        cursor = MenuGraph.PauseRoot();
        for (int i = 0; i < MenuGraph.ParentRow(MenuId.Settings); i++)
            cursor = MenuGraph.Apply(cursor, PadEvent.Right);
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Settings)
            report.Fail("settings did not open");
        for (int i = 0; i < 11; i++)
            cursor = MenuGraph.Apply(cursor, PadEvent.Down);
        if (cursor.Row != 11)
            report.Fail("replay tips is not its own settings row");
        MenuCursor stayed = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (stayed.Menu != MenuId.Settings || stayed.Row != 11)
            report.Fail("replay tips left the settings card");
        cursor = MenuGraph.Apply(stayed, PadEvent.Down);
        cursor = MenuGraph.Apply(cursor, PadEvent.Confirm);
        if (cursor.Menu != MenuId.Pause)
            report.Fail("settings back is no longer the last row");
    }

    static void CheckWired(OnboardingReport report)
    {
        string hud = Read("Assets/Scripts/Onboard/PlayPromptHud.cs");
        string menu = Read("Assets/Scripts/Settings/SettingsMenuUi.cs");
        string settings = Read("Assets/Scripts/Settings/GameSettings.cs");
        string flow = Read("Assets/Scripts/Core/GameFlow.cs");
        string mode = Read("Assets/Scripts/Modes/TagModeController.cs");
        if (hud == null || menu == null || settings == null || flow == null || mode == null)
        {
            report.Fail("prompt source is missing");
            return;
        }
        string[] steps =
        {
            "HintStep.Move", "HintStep.Sprint", "HintStep.Jump", "HintStep.Slide",
            "HintStep.ClingClimb", "HintStep.WallJump", "HintStep.AirDash", "HintStep.Punch"
        };
        for (int i = 0; i < steps.Length; i++)
        {
            if (hud.IndexOf(steps[i], StringComparison.Ordinal) < 0)
                report.Fail("the hud does not watch " + steps[i]);
        }
        if (hud.IndexOf("KeyCode.F12", StringComparison.Ordinal) < 0)
            report.Fail("skip is not wired");
        if (hud.IndexOf("timeScale = 0", StringComparison.Ordinal) >= 0)
            report.Fail("prompts pause the game");
        if (hud.IndexOf("LedgeHang", StringComparison.Ordinal) >= 0 || hud.IndexOf("Shimmy", StringComparison.Ordinal) >= 0)
            report.Fail("a rejected ledge move was added");
        if (menu.IndexOf("OnboardingSession.Live.Replay()", StringComparison.Ordinal) < 0)
            report.Fail("replay tips button does not restart the sequence");
        if (settings.IndexOf("Replay tips", StringComparison.Ordinal) < 0)
            report.Fail("settings is missing replay tips");
        if (flow.IndexOf("How to play", StringComparison.Ordinal) < 0 || mode.IndexOf("How to play", StringComparison.Ordinal) < 0)
            report.Fail("pause is missing how to play");
        if (HotPathAlloc.CountFile(hud) != 0)
            report.Fail("prompt hud allocates on the hot path");
    }

    static void CheckHot(OnboardingReport report)
    {
        HotPathAlloc.Report hot = HotPathAlloc.Run();
        if (!hot.Ok || hot.After > 9)
            report.Fail("hot-path allocs moved above 9");
    }

    static void Restore()
    {
        ActionBinds.Current = ActionBinds.Defaults();
        ControlGlyphs.ResetStatics();
        PromptText.ResetStatics();
        HowToPlay.ResetStatics();
        OnboardingSession.ResetStatics();
    }

    static string Read(string path)
    {
        if (!File.Exists(path)) return null;
        return File.ReadAllText(path);
    }
}
