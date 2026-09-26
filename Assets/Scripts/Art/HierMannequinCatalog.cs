using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Runtime refs to the curved Hier HiPoly mannequins. Play scene pawns serialize
    /// the flat Dummy_Runner / Dummy_It prefabs; this catalog lets a player build
    /// load Hier without AssetDatabase.
    /// Slots must be the FBX model root (a GameObject). Prefab fileID 100100000 on an
    /// FBX guid does not resolve and Unity treats the slot as null. Until those roots
    /// are assigned in the Editor, <see cref="DummyAvatarBinder"/> loads Tan and Orange
    /// by path.
    /// </summary>
    public class HierMannequinCatalog : ScriptableObject
    {
        public GameObject blue;
        public GameObject mint;
        public GameObject orange;
        public GameObject lavender;
        public GameObject tan;
        public GameObject red;

        public GameObject ForRunner(string color)
        {
            switch (color)
            {
                case "Blue": return blue;
                case "Mint": return mint;
                case "Orange": return orange;
                case "Lavender": return lavender;
                case "Tan": return tan;
                case "Red": return red;
                default: return tan != null ? tan : blue;
            }
        }

        /// <summary>Approved HiPoly runner: Tan (bone + teal).</summary>
        public GameObject Runner => tan != null ? tan : ForRunner("Tan");

        /// <summary>Approved HiPoly It: Orange (orange + black nested Vs). Red remains only if Orange is missing.</summary>
        public GameObject It => orange != null ? orange : (red != null ? red : ForRunner("Orange"));
    }
}
