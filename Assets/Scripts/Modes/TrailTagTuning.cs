using UnityEngine;

namespace Tag.Modes
{
    public enum TrailEmitterMode
    {
        All = 0,
        ItOnly = 1
    }

    /// <summary>modes-sheet v0.1 Trail Tag knobs.</summary>
    [CreateAssetMenu(fileName = "TrailTagTuning", menuName = "Tag/Modes/Trail Tag Tuning", order = 22)]
    public class TrailTagTuning : ScriptableObject
    {
        [Header("Emitters")]
        public TrailEmitterMode emitters = TrailEmitterMode.All;

        [Header("Ribbon")]
        public float trailWidth = 0.55f;
        public float trailHeight = 1.0f;
        public float bottomClearance = 1.05f;
        public float sampleHz = 20f;
        public float minSpacing = 0.25f;
        public float lifetime = 6.0f;
        public float fade = 0.75f;
        public float maxTrailMeters = 80f;
        public float maxTrailMetersAlive { get => maxTrailMeters; set => maxTrailMeters = value; }

        [Header("Self-hit grace")]
        public float selfHitGraceSec = 0.80f;
        public float selfHitGraceDist = 2.0f;
        public float spawnTrailDelay = 1.0f;
        public bool trailWhileRagdolled = false;
        public bool trailWhileAirborne = true;
        public bool trailWhileWallRun = true;
        public bool eliminateSelfAfterGrace = true;

        [Header("Match")]
        public float stallFailsafeSec = 8f;
        public float matchTimeCap = 180f;
        public float suddenDeathGraceScale = 0.5f;
        [Range(1f, 2f)] public float itTrailBrightness = 1.45f;

        [Header("Colors")]
        // Art bible neon cyan flat ribbon #00E5FF (all slots same until meshes)
        public Color[] colors = new Color[]
        {
            new Color(0f, 229f/255f, 1f, 0.95f),
            new Color(0f, 229f/255f, 1f, 0.95f),
            new Color(0f, 229f/255f, 1f, 0.95f),
            new Color(0f, 229f/255f, 1f, 0.95f)
        };

        public float trailLifetime { get => lifetime; set => lifetime = value; }
        public float segmentLength { get => minSpacing; set => minSpacing = value; }
        public float selfGrace { get => selfHitGraceSec; set => selfHitGraceSec = value; }

        public Color GetColor(int index)
        {
            if (colors == null || colors.Length == 0)
                return new Color(0.3f, 0.8f, 1f, 0.95f);
            return colors[Mathf.Abs(index) % colors.Length];
        }

        public static TrailTagTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<TrailTagTuning>();
            t.name = "TrailTagTuning (Runtime)";
            return t;
        }
    }
}
