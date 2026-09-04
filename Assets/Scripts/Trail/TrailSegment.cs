using System;
using Tag.Gameplay;
using UnityEngine;

namespace Tag.Trail
{
    /// <summary>Trigger collider for one trail ribbon segment. Calls back on contact.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class TrailSegment : MonoBehaviour
    {
        public string OwnerId { get; private set; }
        public ItController Owner { get; private set; }
        public float SpawnTime { get; private set; }
        public float Lifetime { get; private set; }
        public float SelfGraceSec { get; private set; }
        public float SelfGraceDist { get; private set; }
        public Vector3 SpawnOrigin { get; private set; }
        public bool EliminateSelfAfterGrace { get; private set; }

        Action<ItController, ItController> _onHit;
        bool _collisionEnabled = true;

        public void Init(
            ItController owner,
            float lifetime,
            float selfGraceSec,
            float selfGraceDist,
            bool eliminateSelfAfterGrace,
            Action<ItController, ItController> onHit)
        {
            Owner = owner;
            OwnerId = owner != null ? owner.PlayerId : "?";
            SpawnTime = Time.time;
            Lifetime = lifetime;
            SelfGraceSec = selfGraceSec;
            SelfGraceDist = selfGraceDist;
            SpawnOrigin = transform.position;
            EliminateSelfAfterGrace = eliminateSelfAfterGrace;
            _onHit = onHit;
            _collisionEnabled = true;
        }

        public void SetCollisionEnabled(bool enabled) => _collisionEnabled = enabled;

        void OnTriggerEnter(Collider other)
        {
            if (!_collisionEnabled || other == null) return;

            var victim = other.GetComponentInParent<ItController>();
            if (victim == null || !victim.IsAlive) return;

            // Air-dodge i-frames intentionally do NOT ignore trails (modes sheet).

            bool isSelf = Owner != null && victim == Owner;
            if (isSelf)
            {
                if (!EliminateSelfAfterGrace) return;
                float age = Time.time - SpawnTime;
                float dist = Vector3.Distance(victim.transform.position, SpawnOrigin);
                // Self-lethal only when BOTH age ≥ graceSec AND dist ≥ graceDist (protected while either grace holds)
                if (age < SelfGraceSec || dist < SelfGraceDist) return;
            }

            _onHit?.Invoke(victim, Owner);
        }
    }
}
