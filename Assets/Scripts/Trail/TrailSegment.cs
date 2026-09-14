using System;
using System.Collections.Generic;
using Tag.Gameplay;
using UnityEngine;

namespace Tag.Trail
{
    /// <summary>Trigger collider for one trail ribbon segment. Calls back on contact.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class TrailSegment : MonoBehaviour
    {
        static readonly List<TrailSegment> s_active = new List<TrailSegment>();

        /// <summary>Live enabled segments (OnEnable/OnDisable). Prefer over FindObjectsByType.</summary>
        public static IReadOnlyList<TrailSegment> Active => s_active;

        public string OwnerId { get; private set; }
        public ItController Owner { get; private set; }
        public float SpawnTime { get; private set; }
        public float Lifetime { get; private set; }
        public float SelfGraceSec { get; private set; }
        public float SelfGraceDist { get; private set; }
        public Vector3 SpawnOrigin { get; private set; }
        public bool EliminateSelfAfterGrace { get; private set; }

        /// <summary>Ribbon sample endpoints (world). Midpoint is transform.position; prefer these for distance.</summary>
        public Vector3 PointA { get; private set; }
        public Vector3 PointB { get; private set; }

        Action<ItController, ItController> _onHit;
        bool _collisionEnabled = true;
        readonly HashSet<int> _hitVictims = new HashSet<int>();

        void OnEnable() => s_active.Add(this);
        void OnDisable() => s_active.Remove(this);

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
            _hitVictims.Clear();
        }

        public void SetEndpoints(Vector3 a, Vector3 b)
        {
            PointA = a;
            PointB = b;
        }

        /// <summary>Closest world point on the A–B ribbon sample (clamped to the segment).</summary>
        public Vector3 ClosestPointOnSegment(Vector3 worldPos)
        {
            Vector3 ab = PointB - PointA;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f)
                return PointA;
            float t = Mathf.Clamp01(Vector3.Dot(worldPos - PointA, ab) / lenSq);
            return PointA + ab * t;
        }

        public void SetCollisionEnabled(bool enabled) => _collisionEnabled = enabled;

        void OnTriggerEnter(Collider other) => TryHit(other);

        // Stay catches: (1) RB ContinuousDynamic tunneling past thin segments,
        // (2) owner still overlapping when self-grace expires (Enter already fired during grace).
        void OnTriggerStay(Collider other) => TryHit(other);

        void TryHit(Collider other)
        {
            if (!_collisionEnabled || other == null) return;

            var victim = other.GetComponentInParent<ItController>();
            if (victim == null || !victim.IsAlive) return;

            int id = victim.GetInstanceID();
            if (_hitVictims.Contains(id)) return;

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

            _hitVictims.Add(id);
            _onHit?.Invoke(victim, Owner);
        }
    }
}
