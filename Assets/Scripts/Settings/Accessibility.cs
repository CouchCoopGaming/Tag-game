using System;

namespace Tag.Settings
{
    /// <summary>
    /// Colorblind-safe palettes, caption cues, and gamepad rumble.
    /// Presentation only. Coyote, jump, cling, and the other feel locks
    /// are not stored here. Cling stays a hold.
    /// </summary>
    public static class AccessibilityPalette
    {
        public const int Count = 5;
        public const int Default = 0;
        public const int Deuteranopia = 1;
        public const int Protanopia = 2;
        public const int Tritanopia = 3;
        public const int HighContrast = 4;

        public const int Players = 4;
        public const int Verbs = 4;
        public const float MinPairDistance = 0.35f;
        public const float MinContrast = 3f;

        public const int CvdDeuteranopia = 0;
        public const int CvdProtanopia = 1;
        public const int CvdTritanopia = 2;
        public const int CvdCount = 3;

        /// <summary>Ground albedos: mulch, sand, grass, concrete.</summary>
        public const int Grounds = 4;

        static readonly string[] Names =
        {
            "Default", "Deuteranopia", "Protanopia", "Tritanopia", "High contrast"
        };

        public static readonly string[] PlayerGlyph = { "●", "■", "▲", "◆" };
        public const string ItGlyph = "★";

        // Four light player colors per palette. Pairwise distance stays above
        // MinPairDistance after deuteranopia, protanopia, and tritanopia
        // simulation, and each clears MinContrast against the ground albedos.
        static readonly float[] PlayerR =
        {
            0.78f, 0.57f, 1.00f, 0.00f,
            0.98f, 0.96f, 0.67f, 0.00f,
            0.95f, 0.57f, 1.00f, 0.01f,
            0.64f, 1.00f, 0.62f, 0.00f,
            0.99f, 0.58f, 1.00f, 0.00f
        };
        static readonly float[] PlayerG =
        {
            0.78f, 0.99f, 1.00f, 0.84f,
            0.69f, 1.00f, 1.00f, 0.85f,
            0.79f, 0.81f, 1.00f, 0.81f,
            0.99f, 0.67f, 1.00f, 0.84f,
            0.73f, 0.99f, 1.00f, 0.83f
        };
        static readonly float[] PlayerB =
        {
            0.00f, 0.45f, 1.00f, 0.75f,
            0.00f, 0.38f, 1.00f, 0.67f,
            0.04f, 0.47f, 0.82f, 1.00f,
            0.00f, 0.39f, 1.00f, 0.76f,
            0.00f, 0.53f, 1.00f, 0.81f
        };

        // Verb HUD states follow the same four swatches so dash, safe,
        // stagger, and cling stay separable under the same simulations.
        static readonly float[] ItR = { 1.00f, 1.00f, 0.85f, 1.00f, 1.00f };
        static readonly float[] ItG = { 0.95f, 1.00f, 1.00f, 0.85f, 1.00f };
        static readonly float[] ItB = { 0.20f, 0.55f, 0.15f, 0.00f, 0.20f };

        static readonly float[] GlowR = { 0.20f, 0.55f, 0.15f, 1.00f, 0.20f };
        static readonly float[] GlowG = { 1.00f, 1.00f, 1.00f, 1.00f, 1.00f };
        static readonly float[] GlowB = { 0.92f, 1.00f, 0.75f, 0.55f, 0.92f };

        static readonly float[] GroundR = { 0x3A / 255f, 0x7A / 255f, 0x3A / 255f, 0x6A / 255f };
        static readonly float[] GroundG = { 0x22 / 255f, 0x62 / 255f, 0x64 / 255f, 0x61 / 255f };
        static readonly float[] GroundB = { 0x18 / 255f, 0x40 / 255f, 0x32 / 255f, 0x54 / 255f };

        public static string Name(int palette)
        {
            if (palette < 0 || palette >= Count) return Names[0];
            return Names[palette];
        }

