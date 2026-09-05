using System;
using System.Collections.Generic;
using Tag.Gameplay;
using Tag.Modes;
using Tag.Movement;
using UnityEngine;

namespace Tag.Trail
{
    /// <summary>
    /// Samples at ~20Hz, spawns trail segment colliders, LineRenderer ribbon.
    /// No emit while ragdolled (tunable). Air/wall-run emit OK. Max meters cap.
    /// Dodge i-frames do not suppress trail hits (handled in TrailSegment).
    /// </summary>
    public class PlayerTrailEmitter : MonoBehaviour
    {
        TrailTagTuning _tuning;
        ItController _owner;
        PlayerMotor _motor;
        PlayerRagdoll _ragdoll;
        LineRenderer _line;
        Action<ItController, ItController> _onHit;
        int _colorIndex;

        bool _emittingDesired;
        bool _emitting;
        float _spawnDelayLeft;
        float _sampleAccum;
        float _metersEmitted;
        bool _suddenDeath;
        Vector3 _lastPoint;
        bool _hasLast;
        readonly List<SegmentRec> _segments = new List<SegmentRec>();
        readonly List<Vector3> _linePoints = new List<Vector3>();
        Color _color = new Color(0.2f, 0.9f, 1f, 0.85f);
        float _itBrightness = 1f;

        struct SegmentRec
        {
            public GameObject Go;
            public TrailSegment Seg;
            public float SpawnTime;
            public float Lifetime;
            public Vector3 A;
            public Vector3 B;
            public float Length;
        }

        public bool IsEmitting => _emitting;

        /// <summary>True if any live segment is past self-grace (lethal to owner).</summary>
        public bool HasLethalSegment()
        {
            float now = Time.time;
            float graceSec = _tuning != null ? _tuning.selfHitGraceSec : 0.8f;
            float graceDist = _tuning != null ? _tuning.selfHitGraceDist : 2f;
            if (_suddenDeath && _tuning != null)
            {
                graceSec *= _tuning.suddenDeathGraceScale;
                graceDist *= _tuning.suddenDeathGraceScale;
            }
            for (int i = 0; i < _segments.Count; i++)
            {
                var s = _segments[i];
                if (s.Go == null || s.Seg == null) continue;
                float age = now - s.SpawnTime;
                if (age >= graceSec && s.Length >= 0f) // age gate; dist checked on contact
                    return true;
            }
            return false;
        }

        public string OwnerId => _owner != null ? _owner.PlayerId : name;

        void Awake()
        {
            _owner = GetComponent<ItController>();
            _motor = GetComponent<PlayerMotor>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            EnsureLine();
            if (_tuning == null)
                _tuning = TrailTagTuning.CreateRuntimeDefaults();
            PickColor();
        }

