namespace Tag.Local
{
    /// <summary>
    /// Solo tell. EnableFor is the solo human only.
    /// Couch humans take the same rope through CouchRope. The opponent does not.
    /// A miss is not decided here: the hook still latches nothing when the ray hits nothing.
    /// </summary>
    public static class SoloGrappleGate
    {
        public const string SoloPawnName = "Player";
        public const string OpponentPawnName = "DummyRunner";

        public static bool EnableFor(bool couch, bool ai, int index, string pawnName)
        {
            if (couch || ai || index != 0) return false;
            if (pawnName == OpponentPawnName) return false;
            return pawnName == SoloPawnName;
        }
    }
}
