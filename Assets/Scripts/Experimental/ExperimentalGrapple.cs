using Tag.Art;
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
    /// The line, the mesh, and the aim cue are presentation. A miss still latches nothing.
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

        /// <summary>True only while enableGrapple is on and a rope is attached. A miss leaves this false.</summary>
        public bool IsPulling => enableGrapple && _attached;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _input = GetComponent<PlayerInputReader>();
        }

        /// <summary>Called from PlayerMotor after input is read, before the single Move.</summary>
        public void ResolveAttach()
        {
            if (!enableGrapple)
            {
                Release();
                return;
            }

            if (Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked)
            {
                Release();
                return;
            }

            EnsureRope();
            if (_attached && (_anchor - transform.position).magnitude > _ropeLength + 3f)
                Release();

            if (!ReadFire())
            {
                Release();
                return;
            }

            if (!_attached)
                TryAttach();
            _casting = !_attached;
        }

        public void Release()
        {
            _attached = false;
            _casting = false;
            _ropeLength = 0f;
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
            if (!enableGrapple || (!_attached && !_casting))
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
                return;
            }

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

            if (best < 0) return;
            _anchor = _hits[best].point;
            _ropeLength = (_anchor - transform.position).magnitude;
            if (_ropeLength <= 0.05f) return;
            _attached = true;
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

        void OnDisable() => Release();

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
    }
}
