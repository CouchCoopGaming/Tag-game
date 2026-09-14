using Tag.Gameplay;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Third-person It tell: bright hat + bobbing crown + pulsing floor halo.
    /// Readable at mid-arena distance without purchased VFX.
    /// </summary>
    public class ItMarker : MonoBehaviour
    {
        [SerializeField] Color itHat = new Color(1f, 0.2f, 0.02f, 1f);
        [SerializeField] Color itGlow = new Color(1f, 0.35f, 0.05f, 0.72f);
        [SerializeField] float hatHeight = 2.12f;
        [SerializeField] float bobAmp = 0.12f;
        [SerializeField] float bobHz = 2.4f;

        ItController _it;
        Transform _hat;
        Transform _halo;
        Light _light;
        Vector3 _hatBaseLocal;
        Vector3 _hatBaseScale;
        Vector3 _haloBaseScale;
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

            float t = Time.time;
            float pulse = 0.78f + 0.22f * Mathf.Sin(t * 7.5f);
            float bob = Mathf.Sin(t * (Mathf.PI * 2f * bobHz)) * bobAmp;

            if (_hat != null)
            {
                _hat.localPosition = _hatBaseLocal + new Vector3(0f, bob, 0f);
                // Slight spin so the brim reads in TP
                _hat.localRotation = Quaternion.Euler(0f, t * 55f, 0f);
                _hat.localScale = _hatBaseScale * (0.96f + 0.08f * pulse);
            }

            if (_halo != null)
                _halo.localScale = _haloBaseScale * pulse;

            if (_light != null)
            {
                _light.intensity = 2.8f * pulse;
                _light.range = 7.5f + 1.2f * pulse;
            }
        }

        void EnsureParts()
        {
            if (_built) return;
            _built = true;

            var hatGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatGo.name = "ItHat";
            hatGo.transform.SetParent(transform, false);
            _hatBaseLocal = new Vector3(0f, hatHeight, 0f);
            hatGo.transform.localPosition = _hatBaseLocal;
            _hatBaseScale = new Vector3(0.48f, 0.2f, 0.48f);
            hatGo.transform.localScale = _hatBaseScale;
            DestroyCollider(hatGo);
            ApplyMat(hatGo, itHat, emissive: true, emissionMul: 2.6f);
            _hat = hatGo.transform;

            var brim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            brim.name = "ItHatBrim";
            brim.transform.SetParent(_hat, false);
            brim.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            brim.transform.localScale = new Vector3(1.7f, 0.14f, 1.7f);
            DestroyCollider(brim);
            ApplyMat(brim, itHat, emissive: true, emissionMul: 2.4f);

            // Crown tip for silhouette read
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "ItHatTip";
            tip.transform.SetParent(_hat, false);
            tip.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            tip.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            DestroyCollider(tip);
            ApplyMat(tip, new Color(1f, 0.85f, 0.15f, 1f), emissive: true, emissionMul: 3.2f);

            var haloGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloGo.name = "ItHalo";
            haloGo.transform.SetParent(transform, false);
            haloGo.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            _haloBaseScale = new Vector3(1.45f, 0.1f, 1.45f);
            haloGo.transform.localScale = _haloBaseScale;
            DestroyCollider(haloGo);
            ApplyMat(haloGo, itGlow, emissive: true, emissionMul: 2.8f);
            _halo = haloGo.transform;

            var lightGo = new GameObject("ItLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.4f, 0.08f);
            _light.range = 8f;
            _light.intensity = 2.8f;
            _light.shadows = LightShadows.None;

            bool on = _it != null && _it.IsIt && _it.IsAlive;
            hatGo.SetActive(on);
            haloGo.SetActive(on);
            _light.enabled = on;
        }

        static void DestroyCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);
        }

        static void ApplyMat(GameObject go, Color c, bool emissive, float emissionMul = 1.8f)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = DummyPrimitiveFactory.MakeMat(c);
            if (emissive)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", c * emissionMul);
                }
            }
            r.sharedMaterial = mat;
        }
    }
}
