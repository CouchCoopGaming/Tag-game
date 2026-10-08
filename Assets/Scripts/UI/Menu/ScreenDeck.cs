using System;
using System.IO;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// One screen root is visible. Opening the next screen hides the last.
    /// ShowStacked is the old open, which left the previous screen's widgets up.
    /// </summary>
    public static class ScreenDeck
    {
        public const int Cap = 16;
        static readonly bool[] Live = new bool[Cap];

        public static int Visible { get; private set; }

        public static void Reset()
        {
            for (int i = 0; i < Cap; i++) Live[i] = false;
            Visible = 0;
        }

        public static void ShowOnly(int screen)
        {
            Reset();
            Raise(screen);
        }

        /// <summary>Old open. The new screen draws on top of the one already up.</summary>
        public static void ShowStacked(int screen)
        {
            Raise(screen);
        }

        public static bool OneRoot()
        {
            return Visible <= 1;
        }

        public static int After(bool exclusive, int first, int second)
        {
            Reset();
            if (exclusive)
            {
                ShowOnly(first);
                ShowOnly(second);
            }
            else
            {
                ShowStacked(first);
                ShowStacked(second);
            }
            return Visible;
        }

        public static bool HostExclusive(string source)
        {
            if (string.IsNullOrEmpty(source)) return false;
            int open = source.IndexOf("void Open(MenuScreenId id)", StringComparison.Ordinal);
            if (open < 0) return false;
            int clearFn = source.IndexOf("void ClearBody()", open, StringComparison.Ordinal);
            if (clearFn < 0) return false;
            string openBody = source.Substring(open, clearFn - open);
            int clearCall = openBody.IndexOf("ClearBody();", StringComparison.Ordinal);
            int rules = openBody.IndexOf("BuildRules();", StringComparison.Ordinal);
            if (clearCall < 0) return false;
            if (rules >= 0 && rules < clearCall) return false;
            int next = source.IndexOf("MenuTile AddTile(", clearFn, StringComparison.Ordinal);
            if (next < 0) next = source.Length;
            string clearBody = source.Substring(clearFn, next - clearFn);
            return clearBody.IndexOf("ScreenDeck.ShowOnly((int)_screen);", StringComparison.Ordinal) >= 0;
        }

        public static string SpikeOld(string source)
        {
            if (string.IsNullOrEmpty(source)) return "";
            return source.Replace(
                "ScreenDeck.ShowOnly((int)_screen);",
                "ScreenDeck.ShowStacked((int)_screen);");
        }

        public static bool Holds(string root)
        {
            int clean = After(true, 6, 5);
            int stacked = After(false, 6, 5);
            bool exclusive = OneRoot() == false && stacked > 1 && clean == 1;
            string path = Path.Combine(root ?? "", "Assets", "Scripts", "UI", "Menu", "MenuHost.cs");
            string source = File.Exists(path) ? File.ReadAllText(path) : "";
            bool host = HostExclusive(source);
            bool oldHost = HostExclusive(SpikeOld(source));
            if (Environment.GetEnvironmentVariable("TAG_SHOW_ROOTS") == "1")
            {
                Console.WriteLine("screen-roots clean=" + clean.ToString());
                Console.WriteLine("screen-roots stacked=" + stacked.ToString()
                    + (stacked > 1 ? " FAIL two roots" : " MISS"));
            }
            ShowOnly(5);
            return exclusive && host && !oldHost && OneRoot();
        }

        static void Raise(int screen)
        {
            if (screen < 0 || screen >= Cap) return;
            if (Live[screen]) return;
            Live[screen] = true;
            Visible++;
        }
    }
}
