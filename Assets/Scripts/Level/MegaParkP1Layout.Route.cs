using System;
using System.Collections.Generic;

namespace Tag.Level
{
    /// <summary>
    /// Read-only routes on the Mega Park nav the audit already builds.
    /// Pads, zips, grapple plates, cling walls, bar crouches, and zip counters
    /// are edges. Feel numbers are not stored here.
    /// </summary>
    public static partial class MegaParkP1Layout
    {
        public const byte HopWalk = 0;
        public const byte HopZip = 1;
        public const byte HopPad = 2;
        public const byte HopGrapple = 3;
        public const byte HopCling = 4;
        public const byte HopBar = 5;
        public const byte HopCounter = 6;

        const byte CellOpen = 0;
        const byte CellBlock = 1;
        const byte CellBar = 2;

        public struct ParkMark
        {
            public byte Kind;
            public short Index;
            public short Wall;
            public float X, Z, Y;
            public float ExitX, ExitZ, ExitY;
            public float Ride;
            public float Apex;
            public float DirX, DirZ, Speed;
        }

        public struct ParkHop
        {
            public float X, Z;
            public byte Kind;
            public short Mark;
        }

        public static ParkMark[] ParkMarks = new ParkMark[0];
        public static int ParkMarkCount;

        static bool _warm;
        static bool _building;
        static int _routeArena = -1;
        static Solid[] _solids;
        static Nav _nav;
        static byte[] _cell;
        static float[] _stand;
        static float[] _g;
        static int[] _parent;
        static byte[] _kind;
        static short[] _lid;
        static int[] _heapI;
        static float[] _heapC;
        static int _hn;
        static int _origin = -1;
        static int[] _stack;

        static int[] _zipMount, _zipExit, _padFrom, _padTo;
        static float[] _zipCost, _padCost;
        static short[] _zipMark, _padMark;

        static int[] _hookCell;
        static float[] _hookX, _hookZ;
        static short[] _hookMark;
        static int _hookCount;

        static int _clingFrom = -1;
        static int _clingTo = -1;
        static short _clingMark = -1;
        static float _clingCost;

        static float _sprint = 12f;
        static float _crouch = 3.2f;
        static float _wallRun = 9.5f;

