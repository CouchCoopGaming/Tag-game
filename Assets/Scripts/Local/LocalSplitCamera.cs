using Tag.Couch;
using Tag.Gameplay;
using Tag.Settings;
using UnityEngine;

namespace Tag.Local
{
    /// <summary>
    /// One chase camera per human. 1 is full screen, 2 is a vertical or horizontal
    /// split, 3 and 4 are quadrants. Three humans leave the last quadrant for the score.
    /// One listener, on P1 or at the average of the humans. The voice cap stays 16.
    /// </summary>
    public class LocalSplitCamera : MonoBehaviour
    {
        Camera[] _cams = new Camera[CouchPlay.Max];
        Transform[] _bodies = new Transform[CouchPlay.Max];
        AudioListener _average;
        int _count;

        void Start() => Apply();

        void LateUpdate()
        {
            if (_average == null || _count <= 0) return;
            if (GameSettings.Current == null || GameSettings.Current.Listener != GameSettings.ListenAverage) return;
            float x = 0f;
            float y = 0f;
            float z = 0f;
            int n = 0;
            for (int i = 0; i < _count; i++)
            {
                Transform body = _bodies[i];
                if (body == null) continue;
                Vector3 p = body.position;
                x += p.x;
                y += p.y;
                z += p.z;
                n++;
            }
            if (n <= 0) return;
            _average.transform.position = new Vector3(x / n, y / n + 1.6f, z / n);
        }

        [ContextMenu("Apply Split")]
        public void Apply()
        {
            _count = 0;
            var its = FindObjectsByType<ItController>(FindObjectsSortMode.None);
            int humans = CouchPlay.Humans;
            if (humans < 1) humans = 1;
            for (int s = 0; s < CouchPlay.Max && _count < humans; s++)
            {
                if (CouchPlay.Humans >= 2 && !CouchPlay.HumanAt(s)) continue;
                string name = CouchPlay.Name(s);
                for (int i = 0; i < its.Length; i++)
                {
                    ItController it = its[i];
                    if (it == null || !it.gameObject.activeInHierarchy) continue;
                    if (CouchPlay.Humans >= 2 && it.PlayerId != name) continue;
                    // The Play scene's Player keeps a disabled CameraPivot camera, and clones copy it.
                    // GetComponentInChildren returned that one first, so no seat matched, no rect was
                    // set, and every pane stayed full screen with P1's on top.
                    Camera cam = null;
                    foreach (Camera c in it.GetComponentsInChildren<Camera>())
                        if (c.enabled) { cam = c; break; }
                    if (cam == null) continue;
                    _cams[_count] = cam;
                    _bodies[_count] = it.transform;
                    _count++;
                    break;
                }
                if (CouchPlay.Humans < 2 && _count > 0) break;
            }

            if (_count == 0) return;
            int shown = CouchPlay.Humans >= 2 ? CouchPlay.Humans : 1;
            if (shown > _count) shown = _count;
            int split = GameSettings.Current != null ? GameSettings.Current.SplitAxis : GameSettings.SplitVertical;
            bool average = GameSettings.Current != null && GameSettings.Current.Listener == GameSettings.ListenAverage && shown > 1;
            EnsureAverage(average);

            for (int i = 0; i < _count; i++)
            {
                Camera cam = _cams[i];
                if (cam == null) continue;
                if (i < shown)
                {
                    CouchPlay.Norm(i, shown, split, out float x, out float y, out float w, out float h);
                    cam.rect = new Rect(x, y, w, h);
                    cam.enabled = true;
                }
                else
                    cam.enabled = false;
                AudioListener listener = cam.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = !average && i == 0 && i < shown;
            }
            if (_average != null) _average.enabled = average;
        }

        void EnsureAverage(bool on)
        {
            if (!on)
            {
                if (_average != null) _average.enabled = false;
                return;
            }
            if (_average != null) return;
            var go = new GameObject("CouchListener");
            go.transform.SetParent(transform, false);
            _average = go.AddComponent<AudioListener>();
        }
    }
}
