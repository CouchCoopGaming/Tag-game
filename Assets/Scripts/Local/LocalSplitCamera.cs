using System.Collections.Generic;
using Tag.Gameplay;
using UnityEngine;

namespace Tag.Local
{
    /// <summary>Assigns camera rects for 2–4 local players.</summary>
    public class LocalSplitCamera : MonoBehaviour
    {
        void Start() => Apply();
        void LateUpdate()
        {
            // Re-apply if players spawn late
        }

        [ContextMenu("Apply Split")]
        public void Apply()
        {
            var cams = new List<Camera>();
            foreach (var it in FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (!it.gameObject.activeInHierarchy) continue;
                var cam = it.GetComponentInChildren<Camera>();
                if (cam != null) cams.Add(cam);
            }
            int n = cams.Count;
            if (n == 0) return;

            // Disable extra audio listeners
            bool listenerKept = false;
            for (int i = 0; i < n; i++)
            {
                var al = cams[i].GetComponent<AudioListener>();
                if (al != null)
                {
                    al.enabled = !listenerKept;
                    listenerKept = true;
                }
                cams[i].rect = RectFor(i, n);
            }
            Debug.Log($"[LocalSplitCamera] Split {n} cameras");
        }

        static Rect RectFor(int i, int n)
        {
            if (n == 1) return new Rect(0, 0, 1, 1);
            if (n == 2) return i == 0 ? new Rect(0, 0.5f, 1, 0.5f) : new Rect(0, 0, 1, 0.5f);
            // 3 or 4: quad
            float x = (i % 2) * 0.5f;
            float y = (i < 2) ? 0.5f : 0f;
            if (n == 3 && i == 2) return new Rect(0.25f, 0f, 0.5f, 0.5f);
            return new Rect(x, y, 0.5f, 0.5f);
        }
    }
}
