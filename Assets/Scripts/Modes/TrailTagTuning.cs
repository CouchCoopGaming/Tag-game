using UnityEngine;

namespace Tag.Modes
{
    [CreateAssetMenu(fileName = "TrailTagTuning", menuName = "Tag/Modes/Trail Tag Tuning", order = 22)]
    public class TrailTagTuning : ScriptableObject
    {
        public float trailWidth = 0.45f;
        public float trailLifetime = 4f;
        public float segmentLength = 0.35f;
        public float selfGrace = 0.6f;
        public int maxPoints = 200;
        public float matchTimeCap = 180f;
        public bool eliminateSelfAfterGrace = true;
        public float trailHeight = 1.2f;
        public float bottomClearance = 0.35f;

        public static TrailTagTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<TrailTagTuning>();
            t.name = "TrailTagTuning (Runtime)";
            t.trailWidth = 0.45f;
            t.trailLifetime = 4f;
            t.segmentLength = 0.35f;
            t.selfGrace = 0.6f;
            t.maxPoints = 200;
            t.matchTimeCap = 180f;
            t.eliminateSelfAfterGrace = true;
            t.trailHeight = 1.2f;
            t.bottomClearance = 0.35f;
            return t;
        }
    }
}