        public static string Glyph(int slot)
        {
            if (slot < 0 || slot >= PlayerGlyph.Length) return PlayerGlyph[0];
            return PlayerGlyph[slot];
        }

        public static void Player(int palette, int slot, out float r, out float g, out float b)
        {
            int i = Index(palette, slot);
            r = PlayerR[i];
            g = PlayerG[i];
            b = PlayerB[i];
        }

        public static void Verb(int palette, int index, out float r, out float g, out float b)
        {
            Player(palette, index, out r, out g, out b);
        }

        public static void It(int palette, out float r, out float g, out float b)
        {
            int i = palette;
            if (i < 0 || i >= Count) i = 0;
            r = ItR[i];
            g = ItG[i];
            b = ItB[i];
        }

        /// <summary>Player swatch versus the It marker, under every CVD simulation.</summary>
        public static bool ClearsIt(int palette, int slot)
        {
            Player(palette, slot, out float pr, out float pg, out float pb);
            It(palette, out float ir, out float ig, out float ib);
            return Apart(pr, pg, pb, ir, ig, ib);
        }

        /// <summary>
        /// The It marker. When the seated swatch sits on the default It color,
        /// a cyan fallback is used so the crown stays readable.
        /// </summary>
        public static void ItAgainst(int palette, int slot, out float r, out float g, out float b)
        {
            if (ClearsIt(palette, slot))
            {
                It(palette, out r, out g, out b);
                return;
            }
            Player(palette, slot, out float pr, out float pg, out float pb);
            r = 0f;
            g = 0.9f;
            b = 1f;
            if (Apart(pr, pg, pb, r, g, b)) return;
            r = 0.15f;
            g = 0.55f;
            b = 1f;
        }

        static bool Apart(float ar, float ag, float ab, float br, float bg, float bb)
        {
            for (int cvd = 0; cvd < CvdCount; cvd++)
            {
                Simulate(cvd, ar, ag, ab, out float ar2, out float ag2, out float ab2);
                Simulate(cvd, br, bg, bb, out float br2, out float bg2, out float bb2);
                if (Distance(ar2, ag2, ab2, br2, bg2, bb2) < MinPairDistance) return false;
            }
            return true;
        }

        public static void Glow(int palette, out float r, out float g, out float b)
        {
            int i = palette;
            if (i < 0 || i >= Count) i = 0;
            r = GlowR[i];
            g = GlowG[i];
            b = GlowB[i];
        }

        public static void Ground(int index, out float r, out float g, out float b)
        {
            int i = index;
            if (i < 0 || i >= Grounds) i = 0;
            r = GroundR[i];
            g = GroundG[i];
            b = GroundB[i];
        }

        public static bool Separates()
        {
            for (int p = 0; p < Count; p++)
            {
                for (int cvd = 0; cvd < CvdCount; cvd++)
                {
                    if (MinPlayerDistance(p, cvd) < MinPairDistance)
                        return false;
                }
                for (int s = 0; s < Players; s++)
                {
                    Player(p, s, out float r, out float g, out float b);
                    if (!ClearsGround(r, g, b)) return false;
                }
                It(p, out float ir, out float ig, out float ib);
                Glow(p, out float gr, out float gg, out float gb);
                if (!ClearsGround(ir, ig, ib) || !ClearsGround(gr, gg, gb))
                    return false;
                for (int v = 0; v < Verbs; v++)
                {
                    Verb(p, v, out float vr, out float vg, out float vb);
                    if (!ClearsGround(vr, vg, vb)) return false;
                }
            }
            return true;
        }

        public static float MinPlayerDistance(int palette, int cvd)
        {
            float worst = 99f;
            for (int a = 0; a < Players; a++)
            {
                Player(palette, a, out float ar, out float ag, out float ab);
                Simulate(cvd, ar, ag, ab, out float ar2, out float ag2, out float ab2);
                for (int b = a + 1; b < Players; b++)
                {
                    Player(palette, b, out float br, out float bg, out float bb);
                    Simulate(cvd, br, bg, bb, out float br2, out float bg2, out float bb2);
                    float d = Distance(ar2, ag2, ab2, br2, bg2, bb2);
                    if (d < worst) worst = d;
                }
            }
            return worst;
        }

