using System;
using System.Globalization;

namespace Tag.Settings
{
    /// <summary>
    /// Pause settings. Defaults match the current camera, listener, and HUD
    /// so an untouched install does not change feel. FOV, scale, and look
    /// speeds clamp to a small range. Coyote, jump, and the other motor
    /// numbers are not stored here.
    /// </summary>
    public sealed class GameSettings
    {
        public const float FovDefault = 78f;
        public const float FovMin = 60f;
        public const float FovMax = 100f;
        public const float HudDefault = 1f;
        public const float HudMin = 0.75f;
        public const float HudMax = 1.5f;
        public const float MouseDefault = 1.8f;
        public const float MouseMin = 0.5f;
        public const float MouseMax = 5f;
        public const float PadLookDefault = 2.2f;
        public const float PadLookMin = 0.5f;
        public const float PadLookMax = 8f;
        public const float StickInnerDefault = 0.19f;
        public const float StickOuterDefault = 1f;
        public const float StickCurveDefault = 1f;
        public const float LookAccelDefault = 0f;
        public const float MasterDefault = 0.8f;
        public const float SfxDefault = 1f;
        public const float UiDefault = 1f;
        public const float MusicDefault = 0.35f;

        public const int RowMouse = 0;
        public const int RowPad = 1;
        public const int RowInvert = 2;
        public const int RowFov = 3;
        public const int RowMaster = 4;
        public const int RowSfx = 5;
        public const int RowUi = 6;
        public const int RowMusic = 7;
        public const int RowMute = 8;
        public const int RowHud = 9;
        public const int RowColorblind = 10;
        public const int RowMinimap = 11;
        public const int RowPlayer = 12;
        public const int RowCaptions = 13;
        public const int RowRumble = 14;
        public const int RowReduceFlash = 15;
        public const int RowComic = 16;
        public const int RowEffects = 17;
        public const int RowReset = 18;
        public const int RowReplay = 19;
        public const int RowBack = 20;
        public const int RowCount = 21;

        public static readonly float[] MouseSteps = { 1.0f, 1.4f, 1.8f, 2.4f, 3.2f };
        public static readonly float[] PadLookSteps = { 1.0f, 1.6f, 2.2f, 3.0f, 4.5f };
        public static readonly float[] FovSteps = { 60f, 70f, 78f, 90f, 100f };
        public static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.8f, 1f };
        public static readonly float[] SfxSteps = { 0f, 0.5f, 0.75f, 1f };
        public static readonly float[] UiSteps = { 0f, 0.5f, 0.75f, 1f };
        public static readonly float[] MusicSteps = { 0f, 0.15f, 0.35f, 0.55f, 1f };
        public static readonly float[] HudSteps = { 0.75f, 1f, 1.25f, 1.5f };
        public static readonly float[] RumbleSteps = { 0f, 25f, 50f, 75f, 100f };
        static readonly string[] SeatNames = { "P1", "P2", "P3", "P4" };

        public static GameSettings Current = Defaults();

        public float MouseSensitivity = MouseDefault;
        public float GamepadLook = PadLookDefault;
        public float StickInner = StickInnerDefault;
        public float StickOuter = StickOuterDefault;
        public float StickCurve = StickCurveDefault;
        public float LookAccel = LookAccelDefault;
        public bool InvertY;
        public float Fov = FovDefault;
        public float Master = MasterDefault;
        public float Sfx = SfxDefault;
        public float Ui = UiDefault;
        public float Music = MusicDefault;
        public bool Muted;
        public float HudScale = HudDefault;
        public bool Colorblind;
        public bool Minimap = true;
        public int Arena;

