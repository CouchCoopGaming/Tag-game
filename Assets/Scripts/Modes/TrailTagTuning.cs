using UnityEngine;

namespace Tag.Modes
{
    [CreateAssetMenu(fileName = "TrailTagTuning", menuName = "Tag/Modes/Trail Tag Tuning", order = 22)]
    public class TrailTagTuning : ScriptableObject
    {
        [Header("Ribbon (brief defaults)")]
        public float trailWidth = 0.45f;
        public float trailHeight = 1.2f;
        public float lifetime = 4f;
        public float minSpacing = 0.35f;
        public float bottomClearance = 0.35f;
        public float sampleHz = 20f;
        public float maxTrailMeters = 80f;
        public float fade = 0.75f;
        public int maxPoints = 200;

        [Header("Emit gates")]
        public bool trailWhileRagdolled = false;
        public bool trailWhileAirborne = true;
        public bool trailWhileWallRun = true;

        [Header("Self-hit")]
        public bool eliminateSelfAfterGrace = true;
        public float selfHitGraceSec = 0.6f;
        public float selfHitGraceDist = 1.5f;
        public float suddenDeathGraceScale = 0.5f;

        [Header("Round")]
        public float matchTimeCap = 180f;
        public float spawnEmitDelay = 0.75f;

        [Header("It emphasis")]
        [Range(1f, 2f)] public float itTrailBrightness = 1.45f;

        [Header("Colors (by player index)")]
        public Color[] colors = new Color[]
        {
            new Color(0.20f, 0.85f, 1.00f, 0.95f),
            new Color(1.00f, 0.55f, 0.15f, 0.95f),
            new Color(0.45f, 1.00f, 0.40f, 0.95f),
            new Color(0.95f, 0.35f, 0.85f, 0.95f)
        };

        public float width { get => trailWidth; set => trailWidth = value; }
        public float trailLifetime { get => lifetime; set => lifetime = value; }
        public float segmentLength { get => minSpacing; set => minSpacing = value; }
        public float segmentSpacing { get => minSpacing; set => minSpacing = value; }
        public float selfGrace { get => selfHitGraceSec; set => selfHitGraceSec = value; }
        public float selfHitGrace { get => selfHitGraceSec; set => selfHitGraceSec = value; }
        public bool enableSelfCollision { get => eliminateSelfAfterGrace; set => eliminateSelfAfterGrace = value; }

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
            t.trailWidth = 0.45f;
            t.lifetime = 4f;
            t.minSpacing = 0.35f;
            t.selfHitGraceSec = 0.6f;
            t.maxPoints = 200;
            t.matchTimeCap = 180f;
            t.trailHeight = 1.2f;
            t.bottomClearance = 0.35f;
            return t;
        }
    }
}
