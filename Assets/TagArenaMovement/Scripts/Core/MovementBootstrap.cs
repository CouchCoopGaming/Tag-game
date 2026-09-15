using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Drop this on an empty GameObject in a new scene and press Play.
    /// Builds a test capsule, camera, config, and sloped arena so you can
    /// feel the motor before art exists.
    /// </summary>
    public class MovementBootstrap : MonoBehaviour
    {
        public bool buildOnAwake = false;

        void Awake()
        {
            if (buildOnAwake) Build();
        }

        [ContextMenu("Build Test Arena")]
        public void Build()
        {
            var cfg = ScriptableObject.CreateInstance<MovementConfig>();

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(80f, 1f, 80f);

            BuildRamp(new Vector3(0, 0, 18), new Vector3(16, 8, 24), Quaternion.Euler(-22f, 0f, 0f), "Ramp_Down");
            BuildRamp(new Vector3(18, 2, 0), new Vector3(14, 1, 28), Quaternion.Euler(0f, 90f, -18f), "Ramp_Side");
            BuildBox(new Vector3(-8, 1.0f, 8), new Vector3(4, 2, 1), "Wall_Low");
            BuildBox(new Vector3(-8, 2.4f, 14), new Vector3(4, 4.8f, 1), "Wall_High");
            BuildBox(new Vector3(8, 0.6f, 6), new Vector3(3, 1.2f, 3), "Mantle_Box");
            BuildBox(new Vector3(8, 1.5f, 12), new Vector3(2, 3f, 6), "WallRun");

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            Destroy(player.GetComponent<MeshCollider>());

            var cap = player.GetComponent<CapsuleCollider>();
            cap.height = cfg.standingHeight;
            cap.radius = cfg.radius;
            cap.center = new Vector3(0, cfg.standingHeight * 0.5f, 0);

            var rb = player.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            player.AddComponent<PlayerInputReader>();
            player.AddComponent<SurfaceProbe>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.cfg = cfg;

            var tag = player.AddComponent<TagRole>();
            tag.IsIt = false;
            motor.tagRole = tag;
            player.AddComponent<MoveAnimDriver>().motor = motor;
            player.AddComponent<SpeedEnergyHUD>().motor = motor;

            var camRig = new GameObject("CamRig");
            camRig.transform.SetParent(player.transform);
            camRig.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            var pitch = new GameObject("Pitch");
            pitch.transform.SetParent(camRig.transform);
            pitch.transform.localPosition = Vector3.zero;

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(pitch.transform);
            camGo.transform.localPosition = Vector3.zero;
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = cfg.fovIdle;
            camGo.AddComponent<AudioListener>();

            var feel = camRig.AddComponent<FpsMoveCamera>();
            feel.motor = motor;
            feel.cfg = cfg;
            feel.pitchPivot = pitch.transform;
            feel.cam = cam;
            motor.cam = cam.transform;

            var light = new GameObject("Sun");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.15f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            if (Camera.main && Camera.main != cam)
                Camera.main.gameObject.SetActive(false);
        }

        static void BuildRamp(Vector3 pos, Vector3 scale, Quaternion rot, string name)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.position = pos;
            g.transform.localScale = scale;
            g.transform.rotation = rot;
        }

        static void BuildBox(Vector3 pos, Vector3 scale, string name)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.position = pos;
            g.transform.localScale = scale;
        }
    }
}
