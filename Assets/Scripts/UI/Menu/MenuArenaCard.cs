using Tag.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Arena name stays on the card. This adds the measured size, a short
    /// flavor line, and a top-down of the spawns and pads from the layout data.
    /// </summary>
    public static class MenuArenaCard
    {
        public static string Size(int id)
        {
            ParkArena.Containment(id, out float w, out float d, out _);
            int iw = (int)(w + 0.5f);
            int idm = (int)(d + 0.5f);
            return iw.ToString() + " x " + idm.ToString() + " m";
        }

        public static string Flavor(int id)
        {
            if (id == ParkArena.Pocket) return "A tight park on the same pieces.";
            if (id == ParkArena.Stack) return "Decks at 6 m and roofs at 12 m.";
            return "Pads, zips, and the long loop.";
        }

        public static string Blurb(int id)
        {
            return Size(id) + "\n" + Flavor(id);
        }

        public static void Paint(Transform frame, int id)
        {
            if (frame == null) return;
            for (int i = frame.childCount - 1; i >= 0; i--)
            {
                Transform child = frame.GetChild(i);
                if (child.name == "Map") Object.DestroyImmediate(child.gameObject);
            }
            ParkArena.Containment(id, out float mapW, out float mapD, out _);
            if (mapW < 1f) mapW = 1f;
            if (mapD < 1f) mapD = 1f;
            RectTransform map = MenuWidgets.Place(frame, "Map", 700f, 250f, 240f, 180f);
            Image plate = map.gameObject.AddComponent<Image>();
            plate.color = new Color(0.04f, 0.14f, 0.26f, 0.92f);
            plate.raycastTarget = false;
            RectTransform ground = MenuWidgets.Place(map, "Ground", 12f, 12f, 216f, 156f);
            Image grass = ground.gameObject.AddComponent<Image>();
            grass.color = new Color(0.16f, 0.48f, 0.26f, 1f);
            grass.raycastTarget = false;
            MegaParkP1Layout.SpawnPad[] spawns = Spawns(id);
            int seats = spawns != null ? spawns.Length : 0;
            if (seats > 4) seats = 4;
            for (int s = 0; s < seats; s++)
                Dot(map, spawns[s].X, spawns[s].Z, mapW, mapD, MenuTheme.Seat(s), 16f);
            MegaParkP1Layout.PadSpot[] pads = Pads(id);
            int n = pads != null ? pads.Length : 0;
            if (n > 6) n = 6;
            for (int p = 0; p < n; p++)
                Dot(map, pads[p].X, pads[p].Z, mapW, mapD, MenuTheme.Gold, 10f);
        }

        static MegaParkP1Layout.SpawnPad[] Spawns(int id)
        {
            if (id == ParkArena.Pocket) return PocketParkLayout.Spawns;
            if (id == ParkArena.Stack) return StackYardLayout.Spawns;
            return MegaParkP1Layout.Spawns;
        }

        static MegaParkP1Layout.PadSpot[] Pads(int id)
        {
            if (id == ParkArena.Pocket) return PocketParkLayout.LaunchPads;
            if (id == ParkArena.Stack) return StackYardLayout.LaunchPads;
            return MegaParkP1Layout.LaunchPads;
        }

        static void Dot(Transform map, float x, float z, float mapW, float mapD, Color color, float size)
        {
            float px = 12f + (x / mapW) * 216f - size * 0.5f;
            float py = 12f + (1f - z / mapD) * 156f - size * 0.5f;
            if (px < 8f) px = 8f;
            if (py < 8f) py = 8f;
            RectTransform rt = MenuWidgets.Place(map, "Dot", px, py, size, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }
    }
}
