using System;
using System.IO;
using Tag.Couch;
using Tag.Modes;
using Tag.Onboard;
using Tag.Settings;

namespace Tag.Front
{
    public enum FrontScreen
    {
        Title = 0,
        Setup = 1,
        Settings = 2,
        HowTo = 3,
        Playing = 4,
        Results = 5,
        Quit = 6,
        Join = 7
    }

    /// <summary>
    /// Title, match setup, and results. The Play scene does not start a round
    /// until setup confirms. Scores are copied off RoundFlow before the next
    /// countdown wipes them. Round rules stay in RoundFlow.
    /// </summary>
    public static class FrontSession
    {
        public const string GameName = "TAG";
        public const int ActNone = 0;
        public const int ActSetup = 1;
        public const int ActSettings = 2;
        public const int ActHowTo = 3;
        public const int ActStart = 4;
        public const int ActQuit = 5;
        public const int ActTitle = 6;
        public const int ActRematch = 7;
        public const int ActJoin = 8;
        public const int SoundMove = 1;
        public const int SoundConfirm = 2;
        public const int SoundBack = 3;

        public static FrontScreen Screen = FrontScreen.Title;
        public static int Row;
        public static bool Armed;
        public static int Live { get; private set; }
        public static int BeforeRelease { get; private set; }
        public static int LastRelease { get; private set; }
        public static int Moves;
        public static int Confirms;
        public static int Backs;
        public static int RoundsPlayed;
        public static int Winner = -1;
        public const int WinnerTie = -2;
        public static int Hosts { get; private set; }
        public static float Longest;
        public static string Failure = "";

        static readonly float[] _time = new float[2];
        static readonly int[] _tags = new int[2];
        static readonly string[] _rows = new string[8];
        static readonly string[] _digits = { "0", "1", "2", "3", "4", "5" };
        static readonly string[] _lengths = { "60s", "120s", "180s", "300s" };
        static int _sound;
        static bool _dirty;
        static RoundFlow.State _round;
        static string _name0 = "You";
        static string _name1 = "Dummy";

        public static int RowCount
        {
            get
            {
                if (Screen == FrontScreen.Setup) return 7;
                if (Screen == FrontScreen.Join) return 4;
                if (Screen == FrontScreen.Results) return 3;
                if (Screen == FrontScreen.Title || Screen == FrontScreen.Quit) return 4;
                return 1;
            }
        }

        public static string RowText(int index)
        {
            if (index < 0 || index >= _rows.Length) return "";
            return _rows[index] ?? "";
        }

        public static void ResetStatics()
        {
            Moves = 0;
            Confirms = 0;
            Backs = 0;
            _sound = 0;
            _dirty = false;
            Failure = "";
            ShowTitle();
        }

        public static void ShowTitle()
        {
            ReleaseObjects();
            CouchPlay.Release();
            Armed = false;
            Screen = FrontScreen.Title;
            Row = 0;
            _round = null;
            Rebuild();
        }

        public static void ShowSetup()
        {
            ReleaseObjects();
            CouchPlay.Release();
            Armed = false;
            Screen = FrontScreen.Setup;
            Row = 0;
            _round = null;
            Rebuild();
        }

        public static int ConsumeSound()
        {
            int s = _sound;
            _sound = 0;
            return s;
        }

        public static bool ConsumeDirty()
        {
            bool d = _dirty;
            _dirty = false;
            return d;
        }

        public static void Highlight(int index)
        {
            int max = RowCount - 1;
            if (index < 0) index = 0;
            if (index > max) index = max;
            if (index == Row) return;
            Row = index;
            Note(SoundMove);
            Rebuild();
        }

        public static void Nudge(int dir)
        {
            Highlight(Row + dir);
        }