        public static bool ClearsGround(float r, float g, float b)
        {
            for (int i = 0; i < Grounds; i++)
            {
                Ground(i, out float gr, out float gg, out float gb);
                if (Contrast(r, g, b, gr, gg, gb) < MinContrast)
                    return false;
            }
            return true;
        }

        public static void Simulate(int cvd, float r, float g, float b, out float oR, out float oG, out float oB)
        {
            if (cvd == CvdProtanopia)
            {
                oR = 0.152286f * r + 1.052583f * g - 0.204868f * b;
                oG = 0.114503f * r + 0.786281f * g + 0.099216f * b;
                oB = -0.003882f * r - 0.048116f * g + 1.051998f * b;
            }
            else if (cvd == CvdTritanopia)
            {
                oR = 1.255528f * r - 0.076749f * g - 0.178779f * b;
                oG = -0.078411f * r + 0.930809f * g + 0.147602f * b;
                oB = 0.004733f * r + 0.691367f * g + 0.303900f * b;
            }
            else
            {
                oR = 0.367322f * r + 0.860646f * g - 0.227968f * b;
                oG = 0.280085f * r + 0.672501f * g + 0.047413f * b;
                oB = -0.011820f * r + 0.042940f * g + 0.968881f * b;
            }
            if (oR < 0f) oR = 0f;
            else if (oR > 1f) oR = 1f;
            if (oG < 0f) oG = 0f;
            else if (oG > 1f) oG = 1f;
            if (oB < 0f) oB = 0f;
            else if (oB > 1f) oB = 1f;
        }

        public static float Contrast(float ar, float ag, float ab, float br, float bg, float bb)
        {
            float l1 = Luminance(ar, ag, ab);
            float l2 = Luminance(br, bg, bb);
            float hi = l1 > l2 ? l1 : l2;
            float lo = l1 > l2 ? l2 : l1;
            return (hi + 0.05f) / (lo + 0.05f);
        }

        public static float Luminance(float r, float g, float b)
        {
            return 0.2126f * Linear(r) + 0.7152f * Linear(g) + 0.0722f * Linear(b);
        }

        static float Linear(float c)
        {
            if (c <= 0.04045f) return c / 12.92f;
            float x = (c + 0.055f) / 1.055f;
            return (float)Math.Pow(x, 2.4);
        }

        static float Distance(float ar, float ag, float ab, float br, float bg, float bb)
        {
            float dr = ar - br;
            float dg = ag - bg;
            float db = ab - bb;
            return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        static int Index(int palette, int slot)
        {
            if (palette < 0 || palette >= Count) palette = 0;
            if (slot < 0) slot = 0;
            if (slot >= Players) slot = Players - 1;
            return palette * Players + slot;
        }
    }

    /// <summary>
    /// One live cue per kind. No heap traffic on the note or the draw.
    /// </summary>
    public static class CaptionFeed
    {
        public const int Foot = 0;
        public const int Countdown = 1;
        public const int Tag = 2;
        public const int Pad = 3;
        public const int Zip = 4;
        public const int Count = 5;
        public const float Life = 0.85f;
        public const float Nearby = 18f;

        static readonly string[] Icons = { "▲", "●", "✦", "▸", "◆" };
        static readonly string[] Arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };

        struct Cue
        {
            public float X;
            public float Z;
            public float Left;
        }

        static readonly Cue[] Cues = new Cue[Count];

        public static void Reset()
        {
            for (int i = 0; i < Count; i++)
                Cues[i] = default;
        }

        public static void Footstep(float x, float z) => Note(Foot, x, z);
        public static void CountdownTick(float x, float z) => Note(Countdown, x, z);
        public static void Tagged(float x, float z) => Note(Tag, x, z);
        public static void PadLaunch(float x, float z) => Note(Pad, x, z);
        public static void ZipGrab(float x, float z) => Note(Zip, x, z);

