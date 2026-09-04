using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Optional helper. Primary ModeSelect lives in GameFlow OnGUI (keys 1/2/3 + Enter).
    /// Kept so older scene refs do not break; does nothing while GameFlow owns the menu.
    /// Rules copy (if this ever grows labels): Hot Potato first-to-2, fuse 45/40/35 by
    /// player count — not ~75s / It loses. Least It 120s + next-punch tiebreak — not 105s.
    /// </summary>
    public class ModeSelectUI : MonoBehaviour
    {
        [SerializeField] TagModeId highlighted = TagModeId.LeastIt;
        public TagModeId Highlighted => highlighted;
    }
}
