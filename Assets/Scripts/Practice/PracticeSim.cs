using System;
using System.Globalization;
using Tag.Level;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Practice
{
    /// <summary>
    /// Drives a route with the kinematic step, the pad set, the zip hang,
    /// and the wall-run / wall-jump numbers from the motor. No new verb.
    /// </summary>
    public static class PracticeSim
    {
        public const float Dt = 1f / 60f;
        const float Radius = 0.38f;
        const float Half = 0.9f;

        public struct Result
        {
            public bool Ok;
            public float Time;
            public int Samples;
            public string Why;
            public string Times;
        }

        enum Mode
        {
            Ground = 0,
            Air = 1,
            Wall = 2,
            Zip = 3
        }

        public static Result Run(PracticeRoute route, MovementConfig cfg)
        {
            var result = new Result();
            if (route == null || route.Gates == null || route.Gates.Length < 2 || cfg == null)
            {
                result.Why = "route missing";
                return result;
            }
            LayoutOf(route.Arena, out MegaParkP1Layout.Solid[] solids, out MegaParkP1Layout.PadSpot[] pads, out MegaParkP1Layout.ZipLineSpot[] zips);
            var body = new Body(cfg, solids, pads, zips, route);
            body.Spawn();
            int guard = (int)(48f / Dt);
            for (int step = 0; step < guard && !body.Done; step++)
                body.Tick();
            result.Time = body.Time;
            result.Samples = PracticeGhost.Count;
            result.Ok = body.Done && !body.Failed && body.VerbsMet();
            if (!result.Ok)
            {
                result.Why = route.Id + " gate " + body.Next.ToString(CultureInfo.InvariantCulture)
                    + " at " + body.Pos.x.ToString("0.0", CultureInfo.InvariantCulture)
                    + "," + body.Pos.y.ToString("0.0", CultureInfo.InvariantCulture)
                    + "," + body.Pos.z.ToString("0.0", CultureInfo.InvariantCulture)
                    + " " + body.Note;
            }
            return result;
        }

        static void LayoutOf(string arena, out MegaParkP1Layout.Solid[] solids, out MegaParkP1Layout.PadSpot[] pads, out MegaParkP1Layout.ZipLineSpot[] zips)
        {
            if (arena == ParkArena.NameOf(ParkArena.Pocket))
            {
                solids = PocketParkLayout.BuildSolids();
                pads = PocketParkLayout.LaunchPads;
                zips = PocketParkLayout.ZipLines;
                return;
            }
            if (arena == ParkArena.NameOf(ParkArena.Stack))
            {
                solids = StackYardLayout.BuildSolids();
                pads = StackYardLayout.LaunchPads;
                zips = StackYardLayout.ZipLines;
                return;
            }
            solids = MegaParkP1Layout.BuildSolids();
            pads = MegaParkP1Layout.LaunchPads;
            zips = MegaParkP1Layout.ZipLines;
        }

        sealed class Body
        {
            public Vector3 Pos;
            public Vector3 Vel;
            public float Time;
            public int Next;
            public string Note = "";
            public bool Done;
            public bool Failed;

            readonly MovementConfig _cfg;
            readonly MegaParkP1Layout.Solid[] _solids;
            readonly MegaParkP1Layout.PadSpot[] _pads;
            readonly MegaParkP1Layout.ZipLineSpot[] _zips;
            readonly PracticeRoute _route;
            readonly bool[] _need = new bool[PracticeVerb.Bits];
            readonly bool[] _used = new bool[PracticeVerb.Bits];
            readonly float[] _splits;
            Mode _mode;
            float _wallT;
            float _dashT;
            bool _dashed;
            bool _padRise;
            int _zip = -1;
            float _stuck;
            Vector3 _stuckAt;
            int _wall = -1;
            Vector3 _wallNormal;
            Vector3 _wallAlong;

            public Body(MovementConfig cfg, MegaParkP1Layout.Solid[] solids, MegaParkP1Layout.PadSpot[] pads, MegaParkP1Layout.ZipLineSpot[] zips, PracticeRoute route)
            {
                _cfg = cfg;
                _solids = solids;
                _pads = pads ?? new MegaParkP1Layout.PadSpot[0];
                _zips = zips ?? new MegaParkP1Layout.ZipLineSpot[0];
                _route = route;
                _splits = new float[route.Gates.Length];
                string verbs = route.Verbs ?? "";
                int i = 0;
                while (i < verbs.Length)
                {
                    int comma = verbs.IndexOf(',', i);
                    if (comma < 0) comma = verbs.Length;
                    string part = verbs.Substring(i, comma - i).Trim();
                    byte verb = PracticeVerb.Parse(part);
                    if (verb != PracticeVerb.None) _need[verb] = true;
                    i = comma + 1;
                }
            }

            public void Spawn()
            {
                PracticeGate start = _route.Gates[0];
                float floor = FloorAt(start.X, start.Z);
                Pos = new Vector3(start.X, floor + Half, start.Z);
                Vel = Vector3.zero;
                Time = 0f;
                Next = 1;
                _mode = Mode.Ground;
                _stuckAt = Pos;
                PracticeGhost.Clear();
                Offer();
                NoteGates();
            }

            public bool VerbsMet()
            {
                for (int i = 0; i < _need.Length; i++)
                {
                    if (_need[i] && !_used[i]) return false;
                }
                return true;
            }

            public void Tick()
            {
                PracticeGate gate = _route.Gates[Next];
                Vector3 wish = Wish(gate);
                if (_mode == Mode.Zip)
                    TickZip(wish);
                else if (_mode == Mode.Wall)
                    TickWall(wish, gate);
                else if (_mode == Mode.Air)
                    TickAir(wish, gate);
                else
                    TickGround(wish, gate);

                if (_mode == Mode.Ground)
                {
                    float floor = FloorAt(Pos.x, Pos.z);
                    Pos.y = floor + Half;
                    Vel.y = 0f;
                    if (Horiz() > 8f) _used[PracticeVerb.Sprint] = true;
                }

                Time += Dt;
                NoteGates();
                Offer();
                float dx = Pos.x - _stuckAt.x;
                float dz = Pos.z - _stuckAt.z;
                if (dx * dx + dz * dz < 0.02f * 0.02f) _stuck += Dt;
                else
                {
                    _stuck = 0f;
                    _stuckAt = Pos;
                }
                if (_stuck > 2.5f)
                {
                    Note = "stuck " + _mode.ToString();
                    Failed = true;
                    Done = true;
                }
            }

            void TickGround(Vector3 wish, PracticeGate gate)
            {
                bool jumpNow = false;
                if (gate.Verb == PracticeVerb.WallRun && !_used[PracticeVerb.WallRun])
                    wish = ClingApproach(out jumpNow);
                else if (gate.Verb == PracticeVerb.Jump && !_used[PracticeVerb.Jump] && FlatDist(gate) < 3.2f && Horiz() > 6f)
                    jumpNow = true;

                if (gate.Verb == PracticeVerb.Pad && !_used[PracticeVerb.Pad] && NearPad(out int pad))
                {
                    Launch(pad);
                    return;
                }
                if (gate.Verb == PracticeVerb.Zip && !_used[PracticeVerb.Zip] && FlatDist(gate) < 1.15f)
                {
                    Jump();
                    return;
                }

                Vector3 hv = WishAccel.Horizontal(Vel);
                hv = KinematicStep.GroundSteer(hv, wish, _cfg.sprintSpeed, _cfg.groundAccel, _cfg.groundDecel, Dt, jumpNow);
                hv = WishAccel.ClampPlanarSpeed(hv, _cfg.sprintSpeed);
                Vel = hv;
                if (jumpNow)
                {
                    Jump();
                    return;
                }
                MoveHorizontal();
            }

            void TickAir(Vector3 wish, PracticeGate gate)
            {
                if (gate.Verb == PracticeVerb.Zip && !_used[PracticeVerb.Zip] && TryGrab())
                    return;
                if (_padRise && Vel.y <= 0f) _padRise = false;
                if (!_padRise && !_dashed && _need[PracticeVerb.AirDash] && _dashT <= 0f)
                {
                    _dashT = _cfg.airDashDuration;
                    _dashed = true;
                    _used[PracticeVerb.AirDash] = true;
                }

                float g = KinematicStep.AirGravity(Vel.y, _cfg.gravity, _cfg.fallGravityMult);
                Vel.y = KinematicStep.IntegrateVertical(Vel.y, g, Dt, _cfg.maxFallSpeed);
                Vector3 hv = WishAccel.Horizontal(Vel);
                if (_dashT > 0f)
                {
                    _dashT -= Dt;
                    Vector3 dir = wish.sqrMagnitude > 0.01f ? wish.normalized : hv.normalized;
                    Vel = WishAccel.SetHoriz(Vel, dir * _cfg.airDashSpeed);
                }
                else if (wish.sqrMagnitude > 0.01f)
                {
                    float cap = _cfg.sprintSpeed;
                    hv = KinematicStep.AirSteer(hv, wish, cap, _cfg.airAccel, Dt);
                    Vel = WishAccel.SetHoriz(Vel, hv);
                }

                Pos += Vel * Dt;
                Resolve();
                if (OverlapsWall(out int wall, out Vector3 normal) && Horiz() >= _cfg.wallRunMinSpeed
                    && _need[PracticeVerb.WallRun] && !_used[PracticeVerb.WallJump])
                {
                    BeginWall(wall, normal);
                    return;
                }
                float floor = FloorAt(Pos.x, Pos.z);
                if (Vel.y <= 0f && Pos.y <= floor + Half + 0.05f)
                {
                    Pos.y = floor + Half;
                    Vel.y = 0f;
                    _mode = Mode.Ground;
                    _padRise = false;
                }
            }

            void TickWall(Vector3 wish, PracticeGate gate)
            {
                _wallT += Dt;
                float tNorm = _wallT / Mathf.Max(0.05f, _cfg.wallRunMaxTime);
                if (tNorm < 0f) tNorm = 0f;
                if (tNorm > 1f) tNorm = 1f;
                float fade = Mathf.Lerp(1f, 0.55f, tNorm * tNorm);
                float speed = _cfg.wallRunSpeed * fade;
                Vector3 hv = _wallAlong * speed;
                float y = Vel.y;
                float grav = _cfg.wallRunGravity * Mathf.Lerp(1f, Mathf.Max(1f, _cfg.wallRunGravityEndMult), tNorm * tNorm);
                y -= grav * Dt;
                float minY = tNorm < 0.3f ? -1.5f : Mathf.Lerp(-2.5f, -16f, (tNorm - 0.3f) / 0.7f);
                if (y < minY) y = minY;
                Vel = hv + Vector3.up * y - _wallNormal * 2.8f;
                Pos += new Vector3(0f, Vel.y * Dt, 0f);
                Pos += _wallAlong * (speed * Dt);
                float face = FaceX(_wall);
                Pos.x = face + _wallNormal.x * (Radius + 0.02f);
                _used[PracticeVerb.WallRun] = true;
                _used[PracticeVerb.Cling] = true;

                bool leave = _wallT > _cfg.wallRunMaxTime || Pos.y > 4.3f || Pos.y < 0.4f;
                bool wantJump = gate.Verb == PracticeVerb.WallJump || (_need[PracticeVerb.WallJump] && _used[PracticeVerb.WallRun]);
                if (wantJump && _wallT > 0.04f && !_used[PracticeVerb.WallJump])
                {
                    WallJump();
                    return;
                }
                if (leave)
                {
                    _mode = Mode.Air;
                    Note = "left wall";
                }
            }

            void TickZip(Vector3 wish)
            {
                MegaParkP1Layout.ZipLineSpot spot = _zips[_zip];
                Vector3 a = new Vector3(spot.Ax, spot.Ay, spot.Az);
                Vector3 b = new Vector3(spot.Bx, spot.By, spot.Bz);
                Vector3 entry = ZipLineRules.EntryPoint(a, b);
                Vector3 exit = ZipLineRules.ExitPoint(a, b);
                Vector3 vel = ZipLineRules.HangVelocity(Pos, entry, exit, spot.Speed, Dt);
                Pos += vel * Dt;
                Vel = vel;
                _used[PracticeVerb.Zip] = true;
                if (ZipLineRules.ReachedEnd(Pos, entry, exit, spot.Speed, Dt))
                {
                    Vector3 drop = ZipLineRules.EndDrop(vel);
                    Vel = drop;
                    _mode = Mode.Air;
                    _zip = -1;
                }
            }

            Vector3 ClingApproach(out bool jumpNow)
            {
                jumpNow = false;
                if (!NearestCling(out int wall, out float face, out float z0, out float z1))
                    return new Vector3(0f, 0f, -1f);
                _wall = wall;
                if (Pos.z > z1 - 0.4f)
                    return new Vector3(-0.15f, 0f, -1f).normalized;
                if (Pos.x > face + 1.05f)
                    return new Vector3(-1f, 0f, -0.2f).normalized;
                if (Horiz() > 7f && Pos.z <= z1 && Pos.z >= z0)
                    jumpNow = true;
                return new Vector3(-1f, 0f, -0.35f).normalized;
            }

            void Jump()
            {
                Vel.y = _cfg.jumpSpeed;
                _mode = Mode.Air;
                _used[PracticeVerb.Jump] = true;
                _padRise = false;
            }

            void Launch(int pad)
            {
                MegaParkP1Layout.PadSpot spot = _pads[pad];
                float mag = spot.DirX * spot.DirX + spot.DirZ * spot.DirZ;
                mag = mag > 0.01f ? (float)Math.Sqrt(mag) : 1f;
                Vector3 horiz = new Vector3(spot.DirX / mag * spot.Speed, 0f, spot.DirZ / mag * spot.Speed);
                Vel = LaunchPadRules.VelocitySet(Vel, spot.Apex, _cfg.gravity, horiz, true);
                _mode = Mode.Air;
                _padRise = true;
                _used[PracticeVerb.Pad] = true;
            }

            void BeginWall(int wall, Vector3 normal)
            {
                _mode = Mode.Wall;
                _wall = wall;
                _wallT = 0f;
                _wallNormal = normal;
                Vector3 along = new Vector3(-normal.z, 0f, normal.x);
                if (Vel.x * along.x + Vel.z * along.z < 0f)
                    along = new Vector3(-along.x, 0f, -along.z);
                if (along.sqrMagnitude < 0.01f) along = new Vector3(0f, 0f, -1f);
                _wallAlong = along.normalized;
                _used[PracticeVerb.WallRun] = true;
            }

            void WallJump()
            {
                // North off the first cling face. East would meet the next lane.
                Vector3 look = new Vector3(0f, 0f, 1f);
                Vel = _wallNormal * _cfg.wallRunJumpOut + Vector3.up * _cfg.wallRunJumpUp + look * 3.5f;
                _mode = Mode.Air;
                _used[PracticeVerb.WallJump] = true;
                _padRise = false;
            }

            bool TryGrab()
            {
                MegaParkP1Layout.ZipLineSpot[] lines = _zips;
                for (int i = 0; i < lines.Length; i++)
                {
                    MegaParkP1Layout.ZipLineSpot spot = lines[i];
                    Vector3 a = new Vector3(spot.Ax, spot.Ay, spot.Az);
                    Vector3 b = new Vector3(spot.Bx, spot.By, spot.Bz);
                    if (!ZipLineRules.InGrabVolume(Pos, a, b)) continue;
                    _zip = i;
                    _mode = Mode.Zip;
                    _used[PracticeVerb.Zip] = true;
                    return true;
                }
                return false;
            }

            bool NearPad(out int pad)
            {
                pad = -1;
                float best = 1.45f;
                MegaParkP1Layout.PadSpot[] pads = _pads;
                for (int i = 0; i < pads.Length; i++)
                {
                    float dx = Pos.x - pads[i].X;
                    float dz = Pos.z - pads[i].Z;
                    float d = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (d < best)
                    {
                        best = d;
                        pad = i;
                    }
                }
                return pad >= 0;
            }

            Vector3 Wish(PracticeGate gate)
            {
                float dx = gate.X - Pos.x;
                float dz = gate.Z - Pos.z;
                float m = (float)Math.Sqrt(dx * dx + dz * dz);
                if (m < 0.08f) return new Vector3(0f, 0f, 1f);
                return new Vector3(dx / m, 0f, dz / m);
            }

            void MoveHorizontal()
            {
                float nx = Pos.x + Vel.x * Dt;
                float nz = Pos.z + Vel.z * Dt;
                if (Blocked(nx, Pos.y, nz, -1))
                {
                    if (!Blocked(Pos.x, Pos.y, nz, -1)) nx = Pos.x;
                    else if (!Blocked(nx, Pos.y, Pos.z, -1)) nz = Pos.z;
                    else
                    {
                        nx = Pos.x;
                        nz = Pos.z;
                    }
                }
                Pos.x = nx;
                Pos.z = nz;
            }

            void Resolve()
            {
                if (!Blocked(Pos.x, Pos.y, Pos.z, _wall))
                    return;
                float back = Dt > 0f ? Dt : 0.01f;
                Pos.x -= Vel.x * back;
                Pos.z -= Vel.z * back;
            }

            void NoteGates()
            {
                if (Next < 0 || Next >= _route.Gates.Length) return;
                PracticeGate gate = _route.Gates[Next];
                bool ready = gate.Verb == PracticeVerb.None || _used[gate.Verb];
                int after = PracticeGates.Step(_route.Gates, Next, Pos.x, Pos.y, Pos.z, ready);
                if (after == Next) return;
                _splits[Next] = Time;
                Next = after;
                if (Next < _route.Gates.Length) return;
                Done = true;
                if (!PracticeScore.Commit(true, false, false, Time, PracticeBests.TimeOf(_route.Id))) return;
                PracticeBests.Set(_route.Id, Time, _splits, _route.Gates.Length);
                PracticeGhost.Keep(_route.Id);
            }

            void Offer()
            {
                float yaw = 0f;
                if (Horiz() > 0.2f)
                    yaw = (float)Math.Atan2(Vel.x, Vel.z);
                PracticeGhost.Offer(Dt, Pos.x, Pos.y, Pos.z, yaw, PoseNow());
            }

            byte PoseNow()
            {
                if (_mode == Mode.Zip) return PracticeVerb.Zip;
                if (_mode == Mode.Wall) return PracticeVerb.WallRun;
                if (_dashT > 0f) return PracticeVerb.AirDash;
                if (_used[PracticeVerb.WallJump] && _mode == Mode.Air && Time < 0.4f) return PracticeVerb.WallJump;
                if (_padRise) return PracticeVerb.Pad;
                if (_mode == Mode.Air) return PracticeVerb.Jump;
                if (_mode == Mode.Ground && Horiz() > 8f) return PracticeVerb.Sprint;
                return PracticeVerb.None;
            }

            float Horiz()
            {
                return (float)Math.Sqrt(Vel.x * Vel.x + Vel.z * Vel.z);
            }

            float FlatDist(PracticeGate gate)
            {
                float dx = Pos.x - gate.X;
                float dz = Pos.z - gate.Z;
                return (float)Math.Sqrt(dx * dx + dz * dz);
            }

            float FloorAt(float x, float z)
            {
                float top = 0f;
                for (int i = 0; i < _solids.Length; i++)
                {
                    MegaParkP1Layout.Solid s = _solids[i];
                    if (!FloorKind(s)) continue;
                    if (Math.Abs(x - s.X) > s.Sx * 0.5f) continue;
                    if (Math.Abs(z - s.Z) > s.Sz * 0.5f) continue;
                    float t = s.Y + s.Sy * 0.5f;
                    if (s.SupportY > t - 0.05f && s.SupportY < t + 2f && s.Kind == "ground")
                        t = s.SupportY;
                    if (t > top) top = t;
                }
                return top;
            }

            static bool FloorKind(MegaParkP1Layout.Solid s)
            {
                if (s.Kind == "ground" || s.Kind == "bump") return true;
                if (s.Kind == "vault" && s.Sy <= 1.2f) return true;
                return false;
            }

            bool Blocked(float x, float y, float z, int ignore)
            {
                float feet = y - Half;
                float head = y + Half;
                for (int i = 0; i < _solids.Length; i++)
                {
                    if (i == ignore) continue;
                    MegaParkP1Layout.Solid s = _solids[i];
                    if (FloorKind(s) || s.Kind == "toy" || s.Kind == "bar") continue;
                    if (s.Kind == "wall" && s.Mat == "blue" && _need[PracticeVerb.WallRun] && !_used[PracticeVerb.WallJump])
                        continue;
                    if (Math.Abs(x - s.X) > s.Sx * 0.5f + Radius) continue;
                    if (Math.Abs(z - s.Z) > s.Sz * 0.5f + Radius) continue;
                    float bottom = s.Y - s.Sy * 0.5f;
                    float top = s.Y + s.Sy * 0.5f;
                    if (head < bottom || feet > top) continue;
                    Note = s.Name;
                    return true;
                }
                return false;
            }

            bool OverlapsWall(out int wall, out Vector3 normal)
            {
                wall = -1;
                normal = Vector3.right;
                float best = 0.35f;
                for (int i = 0; i < _solids.Length; i++)
                {
                    MegaParkP1Layout.Solid s = _solids[i];
                    if (s.Kind != "wall" || s.Mat != "blue") continue;
                    float face = s.X + s.Sx * 0.5f;
                    float gap = Pos.x - (face + Radius);
                    if (gap < -0.2f || gap > 0.45f) continue;
                    float z0 = s.Z - s.Sz * 0.5f;
                    float z1 = s.Z + s.Sz * 0.5f;
                    if (Pos.z < z0 - 0.2f || Pos.z > z1 + 0.2f) continue;
                    if (Pos.y > s.Y + s.Sy * 0.5f || Pos.y < s.Y - s.Sy * 0.5f) continue;
                    float d = gap < 0f ? -gap : gap;
                    if (d < best)
                    {
                        best = d;
                        wall = i;
                        normal = Vector3.right;
                    }
                }
                return wall >= 0;
            }

            bool NearestCling(out int wall, out float face, out float z0, out float z1)
            {
                wall = -1;
                face = 0f;
                z0 = 0f;
                z1 = 0f;
                float best = 30f;
                for (int i = 0; i < _solids.Length; i++)
                {
                    MegaParkP1Layout.Solid s = _solids[i];
                    if (s.Kind != "wall" || s.Mat != "blue") continue;
                    float f = s.X + s.Sx * 0.5f;
                    float dx = Pos.x - f;
                    float dz = Pos.z - s.Z;
                    float d = (float)Math.Sqrt(dx * dx + dz * dz);
                    if (d < best)
                    {
                        best = d;
                        wall = i;
                        face = f;
                        z0 = s.Z - s.Sz * 0.5f;
                        z1 = s.Z + s.Sz * 0.5f;
                    }
                }
                return wall >= 0;
            }

            float FaceX(int wall)
            {
                if (wall < 0 || wall >= _solids.Length) return Pos.x;
                MegaParkP1Layout.Solid s = _solids[wall];
                return s.X + s.Sx * 0.5f * _wallNormal.x;
            }
        }
    }
}
