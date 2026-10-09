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
        public bool KeepBar;
        public bool WinnerStroke;
        public bool LockColors;
        public Color BarColor;
        static readonly Color ChosenFill = new Color(0.12f, 0.40f, 0.78f, 1f);
        public RectTransform Sweep;
        public float SweepSpan;
        public bool Allow = true;
        public Action<int> Hovered;
        public Action<int> Pressed;

        bool _hot;
        bool _chosen;
        RectTransform _chipRt;
        float _punch;
        float _confirm;
        float _select;
        float _vel;
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

        public void PopSelect()
        {
            if (!Allow || MenuVideo.ReduceMotion) return;
            _select = 1f;
        }

        public void SetChosen(bool chosen)
        {
            SetChosen(chosen, false);
        }

        public void SetChosen(bool chosen, bool chip)
        {
            _chosen = chosen;
            EnsureCheck(chip);
            if (_chipRt != null) _chipRt.gameObject.SetActive(chosen);
            if (!_hot) PaintRest();
        }

        public void SetHot(bool hot)
        {
            if (hot == _hot)
            {
                if (hot) PaintHot();
                else PaintRest();
                return;
            }
            _hot = hot;
            if (hot && !MenuVideo.ReduceMotion) _punch = 1f;
            if (hot) PaintHot();
            else PaintRest();
        }

        /// <summary>
        /// The other row in a bind swap. Gold stays on the focused row only.
        /// </summary>
        public void MarkPair(bool on)
        {
            if (_hot || Stroke == null) return;
            Stroke.color = on ? MenuTheme.Cream : MenuTheme.Stroke;
        }

        void PaintHot()
        {
            if (LockColors)
            {
                if (Plate != null) Plate.color = _base;
                if (Stroke != null) Stroke.color = MenuTheme.Gold;
                if (Label != null) Label.color = MenuTheme.Cream;
                if (Detail != null) Detail.color = MenuTheme.Mute;
                if (Bar != null) Bar.color = KeepBar ? BarColor : new Color(1f, 1f, 1f, 0.35f);
                return;
            }
            if (Plate != null) Plate.color = _hotColor;
            if (Stroke != null) Stroke.color = MenuTheme.Gold;
            if (Label != null) Label.color = MenuTheme.Ink;
            if (Detail != null) Detail.color = MenuTheme.Ink;
            if (Bar != null) Bar.color = MenuTheme.Ink;
        }

        void PaintRest()
        {
            bool gold = WinnerStroke;
            if (LockColors)
            {
                if (Plate != null) Plate.color = _base;
                if (Stroke != null) Stroke.color = gold ? MenuTheme.Gold : MenuTheme.Stroke;
                if (Label != null) Label.color = MenuTheme.Cream;
                if (Detail != null) Detail.color = MenuTheme.Mute;
                if (Bar != null) Bar.color = KeepBar ? BarColor : new Color(1f, 1f, 1f, 0.35f);
                return;
            }
            if (Plate != null) Plate.color = _chosen ? ChosenFill : _base;
            if (Stroke != null) Stroke.color = gold ? MenuTheme.Gold : MenuTheme.Stroke;
            if (Label != null) Label.color = MenuTheme.Cream;
            if (Detail != null) Detail.color = MenuTheme.Mute;
            if (Bar != null) Bar.color = KeepBar ? BarColor : new Color(1f, 1f, 1f, 0.35f);
        }

        void EnsureCheck(bool chip)
        {
            if (_chipRt != null) return;
            float side = chip ? 44f : 36f;
            RectTransform rt = MenuWidgets.Place(transform, "Check", 0f, 0f, side, side);
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-14f, 0f);
            rt.sizeDelta = new Vector2(side, side);
            Color ink = chip ? MenuTheme.Ink : MenuTheme.Cream;
            if (chip)
            {
                Image plate = rt.gameObject.AddComponent<Image>();
                MenuArt.Plate(plate, MenuTheme.Gold, true);
                plate.raycastTarget = false;
            }
            // Two bars that share a corner. No font character.
            CheckStroke(rt, side * 0.20f, side * 0.46f, side * 0.42f, side * 0.70f, side * 0.16f, ink);
            CheckStroke(rt, side * 0.42f, side * 0.70f, side * 0.82f, side * 0.24f, side * 0.16f, ink);
            _chipRt = rt;
            _chipRt.gameObject.SetActive(false);
        }

        static void CheckStroke(RectTransform parent, float x0, float y0, float x1, float y1, float thick, Color ink)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 1f) len = 1f;
            float degrees = Mathf.Atan2(-dy, dx) * Mathf.Rad2Deg;
            RectTransform rt = MenuWidgets.Place(parent, "Mark", x0, y0, len, thick);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x0, -y0);
            rt.sizeDelta = new Vector2(len, thick);
            rt.localRotation = Quaternion.Euler(0f, 0f, degrees);
            Image bar = rt.gameObject.AddComponent<Image>();
            bar.color = ink;
            bar.raycastTarget = false;
        }

        /// <summary>A thicker gold edge on the focused row, so it reads from the couch.</summary>
        public void CouchEdge()
        {
            if (Stroke == null) return;
            RectTransform rt = transform as RectTransform;
            if (rt == null) return;
            float pad = _hot ? 16f : 5f;
            RectTransform stroke = Stroke.rectTransform;
            stroke.anchoredPosition = new Vector2(-pad, pad);
            stroke.sizeDelta = new Vector2(rt.sizeDelta.x + pad * 2f, rt.sizeDelta.y + pad * 2f);
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
            float target = _hot ? MenuPolish.HotScale : 1f;
            RectTransform rt = transform as RectTransform;
            if (MenuVideo.ReduceMotion)
            {
                _vel = 0f;
                transform.localScale = new Vector3(target, target, 1f);
                if (rt != null && _restSet) rt.anchoredPosition = _rest;
                HideSweep();
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
            if (_select > 0f)
            {
                _select -= Time.unscaledDeltaTime / 0.2f;
                if (_select < 0f) _select = 0f;
                float u = 1f - _select;
                float e = u * u * (3f - 2f * u);
                float rest = _hot ? MenuPolish.HotScale : 1f;
                float peak = 1.08f / rest;
                float kick = e < 0.35f
                    ? 1f + (peak - 1f) * (e / 0.35f)
                    : peak + (1f - peak) * ((e - 0.35f) / 0.65f);
                target *= kick;
            }
            float next = transform.localScale.x;
            MenuPolish.Spring(ref next, ref _vel, target, Time.unscaledDeltaTime);
            transform.localScale = new Vector3(next, next, 1f);
            if (rt != null && _restSet)
            {
                float bob = _hot ? Mathf.Sin(Time.unscaledTime * 3.1f) * 5f : 0f;
                Vector2 p = _rest;
                p.y += bob;
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, p, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            }
            SweepTick();
        }

        bool _sweepOn;

        void HideSweep()
        {
            if (Sweep == null || !_sweepOn) return;
            _sweepOn = false;
            Sweep.gameObject.SetActive(false);
        }

        void SweepTick()
        {
            if (Sweep == null) return;
            if (!_hot)
            {
                HideSweep();
                return;
            }
            if (!_sweepOn)
            {
                _sweepOn = true;
                Sweep.gameObject.SetActive(true);
            }
            float u = Mathf.Repeat(Time.unscaledTime * 0.45f, 1f);
            float span = SweepSpan;
            Vector2 p = Sweep.anchoredPosition;
            p.x = Mathf.Lerp(-span, span, u);
            Sweep.anchoredPosition = p;
        }
    }
}
