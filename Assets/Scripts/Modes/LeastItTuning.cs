using UnityEngine;

namespace Tag.Modes
{
    [CreateAssetMenu(fileName = "LeastItTuning", menuName = "Tag/Modes/Least It Tuning", order = 20)]
    public class LeastItTuning : ScriptableObject
    {
        public float roundDuration = 105f;

        public static LeastItTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<LeastItTuning>();
            t.name = "LeastItTuning (Runtime)";
            t.roundDuration = 105f;
            return t;
        }
    }
}
