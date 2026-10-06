using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Tag.Core;

namespace Tag.Level
{
    /// <summary>
    /// Which park Play builds. Mega Park is the default. Pocket Park and Stack Yard
    /// are the other picks. The pause Arena menu and the countdown keys share this id.
    /// No Unity types, so the headless sim can read the same id.
    /// </summary>
    public static class ParkArena
    {
        public const int Mega = 0;
        public const int Pocket = 1;
        public const int Stack = 2;
        public const int Count = 3;
        public const string PrefsKey = "Tag.ParkArena";

        public static int Id;
        public static bool HasExplicitChoice;

        public static bool IsPocket => Id == Pocket;
        public static bool IsStack => Id == Stack;
        public static string DisplayName => NameOf(Id);

        public static string NameOf(int id)
        {
            if (id == Pocket) return "Pocket Park";
            if (id == Stack) return "Stack Yard";
            return "Mega Park";
        }

        public static void Select(int id)
        {
            if (id == Pocket) Id = Pocket;
            else if (id == Stack) Id = Stack;
            else Id = Mega;
        }

        /// <summary>
        /// Boot reads the saved pick when the player has chosen. Otherwise the scene default stands.
        /// </summary>
        public static void ApplySaved(bool hasKey, int saved, int sceneDefault)
        {
            if (hasKey)
            {
                Select(saved);
                HasExplicitChoice = true;
                return;
            }
            HasExplicitChoice = false;
            Select(sceneDefault);
        }

        public static float MapW => IsStack ? StackYardLayout.MapW : IsPocket ? PocketParkLayout.MapW : MegaParkP1Layout.MapW;
        public static float MapD => IsStack ? StackYardLayout.MapD : IsPocket ? PocketParkLayout.MapD : MegaParkP1Layout.MapD;

        /// <summary>
        /// Fence footprint and kill height for an arena id. Unknown ids use Mega Park.
        /// The kill height is the shared plane. Feel locks are not stored here.
        /// </summary>
        public static void Containment(int id, out float mapW, out float mapD, out float killY)
        {
            if (id == Pocket)
            {
                mapW = PocketParkLayout.MapW;
                mapD = PocketParkLayout.MapD;
            }
            else if (id == Stack)
            {
                mapW = StackYardLayout.MapW;
                mapD = StackYardLayout.MapD;
            }
            else
            {
                mapW = MegaParkP1Layout.MapW;
                mapD = MegaParkP1Layout.MapD;
            }
            killY = MegaParkP1Layout.KillPlaneY;
        }

        public static void PickRespawn(float fromX, float fromZ, float itX, float itZ, bool hasIt,
            out float x, out float y, out float z)
        {
            if (IsStack)
                StackYardLayout.PickRespawn(fromX, fromZ, itX, itZ, hasIt, out x, out y, out z);
            else if (IsPocket)
                PocketParkLayout.PickRespawn(fromX, fromZ, itX, itZ, hasIt, out x, out y, out z);
            else
                MegaParkP1Layout.PickRespawn(fromX, fromZ, itX, itZ, hasIt, out x, out y, out z);
        }

        public const int HumanSeats = 4;