        public static void Step(int dir)
        {
            if (dir == 0) return;
            if (Screen == FrontScreen.Join)
            {
                EnsureSettings();
                GameSettings seat = GameSettings.Current;
                if (Row == 0)
                    seat.SplitAxis = seat.SplitAxis == GameSettings.SplitHorizontal
                        ? GameSettings.SplitVertical
                        : GameSettings.SplitHorizontal;
                else if (Row == 1)
                    seat.Listener = seat.Listener == GameSettings.ListenAverage
                        ? GameSettings.ListenP1
                        : GameSettings.ListenAverage;
                else return;
                seat.Clamp();
                _dirty = true;
                Note(SoundMove);
                Rebuild();
                return;
            }
            if (Screen != FrontScreen.Setup) return;
            EnsureSettings();
            GameSettings s = GameSettings.Current;
            if (Row == 0) s.Arena += dir > 0 ? 1 : -1;
            else if (Row == 1) s.AiOpponents += dir > 0 ? 1 : -1;
            else if (Row == 2) s.DifficultyTier += dir > 0 ? 1 : -1;
            else if (Row == 3) s.RoundLengthIndex += dir > 0 ? 1 : -1;
            else if (Row == 4) s.RoundsPerMatch += dir > 0 ? 1 : -1;
            else return;
            s.Clamp();
            _dirty = true;
            Note(SoundMove);
            Rebuild();
        }

        public static int Confirm()
        {
            Note(SoundConfirm);
            if (Screen == FrontScreen.Title || Screen == FrontScreen.Quit)
            {
                if (Row == 1)
                {
                    Screen = FrontScreen.Settings;
                    Row = 0;
                    Rebuild();
                    return ActSettings;
                }
                if (Row == 2)
                {
                    Screen = FrontScreen.HowTo;
                    Row = 0;
                    HowToPlay.Ensure(InputDeviceKind.Keyboard, ArenaRegistry.Count);
                    Rebuild();
                    return ActHowTo;
                }
                if (Row == 3)
                {
                    Screen = FrontScreen.Quit;
                    Rebuild();
                    return ActQuit;
                }
                Screen = FrontScreen.Setup;
                Row = 0;
                Rebuild();
                return ActSetup;
            }
            if (Screen == FrontScreen.Setup)
            {
                if (Row >= 6) return Back();
                if (Row == 5)
                {
                    EnsureSettings();
                    GameSettings.Current.Clamp();
                    Screen = FrontScreen.Join;
                    Row = 2;
                    Armed = false;
                    Rebuild();
                    return ActJoin;
                }
                Step(1);
                return ActNone;
            }
            if (Screen == FrontScreen.Join)
            {
                if (Row >= 3) return Back();
                if (Row == 2)
                {
                    if (CouchPlay.Humans < 1) return ActNone;
                    EnsureSettings();
                    GameSettings.Current.Clamp();
                    _dirty = true;
                    Arm();
                    return ActStart;
                }
                Step(1);
                return ActNone;
            }
            if (Screen == FrontScreen.Results)
            {
                if (Row <= 0)
                {
                    NoteRematch();
                    return ActRematch;
                }
                if (Row == 1)
                {
                    ShowSetup();
                    return ActSetup;
                }
                ShowTitle();
                return ActTitle;
            }
            if (Screen == FrontScreen.Settings || Screen == FrontScreen.HowTo)
                return Back();
            return ActNone;
        }

        public static int Back()
        {
            if (Screen == FrontScreen.Title || Screen == FrontScreen.Playing)
                return ActNone;
            Note(SoundBack);
            if (Screen == FrontScreen.Results)
            {
                ShowTitle();
                return ActTitle;
            }
            if (Screen == FrontScreen.Join)
            {
                Screen = FrontScreen.Setup;
                Row = 0;
                Armed = false;
                Rebuild();
                return ActSetup;
            }
            if (Screen == FrontScreen.Setup || Screen == FrontScreen.Settings
                || Screen == FrontScreen.HowTo || Screen == FrontScreen.Quit)
            {
                if (Screen == FrontScreen.Setup) ReleaseObjects();
                Screen = FrontScreen.Title;
                Row = 0;
                Armed = false;
                Rebuild();
                return ActTitle;
            }
            return ActNone;
        }

        public static void CloseOverlay()
        {
            if (Screen != FrontScreen.Settings && Screen != FrontScreen.HowTo) return;
            Screen = FrontScreen.Title;
            Row = 0;
            Rebuild();
        }

        public static void Pointer(int index)
        {
            Row = index;
            int act = Confirm();
            if (FrontHooks.Sound != null) FrontHooks.Sound();
            if (FrontHooks.Act != null) FrontHooks.Act(act);
        }

