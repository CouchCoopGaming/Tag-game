using System;
using System.Text;
using Tag.Audio;
using Tag.Core;
using Tag.Gameplay;
using Tag.Level;
using Tag.Onboard;
using Tag.Profiles;
using Tag.Settings;
using TagArena.Movement;

namespace Tag.Couch
{
    /// <summary>
    /// Local couch for 2–4 humans. One keyboard and up to four pads join
    /// between match setup and the round. AI from setup fills the seats that
    /// are left, and humans plus AI never pass 4. Each device drives one pawn
    /// through its own ActionBinds table. The keyboard player keeps
    /// ActionBinds.Current. No new verb, and the chase-cam locks stay at 0.
    /// </summary>
    public static class CouchPlay
    {
        public const int Max = 4;
        public const int DeviceKeyboard = 0;
        public const int DevicePad0 = 1;
        public const int DevicePad1 = 2;
        public const int DevicePad2 = 3;
        public const int DevicePad3 = 4;

        public struct View
        {
            public float X, Y, W, H;
            public bool Score;
            public float Right => X + W;
            public float Top => Y + H;
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

        static readonly string[] Names = { "P1", "P2", "P3", "P4" };
        static readonly string[] OpenLine = { "P1  open", "P2  open", "P3  open", "P4  open" };
        static readonly string[] KeyLine = { "P1  keyboard", "P2  keyboard", "P3  keyboard", "P4  keyboard" };
        static readonly string[] PadLine = { "P1  pad", "P2  pad", "P3  pad", "P4  pad" };
        static readonly string[] AiLine = { "P1  ai", "P2  ai", "P3  ai", "P4  ai" };
        static readonly string[] StepLabel =
        {
            "Move", "Sprint", "Jump", "Slide", "Cling", "Wall jump", "Air dash", "Punch / tag"
        };
        static readonly PlayAction[] StepAction =
        {
            PlayAction.Move, PlayAction.Sprint, PlayAction.Jump, PlayAction.Slide,
            PlayAction.Cling, PlayAction.Jump, PlayAction.AirDash, PlayAction.Punch
        };

        static readonly bool[] Human = new bool[Max];
        static readonly bool[] Ai = new bool[Max];
        static readonly int[] Device = new int[Max];
        static readonly float[] PosX = new float[Max];
        static readonly float[] PosZ = new float[Max];
        static readonly float[] TimeAsIt = new float[Max];
        static readonly int[] Tags = new int[Max];
        static readonly PunchStagger.Clock[] Stagger = new PunchStagger.Clock[Max];
        static readonly TagBackImmunity.Window[] Back = new TagBackImmunity.Window[Max];
        static readonly ActionBinds[] Pads = new ActionBinds[Max];
        static readonly string[] Hints = new string[5 * OnboardingSession.StepCount];
        static readonly string[] ClingLine = new string[5];
        static readonly string[] ZipLine = new string[5];
        static readonly string[] DropLine = new string[5];
        static readonly StringBuilder Sb = new StringBuilder(256);

        static readonly bool[] Gone = new bool[Max];
        static readonly string[] ScoreLine = new string[Max];
        static readonly string[] RejoinLine = { "P1 reconnect", "P2 reconnect", "P3 reconnect", "P4 reconnect" };
        static readonly string[] Tagged = new string[Max];
        const string TieLine = "Tie";

        static int _humans;
        static int _ai;
        static int _it = -1;
        static int _hintRev = -1;
        static int _rejoin = -1;
        static int _least = -1;
        static int _leastCount;

        public static int Humans => _humans;
        public static int AiCount => _ai;
        public static int Total => _humans + _ai;

