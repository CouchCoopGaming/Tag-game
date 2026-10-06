namespace Tag.Modes
{
    /// <summary>
    /// Verb cluster sits clear of the top-right mute chip (Comma) and the
    /// bottom-right minimap (M). Checked at 1920x1080 and 1280x720.
    /// </summary>
    public static class VerbHudLayout
    {
        static readonly Box[] _reserved = new Box[7];

        public struct Box
        {
            public float X, Y, W, H;
            public float Right => X + W;
            public float Bottom => Y + H;
        }

        public static Box Mute(float sw, float sh)
        {
            float w = sw >= 1600f ? 300f : 240f;
            float h = sh >= 1000f ? 40f : 36f;
            return new Box { X = sw - w - 12f, Y = 8f, W = w, H = h };
        }

        public static Box Minimap(float sw, float sh)
        {
            float size = sh >= 1000f ? 220f : 168f;
            const float m = 16f;
            return new Box { X = sw - size - m, Y = sh - size - m, W = size, H = size };
        }

        public static Box Banner(float sw)
        {
            const float w = 440f;
            return new Box { X = (sw - w) * 0.5f, Y = 12f, W = w, H = 84f };
        }

        public static Box ModeCard(float sh)
        {
            return new Box { X = 8f, Y = sh - 176f, W = 500f, H = 168f };
        }

        public static Box Cluster(float sw, float sh)
        {
            Box mute = Mute(sw, sh);
            Box map = Minimap(sw, sh);
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
            Box cluster = Cluster(sw, sh);
            if (Overlap(cluster, Mute(sw, sh))) return false;
            if (Overlap(cluster, Minimap(sw, sh))) return false;
            if (Overlap(cluster, Banner(sw))) return false;
            if (Overlap(cluster, ModeCard(sh))) return false;
            Box hint = HintBar(sw, sh);
            Box chip = ContextChip(sw, sh);
            if (Overlap(hint, Mute(sw, sh)) || Overlap(hint, Minimap(sw, sh))) return false;
            if (Overlap(hint, cluster) || Overlap(hint, Banner(sw)) || Overlap(hint, ModeCard(sh))) return false;
            if (Overlap(chip, Mute(sw, sh)) || Overlap(chip, Minimap(sw, sh))) return false;
            if (Overlap(chip, cluster) || Overlap(chip, Banner(sw)) || Overlap(chip, ModeCard(sh))) return false;
            if (Overlap(hint, chip)) return false;
            if (hint.W < 160f || hint.H < 28f || chip.W < 80f || chip.H < 24f) return false;
            if (hint.X < 8f || hint.Right > sw - 4f || hint.Bottom > sh - 4f) return false;
            if (chip.X < 8f || chip.Right > sw - 4f || chip.Bottom > sh - 4f) return false;
            if (cluster.X < 8f || cluster.Y < 8f) return false;
            if (cluster.Right > sw - 4f || cluster.Bottom > sh - 4f) return false;
            if (cluster.W < 80f || cluster.H < 160f) return false;
            for (int i = 0; i < 4; i++)
            {
                Box row = Row(cluster, i);
                if (row.H < 24f || row.W < 80f) return false;
            }
            return true;
        }

        /// <summary>Nudge a screen marker out of the mute chip, the minimap, and the verb cluster.</summary>
        public static void PushMarker(float sw, float sh, ref float x, ref float y, float w, float h)
        {
            Box self = new Box { X = x, Y = y, W = w, H = h };
            _reserved[0] = Mute(sw, sh);
            _reserved[1] = Minimap(sw, sh);
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
                + " 1280x720=" + (sd ? "clear" : "overlap");
        }
    }
}
