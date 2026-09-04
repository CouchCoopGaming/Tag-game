using UnityEngine;

namespace Tag.Modes
{
    [CreateAssetMenu(fileName = "MatchTuning", menuName = "Tag/Modes/Match Tuning", order = 10)]
    public class MatchTuning : ScriptableObject
    {
        public float countdownSec = 3f;
        public float postRoundSec = 4f;
        public float spawnIFramesSec = 1.0f;
        public int minPlayers = 2;
        public enum StartingItRule { Random, Winner, Loser, HostPick }
        public StartingItRule startingIt = StartingItRule.Random;

        public static MatchTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<MatchTuning>();
            t.name = "MatchTuning (Runtime)";
            return t;
        }
    }
}
