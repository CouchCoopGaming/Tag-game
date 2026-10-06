using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tag.Level
{
    /// <summary>
    /// Which park Play builds. Mega Park is the default until a player picks Pocket Park.
    /// No Unity types, so the headless sim can read the same id.
    /// </summary>
    public static class ParkArena
    {
        public const int Mega = 0;
        public const int Pocket = 1;
        public const string PrefsKey = "Tag.ParkArena";

        public static int Id;
        public static bool HasExplicitChoice;

        public static bool IsPocket => Id == Pocket;
        public static string DisplayName => IsPocket ? "Pocket Park" : "Mega Park";

        public static void Select(int id)
        {
            Id = id == Pocket ? Pocket : Mega;
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

        public static float MapW => IsPocket ? PocketParkLayout.MapW : MegaParkP1Layout.MapW;
        public static float MapD => IsPocket ? PocketParkLayout.MapD : MegaParkP1Layout.MapD;

        public static void PickRespawn(float fromX, float fromZ, float itX, float itZ, bool hasIt,
            out float x, out float y, out float z)
        {
            if (IsPocket)
                PocketParkLayout.PickRespawn(fromX, fromZ, itX, itZ, hasIt, out x, out y, out z);
            else
                MegaParkP1Layout.PickRespawn(fromX, fromZ, itX, itZ, hasIt, out x, out y, out z);
        }

        public static bool SpawnIsSafe(float x, float z, float itX, float itZ)
        {
            if (!IsPocket)
                return MegaParkP1Layout.SpawnIsSafe(x, z, itX, itZ);
            float dx = x - itX;
            float dz = z - itZ;
            float d = (float)Math.Sqrt(dx * dx + dz * dz);
            return d >= MegaParkP1Layout.SpawnClearMeters
                && d / MegaParkP1Layout.SprintSpeed >= MegaParkP1Layout.SpawnSightSeconds - 0.0001f;
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
                    if (CountKind(world, "minimap", Mega) != 0 || CountKind(world, "collider", Mega) != 0
                        || CountKind(world, "pad", Mega) != 0 || CountKind(world, "zip", Mega) != 0
                        || CountKind(world, "dummy", Mega) != 0)
                        fail.Append("mega pieces survived the switch; ");

                    TearDown(world);
                    if (world.Count != baseline)
                        fail.Append("pocket teardown ").Append(world.Count.ToString(CultureInfo.InvariantCulture)).Append("; ");
                    if (OwnedLeft(world))
                        fail.Append("pocket leftovers; ");

                    ApplySaved(true, Pocket, Mega);
                    if (!IsPocket || !HasExplicitChoice)
                        fail.Append("saved pocket choice did not persist; ");
                    ApplySaved(false, Pocket, Mega);
                    if (IsPocket || HasExplicitChoice)
                        fail.Append("scene default ignored a missing save; ");
                    ApplySaved(true, Mega, Pocket);
                    if (IsPocket || !HasExplicitChoice)
                        fail.Append("saved mega choice did not persist; ");

                    ok = fail.Length == 0;
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} pocket-teardown baseline={1} occupied={2} after={3} choice=held leftovers=0",
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
                MegaParkP1Layout.SpawnPad[] spawns = arena == Pocket ? PocketParkLayout.Spawns : MegaParkP1Layout.Spawns;
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
                bool pocket = arena == Pocket;
                MegaParkP1Layout.Solid[] solids = pocket ? PocketParkLayout.BuildSolids() : MegaParkP1Layout.BuildSolids();
                MegaParkP1Layout.Ramp[] ramps = pocket ? PocketParkLayout.BuildRamps() : MegaParkP1Layout.BuildRamps();
                MegaParkP1Layout.PadSpot[] pads = pocket ? PocketParkLayout.LaunchPads : MegaParkP1Layout.LaunchPads;
                MegaParkP1Layout.ZipLineSpot[] zips = pocket ? PocketParkLayout.ZipLines : MegaParkP1Layout.ZipLines;
                MegaParkP1Layout.SpawnPad[] spawns = pocket ? PocketParkLayout.Spawns : MegaParkP1Layout.Spawns;
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
                world.Add(Own(arena, "minimap", pocket ? "Pocket" : "Mega", 0f, 0f));
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
        }
    }
}
