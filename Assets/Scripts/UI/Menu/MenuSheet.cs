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
