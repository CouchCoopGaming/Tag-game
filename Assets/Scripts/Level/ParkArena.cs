using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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