        public static void FromHook(int hook, float x, float z)
        {
            // AudioBus.Hook: Tag 11, PadLaunch 14, ZipGrab 15, CountdownBeep 18.
            if (hook == 11) Note(Tag, x, z);
            else if (hook == 14) Note(Pad, x, z);
            else if (hook == 15) Note(Zip, x, z);
            else if (hook == 18) Note(Countdown, x, z);
        }

        public static void Note(int kind, float x, float z)
        {
            if (kind < 0 || kind >= Count) return;
            Cues[kind].X = x;
            Cues[kind].Z = z;
            Cues[kind].Left = Life;
        }

        public static void Decay(float dt)
        {
            if (dt < 0f) dt = 0f;
            for (int i = 0; i < Count; i++)
            {
                if (Cues[i].Left <= 0f) continue;
                Cues[i].Left -= dt;
                if (Cues[i].Left < 0f) Cues[i].Left = 0f;
            }
        }

        public static bool Live(int kind)
        {
            return kind >= 0 && kind < Count && Cues[kind].Left > 0f;
        }

        public static bool Shows(GameSettings settings, int seat, int kind, float camX, float camZ)
        {
            if (settings == null || !settings.CaptionsOf(seat) || !Live(kind))
                return false;
            if (kind != Foot) return true;
            float dx = Cues[kind].X - camX;
            float dz = Cues[kind].Z - camZ;
            return dx * dx + dz * dz <= Nearby * Nearby;
        }

        public static string Icon(int kind)
        {
            if (kind < 0 || kind >= Count) return Icons[0];
            return Icons[kind];
        }

        public static string Arrow(int dir)
        {
            if (dir < 0 || dir >= Arrows.Length) return Arrows[0];
            return Arrows[dir];
        }

        public static bool HasDirection(int kind)
        {
            return kind == Foot || kind == Tag || kind == Pad || kind == Zip;
        }

        public static int Direction(int kind, float camX, float camZ)
        {
            if (kind < 0 || kind >= Count) return 0;
            return DirectionOf(camX, camZ, Cues[kind].X, Cues[kind].Z);
        }

        public static int DirectionOf(float fromX, float fromZ, float toX, float toZ)
        {
            float dx = toX - fromX;
            float dz = toZ - fromZ;
            if (dx * dx + dz * dz < 0.0001f) return 0;
            double ang = Math.Atan2(dx, dz);
            int oct = (int)Math.Round(ang / (Math.PI / 4.0));
            oct %= 8;
            if (oct < 0) oct += 8;
            return oct;
        }

        public static void CueAt(int kind, out float x, out float z)
        {
            if (kind < 0 || kind >= Count)
            {
                x = 0f;
                z = 0f;
                return;
            }
            x = Cues[kind].X;
            z = Cues[kind].Z;
        }
    }

    /// <summary>
    /// Short motor pulses. Keyboard seats never write a motor.
    /// The arrays are fixed. Tick does not allocate.
    /// </summary>
    public static class PadRumble
    {
        public const int TagClaim = 0;
        public const int Tagged = 1;
        public const int PunchHit = 2;
        public const int Stagger = 3;
        public const int HardLand = 4;
        public const int PadLaunch = 5;
        public const int Events = 6;
        public const int Seats = 4;

        static readonly float[] LowAmp = { 0.85f, 0.95f, 0.55f, 0.70f, 0.80f, 0.60f };
        static readonly float[] HighAmp = { 0.20f, 0.40f, 0.45f, 0.25f, 0.15f, 0.55f };
        static readonly float[] Seconds = { 0.12f, 0.16f, 0.08f, 0.14f, 0.10f, 0.12f };

        static readonly int[] Instance = new int[Seats];
        static readonly int[] Device = new int[Seats];
        static readonly float[] Low = new float[Seats];
        static readonly float[] High = new float[Seats];
        static readonly float[] Left = new float[Seats];

