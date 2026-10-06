using System;

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
    }
}
