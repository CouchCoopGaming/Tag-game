using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Two bright ground rings and a short label on the tagged body.
    /// Either pawn can play it. Misses do not call PlayOn.
    /// Colliders are stripped so the flash cannot change punch reach.
    /// </summary>
    [DisallowMultipleComponent]
    public class TagLandFlash : MonoBehaviour
    {
        Transform _root;
        Transform _inner;
        Transform _outer;
        Transform _label;
        TextMesh _text;
        Material _innerMat;
        Material _outerMat;
        Light _light;
        float _innerScale;
        float _outerScale;
        float _life;
        float _labelLeft;
        bool _built;
        bool _playing;

        public bool IsPlaying => _playing;

        public static void PlayOn(Component host)
        {
            if (host == null) return;
            TagLandFlash tell = host.GetComponent<TagLandFlash>();
            if (tell == null) tell = host.gameObject.AddComponent<TagLandFlash>();
            tell.Play();
        }

        public void Play()
        {
            EnsureBuilt();
            _life = TagLandTell.FlashSeconds;
            _labelLeft = TagLandTell.LabelSeconds;
            _playing = true;
            if (_root != null) _root.gameObject.SetActive(true);
            if (_text != null)
                _text.text = TagLandTell.VictimLine + "\n" + TagLandTell.ItLine;
            Apply(1f);
        }

        void LateUpdate()
        {
            if (!_playing) return;
            float dt = Time.deltaTime;
            _life -= dt;
            _labelLeft -= dt;
            if (_life > 0f)
                Apply(Mathf.Clamp01(_life / TagLandTell.FlashSeconds));
            else
                HideRings();

            if (_label != null)
                _label.gameObject.SetActive(_labelLeft > 0f);
            PlaceLabel();

            if (_life <= 0f && _labelLeft <= 0f)
            {
                _playing = false;
                if (_root != null) _root.gameObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            _playing = false;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        void Apply(float fade)
        {
            float distMul = DistanceMul();
            float innerExpand = Mathf.Lerp(1.18f, 0.86f, fade);
            float outerExpand = Mathf.Lerp(1.30f, 0.90f, fade);
            if (_inner != null)
                _inner.localScale = new Vector3(_innerScale * innerExpand * distMul, 0.012f, _innerScale * innerExpand * distMul);
            if (_outer != null)
                _outer.localScale = new Vector3(_outerScale * outerExpand * distMul, 0.012f, _outerScale * outerExpand * distMul);

            Color hot = Color.Lerp(new Color(0.95f, 0.78f, 0.22f, 1f), new Color(1f, 0.98f, 0.92f, 1f), fade);
            float emit = Mathf.Lerp(1.4f, 7.2f, fade);
            Paint(_innerMat, Color.Lerp(hot, Color.white, 0.35f), emit);
            Paint(_outerMat, hot, emit * 0.85f);
            if (_light != null)
            {
                _light.enabled = true;
                _light.color = hot;
                _light.intensity = 7.5f * fade;
            }
            if (_inner != null) _inner.gameObject.SetActive(true);
            if (_outer != null) _outer.gameObject.SetActive(true);
            PlaceLabel();
        }

        void HideRings()
        {
            if (_inner != null) _inner.gameObject.SetActive(false);
            if (_outer != null) _outer.gameObject.SetActive(false);
            if (_light != null) _light.enabled = false;
        }

        float DistanceMul()
        {
            Camera cam = Camera.main;
            if (cam == null) return 1f;
            if (cam.transform.IsChildOf(transform)) return 0.8f;
            float d = Vector3.Distance(cam.transform.position, transform.position);
            return Mathf.Clamp(d / 16f, 1f, 2.2f);
        }

        void PlaceLabel()
        {
            if (_label == null) return;
            Vector3 toward = -transform.forward;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 toCam = cam.transform.position - transform.position;
                if (toCam.sqrMagnitude > 0.04f)
                    toward = toCam;
                Vector3 flat = toCam;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.001f)
                    _label.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            }
            _label.position = TagLandTell.LabelPoint(transform.position, toward);
        }

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var rootGo = new GameObject(TagLandTell.MarkerName);
            rootGo.transform.SetParent(transform, false);
            rootGo.transform.localPosition = Vector3.zero;
            rootGo.transform.localRotation = Quaternion.identity;
            _root = rootGo.transform;

            _innerScale = TagLandTell.RingScale(TagLandTell.InnerRadius);
            _outerScale = TagLandTell.RingScale(TagLandTell.OuterRadius);
            _inner = MakeRing("TagLandRingInner", TagLandTell.InnerHeight, _innerScale, new Color(1f, 0.98f, 0.92f, 1f), out _innerMat);
            _outer = MakeRing("TagLandRingOuter", TagLandTell.OuterHeight, _outerScale, new Color(0.95f, 0.78f, 0.22f, 1f), out _outerMat);

            var labelGo = new GameObject("TagLandLabel");
            labelGo.transform.SetParent(_root, false);
            _label = labelGo.transform;
            _text = labelGo.AddComponent<TextMesh>();
            _text.text = TagLandTell.VictimLine + "\n" + TagLandTell.ItLine;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.fontSize = 64;
            _text.characterSize = 0.10f;
            _text.fontStyle = FontStyle.Bold;
            _text.color = new Color(1f, 0.97f, 0.86f, 1f);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null) _text.font = font;

            var lightGo = new GameObject("TagLandLight");
            lightGo.transform.SetParent(_root, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.range = 18f;
            _light.intensity = 7.5f;
            _light.enabled = false;

            rootGo.SetActive(false);
        }

        Transform MakeRing(string name, float height, float xzScale, Color color, out Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            go.transform.localScale = new Vector3(xzScale, 0.012f, xzScale);
            go.layer = 2;
            StripCollider(go);
            mat = DummyPrimitiveFactory.MakeMat(color, 0.2f, 0f);
            Paint(mat, color, 6.5f);
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
            return go.transform;
        }

        static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col == null) return;
            col.enabled = false;
            DestroyImmediate(col);
        }

        static void Paint(Material mat, Color c, float emission)
        {
            if (mat == null) return;
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * emission);
            }
        }
    }
}
