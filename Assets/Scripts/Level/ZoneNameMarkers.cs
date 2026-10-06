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

        /// <summary>Mega Park zone centers in real meters (160×100, origin SW). Shared with the HUD.</summary>
        public static readonly (string label, float cx, float cz)[] ZoneCenters =
        {
            ("SOFT-PLAY", 18f, 24f),
            ("CLING", 8f, 58f),
            ("MERRY", 34f, 47f),
            ("SLIDE", 40f, 80f),
            ("SWING", 78f, 81f),
            ("FORTS", 140f, 48f),
            ("KICKBALL", 96f, 48f),
            ("CRASH", 62f, 50f),
            ("BARS", 78f, 16f),
            ("HOPSCOTCH", 136f, 15f),
        };

        static Transform _parkCached;

        Transform[] _labels;
        Camera _cam;

        void Start() => Place();

        [ContextMenu("Place Zone Name Markers")]
        public void Place()
        {
            var park = transform.Find("MegaPark");
            if (park == null)
                park = transform.Find("PARK");
            if (park == null)
            {
                Debug.LogWarning("[ZoneNameMarkers] No MegaPark root.");
                return;
            }

            _parkCached = park;

            var existing = park.Find(FolderName);
            if (existing != null)
                Destroy(existing.gameObject);

            var folder = new GameObject(FolderName).transform;
            folder.SetParent(park, false);
            folder.localPosition = Vector3.zero;
            folder.localRotation = Quaternion.identity;
            folder.localScale = Vector3.one;

            _labels = new Transform[ZoneCenters.Length];
            for (int i = 0; i < ZoneCenters.Length; i++)
            {
                var z = ZoneCenters[i];
                var go = new GameObject("Label_" + z.label.Replace(' ', '_'));
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

        /// <summary>
        /// Nearest mega-park zone label for a world-space position (XZ, graybox centers).
        /// Uses PARK InverseTransformPoint when available so WorldScale cannot drift.
        /// </summary>
        public static string GetNearestZoneName(Vector3 worldPos)
        {
            var centers = ZoneCenters;
            if (centers == null || centers.Length == 0)
                return "";

            Vector2 local = WorldToGrayboxXZ(worldPos);
            string best = centers[0].label;
            float bestSq = float.MaxValue;
            for (int i = 0; i < centers.Length; i++)
            {
                var z = centers[i];
                float dx = local.x - z.cx;
                float dz = local.y - z.cz;
                float sq = dx * dx + dz * dz;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = z.label;
                }
            }
            return best;
        }

        static Vector2 WorldToGrayboxXZ(Vector3 worldPos)
        {
            var park = ResolvePark();
            if (park != null)
            {
                var local = park.InverseTransformPoint(worldPos);
                return new Vector2(local.x, local.z);
            }

            // Mega Park is scale 1. World XZ is the map XZ.
            return new Vector2(worldPos.x, worldPos.z);
        }

        static Transform ResolvePark()
        {
            if (_parkCached != null)
                return _parkCached;
            var go = GameObject.Find("MegaPark");
            if (go == null)
                go = GameObject.Find("PARK");
            if (go != null)
                _parkCached = go.transform;
            return _parkCached;
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