        void EnsureLine()
        {
            if (_line != null) return;
            _line = GetComponent<LineRenderer>();
            if (_line == null)
                _line = gameObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 0;
            _line.widthMultiplier = 0.55f;
            _line.numCapVertices = 2;
            _line.numCornerVertices = 2;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            // Prefer Art bible trail mat when present (Hub import).
            var artMat = Resources.Load<Material>("Mat_Trail_Cyan");
#if UNITY_EDITOR
            if (artMat == null)
                artMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Trail/Mat_Trail_Cyan.mat");
#endif
            if (artMat != null)
            {
                _line.material = artMat;
            }
            else
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null)
                    _line.material = new Material(shader);
            }
        }

        void PickColor()
        {
            if (_tuning != null)
                _color = _tuning.GetColor(_colorIndex);
            else
            {
                int h = (OwnerId ?? name).GetHashCode();
                _color = Color.HSVToRGB(Mathf.Abs(h % 1000) / 1000f, 0.75f, 1f);
                _color.a = 0.9f;
            }
            if (_line != null)
            {
                _line.widthMultiplier = _tuning != null ? _tuning.trailWidth : 0.55f;
                ApplyLineColor();
            }
        }

        public void Configure(TrailTagTuning tuning, ItController owner, Action<ItController, ItController> onHit, int colorIndex = 0)
        {
            _tuning = tuning != null ? tuning : TrailTagTuning.CreateRuntimeDefaults();
            _owner = owner != null ? owner : GetComponent<ItController>();
            _onHit = onHit;
            _colorIndex = colorIndex;
            EnsureLine();
            PickColor();
            ApplyLineColor();
        }

        public void SetItEmphasis(bool isIt, float brightness)
        {
            _itBrightness = isIt ? Mathf.Max(1f, brightness) : 1f;
            PickColor();
            ApplyLineColor();
        }

        void ApplyLineColor()
        {
            if (_line == null) return;
            var c = _color;
            c.r = Mathf.Clamp01(c.r * _itBrightness);
            c.g = Mathf.Clamp01(c.g * _itBrightness);
            c.b = Mathf.Clamp01(c.b * _itBrightness);
            _line.startColor = c;
            _line.endColor = new Color(c.r, c.g, c.b, 0.15f);
            if (_line.material != null)
            {
                if (_line.material.HasProperty("_Color"))
                    _line.material.color = c;
                if (_line.material.HasProperty("_BaseColor"))
                    _line.material.SetColor("_BaseColor", c);
            }
        }

        public void BeginSpawnDelay(float seconds)
        {
            _spawnDelayLeft = Mathf.Max(0f, seconds);
            _hasLast = false;
        }

        public void SetSuddenDeath(bool value)
        {
            _suddenDeath = value;
        }

        public void SetEmitting(bool value)
        {
            _emittingDesired = value;
            if (!value)
            {
                _emitting = false;
                return;
            }
            // Actual emit gated in Update by delay / ragdoll / meters
            if (_spawnDelayLeft <= 0f)
                _emitting = true;
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
            _metersEmitted = 0f;
            _sampleAccum = 0f;
            if (_line != null)
                _line.positionCount = 0;
        }

        public static void ClearAllTrails()
        {
            foreach (var e in FindObjectsByType<PlayerTrailEmitter>(FindObjectsSortMode.None))
                e.ClearTrail();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            ExpireSegments();

            if (_spawnDelayLeft > 0f)
            {
                _spawnDelayLeft -= dt;
                if (_spawnDelayLeft <= 0f && _emittingDesired)
                    _emitting = true;
            }

            if (!_emittingDesired || !_emitting)
            {
                RefreshLine();
                return;
            }

            if (_owner != null && !_owner.IsAlive)
            {
                SetEmitting(false);
                RefreshLine();
                return;
            }

            if (_tuning != null && !_tuning.trailWhileRagdolled && _ragdoll != null && _ragdoll.IsRagdolling)
            {
                _hasLast = false;
                RefreshLine();
                return;
            }

            if (_motor != null && _tuning != null)
            {
                if (!_tuning.trailWhileAirborne && !_motor.IsGrounded && !_motor.IsWallRunning)
                {
                    RefreshLine();
                    return;
                }
                if (!_tuning.trailWhileWallRun && _motor.IsWallRunning)
                {
                    RefreshLine();
                    return;
                }
            }

            if (_tuning != null && _metersEmitted >= _tuning.maxTrailMeters)
            {
                RefreshLine();
                return;
            }

            float hz = _tuning != null ? Mathf.Max(1f, _tuning.sampleHz) : 20f;
            _sampleAccum += dt;
            float interval = 1f / hz;
            if (_sampleAccum < interval)
            {
                RefreshLine();
                return;
            }
            _sampleAccum -= interval;

            float clearance = _tuning != null ? _tuning.bottomClearance : 0.35f;
            Vector3 pos = transform.position + Vector3.up * (clearance + 0.05f);
            if (!_hasLast)
            {
                _lastPoint = pos;
                _hasLast = true;
                RefreshLine();
                return;
            }

            float minSpacing = _tuning != null ? _tuning.minSpacing : 0.25f;
            float dist = Vector3.Distance(pos, _lastPoint);
            if (dist >= minSpacing)
            {
                float remaining = _tuning != null ? _tuning.maxTrailMeters - _metersEmitted : 80f;
                if (dist > remaining) dist = remaining;
                if (dist >= minSpacing * 0.5f)
                {
                    SpawnSegment(_lastPoint, pos);
                    _metersEmitted += dist;
                    _lastPoint = pos;
                }
            }

            RefreshLine();
        }

        void SpawnSegment(Vector3 a, Vector3 b)
        {
            if (_tuning == null) _tuning = TrailTagTuning.CreateRuntimeDefaults();

            Vector3 mid = (a + b) * 0.5f;
            Vector3 delta = b - a;
            float len = Mathf.Max(delta.magnitude, 0.05f);
            Vector3 dir = delta / len;

            var go = new GameObject($"TrailSeg_{OwnerId}");
            go.layer = gameObject.layer;
            float height = _tuning.trailHeight;
            float width = _tuning.trailWidth;
            go.transform.position = mid + Vector3.up * (_tuning.bottomClearance + height * 0.5f);
            if (dir.sqrMagnitude > 0.0001f)
                go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            // Scale drives both mesh and collider (unit cube / size=1)
            go.transform.localScale = new Vector3(width, height, len);

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = Vector3.one;
            col.center = Vector3.zero;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Visible ribbon body (unit cube scaled by transform)
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = GetUnitCubeMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = GetTrailMeshMaterial();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var mpb = new MaterialPropertyBlock();
            var vis = _color;
            vis.r = Mathf.Clamp01(vis.r * _itBrightness);
            vis.g = Mathf.Clamp01(vis.g * _itBrightness);
            vis.b = Mathf.Clamp01(vis.b * _itBrightness);
            mpb.SetColor("_BaseColor", vis);
            mpb.SetColor("_Color", vis);
            mr.SetPropertyBlock(mpb);

            float graceSec = _tuning.selfHitGraceSec;
            float graceDist = _tuning.selfHitGraceDist;
            if (_suddenDeath)
            {
                graceSec *= _tuning.suddenDeathGraceScale;
                graceDist *= _tuning.suddenDeathGraceScale;
            }

            var seg = go.AddComponent<TrailSegment>();
            seg.Init(_owner, _tuning.lifetime, graceSec, graceDist, _tuning.eliminateSelfAfterGrace, HandleHit);

            _segments.Add(new SegmentRec
            {
                Go = go,
                Seg = seg,
                SpawnTime = Time.time,
                Lifetime = _tuning.lifetime,
                A = a,
                B = b,
                Length = len
            });
        }


        static Mesh _unitCube;
        static Material _trailMeshMat;

        static Mesh GetUnitCubeMesh()
        {
            if (_unitCube != null) return _unitCube;
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _unitCube = temp.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.Destroy(temp);
            return _unitCube;
        }

        static Material GetTrailMeshMaterial()
        {
            if (_trailMeshMat != null) return _trailMeshMat;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _trailMeshMat = new Material(shader);
            _trailMeshMat.name = "TrailSegMeshMat";
            return _trailMeshMat;
        }

        void HandleHit(ItController victim, ItController owner)
        {
            _onHit?.Invoke(victim, owner);
        }

        void ExpireSegments()
        {
            if (_tuning == null) return;
            float now = Time.time;
            float fadeStart = Mathf.Clamp01(1f - _tuning.fade); // fade window length as fraction? sheet Fade=0.75s
            // Interpret Fade as seconds of fade at end of life
            float fadeSec = Mathf.Max(0.05f, _tuning.fade);

            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                var s = _segments[i];
                float age = now - s.SpawnTime;
                float life = s.Lifetime > 0f ? s.Lifetime : 6f;

                if (s.Seg != null && age >= life - fadeSec)
                    s.Seg.SetCollisionEnabled(false);

                if (age >= life || s.Go == null)
                {
                    if (s.Go != null) Destroy(s.Go);
                    _metersEmitted = Mathf.Max(0f, _metersEmitted - s.Length);
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
            if (_emitting && _hasLast && (_linePoints.Count == 0 ||
                (_linePoints[_linePoints.Count - 1] - _lastPoint).sqrMagnitude > 0.01f))
                _linePoints.Add(_lastPoint);

            _line.positionCount = _linePoints.Count;
            for (int i = 0; i < _linePoints.Count; i++)
                _line.SetPosition(i, _linePoints[i]);

            if (_tuning != null)
            {
                _line.widthMultiplier = _tuning.trailWidth;
                var start = _color;
                start.a = 0.9f;
                _line.startColor = start;
                var end = _color;
                end.a = 0.15f;
                _line.endColor = end;
            }
        }

        void OnDestroy()
        {
            ClearTrail();
        }
    }
}
