using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Makes Tribes jet thrust visible in third-person (LineRenderer + nozzle glow).
    /// There is no grapple/zipline in this project — RMB jet was the invisible "rope" feel.
    /// </summary>
    public class JetThrustVisual : MonoBehaviour
    {
        PlayerMotor _motor;
        Transform _anchor;
        LineRenderer _beam;
        Transform _nozzle;
        Material _mat;
        bool _built;

        public void Bind(PlayerMotor motor, Transform visualRoot)
        {
            _motor = motor;
            _anchor = visualRoot != null ? visualRoot : transform;
            EnsureBuilt();
        }

        void LateUpdate()
        {
            if (_motor == null) _motor = GetComponentInParent<PlayerMotor>();
            if (_anchor == null) _anchor = transform;
            EnsureBuilt();
            if (!_built) return;

            bool on = _motor != null && _motor.Jetting;
            _beam.enabled = on;
            if (_nozzle != null) _nozzle.gameObject.SetActive(on);
            if (!on) return;

            Vector3 origin = _anchor.position + Vector3.up * 0.85f - _anchor.forward * 0.12f;
            // Thrust reads as a short pack flame — down + slightly back
            Vector3 dir = (-_anchor.up * 0.65f - _anchor.forward * 0.45f).normalized;
            float len = 0.85f + 0.25f * Mathf.Sin(Time.time * 22f);
            _beam.SetPosition(0, origin);
            _beam.SetPosition(1, origin + dir * len);
            _beam.widthMultiplier = 0.12f + 0.04f * Mathf.Sin(Time.time * 30f);
            if (_nozzle != null)
            {
                _nozzle.position = origin;
                _nozzle.rotation = Quaternion.LookRotation(dir);
                float s = 0.14f + 0.04f * Mathf.Sin(Time.time * 26f);
                _nozzle.localScale = new Vector3(s, s, s * 1.6f);
            }
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var go = new GameObject("JetThrustBeam");
            go.transform.SetParent(transform, false);
            _beam = go.AddComponent<LineRenderer>();
            _beam.positionCount = 2;
            _beam.startWidth = 0.14f;
            _beam.endWidth = 0.02f;
            _beam.numCapVertices = 4;
            _beam.useWorldSpace = true;
            _beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _beam.receiveShadows = false;
            _mat = CreateGlowMat(new Color(0.25f, 0.85f, 1f, 0.95f));
            _beam.sharedMaterial = _mat;
            _beam.startColor = new Color(0.6f, 0.95f, 1f, 0.95f);
            _beam.endColor = new Color(0.1f, 0.4f, 1f, 0.05f);
            _beam.enabled = false;

            var nozzleGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nozzleGo.name = "JetNozzleGlow";
            Object.Destroy(nozzleGo.GetComponent<Collider>());
            nozzleGo.transform.SetParent(transform, false);
            var mr = nozzleGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = _mat;
            nozzleGo.SetActive(false);
            _nozzle = nozzleGo.transform;
        }

        static Material CreateGlowMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Standard");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 2.5f);
            }
            return m;
        }
    }
}