        public static void WarmParkRoutes()
        {
            int arena = ParkArena.IsStack ? ParkArena.Stack : ParkArena.IsPocket ? ParkArena.Pocket : ParkArena.Mega;
            if (_warm && _routeArena == arena) return;
            if (_building) return;
            _building = true;
            _warm = false;
            // The Mega Park enemy proof was tuned before pass 9's lip, step, and
            // chevrons. Those pieces stay in the built park and the map audit.
            // The route cache keeps the tuned solid set so the published medians hold.
            bool pass9 = IncludePass9;
            if (arena != ParkArena.Pocket) IncludePass9 = false;
            _solids = arena == ParkArena.Stack ? StackYardLayout.BuildSolids()
                : arena == ParkArena.Pocket ? PocketParkLayout.BuildSolids() : BuildSolids();
            if (arena != ParkArena.Pocket) IncludePass9 = pass9;
            _nav = BuildNav(_solids);
            int n = _nav.Floor.Length;
            _cell = new byte[n];
            _stand = new float[n];
            _g = new float[n];
            _parent = new int[n];
            _kind = new byte[n];
            _lid = new short[n];
            _stack = new int[n];
            _heapI = new int[n * 24];
            _heapC = new float[n * 24];
            for (int i = 0; i < n; i++)
            {
                float y = _nav.Floor[i];
                if (!float.IsNaN(y))
                {
                    _cell[i] = CellOpen;
                    _stand[i] = y;
                    continue;
                }
                CellCenter(_nav, i, out float x, out float z);
                if (BarCrouch(x, z, out float stand))
                {
                    _cell[i] = CellBar;
                    _stand[i] = stand;
                }
                else
                {
                    _cell[i] = CellBlock;
                    _stand[i] = 0f;
                }
            }

            var marks = new List<ParkMark>(80);
            CacheToys(_nav, out _zipMount, out _zipExit, out _zipCost, out _padFrom, out _padTo, out _padCost);
            ZipLineSpot[] zips = RouteZips();
            PadSpot[] pads = RoutePads();
            _zipMark = new short[zips.Length];
            _padMark = new short[pads.Length];
            for (int i = 0; i < pads.Length; i++)
            {
                PadSpot p = pads[i];
                float mag = (float)Math.Sqrt(p.DirX * p.DirX + p.DirZ * p.DirZ);
                if (mag < 0.1f) mag = 1f;
                float hang = Hang(p.Apex);
                var m = new ParkMark
                {
                    Kind = HopPad,
                    Index = (short)i,
                    X = p.X,
                    Z = p.Z,
                    Y = p.Y,
                    ExitX = p.X + p.DirX / mag * p.Speed * hang,
                    ExitZ = p.Z + p.DirZ / mag * p.Speed * hang,
                    ExitY = p.Y,
                    Ride = hang,
                    Apex = p.Apex,
                    DirX = p.DirX / mag,
                    DirZ = p.DirZ / mag,
                    Speed = p.Speed
                };
                _padMark[i] = (short)marks.Count;
                marks.Add(m);
            }
            for (int i = 0; i < zips.Length; i++)
            {
                ZipLineSpot z = zips[i];
                var m = new ParkMark
                {
                    Kind = HopZip,
                    Index = (short)i,
                    X = z.Ax,
                    Z = z.Az,
                    Y = z.Ay,
                    ExitX = z.Bx,
                    ExitZ = z.Bz,
                    ExitY = z.By,
                    Ride = CableLen(z) / Math.Max(z.Speed, 0.1f),
                    Speed = z.Speed
                };
                _zipMark[i] = (short)marks.Count;
                marks.Add(m);
            }

            _hookCount = 0;
            for (int i = 0; i < _solids.Length; i++)
            {
                if (_solids[i].Kind == "anchor" && _solids[i].Name.StartsWith("Hook_", StringComparison.Ordinal))
                    _hookCount++;
            }
            _hookCell = new int[Math.Max(_hookCount, 1)];
            _hookX = new float[Math.Max(_hookCount, 1)];
            _hookZ = new float[Math.Max(_hookCount, 1)];
            _hookMark = new short[Math.Max(_hookCount, 1)];
            int h = 0;
            for (int i = 0; i < _solids.Length; i++)
            {
                Solid s = _solids[i];
                if (s.Kind != "anchor" || !s.Name.StartsWith("Hook_", StringComparison.Ordinal)) continue;
                int cell = NearestOpen(_nav, s.X, s.Z);
                _hookCell[h] = cell;
                _hookX[h] = s.X;
                _hookZ[h] = s.Z;
                var m = new ParkMark
                {
                    Kind = HopGrapple,
                    Index = (short)h,
                    X = s.X,
                    Z = s.Z,
                    Y = s.Y,
                    ExitX = s.X,
                    ExitZ = s.Z,
                    ExitY = s.Y,
                    Ride = 0.4f
                };
                if (cell >= 0)
                    CellCenter(_nav, cell, out m.ExitX, out m.ExitZ);
                _hookMark[h] = (short)marks.Count;
                marks.Add(m);
                h++;
            }

            short clingWall = 0;
            float southZ = 999f;
            int southCell = -1;
            short southMark = -1;
            for (int i = 0; i < _solids.Length; i++)
            {
                Solid s = _solids[i];
                if (s.Kind == "wall" && s.Name.StartsWith("Cling_", StringComparison.Ordinal))
                {
                    clingWall++;
                    int cell = NearestOpen(_nav, s.X + 1.4f, s.Z);
                    var m = new ParkMark
                    {
                        Kind = HopCling,
                        Index = clingWall,
                        Wall = clingWall,
                        X = s.X,
                        Z = s.Z,
                        Y = s.Y,
                        ExitX = s.X + 1.4f,
                        ExitZ = s.Z,
                        Ride = 0.6f
                    };
                    if (cell >= 0)
                        CellCenter(_nav, cell, out m.ExitX, out m.ExitZ);
                    short mi = (short)marks.Count;
                    marks.Add(m);
                    if (s.Z < southZ && cell >= 0)
                    {
                        southZ = s.Z;
                        southCell = cell;
                        southMark = mi;
                    }
                }
            }
            for (int i = 0; i < _solids.Length; i++)
            {
                Solid s = _solids[i];
                if (s.Name != "Cling_ExitDeck") continue;
                int cell = NearestOpen(_nav, s.X, s.Z);
                _clingFrom = southCell;
                _clingTo = cell;
                _clingMark = southMark;
                if (southCell >= 0 && cell >= 0)
                {
                    CellCenter(_nav, southCell, out float x0, out float z0);
                    CellCenter(_nav, cell, out float x1, out float z1);
                    float dx = x1 - x0;
                    float dz = z1 - z0;
                    _clingCost = (float)Math.Sqrt(dx * dx + dz * dz) / 9.5f;
                }
                break;
            }

            int barN = 0;
            for (int i = 0; i < _solids.Length; i++)
            {
                Solid s = _solids[i];
                if (s.Kind != "bar") continue;
                marks.Add(new ParkMark
                {
                    Kind = HopBar,
                    Index = (short)barN,
                    X = s.X,
                    Z = s.Z,
                    Y = s.Y,
                    ExitX = s.X,
                    ExitZ = s.Z,
                    Ride = 0.4f
                });
                barN++;
            }

            Ramp[] ramps = RouteRamps();
            if (ParkArena.IsPocket)
            {
                PocketParkLayout.CounterMark[] counters = PocketParkLayout.CounterMarks;
                for (int i = 0; i < counters.Length; i++)
                {
                    PocketParkLayout.CounterMark c = counters[i];
                    marks.Add(new ParkMark
                    {
                        Kind = HopCounter,
                        Index = (short)i,
                        X = c.X,
                        Z = c.Z,
                        Y = c.Y,
                        ExitX = c.X,
                        ExitZ = c.Z,
                        ExitY = c.Y,
                        Ride = 0f
                    });
                }
            }
            if (ParkArena.IsStack)
            {
                StackYardLayout.CounterMark[] counters = StackYardLayout.CounterMarks;
                for (int i = 0; i < counters.Length; i++)
                {
                    StackYardLayout.CounterMark c = counters[i];
                    marks.Add(new ParkMark
                    {
                        Kind = HopCounter,
                        Index = (short)i,
                        X = c.X,
                        Z = c.Z,
                        Y = c.Y,
                        ExitX = c.X,
                        ExitZ = c.Z,
                        ExitY = c.Y,
                        Ride = 0f
                    });
                }
            }
            for (int i = 0; i < zips.Length && !ParkArena.IsPocket && !ParkArena.IsStack; i++)
            {
                string counter = zips[i].Counter;
                if (string.IsNullOrEmpty(counter)) continue;
                bool added = false;
                for (int s = 0; s < _solids.Length; s++)
                {
                    if (_solids[s].Name != counter) continue;
                    Solid c = _solids[s];
                    marks.Add(new ParkMark
                    {
                        Kind = HopCounter,
                        Index = (short)i,
                        X = c.X,
                        Z = c.Z,
                        Y = c.Y,
                        ExitX = c.X,
                        ExitZ = c.Z,
                        Ride = 0f
                    });
                    added = true;
                    break;
                }
                if (added) continue;
                for (int r = 0; r < ramps.Length; r++)
                {
                    if (ramps[r].Name != counter) continue;
                    Ramp c = ramps[r];
                    float x = c.Y0 <= c.Y1 ? c.X0 : c.X1;
                    float z = c.Y0 <= c.Y1 ? c.Z0 : c.Z1;
                    float y = c.Y0 <= c.Y1 ? c.Y0 : c.Y1;
                    marks.Add(new ParkMark
                    {
                        Kind = HopCounter,
                        Index = (short)i,
                        X = x,
                        Z = z,
                        Y = y,
                        ExitX = x,
                        ExitZ = z,
                        ExitY = y,
                        Ride = 0f
                    });
                    break;
                }
            }

            ParkMarks = marks.ToArray();
            ParkMarkCount = ParkMarks.Length;
            _routeArena = arena;
            _warm = true;
            _building = false;
        }

