using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Cheap floating zone name markers (TextMesh + yaw billboard) for mega-park orientation.
    /// Centers match CutArenaBootstrap pads / PgkLandmarkPlacer landmark slots (graybox m).
    /// </summary>
    [DefaultExecutionOrder(70)]
    public class ZoneNameMarkers : MonoBehaviour
    {
        const string FolderName = "_ZoneNameMarkers";
        // Graybox Y; world = * WorldScale (~150). Clears towers (~5 graybox tops).
        const float LabelHeight = 15f;

        static readonly (string label, float cx, float cz)[] Zones =
        {
            ("CRASH",  36f, 27f),
            ("PIRATE", 14f, 12f),
            ("ARMY",   58f, 12f),
            ("ASTRO",  14f, 42f),
            ("KNIGHT", 58f, 42f),
            ("TRON",   36f,  8f),
            ("NINJA",  36f, 46f),
        };

        Transform[] _labels;
        Camera _cam;

        void Start() => Place();

        [ContextMenu("Place Zone Name Markers")]
        public void Place()
        {
            var park = transform.Find("PARK");
            if (park == null)
            {
                Debug.LogWarning("[ZoneNameMarkers] No PARK root.");
                return;
            }

            var existing = park.Find(FolderName);
            if (existing != null)
                Destroy(existing.gameObject);

            var folder = new GameObject(FolderName).transform;
            folder.SetParent(park, false);
            folder.localPosition = Vector3.zero;
            folder.localRotation = Quaternion.identity;
            folder.localScale = Vector3.one;

            _labels = new Transform[Zones.Length];
            for (int i = 0; i < Zones.Length; i++)
            {
                var z = Zones[i];
                var go = new GameObject("Label_" + z.label);
                go.transform.SetParent(folder, false);
                go.transform.localPosition = new Vector3(z.cx, LabelHeight, z.cz);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;

                var tm = go.AddComponent<TextMesh>();
                tm.text = z.label;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontSize = 64;
                // Under PARK WorldScale (~10): ~3 world-unit glyphs, readable from afar
                tm.characterSize = 0.3f;
                tm.color = new Color(1f, 0.95f, 0.75f, 1f);
                tm.fontStyle = FontStyle.Bold;

                _labels[i] = go.transform;
            }

            Debug.Log($"[ZoneNameMarkers] placed {_labels.Length} zone labels");
        }

        void LateUpdate()
        {
            if (_labels == null || _labels.Length == 0) return;
            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam == null) return;
            }

            var camPos = _cam.transform.position;
            for (int i = 0; i < _labels.Length; i++)
            {
                var t = _labels[i];
                if (t == null) continue;
                var toCam = camPos - t.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude < 0.0001f) continue;
                t.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
        }
    }
}