        public static void Arm()
        {
            EnsureSettings();
            GameSettings.Current.Clamp();
            Armed = true;
            Screen = FrontScreen.Playing;
            SpawnRoster();
            Rebuild();
        }

        public static void NoteRematch()
        {
            ReleaseObjects();
            CouchPlay.ClearResidue();
            EnsureSettings();
            Armed = true;
            Screen = FrontScreen.Playing;
            SpawnRoster();
            Rebuild();
        }

        public static int PickWinner(float a, float b)
        {
            if (a > b - 0.0001f && a < b + 0.0001f) return WinnerTie;
            return a < b ? 0 : 1;
        }

        public static float TimeAsItOf(int index)
        {
            if (index < 0 || index > 1) return 0f;
            return _time[index];
        }

        public static int TagsOf(int index)
        {
            if (index < 0 || index > 1) return 0;
            return _tags[index];
        }

        public static string NameOf(int index)
        {
            return index == 0 ? _name0 : _name1;
        }

        /// <summary>
        /// Plays the configured number of rounds with RoundFlow's own countdown,
        /// tag, and end. Totals are copied before Advance clears the round.
        /// </summary>
        public static bool PlayOut()
        {
            EnsureSettings();
            GameSettings s = GameSettings.Current;
            s.Clamp();
            if (Live == 0) SpawnRoster();
            Armed = true;
            ClearTotals();
            int target = s.RoundsPerMatch;
            float duration = s.RoundSeconds();
            _round = RoundFlow.NewRound(duration, 3956);
            _name0 = _round.Names[0];
            _name1 = _round.Names[1];
            RoundsPlayed = 0;
            for (int i = 0; i < target; i++)
            {
                if (!PlayOneRound())
                {
                    Fail("round " + i + " did not end");
                    return false;
                }
                _time[0] += _round.TimeAsIt[0];
                _time[1] += _round.TimeAsIt[1];
                _tags[0] += _round.TagsBy[0];
                _tags[1] += _round.TagsBy[1];
                if (_round.LongestChase > Longest) Longest = _round.LongestChase;
                RoundsPlayed++;
                if (i + 1 >= target) break;
                RoundFlow.Tick(_round, RoundFlow.ResultsArmSeconds + 0.01f);
                if (!RoundFlow.Advance(_round))
                {
                    Fail("round " + i + " did not advance");
                    return false;
                }
            }
            if (_round == null || _round.Phase != RoundFlow.Phase.Results)
            {
                Fail("match did not rest on results");
                return false;
            }
            Winner = PickWinner(_time[0], _time[1]);
            Screen = FrontScreen.Results;
            Row = 0;
            Rebuild();
            return Failure.Length == 0;
        }

        public sealed class Report
        {
            public bool Ok = true;
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
            ResetStatics();
            GameSettings.Current = GameSettings.Defaults();
            ActionBinds.Current = ActionBinds.Defaults();
            ControlGlyphs.Note(InputDeviceKind.Keyboard);
            HowToPlay.ResetStatics();

            if (Screen != FrontScreen.Title || RowText(0) != "Play" || RowText(3) != "Quit")
                report.Fail("title is missing Play or Quit");
            Nudge(1);
            if (Confirm() != ActSettings || Screen != FrontScreen.Settings)
                report.Fail("settings was not reachable from the title");
            if (Back() != ActTitle || Screen != FrontScreen.Title)
                report.Fail("settings did not return to the title");
            Highlight(2);
            if (Confirm() != ActHowTo || Screen != FrontScreen.HowTo)
                report.Fail("how to play was not reachable from the title");
            if (HowToPlay.Count < 1)
                report.Fail("how to play card was empty");
            bool sawRule = false;
            for (int i = 0; i < HowToPlay.Count; i++)
            {
                if (HowToPlay.Line(i).IndexOf("Tag-back immunity 1.0 s", StringComparison.Ordinal) >= 0)
                    sawRule = true;
            }
            if (!sawRule)
                report.Fail("how to play did not reuse the pause card");
            if (Back() != ActTitle || Screen != FrontScreen.Title)
                report.Fail("how to play did not return to the title");
            Highlight(3);
            if (Confirm() != ActQuit || Screen != FrontScreen.Quit)
                report.Fail("quit was not reachable");
            if (Back() != ActTitle || Screen != FrontScreen.Title)
                report.Fail("quit did not return to the title");

