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
        LineRenderer _rope;
        Material _mat;
        bool _attached;
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
        }

        public void Release()
        {
            _attached = false;
            _ropeLength = 0f;
            if (_rope != null) _rope.enabled = false;
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
            if (!IsPulling)
            {
                if (_rope != null) _rope.enabled = false;
                return;
            }

            EnsureRope();
            if (_rope == null) return;
            _rope.enabled = true;
            Vector3 hand = transform.position + Vector3.up * 1.1f + transform.forward * 0.2f;
            _rope.SetPosition(0, hand);
            _rope.SetPosition(1, _anchor);
        }

        bool ReadFire()
        {
            if (useJetHeldAsFire && _input != null)
                return _input.JetHeld;
            return UnityEngine.Input.GetKey(fireKey) || UnityEngine.Input.GetMouseButton(1);
        }

        void TryAttach()
        {
            Transform cam = _motor != null && _motor.cam != null
                ? _motor.cam
                : Camera.main != null ? Camera.main.transform : null;
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

        void OnDisable() => Release();

        void EnsureRope()
        {
            if (_built) return;
            _built = true;
            var go = new GameObject("EXPERIMENTAL_GrappleRope");
            go.transform.SetParent(transform, false);
            _rope = go.AddComponent<LineRenderer>();
            _rope.positionCount = 2;
            _rope.startWidth = 0.06f;
            _rope.endWidth = 0.03f;
            _rope.numCapVertices = 3;
            _rope.useWorldSpace = true;
            _rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _rope.receiveShadows = false;
            _mat = CreateMat(ropeColor);
            _rope.sharedMaterial = _mat;
            _rope.startColor = ropeColor;
            _rope.endColor = new Color(ropeColor.r, ropeColor.g, ropeColor.b, 0.35f);
            _rope.enabled = false;
        }

        static Material CreateMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Standard");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }
    }
}
