using Tag.Art;
using Tag.Gameplay;
using Tag.Modes;
using Tag.Trail;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Local
{
    /// <summary>
    /// Spawns 2–4 local players on PARK pads using TagArena (Apex×Tribes) motor + FP camera.
    /// </summary>
    public class LocalPlayerSpawner : MonoBehaviour
    {
        static readonly Vector3[] Spawns =
        {
            new Vector3(60f, 1.5f, 50f),
            new Vector3(660f, 1.5f, 50f),
            new Vector3(60f, 1.5f, 490f),
            new Vector3(660f, 1.5f, 490f)
        };
        static readonly float[] Yaws = { 90f, 0f, 180f, -90f };

        static MovementConfig _sharedCfg;

        [SerializeField] GameObject playerTemplate;

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
            // PlayerIndex lives on Tag.Input legacy — arena reader has no index; split-screen later

            if (go.GetComponent<SurfaceProbe>() == null) go.AddComponent<SurfaceProbe>();
            var motor = go.GetComponent<PlayerMotor>();
            if (motor == null) motor = go.AddComponent<PlayerMotor>();
            motor.cfg = SharedConfig();

            // Do NOT attach TagArena.TagRole auto-tag — Tag uses PunchHitbox + ItController
            motor.tagRole = null;

            if (go.GetComponent<MoveAnimDriver>() == null)
            {
                var anim = go.AddComponent<MoveAnimDriver>();
                anim.motor = motor;
            }

            if (go.GetComponent<PunchHitbox>() == null) go.AddComponent<PunchHitbox>();
            if (go.GetComponent<PlayerRagdoll>() == null) go.AddComponent<PlayerRagdoll>();
            if (go.GetComponent<PlayerTrailEmitter>() == null) go.AddComponent<PlayerTrailEmitter>();
            if (go.GetComponent<DummyAvatarBinder>() == null) go.AddComponent<DummyAvatarBinder>();
            if (go.GetComponent<ItMarker>() == null) go.AddComponent<ItMarker>();
            if (go.GetComponent<ItController>() == null) go.AddComponent<ItController>();
            var it = go.GetComponent<ItController>();
            if (it != null) it.PlayerId = $"P{index + 1}";

            // First-person camera only for human pawns (AI keeps no MainCamera)
            if (!ai)
                SetupFpCamera(go, motor);
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

        static MovementConfig SharedConfig()
        {
            if (_sharedCfg == null)
                _sharedCfg = ScriptableObject.CreateInstance<MovementConfig>();
            return _sharedCfg;
        }

        static void SetupFpCamera(GameObject go, PlayerMotor motor)
        {
            // Remove old child cameras that aren't under CamRig
            foreach (var cam in go.GetComponentsInChildren<Camera>(true))
            {
                if (cam.transform.parent != null && cam.transform.parent.name == "Pitch") continue;
                if (cam.transform.name == "Camera" && cam.transform.parent != null && cam.transform.parent.name == "Pitch") continue;
                // keep if already in CamRig
                bool underRig = false;
                var t = cam.transform;
                while (t != null)
                {
                    if (t.name == "CamRig") { underRig = true; break; }
                    t = t.parent;
                }
                if (!underRig && cam.gameObject.name != "Camera")
                    Destroy(cam.gameObject);
                else if (!underRig)
                {
                    // orphan old camera — disable, build new rig
                    cam.enabled = false;
                    var al = cam.GetComponent<AudioListener>();
                    if (al) al.enabled = false;
                }
            }

            Transform camRig = go.transform.Find("CamRig");
            if (camRig == null)
            {
                var rigGo = new GameObject("CamRig");
                camRig = rigGo.transform;
                camRig.SetParent(go.transform, false);
                camRig.localPosition = new Vector3(0f, 1.62f, 0f);

                var pitchGo = new GameObject("Pitch");
                pitchGo.transform.SetParent(camRig, false);

                var camGo = new GameObject("Camera");
                camGo.transform.SetParent(pitchGo.transform, false);
                var cam = camGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.fieldOfView = motor.cfg != null ? motor.cfg.fovIdle : 75f;
                if (camGo.GetComponent<AudioListener>() == null)
                    camGo.AddComponent<AudioListener>();

                var feel = camRig.gameObject.AddComponent<FpsMoveCamera>();
                feel.motor = motor;
                feel.cfg = motor.cfg;
                feel.pitchPivot = pitchGo.transform;
                feel.cam = cam;
                motor.cam = cam.transform;
            }
            else
            {
                var feel = camRig.GetComponent<FpsMoveCamera>();
                if (feel == null) feel = camRig.gameObject.AddComponent<FpsMoveCamera>();
                feel.motor = motor;
                feel.cfg = motor.cfg;
                var pitch = camRig.Find("Pitch");
                if (pitch != null)
                {
                    feel.pitchPivot = pitch;
                    var cam = pitch.GetComponentInChildren<Camera>();
                    if (cam != null)
                    {
                        feel.cam = cam;
                        motor.cam = cam.transform;
                        cam.tag = "MainCamera";
                    }
                }
            }
        }
    }
}
