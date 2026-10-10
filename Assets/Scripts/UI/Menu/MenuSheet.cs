using System;
using System.IO;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Secondary screens share one park wash and one short comic wipe.
    /// Title, the main menu, and character select stay with the other lane.
    /// Screen numbers match MenuScreenId.
    /// </summary>
    public static class MenuSheet
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
        public const int Records = 14;

        public const int ArenaCols = 3;
        public const float WipeSeconds = 0.36f;
        public const string ResultsWord = "RESULTS";
        public const string JoinPrompt = "Press Space or A to join";

        /// <summary>Main-menu row indexes. Each value is unique, so one focus lights one row.</summary>
        public static readonly int[] MainIndex = { 0, 1, 2, 3, 4, 6, 5 };

        /// <summary>True when this focus index is on exactly one main-menu row.</summary>
        public static bool OneHot(int focus)
        {
            if (MainIndex == null || MainIndex.Length < 1) return false;
            var seen = new bool[8];
            int hot = 0;
            for (int i = 0; i < MainIndex.Length; i++)
            {
                int id = MainIndex[i];
                if (id < 0 || id >= seen.Length) return false;
                if (seen[id]) return false;
                seen[id] = true;
                if (id == focus) hot++;
            }
            return hot == 1;
        }

        public static bool OneFocus()
        {
            for (int i = 0; i < MainIndex.Length; i++)
            {
                if (!OneHot(MainIndex[i])) return false;
            }
            return true;
        }

        public const string ReadyLine = "Everyone Ready? Press Start";

        /// <summary>
        /// Start line only when every joined seat is ready and at least two are in.
        /// Otherwise the waiting line names how many still have to ready up.
        /// </summary>
        public static string JoinBanner(int joined, int readyCount)
        {
            if (joined < 0) joined = 0;
            if (readyCount < 0) readyCount = 0;
            if (readyCount > joined) readyCount = joined;
            if (joined >= 2 && readyCount == joined) return ReadyLine;
            if (joined == 0) return "Anyone can join";
            int waiting = joined - readyCount;
            if (waiting < 1) waiting = 2 - joined;
            if (waiting < 1) waiting = 1;
            if (waiting == 1) return "Waiting for 1 player to ready up";
            return "Waiting for " + waiting.ToString() + " players to ready up";
        }

        /// <summary>The old drop-in rule. Any occupied seat printed the start line.</summary>
        public static string JoinBannerOld(int humans)
        {
            return humans > 0 ? ReadyLine : "Anyone can join";
        }

        public static bool JoinBannerHolds()
        {
            string old = JoinBannerOld(2);
            string now = JoinBanner(2, 1);
            if (old != ReadyLine) return false;
            if (now == old) return false;
            if (now != "Waiting for 1 player to ready up") return false;
            if (JoinBanner(2, 2) != ReadyLine) return false;
            if (JoinBanner(4, 4) != ReadyLine) return false;
            if (JoinBanner(1, 1) == ReadyLine) return false;
            if (JoinBanner(2, 0) != "Waiting for 2 players to ready up") return false;
            if (JoinBanner(3, 1) != "Waiting for 2 players to ready up") return false;
            if (JoinBanner(0, 0) != "Anyone can join") return false;
            return true;
        }

        public static bool WantsPark(int screen)
        {
            return screen == Join
                || screen == Rules
                || screen == Arena
                || screen == Loading
                || screen == Pause
                || screen == Results
                || screen == Options
                || screen == Controls
                || screen == Credits
                || screen == Records;
        }

        public static bool Wipes(int screen)
        {
            return WantsPark(screen);
        }

        public static float ParkAlpha(int screen)
        {
            if (screen == Pause) return 0.28f;
            if (screen == Loading) return 0.62f;
            if (screen == Results) return 0.85f;
            if (screen == Arena) return 0.42f;
            return 0.5f;
        }

        public static bool Holds(string root)
        {
            if (!MenuRuleNav.Holds()) return false;
            if (!WantsPark(Rules) || !WantsPark(Arena) || !WantsPark(Records)) return false;
            if (WantsPark(Title) || WantsPark(Main) || WantsPark(Cast) || WantsPark(Practice)) return false;
            if (ParkAlpha(Pause) >= ParkAlpha(Rules)) return false;
            if (WipeSeconds <= 0f || WipeSeconds >= 0.4f) return false;
            if (ArenaCols != 3) return false;
            if (ResultsWord != "RESULTS") return false;
            if (JoinPrompt.IndexOf("Space", StringComparison.Ordinal) < 0) return false;
            if (JoinPrompt.IndexOf("A", StringComparison.Ordinal) < 0) return false;
            if (string.IsNullOrEmpty(root)) return false;
            string menu = Path.Combine(root, "Assets", "Scripts", "UI", "Menu");
            string host = File.ReadAllText(Path.Combine(menu, "MenuHost.cs"));
            if (host.IndexOf("string headline = MenuSheet.ResultsWord;", StringComparison.Ordinal) < 0) return false;
            if (host.IndexOf("MenuMannequin.NameOf(_rows[0].Hier)", StringComparison.Ordinal) < 0) return false;
            if (host.IndexOf("MenuSheet.WantsPark((int)id) && _banner != null", StringComparison.Ordinal) < 0) return false;
            if (host.IndexOf("\"TipPlate\"", StringComparison.Ordinal) < 0) return false;
            if (host.IndexOf("MenuBindRow.Stamp", StringComparison.Ordinal) < 0) return false;
            if (host.IndexOf("MenuSheet.JoinPrompt", StringComparison.Ordinal) < 0) return false;
            int arena = host.IndexOf("void BuildArena()", StringComparison.Ordinal);
            if (arena < 0) return false;
            int arenaEnd = host.IndexOf("void ShowArenaPreview()", StringComparison.Ordinal);
            if (arenaEnd < arena) return false;
            string arenaBody = host.Substring(arena, arenaEnd - arena);
            if (arenaBody.IndexOf("_cols = MenuSheet.ArenaCols", StringComparison.Ordinal) < 0) return false;
            string preview = File.ReadAllText(Path.Combine(menu, "MenuPreview.cs"));
            int show = preview.IndexOf("void ShowPodium", StringComparison.Ordinal);
            int next = preview.IndexOf("void EnsureDisc", StringComparison.Ordinal);
            if (show < 0 || next < show) return false;
            if (preview.Substring(show, next - show).IndexOf("MenuCheer.Dress", StringComparison.Ordinal) >= 0) return false;
            if (preview.IndexOf("PlantY = 0.005f", StringComparison.Ordinal) < 0) return false;
            if (preview.IndexOf("HoldRest()", StringComparison.Ordinal) < 0) return false;
            string idle = File.ReadAllText(Path.Combine(menu, "MenuIdle.cs"));
            if (idle.IndexOf("IdlePose.At(0f, 0f)", StringComparison.Ordinal) < 0) return false;
            if (!JoinBannerHolds()) return false;
            int main = host.IndexOf("void BuildMain()", StringComparison.Ordinal);
            int joinAt = host.IndexOf("void BuildJoin()", StringComparison.Ordinal);
            if (main < 0 || joinAt < main) return false;
            string mainBody = host.Substring(main, joinAt - main);
            if (mainBody.IndexOf("MenuBackdrop.Chase", StringComparison.Ordinal) >= 0) return false;
            if (host.IndexOf("ShowMenuPair", StringComparison.Ordinal) < 0) return false;
            int sync = host.IndexOf("void SyncStartMarks()", StringComparison.Ordinal);
            if (sync < joinAt) return false;
            string joinBody = host.Substring(joinAt, sync - joinAt);
            if (joinBody.IndexOf("MenuSheet.JoinBanner(", StringComparison.Ordinal) < 0) return false;
            if (joinBody.IndexOf("CouchPlay.Humans > 0", StringComparison.Ordinal) >= 0) return false;
            string rows = File.ReadAllText(Path.Combine(menu, "MenuPodium.cs"));
            if (rows.IndexOf("int[] body = { 5, 0, 2, 3 }", StringComparison.Ordinal) < 0) return false;
            if (rows.IndexOf("int[] accent = { 4, 1, 4, 1 }", StringComparison.Ordinal) < 0) return false;
            string icons = File.ReadAllText(Path.Combine(menu, "MenuIcons.cs"));
            if (icons.IndexOf("if (action == 2) return KeySpace;", StringComparison.Ordinal) < 0) return false;
            string depth = File.ReadAllText(Path.Combine(menu, "MenuDepth.cs"));
            if (depth.IndexOf("return \"Sound\";", StringComparison.Ordinal) < 0) return false;
            if (depth.IndexOf("return \"Picture\";", StringComparison.Ordinal) < 0) return false;
            string wipe = File.ReadAllText(Path.Combine(menu, "MenuWipe.cs"));
            if (wipe.IndexOf("MenuSheet.WipeSeconds", StringComparison.Ordinal) < 0) return false;
            return true;
        }
    }
}
