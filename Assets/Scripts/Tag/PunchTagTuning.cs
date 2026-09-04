using UnityEngine;

namespace Tag.Gameplay
{
    /// <summary>
    /// Systems Punch / Transfer-It Numbers v0.
    /// It-only dedicated melee — NOT a passive contact aura.
    /// </summary>
    [CreateAssetMenu(fileName = "PunchTagTuning", menuName = "Tag/Punch Tag Tuning", order = 1)]
    public class PunchTagTuning : ScriptableObject
    {
        [Header("Phases (seconds)")]
        public float inputBuffer = 0.080f;
        public float windup = 0.120f;
        public float active = 0.100f;
        public float hitRecover = 0.150f;
        public float missRecover = 0.320f;

        [Header("Windup")]
        [Range(0f, 1f)] public float windupMoveSpeedScale = 0.70f;
        public bool allowSlideCancelDuringPunch = false;

        [Header("Hitbox")]
        public float reach = 1.35f;
        public float width = 0.70f;
        public float height = 1.20f;
        public float midTorsoHeight = 0.90f;
        public float pitchToleranceDeg = 15f;
        public bool preferContinuousCast = true;
        public LayerMask runnerMask = ~0;
        public LayerMask losMask = ~0;

        [Header("On Hit")]
        public float ragdollDuration = 1.5f;
        public bool ragdollHasIFrames = true;
        public float knockbackHorizontal = 4.0f;
        public float knockbackUp = 2.0f;

        [Header("Puncher Buff")]
        public float speedBuffPercent = 0.08f;
        public float speedBuffDuration = 2.0f;
        public bool speedBuffStacks = false;
        public bool speedBuffRefreshOnHit = true;
        public bool speedBuffClearsOnLosingIt = true;

        public static PunchTagTuning CreateRuntimeDefaults()
        {
            var t = CreateInstance<PunchTagTuning>();
            t.name = "PunchTagTuning (Runtime)";
            return t;
        }
    }
}
