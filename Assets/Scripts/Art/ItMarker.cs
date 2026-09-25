using Tag.Gameplay;
using Tag.Modes;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Third-person It tell: bright hat + bobbing crown + pulsing floor halo.
    /// Hot Potato: pulse harder (scale/color/light) as TagModeController.Remaining runs low.
    /// Readable at mid-arena distance without purchased VFX.
    /// </summary>
    public class ItMarker : MonoBehaviour
    {
        [SerializeField] Color itHat = new Color(1f, 0.2f, 0.02f, 1f);
        [SerializeField] Color itGlow = new Color(1f, 0.35f, 0.05f, 0.72f);
        [SerializeField] float hatHeight = 2.12f;
        [SerializeField] float bobAmp = 0.12f;
        [SerializeField] float bobHz = 2.4f;
        [Tooltip("Fallback Hot Potato fuse warn window when HotPotatoTuning unavailable.")]
        [SerializeField] float hotPotatoWarnSec = 10f;

        ItController _it;
        Transform _hat;
        Transform _halo;
        Light _light;
        Renderer _hatRend;
        Renderer _brimRend;
        Renderer _tipRend;
        Renderer _haloRend;
        Vector3 _hatBaseLocal;
        Vector3 _hatBaseScale;
        Vector3 _haloBaseScale;
        bool _built;
        bool _popPrimed;
        bool _wasOn;
        float _pop;
        Transform _beacon;
        Renderer _beaconRend;

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
            if (!_popPrimed)
            {
                _wasOn = on;
                _popPrimed = true;
            }
            else if (on && !_wasOn)
                _pop = 1f;
            _wasOn = on;
            _pop = Mathf.MoveTowards(_pop, 0f, Time.deltaTime / 0.32f);
            if (!on) return;

            float urgency = HotPotatoFuseUrgency();
            float t = Time.time;
            float pulseHz = Mathf.Lerp(7.5f, 22f, urgency);
            float pulseAmp = 0.22f + 0.45f * urgency;
            float pulse = (0.78f - 0.12f * urgency) + pulseAmp * Mathf.Sin(t * pulseHz);
            float bob = Mathf.Sin(t * (Mathf.PI * 2f * (bobHz + 3.5f * urgency))) * (bobAmp * (1f + 0.8f * urgency));

            float scaleMul = (0.96f + 0.08f * pulse + 0.22f * urgency * pulse) * (1f + 0.7f * _pop);
            // Mega park: a 0.5 m hat disappears past a fort. Grow with camera distance, clamp up close.
            // The chase cam is a child of this body — hide the beacon so it does not fill your own lens.
            float distMul = 1f;
            bool ownView = false;
            var cam = Camera.main;
            if (cam != null)
            {
                ownView = cam.transform.IsChildOf(transform);
                if (!ownView && _hat != null)
                {
                    float d = Vector3.Distance(cam.transform.position, _hat.position);
                    distMul = Mathf.Clamp(d / 16f, 1f, 4.5f);
                }
            }
            if (ownView) scaleMul *= 0.82f;
            else scaleMul *= distMul;
            if (_beacon != null) _beacon.gameObject.SetActive(!ownView);
            if (_hat != null)
            {
                _hat.localPosition = _hatBaseLocal + new Vector3(0f, ownView ? bob * 0.35f : bob, 0f);
                float spin = ownView ? 12f : (55f + 90f * urgency);
                _hat.localRotation = Quaternion.Euler(0f, t * spin, 0f);
                _hat.localScale = _hatBaseScale * scaleMul;
            }

            if (_halo != null)
                _halo.localScale = _haloBaseScale * (pulse * (1f + 0.55f * urgency));

            if (_light != null)
            {
                _light.intensity = (2.8f + 5.5f * urgency) * pulse;
                _light.range = ownView
                    ? 4.5f
                    : (14f + 1.2f * pulse + 6f * urgency) * Mathf.Lerp(1f, 1.6f, (distMul - 1f) / 3.5f);
                _light.color = Color.Lerp(new Color(1f, 0.4f, 0.08f), new Color(1f, 0.95f, 0.55f), urgency);
            }

            // Hotter / brighter materials as fuse drains
            Color hatCol = Color.Lerp(itHat, new Color(1f, 0.92f, 0.35f, 1f), urgency);
            Color glowCol = Color.Lerp(itGlow, new Color(1f, 0.75f, 0.15f, 0.9f), urgency);
            float emitMul = 2.6f + 3.4f * urgency * pulse;
            ApplyRuntimeColor(_hatRend, hatCol, emitMul);
            ApplyRuntimeColor(_brimRend, hatCol, emitMul * 0.92f);
            ApplyRuntimeColor(_tipRend, Color.Lerp(new Color(1f, 0.85f, 0.15f, 1f), Color.white, urgency), emitMul * 1.15f);
            ApplyRuntimeColor(_beaconRend, Color.Lerp(new Color(1f, 0.45f, 0.05f), Color.white, urgency), emitMul * 1.2f);
            ApplyRuntimeColor(_haloRend, glowCol, 2.8f + 4f * urgency * pulse);
        }

        /// <summary>
        /// 0 = calm / not Hot Potato; 1 = fuse about to pop (Remaining near 0).
        /// Uses TagModeController.Remaining vs HotPotatoTuning.warnSec (fallback: hotPotatoWarnSec).
        /// </summary>
        float HotPotatoFuseUrgency()
        {
            var modes = TagModeController.Instance;
            if (modes == null || modes.SelectedMode != TagModeId.HotPotato)
                return 0f;
            float remain = modes.Remaining;
            if (remain <= 0f)
                return 0f;
            float warnSec = hotPotatoWarnSec;
            var tuning = modes.HotPotatoTuningAsset;
            if (tuning != null && tuning.warnSec > 0f)
                warnSec = tuning.warnSec;
            float warn = Mathf.Max(0.5f, warnSec);
            return 1f - Mathf.Clamp01(remain / warn);
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
            _hatRend = ApplyMat(hatGo, itHat, emissive: true, emissionMul: 2.6f);
            _hat = hatGo.transform;

            var brim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            brim.name = "ItHatBrim";
            brim.transform.SetParent(_hat, false);
            brim.transform.localPosition = new Vector3(0f, -0.65f, 0f);
            brim.transform.localScale = new Vector3(1.7f, 0.14f, 1.7f);
            DestroyCollider(brim);
            _brimRend = ApplyMat(brim, itHat, emissive: true, emissionMul: 2.4f);

            // Crown tip for silhouette read
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = "ItHatTip";
            tip.transform.SetParent(_hat, false);
            tip.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            tip.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            DestroyCollider(tip);
            _tipRend = ApplyMat(tip, new Color(1f, 0.85f, 0.15f, 1f), emissive: true, emissionMul: 3.2f);

            // Tall emissive spike so the It reads before the brim does.
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "ItHatBeacon";
            beacon.transform.SetParent(_hat, false);
            beacon.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            beacon.transform.localScale = new Vector3(0.22f, 2.8f, 0.22f);
            DestroyCollider(beacon);
            _beaconRend = ApplyMat(beacon, new Color(1f, 0.45f, 0.05f, 1f), emissive: true, emissionMul: 3.4f);
            _beacon = beacon.transform;

            var haloGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            haloGo.name = "ItHalo";
            haloGo.transform.SetParent(transform, false);
            haloGo.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            _haloBaseScale = new Vector3(1.45f, 0.1f, 1.45f);
            haloGo.transform.localScale = _haloBaseScale;
            DestroyCollider(haloGo);
            _haloRend = ApplyMat(haloGo, itGlow, emissive: true, emissionMul: 2.8f);
            _halo = haloGo.transform;

            var lightGo = new GameObject("ItLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.4f, 0.08f);
            _light.range = 18f;
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

        static Renderer ApplyMat(GameObject go, Color c, bool emissive, float emissionMul = 1.8f)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return null;
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
            return r;
        }

        static void ApplyRuntimeColor(Renderer r, Color c, float emissionMul)
        {
            if (r == null) return;
            var mat = r.material;
            mat.color = c;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * emissionMul);
            }
        }
    }
}
