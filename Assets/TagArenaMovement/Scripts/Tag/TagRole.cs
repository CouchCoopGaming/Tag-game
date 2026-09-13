using UnityEngine;

namespace TagArena.Movement
{
    public class TagRole : MonoBehaviour
    {
        public bool IsIt;
        public float iFrame = 0.8f;
        public Renderer[] tintTargets;
        public Color runnerColor = new Color(0.3f, 0.8f, 1f);
        public Color itColor = new Color(1f, 0.35f, 0.2f);

        float _safeUntil;

        public void Tag(TagRole victim)
        {
            if (Time.time < victim._safeUntil) return;
            IsIt = false;
            victim.IsIt = true;
            victim._safeUntil = Time.time + victim.iFrame;
            ApplyTint();
            victim.ApplyTint();
        }

        public void ApplyTint()
        {
            if (tintTargets == null) return;
            var c = IsIt ? itColor : runnerColor;
            foreach (var r in tintTargets)
            {
                if (!r) continue;
                foreach (var m in r.materials) m.color = c;
            }
        }

        void Start() => ApplyTint();
    }
}