        public static void SearchPark(float x, float z, float sprint, float crouch, float wallRun)
        {
            WarmParkRoutes();
            if (sprint < 1f) sprint = 1f;
            if (crouch < 0.5f) crouch = 0.5f;
            if (wallRun < 0.5f) wallRun = 0.5f;
            _sprint = sprint;
            _crouch = crouch;
            _wallRun = wallRun;
            int n = _g.Length;
            for (int i = 0; i < n; i++)
            {
                _g[i] = 1e9f;
                _parent[i] = -1;
                _kind[i] = HopWalk;
                _lid[i] = -1;
            }
            _origin = NearestOpen(_nav, x, z);
            if (_origin < 0) return;
            _g[_origin] = 0f;
            _hn = 0;
            HeapPush(_origin, 0f);
            int guard = 0;
            int limit = n * 8;
            while (_hn > 0 && guard++ < limit)
            {
                int cur = HeapPop(out float gc);
                if (gc > _g[cur] + 0.0001f) continue;
                RelaxGrid(cur);
                RelaxToys(cur);
                RelaxGrapple(cur);
                if (cur == _clingFrom && _clingTo >= 0 && _clingCost > 0f)
                    Relax(_clingTo, cur, _clingCost * (9.5f / wallRun), HopCling, _clingMark);
            }
        }