        public static int KeyboardMotors;
        public static int PadMotors;

        public static void Reset()
        {
            for (int i = 0; i < Seats; i++)
            {
                Instance[i] = 0;
                Device[i] = 0;
                Low[i] = 0f;
                High[i] = 0f;
                Left[i] = 0f;
            }
            KeyboardMotors = 0;
            PadMotors = 0;
        }

        public static bool EventsMapped()
        {
            if (LowAmp.Length != Events || HighAmp.Length != Events || Seconds.Length != Events)
                return false;
            for (int i = 0; i < Events; i++)
            {
                if (LowAmp[i] + HighAmp[i] <= 0f) return false;
                if (Seconds[i] <= 0f || Seconds[i] > 0.25f) return false;
            }
            return true;
        }

        public static void Bind(int seat, int instanceId, int device)
        {
            if (seat < 0 || seat >= Seats) return;
            Instance[seat] = instanceId;
            Device[seat] = device;
        }

        public static void PulseId(int instanceId, int ev)
        {
            if (instanceId == 0) return;
            for (int i = 0; i < Seats; i++)
            {
                if (Instance[i] != instanceId) continue;
                int intensity = 0;
                GameSettings settings = GameSettings.Current;
                if (settings != null) intensity = settings.RumbleOf(i);
                Fire(i, ev, intensity);
                return;
            }
        }

        public static void Fire(int seat, int ev, int intensity)
        {
            if (seat < 0 || seat >= Seats) return;
            if (ev < 0 || ev >= Events) return;
            if (Device[seat] <= 0)
            {
                // Keyboard. Leave the motors at rest.
                return;
            }
            if (intensity <= 0) return;
            if (intensity > 100) intensity = 100;
            float scale = intensity / 100f;
            Low[seat] = LowAmp[ev] * scale;
            High[seat] = HighAmp[ev] * scale;
            Left[seat] = Seconds[ev];
            PadMotors++;
        }

        public static void Decay(float dt)
        {
            if (dt < 0f) dt = 0f;
            for (int i = 0; i < Seats; i++)
            {
                if (Left[i] <= 0f)
                {
                    Low[i] = 0f;
                    High[i] = 0f;
                    continue;
                }
                Left[i] -= dt;
                if (Left[i] <= 0f)
                {
                    Left[i] = 0f;
                    Low[i] = 0f;
                    High[i] = 0f;
                }
            }
        }

        /// <summary>Motors go to rest immediately. Pause, round end, and a dropped pad use this.</summary>
        public static void Silence()
        {
            for (int i = 0; i < Seats; i++)
                SilenceSeat(i);
        }

        public static void SilenceSeat(int seat)
        {
            if (seat < 0 || seat >= Seats) return;
            Low[seat] = 0f;
            High[seat] = 0f;
            Left[seat] = 0f;
        }

        public static void Motor(int seat, out float low, out float high)
        {
            if (seat < 0 || seat >= Seats)
            {
                low = 0f;
                high = 0f;
                return;
            }
            low = Low[seat];
            high = High[seat];
        }

        public static int PadIndex(int seat)
        {
            if (seat < 0 || seat >= Seats) return -1;
            if (Device[seat] <= 0) return -1;
            return Device[seat] - 1;
        }
    }

    public struct AccessibilityReport
    {
        public bool Ok;
        public string Line;
        public string Failure;

        public void Fail(string why)
        {
            Ok = false;
            if (string.IsNullOrEmpty(Failure)) Failure = why;
        }
    }

