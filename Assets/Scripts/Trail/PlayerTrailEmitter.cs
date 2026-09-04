using System;
using System.Collections.Generic;
using Tag.Gameplay;
using Tag.Modes;
using Tag.Movement;
using UnityEngine;

namespace Tag.Trail
{
    /// <summary>
    /// Samples position while alive/moving, spawns segment colliders with owner id,
    /// fades by lifetime, LineRenderer ribbon colored by player.
    /// </summary>
    public class PlayerTrailEmitter : MonoBehaviour
    {
        [SerializeField] bool emitOnAwake;
        TrailTagTuning _tuning;
        ItController _owner;
        PlayerMotor _motor;
        PlayerRagdoll _ragdoll;
        LineRenderer _line;
        Action<ItController, ItController> _onHit;

        bool _emitting;
        Vector3 _lastPoint;
        bool _hasLast;
        readonly List<SegmentRec> _segments = new List<SegmentRec>();
        readonly List<Vector3> _linePoints = new List<Vector3>();
        Color _color = new Color(0.2f, 0.9f, 1f, 0.85f);

        struct SegmentRec
        {
            public GameObject Go;
            public TrailSegment Seg;
            public float SpawnTime;
            public float Lifetime;
            public Vector3 A;
            public Vector3 B;
        }

        public bool IsEmitting => _emitting;
        public string OwnerId => _owner != null ? _owner.PlayerId : name;

        void Awake()
        {
            _owner = GetComponent<ItController>();
            _motor = GetComponent<PlayerMotor>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            EnsureLine();
            PickColor();
            if (_tuning == null)
                _tuning = TrailTagTuning.CreateRuntimeDefaults();
            if (emitOnAwake)
                SetEmitting(true);
        }

        void EnsureLine()
        {
            if (_line != null) return;
            _line = GetComponent<LineRenderer>();
            if (_line == null)
                _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 0;
            _line.widthMultiplier = 0.45f;
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            // Unlit-ish default material
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
                _line.material = new Material(shader);
            _line.startColor = _color;
            _line.endColor = new Color(_color.r, _color.g, _color.b, 0.15f);
        }

        void PickColor()
        {
            // Stable-ish hash from name
            int h = (OwnerId ?? name).GetHashCode();
            float hue = Mathf.Abs(h % 1000) / 1000f;
            _color = Color.HSVToRGB(hue, 0.75f, 1f);
            _color.a = 0.9f;
            if (_line != null)
            {
                _line.startColor = _color;
                _line.endColor = new Color(_color.r, _color.g, _color.b, 0.15f);
            }
        }

        public void Configure(TrailTagTuning tuning, ItController owner, Action<ItController, ItController> onHit)
        {
            _tuning = tuning != null ? tuning : TrailTagTuning.CreateRuntimeDefaults();
            _owner = owner != null ? owner : GetComponent<ItController>();
            _onHit = onHit;
            EnsureLine();
            PickColor();
            if (_line != null)
                _line.widthMultiplier = _tuning.trailWidth;
        }

        public void SetEmitting(bool value)
        {
            _emitting = value;
            if (value)
            {
                _hasLast = false;
                PickColor();
            }
        }