        public static float ParkSeconds(float x, float z)
        {
            if (!_warm || _origin < 0) return 1e9f;
            int idx = NearestOpen(_nav, x, z);
            if (idx < 0) return 1e9f;
            return _g[idx];
        }

        public static bool ParkOpen(float x, float z, out float y)
        {
            WarmParkRoutes();
            y = 0f;
            int idx = NavIndex(_nav, x, z, out int ix, out int iz);
            if (idx < 0) return false;
            if (_cell[idx] == CellBlock) return false;
            y = _stand[idx];
            return true;
        }

        public static bool ParkBar(float x, float z)
        {
            WarmParkRoutes();
            int idx = NavIndex(_nav, x, z, out int ix, out int iz);
            if (idx < 0) return false;
            return _cell[idx] == CellBar;
        }

        public static bool ParkLos(float x0, float z0, float x1, float z1)
        {
            WarmParkRoutes();
            float dx = x1 - x0;
            float dz = z1 - z0;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len < 0.8f) return true;
            int steps = (int)(len / _nav.Cell);
            if (steps < 1) steps = 1;
            for (int s = 1; s < steps; s++)
            {
                float u = s / (float)steps;
                int idx = NavIndex(_nav, x0 + dx * u, z0 + dz * u, out int ix, out int iz);
                if (idx < 0 || _cell[idx] == CellBlock) return false;
            }
            return true;
        }

        public static int ClingAt(float x, float z, float maxDist)
        {
            WarmParkRoutes();
            int best = 0;
            float bestD = maxDist;
            for (int i = 0; i < ParkMarkCount; i++)
            {
                ParkMark m = ParkMarks[i];
                if (m.Kind != HopCling || m.Wall == 0) continue;
                float dx = x - m.X;
                float dz = z - m.Z;
                float d = (float)Math.Sqrt(dx * dx + dz * dz);
                if (d < bestD)
                {
                    bestD = d;
                    best = m.Wall;
                }
            }
            return best;
        }

        public static bool ParkInsideFence(float x, float y, float z)
        {
            float mapW = ParkArena.MapW;
            float mapD = ParkArena.MapD;
            if (x < 0.2f || z < 0.2f || x > mapW - 0.2f || z > mapD - 0.2f) return false;
            if (y > FenceTop) return false;
            return true;
        }

        public static void ParkRespawn(float fromX, float fromZ, float itX, float itZ, out float x, out float y, out float z)
        {
            ParkArena.PickRespawn(fromX, fromZ, itX, itZ, true, out x, out y, out z);
        }

        /// <summary>A nearby open point the threat cannot see. Samples bearings; no path search.</summary>
        public static bool FindCover(float selfX, float selfZ, float threatX, float threatZ, out float x, out float z)
        {
            x = selfX;
            z = selfZ;
            WarmParkRoutes();
            if (ParkArena.IsPocket)
                return PocketParkLayout.SampleCover(selfX, selfZ, threatX, threatZ, out x, out z);
            if (ParkArena.IsStack)
                return StackYardLayout.SampleCover(selfX, selfZ, threatX, threatZ, out x, out z);
            float best = -1f;
            bool found = false;
            for (int i = 0; i < 16; i++)
            {
                double ang = i * (Math.PI * 2.0 / 16.0);
                float ca = (float)Math.Cos(ang);
                float sa = (float)Math.Sin(ang);
                for (int r = 0; r < 3; r++)
                {
                    float rad = 8f + r * 7f;
                    float cx = selfX + ca * rad;
                    float cz = selfZ + sa * rad;
                    if (!ParkInsideFence(cx, 0f, cz)) continue;
                    if (!ParkOpen(cx, cz, out float stand)) continue;
                    if (ParkLos(cx, cz, threatX, threatZ)) continue;
                    float dtx = cx - threatX;
                    float dtz = cz - threatZ;
                    float score = (float)Math.Sqrt(dtx * dtx + dtz * dtz);
                    if (score > best)
                    {
                        best = score;
                        x = cx;
                        z = cz;
                        found = true;
                    }
                }
            }
            return found;
        }