            Highlight(0);
            if (Confirm() != ActSetup || Screen != FrontScreen.Setup)
                report.Fail("play did not open match setup");
            if (ArenaRegistry.Count < 1)
                report.Fail("arena registry was empty");
            for (int i = 0; i < ArenaRegistry.Count; i++)
            {
                if (GameSettings.ArenaName(i) != ArenaRegistry.All[i].Name)
                    report.Fail("setup arena name was not read from the registry");
            }
            Highlight(0);
            Step(1);
            Highlight(1);
            Step(-1);
            Step(1);
            Step(1);
            Step(1);
            Step(-1);
            Highlight(2);
            Step(1);
            Highlight(3);
            Step(-1);
            Highlight(4);
            Step(1);
            Step(1);
            GameSettings edited = GameSettings.Current;
            if (edited.Arena != 1 || edited.AiOpponents != 2 || edited.DifficultyTier != 2)
                report.Fail("setup did not keep arena, opponents, or difficulty");
            if (edited.RoundLengthIndex != 0 || edited.RoundsPerMatch != 3)
                report.Fail("setup did not keep round length or rounds");
            if (Math.Abs(edited.DifficultyValue() - 0.9f) > 0.0001f)
                report.Fail("hard is not the 0.9 tier");
            if (Math.Abs(GameSettings.DifficultyTiers[0] - 0.2f) > 0.0001f
                || Math.Abs(GameSettings.DifficultyTiers[1] - 0.5f) > 0.0001f)
                report.Fail("easy or normal tier drifted");
            if (GameSettings.DifficultyNames[0] != "Easy" || GameSettings.DifficultyNames[1] != "Normal"
                || GameSettings.DifficultyNames[2] != "Hard")
                report.Fail("difficulty labels drifted");
            if (Math.Abs(edited.RoundSeconds() - 60f) > 0.001f
                || Math.Abs(GameSettings.RoundLengthPresets[GameSettings.RoundLengthDefault] - 120f) > 0.001f)
                report.Fail("round length presets drifted from 120 plus the short preset");

            edited.Arena = 8;
            edited.AiOpponents = 9;
            edited.DifficultyTier = 9;
            edited.RoundLengthIndex = 9;
            edited.RoundsPerMatch = 0;
            edited.Clamp();
            if (edited.Arena != ArenaRegistry.Count - 1 || edited.AiOpponents != 3 || edited.DifficultyTier != 2)
                report.Fail("setup clamp missed arena, opponents, or difficulty");
            if (edited.RoundLengthIndex != GameSettings.RoundLengthPresets.Length - 1 || edited.RoundsPerMatch != 1)
                report.Fail("setup clamp missed length or rounds");
            edited.Arena = 1;
            edited.AiOpponents = 2;
            edited.DifficultyTier = 2;
            edited.RoundLengthIndex = 0;
            edited.RoundsPerMatch = 3;
            edited.Clamp();

            string blob = SettingsFile.Write(edited, ActionBinds.Defaults());
            GameSettings loaded = GameSettings.Defaults();
            ActionBinds binds = ActionBinds.Defaults();
            SettingsFile.Read(blob, loaded, binds);
            if (loaded.Arena != 1 || loaded.AiOpponents != 2 || loaded.DifficultyTier != 2)
                report.Fail("match setup did not persist");
            if (loaded.RoundLengthIndex != 0 || loaded.RoundsPerMatch != 3)
                report.Fail("round setup did not persist");
            if (blob.IndexOf("ai=2", StringComparison.Ordinal) < 0 || blob.IndexOf("rounds=3", StringComparison.Ordinal) < 0)
                report.Fail("settings json omitted the match rows");

