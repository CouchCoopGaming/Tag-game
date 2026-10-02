using TagArena.Movement;
using UnityEngine;

namespace Tag.Experimental
{
    /// <summary>
    /// EXPERIMENTAL — optional grapple hook. Core tag loop works without this.
    /// Off by default (enableGrapple=false). Add to a player with PlayerMotor +
    /// PlayerInputReader, set enableGrapple=true. Hold fire (default RMB / JetHeld
    /// while jet is disabled) to attach a LineRenderer rope and pull toward the hit;
    /// release cancels.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExperimentalGrapple : MonoBehaviour
    {
        [Header("EXPERIMENTAL — off by default")]
        public bool enableGrapple = false;
        [Tooltip("If true, uses PlayerInputReader.JetHeld (RMB). Safe while MovementConfig.enableJet=false.")]
        public bool useJetHeldAsFire = true;
        public KeyCode fireKey = KeyCode.Mouse1;
        public float maxRange = 28f;
        public float pullAccel = 38f;
        public float maxPullSpeed = 22f;
        public float attachSlack = 0.35f;
        public LayerMask hitMask = ~0;
        public Color ropeColor = new Color(0.95f, 0.85f, 0.35f, 0.95f);

        PlayerMotor _motor;
        PlayerInputReader _input;
        Rigidbody _rb;
        LineRenderer _rope;
        Material _mat;
        bool _attached;
        Vector3 _anchor;

        /// <summary>True only while the gate is on and a rope is attached. The default gate stays off.</summary>
        public bool IsPulling => enableGrapple && _attached;
        bool _built;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _input = GetComponent<PlayerInputReader>();
            _rb = GetComponent<Rigidbody>();
        }

        void LateUpdate()
        {
            if (!enableGrapple)
            {
                Cancel();
                return;
            }

            // Pause and the results card must not keep pulling.
            if (Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked)
            {
                Cancel();
                return;
            }

            EnsureRope();
            bool fire = ReadFire();
            if (!fire)
            {
                Cancel();
                return;
            }

            if (!_attached)
                TryAttach();

            if (_attached)
                PullAndDraw();
        }

        bool ReadFire()
        {
            if (useJetHeldAsFire && _input != null)
                return _input.JetHeld;
            return UnityEngine.Input.GetKey(fireKey) || UnityEngine.Input.GetMouseButton(1);
        }

        void TryAttach()
        {
            Transform cam = _motor != null && _motor.cam != null ? _motor.cam : Camera.main != null ? Camera.main.transform : null;
            if (cam == null) return;
            Vector3 origin = cam.position;
            Vector3 dir = cam.forward;
            if (!Physics.Raycast(origin, dir, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
                return;
            // Don't latch onto self
            if (hit.rigidbody != null && hit.rigidbody == _rb) return;
            if (hit.collider != null && hit.collider.transform.IsChildOf(transform)) return;
            _anchor = hit.point;
            _attached = true;
        }

        void PullAndDraw()
        {
            if (_rb == null) return;
            Vector3 to = _anchor - transform.position;
            float dist = to.magnitude;
            if (dist < 0.05f)
            {
                Cancel();
                return;
            }

            Vector3 dir = to / dist;
            // Soft pull — accelerate toward anchor, clamp planar+vertical blend
            Vector3 v = _rb.linearVelocity;
            v += dir * (pullAccel * Time.deltaTime);
            if (v.magnitude > maxPullSpeed)
                v = v.normalized * maxPullSpeed;
            // Mild length spring so rope does not overshoot forever
            float over = dist - attachSlack;
            if (over > 0f)
                v += dir * (over * 8f * Time.deltaTime);
            _rb.linearVelocity = v;

            if (_rope != null)
            {
                _rope.enabled = true;
                Vector3 hand = transform.position + Vector3.up * 1.1f + transform.forward * 0.2f;
                _rope.SetPosition(0, hand);
                _rope.SetPosition(1, _anchor);
            }
        }

        void Cancel()
        {
            _attached = false;
            if (_rope != null) _rope.enabled = false;
        }

        void OnDisable() => Cancel();

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
