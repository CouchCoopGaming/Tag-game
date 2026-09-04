using UnityEngine;

namespace Tag.Modes
{
    [CreateAssetMenu(fileName = "HotPotatoTuning", menuName = "Tag/Modes/Hot Potato Tuning", order = 21)]
    public class HotPotatoTuning : ScriptableObject
    {
        [Tooltip("Brief default fuse; DurationForPlayerCount uses this when array unset.")]
        public float fuseDuration = 75f;
        public float warnSec = 10f;
        public float[] durationByPlayerCount = { 0f, 0f, 75f, 75f, 75f };
        public int winsToTakeMatch = 1;
        public int maxRounds = 1;
        public float postRoundSec = 0f;
        public bool punchTransfersIt = true;

        public float DurationForPlayerCount(int n)
        {
            n = Mathf.Clamp(n, 2, 4);
            if (durationByPlayerCount != null && durationByPlayerCount.Length > n && durationByPlayerCount[n] > 0f)
                return durationByPlayerCount[n];
            return fuseDuration > 0f ? fuseDuration : 75f;
        }

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