        public const int SeatCount = 4;
        /// <summary>Which seat the palette, captions, rumble, and flash rows edit.</summary>
        public int AccessSeat;
        public readonly int[] Palette = new int[SeatCount];
        public readonly bool[] Captions = new bool[SeatCount];
        public readonly int[] Rumble = new int[SeatCount];
        public readonly bool[] ReduceFlash = new bool[SeatCount];
        /// <summary>Comic contact words. On until a seat turns them off. Reduced flashing also hides them.</summary>
        public bool ComicWords = true;
        /// <summary>
        /// Air-dash and grapple speed lines. Off until a seat turns them on.
        /// Not one of the 21 rows. The Options screen lists <see cref="SpeedLinesLabel"/>.
        /// </summary>
        public bool SpeedLines;
        /// <summary>
        /// Seat-colored strip left on a wall run. Off until a seat turns it on.
        /// Not one of the 21 rows, and not one of the seven FX-kit toggles.
        /// </summary>
        public bool WallRibbon;
        /// <summary>Body-sized black card on a hit. Off until a seat turns it on. Not one of the 21 rows.</summary>
        public bool InkCard;
        /// <summary>Off-screen crown wedge. Off until a seat turns it on. Not one of the 21 rows.</summary>
        public bool ItWedge;
        /// <summary>Crown plate above the hat. Off until a seat turns it on. Not one of the 21 rows.</summary>
        public bool CrownPlate;
        /// <summary>
        /// Owner-pane margin streaks. Off until a seat turns them on.
        /// When on, full effects draws 6, low draws 3, and off or reduced flashing draws 0.
        /// Not one of the 21 rows, and not one of the seven FX-kit toggles. Speed lines stay off.
        /// </summary>
        public bool EdgeStreaks;
        /// <summary>
        /// Foot contact mark in the surface's shape. Off until a seat turns it on.
        /// Not one of the 21 rows, and not one of the seven FX-kit toggles.
        /// </summary>
        public bool ContactMarks;
        /// <summary>
        /// Wall contact mark in the foot's shape. Off until a seat turns it on.
        /// Not one of the 21 rows, and not one of the seven FX-kit toggles.
        /// </summary>
        public bool WallMarks;
        /// <summary>0 off, 1 low, 2 full. Visual density only.</summary>
        public int Effects = 2;

        /// <summary>Opponents beside the local player. 0 is solo. 3 fills the pads.</summary>
        public const int AiMin = 0;
        public const int AiMax = 3;
        public const int RoundsMin = 1;
        public const int RoundsMax = 5;
        public const int RoundLengthDefault = 1;

