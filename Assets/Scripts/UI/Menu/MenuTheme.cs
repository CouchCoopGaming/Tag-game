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
        public static readonly Color Ink = new Color(0.04f, 0.07f, 0.12f, 1f);
        public static readonly Color Navy = new Color(0.08f, 0.14f, 0.26f, 1f);
        public static readonly Color Panel = new Color(0.12f, 0.24f, 0.46f, 1f);
        public static readonly Color PanelHot = new Color(0.18f, 0.38f, 0.72f, 1f);
        public static readonly Color Gold = new Color(0.98f, 0.78f, 0.22f, 1f);
        public static readonly Color Cream = new Color(0.96f, 0.95f, 0.90f, 1f);
        public static readonly Color Mute = new Color(0.62f, 0.68f, 0.76f, 1f);
        public static readonly Color Dim = new Color(0.03f, 0.05f, 0.09f, 0.62f);
        public static readonly Color Ready = new Color(0.25f, 0.85f, 0.48f, 1f);
        public static readonly Color Off = new Color(0.22f, 0.24f, 0.28f, 1f);

        static readonly Color[] Fallback =
        {
            new Color(0.95f, 0.28f, 0.30f, 1f),
            new Color(0.28f, 0.62f, 0.98f, 1f),
            new Color(0.24f, 0.82f, 0.42f, 1f),
            new Color(0.98f, 0.78f, 0.22f, 1f)
        };

        static Font _font;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
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
