using Tag.Gameplay;
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
        TagBackImmunity.Window _tagBack;
        TagRole _tagBackFrom;
        int _tagPawnId;
        static int _nextTagPawnId = 1;

        public int TagPawnId
        {
            get
            {
                if (_tagPawnId == 0) _tagPawnId = _nextTagPawnId++;
                return _tagPawnId;
            }
        }

        public float TagBackRemaining => _tagBack.Remaining;
        public float TagBackGlow01(float time) => TagBackImmunity.GlowPulse(_tagBack, time);

        void Update()
        {
            _tagBack = TagBackImmunity.Tick(_tagBack, Time.deltaTime);
            if (_tagBack.Remaining <= 0f || _tagBackFrom == null || !_tagBackFrom.IsIt)
                ClearTagBackImmunity();
        }

        /// <summary>
        /// Move It onto <paramref name="victim"/>. Returns false when i-frames or the
        /// tag-back window refuse the transfer. A refused tag-back plays the spark and thunk.
        /// </summary>
        public bool Tag(TagRole victim)
        {
            if (victim == null) return false;
            if (Time.time < victim._safeUntil) return false;
            if (victim.BlocksTagBackFrom(this))
            {
                Tag.Art.TagBackBlockedTell.PlayAt(victim.transform.position + Vector3.up * 1.1f);
                Tag.Audio.TagSfx.TagBackThunk(victim.transform.position);
                return false;
            }
            IsIt = false;
            victim.IsIt = true;
            victim._safeUntil = Time.time + victim.iFrame;
            victim.ClearTagBackImmunity();
            BeginTagBackImmunity(victim, TagBackSeconds(victim));
            ApplyTint();
            victim.ApplyTint();
            return true;
        }

        public void ClearTagBackImmunity()
        {
            _tagBack = default;
            _tagBackFrom = null;
        }

        public void BeginTagBackImmunity(TagRole newIt, float seconds)
        {
            _tagBackFrom = newIt;
            int id = newIt != null ? newIt.TagPawnId : 0;
            _tagBack = TagBackImmunity.Open(id, seconds);
            if (GetComponent<Tag.Art.TagBackGlow>() == null)
                gameObject.AddComponent<Tag.Art.TagBackGlow>();
        }

        public bool BlocksTagBackFrom(TagRole attacker)
        {
            if (attacker == null || _tagBackFrom == null || attacker != _tagBackFrom) return false;
            return TagBackImmunity.Blocks(_tagBack, attacker.TagPawnId);
        }

        float TagBackSeconds(TagRole victim)
        {
            PunchHitbox box = GetComponent<PunchHitbox>();
            if (box == null && victim != null) box = victim.GetComponent<PunchHitbox>();
            return box != null ? box.TagBackImmunitySeconds : TagBackImmunity.DefaultSeconds;
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
