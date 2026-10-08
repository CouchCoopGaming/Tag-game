using Tag.Couch;
using Tag.Profiles;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Couch-readable colors. Seat tints come from the live accessibility palette
    /// once a player has joined. Feel numbers are not stored here.
    /// </summary>
    public static class MenuTheme
    {
        public static readonly Color SkyTop = new Color(0.22f, 0.72f, 1f, 1f);
        public static readonly Color SkyBot = new Color(0.05f, 0.10f, 0.42f, 1f);
        public static readonly Color Ink = new Color(0.04f, 0.07f, 0.16f, 1f);
        public static readonly Color Veil = new Color(0.04f, 0.10f, 0.28f, 0.38f);
        public static readonly Color Navy = new Color(0.06f, 0.16f, 0.40f, 1f);
        public static readonly Color Panel = new Color(0.08f, 0.32f, 0.86f, 1f);
        public static readonly Color PanelHot = new Color(0.20f, 0.58f, 1f, 1f);
        public static readonly Color Gold = new Color(1f, 0.84f, 0.12f, 1f);
        public static readonly Color Cream = new Color(1f, 0.98f, 0.92f, 1f);
        public static readonly Color Mute = new Color(0.78f, 0.88f, 1f, 1f);
        public static readonly Color Dim = new Color(0.02f, 0.05f, 0.12f, 0.55f);
        public static readonly Color Ready = new Color(0.20f, 0.95f, 0.42f, 1f);
        public static readonly Color Off = new Color(0.22f, 0.26f, 0.34f, 1f);
        public static readonly Color Stroke = new Color(0.02f, 0.04f, 0.10f, 1f);
        public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.48f);

        // One seat palette. P1 red, P2 blue, P3 orange, P4 purple.
        // Results bands and the other screens read these. Do not keep a second copy.
        static readonly Color[] Fallback =
        {
            new Color(0.95f, 0.16f, 0.22f, 1f),
            new Color(0.16f, 0.45f, 1f, 1f),
            new Color(0.94f, 0.42f, 0.14f, 1f),
            new Color(0.62f, 0.32f, 0.86f, 1f)
        };

        static Font _font;
        static Font _display;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.Load<Font>("UI/Fonts/LiberationSans-Bold");
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static Font Display
        {
            get
            {
                if (_display != null) return _display;
                _display = Resources.Load<Font>(MenuPolish.DisplayResource);
                if (_display == null) _display = Font;
                return _display;
            }
        }

        public static Color Seat(int seat)
        {
            int i = seat;
            if (i < 0) i = 0;
            if (i > 3) i = 3;
            if (CouchPlay.HumanAt(i) || LocalProfiles.SeatColor(i) >= 0)
            {
                CouchPlay.Tint(i, out float r, out float g, out float b);
                return new Color(r, g, b, 1f);
            }
            return SeatBand(i);
        }

        /// <summary>
        /// The seat palette itself. P1 red, P2 blue, P3 orange, P4 purple.
        /// A joined player may tint Seat. The band on a results block stays this color.
        /// </summary>
        public static Color SeatBand(int seat)
        {
            int i = seat;
            if (i < 0) i = 0;
            if (i > 3) i = 3;
            return Fallback[i];
        }

        public static string Place(int rank)
        {
            if (rank == 0) return "1st";
            if (rank == 1) return "2nd";
            if (rank == 2) return "3rd";
            return "4th";
        }
    }
}
