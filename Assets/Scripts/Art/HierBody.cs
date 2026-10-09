namespace Tag.Art
{
    /// <summary>
    /// Visual offsets for Hier v0.8.0. The hip sits 12 cm lower and the arm is
    /// about 8.5 cm shorter. These constants move feet, hands, and the mesh
    /// inside the capsule. They do not change the capsule or any feel number.
    /// </summary>
    public static class HierBody
    {
        public const float HipDrop = 0.12f;
        public const float ArmReachShort = 0.085f;
        public const float SoleBelowAnkle = 0.020f;
    }
}
