using Tag.Art;
using Tag.Local;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Experimental
{
    /// <summary>
    /// EXPERIMENTAL — optional rope. Core tag loop works without this.
    /// The field starts off (enableGrapple=false). LocalPlayerSpawner turns it on
    /// for the solo Player pawn only. Couch pawns and DummyRunner do not get one.
    /// Button: RMB (Mouse1), the same hold as JetHeld. Jet stays off, so RMB does not jet.
    /// The hook attaches to the nearest collider along the camera forward ray that is not
    /// this pawn. A miss attaches to nothing: there is no stand-in swing.
    /// PlayerMotor strips outward horizontal speed against that hit. This component does
    /// not write velocity, does not Move, and does not touch vertical speed.
    /// The line, the mesh, the aim cue, the latch flash, and the miss cue are presentation.
    /// A successful latch flashes the knot and runs a short pulse along the rope.
    /// A miss still latches nothing. A fired miss flicks a short cool stub, not the latch flash.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExperimentalGrapple : MonoBehaviour
    {
        public const string FireButton = "RMB";

        [Header("EXPERIMENTAL — off by default")]
        public bool enableGrapple = false;
        [Tooltip("If true, uses PlayerInputReader.JetHeld (RMB). Safe while MovementConfig.enableJet=false.")]
        public bool useJetHeldAsFire = true;
        public KeyCode fireKey = KeyCode.Mouse1;
        public float maxRange = 28f;
        public float attachSlack = 0.35f;
        public LayerMask hitMask = ~0;
        public Color ropeColor = new Color(0.95f, 0.85f, 0.35f, 0.95f);

        PlayerMotor _motor;
        PlayerInputReader _input;
        Transform _root;
        LineRenderer _rope;
        LineRenderer _halo;
        LineRenderer _aim;
        Transform _mesh;
        Transform _aimMesh;
        Transform _knot;
        bool _attached;
        bool _casting;
        Vector3 _anchor;
        float _ropeLength;
        readonly RaycastHit[] _hits = new RaycastHit[16];
        bool _built;
        float _latchAge = -1f;
        bool _latchBuilt;
        Transform _latchRoot;
        Transform _latchKnot;
        LineRenderer _latchPulse;
        Material _latchMat;
        static Mesh _latchSphere;
        bool _fireWas;
        bool _rayMiss;
        float _missAge = -1f;
        bool _missBuilt;
        Transform _missRoot;
        Transform _missKnot;
        LineRenderer _missStub;
        Material _missMat;
        static Mesh _missDiamond;

        /// <summary>True only while enableGrapple is on and a rope is attached. A miss leaves this false.</summary>
        public bool IsPulling => enableGrapple && _attached;

        /// <summary>Fire is held and nothing is latched. The aim preview is up. Presentation only.</summary>
        public bool IsAiming => enableGrapple && _casting && !_attached;

        /// <summary>Seconds since the latch flash armed. Negative when the flash is quiet.</summary>
        public float LatchAge => _latchAge;

        /// <summary>Seconds since a fired miss. Negative when the flick is quiet.</summary>
        public float MissAge => _missAge;

        /// <summary>True while GrappleMissTell is drawing the stub. A latch stays false.</summary>
        public bool MissFlickOn => GrappleMissTell.Show(MissAvailable(), _attached, _missAge);

        /// <summary>Camera aim used by the preview and the miss stub. Does not cast.</summary>
        public Vector3 PresentationAim() => AimDirection();

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _input = GetComponent<PlayerInputReader>();
        }

        /// <summary>Called from PlayerMotor after input is read, before the single Move.</summary>
        public void ResolveAttach()
        {
            bool gateOpen = enableGrapple && Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked;
            bool held = gateOpen && ReadFire();
            bool fired = held && !_fireWas;
            _fireWas = held;

            if (!enableGrapple)
            {
                Release();
                GrappleMissTell.Clear(ref _missAge);
                return;
            }

            if (Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked)
            {
                Release();
                GrappleMissTell.Clear(ref _missAge);
                return;
            }

            EnsureRope();
            if (_attached && (_anchor - transform.position).magnitude > _ropeLength + 3f)
                Release();

            if (!ReadFire())
            {
                Release();
                _rayMiss = false;
                GrappleMissTell.Note(ref _missAge, MissAvailable(), false, false, false);
                return;
            }

            if (!_attached)
                TryAttach();
            else
                _rayMiss = false;

            _casting = !_attached;
            GrappleMissTell.Note(ref _missAge, MissAvailable(), fired, _rayMiss && !_attached, _attached);
        }

        public void Release()
        {
            _attached = false;
            _casting = false;
            _ropeLength = 0f;
            GrappleLatchTell.Clear(ref _latchAge);
            HideAll();
        }

        public bool TryGetRope(out Vector3 anchor, out float length, out float slack)
        {
            anchor = _anchor;
            length = _ropeLength;
            slack = attachSlack;
            if (!IsPulling || length <= 0.05f)
                return false;
            return true;
        }

        void LateUpdate()
        {
            if (!enableGrapple)
            {
                GrappleMissTell.Clear(ref _missAge);
                HideMiss();
                HideAll();
                return;
            }

            TickMissTell();

            if (!_attached && !_casting)
            {
                HideAll();
                return;
            }

            EnsureRope();
            if (!_built) return;

            Vector3 origin = transform.position;
            Vector3 face = transform.forward;
            if (_attached)
            {
                if (!GrappleRopeTell.AttachedSpan(origin, face, _anchor, out Vector3 hand, out Vector3 end))
                {
                    HideAll();
                    return;
                }

                PlaceSpan(_rope, _halo, _mesh, hand, end, GrappleRopeTell.RopeStartWidth, GrappleRopeTell.RopeEndWidth, GrappleRopeTell.HaloWidth);
                HideSpan(_aim, null, _aimMesh);
                PlaceKnot(end, GrappleRopeTell.HookMarkerSize);
                TickLatchFlash(hand, end);
                return;
            }

            HideLatchFlash();
            if (!GrappleRopeTell.AimSpan(origin, face, AimDirection(), out Vector3 from, out Vector3 tip))
            {
                HideAll();
                return;
            }

            HideSpan(_rope, _halo, _mesh);
            PlaceSpan(_aim, null, _aimMesh, from, tip, GrappleRopeTell.AimWidth, GrappleRopeTell.AimWidth * 0.55f, 0f);
            PlaceKnot(tip, GrappleRopeTell.HookMarkerSize * 0.55f);
        }

        bool ReadFire()
        {
            if (useJetHeldAsFire && _input != null)
                return _input.JetHeld;
            return UnityEngine.Input.GetKey(fireKey) || UnityEngine.Input.GetMouseButton(1);
        }

        void TryAttach()
        {
            _rayMiss = false;
            Transform cam = AimCamera();
            if (cam == null) return;

            int count = Physics.RaycastNonAlloc(cam.position, cam.forward, _hits, maxRange, hitMask, QueryTriggerInteraction.Ignore);
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider col = _hits[i].collider;
                if (col == null) continue;
                Transform hitTransform = col.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
                if (_hits[i].distance < bestDist)
                {
                    bestDist = _hits[i].distance;
                    best = i;
                }
            }

            _rayMiss = best < 0;
            if (best < 0) return;
            _anchor = _hits[best].point;
            _ropeLength = (_anchor - transform.position).magnitude;
            if (_ropeLength <= 0.05f) return;
            _attached = true;
            GrappleLatchTell.Arm(ref _latchAge);
        }

        Transform AimCamera()
        {
            if (_motor != null && _motor.cam != null) return _motor.cam;
            Camera main = Camera.main;
            return main != null ? main.transform : null;
        }

        Vector3 AimDirection()
        {
            Transform cam = AimCamera();
            return cam != null ? cam.forward : transform.forward;
        }

        void OnDisable()
        {
            Release();
            GrappleMissTell.Clear(ref _missAge);
            HideMiss();
        }

        void EnsureRope()
        {
            if (_built) return;
            _built = true;

            Color core = ropeColor;
            core.a = 1f;
            Color halo = ropeColor;
            halo.a = 0.42f;
            Color aim = Color.Lerp(core, Color.white, 0.28f);
            aim.a = 1f;

            var rootGo = new GameObject("EXPERIMENTAL_GrappleRope");
            rootGo.transform.SetParent(transform, false);
            _root = rootGo.transform;

            Material coreMat = CreateMat(core, false);
            Material haloMat = CreateMat(halo, true);
            Material aimMat = CreateMat(aim, false);
            _rope = MakeLine("GrappleRopeCore", _root, coreMat, core, new Color(core.r, core.g, core.b, 0.85f));
            _halo = MakeLine("GrappleRopeHalo", _root, haloMat, halo, new Color(halo.r, halo.g, halo.b, 0.12f));
            _aim = MakeLine("GrappleAimTell", _root, aimMat, aim, new Color(aim.r, aim.g, aim.b, 0.35f));
            _mesh = MakeBar("GrappleRopeMesh", _root, coreMat);
            _aimMesh = MakeBar("GrappleAimMesh", _root, aimMat);
            _knot = MakeKnot("GrappleHookKnot", _root, coreMat);
        }

        void PlaceSpan(LineRenderer core, LineRenderer halo, Transform mesh, Vector3 a, Vector3 b, float startWidth, float endWidth, float haloWidth)
        {
            if (core != null)
            {
                core.enabled = true;
                core.startWidth = startWidth;
                core.endWidth = endWidth;
                core.SetPosition(0, a);
                core.SetPosition(1, b);
            }

            if (halo != null)
            {
                halo.enabled = haloWidth > 0.001f;
                if (halo.enabled)
                {
                    halo.startWidth = haloWidth;
                    halo.endWidth = Mathf.Max(endWidth, haloWidth * 0.65f);
                    halo.SetPosition(0, a);
                    halo.SetPosition(1, b);
                }
            }

            if (mesh != null)
                PlaceBar(mesh, a, b, startWidth);
        }

        void PlaceBar(Transform bar, Vector3 a, Vector3 b, float thickness)
        {
            Vector3 delta = b - a;
            float len = delta.magnitude;
            if (len < 0.02f)
            {
                bar.gameObject.SetActive(false);
                return;
            }

            bar.gameObject.SetActive(true);
            bar.position = (a + b) * 0.5f;
            bar.rotation = Quaternion.FromToRotation(Vector3.up, delta);
            SetWorldScale(bar, thickness, len * 0.5f, thickness);
        }

        void PlaceKnot(Vector3 point, float size)
        {
            if (_knot == null) return;
            _knot.gameObject.SetActive(true);
            _knot.position = point;
            SetWorldScale(_knot, size, size, size);
        }

        static void SetWorldScale(Transform t, float x, float y, float z)
        {
            Vector3 lossy = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(SafeDiv(x, lossy.x), SafeDiv(y, lossy.y), SafeDiv(z, lossy.z));
        }

        static float SafeDiv(float value, float scale)
        {
            float denom = Mathf.Abs(scale) > 1e-4f ? scale : 1f;
            return value / denom;
        }

        void HideSpan(LineRenderer core, LineRenderer halo, Transform mesh)
        {
            if (core != null) core.enabled = false;
            if (halo != null) halo.enabled = false;
            if (mesh != null) mesh.gameObject.SetActive(false);
        }

        void HideAll()
        {
            HideSpan(_rope, _halo, _mesh);
            HideSpan(_aim, null, _aimMesh);
            if (_knot != null) _knot.gameObject.SetActive(false);
            HideLatchFlash();
        }

        static LineRenderer MakeLine(string name, Transform parent, Material mat, Color start, Color end)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.numCapVertices = 4;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            if (mat != null) line.sharedMaterial = mat;
            line.startColor = start;
            line.endColor = end;
            line.enabled = false;
            return line;
        }

        static Transform MakeBar(string name, Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            StripCollider(go);
            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
                if (mat != null) rend.sharedMaterial = mat;
            }
            go.SetActive(false);
            return go.transform;
        }

        static Transform MakeKnot(string name, Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            StripCollider(go);
            var rend = go.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
                if (mat != null) rend.sharedMaterial = mat;
            }
            go.SetActive(false);
            return go.transform;
        }

        static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        static Material CreateMat(Color c, bool transparent)
        {
            var shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Unlit/Transparent")
                         ?? Shader.Find("Standard");
            if (shader == null) return null;
            var m = new Material(shader);
            Texture2D tex = Texture2D.whiteTexture;
            if (tex != null)
            {
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * 1.35f);
            }
            if (!transparent) return m;

            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", 5f);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", 10f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            return m;
        }

        void TickLatchFlash(Vector3 hand, Vector3 knot)
        {
            if (!GrappleLatchTell.Show(true, _latchAge)
                || !GrappleLatchTell.Knot(true, knot, _latchAge, out Vector3 at))
            {
                HideLatchFlash();
                return;
            }

            EnsureLatchFlash();
            float alpha = GrappleLatchTell.Alpha(_latchAge);
            PlaceLatchKnot(at, GrappleLatchTell.KnotScale(GrappleLatchTell.Fade(_latchAge)), alpha);
            if (GrappleLatchTell.Pulse(true, hand, knot, _latchAge, out Vector3 from, out Vector3 to))
                PlaceLatchPulse(from, to, alpha);
            else if (_latchPulse != null)
                _latchPulse.enabled = false;
            GrappleLatchTell.Step(ref _latchAge, Time.deltaTime, true);
        }

        void PlaceLatchKnot(Vector3 point, float size, float alpha)
        {
            if (_latchKnot == null) return;
            _latchKnot.gameObject.SetActive(true);
            _latchKnot.position = point;
            SetWorldScale(_latchKnot, size, size, size);
            PaintLatch(alpha);
        }

        void PlaceLatchPulse(Vector3 a, Vector3 b, float alpha)
        {
            if (_latchPulse == null) return;
            _latchPulse.enabled = true;
            _latchPulse.startWidth = GrappleLatchTell.PulseWidth;
            _latchPulse.endWidth = GrappleLatchTell.PulseWidth;
            _latchPulse.SetPosition(0, a);
            _latchPulse.SetPosition(1, b);
            var c = new Color(GrappleLatchTell.MarkR, GrappleLatchTell.MarkG, GrappleLatchTell.MarkB, alpha);
            _latchPulse.startColor = c;
            _latchPulse.endColor = c;
            PaintLatch(alpha);
        }

        void HideLatchFlash()
        {
            if (_latchKnot != null) _latchKnot.gameObject.SetActive(false);
            if (_latchPulse != null) _latchPulse.enabled = false;
        }

        void EnsureLatchFlash()
        {
            if (_latchBuilt) return;
            _latchBuilt = true;
            var rootGo = new GameObject(GrappleLatchTell.MarkerName);
            rootGo.transform.SetParent(transform, false);
            _latchRoot = rootGo.transform;
            _latchMat = MakeLatchMat();
            _latchKnot = MakeLatchKnot("GrappleLatchKnot");
            _latchPulse = MakeLatchPulse("GrappleLatchPulse");
        }

        Transform MakeLatchKnot(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_latchRoot, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = LatchSphere();
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = _latchMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = Vector3.one * GrappleLatchTell.KnotSize;
            go.SetActive(false);
            return go.transform;
        }

        LineRenderer MakeLatchPulse(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_latchRoot, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.numCapVertices = 4;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.startWidth = GrappleLatchTell.PulseWidth;
            line.endWidth = GrappleLatchTell.PulseWidth;
            if (_latchMat != null) line.sharedMaterial = _latchMat;
            var c = new Color(GrappleLatchTell.MarkR, GrappleLatchTell.MarkG, GrappleLatchTell.MarkB, GrappleLatchTell.MaxAlpha);
            line.startColor = c;
            line.endColor = c;
            line.enabled = false;
            return line;
        }

        static Material MakeLatchMat()
        {
            var shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Transparent")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "GrappleLatchFlash" };
            var c = new Color(GrappleLatchTell.MarkR, GrappleLatchTell.MarkG, GrappleLatchTell.MarkB, GrappleLatchTell.MaxAlpha);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", 5f);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", 10f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            return mat;
        }

        void PaintLatch(float alpha)
        {
            if (_latchMat == null) return;
            var c = new Color(GrappleLatchTell.MarkR, GrappleLatchTell.MarkG, GrappleLatchTell.MarkB, alpha);
            if (_latchMat.HasProperty("_BaseColor")) _latchMat.SetColor("_BaseColor", c);
            if (_latchMat.HasProperty("_Color")) _latchMat.SetColor("_Color", c);
        }

        static Mesh LatchSphere()
        {
            if (_latchSphere != null) return _latchSphere;
            const int slices = 12;
            const int stacks = 8;
            var verts = new Vector3[(stacks + 1) * (slices + 1)];
            var tris = new int[stacks * slices * 6];
            int vi = 0;
            for (int y = 0; y <= stacks; y++)
            {
                float phi = Mathf.PI * y / stacks;
                float yPos = Mathf.Cos(phi) * 0.5f;
                float r = Mathf.Sin(phi) * 0.5f;
                for (int x = 0; x <= slices; x++)
                {
                    float theta = Mathf.PI * 2f * x / slices;
                    verts[vi++] = new Vector3(Mathf.Cos(theta) * r, yPos, Mathf.Sin(theta) * r);
                }
            }

            int ti = 0;
            int row = slices + 1;
            for (int y = 0; y < stacks; y++)
            {
                for (int x = 0; x < slices; x++)
                {
                    int a = y * row + x;
                    int b = a + row;
                    tris[ti++] = a;
                    tris[ti++] = b;
                    tris[ti++] = a + 1;
                    tris[ti++] = a + 1;
                    tris[ti++] = b;
                    tris[ti++] = b + 1;
                }
            }

            var mesh = new Mesh { name = "GrappleLatchKnot" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            _latchSphere = mesh;
            return mesh;
        }

        bool MissAvailable()
        {
            return enableGrapple && GrappleMissTell.ForPawn(LocalPlayerRoster.IsCouch, false, 0, gameObject.name);
        }

        void TickMissTell()
        {
            bool available = MissAvailable();
            if (!GrappleMissTell.Show(available, _attached, _missAge))
            {
                HideMiss();
                if (!available || _attached)
                    GrappleMissTell.Clear(ref _missAge);
                return;
            }

            EnsureMissTell();
            Vector3 hand = GrappleRopeTell.Hand(transform.position, transform.forward);
            float alpha = GrappleMissTell.Alpha(_missAge);
            float fade = GrappleMissTell.Fade(_missAge);
            if (GrappleMissTell.Knot(available, _attached, hand, _missAge, out Vector3 at))
                PlaceMissKnot(at, GrappleMissTell.KnotScale(fade), alpha);
            if (GrappleMissTell.Stub(available, _attached, hand, AimDirection(), _missAge, out Vector3 from, out Vector3 to))
                PlaceMissStub(from, to, alpha);
            else if (_missStub != null)
                _missStub.enabled = false;
            GrappleMissTell.Step(ref _missAge, Time.deltaTime, available, _attached);
        }

        void PlaceMissKnot(Vector3 point, float size, float alpha)
        {
            if (_missKnot == null) return;
            _missKnot.gameObject.SetActive(true);
            _missKnot.position = point;
            SetWorldScale(_missKnot, size, size, size);
            PaintMiss(alpha);
        }

        void PlaceMissStub(Vector3 a, Vector3 b, float alpha)
        {
            if (_missStub == null) return;
            _missStub.enabled = true;
            _missStub.startWidth = GrappleMissTell.StubWidth;
            _missStub.endWidth = GrappleMissTell.StubWidth;
            _missStub.SetPosition(0, a);
            _missStub.SetPosition(1, b);
            var c = new Color(GrappleMissTell.MarkR, GrappleMissTell.MarkG, GrappleMissTell.MarkB, alpha);
            _missStub.startColor = c;
            _missStub.endColor = c;
            PaintMiss(alpha);
        }

        void HideMiss()
        {
            if (_missKnot != null) _missKnot.gameObject.SetActive(false);
            if (_missStub != null) _missStub.enabled = false;
        }

        void EnsureMissTell()
        {
            if (_missBuilt) return;
            _missBuilt = true;
            var rootGo = new GameObject(GrappleMissTell.MarkerName);
            rootGo.transform.SetParent(transform, false);
            _missRoot = rootGo.transform;
            _missMat = MakeMissMat();
            _missKnot = MakeMissKnot("GrappleMissKnot");
            _missStub = MakeMissStub("GrappleMissStub");
        }

        Transform MakeMissKnot(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_missRoot, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = MissDiamond();
            var rend = go.AddComponent<MeshRenderer>();
            rend.sharedMaterial = _missMat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;
            go.transform.localScale = Vector3.one * GrappleMissTell.KnotSize;
            go.SetActive(false);
            return go.transform;
        }

        LineRenderer MakeMissStub(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_missRoot, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.numCapVertices = 4;
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.startWidth = GrappleMissTell.StubWidth;
            line.endWidth = GrappleMissTell.StubWidth;
            if (_missMat != null) line.sharedMaterial = _missMat;
            var c = new Color(GrappleMissTell.MarkR, GrappleMissTell.MarkG, GrappleMissTell.MarkB, GrappleMissTell.MaxAlpha);
            line.startColor = c;
            line.endColor = c;
            line.enabled = false;
            return line;
        }

        static Material MakeMissMat()
        {
            var shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Transparent")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "GrappleMissFail" };
            var c = new Color(GrappleMissTell.MarkR, GrappleMissTell.MarkG, GrappleMissTell.MarkB, GrappleMissTell.MaxAlpha);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", 5f);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", 10f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            return mat;
        }

        void PaintMiss(float alpha)
        {
            if (_missMat == null) return;
            var c = new Color(GrappleMissTell.MarkR, GrappleMissTell.MarkG, GrappleMissTell.MarkB, alpha);
            if (_missMat.HasProperty("_BaseColor")) _missMat.SetColor("_BaseColor", c);
            if (_missMat.HasProperty("_Color")) _missMat.SetColor("_Color", c);
        }

        static Mesh MissDiamond()
        {
            if (_missDiamond != null) return _missDiamond;
            var verts = new[]
            {
                new Vector3(0f, 0.5f, 0f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(0f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0f),
                new Vector3(0f, 0f, -0.5f),
                new Vector3(0f, -0.5f, 0f),
            };
            var tris = new[]
            {
                0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1,
                5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 1, 4,
            };
            var mesh = new Mesh { name = "GrappleMissKnot" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            _missDiamond = mesh;
            return mesh;
        }
    }
}
