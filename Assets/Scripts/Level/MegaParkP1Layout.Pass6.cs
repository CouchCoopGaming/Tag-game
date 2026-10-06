using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Pass 6 checks for <see cref="MegaParkP1Layout"/>: containment, spawn safety,
    /// landmarks, static-batch census, and the tag-back pulse against zone tints.
    /// Feel locks are read, not written.
    /// </summary>
    public static partial class MegaParkP1Layout
    {
        public struct RampDraw
        {
            public Ramp Ramp;
            public bool KeepCollider;
        }

        /// <summary>
        /// Sand-corner strips are 0.16 m wide. Adjacent pairs become one collider.
        /// The merged surface stays within half their 0.04 m step.
        /// </summary>
        public static void PlanRampColliders(Ramp[] ramps, List<RampDraw> drawn, List<Ramp> merged, out float gap)
        {
            drawn.Clear();
            merged.Clear();
            SplitSand(ramps, out List<int> a, out List<int> b, out List<int> alone, out gap);
            var paired = new bool[ramps.Length];
            for (int i = 0; i < a.Count; i++)
            {
                paired[a[i]] = true;
                paired[b[i]] = true;
                merged.Add(MergeStrip(ramps[a[i]], ramps[b[i]], i));
            }
            for (int i = 0; i < ramps.Length; i++)
            {
                drawn.Add(new RampDraw { Ramp = ramps[i], KeepCollider = !paired[i] });
            }
            if (alone.Count < 0) gap = 0f;
        }

        public static void PickRespawn(float fromX, float fromZ, float itX, float itZ, bool hasIt,
            out float x, out float y, out float z)
        {
            y = SpawnY;
            Solid[] solids = hasIt ? BuildSolids() : null;
            SpawnPad[] pads = AllPads();
            SpawnPad best = pads[0];
            float bestScore = float.MaxValue;
            bool any = false;
            for (int i = 0; i < pads.Length; i++)
            {
                SpawnPad pad = pads[i];
                if (hasIt && !PadSafe(pad.X, pad.Z, itX, itZ, solids)) continue;
                if (hasIt && AdjacentArc(pad.X, pad.Z, itX, itZ)) continue;
                float score = LoopArc(fromX, fromZ, pad.X, pad.Z);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = pad;
                    any = true;
                }
            }
            if (!any && hasIt)
            {
                float far = -1f;
                for (int i = 0; i < pads.Length; i++)
                {
                    SpawnPad pad = pads[i];
                    if (!PadSafe(pad.X, pad.Z, itX, itZ, solids)) continue;
                    float d = DistPoint(pad.X, pad.Z, itX, itZ);
                    if (d > far)
                    {
                        far = d;
                        best = pad;
                        any = true;
                    }
                }
            }
            if (!any)
            {
                bestScore = float.MaxValue;
                for (int i = 0; i < pads.Length; i++)
                {
                    float score = LoopArc(fromX, fromZ, pads[i].X, pads[i].Z);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = pads[i];
                    }
                }
            }
            x = best.X;
            z = best.Z;
        }

        public static bool SpawnIsSafe(float x, float z, float itX, float itZ)
        {
            if (DistPoint(x, z, itX, itZ) >= SpawnClearMeters
                && DistPoint(x, z, itX, itZ) / SprintSpeed >= SpawnSightSeconds - 0.0001f)
                return true;
            return PadSafe(x, z, itX, itZ, BuildSolids());
        }

        static int ContainmentReport(Solid[] solids, StringBuilder fail, out bool held)
        {
            held = true;
            int sweeps = 0;
            int misses = 0;
            if (!FeelLocksMatch(fail)) held = false;
            float stand = MaxStand(solids);
            float apex = LockedJumpSpeed * LockedJumpSpeed / (2f * RiseGravity);
            if (FenceTop < stand + apex + 1f)
            {
                fail.Append("fence ").Append(FenceTop.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append(" is below a jump off ").Append(stand.ToString("0.0", CultureInfo.InvariantCulture)).Append("; ");
                held = false;
            }
            if (!FenceShell(solids, fail)) held = false;
            float lowest = LowestBottom(solids);
            if (KillPlaneY > lowest - 0.4f || KillPlaneY < -6f)
            {
                fail.Append("kill plane ").Append(KillPlaneY.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" is not under the map; ");
                held = false;
            }
            string voidSrc = ReadText("Assets/Scripts/Local/VoidRespawn.cs");
            string modeSrc = ReadText("Assets/Scripts/Modes/TagModeController.cs");
            if (voidSrc == null || voidSrc.IndexOf("KillPlaneY", StringComparison.Ordinal) < 0
                || voidSrc.IndexOf("PickRespawn", StringComparison.Ordinal) < 0)
            {
                fail.Append("kill plane is not wired to a safe respawn; ");
                held = false;
            }
            if (modeSrc == null || modeSrc.IndexOf("SpawnIsSafe", StringComparison.Ordinal) < 0)
            {
                fail.Append("round start does not check spawn safety; ");
                held = false;
            }

            if (LaunchPads != null)
            {
                for (int i = 0; i < LaunchPads.Length; i++)
                {
                    PadSpot p = LaunchPads[i];
                    float mag = (float)Math.Sqrt(p.DirX * p.DirX + p.DirZ * p.DirZ);
                    if (mag < 0.1f) continue;
                    float ux = p.DirX / mag;
                    float uz = p.DirZ / mag;
                    float vy = (float)Math.Sqrt(2f * RiseGravity * p.Apex);
                    sweeps++;
                    if (!Fly(p.X, p.Y, p.Z, ux * p.Speed, vy, uz * p.Speed))
                        NoteMiss(fail, ref misses, p.Name + " arc");
                    float hang = Hang(p.Apex);
                    for (int s = 0; s < 4; s++)
                    {
                        float t = hang * (s + 1) / 5f;
                        float x = p.X + ux * p.Speed * t;
                        float z = p.Z + uz * p.Speed * t;
                        float y = ArcY(p.Y, p.Apex, t);
                        Fan(x, y, z, MaxAirSpeed, p.Name + " arc", fail, ref sweeps, ref misses);
                    }
                }
            }

            if (ZipLines != null)
            {
                for (int i = 0; i < ZipLines.Length; i++)
                {
                    ZipLineSpot z = ZipLines[i];
                    RideAxes(z, out float hx, out float hz);
                    SweepEnd(z.Name + " A", z.Ax, z.Ay, z.Az, hx, hz, fail, ref sweeps, ref misses);
                    SweepEnd(z.Name + " B", z.Bx, z.By, z.Bz, hx, hz, fail, ref sweeps, ref misses);
                }
            }

            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Name.StartsWith("Hook_", StringComparison.Ordinal))
                {
                    float top = s.Y + s.Sy * 0.5f;
                    Fan(s.X, top, s.Z, MaxAirSpeed, s.Name, fail, ref sweeps, ref misses);
                }
                if (s.Kind == "wall" || s.Name.StartsWith("Rim_", StringComparison.Ordinal))
                    WallRunOff(s, fail, ref sweeps, ref misses);
            }

            RimFan(fail, ref sweeps, ref misses);
            if (misses > 0) held = false;
            return sweeps;
        }

        static void SweepEnd(string name, float x, float y, float z, float hx, float hz,
            StringBuilder fail, ref int sweeps, ref int misses)
        {
            float feet = y - ZipHang - PawnHeight * 0.5f;
            float floor = BowlCut(x, z) ? BowlFloorY : 0f;
            if (feet < floor) feet = floor;
            sweeps++;
            if (!Fly(x, feet, z, hx, 0f, hz))
                NoteMiss(fail, ref misses, name + " release");
            sweeps++;
            if (!Fly(x, feet, z, hx, LockedJumpSpeed, hz))
                NoteMiss(fail, ref misses, name + " jump");
            Fan(x, feet, z, MaxAirSpeed, name, fail, ref sweeps, ref misses);
        }

        static void Fan(float x, float y, float z, float speed, string name,
            StringBuilder fail, ref int sweeps, ref int misses)
        {
            for (int d = 0; d < 8; d++)
            {
                float dx = DirX(d) * speed;
                float dz = DirZ(d) * speed;
                sweeps++;
                if (!Fly(x, y, z, dx, 0f, dz))
                    NoteMiss(fail, ref misses, name + " air");
                sweeps++;
                if (!Fly(x, y, z, dx, LockedJumpSpeed, dz))
                    NoteMiss(fail, ref misses, name + " jump");
            }
        }

        static void WallRunOff(Solid s, StringBuilder fail, ref int sweeps, ref int misses)
        {
            float top = s.Y + s.Sy * 0.5f;
            NearestOut(s.X, s.Z, out float ox, out float oz);
            float alongX = -oz;
            float alongZ = ox;
            float startX = s.X + ox * (Math.Min(s.Sx, s.Sz) * 0.5f + 0.6f);
            float startZ = s.Z + oz * (Math.Min(s.Sx, s.Sz) * 0.5f + 0.6f);
            if (startX < 1.2f) startX = 1.2f;
            if (startZ < 1.2f) startZ = 1.2f;
            if (startX > MapW - 1.2f) startX = MapW - 1.2f;
            if (startZ > MapD - 1.2f) startZ = MapD - 1.2f;
            float[,] kicks =
            {
                { ox * 8f, alongX * 0f },
                { ox * 8f + alongX * 9.5f, oz * 8f + alongZ * 9.5f },
                { ox * 8f - alongX * 9.5f, oz * 8f - alongZ * 9.5f },
            };
            for (int k = 0; k < 3; k++)
            {
                float vx = k == 0 ? ox * 8f : kicks[k, 0];
                float vz = k == 0 ? oz * 8f : kicks[k, 1];
                sweeps++;
                if (!Fly(startX, top, startZ, vx, 6.2f, vz))
                    NoteMiss(fail, ref misses, s.Name + " wall-run");
            }
        }

        static void RimFan(StringBuilder fail, ref int sweeps, ref int misses)
        {
            float[] heights = { 0f, 5f, LandmarkCrown };
            for (float x = 8f; x < MapW; x += 12f)
            {
                for (int h = 0; h < heights.Length; h++)
                {
                    Edge(x, heights[h], 1.2f, 0f, -1f, "rim S", fail, ref sweeps, ref misses);
                    Edge(x, heights[h], MapD - 1.2f, 0f, 1f, "rim N", fail, ref sweeps, ref misses);
                }
            }
            for (float z = 8f; z < MapD; z += 12f)
            {
                for (int h = 0; h < heights.Length; h++)
                {
                    Edge(1.2f, heights[h], z, -1f, 0f, "rim W", fail, ref sweeps, ref misses);
                    Edge(MapW - 1.2f, heights[h], z, 1f, 0f, "rim E", fail, ref sweeps, ref misses);
                }
            }
        }

        static void Edge(float x, float y, float z, float ox, float oz, string name,
            StringBuilder fail, ref int sweeps, ref int misses)
        {
            float ax = -oz;
            float az = ox;
            float[,] dir =
            {
                { ox, oz },
                { ox * 0.7071f + ax * 0.7071f, oz * 0.7071f + az * 0.7071f },
                { ox * 0.7071f - ax * 0.7071f, oz * 0.7071f - az * 0.7071f },
            };
            for (int d = 0; d < 3; d++)
            {
                sweeps++;
                if (!Fly(x, y, z, dir[d, 0] * MaxAirSpeed, LockedJumpSpeed, dir[d, 1] * MaxAirSpeed))
                    NoteMiss(fail, ref misses, name);
            }
        }

        static bool Fly(float x, float y, float z, float vx, float vy, float vz)
        {
            const float r = PawnRadius;
            if (x - r < 0f || x + r > MapW || z - r < 0f || z + r > MapD) return false;
            for (int step = 0; step < 480; step++)
            {
                const float dt = 0.05f;
                float nx = x + vx * dt;
                float ny = y + vy * dt;
                float nz = z + vz * dt;
                if (FenceStops(x, y, z, nx, ny, nz)) return true;
                if (nx - r < -0.02f || nx + r > MapW + 0.02f || nz - r < -0.02f || nz + r > MapD + 0.02f)
                    return false;
                x = nx;
                y = ny;
                z = nz;
                float g = vy > 0f ? RiseGravity : RiseGravity * FallGravity;
                vy -= g * dt;
                if (vy < -52f) vy = -52f;
                float floor = BowlCut(x, z) ? BowlFloorY : 0f;
                if (y <= floor && vy <= 0f) return true;
                if (y < KillPlaneY) return true;
            }
            return x - r >= 0f && x + r <= MapW && z - r >= 0f && z + r <= MapD;
        }

        static bool FenceStops(float x, float y, float z, float nx, float ny, float nz)
        {
            const float r = PawnRadius;
            if (CrossFace(x - r, nx - r, 0f, y, ny)) return true;
            if (CrossFace(MapW - (x + r), MapW - (nx + r), 0f, y, ny)) return true;
            if (CrossFace(z - r, nz - r, 0f, y, ny)) return true;
            if (CrossFace(MapD - (z + r), MapD - (nz + r), 0f, y, ny)) return true;
            return false;
        }

        static bool CrossFace(float before, float after, float face, float y, float ny)
        {
            if (before >= face - 0.001f && after < face)
            {
                float denom = before - after;
                float t = denom > 1e-6f ? (before - face) / denom : 0f;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
                float yc = y + (ny - y) * t;
                if (yc < FenceTop) return true;
            }
            return false;
        }

        static void NoteMiss(StringBuilder fail, ref int misses, string name)
        {
            misses++;
            if (misses <= 3)
                fail.Append(name).Append(" leaves the park; ");
        }

        static void RideAxes(ZipLineSpot z, out float hx, out float hz)
        {
            float dx = z.Bx - z.Ax;
            float dy = z.By - z.Ay;
            float dz = z.Bz - z.Az;
            if (z.By > z.Ay)
            {
                dx = -dx;
                dy = -dy;
                dz = -dz;
            }
            float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (len < 0.01f)
            {
                hx = 0f;
                hz = 0f;
                return;
            }
            hx = dx / len * z.Speed;
            hz = dz / len * z.Speed;
        }

        static void NearestOut(float x, float z, out float ox, out float oz)
        {
            float west = x;
            float east = MapW - x;
            float south = z;
            float north = MapD - z;
            ox = 0f;
            oz = -1f;
            float best = south;
            if (north < best) { best = north; ox = 0f; oz = 1f; }
            if (west < best) { best = west; ox = -1f; oz = 0f; }
            if (east < best) { ox = 1f; oz = 0f; }
        }

        static float DirX(int i)
        {
            switch (i)
            {
                case 0: return 1f;
                case 1: return -1f;
                case 2: return 0f;
                case 3: return 0f;
                case 4: return 0.7071f;
                case 5: return 0.7071f;
                case 6: return -0.7071f;
                default: return -0.7071f;
            }
        }

        static float DirZ(int i)
        {
            switch (i)
            {
                case 0: return 0f;
                case 1: return 0f;
                case 2: return 1f;
                case 3: return -1f;
                case 4: return 0.7071f;
                case 5: return -0.7071f;
                case 6: return 0.7071f;
                default: return -0.7071f;
            }
        }

        static bool FeelLocksMatch(StringBuilder fail)
        {
            string src = ReadText("Assets/TagArenaMovement/Scripts/Core/MovementConfig.cs");
            if (src == null
                || src.IndexOf("jumpSpeed = 24.7", StringComparison.Ordinal) < 0
                || src.IndexOf("airDashSpeed = 15", StringComparison.Ordinal) < 0
                || src.IndexOf("taggerLungeSpeed = 16", StringComparison.Ordinal) < 0
                || src.IndexOf("wallRunSpeed = 9.5", StringComparison.Ordinal) < 0
                || src.IndexOf("wallRunJumpOut = 8", StringComparison.Ordinal) < 0
                || src.IndexOf("wallRunJumpUp = 6.2", StringComparison.Ordinal) < 0
                || src.IndexOf("gravity = 22", StringComparison.Ordinal) < 0)
            {
                fail.Append("containment sweep drifted off the feel locks; ");
                return false;
            }
            return true;
        }

        static bool FenceShell(Solid[] solids, StringBuilder fail)
        {
            bool s = false, n = false, w = false, e = false;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid f = solids[i];
                if (f.Kind != "fence") continue;
                float top = f.Y + f.Sy * 0.5f;
                float bottom = f.Y - f.Sy * 0.5f;
                if (top < FenceTop - 0.05f || bottom > 0.05f)
                {
                    fail.Append(f.Name).Append(" does not span the fence; ");
                    return false;
                }
                if (f.Name == "Fence_S") s = Math.Abs((f.Z + f.Sz * 0.5f) - 0f) < 0.02f;
                if (f.Name == "Fence_N") n = Math.Abs((f.Z - f.Sz * 0.5f) - MapD) < 0.02f;
                if (f.Name == "Fence_W") w = Math.Abs((f.X + f.Sx * 0.5f) - 0f) < 0.02f;
                if (f.Name == "Fence_E") e = Math.Abs((f.X - f.Sx * 0.5f) - MapW) < 0.02f;
                float thin = Math.Min(f.Sx, f.Sz);
                if (thin >= PawnRadius)
                {
                    fail.Append(f.Name).Append(" is thick enough to perch on; ");
                    return false;
                }
            }
            if (!s || !n || !w || !e)
            {
                fail.Append("perimeter fence is open; ");
                return false;
            }
            return true;
        }

        static float MaxStand(Solid[] solids)
        {
            float max = 0f;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                float top = s.Y + s.Sy * 0.5f;
                if (top > max) max = top;
            }
            return max;
        }

        static float LowestBottom(Solid[] solids)
        {
            float min = 0f;
            for (int i = 0; i < solids.Length; i++)
            {
                float bottom = solids[i].Y - solids[i].Sy * 0.5f;
                if (bottom < min) min = bottom;
            }
            return min;
        }

        static string SpawnSafetyReport(Solid[] solids, StringBuilder fail)
        {
            SpawnPad[] pads = AllPads();
            float minGap = float.MaxValue;
            float minSight = float.MaxValue;
            int sightPairs = 0;
            for (int i = 0; i < pads.Length; i++)
            {
                for (int j = i + 1; j < pads.Length; j++)
                {
                    float gap = DistPoint(pads[i].X, pads[i].Z, pads[j].X, pads[j].Z);
                    if (gap < minGap) minGap = gap;
                    if (gap < SpawnClearMeters)
                        fail.Append(pads[i].Name).Append(" is within 20 m of ").Append(pads[j].Name).Append("; ");
                    bool los = HasLos(solids, pads[i].X, pads[i].Z, pads[j].X, pads[j].Z);
                    float sight = gap / SprintSpeed;
                    if (los)
                    {
                        sightPairs++;
                        if (sight < minSight) minSight = sight;
                        if (sight < SpawnSightSeconds)
                            fail.Append(pads[i].Name).Append(" sees ").Append(pads[j].Name)
                                .Append(" in ").Append(sight.ToString("0.00", CultureInfo.InvariantCulture)).Append(" s; ");
                    }
                }
            }

            int checks = 0;
            if (!RespawnFair(solids, fail, ref checks))
                fail.Append("respawn is not a safe arc; ");
            if (minGap > 1000f) minGap = 0f;
            if (minSight > 1000f) minSight = minGap / SprintSpeed;
            return string.Format(
                CultureInfo.InvariantCulture,
                "spawn safety gap {0:0.0} m sight {1:0.00} s respawn {2}",
                minGap, minSight, checks);
        }

        static bool RespawnFair(Solid[] solids, StringBuilder fail, ref int checks)
        {
            SpawnPad[] pads = AllPads();
            float[] xs = { 8f, 40f, 62f, 96f, 140f, 152f };
            float[] zs = { 8f, 16f, 40f, 50f, 78f, 92f };
            bool ok = true;
            for (int it = 0; it < pads.Length; it++)
            {
                for (int a = 0; a < xs.Length; a++)
                {
                    for (int b = 0; b < zs.Length; b++)
                    {
                        checks++;
                        PickRespawn(xs[a], zs[b], pads[it].X, pads[it].Z, true, out float x, out float y, out float z);
                        if (!PadSafe(x, z, pads[it].X, pads[it].Z, solids) || AdjacentArc(x, z, pads[it].X, pads[it].Z))
                        {
                            if (ok)
                                fail.Append("respawn (").Append(x.ToString("0", CultureInfo.InvariantCulture))
                                    .Append(',').Append(z.ToString("0", CultureInfo.InvariantCulture))
                                    .Append(") is unsafe near ").Append(pads[it].Name).Append("; ");
                            ok = false;
                        }
                        float score = LoopArc(xs[a], zs[b], x, z);
                        for (int p = 0; p < pads.Length; p++)
                        {
                            if (!PadSafe(pads[p].X, pads[p].Z, pads[it].X, pads[it].Z, solids)) continue;
                            if (AdjacentArc(pads[p].X, pads[p].Z, pads[it].X, pads[it].Z)) continue;
                            float alt = LoopArc(xs[a], zs[b], pads[p].X, pads[p].Z);
                            if (alt + 0.05f < score)
                            {
                                if (ok)
                                    fail.Append("respawn skipped a nearer safe arc; ");
                                ok = false;
                            }
                        }
                    }
                }
            }
            return ok;
        }

        static bool PadSafe(float x, float z, float itX, float itZ, Solid[] solids)
        {
            float gap = DistPoint(x, z, itX, itZ);
            if (gap < SpawnClearMeters) return false;
            if (gap / SprintSpeed < SpawnSightSeconds && solids != null && HasLos(solids, x, z, itX, itZ))
                return false;
            return true;
        }

        static bool AdjacentArc(float x, float z, float itX, float itZ)
        {
            return LoopArc(x, z, itX, itZ) <= 118.05f;
        }

        static float LoopArc(float x0, float z0, float x1, float z1)
        {
            float la, lb;
            float ta = ProjectLoop(x0, z0, out la);
            float tb = ProjectLoop(x1, z1, out lb);
            float along = Math.Abs(ta - tb);
            if (along > LoopLengthM * 0.5f) along = LoopLengthM - along;
            return along;
        }

        static bool HasLos(Solid[] solids, float x0, float z0, float x1, float z1)
        {
            float y0 = EyeY(solids, x0, z0);
            float y1 = EyeY(solids, x1, z1);
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                float t;
                if (!SegmentHit(s, x0, y0, z0, x1, y1, z1, out t)) continue;
                if (t > 0.04f && t < 0.96f) return false;
            }
            return true;
        }

        static float EyeY(Solid[] solids, float x, float z)
        {
            return FloorAt(solids, x, z) + 1.6f;
        }

        static SpawnPad[] AllPads()
        {
            int n = Spawns.Length + (RunnerSpawns == null ? 0 : RunnerSpawns.Length);
            var pads = new SpawnPad[n];
            for (int i = 0; i < Spawns.Length; i++) pads[i] = Spawns[i];
            if (RunnerSpawns != null)
            {
                for (int i = 0; i < RunnerSpawns.Length; i++) pads[Spawns.Length + i] = RunnerSpawns[i];
            }
            return pads;
        }

        struct ZoneBox
        {
            public string Id;
            public float X0, X1, Z0, Z1;
        }

        static readonly ZoneBox[] ZoneBoxes =
        {
            new ZoneBox { Id = "Z8", X0 = 46f, X1 = 78f, Z0 = 34f, Z1 = 66f },
            new ZoneBox { Id = "Z10", X0 = 118f, X1 = 156f, Z0 = 2f, Z1 = 22f },
            new ZoneBox { Id = "Z9", X0 = 38f, X1 = 118f, Z0 = 12f, Z1 = 20f },
            new ZoneBox { Id = "Z2", X0 = 2f, X1 = 18f, Z0 = 38f, Z1 = 78f },
            new ZoneBox { Id = "Z1", X0 = 2f, X1 = 38f, Z0 = 2f, Z1 = 36f },
            new ZoneBox { Id = "Z4", X0 = 22f, X1 = 56f, Z0 = 72f, Z1 = 98f },
            new ZoneBox { Id = "Z5", X0 = 58f, X1 = 100f, Z0 = 78f, Z1 = 98f },
            new ZoneBox { Id = "Z3", X0 = 22f, X1 = 46f, Z0 = 34f, Z1 = 60f },
            new ZoneBox { Id = "Z6", X0 = 118f, X1 = 158f, Z0 = 10f, Z1 = 90f },
            new ZoneBox { Id = "Z7", X0 = 64f, X1 = 114f, Z0 = 28f, Z1 = 68f },
        };

        static string LandmarkReport(Solid[] solids, StringBuilder fail)
        {
            int zones = 0;
            for (int z = 0; z < ZoneBoxes.Length; z++)
            {
                if (HasLandmark(solids, ZoneBoxes[z].Id)) zones++;
                else fail.Append(ZoneBoxes[z].Id).Append(" has no landmark; ");
            }
            int pieces = 0;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind != "landmark") continue;
                pieces++;
                if (s.Mat == "blue" || s.Mat == "yellow" || s.Mat == "plate")
                    fail.Append(s.Name).Append(" uses a verb color; ");
                if (AabbHits(s, 52f, 72f, 40f, 58f))
                    fail.Append(s.Name).Append(" blocks the open bowl; ");
                if (AabbHits(s, 52f, 72f, 58f, 76f))
                    fail.Append(s.Name).Append(" blocks the rim view slot; ");
                float top = s.Y + s.Sy * 0.5f;
                if (top < LandmarkCrown - 0.05f && !s.Name.EndsWith("_Pole", StringComparison.Ordinal)
                    && !s.Name.EndsWith("_PostS", StringComparison.Ordinal)
                    && !s.Name.EndsWith("_PostN", StringComparison.Ordinal))
                    fail.Append(s.Name).Append(" is not a skyline; ");
            }
            if (pieces < 10)
                fail.Append("landmarks are thin; ");

            int seen = 0;
            int samples = 0;
            for (float x = 8f; x <= 152f; x += 12f)
            {
                for (float z = 8f; z <= 92f; z += 12f)
                {
                    if (Sheltered(solids, x, z)) continue;
                    string zone = ZoneAt(x, z);
                    if (zone == null) continue;
                    samples++;
                    if (SeesZone(solids, x, z, zone)) seen++;
                    else if (samples - seen <= 3)
                        fail.Append(zone).Append(" hidden at ")
                            .Append(x.ToString("0", CultureInfo.InvariantCulture)).Append(',')
                            .Append(z.ToString("0", CultureInfo.InvariantCulture)).Append("; ");
                }
            }
            float[,] must =
            {
                { 8f, 8f }, { 14f, 58f }, { 34f, 38f }, { 36f, 88f }, { 78f, 90f },
                { 136f, 28f }, { 136f, 72f }, { 96f, 40f }, { 62f, 50f }, { 90f, 16f }, { 140f, 8f },
            };
            for (int i = 0; i < must.GetLength(0); i++)
            {
                float x = must[i, 0];
                float z = must[i, 1];
                string zone = ZoneAt(x, z);
                samples++;
                if (zone != null && SeesZone(solids, x, z, zone)) seen++;
                else
                    fail.Append(zone ?? "ground").Append(" hidden at ")
                        .Append(x.ToString("0", CultureInfo.InvariantCulture)).Append(',')
                        .Append(z.ToString("0", CultureInfo.InvariantCulture)).Append("; ");
            }
            for (int z = 0; z < ZoneBoxes.Length; z++)
            {
                if (!SeesZone(solids, 62f, 50f, ZoneBoxes[z].Id))
                    fail.Append("bowl cannot see ").Append(ZoneBoxes[z].Id).Append("; ");
            }
            if (RimToBowlHitsLandmark(solids))
                fail.Append("a landmark blocks the bowl from the rim; ");
            if (samples > 0 && seen < samples)
                fail.Append("landmark visibility ").Append(seen.ToString(CultureInfo.InvariantCulture))
                    .Append('/').Append(samples.ToString(CultureInfo.InvariantCulture)).Append("; ");
            return "landmarks " + zones.ToString(CultureInfo.InvariantCulture) + " zones visible";
        }

        static bool HasLandmark(Solid[] solids, string zone)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                if (solids[i].Kind == "landmark" && solids[i].Zone == zone
                    && solids[i].Name.StartsWith("Landmark_", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        static string ZoneAt(float x, float z)
        {
            for (int i = 0; i < ZoneBoxes.Length; i++)
            {
                ZoneBox b = ZoneBoxes[i];
                if (x >= b.X0 && x <= b.X1 && z >= b.Z0 && z <= b.Z1) return b.Id;
            }
            return null;
        }

        static bool Sheltered(Solid[] solids, float x, float z)
        {
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "landmark") continue;
                float top = s.Y + s.Sy * 0.5f;
                if (top < 2.5f) continue;
                if (DistXZ(x, z, s) < 2.5f) return true;
            }
            return false;
        }

        static bool SeesZone(Solid[] solids, float x, float z, string zone)
        {
            Solid crown = default;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind != "landmark" || s.Zone != zone) continue;
                if (!s.Name.EndsWith("_Flag", StringComparison.Ordinal)
                    && !s.Name.EndsWith("_Beam", StringComparison.Ordinal))
                    continue;
                float d = DistPoint(x, z, s.X, s.Z);
                if (d < best)
                {
                    best = d;
                    crown = s;
                    found = true;
                }
            }
            if (!found) return false;
            float top = crown.Y + crown.Sy * 0.5f - 0.05f;
            float eye = EyeY(solids, x, z);
            if (RayHitsLandmark(solids, x, eye, z, crown.X, top, crown.Z, zone)) return true;
            bool wideX = crown.Sx >= crown.Sz;
            float w = (wideX ? crown.Sx : crown.Sz) * 0.4f;
            if (wideX)
            {
                if (RayHitsLandmark(solids, x, eye, z, crown.X - w, top, crown.Z, zone)) return true;
                if (RayHitsLandmark(solids, x, eye, z, crown.X + w, top, crown.Z, zone)) return true;
            }
            else
            {
                if (RayHitsLandmark(solids, x, eye, z, crown.X, top, crown.Z - w, zone)) return true;
                if (RayHitsLandmark(solids, x, eye, z, crown.X, top, crown.Z + w, zone)) return true;
            }
            return false;
        }

        static bool RayHitsLandmark(Solid[] solids, float x0, float y0, float z0, float x1, float y1, float z1, string zone)
        {
            float best = 2f;
            string who = null;
            string whoZone = null;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                float t;
                if (!SegmentHit(s, x0, y0, z0, x1, y1, z1, out t)) continue;
                if (t < 0.03f || t >= best) continue;
                best = t;
                who = s.Name;
                whoZone = s.Zone;
            }
            return who != null && whoZone == zone && who.StartsWith("Landmark_", StringComparison.Ordinal);
        }

        static bool RimToBowlHitsLandmark(Solid[] solids)
        {
            if (FirstLandmark(solids, 62f, 5.6f, 78f, 62f, 0.6f, 50f)) return true;
            if (FirstLandmark(solids, 51f, 6.6f, 76f, 62f, 0.6f, 50f)) return true;
            if (FirstLandmark(solids, 86f, 5.1f, 77f, 62f, 0.6f, 50f)) return true;
            return false;
        }

        static bool FirstLandmark(Solid[] solids, float x0, float y0, float z0, float x1, float y1, float z1)
        {
            float best = 2f;
            bool landmark = false;
            for (int i = 0; i < solids.Length; i++)
            {
                Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence") continue;
                float t;
                if (!SegmentHit(s, x0, y0, z0, x1, y1, z1, out t)) continue;
                if (t < 0.05f || t >= best) continue;
                best = t;
                landmark = s.Kind == "landmark";
            }
            return landmark;
        }

        static string PerfReport(Solid[] solids, Ramp[] ramps, StringBuilder fail)
        {
            string boot = ReadText("Assets/Scripts/Level/MegaParkP1Bootstrap.cs");
            if (boot == null
                || boot.IndexOf("isStatic = true", StringComparison.Ordinal) < 0
                || boot.IndexOf("enableInstancing = true", StringComparison.Ordinal) < 0
                || boot.IndexOf("StaticBatchingUtility.Combine", StringComparison.Ordinal) < 0
                || boot.IndexOf("Occlusion_", StringComparison.Ordinal) < 0
                || boot.IndexOf("PlanRampColliders", StringComparison.Ordinal) < 0)
                fail.Append("static batching or collider merge is not wired; ");

            SplitSand(ramps, out List<int> pairA, out List<int> pairB, out List<int> alone, out float gap);
            if (gap > MeshMatch)
                fail.Append("merged collider gap ").Append(gap.ToString("0.000", CultureInfo.InvariantCulture)).Append("; ");
            int rampBefore = ramps.Length;
            int rampAfter = alone.Count + pairA.Count;
            const int paint = 21;
            const int spawnRenderers = 12;
            const int spawnBatches = 7;
            const int dynamicDraws = 35;
            const int dynamicColliders = 25;
            int dress = DressBatchCount();
            int drawsBefore = solids.Length + ramps.Length + paint + spawnRenderers + dynamicDraws + DressPieceCount() + MinimapDraws;
            int drawsAfter = BatchCount(solids, true) + BatchCountRamps(ramps) + 12 + spawnBatches + dynamicDraws + dress + MinimapDraws;
            if (drawsAfter > DrawCap)
                fail.Append("draw calls ").Append(drawsAfter.ToString(CultureInfo.InvariantCulture)).Append(" over 120; ");
            int colsBefore = solids.Length + rampBefore + dynamicColliders;
            int colsAfter = solids.Length + rampAfter + dynamicColliders;
            return string.Format(
                CultureInfo.InvariantCulture,
                "perf draws {0}->{1} colliders {2}->{3} merge {4:0.000} m",
                drawsBefore, drawsAfter, colsBefore, colsAfter, gap);
        }

        static int BatchCount(Solid[] solids, bool solid)
        {
            var keys = new HashSet<string>();
            for (int i = 0; i < solids.Length; i++)
                keys.Add("s|" + solids[i].Zone + "|" + solids[i].Mat);
            return keys.Count;
        }

        static int BatchCountRamps(Ramp[] ramps)
        {
            var keys = new HashSet<string>();
            for (int i = 0; i < ramps.Length; i++)
                keys.Add("r|" + ramps[i].Zone + "|" + ramps[i].Mat);
            return keys.Count;
        }

        static float SandMergeGap(Ramp[] ramps)
        {
            SplitSand(ramps, out List<int> a, out List<int> b, out List<int> alone, out float gap);
            if (a.Count < 1 && alone.Count < 0) return gap;
            return gap;
        }

        static void SplitSand(Ramp[] ramps, out List<int> pairA, out List<int> pairB, out List<int> alone, out float gap)
        {
            pairA = new List<int>();
            pairB = new List<int>();
            alone = new List<int>();
            gap = 0f;
            var used = new bool[ramps.Length];
            for (int i = 0; i < ramps.Length; i++)
            {
                if (used[i]) continue;
                if (i + 1 < ramps.Length && SandPair(ramps[i], ramps[i + 1]))
                {
                    used[i] = true;
                    used[i + 1] = true;
                    pairA.Add(i);
                    pairB.Add(i + 1);
                    float d = Math.Abs(ramps[i].Y1 - ramps[i + 1].Y1) * 0.5f;
                    if (d > gap) gap = d;
                }
                else alone.Add(i);
            }
        }

        static bool SandPair(Ramp a, Ramp b)
        {
            if (!a.Name.StartsWith("SandCorner_", StringComparison.Ordinal)) return false;
            if (!b.Name.StartsWith("SandCorner_", StringComparison.Ordinal)) return false;
            if (!SameCorner(a.Name, b.Name)) return false;
            if (Math.Abs(Math.Abs(a.Z0 - b.Z0) - a.Width) > 0.02f) return false;
            if (Math.Abs(a.Width - b.Width) > 0.001f) return false;
            if (Math.Abs(a.X0 - b.X0) > 0.01f || Math.Abs(a.X1 - b.X1) > 0.01f) return false;
            return true;
        }

        static Ramp MergeStrip(Ramp a, Ramp b, int n)
        {
            float z = (a.Z0 + b.Z0) * 0.5f;
            float y1 = (a.Y1 + b.Y1) * 0.5f;
            return RampOf(
                "SandMerge_" + n.ToString(CultureInfo.InvariantCulture),
                "Z8", "sand", a.X0, a.Y0, z, a.X1, y1, z, a.Width + b.Width);
        }

        static string PulseReport(StringBuilder fail)
        {
            float r, g, b;
            if (!TryReadPulse(out r, out g, out b))
            {
                fail.Append("tag-back pulse color is missing; ");
                return "tagback missing";
            }
            string clash = null;
            float worstHue = 999f;
            for (int i = 0; i < Swatches.Length; i++)
            {
                Swatch tint = Swatches[i];
                if (tint.Name == "tag" || tint.Name == "sky") continue;
                float dh = HueDelta(r, g, b, tint.R, tint.G, tint.B);
                if (dh < worstHue) worstHue = dh;
                if (Satur(r, g, b) >= 0.35f && Satur(tint.R, tint.G, tint.B) >= 0.35f && dh < 28f && Contrast(r, g, b, tint.R, tint.G, tint.B) < 2.4f)
                    clash = tint.Name;
            }
            bool shifted = Math.Abs(r - 0.45f) > 0.02f || Math.Abs(g - 0.95f) > 0.02f || Math.Abs(b - 1f) > 0.02f;
            if (clash != null)
            {
                fail.Append("tag-back pulse clashes with ").Append(clash).Append("; ");
                return "tagback clashes " + clash;
            }
            return shifted ? "tagback aqua-cyan shifted distinct" : "tagback cyan distinct";
        }

        static bool TryReadPulse(out float r, out float g, out float b)
        {
            r = g = b = 0f;
            string src = ReadText("Assets/Scripts/Art/TagBackGlow.cs");
            if (src == null) return false;
            int i = src.IndexOf("Color Safe = new Color(", StringComparison.Ordinal);
            if (i < 0) return false;
            i = src.IndexOf('(', i);
            int end = src.IndexOf(')', i);
            if (end < 0) return false;
            string body = src.Substring(i + 1, end - i - 1);
            string[] bits = body.Split(',');
            if (bits.Length < 3) return false;
            return ParseFloat(bits[0], out r) && ParseFloat(bits[1], out g) && ParseFloat(bits[2], out b);
        }

        static bool ParseFloat(string text, out float value)
        {
            text = text.Trim().TrimEnd('f', 'F');
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        static float HueOf(float r, float g, float b)
        {
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float d = max - min;
            if (d < 1e-5f) return 0f;
            float h;
            if (max == r) h = ((g - b) / d) % 6f;
            else if (max == g) h = (b - r) / d + 2f;
            else h = (r - g) / d + 4f;
            if (h < 0f) h += 6f;
            return h * 60f;
        }

        static float HueDelta(float ar, float ag, float ab, float br, float bg, float bb)
        {
            float d = Math.Abs(HueOf(ar, ag, ab) - HueOf(br, bg, bb));
            if (d > 180f) d = 360f - d;
            return d;
        }

        static float Satur(float r, float g, float b)
        {
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float l = (max + min) * 0.5f;
            if (max - min < 1e-5f) return 0f;
            return (max - min) / (1f - Math.Abs(2f * l - 1f));
        }

        static float Contrast(float ar, float ag, float ab, float br, float bg, float bb)
        {
            float la = Lum(ar, ag, ab);
            float lb = Lum(br, bg, bb);
            float hi = Math.Max(la, lb);
            float lo = Math.Min(la, lb);
            return (hi + 0.05f) / (lo + 0.05f);
        }

        static float Lum(float r, float g, float b)
        {
            return 0.2126f * Lin(r) + 0.7152f * Lin(g) + 0.0722f * Lin(b);
        }

        static float Lin(float c)
        {
            if (c <= 0.04045f) return c / 12.92f;
            return (float)Math.Pow((c + 0.055f) / 1.055f, 2.4);
        }
    }
}
