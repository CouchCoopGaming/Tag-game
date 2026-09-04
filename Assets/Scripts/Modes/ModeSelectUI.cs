using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Optional helper. Primary ModeSelect lives in GameFlow OnGUI (keys 1/2/3 + Enter).
    /// Kept so older scene refs do not break; does nothing while GameFlow owns the menu.
    /// </summary>
    public class ModeSelectUI : MonoBehaviour
    {
        [SerializeField] TagModeId highlighted = TagModeId.LeastIt;
        public TagModeId Highlighted => highlighted;
    }
}
