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
        public Text Label;
        public Text Detail;
        public Image Bar;
        public bool Allow = true;
        public Action<int> Hovered;
        public Action<int> Pressed;

        bool _hot;
        float _punch;
        Color _base;
        Color _hotColor;

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
        }

        public void SetHot(bool hot)
        {
            if (hot == _hot) return;
            _hot = hot;
            if (hot && !MenuVideo.ReduceMotion) _punch = 1f;
            if (Plate != null) Plate.color = hot ? _hotColor : _base;
            if (Bar != null) Bar.color = hot ? MenuTheme.Gold : new Color(1f, 1f, 1f, 0.16f);
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
            float target = _hot ? 1.06f : 1f;
            if (MenuVideo.ReduceMotion)
            {
                transform.localScale = new Vector3(target, target, 1f);
                return;
            }
            if (_punch > 0f)
            {
                _punch -= Time.unscaledDeltaTime / 0.2f;
                if (_punch < 0f) _punch = 0f;
                float kick = 1f + 0.06f * Mathf.Sin((1f - _punch) * Mathf.PI);
                target *= kick;
            }
            Vector3 scale = transform.localScale;
            float next = Mathf.Lerp(scale.x, target, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(next, next, 1f);
        }
    }
}
