using UnityEngine;

namespace Tag.Modes
{
    public enum TrailEmitterMode
    {
        All = 0,
        ItOnly = 1
    }

    /// <summary>modes-sheet v0.1 Trail Tag knobs. Defaults scaled for mega park (WorldScale 10).</summary>
    [CreateAssetMenu(fileName = "TrailTagTuning", menuName = "Tag/Modes/Trail Tag Tuning", order = 22)]
    public class TrailTagTuning : ScriptableObject
    {
        [Header("Emitters")]
        public TrailEmitterMode emitters = TrailEmitterMode.All;

        [Header("Ribbon")]
        // Mega campus is ~720x540 world units; keep light-cycle walls readable at distance.
        public float trailWidth = 2.4f;
        public float trailHeight = 2.4f;
        public float bottomClearance = 0.85f;
        public float sampleHz = 20f;
        public float minSpacing = 0.55f;
        public float lifetime = 10.0f;
        public float fade = 1.0f;
        public float maxTrailMeters = 450f;
        public float maxTrailMetersAlive { get => maxTrailMeters; set => maxTrailMeters = value; }

        [Header("Self-hit grace")]
        public float selfHitGraceSec = 1.0f;
        public float selfHitGraceDist = 5.0f;
        public float spawnTrailDelay = 1.0f;
        public bool trailWhileRagdolled = false;
        public bool trailWhileAirborne = true;
        public bool trailWhileWallRun = true;
        public bool eliminateSelfAfterGrace = true;

        [Header("Match")]
        public float stallFailsafeSec = 8f;
        public float matchTimeCap = 180f;
        public float suddenDeathGraceScale = 0.5f;
        [Range(1f, 2.5f)] public float itTrailBrightness = 1.85f;

        [Header("Colors")]
        // Art bible neon cyan flat ribbon #00E5FF — bright for playtest read
        public Color[] colors = new Color[]
        {
            new Color(0.15f, 1f, 1f, 1f),
            new Color(0.15f, 1f, 1f, 1f),
            new Color(0.15f, 1f, 1f, 1f),
            new Color(0.15f, 1f, 1f, 1f)
        };

        public float trailLifetime { get => lifetime; set => lifetime = value; }
        public float segmentLength { get => minSpacing; set => minSpacing = value; }
        public float selfGrace { get => selfHitGraceSec; set => selfHitGraceSec = value; }

        public Color GetColor(int index)
        {
            if (colors == null || colors.Length == 0)
                return new Color(0.3f, 0.95f, 1f, 1f);
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
