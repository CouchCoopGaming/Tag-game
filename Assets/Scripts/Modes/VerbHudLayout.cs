namespace Tag.Modes
{
    /// <summary>
    /// One reserve for the mute chip, the It banner, the verb rings, the
    /// minimap, the mode card, and the countdown arena picker. Pocket Park
    /// draws a smaller minimap. Checked at 1920x1080 and 1280x720 with the
    /// picker up, on both map scales.
    /// </summary>
    public static class VerbHudLayout
    {
        public const float PocketMinimapScale = 0.72f;

        /// <summary>
        /// Stack Yard is 110 m on the long side. Mega Park is 160 m and draws full size.
        /// 110/160 is 0.69; the widget is lifted to 0.84 so the park still reads in the corner
        /// and stays clear of the verb cluster at 1080p and 720p.
        /// </summary>
        public const float StackMinimapScale = 0.84f;

        static readonly Box[] _reserved = new Box[7];

        public struct Box
        {
            public float X, Y, W, H;
            public float Right => X + W;
            public float Bottom => Y + H;
        }

        public static Box Mute(float sw, float sh)
        {
            const float w = 360f;
            float h = sh >= 1000f ? 40f : 36f;
            return new Box { X = sw - w - 16f, Y = 12f, W = w, H = h };
        }

        public static Box Minimap(float sw, float sh)
        {
            return Minimap(sw, sh, false);
        }

        /// <summary>Pocket Park uses a smaller corner scale. Mega Park stays full size.</summary>
        public static Box Minimap(float sw, float sh, bool pocket)
        {
            return Minimap(sw, sh, pocket, false);
        }

        /// <summary>Stack Yard uses its own corner scale. Mega Park stays full size.</summary>
        public static Box Minimap(float sw, float sh, bool pocket, bool stack)
        {
            float scale = 1f;
            if (stack) scale = StackMinimapScale;
            else if (pocket) scale = PocketMinimapScale;
            float size = (sh >= 1000f ? 220f : 168f) * scale;
            const float m = 16f;
            return new Box { X = sw - size - m, Y = sh - size - m, W = size, H = size };
        }

        public static Box Banner(float sw)
        {
            const float w = 460f;
            return new Box { X = (sw - w) * 0.5f, Y = 16f, W = w, H = 78f };
        }

        public static Box ModeCard(float sh)
        {
            return new Box { X = 8f, Y = sh - 176f, W = 500f, H = 168f };
        }

        /// <summary>Countdown card: keys 1 and 2 pick the arena.</summary>
        public static Box Picker(float sw, float sh)
        {
            const float w = 480f;
            const float h = 210f;
            return new Box { X = (sw - w) * 0.5f, Y = sh * 0.28f, W = w, H = h };
        }

        public static Box Cluster(float sw, float sh)
        {
            Box mute = Mute(sw, sh);
            Box map = Minimap(sw, sh, false);
            float scale = sh >= 1000f ? 1f : 0.92f;
            float row = 78f * scale;
            float w = 176f * scale;
            float h = row * 4f;
            float x = sw - w - 20f;
            float y = mute.Bottom + 16f;
            if (y + h > map.Y - 12f)
                y = map.Y - 12f - h;
            return new Box { X = x, Y = y, W = w, H = h };
        }

        public static Box Row(Box cluster, int index)
        {
            float row = cluster.H / 4f;
            float y = cluster.Y + row * index;
            return new Box { X = cluster.X, Y = y, W = cluster.W, H = row - 6f };
        }

        /// <summary>First-run hint. Sits right of the mode card and left of the minimap.</summary>
        public static Box HintBar(float sw, float sh)
        {
            Box card = ModeCard(sh);
            Box map = Minimap(sw, sh);
            float h = sh >= 1000f ? 48f : 44f;
            float x = card.Right + 12f;
            float cap = sh >= 1000f ? 560f : 420f;
            float room = map.X - 12f - x;
            float w = room < cap ? room : cap;
            if (w < 160f) w = 160f;
            float y = sh - h - 16f;
            return new Box { X = x, Y = y, W = w, H = h };
        }

        /// <summary>Fallback chip for a contextual prompt when it is not on a world point.</summary>
        public static Box ContextChip(float sw, float sh)
        {
            float w = sh >= 1000f ? 280f : 240f;
            float h = 36f;
            return new Box { X = 16f, Y = sh * 0.38f, W = w, H = h };
        }

        public static bool Overlap(Box a, Box b)
        {
            return a.X < b.Right && b.X < a.Right && a.Y < b.Bottom && b.Y < a.Bottom;
        }

        public static bool Separated(float sw, float sh)
        {
            return LayoutClear(sw, sh, false, false) && LayoutClear(sw, sh, true, false) && LayoutClear(sw, sh, false, true);
        }

        static bool LayoutClear(float sw, float sh, bool pocket, bool stack)
        {
            Box cluster = Cluster(sw, sh);
            Box mute = Mute(sw, sh);
            Box map = Minimap(sw, sh, pocket, stack);
            Box banner = Banner(sw);
            Box mode = ModeCard(sh);
            Box picker = Picker(sw, sh);
            Box hint = HintBar(sw, sh);
            Box chip = ContextChip(sw, sh);
            if (!Apart(cluster, mute) || !Apart(cluster, map) || !Apart(cluster, banner)
                || !Apart(cluster, mode) || !Apart(cluster, picker))
                return false;
            if (!Apart(picker, mute) || !Apart(picker, map) || !Apart(picker, banner) || !Apart(picker, mode))
                return false;
            if (!Apart(banner, mute) || !Apart(map, mode))
                return false;
            if (!OnScreen(cluster, sw, sh) || !OnScreen(picker, sw, sh) || !OnScreen(map, sw, sh)
                || !OnScreen(mute, sw, sh) || !OnScreen(banner, sw, sh))
                return false;
            if (Overlap(hint, mute) || Overlap(hint, map) || Overlap(hint, cluster)
                || Overlap(hint, banner) || Overlap(hint, mode) || Overlap(hint, picker))
                return false;
            if (Overlap(chip, mute) || Overlap(chip, map) || Overlap(chip, cluster)
                || Overlap(chip, banner) || Overlap(chip, mode) || Overlap(chip, picker))
                return false;
            if (Overlap(hint, chip)) return false;
            if (hint.W < 160f || hint.H < 28f || chip.W < 80f || chip.H < 24f) return false;
            if (hint.X < 8f || hint.Right > sw - 4f || hint.Bottom > sh - 4f) return false;
            if (chip.X < 8f || chip.Right > sw - 4f || chip.Bottom > sh - 4f) return false;
            if (cluster.W < 80f || cluster.H < 160f) return false;
            for (int i = 0; i < 4; i++)
            {
                Box row = Row(cluster, i);
                if (row.H < 24f || row.W < 80f) return false;
            }
            return true;
        }

        static bool Apart(Box a, Box b)
        {
            const float gap = 8f;
            Box grown = new Box { X = b.X - gap, Y = b.Y - gap, W = b.W + gap * 2f, H = b.H + gap * 2f };
            return !Overlap(a, grown);
        }

        static bool OnScreen(Box box, float sw, float sh)
        {
            return box.X >= 4f && box.Y >= 4f && box.Right <= sw - 4f && box.Bottom <= sh - 4f && box.W > 8f && box.H > 8f;
        }

        /// <summary>Nudge a screen marker out of the mute chip, the minimap, the verb cluster, and the It banner.</summary>
        public static void PushMarker(float sw, float sh, ref float x, ref float y, float w, float h)
        {
            PushMarker(sw, sh, ref x, ref y, w, h, false);
        }

        public static void PushMarker(float sw, float sh, ref float x, ref float y, float w, float h, bool pocket)
        {
            PushMarker(sw, sh, ref x, ref y, w, h, pocket, false);
        }

        public static void PushMarker(float sw, float sh, ref float x, ref float y, float w, float h, bool pocket, bool stack)
        {
            Box self = new Box { X = x, Y = y, W = w, H = h };
            _reserved[0] = Mute(sw, sh);
            _reserved[1] = Minimap(sw, sh, pocket, stack);
            _reserved[2] = Cluster(sw, sh);
            _reserved[3] = Banner(sw);
            _reserved[4] = ModeCard(sh);
            _reserved[5] = HintBar(sw, sh);
            _reserved[6] = ContextChip(sw, sh);
            for (int i = 0; i < _reserved.Length; i++)
            {
                if (!Overlap(self, _reserved[i])) continue;
                float above = _reserved[i].Y - h - 6f;
                if (above >= 8f)
                    self.Y = above;
                else
                    self.Y = _reserved[i].Bottom + 6f;
                if (self.X < 8f) self.X = 8f;
                if (self.Right > sw - 8f) self.X = sw - 8f - w;
                if (self.Bottom > sh - 8f) self.Y = sh - 8f - h;
            }
            x = self.X;
            y = self.Y;
        }

        public static string ProofLine()
        {
            bool hd = Separated(1920f, 1080f);
            bool sd = Separated(1280f, 720f);
            return "verb-hud 1920x1080=" + (hd ? "clear" : "overlap")
                + " 1280x720=" + (sd ? "clear" : "overlap")
                + " picker=shown pocket-map=" + PocketMinimapScale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + " stack-map=" + StackMinimapScale.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
