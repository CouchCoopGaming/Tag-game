using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tag.Modes;
using Tag.Settings;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Every screen string has to be in a UI font. The check mark is not.
    /// A spiked check character must fail this, and the live strings must pass.
    /// </summary>
    public static class FontCover
    {
        public static bool Holds(string root)
        {
            var body = Load(Path.Combine(root ?? "", "Assets", "UI", "Fonts", "LiberationSans-Bold.ttf"));
            var display = Load(Path.Combine(root ?? "", "Assets", "UI", "Fonts", "Bangers-Regular.ttf"));
            if (body == null || display == null || body.Count < 26 || display.Count < 26)
                return false;
            var strings = new List<string>();
            Collect(root, strings);
            int clean = Missing(strings, body, display);
            var spiked = new List<string>(strings);
            spiked.Add(((char)0x2713).ToString());
            int dirty = Missing(spiked, body, display);
            if (Environment.GetEnvironmentVariable("TAG_SHOW_GLYPHS") == "1")
            {
                Console.WriteLine("glyph-cover clean=" + clean.ToString());
                Console.WriteLine("glyph-cover check=" + dirty.ToString()
                    + (dirty > 0 ? " FAIL U+2713" : " MISS"));
            }
            return clean == 0 && dirty > 0;
        }

        public static int Missing(List<string> strings, HashSet<int> body, HashSet<int> display)
        {
            if (strings == null || body == null || display == null) return 1;
            int missing = 0;
            var seen = new HashSet<int>();
            for (int i = 0; i < strings.Count; i++)
            {
                string text = strings[i];
                if (string.IsNullOrEmpty(text)) continue;
                for (int c = 0; c < text.Length; c++)
                {
                    int ch = text[c];
                    if (ch == '\n' || ch == '\r' || ch == '\t') continue;
                    if (seen.Contains(ch)) continue;
                    seen.Add(ch);
                    if (!body.Contains(ch) || !display.Contains(ch)) missing++;
                }
            }
            return missing;
        }

        static void Collect(string root, List<string> into)
        {
            GameSettings fresh = GameSettings.Defaults();
            fresh.Clamp();
            for (int i = 0; i < MenuCatalog.Modes; i++)
            {
                var id = (TagModeId)i;
                into.Add(MenuCatalog.ModeName(id));
                into.Add(MenuCatalog.ModeBlurb(id, fresh));
                into.Add(RuleBook.Help(id, i));
            }
            for (int i = 0; i < RuleBook.Count; i++)
            {
                into.Add(RuleBook.Title(i));
                into.Add(RuleBook.Detail(fresh, i));
                into.Add(RuleBook.Help(TagModeId.LeastIt, i));
                into.Add(RuleBook.Help(TagModeId.HotPotato, i));
            }
            into.Add(MenuCatalog.Credits());
            into.Add(RuleBook.LoadLength(fresh));
            into.Add(RuleBook.LoadRounds(fresh));
            into.Add(RuleBook.LoadWin(fresh));
            string dir = Path.Combine(root ?? "", "Assets", "Scripts", "UI");
            if (!Directory.Exists(dir)) return;
            string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
                Literals(File.ReadAllText(files[i]), into);
        }

        public static void Literals(string source, List<string> into)
        {
            if (string.IsNullOrEmpty(source) || into == null) return;
            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];
                if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
                {
                    i += 2;
                    while (i < source.Length && source[i] != '\n') i++;
                    continue;
                }
                if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/')) i++;
                    i += 2;
                    continue;
                }
                if (c != '"')
                {
                    i++;
                    continue;
                }
                i++;
                var sb = new StringBuilder();
                while (i < source.Length && source[i] != '"')
                {
                    if (source[i] == '\\' && i + 1 < source.Length)
                    {
                        char e = source[i + 1];
                        if (e == 'u' && i + 5 < source.Length)
                        {
                            int code = Hex(source, i + 2, 4);
                            if (code >= 0) sb.Append((char)code);
                            i += 6;
                            continue;
                        }
                        if (e == 'n') sb.Append('\n');
                        else if (e == 'r') sb.Append('\r');
                        else if (e == 't') sb.Append('\t');
                        else sb.Append(e);
                        i += 2;
                        continue;
                    }
                    sb.Append(source[i]);
                    i++;
                }
                if (i < source.Length && source[i] == '"') i++;
                into.Add(sb.ToString());
            }
        }

        static int Hex(string s, int at, int n)
        {
            int v = 0;
            for (int i = 0; i < n; i++)
            {
                char c = s[at + i];
                int d;
                if (c >= '0' && c <= '9') d = c - '0';
                else if (c >= 'a' && c <= 'f') d = c - 'a' + 10;
                else if (c >= 'A' && c <= 'F') d = c - 'A' + 10;
                else return -1;
                v = (v << 4) + d;
            }
            return v;
        }

        public static HashSet<int> Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            byte[] data = File.ReadAllBytes(path);
            if (data.Length < 12) return null;
            int tables = U16(data, 4);
            int cmap = -1;
            int at = 12;
            for (int i = 0; i < tables; i++)
            {
                if (at + 16 > data.Length) return null;
                if (data[at] == (byte)'c' && data[at + 1] == (byte)'m' && data[at + 2] == (byte)'a' && data[at + 3] == (byte)'p')
                    cmap = (int)U32(data, at + 8);
                at += 16;
            }
            if (cmap < 0 || cmap + 4 > data.Length) return null;
            int ntab = U16(data, cmap + 2);
            int format4 = -1;
            int format12 = -1;
            for (int i = 0; i < ntab; i++)
            {
                int rec = cmap + 4 + i * 8;
                if (rec + 8 > data.Length) break;
                int off = cmap + (int)U32(data, rec + 4);
                if (off + 2 > data.Length) continue;
                int fmt = U16(data, off);
                if (fmt == 4) format4 = off;
                if (fmt == 12) format12 = off;
            }
            var chars = new HashSet<int>();
            if (format12 >= 0) Read12(data, format12, chars);
            else if (format4 >= 0) Read4(data, format4, chars);
            else return null;
            return chars;
        }

        static void Read4(byte[] data, int o, HashSet<int> chars)
        {
            if (o + 14 > data.Length) return;
            int seg = U16(data, o + 6) / 2;
            int end = o + 14;
            int start = end + 2 * seg + 2;
            int delta = start + 2 * seg;
            int range = delta + 2 * seg;
            for (int i = 0; i < seg; i++)
            {
                if (range + 2 * i + 2 > data.Length) return;
                int s = U16(data, start + 2 * i);
                int e = U16(data, end + 2 * i);
                int d = (short)U16(data, delta + 2 * i);
                int r = U16(data, range + 2 * i);
                for (int c = s; c <= e && c < 0xFFFF; c++)
                {
                    int g;
                    if (r == 0) g = (c + d) & 0xFFFF;
                    else
                    {
                        int gpos = range + 2 * i + r + 2 * (c - s);
                        if (gpos + 2 > data.Length) continue;
                        g = U16(data, gpos);
                        if (g != 0) g = (g + d) & 0xFFFF;
                    }
                    if (g != 0) chars.Add(c);
                }
            }
        }

        static void Read12(byte[] data, int o, HashSet<int> chars)
        {
            if (o + 16 > data.Length) return;
            int n = (int)U32(data, o + 12);
            int p = o + 16;
            for (int i = 0; i < n; i++)
            {
                if (p + 12 > data.Length) return;
                int s = (int)U32(data, p);
                int e = (int)U32(data, p + 4);
                p += 12;
                if (e - s > 200000) continue;
                for (int c = s; c <= e; c++) chars.Add(c);
            }
        }

        static int U16(byte[] data, int at)
        {
            return (data[at] << 8) | data[at + 1];
        }

        static uint U32(byte[] data, int at)
        {
            return ((uint)data[at] << 24) | ((uint)data[at + 1] << 16) | ((uint)data[at + 2] << 8) | data[at + 3];
        }
    }
}