    public static class AccessibilityProof
    {
        public static AccessibilityReport Run()
        {
            var report = new AccessibilityReport { Ok = true };
            if (AccessibilityPalette.Count != 5)
                report.Fail("palette count drifted");
            if (!AccessibilityPalette.Separates())
                report.Fail("a palette failed cvd distance or ground contrast");
            if (AccessibilityPalette.PlayerGlyph.Length != 4 || AccessibilityPalette.ItGlyph.Length == 0)
                report.Fail("shape marks are missing");
            if (!CaptionsHold(report))
                report.Fail("captions did not map");
            if (!RumbleHolds(report))
                report.Fail("rumble did not map");
            if (!SeatsHold(report))
                report.Fail("per-player settings leaked");
            if (!FlashHolds(report))
                report.Fail("reduced flashing moved timing");
            if (GameSettings.HudMin != 0.75f || GameSettings.HudMax != 1.5f)
                report.Fail("text scale left the hud range");

            if (!report.Ok)
                report.Line = "accessibility FAIL " + report.Failure;
            else
                report.Line = "accessibility palettes=5 cvd=ok contrast=ok captions=ok rumble events mapped, kb rumble=0, per-player settings isolated in split";
            return report;
        }

        static bool CaptionsHold(AccessibilityReport report)
        {
            CaptionFeed.Reset();
            if (CaptionFeed.Icon(CaptionFeed.Foot).Length == 0) return false;
            if (CaptionFeed.Icon(CaptionFeed.Countdown).Length == 0) return false;
            if (CaptionFeed.Icon(CaptionFeed.Tag).Length == 0) return false;
            if (CaptionFeed.Icon(CaptionFeed.Pad).Length == 0) return false;
            if (CaptionFeed.Icon(CaptionFeed.Zip).Length == 0) return false;
            CaptionFeed.Footstep(0f, 10f);
            CaptionFeed.CountdownTick(0f, 0f);
            CaptionFeed.Tagged(8f, 0f);
            CaptionFeed.PadLaunch(-6f, 2f);
            CaptionFeed.ZipGrab(3f, -4f);
            for (int k = 0; k < CaptionFeed.Count; k++)
            {
                if (!CaptionFeed.Live(k)) return false;
                if (CaptionFeed.HasDirection(k) && CaptionFeed.Icon(k).Length == 0) return false;
            }
            if (CaptionFeed.Direction(CaptionFeed.Foot, 0f, 0f) != 0) return false;
            if (CaptionFeed.Arrow(0) != "↑") return false;

            GameSettings settings = GameSettings.Defaults();
            settings.Captions[0] = true;
            settings.Captions[1] = false;
            if (!CaptionFeed.Shows(settings, 0, CaptionFeed.Foot, 0f, 0f)) return false;
            if (CaptionFeed.Shows(settings, 1, CaptionFeed.Foot, 0f, 0f)) return false;
            if (CaptionFeed.Shows(settings, 0, CaptionFeed.Foot, 0f, 40f)) return false;
            if (!CaptionFeed.Shows(settings, 0, CaptionFeed.Countdown, 0f, 0f)) return false;
            if (CaptionFeed.Shows(settings, 1, CaptionFeed.Tag, 0f, 0f)) return false;
            CaptionFeed.Decay(1f);
            if (CaptionFeed.Live(CaptionFeed.Tag)) return false;
            CaptionFeed.Reset();
            return true;
        }