        public static readonly float[] DifficultyTiers = { 0.2f, 0.5f, 0.9f };
        public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };
        /// <summary>Index 1 is the 120s Least It default. The others are presets.</summary>
        public static readonly float[] RoundLengthPresets = { 60f, 120f, 180f, 300f };

        public int AiOpponents = 1;
        public int DifficultyTier = 1;
        public int RoundLengthIndex = RoundLengthDefault;
        public int RoundsPerMatch = 1;

        /// <summary>Two humans. 0 is a vertical split, 1 is a horizontal split.</summary>
        public const int SplitVertical = 0;
        public const int SplitHorizontal = 1;
        /// <summary>One listener on P1, or one listener at the average of the humans.</summary>
        public const int ListenP1 = 0;
        public const int ListenAverage = 1;
        public int SplitAxis = SplitVertical;
        public int Listener = ListenP1;

        public static GameSettings Defaults()
        {
            return new GameSettings();
        }

        public void CopyFrom(GameSettings other)
        {
            MouseSensitivity = other.MouseSensitivity;
            GamepadLook = other.GamepadLook;
            StickInner = other.StickInner;
            StickOuter = other.StickOuter;
            StickCurve = other.StickCurve;
            LookAccel = other.LookAccel;
            InvertY = other.InvertY;
            Fov = other.Fov;
            Master = other.Master;
            Sfx = other.Sfx;
            Ui = other.Ui;
            Music = other.Music;
            Muted = other.Muted;
            HudScale = other.HudScale;
            Colorblind = other.Colorblind;
            Minimap = other.Minimap;
            Arena = other.Arena;
            AccessSeat = other.AccessSeat;
            CopySeats(other);
            AiOpponents = other.AiOpponents;
            DifficultyTier = other.DifficultyTier;
            RoundLengthIndex = other.RoundLengthIndex;
            RoundsPerMatch = other.RoundsPerMatch;
            SplitAxis = other.SplitAxis;
            Listener = other.Listener;
            ComicWords = other.ComicWords;
            SpeedLines = other.SpeedLines;
            WallRibbon = other.WallRibbon;
            InkCard = other.InkCard;
            ItWedge = other.ItWedge;
            CrownPlate = other.CrownPlate;
            EdgeStreaks = other.EdgeStreaks;
            ContactMarks = other.ContactMarks;
            WallMarks = other.WallMarks;
            Effects = other.Effects;
        }

        public void ResetToDefaults()
        {
            CopyFrom(Defaults());
        }

        public void Clamp()
        {
            MouseSensitivity = ClampFloat(MouseSensitivity, MouseMin, MouseMax);
            GamepadLook = ClampFloat(GamepadLook, PadLookMin, PadLookMax);
            StickInner = ClampFloat(StickInner, 0f, 0.5f);
            float outerMin = StickInner + 0.05f;
            if (outerMin > 1f) outerMin = 1f;
            StickOuter = ClampFloat(StickOuter, outerMin, 1f);
            StickCurve = ClampFloat(StickCurve, 0.5f, 3f);
            LookAccel = ClampFloat(LookAccel, 0f, 1f);
            Fov = ClampFloat(Fov, FovMin, FovMax);
            Master = ClampFloat(Master, 0f, 1f);
            Sfx = ClampFloat(Sfx, 0f, 1f);
            Ui = ClampFloat(Ui, 0f, 1f);
            Music = ClampFloat(Music, 0f, 1f);
            HudScale = ClampFloat(HudScale, HudMin, HudMax);
            int lastArena = Tag.Onboard.ArenaRegistry.Count - 1;
            if (lastArena < 0) lastArena = 0;
            if (Arena < 0) Arena = 0;
            if (Arena > lastArena) Arena = lastArena;
            if (AiOpponents < AiMin) AiOpponents = AiMin;
            if (AiOpponents > AiMax) AiOpponents = AiMax;
            if (DifficultyTier < 0) DifficultyTier = 0;
            if (DifficultyTier >= DifficultyTiers.Length) DifficultyTier = DifficultyTiers.Length - 1;
            if (RoundLengthIndex < 0) RoundLengthIndex = 0;
            if (RoundLengthIndex >= RoundLengthPresets.Length) RoundLengthIndex = RoundLengthPresets.Length - 1;
            if (RoundsPerMatch < RoundsMin) RoundsPerMatch = RoundsMin;
            if (RoundsPerMatch > RoundsMax) RoundsPerMatch = RoundsMax;
            if (SplitAxis != SplitHorizontal) SplitAxis = SplitVertical;
            if (Listener != ListenAverage) Listener = ListenP1;
            if (AccessSeat < 0) AccessSeat = 0;
            if (AccessSeat >= SeatCount) AccessSeat = SeatCount - 1;
            if (Effects < 0) Effects = 0;
            if (Effects > 2) Effects = 2;
            for (int i = 0; i < SeatCount; i++)
            {
                if (Palette[i] < 0) Palette[i] = 0;
                if (Palette[i] >= AccessibilityPalette.Count) Palette[i] = AccessibilityPalette.Count - 1;
                if (Rumble[i] < 0) Rumble[i] = 0;
                if (Rumble[i] > 100) Rumble[i] = 100;
            }
        }

        public int PaletteOf(int seat)
        {
            int i = seat;
            if (i < 0 || i >= SeatCount) i = 0;
            int p = Palette[i];
            if (p < 0 || p >= AccessibilityPalette.Count) p = 0;
            if (i == 0 && p == 0 && Colorblind) return AccessibilityPalette.Deuteranopia;
            return p;
        }

        public bool CaptionsOf(int seat)
        {
            if (seat < 0 || seat >= SeatCount) return Captions[0];
            return Captions[seat];
        }

        public int RumbleOf(int seat)
        {
            if (seat < 0 || seat >= SeatCount) return 0;
            int v = Rumble[seat];
            if (v < 0) return 0;
            if (v > 100) return 100;
            return v;
        }

        public bool AnyReduceFlash()
        {
            for (int i = 0; i < SeatCount; i++)
            {
                if (ReduceFlash[i]) return true;
            }
            return false;
        }

        /// <summary>Flattens the tag-back strobe. The immunity window is not touched.</summary>
        public float GlowVisual(float pulse)
        {
            if (!AnyReduceFlash()) return pulse;
            return 0.58f + pulse * 0.05f;
        }

        /// <summary>Countdown digit brightness. The phase timer is not touched.</summary>
        public float CountdownFlash(float phaseTimer)
        {
            if (AnyReduceFlash()) return 1f;
            float frac = phaseTimer;
            if (frac < 0f) frac = 0f;
            frac -= (int)frac;
            return 0.42f + 0.58f * frac;
        }

        void CopySeats(GameSettings other)
        {
            if (other == null) return;
            for (int i = 0; i < SeatCount; i++)
            {
                Palette[i] = other.Palette[i];
                Captions[i] = other.Captions[i];
                Rumble[i] = other.Rumble[i];
                ReduceFlash[i] = other.ReduceFlash[i];
            }
        }

        public float DifficultyValue()
        {
            int i = DifficultyTier;
            if (i < 0 || i >= DifficultyTiers.Length) i = 1;
            return DifficultyTiers[i];
        }

        public string DifficultyLabel()
        {
            int i = DifficultyTier;
            if (i < 0 || i >= DifficultyNames.Length) i = 1;
            return DifficultyNames[i];
        }

        public float RoundSeconds()
        {
            int i = RoundLengthIndex;
            if (i < 0 || i >= RoundLengthPresets.Length) i = RoundLengthDefault;
            return RoundLengthPresets[i];
        }

        public void Nudge(int row, int dir)
        {
            if (dir == 0) return;
            switch (row)
            {
                case 0: MouseSensitivity = Step(MouseSensitivity, dir, MouseSteps); break;
                case 1: GamepadLook = Step(GamepadLook, dir, PadLookSteps); break;
                case 2: InvertY = !InvertY; break;
                case 3: Fov = Step(Fov, dir, FovSteps); break;
                case RowMaster: Master = Step(Master, dir, VolumeSteps); break;
                case RowSfx: Sfx = Step(Sfx, dir, SfxSteps); break;
                case RowUi: Ui = Step(Ui, dir, UiSteps); break;
                case RowMusic: Music = Step(Music, dir, MusicSteps); break;
                case RowMute: Muted = !Muted; break;
                case RowHud: HudScale = Step(HudScale, dir, HudSteps); break;
                case RowColorblind: CyclePalette(dir); break;
                case RowMinimap: Minimap = !Minimap; break;
                case RowPlayer: CycleSeat(dir); break;
                case RowCaptions: Captions[AccessSeat] = !Captions[AccessSeat]; break;
                case RowRumble: Rumble[AccessSeat] = (int)Step(Rumble[AccessSeat], dir, RumbleSteps); break;
                case RowReduceFlash: ReduceFlash[AccessSeat] = !ReduceFlash[AccessSeat]; break;
                case RowComic: ComicWords = !ComicWords; break;
                case RowEffects:
                    Effects += dir >= 0 ? 1 : -1;
                    if (Effects > 2) Effects = 0;
                    if (Effects < 0) Effects = 2;
                    break;
            }
            Clamp();
        }

        public string RowLabel(int row)
        {
            switch (row)
            {
                case 0: return "Mouse sensitivity  " + MouseSensitivity.ToString("0.0", CultureInfo.InvariantCulture);
                case 1: return "Gamepad look  " + GamepadLook.ToString("0.0", CultureInfo.InvariantCulture);
                case 2: return "Invert Y  " + (InvertY ? "On" : "Off");
                case 3: return "FOV  " + Fov.ToString("0", CultureInfo.InvariantCulture);
                case RowMaster: return "Master  " + Master.ToString("0.00", CultureInfo.InvariantCulture);
                case RowSfx: return "SFX  " + Sfx.ToString("0.00", CultureInfo.InvariantCulture);
                case RowUi: return "UI  " + Ui.ToString("0.00", CultureInfo.InvariantCulture);
                case RowMusic: return "Music  " + Music.ToString("0.00", CultureInfo.InvariantCulture);
                case RowMute: return Muted ? "Unmute  (Comma)" : "Mute  (Comma)";
                case RowHud: return "HUD scale  " + HudScale.ToString("0.00", CultureInfo.InvariantCulture);
                case RowColorblind: return "Colorblind palette  " + AccessibilityPalette.Name(PaletteOf(AccessSeat));
                case RowMinimap: return "Minimap  " + (Minimap ? "On  (M)" : "Off  (M)");
                case RowPlayer: return "Player  " + SeatNames[AccessSeat];
                case RowCaptions: return "Captions  " + (Captions[AccessSeat] ? "On" : "Off");
                case RowRumble: return "Rumble  " + RumbleText(Rumble[AccessSeat]);
                case RowReduceFlash: return "Reduced flashing  " + (ReduceFlash[AccessSeat] ? "On" : "Off");
                case RowComic: return "Comic words  " + (ComicWords ? "On" : "Off");
                case RowEffects: return "Effects  " + (Effects <= 0 ? "Off" : Effects == 1 ? "Low" : "Full");
                case RowReset: return "Reset to defaults";
                case RowReplay: return "Replay tips";
                default: return "Back";
            }
        }

        /// <summary>Label for the Options row. Default is Off.</summary>
        public string SpeedLinesLabel()
        {
            return SpeedLines ? "Speed lines: On" : "Speed lines: Off";
        }

        /// <summary>Label for the Options row. Default is Off.</summary>
        public string WallRibbonLabel()
        {
            return WallRibbon ? "Wall ribbon: On" : "Wall ribbon: Off";
        }

        public string InkCardLabel()
        {
            return InkCard ? "Ink card: On" : "Ink card: Off";
        }

        public string ItWedgeLabel()
        {
            return ItWedge ? "It wedge: On" : "It wedge: Off";
        }

        public string CrownPlateLabel()
        {
            return CrownPlate ? "Crown plate: On" : "Crown plate: Off";
        }

        public string EdgeStreaksLabel()
        {
            return EdgeStreaks ? "Edge streaks: On" : "Edge streaks: Off";
        }

        public string ContactMarksLabel()
        {
            return ContactMarks ? "Contact marks: On" : "Contact marks: Off";
        }

        public string WallMarksLabel()
        {
            return WallMarks ? "Wall marks: On" : "Wall marks: Off";
        }

        public static string ArenaName(int arena)
        {
            if (arena >= 0 && arena < Tag.Onboard.ArenaRegistry.Count)
                return Tag.Onboard.ArenaRegistry.All[arena].Name;
            return Tag.Onboard.ArenaRegistry.All[0].Name;
        }

        /// <summary>
        /// Verb HUD marks. Index 0 dash, 1 safe, 2 stagger, 3 cling.
        /// The default set is the colors the cluster already draws.
        /// The safe set is blue, yellow, white, and cyan. No red/green pair.
        /// </summary>
        public static void VerbMark(bool colorblind, int index, out float r, out float g, out float b)
        {
            if (!colorblind)
            {
                switch (index)
                {
                    case 1: r = 0.95f; g = 0.72f; b = 0.22f; return;
                    case 2: r = 0.62f; g = 0.45f; b = 0.95f; return;
                    case 3: r = 0.93f; g = 0.95f; b = 0.98f; return;
                    default: r = 0.25f; g = 0.55f; b = 0.95f; return;
                }
            }

            switch (index)
            {
                case 1: r = 0.95f; g = 0.85f; b = 0.15f; return;
                case 2: r = 0.95f; g = 0.95f; b = 0.95f; return;
                case 3: r = 0.15f; g = 0.80f; b = 0.85f; return;
                default: r = 0.20f; g = 0.45f; b = 0.95f; return;
            }
        }

        public static bool SafePaletteSeparable()
        {
            for (int i = 0; i < 4; i++)
            {
                VerbMark(true, i, out float r, out float g, out float b);
                bool red = r > 0.8f && g < 0.35f && b < 0.35f;
                bool green = g > 0.8f && r < 0.35f && b < 0.35f;
                if (red || green) return false;
            }
            VerbMark(true, 0, out float br, out float bg, out float bb);
            VerbMark(true, 1, out float yr, out float yg, out float yb);
            if (bb < 0.7f || yb > 0.4f) return false;
            if (Math.Abs(br - yr) < 0.2f && Math.Abs(bg - yg) < 0.2f && Math.Abs(bb - yb) < 0.2f)
                return false;
            return true;
        }

        void CyclePalette(int dir)
        {
            int seat = AccessSeat;
            if (seat < 0 || seat >= SeatCount) seat = 0;
            // Step from the palette the pawn is actually drawing. A stale
            // colorblind flag used to leave the row on Default while seat 0
            // kept drawing deuteranopia, so the first nudge did nothing.
            int next = PaletteOf(seat) + (dir < 0 ? -1 : 1);
            if (next < 0) next = 0;
            if (next >= AccessibilityPalette.Count) next = AccessibilityPalette.Count - 1;
            Palette[seat] = next;
            if (seat == 0) Colorblind = next != 0;
        }

        void CycleSeat(int dir)
        {
            int next = AccessSeat + (dir < 0 ? -1 : 1);
            if (next < 0) next = 0;
            if (next >= SeatCount) next = SeatCount - 1;
            AccessSeat = next;
        }

        static string RumbleText(int value)
        {
            if (value <= 0) return "Off";
            if (value < 37) return "25%";
            if (value < 62) return "50%";
            if (value < 87) return "75%";
            return "100%";
        }

        static float Step(float value, int dir, float[] steps)
        {
            int i = 0;
            float best = Math.Abs(steps[0] - value);
            for (int n = 1; n < steps.Length; n++)
            {
                float d = Math.Abs(steps[n] - value);
                if (d < best)
                {
                    best = d;
                    i = n;
                }
            }
            int next = i + (dir < 0 ? -1 : 1);
            if (next < 0) next = 0;
            if (next >= steps.Length) next = steps.Length - 1;
            return steps[next];
        }

        static float ClampFloat(float v, float lo, float hi)
        {
            if (float.IsNaN(v) || float.IsInfinity(v)) return lo;
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}
