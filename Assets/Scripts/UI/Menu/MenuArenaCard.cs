using Tag.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Arena name stays on the card. Size is the fence footprint. The feel
    /// line and the top-down both come from that park's layout arrays.
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
            if (id == ParkArena.Stack)
            {
                int mid = (int)(StackYardLayout.MidY + 0.5f);
                int roof = (int)(StackYardLayout.RoofY + 0.5f);
                return "Decks at " + mid.ToString() + " m and roofs at " + roof.ToString() + " m.";
            }
            MegaParkP1Layout.PadSpot[] pads = Pads(id);
            MegaParkP1Layout.ZipLineSpot[] zips = Zips(id);
            int pc = pads != null ? pads.Length : 0;
            int zc = zips != null ? zips.Length : 0;
            return pc.ToString() + " pads and " + zc.ToString() + " zips.";
        }

        public static string Blurb(int id)
        {
            return Size(id) + "\n" + Flavor(id);
        }

        public static void Counts(int id, out int pads, out int zips)
        {
            MegaParkP1Layout.PadSpot[] padList = Pads(id);
            MegaParkP1Layout.ZipLineSpot[] zipList = Zips(id);
            pads = padList != null ? padList.Length : 0;
            zips = zipList != null ? zipList.Length : 0;
        }

        public static void Paint(Transform frame, int id)
        {
            if (frame == null) return;
            RectTransform rt = frame as RectTransform;
            float w = rt != null ? rt.sizeDelta.x : 240f;
            float h = rt != null ? rt.sizeDelta.y : 180f;
            if (w < 40f) w = 240f;
            if (h < 40f) h = 180f;
            PaintAt(frame, id, 0f, 0f, w, h);
        }

        public static void PaintAt(Transform frame, int id, float x, float y, float w, float h)
        {
            if (frame == null) return;
            if (w < 24f) w = 24f;
            if (h < 24f) h = 24f;
            for (int i = frame.childCount - 1; i >= 0; i--)
            {
                Transform child = frame.GetChild(i);
                if (child.name == "Map") Object.DestroyImmediate(child.gameObject);
            }
            ParkArena.Containment(id, out float mapW, out float mapD, out _);
            if (mapW < 1f) mapW = 1f;
            if (mapD < 1f) mapD = 1f;
            RectTransform map = MenuWidgets.Place(frame, "Map", x, y, w, h);
            Image plate = map.gameObject.AddComponent<Image>();
            plate.color = new Color(0.04f, 0.14f, 0.26f, 0.92f);
            plate.raycastTarget = false;
            float inset = w < 80f ? 4f : 8f;
            float innerW = w - inset * 2f;
            float innerH = h - inset * 2f;
            if (innerW < 8f) innerW = 8f;
            if (innerH < 8f) innerH = 8f;
            RectTransform ground = MenuWidgets.Place(map, "Ground", inset, inset, innerW, innerH);
            Image grass = ground.gameObject.AddComponent<Image>();
            grass.color = new Color(0.16f, 0.42f, 0.28f, 1f);
            grass.raycastTarget = false;
            MegaParkP1Layout.Pt[] loop = Loop(id);
            if (loop != null && loop.Length > 1)
            {
                for (int i = 0; i < loop.Length; i++)
                {
                    MegaParkP1Layout.Pt a = loop[i];
                    MegaParkP1Layout.Pt b = loop[(i + 1) % loop.Length];
                    Line(map, a.X, a.Z, b.X, b.Z, mapW, mapD, inset, innerW, innerH, new Color(0.93f, 0.95f, 0.88f, 0.9f), 3f);
                }
            }
            if (id == ParkArena.Stack)
            {
                StackYardLayout.RoofMark[] marks = StackYardLayout.RoofAccess;
                int n = marks != null ? marks.Length : 0;
                for (int i = 0; i < n; i++)
                {
                    float dot = marks[i].Y >= StackYardLayout.RoofY - 0.2f ? 14f : 11f;
                    Color tone = marks[i].Y >= StackYardLayout.RoofY - 0.2f
                        ? new Color(0.92f, 0.55f, 0.22f, 1f)
                        : marks[i].Y >= StackYardLayout.MidY - 0.2f
                            ? new Color(0.72f, 0.62f, 0.38f, 1f)
                            : new Color(0.25f, 0.55f, 0.32f, 1f);
                    Dot(map, marks[i].X, marks[i].Z, mapW, mapD, inset, innerW, innerH, tone, dot);
                }
            }
            float[] markX;
            float[] markZ;
            int markN = Marks(id, out markX, out markZ);
            float markSize = w < 100f ? 5f : 9f;
            for (int m = 0; m < markN; m++)
                Dot(map, markX[m], markZ[m], mapW, mapD, inset, innerW, innerH, new Color(0.96f, 0.93f, 0.84f, 1f), markSize);
            MegaParkP1Layout.ZipLineSpot[] zips = Zips(id);
            int zn = zips != null ? zips.Length : 0;
            float zipThick = w < 100f ? 1.5f : 2.5f;
            for (int i = 0; i < zn; i++)
                Line(map, zips[i].Ax, zips[i].Az, zips[i].Bx, zips[i].Bz, mapW, mapD, inset, innerW, innerH, MenuTheme.Gold, zipThick);
            MegaParkP1Layout.PadSpot[] pads = Pads(id);
            int pn = pads != null ? pads.Length : 0;
            float padSize = w < 100f ? 5f : 8f;
            for (int p = 0; p < pn; p++)
                Dot(map, pads[p].X, pads[p].Z, mapW, mapD, inset, innerW, innerH, MenuTheme.Gold, padSize);
            MegaParkP1Layout.SpawnPad[] spawns = Spawns(id);
            int seats = spawns != null ? spawns.Length : 0;
            if (seats > 4) seats = 4;
            for (int s = 0; s < seats; s++)
                Dot(map, spawns[s].X, spawns[s].Z, mapW, mapD, inset, innerW, innerH, MenuTheme.Seat(s), 10f);
        }

        static readonly float[] MegaMarkX = { 20f, 15.2f, 40f, 84f, 109f, 148.2f, 54f, 150.4f };
        static readonly float[] MegaMarkZ = { 10f, 77.4f, 36.6f, 95f, 58f, 28.8f, 34.35f, 5.2f };
        static readonly float[] PocketMarkX = { 46f, 66f, 12f, 14f };
        static readonly float[] PocketMarkZ = { 42f, 32f, 22f, 40f };
        static readonly float[] StackMarkX = { 14f, 96f, 14f, 96f };
        static readonly float[] StackMarkZ = { 18f, 18f, 56f, 56f };

        static int Marks(int id, out float[] xs, out float[] zs)
        {
            if (id == ParkArena.Pocket)
            {
                xs = PocketMarkX;
                zs = PocketMarkZ;
                return PocketMarkX.Length;
            }
            if (id == ParkArena.Stack)
            {
                xs = StackMarkX;
                zs = StackMarkZ;
                return StackMarkX.Length;
            }
            xs = MegaMarkX;
            zs = MegaMarkZ;
            return MegaMarkX.Length;
        }

        static MegaParkP1Layout.Pt[] Loop(int id)
        {
            if (id == ParkArena.Pocket) return PocketParkLayout.LoopCcw;
            if (id == ParkArena.Stack) return StackYardLayout.LoopCcw;
            return MegaParkP1Layout.LoopCcw;
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

        static MegaParkP1Layout.ZipLineSpot[] Zips(int id)
        {
            if (id == ParkArena.Pocket) return PocketParkLayout.ZipLines;
            if (id == ParkArena.Stack) return StackYardLayout.ZipLines;
            return MegaParkP1Layout.ZipLines;
        }

        static void Line(Transform map, float x0, float z0, float x1, float z1, float mapW, float mapD, float inset, float innerW, float innerH, Color color, float thick)
        {
            float px0, py0, px1, py1;
            Map(x0, z0, mapW, mapD, inset, innerW, innerH, out px0, out py0);
            Map(x1, z1, mapW, mapD, inset, innerW, innerH, out px1, out py1);
            float ax = px1 - px0;
            float ay = -(py1 - py0);
            float len = Mathf.Sqrt(ax * ax + ay * ay);
            if (len < 1f) return;
            float ang = Mathf.Atan2(ay, ax) * Mathf.Rad2Deg;
            RectTransform rt = MenuWidgets.Place(map, "Seg", px0, py0, len, thick);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(px0, -py0);
            rt.localEulerAngles = new Vector3(0f, 0f, ang);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        static void Dot(Transform map, float x, float z, float mapW, float mapD, float inset, float innerW, float innerH, Color color, float size)
        {
            float px, py;
            Map(x, z, mapW, mapD, inset, innerW, innerH, out px, out py);
            px -= size * 0.5f;
            py -= size * 0.5f;
            if (px < 2f) px = 2f;
            if (py < 2f) py = 2f;
            RectTransform rt = MenuWidgets.Place(map, "Dot", px, py, size, size);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        static void Map(float x, float z, float mapW, float mapD, float inset, float innerW, float innerH, out float px, out float py)
        {
            float u = x / mapW;
            float v = z / mapD;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            if (v < 0f) v = 0f;
            if (v > 1f) v = 1f;
            px = inset + u * innerW;
            py = inset + (1f - v) * innerH;
        }
    }
}
