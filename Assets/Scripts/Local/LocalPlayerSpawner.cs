using Tag.Art;
using Tag.Couch;
using Tag.Experimental;
using Tag.Gameplay;
using Tag.Level;
using Tag.Modes;
using Tag.Onboard;
using Tag.Settings;
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
        /// <summary>Solo human. Couch clones and the campus opponent do not get a rope.</summary>
        public const string SoloPawnName = SoloGrappleGate.SoloPawnName;
        /// <summary>Solo campus opponent. Couch play turns this pawn off.</summary>
        public const string OpponentPawnName = SoloGrappleGate.OpponentPawnName;
        /// <summary>Mega Park SE pad. Real meters; world position is Spawns[1].</summary>
        public const string OpponentPadName = "Spawn_SE";
        public const float OpponentPadGrayX = MegaParkP1Layout.SpawnSeX;
        public const float OpponentPadGrayZ = MegaParkP1Layout.SpawnSeZ;

        public static readonly Vector3[] Spawns =
        {
            new Vector3(MegaParkP1Layout.SpawnSwX, MegaParkP1Layout.SpawnY, MegaParkP1Layout.SpawnSwZ),
            new Vector3(MegaParkP1Layout.SpawnSeX, MegaParkP1Layout.SpawnY, MegaParkP1Layout.SpawnSeZ),
            new Vector3(MegaParkP1Layout.SpawnNwX, MegaParkP1Layout.SpawnY, MegaParkP1Layout.SpawnNwZ),
            new Vector3(MegaParkP1Layout.SpawnNeX, MegaParkP1Layout.SpawnY, MegaParkP1Layout.SpawnNeZ)
        };
        // Pad facing is the layout's CCW yaw. Positions already come from the same pads.
        static readonly float[] Yaws =
        {
            MegaParkP1Layout.Spawns[0].YawDeg,
            MegaParkP1Layout.Spawns[1].YawDeg,
            MegaParkP1Layout.Spawns[2].YawDeg,
            MegaParkP1Layout.Spawns[3].YawDeg,
        };

        static MovementConfig _sharedCfg;

        [SerializeField] GameObject playerTemplate;
        [SerializeField] MovementConfig configOverride = null;
        [Tooltip("Solo dummies in the Play scene. 1 is the placed DummyRunner. 2 and 3 clone it.")]
        [SerializeField] int dummyCount = 1;

        void Awake()
        {
            LocalPlayerRoster.Load();
            if (playerTemplate == null)
                playerTemplate = GameObject.Find(SoloPawnName);

            var dummy = GameObject.Find(OpponentPawnName);
            if (LocalPlayerRoster.IsCouch || CouchPlay.Humans >= 2)
            {
                if (dummy != null) dummy.SetActive(false);
                SpawnSeats(dummy);
            }
            else
            {
                if (dummy != null) dummy.SetActive(true);
                var p0 = GameObject.Find(SoloPawnName);
                if (p0 != null) ConfigurePawn(p0, 0);
                if (dummy != null)
                {
                    ConfigurePawn(dummy, 1, ai: true);
                    SpawnExtraDummies(dummy, dummyCount);
                }
            }
        }

        public void SpawnAll(int count)
        {
            count = Mathf.Clamp(count, 2, 4);
            GameObject p0 = GameObject.Find(SoloPawnName) ?? playerTemplate;
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

            // CharacterController is the motor. The rigidbody wakes only for the ragdoll window.
            var legacyCc = go.GetComponent<CharacterController>();
            if (legacyCc == null) legacyCc = go.AddComponent<CharacterController>();
            legacyCc.enabled = true;
            legacyCc.height = 1.8f;
            legacyCc.radius = 0.35f;
            legacyCc.center = new Vector3(0f, 0.9f, 0f);
            legacyCc.slopeLimit = TagArena.Movement.KinematicStep.SlopeLimit(48f, false);
            legacyCc.stepOffset = TagArena.Movement.KinematicStep.StepOffset(1.8f, 0.38f, 0.02f, false);
            legacyCc.skinWidth = 0.02f;

            var cap = go.GetComponent<CapsuleCollider>();
            if (cap == null) cap = go.AddComponent<CapsuleCollider>();
            cap.height = 1.8f;
            cap.radius = 0.35f;
            cap.center = new Vector3(0f, 0.9f, 0f);
            cap.enabled = false;

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.mass = 80f;

            if (go.GetComponent<PlayerInputReader>() == null) go.AddComponent<PlayerInputReader>();
            var input = go.GetComponent<PlayerInputReader>();
            if (input != null)
            {
                input.ExternalControl = ai;
                if (!ai && CouchPlay.Humans >= 2)
                    input.DriveDevice = CouchPlay.DeviceOf(index);
            }

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
            // Both pawns can show the handoff flash. It does not tag on touch.
            if (go.GetComponent<TagLandFlash>() == null) go.AddComponent<TagLandFlash>();
            if (go.GetComponent<ItController>() == null) go.AddComponent<ItController>();
            var it = go.GetComponent<ItController>();
            if (it != null) it.PlayerId = $"P{index + 1}";

            // Speed HUD stays on the first human. Every human gets a viewport HUD.
            if (!ai && index == 0)
            {
                var hud = go.GetComponent<SpeedEnergyHUD>();
                if (hud == null) hud = go.AddComponent<SpeedEnergyHUD>();
                hud.motor = motor;
            }
            if (!ai)
            {
                var verbs = go.GetComponent<Tag.Modes.VerbStatusHud>();
                if (verbs == null) verbs = go.AddComponent<Tag.Modes.VerbStatusHud>();
                verbs.motor = motor;
                if (CouchPlay.Humans >= 2)
                {
                    verbs.Seat = index;
                    verbs.DriveDevice = CouchPlay.DeviceOf(index);
                }
                if (go.GetComponent<PlayPromptHud>() == null)
                    go.AddComponent<PlayPromptHud>();
                var prompts = go.GetComponent<PlayPromptHud>();
                if (prompts != null && CouchPlay.Humans >= 2)
                {
                    prompts.Seat = index;
                    prompts.DriveDevice = CouchPlay.DeviceOf(index);
                }
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

            // Rope is the solo human only. Jet stays off, so RMB hooks and does not jet.
            ApplySoloGrapple(go, index, ai);
            // The opponent uses the same RMB rope. The solo gate stays closed.
            if (ai && EnemyAi.AllowRope(true, go.name))
                ApplyEnemyRope(go);

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

        void SpawnSeats(GameObject dummy)
        {
            GameObject template = GameObject.Find(SoloPawnName) ?? playerTemplate;
            if (template == null)
            {
                Debug.LogError("[LocalPlayerSpawner] No Player template");
                return;
            }
            bool used = false;
            for (int i = 0; i < CouchPlay.Max; i++)
            {
                bool human = CouchPlay.HumanAt(i);
                bool ai = CouchPlay.AiAt(i);
                if (!human && !ai) continue;
                GameObject go;
                if (!used)
                {
                    go = template;
                    used = true;
                }
                else
                {
                    GameObject src = ai && dummy != null ? dummy : template;
                    go = Instantiate(src);
                    go.SetActive(true);
                }
                if (i > 0) go.name = "Player_" + CouchPlay.Name(i);
                ConfigurePawn(go, i, ai);
                var marker = go.GetComponent<ItMarker>();
                if (marker != null)
                {
                    CouchPlay.Tint(i, out float r, out float g, out float b);
                    marker.SetIdentity(CouchPlay.Name(i), new Color(r, g, b, 1f));
                }
                if (!ai)
                {
                    Camera cam = go.GetComponentInChildren<Camera>();
                    var verbs = go.GetComponent<VerbStatusHud>();
                    if (verbs != null) verbs.View = cam;
                    var prompts = go.GetComponent<PlayPromptHud>();
                    if (prompts != null) prompts.View = cam;
                }
                else if (GameSettings.Current != null)
                {
                    var patrol = go.GetComponent<DummyPatrol>();
                    if (patrol != null) patrol.ApplyDifficulty(GameSettings.Current.DifficultyValue());
                }
            }
            if (CouchPlay.Humans == 3 && GetComponent<CouchScoreHud>() == null)
                gameObject.AddComponent<CouchScoreHud>();
            var split = GetComponent<LocalSplitCamera>();
            if (split == null) split = gameObject.AddComponent<LocalSplitCamera>();
            split.Apply();
        }

        public void ApplyOpponents(int count)
        {
            if (count < 0) count = 0;
            if (count > 3) count = 3;
            Retire(OpponentPawnName + "_2");
            Retire(OpponentPawnName + "_3");
            GameObject dummy = GameObject.Find(OpponentPawnName);
            if (count <= 0)
            {
                if (dummy != null) dummy.SetActive(false);
                return;
            }
            if (dummy == null) return;
            dummy.SetActive(true);
            if (count >= 2)
                SpawnExtraDummies(dummy, count);
        }

        static void Retire(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) return;
            go.SetActive(false);
            Destroy(go);
        }

        void SpawnExtraDummies(GameObject template, int count)
        {
            int n = EnemyAi.ClampDummyCount(count);
            for (int i = 2; i <= n; i++)
            {
                string name = OpponentPawnName + "_" + i.ToString();
                if (GameObject.Find(name) != null) continue;
                GameObject clone = Instantiate(template);
                clone.name = name;
                clone.SetActive(true);
                ConfigurePawn(clone, i, ai: true);
            }
        }

        static void ApplyEnemyRope(GameObject go)
        {
            ExperimentalGrapple rope = go.GetComponent<ExperimentalGrapple>();
            if (rope == null)
                rope = go.AddComponent<ExperimentalGrapple>();
            rope.enableGrapple = true;
            rope.useJetHeldAsFire = true;
        }

        static void ApplySoloGrapple(GameObject go, int index, bool ai)
        {
            bool on = SoloGrappleGate.EnableFor(LocalPlayerRoster.IsCouch, ai, index, go.name);
            var rope = go.GetComponent<ExperimentalGrapple>();
            if (on)
            {
                if (rope == null)
                    rope = go.AddComponent<ExperimentalGrapple>();
                rope.enableGrapple = true;
                return;
            }

            if (ai && EnemyAi.AllowRope(true, go.name))
                return;
            if (rope == null) return;
            rope.enableGrapple = false;
            Destroy(rope);
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
                    // A disabled listener still counts, so the follow camera would log two listeners.
                    var al = cam.GetComponent<AudioListener>();
                    if (al) DestroyImmediate(al);
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

