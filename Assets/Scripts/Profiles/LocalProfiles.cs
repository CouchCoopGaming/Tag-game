using System;
using System.Globalization;
using System.Text;
using Tag.Core;
using Tag.Couch;
using Tag.Level;
using Tag.MatchStats;
using Tag.Practice;
using Tag.Settings;

namespace Tag.Profiles
{
    /// <summary>
    /// Couch profiles in the settings blob. A profile keeps a name, a color,
    /// a Hier look, binds, accessibility, and that player's practice board.
    /// Seats pick one profile or Guest. Two seats cannot hold the same profile.
    /// Colors that clash shift, and seat 0 does not keep the color by default.
    /// No new verb. Feel locks are not stored here.
    /// </summary>
    public static class LocalProfiles
    {
        public const int Max = 8;
        public const int Seats = 4;
        public const int NameMax = 12;
        public const int None = -1;
        public const int Guest = -2;
        public const string GuestName = "Guest";
        public const bool HeadbandMesh = false;

        public static readonly string[] HierNames =
        {
            "Blue", "Mint", "Orange", "Lavender", "Tan", "Red"
        };

        public const int Cols = 10;
        public const int Rows = 4;

        static readonly char[] Grid =
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3',
            '4', '5', '6', '7', '8', '9', ' ', '\b', '\n', '\0'
        };

        static readonly bool[] Used = new bool[Max];
        static readonly int[] Id = new int[Max];
        static readonly string[] Name = new string[Max];
        static readonly int[] Color = new int[Max];
        static readonly int[] Hier = new int[Max];
        static readonly int[] Accent = new int[Max];
        static readonly int[] Hat = new int[Max];
        static readonly int[] Palette = new int[Max];
        static readonly float[] Scale = new float[Max];
        static readonly bool[] Captions = new bool[Max];
        static readonly int[] Rumble = new int[Max];
        static readonly ActionBinds[] Binds = new ActionBinds[Max];
        static readonly int[] Matches = new int[Max];
        static readonly int[] Wins = new int[Max];
        static readonly int[] Tags = new int[Max];
        static readonly float[] Live = new float[Max];
        static readonly string[] Card = new string[Max];

        static readonly string[] RouteId = new string[Max * PracticeBests.Slots];
        static readonly float[] RouteTime = new float[Max * PracticeBests.Slots];
        static readonly int[] RouteN = new int[Max * PracticeBests.Slots];
        static readonly float[] RouteSp = new float[Max * PracticeBests.Slots * PracticeBests.Splits];

        static readonly string[] GhId = new string[Max * PracticeGhost.Slots];
        static readonly int[] GhN = new int[Max * PracticeGhost.Slots];
        static readonly float[] GhX = new float[Max * PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] GhY = new float[Max * PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] GhZ = new float[Max * PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] GhYaw = new float[Max * PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly byte[] GhPose = new byte[Max * PracticeGhost.Slots * PracticeGhost.Cap];

        static readonly string[] HoldId = new string[PracticeBests.Slots];
        static readonly float[] HoldTime = new float[PracticeBests.Slots];
        static readonly int[] HoldN = new int[PracticeBests.Slots];
        static readonly float[] HoldSp = new float[PracticeBests.Slots * PracticeBests.Splits];
        static readonly string[] HoldGId = new string[PracticeGhost.Slots];
        static readonly int[] HoldGN = new int[PracticeGhost.Slots];
        static readonly float[] HoldGX = new float[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] HoldGY = new float[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] HoldGZ = new float[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] HoldGYaw = new float[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly byte[] HoldGPose = new byte[PracticeGhost.Slots * PracticeGhost.Cap];

        static readonly string[] ExId = new string[PracticeBests.Slots];
        static readonly float[] ExTime = new float[PracticeBests.Slots];
        static readonly int[] ExN = new int[PracticeBests.Slots];
        static readonly float[] ExSp = new float[PracticeBests.Slots * PracticeBests.Splits];
        static readonly string[] GExId = new string[PracticeGhost.Slots];
        static readonly int[] GExN = new int[PracticeGhost.Slots];
        static readonly float[] GExX = new float[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] GExY = new float[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] GExZ = new float[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] GExYaw = new float[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly byte[] GExPose = new byte[PracticeGhost.Slots * PracticeGhost.Cap];
        static readonly float[] ScratchX = new float[PracticeGhost.Cap];
        static readonly float[] ScratchY = new float[PracticeGhost.Cap];
        static readonly float[] ScratchZ = new float[PracticeGhost.Cap];
        static readonly float[] ScratchYaw = new float[PracticeGhost.Cap];
        static readonly byte[] ScratchPose = new byte[PracticeGhost.Cap];

        static readonly int[] Seat = { None, None, None, None };
        static readonly int[] Resolved = { 0, 1, 2, 3 };
        static readonly int[] GuestSerial = new int[Seats];
        static readonly int[] Roster = new int[MatchBook.Cap];
        static readonly string[] ItCache = new string[Seats];
        static readonly string[] LabelName = new string[Seats];
        static readonly int[] Order = new int[Seats];
        static readonly bool[] Taken = new bool[Seats];
        static readonly bool[] Done = new bool[Seats];
        static readonly int[] Opt = new int[2 + Max];
        static readonly int[] Scroll = new int[Seats];
        static readonly int[] SnapPal = new int[Seats];
        static readonly bool[] SnapCap = new bool[Seats];
        static readonly int[] SnapRum = new int[Seats];
        static readonly bool[] SnapSet = new bool[Seats];
        static readonly char[] CleanBuf = new char[NameMax];
        static readonly char[] PadBuf = new char[NameMax];
        static readonly StringBuilder Sb = new StringBuilder(128);
        static readonly StringBuilder Save = new StringBuilder(256);

        public static int LabelGen;

        static int _nextId;
        static int _active = -1;
        static int _armKind;
        static int _armId = -1;
        static bool _saw;
        static int _padLen;
        static int _col;
        static int _row;
        static char _typed;
        static int _joinSerial;
        static bool _snapBlind;
        static bool _snapBlindSet;
        public const int ListWindow = 5;
        const int KindRename = 1;
        const int KindDelete = 2;

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

        public static int Count
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Max; i++)
                {
                    if (Used[i]) n++;
                }
                return n;
            }
        }

