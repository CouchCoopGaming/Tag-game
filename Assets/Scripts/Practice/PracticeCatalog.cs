using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Tag.Practice
{
    public struct PracticeGate
    {
        public byte Kind;
        public byte Verb;
        public float X, Y, Z, R;
    }

    public sealed class PracticeRoute
    {
        public string Id = "";
        public string Arena = "";
        public string Name = "";
        public string Skill = "";
        public string Verbs = "";
        public PracticeGate[] Gates = new PracticeGate[0];
    }

    /// <summary>
    /// Routes live in PracticeRoutes.json. A later map adds arenas by editing
    /// that file. The ScriptableObject is the Unity handle over the same text.
    /// </summary>
    public static class PracticeCatalog
    {
        public const byte KindStart = 0;
        public const byte KindCheck = 1;
        public const byte KindFinish = 2;

        public const int MaxRoutes = 24;

        public static readonly PracticeRoute[] All = new PracticeRoute[MaxRoutes];
        public static int Count;

        public static int Load()
        {
            string path = FilePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Count = 0;
                return 0;
            }
            Apply(File.ReadAllText(path));
            return Count;
        }

        public static void Apply(string json)
        {
            Count = 0;
            if (string.IsNullOrEmpty(json)) return;
            int routes = json.IndexOf("\"routes\"", StringComparison.Ordinal);
            if (routes < 0) return;
            int array = json.IndexOf('[', routes);
            if (array < 0) return;
            int i = array + 1;
            while (i < json.Length && Count < MaxRoutes)
            {
                int obj = json.IndexOf('{', i);
                if (obj < 0) break;
                int end = Match(json, obj);
                if (end < 0) break;
                if (obj > array && json.IndexOf(']', array, obj - array) >= 0 && DepthOf(json, array, obj) == 0)
                    break;
                var route = ParseRoute(json.Substring(obj, end - obj + 1));
                if (route != null && route.Gates.Length >= 2)
                {
                    All[Count] = route;
                    Count++;
                }
                i = end + 1;
            }
        }

        public static int CountFor(string arena)
        {
            int n = 0;
            for (int i = 0; i < Count; i++)
            {
                if (All[i] != null && All[i].Arena == arena) n++;
            }
            return n;
        }

        public static PracticeRoute ForArena(string arena, int nth)
        {
            if (nth < 0) return null;
            int n = 0;
            for (int i = 0; i < Count; i++)
            {
                PracticeRoute route = All[i];
                if (route == null || route.Arena != arena) continue;
                if (n == nth) return route;
                n++;
            }
            return null;
        }

        public static PracticeRoute ById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < Count; i++)
            {
                if (All[i] != null && All[i].Id == id) return All[i];
            }
            return null;
        }

        public static bool Hit(PracticeGate gate, float x, float y, float z)
        {
            float dx = x - gate.X;
            float dz = z - gate.Z;
            float r = gate.R > 0.2f ? gate.R : 2f;
            if (dx * dx + dz * dz > r * r) return false;
            float dy = y - gate.Y;
            float tall = r + 6f;
            return dy * dy <= tall * tall;
        }

        public static string FilePath()
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                string path = Path.Combine(dir, "Assets", "Resources", "TagArena", "PracticeRoutes.json");
                if (File.Exists(path)) return path;
                dir = Path.GetDirectoryName(dir);
            }
            return "";
        }

        static PracticeRoute ParseRoute(string obj)
        {
            var route = new PracticeRoute();
            route.Id = Str(obj, "id");
            route.Arena = Str(obj, "arena");
            route.Name = Str(obj, "name");
            route.Skill = Str(obj, "skill");
            route.Verbs = Str(obj, "verbs");
            if (route.Id.Length == 0 || route.Arena.Length == 0) return null;
            int gates = obj.IndexOf("\"gates\"", StringComparison.Ordinal);
            if (gates < 0) return null;
            int array = obj.IndexOf('[', gates);
            if (array < 0) return null;
            var list = new PracticeGate[8];
            int n = 0;
            int i = array + 1;
            while (i < obj.Length && n < list.Length)
            {
                int g = obj.IndexOf('{', i);
                if (g < 0) break;
                int end = Match(obj, g);
                if (end < 0) break;
                string one = obj.Substring(g, end - g + 1);
                var gate = new PracticeGate();
                gate.Kind = KindOf(Str(one, "k"));
                gate.Verb = PracticeVerb.Parse(Str(one, "verb"));
                gate.X = Num(one, "x");
                gate.Y = Num(one, "y");
                gate.Z = Num(one, "z");
                gate.R = Num(one, "r");
                if (gate.R < 0.2f) gate.R = 2f;
                list[n++] = gate;
                i = end + 1;
                if (obj.IndexOf(']', i) >= 0 && obj.IndexOf('{', i) < 0) break;
                if (i < obj.Length && obj[i] == ']') break;
            }
            if (n < 2) return null;
            route.Gates = new PracticeGate[n];
            for (int g = 0; g < n; g++) route.Gates[g] = list[g];
            return route;
        }

        static byte KindOf(string k)
        {
            if (k == "finish") return KindFinish;
            if (k == "check") return KindCheck;
            return KindStart;
        }

        static int DepthOf(string text, int from, int to)
        {
            int d = 0;
            for (int i = from; i < to && i < text.Length; i++)
            {
                if (text[i] == '[') d++;
                else if (text[i] == ']') d--;
            }
            return d;
        }

        static int Match(string text, int open)
        {
            int depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        static string Str(string obj, string key)
        {
            string pat = "\"" + key + "\"";
            int i = 0;
            while (i < obj.Length)
            {
                int at = obj.IndexOf(pat, i, StringComparison.Ordinal);
                if (at < 0) return "";
                int colon = obj.IndexOf(':', at + pat.Length);
                if (colon < 0) return "";
                int q1 = obj.IndexOf('"', colon + 1);
                if (q1 < 0) return "";
                int q2 = q1 + 1;
                while (q2 < obj.Length && obj[q2] != '"') q2++;
                if (q2 >= obj.Length) return "";
                return obj.Substring(q1 + 1, q2 - q1 - 1);
            }
            return "";
        }

        static float Num(string obj, string key)
        {
            string pat = "\"" + key + "\"";
            int at = obj.IndexOf(pat, StringComparison.Ordinal);
            if (at < 0) return 0f;
            int colon = obj.IndexOf(':', at + pat.Length);
            if (colon < 0) return 0f;
            int s = colon + 1;
            while (s < obj.Length && (obj[s] == ' ' || obj[s] == '\n' || obj[s] == '\r' || obj[s] == '\t')) s++;
            int e = s;
            while (e < obj.Length && (char.IsDigit(obj[e]) || obj[e] == '-' || obj[e] == '.' || obj[e] == 'e' || obj[e] == 'E' || obj[e] == '+'))
                e++;
            if (e <= s) return 0f;
            if (float.TryParse(obj.Substring(s, e - s), NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;
            return 0f;
        }
    }

    public static class PracticeVerb
    {
        public const byte None = 0;
        public const byte Jump = 1;
        public const byte WallRun = 2;
        public const byte WallJump = 3;
        public const byte Pad = 4;
        public const byte Zip = 5;
        public const byte AirDash = 6;
        public const byte Sprint = 7;
        public const byte Slide = 8;
        public const byte Punch = 9;
        public const byte Cling = 10;
        public const int Bits = 11;

        public static byte Parse(string name)
        {
            if (name == "jump") return Jump;
            if (name == "wallrun") return WallRun;
            if (name == "walljump") return WallJump;
            if (name == "pad") return Pad;
            if (name == "zip") return Zip;
            if (name == "airdash") return AirDash;
            if (name == "sprint") return Sprint;
            if (name == "slide") return Slide;
            if (name == "punch") return Punch;
            if (name == "cling") return Cling;
            return None;
        }

        public static string Name(byte verb)
        {
            switch (verb)
            {
                case Jump: return "jump";
                case WallRun: return "wallrun";
                case WallJump: return "walljump";
                case Pad: return "pad";
                case Zip: return "zip";
                case AirDash: return "airdash";
                case Sprint: return "sprint";
                case Slide: return "slide";
                case Punch: return "punch";
                case Cling: return "cling";
                default: return "";
            }
        }
    }

    /// <summary>
    /// Unity asset over the JSON. The map lane edits the file; this object
    /// only reads it. Feel numbers are not stored here.
    /// </summary>
    [CreateAssetMenu(menuName = "Tag/Practice Routes", fileName = "PracticeRoutes")]
    public class PracticeRouteTable : ScriptableObject
    {
        public string sourceNote = "Assets/Resources/TagArena/PracticeRoutes.json";
        public int routeCount;

        public void Pull()
        {
            PracticeCatalog.Load();
            routeCount = PracticeCatalog.Count;
        }
    }
}
