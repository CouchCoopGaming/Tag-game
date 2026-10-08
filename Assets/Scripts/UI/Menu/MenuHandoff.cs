using System;
using System.IO;
using Tag.Couch;
using Tag.Front;
using Tag.Level;
using Tag.Modes;
using Tag.Settings;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Keyboard and pad on mode, rules, and arena. The values are the ones
    /// TagModeController and the play scene read, not the tile label.
    /// Rematch keeps the seats, looks, and arena. Main Menu opens the title
    /// and clears the seats.
    /// </summary>
    public static class MenuHandoff
    {
        const string ModeKey = "Tag.SelectedMode";
        const string BlobKey = "Tag.GameSettingsJson";

        public static string HandoffLine(string repo)
        {
            bool mode = true;
            bool rules = true;
            bool arena = true;
            if (!Locks(repo))
            {
                mode = false;
                rules = false;
                arena = false;
            }
            else if (!Play(out bool modeOk, out bool rulesOk, out bool arenaOk))
            {
                mode = modeOk;
                rules = rulesOk;
                arena = arenaOk;
            }
            Restore();
            return "handoff mode=" + (mode ? "ok" : "no")
                + " rules=" + (rules ? "ok" : "no")
                + " arena=" + (arena ? "ok" : "no");
        }

        public static string ResultsLine(string repo)
        {
            bool rematch = false;
            bool title = false;
            if (ResultLocks(repo))
                PlayResults(out rematch, out title);
            Restore();
            return "results rematch=" + (rematch ? "ok" : "no")
                + " title=" + (title ? "ok" : "no");
        }

        static bool Play(out bool mode, out bool rules, out bool arena)
        {
            mode = true;
            rules = true;
            arena = true;
            CouchPlay.Release();
            MenuSession.Reset();
            GameSettings live = GameSettings.Defaults();
            GameSettings.Current = live;
            if (!CouchPlay.Join(CouchPlay.DeviceKeyboard) || !CouchPlay.Join(CouchPlay.DevicePad0))
            {
                mode = false;
                rules = false;
                arena = false;
                return false;
            }
            for (int kind = 0; kind < 2; kind++)
            {
                if (!Walk(kind, live, ref mode, ref rules, ref arena))
                    return false;
            }
            return mode && rules && arena;
        }

        static bool Walk(int kind, GameSettings live, ref bool mode, ref bool rules, ref bool arena)
        {
            int focus = 0;
            int count = RuleBook.Count;
            for (int i = 0; i < count; i++)
            {
                if (!MoveRules(ref focus, -1, count) && i < count - 1)
                {
                    rules = false;
                    return false;
                }
            }
            if (focus != count - 1)
            {
                rules = false;
                return false;
            }
            for (int i = 0; i < count; i++)
            {
                if (!MoveRules(ref focus, 1, count) && i < count - 1)
                {
                    rules = false;
                    return false;
                }
            }
            if (focus != 0)
            {
                rules = false;
                return false;
            }
            for (int step = 0; step < 3; step++)
            {
                if (!StepRules(live, ref focus, 1))
                {
                    mode = false;
                    return false;
                }
            }
            if (focus != 3)
            {
                mode = false;
                return false;
            }
            for (int step = 0; step < 3; step++)
            {
                if (!StepRules(live, ref focus, -1))
                {
                    mode = false;
                    return false;
                }
            }
            if (focus != 0)
            {
                mode = false;
                return false;
            }
            for (int id = 0; id < 4; id++)
            {
                focus = id;
                bool useStart = kind == 1 ? id != 0 : id == 3;
                if (!CommitMode(live, focus, useStart))
                {
                    mode = false;
                    return false;
                }
            }
            for (int row = RuleBook.Length; row < RuleBook.Arena; row++)
            {
                focus = row;
                if (!StepRules(live, ref focus, 1) || focus != row)
                {
                    rules = false;
                    return false;
                }
                if (!Commit(live) || !RulesLanded(live) || !ModeLanded(MenuSession.Mode))
                {
                    rules = false;
                    return false;
                }
                if (!StepRules(live, ref focus, -1) || focus != row)
                {
                    rules = false;
                    return false;
                }
                if (!Commit(live) || !RulesLanded(live))
                {
                    rules = false;
                    return false;
                }
            }
            focus = RuleBook.Length;
            bool confirm = kind == 0;
            if (!ActivateRules(live, ref focus, confirm, CouchPlay.Humans, out int screen))
            {
                rules = false;
                return false;
            }
            if (focus != RuleBook.Length || screen != 0)
            {
                rules = false;
                return false;
            }
            if (!StepRules(live, ref focus, -1))
            {
                rules = false;
                return false;
            }
            focus = RuleBook.Rounds;
            if (!ActivateRules(live, ref focus, !confirm, CouchPlay.Humans, out screen))
            {
                rules = false;
                return false;
            }
            if (!StepRules(live, ref focus, -1))
            {
                rules = false;
                return false;
            }
            focus = RuleBook.Back;
            if (!ActivateRules(live, ref focus, true, CouchPlay.Humans, out screen) || screen != 1)
            {
                rules = false;
                return false;
            }
            focus = RuleBook.Arena;
            if (!ActivateRules(live, ref focus, true, CouchPlay.Humans, out screen) || screen != 2)
            {
                arena = false;
                return false;
            }
            if (!RulesLanded(live) || !ModeLanded(MenuSession.Mode))
            {
                rules = false;
                mode = false;
                return false;
            }
            int held = ParkArena.Id;
            CouchPlay.Leave(CouchPlay.DevicePad0);
            MenuSession.Mode = TagModeId.LeastIt;
            if (MenuLobby.Enough(MenuSession.Mode, CouchPlay.Humans))
            {
                arena = false;
                return false;
            }
            if (ActivateArena(live, 0, CouchPlay.Humans) || ParkArena.Id != held)
            {
                arena = false;
                return false;
            }
            if (!CouchPlay.Join(CouchPlay.DevicePad0))
            {
                arena = false;
                return false;
            }
            int seen = 0;
            int cursor = 0;
            for (int i = 0; i < 5; i++)
            {
                if (MoveArena(ref cursor, 0, -1)) seen++;
            }
            if (seen < 4)
            {
                arena = false;
                return false;
            }
            int stay = cursor;
            if (MoveArena(ref cursor, 1, 0) || cursor != stay)
            {
                arena = false;
                return false;
            }
            if (MoveArena(ref cursor, -1, 0) || cursor != stay)
            {
                arena = false;
                return false;
            }
            for (int tile = 0; tile < 3; tile++)
            {
                MenuSession.RandomArena = false;
                if (!ActivateArena(live, tile, CouchPlay.Humans) || ParkArena.Id != tile)
                {
                    arena = false;
                    return false;
                }
                if (!ArenaLanded(tile) || !ModeLanded(MenuSession.Mode))
                {
                    arena = false;
                    mode = false;
                    return false;
                }
            }
            MenuSession.RandomArena = true;
            if (!ActivateArena(live, 3, CouchPlay.Humans))
            {
                arena = false;
                return false;
            }
            int rolled = ParkArena.Id;
            if (rolled < 0 || rolled >= ParkArena.Count || !ArenaLanded(rolled))
            {
                arena = false;
                return false;
            }
            if (ActivateArena(live, 4, CouchPlay.Humans) || ParkArena.Id != rolled)
            {
                arena = false;
                return false;
            }
            return true;
        }

        static bool MoveRules(ref int focus, int dy, int count)
        {
            int next = focus + (dy > 0 ? -1 : 1);
            if (next < 0) next = 0;
            if (next >= count) next = count - 1;
            if (next == focus) return false;
            focus = next;
            return true;
        }

        static bool StepRules(GameSettings s, ref int focus, int dir)
        {
            if (focus <= 3)
            {
                int next = focus + (dir > 0 ? 1 : -1);
                if (next < 0) next = 0;
                if (next > 3) next = 3;
                if (next == focus) return false;
                focus = next;
                return true;
            }
            return RuleBook.Edit(s, focus, dir);
        }

        static bool ActivateRules(GameSettings s, ref int focus, bool commit, int humans, out int screen)
        {
            screen = 0;
            if (focus <= 3)
            {
                MenuSession.Mode = (TagModeId)focus;
                return Commit(s) && ModeLanded(MenuSession.Mode);
            }
            if (focus == RuleBook.Arena)
            {
                screen = 2;
                return Commit(s);
            }
            if (focus >= RuleBook.Back)
            {
                screen = 1;
                return true;
            }
            if (!StepRules(s, ref focus, 1)) return false;
            return Commit(s) && RulesLanded(s);
        }

        static bool MoveArena(ref int focus, int dx, int dy)
        {
            int count = 5;
            int cols = 1;
            int rows = count;
            int x = focus % cols;
            int y = focus / cols;
            if (dx != 0) x = (x + (dx > 0 ? 1 : -1) + cols) % cols;
            if (dy != 0) y = (y + (dy > 0 ? -1 : 1) + rows) % rows;
            int next = y * cols + x;
            if (next >= count) next = count - 1;
            if (next < 0) next = 0;
            if (next == focus) return false;
            focus = next;
            return true;
        }

        static bool ActivateArena(GameSettings live, int focus, int humans)
        {
            if (focus >= 4) return false;
            if (!MenuLobby.Enough(MenuSession.Mode, humans)) return false;
            MenuSession.RandomArena = focus == 3;
            if (!MenuSession.RandomArena) MenuSession.Arena = focus;
            int arena = ResolveArena();
            MenuSession.Arena = arena;
            ParkArena.Select(arena);
            ParkArena.HasExplicitChoice = true;
            live.Arena = arena;
            live.Clamp();
            if (!Commit(live)) return false;
            return ArenaLanded(arena);
        }

        static int ResolveArena()
        {
            if (!MenuSession.RandomArena)
            {
                int id = MenuSession.Arena;
                if (id < 0) id = 0;
                if (id >= ParkArena.Count) id = 0;
                return id;
            }
            return UnityEngine.Random.Range(0, ParkArena.Count);
        }

        static bool CommitMode(GameSettings live, int focus, bool start)
        {
            if (!start && focus < 0) return false;
            MenuSession.Mode = (TagModeId)focus;
            if (!Commit(live)) return false;
            return ModeLanded(MenuSession.Mode) && RulesLanded(live);
        }

        static bool Commit(GameSettings live)
        {
            live.Arena = MenuSession.Arena;
            live.Clamp();
            GameSettings.Current = live;
            ActionBinds binds = ActionBinds.Current ?? ActionBinds.Defaults();
            string json = SettingsFile.Write(live, binds);
            PlayerPrefs.SetString(BlobKey, json);
            PlayerPrefs.SetInt(ModeKey, (int)MenuSession.Mode);
            PlayerPrefs.SetInt(ParkArena.PrefsKey, MenuSession.Arena);
            PlayerPrefs.Save();
            return json.IndexOf("mode=", StringComparison.Ordinal) >= 0;
        }

        static bool ModeLanded(TagModeId want)
        {
            if (!PlayerPrefs.HasKey(ModeKey)) return false;
            var got = (TagModeId)PlayerPrefs.GetInt(ModeKey, (int)TagModeId.LeastIt);
            return got == want && MenuSession.Mode == want;
        }

        static bool RulesLanded(GameSettings live)
        {
            var back = GameSettings.Defaults();
            SettingsFile.Read(PlayerPrefs.GetString(BlobKey, ""), back, ActionBinds.Defaults());
            if (Math.Abs(back.RoundSeconds() - live.RoundSeconds()) > 0.01f) return false;
            if (back.RoundsPerMatch != live.RoundsPerMatch) return false;
            if (back.WinTarget != live.WinTarget) return false;
            if (back.StartIt != live.StartIt) return false;
            if (back.StartSeat != live.StartSeat) return false;
            if (back.HazardPads != live.HazardPads || back.HazardZips != live.HazardZips) return false;
            if (back.AiOpponents != live.AiOpponents || back.DifficultyTier != live.DifficultyTier) return false;
            if (back.SplitAxis != live.SplitAxis || back.Listener != live.Listener) return false;
            if (back.RoundLengthIndex != live.RoundLengthIndex) return false;
            if (back.Arena != MenuSession.Arena) return false;
            for (int i = 0; i < GameSettings.SeatCount; i++)
            {
                if (back.Handicap[i] != live.Handicap[i]) return false;
            }
            return MenuSession.Mode == (TagModeId)PlayerPrefs.GetInt(ModeKey, -1);
        }

        static bool ArenaLanded(int id)
        {
            if (ParkArena.Id != id || !ParkArena.HasExplicitChoice) return false;
            if (!PlayerPrefs.HasKey(ParkArena.PrefsKey)) return false;
            if (PlayerPrefs.GetInt(ParkArena.PrefsKey, -1) != id) return false;
            ParkArena.HasExplicitChoice = false;
            ParkArena.ApplySaved(
                PlayerPrefs.HasKey(ParkArena.PrefsKey),
                PlayerPrefs.GetInt(ParkArena.PrefsKey, ParkArena.Mega),
                ParkArena.Mega);
            return ParkArena.Id == id && ParkArena.HasExplicitChoice;
        }

        static bool PlayResults(out bool rematch, out bool title)
        {
            rematch = false;
            title = false;
            CouchPlay.Release();
            MenuSession.Reset();
            if (!CouchPlay.Join(CouchPlay.DeviceKeyboard)) return false;
            if (!CouchPlay.Join(CouchPlay.DevicePad0)) return false;
            if (!CouchPlay.Join(CouchPlay.DevicePad1)) return false;
            if (!CouchPlay.Join(CouchPlay.DevicePad2)) return false;
            if (CouchPlay.Humans != 4) return false;
            int[] hier = { 5, 0, 2, 3 };
            int[] accent = { 4, 1, 3, 1 };
            for (int i = 0; i < 4; i++)
            {
                MenuSession.Hier[i] = hier[i];
                MenuSession.Accent[i] = accent[i];
                MenuSession.Hat[i] = i == 1 ? 1 : 0;
            }
            MenuSession.Mode = TagModeId.TrailTag;
            MenuSession.RandomArena = false;
            MenuSession.Arena = ParkArena.Stack;
            ParkArena.Select(ParkArena.Stack);
            ParkArena.HasExplicitChoice = true;
            GameSettings live = GameSettings.Defaults();
            live.Arena = ParkArena.Stack;
            if (!Commit(live)) return false;
            int[] devices = new int[4];
            for (int i = 0; i < 4; i++) devices[i] = CouchPlay.DeviceOf(i);
            if (!SameParty(hier, accent, devices, ParkArena.Stack, TagModeId.TrailTag)) return false;
            rematch = true;
            FrontSession.ShowTitle();
            title = CouchPlay.Humans == 0
                && CouchPlay.Leftovers == 0
                && FrontSession.Screen == FrontScreen.Title
                && !FrontSession.Armed;
            for (int i = 0; i < 4; i++)
            {
                if (CouchPlay.HumanAt(i)) title = false;
            }
            return rematch && title;
        }

        static bool SameParty(int[] hier, int[] accent, int[] devices, int arena, TagModeId mode)
        {
            if (CouchPlay.Humans != 4) return false;
            if (ParkArena.Id != arena || MenuSession.Arena != arena) return false;
            if (MenuSession.Mode != mode) return false;
            if (!ModeLanded(mode)) return false;
            if (PlayerPrefs.GetInt(ParkArena.PrefsKey, -1) != arena) return false;
            for (int i = 0; i < 4; i++)
            {
                if (!CouchPlay.HumanAt(i)) return false;
                if (CouchPlay.DeviceOf(i) != devices[i]) return false;
                if (MenuSession.Hier[i] != hier[i] || MenuSession.Accent[i] != accent[i]) return false;
            }
            return true;
        }

        static void Restore()
        {
            CouchPlay.Release();
            MenuSession.Reset();
            GameSettings.Current = GameSettings.Defaults();
            ParkArena.Select(ParkArena.Mega);
            ParkArena.HasExplicitChoice = false;
        }

        static bool Locks(string repo)
        {
            string host = Read(repo, "Assets/Scripts/UI/Menu/MenuHost.cs");
            string match = Read(repo, "Assets/Scripts/UI/Menu/MenuMatch.cs");
            string mode = Read(repo, "Assets/Scripts/Modes/TagModeController.cs");
            string flow = Read(repo, "Assets/Scripts/Core/GameFlow.cs");
            string live = Read(repo, "Assets/Scripts/Front/FrontLive.cs");
            string boot = Read(repo, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
            string runtime = Read(repo, "Assets/Scripts/Settings/SettingsRuntime.cs");
            if (host.Length == 0 || match.Length == 0 || mode.Length == 0 || flow.Length == 0) return false;
            string rules = Slice(host, "void ActivateRules()", "void ActivateArena()");
            if (rules.IndexOf("_focus == RuleBook.Arena", StringComparison.Ordinal) < 0) return false;
            if (rules.IndexOf("_focus >= RuleBook.Back", StringComparison.Ordinal) < 0) return false;
            if (rules.IndexOf("MenuSession.Mode", StringComparison.Ordinal) < 0) return false;
            if (rules.IndexOf("StepRules(1)", StringComparison.Ordinal) < 0) return false;
            if (rules.IndexOf("_focus == 10", StringComparison.Ordinal) >= 0) return false;
            string tick = Slice(host, "void TickRules()", "void TickArena()");
            if (tick.IndexOf("MoveRules", StringComparison.Ordinal) < 0) return false;
            if (tick.IndexOf("StepRules", StringComparison.Ordinal) < 0) return false;
            if (tick.IndexOf("ArmActivate", StringComparison.Ordinal) < 0) return false;
            string arena = Slice(host, "void TickArena()", "void TickLoading()");
            if (arena.IndexOf("ArmActivate", StringComparison.Ordinal) < 0) return false;
            string act = Slice(host, "void ActivateArena()", "void ActivatePause()");
            if (act.IndexOf("MenuSession.Arena", StringComparison.Ordinal) < 0) return false;
            if (act.IndexOf("BeginLoading(false)", StringComparison.Ordinal) < 0) return false;
            if (act.IndexOf("RandomArena", StringComparison.Ordinal) < 0) return false;
            if (match.IndexOf("PlayerPrefs.SetInt(TagModeController.PrefsModeKey, (int)MenuSession.Mode)", StringComparison.Ordinal) < 0) return false;
            if (match.IndexOf("flow.SyncSelectedMode(MenuSession.Mode)", StringComparison.Ordinal) < 0) return false;
            if (match.IndexOf("ParkArena.Select(arena)", StringComparison.Ordinal) < 0) return false;
            if (match.IndexOf("flow.BeginFromMenu(true)", StringComparison.Ordinal) < 0) return false;
            if (match.IndexOf("Random.Range(0, ParkArena.Count)", StringComparison.Ordinal) < 0) return false;
            if (match.IndexOf("PlayerPrefs.SetInt(ParkArena.PrefsKey, arena)", StringComparison.Ordinal) < 0) return false;
            if (mode.IndexOf("PrefsModeKey = \"Tag.SelectedMode\"", StringComparison.Ordinal) < 0) return false;
            if (mode.IndexOf("selectedMode = GameFlow.Instance.SelectedMode", StringComparison.Ordinal) < 0) return false;
            if (mode.IndexOf("PlayerPrefs.GetInt(PrefsModeKey", StringComparison.Ordinal) < 0) return false;
            if (mode.IndexOf("leastItTuning.roundDuration = duration", StringComparison.Ordinal) < 0) return false;
            if (mode.IndexOf("leastItTuning.roundCount = rounds", StringComparison.Ordinal) < 0) return false;
            if (flow.IndexOf("SceneManager.LoadScene(playSceneName)", StringComparison.Ordinal) < 0) return false;
            if (live.IndexOf("ApplyRound(s.RoundSeconds(), s.RoundsPerMatch)", StringComparison.Ordinal) < 0) return false;
            if (boot.IndexOf("ParkArena.ApplySaved", StringComparison.Ordinal) < 0) return false;
            if (runtime.IndexOf("PrefsKey = \"Tag.GameSettingsJson\"", StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static bool ResultLocks(string repo)
        {
            string host = Read(repo, "Assets/Scripts/UI/Menu/MenuHost.cs");
            string flow = Read(repo, "Assets/Scripts/Core/GameFlow.cs");
            string mode = Read(repo, "Assets/Scripts/Modes/TagModeController.cs");
            string life = Read(repo, "Assets/Scripts/Core/StaticLifecycle.cs");
            string front = Read(repo, "Assets/Scripts/Front/FrontSession.cs");
            if (host.Length == 0 || flow.Length == 0 || mode.Length == 0) return false;
            string results = Slice(host, "void ActivateResults()", "void ActivateOptions()");
            if (results.IndexOf("QuitMatch()", StringComparison.Ordinal) < 0) return false;
            if (results.IndexOf("RememberRules()", StringComparison.Ordinal) < 0) return false;
            if (results.IndexOf(".Rematch()", StringComparison.Ordinal) < 0) return false;
            string quit = Slice(host, "void QuitMatch()", "void QuitApp()");
            if (quit.IndexOf("QuitToMenu()", StringComparison.Ordinal) < 0) return false;
            if (quit.IndexOf("ShowTitle()", StringComparison.Ordinal) < 0) return false;
            string title = Slice(host, "public void ShowTitle()", "public void HideForMatch()");
            if (title.IndexOf("CouchPlay.Release()", StringComparison.Ordinal) < 0) return false;
            if (title.IndexOf("Open(MenuScreenId.Title)", StringComparison.Ordinal) < 0) return false;
            string again = Slice(flow, "public void Rematch()", "public void ReturnToPlay()");
            if (again.IndexOf("modeController.Rematch()", StringComparison.Ordinal) < 0) return false;
            if (again.IndexOf("CouchPlay.Release", StringComparison.Ordinal) >= 0) return false;
            if (again.IndexOf("ParkArena.Select", StringComparison.Ordinal) >= 0) return false;
            string round = Slice(mode, "public void Rematch()", "void EnterPostRound");
            if (round.IndexOf("StartRound(selectedMode)", StringComparison.Ordinal) < 0) return false;
            if (round.IndexOf("ParkArena.Select", StringComparison.Ordinal) >= 0) return false;
            if (flow.IndexOf("StaticLifecycle.ReleaseMatch()", StringComparison.Ordinal) < 0) return false;
            if (life.IndexOf("CouchPlay.Release()", StringComparison.Ordinal) < 0) return false;
            if (front.IndexOf("CouchPlay.Release()", StringComparison.Ordinal) < 0) return false;
            if (front.IndexOf("FrontScreen.Title", StringComparison.Ordinal) < 0) return false;
            return true;
        }

        static string Slice(string text, string start, string end)
        {
            int a = text.IndexOf(start, StringComparison.Ordinal);
            if (a < 0) return "";
            int b = text.IndexOf(end, a + start.Length, StringComparison.Ordinal);
            if (b < 0) return text.Substring(a);
            return text.Substring(a, b - a);
        }

        static string Read(string repo, string rel)
        {
            if (string.IsNullOrEmpty(repo)) return "";
            string path = Path.Combine(repo, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return "";
            return File.ReadAllText(path);
        }
    }
}
