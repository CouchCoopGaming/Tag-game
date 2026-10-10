using UnityEngine;
using UnityEngine.UI;

namespace Tag.Ui.Menu
{
    /// <summary>
    /// Short fade-in for the results rows, then the actions.
    /// Reduce motion and capture snap to the finished board.
    /// </summary>
    public static class MenuReveal
    {
        const int Cap = 4;
        const float Stagger = 0.12f;
        const float Fade = 0.28f;
        const float CardDelay = 0.85f;
        const float Slide = 72f;

        static readonly CanvasGroup[] _rows = new CanvasGroup[Cap];
        static readonly CanvasGroup[] _actions = new CanvasGroup[Cap];
        static readonly RectTransform[] _rowRt = new RectTransform[Cap];
        static readonly float[] _rowRest = new float[Cap];
        static int _rowCount;
        static int _actionCount;
        static float _t0;
        static bool _live;

        public static void Clear()
        {
            _live = false;
            _rowCount = 0;
            _actionCount = 0;
            for (int i = 0; i < Cap; i++)
            {
                _rows[i] = null;
                _actions[i] = null;
                _rowRt[i] = null;
            }
        }

        public static void Row(RectTransform rt)
        {
            if (rt == null || _rowCount >= Cap) return;
            _rows[_rowCount] = Group(rt);
            _rowRt[_rowCount] = rt;
            _rowRest[_rowCount] = rt.anchoredPosition.y;
            _rowCount++;
        }

        public static void Action(RectTransform rt)
        {
            if (rt == null || _actionCount >= Cap) return;
            _actions[_actionCount] = Group(rt);
            _actionCount++;
        }

        public static void Begin()
        {
            _t0 = Time.unscaledTime;
            _live = true;
            if (MenuVideo.ReduceMotion || MenuCapture.Running)
                Snap();
            else
            {
                for (int i = 0; i < _rowCount; i++)
                {
                    Set(_rows[i], 0f);
                    SlideRow(i, 0f);
                }
                for (int i = 0; i < _actionCount; i++) Set(_actions[i], 0f);
            }
        }

        public static void Tick()
        {
            if (!_live) return;
            if (MenuVideo.ReduceMotion || MenuCapture.Running)
            {
                Snap();
                return;
            }
            float elapsed = Time.unscaledTime - _t0;
            bool done = true;
            for (int i = 0; i < _rowCount; i++)
            {
                float u = (elapsed - CardDelay - i * Stagger) / Fade;
                SlideRow(i, u);
                if (!Apply(_rows[i], u)) done = false;
            }
            float buttonAt = CardDelay + _rowCount * Stagger;
            for (int i = 0; i < _actionCount; i++)
            {
                float u = (elapsed - buttonAt) / Fade;
                if (!Apply(_actions[i], u)) done = false;
            }
            if (done) _live = false;
        }

        static CanvasGroup Group(RectTransform rt)
        {
            CanvasGroup group = rt.GetComponent<CanvasGroup>();
            if (group == null) group = rt.gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        static void Snap()
        {
            for (int i = 0; i < _rowCount; i++)
            {
                Set(_rows[i], 1f);
                SlideRow(i, 1f);
            }
            for (int i = 0; i < _actionCount; i++) Set(_actions[i], 1f);
            _live = false;
        }

        static void SlideRow(int i, float u)
        {
            RectTransform rt = _rowRt[i];
            if (rt == null) return;
            if (u < 0f) u = 0f;
            if (u > 1f) u = 1f;
            float e = u * u * (3f - 2f * u);
            Vector2 p = rt.anchoredPosition;
            p.y = _rowRest[i] - Slide * (1f - e);
            rt.anchoredPosition = p;
        }

        static bool Apply(CanvasGroup group, float u)
        {
            if (u < 0f) u = 0f;
            bool finished = u >= 1f;
            if (u > 1f) u = 1f;
            Set(group, u);
            return finished;
        }

        static void Set(CanvasGroup group, float u)
        {
            if (group == null) return;
            group.alpha = u;
            bool on = u > 0.9f;
            group.interactable = on;
            group.blocksRaycasts = on;
        }
    }
}