        public static int Leftovers
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Max; i++)
                {
                    if (Used[i]) n++;
                }
                for (int s = 0; s < Seats; s++)
                {
                    if (Seat[s] != None) n++;
                }
                if (_armKind != 0) n++;
                if (_padLen != 0) n++;
                for (int i = 0; i < Roster.Length; i++)
                {
                    if (Roster[i] > 0) n++;
                }
                return n;
            }
        }

        public static void Clear()
        {
            for (int i = 0; i < Max; i++)
            {
                Used[i] = false;
                Id[i] = 0;
                Name[i] = null;
                Color[i] = 0;
                Hier[i] = 4;
                Accent[i] = 4;
                Hat[i] = 0;
                Palette[i] = 0;
                Scale[i] = GameSettings.HudDefault;
                Captions[i] = false;
                Rumble[i] = 0;
                Binds[i] = null;
                Matches[i] = 0;
                Wins[i] = 0;
                Tags[i] = 0;
                Live[i] = 0f;
                Card[i] = null;
            }
            ClearRoutes();
            for (int s = 0; s < Seats; s++)
            {
                Seat[s] = None;
                Resolved[s] = s;
                GuestSerial[s] = 0;
                ItCache[s] = null;
                LabelName[s] = null;
            }
            LabelGen++;
            for (int i = 0; i < Roster.Length; i++)
                Roster[i] = 0;
            _nextId = 0;
            _active = -1;
            _armKind = 0;
            _armId = -1;
            _saw = false;
            _padLen = 0;
            _col = 0;
            _row = 0;
            _typed = '\0';
            _joinSerial = 0;
            _snapBlind = false;
            _snapBlindSet = false;
            for (int s = 0; s < Seats; s++)
            {
                Scroll[s] = 0;
                SnapSet[s] = false;
                SnapPal[s] = 0;
                SnapCap[s] = false;
                SnapRum[s] = 0;
            }
        }

        public static void BeginRead()
        {
            Clear();
        }

        public static bool IsKey(string key)
        {
            return key != null && key.Length >= 4 && key[0] == 'p' && key[1] >= '0' && key[1] <= '7' && key[2] == '.';
        }

        public static void Write(StringBuilder text)
        {
            if (text == null) return;
            if (_active >= 0) StoreLive(_active);
            for (int s = 0; s < Max; s++)
            {
                if (!Used[s]) continue;
                string p = "p" + ((char)('0' + s)).ToString();
                Line(text, p + ".id", Id[s]);
                Line(text, p + ".nm", Name[s] ?? "Player");
                Line(text, p + ".col", Color[s]);
                Line(text, p + ".hier", Hier[s]);
                Line(text, p + ".acc", Accent[s]);
                Line(text, p + ".hat", Hat[s]);
                Line(text, p + ".pal", Palette[s]);
                Line(text, p + ".scale", Scale[s]);
                Line(text, p + ".cap", Captions[s] ? 1 : 0);
                Line(text, p + ".rum", Rumble[s]);
                Line(text, p + ".mat", Matches[s]);
                Line(text, p + ".win", Wins[s]);
                Line(text, p + ".tag", Tags[s]);
                Line(text, p + ".live", Live[s]);
                WriteBinds(text, s, p);
                WriteBoard(text, s, p);
            }
        }

        public static bool ApplyKey(string key, string value)
        {
            if (!IsKey(key)) return false;
            int slot = key[1] - '0';
            if (Touch(slot) < 0) return true;
            _saw = true;
            string rest = key.Substring(3);
            if (rest == "id")
            {
                int id = Int(value, Id[slot]);
                if (id > 0) Id[slot] = id;
                if (id > _nextId) _nextId = id;
            }
            else if (rest == "nm") Name[slot] = Clean(value);
            else if (rest == "col") Color[slot] = Clamp(Int(value, 0), 0, 3);
            else if (rest == "hier") Hier[slot] = Clamp(Int(value, 4), 0, HierNames.Length - 1);
            else if (rest == "acc") Accent[slot] = Clamp(Int(value, 4), 0, HierNames.Length - 1);
            else if (rest == "hat") Hat[slot] = Int(value, 0) == 0 ? 0 : 1;
            else if (rest == "pal") Palette[slot] = Clamp(Int(value, 0), 0, AccessibilityPalette.Count - 1);
            else if (rest == "scale") Scale[slot] = ClampScale(Num(value, GameSettings.HudDefault));
            else if (rest == "cap") Captions[slot] = value == "1";
            else if (rest == "rum") Rumble[slot] = Clamp(Int(value, 0), 0, 100);
            else if (rest == "mat") Matches[slot] = Math.Max(0, Int(value, 0));
            else if (rest == "win") Wins[slot] = Math.Max(0, Int(value, 0));
            else if (rest == "tag") Tags[slot] = Math.Max(0, Int(value, 0));
            else if (rest == "live") Live[slot] = Math.Max(0f, Num(value, 0f));
            else if (rest.StartsWith("kb.", StringComparison.Ordinal))
                AssignBind(slot, rest.Substring(3), value, true);
            else if (rest.StartsWith("pad.", StringComparison.Ordinal))
                AssignBind(slot, rest.Substring(4), value, false);
            else if (rest.StartsWith("pb.", StringComparison.Ordinal))
                SetRouteTime(slot, rest.Substring(3), Num(value, 0f));
            else if (rest.StartsWith("sp.", StringComparison.Ordinal))
                SetRouteSplits(slot, rest.Substring(3), value);
            else if (rest.StartsWith("gh.", StringComparison.Ordinal))
                TakeGhost(slot, rest.Substring(3), value);
            return true;
        }

        public static void EndRead(int version, GameSettings settings, ActionBinds binds)
        {
            if (_saw)
            {
                if (!PracticeBests.HasAny())
                {
                    int live = FirstUsed();
                    if (live >= 0) Restore(live);
                }
                RebuildLabels();
                return;
            }
            if (version >= SettingsFile.Version) return;
            int made = Create("Player");
            int slot = Find(made);
            if (slot < 0) return;
            if (settings != null)
            {
                Palette[slot] = Clamp(settings.Palette[0], 0, AccessibilityPalette.Count - 1);
                Captions[slot] = settings.Captions[0];
                Rumble[slot] = Clamp(settings.Rumble[0], 0, 100);
                Scale[slot] = ClampScale(settings.HudScale);
            }
            if (binds != null) CopyBinds(slot, binds);
            StoreLive(slot);
            RebuildCard(slot);
        }

        public static int Create(string name)
        {
            int slot = Free();
            if (slot < 0) return -1;
            Used[slot] = true;
            Id[slot] = ++_nextId;
            Name[slot] = Clean(name);
            Color[slot] = slot & 3;
            Hier[slot] = 4;
            Accent[slot] = 4;
            Hat[slot] = 0;
            Palette[slot] = 0;
            Scale[slot] = GameSettings.HudDefault;
            Captions[slot] = false;
            Rumble[slot] = 0;
            Binds[slot] = ActionBinds.Defaults();
            Matches[slot] = 0;
            Wins[slot] = 0;
            Tags[slot] = 0;
            Live[slot] = 0f;
            Card[slot] = null;
            ClearBoard(slot);
            RebuildCard(slot);
            return Id[slot];
        }

        public static int CreateFromPad()
        {
            return Create(PadText());
        }

        public static bool ArmRename(int id)
        {
            if (Find(id) < 0) return false;
            _armKind = KindRename;
            _armId = id;
            return true;
        }

        public static bool Rename(string name)
        {
            if (_armKind != KindRename) return false;
            int slot = Find(_armId);
            _armKind = 0;
            _armId = -1;
            if (slot < 0) return false;
            if (!AcceptName(name, out string next) || NameTaken(slot, next)) return false;
            Name[slot] = next;
            RebuildLabels();
            RebuildCard(slot);
            CouchPlay.RefreshTags();
            return true;
        }

        public static bool RenameFromPad()
        {
            return Rename(PadText());
        }

        public static bool ArmDelete(int id)
        {
            if (Find(id) < 0) return false;
            _armKind = KindDelete;
            _armId = id;
            return true;
        }

        public static bool Delete()
        {
            if (_armKind != KindDelete) return false;
            int id = _armId;
            _armKind = 0;
            _armId = -1;
            int slot = Find(id);
            if (slot < 0) return false;
            for (int s = 0; s < Seats; s++)
            {
                if (Seat[s] != slot) continue;
                RestoreSnap(s);
                Seat[s] = None;
            }
            for (int i = 0; i < Roster.Length; i++)
            {
                if (Roster[i] == id) Roster[i] = 0;
            }
            if (_active == slot)
            {
                _active = -1;
                PracticeBests.Clear();
                PracticeGhost.Clear();
                PracticeGhost.ClearSaved();
            }
            Used[slot] = false;
            Id[slot] = 0;
            Name[slot] = null;
            Binds[slot] = null;
            Card[slot] = null;
            ClearBoard(slot);
            Resolve();
            RebuildLabels();
            CouchPlay.RefreshTags();
            return true;
        }

        public static string NameOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? "" : (Name[slot] ?? "");
        }

        public static string CardOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? "" : (Card[slot] ?? "");
        }

        public static string FirstCard()
        {
            for (int i = 0; i < Max; i++)
            {
                if (Used[i] && Card[i] != null && Card[i].Length > 0) return Card[i];
            }
            return null;
        }

        public static void SetLook(int id, int hier, int accent, int hat)
        {
            int slot = Find(id);
            if (slot < 0) return;
            Hier[slot] = Clamp(hier, 0, HierNames.Length - 1);
            Accent[slot] = Clamp(accent, 0, HierNames.Length - 1);
            Hat[slot] = hat == 0 ? 0 : 1;
        }

        public static string HierKey(int id)
        {
            int slot = Find(id);
            if (slot < 0) return HierNames[4];
            return HierNames[Hier[slot]];
        }

        public static string AccentKey(int id)
        {
            int slot = Find(id);
            if (slot < 0) return HierNames[4];
            return HierNames[Accent[slot]];
        }

        public static int HatOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? 0 : Hat[slot];
        }

        public static string HierKeyFor(string playerName)
        {
            int slot = SlotForName(playerName);
            if (slot < 0) return null;
            return HierNames[Hier[slot]];
        }

        public static string AccentKeyFor(string playerName)
        {
            int slot = SlotForName(playerName);
            if (slot < 0) return null;
            return HierNames[Accent[slot]];
        }

        public static int HatFor(string playerName)
        {
            int slot = SlotForName(playerName);
            return slot < 0 ? 0 : Hat[slot];
        }

        public static void SetColor(int id, int color)
        {
            int slot = Find(id);
            if (slot < 0) return;
            Color[slot] = Clamp(color, 0, 3);
            Resolve();
        }

        public static void SetAccess(int id, int palette, float scale, bool captions, int rumble)
        {
            int slot = Find(id);
            if (slot < 0) return;
            Palette[slot] = Clamp(palette, 0, AccessibilityPalette.Count - 1);
            Scale[slot] = ClampScale(scale);
            Captions[slot] = captions;
            Rumble[slot] = Clamp(rumble, 0, 100);
            for (int s = 0; s < Seats; s++)
            {
                if (Seat[s] == slot) ApplyAccess(s);
            }
        }

        public static ActionBinds BindsOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? null : Binds[slot];
        }

        public static bool TrySeat(int seat, int id)
        {
            if (seat < 0 || seat >= Seats) return false;
            int slot = Find(id);
            if (slot < 0) return false;
            for (int s = 0; s < Seats; s++)
            {
                if (s != seat && Seat[s] == slot) return false;
            }
            Remember(seat);
            Seat[seat] = slot;
            ApplyAccess(seat);
            if (seat == 0) Use(id);
            Resolve();
            return true;
        }

        public static void SeatGuest(int seat)
        {
            if (seat < 0 || seat >= Seats) return;
            RestoreSnap(seat);
            Seat[seat] = Guest;
            GuestSerial[seat] = ++_joinSerial;
            if (seat < Roster.Length) Roster[seat] = 0;
            if (seat == 0) ParkBoard();
            Resolve();
        }

        public static void ClearSeat(int seat)
        {
            if (seat < 0 || seat >= Seats) return;
            RestoreSnap(seat);
            Seat[seat] = None;
            if (seat < Roster.Length) Roster[seat] = 0;
            if (seat == 0) ParkBoard();
            Resolve();
        }

        public static void ClearSeats()
        {
            for (int s = 0; s < Seats; s++)
                ClearSeat(s);
        }

        /// <summary>Menu rebind writes this seat's table only. The clone is not the other seat's object.</summary>
        public static void StoreBinds(int seat, ActionBinds binds)
        {
            if (binds == null || seat < 0 || seat >= Seats) return;
            int slot = Seat[seat];
            if (slot < 0 || slot >= Max || !Used[slot]) return;
            Binds[slot] = binds.Clone();
        }

        public static int ListScrollOf(int seat)
        {
            if (seat < 0 || seat >= Seats) return 0;
            return Scroll[seat];
        }

        public static int ListIndex(int seat)
        {
            if (seat < 0 || seat >= Seats) return 0;
            int n = FillOpt(seat);
            int cur = CurrentOpt(seat);
            for (int i = 0; i < n; i++)
            {
                if (Opt[i] == cur) return i;
            }
            return 0;
        }

        public static int Cycle(int seat, int dir)
        {
            if (seat < 0 || seat >= Seats) return None;
            int n = FillOpt(seat);
            int cur = CurrentOpt(seat);
            int idx = 0;
            for (int i = 0; i < n; i++)
            {
                if (Opt[i] == cur) idx = i;
            }
            idx += dir < 0 ? -1 : 1;
            if (idx < 0) idx = 0;
            if (idx >= n) idx = n - 1;
            Focus(seat, idx, n);
            return Opt[idx];
        }

        public static string SeatName(int seat)
        {
            if (seat < 0 || seat >= Seats) return null;
            int slot = Seat[seat];
            if (slot == Guest) return GuestName;
            if (slot < 0 || slot >= Max || !Used[slot]) return null;
            return Name[slot];
        }

        public static int SeatColor(int seat)
        {
            if (seat < 0 || seat >= Seats) return -1;
            if (Seat[seat] == None) return -1;
            return Resolved[seat];
        }

        public static ActionBinds BindsForSeat(int seat)
        {
            if (seat < 0 || seat >= Seats) return null;
            int slot = Seat[seat];
            if (slot < 0 || slot >= Max || !Used[slot]) return null;
            return Binds[slot];
        }

        public static int ProfileAt(int seat)
        {
            if (seat < 0 || seat >= Seats) return -1;
            int slot = Seat[seat];
            if (slot < 0 || slot >= Max || !Used[slot]) return -1;
            return Id[slot];
        }

        public static float TextScale(int seat)
        {
            if (seat >= 0 && seat < Seats)
            {
                int slot = Seat[seat];
                if (slot >= 0 && slot < Max && Used[slot]) return Scale[slot];
            }
            if (GameSettings.Current == null) return GameSettings.HudDefault;
            return GameSettings.Current.HudScale;
        }

        public static string ItLabel(int seat)
        {
            if (seat < 0 || seat >= Seats) return "IT";
            if (ItCache[seat] != null) return ItCache[seat];
            return "IT";
        }

        public static void NoteRoster(int index, int id)
        {
            if (index < 0 || index >= Roster.Length) return;
            Roster[index] = id;
        }

        public static void Absorb()
        {
            int n = MatchBook.Count;
            if (n < 1) return;
            if (n > MatchBook.Cap) n = MatchBook.Cap;
            float best = MatchBook.TimeAsIt[0];
            int winners = 1;
            for (int i = 1; i < n; i++)
            {
                float t = MatchBook.TimeAsIt[i];
                if (t < best - 0.0001f)
                {
                    best = t;
                    winners = 1;
                }
                else if (t <= best + 0.0001f)
                    winners++;
            }
            for (int i = 0; i < n; i++)
            {
                int id = i < Roster.Length ? Roster[i] : 0;
                int slot = Find(id);
                if (slot < 0) continue;
                Matches[slot]++;
                Tags[slot] += MatchBook.TagsMade[i];
                if (MatchBook.LongestSurvival[i] > Live[slot])
                    Live[slot] = MatchBook.LongestSurvival[i];
                if (winners == 1 && MatchBook.TimeAsIt[i] <= best + 0.0001f)
                    Wins[slot]++;
                RebuildCard(slot);
            }
        }

        public static float PbTime(int id, string route)
        {
            int slot = Find(id);
            if (slot < 0) return 0f;
            int at = RouteSlot(slot, route, false);
            if (at < 0) return 0f;
            return RouteTime[at];
        }

        public static int GhostSamples(int id, string route)
        {
            int slot = Find(id);
            if (slot < 0) return 0;
            int at = GhostSlot(slot, route, false);
            if (at < 0) return 0;
            return GhN[at];
        }

        public static int PaletteOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? 0 : Palette[slot];
        }

        public static bool CaptionsOf(int id)
        {
            int slot = Find(id);
            return slot >= 0 && Captions[slot];
        }

        public static int RumbleOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? 0 : Rumble[slot];
        }

        public static float ScaleOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? GameSettings.HudDefault : Scale[slot];
        }

        public static bool Use(int id)
        {
            int slot = Find(id);
            if (slot < 0) return false;
            if (_active >= 0 && _active != slot) StoreLive(_active);
            Restore(slot);
            _active = slot;
            return true;
        }

        public static void PadClear()
        {
            _padLen = 0;
            _col = 0;
            _row = 0;
            _typed = '\0';
        }

        public static void PadMove(int dx, int dy)
        {
            _col += dx;
            _row += dy;
            if (_col < 0) _col = 0;
            if (_row < 0) _row = 0;
            if (_col >= Cols) _col = Cols - 1;
            if (_row >= Rows) _row = Rows - 1;
        }

        public static bool PadType()
        {
            char c = _typed != '\0' ? _typed : Grid[_row * Cols + _col];
            _typed = '\0';
            if (c == '\0') return false;
            if (c == '\b')
            {
                if (_padLen > 0) _padLen--;
                return true;
            }
            if (c == '\n') return true;
            if (_padLen >= NameMax) return false;
            PadBuf[_padLen++] = c;
            return true;
        }

        public static string PadText()
        {
            if (_padLen <= 0) return "";
            return new string(PadBuf, 0, _padLen);
        }

        public static bool Spell(string word)
        {
            PadClear();
            if (string.IsNullOrEmpty(word)) return true;
            int n = word.Length;
            if (n > NameMax) n = NameMax;
            for (int i = 0; i < word.Length; i++)
            {
                if (!Seek(word[i])) return false;
                if (!PadType() && i < NameMax) return false;
            }
            string got = PadText();
            if (word.Length <= NameMax) return got == word;
            return got.Length == NameMax;
        }

        public static Report Run()
        {
            var report = new Report();
            GameSettings backup = GameSettings.Defaults();
            if (GameSettings.Current != null) backup.CopyFrom(GameSettings.Current);
            else GameSettings.Current = GameSettings.Defaults();
            PracticeBests.Export(HoldId, HoldTime, HoldN, HoldSp);
            PracticeGhost.ExportSaved(HoldGId, HoldGN, HoldGX, HoldGY, HoldGZ, HoldGYaw, HoldGPose);
            try
            {
                Clear();
                CouchPlay.Release();
                if (!CheckCreate(report)) return Finish(report);
                if (!CheckMigrate(report)) return Finish(report);
                if (!CheckSeats(report)) return Finish(report);
                if (!CheckLife(report)) return Finish(report);
                if (!CheckRoutes(report)) return Finish(report);
                int alloc = Measure();
                Clear();
                CouchPlay.Release();
                MatchBook.ResetMatch();
                if (alloc != 0) report.Fail("alloc " + alloc.ToString(CultureInfo.InvariantCulture));
                if (Leftovers != 0) report.Fail("leftovers");
                if (!report.Ok) return Finish(report);
                report.Line = "profiles create=ok rename=ok delete=ok migrate=ok dupSeat=blocked colorClash=fixed bindsPerProfile=ok pbMoved=ok alloc=0 leftovers=0";
                return report;
            }
            finally
            {
                PracticeBests.Import(HoldId, HoldTime, HoldN, HoldSp);
                PracticeGhost.ImportSaved(HoldGId, HoldGN, HoldGX, HoldGY, HoldGZ, HoldGYaw, HoldGPose);
                if (GameSettings.Current == null) GameSettings.Current = GameSettings.Defaults();
                GameSettings.Current.CopyFrom(backup);
            }
        }

        static Report Finish(Report report)
        {
            Clear();
            CouchPlay.Release();
            MatchBook.ResetMatch();
            report.Line = "profiles FAIL " + report.Failure;
            return report;
        }

        static bool CheckCreate(Report report)
        {
            if (!Spell("Sam"))
            {
                report.Fail("pad name");
                return false;
            }
            int sam = CreateFromPad();
            if (sam <= 0 || NameOf(sam) != "Sam" || NameOf(sam).Length > NameMax)
            {
                report.Fail("create");
                return false;
            }
            SetLook(sam, 4, 0, 1);
            if (HierKey(sam) != "Tan" || AccentKey(sam) != "Blue" || HatOf(sam) != 1 || HeadbandMesh)
            {
                report.Fail("look");
                return false;
            }
            PadClear();
            if (!Seek('A'))
            {
                report.Fail("pad seek");
                return false;
            }
            int typed = 0;
            for (int i = 0; i < 13; i++)
            {
                if (PadType()) typed++;
            }
            if (typed != NameMax || PadText().Length != NameMax)
            {
                report.Fail("name cap");
                return false;
            }
            int alex = Create("Alex");
            if (Rename("Pat"))
            {
                report.Fail("rename skipped confirm");
                return false;
            }
            if (!ArmRename(alex) || !Rename("Pat") || NameOf(alex) != "Pat")
            {
                report.Fail("rename");
                return false;
            }
            if (Delete())
            {
                report.Fail("delete skipped confirm");
                return false;
            }
            if (!ArmDelete(alex) || !Delete() || Find(alex) >= 0)
            {
                report.Fail("delete");
                return false;
            }
            if (Count != 1 || NameOf(sam) != "Sam")
            {
                report.Fail("delete removed the other profile");
                return false;
            }
            return true;
        }

        static bool CheckMigrate(Report report)
        {
            var settings = GameSettings.Defaults();
            var binds = ActionBinds.Defaults();
            const string old = "v=1\npb.mega-beginner=9.5\nsp.mega-beginner=1.250,2.500\ngh.mega-beginner=2;1,2,3,0,1;4,5,6,0.5,2\npalette=2\ncaptions=1\nrumble=75\nhud=1.25\nkb.Jump=e\npad.Jump=buttonNorth\n";
            SettingsFile.Read(old, settings, binds);
            if (Count != 1)
            {
                report.Fail("migrate count");
                return false;
            }
            int id = Id[FirstUsed()];
            if (Math.Abs(PbTime(id, "mega-beginner") - 9.5f) > 0.001f)
            {
                report.Fail("pb moved");
                return false;
            }
            if (GhostSamples(id, "mega-beginner") != 2)
            {
                report.Fail("ghost moved");
                return false;
            }
            if (PaletteOf(id) != 2 || !CaptionsOf(id) || RumbleOf(id) != 75 || Math.Abs(ScaleOf(id) - 1.25f) > 0.001f)
            {
                report.Fail("access moved");
                return false;
            }
            ActionBinds owned = BindsOf(id);
            if (owned == null || owned.Keyboard[(int)PlayAction.Jump] != "e" || owned.Gamepad[(int)PlayAction.Jump] != "buttonNorth")
            {
                report.Fail("binds moved");
                return false;
            }
            if (Math.Abs(PracticeBests.TimeOf("mega-beginner") - 9.5f) > 0.001f)
            {
                report.Fail("live pb");
                return false;
            }
            PracticeBests.Clear();
            PracticeGhost.ClearSaved();
            if (!Use(id) || Math.Abs(PracticeBests.TimeOf("mega-beginner") - 9.5f) > 0.001f)
            {
                report.Fail("pb restore");
                return false;
            }
            if (!PracticeGhost.Load("mega-beginner") || PracticeGhost.Count != 2)
            {
                report.Fail("ghost restore");
                return false;
            }
            string blob = SettingsFile.Write(settings, binds);
            if (blob.IndexOf("p0.pb.0/mega-beginner=", StringComparison.Ordinal) < 0)
            {
                report.Fail("pb not under profile");
                return false;
            }
            PracticeBests.Clear();
            PracticeGhost.ClearSaved();
            var again = GameSettings.Defaults();
            var againBinds = ActionBinds.Defaults();
            SettingsFile.Read(blob, again, againBinds);
            int id2 = Id[FirstUsed()];
            if (Math.Abs(PbTime(id2, "mega-beginner") - 9.5f) > 0.001f)
            {
                report.Fail("pb round trip");
                return false;
            }
            return true;
        }

        static bool CheckSeats(Report report)
        {
            Clear();
            CouchPlay.Release();
            PracticeBests.Clear();
            PracticeGhost.ClearSaved();
            int low = Create("Ann");
            int high = Create("Bea");
            SetColor(low, 0);
            SetColor(high, 0);
            if (!TrySeat(0, high) || !TrySeat(1, low))
            {
                report.Fail("seat");
                return false;
            }
            if (SeatColor(1) != 0 || SeatColor(0) == 0 || SeatColor(0) == SeatColor(1))
            {
                report.Fail("p1 kept the clash");
                return false;
            }
            ClearSeats();
            if (!TrySeat(0, low) || !TrySeat(1, high))
            {
                report.Fail("swap seat");
                return false;
            }
            if (SeatColor(0) != 0 || SeatColor(1) == SeatColor(0) || !Apart(0, 1))
            {
                report.Fail("color clash");
                return false;
            }
            if (TrySeat(1, low))
            {
                report.Fail("dup seat");
                return false;
            }
            SeatGuest(1);
            if (SeatName(1) != GuestName || !TrySeat(1, high))
            {
                report.Fail("guest");
                return false;
            }
            if (!CouchPlay.Join(CouchPlay.DeviceKeyboard) || !CouchPlay.Join(CouchPlay.DevicePad0))
            {
                report.Fail("join");
                return false;
            }
            ActionBinds a = BindsOf(low);
            ActionBinds b = BindsOf(high);
            a.SetGamepad(PlayAction.Jump, "buttonNorth");
            b.SetKeyboard(PlayAction.Jump, "e");
            ActionBinds key = CouchPlay.BindsFor(CouchPlay.DeviceKeyboard);
            ActionBinds pad = CouchPlay.BindsFor(CouchPlay.DevicePad0);
            if (key != a || pad != b)
            {
                report.Fail("binds not per profile");
                return false;
            }
            if (key.Gamepad[(int)PlayAction.Jump] != "buttonNorth" || pad.Gamepad[(int)PlayAction.Jump] != "buttonSouth")
            {
                report.Fail("pad bind leaked");
                return false;
            }
            if (pad.Keyboard[(int)PlayAction.Jump] == "e" && key.Keyboard[(int)PlayAction.Jump] == "e")
            {
                report.Fail("keyboard bind leaked");
                return false;
            }
            if (ActionBinds.Current != null && ActionBinds.Current.Gamepad[(int)PlayAction.Jump] != "buttonSouth")
            {
                report.Fail("profile bind wrote current");
                return false;
            }
            return true;
        }

        static bool CheckLife(Report report)
        {
            int low = 0;
            int high = 0;
            for (int i = 0; i < Max; i++)
            {
                if (!Used[i]) continue;
                if (Name[i] == "Ann") low = Id[i];
                if (Name[i] == "Bea") high = Id[i];
            }
            MatchBook.ResetMatch();
            MatchBook.Open(2);
            MatchBook.TagsMade[0] = 4;
            MatchBook.TimeAsIt[0] = 1f;
            MatchBook.TimeAsIt[1] = 5f;
            MatchBook.LongestSurvival[0] = 6.2f;
            MatchBook.LongestSurvival[1] = 1f;
            NoteRoster(0, low);
            NoteRoster(1, high);
            Absorb();
            string card = CardOf(low);
            if (card.IndexOf("matches", StringComparison.Ordinal) < 0
                || card.IndexOf("wins", StringComparison.Ordinal) < 0
                || card.IndexOf("tags", StringComparison.Ordinal) < 0
                || card.IndexOf("live", StringComparison.Ordinal) < 0
                || card.IndexOf("4", StringComparison.Ordinal) < 0
                || card.IndexOf("6.2", StringComparison.Ordinal) < 0)
            {
                report.Fail("card");
                return false;
            }
            if (WinsOf(low) != 1 || WinsOf(high) != 0 || TagsOf(low) != 4)
            {
                report.Fail("lifetime");
                return false;
            }
            return true;
        }

        static int WinsOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? 0 : Wins[slot];
        }

        static int TagsOf(int id)
        {
            int slot = Find(id);
            return slot < 0 ? 0 : Tags[slot];
        }

        static int Measure()
        {
            Resolve();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 64; i++)
                Resolve();
            long after = GC.GetAllocatedBytesForCurrentThread();
            long delta = after - before;
            if (delta < 0) delta = 0;
            if (delta > int.MaxValue) return int.MaxValue;
            return (int)delta;
        }

        static void Resolve()
        {
            for (int s = 0; s < Seats; s++)
            {
                Resolved[s] = s;
                Done[s] = false;
            }
            int n = 0;
            for (int s = 0; s < Seats; s++)
            {
                if (Seat[s] != None) Order[n++] = s;
            }
            if (n == 0)
            {
                RebuildLabels();
                return;
            }
            for (int c = 0; c < Seats; c++)
                Taken[c] = false;
            for (int c = 0; c < Seats; c++)
            {
                int best = -1;
                int bestId = int.MaxValue;
                for (int k = 0; k < n; k++)
                {
                    int s = Order[k];
                    int slot = Seat[s];
                    if (slot < 0) continue;
                    if (Color[slot] != c) continue;
                    if (Id[slot] < bestId)
                    {
                        bestId = Id[slot];
                        best = s;
                    }
                }
                if (best >= 0)
                {
                    int wanters = 0;
                    for (int k = 0; k < n; k++)
                    {
                        int s = Order[k];
                        int slot = Seat[s];
                        if (slot < 0) continue;
                        if (Color[slot] != c) continue;
                        wanters++;
                    }
                    bool clash = !AccessibilityPalette.ClearsIt(PaletteFor(best), c);
                    if (wanters >= 2 || !clash)
                    {
                        Resolved[best] = c;
                        Taken[c] = true;
                        Done[best] = true;
                    }
                }
            }
            for (int a = 0; a < n; a++)
            {
                for (int b = a + 1; b < n; b++)
                {
                    if (Rank(Order[b]) < Rank(Order[a]))
                    {
                        int tmp = Order[a];
                        Order[a] = Order[b];
                        Order[b] = tmp;
                    }
                }
            }
            for (int k = 0; k < n; k++)
            {
                int s = Order[k];
                if (Done[s]) continue;
                int pal = PaletteFor(s);
                int pick = -1;
                for (int c = 0; c < Seats; c++)
                {
                    if (Taken[c]) continue;
                    if (Fits(s, pal, c) && AccessibilityPalette.ClearsIt(pal, c))
                    {
                        pick = c;
                        break;
                    }
                }
                if (pick < 0)
                {
                    for (int c = 0; c < Seats; c++)
                    {
                        if (Taken[c]) continue;
                        if (Fits(s, pal, c))
                        {
                            pick = c;
                            break;
                        }
                    }
                }
                if (pick < 0)
                {
                    for (int c = 0; c < Seats; c++)
                    {
                        if (!Taken[c])
                        {
                            pick = c;
                            break;
                        }
                    }
                }
                if (pick < 0) pick = 0;
                Resolved[s] = pick;
                Taken[pick] = true;
                Done[s] = true;
            }
            RebuildLabels();
        }

        static int Rank(int seat)
        {
            int slot = Seat[seat];
            if (slot >= 0) return Id[slot];
            return 100000 + GuestSerial[seat];
        }

        static int PaletteFor(int seat)
        {
            int slot = Seat[seat];
            if (slot >= 0 && slot < Max) return Palette[slot];
            if (GameSettings.Current == null) return 0;
            return GameSettings.Current.PaletteOf(seat);
        }

        static bool Fits(int seat, int palette, int color)
        {
            for (int s = 0; s < Seats; s++)
            {
                if (s == seat || !Done[s] || Seat[s] == None) continue;
                if (!Far(palette, color, PaletteFor(s), Resolved[s])) return false;
            }
            return true;
        }

        static bool Apart(int a, int b)
        {
            return Far(PaletteFor(a), Resolved[a], PaletteFor(b), Resolved[b]);
        }

        static bool Far(int palA, int slotA, int palB, int slotB)
        {
            AccessibilityPalette.Player(palA, slotA, out float ar, out float ag, out float ab);
            AccessibilityPalette.Player(palB, slotB, out float br, out float bg, out float bb);
            for (int cvd = 0; cvd < AccessibilityPalette.CvdCount; cvd++)
            {
                AccessibilityPalette.Simulate(cvd, ar, ag, ab, out float ar2, out float ag2, out float ab2);
                AccessibilityPalette.Simulate(cvd, br, bg, bb, out float br2, out float bg2, out float bb2);
                float dr = ar2 - br2;
                float dg = ag2 - bg2;
                float db = ab2 - bb2;
                float d = (float)Math.Sqrt(dr * dr + dg * dg + db * db);
                if (d < AccessibilityPalette.MinPairDistance) return false;
            }
            return true;
        }

        static void ApplyAccess(int seat)
        {
            int slot = Seat[seat];
            if (slot < 0 || GameSettings.Current == null) return;
            GameSettings.Current.Palette[seat] = Palette[slot];
            GameSettings.Current.Captions[seat] = Captions[slot];
            GameSettings.Current.Rumble[seat] = Rumble[slot];
            if (seat == 0) GameSettings.Current.Colorblind = Palette[slot] != 0;
        }

        static void RebuildLabels()
        {
            bool dirty = false;
            for (int s = 0; s < Seats; s++)
            {
                string n = SeatName(s);
                if (n == LabelName[s]) continue;
                LabelName[s] = n;
                ItCache[s] = n == null ? null : "IT " + n;
                dirty = true;
            }
            if (dirty) LabelGen++;
        }

        static void RebuildCard(int slot)
        {
            if (slot < 0 || !Used[slot]) return;
            Sb.Clear();
            Sb.Append(Name[slot] ?? "Player");
            Sb.Append('\n');
            Sb.Append("matches ");
            Sb.Append(HudDigits.Whole0(Matches[slot]));
            Sb.Append('\n');
            Sb.Append("wins ");
            Sb.Append(HudDigits.Whole0(Wins[slot]));
            Sb.Append('\n');
            Sb.Append("tags ");
            Sb.Append(HudDigits.Whole0(Tags[slot]));
            Sb.Append('\n');
            Sb.Append("live ");
            Sb.Append(HudDigits.Tenth0(Live[slot]));
            Card[slot] = Sb.ToString();
        }

        static int SlotForName(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return -1;
            for (int s = 0; s < Seats; s++)
            {
                int slot = Seat[s];
                if (slot >= 0 && Name[slot] == playerName) return slot;
            }
            for (int i = 0; i < Max; i++)
            {
                if (Used[i] && Name[i] == playerName) return i;
            }
            return -1;
        }

        static int Find(int id)
        {
            if (id <= 0) return -1;
            for (int i = 0; i < Max; i++)
            {
                if (Used[i] && Id[i] == id) return i;
            }
            return -1;
        }

        static int Free()
        {
            for (int i = 0; i < Max; i++)
            {
                if (!Used[i]) return i;
            }
            return -1;
        }

        static int FirstUsed()
        {
            for (int i = 0; i < Max; i++)
            {
                if (Used[i]) return i;
            }
            return -1;
        }

        static int Touch(int slot)
        {
            if (slot < 0 || slot >= Max) return -1;
            if (!Used[slot])
            {
                Used[slot] = true;
                Id[slot] = ++_nextId;
                Name[slot] = "Player";
                Hier[slot] = 4;
                Accent[slot] = 4;
                Scale[slot] = GameSettings.HudDefault;
                Binds[slot] = ActionBinds.Defaults();
            }
            return slot;
        }

        static void CopyBinds(int slot, ActionBinds src)
        {
            if (Binds[slot] == null) Binds[slot] = ActionBinds.Defaults();
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                Binds[slot].SetKeyboard((PlayAction)i, src.Keyboard[i]);
                Binds[slot].SetGamepad((PlayAction)i, src.Gamepad[i]);
            }
        }

        static void AssignBind(int slot, string name, string value, bool keyboard)
        {
            if (Binds[slot] == null) Binds[slot] = ActionBinds.Defaults();
            if (string.IsNullOrEmpty(value)) return;
            if (!Enum.TryParse(name, false, out PlayAction action)) return;
            if (action < 0 || action >= PlayAction.Count) return;
            if (keyboard) Binds[slot].SetKeyboard(action, value);
            else Binds[slot].SetGamepad(action, value);
        }

        static void StoreLive(int slot)
        {
            PracticeBests.Export(ExId, ExTime, ExN, ExSp);
            int b = slot * PracticeBests.Slots;
            for (int s = 0; s < PracticeBests.Slots; s++)
            {
                RouteId[b + s] = string.IsNullOrEmpty(ExId[s]) ? null : Canon(ExId[s]);
                RouteTime[b + s] = ExTime[s];
                RouteN[b + s] = ExN[s];
                int o = s * PracticeBests.Splits;
                int d = (b + s) * PracticeBests.Splits;
                for (int i = 0; i < PracticeBests.Splits; i++)
                    RouteSp[d + i] = ExSp[o + i];
            }
            PracticeGhost.ExportSaved(GExId, GExN, GExX, GExY, GExZ, GExYaw, GExPose);
            int g = slot * PracticeGhost.Slots;
            for (int s = 0; s < PracticeGhost.Slots; s++)
            {
                GhId[g + s] = string.IsNullOrEmpty(GExId[s]) ? null : Canon(GExId[s]);
                GhN[g + s] = GExN[s];
                int n = GExN[s];
                if (n < 0) n = 0;
                if (n > PracticeGhost.Cap) n = PracticeGhost.Cap;
                int o = s * PracticeGhost.Cap;
                int d = (g + s) * PracticeGhost.Cap;
                for (int i = 0; i < n; i++)
                {
                    GhX[d + i] = GExX[o + i];
                    GhY[d + i] = GExY[o + i];
                    GhZ[d + i] = GExZ[o + i];
                    GhYaw[d + i] = GExYaw[o + i];
                    GhPose[d + i] = GExPose[o + i];
                }
            }
        }

        static void Restore(int slot)
        {
            int b = slot * PracticeBests.Slots;
            for (int s = 0; s < PracticeBests.Slots; s++)
            {
                ExId[s] = RouteOf(RouteId[b + s]);
                ExTime[s] = RouteTime[b + s];
                ExN[s] = RouteN[b + s];
                int o = s * PracticeBests.Splits;
                int d = (b + s) * PracticeBests.Splits;
                for (int i = 0; i < PracticeBests.Splits; i++)
                    ExSp[o + i] = RouteSp[d + i];
            }
            PracticeBests.Import(ExId, ExTime, ExN, ExSp);
            int g = slot * PracticeGhost.Slots;
            for (int s = 0; s < PracticeGhost.Slots; s++)
            {
                GExId[s] = RouteOf(GhId[g + s]);
                GExN[s] = GhN[g + s];
                int n = GhN[g + s];
                if (n < 0) n = 0;
                if (n > PracticeGhost.Cap) n = PracticeGhost.Cap;
                int o = s * PracticeGhost.Cap;
                int d = (g + s) * PracticeGhost.Cap;
                for (int i = 0; i < n; i++)
                {
                    GExX[o + i] = GhX[d + i];
                    GExY[o + i] = GhY[d + i];
                    GExZ[o + i] = GhZ[d + i];
                    GExYaw[o + i] = GhYaw[d + i];
                    GExPose[o + i] = GhPose[d + i];
                }
            }
            PracticeGhost.ImportSaved(GExId, GExN, GExX, GExY, GExZ, GExYaw, GExPose);
        }

        static void ClearRoutes()
        {
            for (int i = 0; i < RouteId.Length; i++)
            {
                RouteId[i] = null;
                RouteTime[i] = 0f;
                RouteN[i] = 0;
            }
            for (int i = 0; i < GhId.Length; i++)
            {
                GhId[i] = null;
                GhN[i] = 0;
            }
        }

        static void ClearBoard(int slot)
        {
            int b = slot * PracticeBests.Slots;
            for (int s = 0; s < PracticeBests.Slots; s++)
            {
                RouteId[b + s] = null;
                RouteTime[b + s] = 0f;
                RouteN[b + s] = 0;
            }
            int g = slot * PracticeGhost.Slots;
            for (int s = 0; s < PracticeGhost.Slots; s++)
            {
                GhId[g + s] = null;
                GhN[g + s] = 0;
            }
        }

        static int RouteSlot(int profile, string route, bool claim)
        {
            if (string.IsNullOrEmpty(route)) return -1;
            string key = Canon(route);
            int b = profile * PracticeBests.Slots;
            int free = -1;
            for (int s = 0; s < PracticeBests.Slots; s++)
            {
                if (RouteId[b + s] == key) return b + s;
                if (free < 0 && string.IsNullOrEmpty(RouteId[b + s])) free = b + s;
            }
            if (!claim || free < 0) return -1;
            RouteId[free] = key;
            return free;
        }

        static void SetRouteTime(int profile, string route, float time)
        {
            if (time <= 0f) return;
            int at = RouteSlot(profile, route, true);
            if (at < 0) return;
            RouteTime[at] = time;
        }

        static void SetRouteSplits(int profile, string route, string text)
        {
            int at = RouteSlot(profile, route, true);
            if (at < 0 || string.IsNullOrEmpty(text)) return;
            int local = at - profile * PracticeBests.Slots;
            int o = (profile * PracticeBests.Slots + local) * PracticeBests.Splits;
            int n = 0;
            int i = 0;
            while (i < text.Length && n < PracticeBests.Splits)
            {
                int comma = text.IndexOf(',', i);
                if (comma < 0) comma = text.Length;
                string part = text.Substring(i, comma - i).Trim();
                if (part.Length > 0 && float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float v)
                    && !float.IsNaN(v) && !float.IsInfinity(v))
                {
                    RouteSp[o + n] = v;
                    n++;
                }
                i = comma + 1;
            }
            RouteN[at] = n;
        }

        static int GhostSlot(int profile, string route, bool claim)
        {
            if (string.IsNullOrEmpty(route)) return -1;
            string key = Canon(route);
            int b = profile * PracticeGhost.Slots;
            int free = -1;
            for (int s = 0; s < PracticeGhost.Slots; s++)
            {
                if (GhId[b + s] == key) return b + s;
                if (free < 0 && string.IsNullOrEmpty(GhId[b + s])) free = b + s;
            }
            if (!claim || free < 0) return -1;
            GhId[free] = key;
            return free;
        }

        static string Canon(string route)
        {
            if (string.IsNullOrEmpty(route)) return route;
            int slash = route.IndexOf('/');
            if (slash > 0 && slash < 3) return route;
            int arena = ArenaOf(RouteOf(route));
            return arena.ToString(CultureInfo.InvariantCulture) + "/" + RouteOf(route);
        }

        static string RouteOf(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            int slash = key.IndexOf('/');
            if (slash >= 0 && slash + 1 < key.Length) return key.Substring(slash + 1);
            return key;
        }

        static int ArenaOf(string route)
        {
            if (string.IsNullOrEmpty(route)) return 0;
            PracticeCatalog.Load();
            PracticeRoute found = PracticeCatalog.ById(route);
            if (found != null)
            {
                if (found.Arena == "Pocket Park") return ParkArena.Pocket;
                if (found.Arena == "Stack Yard") return ParkArena.Stack;
                return ParkArena.Mega;
            }
            if (route.StartsWith("pocket", StringComparison.Ordinal)) return ParkArena.Pocket;
            if (route.StartsWith("stack", StringComparison.Ordinal)) return ParkArena.Stack;
            return ParkArena.Mega;
        }

        static bool CheckRoutes(Report report)
        {
            Clear();
            CouchPlay.Release();
            PracticeBests.Clear();
            PracticeGhost.ClearSaved();
            int id = Create("Rio");
            if (!Use(id))
            {
                report.Fail("route profile");
                return false;
            }
            string[] routes =
            {
                "mega-beginner", "mega-wall", "mega-toy",
                "pocket-beginner", "pocket-toy",
                "stack-beginner", "stack-toy",
            };
            int[] arenas = { 0, 0, 0, 1, 1, 2, 2 };
            for (int i = 0; i < routes.Length; i++)
            {
                PracticeBests.SetTime(routes[i], 4f + i);
                PracticeGhost.Read(routes[i], "2;1,2,3,0,1;4,5,6,0.5,2");
            }
            var text = new StringBuilder(256);
            Write(text);
            string blob = text.ToString();
            for (int i = 0; i < routes.Length; i++)
            {
                string key = arenas[i].ToString(CultureInfo.InvariantCulture) + "/" + routes[i];
                if (Math.Abs(PbTime(id, routes[i]) - (4f + i)) > 0.001f || Math.Abs(PbTime(id, key) - (4f + i)) > 0.001f)
                {
                    report.Fail("route key " + key);
                    return false;
                }
                if (GhostSamples(id, key) < 2)
                {
                    report.Fail("route ghost " + key);
                    return false;
                }
                if (blob.IndexOf(".pb." + key + "=", StringComparison.Ordinal) < 0)
                {
                    report.Fail("route saved " + key);
                    return false;
                }
            }
            PracticeBests.Clear();
            PracticeGhost.ClearSaved();
            if (!Use(id))
            {
                report.Fail("route restore");
                return false;
            }
            for (int i = 0; i < routes.Length; i++)
            {
                if (Math.Abs(PracticeBests.TimeOf(routes[i]) - (4f + i)) > 0.001f)
                {
                    report.Fail("route live " + routes[i]);
                    return false;
                }
                if (!PracticeGhost.Load(routes[i]) || PracticeGhost.Count < 2)
                {
                    report.Fail("route ghost live " + routes[i]);
                    return false;
                }
            }
            return true;
        }

        static void TakeGhost(int profile, string route, string value)
        {
            if (!PracticeGhost.Read(route, value)) return;
            int n = PracticeGhost.CopyRoute(route, ScratchX, ScratchY, ScratchZ, ScratchYaw, ScratchPose);
            if (n < 1) return;
            int at = GhostSlot(profile, route, true);
            if (at < 0) return;
            if (n > PracticeGhost.Cap) n = PracticeGhost.Cap;
            GhN[at] = n;
            int local = at - profile * PracticeGhost.Slots;
            int o = (profile * PracticeGhost.Slots + local) * PracticeGhost.Cap;
            for (int i = 0; i < n; i++)
            {
                GhX[o + i] = ScratchX[i];
                GhY[o + i] = ScratchY[i];
                GhZ[o + i] = ScratchZ[i];
                GhYaw[o + i] = ScratchYaw[i];
                GhPose[o + i] = ScratchPose[i];
            }
        }

        static void WriteBinds(StringBuilder text, int slot, string p)
        {
            ActionBinds binds = Binds[slot];
            if (binds == null) return;
            for (int i = 0; i < (int)PlayAction.Count; i++)
            {
                var action = (PlayAction)i;
                text.Append(p);
                text.Append(".kb.");
                text.Append(action.ToString());
                text.Append('=');
                text.Append(binds.Keyboard[i] ?? "");
                text.Append('\n');
                text.Append(p);
                text.Append(".pad.");
                text.Append(action.ToString());
                text.Append('=');
                text.Append(binds.Gamepad[i] ?? "");
                text.Append('\n');
            }
        }

        static void WriteBoard(StringBuilder text, int slot, string p)
        {
            int b = slot * PracticeBests.Slots;
            for (int s = 0; s < PracticeBests.Slots; s++)
            {
                if (string.IsNullOrEmpty(RouteId[b + s]) || RouteTime[b + s] <= 0f) continue;
                text.Append(p);
                text.Append(".pb.");
                text.Append(RouteId[b + s]);
                text.Append('=');
                text.Append(RouteTime[b + s].ToString("0.000", CultureInfo.InvariantCulture));
                text.Append('\n');
                if (RouteN[b + s] < 1) continue;
                text.Append(p);
                text.Append(".sp.");
                text.Append(RouteId[b + s]);
                text.Append('=');
                int o = (b + s) * PracticeBests.Splits;
                for (int i = 0; i < RouteN[b + s]; i++)
                {
                    if (i > 0) text.Append(',');
                    text.Append(RouteSp[o + i].ToString("0.000", CultureInfo.InvariantCulture));
                }
                text.Append('\n');
            }
            int g = slot * PracticeGhost.Slots;
            for (int s = 0; s < PracticeGhost.Slots; s++)
            {
                if (string.IsNullOrEmpty(GhId[g + s]) || GhN[g + s] < 1) continue;
                text.Append(p);
                text.Append(".gh.");
                text.Append(GhId[g + s]);
                text.Append('=');
                text.Append(GhN[g + s].ToString(CultureInfo.InvariantCulture));
                int n = GhN[g + s];
                int o = (g + s) * PracticeGhost.Cap;
                for (int i = 0; i < n; i++)
                {
                    text.Append(';');
                    text.Append(GhX[o + i].ToString("R", CultureInfo.InvariantCulture));
                    text.Append(',');
                    text.Append(GhY[o + i].ToString("R", CultureInfo.InvariantCulture));
                    text.Append(',');
                    text.Append(GhZ[o + i].ToString("R", CultureInfo.InvariantCulture));
                    text.Append(',');
                    text.Append(GhYaw[o + i].ToString("R", CultureInfo.InvariantCulture));
                    text.Append(',');
                    text.Append(GhPose[o + i].ToString(CultureInfo.InvariantCulture));
                }
                text.Append('\n');
            }
        }

        static void ParkBoard()
        {
            if (_active < 0) return;
            StoreLive(_active);
            _active = -1;
            PracticeBests.Clear();
            PracticeGhost.Clear();
            PracticeGhost.ClearSaved();
        }

        static void Remember(int seat)
        {
            if (seat < 0 || seat >= Seats || SnapSet[seat] || GameSettings.Current == null) return;
            SnapPal[seat] = GameSettings.Current.Palette[seat];
            SnapCap[seat] = GameSettings.Current.Captions[seat];
            SnapRum[seat] = GameSettings.Current.Rumble[seat];
            SnapSet[seat] = true;
            if (seat == 0 && !_snapBlindSet)
            {
                _snapBlind = GameSettings.Current.Colorblind;
                _snapBlindSet = true;
            }
        }

        static void RestoreSnap(int seat)
        {
            if (seat < 0 || seat >= Seats || !SnapSet[seat] || GameSettings.Current == null) return;
            GameSettings.Current.Palette[seat] = SnapPal[seat];
            GameSettings.Current.Captions[seat] = SnapCap[seat];
            GameSettings.Current.Rumble[seat] = SnapRum[seat];
            SnapSet[seat] = false;
            if (seat == 0 && _snapBlindSet)
            {
                GameSettings.Current.Colorblind = _snapBlind;
                _snapBlindSet = false;
            }
        }

        static int FillOpt(int seat)
        {
            int n = 0;
            Opt[n++] = None;
            Opt[n++] = Guest;
            for (int i = 0; i < Max; i++)
            {
                if (!Used[i]) continue;
                bool held = false;
                for (int s = 0; s < Seats; s++)
                {
                    if (s != seat && Seat[s] == i) held = true;
                }
                if (!held) Opt[n++] = Id[i];
            }
            return n;
        }

        static int CurrentOpt(int seat)
        {
            if (Seat[seat] == Guest) return Guest;
            if (Seat[seat] >= 0 && Seat[seat] < Max && Used[Seat[seat]]) return Id[Seat[seat]];
            return None;
        }

        static void Focus(int seat, int idx, int n)
        {
            int win = ListWindow;
            if (idx < Scroll[seat]) Scroll[seat] = idx;
            if (idx >= Scroll[seat] + win) Scroll[seat] = idx - win + 1;
            if (Scroll[seat] < 0) Scroll[seat] = 0;
            int max = n - win;
            if (max < 0) max = 0;
            if (Scroll[seat] > max) Scroll[seat] = max;
        }

        static bool AcceptName(string raw, out string name)
        {
            name = "";
            if (string.IsNullOrEmpty(raw)) return false;
            int start = 0;
            while (start < raw.Length && raw[start] == ' ') start++;
            int end = raw.Length;
            while (end > start && raw[end - 1] == ' ') end--;
            int n = 0;
            for (int i = start; i < end && n < NameMax; i++)
            {
                char c = raw[i];
                bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == ' ';
                if (!ok) continue;
                CleanBuf[n++] = c;
            }
            while (n > 0 && CleanBuf[n - 1] == ' ') n--;
            if (n == 0) return false;
            name = new string(CleanBuf, 0, n);
            return true;
        }

        static bool NameTaken(int slot, string name)
        {
            for (int i = 0; i < Max; i++)
            {
                if (i == slot || !Used[i] || string.IsNullOrEmpty(Name[i])) continue;
                if (string.Equals(Name[i], name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static string Clean(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Player";
            int n = 0;
            for (int i = 0; i < raw.Length && n < NameMax; i++)
            {
                char c = raw[i];
                bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == ' ';
                if (ok) CleanBuf[n++] = c;
            }
            if (n == 0) return "Player";
            return new string(CleanBuf, 0, n);
        }

        static bool Seek(char c)
        {
            char look = c;
            if (c >= 'a' && c <= 'z') look = (char)(c - ('a' - 'A'));
            int target = -1;
            for (int i = 0; i < Grid.Length; i++)
            {
                if (Grid[i] == look)
                {
                    target = i;
                    break;
                }
            }
            if (target < 0) return false;
            int tc = target % Cols;
            int tr = target / Cols;
            int guard = 0;
            while ((_col != tc || _row != tr) && guard < 80)
            {
                int dx = 0;
                int dy = 0;
                if (_col < tc) dx = 1;
                else if (_col > tc) dx = -1;
                else if (_row < tr) dy = 1;
                else dy = -1;
                PadMove(dx, dy);
                guard++;
            }
            bool there = _col == tc && _row == tr;
            _typed = there && c != look ? c : '\0';
            return there;
        }

        static void Line(StringBuilder text, string key, int value)
        {
            text.Append(key);
            text.Append('=');
            text.Append(value.ToString(CultureInfo.InvariantCulture));
            text.Append('\n');
        }

        static void Line(StringBuilder text, string key, float value)
        {
            text.Append(key);
            text.Append('=');
            text.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            text.Append('\n');
        }

        static void Line(StringBuilder text, string key, string value)
        {
            text.Append(key);
            text.Append('=');
            text.Append(value ?? "");
            text.Append('\n');
        }

        static int Int(string value, int fallback)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                return n;
            return fallback;
        }

        static float Num(string value, float fallback)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float n)
                && !float.IsNaN(n) && !float.IsInfinity(n))
                return n;
            return fallback;
        }

        static int Clamp(int v, int lo, int hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }

        static float ClampScale(float v)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return GameSettings.HudDefault;
            if (v < GameSettings.HudMin) return GameSettings.HudMin;
            if (v > GameSettings.HudMax) return GameSettings.HudMax;
            return v;
        }
    }
}
