using Tag.Art;
using Tag.Gameplay;
using Tag.Modes;
using Tag.Trail;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Local
{
    /// <summary>
    /// Spawns 2â€“4 local players on PARK pads using TagArena (ApexÃ—Tribes) motor + third-person camera.
    /// </summary>
    public class LocalPlayerSpawner : MonoBehaviour
    {
        public static readonly Vector3[] Spawns =
        {
            new Vector3(60f, 1.5f, 50f),
            new Vector3(660f, 1.5f, 50f),
            new Vector3(60f, 1.5f, 490f),
            new Vector3(660f, 1.5f, 490f)
        };
        static readonly float[] Yaws = { 90f, 0f, 180f, -90f };

        static MovementConfig _sharedCfg;

        [SerializeField] GameObject playerTemplate;
        [SerializeField] MovementConfig configOverride = null;

        void Awake()
        {
            LocalPlayerRoster.Load();
            if (playerTemplate == null)
                playerTemplate = GameObject.Find("Player");

            var dummy = GameObject.Find("DummyRunner");
            if (LocalPlayerRoster.IsCouch)
            {
                if (dummy != null) dummy.SetActive(false);
                SpawnAll(LocalPlayerRoster.PlayerCount);
            }
            else
            {
                if (dummy != null) dummy.SetActive(true);
                var p0 = GameObject.Find("Player");
                if (p0 != null) ConfigurePawn(p0, 0);
                if (dummy != null) ConfigurePawn(dummy, 1, ai: true);
            }
        }

        public void SpawnAll(int count)
        {
            count = Mathf.Clamp(count, 2, 4);
            GameObject p0 = GameObject.Find("Player") ?? playerTemplate;
            if (p0 == null)
            {
                Debug.LogError("[LocalPlayerSpawner] No Player template");
                return;
            }

            ConfigurePawn(p0, 0);

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

        void ConfigurePawn(GameObject go, int index, bool ai = false)
        {
            go.transform.position = Spawns[Mathf.Clamp(index, 0, Spawns.Length - 1)];
            go.transform.rotation = Quaternion.Euler(0f, Yaws[Mathf.Clamp(index, 0, Yaws.Length - 1)], 0f);

            // Strip legacy CharacterController motor path
            var legacyCc = go.GetComponent<CharacterController>();
            if (legacyCc != null) legacyCc.enabled = false;

            var cap = go.GetComponent<CapsuleCollider>();
            if (cap == null) cap = go.AddComponent<CapsuleCollider>();
            cap.height = 1.8f;
            cap.radius = 0.35f;
            cap.center = new Vector3(0f, 0.9f, 0f);

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = 80f;

            if (go.GetComponent<PlayerInputReader>() == null) go.AddComponent<PlayerInputReader>();
            var input = go.GetComponent<PlayerInputReader>();
            if (input != null) input.ExternalControl = ai;
            // PlayerIndex lives on Tag.Input legacy â€” arena reader has no index; split-screen later

            if (go.GetComponent<SurfaceProbe>() == null) go.AddComponent<SurfaceProbe>();
            var motor = go.GetComponent<PlayerMotor>();
            if (motor == null) motor = go.AddComponent<PlayerMotor>();
            motor.cfg = SharedConfig();

            // Do NOT attach TagArena.TagRole auto-tag â€” Tag uses PunchHitbox + ItController
            motor.tagRole = null;

            if (go.GetComponent<MoveAnimDriver>() == null)
            {
                var anim = go.AddComponent<MoveAnimDriver>();
                anim.motor = motor;
            }

            if (go.GetComponent<PunchHitbox>() == null) go.AddComponent<PunchHitbox>();
            if (go.GetComponent<PlayerRagdoll>() == null) go.AddComponent<PlayerRagdoll>();
            if (go.GetComponent<VoidRespawn>() == null) go.AddComponent<VoidRespawn>();
            if (go.GetComponent<PlayerTrailEmitter>() == null) go.AddComponent<PlayerTrailEmitter>();
            if (go.GetComponent<DummyAvatarBinder>() == null) go.AddComponent<DummyAvatarBinder>();
            if (go.GetComponent<ItMarker>() == null) go.AddComponent<ItMarker>();
            if (go.GetComponent<ItController>() == null) go.AddComponent<ItController>();
            var it = go.GetComponent<ItController>();
            if (it != null) it.PlayerId = $"P{index + 1}";

            // Speed/ski/jet HUD for primary local human only (not AI, not couch clones)
            if (!ai && index == 0)
            {
                var hud = go.GetComponent<SpeedEnergyHUD>();
                if (hud == null) hud = go.AddComponent<SpeedEnergyHUD>();
                hud.motor = motor;
            }

            // Third-person camera for human pawns (AI keeps no MainCamera)
            if (!ai)
                SetupTpCamera(go, motor);
            else
            {
                // Kill orphan cameras on DummyRunner so they don't steal MainCamera
                foreach (var cam in go.GetComponentsInChildren<Camera>(true))
                {
                    cam.enabled = false;
                    var al = cam.GetComponent<AudioListener>();
                    if (al) al.enabled = false;
                }
            }

            if (ai && go.GetComponent<DummyPatrol>() == null)
                go.AddComponent<DummyPatrol>();

            // Hide capsule mesh if present
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        MovementConfig SharedConfig()
        {
            if (_sharedCfg != null)
                return _sharedCfg;

            if (configOverride != null)
                _sharedCfg = configOverride;
            else
                _sharedCfg = Resources.Load<MovementConfig>("TagArena/MovementConfig");

            if (_sharedCfg == null)
            {
                Debug.LogWarning("[LocalPlayerSpawner] MovementConfig asset missing; using CreateInstance defaults. Expected Resources/TagArena/MovementConfig.");
                _sharedCfg = ScriptableObject.CreateInstance<MovementConfig>();
            }

            return _sharedCfg;
        }

        static void SetupTpCamera(GameObject go, PlayerMotor motor)
        {
            // Disable / remove first-person eye CamRig and FpsMoveCamera
            foreach (var fps in go.GetComponentsInChildren<FpsMoveCamera>(true))
                Destroy(fps);

            foreach (var cam in go.GetComponentsInChildren<Camera>(true))
            {
                bool underRig = false;
                var t = cam.transform;
                while (t != null)
                {
                    if (t.name == "CamRig") { underRig = true; break; }
                    t = t.parent;
                }
                if (!underRig)
                {
                    cam.enabled = false;
                    var al = cam.GetComponent<AudioListener>();
                    if (al) al.enabled = false;
                }
            }

            // Tear down old FP eye placement if present (CamRig at eye height with Pitch child)
            Transform existing = go.transform.Find("CamRig");
            if (existing != null)
            {
                // Rebuild clean TP boom â€” destroy old eye rig
                Destroy(existing.gameObject);
                existing = null;
            }

            var rigGo = new GameObject("CamRig");
            var camRig = rigGo.transform;
            camRig.SetParent(go.transform, false);
            camRig.localPosition = Vector3.zero;
            camRig.localRotation = Quaternion.identity;

            var pivotGo = new GameObject("Pivot");
            pivotGo.transform.SetParent(camRig, false);
            pivotGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(pivotGo.transform, false);
            camGo.transform.localPosition = new Vector3(0.4f, 0.45f, -5.2f);
            var camComp = camGo.AddComponent<Camera>();
            camComp.tag = "MainCamera";
            camComp.fieldOfView = motor.cfg != null ? motor.cfg.fovIdle : 70f;
            camComp.nearClipPlane = 0.15f;
            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();

            var tps = camRig.gameObject.AddComponent<TpsMoveCamera>();
            tps.motor = motor;
            tps.cfg = motor.cfg;
            tps.pitchPivot = pivotGo.transform;
            tps.cam = camComp;
            tps.boomOffset = new Vector3(0.4f, 0.45f, -5.2f);
            tps.pivotHeight = 1.4f;
            motor.cam = camComp.transform;

            ResumeInputGate.LockPlayCursor();
        }
    }
}

