using System;
using System.Globalization;
using System.IO;
using Tag.Art;
using Tag.Level;
using UnityEngine;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Menu poses at 30 fps. A rigid piece may sink 0.5 cm into a scene solid
    /// or into another piece. Joined neighbours may overlap within 3 cm of
    /// the joint. The rest-pose depth is not subtracted. Loading shows no runner.
    /// </summary>
    public static class MenuNoClip
    {
        const float Dt = 1f / 30f;
        const float Limit = 0.005f;
        const float Joint = 0.03f;
        const float Cell = 0.004f;
        static bool Verbose;
        static bool Trace;
        static int RayTests;
        static float[] PoseMax;
        static string[] PoseWhere;
        static int ForeignSamples;
        static bool RailKnown;
        static float RailClear;
        static bool[] RestFail;
        static bool[] PoseNew;
        static int PairN;
        static int RigJoint;
        static int PosePairs;
        static string PoseNote;

        /// <summary>
        /// True when every self fail is the hip shell inside an upper leg.
        /// The trimmed rig owns that rest overlap.
        /// </summary>
        public static bool RestOverlapOnly;

        public static bool Run(string repo, out string line, bool verbose)
        {
            return Run(repo, out line, verbose, false);
        }

        public static bool Run(string repo, out string line, bool verbose, bool probe)
        {
            Verbose = verbose;
            Trace = probe;
            RayTests = 0;
            PoseMax = new float[6];
            PoseWhere = new string[6];
            ForeignSamples = 0;
            RestOverlapOnly = false;
            RailKnown = false;
            RailClear = 0f;
            RestFail = null;
            PoseNew = null;
            PairN = 0;
            RigJoint = 0;
            PosePairs = 0;
            PoseNote = "";
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int screens = 0;
            int frames = 0;
            int fails = 0;
            float selfMax = 0f;
            float worldMax = 0f;
            string worstSelf = "";
            string worstWorld = "";
            line = "no-clip screens=0 frames=0 worldMax=0.00 selfMax=0.00 fails=1";
            if (!LockMenu(repo))
            {
                fails = 1;
                worstWorld = "menu source drifted";
                Finish(screens, frames, selfMax, worldMax, fails, out line);
                if (verbose) Console.Error.WriteLine("no-clip " + worstWorld);
                return false;
            }

            Rig rig;
            try
            {
                rig = Rig.Load(Path.Combine(repo, "Docs", "UiStills", "hier-rigid.bin"));
            }
            catch (Exception ex)
            {
                fails = 1;
                Finish(screens, frames, selfMax, worldMax, fails, out line);
                Console.Error.WriteLine("no-clip rig " + ex.Message);
                return false;
            }

            if (!EulerHolds() || !RestFootHolds(rig))
            {
                fails = 1;
                worstWorld = "rig basis";
                Finish(5, frames, selfMax, worldMax, fails, out line);
                Console.Error.WriteLine("no-clip " + worstWorld);
                return false;
            }

            CaptureRest(rig);
            var hit = new Hit();
            Title(rig, ref frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
            screens++;
            Main(rig, repo, ref frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
            screens++;
            Cards(rig, repo, ref frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
            screens++;
            Results(rig, ref frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
            screens++;
            screens++;
            TitleRail(rig, ref fails, ref worldMax, ref worstWorld);
            RestOverlapOnly = fails > 0 && ForeignSamples == 0 && worldMax <= Limit && PosePairs == 0;
            Finish(screens, frames, selfMax, worldMax, fails, out line);
            if (verbose || fails != 0)
            {
                Console.Error.WriteLine("no-clip worst-self " + worstSelf);
                Console.Error.WriteLine("no-clip worst-world " + worstWorld);
                string[] poseName = { "idle", "ready", "run", "step", "cheer", "slump" };
                for (int i = 0; i < poseName.Length; i++)
                    Console.Error.WriteLine("no-clip pose " + poseName[i] + " " + Cm(PoseAt(i)) + " " + (PoseWhere[i] ?? ""));
                Console.Error.WriteLine("no-clip foreign=" + ForeignSamples.ToString(CultureInfo.InvariantCulture)
                    + " rest-overlap-only=" + (RestOverlapOnly ? "yes" : "no")
                    + " rigJoint=" + RigJoint.ToString(CultureInfo.InvariantCulture)
                    + " pose=" + PosePairs.ToString(CultureInfo.InvariantCulture)
                    + (PoseNote.Length == 0 ? "" : " " + PoseNote));
                Console.Error.WriteLine("no-clip rays=" + RayTests.ToString(CultureInfo.InvariantCulture)
                    + " ms=" + sw.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture));
            }
            return fails == 0 && PosePairs == 0;
        }

        static void Finish(int screens, int frames, float selfMax, float worldMax, int fails, out string line)
        {
            line = "no-clip screens=" + screens.ToString(CultureInfo.InvariantCulture)
                + " frames=" + frames.ToString(CultureInfo.InvariantCulture)
                + " worldMax=" + Cm(worldMax)
                + " selfMax=" + Cm(selfMax)
                + " fails=" + fails.ToString(CultureInfo.InvariantCulture)
                + " idle=" + Cm(PoseAt(0))
                + " ready=" + Cm(PoseAt(1))
                + " run=" + Cm(PoseAt(2))
                + " step=" + Cm(PoseAt(3))
                + " cheer=" + Cm(PoseAt(4))
                + " slump=" + Cm(PoseAt(5));
            if (RailKnown)
                line += " rail=" + Cm(RailClear);
            line += " rigJoint=" + RigJoint.ToString(CultureInfo.InvariantCulture)
                + " pose=" + PosePairs.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Directed piece pairs that already fail at the rest pose. rigJoint is
        /// that count. pose is a pair that fails on a live frame and did not
        /// fail at rest. A different shell of the same piece is not a new pair.
        /// </summary>
        static void CaptureRest(Rig rig)
        {
            PairN = rig.Pieces;
            int n = PairN * PairN;
            if (n < 1) n = 1;
            RestFail = new bool[n];
            PoseNew = new bool[n];
            RigJoint = 0;
            PosePairs = 0;
            PoseNote = "";
            var posed = new Posed(rig);
            posed.Place(new Pose(), 0f, 0f, 0f);
            for (int a = 0; a < PairN; a++)
            {
                for (int b = 0; b < PairN; b++)
                {
                    if (a == b) continue;
                    if (!DirectedFails(posed, a, posed, b)) continue;
                    RestFail[a * PairN + b] = true;
                    RigJoint++;
                }
            }
        }

        static void NotePosePair(Posed src, int a, int b)
        {
            if (RestFail == null || PoseNew == null || PairN <= 0) return;
            if (a < 0 || b < 0 || a >= PairN || b >= PairN) return;
            int i = a * PairN + b;
            if (RestFail[i] || PoseNew[i]) return;
            PoseNew[i] = true;
            PosePairs++;
            if (PoseNote.Length > 180) return;
            if (PoseNote.Length > 0) PoseNote += "; ";
            PoseNote += src.Rig.Piece[a].Name + " in " + src.Rig.Piece[b].Name;
        }

        static bool DirectedFails(Posed src, int a, Posed dst, int b)
        {
            if (!Aabb(src.MinX[a], src.MinY[a], src.MinZ[a], src.MaxX[a], src.MaxY[a], src.MaxZ[a], dst.MinX[b], dst.MinY[b], dst.MinZ[b], dst.MaxX[b], dst.MaxY[b], dst.MaxZ[b], 0.01f))
                return false;
            bool joined = src.Rig.Join[a] == b || src.Rig.Join[b] == a;
            float jx = 0f, jy = 0f, jz = 0f;
            if (joined)
            {
                int child = src.Rig.Join[a] == b ? a : b;
                jx = src.Ox[child];
                jy = src.Oy[child];
                jz = src.Oz[child];
            }
            int n = src.Rig.Piece[a].Samples;
            float[] sx = src.Wx[a];
            float[] sy = src.Wy[a];
            float[] sz = src.Wz[a];
            float minX = dst.MinX[b] - 0.01f, maxX = dst.MaxX[b] + 0.01f;
            float minY = dst.MinY[b] - 0.01f, maxY = dst.MaxY[b] + 0.01f;
            float minZ = dst.MinZ[b] - 0.01f, maxZ = dst.MaxZ[b] + 0.01f;
            for (int i = 0; i < n; i++)
            {
                if (sx[i] < minX || sx[i] > maxX || sy[i] < minY || sy[i] > maxY || sz[i] < minZ || sz[i] > maxZ)
                    continue;
                string shell;
                float depth = InsidePiece(dst, b, sx[i], sy[i], sz[i], joined, jx, jy, jz, out shell);
                if (depth > Limit) return true;
            }
            return false;
        }

        /// <summary>
        /// Title still on the south straight. The far-right runner stands by the
        /// bar posts. Foot and shin must clear that rail by 2 cm. rail= is that
        /// gap in centimetres.
        /// </summary>
        static void TitleRail(Rig rig, ref int fails, ref float worldMax, ref string worstWorld)
        {
            string note;
            float gap = TitleRailGap(rig, out note);
            RailClear = gap;
            RailKnown = true;
            if (gap < 0f && -gap > worldMax)
            {
                worldMax = -gap;
                worstWorld = note;
            }
            if (gap < 0.02f)
            {
                fails++;
                if (worstWorld.Length == 0) worstWorld = note;
            }
            if (Verbose)
                Console.Error.WriteLine("no-clip " + note);
        }

        public static string ProbePairs(string repo)
        {
            Rig rig = Rig.Load(Path.Combine(repo, "Docs", "UiStills", "hier-rigid.bin"));
            var posed = new Posed(rig);
            var sb = new System.Text.StringBuilder();
            ProbeOne(posed, new Pose(), "rest", sb);
            ProbeOne(posed, From(MenuAlive.Idle(1.2f, 0.4f)), "idle", sb);
            ProbeOne(posed, From(MenuAlive.Ready()), "ready", sb);
            ProbeOne(posed, From(MenuAlive.Run(1.5707963f / 2.4f)), "run", sb);
            ProbeOne(posed, From(MenuAlive.Step(0.5f / 0.28f)), "step", sb);
            ProbeOne(posed, From(MenuAlive.Cheer(0.4f, 1f)), "cheer", sb);
            ProbeOne(posed, From(MenuAlive.Slump(0.2f)), "slump", sb);
            return sb.ToString();
        }

        static void ProbeOne(Posed posed, Pose pose, string name, System.Text.StringBuilder sb)
        {
            posed.Place(pose, 0f, 0f, 0f);
            Rig rig = posed.Rig;
            sb.Append(name);
            for (int a = 0; a < rig.Pieces; a++)
            {
                for (int b = 0; b < rig.Pieces; b++)
                {
                    if (a == b) continue;
                    float deep = 0f;
                    int n = 0;
                    string from = "";
                    string into = "";
                    Piece pa = rig.Piece[a];
                    Piece pb = rig.Piece[b];
                    bool joined = rig.Join[a] == b || rig.Join[b] == a;
                    float jx = 0f, jy = 0f, jz = 0f;
                    if (joined)
                    {
                        int child = rig.Join[a] == b ? a : b;
                        jx = posed.Ox[child];
                        jy = posed.Oy[child];
                        jz = posed.Oz[child];
                    }
                    int samples = pa.Samples;
                    for (int i = 0; i < samples; i++)
                    {
                        if (posed.Wx[a][i] < posed.MinX[b] - 0.01f || posed.Wx[a][i] > posed.MaxX[b] + 0.01f) continue;
                        if (posed.Wy[a][i] < posed.MinY[b] - 0.01f || posed.Wy[a][i] > posed.MaxY[b] + 0.01f) continue;
                        if (posed.Wz[a][i] < posed.MinZ[b] - 0.01f || posed.Wz[a][i] > posed.MaxZ[b] + 0.01f) continue;
                        string shell;
                        float depth = InsidePiece(posed, b, posed.Wx[a][i], posed.Wy[a][i], posed.Wz[a][i], joined, jx, jy, jz, out shell);
                        if (depth <= Limit) continue;
                        n++;
                        if (depth > deep)
                        {
                            deep = depth;
                            from = SampleShell(posed, a, i);
                            into = shell;
                        }
                    }
                    if (n == 0) continue;
                    sb.Append('\n');
                    sb.Append("  ");
                    sb.Append(pa.Name);
                    sb.Append('/');
                    sb.Append(from);
                    sb.Append(" in ");
                    sb.Append(pb.Name);
                    sb.Append('/');
                    sb.Append(into);
                    sb.Append(' ');
                    sb.Append((deep * 100f).ToString("0.00", CultureInfo.InvariantCulture));
                    sb.Append(" n=");
                    sb.Append(n.ToString(CultureInfo.InvariantCulture));
                    sb.Append(RestPair(pa.Name, from, pb.Name, into) ? " hip" : " OTHER");
                }
            }
            sb.Append('\n');
        }

        public static string ProbeRail(string repo)
        {
            Rig rig = Rig.Load(Path.Combine(repo, "Docs", "UiStills", "hier-rigid.bin"));
            string note;
            float gap = TitleRailGap(rig, out note);
            return "rail=" + (gap * 100f).ToString("0.00", CultureInfo.InvariantCulture) + " " + note;
        }

        static float TitleRailGap(Rig rig, out string note)
        {
            float[] xs = { 75.45f, 77.15f, 78.85f, 80.55f };
            float halfPi = 1.5707963f;
            MenuAlive.Angles[] angles =
            {
                MenuAlive.Run(halfPi / 2.4f),
                MenuAlive.Step(0.5f / 0.28f),
                MenuAlive.Run(-halfPi / 2.4f),
                MenuAlive.Step(0.22f / 0.28f)
            };
            MegaParkP1Layout.Solid[] world = MegaParkP1Layout.BuildSolids();
            float best = 1e9f;
            float bestPost = 1e9f;
            string postNote = "post none";
            note = "rail clear";
            var posed = new Posed(rig);
            for (int i = 0; i < 4; i++)
            {
                posed.Place(From(angles[i]), 0f, 0f, 0f);
                for (int p = 0; p < rig.Pieces; p++)
                {
                    string piece = rig.Piece[p].Name;
                    if (piece == null) continue;
                    if (piece.IndexOf("Foot", StringComparison.Ordinal) < 0 && piece.IndexOf("LowerLeg", StringComparison.Ordinal) < 0)
                        continue;
                    int samples = rig.Piece[p].Samples;
                    for (int s = 0; s < samples; s++)
                    {
                        float x = xs[i] + posed.Wz[p][s];
                        float y = 0.2f + posed.Wy[p][s];
                        float z = 16f - posed.Wx[p][s];
                        for (int w = 0; w < world.Length; w++)
                        {
                            MegaParkP1Layout.Solid solid = world[w];
                            if (!RailKind(solid.Kind)) continue;
                            if (!NearRail(solid, x, z)) continue;
                            float gap = BoxGap(x, y, z, solid);
                            if (gap < best)
                            {
                                best = gap;
                                note = "P" + (i + 1).ToString(CultureInfo.InvariantCulture)
                                    + " " + piece + " " + solid.Name
                                    + " gap=" + (gap * 100f).ToString("0.00", CultureInfo.InvariantCulture);
                            }
                            if (solid.Kind == "post" && gap < bestPost)
                            {
                                bestPost = gap;
                                postNote = solid.Name + " " + (gap * 100f).ToString("0.00", CultureInfo.InvariantCulture);
                            }
                        }
                    }
                }
            }
            note = note + " post=" + postNote;
            return best;
        }

        static bool RailKind(string kind)
        {
            return kind == "post" || kind == "bar" || kind == "fence";
        }

        static bool NearRail(MegaParkP1Layout.Solid s, float x, float z)
        {
            float hx = s.Sx * 0.5f + 3f;
            float hz = s.Sz * 0.5f + 3f;
            if (x < s.X - hx || x > s.X + hx) return false;
            if (z < s.Z - hz || z > s.Z + hz) return false;
            return true;
        }

        static float BoxGap(float x, float y, float z, MegaParkP1Layout.Solid s)
        {
            float dx = Math.Abs(x - s.X) - s.Sx * 0.5f;
            float dy = Math.Abs(y - s.Y) - s.Sy * 0.5f;
            float dz = Math.Abs(z - s.Z) - s.Sz * 0.5f;
            if (dx <= 0f && dy <= 0f && dz <= 0f)
                return Math.Max(dx, Math.Max(dy, dz));
            float ox = dx > 0f ? dx : 0f;
            float oy = dy > 0f ? dy : 0f;
            float oz = dz > 0f ? dz : 0f;
            return (float)Math.Sqrt(ox * ox + oy * oy + oz * oz);
        }

        static float PoseAt(int i)
        {
            if (PoseMax == null || i < 0 || i >= PoseMax.Length) return 0f;
            return PoseMax[i];
        }

        static string Cm(float meters)
        {
            return (meters * 100f).ToString("0.00", CultureInfo.InvariantCulture);
        }

        static void Note(int pose, float local, int frame, string where)
        {
            if (PoseMax == null || pose < 0 || pose >= PoseMax.Length) return;
            if (local > PoseMax[pose])
            {
                PoseMax[pose] = local;
                if (PoseWhere != null) PoseWhere[pose] = "f=" + frame.ToString(CultureInfo.InvariantCulture) + " " + where;
            }
        }

        static bool Judge(Posed posed, Posed[] group, int index, Solid[] solids, string screen, int pose, int frame, ref int fails, ref float selfMax, ref float worldMax, ref string worstSelf, ref string worstWorld, Hit hit)
        {
            float localSelf = 0f;
            float localWorld = 0f;
            string ls = "";
            string lw = "";
            bool bad = Test(posed, group, index, solids, screen, ref localSelf, ref localWorld, ref ls, ref lw, hit);
            if (localSelf > selfMax)
            {
                selfMax = localSelf;
                worstSelf = ls;
            }
            if (localWorld > worldMax)
            {
                worldMax = localWorld;
                worstWorld = lw;
            }
            Note(pose, localSelf, frame, ls);
            if (bad) fails++;
            return bad;
        }

        public static void ExportPoses(string repo, string folder)
        {
            Directory.CreateDirectory(folder);
            Rig rig = Rig.Load(Path.Combine(repo, "Docs", "UiStills", "hier-rigid.bin"));
            float halfPi = 1.5707963f;
            WritePose(rig, Path.Combine(folder, "run_pos.tris"), From(MenuAlive.Run(halfPi / 2.4f)));
            WritePose(rig, Path.Combine(folder, "run_neg.tris"), From(MenuAlive.Run(-halfPi / 2.4f)));
            WritePose(rig, Path.Combine(folder, "step.tris"), From(MenuAlive.Step(0.5f / 0.28f)));
            WritePose(rig, Path.Combine(folder, "step_b.tris"), From(MenuAlive.Step(0.22f / 0.28f)));
            WritePose(rig, Path.Combine(folder, "idle_a.tris"), From(MenuAlive.Idle(halfPi, halfPi)));
            WritePose(rig, Path.Combine(folder, "idle_b.tris"), From(MenuAlive.Idle(-halfPi, 0f)));
            WritePose(rig, Path.Combine(folder, "ready.tris"), From(MenuAlive.Ready()));
            WritePose(rig, Path.Combine(folder, "cheer.tris"), From(MenuAlive.Cheer(halfPi / 2.1f, 1f)));
            WritePose(rig, Path.Combine(folder, "cheer_b.tris"), From(MenuAlive.Cheer(halfPi / 2.1f, 0.55f)));
            WritePose(rig, Path.Combine(folder, "cheer_c.tris"), From(MenuAlive.Cheer(halfPi / 2.1f, 0.3f)));
            WritePose(rig, Path.Combine(folder, "slump.tris"), From(MenuAlive.Slump(0f)));
        }

        /// <summary>
        /// Rest pose with the penetrating hip/thigh triangles marked red (mat 4).
        /// </summary>
        public static void ExportCelebrate(string repo, string folder)
        {
            Directory.CreateDirectory(folder);
            Rig rig = Rig.Load(Path.Combine(repo, "Docs", "UiStills", "hier-rigid.bin"));
            float peak = 1.5707963f / 2.1f;
            WriteOverlapPose(rig, Path.Combine(folder, "victory.tris"), From(MenuAlive.Cheer(peak, 1f)));
            WriteOverlapPose(rig, Path.Combine(folder, "pump.tris"), From(MenuAlive.Pump(0.4f)));
            WriteOverlapPose(rig, Path.Combine(folder, "chest.tris"), From(MenuAlive.Chest(0.4f)));
            WriteOverlapPose(rig, Path.Combine(folder, "slump.tris"), From(MenuAlive.Slump(0.2f)));
        }

        static void WriteOverlapPose(Rig rig, string path, Pose pose)
        {
            var posed = new Posed(rig);
            posed.Place(pose, 0f, 0f, 0f);
            int n = 0;
            for (int p = 0; p < rig.Pieces; p++)
                for (int s = 0; s < rig.Piece[p].Subs; s++)
                    n += rig.Piece[p].Sub[s].Tris;
            using (var fs = File.Create(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(0x52454948);
                bw.Write(n);
                for (int p = 0; p < rig.Pieces; p++)
                {
                    Piece piece = rig.Piece[p];
                    var q = new Rot { X = posed.Qx[p], Y = posed.Qy[p], Z = posed.Qz[p], W = posed.Qw[p] };
                    float ox = posed.Ox[p], oy = posed.Oy[p], oz = posed.Oz[p];
                    for (int s = 0; s < piece.Subs; s++)
                    {
                        Shell shell = piece.Sub[s];
                        for (int t = 0; t < shell.Tris; t++)
                        {
                            byte mat = OverlapAny(posed, p, shell, t) ? (byte)4 : MatOf(piece.Name, shell.Name);
                            bw.Write(mat);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I0[t]);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I1[t]);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I2[t]);
                        }
                    }
                }
            }
        }

        static bool OverlapAny(Posed posed, int piece, Shell shell, int t)
        {
            float ax, ay, az, bx, by, bz, cx, cy, cz;
            WorldVert(posed, piece, shell, shell.I0[t], out ax, out ay, out az);
            WorldVert(posed, piece, shell, shell.I1[t], out bx, out by, out bz);
            WorldVert(posed, piece, shell, shell.I2[t], out cx, out cy, out cz);
            float mx = (ax + bx + cx) / 3f;
            float my = (ay + by + cy) / 3f;
            float mz = (az + bz + cz) / 3f;
            Rig rig = posed.Rig;
            for (int b = 0; b < rig.Pieces; b++)
            {
                if (b == piece) continue;
                if (mx < posed.MinX[b] - 0.01f || mx > posed.MaxX[b] + 0.01f) continue;
                if (my < posed.MinY[b] - 0.01f || my > posed.MaxY[b] + 0.01f) continue;
                if (mz < posed.MinZ[b] - 0.01f || mz > posed.MaxZ[b] + 0.01f) continue;
                bool joined = rig.Join[piece] == b || rig.Join[b] == piece;
                float jx = 0f, jy = 0f, jz = 0f;
                if (joined)
                {
                    int child = rig.Join[piece] == b ? piece : b;
                    jx = posed.Ox[child];
                    jy = posed.Oy[child];
                    jz = posed.Oz[child];
                }
                string shellName;
                float depth = InsidePiece(posed, b, mx, my, mz, joined, jx, jy, jz, out shellName);
                if (depth > Limit) return true;
            }
            return false;
        }

        public static void ExportOverlap(string repo, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Rig rig = Rig.Load(Path.Combine(repo, "Docs", "UiStills", "hier-rigid.bin"));
            var posed = new Posed(rig);
            posed.Place(new Pose(), 0f, 0f, 0f);
            int n = 0;
            for (int p = 0; p < rig.Pieces; p++)
                for (int s = 0; s < rig.Piece[p].Subs; s++)
                    if (HipLegShell(rig.Piece[p].Name, rig.Piece[p].Sub[s].Name))
                        n += rig.Piece[p].Sub[s].Tris;
            using (var fs = File.Create(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(0x52454948);
                bw.Write(n);
                for (int p = 0; p < rig.Pieces; p++)
                {
                    Piece piece = rig.Piece[p];
                    var q = new Rot { X = posed.Qx[p], Y = posed.Qy[p], Z = posed.Qz[p], W = posed.Qw[p] };
                    float ox = posed.Ox[p], oy = posed.Oy[p], oz = posed.Oz[p];
                    for (int s = 0; s < piece.Subs; s++)
                    {
                        Shell shell = piece.Sub[s];
                        if (!HipLegShell(piece.Name, shell.Name)) continue;
                        for (int t = 0; t < shell.Tris; t++)
                        {
                            byte mat = OverlapTri(posed, p, shell, t) ? (byte)4 : MatOf(piece.Name, shell.Name);
                            bw.Write(mat);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I0[t]);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I1[t]);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I2[t]);
                        }
                    }
                }
            }
        }

        static bool HipLegShell(string piece, string shell)
        {
            return HipWord(piece) || HipWord(shell) || LegWord(piece) || LegWord(shell);
        }

        static bool OverlapTri(Posed posed, int piece, Shell shell, int t)
        {
            float ax, ay, az, bx, by, bz, cx, cy, cz;
            WorldVert(posed, piece, shell, shell.I0[t], out ax, out ay, out az);
            WorldVert(posed, piece, shell, shell.I1[t], out bx, out by, out bz);
            WorldVert(posed, piece, shell, shell.I2[t], out cx, out cy, out cz);
            float mx = (ax + bx + cx) / 3f;
            float my = (ay + by + cy) / 3f;
            float mz = (az + bz + cz) / 3f;
            Rig rig = posed.Rig;
            for (int b = 0; b < rig.Pieces; b++)
            {
                if (b == piece) continue;
                if (mx < posed.MinX[b] - 0.01f || mx > posed.MaxX[b] + 0.01f) continue;
                if (my < posed.MinY[b] - 0.01f || my > posed.MaxY[b] + 0.01f) continue;
                if (mz < posed.MinZ[b] - 0.01f || mz > posed.MaxZ[b] + 0.01f) continue;
                bool joined = rig.Join[piece] == b || rig.Join[b] == piece;
                float jx = 0f, jy = 0f, jz = 0f;
                if (joined)
                {
                    int child = rig.Join[piece] == b ? piece : b;
                    jx = posed.Ox[child];
                    jy = posed.Oy[child];
                    jz = posed.Oz[child];
                }
                string shellName;
                float depth = InsidePiece(posed, b, mx, my, mz, joined, jx, jy, jz, out shellName);
                if (depth > Limit && RestPair(rig.Piece[piece].Name, shell.Name, rig.Piece[b].Name, shellName))
                    return true;
            }
            return false;
        }

        static void WorldVert(Posed posed, int piece, Shell shell, int i, out float x, out float y, out float z)
        {
            var q = new Rot { X = posed.Qx[piece], Y = posed.Qy[piece], Z = posed.Qz[piece], W = posed.Qw[piece] };
            Rotate(q, shell.Vx[i], shell.Vy[i], shell.Vz[i], out x, out y, out z);
            x += posed.Ox[piece];
            y += posed.Oy[piece];
            z += posed.Oz[piece];
        }

        static void WritePose(Rig rig, string path, Pose pose)
        {
            var posed = new Posed(rig);
            posed.Place(pose, 0f, 0f, 0f);
            Console.WriteLine("pose " + Path.GetFileName(path)
                + " x=" + posed.MinXAll.ToString("0.00", CultureInfo.InvariantCulture) + ".." + posed.MaxXAll.ToString("0.00", CultureInfo.InvariantCulture)
                + " y=" + posed.MinYAll.ToString("0.00", CultureInfo.InvariantCulture) + ".." + posed.MaxYAll.ToString("0.00", CultureInfo.InvariantCulture)
                + " z=" + posed.MinZAll.ToString("0.00", CultureInfo.InvariantCulture) + ".." + posed.MaxZAll.ToString("0.00", CultureInfo.InvariantCulture));
            int n = 0;
            for (int p = 0; p < rig.Pieces; p++)
                for (int s = 0; s < rig.Piece[p].Subs; s++)
                    n += rig.Piece[p].Sub[s].Tris;
            using (var fs = File.Create(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(0x52454948);
                bw.Write(n);
                for (int p = 0; p < rig.Pieces; p++)
                {
                    Piece piece = rig.Piece[p];
                    var q = new Rot { X = posed.Qx[p], Y = posed.Qy[p], Z = posed.Qz[p], W = posed.Qw[p] };
                    float ox = posed.Ox[p], oy = posed.Oy[p], oz = posed.Oz[p];
                    for (int s = 0; s < piece.Subs; s++)
                    {
                        Shell shell = piece.Sub[s];
                        byte mat = MatOf(piece.Name, shell.Name);
                        for (int t = 0; t < shell.Tris; t++)
                        {
                            bw.Write(mat);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I0[t]);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I1[t]);
                            WriteVert(bw, q, ox, oy, oz, shell, shell.I2[t]);
                        }
                    }
                }
            }
        }

        static void WriteVert(BinaryWriter bw, Rot q, float ox, float oy, float oz, Shell shell, int i)
        {
            float x, y, z;
            Rotate(q, shell.Vx[i], shell.Vy[i], shell.Vz[i], out x, out y, out z);
            bw.Write(x + ox);
            bw.Write(y + oy);
            bw.Write(z + oz);
        }

        static byte MatOf(string piece, string sub)
        {
            string n = sub == null ? "" : sub;
            if (n.IndexOf("Eye", StringComparison.Ordinal) >= 0 || n.IndexOf("Pupil", StringComparison.Ordinal) >= 0) return 3;
            if (n.IndexOf("Joint", StringComparison.Ordinal) >= 0) return 2;
            if (n.IndexOf("Panel", StringComparison.Ordinal) >= 0) return 1;
            if (piece != null && (piece.StartsWith("Hand", StringComparison.Ordinal) || piece.StartsWith("Foot", StringComparison.Ordinal))) return 1;
            return 0;
        }

        static int Frames(float period)
        {
            int n = (int)Math.Ceiling(period / Dt - 1e-4);
            return n < 1 ? 1 : n;
        }

        static void Title(Rig rig, ref int frames, ref int fails, ref float selfMax, ref float worldMax, ref string worstSelf, ref string worstWorld, Hit hit)
        {
            int n = Frames(1f / 0.28f);
            float[] x = { -2.4f, -0.8f, 0.8f, 2.4f };
            var posed = new Posed[4];
            for (int i = 0; i < 4; i++) posed[i] = new Posed(rig);
            var solids = new Solid[2];
            for (int k = 0; k < n; k++)
            {
                float t = k * Dt;
                for (int i = 0; i < 4; i++)
                {
                    float age = t + i * 0.37f;
                    Pose pose = (i % 2 == 1) ? Step(age) : Run(age);
                    posed[i].Place(pose, x[i], 0.08f, 0f);
                }
                for (int i = 0; i < 4; i++)
                {
                    solids[0] = Solid.Cyl("pedestal", x[i], 0.05f, 0f, 0.575f, 0.04f, 0.575f);
                    solids[1] = Solid.Cyl("contact", x[i], 0.012f, 0f, 0.775f, 0.012f, 0.775f);
                    frames++;
                    Judge(posed[i], posed, i, solids, "title", (i % 2 == 1) ? 3 : 2, frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
                }
            }
        }

        static void Main(Rig rig, string repo, ref int frames, ref int fails, ref float selfMax, ref float worldMax, ref string worstSelf, ref string worstWorld, Hit hit)
        {
            int n = Frames((float)(Math.PI * 2.0 / 2.4));
            float[] phase = { 0f, 0.37f, 0.74f, 1.11f };
            var posed = new Posed(rig);
            var solids = new Solid[1];
            solids[0] = Solid.Box("ground", 0f, -1f, 0f, 40f, 1f, 40f);
            for (int i = 0; i < 4; i++)
            {
                for (int k = 0; k < n; k++)
                {
                    float age = k * Dt + phase[i];
                    posed.Place(Run(age), 0f, 0.2f, 0f);
                    frames++;
                    Judge(posed, null, 0, solids, "main", 2, frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
                }
            }
            BakeFloor(Path.Combine(repo, "Docs", "UiStills", "hier-run.tris"), 0.2f, 0f, ref worldMax, ref fails, ref worstWorld);
        }

        static void Cards(Rig rig, string repo, ref int frames, ref int fails, ref float selfMax, ref float worldMax, ref string worstSelf, ref string worstWorld, Hit hit)
        {
            int n = Frames((float)(Math.PI * 2.0 / IdlePose.ShiftRate));
            var posed = new Posed(rig);
            var solids = new Solid[2];
            solids[0] = Solid.Cyl("pedestal", 0f, 0.055f, 0f, 0.575f, 0.045f, 0.575f);
            solids[1] = Solid.Cyl("contact", 0f, 0.012f, 0f, 0.825f, 0.012f, 0.825f);
            for (int seat = 0; seat < 2; seat++)
            {
                for (int k = 0; k < n; k++)
                {
                    float shift = k * Dt * IdlePose.ShiftRate;
                    float breath = k * Dt * IdlePose.BreathRate;
                    posed.Place(Idle(shift, breath, 0f), 0f, 0.12f, 0f);
                    frames++;
                    Judge(posed, null, 0, solids, "characters", 0, frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
                }
            }
            for (int seat = 0; seat < 2; seat++)
            {
                float shift = 0f;
                float breath = 0f;
                float blend = 0f;
                float hop = 1f;
                for (int k = 0; k < 12; k++)
                {
                    float y = 0.12f + MenuPolish.Hop(hop);
                    posed.Place(Idle(shift, breath, blend), 0f, y, 0f);
                    frames++;
                    Judge(posed, null, 0, solids, "characters", blend < 0.5f ? 0 : 1, frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
                    shift += IdlePose.ShiftRate * Dt;
                    breath += IdlePose.BreathRate * Dt;
                    blend += Dt / 0.18f;
                    if (blend > 1f) blend = 1f;
                    hop -= Dt / 0.36f;
                    if (hop < 0f) hop = 0f;
                }
                posed.Place(Idle(0f, 0f, 1f), 0f, 0.12f, 0f);
                frames++;
                Judge(posed, null, 0, solids, "characters", 1, frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
            }
            for (int i = 0; i < 4; i++)
                BakeFloor(Path.Combine(repo, "Docs", "UiStills", "hier-idle-" + i.ToString(CultureInfo.InvariantCulture) + ".tris"), 0f, 0f, ref worldMax, ref fails, ref worstWorld);
        }

        static void Results(Rig rig, ref int frames, ref int fails, ref float selfMax, ref float worldMax, ref string worstSelf, ref string worstWorld, Hit hit)
        {
            int n = Frames(4f);
            var posed = new Posed[4];
            for (int i = 0; i < 4; i++) posed[i] = new Posed(rig);
            var solids = new Solid[4 + 18];
            for (int k = 0; k < n; k++)
            {
                float t = k * Dt;
                for (int rank = 0; rank < 4; rank++)
                {
                    Slot(rank, out float sx, out float height);
                    float wide = 0.92f;
                    float deep = 0.70f;
                    Pose pose;
                    int poseId;
                    float hop = 0f;
                    if (rank == 0) { pose = Cheer(t, 1f); poseId = 4; hop = MenuAlive.Hop(t); }
                    else if (rank == 3) { pose = Slump(t); poseId = 5; }
                    else if (rank == 2) { pose = Chest(t); poseId = 4; }
                    else { pose = Pump(t); poseId = 4; }
                    posed[rank].Place(pose, sx, height + 0.09f + hop, 0f);
                    solids[0] = Solid.Box("step", sx, height * 0.5f, 0f, wide * 0.5f, height * 0.5f, deep * 0.5f);
                    solids[1] = Solid.Box("trim", sx, height + 0.025f, 0f, wide * 0.5f, 0.025f, deep * 0.5f);
                    solids[2] = Solid.Cyl("shade", sx, height + 0.065f, 0f, 0.38f, 0.012f, 0.25f);
                    solids[3] = Solid.Box("face", sx, height * 0.48f, deep * 0.5f + 0.02f, wide * 0.46f, height * 0.28f, 0.018f);
                    for (int c = 0; c < 18; c++)
                    {
                        float ang = c * 0.55f;
                        float fall = Repeat(t * (0.8f + (c % 5) * 0.15f) + c * 0.2f, 3.2f);
                        float yaw = (t * 80f + c * 20f) * (float)(Math.PI / 180.0);
                        solids[4 + c] = Solid.YawBox("confetti", (float)Math.Sin(ang) * 1.35f, 3.4f - fall, 0.92f + (float)Math.Cos(ang) * 0.18f, 0.08f, 0.13f, 0.025f, yaw);
                    }
                    frames++;
                    Judge(posed[rank], posed, rank, solids, "results", poseId, frames, ref fails, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit);
                }
            }
        }

        static bool Test(Posed self, Posed[] group, int index, Solid[] solids, string screen, ref float selfMax, ref float worldMax, ref string worstSelf, ref string worstWorld, Hit hit)
        {
            bool bad = false;
            Rig rig = self.Rig;
            for (int a = 0; a < rig.Pieces; a++)
            {
                for (int b = a + 1; b < rig.Pieces; b++)
                {
                    if (SamplesIn(self, a, self, b, true, screen, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit)) bad = true;
                    if (SamplesIn(self, b, self, a, true, screen, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit)) bad = true;
                }
                if (solids == null) continue;
                for (int s = 0; s < solids.Length; s++)
                {
                    if (SamplesSolid(self, a, solids[s], screen, ref worldMax, ref worstWorld, hit)) bad = true;
                }
            }
            if (group == null) return bad;
            for (int g = 0; g < group.Length; g++)
            {
                if (g == index || group[g] == null) continue;
                if (!Aabb(self.MinXAll, self.MinYAll, self.MinZAll, self.MaxXAll, self.MaxYAll, self.MaxZAll, group[g].MinXAll, group[g].MinYAll, group[g].MinZAll, group[g].MaxXAll, group[g].MaxYAll, group[g].MaxZAll, 0.02f))
                    continue;
                for (int a = 0; a < rig.Pieces; a++)
                {
                    for (int b = 0; b < rig.Pieces; b++)
                    {
                        if (SamplesIn(self, a, group[g], b, false, screen, ref selfMax, ref worldMax, ref worstSelf, ref worstWorld, hit)) bad = true;
                    }
                }
            }
            return bad;
        }

        static bool SamplesIn(Posed src, int a, Posed dst, int b, bool sameBody, string screen, ref float selfMax, ref float worldMax, ref string worstSelf, ref string worstWorld, Hit hit)
        {
            Piece pa = src.Rig.Piece[a];
            Piece pb = dst.Rig.Piece[b];
            if (!Aabb(src.MinX[a], src.MinY[a], src.MinZ[a], src.MaxX[a], src.MaxY[a], src.MaxZ[a], dst.MinX[b], dst.MinY[b], dst.MinZ[b], dst.MaxX[b], dst.MaxY[b], dst.MaxZ[b], 0.01f))
                return false;
            bool joined = sameBody && (src.Rig.Join[a] == b || src.Rig.Join[b] == a);
            float jx = 0f, jy = 0f, jz = 0f;
            if (joined)
            {
                int child = src.Rig.Join[a] == b ? a : b;
                jx = src.Ox[child];
                jy = src.Oy[child];
                jz = src.Oz[child];
            }
            bool bad = false;
            int n = pa.Samples;
            float[] sx = src.Wx[a];
            float[] sy = src.Wy[a];
            float[] sz = src.Wz[a];
            float minX = dst.MinX[b] - 0.01f, maxX = dst.MaxX[b] + 0.01f;
            float minY = dst.MinY[b] - 0.01f, maxY = dst.MaxY[b] + 0.01f;
            float minZ = dst.MinZ[b] - 0.01f, maxZ = dst.MaxZ[b] + 0.01f;
            for (int i = 0; i < n; i++)
            {
                if (sx[i] < minX || sx[i] > maxX || sy[i] < minY || sy[i] > maxY || sz[i] < minZ || sz[i] > maxZ)
                    continue;
                string shell;
                float depth = InsidePiece(dst, b, sx[i], sy[i], sz[i], joined, jx, jy, jz, out shell);
                if (depth <= 0f) continue;
                if (sameBody)
                {
                    if (depth > selfMax)
                    {
                        selfMax = depth;
                        float jdx = sx[i] - jx, jdy = sy[i] - jy, jdz = sz[i] - jz;
                        float jd = (float)Math.Sqrt(jdx * jdx + jdy * jdy + jdz * jdz);
                        string from = SampleShell(src, a, i);
                        worstSelf = screen + " " + pa.Name + "/" + from + " in " + pb.Name + "/" + shell + " " + (depth * 100f).ToString("0.00", CultureInfo.InvariantCulture)
                            + " joint=" + (joined ? (jd * 100f).ToString("0.00", CultureInfo.InvariantCulture) : "-")
                            + " p=" + sx[i].ToString("0.00", CultureInfo.InvariantCulture) + "," + sy[i].ToString("0.00", CultureInfo.InvariantCulture) + "," + sz[i].ToString("0.00", CultureInfo.InvariantCulture);
                    }
                    if (depth > Limit && !RestPair(pa.Name, SampleShell(src, a, i), pb.Name, shell))
                        ForeignSamples++;
                    if (depth > Limit) NotePosePair(src, a, b);
                }
                else if (depth > worldMax)
                {
                    worldMax = depth;
                    worstWorld = screen + " " + pa.Name + " in " + pb.Name + " " + (depth * 100f).ToString("0.00", CultureInfo.InvariantCulture);
                }
                if (depth > Limit) bad = true;
                hit.N++;
            }
            return bad;
        }

        static bool SamplesSolid(Posed src, int a, Solid solid, string screen, ref float worldMax, ref string worst, Hit hit)
        {
            if (!Aabb(src.MinX[a], src.MinY[a], src.MinZ[a], src.MaxX[a], src.MaxY[a], src.MaxZ[a], solid.MinX, solid.MinY, solid.MinZ, solid.MaxX, solid.MaxY, solid.MaxZ, 0.01f))
                return false;
            bool bad = false;
            int n = src.Rig.Piece[a].Samples;
            float[] sx = src.Wx[a];
            float[] sy = src.Wy[a];
            float[] sz = src.Wz[a];
            for (int i = 0; i < n; i++)
            {
                if (sx[i] < solid.MinX || sx[i] > solid.MaxX || sy[i] < solid.MinY || sy[i] > solid.MaxY || sz[i] < solid.MinZ || sz[i] > solid.MaxZ)
                    continue;
                float depth = solid.Depth(sx[i], sy[i], sz[i]);
                if (depth <= 0f) continue;
                if (depth > worldMax)
                {
                    worldMax = depth;
                    worst = screen + " " + src.Rig.Piece[a].Name + " in " + solid.Name + " " + (depth * 100f).ToString("0.00", CultureInfo.InvariantCulture);
                }
                if (depth > Limit) bad = true;
                hit.N++;
            }
            return bad;
        }

        static float InsidePiece(Posed dst, int b, float wx, float wy, float wz, bool joined, float jx, float jy, float jz)
        {
            string ignored;
            return InsidePiece(dst, b, wx, wy, wz, joined, jx, jy, jz, out ignored);
        }

        static float InsidePiece(Posed dst, int b, float wx, float wy, float wz, bool joined, float jx, float jy, float jz, out string shell)
        {
            shell = "";
            if (joined)
            {
                float dx = wx - jx;
                float dy = wy - jy;
                float dz = wz - jz;
                if (dx * dx + dy * dy + dz * dz < Joint * Joint) return 0f;
            }
            float lx, ly, lz;
            Inv(dst.Qx[b], dst.Qy[b], dst.Qz[b], dst.Qw[b], wx - dst.Ox[b], wy - dst.Oy[b], wz - dst.Oz[b], out lx, out ly, out lz);
            Piece piece = dst.Rig.Piece[b];
            float best = 0f;
            for (int s = 0; s < piece.Subs; s++)
            {
                float d = piece.Sub[s].Depth(lx, ly, lz);
                if (d > best)
                {
                    best = d;
                    shell = piece.Sub[s].Name;
                }
            }
            return best;
        }

        static string SampleShell(Posed src, int piece, int sample)
        {
            Piece p = src.Rig.Piece[piece];
            if (p.ShellOf == null || sample < 0 || sample >= p.ShellOf.Length) return p.Name;
            int s = p.ShellOf[sample];
            if (s < 0 || s >= p.Subs || p.Sub[s] == null) return p.Name;
            return p.Sub[s].Name;
        }

        static bool RestPair(string pieceA, string shellA, string pieceB, string shellB)
        {
            bool hipA = HipWord(pieceA) || HipWord(shellA);
            bool hipB = HipWord(pieceB) || HipWord(shellB);
            bool legA = LegWord(pieceA) || LegWord(shellA);
            bool legB = LegWord(pieceB) || LegWord(shellB);
            return (hipA && legB) || (hipB && legA);
        }

        static bool HipWord(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return s.IndexOf("Hip", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Pelvis", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool LegWord(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return s.IndexOf("UpperLeg", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void BakeFloor(string path, float lift, float floorY, ref float worldMax, ref int fails, ref string worst)
        {
            if (!File.Exists(path))
            {
                fails++;
                worst = "missing " + path;
                return;
            }
            float minY = 1e9f;
            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                int magic = br.ReadInt32();
                if (magic != 0x52454948) return;
                int n = br.ReadInt32();
                for (int i = 0; i < n; i++)
                {
                    br.ReadByte();
                    for (int v = 0; v < 3; v++)
                    {
                        br.ReadSingle();
                        float y = br.ReadSingle();
                        br.ReadSingle();
                        if (y < minY) minY = y;
                    }
                }
            }
            float pen = floorY - (minY + lift);
            if (pen > worldMax)
            {
                worldMax = pen;
                worst = "bake " + Path.GetFileName(path) + " " + (pen * 100f).ToString("0.00", CultureInfo.InvariantCulture);
            }
            if (pen > Limit) fails++;
        }

        static Pose Run(float age)
        {
            return From(MenuAlive.Run(age));
        }

        static Pose Step(float age)
        {
            return From(MenuAlive.Step(age));
        }

        static Pose Idle(float shift, float breath, float ready)
        {
            return From(MenuAlive.Lerp(MenuAlive.Idle(shift, breath), MenuAlive.Ready(), ready));
        }

        static Pose Cheer(float t, float lean)
        {
            return From(MenuAlive.Cheer(t, lean));
        }

        static Pose Pump(float t)
        {
            return From(MenuAlive.Pump(t));
        }

        static Pose Chest(float t)
        {
            return From(MenuAlive.Chest(t));
        }

        static Pose Slump(float t)
        {
            return From(MenuAlive.Slump(t));
        }

        static Pose From(MenuAlive.Angles a)
        {
            var p = new Pose();
            p.RootPitch = a.RootPitch; p.RootYaw = a.RootYaw; p.RootRoll = a.RootRoll;
            p.Hip = a.Hip; p.HipYaw = a.HipYaw; p.HipRoll = a.HipRoll;
            p.Spine = a.Spine; p.SpineYaw = a.SpineYaw; p.SpineRoll = a.SpineRoll;
            p.Head = a.Head; p.HeadYaw = a.HeadYaw;
            p.ArmPitchL = a.ArmPitchL; p.ArmPitchR = a.ArmPitchR;
            p.ArmYawL = a.ArmYawL; p.ArmYawR = a.ArmYawR;
            p.ArmRollL = a.ArmRollL; p.ArmRollR = a.ArmRollR;
            p.ElbowL = a.ElbowL; p.ElbowR = a.ElbowR;
            p.ThighL = a.ThighL; p.ThighR = a.ThighR;
            p.KneeL = a.KneeL; p.KneeR = a.KneeR;
            return p;
        }

        static bool EulerHolds()
        {
            Rot q = Euler(0f, 90f, 0f);
            float x, y, z;
            Rotate(q, 0f, 0f, 1f, out x, out y, out z);
            return x > 0.99f && Math.Abs(y) < 0.01f && Math.Abs(z) < 0.01f;
        }

        static bool RestFootHolds(Rig rig)
        {
            var posed = new Posed(rig);
            posed.Place(new Pose(), 0f, 0f, 0f);
            int foot = -1;
            for (int i = 0; i < rig.Pieces; i++)
            {
                if (rig.Piece[i].Name == "Foot_L") foot = i;
            }
            if (foot < 0) return false;
            float min = 1e9f;
            for (int i = 0; i < rig.Piece[foot].Samples; i++)
            {
                if (posed.Wy[foot][i] < min) min = posed.Wy[foot][i];
            }
            return min > 0.02f && min < 0.12f;
        }

        static bool LockMenu(string repo)
        {
            string stride = Read(repo, "Assets/Scripts/UI/Menu/MenuStride.cs");
            string idle = Read(repo, "Assets/Scripts/UI/Menu/MenuIdle.cs");
            string cheer = Read(repo, "Assets/Scripts/UI/Menu/MenuCheer.cs");
            string preview = Read(repo, "Assets/Scripts/UI/Menu/MenuPreview.cs");
            string host = Read(repo, "Assets/Scripts/UI/Menu/MenuHost.cs");
            string alive = Read(repo, "Assets/Scripts/UI/Menu/MenuAlive.cs");
            if (stride == null || idle == null || cheer == null || preview == null || host == null || alive == null) return false;
            if (stride.IndexOf("MenuAlive.Run", StringComparison.Ordinal) < 0) return false;
            if (stride.IndexOf("MenuAlive.Step", StringComparison.Ordinal) < 0) return false;
            if (idle.IndexOf("MenuAlive.Idle", StringComparison.Ordinal) < 0) return false;
            if (idle.IndexOf("MenuAlive.Ready", StringComparison.Ordinal) < 0) return false;
            if (cheer.IndexOf("MenuAlive.Cheer", StringComparison.Ordinal) < 0) return false;
            if (cheer.IndexOf("MenuAlive.Slump", StringComparison.Ordinal) < 0) return false;
            if (alive.IndexOf("RootPitch = -16f", StringComparison.Ordinal) < 0) return false;
            if (alive.IndexOf("RootPitch = 16f", StringComparison.Ordinal) < 0) return false;
            if (alive.IndexOf("HeadYaw = 12f * s", StringComparison.Ordinal) < 0) return false;
            if (cheer.IndexOf("height = 0.72f", StringComparison.Ordinal) < 0) return false;
            if (cheer.IndexOf("x = -1.58f", StringComparison.Ordinal) < 0) return false;
            if (cheer.IndexOf("x = 3.27f", StringComparison.Ordinal) < 0) return false;
            if (preview.IndexOf("new Vector3(0f, 0.08f, 0f)", StringComparison.Ordinal) < 0) return false;
            if (preview.IndexOf("new Vector3(0f, 0.12f, 0f)", StringComparison.Ordinal) < 0) return false;
            if (preview.IndexOf("new Vector3(0f, 0.04f, 0f)", StringComparison.Ordinal) < 0) return false;
            if (preview.IndexOf("new Vector3(1.15f, 0.04f, 1.15f)", StringComparison.Ordinal) < 0) return false;
            if (preview.IndexOf("new Vector3(1.15f, 0.045f, 1.15f)", StringComparison.Ordinal) < 0) return false;
            if (preview.IndexOf("Begin(i % 2 == 1, i * 0.37f)", StringComparison.Ordinal) < 0) return false;
            if (preview.IndexOf("0.92f + Mathf.Cos(ang) * 0.18f", StringComparison.Ordinal) < 0) return false;
            int load = host.IndexOf("void BuildLoading()", StringComparison.Ordinal);
            int next = load < 0 ? -1 : host.IndexOf("void ", load + 20, StringComparison.Ordinal);
            if (load < 0 || next < 0) return false;
            if (host.Substring(load, next - load).IndexOf("MenuMannequin", StringComparison.Ordinal) >= 0) return false;
            if (host.IndexOf("MenuSession.CardBody(s)", StringComparison.Ordinal) < 0) return false;
            int apply = preview.IndexOf("public void Apply(", StringComparison.Ordinal);
            int podium = preview.IndexOf("public void ShowPodium(", StringComparison.Ordinal);
            if (apply < 0 || podium < apply) return false;
            string cast = preview.Substring(apply, podium - apply);
            if (cast.IndexOf("MenuMannequin.NameOf(hier)", StringComparison.Ordinal) < 0) return false;
            if (cast.IndexOf("MenuCheer.Dress", StringComparison.Ordinal) >= 0) return false;
            return true;
        }

        static string Read(string repo, string rel)
        {
            string path = Path.Combine(repo, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return null;
            return File.ReadAllText(path);
        }

        static void Slot(int rank, out float x, out float height)
        {
            if (rank <= 0) { x = -0.06f; height = 0.72f; return; }
            if (rank == 1) { x = -1.58f; height = 0.50f; return; }
            if (rank == 2) { x = 1.55f; height = 0.34f; return; }
            x = 3.27f;
            height = 0.20f;
        }

        static float Repeat(float t, float length)
        {
            if (length <= 0f) return 0f;
            float m = t % length;
            if (m < 0f) m += length;
            return m;
        }

        static bool Aabb(float ax0, float ay0, float az0, float ax1, float ay1, float az1, float bx0, float by0, float bz0, float bx1, float by1, float bz1, float pad)
        {
            if (ax1 + pad < bx0 || bx1 + pad < ax0) return false;
            if (ay1 + pad < by0 || by1 + pad < ay0) return false;
            if (az1 + pad < bz0 || bz1 + pad < az0) return false;
            return true;
        }

        struct Pose
        {
            public float RootPitch, RootYaw, RootRoll;
            public float Hip, HipYaw, HipRoll;
            public float Spine, SpineYaw, SpineRoll;
            public float Head, HeadYaw;
            public float ArmPitchL, ArmPitchR, ArmYawL, ArmYawR, ArmRollL, ArmRollR;
            public float ElbowL, ElbowR;
            public float ThighL, ThighR, KneeL, KneeR;
        }

        struct Rot
        {
            public float X, Y, Z, W;
        }

        struct Hit
        {
            public int N;
        }

        static Rot Euler(float xDeg, float yDeg, float zDeg)
        {
            float x = xDeg * (float)(Math.PI / 180.0) * 0.5f;
            float y = yDeg * (float)(Math.PI / 180.0) * 0.5f;
            float z = zDeg * (float)(Math.PI / 180.0) * 0.5f;
            float cx = (float)Math.Cos(x), sx = (float)Math.Sin(x);
            float cy = (float)Math.Cos(y), sy = (float)Math.Sin(y);
            float cz = (float)Math.Cos(z), sz = (float)Math.Sin(z);
            Rot qx = new Rot { X = sx, Y = 0f, Z = 0f, W = cx };
            Rot qy = new Rot { X = 0f, Y = sy, Z = 0f, W = cy };
            Rot qz = new Rot { X = 0f, Y = 0f, Z = sz, W = cz };
            return Mul(qy, Mul(qx, qz));
        }

        static Rot Mul(Rot a, Rot b)
        {
            return new Rot
            {
                X = a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
                Y = a.W * b.Y + a.Y * b.W + a.Z * b.X - a.X * b.Z,
                Z = a.W * b.Z + a.Z * b.W + a.X * b.Y - a.Y * b.X,
                W = a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
            };
        }

        static void Rotate(Rot q, float vx, float vy, float vz, out float x, out float y, out float z)
        {
            float tx = 2f * (q.Y * vz - q.Z * vy);
            float ty = 2f * (q.Z * vx - q.X * vz);
            float tz = 2f * (q.X * vy - q.Y * vx);
            x = vx + q.W * tx + (q.Y * tz - q.Z * ty);
            y = vy + q.W * ty + (q.Z * tx - q.X * tz);
            z = vz + q.W * tz + (q.X * ty - q.Y * tx);
        }

        static void Inv(float qx, float qy, float qz, float qw, float vx, float vy, float vz, out float x, out float y, out float z)
        {
            Rotate(new Rot { X = -qx, Y = -qy, Z = -qz, W = qw }, vx, vy, vz, out x, out y, out z);
        }

        sealed class Posed
        {
            public readonly Rig Rig;
            public readonly float[][] Wx;
            public readonly float[][] Wy;
            public readonly float[][] Wz;
            public readonly float[] MinX, MinY, MinZ, MaxX, MaxY, MaxZ;
            public readonly float[] Ox, Oy, Oz, Qx, Qy, Qz, Qw;
            public float MinXAll, MinYAll, MinZAll, MaxXAll, MaxYAll, MaxZAll;

            public Posed(Rig rig)
            {
                Rig = rig;
                int n = rig.Pieces;
                Wx = new float[n][];
                Wy = new float[n][];
                Wz = new float[n][];
                MinX = new float[n]; MinY = new float[n]; MinZ = new float[n];
                MaxX = new float[n]; MaxY = new float[n]; MaxZ = new float[n];
                Ox = new float[n]; Oy = new float[n]; Oz = new float[n];
                Qx = new float[n]; Qy = new float[n]; Qz = new float[n]; Qw = new float[n];
                for (int i = 0; i < n; i++)
                {
                    int s = rig.Piece[i].Samples;
                    Wx[i] = new float[s];
                    Wy[i] = new float[s];
                    Wz[i] = new float[s];
                }
            }

            public void Place(Pose pose, float ox, float oy, float oz)
            {
                int bones = Rig.Bones;
                var px = new float[bones];
                var py = new float[bones];
                var pz = new float[bones];
                var qx = new float[bones];
                var qy = new float[bones];
                var qz = new float[bones];
                var qw = new float[bones];
                var done = new bool[bones];
                for (int step = 0; step < bones; step++)
                {
                    for (int i = 0; i < bones; i++)
                    {
                        if (done[i]) continue;
                        int parent = Rig.Parent[i];
                        if (parent >= 0 && !done[parent]) continue;
                        float ex, ey, ez;
                        Channels(Rig.Name[i], pose, out ex, out ey, out ez);
                        float bx = Rig.Lx[i], by = Rig.Ly[i], bz = Rig.Lz[i];
                        Rot local = Mul(Rig.Rest[i], Euler(ex, ey, ez));
                        if (parent < 0)
                        {
                            Rotate(local, bx, by, bz, out px[i], out py[i], out pz[i]);
                            px[i] += ox; py[i] += oy; pz[i] += oz;
                            qx[i] = local.X; qy[i] = local.Y; qz[i] = local.Z; qw[i] = local.W;
                        }
                        else
                        {
                            Rot pr = new Rot { X = qx[parent], Y = qy[parent], Z = qz[parent], W = qw[parent] };
                            float lx, ly, lz;
                            Rotate(pr, bx, by, bz, out lx, out ly, out lz);
                            px[i] = px[parent] + lx;
                            py[i] = py[parent] + ly;
                            pz[i] = pz[parent] + lz;
                            Rot wr = Mul(pr, local);
                            qx[i] = wr.X; qy[i] = wr.Y; qz[i] = wr.Z; qw[i] = wr.W;
                        }
                        done[i] = true;
                    }
                }
                MinXAll = 1e9f; MinYAll = 1e9f; MinZAll = 1e9f;
                MaxXAll = -1e9f; MaxYAll = -1e9f; MaxZAll = -1e9f;
                for (int p = 0; p < Rig.Pieces; p++)
                {
                    int b = Rig.Piece[p].Bone;
                    Ox[p] = px[b]; Oy[p] = py[b]; Oz[p] = pz[b];
                    Qx[p] = qx[b]; Qy[p] = qy[b]; Qz[p] = qz[b]; Qw[p] = qw[b];
                    Rot r = new Rot { X = qx[b], Y = qy[b], Z = qz[b], W = qw[b] };
                    Piece piece = Rig.Piece[p];
                    float minX = 1e9f, minY = 1e9f, minZ = 1e9f;
                    float maxX = -1e9f, maxY = -1e9f, maxZ = -1e9f;
                    for (int i = 0; i < piece.Samples; i++)
                    {
                        float x, y, z;
                        Rotate(r, piece.Sx[i], piece.Sy[i], piece.Sz[i], out x, out y, out z);
                        x += px[b]; y += py[b]; z += pz[b];
                        Wx[p][i] = x; Wy[p][i] = y; Wz[p][i] = z;
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (z < minZ) minZ = z;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                        if (z > maxZ) maxZ = z;
                    }
                    MinX[p] = minX; MinY[p] = minY; MinZ[p] = minZ;
                    MaxX[p] = maxX; MaxY[p] = maxY; MaxZ[p] = maxZ;
                    if (minX < MinXAll) MinXAll = minX;
                    if (minY < MinYAll) MinYAll = minY;
                    if (minZ < MinZAll) MinZAll = minZ;
                    if (maxX > MaxXAll) MaxXAll = maxX;
                    if (maxY > MaxYAll) MaxYAll = maxY;
                    if (maxZ > MaxZAll) MaxZAll = maxZ;
                }
            }
        }

        static void Channels(string name, Pose p, out float x, out float y, out float z)
        {
            x = 0f; y = 0f; z = 0f;
            if (name == "DummyRoot") { x = p.RootPitch; y = p.RootYaw; z = p.RootRoll; return; }
            if (name == "Spine") { x = p.Spine; y = p.SpineYaw; z = p.SpineRoll; return; }
            if (name == "Head") { x = p.Head; y = p.HeadYaw; return; }
            if (name == "UpperArm_L") { x = p.ArmPitchL; y = p.ArmYawL; z = p.ArmRollL; return; }
            if (name == "UpperArm_R") { x = p.ArmPitchR; y = p.ArmYawR; z = p.ArmRollR; return; }
            if (name == "LowerArm_L") { x = p.ElbowL; return; }
            if (name == "LowerArm_R") { x = p.ElbowR; return; }
            if (name == "UpperLeg_L") { x = p.ThighL; return; }
            if (name == "UpperLeg_R") { x = p.ThighR; return; }
            if (name == "LowerLeg_L") { x = p.KneeL; return; }
            if (name == "LowerLeg_R") { x = p.KneeR; return; }
        }

        struct Solid
        {
            public string Name;
            public int Kind;
            public float Cx, Cy, Cz, Hx, Hy, Hz, Yaw, Cos, Sin;
            public float MinX, MinY, MinZ, MaxX, MaxY, MaxZ;

            public static Solid Box(string name, float cx, float cy, float cz, float hx, float hy, float hz)
            {
                var s = new Solid { Name = name, Kind = 0, Cx = cx, Cy = cy, Cz = cz, Hx = hx, Hy = hy, Hz = hz };
                s.Bound();
                return s;
            }

            public static Solid Cyl(string name, float cx, float cy, float cz, float rx, float hy, float rz)
            {
                var s = new Solid { Name = name, Kind = 1, Cx = cx, Cy = cy, Cz = cz, Hx = rx, Hy = hy, Hz = rz };
                s.Bound();
                return s;
            }

            public static Solid YawBox(string name, float cx, float cy, float cz, float hx, float hy, float hz, float yaw)
            {
                var s = new Solid
                {
                    Name = name, Kind = 2, Cx = cx, Cy = cy, Cz = cz, Hx = hx, Hy = hy, Hz = hz, Yaw = yaw,
                    Cos = (float)Math.Cos(yaw), Sin = (float)Math.Sin(yaw)
                };
                s.Bound();
                return s;
            }

            void Bound()
            {
                float hx = Hx, hz = Hz;
                if (Kind == 2)
                {
                    float ax = Math.Abs(Cos) * Hx + Math.Abs(Sin) * Hz;
                    float az = Math.Abs(Sin) * Hx + Math.Abs(Cos) * Hz;
                    hx = ax; hz = az;
                }
                MinX = Cx - hx; MaxX = Cx + hx;
                MinY = Cy - Hy; MaxY = Cy + Hy;
                MinZ = Cz - hz; MaxZ = Cz + hz;
            }

            public float Depth(float x, float y, float z)
            {
                if (Kind == 1) return CylDepth(x, y, z);
                float lx = x - Cx;
                float ly = y - Cy;
                float lz = z - Cz;
                if (Kind == 2)
                {
                    float rx = Cos * lx - Sin * lz;
                    float rz = Sin * lx + Cos * lz;
                    lx = rx; lz = rz;
                }
                float dx = Hx - Math.Abs(lx);
                float dy = Hy - Math.Abs(ly);
                float dz = Hz - Math.Abs(lz);
                if (dx < 0f || dy < 0f || dz < 0f) return 0f;
                float d = dx < dy ? dx : dy;
                if (dz < d) d = dz;
                return d;
            }

            float CylDepth(float x, float y, float z)
            {
                float ly = y - Cy;
                float ay = Math.Abs(ly);
                if (ay > Hy) return 0f;
                float lx = x - Cx;
                float lz = z - Cz;
                float nx = lx / Hx;
                float nz = lz / Hz;
                float s2 = nx * nx + nz * nz;
                if (s2 > 1f) return 0f;
                float yDepth = Hy - ay;
                float s = (float)Math.Sqrt(s2);
                float radial;
                if (Math.Abs(Hx - Hz) < 1e-4f)
                    radial = Hx - (float)Math.Sqrt(lx * lx + lz * lz);
                else if (s < 1e-5f)
                    radial = Hx < Hz ? Hx : Hz;
                else
                {
                    float rad = (float)Math.Sqrt(lx * lx + lz * lz);
                    radial = rad * (1f - s) / s;
                }
                return yDepth < radial ? yDepth : radial;
            }
        }

        sealed class Shell
        {
            public float[] Vx, Vy, Vz;
            public int[] I0, I1, I2;
            public int Tris;
            public string Name;
            public float MinX, MinY, MinZ, MaxX, MaxY, MaxZ;
            public int Nx, Ny, Nz;
            public float Ox, Oy, Oz;
            public byte[] Tag;
            public int[] NodeL, NodeR, NodeS, NodeC;
            public float[] NminX, NminY, NminZ, NmaxX, NmaxY, NmaxZ;
            public int Nodes;
            public int[] Order;
            public int[] Scratch;
            public bool Leaky;

            public void Build()
            {
                MinX = MinY = MinZ = 1e9f;
                MaxX = MaxY = MaxZ = -1e9f;
                for (int i = 0; i < Vx.Length; i++)
                {
                    if (Vx[i] < MinX) MinX = Vx[i];
                    if (Vy[i] < MinY) MinY = Vy[i];
                    if (Vz[i] < MinZ) MinZ = Vz[i];
                    if (Vx[i] > MaxX) MaxX = Vx[i];
                    if (Vy[i] > MaxY) MaxY = Vy[i];
                    if (Vz[i] > MaxZ) MaxZ = Vz[i];
                }
                const float pad = Cell * 3f;
                Ox = MinX - pad; Oy = MinY - pad; Oz = MinZ - pad;
                Nx = (int)((MaxX + pad - Ox) / Cell) + 1;
                Ny = (int)((MaxY + pad - Oy) / Cell) + 1;
                Nz = (int)((MaxZ + pad - Oz) / Cell) + 1;
                if (Nx < 1) Nx = 1;
                if (Ny < 1) Ny = 1;
                if (Nz < 1) Nz = 1;
                Tag = new byte[Nx * Ny * Nz];
                for (int t = 0; t < Tris; t++)
                    Stamp(t);
                Flood();
                float volume = Math.Abs(SignedVolume());
                int interior = 0;
                for (int i = 0; i < Tag.Length; i++)
                {
                    if (Tag[i] == 0) interior++;
                }
                float filled = interior * Cell * Cell * Cell;
                Leaky = volume > 0.0008f && filled < volume * 0.15f;
                if (Trace)
                {
                    Console.Error.WriteLine("shell tris=" + Tris.ToString(CultureInfo.InvariantCulture)
                        + " grid=" + Nx.ToString(CultureInfo.InvariantCulture) + "x" + Ny.ToString(CultureInfo.InvariantCulture) + "x" + Nz.ToString(CultureInfo.InvariantCulture)
                        + " interior=" + interior.ToString(CultureInfo.InvariantCulture)
                        + " vol=" + volume.ToString("0.0000", CultureInfo.InvariantCulture)
                        + " leaky=" + (Leaky ? "1" : "0"));
                    Console.Error.Flush();
                }
                BuildBvh();
                Scratch = new int[128];
            }

            void Stamp(int t)
            {
                int ia = I0[t], ib = I1[t], ic = I2[t];
                float ax = Vx[ia], ay = Vy[ia], az = Vz[ia];
                float bx = Vx[ib], by = Vy[ib], bz = Vz[ib];
                float cx = Vx[ic], cy = Vy[ic], cz = Vz[ic];
                float ab = (float)Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay) + (bz - az) * (bz - az));
                float ac = (float)Math.Sqrt((cx - ax) * (cx - ax) + (cy - ay) * (cy - ay) + (cz - az) * (cz - az));
                float bc = (float)Math.Sqrt((cx - bx) * (cx - bx) + (cy - by) * (cy - by) + (cz - bz) * (cz - bz));
                float edge = ab > ac ? ab : ac;
                if (bc > edge) edge = bc;
                int n = (int)(edge / 0.0016f);
                if (n < 1) n = 1;
                for (int i = 0; i <= n; i++)
                {
                    for (int j = 0; j <= n - i; j++)
                    {
                        float u = i / (float)n;
                        float v = j / (float)n;
                        float w = 1f - u - v;
                        Mark(ax * w + bx * u + cx * v, ay * w + by * u + cy * v, az * w + bz * u + cz * v);
                    }
                }
            }

            void Mark(float px, float py, float pz)
            {
                int ix = (int)((px - Ox) / Cell);
                int iy = (int)((py - Oy) / Cell);
                int iz = (int)((pz - Oz) / Cell);
                for (int dz = -1; dz <= 1; dz++)
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx != 0 && dy != 0) continue;
                    if (dx != 0 && dz != 0) continue;
                    if (dy != 0 && dz != 0) continue;
                    int x = ix + dx;
                    int y = iy + dy;
                    int z = iz + dz;
                    if ((uint)x >= (uint)Nx || (uint)y >= (uint)Ny || (uint)z >= (uint)Nz) continue;
                    Tag[(z * Ny + y) * Nx + x] = 2;
                }
            }

            void Flood()
            {
                var qx = new int[Tag.Length];
                var qy = new int[Tag.Length];
                var qz = new int[Tag.Length];
                int qh = 0, qt = 0;
                for (int z = 0; z < Nz && qh == 0; z++)
                for (int y = 0; y < Ny && qh == 0; y++)
                for (int x = 0; x < Nx; x++)
                {
                    if (Tag[(z * Ny + y) * Nx + x] != 0) continue;
                    qx[qt] = x; qy[qt] = y; qz[qt] = z; qt++;
                    Tag[(z * Ny + y) * Nx + x] = 1;
                    qh = 1;
                    break;
                }
                int head = 0;
                while (head < qt)
                {
                    int x = qx[head], y = qy[head], z = qz[head];
                    head++;
                    Push(qx, qy, qz, ref qt, x - 1, y, z);
                    Push(qx, qy, qz, ref qt, x + 1, y, z);
                    Push(qx, qy, qz, ref qt, x, y - 1, z);
                    Push(qx, qy, qz, ref qt, x, y + 1, z);
                    Push(qx, qy, qz, ref qt, x, y, z - 1);
                    Push(qx, qy, qz, ref qt, x, y, z + 1);
                }
            }

            void Push(int[] qx, int[] qy, int[] qz, ref int qt, int x, int y, int z)
            {
                if ((uint)x >= (uint)Nx || (uint)y >= (uint)Ny || (uint)z >= (uint)Nz) return;
                int idx = (z * Ny + y) * Nx + x;
                if (Tag[idx] != 0) return;
                Tag[idx] = 1;
                qx[qt] = x; qy[qt] = y; qz[qt] = z; qt++;
            }

            float SignedVolume()
            {
                double vol = 0.0;
                for (int t = 0; t < Tris; t++)
                {
                    int a = I0[t], b = I1[t], c = I2[t];
                    vol += (double)Vx[a] * ((double)Vy[b] * Vz[c] - (double)Vz[b] * Vy[c]);
                    vol += (double)Vy[a] * ((double)Vz[b] * Vx[c] - (double)Vx[b] * Vz[c]);
                    vol += (double)Vz[a] * ((double)Vx[b] * Vy[c] - (double)Vy[b] * Vx[c]);
                }
                return (float)(vol / 6.0);
            }

            void BuildBvh()
            {
                Order = new int[Tris];
                for (int i = 0; i < Tris; i++) Order[i] = i;
                int cap = Tris * 2 + 4;
                NodeL = new int[cap];
                NodeR = new int[cap];
                NodeS = new int[cap];
                NodeC = new int[cap];
                NminX = new float[cap]; NminY = new float[cap]; NminZ = new float[cap];
                NmaxX = new float[cap]; NmaxY = new float[cap]; NmaxZ = new float[cap];
                Nodes = 0;
                BuildNode(0, Tris);
            }

            int BuildNode(int start, int count)
            {
                int n = Nodes++;
                float minX = 1e9f, minY = 1e9f, minZ = 1e9f;
                float maxX = -1e9f, maxY = -1e9f, maxZ = -1e9f;
                for (int i = 0; i < count; i++)
                {
                    int t = Order[start + i];
                    Expand(I0[t], ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
                    Expand(I1[t], ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
                    Expand(I2[t], ref minX, ref minY, ref minZ, ref maxX, ref maxY, ref maxZ);
                }
                NminX[n] = minX; NminY[n] = minY; NminZ[n] = minZ;
                NmaxX[n] = maxX; NmaxY[n] = maxY; NmaxZ[n] = maxZ;
                if (count <= 8)
                {
                    NodeL[n] = -1; NodeR[n] = -1; NodeS[n] = start; NodeC[n] = count;
                    return n;
                }
                float ex = maxX - minX, ey = maxY - minY, ez = maxZ - minZ;
                int axis = 0;
                if (ey > ex && ey >= ez) axis = 1;
                else if (ez > ex && ez >= ey) axis = 2;
                int mid = count / 2;
                SortRange(start, start + count - 1, axis);
                NodeL[n] = BuildNode(start, mid);
                NodeR[n] = BuildNode(start + mid, count - mid);
                return n;
            }

            void SortRange(int lo, int hi, int axis)
            {
                while (lo < hi)
                {
                    int i = lo;
                    int j = hi;
                    float pv = Centroid(Order[(lo + hi) >> 1], axis);
                    while (i <= j)
                    {
                        while (Centroid(Order[i], axis) < pv) i++;
                        while (Centroid(Order[j], axis) > pv) j--;
                        if (i <= j)
                        {
                            int s = Order[i];
                            Order[i] = Order[j];
                            Order[j] = s;
                            i++;
                            j--;
                        }
                    }
                    if (j - lo < hi - i)
                    {
                        if (lo < j) SortRange(lo, j, axis);
                        lo = i;
                    }
                    else
                    {
                        if (i < hi) SortRange(i, hi, axis);
                        hi = j;
                    }
                }
            }

            void Expand(int v, ref float minX, ref float minY, ref float minZ, ref float maxX, ref float maxY, ref float maxZ)
            {
                if (Vx[v] < minX) minX = Vx[v];
                if (Vy[v] < minY) minY = Vy[v];
                if (Vz[v] < minZ) minZ = Vz[v];
                if (Vx[v] > maxX) maxX = Vx[v];
                if (Vy[v] > maxY) maxY = Vy[v];
                if (Vz[v] > maxZ) maxZ = Vz[v];
            }

            float Centroid(int t, int axis)
            {
                float a, b, c;
                if (axis == 0) { a = Vx[I0[t]]; b = Vx[I1[t]]; c = Vx[I2[t]]; }
                else if (axis == 1) { a = Vy[I0[t]]; b = Vy[I1[t]]; c = Vy[I2[t]]; }
                else { a = Vz[I0[t]]; b = Vz[I1[t]]; c = Vz[I2[t]]; }
                return (a + b + c) * (1f / 3f);
            }

            public float Depth(float x, float y, float z)
            {
                if (x < MinX || y < MinY || z < MinZ || x > MaxX || y > MaxY || z > MaxZ) return 0f;
                int ix = (int)((x - Ox) / Cell);
                int iy = (int)((y - Oy) / Cell);
                int iz = (int)((z - Oz) / Cell);
                if (ix < 0 || iy < 0 || iz < 0 || ix >= Nx || iy >= Ny || iz >= Nz) return 0f;
                int idx = (iz * Ny + iy) * Nx + ix;
                byte tag = Tag[idx];
                if (!Leaky && tag == 1 && !Near(ix, iy, iz)) return 0f;
                bool knownIn = !Leaky && tag == 0;
                if (!knownIn && !Odd(x, y, z, 1f, 0.017f, 0.031f)) return 0f;
                float dist = Closest(x, y, z);
                if (knownIn || dist <= 0.0008f) return dist;
                if (!Odd(x, y, z, 0.023f, 1f, 0.019f)) return 0f;
                return dist;
            }

            bool Near(int ix, int iy, int iz)
            {
                int x0 = ix > 0 ? ix - 1 : ix;
                int y0 = iy > 0 ? iy - 1 : iy;
                int z0 = iz > 0 ? iz - 1 : iz;
                int x1 = ix + 1 < Nx ? ix + 1 : ix;
                int y1 = iy + 1 < Ny ? iy + 1 : iy;
                int z1 = iz + 1 < Nz ? iz + 1 : iz;
                for (int z = z0; z <= z1; z++)
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (Tag[(z * Ny + y) * Nx + x] != 1) return true;
                }
                return false;
            }

            bool Odd(float ox, float oy, float oz, float dx, float dy, float dz)
            {
                float len = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                dx /= len; dy /= len; dz /= len;
                RayTests++;
                int hits = 0;
                int[] stack = Scratch;
                int sp = 0;
                stack[sp++] = 0;
                while (sp > 0)
                {
                    int n = stack[--sp];
                    if (!RayAabb(ox, oy, oz, dx, dy, dz, NminX[n], NminY[n], NminZ[n], NmaxX[n], NmaxY[n], NmaxZ[n]))
                        continue;
                    if (NodeL[n] < 0)
                    {
                        int start = NodeS[n];
                        int count = NodeC[n];
                        for (int i = 0; i < count; i++)
                        {
                            int t = Order[start + i];
                            if (RayTri(ox, oy, oz, dx, dy, dz, t)) hits++;
                        }
                    }
                    else
                    {
                        if (sp + 2 >= stack.Length)
                        {
                            var bigger = new int[stack.Length * 2];
                            Array.Copy(stack, bigger, stack.Length);
                            Scratch = bigger;
                            stack = Scratch;
                        }
                        stack[sp++] = NodeL[n];
                        stack[sp++] = NodeR[n];
                    }
                }
                return (hits & 1) == 1;
            }

            bool RayAabb(float ox, float oy, float oz, float dx, float dy, float dz, float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
            {
                float t0 = 0f;
                float t1 = 1e6f;
                if (!Slab(ox, dx, minX, maxX, ref t0, ref t1)) return false;
                if (!Slab(oy, dy, minY, maxY, ref t0, ref t1)) return false;
                if (!Slab(oz, dz, minZ, maxZ, ref t0, ref t1)) return false;
                return t1 >= t0 && t1 > 0f;
            }

            static bool Slab(float o, float d, float min, float max, ref float t0, ref float t1)
            {
                if (Math.Abs(d) < 1e-8f) return o >= min && o <= max;
                float inv = 1f / d;
                float a = (min - o) * inv;
                float b = (max - o) * inv;
                if (a > b) { float s = a; a = b; b = s; }
                if (a > t0) t0 = a;
                if (b < t1) t1 = b;
                return t1 >= t0;
            }

            bool RayTri(float ox, float oy, float oz, float dx, float dy, float dz, int t)
            {
                int a = I0[t], b = I1[t], c = I2[t];
                float e1x = Vx[b] - Vx[a], e1y = Vy[b] - Vy[a], e1z = Vz[b] - Vz[a];
                float e2x = Vx[c] - Vx[a], e2y = Vy[c] - Vy[a], e2z = Vz[c] - Vz[a];
                float px = dy * e2z - dz * e2y;
                float py = dz * e2x - dx * e2z;
                float pz = dx * e2y - dy * e2x;
                float det = e1x * px + e1y * py + e1z * pz;
                if (det > -1e-7f && det < 1e-7f) return false;
                float inv = 1f / det;
                float tx = ox - Vx[a], ty = oy - Vy[a], tz = oz - Vz[a];
                float u = (tx * px + ty * py + tz * pz) * inv;
                if (u < 0f || u > 1f) return false;
                float qx = ty * e1z - tz * e1y;
                float qy = tz * e1x - tx * e1z;
                float qz = tx * e1y - ty * e1x;
                float v = (dx * qx + dy * qy + dz * qz) * inv;
                if (v < 0f || u + v > 1f) return false;
                float hit = (e2x * qx + e2y * qy + e2z * qz) * inv;
                return hit > 1e-4f;
            }

            float Closest(float x, float y, float z)
            {
                float best = 1e6f;
                int[] stack = Scratch;
                int sp = 0;
                stack[sp++] = 0;
                while (sp > 0)
                {
                    int n = stack[--sp];
                    if (AabbDist2(x, y, z, NminX[n], NminY[n], NminZ[n], NmaxX[n], NmaxY[n], NmaxZ[n]) >= best * best)
                        continue;
                    if (NodeL[n] < 0)
                    {
                        int start = NodeS[n];
                        int count = NodeC[n];
                        for (int i = 0; i < count; i++)
                        {
                            float d = TriDist(x, y, z, Order[start + i]);
                            if (d < best) best = d;
                        }
                    }
                    else
                    {
                        if (sp + 2 >= stack.Length)
                        {
                            var bigger = new int[stack.Length * 2];
                            Array.Copy(stack, bigger, stack.Length);
                            Scratch = bigger;
                            stack = Scratch;
                        }
                        stack[sp++] = NodeL[n];
                        stack[sp++] = NodeR[n];
                    }
                }
                return best;
            }

            static float AabbDist2(float x, float y, float z, float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
            {
                float dx = 0f, dy = 0f, dz = 0f;
                if (x < minX) dx = minX - x;
                else if (x > maxX) dx = x - maxX;
                if (y < minY) dy = minY - y;
                else if (y > maxY) dy = y - maxY;
                if (z < minZ) dz = minZ - z;
                else if (z > maxZ) dz = z - maxZ;
                return dx * dx + dy * dy + dz * dz;
            }

            float TriDist(float px, float py, float pz, int t)
            {
                int ia = I0[t], ib = I1[t], ic = I2[t];
                return PointTri(px, py, pz, Vx[ia], Vy[ia], Vz[ia], Vx[ib], Vy[ib], Vz[ib], Vx[ic], Vy[ic], Vz[ic]);
            }

            static float PointTri(float px, float py, float pz, float ax, float ay, float az, float bx, float by, float bz, float cx, float cy, float cz)
            {
                float abx = bx - ax, aby = by - ay, abz = bz - az;
                float acx = cx - ax, acy = cy - ay, acz = cz - az;
                float apx = px - ax, apy = py - ay, apz = pz - az;
                float d1 = abx * apx + aby * apy + abz * apz;
                float d2 = acx * apx + acy * apy + acz * apz;
                if (d1 <= 0f && d2 <= 0f) return Dist(px, py, pz, ax, ay, az);
                float bpx = px - bx, bpy = py - by, bpz = pz - bz;
                float d3 = abx * bpx + aby * bpy + abz * bpz;
                float d4 = acx * bpx + acy * bpy + acz * bpz;
                if (d3 >= 0f && d4 <= d3) return Dist(px, py, pz, bx, by, bz);
                float cpx = px - cx, cpy = py - cy, cpz = pz - cz;
                float d5 = abx * cpx + aby * cpy + abz * cpz;
                float d6 = acx * cpx + acy * cpy + acz * cpz;
                if (d6 >= 0f && d5 <= d6) return Dist(px, py, pz, cx, cy, cz);
                float vc = d1 * d4 - d3 * d2;
                if (vc <= 0f && d1 >= 0f && d3 <= 0f)
                {
                    float v = d1 / (d1 - d3);
                    return Dist(px, py, pz, ax + abx * v, ay + aby * v, az + abz * v);
                }
                float vb = d5 * d2 - d1 * d6;
                if (vb <= 0f && d2 >= 0f && d6 <= 0f)
                {
                    float w = d2 / (d2 - d6);
                    return Dist(px, py, pz, ax + acx * w, ay + acy * w, az + acz * w);
                }
                float va = d3 * d6 - d5 * d4;
                if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
                {
                    float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                    return Dist(px, py, pz, bx + (cx - bx) * w, by + (cy - by) * w, bz + (cz - bz) * w);
                }
                float denom = 1f / (va + vb + vc);
                float v2 = vb * denom;
                float w2 = vc * denom;
                return Dist(px, py, pz, ax + abx * v2 + acx * w2, ay + aby * v2 + acy * w2, az + abz * v2 + acz * w2);
            }

            static float Dist(float ax, float ay, float az, float bx, float by, float bz)
            {
                float dx = ax - bx, dy = ay - by, dz = az - bz;
                return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
        }

        sealed class Piece
        {
            public string Name;
            public int Bone;
            public int Samples;
            public int Subs;
            public float[] Sx, Sy, Sz;
            public int[] ShellOf;
            public Shell[] Sub;
        }

        sealed class Rig
        {
            public int Bones;
            public int Pieces;
            public string[] Name;
            public int[] Parent;
            public float[] Lx, Ly, Lz;
            public Rot[] Rest;
            public int[] Join;
            public Piece[] Piece;

            public static Rig Load(string path)
            {
                var rig = new Rig();
                using (var fs = File.OpenRead(path))
                using (var br = new BinaryReader(fs))
                {
                    byte[] magic = br.ReadBytes(4);
                    if (magic[0] != (byte)'H' || magic[1] != (byte)'R' || magic[2] != (byte)'I' || magic[3] != (byte)'G')
                        throw new InvalidDataException("magic");
                    int version = br.ReadInt32();
                    if (version != 2) throw new InvalidDataException("version");
                    int bones = br.ReadInt32();
                    rig.Bones = bones;
                    rig.Name = new string[bones];
                    var parentName = new string[bones];
                    rig.Parent = new int[bones];
                    rig.Lx = new float[bones];
                    rig.Ly = new float[bones];
                    rig.Lz = new float[bones];
                    rig.Rest = new Rot[bones];
                    for (int i = 0; i < bones; i++)
                    {
                        rig.Name[i] = ReadName(br);
                        parentName[i] = ReadName(br);
                        rig.Lx[i] = br.ReadSingle();
                        rig.Ly[i] = br.ReadSingle();
                        rig.Lz[i] = br.ReadSingle();
                        rig.Rest[i] = new Rot
                        {
                            X = br.ReadSingle(),
                            Y = br.ReadSingle(),
                            Z = br.ReadSingle(),
                            W = br.ReadSingle()
                        };
                    }
                    for (int i = 0; i < bones; i++)
                    {
                        rig.Parent[i] = -1;
                        if (parentName[i].Length == 0) continue;
                        for (int j = 0; j < bones; j++)
                        {
                            if (rig.Name[j] == parentName[i]) rig.Parent[i] = j;
                        }
                    }
                    int pieces = br.ReadInt32();
                    rig.Pieces = pieces;
                    rig.Piece = new Piece[pieces];
                    var boneOf = new int[pieces];
                    for (int p = 0; p < pieces; p++)
                    {
                        var piece = new Piece();
                        piece.Name = ReadName(br);
                        piece.Bone = IndexOf(rig.Name, piece.Name);
                        boneOf[p] = piece.Bone;
                        int subs = br.ReadInt32();
                        piece.Subs = subs;
                        piece.Sub = new Shell[subs];
                        for (int s = 0; s < subs; s++)
                        {
                            var shell = new Shell();
                            shell.Name = ReadName(br);
                            int nv = br.ReadInt32();
                            int nt = br.ReadInt32();
                            shell.Vx = new float[nv];
                            shell.Vy = new float[nv];
                            shell.Vz = new float[nv];
                            for (int v = 0; v < nv; v++)
                            {
                                shell.Vx[v] = br.ReadSingle();
                                shell.Vy[v] = br.ReadSingle();
                                shell.Vz[v] = br.ReadSingle();
                            }
                            shell.Tris = nt;
                            shell.I0 = new int[nt];
                            shell.I1 = new int[nt];
                            shell.I2 = new int[nt];
                            for (int t = 0; t < nt; t++)
                            {
                                shell.I0[t] = br.ReadInt32();
                                shell.I1[t] = br.ReadInt32();
                                shell.I2[t] = br.ReadInt32();
                            }
                            shell.Build();
                            piece.Sub[s] = shell;
                        }
                        int ns = br.ReadInt32();
                        piece.Samples = ns;
                        piece.Sx = new float[ns];
                        piece.Sy = new float[ns];
                        piece.Sz = new float[ns];
                        for (int v = 0; v < ns; v++)
                        {
                            piece.Sx[v] = br.ReadSingle();
                            piece.Sy[v] = br.ReadSingle();
                            piece.Sz[v] = br.ReadSingle();
                        }
                        Downsample(piece, 0.006f);
                        TagSamples(piece);
                        rig.Piece[p] = piece;
                    }
                    rig.Join = new int[pieces];
                    for (int p = 0; p < pieces; p++)
                    {
                        rig.Join[p] = -1;
                        int b = rig.Parent[boneOf[p]];
                        while (b >= 0)
                        {
                            int found = -1;
                            for (int q = 0; q < pieces; q++)
                            {
                                if (boneOf[q] == b) found = q;
                            }
                            if (found >= 0)
                            {
                                rig.Join[p] = found;
                                break;
                            }
                            b = rig.Parent[b];
                        }
                    }
                }
                return rig;
            }

            static void TagSamples(Piece piece)
            {
                int n = piece.Samples;
                piece.ShellOf = new int[n];
                for (int i = 0; i < n; i++)
                {
                    float best = 1e12f;
                    int shell = 0;
                    float x = piece.Sx[i];
                    float y = piece.Sy[i];
                    float z = piece.Sz[i];
                    for (int s = 0; s < piece.Subs; s++)
                    {
                        Shell sub = piece.Sub[s];
                        int verts = sub.Vx.Length;
                        int step = verts > 800 ? 4 : 1;
                        for (int v = 0; v < verts; v += step)
                        {
                            float dx = sub.Vx[v] - x;
                            float dy = sub.Vy[v] - y;
                            float dz = sub.Vz[v] - z;
                            float d = dx * dx + dy * dy + dz * dz;
                            if (d < best)
                            {
                                best = d;
                                shell = s;
                            }
                        }
                    }
                    piece.ShellOf[i] = shell;
                }
            }

            static void Downsample(Piece piece, float cell)
            {
                int n = piece.Samples;
                var keepX = new float[n];
                var keepY = new float[n];
                var keepZ = new float[n];
                var seen = new System.Collections.Generic.Dictionary<long, int>(n);
                int m = 0;
                float inv = 1f / cell;
                for (int i = 0; i < n; i++)
                {
                    int ix = (int)Math.Floor(piece.Sx[i] * inv);
                    int iy = (int)Math.Floor(piece.Sy[i] * inv);
                    int iz = (int)Math.Floor(piece.Sz[i] * inv);
                    long key = ((long)(ix + 100000) << 42) | ((long)(iy + 100000) << 21) | (long)(iz + 100000);
                    if (seen.ContainsKey(key)) continue;
                    seen[key] = 1;
                    keepX[m] = piece.Sx[i];
                    keepY[m] = piece.Sy[i];
                    keepZ[m] = piece.Sz[i];
                    m++;
                }
                if (m == n) return;
                piece.Samples = m;
                piece.Sx = new float[m];
                piece.Sy = new float[m];
                piece.Sz = new float[m];
                Array.Copy(keepX, piece.Sx, m);
                Array.Copy(keepY, piece.Sy, m);
                Array.Copy(keepZ, piece.Sz, m);
                if (Trace)
                    Console.Error.WriteLine("samples " + piece.Name + " " + n.ToString(CultureInfo.InvariantCulture) + "->" + m.ToString(CultureInfo.InvariantCulture));
            }

            static int IndexOf(string[] names, string name)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] == name) return i;
                }
                return -1;
            }

            static string ReadName(BinaryReader br)
            {
                int n = br.ReadUInt16();
                if (n == 0) return "";
                return System.Text.Encoding.UTF8.GetString(br.ReadBytes(n));
            }
        }
    }
}
