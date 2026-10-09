using System.Collections.Generic;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Bold menu glyphs drawn in code. No purchased art.
    /// </summary>
    public static class MenuIcons
    {
        public static readonly Color PlayTint = new Color(0.95f, 0.22f, 0.28f, 1f);
        public static readonly Color PracticeTint = new Color(1f, 0.55f, 0.12f, 1f);
        public static readonly Color OptionsTint = new Color(0.25f, 0.62f, 1f, 1f);
        public static readonly Color ControlsTint = new Color(0.20f, 0.82f, 0.38f, 1f);
        public static readonly Color CreditsTint = new Color(1f, 0.84f, 0.12f, 1f);
        public static readonly Color QuitTint = new Color(0.95f, 0.45f, 0.18f, 1f);

        static Sprite _play;
        static Sprite _cone;
        static Sprite _gear;
        static Sprite _pad;
        static Sprite _star;
        static Sprite _door;
        static Sprite _keys;
        static Sprite _either;
        static Sprite _arrows;
        static Sprite _space;
        static Sprite _esc;
        static Sprite _stick;
        static Sprite _south;
        static Sprite _east;
        static Sprite _west;
        static Sprite _north;
        static Sprite _cross;
        static Sprite _circle;
        static Sprite _switchB;
        static Sprite _switchA;
        static Sprite _start;
        static Sprite _bust;

        public static Sprite Play => _play ??= Runner();
        public static Sprite Cone => _cone ??= TrafficCone();
        public static Sprite Gear => _gear ??= Cog();
        public static Sprite Pad => _pad ??= Gamepad();
        public static Sprite Star => _star ??= Burst();
        public static Sprite Door => _door ??= ExitDoor();
        public static Sprite Keys => _keys ??= Keyboard();
        public static Sprite Either => _either ??= JoinMark();
        public static Sprite KeyArrows => _arrows ??= ArrowKeys();
        public static Sprite KeySpace => _space ??= SpaceKey();
        public static Sprite KeyEsc => _esc ??= EscKey();
        public static Sprite Stick => _stick ??= StickCap();
        public static Sprite South => _south ??= FaceCap(new Color(0.15f, 0.72f, 0.32f, 1f), LetterA());
        public static Sprite East => _east ??= FaceCap(new Color(0.90f, 0.22f, 0.28f, 1f), LetterB());
        public static Sprite West => _west ??= FaceCap(new Color(0.10f, 0.38f, 0.92f, 1f), LetterX());
        public static Sprite North => _north ??= FaceCap(new Color(0.95f, 0.78f, 0.16f, 1f), LetterY(), new Color(0.08f, 0.08f, 0.10f, 1f));
        public static Sprite Cross => _cross ??= CrossMark();
        public static Sprite Circle => _circle ??= CircleMark();
        public static Sprite SwitchSouth => _switchB ??= FaceCap(new Color(0.95f, 0.78f, 0.16f, 1f), LetterB());
        public static Sprite SwitchEast => _switchA ??= FaceCap(new Color(0.90f, 0.22f, 0.28f, 1f), LetterA());
        public static Sprite StartButton => _start ??= StartCap();
        public static Sprite HierBust => _bust ??= HierSilhouette();

        public static Sprite Slot(int family, int slot)
        {
            return Slot(family, slot, FaceMap.DefaultOf(family));
        }

        public static Sprite Slot(int family, int slot, int face)
        {
            if (slot <= 0) return MoveOf(family);
            if (slot == 1) return ConfirmOf(family, face);
            return BackOf(family, face);
        }

        public static Sprite MoveOf(int family)
        {
            if (family == PadGlyph.Keyboard) return KeyArrows;
            return Stick;
        }

        public static Sprite ConfirmOf(int family)
        {
            return ConfirmOf(family, FaceMap.DefaultOf(family));
        }

        public static Sprite ConfirmOf(int family, int face)
        {
            if (family == PadGlyph.Keyboard) return KeySpace;
            bool east = face == FaceMap.East;
            if (family == PadGlyph.PlayStation) return east ? Circle : Cross;
            if (family == PadGlyph.Switch) return east ? SwitchEast : SwitchSouth;
            return east ? East : South;
        }

        public static Sprite BackOf(int family)
        {
            return BackOf(family, FaceMap.DefaultOf(family));
        }

        public static Sprite BackOf(int family, int face)
        {
            if (family == PadGlyph.Keyboard) return KeyEsc;
            bool east = face == FaceMap.East;
            if (family == PadGlyph.PlayStation) return east ? Cross : Circle;
            if (family == PadGlyph.Switch) return east ? SwitchSouth : SwitchEast;
            return east ? South : East;
        }

        public static Sprite SouthOf(int family)
        {
            if (family == PadGlyph.PlayStation) return Cross;
            if (family == PadGlyph.Switch) return SwitchSouth;
            if (family == PadGlyph.Keyboard) return KeySpace;
            return South;
        }

        public static Sprite BindOf(int family, int action)
        {
            if (family == PadGlyph.Keyboard)
            {
                if (action <= 1) return KeyArrows;
                if (action == 2) return KeySpace;
                if (action == 8) return KeyEsc;
                return Keys;
            }
            if (action <= 1) return Stick;
            if (action == 2 || action == 6) return SouthOf(family);
            if (action == 8) return family == PadGlyph.PlayStation ? Circle : family == PadGlyph.Switch ? SwitchEast : East;
            return Pad;
        }

        static readonly Dictionary<string, Sprite> Glyphs = new Dictionary<string, Sprite>();

        /// <summary>
        /// Sprite for one ActionBinds token. West is the blue X. East is the red B.
        /// </summary>
        public static Sprite Glyph(string token)
        {
            if (string.IsNullOrEmpty(token)) return Keys;
            if (Glyphs.TryGetValue(token, out Sprite have)) return have;
            Sprite made = MakeGlyph(token);
            if (made == null) made = Keys;
            Glyphs[token] = made;
            return made;
        }

        static Sprite MakeGlyph(string token)
        {
            switch (token)
            {
                case "wasd":
                case "arrows": return KeyArrows;
                case "space": return KeySpace;
                case "escape": return KeyEsc;
                case "leftStick":
                case "leftStickPress":
                case "rightStickPress": return Stick;
                case "rightStick": return FitLabel("RIGHT STICK");
                case "leftStickHold": return FitLabel("LEFT STICK");
                case "buttonSouth": return South;
                case "buttonEast": return East;
                case "buttonWest": return West;
                case "buttonNorth": return North;
                case "start": return StartButton;
                case "mouse": return FitLabel("MOUSE");
                case "mouseLeft": return MouseBody(true);
                case "mouseRight": return WordKey("RMB");
                case "mouseMiddle": return WordKey("MMB");
                case "holdIntoWall": return FitLabel("WASD");
                case "leftCtrl": return WordKey("CTRL");
                case "leftShift":
                case "rightShift": return WordKey("SHIFT");
                case "leftAlt": return WordKey("ALT");
                case "rightShoulder": return Bumper("RB");
                case "leftShoulder": return Bumper("LB");
                case "leftTrigger": return Bumper("LT");
                case "rightTrigger": return Bumper("RT");
                case "select": return WordKey("SEL");
                case "alpha1": return WordKey("1");
                case "alpha2": return WordKey("2");
                case "alpha3": return WordKey("3");
                case "dpadLeft": return Dpad(0);
                case "dpadRight": return Dpad(1);
                case "dpadUp": return Dpad(2);
                case "dpadDown": return Dpad(3);
                default:
                    if (token.Length == 1) return WordKey(token.ToUpperInvariant());
                    return Keys;
            }
        }

        static Sprite Runner()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            Color ink = Color.white;
            Disc(px, n, 62, 74, 11, ink);
            Limb(px, n, 54, 66, 46, 40, 8, ink);
            Limb(px, n, 48, 42, 28, 18, 7, ink);
            Limb(px, n, 50, 44, 78, 24, 7, ink);
            Limb(px, n, 52, 58, 30, 62, 6, ink);
            Limb(px, n, 56, 58, 82, 70, 6, ink);
            return Bake(px, n, n);
        }

        static Sprite TrafficCone()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            Tri(px, n, 48, 84, 22, 22, 74, 22, new Color(1f, 0.45f, 0.08f, 1f));
            Fill(px, n, 30, 48, 66, 58, Color.white);
            Fill(px, n, 18, 16, 78, 26, new Color(0.15f, 0.16f, 0.2f, 1f));
            return Bake(px, n, n);
        }

        static Sprite Cog()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            Disc(px, n, 48, 48, 28, Color.white);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 0.25f;
                int cx = 48 + Mathf.RoundToInt(Mathf.Cos(a) * 30f);
                int cy = 48 + Mathf.RoundToInt(Mathf.Sin(a) * 30f);
                Disc(px, n, cx, cy, 8, Color.white);
            }
            Disc(px, n, 48, 48, 12, new Color(0f, 0f, 0f, 0f));
            return Bake(px, n, n);
        }

        static Sprite Gamepad()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            RoundBox(px, n, 10, 28, 86, 70, 16, Color.white);
            Fill(px, n, 24, 44, 30, 58, new Color(0.1f, 0.12f, 0.16f, 1f));
            Fill(px, n, 32, 36, 38, 66, new Color(0.1f, 0.12f, 0.16f, 1f));
            Disc(px, n, 64, 54, 6, new Color(0.95f, 0.22f, 0.28f, 1f));
            Disc(px, n, 76, 44, 6, new Color(0.25f, 0.62f, 1f, 1f));
            return Bake(px, n, n);
        }

        static Sprite Burst()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            DrawStar(px, n, 48, 48, 44f, 18f, new Color(0.12f, 0.06f, 0.02f, 1f));
            DrawStar(px, n, 48, 48, 34f, 14f, new Color(1f, 0.98f, 0.92f, 1f));
            return Bake(px, n, n);
        }

        static void DrawStar(Color[] px, int n, int cx, int cy, float outer, float inner, Color c)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = -Mathf.PI * 0.5f + i * Mathf.PI * 2f / 5f;
                float b = a + Mathf.PI * 2f / 10f;
                float d = a + Mathf.PI * 2f / 5f;
                int x0 = cx + Mathf.RoundToInt(Mathf.Cos(a) * outer);
                int y0 = cy + Mathf.RoundToInt(Mathf.Sin(a) * outer);
                int x1 = cx + Mathf.RoundToInt(Mathf.Cos(b) * inner);
                int y1 = cy + Mathf.RoundToInt(Mathf.Sin(b) * inner);
                int x2 = cx + Mathf.RoundToInt(Mathf.Cos(d) * outer);
                int y2 = cy + Mathf.RoundToInt(Mathf.Sin(d) * outer);
                Tri(px, n, cx, cy, x0, y0, x1, y1, c);
                Tri(px, n, cx, cy, x1, y1, x2, y2, c);
            }
        }

        static Sprite ArrowKeys()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            KeyBody(px, n, 8, 16, 120, 112);
            Color ink = new Color(0.06f, 0.10f, 0.20f, 1f);
            Tri(px, n, 64, 100, 48, 78, 80, 78, ink);
            Tri(px, n, 64, 28, 48, 50, 80, 50, ink);
            Tri(px, n, 28, 64, 50, 48, 50, 80, ink);
            Tri(px, n, 100, 64, 78, 48, 78, 80, ink);
            Fill(px, n, 58, 50, 70, 78, ink);
            Fill(px, n, 50, 58, 78, 70, ink);
            return Bake(px, n, n);
        }

        static Sprite SpaceKey()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            KeyBody(px, n, 6, 28, 122, 100);
            RoundBox(px, n, 22, 48, 106, 78, 8, new Color(0.06f, 0.10f, 0.20f, 1f));
            return Bake(px, n, n);
        }

        static Sprite EscKey()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            KeyBody(px, n, 8, 24, 120, 104);
            Color ink = new Color(0.06f, 0.10f, 0.20f, 1f);
            Stamp(px, n, 18, 40, 5, LetterE(), ink);
            Stamp(px, n, 50, 40, 5, LetterS(), ink);
            Stamp(px, n, 82, 40, 5, LetterC(), ink);
            return Bake(px, n, n);
        }

        static Sprite StickCap()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Disc(px, n, 64, 58, 40, new Color(0.10f, 0.14f, 0.22f, 1f));
            Disc(px, n, 64, 58, 30, new Color(0.82f, 0.88f, 0.96f, 1f));
            Disc(px, n, 64, 58, 8, new Color(0.10f, 0.14f, 0.22f, 1f));
            Disc(px, n, 86, 80, 16, new Color(0.06f, 0.10f, 0.20f, 1f));
            Disc(px, n, 86, 80, 11, Color.white);
            return Bake(px, n, n);
        }

        static Sprite CrossMark()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Disc(px, n, 64, 64, 52, new Color(0.12f, 0.45f, 0.95f, 1f));
            Disc(px, n, 64, 64, 40, new Color(0.20f, 0.62f, 1f, 1f));
            Limb(px, n, 40, 40, 88, 88, 8, Color.white);
            Limb(px, n, 88, 40, 40, 88, 8, Color.white);
            return Bake(px, n, n);
        }

        static Sprite CircleMark()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Disc(px, n, 64, 64, 52, new Color(0.90f, 0.18f, 0.22f, 1f));
            Disc(px, n, 64, 64, 34, new Color(0.10f, 0.12f, 0.16f, 1f));
            return Bake(px, n, n);
        }

        static Sprite FaceCap(Color face, int[] letter)
        {
            return FaceCap(face, letter, Color.white);
        }

        static Sprite FaceCap(Color face, int[] letter, Color ink)
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Disc(px, n, 64, 64, 52, new Color(0.06f, 0.08f, 0.12f, 1f));
            Disc(px, n, 64, 64, 44, face);
            Disc(px, n, 64, 78, 10, new Color(1f, 1f, 1f, 0.28f));
            Stamp(px, n, 46, 42, 7, letter, ink);
            return Bake(px, n, n);
        }

        static void KeyBody(Color[] px, int n, int x0, int y0, int x1, int y1)
        {
            RoundBox(px, n, x0, y0, x1, y1, 16, new Color(0.06f, 0.10f, 0.18f, 1f));
            RoundBox(px, n, x0 + 6, y0 + 6, x1 - 6, y1 - 6, 12, new Color(0.96f, 0.97f, 1f, 1f));
        }

        static int[] LetterE()
        {
            return new[] { 0x1F, 0x10, 0x1E, 0x10, 0x1F };
        }

        static int[] LetterS()
        {
            return new[] { 0x0F, 0x10, 0x0E, 0x01, 0x1E };
        }

        static int[] LetterC()
        {
            return new[] { 0x0F, 0x10, 0x10, 0x10, 0x0F };
        }

        static int[] LetterA()
        {
            return new[] { 0x0E, 0x11, 0x1F, 0x11, 0x11 };
        }

        static int[] LetterB()
        {
            return new[] { 0x1E, 0x11, 0x1E, 0x11, 0x1E };
        }

        static int[] LetterX()
        {
            return new[] { 0x11, 0x0A, 0x04, 0x0A, 0x11 };
        }

        static int[] LetterY()
        {
            return new[] { 0x11, 0x0A, 0x04, 0x04, 0x04 };
        }

        static void Stamp(Color[] px, int n, int ox, int oy, int scale, int[] rows, Color c)
        {
            Stamp(px, n, n, ox, oy, scale, rows, c);
        }

        static void Stamp(Color[] px, int w, int h, int ox, int oy, int scale, int[] rows, Color c)
        {
            int n = rows.Length;
            for (int r = 0; r < n; r++)
            {
                int bits = rows[r];
                for (int col = 0; col < 5; col++)
                {
                    int mask = 1 << (4 - col);
                    if ((bits & mask) == 0) continue;
                    int x0 = ox + col * scale;
                    int y0 = oy + (n - 1 - r) * scale;
                    Fill(px, w, h, x0, y0, x0 + scale - 1, y0 + scale - 1, c);
                }
            }
        }

        static Sprite ExitDoor()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            RoundBox(px, n, 22, 12, 74, 84, 8, new Color(0.95f, 0.55f, 0.22f, 1f));
            RoundBox(px, n, 34, 20, 66, 78, 6, new Color(0.18f, 0.28f, 0.55f, 1f));
            Disc(px, n, 58, 48, 4, MenuTheme.Gold);
            return Bake(px, n, n);
        }

        static Sprite StartCap()
        {
            const int w = 64;
            const int h = 48;
            Color[] px = Clear(w, h);
            RoundBox(px, w, h, 2, 4, 62, 44, 10, Color.white);
            Fill(px, w, h, 14, 12, 50, 16, new Color(0.08f, 0.12f, 0.22f, 1f));
            Fill(px, w, h, 14, 22, 50, 26, new Color(0.08f, 0.12f, 0.22f, 1f));
            Fill(px, w, h, 14, 32, 50, 36, new Color(0.08f, 0.12f, 0.22f, 1f));
            return Bake(px, w, h);
        }

        /// <summary>
        /// Front idle of the Hier bake, body and costume only. Head, neck gap,
        /// tapered chest, arms held off the torso, and two legs.
        /// </summary>
        static Sprite HierSilhouette()
        {
            const int w = 48;
            const int h = 96;
            Color[] px = Clear(w, h);
            Color ink = Color.white;
            for (int row = 0; row < h; row++)
            {
                ulong bits = HierRows[row];
                int ty = h - 1 - row;
                for (int col = 0; col < w; col++)
                {
                    if ((bits & (1UL << (w - 1 - col))) == 0) continue;
                    Plot(px, w, h, col, ty, ink);
                }
            }
            return Bake(px, w, h);
        }

        static readonly ulong[] HierRows =
        {
            0x00000FC00000, 0x00001FE00000, 0x00001FF00000, 0x00003FF00000,
            0x00003FF80000, 0x00003FF80000, 0x00007FFC0000, 0x00007FFC0000,
            0x00007FFC0000, 0x00007FFC0000, 0x00007FFC0000, 0x00007FFC0000,
            0x00007FFC0000, 0x00007FFC0000, 0x00003FF80000, 0x00003FF80000,
            0x00001FF80000, 0x00001FF00000, 0x00000FF00000, 0x000007E00000,
            0x000000000000, 0x000000000000, 0x000007C00000, 0x00000FE00000,
            0x00003FF80000, 0x00307FFC1800, 0x007C7FFC7C00, 0x007E7FFCFC00,
            0x007E7FFCFC00, 0x007F7FFDFC00, 0x00FFFFFFFE00, 0x00FFFFFFFE00,
            0x01FFFFFFFF00, 0x01F7FFFFDF00, 0x01F77FFDDF00, 0x03F77FFDDF80,
            0x03F77FFDDF80, 0x03E73FF9CF80, 0x07E73FF9CFC0, 0x07C73FF9C7C0,
            0x0FC63FF8C7E0, 0x0FC03FF807E0, 0x0F803FF803E0, 0x0F803FF803E0,
            0x0F801FF003E0, 0x1E000FE000F0, 0x1E00000000F0, 0x1E00000000F0,
            0x3C0000000078, 0x3C0000000078, 0x3C00FC7E0078, 0x3C00FC7E0078,
            0x7C00FC7E007C, 0x7800FC7E003C, 0x7800FC7E003C, 0xF800FC7E003E,
            0xF800FC7E003E, 0xF800FC7E003E, 0xF000FC7E001E, 0xF000FC7E001E,
            0xF000FC7E001E, 0x0000FC7E0000, 0x0000FC7E0000, 0x0000FC7E0000,
            0x0000FC7E0000, 0x0000FC7E0000, 0x0000FC7E0000, 0x0000FC7E0000,
            0x0000FC7E0000, 0x0000FC7E0000, 0x0000FC7E0000, 0x0000FC7E0000,
            0x0000FC7E0000, 0x0000FC7E0000, 0x00007C7C0000, 0x00007C7C0000,
            0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000,
            0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000,
            0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000,
            0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000,
            0x00007C7C0000, 0x00007C7C0000, 0x00007C7C0000, 0x000010100000
        };

        static Sprite MouseBody(bool leftDown)
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Color body = new Color(0.96f, 0.97f, 1f, 1f);
            Color ink = new Color(0.06f, 0.10f, 0.20f, 1f);
            RoundBox(px, n, 34, 16, 94, 114, 28, ink);
            RoundBox(px, n, 40, 22, 88, 108, 24, body);
            Color left = leftDown ? new Color(0.16f, 0.42f, 0.95f, 1f) : body;
            Fill(px, n, 42, 72, 62, 104, left);
            Fill(px, n, 66, 72, 86, 104, leftDown ? body : new Color(0.82f, 0.86f, 0.94f, 1f));
            Fill(px, n, 62, 30, 66, 104, ink);
            Disc(px, n, 64, 56, 6, ink);
            return Bake(px, n, n);
        }

        static Sprite WallHold()
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Color ink = new Color(0.96f, 0.97f, 1f, 1f);
            Fill(px, n, 96, 14, 118, 114, new Color(0.62f, 0.70f, 0.84f, 1f));
            Fill(px, n, 90, 14, 96, 114, ink);
            Fill(px, n, 14, 54, 42, 74, ink);
            Tri(px, n, 86, 64, 40, 36, 40, 92, ink);
            return Bake(px, n, n);
        }

        static Sprite FitLabel(string word)
        {
            int letters = 0;
            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] != ' ') letters++;
            }
            int scale = letters >= 8 ? 4 : letters >= 5 ? 5 : 7;
            int gap = 2;
            int lw = 5 * scale;
            int lh = 5 * scale;
            int spaces = word.Length - letters;
            int textW = letters * lw + (letters > 1 ? (letters - 1) * gap : 0) + spaces * scale * 3;
            int pad = 10;
            int w = textW + pad * 2;
            int h = lh + pad * 2;
            Color[] px = Clear(w, h);
            int radius = h / 5;
            if (radius < 4) radius = 4;
            if (radius > 12) radius = 12;
            RoundBox(px, w, h, 0, 0, w - 1, h - 1, radius, new Color(0.06f, 0.10f, 0.18f, 1f));
            RoundBox(px, w, h, 4, 4, w - 5, h - 5, radius - 2, new Color(0.96f, 0.97f, 1f, 1f));
            Color ink = new Color(0.06f, 0.10f, 0.20f, 1f);
            int ox = (w - textW) / 2;
            int oy = (h - lh) / 2;
            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] == ' ')
                {
                    ox += scale * 3;
                    continue;
                }
                Stamp(px, w, h, ox, oy, scale, Bits(word[i]), ink);
                ox += lw + gap;
            }
            return Bake(px, w, h);
        }

        static Sprite WordKey(string word)
        {
            int scale = word.Length >= 5 ? 6 : word.Length >= 4 ? 7 : 8;
            int gap = 3;
            int lw = 5 * scale;
            int lh = 5 * scale;
            int textW = word.Length * lw + (word.Length - 1) * gap;
            int pad = 12;
            int w = textW + pad * 2;
            int h = lh + pad * 2;
            if (w < h) w = h;
            Color[] px = Clear(w, h);
            int radius = h / 5;
            if (radius < 6) radius = 6;
            if (radius > 16) radius = 16;
            RoundBox(px, w, h, 0, 0, w - 1, h - 1, radius, new Color(0.06f, 0.10f, 0.18f, 1f));
            RoundBox(px, w, h, 5, 5, w - 6, h - 6, radius - 2, new Color(0.96f, 0.97f, 1f, 1f));
            Color ink = new Color(0.06f, 0.10f, 0.20f, 1f);
            int ox = (w - textW) / 2;
            int oy = (h - lh) / 2;
            for (int i = 0; i < word.Length; i++)
            {
                int[] rows = Bits(word[i]);
                Stamp(px, w, h, ox, oy, scale, rows, ink);
                ox += lw + gap;
            }
            return Bake(px, w, h);
        }

        static Sprite Bumper(string word)
        {
            int scale = 8;
            int gap = 3;
            int lw = 5 * scale;
            int lh = 5 * scale;
            int textW = word.Length * lw + (word.Length - 1) * gap;
            int w = textW + 36;
            int h = lh + 28;
            Color[] px = Clear(w, h);
            RoundBox(px, w, h, 0, 0, w - 1, h - 1, h / 2, new Color(0.08f, 0.11f, 0.18f, 1f));
            RoundBox(px, w, h, 6, 6, w - 7, h - 7, h / 2 - 4, new Color(0.96f, 0.97f, 1f, 1f));
            Color ink = new Color(0.06f, 0.10f, 0.20f, 1f);
            int ox = (w - textW) / 2;
            int oy = (h - lh) / 2;
            for (int i = 0; i < word.Length; i++)
            {
                Stamp(px, w, h, ox, oy, scale, Bits(word[i]), ink);
                ox += lw + gap;
            }
            return Bake(px, w, h);
        }

        static Sprite Dpad(int dir)
        {
            const int n = 128;
            Color[] px = Clear(n, n);
            Color ink = new Color(0.90f, 0.93f, 0.98f, 1f);
            Color hot = new Color(0.95f, 0.78f, 0.16f, 1f);
            Fill(px, n, 52, 16, 76, 112, ink);
            Fill(px, n, 16, 52, 112, 76, ink);
            if (dir == 0) Fill(px, n, 16, 52, 52, 76, hot);
            else if (dir == 1) Fill(px, n, 76, 52, 112, 76, hot);
            else if (dir == 2) Fill(px, n, 52, 76, 76, 112, hot);
            else Fill(px, n, 52, 16, 76, 52, hot);
            return Bake(px, n, n);
        }

        static int[] Bits(char c)
        {
            switch (c)
            {
                case 'A': return LetterA();
                case 'B': return LetterB();
                case 'C': return LetterC();
                case 'D': return new[] { 0x1E, 0x11, 0x11, 0x11, 0x1E };
                case 'E': return LetterE();
                case 'F': return new[] { 0x1F, 0x10, 0x1E, 0x10, 0x10 };
                case 'G': return new[] { 0x0E, 0x10, 0x17, 0x11, 0x0E };
                case 'H': return new[] { 0x11, 0x11, 0x1F, 0x11, 0x11 };
                case 'I': return new[] { 0x1F, 0x04, 0x04, 0x04, 0x1F };
                case 'K': return new[] { 0x11, 0x12, 0x1C, 0x12, 0x11 };
                case 'L': return new[] { 0x10, 0x10, 0x10, 0x10, 0x1F };
                case 'M': return new[] { 0x11, 0x1B, 0x15, 0x11, 0x11 };
                case 'N': return new[] { 0x11, 0x19, 0x15, 0x13, 0x11 };
                case 'O': return new[] { 0x0E, 0x11, 0x11, 0x11, 0x0E };
                case 'P': return new[] { 0x1E, 0x11, 0x1E, 0x10, 0x10 };
                case 'Q': return new[] { 0x0E, 0x11, 0x15, 0x12, 0x0D };
                case 'R': return new[] { 0x1E, 0x11, 0x1E, 0x12, 0x11 };
                case 'S': return LetterS();
                case 'T': return new[] { 0x1F, 0x04, 0x04, 0x04, 0x04 };
                case 'U': return new[] { 0x11, 0x11, 0x11, 0x11, 0x0E };
                case 'V': return new[] { 0x11, 0x11, 0x11, 0x0A, 0x04 };
                case 'W': return new[] { 0x11, 0x11, 0x15, 0x1B, 0x11 };
                case 'X': return LetterX();
                case 'Y': return LetterY();
                case '1': return new[] { 0x04, 0x0C, 0x04, 0x04, 0x0E };
                case '2': return new[] { 0x0E, 0x11, 0x02, 0x04, 0x1F };
                case '3': return new[] { 0x1E, 0x01, 0x06, 0x01, 0x1E };
                default: return new[] { 0x1F, 0x11, 0x11, 0x11, 0x1F };
            }
        }

        static Sprite Keyboard()
        {
            const int n = 96;
            Color[] px = Clear(n, n);
            RoundBox(px, n, 8, 28, 88, 70, 10, Color.white);
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    int x = 18 + col * 17;
                    int y = 36 + row * 16;
                    Fill(px, n, x, y, x + 12, y + 10, new Color(0.12f, 0.16f, 0.24f, 1f));
                }
            }
            return Bake(px, n, n);
        }

        static Sprite JoinMark()
        {
            const int w = 128;
            const int h = 64;
            Color[] px = Clear(w, h);
            RoundBox(px, w, h, 4, 8, 58, 56, 8, Color.white);
            Fill(px, w, h, 12, 18, 24, 28, new Color(0.12f, 0.16f, 0.24f, 1f));
            Fill(px, w, h, 28, 18, 40, 28, new Color(0.12f, 0.16f, 0.24f, 1f));
            Fill(px, w, h, 12, 34, 50, 44, new Color(0.12f, 0.16f, 0.24f, 1f));
            RoundBox(px, w, h, 70, 14, 122, 52, 10, Color.white);
            Disc(px, w, h, 86, 34, 4, new Color(0.95f, 0.22f, 0.28f, 1f));
            Disc(px, w, h, 100, 28, 4, new Color(0.25f, 0.62f, 1f, 1f));
            return Bake(px, w, h);
        }

        static Color[] Clear(int w, int h)
        {
            return new Color[w * h];
        }

        static void Disc(Color[] px, int n, int cx, int cy, int r, Color c)
        {
            Disc(px, n, n, cx, cy, r, c);
        }

        static void Disc(Color[] px, int w, int h, int cx, int cy, int r, Color c)
        {
            int r2 = r * r;
            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= r2) Plot(px, w, h, x, y, c);
                }
            }
        }

        static void Fill(Color[] px, int n, int x0, int y0, int x1, int y1, Color c)
        {
            Fill(px, n, n, x0, y0, x1, y1, c);
        }

        static void Fill(Color[] px, int w, int h, int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    Plot(px, w, h, x, y, c);
        }

        static void RoundBox(Color[] px, int n, int x0, int y0, int x1, int y1, int r, Color c)
        {
            RoundBox(px, n, n, x0, y0, x1, y1, r, c);
        }

        static void RoundBox(Color[] px, int w, int h, int x0, int y0, int x1, int y1, int r, Color c)
        {
            Fill(px, w, h, x0 + r, y0, x1 - r, y1, c);
            Fill(px, w, h, x0, y0 + r, x1, y1 - r, c);
            Disc(px, w, h, x0 + r, y0 + r, r, c);
            Disc(px, w, h, x1 - r, y0 + r, r, c);
            Disc(px, w, h, x0 + r, y1 - r, r, c);
            Disc(px, w, h, x1 - r, y1 - r, r, c);
        }

        static void Limb(Color[] px, int n, int x0, int y0, int x1, int y1, int radius, Color c)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps < 1) steps = 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                Disc(px, n, x, y, radius, c);
            }
        }

        static void Tri(Color[] px, int n, int x0, int y0, int x1, int y1, int x2, int y2, Color c)
        {
            int minX = Mathf.Min(x0, Mathf.Min(x1, x2));
            int maxX = Mathf.Max(x0, Mathf.Max(x1, x2));
            int minY = Mathf.Min(y0, Mathf.Min(y1, y2));
            int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));
            float area = Edge(x0, y0, x1, y1, x2, y2);
            if (Mathf.Abs(area) < 0.5f) return;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float w0 = Edge(x1, y1, x2, y2, x, y);
                    float w1 = Edge(x2, y2, x0, y0, x, y);
                    float w2 = Edge(x0, y0, x1, y1, x, y);
                    bool pos = w0 >= 0f && w1 >= 0f && w2 >= 0f;
                    bool neg = w0 <= 0f && w1 <= 0f && w2 <= 0f;
                    if (pos || neg) Plot(px, n, n, x, y, c);
                }
            }
        }

        static float Edge(int ax, int ay, int bx, int by, int cx, int cy)
        {
            return (cx - ax) * (by - ay) - (cy - ay) * (bx - ax);
        }

        static void Plot(Color[] px, int w, int h, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x;
            if (c.a <= 0.01f)
            {
                px[i] = new Color(0f, 0f, 0f, 0f);
                return;
            }
            Color d = px[i];
            float a = c.a + d.a * (1f - c.a);
            if (a <= 0.001f) return;
            px[i] = new Color(
                (c.r * c.a + d.r * d.a * (1f - c.a)) / a,
                (c.g * c.a + d.g * d.a * (1f - c.a)) / a,
                (c.b * c.a + d.b * d.a * (1f - c.a)) / a,
                a);
        }

        static Sprite Bake(Color[] px, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
