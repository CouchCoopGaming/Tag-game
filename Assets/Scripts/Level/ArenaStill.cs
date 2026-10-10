using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Headless lit stills. Boxes are rasterized with the afternoon sun (pitch 28),
    /// a shadow map, and hemisphere ambient. No Unity camera and no new move.
    /// </summary>
    public static partial class ArenaStill
    {
        public const int Width = 1280;
        public const int Height = 720;

        struct Tri
        {
            public float X0, Y0, Z0, X1, Y1, Z1, X2, Y2, Z2;
            public float Nx, Ny, Nz;
            public float R, G, B, A;
        }

        struct V
        {
            public float Wx, Wy, Wz, Cx, Cy, Cz;
        }

        public static void WritePair(
            string park,
            MegaParkP1Layout.Solid[] solids,
            MegaParkP1Layout.Ramp[] ramps,
            MegaParkP1Layout.Dress[] dress,
            MegaParkP1Layout.PadSpot[] pads,
            MegaParkP1Layout.ZipLineSpot[] zips,
            MegaParkP1Layout.SpawnPad eyeSpawn,
            float mapW,
            float mapD,
            StringBuilder fail)
        {
            string dir = RepoDocs();
            if (dir == null)
            {
                fail.Append(park).Append(" stills folder; ");
                return;
            }
            string folder = Path.Combine(dir, "ArenaStills");
            Directory.CreateDirectory(folder);
            var tris = new List<Tri>(1024);
            if (solids != null)
            {
                for (int i = 0; i < solids.Length; i++)
                {
                    MegaParkP1Layout.Solid s = solids[i];
                    if (s.Kind == "mark") continue;
                    if (s.Kind == "fence")
                    {
                        AddRailFence(tris, s);
                        continue;
                    }
                    if (ContainerDeck(s))
                    {
                        AddContainerStack(tris, s);
                        continue;
                    }
                    Albedo(s.Mat, out float r, out float g, out float b);
                    AddBox(tris, s.X, s.Y, s.Z, s.Sx, s.Sy, s.Sz, r, g, b);
                    if (s.Name.StartsWith("Ship_", StringComparison.Ordinal))
                        AddCorrugation(tris, s, r, g, b);
                }
            }
            if (dress != null)
            {
                for (int i = 0; i < dress.Length; i++)
                {
                    MegaParkP1Layout.Dress d = dress[i];
                    Albedo(d.Mat, out float r, out float g, out float b);
                    AddBox(tris, d.X, d.Y, d.Z, d.Sx, d.Sy, d.Sz, r, g, b);
                }
            }
            if (ramps != null)
            {
                for (int i = 0; i < ramps.Length; i++)
                    AddRamp(tris, ramps[i]);
            }
            if (pads != null)
            {
                for (int i = 0; i < pads.Length; i++)
                    AddPad(tris, pads[i]);
            }
            if (zips != null)
            {
                for (int i = 0; i < zips.Length; i++)
                    AddZip(tris, zips[i]);
            }
            AddZoneMarks(tris, park, solids);

            float figX = mapW * 0.38f;
            float figZ = mapD * 0.28f;
            AddFigure(tris, figX, 0f, figZ);

            string overview = Path.Combine(folder, park + "_Overview.png");
            string eye = Path.Combine(folder, park + "_Eye.png");
            float span = Math.Max(mapW, mapD);
            // Above the yard, a little south of centre. The old corner camera
            // sat in the treeline, so poles and trees drew a dark bar across the plate.
            // Mega is wide enough that the same framing clipped the rim, so it sits higher.
            bool wide = span > 140f;
            float camY = span * (wide ? 1.82f : 1.48f);
            float camZ = mapD * 0.50f - span * (wide ? 0.26f : 0.16f);
            float fov = wide ? 50f : 40f;
            Render(tris, overview, mapW, mapD,
                mapW * 0.50f, camY, camZ,
                mapW * 0.50f, 1.2f, mapD * 0.50f,
                fov, false);
            float ex = eyeSpawn.X;
            float ez = eyeSpawn.Z;
            float tx = mapW * 0.55f;
            float tz = mapD * 0.55f;
            float lookX = tx - ex;
            float lookZ = tz - ez;
            float lookM = (float)Math.Sqrt(lookX * lookX + lookZ * lookZ);
            if (lookM < 0.1f) lookM = 1f;
            lookX /= lookM;
            lookZ /= lookM;
            float sideX = lookZ;
            float sideZ = -lookX;
            AddFigure(tris, ex + lookX * 8f + sideX * 2.2f, 0f, ez + lookZ * 8f + sideZ * 2.2f);
            Render(tris, eye, mapW, mapD,
                ex, 1.65f, ez,
                tx, 3.2f, tz,
                68f, true);
            float edgeX = mapW * 0.62f;
            float edgeZ = 2.35f;
            AddFigure(tris, edgeX - 2.6f, 0f, 0.85f);
            AddFenceShimmer(tris, mapW, mapD, edgeX, edgeZ);
            string edge = Path.Combine(folder, park + "_Edge.png");
            Render(tris, edge, mapW, mapD,
                edgeX, 1.65f, edgeZ,
                edgeX, 3.4f, -16f,
                68f, true);
            Check(overview, park + " overview", fail);
            Check(eye, park + " eye", fail);
            Check(edge, park + " edge", fail);
        }

        /// <summary>
        /// Gameplay chase camera: 4.99 m behind the feet, 2.92 m up, look height 1.25, fov 78.
        /// Yaw 0 faces +Z. The plate has no figure, so a composite can stand the runners in.
        /// </summary>
        public static void WriteChase(string path, int arena, float feetX, float feetZ, float yawDeg)
        {
            List<Tri> tris = Gather(arena);
            float mapW = arena == ParkArena.Stack ? StackYardLayout.MapW
                : arena == ParkArena.Pocket ? PocketParkLayout.MapW
                : MegaParkP1Layout.MapW;
            float mapD = arena == ParkArena.Stack ? StackYardLayout.MapD
                : arena == ParkArena.Pocket ? PocketParkLayout.MapD
                : MegaParkP1Layout.MapD;
            float yaw = yawDeg * (float)(Math.PI / 180.0);
            float fx = (float)Math.Sin(yaw);
            float fz = (float)Math.Cos(yaw);
            float lx = fz;
            float lz = -fx;
            const float back = 4.992f;
            const float side = 0.40f;
            float ex = feetX - fx * back - lx * side;
            float ey = 2.921f;
            float ez = feetZ - fz * back - lz * side;
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            Render(tris, path, mapW, mapD, ex, ey, ez, feetX, 1.25f, feetZ, 78f, true);
        }

        static void AddRailFence(List<Tri> tris, MegaParkP1Layout.Solid s)
        {
            MegaParkP1Layout.TryLook("fence", out float r, out float g, out float b, out _, out _);
            float rail = MegaParkP1Layout.FenceRail;
            bool alongX = s.Sx >= s.Sz;
            float length = alongX ? s.Sx : s.Sz;
            float origin = alongX ? (s.X - s.Sx * 0.5f) : (s.Z - s.Sz * 0.5f);
            float fixedC = alongX ? s.Z : s.X;
            float post = 4.6f;
            int n = (int)(length / post);
            if (n < 2) n = 2;
            for (int i = 0; i <= n; i++)
            {
                float u = origin + length * (i / (float)n);
                Place(alongX, u, fixedC, out float px, out float pz);
                AddBox(tris, px, rail * 0.5f, pz, 0.16f, rail, 0.16f, r, g, b);
            }
            float[] rails = { 0.42f, 1.48f, rail - 0.1f };
            for (int i = 0; i < rails.Length; i++)
            {
                float y = rails[i];
                if (alongX)
                    AddBox(tris, s.X, y, fixedC, length, 0.08f, 0.08f, r, g, b);
                else
                    AddBox(tris, fixedC, y, s.Z, 0.08f, 0.08f, length, r, g, b);
            }
            float picket = 1.55f;
            int wires = (int)(length / picket);
            if (wires < 2) wires = 2;
            if (wires > 80) wires = 80;
            for (int i = 0; i < wires; i++)
            {
                float u = origin + length * ((i + 0.5f) / wires);
                Place(alongX, u, fixedC, out float px, out float pz);
                AddBox(tris, px, rail * 0.48f, pz, 0.045f, rail * 0.86f, 0.045f, r * 0.85f, g * 0.85f, b * 0.85f);
            }
        }

        static void Place(bool alongX, float u, float fixedC, out float x, out float z)
        {
            if (alongX)
            {
                x = u;
                z = fixedC;
            }
            else
            {
                x = fixedC;
                z = u;
            }
        }

        static void AddFenceShimmer(List<Tri> tris, float mapW, float mapD, float eyeX, float eyeZ)
        {
            float reach = MegaParkP1Layout.FenceShimmer;
            float rail = MegaParkP1Layout.FenceRail;
            float top = rail + 12f;
            float y = (rail + top) * 0.5f;
            float sy = top - rail;
            float sr = 0.45f;
            float sg = 0.72f;
            float sb = 0.68f;
            const float a = 0.18f;
            if (eyeZ <= reach)
                AddBoxAlpha(tris, mapW * 0.5f, y, 0f, mapW, sy, 0.06f, sr, sg, sb, a);
            if (mapD - eyeZ <= reach)
                AddBoxAlpha(tris, mapW * 0.5f, y, mapD, mapW, sy, 0.06f, sr, sg, sb, a);
            if (eyeX <= reach)
                AddBoxAlpha(tris, 0f, y, mapD * 0.5f, 0.06f, sy, mapD, sr, sg, sb, a);
            if (mapW - eyeX <= reach)
                AddBoxAlpha(tris, mapW, y, mapD * 0.5f, 0.06f, sy, mapD, sr, sg, sb, a);
        }

        static void Check(string path, string label, StringBuilder fail)
        {
            if (!File.Exists(path))
            {
                fail.Append(label).Append(" still missing; ");
                return;
            }
            var info = new FileInfo(path);
            if (info.Length < 12000)
                fail.Append(label).Append(" still is too small; ");
            if (!PngSize(path, out int w, out int h) || w < Width || h < Height)
                fail.Append(label).Append(" still is under 1280x720; ");
        }

        static void AddZoneMarks(List<Tri> tris, string park, MegaParkP1Layout.Solid[] solids)
        {
            int arena = park == "PocketPark" ? ParkArena.Pocket : park == "StackYard" ? ParkArena.Stack : ParkArena.Mega;
            ZoneReadability.Mark[] marks = ZoneReadability.Fill(arena, solids);
            for (int i = 0; i < marks.Length; i++)
            {
                ZoneReadability.Mark m = marks[i];
                Albedo(m.Mat, out float r, out float g, out float b);
                AddBox(tris, m.X, m.Y, m.Z, m.Sx, m.Sy, m.Sz, r, g, b);
            }
        }

        static void Albedo(string mat, out float r, out float g, out float b)
        {
            if (!MegaParkP1Layout.TryLook(mat, out r, out g, out b, out _, out _))
            {
                r = 0.45f;
                g = 0.42f;
                b = 0.38f;
            }
        }

        static void AddPad(List<Tri> tris, MegaParkP1Layout.PadSpot pad)
        {
            AddBox(tris, pad.X, pad.Y + 0.12f, pad.Z, 1.7f, 0.18f, 1.7f, 1f, 0.46f, 0.08f);
            float mag = (float)Math.Sqrt(pad.DirX * pad.DirX + pad.DirZ * pad.DirZ);
            if (mag < 0.1f) mag = 1f;
            float dx = pad.DirX / mag;
            float dz = pad.DirZ / mag;
            AddBox(tris, pad.X + dx * 0.7f, pad.Y + 0.28f, pad.Z + dz * 0.7f, 0.28f, 0.16f, 0.28f, 1f, 0.82f, 0.35f);
        }

        static void AddZip(List<Tri> tris, MegaParkP1Layout.ZipLineSpot zip)
        {
            MegaParkP1Layout.TryLook("zip", out float r, out float g, out float b, out _, out _);
            AddBox(tris, zip.Ax, zip.Ay, zip.Az, 0.35f, 0.35f, 0.35f, r, g, b);
            AddBox(tris, zip.Bx, zip.By, zip.Bz, 0.35f, 0.35f, 0.35f, r, g, b);
            float dx = zip.Bx - zip.Ax;
            float dy = zip.By - zip.Ay;
            float dz = zip.Bz - zip.Az;
            float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            int n = (int)(len / 3.2f);
            if (n < 2) n = 2;
            if (n > 28) n = 28;
            for (int i = 1; i < n; i++)
            {
                float u = i / (float)n;
                AddBox(tris, zip.Ax + dx * u, zip.Ay + dy * u, zip.Az + dz * u, 0.22f, 0.22f, 0.22f, r, g, b);
            }
        }

        static void AddFigure(List<Tri> tris, float x, float y, float z)
        {
            AddBox(tris, x, y + 0.46f, z, 0.34f, 0.92f, 0.24f, 0.12f, 0.14f, 0.18f);
            AddBox(tris, x, y + 1.22f, z, 0.46f, 0.62f, 0.26f, 0.86f, 0.16f, 0.14f);
            AddBox(tris, x, y + 1.66f, z, 0.24f, 0.28f, 0.24f, 0.93f, 0.74f, 0.60f);
        }

        static void AddRamp(List<Tri> tris, MegaParkP1Layout.Ramp ramp)
        {
            Albedo(ramp.Mat, out float r, out float g, out float b);
            float dx = ramp.X1 - ramp.X0;
            float dz = ramp.Z1 - ramp.Z0;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len < 0.05f) len = 1f;
            float px = -dz / len * ramp.Width * 0.5f;
            float pz = dx / len * ramp.Width * 0.5f;
            float y0 = ramp.Y0 + 0.08f;
            float y1 = ramp.Y1 + 0.08f;
            float ax = ramp.X0 + px, ay = y0, az = ramp.Z0 + pz;
            float bx = ramp.X0 - px, by = y0, bz = ramp.Z0 - pz;
            float cx = ramp.X1 - px, cy = y1, cz = ramp.Z1 - pz;
            float ex = ramp.X1 + px, ey = y1, ez = ramp.Z1 + pz;
            float ux = bx - ax, uy = by - ay, uz = bz - az;
            float vx = cx - ax, vy = cy - ay, vz = cz - az;
            float ny = uz * vx - ux * vz;
            if (ny < 0f)
            {
                AddTri(tris, ax, ay, az, ex, ey, ez, cx, cy, cz, r, g, b);
                AddTri(tris, ax, ay, az, cx, cy, cz, bx, by, bz, r, g, b);
            }
            else
            {
                AddTri(tris, ax, ay, az, bx, by, bz, cx, cy, cz, r, g, b);
                AddTri(tris, ax, ay, az, cx, cy, cz, ex, ey, ez, r, g, b);
            }
        }

        static bool ContainerDeck(MegaParkP1Layout.Solid s)
        {
            if (s.Kind != "block" || s.Sy < 2.4f) return false;
            return s.Name.StartsWith("Crate_", StringComparison.Ordinal)
                || s.Name.StartsWith("Stack_", StringComparison.Ordinal);
        }

        /// <summary>
        /// The tower slab stays one collider. The still paints it as two or three
        /// containers so the stack reads without a new chase cell.
        /// </summary>
        static void AddContainerStack(List<Tri> tris, MegaParkP1Layout.Solid s)
        {
            int stories = s.Sy >= 5f ? 3 : 2;
            float bot = s.Y - s.Sy * 0.5f;
            float h = s.Sy / stories;
            for (int i = 0; i < stories; i++)
            {
                string mat = StackMat(s.Mat, i);
                Albedo(mat, out float r, out float g, out float b);
                float y = bot + h * (i + 0.5f);
                AddBox(tris, s.X, y, s.Z, s.Sx, h, s.Sz, r, g, b);
                var band = s;
                band.Y = y;
                band.Sy = h;
                band.Mat = mat;
                AddCorrugation(tris, band, r, g, b);
            }
        }

        static string StackMat(string mat, int story)
        {
            if (mat == "amber" || mat == "pad")
            {
                if (story == 1) return "pad";
                return "amber";
            }
            if (story == 0) return mat == "concrete" ? "concrete" : "army";
            if (story == 1) return "knight";
            return "concrete";
        }

        static void AddCorrugation(List<Tri> tris, MegaParkP1Layout.Solid s, float r, float g, float b)
        {
            float sr = r * 0.38f;
            float sg = g * 0.38f;
            float sb = b * 0.38f;
            float span = s.Sy;
            if (span < 0.4f) return;
            int bands = (int)(span / 0.62f);
            if (bands < 2) bands = 2;
            if (bands > 6) bands = 6;
            float band = span / (bands * 4.5f);
            if (band < 0.06f) band = 0.06f;
            if (band > 0.14f) band = 0.14f;
            const float proud = 0.035f;
            float bot = s.Y - s.Sy * 0.5f;
            for (int i = 0; i < bands; i++)
            {
                float y = bot + span * ((i + 0.5f) / bands);
                AddBox(tris, s.X, y, s.Z - s.Sz * 0.5f - proud * 0.5f, s.Sx * 0.9f, band, proud, sr, sg, sb);
                AddBox(tris, s.X, y, s.Z + s.Sz * 0.5f + proud * 0.5f, s.Sx * 0.9f, band, proud, sr, sg, sb);
                AddBox(tris, s.X - s.Sx * 0.5f - proud * 0.5f, y, s.Z, proud, band, s.Sz * 0.9f, sr, sg, sb);
                AddBox(tris, s.X + s.Sx * 0.5f + proud * 0.5f, y, s.Z, proud, band, s.Sz * 0.9f, sr, sg, sb);
            }
        }

        static void AddBox(List<Tri> tris, float x, float y, float z, float sx, float sy, float sz, float r, float g, float b)
        {
            if (sx < 0.02f) sx = 0.02f;
            if (sy < 0.02f) sy = 0.02f;
            if (sz < 0.02f) sz = 0.02f;
            float hx = sx * 0.5f;
            float hy = sy * 0.5f;
            float hz = sz * 0.5f;
            Face(tris, x, y, z, hx, hy, hz, 0f, 1f, 0f, r, g, b);
            Face(tris, x, y, z, hx, hy, hz, 0f, -1f, 0f, r * 0.72f, g * 0.72f, b * 0.72f);
            Face(tris, x, y, z, hx, hy, hz, 1f, 0f, 0f, r, g, b);
            Face(tris, x, y, z, hx, hy, hz, -1f, 0f, 0f, r, g, b);
            Face(tris, x, y, z, hx, hy, hz, 0f, 0f, 1f, r, g, b);
            Face(tris, x, y, z, hx, hy, hz, 0f, 0f, -1f, r, g, b);
        }

        static void Face(List<Tri> tris, float x, float y, float z, float hx, float hy, float hz,
            float nx, float ny, float nz, float r, float g, float b)
        {
            float ax, ay, az, bx, by, bz;
            if (Math.Abs(ny) > 0.5f)
            {
                ax = hx; ay = 0f; az = 0f;
                bx = 0f; by = 0f; bz = hz;
            }
            else if (Math.Abs(nx) > 0.5f)
            {
                ax = 0f; ay = hy; az = 0f;
                bx = 0f; by = 0f; bz = hz;
            }
            else
            {
                ax = hx; ay = 0f; az = 0f;
                bx = 0f; by = hy; bz = 0f;
            }
            float extent = Math.Abs(nx) * hx + Math.Abs(ny) * hy + Math.Abs(nz) * hz;
            float cx = x + nx * extent;
            float cy = y + ny * extent;
            float cz = z + nz * extent;
            float x00 = cx - ax - bx, y00 = cy - ay - by, z00 = cz - az - bz;
            float x10 = cx - ax + bx, y10 = cy - ay + by, z10 = cz - az + bz;
            float x11 = cx + ax + bx, y11 = cy + ay + by, z11 = cz + az + bz;
            float x01 = cx + ax - bx, y01 = cy + ay - by, z01 = cz + az - bz;
            float ux = x10 - x00, uy = y10 - y00, uz = z10 - z00;
            float vx = x11 - x00, vy = y11 - y00, vz = z11 - z00;
            float cnx = uy * vz - uz * vy;
            float cny = uz * vx - ux * vz;
            float cnz = ux * vy - uy * vx;
            if (cnx * nx + cny * ny + cnz * nz < 0f)
            {
                float tx = x10, ty = y10, tz = z10;
                x10 = x01; y10 = y01; z10 = z01;
                x01 = tx; y01 = ty; z01 = tz;
            }
            AddTri(tris, x00, y00, z00, x10, y10, z10, x11, y11, z11, r, g, b);
            AddTri(tris, x00, y00, z00, x11, y11, z11, x01, y01, z01, r, g, b);
        }

        static void AddTri(List<Tri> tris, float x0, float y0, float z0, float x1, float y1, float z1, float x2, float y2, float z2, float r, float g, float b)
        {
            float ux = x1 - x0;
            float uy = y1 - y0;
            float uz = z1 - z0;
            float vx = x2 - x0;
            float vy = y2 - y0;
            float vz = z2 - z0;
            float nx = uy * vz - uz * vy;
            float ny = uz * vx - ux * vz;
            float nz = ux * vy - uy * vx;
            float m = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (m < 1e-6f) return;
            Tri t;
            t.X0 = x0; t.Y0 = y0; t.Z0 = z0;
            t.X1 = x1; t.Y1 = y1; t.Z1 = z1;
            t.X2 = x2; t.Y2 = y2; t.Z2 = z2;
            t.Nx = nx / m; t.Ny = ny / m; t.Nz = nz / m;
            t.R = r; t.G = g; t.B = b; t.A = 1f;
            tris.Add(t);
        }

        static void AddBoxAlpha(List<Tri> tris, float x, float y, float z, float sx, float sy, float sz, float r, float g, float b, float a)
        {
            int before = tris.Count;
            AddBox(tris, x, y, z, sx, sy, sz, r, g, b);
            for (int i = before; i < tris.Count; i++)
            {
                Tri t = tris[i];
                t.A = a;
                tris[i] = t;
            }
        }

        static bool _overview;

        static void Render(List<Tri> tris, string path, float mapW, float mapD,
            float ex, float ey, float ez, float tx, float ty, float tz, float fov, bool eyeLevel)
        {
            _overview = !eyeLevel;
            Sun(out float sx, out float sy, out float sz);
            float half = Math.Max(mapW, mapD) * 0.70f + 28f;
            var shadow = new float[768 * 768];
            for (int i = 0; i < shadow.Length; i++) shadow[i] = -1e20f;
            float cx = mapW * 0.5f;
            float cy = 6f;
            float cz = mapD * 0.5f;
            Basis(sx, sy, sz, out float rx, out float ry, out float rz, out float ux, out float uy, out float uz);
            for (int i = 0; i < tris.Count; i++)
            {
                if (tris[i].A < 0.99f) continue;
                if (_overview && NearCamera(tris[i], ex, ey, ez)) continue;
                ShadowTri(tris[i], shadow, 768, cx, cy, cz, rx, ry, rz, ux, uy, uz, sx, sy, sz, half);
            }

            var rgb = new byte[Width * Height * 3];
            var depth = new float[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                float v = 1f - y / (float)(Height - 1);
                float sky = eyeLevel ? 0.55f + v * 0.45f : 0.35f + v * 0.65f;
                byte br = (byte)(255f * (0.95f * (1f - sky) + 0.45f * sky));
                byte bg = (byte)(255f * (0.62f * (1f - sky) + 0.68f * sky));
                byte bb = (byte)(255f * (0.38f * (1f - sky) + 0.88f * sky));
                int row = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    int p = (row + x) * 3;
                    rgb[p] = br;
                    rgb[p + 1] = bg;
                    rgb[p + 2] = bb;
                    depth[row + x] = 1e20f;
                }
            }

            BasisFrom(ex, ey, ez, tx, ty, tz, out float fx, out float fy, out float fz, out float crx, out float cry, out float crz, out float cux, out float cuy, out float cuz);
            for (int i = 0; i < tris.Count; i++)
            {
                if (_overview && NearCamera(tris[i], ex, ey, ez)) continue;
                DrawTri(tris[i], rgb, depth, Width, Height, ex, ey, ez, fx, fy, fz, crx, cry, crz, cux, cuy, cuz, fov,
                    shadow, 768, cx, cy, cz, rx, ry, rz, ux, uy, uz, sx, sy, sz, half);
            }
            WritePng(path, rgb, Width, Height);
        }

        static bool NearCamera(Tri t, float ex, float ey, float ez)
        {
            const float reach = 15f;
            float r2 = reach * reach;
            if (Dist2(t.X0, t.Y0, t.Z0, ex, ey, ez) < r2) return true;
            if (Dist2(t.X1, t.Y1, t.Z1, ex, ey, ez) < r2) return true;
            if (Dist2(t.X2, t.Y2, t.Z2, ex, ey, ez) < r2) return true;
            return false;
        }

        static float Dist2(float x, float y, float z, float ex, float ey, float ez)
        {
            float dx = x - ex;
            float dy = y - ey;
            float dz = z - ez;
            return dx * dx + dy * dy + dz * dz;
        }

        static void Sun(out float x, out float y, out float z)
        {
            float pitch = MegaParkP1Layout.SunPitch * (float)(Math.PI / 180.0);
            float yaw = MegaParkP1Layout.SunYaw * (float)(Math.PI / 180.0);
            float cp = (float)Math.Cos(pitch);
            float sp = (float)Math.Sin(pitch);
            x = (float)Math.Sin(yaw) * cp;
            y = sp;
            z = (float)Math.Cos(yaw) * cp;
        }

        static void Basis(float fx, float fy, float fz, out float rx, out float ry, out float rz, out float ux, out float uy, out float uz)
        {
            float dx = -fy * fz;
            float dy = 1f - fy * fy;
            float dz = -fy * fz;
            // World up projected off the forward axis. Forward here is toSun.
            dx = 0f - fx * fy;
            dy = 1f - fy * fy;
            dz = 0f - fz * fy;
            float m = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (m < 1e-4f)
            {
                dx = 1f; dy = 0f; dz = 0f; m = 1f;
            }
            ux = dx / m; uy = dy / m; uz = dz / m;
            rx = uy * fz - uz * fy;
            ry = uz * fx - ux * fz;
            rz = ux * fy - uy * fx;
        }

        static void BasisFrom(float ex, float ey, float ez, float tx, float ty, float tz,
            out float fx, out float fy, out float fz, out float rx, out float ry, out float rz, out float ux, out float uy, out float uz)
        {
            fx = tx - ex;
            fy = ty - ey;
            fz = tz - ez;
            float m = (float)Math.Sqrt(fx * fx + fy * fy + fz * fz);
            if (m < 1e-4f) m = 1f;
            fx /= m; fy /= m; fz /= m;
            float ux0 = -fx * fy;
            float uy0 = 1f - fy * fy;
            float uz0 = -fz * fy;
            float um = (float)Math.Sqrt(ux0 * ux0 + uy0 * uy0 + uz0 * uz0);
            if (um < 1e-4f)
            {
                ux0 = 0f; uy0 = 0f; uz0 = 1f; um = 1f;
            }
            ux = ux0 / um; uy = uy0 / um; uz = uz0 / um;
            rx = uy * fz - uz * fy;
            ry = uz * fx - ux * fz;
            rz = ux * fy - uy * fx;
            float rm = (float)Math.Sqrt(rx * rx + ry * ry + rz * rz);
            if (rm < 1e-4f) rm = 1f;
            rx /= rm; ry /= rm; rz /= rm;
        }

        static void ShadowTri(Tri t, float[] map, int n, float ox, float oy, float oz,
            float rx, float ry, float rz, float ux, float uy, float uz, float sx, float sy, float sz, float half)
        {
            ProjectLight(t.X0, t.Y0, t.Z0, ox, oy, oz, rx, ry, rz, ux, uy, uz, sx, sy, sz, half, n, out float x0, out float y0, out float d0);
            ProjectLight(t.X1, t.Y1, t.Z1, ox, oy, oz, rx, ry, rz, ux, uy, uz, sx, sy, sz, half, n, out float x1, out float y1, out float d1);
            ProjectLight(t.X2, t.Y2, t.Z2, ox, oy, oz, rx, ry, rz, ux, uy, uz, sx, sy, sz, half, n, out float x2, out float y2, out float d2);
            FillShadow(map, n, x0, y0, d0, x1, y1, d1, x2, y2, d2);
        }

        static void ProjectLight(float x, float y, float z, float ox, float oy, float oz,
            float rx, float ry, float rz, float ux, float uy, float uz, float sx, float sy, float sz, float half, int n,
            out float px, out float py, out float depth)
        {
            float dx = x - ox;
            float dy = y - oy;
            float dz = z - oz;
            float lx = dx * rx + dy * ry + dz * rz;
            float ly = dx * ux + dy * uy + dz * uz;
            depth = x * sx + y * sy + z * sz;
            px = (lx / half * 0.5f + 0.5f) * (n - 1);
            py = (ly / half * 0.5f + 0.5f) * (n - 1);
        }

        static void FillShadow(float[] map, int n, float x0, float y0, float d0, float x1, float y1, float d1, float x2, float y2, float d2)
        {
            float area = (x1 - x0) * (y2 - y0) - (y1 - y0) * (x2 - x0);
            if (area < 0f)
            {
                float tx = x1, ty = y1, td = d1;
                x1 = x2; y1 = y2; d1 = d2;
                x2 = tx; y2 = ty; d2 = td;
                area = -area;
            }
            if (area < 0.5f) return;
            int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2)));
            int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2)));
            int minY = (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2)));
            int maxY = (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2)));
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            if (maxX >= n) maxX = n - 1;
            if (maxY >= n) maxY = n - 1;
            float inv = 1f / area;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float w0 = ((x1 - px) * (y2 - py) - (y1 - py) * (x2 - px)) * inv;
                    float w1 = ((x2 - px) * (y0 - py) - (y2 - py) * (x0 - px)) * inv;
                    float w2 = 1f - w0 - w1;
                    if (w0 < 0f || w1 < 0f || w2 < 0f) continue;
                    float d = w0 * d0 + w1 * d1 + w2 * d2;
                    int i = y * n + x;
                    if (d > map[i]) map[i] = d;
                }
            }
        }

        static void DrawTri(Tri t, byte[] rgb, float[] depth, int w, int h,
            float ex, float ey, float ez, float fx, float fy, float fz, float rx, float ry, float rz, float ux, float uy, float uz, float fov,
            float[] shadow, int sn, float sox, float soy, float soz, float srx, float sry, float srz, float sux, float suy, float suz,
            float lsx, float lsy, float lsz, float half)
        {
            float vx = ex - (t.X0 + t.X1 + t.X2) / 3f;
            float vy = ey - (t.Y0 + t.Y1 + t.Y2) / 3f;
            float vz = ez - (t.Z0 + t.Z1 + t.Z2) / 3f;
            if (vx * t.Nx + vy * t.Ny + vz * t.Nz <= 0f) return;

            const float near = 0.25f;
            V a = ToCam(t.X0, t.Y0, t.Z0, ex, ey, ez, fx, fy, fz, rx, ry, rz, ux, uy, uz);
            V b = ToCam(t.X1, t.Y1, t.Z1, ex, ey, ez, fx, fy, fz, rx, ry, rz, ux, uy, uz);
            V c = ToCam(t.X2, t.Y2, t.Z2, ex, ey, ez, fx, fy, fz, rx, ry, rz, ux, uy, uz);
            var poly = new V[8];
            int pn = ClipNear(a, b, c, near, poly);
            if (pn < 3) return;
            float aspect = w / (float)h;
            float tan = (float)Math.Tan(fov * 0.5f * Math.PI / 180.0);
            Shade(t, out float litR, out float litG, out float litB, out float shR, out float shG, out float shB);
            for (int i = 1; i < pn - 1; i++)
                Raster(poly[0], poly[i], poly[i + 1], rgb, depth, w, h, aspect, tan, litR, litG, litB, shR, shG, shB, t.A,
                    shadow, sn, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
        }

        static V ToCam(float x, float y, float z, float ex, float ey, float ez, float fx, float fy, float fz, float rx, float ry, float rz, float ux, float uy, float uz)
        {
            float dx = x - ex;
            float dy = y - ey;
            float dz = z - ez;
            V v;
            v.Wx = x; v.Wy = y; v.Wz = z;
            v.Cx = dx * rx + dy * ry + dz * rz;
            v.Cy = dx * ux + dy * uy + dz * uz;
            v.Cz = dx * fx + dy * fy + dz * fz;
            return v;
        }

        static int ClipNear(V a, V b, V c, float near, V[] poly)
        {
            V[] src = { a, b, c };
            int n = 0;
            for (int i = 0; i < 3; i++)
            {
                V s = src[i];
                V e = src[(i + 1) % 3];
                bool sin = s.Cz >= near;
                bool ein = e.Cz >= near;
                if (sin && ein)
                {
                    poly[n++] = e;
                }
                else if (sin && !ein)
                {
                    poly[n++] = Lerp(s, e, near);
                }
                else if (!sin && ein)
                {
                    poly[n++] = Lerp(s, e, near);
                    poly[n++] = e;
                }
            }
            return n;
        }

        static V Lerp(V s, V e, float near)
        {
            float d = e.Cz - s.Cz;
            float u = Math.Abs(d) < 1e-6f ? 0f : (near - s.Cz) / d;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            V v;
            v.Wx = s.Wx + (e.Wx - s.Wx) * u;
            v.Wy = s.Wy + (e.Wy - s.Wy) * u;
            v.Wz = s.Wz + (e.Wz - s.Wz) * u;
            v.Cx = s.Cx + (e.Cx - s.Cx) * u;
            v.Cy = s.Cy + (e.Cy - s.Cy) * u;
            v.Cz = near;
            return v;
        }

        static void Shade(Tri t, out float litR, out float litG, out float litB, out float shR, out float shG, out float shB)
        {
            Sun(out float sx, out float sy, out float sz);
            float ndl = t.Nx * sx + t.Ny * sy + t.Nz * sz;
            if (ndl < 0f) ndl = 0f;
            float up = t.Ny;
            float sky = up > 0f ? up : 0f;
            float gnd = up < 0f ? -up : 0f;
            float eq = 1f - Math.Abs(up);
            float ambR = (MegaParkP1Layout.AmbSkyR * sky + MegaParkP1Layout.AmbEqR * eq + MegaParkP1Layout.AmbGndR * gnd) * MegaParkP1Layout.AmbIntensity;
            float ambG = (MegaParkP1Layout.AmbSkyG * sky + MegaParkP1Layout.AmbEqG * eq + MegaParkP1Layout.AmbGndG * gnd) * MegaParkP1Layout.AmbIntensity;
            float ambB = (MegaParkP1Layout.AmbSkyB * sky + MegaParkP1Layout.AmbEqB * eq + MegaParkP1Layout.AmbGndB * gnd) * MegaParkP1Layout.AmbIntensity;
            float sunR = MegaParkP1Layout.SunR * MegaParkP1Layout.SunIntensity * ndl;
            float sunG = MegaParkP1Layout.SunG * MegaParkP1Layout.SunIntensity * ndl;
            float sunB = MegaParkP1Layout.SunB * MegaParkP1Layout.SunIntensity * ndl;
            float lr = Lin(t.R);
            float lg = Lin(t.G);
            float lb = Lin(t.B);
            float keep = 1f - MegaParkP1Layout.ShadowStrength;
            float gain = _overview ? 1.72f : 1f;
            float wr = _overview ? 1.06f : 1f;
            float wg = _overview ? 1.04f : 1f;
            float wb = _overview ? 0.94f : 1f;
            litR = Plate(Enc(lr * (ambR + sunR) * gain * wr));
            litG = Plate(Enc(lg * (ambG + sunG) * gain * wg));
            litB = Plate(Enc(lb * (ambB + sunB) * gain * wb));
            shR = Plate(Enc(lr * (ambR + sunR * keep) * gain * wr));
            shG = Plate(Enc(lg * (ambG + sunG * keep) * gain * wg));
            shB = Plate(Enc(lb * (ambB + sunB * keep) * gain * wb));
        }

        static float Plate(float e)
        {
            if (!_overview) return e;
            if (e < 0f) e = 0f;
            if (e > 1f) e = 1f;
            return (float)Math.Pow(e, 0.78);
        }

        static float Lin(float c)
        {
            if (c <= 0.04045f) return c / 12.92f;
            return (float)Math.Pow((c + 0.055f) / 1.055f, 2.4);
        }

        static float Enc(float c)
        {
            if (c < 0f) c = 0f;
            if (c > 1f) c = 1f;
            if (c <= 0.0031308f) return 12.92f * c;
            return 1.055f * (float)Math.Pow(c, 1.0 / 2.4) - 0.055f;
        }

        static void Raster(V a, V b, V c, byte[] rgb, float[] depth, int w, int h, float aspect, float tan,
            float litR, float litG, float litB, float shR, float shG, float shB, float alpha,
            float[] shadow, int sn, float sox, float soy, float soz, float srx, float sry, float srz, float sux, float suy, float suz,
            float lsx, float lsy, float lsz, float half)
        {
            Project(a, w, h, aspect, tan, out float x0, out float y0);
            Project(b, w, h, aspect, tan, out float x1, out float y1);
            Project(c, w, h, aspect, tan, out float x2, out float y2);
            float area = (x1 - x0) * (y2 - y0) - (y1 - y0) * (x2 - x0);
            if (area < 0f)
            {
                V tmp = b; b = c; c = tmp;
                float tx = x1, ty = y1;
                x1 = x2; y1 = y2; x2 = tx; y2 = ty;
                area = -area;
            }
            if (area < 0.5f) return;
            int minX = (int)Math.Floor(Math.Min(x0, Math.Min(x1, x2)));
            int maxX = (int)Math.Ceiling(Math.Max(x0, Math.Max(x1, x2)));
            int minY = (int)Math.Floor(Math.Min(y0, Math.Min(y1, y2)));
            int maxY = (int)Math.Ceiling(Math.Max(y0, Math.Max(y1, y2)));
            if (minX < 0) minX = 0;
            if (minY < 0) minY = 0;
            if (maxX >= w) maxX = w - 1;
            if (maxY >= h) maxY = h - 1;
            float inv = 1f / area;
            float iz0 = 1f / a.Cz;
            float iz1 = 1f / b.Cz;
            float iz2 = 1f / c.Cz;
            for (int y = minY; y <= maxY; y++)
            {
                int row = y * w;
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float w0 = ((x1 - px) * (y2 - py) - (y1 - py) * (x2 - px)) * inv;
                    float w1 = ((x2 - px) * (y0 - py) - (y2 - py) * (x0 - px)) * inv;
                    float w2 = 1f - w0 - w1;
                    if (w0 < 0f || w1 < 0f || w2 < 0f) continue;
                    float iz = w0 * iz0 + w1 * iz1 + w2 * iz2;
                    if (iz <= 0f) continue;
                    float z = 1f / iz;
                    int di = row + x;
                    if (z >= depth[di]) continue;
                    float wx = (w0 * a.Wx * iz0 + w1 * b.Wx * iz1 + w2 * c.Wx * iz2) / iz;
                    float wy = (w0 * a.Wy * iz0 + w1 * b.Wy * iz1 + w2 * c.Wy * iz2) / iz;
                    float wz = (w0 * a.Wz * iz0 + w1 * b.Wz * iz1 + w2 * c.Wz * iz2) / iz;
                    bool sh = Shadowed(wx, wy, wz, shadow, sn, sox, soy, soz, srx, sry, srz, sux, suy, suz, lsx, lsy, lsz, half);
                    int p = di * 3;
                    float cr = (sh ? shR : litR) * 255f;
                    float cg = (sh ? shG : litG) * 255f;
                    float cb = (sh ? shB : litB) * 255f;
                    if (alpha >= 0.99f)
                    {
                        depth[di] = z;
                        rgb[p] = (byte)cr;
                        rgb[p + 1] = (byte)cg;
                        rgb[p + 2] = (byte)cb;
                    }
                    else
                    {
                        float keep = 1f - alpha;
                        rgb[p] = (byte)(cr * alpha + rgb[p] * keep);
                        rgb[p + 1] = (byte)(cg * alpha + rgb[p + 1] * keep);
                        rgb[p + 2] = (byte)(cb * alpha + rgb[p + 2] * keep);
                    }
                }
            }
        }

        static void Project(V v, int w, int h, float aspect, float tan, out float x, out float y)
        {
            float ndcX = (v.Cx / v.Cz) / tan / aspect;
            float ndcY = (v.Cy / v.Cz) / tan;
            x = (ndcX * 0.5f + 0.5f) * (w - 1);
            y = (0.5f - ndcY * 0.5f) * (h - 1);
        }

        static bool Shadowed(float x, float y, float z, float[] map, int n, float ox, float oy, float oz,
            float rx, float ry, float rz, float ux, float uy, float uz, float sx, float sy, float sz, float half)
        {
            ProjectLight(x, y, z, ox, oy, oz, rx, ry, rz, ux, uy, uz, sx, sy, sz, half, n, out float px, out float py, out float d);
            int ix = (int)Math.Round(px);
            int iy = (int)Math.Round(py);
            if (ix < 1 || iy < 1 || ix >= n - 1 || iy >= n - 1) return false;
            float stored = map[iy * n + ix];
            if (stored < -1e10f) return false;
            return stored > d + 0.45f;
        }

        static string RepoDocs()
        {
            string dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                string docs = Path.Combine(dir, "Docs");
                if (Directory.Exists(docs)) return docs;
                DirectoryInfo parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }
            return null;
        }

        static bool PngSize(string path, out int w, out int h)
        {
            w = 0;
            h = 0;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                var buf = new byte[24];
                if (fs.Read(buf, 0, 24) < 24) return false;
                w = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
                h = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
                return w > 0 && h > 0;
            }
            catch (IOException)
            {
                return false;
            }
        }

        static void WritePng(string path, byte[] rgb, int w, int h)
        {
            int stride = w * 3;
            var raw = new byte[(stride + 1) * h];
            for (int y = 0; y < h; y++)
            {
                raw[y * (stride + 1)] = 0;
                Buffer.BlockCopy(rgb, y * stride, raw, y * (stride + 1) + 1, stride);
            }
            byte[] deflated;
            using (var ms = new MemoryStream())
            {
                using (var def = new DeflateStream(ms, System.IO.Compression.CompressionLevel.Fastest, true))
                    def.Write(raw, 0, raw.Length);
                deflated = ms.ToArray();
            }
            uint a = 1;
            uint bsum = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                a = (a + raw[i]) % 65521;
                bsum = (bsum + a) % 65521;
            }
            uint adler = (bsum << 16) | a;
            var zlib = new byte[deflated.Length + 6];
            zlib[0] = 0x78;
            zlib[1] = 0x01;
            Buffer.BlockCopy(deflated, 0, zlib, 2, deflated.Length);
            zlib[zlib.Length - 4] = (byte)(adler >> 24);
            zlib[zlib.Length - 3] = (byte)(adler >> 16);
            zlib[zlib.Length - 2] = (byte)(adler >> 8);
            zlib[zlib.Length - 1] = (byte)adler;
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
            fs.Write(sig, 0, sig.Length);
            var ihdr = new byte[13];
            Be(ihdr, 0, w);
            Be(ihdr, 4, h);
            ihdr[8] = 8;
            ihdr[9] = 2;
            Chunk(fs, "IHDR", ihdr);
            Chunk(fs, "IDAT", zlib);
            Chunk(fs, "IEND", new byte[0]);
        }

        static void Chunk(Stream fs, string name, byte[] data)
        {
            var len = new byte[4];
            Be(len, 0, data.Length);
            fs.Write(len, 0, 4);
            byte[] tag = Encoding.ASCII.GetBytes(name);
            fs.Write(tag, 0, 4);
            if (data.Length > 0) fs.Write(data, 0, data.Length);
            uint crc = 0xffffffff;
            for (int i = 0; i < tag.Length; i++) crc = Crc(crc, tag[i]);
            for (int i = 0; i < data.Length; i++) crc = Crc(crc, data[i]);
            crc ^= 0xffffffff;
            var c = new byte[4];
            Be(c, 0, crc);
            fs.Write(c, 0, 4);
        }

        static uint Crc(uint crc, byte value)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
            return crc;
        }

        static void Be(byte[] buf, int o, int value)
        {
            uint v = (uint)value;
            buf[o] = (byte)(v >> 24);
            buf[o + 1] = (byte)(v >> 16);
            buf[o + 2] = (byte)(v >> 8);
            buf[o + 3] = (byte)v;
        }

        static void Be(byte[] buf, int o, uint value)
        {
            buf[o] = (byte)(value >> 24);
            buf[o + 1] = (byte)(value >> 16);
            buf[o + 2] = (byte)(value >> 8);
            buf[o + 3] = (byte)value;
        }
    }
}
