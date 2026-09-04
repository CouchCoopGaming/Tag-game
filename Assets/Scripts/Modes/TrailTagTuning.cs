using UnityEngine;

namespace Tag.Modes
{
    public enum TrailEmitterMode { All, ItOnly }

    [CreateAssetMenu(fileName = "TrailTagTuning", menuName = "Tag/Modes/Trail Tag Tuning", order = 22)]
    public class TrailTagTuning : ScriptableObject
    {
        public TrailEmitterMode emitters = TrailEmitterMode.All;
        public float trailWidth = 0.55f;
        public float trailHeight = 1.2f;
        public float bottomClearance = 0.35f;
        public float sampleHz = 20f;
        public float minSpacing = 0.25f;
        public float lifetime = 6.0f;
        public float fade = 0.75f;
        public float selfHitGraceSec = 0.80f;
        public float selfHitGraceDist = 2.0f;
        public float spawnTrailDelay = 1.0f;
        public bool trailWhileRagdolled = false;
        public bool trailAirborneWallRun = true;
        public float maxTrailMeters = 80f;
        public float matchTimeCap = 180f;
        public Color trailColor = new Color(0f, 229f/255f, 1f); // #00E5FF Art

        public static TrailTagTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<TrailTagTuning>();
            t.name = "TrailTagTuning (Runtime)";
            return t;
        }
    }
}
