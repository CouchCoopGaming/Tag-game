using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Small spark when a tag-back punch is refused. No collider, so reach stays put.
    /// The thunk is TagSfx.TagBackThunk. This does not transfer It and does not stagger.
    /// </summary>
    public class TagBackBlockedTell : MonoBehaviour
    {
        const float Life = 0.18f;
        const int Sparks = 5;
        const int Pool = 4;

        static readonly TagBackBlockedTell[] Slots = new TagBackBlockedTell[Pool];
        static int _cursor;

        Transform[] _bits;
        Vector3[] _vel;
        Material _mat;
        float _age;
        bool _playing;

        public static void ResetStatics()
        {
            for (int i = 0; i < Slots.Length; i++) Slots[i] = null;
            _cursor = 0;
        }

        public static void PlayAt(Vector3 point)
        {
            int i = _cursor;
            _cursor = (_cursor + 1) % Pool;
            TagBackBlockedTell tell = Slots[i];
            if (tell == null)
            {
                var go = new GameObject("TagBackSpark");
                tell = go.AddComponent<TagBackBlockedTell>();
                Slots[i] = tell;
                tell.Begin();
            }
            tell.Restart(point);
        }

        void Restart(Vector3 point)
        {
            transform.position = point;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            _age = 0f;
            _playing = true;
            if (_bits == null) return;
            for (int i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] == null) continue;
                _bits[i].localPosition = Vector3.zero;
                Vector3 dir = Random.insideUnitSphere;
                if (dir.sqrMagnitude < 0.01f) dir = Vector3.up;
                dir.Normalize();
                dir.y = Mathf.Abs(dir.y) + 0.25f;
                _vel[i] = dir * Random.Range(1.6f, 3.4f);
            }
        }

        void Begin()
        {
            _bits = new Transform[Sparks];
            _vel = new Vector3[Sparks];
            _mat = DummyPrimitiveFactory.MakeMat(new Color(0.62f, 1f, 0.94f, 1f), 0.1f, 0f);
            Paint(_mat, new Color(0.78f, 1f, 0.96f, 1f), 6.5f);
            for (int i = 0; i < Sparks; i++)
            {
                var bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bit.name = "TagBackBit";
                bit.transform.SetParent(transform, false);
                bit.transform.localScale = Vector3.one * (i == 0 ? 0.16f : 0.07f);
                bit.layer = 2;
                var col = bit.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                    DestroyImmediate(col);
                }
                var rend = bit.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = _mat;
                Vector3 dir = Random.insideUnitSphere;
                if (dir.sqrMagnitude < 0.01f) dir = Vector3.up;
                dir.Normalize();
                dir.y = Mathf.Abs(dir.y) + 0.25f;
                _vel[i] = dir * Random.Range(1.6f, 3.4f);
                _bits[i] = bit.transform;
            }
        }

        void Update()
        {
            if (!_playing || _bits == null) return;
            float dt = Time.deltaTime;
            _age += dt;
            float u = Mathf.Clamp01(_age / Life);
            float fade = 1f - u;
            for (int i = 0; i < _bits.Length; i++)
            {
                if (_bits[i] == null) continue;
                _vel[i] += Vector3.down * 6f * dt;
                _bits[i].position += _vel[i] * dt;
                float s = (i == 0 ? 0.16f : 0.07f) * Mathf.Lerp(1f, 0.2f, u);
                _bits[i].localScale = Vector3.one * s;
            }
            if (_mat != null)
            {
                Color c = new Color(0.78f, 1f, 0.96f, fade);
                Paint(_mat, c, 6.5f * fade);
            }
            if (_age >= Life)
                _playing = false;
        }

        void OnDestroy()
        {
            for (int i = 0; i < Slots.Length; i++)
                if (Slots[i] == this) Slots[i] = null;
            if (_mat != null)
                Destroy(_mat);
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
                mat.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * emission);
            }
        }
    }
}