        static bool RumbleHolds(AccessibilityReport report)
        {
            if (!PadRumble.EventsMapped()) return false;
            GameSettings prev = GameSettings.Current;
            PadRumble.Reset();
            GameSettings settings = GameSettings.Defaults();
            settings.Rumble[0] = 100;
            settings.Rumble[1] = 100;
            GameSettings.Current = settings;
            PadRumble.Bind(0, 11, 0);
            PadRumble.Bind(1, 12, 1);
            for (int ev = 0; ev < PadRumble.Events; ev++)
                PadRumble.PulseId(11, ev);
            PadRumble.Motor(0, out float kLow, out float kHigh);
            if (kLow != 0f || kHigh != 0f || PadRumble.KeyboardMotors != 0)
            {
                GameSettings.Current = prev;
                PadRumble.Reset();
                return false;
            }
            int pads = 0;
            for (int ev = 0; ev < PadRumble.Events; ev++)
            {
                PadRumble.Fire(1, ev, 100);
                PadRumble.Motor(1, out float low, out float high);
                if (low <= 0f && high <= 0f)
                {
                    GameSettings.Current = prev;
                    PadRumble.Reset();
                    return false;
                }
                pads++;
                PadRumble.Decay(1f);
            }
            if (pads != PadRumble.Events || PadRumble.KeyboardMotors != 0)
            {
                GameSettings.Current = prev;
                PadRumble.Reset();
                return false;
            }
            PadRumble.Fire(1, PadRumble.TagClaim, 0);
            PadRumble.Motor(1, out float offLow, out float offHigh);
            if (offLow != 0f || offHigh != 0f)
            {
                GameSettings.Current = prev;
                PadRumble.Reset();
                return false;
            }
            settings.Rumble[0] = 80;
            settings.Rumble[1] = 0;
            PadRumble.Bind(0, 21, 2);
            PadRumble.Bind(1, 22, 3);
            PadRumble.PulseId(21, PadRumble.HardLand);
            PadRumble.PulseId(22, PadRumble.HardLand);
            PadRumble.Motor(0, out float aLow, out _);
            PadRumble.Motor(1, out float bLow, out _);
            bool isolated = aLow > 0f && bLow == 0f;
            GameSettings.Current = prev;
            PadRumble.Reset();
            return isolated;
        }

        static bool SeatsHold(AccessibilityReport report)
        {
            GameSettings edited = GameSettings.Defaults();
            edited.AccessSeat = 1;
            edited.Palette[0] = AccessibilityPalette.Deuteranopia;
            edited.Palette[1] = AccessibilityPalette.Tritanopia;
            edited.Palette[2] = AccessibilityPalette.HighContrast;
            edited.Captions[0] = true;
            edited.Captions[1] = false;
            edited.Rumble[0] = 25;
            edited.Rumble[1] = 100;
            edited.ReduceFlash[0] = false;
            edited.ReduceFlash[1] = true;
            edited.Colorblind = false;
            string blob = SettingsFile.Write(edited, ActionBinds.Defaults());
            GameSettings loaded = GameSettings.Defaults();
            SettingsFile.Read(blob, loaded, ActionBinds.Defaults());
            if (loaded.Palette[0] != AccessibilityPalette.Deuteranopia) return false;
            if (loaded.Palette[1] != AccessibilityPalette.Tritanopia) return false;
            if (loaded.Palette[2] != AccessibilityPalette.HighContrast) return false;
            if (!loaded.Captions[0] || loaded.Captions[1]) return false;
            if (loaded.Rumble[0] != 25 || loaded.Rumble[1] != 100) return false;
            if (loaded.ReduceFlash[0] || !loaded.ReduceFlash[1]) return false;
            if (loaded.AccessSeat != 1) return false;
            loaded.Palette[1] = AccessibilityPalette.Protanopia;
            loaded.Rumble[1] = 0;
            loaded.Captions[1] = true;
            if (loaded.Palette[0] != AccessibilityPalette.Deuteranopia) return false;
            if (loaded.Rumble[0] != 25 || loaded.Captions[0] != true) return false;
            loaded.ResetToDefaults();
            if (loaded.Palette[0] != 0 || loaded.Palette[1] != 0) return false;
            if (loaded.Captions[0] || loaded.ReduceFlash[1] || loaded.Rumble[0] != 0) return false;
            if (loaded.Colorblind) return false;
            return true;
        }

        static bool FlashHolds(AccessibilityReport report)
        {
            GameSettings settings = GameSettings.Defaults();
            float hard = settings.CountdownFlash(2.1f);
            float glow = settings.GlowVisual(1f);
            settings.ReduceFlash[1] = true;
            float soft = settings.CountdownFlash(2.1f);
            float flat = settings.GlowVisual(1f);
            if (soft != 1f) return false;
            if (hard == soft) return false;
            if (flat >= glow) return false;
            if (Math.Abs(Tag.Gameplay.TagBackImmunity.DefaultSeconds - 1.0f) > 0.001f) return false;
            return true;
        }
    }
}
