using Tag.Gameplay;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Third-person It tell: orange hat + pulsing halo. Works without FBX bind.
    /// </summary>
    public class ItMarker : MonoBehaviour
    {
        [SerializeField] Color itHat = new Color(1f, 0.35f, 0.05f, 1f);
        [SerializeField] Color itGlow = new Color(1f, 0.45f, 0.08f, 0.55f);
        [SerializeField] float hatHeight = 2.05f;

        ItController _it;
        Transform _hat;
        Transform _halo;
        Light _light;
        bool _built;

        void Awake()
        {
            _it = GetComponent<ItController>();
            EnsureParts();
        }

        void LateUpdate()
        {
            if (!_built) EnsureParts();
            bool on = _it != null && _it.IsIt && _it.IsAlive;
            if (_hat != null) _hat.gameObject.SetActive(on);
            if (_halo != null) _halo.gameObject.SetActive(on);
            if (_light != null) _light.enabled = on;
            if (!on) return;

            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 6f);
            if (_halo != null)
                _halo.localScale = new Vector3(1.15f, 0.08f, 1.15f) * pulse;
            if (_light != null)
                _light.intensity = 1.6f * pulse;
        }

        void EnsureParts()
        {
            if (_built) return;
            _built = true;

            var hatGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatGo.name = "ItHat";
            hatGo.transform.SetParent(transform, false);
            hatGo.transform.localPosition = new Vector3(0f, hatHeight, 0f);
            hatGo.transform.localScale = new Vector3(0.38f, 0.16f, 0.38f);
            DestroyCollider(hatGo);
            ApplyMat(hatGo, itHat, emissive: true);
            _hat = hatGo.transform;

            var brim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            brim.name = "ItHatBrim";
            brim.transform.SetParent(_hat, false);
            brim.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            brim.transform.localScale = new Vector3(1.55f, 0.12f, 1.55f);
            DestroyCollider(brim);
            ApplyMat(brim, itHat, emissive: true);

            var haloGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloGo.name = "ItHalo";
            haloGo.transform.SetParent(transform, false);
            haloGo.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            haloGo.transform.localScale = new Vector3(1.15f, 0.08f, 1.15f);
            DestroyCollider(haloGo);
            ApplyMat(haloGo, itGlow, emissive: true);
            _halo = haloGo.transform;

            var lightGo = new GameObject("ItLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.5f, 0.15f);
            _light.range = 6f;
            _light.intensity = 1.6f;
            _light.shadows = LightShadows.None;

            bool on = _it != null && _it.IsIt;
            hatGo.SetActive(on);
            haloGo.SetActive(on);
            _light.enabled = on;
        }

        static void DestroyCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);
        }

        static void ApplyMat(GameObject go, Color c, bool emissive)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = DummyPrimitiveFactory.MakeMat(c);
            if (emissive)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", c * 1.8f);
                }
            }
            r.sharedMaterial = mat;
        }
    }
}
