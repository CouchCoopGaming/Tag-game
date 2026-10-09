using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using Tag.Settings;
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
        GUIStyle _itStyle;
        string _plateName = "";
        Color _plateTint = Color.white;
        int _shape;
        bool _hasPlate;
        Transform _plate;
        Renderer _plateRend;
        TextMesh _plateText;
        bool _plateBuilt;

        void Awake()
        {
            _it = GetComponent<ItController>();
            EnsureParts();
        }

        /// <summary>One tinted name plate, built once. Same emissive path as the It hat.</summary>
        public void SetIdentity(string plateName, Color tint)
        {
            SetIdentity(plateName, tint, 0);
        }

        /// <summary>Shape 0 circle, 1 triangle, 2 square, 3 diamond. The glyph matches.</summary>
        public void SetIdentity(string plateName, Color tint, int shape)
        {
            _plateName = plateName ?? "";
            _plateTint = tint;
            _shape = shape;
            if (_shape < 0) _shape = 0;
            if (_shape > 3) _shape = 3;
            _hasPlate = true;
            EnsurePlate();
        }

        void EnsurePlate()
        {
            if (_plateBuilt || !_hasPlate) return;
            _plateBuilt = true;
            GameObject go;
            if (_shape == 1)
                go = TrianglePlate();
            else
            {
                PrimitiveType kind = PrimitiveType.Sphere;
                if (_shape == 2) kind = PrimitiveType.Cube;
                else if (_shape == 3) kind = PrimitiveType.Cylinder;
                go = GameObject.CreatePrimitive(kind);
            }
            go.name = "NameTag";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            go.transform.localScale = new Vector3(0.28f, 0.18f, 0.28f);
            DestroyCollider(go);
            _plateRend = ApplyMat(go, _plateTint, true, 2.4f);
            _plate = go.transform;

            var textGo = new GameObject("NameText");
            textGo.transform.SetParent(go.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var mesh = textGo.AddComponent<TextMesh>();
            mesh.text = _plateName;
            _plateText = mesh;

            var markGo = new GameObject("NameShape");
            markGo.transform.SetParent(go.transform, false);
            markGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            var mark = markGo.AddComponent<TextMesh>();
            mark.text = AccessibilityPalette.Glyph(_shape);
            mark.characterSize = 0.28f;
            mark.anchor = TextAnchor.MiddleCenter;
            mark.alignment = TextAlignment.Center;
            mark.color = Color.white;
            mark.fontSize = 64;
            mesh.characterSize = 0.22f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = _plateTint;
            mesh.fontSize = 48;
        }

        void WarmStyle()
        {
            if (_itStyle == null) BootStyle();
        }

        void BootStyle()
        {
            _itStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
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
            _pop = Mathf.MoveTowards(_pop, 0f, Time.deltaTime / 0.34f); // slightly longer handoff pop so It read sticks
            if (!on) return;

            float urgency = HotPotatoFuseUrgency();
            float t = Time.time;
            float pulseHz = Mathf.Lerp(7.5f, 22f, urgency);
            float pulseAmp = 0.24f + 0.48f * urgency; // hotter fuse pulse for Hot Potato read
            float pulse = (0.78f - 0.12f * urgency) + pulseAmp * Mathf.Sin(t * pulseHz);
            float bob = Mathf.Sin(t * (Mathf.PI * 2f * (bobHz + 3.5f * urgency))) * (bobAmp * (1f + 0.8f * urgency));
            if (GameSettings.Current != null && GameSettings.Current.AnyReduceFlash())
            {
                pulse = GameSettings.Current.GlowVisual(pulse);
                bob = 0f;
            }

            float scaleMul = (0.96f + 0.08f * pulse + 0.22f * urgency * pulse) * (1f + 0.7f * _pop);
            // Mega park: a 0.5 m hat disappears past a fort. Grow with camera distance, clamp up close.
            // The chase cam is a child of this body — hide the beacon so it does not fill your own lens.
            float distMul = 1f;
            bool ownView = false;
            bool pocket = ParkArena.IsPocket;
            var cam = Camera.main;
            if (cam != null)
            {
                ownView = cam.transform.IsChildOf(transform);
                if (!ownView && _hat != null)
                {
                    float d = Vector3.Distance(cam.transform.position, _hat.position);
                    float reach = pocket ? 10f : 16f;
                    float cap = pocket ? 2.4f : 4.5f;
                    distMul = Mathf.Clamp(d / reach, 1f, cap);
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

            int pal = 0;
            if (GameSettings.Current != null)
                pal = GameSettings.Current.PaletteOf(_shape);
            int crown = Tag.Profiles.LocalProfiles.SeatColor(_shape);
            if (crown < 0) crown = _shape;
            AccessibilityPalette.ItAgainst(pal, crown, out float ir, out float ig, out float ib);
            Color itCol = new Color(ir, ig, ib, 1f);
            if (_light != null)
            {
                _light.intensity = (2.8f + 5.5f * urgency) * pulse;
                _light.range = ownView
                    ? 4.5f
                    : (14f + 1.2f * pulse + 6f * urgency) * Mathf.Lerp(1f, 1.6f, (distMul - 1f) / 3.5f);
                _light.color = Color.Lerp(itCol, new Color(1f, 0.95f, 0.55f), urgency);
            }
            if (_plateRend != null)
            {
                AccessibilityPalette.Player(pal, _shape, out float pr, out float pg, out float pb);
                Color plate = new Color(pr, pg, pb, 1f);
                ApplyRuntimeColor(_plateRend, plate, 2.4f);
                if (_plateText != null) _plateText.color = plate;
            }

            // Hotter / brighter materials as fuse drains
            Color hatCol = Color.Lerp(itCol, new Color(1f, 0.92f, 0.35f, 1f), urgency);
            Color glowCol = Color.Lerp(itCol, new Color(1f, 0.75f, 0.15f, 0.9f), urgency);
            glowCol.a = 0.72f;
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

        static GameObject TrianglePlate()
        {
            var go = new GameObject("NameTag");
            var mesh = new Mesh { name = "SeatTriangle" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0.55f, 0f),
                new Vector3(-0.5f, -0.4f, 0.08f),
                new Vector3(0.5f, -0.4f, 0.08f),
                new Vector3(0f, 0.55f, 0f),
                new Vector3(0.5f, -0.4f, -0.08f),
                new Vector3(-0.5f, -0.4f, -0.08f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1,
                3, 4, 5,
                0, 1, 5,
                0, 5, 3,
                0, 4, 2,
                0, 3, 4,
                1, 2, 4,
                1, 4, 5
            };
            mesh.RecalculateNormals();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            return go;
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

        void OnGUI()
        {
            WarmStyle();
            if (_it == null || !_it.IsIt || !_it.IsAlive) return;
            var modes = TagModeController.Instance;
            if (modes != null && modes.Phase != MatchPhase.Playing && modes.Phase != MatchPhase.PostRound)
                return;
            var cam = Camera.main;
            if (cam == null) return;
            if (cam.transform.IsChildOf(transform)) return;

            Vector3 world = transform.position + Vector3.up * 2.35f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool behind = sp.z < 0.1f;
            if (behind)
            {
                sp.x = Screen.width - sp.x;
                sp.y = Screen.height - sp.y;
            }
            float gx = sp.x;
            float gy = Screen.height - sp.y;
            const float mark = 36f;
            float m = 20f;
            bool off = behind || gx < m || gy < m || gx > Screen.width - m || gy > Screen.height - m;
            gx = Mathf.Clamp(gx, m, Screen.width - m);
            gy = Mathf.Clamp(gy, m, Screen.height - m);
            float x = gx - mark * 0.5f;
            float y = gy - mark * 0.5f;
            VerbHudLayout.PushMarker(Screen.width, Screen.height, ref x, ref y, mark, mark + 18f, ParkArena.IsPocket, ParkArena.IsStack);

            var prev = GUI.color;
            GUI.color = new Color(0.05f, 0.07f, 0.1f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, mark, mark), Texture2D.whiteTexture);
            int pal = 0;
            float hud = 1f;
            if (GameSettings.Current != null)
            {
                pal = GameSettings.Current.PaletteOf(_shape);
                hud = Tag.Profiles.LocalProfiles.TextScale(_shape);
            }
            int crown = Tag.Profiles.LocalProfiles.SeatColor(_shape);
            if (crown < 0) crown = _shape;
            AccessibilityPalette.ItAgainst(pal, crown, out float ir, out float ig, out float ib);
            GUI.color = new Color(ir, ig, ib, 1f);
            float inset = 5f;
            GUI.DrawTexture(new Rect(x + inset, y + inset, mark - inset * 2f, mark - inset * 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (_itStyle == null) return;
            _itStyle.fontSize = (int)((Screen.height >= 1000 ? 14 : 12) * hud);
            _itStyle.normal.textColor = new Color(0.08f, 0.08f, 0.1f, 1f);
            GUI.Label(new Rect(x, y, mark, mark * 0.46f), AccessibilityPalette.ItGlyph, _itStyle);
            GUI.Label(new Rect(x, y + mark * 0.40f, mark, mark * 0.60f), Tag.Profiles.LocalProfiles.ItLabel(_shape), _itStyle);
            int swatch = Tag.Profiles.LocalProfiles.SeatColor(_shape);
            if (swatch < 0) swatch = _shape;
            AccessibilityPalette.Player(pal, swatch, out float pr, out float pg, out float pb);
            if (_plateName != null && _plateName.Length > 0)
            {
                _itStyle.normal.textColor = new Color(pr, pg, pb, 1f);
                GUI.Label(new Rect(x - 20f, y + mark, mark + 40f, 16f), _plateName, _itStyle);
            }
            if (off)
            {
                float rawX = sp.x;
                float rawY = Screen.height - sp.y;
                float aimX = rawX - (x + mark * 0.5f);
                float aimY = rawY - (y + mark * 0.5f);
                float ang = Mathf.Atan2(aimX, -aimY) * Mathf.Rad2Deg;
                var pivot = new Vector2(x + mark * 0.5f, y + mark + 8f);
                Matrix4x4 matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(ang, pivot);
                _itStyle.normal.textColor = new Color(pr, pg, pb, 1f);
                GUI.Label(new Rect(pivot.x - 12f, pivot.y - 10f, 24f, 20f), "▲", _itStyle);
                GUI.matrix = matrix;
            }
            GUI.color = prev;
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
