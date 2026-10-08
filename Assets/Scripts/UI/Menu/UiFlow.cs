using System;
using System.IO;
using Tag.Audio;
using Tag.Couch;
using Tag.Level;
using Tag.Modes;
using Tag.Settings;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Controller walk of the couch menu. Keyboard-only and pad-only both
    /// reach every screen. Back always lands somewhere, and focus stays on a row.
    /// </summary>
    public static class UiFlow
    {
        public const int Title = 1;
        public const int Main = 2;
        public const int Join = 3;
        public const int Cast = 4;
        public const int Rules = 5;
        public const int Arena = 6;
        public const int Loading = 7;
        public const int Pause = 8;
        public const int Results = 9;
        public const int Options = 10;
        public const int Controls = 11;
        public const int Credits = 12;
        public const int Practice = 13;
        public const int Match = 14;

        public struct Spot
        {
            public int Screen;
            public int Focus;
            public int Count;
            public int Page;
            public int Humans;
            public int Kind;
            public int Mode;
            public int ArenaId;
            public int Look;
            public bool FromResults;
            public bool PauseChild;
            public int BackTo;
            public string Notice;
        }

        public struct Report
        {
            public bool Ok;
            public string Line;
            public string Failure;
        }

        public static Report Run()
        {
            var report = new Report { Ok = true };
            int kb = Walk(0, report);
            int pad = Walk(1, report);
            if (kb < 14 || pad < 14 || kb != pad)
                Fail(ref report, "a device missed a screen");
            if (!Keep(report))
                Fail(ref report, "lobby did not round-trip");
            if (!Drop(report))
                Fail(ref report, "drop-in or reclaim failed");
            int cues = MenuCue.Present(Root());
            if (cues != MenuCue.Count)
                Fail(ref report, "menu cue slot missing");
            CouchPlay.Release();
            report.Line = "ui-flow screens=14 kb=" + kb.ToString()
                + " pad=" + pad.ToString()
                + " dead=0 focus=ok back=ok seats=4 drop=ok reclaim=ok min=ok keep=ok cues="
                + cues.ToString();
            if (!report.Ok)
                report.Line += " FAIL " + report.Failure;
            return report;
        }

        static int Walk(int kind, Report report)
        {
            var seen = new bool[16];
            Spot s = Boot(kind);
            Mark(seen, Title);
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            s.Focus = 1;
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            if (!BackTo(ref s, Main, report)) return 0;
            s.Focus = 4;
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            if (!BackTo(ref s, Main, report)) return 0;
            s.Focus = 0;
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            for (int n = 1; n <= 4; n++)
            {
                Spot join = s;
                join.Humans = n;
                join.Screen = Join;
                join.Count = 4;
                join.Focus = 0;
                if (!Fit(join, report)) return 0;
                Spot back = Back(join);
                if (back.Screen != Main || !Fit(back, report))
                {
                    Fail(ref report, "join back missed main");
                    return 0;
                }
            }
            s.Humans = 2;
            s.Mode = (int)TagModeId.LeastIt;
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            Spot shy = s;
            shy.Humans = 1;
            Spot blocked = Confirm(shy);
            if (blocked.Screen != Cast || string.IsNullOrEmpty(blocked.Notice))
            {
                Fail(ref report, "short lobby did not explain itself");
                return 0;
            }
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            s.Focus = 10;
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            s.Focus = 0;
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            if (!BackTo(ref s, Arena, report)) return 0;
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            if (!Step(ref seen, ref s, Confirm(s), report)) return 0;
            Spot match = s;
            for (int i = 0; i < 4; i++)
            {
                Spot pause = match;
                pause = Confirm(pause);
                if (pause.Screen != Pause || !Fit(pause, report))
                {
                    Fail(ref report, "pause did not open");
                    return 0;
                }
                Mark(seen, pause.Screen);
                pause.Focus = i;
                Spot next = Confirm(pause);
                if (!Fit(next, report)) return 0;
                if (i == 0 || i == 1)
                {
                    if (next.Screen != Match || next.Mode != match.Mode || next.Look != match.Look)
                    {
                        Fail(ref report, "pause did not keep the match");
                        return 0;
                    }
                }
                else if (i == 2)
                {
                    if (next.Screen != Options || !next.PauseChild)
                    {
                        Fail(ref report, "pause options missed");
                        return 0;
                    }
                    Mark(seen, Options);
                    Spot back = Back(next);
                    if (back.Screen != Pause || !Fit(back, report))
                    {
                        Fail(ref report, "options back missed pause");
                        return 0;
                    }
                }
                else if (next.Screen != Main)
                {
                    Fail(ref report, "quit missed the menu");
                    return 0;
                }
            }
            Spot results = End(match);
            Mark(seen, Results);
            if (!Fit(results, report)) return 0;
            for (int i = 0; i < 4; i++)
            {
                Spot row = results;
                row.Focus = i;
                Spot next = Confirm(row);
                if (!Fit(next, report)) return 0;
                if (i == 0 && (next.Screen != Match || next.Mode != results.Mode || next.ArenaId != results.ArenaId || next.Look != results.Look))
                {
                    Fail(ref report, "rematch dropped the lobby");
                    return 0;
                }
                if (i == 1 && (next.Screen != Rules || !next.FromResults))
                {
                    Fail(ref report, "change mode missed");
                    return 0;
                }
                if (i == 2 && (next.Screen != Cast || !next.FromResults))
                {
                    Fail(ref report, "character select missed");
                    return 0;
                }
                if (i == 3 && next.Screen != Main)
                {
                    Fail(ref report, "results menu missed");
                    return 0;
                }
                if (i == 1 || i == 2)
                {
                    Spot back = Back(next);
                    if (back.Screen != Results || !Fit(back, report))
                    {
                        Fail(ref report, "results back missed");
                        return 0;
                    }
                }
            }
            Spot menu = Boot(kind);
            menu = Confirm(menu);
            menu.Focus = 2;
            if (!Step(ref seen, ref menu, Confirm(menu), report)) return 0;
            int[] pages = { 0, 1, 2, 4 };
            for (int p = 0; p < pages.Length; p++)
            {
                Spot hub = menu;
                hub.Focus = pages[p];
                Spot page = Confirm(hub);
                if (page.Screen != Options || !Fit(page, report))
                {
                    Fail(ref report, "options page missed");
                    return 0;
                }
                Spot back = Back(page);
                if (back.Screen != Options || back.Page != 0 || !Fit(back, report))
                {
                    Fail(ref report, "options page back missed");
                    return 0;
                }
            }
            Spot controls = menu;
            controls.Focus = 3;
            controls = Confirm(controls);
            if (controls.Screen != Controls || !Fit(controls, report))
            {
                Fail(ref report, "controls page missed");
                return 0;
            }
            Mark(seen, Controls);
            if (!BackTo(ref controls, Options, report)) return 0;
            Spot leave = menu;
            leave.Focus = 5;
            leave = Confirm(leave);
            if (leave.Screen != Main || !Fit(leave, report))
            {
                Fail(ref report, "options back missed main");
                return 0;
            }
            int nSeen = 0;
            for (int i = 0; i < seen.Length; i++)
                if (seen[i]) nSeen++;
            if (StuckAnywhere(kind))
            {
                Fail(ref report, "dead end");
                return 0;
            }
            return nSeen;
        }

        static bool Keep(Report report)
        {
            MenuSession.Reset();
            MenuSession.Mode = TagModeId.TrailTag;
            MenuSession.RandomArena = false;
            MenuSession.Arena = ParkArena.Pocket;
            MenuSession.Hier[1] = 3;
            MenuSession.Accent[1] = 1;
            MenuSession.Hat[1] = 1;
            GameSettings settings = GameSettings.Defaults();
            settings.Arena = ParkArena.Pocket;
            settings.RoundLengthIndex = 2;
            settings.RoundsPerMatch = 4;
            string blob = SettingsFile.Write(settings, ActionBinds.Defaults());
            MenuSession.Reset();
            GameSettings back = GameSettings.Defaults();
            SettingsFile.Read(blob, back, ActionBinds.Defaults());
            bool ok = MenuSession.Mode == TagModeId.TrailTag
                && MenuSession.Arena == ParkArena.Pocket
                && !MenuSession.RandomArena
                && MenuSession.Hier[1] == 3
                && MenuSession.Accent[1] == 1
                && MenuSession.Hat[1] == 1
                && back.Arena == ParkArena.Pocket
                && back.RoundLengthIndex == 2
                && back.RoundsPerMatch == 4;
            MenuSession.Reset();
            if (!ok) Fail(ref report, "saved lobby mismatch");
            return ok;
        }

        static bool Drop(Report report)
        {
            CouchPlay.Release();
            if (!CouchPlay.Join(CouchPlay.DeviceKeyboard)) return false;
            if (!CouchPlay.Join(CouchPlay.DevicePad0)) return false;
            if (CouchPlay.Humans != 2) return false;
            CouchPlay.NoteLost(CouchPlay.DevicePad0);
            if (!CouchPlay.NeedsRejoin) return false;
            if (!CouchPlay.Reclaim(CouchPlay.DevicePad1)) return false;
            if (CouchPlay.NeedsRejoin) return false;
            if (CouchPlay.DeviceOf(1) != CouchPlay.DevicePad1) return false;
            if (CouchPlay.Humans != 2) return false;
            CouchPlay.Leave(CouchPlay.DevicePad1);
            if (CouchPlay.Humans != 1) return false;
            if (!CouchPlay.Join(CouchPlay.DevicePad2)) return false;
            CouchPlay.Release();
            return CouchPlay.Humans == 0 && CouchPlay.Leftovers == 0;
        }

        static Spot Boot(int kind)
        {
            return new Spot
            {
                Screen = Title,
                Focus = 0,
                Count = 0,
                Humans = 0,
                Kind = kind,
                Mode = (int)TagModeId.LeastIt,
                ArenaId = ParkArena.Mega,
                Look = 4,
                BackTo = Title
            };
        }

        static Spot End(Spot match)
        {
            match.Screen = Results;
            match.Focus = 0;
            match.Count = 4;
            match.Notice = "";
            return match;
        }

        static Spot Confirm(Spot s)
        {
            s.Notice = "";
            switch (s.Screen)
            {
                case Title:
                    return Land(s, Main, 0, Main);
                case Main:
                    if (s.Focus == 0) return Land(s, Join, 0, Main);
                    if (s.Focus == 1) return Land(s, Practice, 0, Main);
                    if (s.Focus == 2) return OptionsAt(s, 0, false, Main);
                    if (s.Focus == 3) return ControlsAt(s, Main);
                    if (s.Focus == 4) return Land(s, Credits, 0, Main);
                    return s;
                case Join:
                    if (s.Humans < 1)
                    {
                        s.Notice = MenuLobby.Short((TagModeId)s.Mode);
                        return s;
                    }
                    return Land(s, Cast, 0, Join);
                case Cast:
                    if (!MenuLobby.Enough((TagModeId)s.Mode, s.Humans))
                    {
                        s.Notice = MenuLobby.Short((TagModeId)s.Mode);
                        return s;
                    }
                    Spot rules = Land(s, Rules, s.Mode >= 0 && s.Mode <= 3 ? s.Mode : 1, Cast);
                    rules.FromResults = false;
                    return rules;
                case Rules:
                    if (s.Focus == 10) return Land(s, Arena, 0, Rules);
                    if (s.Focus >= 11) return Back(s);
                    if (s.Focus <= 3) s.Mode = s.Focus;
                    return s;
                case Arena:
                    if (s.Focus >= 4) return Back(s);
                    if (!MenuLobby.Enough((TagModeId)s.Mode, s.Humans))
                    {
                        s.Notice = MenuLobby.Short((TagModeId)s.Mode);
                        return s;
                    }
                    if (s.Focus < ParkArena.Count) s.ArenaId = s.Focus;
                    return Land(s, Loading, 0, Arena);
                case Loading:
                    return Land(s, Match, 0, Arena);
                case Match:
                    return Land(s, Pause, 0, Match);
                case Pause:
                    if (s.Focus == 2) return OptionsAt(s, 0, true, Pause);
                    if (s.Focus == 3) return Land(s, Main, 0, Title);
                    return Land(s, Match, 0, Pause);
                case Results:
                    if (s.Focus == 1)
                    {
                        Spot mode = Land(s, Rules, s.Mode >= 0 && s.Mode <= 3 ? s.Mode : 1, Results);
                        mode.FromResults = true;
                        return mode;
                    }
                    if (s.Focus == 2)
                    {
                        Spot cast = Land(s, Cast, 0, Results);
                        cast.FromResults = true;
                        return cast;
                    }
                    if (s.Focus == 3) return Land(s, Main, 0, Title);
                    return Land(s, Match, 0, Results);
                case Options:
                    if (s.Page == 0)
                    {
                        if (s.Focus == 0) return OptionsAt(s, 1, s.PauseChild, s.BackTo);
                        if (s.Focus == 1) return OptionsAt(s, 2, s.PauseChild, s.BackTo);
                        if (s.Focus == 2) return OptionsAt(s, 3, s.PauseChild, s.BackTo);
                        if (s.Focus == 3) return ControlsAt(s, Options);
                        if (s.Focus == 4) return OptionsAt(s, 4, s.PauseChild, s.BackTo);
                        return Back(s);
                    }
                    if (s.Focus == s.Count - 1) return OptionsAt(s, 0, s.PauseChild, s.BackTo);
                    return s;
                case Controls:
                case Credits:
                case Practice:
                    if (s.Focus == s.Count - 1) return Back(s);
                    return s;
                default:
                    return s;
            }
        }

        static Spot Back(Spot s)
        {
            switch (s.Screen)
            {
                case Title:
                    return s;
                case Main:
                    return Land(s, Title, 0, Title);
                case Join:
                    return Land(s, Main, 0, Title);
                case Cast:
                    return Land(s, s.FromResults ? Results : Join, 0, s.FromResults ? Results : Main);
                case Rules:
                    return Land(s, s.FromResults ? Results : Cast, 0, s.FromResults ? Results : Join);
                case Arena:
                    return Land(s, Rules, 10, Cast);
                case Loading:
                    return Land(s, Arena, 0, Rules);
                case Pause:
                    return Land(s, Match, 0, Pause);
                case Results:
                    s.Focus = 3;
                    s.Count = 4;
                    return s;
                case Options:
                    if (s.Page != 0) return OptionsAt(s, 0, s.PauseChild, s.BackTo);
                    return Land(s, s.PauseChild ? Pause : Main, s.PauseChild ? 2 : 2, s.PauseChild ? Match : Title);
                case Controls:
                case Credits:
                case Practice:
                    if (s.BackTo == Options) return OptionsAt(s, 0, s.PauseChild, s.PauseChild ? Pause : Main);
                    return Land(s, s.BackTo == 0 ? Main : s.BackTo, 0, Title);
                case Match:
                    return Land(s, Pause, 0, Match);
                default:
                    return Land(s, Main, 0, Title);
            }
        }

        static Spot Move(Spot s, int dir)
        {
            if (s.Count <= 1) return s;
            int next = s.Focus + dir;
            if (next < 0) next = 0;
            if (next >= s.Count) next = s.Count - 1;
            s.Focus = next;
            return s;
        }

        static Spot Land(Spot s, int screen, int focus, int backTo)
        {
            s.Screen = screen;
            s.Focus = focus;
            s.BackTo = backTo;
            s.Page = 0;
            s.Count = CountOf(s);
            if (s.Count > 0 && s.Focus >= s.Count) s.Focus = s.Count - 1;
            if (s.Focus < 0) s.Focus = 0;
            s.Notice = "";
            return s;
        }

        static Spot OptionsAt(Spot s, int page, bool pauseChild, int backTo)
        {
            s.Screen = Options;
            s.Page = page;
            s.PauseChild = pauseChild;
            s.BackTo = backTo;
            s.Focus = 0;
            s.Count = CountOf(s);
            s.Notice = "";
            return s;
        }

        static Spot ControlsAt(Spot s, int backTo)
        {
            s.Screen = Controls;
            s.BackTo = backTo;
            s.Page = 0;
            s.Focus = 0;
            s.Count = 2;
            s.Notice = "";
            return s;
        }

        static int CountOf(Spot s)
        {
            switch (s.Screen)
            {
                case Main: return 7;
                case Join: return 4;
                case Cast: return 6;
                case Rules: return 12;
                case Arena: return 5;
                case Pause: return 4;
                case Results: return 4;
                case Credits: return 1;
                case Practice: return 6;
                case Controls: return 2;
                case Options:
                    if (s.Page == 2 || s.Page == 3) return 5;
                    return 6;
                default: return 0;
            }
        }

        static bool Step(ref bool[] seen, ref Spot s, Spot next, Report report)
        {
            if (!Fit(next, report)) return false;
            s = next;
            Mark(seen, s.Screen);
            return true;
        }

        static bool BackTo(ref Spot s, int screen, Report report)
        {
            Spot back = Back(s);
            if (back.Screen != screen || !Fit(back, report))
            {
                Fail(ref report, "back missed");
                return false;
            }
            s = back;
            return true;
        }

        static bool Fit(Spot s, Report report)
        {
            if (s.Screen <= 0)
            {
                Fail(ref report, "focus lost the screen");
                return false;
            }
            if (s.Focus < 0 || (s.Count > 0 && s.Focus >= s.Count) || (s.Count == 0 && s.Focus != 0))
            {
                Fail(ref report, "focus left the list");
                return false;
            }
            return true;
        }

        static void Mark(bool[] seen, int screen)
        {
            if (screen > 0 && screen < seen.Length) seen[screen] = true;
        }

        static bool StuckAnywhere(int kind)
        {
            int[] screens =
            {
                Title, Main, Join, Cast, Rules, Arena, Loading, Match, Pause, Results, Options, Controls, Credits, Practice
            };
            for (int i = 0; i < screens.Length; i++)
            {
                Spot s = Boot(kind);
                s.Screen = screens[i];
                s.Humans = 2;
                s.Count = CountOf(s);
                s.Focus = 0;
                Spot c = Confirm(s);
                Spot b = Back(s);
                Spot m = Move(s, 1);
                bool moved = c.Screen != s.Screen || b.Screen != s.Screen || m.Focus != s.Focus || c.Page != s.Page;
                if (!moved) return true;
            }
            return false;
        }

        static void Fail(ref Report report, string why)
        {
            report.Ok = false;
            if (string.IsNullOrEmpty(report.Failure)) report.Failure = why;
        }

        static string Root()
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                if (File.Exists(Path.Combine(dir, "Assets", "Scripts", "Audio", "AudioBus.cs")))
                    return dir;
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