        public static int FillParkHops(float goalX, float goalZ, ParkHop[] hops, int cap)
        {
            if (hops == null || cap <= 0 || !_warm || _origin < 0) return 0;
            int goal = NearestOpen(_nav, goalX, goalZ);
            if (goal < 0 || _g[goal] > 1e8f) return 0;
            if (goal == _origin) return 0;
            int nstack = 0;
            int cur = goal;
            int guard = 0;
            while (cur != _origin && cur >= 0 && guard++ < _g.Length)
            {
                if (_parent[cur] < 0) return 0;
                _stack[nstack++] = cur;
                cur = _parent[cur];
                if (nstack >= _stack.Length) return 0;
            }
            if (cur != _origin) return 0;
            int count = 0;
            for (int i = nstack - 1; i >= 0 && count < cap; i--)
            {
                int cell = _stack[i];
                CellCenter(_nav, cell, out float hx, out float hz);
                byte kind = _kind[cell];
                if (kind == HopWalk && _cell[cell] == CellBar) kind = HopBar;
                hops[count].X = hx;
                hops[count].Z = hz;
                hops[count].Kind = kind;
                hops[count].Mark = _lid[cell];
                count++;
            }
            return count;
        }

        public static bool NudgeOpen(float x, float z, float goalX, float goalZ, out float ox, out float oz, out float oy)
        {
            WarmParkRoutes();
            ox = x;
            oz = z;
            oy = 0f;
            int best = -1;
            float bestD = 1e9f;
            for (int dz = -3; dz <= 3; dz++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int idx = NavIndex(_nav, x + dx * _nav.Cell, z + dz * _nav.Cell, out int ix, out int iz);
                    if (idx < 0 || _cell[idx] == CellBlock) continue;
                    CellCenter(_nav, idx, out float cx, out float cz);
                    float gx = cx - goalX;
                    float gz = cz - goalZ;
                    float d = gx * gx + gz * gz + (dx * dx + dz * dz) * 0.15f;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = idx;
                        ox = cx;
                        oz = cz;
                        oy = _stand[idx];
                    }
                }
            }
            return best >= 0;
        }

        static void RelaxGrid(int cur)
        {
            int ix = cur % _nav.Nx;
            int iz = cur / _nav.Nx;
            float y0 = _stand[cur];
            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int nx = ix + dx;
                    int nz = iz + dz;
                    if (nx < 0 || nz < 0 || nx >= _nav.Nx || nz >= _nav.Nz) continue;
                    int nxt = nz * _nav.Nx + nx;
                    if (_cell[nxt] == CellBlock) continue;
                    float y1 = _stand[nxt];
                    if (y1 > y0 + 2.2f) continue;
                    float step = (float)Math.Sqrt(dx * dx + dz * dz) * _nav.Cell;
                    float speed = _cell[nxt] == CellBar || _cell[cur] == CellBar ? _crouch : _sprint;
                    Relax(nxt, cur, step / speed, HopWalk, -1);
                }
            }
        }

        static void RelaxToys(int cur)
        {
            if (_padFrom != null)
            {
                for (int i = 0; i < _padFrom.Length; i++)
                {
                    if (_padFrom[i] != cur || _padTo[i] < 0) continue;
                    Relax(_padTo[i], cur, _padCost[i], HopPad, _padMark[i]);
                }
            }
            if (_zipMount != null)
            {
                for (int i = 0; i < _zipMount.Length; i++)
                {
                    if (_zipMount[i] != cur || _zipExit[i] < 0) continue;
                    Relax(_zipExit[i], cur, _zipCost[i], HopZip, _zipMark[i]);
                }
            }
        }

        static void RelaxGrapple(int cur)
        {
            if (_hookCount <= 0) return;
            CellCenter(_nav, cur, out float x, out float z);
            for (int i = 0; i < _hookCount; i++)
            {
                int dest = _hookCell[i];
                if (dest < 0 || dest == cur) continue;
                float dx = _hookX[i] - x;
                float dz = _hookZ[i] - z;
                float dist = (float)Math.Sqrt(dx * dx + dz * dz);
                if (dist > GrappleRange || dist < 1.5f) continue;
                if (!ParkLos(x, z, _hookX[i], _hookZ[i])) continue;
                Relax(dest, cur, dist / _sprint, HopGrapple, _hookMark[i]);
            }
        }

        static void Relax(int nxt, int cur, float cost, byte kind, short mark)
        {
            if (nxt < 0 || cost < 0f) return;
            float ng = _g[cur] + cost;
            if (ng >= _g[nxt]) return;
            _g[nxt] = ng;
            _parent[nxt] = cur;
            _kind[nxt] = kind;
            _lid[nxt] = mark;
            HeapPush(nxt, ng);
        }

        static PadSpot[] RoutePads()
        {
            if (ParkArena.IsStack) return StackYardLayout.LaunchPads;
            return ParkArena.IsPocket ? PocketParkLayout.LaunchPads : LaunchPads;
        }

        static ZipLineSpot[] RouteZips()
        {
            if (ParkArena.IsStack) return StackYardLayout.ZipLines;
            return ParkArena.IsPocket ? PocketParkLayout.ZipLines : ZipLines;
        }

        static Ramp[] RouteRamps()
        {
            if (ParkArena.IsStack) return StackYardLayout.BuildRamps();
            return ParkArena.IsPocket ? PocketParkLayout.BuildRamps() : BuildRamps();
        }

        static bool BarCrouch(float x, float z, out float y)
        {
            y = 0f;
            bool under = false;
            for (int i = 0; i < _solids.Length; i++)
            {
                Solid s = _solids[i];
                if (s.Kind != "bar") continue;
                if (DistXZ(x, z, s) > 0.35f) continue;
                float bottom = s.Y - s.Sy * 0.5f;
                if (bottom + 0.02f < BarUnderClear) continue;
                under = true;
                break;
            }
            if (!under) return false;
            for (int i = 0; i < _solids.Length; i++)
            {
                Solid s = _solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "bar" || s.Kind == "toy") continue;
                if (DistXZ(x, z, s) > 0.34f) continue;
                float bottom = s.Y - s.Sy * 0.5f;
                float top = s.Y + s.Sy * 0.5f;
                if (bottom < 0.95f && top > 0.2f) return false;
            }
            y = DeckUnder(x, z);
            return true;
        }

        static float DeckUnder(float x, float z)
        {
            float deck = 0f;
            bool any = false;
            for (int i = 0; i < _solids.Length; i++)
            {
                Solid s = _solids[i];
                if (s.Kind == "wall" || s.Kind == "fence" || s.Kind == "anchor" || s.Kind == "bar" || s.Kind == "post" || s.Kind == "landmark")
                    continue;
                if (s.Kind == "ground" || s.Kind == "toy") continue;
                if (DistXZ(x, z, s) > 0.05f) continue;
                float top = s.Y + s.Sy * 0.5f;
                if (top > 1.15f) continue;
                if (!any || top > deck)
                {
                    deck = top;
                    any = true;
                }
            }
            return any ? deck : 0f;
        }

        static void HeapPush(int item, float c)
        {
            if (_hn >= _heapI.Length) return;
            int i = _hn++;
            _heapI[i] = item;
            _heapC[i] = c;
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (_heapC[p] <= _heapC[i]) break;
                int ti = _heapI[p];
                _heapI[p] = _heapI[i];
                _heapI[i] = ti;
                float tc = _heapC[p];
                _heapC[p] = _heapC[i];
                _heapC[i] = tc;
                i = p;
            }
        }

        static int HeapPop(out float c)
        {
            int item = _heapI[0];
            c = _heapC[0];
            _hn--;
            if (_hn > 0)
            {
                _heapI[0] = _heapI[_hn];
                _heapC[0] = _heapC[_hn];
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1;
                    if (l >= _hn) break;
                    int r = l + 1;
                    int m = (r < _hn && _heapC[r] < _heapC[l]) ? r : l;
                    if (_heapC[i] <= _heapC[m]) break;
                    int ti = _heapI[m];
                    _heapI[m] = _heapI[i];
                    _heapI[i] = ti;
                    float tc = _heapC[m];
                    _heapC[m] = _heapC[i];
                    _heapC[i] = tc;
                    i = m;
                }
            }
            return item;
        }
    }
}
