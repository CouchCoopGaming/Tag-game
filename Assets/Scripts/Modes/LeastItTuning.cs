using UnityEngine;

namespace Tag.Modes
{
    public enum LeastItTieBreak { NextPunch = 0 }

    [CreateAssetMenu(fileName = "LeastItTuning", menuName = "Tag/Modes/Least It Tuning", order = 20)]
    public class LeastItTuning : ScriptableObject
    {
        public float roundDuration = 105f;
        public int roundCount = 1;
        public float scorePrecision = 0.1f;
        public LeastItTieBreak tieBreak = LeastItTieBreak.NextPunch;
        public bool punchTransfersIt = true;

        public static LeastItTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<LeastItTuning>();
            t.name = "LeastItTuning (Runtime)";
            return t;
        }
    }
}