        /// <summary>
        /// Four match-start seats, equally spaced on that arena's perimeter loop.
        /// The authored respawn pads stay put so the AI duels keep their pairs.
        /// </summary>
        public static void HumanSeat(int arena, int index, out float x, out float y, out float z, out float yaw)
        {
            int id = arena == Pocket ? Pocket : arena == Stack ? Stack : Mega;
            MegaParkP1Layout.Pt[] loop = id == Stack ? StackYardLayout.LoopCcw
                : id == Pocket ? PocketParkLayout.LoopCcw
                : MegaParkP1Layout.LoopCcw;
            int n = HumanSeats;
            if (index < 0) index = 0;
            if (index >= n) index = n - 1;
            float length = LoopLength(loop);
            float arc = length * index / n;
            PointOn(loop, arc, out x, out z);
            PointOn(loop, arc + 2f, out float ax, out float az);
            float dx = ax - x;
            float dz = az - z;
            yaw = (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
            y = MegaParkP1Layout.SpawnY;
        }

        public static bool HumanSeatsHold(out string why)
        {
            why = "";
            for (int arena = 0; arena < Count; arena++)
            {
                MegaParkP1Layout.Pt[] loop = arena == Stack ? StackYardLayout.LoopCcw
                    : arena == Pocket ? PocketParkLayout.LoopCcw
                    : MegaParkP1Layout.LoopCcw;
                float length = LoopLength(loop);
                float want = length / HumanSeats;
                MegaParkP1Layout.Solid[] solids = SolidsOf(arena);
                var xs = new float[HumanSeats];
                var zs = new float[HumanSeats];
                for (int i = 0; i < HumanSeats; i++)
                {
                    HumanSeat(arena, i, out xs[i], out float y, out zs[i], out _);
                    SessionRules.ArenaBox box = SessionRules.Bounds(arena);
                    if (SessionRules.Outside(box, xs[i], y, zs[i]))
                    {
                        why = "human seat " + i + " on arena " + arena + " is outside the kill box";
                        return false;
                    }
                    if (Blocked(solids, xs[i], zs[i]))
                    {
                        why = "human seat " + i + " on arena " + arena + " is inside a solid";
                        return false;
                    }
                    int next = (i + 1) % HumanSeats;
                    HumanSeat(arena, next, out float nx, out _, out float nz, out _);
                    float arc = Arc(loop, xs[i], zs[i], nx, nz);
                    if (Math.Abs(arc - want) > 0.05f)
                    {
                        why = "human seat arcs on arena " + arena + " are not equal";
                        return false;
                    }
                }
                for (int a = 0; a < HumanSeats; a++)
                {
                    for (int b = a + 1; b < HumanSeats; b++)
                    {
                        float dx = xs[a] - xs[b];
                        float dz = zs[a] - zs[b];
                        float d = (float)Math.Sqrt(dx * dx + dz * dz);
                        if (d < MegaParkP1Layout.SpawnClearMeters
                            || d / MegaParkP1Layout.SprintSpeed < MegaParkP1Layout.SpawnSightSeconds - 0.0001f)
                        {
                            why = "human seats " + a + " and " + b + " on arena " + arena + " are too close";
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        static MegaParkP1Layout.Solid[] SolidsOf(int arena)
        {
            if (arena == Pocket) return PocketParkLayout.BuildSolids();
            if (arena == Stack) return StackYardLayout.BuildSolids();
            return MegaParkP1Layout.BuildSolids();
        }

        static bool Blocked(MegaParkP1Layout.Solid[] solids, float x, float z)
        {
            if (solids == null) return false;
            for (int i = 0; i < solids.Length; i++)
            {
                MegaParkP1Layout.Solid s = solids[i];
                if (s.Kind == "ground" || s.Kind == "fence" || s.Kind == "mark") continue;
                if (s.Sy < 1.2f) continue;
                float top = s.Y + s.Sy * 0.5f;
                float bot = s.Y - s.Sy * 0.5f;
                if (top < 0.4f || bot > 1.8f) continue;
                float hx = s.Sx * 0.5f + 0.35f;
                float hz = s.Sz * 0.5f + 0.35f;
                if (Math.Abs(x - s.X) < hx && Math.Abs(z - s.Z) < hz)
                    return true;
            }
            return false;
        }

        static float LoopLength(MegaParkP1Layout.Pt[] loop)
        {
            float len = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                MegaParkP1Layout.Pt a = loop[i];
                MegaParkP1Layout.Pt b = loop[(i + 1) % loop.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                len += (float)Math.Sqrt(dx * dx + dz * dz);
            }
            return len;
        }

        static void PointOn(MegaParkP1Layout.Pt[] loop, float arc, out float x, out float z)
        {
            float length = LoopLength(loop);
            if (length < 0.01f)
            {
                x = loop[0].X;
                z = loop[0].Z;
                return;
            }
            arc %= length;
            if (arc < 0f) arc += length;
            float walked = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                MegaParkP1Layout.Pt a = loop[i];
                MegaParkP1Layout.Pt b = loop[(i + 1) % loop.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float seg = (float)Math.Sqrt(dx * dx + dz * dz);
                if (walked + seg >= arc || i == loop.Length - 1)
                {
                    float u = seg > 0.01f ? (arc - walked) / seg : 0f;
                    if (u < 0f) u = 0f;
                    if (u > 1f) u = 1f;
                    x = a.X + dx * u;
                    z = a.Z + dz * u;
                    return;
                }
                walked += seg;
            }
            x = loop[0].X;
            z = loop[0].Z;
        }

        static float Arc(MegaParkP1Layout.Pt[] loop, float x0, float z0, float x1, float z1)
        {
            float t0 = Project(loop, x0, z0);
            float t1 = Project(loop, x1, z1);
            float length = LoopLength(loop);
            float d = t1 - t0;
            if (d < 0f) d += length;
            return d;
        }

        static float Project(MegaParkP1Layout.Pt[] loop, float x, float z)
        {
            float best = 0f;
            float bestD = 1e20f;
            float walked = 0f;
            for (int i = 0; i < loop.Length; i++)
            {
                MegaParkP1Layout.Pt a = loop[i];
                MegaParkP1Layout.Pt b = loop[(i + 1) % loop.Length];
                float dx = b.X - a.X;
                float dz = b.Z - a.Z;
                float seg = (float)Math.Sqrt(dx * dx + dz * dz);
                float u = 0f;
                if (seg > 0.01f)
                {
                    u = ((x - a.X) * dx + (z - a.Z) * dz) / (seg * seg);
                    if (u < 0f) u = 0f;
                    if (u > 1f) u = 1f;
                }
                float px = a.X + dx * u;
                float pz = a.Z + dz * u;
                float ex = x - px;
                float ez = z - pz;
                float d = ex * ex + ez * ez;
                if (d < bestD)
                {
                    bestD = d;
                    best = walked + seg * u;
                }
                walked += seg;
            }
            return best;
        }

        public static bool SpawnIsSafe(float x, float z, float itX, float itZ)
        {
            if (IsPocket || IsStack)
            {
                float dx = x - itX;
                float dz = z - itZ;
                float d = (float)Math.Sqrt(dx * dx + dz * dz);
                return d >= MegaParkP1Layout.SpawnClearMeters
                    && d / MegaParkP1Layout.SprintSpeed >= MegaParkP1Layout.SpawnSightSeconds - 0.0001f;
            }
            return MegaParkP1Layout.SpawnIsSafe(x, z, itX, itZ);
        }

        /// <summary>
        /// Headless arena switch. Pieces are the colliders, pads, zips, dummies, pawns, and minimap
        /// the bootstrap builds. TearDown drops owned pieces back to the baseline count.
        /// </summary>
        public static class Census
        {
            public sealed class Piece
            {
                public string Kind;
                public int Arena;
                public bool Owned;
                public float X, Z;
                public string Name;
            }

            public static string Prove(out bool ok)
            {
                int savedId = Id;
                bool savedChoice = HasExplicitChoice;
                var fail = new StringBuilder();
                try
                {
                    var world = new List<Piece>();
                    world.Add(new Piece { Kind = "persist", Owned = false, Name = "GameFlow", Arena = -1 });
                    int baseline = world.Count;

                    Build(world, Mega, 2);
                    ExpectKinds(world, Mega, fail, "mega");
                    int occupied = world.Count;

                    TearDown(world);
                    if (world.Count != baseline)
                        fail.Append("mega teardown ").Append(world.Count.ToString(CultureInfo.InvariantCulture)).Append("; ");
                    if (OwnedLeft(world))
                        fail.Append("mega leftovers; ");

                    Build(world, Pocket, 2);
                    ExpectKinds(world, Pocket, fail, "pocket");
                    ExpectPawns(world, Pocket, fail);
                    if (Foreign(world, Pocket))
                        fail.Append("mega pieces survived the switch; ");

                    TearDown(world);
                    if (world.Count != baseline)
                        fail.Append("pocket teardown ").Append(world.Count.ToString(CultureInfo.InvariantCulture)).Append("; ");
                    if (OwnedLeft(world))
                        fail.Append("pocket leftovers; ");

                    Build(world, Stack, 2);
                    ExpectKinds(world, Stack, fail, "stack");
                    ExpectPawns(world, Stack, fail);
                    if (Foreign(world, Stack))
                        fail.Append("other pieces survived the stack switch; ");

                    TearDown(world);
                    if (world.Count != baseline)
                        fail.Append("stack teardown ").Append(world.Count.ToString(CultureInfo.InvariantCulture)).Append("; ");
                    if (OwnedLeft(world))
                        fail.Append("stack leftovers; ");

                    ApplySaved(true, Pocket, Mega);
                    if (!IsPocket || !HasExplicitChoice)
                        fail.Append("saved pocket choice did not persist; ");
                    ApplySaved(true, Stack, Mega);
                    if (!IsStack || !HasExplicitChoice)
                        fail.Append("saved stack choice did not persist; ");
                    ApplySaved(false, Stack, Mega);
                    if (IsStack || IsPocket || HasExplicitChoice)
                        fail.Append("scene default ignored a missing save; ");
                    ApplySaved(true, Mega, Pocket);
                    if (IsPocket || IsStack || !HasExplicitChoice)
                        fail.Append("saved mega choice did not persist; ");

                    ok = fail.Length == 0;
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} arena-teardown baseline={1} occupied={2} after={3} parks=3 choice=held leftovers=0",
                        ok ? "PASS" : "FAIL", baseline, occupied, baseline);
                }
                finally
                {
                    Id = savedId;
                    HasExplicitChoice = savedChoice;
                }
            }

            static void ExpectKinds(List<Piece> world, int arena, StringBuilder fail, string label)
            {
                if (CountKind(world, "collider", arena) < 1) fail.Append(label).Append(" colliders; ");
                if (CountKind(world, "pad", arena) < 1) fail.Append(label).Append(" pads; ");
                if (CountKind(world, "zip", arena) < 1) fail.Append(label).Append(" zips; ");
                if (CountKind(world, "dummy", arena) != 1) fail.Append(label).Append(" dummies; ");
                if (CountKind(world, "pawn", arena) < 1) fail.Append(label).Append(" pawns; ");
                if (CountKind(world, "minimap", arena) != 1) fail.Append(label).Append(" minimap; ");
            }

            static void ExpectPawns(List<Piece> world, int arena, StringBuilder fail)
            {
                MegaParkP1Layout.SpawnPad[] spawns = SpawnsOf(arena);
                bool pawn = false;
                bool dummy = false;
                for (int i = 0; i < world.Count; i++)
                {
                    Piece p = world[i];
                    if (p.Arena != arena) continue;
                    if (p.Kind == "pawn")
                    {
                        pawn = true;
                        if (Math.Abs(p.X - spawns[0].X) > 0.01f || Math.Abs(p.Z - spawns[0].Z) > 0.01f)
                            fail.Append(p.Name).Append(" left its spawn; ");
                    }
                    else if (p.Kind == "dummy")
                    {
                        dummy = true;
                        int slot = spawns.Length > 1 ? 1 : 0;
                        if (Math.Abs(p.X - spawns[slot].X) > 0.01f || Math.Abs(p.Z - spawns[slot].Z) > 0.01f)
                            fail.Append(p.Name).Append(" left its spawn; ");
                    }
                }
                if (!pawn || !dummy)
                    fail.Append("dummy was not reseated; ");
            }

            static void Build(List<Piece> world, int arena, int bodies)
            {
                MegaParkP1Layout.Solid[] solids = SolidsOf(arena);
                MegaParkP1Layout.Ramp[] ramps = RampsOf(arena);
                MegaParkP1Layout.PadSpot[] pads = PadsOf(arena);
                MegaParkP1Layout.ZipLineSpot[] zips = ZipsOf(arena);
                MegaParkP1Layout.SpawnPad[] spawns = SpawnsOf(arena);
                for (int i = 0; i < solids.Length; i++)
                    world.Add(Own(arena, "collider", solids[i].Name, solids[i].X, solids[i].Z));
                for (int i = 0; i < ramps.Length; i++)
                    world.Add(Own(arena, "collider", ramps[i].Name, ramps[i].X0, ramps[i].Z0));
                for (int i = 0; i < pads.Length; i++)
                    world.Add(Own(arena, "pad", pads[i].Name, pads[i].X, pads[i].Z));
                for (int i = 0; i < zips.Length; i++)
                    world.Add(Own(arena, "zip", zips[i].Name, zips[i].Ax, zips[i].Az));
                int n = bodies < 1 ? 1 : bodies;
                if (n > spawns.Length) n = spawns.Length;
                for (int i = 0; i < n; i++)
                {
                    bool dummy = i == 1 || (n == 1 && i == 0);
                    if (n >= 2 && i == 0) dummy = false;
                    string kind = dummy ? "dummy" : "pawn";
                    world.Add(Own(arena, kind, kind + i.ToString(CultureInfo.InvariantCulture), spawns[i].X, spawns[i].Z));
                }
                world.Add(Own(arena, "minimap", NameOf(arena), 0f, 0f));
            }

            static MegaParkP1Layout.Solid[] SolidsOf(int arena)
            {
                if (arena == Pocket) return PocketParkLayout.BuildSolids();
                if (arena == Stack) return StackYardLayout.BuildSolids();
                return MegaParkP1Layout.BuildSolids();
            }

            static MegaParkP1Layout.Ramp[] RampsOf(int arena)
            {
                if (arena == Pocket) return PocketParkLayout.BuildRamps();
                if (arena == Stack) return StackYardLayout.BuildRamps();
                return MegaParkP1Layout.BuildRamps();
            }

            static MegaParkP1Layout.PadSpot[] PadsOf(int arena)
            {
                if (arena == Pocket) return PocketParkLayout.LaunchPads;
                if (arena == Stack) return StackYardLayout.LaunchPads;
                return MegaParkP1Layout.LaunchPads;
            }

            static MegaParkP1Layout.ZipLineSpot[] ZipsOf(int arena)
            {
                if (arena == Pocket) return PocketParkLayout.ZipLines;
                if (arena == Stack) return StackYardLayout.ZipLines;
                return MegaParkP1Layout.ZipLines;
            }

            static MegaParkP1Layout.SpawnPad[] SpawnsOf(int arena)
            {
                if (arena == Pocket) return PocketParkLayout.Spawns;
                if (arena == Stack) return StackYardLayout.Spawns;
                return MegaParkP1Layout.Spawns;
            }

            static bool Foreign(List<Piece> world, int arena)
            {
                for (int other = 0; other < Count; other++)
                {
                    if (other == arena) continue;
                    if (CountKind(world, "minimap", other) != 0 || CountKind(world, "collider", other) != 0
                        || CountKind(world, "pad", other) != 0 || CountKind(world, "zip", other) != 0
                        || CountKind(world, "dummy", other) != 0 || CountKind(world, "pawn", other) != 0)
                        return true;
                }
                return false;
            }

            static Piece Own(int arena, string kind, string name, float x, float z)
            {
                return new Piece { Kind = kind, Arena = arena, Owned = true, Name = name, X = x, Z = z };
            }

            static void TearDown(List<Piece> world)
            {
                for (int i = world.Count - 1; i >= 0; i--)
                {
                    if (world[i].Owned)
                        world.RemoveAt(i);
                }
            }

            static bool OwnedLeft(List<Piece> world)
            {
                for (int i = 0; i < world.Count; i++)
                    if (world[i].Owned) return true;
                return false;
            }

            static int CountKind(List<Piece> world, string kind, int arena)
            {
                int n = 0;
                for (int i = 0; i < world.Count; i++)
                    if (world[i].Kind == kind && world[i].Arena == arena) n++;
                return n;
            }

            /// <summary>Persistent pieces only. Round flow builds and tears an arena around this.</summary>
            public static List<Piece> Open(out int baseline)
            {
                var world = new List<Piece>();
                world.Add(new Piece { Kind = "persist", Owned = false, Name = "GameFlow", Arena = -1 });
                baseline = world.Count;
                return world;
            }

            public static void BuildArena(List<Piece> world, int arena)
            {
                Build(world, arena, 2);
            }

            public static void ClearOwned(List<Piece> world)
            {
                TearDown(world);
            }

            public static bool Clean(List<Piece> world, int baseline)
            {
                return world != null && world.Count == baseline && !OwnedLeft(world);
            }

            /// <summary>Pawn and dummy sit on this arena's pads. The other arenas have nothing left.</summary>
            public static bool SpawnedOn(List<Piece> world, int arena)
            {
                if (world == null) return false;
                if (CountKind(world, "minimap", arena) != 1) return false;
                if (Foreign(world, arena)) return false;
                var fail = new StringBuilder();
                ExpectKinds(world, arena, fail, "arena");
                ExpectPawns(world, arena, fail);
                return fail.Length == 0;
            }
        }
    }
}
