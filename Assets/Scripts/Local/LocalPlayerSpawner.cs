using Tag.Art;
using Tag.Gameplay;
using Tag.Input;
using Tag.Modes;
using Tag.Movement;
using Tag.Trail;
using UnityEngine;

namespace Tag.Local
{
    /// <summary>
    /// Spawns 2–4 local players at PARK spawn pads. Uses existing Player as P0 template.
    /// </summary>
    public class LocalPlayerSpawner : MonoBehaviour
    {
        static readonly Vector3[] Spawns =
        {
            new Vector3(3f, 1f, 3f),   // SW
            new Vector3(33f, 1f, 3f),  // SE
            new Vector3(3f, 1f, 25f),  // NW
            new Vector3(33f, 1f, 25f)  // NE
        };
        static readonly float[] Yaws = { 90f, 0f, 180f, -90f }; // CCW

        [SerializeField] GameObject playerTemplate;

        void Awake()
        {
            LocalPlayerRoster.Load();
            if (playerTemplate == null)
            {
                var p = GameObject.Find("Player");
                playerTemplate = p;
            }
            LocalPlayerRoster.Load();
            var dummy = GameObject.Find("DummyRunner");
            if (LocalPlayerRoster.IsCouch)
            {
                if (dummy != null) dummy.SetActive(false);
                SpawnAll(LocalPlayerRoster.PlayerCount);
            }
            else
            {
                // SP demo: keep DummyRunner, only configure P0
                if (dummy != null) dummy.SetActive(true);
                var p0 = GameObject.Find("Player");
                if (p0 != null) ConfigurePawn(p0, 0);
            }
        }

        public void SpawnAll(int count)
        {
            count = Mathf.Clamp(count, 2, 4);
            var existing = FindObjectsByType<ItController>(FindObjectsSortMode.None);
            // Ensure P0
            GameObject p0 = GameObject.Find("Player");
            if (p0 == null && playerTemplate != null)
                p0 = playerTemplate;
            if (p0 == null)
            {
                Debug.LogError("[LocalPlayerSpawner] No Player template");
                return;
            }

            ConfigurePawn(p0, 0);

            // Remove extra previous clones
            foreach (var it in FindObjectsByType<ItController>(FindObjectsSortMode.None))
            {
                if (it.gameObject == p0) continue;
                if (it.gameObject.name.StartsWith("Player_P"))
                    Destroy(it.gameObject);
            }

            for (int i = 1; i < count; i++)
            {
                var clone = Instantiate(p0);
                clone.name = $"Player_P{i}";
                ConfigurePawn(clone, i);
            }
        }

        void ConfigurePawn(GameObject go, int index)
        {
            go.transform.position = Spawns[index];
            go.transform.rotation = Quaternion.Euler(0f, Yaws[index], 0f);

            var input = go.GetComponent<PlayerInputReader>();
            if (input == null) input = go.AddComponent<PlayerInputReader>();
            input.PlayerIndex = index;
            // Only P0 locks cursor
            var soType = typeof(PlayerInputReader);
            // lockCursor via serialized - set with reflection-free public if needed

            var it = go.GetComponent<ItController>();
            if (it != null) it.PlayerId = $"P{index + 1}";

            if (go.GetComponent<PlayerMotor>() == null) go.AddComponent<PlayerMotor>();
            if (go.GetComponent<PunchHitbox>() == null) go.AddComponent<PunchHitbox>();
            if (go.GetComponent<PlayerTrailEmitter>() == null) go.AddComponent<PlayerTrailEmitter>();
            if (go.GetComponent<DummyAvatarBinder>() == null) go.AddComponent<DummyAvatarBinder>();
            if (go.GetComponent<CharacterController>() == null)
            {
                var cc = go.AddComponent<CharacterController>();
                cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0, 0.9f, 0);
            }

            // Camera child for split
            var cam = go.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(go.transform, false);
                camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.gameObject.tag = "MainCamera";
        }
    }
}
