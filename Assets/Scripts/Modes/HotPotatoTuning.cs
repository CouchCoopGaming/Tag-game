using UnityEngine;

namespace Tag.Modes
{
    /// <summary>modes-sheet v0.1 Hot Potato — round wins, NOT last-standing elim.</summary>
    [CreateAssetMenu(fileName = "HotPotatoTuning", menuName = "Tag/Modes/Hot Potato Tuning", order = 21)]
    public class HotPotatoTuning : ScriptableObject
    {
        [Tooltip("Fuse by player count: [2]=45,[3]=40,[4]=35.")]
        public float[] durationByPlayerCount = { 0f, 0f, 45f, 40f, 35f };
        public float warnSec = 10f;
        public int winsToTakeMatch = 2;
        public int maxRounds = 3;
        public float postRoundSec = 4f;
        public bool punchTransfersIt = true;

        public float DurationForPlayerCount(int n)
        {
            n = Mathf.Clamp(n, 2, 4);
            if (durationByPlayerCount != null && durationByPlayerCount.Length > n && durationByPlayerCount[n] > 0f)
                return durationByPlayerCount[n];
            return n == 2 ? 45f : n == 3 ? 40f : 35f;
        }

        public static HotPotatoTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<HotPotatoTuning>();
            t.name = "HotPotatoTuning (Runtime)";
            return t;
        }
    }
}
