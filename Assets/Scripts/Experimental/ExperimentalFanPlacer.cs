using UnityEngine;

namespace Tag.Experimental
{
    /// <summary>
    /// EXPERIMENTAL ONLY — optional fan spawner for the mega park.
    /// Off by default; enable <see cref="spawnFans"/> in the inspector or via code.
    /// Core tag loop must not depend on these fans.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExperimentalFanPlacer : MonoBehaviour
    {
        [Header("EXPERIMENTAL — off by default, not part of core loop")]
        [SerializeField] bool spawnFans = false;
        [SerializeField] float impulse = 22f;
        [Tooltip("Local XZ positions under this transform (park root). Y is placed on a short pedestal.")]
        [SerializeField] Vector3[] fanLocalPositions = new Vector3[]
        {
            new Vector3(18f, 0f, 18f),
            new Vector3(54f, 0f, 18f),
            new Vector3(18f, 0f, 40f),
            new Vector3(54f, 0f, 40f),
        };
        [SerializeField] Vector3[] fanLookDirections = new Vector3[]
        {
            new Vector3(1f, 0.35f, 0.4f),
            new Vector3(-1f, 0.35f, 0.4f),
            new Vector3(1f, 0.35f, -0.25f),
            new Vector3(-1f, 0.35f, -0.25f),
        };

        Transform _folder;
        bool _spawned;

        void Start()
        {
            if (spawnFans) EnsureFans();
        }

        /// <summary>Runtime toggle for playtests — still experimental.</summary>
        public void SetSpawnFans(bool enabled)
        {
            spawnFans = enabled;
            if (enabled) EnsureFans();
            else ClearFans();
        }

        void EnsureFans()
        {
            if (_spawned) return;
            _spawned = true;
            _folder = new GameObject("EXPERIMENTAL_Fans").transform;
            _folder.SetParent(transform, false);
            int n = fanLocalPositions != null ? fanLocalPositions.Length : 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 look = (fanLookDirections != null && i < fanLookDirections.Length)
                    ? fanLookDirections[i]
                    : Vector3.forward + Vector3.up * 0.3f;
                LaunchFan.Build(_folder, fanLocalPositions[i] + Vector3.up * 0.05f, look, impulse);
            }
            Debug.Log($"[EXPERIMENTAL] Spawned {n} launch fans under {name}. Core loop does not require these.");
        }

        void ClearFans()
        {
            if (_folder != null) Destroy(_folder.gameObject);
            _folder = null;
            _spawned = false;
        }
    }
}
