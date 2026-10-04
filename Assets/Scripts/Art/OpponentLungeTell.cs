using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Readable wind-up before the campus opponent lunges.
    /// Lead time is fixed. Score, fuse, and misses do not shorten it.
    /// Presentation only: no collider, so punch reach stays PunchTagTuning.reach.
    /// </summary>
    public class OpponentLungeTell : MonoBehaviour
    {
        public const float LeadSeconds = 0.45f;
        public const float MinGapBeyondReach = 0.35f;
        public const float MaxGapBeyondReach = 4.2f;
        public const float MaxAngleDeg = 22f;
        /// <summary>World radius of the ground ring. Cylinder primitive radius is 0.5, so scale xz is 2x this.</summary>
        public const float RingRadius = 1.15f;
        public const float RingHeight = 0.08f;
        public const string MarkerName = "LungeTell";

        /// <summary>Same lead at every skill value. There is no adaptive difficulty on this tell.</summary>
        public static float LeadSecondsFor(float skill01) => LeadSeconds;

        /// <summary>
        /// Approach band outside the tag fist. Inside reach the existing punch tells instead.
        /// </summary>
        public static bool InLungeWindow(float planarDist, float angleDeg, float reach)
        {
            return planarDist > reach + MinGapBeyondReach
                && planarDist < reach + MaxGapBeyondReach
                && angleDeg <= MaxAngleDeg;
        }

        public static Vector3 RingCenter(Vector3 feet) => feet + Vector3.up * RingHeight;

        Transform _root;
        Transform _ring;
        Transform _chevron;
        Vector3 _ringBase;
        Material _ringMat;
        Material _barMat;
        Light _light;
        bool _built;

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;

        /// <param name="charge01">0 when the tell starts, 1 on the frame the lunge is allowed.</param>
        public void Show(float charge01)
        {
            EnsureBuilt();
            if (_root == null) return;
            _root.gameObject.SetActive(true);

            float charge = Mathf.Clamp01(charge01);
            float hz = Mathf.Lerp(2.5f, 11f, charge);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * hz * Mathf.PI * 2f);
            float amp = Mathf.Lerp(0.08f, 0.28f, charge);
            if (_ring != null)
                _ring.localScale = _ringBase * (1f + amp * pulse);
            if (_chevron != null)
                _chevron.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(charge * (0.45f + 0.55f * pulse)));

            Color c = Color.Lerp(new Color(1f, 0.78f, 0.12f, 1f), new Color(1f, 0.98f, 0.85f, 1f), charge);
            float emit = Mathf.Lerp(1.6f, 6.5f, charge) * (0.7f + 0.6f * pulse);
            Paint(_ringMat, c, emit);
            Paint(_barMat, c, emit * 1.15f);
            if (_light != null)
            {
                _light.enabled = true;
                _light.color = c;
                _light.intensity = Mathf.Lerp(1.2f, 7f, charge) * (0.65f + 0.35f * pulse);
            }
        }

        public void Hide()
        {
            if (_root != null)
                _root.gameObject.SetActive(false);
            if (_light != null)
                _light.enabled = false;
        }

        void OnDisable() => Hide();

        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var rootGo = new GameObject(MarkerName);
            rootGo.transform.SetParent(transform, false);
            rootGo.transform.localPosition = Vector3.zero;
            rootGo.transform.localRotation = Quaternion.identity;
            _root = rootGo.transform;

            var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringGo.name = "LungeRing";
            ringGo.transform.SetParent(_root, false);
            ringGo.transform.localPosition = new Vector3(0f, RingHeight, 0f);
            _ringBase = new Vector3(RingRadius * 2f, 0.012f, RingRadius * 2f);
            ringGo.transform.localScale = _ringBase;
            StripCollider(ringGo);
            _ring = ringGo.transform;
            _ringMat = MakeMarkMat(new Color(1f, 0.78f, 0.12f, 1f));
            var ringRend = ringGo.GetComponent<Renderer>();
            if (ringRend != null) ringRend.sharedMaterial = _ringMat;

            var chev = new GameObject("LungeChevron");
            chev.transform.SetParent(_root, false);
            chev.transform.localPosition = new Vector3(0f, 1.42f, 0.35f);
            _chevron = chev.transform;
            _barMat = MakeMarkMat(new Color(1f, 0.86f, 0.28f, 1f));
            AddBar(_chevron, 32f, new Vector3(0.16f, 0f, 0.02f));
            AddBar(_chevron, -32f, new Vector3(-0.16f, 0f, 0.02f));

            var lightGo = new GameObject("LungeLight");
            lightGo.transform.SetParent(_root, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.35f, 0.2f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.range = 7f;
            _light.intensity = 1.2f;
            _light.enabled = false;

            rootGo.SetActive(false);
        }

        void AddBar(Transform parent, float yaw, Vector3 localPos)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "LungeBar";
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPos;
            bar.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            bar.transform.localScale = new Vector3(0.16f, 0.16f, 0.85f);
            StripCollider(bar);
            var rend = bar.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = _barMat;
        }

        static Material MakeMarkMat(Color c)
        {
            var mat = DummyPrimitiveFactory.MakeMat(c, 0.35f, 0f);
            Paint(mat, c, 2.2f);
            return mat;
        }

        static void Paint(Material mat, Color c, float emission)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * emission);
            }
        }

        static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
                DestroyImmediate(col);
        }
    }
}