            Highlight(5);
            if (Confirm() != ActJoin || Screen != FrontScreen.Join || Armed)
                report.Fail("start did not open the join screen");
            if (!CouchPlay.Join(CouchPlay.DeviceKeyboard) || CouchPlay.Humans != 1)
                report.Fail("keyboard did not join");
            if (!CouchPlay.Leave(CouchPlay.DeviceKeyboard) || CouchPlay.Humans != 0)
                report.Fail("keyboard did not leave");
            if (!CouchPlay.Join(CouchPlay.DeviceKeyboard) || CouchPlay.Humans != 1)
                report.Fail("keyboard did not rejoin");
            Highlight(2);
            if (Confirm() != ActStart || !Armed || Live != 3)
                report.Fail("start did not arm a roster of 1 plus the opponents");
            if (!PlayOut() || Screen != FrontScreen.Results || RoundsPlayed != 3)
                report.Fail(Failure.Length == 0 ? "match did not end on results" : Failure);
            float best = TimeAsItOf(0) <= TimeAsItOf(1) ? TimeAsItOf(0) : TimeAsItOf(1);
            if (Winner < 0 || Math.Abs(TimeAsItOf(Winner) - best) > 0.001f)
                report.Fail("winner was not least time as It");
            if (TagsOf(0) + TagsOf(1) != 3 || Longest <= 0.05f)
                report.Fail("tags or longest survival were empty");
            if (NameOf(0) != "You" || NameOf(1) != "Dummy")
                report.Fail("results names were not the round-flow roster");
            int mid = Live;
            Highlight(0);
            if (Confirm() != ActRematch || BeforeRelease != mid || LastRelease != 0 || Live != 3)
                report.Fail("rematch left leftovers");
            if (!PlayOut() || Screen != FrontScreen.Results)
                report.Fail("rematch did not return to results");
            Highlight(1);
            if (Confirm() != ActSetup || Screen != FrontScreen.Setup || Live != 0 || LastRelease != 0)
                report.Fail("change setup left leftovers");
            Highlight(5);
            Confirm();
            if (!PlayOut() || Screen != FrontScreen.Results)
                report.Fail("second match did not end on results");
            Highlight(2);
            if (Confirm() != ActTitle || Screen != FrontScreen.Title || Live != 0 || LastRelease != 0 || Armed)
                report.Fail("title return left leftovers");
            if (Moves < 1 || Confirms < 1 || Backs < 1)
                report.Fail("move, confirm, or back did not fire");

            CheckWired(report);
            if (!report.Ok)
                report.Line = "front-end FAIL " + report.Failure;
            else
                report.Line = "front-end screens=title,setup,settings,howto,results"
                    + " arenas=" + ArenaRegistry.Count.ToString()
                    + " persist=json rounds=3 ended=results winner=least-it"
                    + " sounds=move,confirm,back rematch=0 title=0 leftovers=0";
            return report;
        }

        static void CheckWired(Report report)
        {
            string flow = Read("Assets/Scripts/Core/GameFlow.cs");
            string life = Read("Assets/Scripts/Core/StaticLifecycle.cs");
            string mode = Read("Assets/Scripts/Modes/TagModeController.cs");
            string spawn = Read("Assets/Scripts/Local/LocalPlayerSpawner.cs");
            if (flow == null || life == null || mode == null || spawn == null)
            {
                report.Fail("front-end wiring file is missing");
                return;
            }
            if (flow.IndexOf("Quit to title", StringComparison.Ordinal) < 0)
                report.Fail("pause is missing quit to title");
            if (flow.IndexOf("TagSfx.UiMove", StringComparison.Ordinal) < 0
                || flow.IndexOf("TagSfx.UiConfirm", StringComparison.Ordinal) < 0
                || flow.IndexOf("TagSfx.UiBack", StringComparison.Ordinal) < 0)
                report.Fail("front end does not use move, confirm, and back");
            if (flow.IndexOf("SettingsMenuUi.Panel.Settings", StringComparison.Ordinal) < 0
                || flow.IndexOf("SettingsMenuUi.Panel.HowTo", StringComparison.Ordinal) < 0)
                report.Fail("title does not open the settings menu or the how to play card");
            if (life.IndexOf("FrontSession.ResetStatics", StringComparison.Ordinal) < 0
                || life.IndexOf("ReleaseMatch", StringComparison.Ordinal) < 0)
                report.Fail("title teardown does not reuse StaticLifecycle");
            if (mode.IndexOf("Quit to title", StringComparison.Ordinal) < 0
                || mode.IndexOf("Change setup", StringComparison.Ordinal) < 0
                || mode.IndexOf("FrontLive.KeepGoing", StringComparison.Ordinal) < 0)
                report.Fail("results card is missing rematch, setup, or the round tally");
            if (spawn.IndexOf("ApplyOpponents", StringComparison.Ordinal) < 0)
                report.Fail("match setup does not apply the opponent count");
        }

