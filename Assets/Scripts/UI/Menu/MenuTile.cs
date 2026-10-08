using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    public sealed class MenuTile : MonoBehaviour, IPointerEnterHandler
    {
        public int Index;
        public Image Plate;
        public Image Stroke;
        public Text Label;
        public Text Detail;
        public Image Bar;
        public bool Allow = true;
        public Action<int> Hovered;
        public Action<int> Pressed;

        bool _hot;
        float _punch;
        float _confirm;
        Color _base;
        Color _hotColor;
        Vector2 _rest;
        bool _restSet;

        public void Setup(int index, string label, string detail, Color plate, bool allow, Action<int> hover, Action<int> press)
        {
            Index = index;
            Allow = allow;
            _base = allow ? plate : MenuTheme.Off;
            _hotColor = MenuTheme.PanelHot;
            if (Plate != null) Plate.color = _base;
            if (Label != null) Label.text = label ?? "";
            if (Detail != null) Detail.text = detail ?? "";
            Hovered = hover;
            Pressed = press;
            RectTransform rt = transform as RectTransform;
            if (rt != null)
            {
                _rest = rt.anchoredPosition;
                _restSet = true;
            }
        }

        public void Tint(Color color)
        {
            _base = color;
            if (!_hot && Plate != null) Plate.color = color;
        }

        public void PunchIn()
        {
            if (!Allow || MenuVideo.ReduceMotion) return;
            _confirm = 1f;
        }

        public void SetHot(bool hot)
        {
            if (hot == _hot) return;
            _hot = hot;
            if (hot && !MenuVideo.ReduceMotion) _punch = 1f;
            if (Plate != null) Plate.color = hot ? _hotColor : _base;
            if (Stroke != null) Stroke.color = hot ? MenuTheme.Gold : MenuTheme.Stroke;
            if (Bar != null) Bar.color = hot ? MenuTheme.Gold : new Color(1f, 1f, 1f, 0.35f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Allow) return;
            if (Hovered != null) Hovered(Index);
        }

        public void Click()
        {
            if (!Allow) return;
            if (Pressed != null) Pressed(Index);
        }

        void Update()
        {
            float target = _hot ? 1.05f : 1f;
            RectTransform rt = transform as RectTransform;
            if (MenuVideo.ReduceMotion)
            {
                transform.localScale = new Vector3(target, target, 1f);
                if (rt != null && _restSet) rt.anchoredPosition = _rest;
                return;
            }
            if (_punch > 0f)
            {
                _punch -= Time.unscaledDeltaTime / 0.2f;
                if (_punch < 0f) _punch = 0f;
                float kick = 1f + 0.05f * Mathf.Sin((1f - _punch) * Mathf.PI);
                target *= kick;
            }
            if (_confirm > 0f)
            {
                _confirm -= Time.unscaledDeltaTime / 0.16f;
                if (_confirm < 0f) _confirm = 0f;
                float t = 1f - _confirm;
                float kick = t < 0.35f
                    ? Mathf.Lerp(1f, 0.88f, t / 0.35f)
                    : Mathf.Lerp(1.1f, 1f, (t - 0.35f) / 0.65f);
                target *= kick;
            }
            Vector3 scale = transform.localScale;
            float next = Mathf.Lerp(scale.x, target, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(next, next, 1f);
            if (rt != null && _restSet)
            {
                float bob = _hot ? Mathf.Sin(Time.unscaledTime * 3.1f) * 5f : 0f;
                Vector2 p = _rest;
                p.y += bob;
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, p, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            }
        }
    }
}
