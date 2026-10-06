using Tag.Gameplay;
using Tag.Local;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Corner minimap painted from Mega Park layout data into one texture.
    /// No second camera. M and gamepad Select toggle it (TagInputActions Minimap).
    /// </summary>
    public class ParkMinimap : MonoBehaviour
    {
        public const int TexSize = 256;
        const int Margin = 16;

        Texture2D _tex;
        Color32[] _base;
        Color32[] _frame;
        int _w;
        int _h;
        float _mapW;
        float _mapD;
        bool _show = true;

        void Start()
        {
            _mapW = ParkArena.MapW;
            _mapD = ParkArena.MapD;
            _w = TexSize;
            _h = Mathf.RoundToInt(TexSize * (_mapD / _mapW));
            if (_h < 1) _h = 1;
            if (_h > TexSize) _h = TexSize;
            _tex = new Texture2D(_w, _h, TextureFormat.RGBA32, false)
            {
                name = "ParkMinimap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _base = new Color32[_w * _h];
            _frame = new Color32[_w * _h];
            PaintBase();
            _tex.SetPixels32(_base);
            _tex.Apply(false);
        }

        void OnDestroy()
        {
            if (_tex != null)
                Destroy(_tex);
        }

        void Update()
        {
            if (Pressed())
                _show = !_show;
        }

        void OnGUI()
        {
            if (!_show || _tex == null)
                return;
            StampActors();
            _tex.SetPixels32(_frame);
            _tex.Apply(false);
            float pw = 200f;
            float ph = pw * (_h / (float)_w);
            var rect = new Rect(Screen.width - pw - Margin, Screen.height - ph - Margin, pw, ph);
            GUI.DrawTexture(rect, _tex, ScaleMode.StretchToFill, false);
        }

        // TagInputActions Minimap binds Keyboard/m and Gamepad/select.
        // The pawn rig reads the legacy manager, so this poll is the one edge.
        static bool Pressed()
        {
            if (Input.GetKeyDown(KeyCode.M)) return true;
            if (Input.GetKeyDown(KeyCode.JoystickButton6)) return true;
            return false;
        }

        void PaintBase()
        {
            Color32 mulch = Swatch("mulch");
            for (int i = 0; i < _base.Length; i++)
                _base[i] = mulch;

            var boxes = new System.Collections.Generic.Dictionary<string, Box>();
            MegaParkP1Layout.Solid[] solids = ParkArena.IsPocket
                ? PocketParkLayout.BuildSolids()
                : MegaParkP1Layout.BuildSolids();
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind == "fence") continue;
                if (s.Kind == "ground" && s.Mat != "sand") continue;
                if (!ZoneMat(s.Mat)) continue;
                string key = s.Zone + "|" + s.Mat;
                if (!boxes.TryGetValue(key, out Box box))
                    box = new Box { Mat = s.Mat };
                Expand(ref box, s.X - s.Sx * 0.5f, s.Z - s.Sz * 0.5f, s.X + s.Sx * 0.5f, s.Z + s.Sz * 0.5f);
                boxes[key] = box;
            }

            foreach (Box box in boxes.Values)
            {
                float area = (box.X1 - box.X0) * (box.Z1 - box.Z0);
                if (area < 8f) continue;
                Fill(box.X0, box.Z0, box.X1, box.Z1, Swatch(box.Mat), 150);
            }

            Color32 fence = Swatch("fence");
            fence.r = (byte)Mathf.Min(255, fence.r + 80);
            fence.g = (byte)Mathf.Min(255, fence.g + 80);
            fence.b = (byte)Mathf.Min(255, fence.b + 80);
            Outline(0f, 0f, _mapW, _mapD, fence);

            Color32 pad = new Color32(255, 255, 255, 255);
            MegaParkP1Layout.PadSpot[] pads = ParkArena.IsPocket
                ? PocketParkLayout.LaunchPads
                : MegaParkP1Layout.LaunchPads;
            for (int i = 0; i < pads.Length; i++)
                Diamond(pads[i].X, pads[i].Z, 3, pad);

            Color32 zip = Swatch("zip");
            MegaParkP1Layout.ZipLineSpot[] zips = ParkArena.IsPocket
                ? PocketParkLayout.ZipLines
                : MegaParkP1Layout.ZipLines;
            for (int i = 0; i < zips.Length; i++)
            {
                MegaParkP1Layout.ZipLineSpot z = zips[i];
                Line(z.Ax, z.Az, z.Bx, z.Bz, zip);
                Diamond(z.Ax, z.Az, 2, zip);
            }
        }

        void StampActors()
        {
            System.Array.Copy(_base, _frame, _base.Length);
            ItController[] pawns = UnityEngine.Object.FindObjectsByType<ItController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var white = new Color32(255, 255, 255, 255);
            var orange = new Color32(255, 150, 40, 255);
            var red = new Color32(255, 48, 40, 255);
            for (int i = 0; i < pawns.Length; i++)
            {
                ItController pawn = pawns[i];
                if (pawn == null) continue;
                string name = pawn.gameObject.name;
                bool dummy = name == LocalPlayerSpawner.OpponentPawnName || name.StartsWith("Dummy");
                bool human = name == LocalPlayerSpawner.SoloPawnName || name.StartsWith("Player");
                if (!dummy && !human) continue;
                Vector3 p = pawn.transform.position;
                // Markers land on the frame copy so the baked park stays put.
                DotFrame(p.x, p.z, dummy ? orange : white, 2);
                if (pawn.IsIt)
                    RingFrame(p.x, p.z, red, 4);
            }
        }

        static bool ZoneMat(string mat)
        {
            switch (mat)
            {
                case "soft":
                case "pad":
                case "blue":
                case "merry":
                case "amber":
                case "yellow":
                case "swing":
                case "army":
                case "knight":
                case "kick":
                case "sand":
                case "hop":
                case "field":
                case "cover":
                case "plate":
                case "concrete":
                case "steel":
                    return true;
                default:
                    return false;
            }
        }

        static Color32 Swatch(string mat)
        {
            MegaParkP1Layout.TryLook(mat, out float r, out float g, out float b, out _, out _);
            return new Color32((byte)(r * 255f), (byte)(g * 255f), (byte)(b * 255f), 255);
        }

        void Fill(float x0, float z0, float x1, float z1, Color32 src, byte a)
        {
            int px0 = X(Mathf.Min(x0, x1));
            int px1 = X(Mathf.Max(x0, x1));
            int py0 = Y(Mathf.Min(z0, z1));
            int py1 = Y(Mathf.Max(z0, z1));
            for (int y = py0; y <= py1; y++)
            {
                int row = y * _w;
                for (int x = px0; x <= px1; x++)
                    _base[row + x] = Blend(_base[row + x], src, a);
            }
        }

        void Outline(float x0, float z0, float x1, float z1, Color32 c)
        {
            Line(x0, z0, x1, z0, c);
            Line(x0, z1, x1, z1, c);
            Line(x0, z0, x0, z1, c);
            Line(x1, z0, x1, z1, c);
        }

        void Line(float x0, float z0, float x1, float z1, Color32 c)
        {
            int ax = X(x0);
            int ay = Y(z0);
            int bx = X(x1);
            int by = Y(z1);
            int dx = Mathf.Abs(bx - ax);
            int dy = Mathf.Abs(by - ay);
            int sx = ax < bx ? 1 : -1;
            int sy = ay < by ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                Plot(_base, ax, ay, c);
                Plot(_base, ax + 1, ay, c);
                if (ax == bx && ay == by) break;
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; ax += sx; }
                if (e2 < dx) { err += dx; ay += sy; }
            }
        }

        void Diamond(float x, float z, int r, Color32 c)
        {
            int cx = X(x);
            int cy = Y(z);
            for (int y = -r; y <= r; y++)
            {
                int span = r - Mathf.Abs(y);
                for (int x0 = -span; x0 <= span; x0++)
                    Plot(_base, cx + x0, cy + y, c);
            }
        }

        void DotFrame(float x, float z, Color32 c, int r)
        {
            int cx = X(x);
            int cy = Y(z);
            for (int y = -r; y <= r; y++)
            {
                for (int x0 = -r; x0 <= r; x0++)
                    Plot(_frame, cx + x0, cy + y, c);
            }
        }

        void RingFrame(float x, float z, Color32 c, int r)
        {
            int cx = X(x);
            int cy = Y(z);
            int r2 = r * r;
            int inner = (r - 1) * (r - 1);
            for (int y = -r; y <= r; y++)
            {
                for (int x0 = -r; x0 <= r; x0++)
                {
                    int d = x0 * x0 + y * y;
                    if (d <= r2 && d >= inner)
                        Plot(_frame, cx + x0, cy + y, c);
                }
            }
        }

        void Plot(Color32[] pix, int x, int y, Color32 c)
        {
            if ((uint)x >= (uint)_w || (uint)y >= (uint)_h) return;
            pix[y * _w + x] = c;
        }

        int X(float worldX)
        {
            int x = Mathf.RoundToInt(worldX / _mapW * (_w - 1));
            if (x < 0) return 0;
            if (x >= _w) return _w - 1;
            return x;
        }

        int Y(float worldZ)
        {
            int y = Mathf.RoundToInt(worldZ / _mapD * (_h - 1));
            if (y < 0) return 0;
            if (y >= _h) return _h - 1;
            return y;
        }

        static Color32 Blend(Color32 dst, Color32 src, byte a)
        {
            int inv = 255 - a;
            return new Color32(
                (byte)((src.r * a + dst.r * inv) / 255),
                (byte)((src.g * a + dst.g * inv) / 255),
                (byte)((src.b * a + dst.b * inv) / 255),
                255);
        }

        struct Box
        {
            public float X0, Z0, X1, Z1;
            public string Mat;
            public bool Any;
        }

        static void Expand(ref Box box, float x0, float z0, float x1, float z1)
        {
            if (!box.Any)
            {
                box.X0 = x0;
                box.Z0 = z0;
                box.X1 = x1;
                box.Z1 = z1;
                box.Any = true;
                return;
            }
            if (x0 < box.X0) box.X0 = x0;
            if (z0 < box.Z0) box.Z0 = z0;
            if (x1 > box.X1) box.X1 = x1;
            if (z1 > box.Z1) box.Z1 = z1;
        }
    }
}
