using UnityEngine;

namespace Tag.Modes
{
    [CreateAssetMenu(fileName = "HotPotatoTuning", menuName = "Tag/Modes/Hot Potato Tuning", order = 21)]
    public class HotPotatoTuning : ScriptableObject
    {
        [Tooltip("Fuse length; when it hits 0 the current It loses / is eliminated.")]
        public float fuseDuration = 75f;
        public float warnSec = 10f;

        public static HotPotatoTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<HotPotatoTuning>();
            t.name = "HotPotatoTuning (Runtime)";
            t.fuseDuration = 75f;
            t.warnSec = 10f;
            return t;
        }
    }
}