        public static int Leftovers
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Max; i++)
                {
                    if (Human[i] || Ai[i]) n++;
                    if (PosX[i] != 0f || PosZ[i] != 0f) n++;
                    if (Stagger[i].Stagger > 0f || Stagger[i].Immune > 0f) n++;
                    if (Back[i].Remaining > 0f) n++;
                }
                if (_it >= 0) n++;
                return n;
            }
        }

        public static void Release()
        {
            for (int i = 0; i < Max; i++)
            {
                Human[i] = false;
                Ai[i] = false;
                Device[i] = -1;
                PosX[i] = 0f;
                PosZ[i] = 0f;
                TimeAsIt[i] = 0f;
                Tags[i] = 0;
                Stagger[i] = default;
                Back[i] = default;
                Pads[i] = null;
                Gone[i] = false;
                ScoreLine[i] = "";
                Tagged[i] = null;
            }
            LocalProfiles.ClearSeats();
            _humans = 0;
            _ai = 0;
            _it = -1;
            _hintRev = -1;
            _rejoin = -1;
            _least = -1;
            _leastCount = 0;
        }

        public static bool HumanAt(int slot)
        {
            return slot >= 0 && slot < Max && Human[slot];
        }

        public static bool AiAt(int slot)
        {
            return slot >= 0 && slot < Max && Ai[slot];
        }

        public static bool Joined(int device)
        {
            return SlotOf(device) >= 0;
        }

        public static int DeviceOf(int slot)
        {
            if (slot < 0 || slot >= Max) return -1;
            return Device[slot];
        }

        public static string Name(int slot)
        {
            string named = LocalProfiles.SeatName(slot);
            if (!string.IsNullOrEmpty(named)) return named;
            if (slot < 0 || slot >= Names.Length) return "";
            return Names[slot];
        }

        public static bool MayQueue(int device)
        {
            int slot = SlotOf(device);
            return slot >= 0 && Human[slot];
        }

        /// <summary>
        /// A seated human's pause may queue the next arena. The live round does not move.
        /// An empty device or an AI seat cannot.
        /// </summary>
        public static bool QueueNext(int device, int liveArena, ref int pending)
        {
            if (!MayQueue(device)) return false;
            if (liveArena < 0 || liveArena >= ParkArena.Count) liveArena = ParkArena.Mega;
            int shown = pending >= 0 ? pending : liveArena;
            int next = (shown + 1) % ParkArena.Count;
            pending = next == liveArena ? -1 : next;
            return true;
        }

        public static string SeatLine(int slot)
        {
            if (slot < 0 || slot >= Max) return "";
            if (Tagged[slot] != null) return Tagged[slot];
            return Core(slot);
        }

        public static void RefreshTags()
        {
            for (int i = 0; i < Max; i++)
                RefreshTag(i);
        }

        static string Core(int slot)
        {
            if (Human[slot])
                return Device[slot] == DeviceKeyboard ? KeyLine[slot] : PadLine[slot];
            if (Ai[slot]) return AiLine[slot];
            return OpenLine[slot];
        }

        static void RefreshTag(int slot)
        {
            if (slot < 0 || slot >= Max) return;
            string named = LocalProfiles.SeatName(slot);
            if (string.IsNullOrEmpty(named))
            {
                Tagged[slot] = null;
                return;
            }
            Tagged[slot] = Core(slot) + "  " + named;
        }

        public static bool AssignProfile(int slot, int id)
        {
            if (!LocalProfiles.TrySeat(slot, id)) return false;
            RefreshTag(slot);
            return true;
        }

        public static void CycleProfile(int slot, int dir)
        {
            if (slot < 0 || slot >= Max) return;
            int pick = LocalProfiles.Cycle(slot, dir);
            if (pick == LocalProfiles.Guest) LocalProfiles.SeatGuest(slot);
            else if (pick == LocalProfiles.None) LocalProfiles.ClearSeat(slot);
            else if (!LocalProfiles.TrySeat(slot, pick)) return;
            RefreshTag(slot);
        }

        public static void Tint(int slot, out float r, out float g, out float b)
        {
            int i = slot;
            if (i < 0) i = 0;
            if (i > 3) i = 3;
            int pal = 0;
            if (GameSettings.Current != null) pal = GameSettings.Current.PaletteOf(i);
            int swatch = LocalProfiles.SeatColor(i);
            if (swatch < 0) swatch = i;
            AccessibilityPalette.Player(pal, swatch, out r, out g, out b);
        }

        public static bool Join(int device)
        {
            if (device < DeviceKeyboard || device > DevicePad3) return false;
            if (SlotOf(device) >= 0) return false;
            int free = FirstEmpty();
            if (free < 0) return false;
            Human[free] = true;
            Ai[free] = false;
            Device[free] = device;
            _humans++;
            RefreshTag(free);
            return true;
        }

        public static bool NeedsRejoin => _rejoin >= 0;
        public static int RejoinSlot => _rejoin;
        public static string RejoinPrompt => _rejoin >= 0 ? RejoinLine[_rejoin] : "";
        public static string TieText => _leastCount > 1 ? TieLine : "";
        public static int LeastSlot => _least;

        public static bool InputBlocked(int slot)
        {
            return slot >= 0 && slot < Max && Gone[slot];
        }

        public static bool InputBlockedDevice(int device)
        {
            return InputBlocked(SlotOf(device));
        }

        /// <summary>
        /// The pad left mid-round. The seat and its pawn stay. Play pauses and
        /// asks that seat to reconnect. Motors on that seat go quiet.
        /// </summary>
        public static void NoteLost(int device)
        {
            int slot = SlotOf(device);
            if (slot < 0 || Gone[slot]) return;
            Gone[slot] = true;
            if (_rejoin < 0) _rejoin = slot;
            PadRumble.SilenceSeat(slot);
        }

        public static void NoteFound(int device)
        {
            int slot = SlotOf(device);
            if (slot < 0 || !Gone[slot]) return;
            Gone[slot] = false;
            _rejoin = -1;
            for (int i = 0; i < Max; i++)
            {
                if (!Gone[i]) continue;
                _rejoin = i;
                break;
            }
        }

        /// <summary>Round restart drops stagger, tag-back, and the sim positions. The roster stays.</summary>
        public static void ClearResidue()
        {
            for (int i = 0; i < Max; i++)
            {
                PosX[i] = 0f;
                PosZ[i] = 0f;
                TimeAsIt[i] = 0f;
                Tags[i] = 0;
                Stagger[i] = default;
                Back[i] = default;
                ScoreLine[i] = "";
            }
            _it = -1;
            _least = -1;
            _leastCount = 0;
        }

        public static void OpenPauseFrom(int device)
        {
            int slot = SlotOf(device);
            if (slot < 0 || GameSettings.Current == null) return;
            GameSettings.Current.AccessSeat = slot;
        }

        /// <summary>Keyboard shares the keyboard. Two different pads do not.</summary>
        public static bool DrivesOverlap(int a, int b)
        {
            if (a <= 0 && b <= 0) return true;
            return a > 0 && a == b;
        }

        public static bool SharedKey(int deviceA, int deviceB, string keyA, string keyB)
        {
            if (!DrivesOverlap(deviceA, deviceB)) return false;
            if (string.IsNullOrEmpty(keyA) || string.IsNullOrEmpty(keyB)) return false;
            return string.Equals(keyA, keyB, StringComparison.Ordinal);
        }

        public static string ScoreText(int slot)
        {
            if (slot < 0 || slot >= Max) return "";
            return ScoreLine[slot] ?? "";
        }

        public static bool Leave(int device)
        {
            int slot = SlotOf(device);
            if (slot < 0) return false;
            Human[slot] = false;
            Device[slot] = -1;
            PosX[slot] = 0f;
            PosZ[slot] = 0f;
            LocalProfiles.ClearSeat(slot);
            Tagged[slot] = null;
            _humans--;
            if (_humans < 0) _humans = 0;
            return true;
        }

        /// <summary>AI from setup take empty seats. The sum with humans stays at or under 4.</summary>
        public static int FillAi(int requested)
        {
            for (int i = 0; i < Max; i++)
            {
                if (!Ai[i]) continue;
                Ai[i] = false;
                _ai--;
            }
            if (_ai < 0) _ai = 0;
            if (requested < 0) requested = 0;
            int room = Max - _humans;
            if (room < 0) room = 0;
            if (requested > room) requested = room;
            int placed = 0;
            for (int i = 0; i < Max && placed < requested; i++)
            {
                if (Human[i] || Ai[i]) continue;
                Ai[i] = true;
                Device[i] = -1;
                placed++;
            }
            _ai = placed;
            return placed;
        }

        public static ActionBinds BindsFor(int device)
        {
            int seated = SlotOf(device);
            if (seated >= 0)
            {
                ActionBinds owned = LocalProfiles.BindsForSeat(seated);
                if (owned != null) return owned;
            }
            if (device == DeviceKeyboard)
            {
                if (ActionBinds.Current == null)
                    ActionBinds.Current = ActionBinds.Defaults();
                return ActionBinds.Current;
            }
            int pad = device - DevicePad0;
            if (pad < 0 || pad >= Pads.Length)
                return ActionBinds.Current ?? ActionBinds.Defaults();
            if (Pads[pad] == null)
                Pads[pad] = ActionBinds.Defaults();
            return Pads[pad];
        }

        public static InputDeviceKind KindOf(int device)
        {
            return device == DeviceKeyboard ? InputDeviceKind.Keyboard : InputDeviceKind.Gamepad;
        }

        public static string Hint(int device, int step)
        {
            EnsureHints();
            if (device < 0 || device > DevicePad3) device = DeviceKeyboard;
            if (step < 0 || step >= OnboardingSession.StepCount) return "";
            return Hints[device * OnboardingSession.StepCount + step] ?? "";
        }

        public static string ContextLine(int device, ContextKind kind)
        {
            EnsureHints();
            if (device < 0 || device > DevicePad3) device = DeviceKeyboard;
            if (kind == ContextKind.Cling) return ClingLine[device] ?? "";
            if (kind == ContextKind.ZipGrab) return ZipLine[device] ?? "";
            if (kind == ContextKind.ZipDrop) return DropLine[device] ?? "";
            return "";
        }

        /// <summary>A device moves only the pawn that joined on it.</summary>
        public static void ApplyMove(int device, float dx, float dz)
        {
            int slot = SlotOf(device);
            if (slot < 0) return;
            PosX[slot] += dx;
            PosZ[slot] += dz;
        }

        public static float X(int slot)
        {
            if (slot < 0 || slot >= Max) return 0f;
            return PosX[slot];
        }

        public static float Z(int slot)
        {
            if (slot < 0 || slot >= Max) return 0f;
            return PosZ[slot];
        }

        public static int Panes(int humans)
        {
            if (humans <= 1) return 1;
            if (humans == 2) return 2;
            return 4;
        }

        /// <summary>Pixel rect, origin at the bottom-left, matching a camera rect.</summary>
        public static View Pane(int index, int humans, float sw, float sh, int split)
        {
            if (humans < 1) humans = 1;
            if (index < 0) index = 0;
            if (sw < 1f) sw = 1f;
            if (sh < 1f) sh = 1f;
            if (humans <= 1)
                return new View { X = 0f, Y = 0f, W = sw, H = sh };
            if (humans == 2)
            {
                if (split == GameSettings.SplitHorizontal)
                {
                    float half = sh * 0.5f;
                    if (index <= 0) return new View { X = 0f, Y = half, W = sw, H = half };
                    return new View { X = 0f, Y = 0f, W = sw, H = half };
                }
                float side = sw * 0.5f;
                if (index <= 0) return new View { X = 0f, Y = 0f, W = side, H = sh };
                return new View { X = side, Y = 0f, W = side, H = sh };
            }
            float qw = sw * 0.5f;
            float qh = sh * 0.5f;
            int col = index % 2;
            int row = index < 2 ? 1 : 0;
            return new View
            {
                X = col * qw,
                Y = row * qh,
                W = qw,
                H = qh,
                Score = humans == 3 && index == 3
            };
        }

        public static void Norm(int index, int humans, int split, out float x, out float y, out float w, out float h)
        {
            View v = Pane(index, humans, 1f, 1f, split);
            x = v.X;
            y = v.Y;
            w = v.W;
            h = v.H;
        }

        public static bool LayoutHolds(float sw, float sh, int humans, int split)
        {
            int n = Panes(humans);
            float area = 0f;
            for (int i = 0; i < n; i++)
            {
                View a = Pane(i, humans, sw, sh, split);
                if (a.W <= 0f || a.H <= 0f) return false;
                if (a.X < -0.01f || a.Y < -0.01f) return false;
                if (a.Right > sw + 0.01f || a.Top > sh + 0.01f) return false;
                area += a.W * a.H;
                for (int j = i + 1; j < n; j++)
                {
                    View b = Pane(j, humans, sw, sh, split);
                    if (Overlap(a.X, a.Y, a.W, a.H, b.X, b.Y, b.W, b.H)) return false;
                }
            }
            if (Math.Abs(area - sw * sh) > 1f) return false;
            if (humans == 3 && !Pane(3, humans, sw, sh, split).Score) return false;
            if (humans != 3 && Pane(0, humans, sw, sh, split).Score) return false;
            return true;
        }

        /// <summary>
        /// Verb cluster, name / It chip, and timer sit inside each camera pane
        /// and miss each other. The fourth pane of a 3-human layout is the score.
        /// </summary>
        public static bool HudClear(float sw, float sh, int humans, int split)
        {
            if (!LayoutHolds(sw, sh, humans, split)) return false;
            int n = Panes(humans);
            for (int i = 0; i < n; i++)
            {
                View cam = Pane(i, humans, sw, sh, split);
                float x = cam.X;
                float y = sh - (cam.Y + cam.H);
                float w = cam.W;
                float h = cam.H;
                if (cam.Score)
                {
                    float sx = x + 12f;
                    float sy = y + 12f;
                    float swid = w - 24f;
                    float shgt = h - 24f;
                    if (swid < 40f || shgt < 40f) return false;
                    if (!Contains(x, y, w, h, sx, sy, swid, shgt)) return false;
                    continue;
                }
                Box(w, out float tw, out float th, out float cw, out float ch, out float nw, out float nh, out float iw, out float ih);
                float tx = x + (w - tw) * 0.5f;
                float ty = y + 6f;
                float nx = x + 8f;
                float ny = y + 6f;
                float ix = x + 8f;
                float iy = y + 36f;
                float cx = x + w - cw - 8f;
                float cy = y + 36f;
                if (!Contains(x, y, w, h, tx, ty, tw, th)) return false;
                if (!Contains(x, y, w, h, nx, ny, nw, nh)) return false;
                if (!Contains(x, y, w, h, ix, iy, iw, ih)) return false;
                if (!Contains(x, y, w, h, cx, cy, cw, ch)) return false;
                if (Overlap(tx, ty, tw, th, nx, ny, nw, nh)) return false;
                if (Overlap(tx, ty, tw, th, ix, iy, iw, ih)) return false;
                if (Overlap(tx, ty, tw, th, cx, cy, cw, ch)) return false;
                if (Overlap(nx, ny, nw, nh, ix, iy, iw, ih)) return false;
                if (Overlap(nx, ny, nw, nh, cx, cy, cw, ch)) return false;
                if (Overlap(ix, iy, iw, ih, cx, cy, cw, ch)) return false;
            }
            return true;
        }

        public static void HudBox(float paneX, float paneY, float paneW, float paneH, int which, out float x, out float y, out float w, out float h)
        {
            Box(paneW, out float tw, out float th, out float cw, out float ch, out float nw, out float nh, out float iw, out float ih);
            if (which == 1)
            {
                x = paneX + 8f;
                y = paneY + 6f;
                w = nw;
                h = nh;
                return;
            }
            if (which == 2)
            {
                x = paneX + 8f;
                y = paneY + 36f;
                w = iw;
                h = ih;
                return;
            }
            if (which == 3)
            {
                x = paneX + paneW - cw - 8f;
                y = paneY + 36f;
                w = cw;
                h = ch;
                return;
            }
            x = paneX + (paneW - tw) * 0.5f;
            y = paneY + 6f;
            w = tw;
            h = th;
        }

        public static bool TryTag(int from, int to)
        {
            if (from == to || from < 0 || to < 0 || from >= Max || to >= Max) return false;
            if (!Occupied(from) || !Occupied(to)) return false;
            if (_it < 0) _it = from;
            if (_it != from) return false;
            if (TagBackImmunity.Blocks(Back[to], Id(from))) return false;
            if (from == to) return false;
            Back[from] = TagBackImmunity.Open(Id(to), TagBackImmunity.DefaultSeconds);
            _it = to;
            Tags[from]++;
            return true;
        }

        public static void TickTag(float dt)
        {
            for (int i = 0; i < Max; i++)
            {
                Back[i] = TagBackImmunity.Tick(Back[i], dt);
                if (Occupied(i) && _it == i) TimeAsIt[i] += dt < 0f ? 0f : dt;
            }
            RebuildScores();
        }

        public static void NoteScores(int slot, float time, int tags)
        {
            if (slot < 0 || slot >= Max) return;
            if (time < 0f) time = 0f;
            if (tags < 0) tags = 0;
            TimeAsIt[slot] = time;
            Tags[slot] = tags;
            RebuildScores();
        }

        public static bool TryStagger(int victim)
        {
            if (victim < 0 || victim >= Max || !Occupied(victim)) return false;
            return PunchStagger.TryStart(ref Stagger[victim]);
        }

        public static void TickStagger(int victim, float dt)
        {
            if (victim < 0 || victim >= Max) return;
            PunchStagger.Tick(ref Stagger[victim], dt);
        }

        public static float StaggerOf(int slot)
        {
            if (slot < 0 || slot >= Max) return 0f;
            return Stagger[slot].Stagger;
        }

        public static float ImmuneOf(int slot)
        {
            if (slot < 0 || slot >= Max) return 0f;
            return Stagger[slot].Immune;
        }

        public static void ListenPoint(int mode, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            int n = 0;
            float sx = 0f;
            float sz = 0f;
            int first = -1;
            for (int i = 0; i < Max; i++)
            {
                if (!Human[i]) continue;
                if (first < 0) first = i;
                sx += PosX[i];
                sz += PosZ[i];
                n++;
            }
            if (n <= 0) return;
            if (mode == GameSettings.ListenAverage && n > 1)
            {
                x = sx / n;
                z = sz / n;
                return;
            }
            x = PosX[first];
            z = PosZ[first];
        }

        public static string ResultsText()
        {
            Sb.Clear();
            bool any = false;
            for (int i = 0; i < Max; i++)
            {
                if (!Human[i] && !Ai[i]) continue;
                if (any) Sb.Append(" | ");
                any = true;
                Sb.Append(Names[i]);
                Sb.Append(' ');
                Sb.Append(Human[i] ? "human" : "ai");
                Sb.Append(' ');
                Sb.Append(HudDigits.Tenth0(TimeAsIt[i]));
                Sb.Append("s tags ");
                Sb.Append(HudDigits.Whole0(Tags[i]));
            }
            return Sb.ToString();
        }

        public static Report Run()
        {
            var report = new Report();
            Release();
            if (ActionBinds.Current == null)
                ActionBinds.Current = ActionBinds.Defaults();
            string jump = ActionBinds.Current.Gamepad[(int)PlayAction.Jump];

            if (!Join(DeviceKeyboard) || !Join(DevicePad0) || !Join(DevicePad1) || !Join(DevicePad2))
                report.Fail("four humans did not join");
            if (Join(DevicePad3) || _humans != 4)
                report.Fail("a fifth human took a seat");
            if (!Leave(DevicePad1) || _humans != 3 || Joined(DevicePad1))
                report.Fail("back did not free a seat");
            if (!Join(DevicePad3) || !Joined(DevicePad3) || _humans != 4)
                report.Fail("a freed seat did not take the next pad");
            if (!Leave(DevicePad2) || !Leave(DevicePad3) || _humans != 2)
                report.Fail("leave did not return to two humans");
            if (FillAi(3) != 2 || _humans + _ai != 4)
                report.Fail("ai did not stop at the empty seats");
            if (FillAi(1) != 1 || _humans + _ai != 3)
                report.Fail("ai count from setup did not fill one remaining seat");
            if (_humans + _ai > Max)
                report.Fail("humans plus ai passed 4");

            if (!LayoutHolds(1920f, 1080f, 1, GameSettings.SplitVertical)
                || !LayoutHolds(1280f, 720f, 1, GameSettings.SplitVertical))
                report.Fail("one player was not full screen");
            if (!LayoutHolds(1920f, 1080f, 2, GameSettings.SplitVertical)
                || !LayoutHolds(1280f, 720f, 2, GameSettings.SplitHorizontal)
                || !LayoutHolds(1920f, 1080f, 2, GameSettings.SplitHorizontal)
                || !LayoutHolds(1280f, 720f, 2, GameSettings.SplitVertical))
                report.Fail("two player split rects overlapped or left a gap");
            if (!LayoutHolds(1920f, 1080f, 3, 0) || !LayoutHolds(1280f, 720f, 4, 0))
                report.Fail("quadrant rects overlapped or left a gap");

            View left = Pane(0, 2, 1920f, 1080f, GameSettings.SplitVertical);
            View right = Pane(1, 2, 1920f, 1080f, GameSettings.SplitVertical);
            View top = Pane(0, 2, 1280f, 720f, GameSettings.SplitHorizontal);
            View low = Pane(1, 2, 1280f, 720f, GameSettings.SplitHorizontal);
            View full = Pane(0, 1, 1920f, 1080f, 0);
            View quad = Pane(3, 4, 1920f, 1080f, 0);
            View score = Pane(3, 3, 1280f, 720f, 0);
            if (full.W != 1920f || full.H != 1080f)
                report.Fail("full screen rect drifted");
            if (left.W != 960f || right.X != 960f || top.H != 360f || low.Y != 0f)
                report.Fail("split rects were not half the screen");
            if (quad.W != 960f || quad.H != 540f || !score.Score)
                report.Fail("quadrant or the 3-player score pane drifted");

            if (!HudClear(1920f, 1080f, 1, 0) || !HudClear(1280f, 720f, 1, 0)
                || !HudClear(1920f, 1080f, 2, GameSettings.SplitVertical)
                || !HudClear(1280f, 720f, 2, GameSettings.SplitHorizontal)
                || !HudClear(1920f, 1080f, 3, 0) || !HudClear(1280f, 720f, 3, 0)
                || !HudClear(1920f, 1080f, 4, 0) || !HudClear(1280f, 720f, 4, 0))
                report.Fail("hud overlapped inside a viewport");

            float p1 = X(0);
            ApplyMove(DevicePad0, 4f, 0f);
            if (X(0) != p1 || X(1) != 4f)
                report.Fail("p2 input moved p1");
            ApplyMove(DeviceKeyboard, 1f, 2f);
            if (X(1) != 4f || X(0) != p1 + 1f || Z(0) != 2f)
                report.Fail("keyboard input moved another pawn");

            ActionBinds pad = BindsFor(DevicePad0);
            pad.SetGamepad(PlayAction.Jump, "buttonNorth");
            if (ActionBinds.Current.Gamepad[(int)PlayAction.Jump] != jump)
                report.Fail("a pad rebind wrote the keyboard table");
            if (BindsFor(DevicePad1).Gamepad[(int)PlayAction.Jump] != "buttonSouth")
                report.Fail("a pad rebind wrote another pad");
            if (ControlGlyphs.GlyphOf(PlayAction.Jump, BindsFor(DeviceKeyboard), InputDeviceKind.Keyboard) != "Space")
                report.Fail("keyboard glyphs left the current binds");
            if (ControlGlyphs.GlyphOf(PlayAction.Jump, pad, InputDeviceKind.Gamepad) != "North")
                report.Fail("pad glyphs did not follow that pad");
            if (Hint(DevicePad0, (int)HintStep.Jump).IndexOf("North", StringComparison.Ordinal) < 0)
                report.Fail("pad hints did not follow that pad");
            if (Hint(DeviceKeyboard, (int)HintStep.Jump).IndexOf("Space", StringComparison.Ordinal) < 0)
                report.Fail("keyboard hints did not stay on the current binds");

            if (Math.Abs(TagBackImmunity.DefaultSeconds - 1.0f) > 0.001f
                || Math.Abs(PunchStagger.Duration - 0.25f) > 0.001f
                || Math.Abs(PunchStagger.Immunity - 0.50f) > 0.001f)
                report.Fail("tag-back or stagger locks moved");
            if (!TryTag(0, 1))
                report.Fail("tag did not pass to the next pawn");
            if (TryTag(1, 0))
                report.Fail("tag-back was open inside 1s");
            if (!TryTag(1, 2))
                report.Fail("tag-back blocked a different pawn");
            TickTag(1.05f);
            if (!TryTag(2, 0))
                report.Fail("tag-back stayed shut after 1s");
            if (!TryStagger(1) || StaggerOf(0) > 0f)
                report.Fail("stagger on p2 hit p1");
            if (TryStagger(1))
                report.Fail("stagger refreshed during the lock");
            TickStagger(1, 0.25f);
            if (StaggerOf(1) > 0f || ImmuneOf(1) <= 0f)
                report.Fail("stagger did not hand off to the 0.50s immunity");
            if (TryStagger(1))
                report.Fail("stagger fired during immunity");
            TickStagger(1, 0.50f);
            if (!TryStagger(1))
                report.Fail("stagger stayed shut after immunity");

            TimeAsIt[0] = 1.5f;
            TimeAsIt[1] = 2f;
            TimeAsIt[2] = 0.5f;
            string results = ResultsText();
            if (results.IndexOf("P1 human", StringComparison.Ordinal) < 0
                || results.IndexOf("P2 human", StringComparison.Ordinal) < 0
                || results.IndexOf("P3 ai", StringComparison.Ordinal) < 0)
                report.Fail("results missed a human or an ai");
            if (results.IndexOf("P4", StringComparison.Ordinal) >= 0)
                report.Fail("results listed an empty seat");

            PosX[0] = 2f;
            PosZ[0] = 4f;
            PosX[1] = 8f;
            PosZ[1] = 10f;
            ListenPoint(GameSettings.ListenP1, out float lx, out float lz);
            ListenPoint(GameSettings.ListenAverage, out float ax, out float az);
            if (lx != 2f || lz != 4f || Math.Abs(ax - 5f) > 0.001f || Math.Abs(az - 7f) > 0.001f)
                report.Fail("listener was not p1 or the average");
            if (VoiceBudget.Cap != 16)
                report.Fail("voice cap moved off 16");
            if (ChaseCam.FovPop != 0f || ChaseCam.Shake != 0f || ChaseCam.SlowMo != 0f)
                report.Fail("chase cam locks moved");

            Release();
            if (!Join(DeviceKeyboard) || !Join(DevicePad0) || !Join(DevicePad1) || !Join(DevicePad2))
                report.Fail("four humans did not sit");
            if (!ParkArena.HumanSeatsHold(out string seats))
                report.Fail(seats.Length == 0 ? "four human spawns were not equal-arc" : seats);
            int pending = -1;
            if (!QueueNext(DeviceKeyboard, ParkArena.Mega, ref pending) || pending != ParkArena.Pocket)
                report.Fail("p1 pause did not queue the next arena");
            if (!QueueNext(DevicePad0, ParkArena.Mega, ref pending) || pending != ParkArena.Stack)
                report.Fail("p2 pause did not queue the next arena");
            if (!QueueNext(DevicePad1, ParkArena.Mega, ref pending) || pending != -1)
                report.Fail("p3 pause did not clear a queue of the live arena");
            if (!QueueNext(DevicePad2, ParkArena.Mega, ref pending) || pending != ParkArena.Pocket)
                report.Fail("p4 pause did not queue the next arena");
            if (QueueNext(DevicePad3, ParkArena.Mega, ref pending))
                report.Fail("an empty device queued an arena");
            Release();
            if (!Join(DeviceKeyboard) || FillAi(1) != 1)
                report.Fail("ai did not take the open seat");
            if (MayQueue(DevicePad0))
                report.Fail("an ai seat queued an arena");

            Release();
            if (Leftovers != 0 || _humans != 0 || _ai != 0)
                report.Fail("teardown left a seat");

            if (!report.Ok)
                report.Line = "couch FAIL " + report.Failure;
            else
                report.Line = "couch join=ok leave=ok humans+ai<=4"
                    + " viewports=1,2v,2h,3,4 rects=1920x1080+1280x720 hud=clear"
                    + " input=isolated results=humans+ai listener=p1|avg voices=16"
                    + " tagBack=1.00 stagger=0.25/0.50 fovPop=0 shake=0 slowMo=0 leftovers=0"
                    + " spawns=4 equal-arc arenas=3 pause=any-seat";
            return report;
        }

        static void Box(float paneW, out float tw, out float th, out float cw, out float ch, out float nw, out float nh, out float iw, out float ih)
        {
            tw = paneW * 0.36f;
            if (tw > 280f) tw = 280f;
            th = 22f;
            cw = paneW * 0.34f;
            if (cw > 176f) cw = 176f;
            if (cw < 96f) cw = 96f;
            ch = 120f;
            nw = 72f;
            nh = 24f;
            iw = 52f;
            ih = 52f;
        }

        static bool Contains(float px, float py, float pw, float ph, float x, float y, float w, float h)
        {
            return x >= px - 0.01f && y >= py - 0.01f && x + w <= px + pw + 0.01f && y + h <= py + ph + 0.01f;
        }

        static bool Overlap(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
        {
            return ax < bx + bw && bx < ax + aw && ay < by + bh && by < ay + ah;
        }

        static void RebuildScores()
        {
            _least = -1;
            _leastCount = 0;
            float best = 0f;
            for (int i = 0; i < Max; i++)
            {
                if (!Occupied(i))
                {
                    ScoreLine[i] = "";
                    continue;
                }
                Sb.Clear();
                Sb.Append(Name(i));
                Sb.Append(' ');
                Sb.Append(HudDigits.Tenth0(TimeAsIt[i]));
                ScoreLine[i] = Sb.ToString();
                if (_least < 0 || TimeAsIt[i] < best)
                {
                    best = TimeAsIt[i];
                    _least = i;
                    _leastCount = 1;
                }
                else if (TimeAsIt[i] == best)
                    _leastCount++;
            }
        }

        static bool Occupied(int slot)
        {
            return Human[slot] || Ai[slot];
        }

        static int Id(int slot)
        {
            return slot + 1;
        }

        static int SlotOf(int device)
        {
            for (int i = 0; i < Max; i++)
            {
                if (Human[i] && Device[i] == device) return i;
            }
            return -1;
        }

        static int FirstEmpty()
        {
            for (int i = 0; i < Max; i++)
            {
                if (!Human[i] && !Ai[i]) return i;
            }
            return -1;
        }

        static void EnsureHints()
        {
            int rev = 0;
            ActionBinds keys = BindsFor(DeviceKeyboard);
            if (keys != null) rev += keys.Revision;
            for (int i = 0; i < Pads.Length; i++)
            {
                if (Pads[i] != null) rev += Pads[i].Revision * (i + 3);
            }
            if (rev == _hintRev && Hints[0] != null) return;
            _hintRev = rev;
            for (int d = 0; d <= DevicePad3; d++)
            {
                ActionBinds binds = BindsFor(d);
                InputDeviceKind kind = KindOf(d);
                for (int s = 0; s < OnboardingSession.StepCount; s++)
                {
                    string glyph = ControlGlyphs.GlyphOf(StepAction[s], binds, kind);
                    Hints[d * OnboardingSession.StepCount + s] = StepLabel[s] + "  [" + glyph + "]";
                }
                string cling = ControlGlyphs.GlyphOf(PlayAction.Cling, binds, kind);
                string jump = ControlGlyphs.GlyphOf(PlayAction.Jump, binds, kind);
                ClingLine[d] = "Hold [" + cling + "]";
                ZipLine[d] = "Hold [" + cling + "] to grab";
                DropLine[d] = "Jump [" + jump + "] to drop";
            }
        }
    }
}
