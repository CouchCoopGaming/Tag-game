using TagArena.Movement;
using UnityEngine;

namespace Tag.Experimental
{
    /// <summary>
    /// EXPERIMENTAL — optional giant fan that launches players along fan forward.
    /// Not required for the core tag loop. Place via ExperimentalFanPlacer or by hand.
    /// </summary>
    [DisallowMultipleComponent]
    public class LaunchFan : MonoBehaviour
    {
        [Header("EXPERIMENTAL — disable / remove freely")]
        public float impulse = 22f;
        public float upwardBias = 6f;
        public float cooldownPerBody = 0.55f;
        public bool rotateBlades = true;
        public float bladeRpm = 180f;

        Transform _blades;
        readonly System.Collections.Generic.Dictionary<int, float> _cd = new();

        public static LaunchFan Build(Transform parent, Vector3 localPos, Vector3 lookDir, float impulse = 22f)
        {
            var root = new GameObject("EXPERIMENTAL_LaunchFan");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            if (lookDir.sqrMagnitude > 0.001f)
                root.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

            var fan = root.AddComponent<LaunchFan>();
            fan.impulse = impulse;
            fan.BuildVisuals();
            return fan;
        }

        void Awake()
        {
            if (_blades == null) BuildVisuals();
        }

        void Update()
        {
            if (rotateBlades && _blades != null)
                _blades.Rotate(Vector3.forward, bladeRpm * 6f * Time.deltaTime, Space.Self);
        }

        void OnTriggerStay(Collider other)
        {
            if (other == null) return;
            var motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null) return;
            int id = motor.GetInstanceID();
            if (_cd.TryGetValue(id, out float until) && Time.time < until) return;
            _cd[id] = Time.time + cooldownPerBody;

            var rb = motor.GetComponent<Rigidbody>();
            if (rb == null) return;
            Vector3 dir = transform.forward;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.up;
            dir.Normalize();
            Vector3 boost = dir * impulse + Vector3.up * upwardBias;
            // Additive launch — preserve some existing velocity so ski/jet chains still feel good.
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, boost, 0.65f) + dir * (impulse * 0.25f);
        }

        void BuildVisuals()
        {
            // Pedestal
            var baseGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseGo.name = "FanPedestal";
            baseGo.transform.SetParent(transform, false);
            baseGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            baseGo.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
            Object.Destroy(baseGo.GetComponent<Collider>());
            Tint(baseGo, new Color(0.25f, 0.28f, 0.32f));

            // Hub + blades
            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "FanHub";
            hub.transform.SetParent(transform, false);
            hub.transform.localPosition = new Vector3(0f, 1.55f, 0.15f);
            hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hub.transform.localScale = new Vector3(0.45f, 0.12f, 0.45f);
            Object.Destroy(hub.GetComponent<Collider>());
            Tint(hub, new Color(0.9f, 0.55f, 0.1f));

            _blades = new GameObject("Blades").transform;
            _blades.SetParent(hub.transform, false);
            for (int i = 0; i < 3; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "Blade_" + i;
                blade.transform.SetParent(_blades, false);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
                blade.transform.localPosition = blade.transform.localRotation * new Vector3(0f, 0.7f, 0f);
                blade.transform.localScale = new Vector3(0.18f, 1.3f, 0.06f);
                Object.Destroy(blade.GetComponent<Collider>());
                Tint(blade, new Color(1f, 0.75f, 0.15f));
            }

            // Emissive arrow showing blow direction
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "BlowArrow";
            arrow.transform.SetParent(transform, false);
            arrow.transform.localPosition = new Vector3(0f, 1.55f, 1.1f);
            arrow.transform.localScale = new Vector3(0.25f, 0.25f, 1.4f);
            Object.Destroy(arrow.GetComponent<Collider>());
            Tint(arrow, new Color(0.2f, 0.9f, 1f), emissive: true);

            // Trigger volume in front of fan
            var trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.4f, 1.6f);
            trigger.size = new Vector3(2.4f, 2.6f, 3.2f);
        }

        static void Tint(GameObject go, Color c, bool emissive = false)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Diffuse");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (emissive && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 2f);
            }
            mr.sharedMaterial = m;
        }
    }
}
