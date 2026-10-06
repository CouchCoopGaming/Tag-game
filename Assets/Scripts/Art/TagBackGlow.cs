using System.Collections.Generic;
using Tag.Gameplay;
using Tag.Settings;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Soft color pulse on the pawn who just lost It. Fades across the tag-back
    /// window so both players can see who is safe. The shell has no collider.
    /// </summary>
    [DisallowMultipleComponent]
    public class TagBackGlow : MonoBehaviour
    {
        // Aqua-cyan. Sky cyan (0.45, 0.95, 1) sat 20° off hopscotch #6AA8D6 at contrast 1.94.
        static readonly Color Safe = new Color(0.20f, 1.00f, 0.92f, 1f);

        ItController _it;
        TagRole _role;
        Transform _shell;
        Renderer _shellRend;
        Material _shellMat;
        Light _light;
        readonly List<Renderer> _pulsed = new List<Renderer>();
        MaterialPropertyBlock _block;
        Renderer[] _bodies = System.Array.Empty<Renderer>();
        int _bodyKids = -1;
        bool _on;

        void Awake()
        {
            _it = GetComponent<ItController>();
            _role = GetComponent<TagRole>();
            _block = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            float glow = 0f;
            float time = Time.time;
            if (_it != null) glow = _it.TagBackGlow01(time);
            if (_role != null) glow = Mathf.Max(glow, _role.TagBackGlow01(time));
            if (glow > 0.001f && GameSettings.Current != null && GameSettings.Current.AnyReduceFlash())
                glow = GameSettings.Current.GlowVisual(glow);
            if (glow <= 0.001f)
            {
                if (_on) End();
                return;
            }
            int kids = transform.childCount;
            if (kids != _bodyKids) RefreshBodies(kids);
            EnsureShell();
            Apply(glow);
            _on = true;
        }

        void RefreshBodies(int kids)
        {
            RememberBodies(kids);
        }

        void RememberBodies(int kids)
        {
            _bodyKids = kids;
            _bodies = GetComponentsInChildren<Renderer>(true);
        }

        void OnDisable()
        {
            if (_on) End();
        }

        void OnDestroy()
        {
            if (_shellMat != null)
                Destroy(_shellMat);
        }

        int SeatIndex()
        {
            string id = _it != null ? _it.PlayerId : null;
            if (string.IsNullOrEmpty(id)) return 0;
            char c = id[id.Length - 1];
            if (c < '1' || c > '4') return 0;
            return c - '1';
        }

        void Apply(float glow)
        {
            ClearPulsed();
            Color ink = Safe;
            if (GameSettings.Current != null)
            {
                int pal = GameSettings.Current.PaletteOf(SeatIndex());
                if (pal != 0)
                {
                    AccessibilityPalette.Glow(pal, out float gr, out float gg, out float gb);
                    ink = new Color(gr, gg, gb, 1f);
                }
            }
            float mix = Mathf.Clamp01(glow) * 0.72f;
            Renderer[] all = _bodies;
            for (int i = 0; i < all.Length; i++)
            {
                Renderer r = all[i];
                if (!BodyRenderer(r)) continue;
                Material mat = r.sharedMaterial;
                if (mat == null) continue;
                bool hasBase = mat.HasProperty("_BaseColor");
                bool hasColor = mat.HasProperty("_Color");
                if (!hasBase && !hasColor) continue;
                Color baseC = hasBase ? mat.GetColor("_BaseColor") : mat.color;
                Color c = Color.Lerp(baseC, ink, mix);
                MaterialPropertyBlock block = _block;
                if (block == null) continue;
                r.GetPropertyBlock(block);
                if (hasBase) block.SetColor("_BaseColor", c);
                if (hasColor) block.SetColor("_Color", c);
                if (mat.HasProperty("_EmissionColor"))
                    block.SetColor("_EmissionColor", ink * (0.35f + 2.4f * glow));
                r.SetPropertyBlock(block);
                _pulsed.Add(r);
            }

            if (_shell != null)
            {
                float pulse = 1f + 0.08f * glow;
                _shell.localScale = new Vector3(0.96f * pulse, 0.82f, 0.96f * pulse);
                _shell.gameObject.SetActive(true);
                Color shell = ink;
                shell.a = 0.18f + 0.42f * glow;
                Paint(_shellMat, shell, 1.2f + 4.5f * glow);
            }
            if (_light != null)
            {
                _light.enabled = true;
                _light.color = ink;
                _light.intensity = 1.35f * glow;
                _light.range = 3.2f;
            }
        }

        void End()
        {
            ClearPulsed();
            if (_shell != null) _shell.gameObject.SetActive(false);
            if (_light != null) _light.enabled = false;
            _on = false;
        }

        void ClearPulsed()
        {
            for (int i = 0; i < _pulsed.Count; i++)
            {
                if (_pulsed[i] != null)
                    _pulsed[i].SetPropertyBlock(null);
            }
            _pulsed.Clear();
        }

        bool BodyRenderer(Renderer r)
        {
            if (r == null || r == _shellRend) return false;
            if (!r.gameObject.activeInHierarchy || !r.enabled) return false;
            if (r is TrailRenderer || r is ParticleSystemRenderer || r is LineRenderer) return false;
            string n = r.gameObject.name;
            if (n.StartsWith("TagBack")) return false;
            if (n.StartsWith("ItHat") || n.StartsWith("ItHalo") || n.StartsWith("ItBeacon")) return false;
            if (n.StartsWith("AirDash") || n.StartsWith("TagLand") || n.StartsWith("HitConfirm")) return false;
            return true;
        }

        void EnsureShell()
        {
            if (_shell != null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "TagBackShell";
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.02f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(0.96f, 0.82f, 0.96f);
            go.layer = 2;
            StripShellCollider(go);
            _shellRend = ShellRenderer(go);
            _shellMat = DummyPrimitiveFactory.MakeMat(Safe, 0.15f, 0f);
            Paint(_shellMat, Safe, 3.5f);
            if (_shellMat.HasProperty("_Surface")) _shellMat.SetFloat("_Surface", 1f);
            if (_shellMat.HasProperty("_ZWrite")) _shellMat.SetFloat("_ZWrite", 0f);
            if (_shellMat.HasProperty("_SrcBlend")) _shellMat.SetFloat("_SrcBlend", 5f);
            if (_shellMat.HasProperty("_DstBlend")) _shellMat.SetFloat("_DstBlend", 10f);
            _shellMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            _shellMat.renderQueue = 3000;
            if (_shellRend != null) _shellRend.sharedMaterial = _shellMat;
            _shell = go.transform;

            var lightGo = new GameObject("TagBackLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.shadows = LightShadows.None;
            _light.color = Safe;
            _light.range = 3.2f;
            _light.intensity = 0f;
            _light.enabled = false;
        }

        static void StripShellCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
                DestroyImmediate(col);
            }
        }

        static Renderer ShellRenderer(GameObject go)
        {
            return go.GetComponent<Renderer>();
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