        static bool PlayOneRound()
        {
            if (_round == null) return false;
            int guard = 0;
            while (_round.Phase == RoundFlow.Phase.Countdown && guard++ < 20)
                RoundFlow.Tick(_round, 1f);
            if (_round.Phase != RoundFlow.Phase.Playing) return false;
            RoundFlow.Tick(_round, 0.5f);
            int from = _round.It;
            int to = from == 0 ? 1 : 0;
            if (!RoundFlow.TryTag(_round, from, to)) return false;
            float rest = _round.Remaining;
            if (rest > 0f) RoundFlow.Tick(_round, rest);
            return _round.Phase == RoundFlow.Phase.Results;
        }

        static void ClearTotals()
        {
            _time[0] = 0f;
            _time[1] = 0f;
            _tags[0] = 0;
            _tags[1] = 0;
            Longest = 0f;
            Winner = -1;
            RoundsPlayed = 0;
        }

        static void SpawnRoster()
        {
            EnsureSettings();
            if (CouchPlay.Humans < 1)
                CouchPlay.Join(CouchPlay.DeviceKeyboard);
            int ai = CouchPlay.FillAi(GameSettings.Current.AiOpponents);
            int n = CouchPlay.Humans + ai;
            if (n < 1) n = 1;
            if (Hosts > 0) Hosts++;
            else Hosts = 1;
            Live = n;
        }

        static void ReleaseObjects()
        {
            BeforeRelease = Live;
            Live = 0;
            LastRelease = Live;
            Hosts = 0;
        }

        static void EnsureSettings()
        {
            if (GameSettings.Current == null)
                GameSettings.Current = GameSettings.Defaults();
        }

        static void Note(int sound)
        {
            _sound = sound;
            if (sound == SoundMove) Moves++;
            else if (sound == SoundConfirm) Confirms++;
            else if (sound == SoundBack) Backs++;
        }

        static void Fail(string why)
        {
            if (Failure.Length == 0) Failure = why;
        }

        static void Rebuild()
        {
            if (Screen == FrontScreen.Setup)
            {
                EnsureSettings();
                GameSettings s = GameSettings.Current;
                _rows[0] = "Arena  " + GameSettings.ArenaName(s.Arena);
                _rows[1] = "AI opponents  " + Digit(s.AiOpponents);
                _rows[2] = "Difficulty  " + s.DifficultyLabel();
                _rows[3] = "Round length  " + LengthLabel(s.RoundLengthIndex);
                _rows[4] = "Rounds  " + Digit(s.RoundsPerMatch);
                _rows[5] = "Start match";
                _rows[6] = "Back";
                return;
            }
            if (Screen == FrontScreen.Join)
            {
                EnsureSettings();
                GameSettings s = GameSettings.Current;
                _rows[0] = s.SplitAxis == GameSettings.SplitHorizontal ? "Split  Horizontal" : "Split  Vertical";
                _rows[1] = s.Listener == GameSettings.ListenAverage ? "Listener  Average" : "Listener  P1";
                _rows[2] = "Start match";
                _rows[3] = "Back";
                return;
            }
            if (Screen == FrontScreen.Results)
            {
                _rows[0] = "Rematch";
                _rows[1] = "Change setup";
                _rows[2] = "Title";
                return;
            }
            _rows[0] = "Play";
            _rows[1] = "Settings";
            _rows[2] = "How to play";
            _rows[3] = "Quit";
        }

        static string Digit(int value)
        {
            if (value < 0) return _digits[0];
            if (value >= _digits.Length) return _digits[_digits.Length - 1];
            return _digits[value];
        }

        static string LengthLabel(int index)
        {
            if (index < 0 || index >= _lengths.Length) index = GameSettings.RoundLengthDefault;
            return _lengths[index];
        }

        static string Read(string path)
        {
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }
    }

    public static class FrontHooks
    {
        public static Action<int> Act;
        public static Action Sound;
    }
}
