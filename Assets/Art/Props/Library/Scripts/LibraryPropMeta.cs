using UnityEngine;

namespace Tag.Art.Library
{
    /// <summary>
    /// Placement notes for a library prop. Collider objects carry the volumes:
    /// Col_* matches a solid part, Climb_* is the cling face (the wall itself, not a
    /// larger shell), Vault_* is the rail whose top is the vault height.
    /// </summary>
    public class LibraryPropMeta : MonoBehaviour
    {
        public string category;
        public string summary;
        public bool climbable;
        public bool vaultable;
        public float vaultHeightMeters;
        public string climbNote;
        public string vaultNote;
    }
}