        public void ClearTrail()
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i].Go != null)
                    Destroy(_segments[i].Go);
            }
            _segments.Clear();
            _linePoints.Clear();
            _hasLast = false;
            if (_line != null)
                _line.positionCount = 0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            ExpireSegments();

            if (!_emitting) { RefreshLine(); return; }
            if (_owner != null && !_owner.IsAlive) { SetEmitting(false); RefreshLine(); return; }
            if (_ragdoll != null && _ragdoll.IsRagdolling) { RefreshLine(); return; }

            Vector3 pos = transform.position + Vector3.up * (_tuning != null ? _tuning.bottomClearance + 0.05f : 0.4f);
            if (!_hasLast)
            {
                _lastPoint = pos;
                _hasLast = true;
                return;
            }

            float minSpacing = _tuning != null ? _tuning.segmentLength : 0.35f;
            if ((pos - _lastPoint).sqrMagnitude >= minSpacing * minSpacing)
            {
                SpawnSegment(_lastPoint, pos);
                _lastPoint = pos;
            }

            RefreshLine();
            // silence unused
            _ = dt;
        }

        void SpawnSegment(Vector3 a, Vector3 b)
        {
            if (_tuning == null) _tuning = TrailTagTuning.CreateRuntimeDefaults();

            // Cap
            while (_segments.Count >= _tuning.maxPoints)
            {
                var oldest = _segments[0];
                if (oldest.Go != null) Destroy(oldest.Go);
                _segments.RemoveAt(0);
            }

            Vector3 mid = (a + b) * 0.5f;
            Vector3 delta = b - a;
            float len = Mathf.Max(delta.magnitude, 0.05f);
            Vector3 dir = delta / len;

            var go = new GameObject($"TrailSeg_{OwnerId}");
            go.layer = gameObject.layer;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            float height = _tuning.trailHeight;
            float width = _tuning.trailWidth;
            col.size = new Vector3(width, height, len);
            col.center = Vector3.zero;

            go.transform.position = mid + Vector3.up * (height * 0.5f);
            if (dir.sqrMagnitude > 0.0001f)
                go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // Kinematic trigger needs a Rigidbody on one side for CharacterController triggers —
            // put RB on segment so CC players fire OnTriggerEnter.
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var seg = go.AddComponent<TrailSegment>();
            seg.Init(_owner, _tuning.trailLifetime, _tuning.selfGrace, _tuning.eliminateSelfAfterGrace, HandleHit);

            _segments.Add(new SegmentRec
            {
                Go = go,
                Seg = seg,
                SpawnTime = Time.time,
                Lifetime = _tuning.trailLifetime,
                A = a,
                B = b
            });
        }

        void HandleHit(ItController victim, ItController owner)
        {
            _onHit?.Invoke(victim, owner);
        }

        void ExpireSegments()
        {
            float now = Time.time;
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                var s = _segments[i];
                float age = now - s.SpawnTime;
                float life = s.Lifetime > 0f ? s.Lifetime : 4f;
                // Fade window: disable collision in last 25%
                if (s.Seg != null && age >= life * 0.75f)
                    s.Seg.SetCollisionEnabled(false);

                if (age >= life || s.Go == null)
                {
                    if (s.Go != null) Destroy(s.Go);
                    _segments.RemoveAt(i);
                }
            }
        }

        void RefreshLine()
        {
            EnsureLine();
            _linePoints.Clear();
            for (int i = 0; i < _segments.Count; i++)
            {
                _linePoints.Add(_segments[i].A);
                if (i == _segments.Count - 1)
                    _linePoints.Add(_segments[i].B);
            }
            if (_emitting && _hasLast && (_linePoints.Count == 0 || (_linePoints[_linePoints.Count - 1] - _lastPoint).sqrMagnitude > 0.01f))
                _linePoints.Add(_lastPoint);

            _line.positionCount = _linePoints.Count;
            for (int i = 0; i < _linePoints.Count; i++)
                _line.SetPosition(i, _linePoints[i]);

            // Alpha by oldest segment age
            if (_segments.Count > 0 && _tuning != null)
            {
                float age = Time.time - _segments[0].SpawnTime;
                float t = Mathf.Clamp01(age / Mathf.Max(0.01f, _tuning.trailLifetime));
                var start = _color;
                start.a = Mathf.Lerp(0.9f, 0.2f, t);
                _line.startColor = start;
                var end = _color;
                end.a = 0.15f;
                _line.endColor = end;
                _line.widthMultiplier = _tuning.trailWidth;
            }
        }

        void OnDestroy()
        {
            ClearTrail();
        }
    }
}
