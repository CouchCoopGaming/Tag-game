using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Three comic bars cross the screen when a secondary page opens.
    /// The pass is shorter than 0.4 s. Reduce motion skips it.
    /// </summary>
    public sealed class MenuWipe : MonoBehaviour
    {
        const int Bars = 3;

        RectTransform[] _bar;
        float _t;

        public static void Clear(RectTransform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child != null && child.name == "ComicWipe")
                    { if (Application.isPlaying) Object.Destroy(child.gameObject); else Object.DestroyImmediate(child.gameObject); }
            }
        }

        public static void Play(RectTransform root)
        {
            if (root == null) return;
            if (MenuVideo.ReduceMotion || MenuCapture.Running) return;
            Clear(root);
            var go = new GameObject("ComicWipe", typeof(RectTransform));
            go.transform.SetParent(root, false);
            int body = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).name == "Body")
                {
                    body = i;
                    break;
                }
            }
            go.transform.SetSiblingIndex(body);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            MenuWipe wipe = go.AddComponent<MenuWipe>();
            wipe.Build(rt);
        }

        void Build(RectTransform root)
        {
            _bar = new RectTransform[Bars];
            Color[] ink =
            {
                MenuTheme.Gold,
                MenuTheme.Cream,
                MenuTheme.PanelHot
            };
            for (int i = 0; i < Bars; i++)
            {
                RectTransform rt = MenuWidgets.Place(root, "WipeBar", -420f, 240f + 160f * i, 140f, 220f);
                Image image = rt.gameObject.AddComponent<Image>();
                MenuArt.Plate(image, new Color(ink[i].r, ink[i].g, ink[i].b, 0.92f), false);
                image.type = Image.Type.Simple;
                image.raycastTarget = false;
                rt.localRotation = Quaternion.Euler(0f, 0f, -14f);
                _bar[i] = rt;
            }
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float u = _t / MenuSheet.WipeSeconds;
            if (u >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            for (int i = 0; i < Bars; i++)
            {
                if (_bar[i] == null) continue;
                float start = -520f - i * 80f;
                float end = 2100f + i * 40f;
                float lag = i * 0.08f;
                float t = (u - lag) / (1f - lag);
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
                Vector2 p = _bar[i].anchoredPosition;
                p.x = Mathf.Lerp(start, end, t);
                _bar[i].anchoredPosition = p;
            }
        }
    }
}
