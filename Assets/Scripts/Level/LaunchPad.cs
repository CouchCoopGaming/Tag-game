using System.Collections.Generic;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Reusable map launch pad. Drop it on an empty object, or call <see cref="Build"/>.
    /// Awake builds a graybox slab, a trigger volume, and a glow strip.
    /// Mega Park placement is left to the map lane. This component does not retune jump.
    ///
    /// Fields:
    /// <list type="bullet">
    /// <item><see cref="apexHeight"/> — designer apex in meters. Default 6. Vertical speed is sqrt(2 * gravity * apex). Entry speed does not change it.</item>
    /// <item><see cref="horizontalDir"/> — pad-local XZ direction. Zero keeps the pawn's horizontal velocity.</item>
    /// <item><see cref="horizontalSpeed"/> — meters per second along <see cref="horizontalDir"/>. Zero keeps horizontal velocity.</item>
    /// <item><see cref="cooldown"/> — seconds before this pawn can fire this pad again. Default 0.3.</item>
    /// </list>
    /// The step is a velocity set inside the one kinematic CharacterController.Move.
    /// No rigidbody force and no root motion. After the set, air strafe may add horizontal only.
    /// The body reuses the jump rise pose. Landing uses the existing air-scaled thud.
    /// The same-wall ban still clears only when the pawn is on the ground.
    /// DummyRunner treats the pad as a known edge and takes it only when the landing is closer to the chase target.
    /// </summary>
    [DisallowMultipleComponent]
    public class LaunchPad : MonoBehaviour
    {
        [Tooltip("Apex height in meters. Default 6. Not scaled by entry speed or the jump button.")]
        public float apexHeight = LaunchPadRules.DefaultApexMeters;

        [Tooltip("Pad-local horizontal direction. Zero keeps the pawn's horizontal velocity.")]
        public Vector3 horizontalDir = Vector3.zero;

        [Tooltip("Meters per second along horizontalDir. Zero keeps horizontal velocity.")]
        public float horizontalSpeed = 0f;

        [Tooltip("Seconds before this pawn can fire this pad again.")]
        public float cooldown = LaunchPadRules.DefaultCooldown;

        const float PulseHz = 1.7f;

        static readonly List<LaunchPad> Active = new List<LaunchPad>();

        readonly Dictionary<int, float> _readyAt = new Dictionary<int, float>();
        readonly HashSet<int> _touching = new HashSet<int>();
        readonly HashSet<int> _seen = new HashSet<int>();
        readonly List<int> _drop = new List<int>();
        readonly Collider[] _hits = new Collider[16];

        BoxCollider _trigger;
        Renderer _glow;
        Material _glowMat;
        Color _glowHot;
        Color _glowDim;
        bool _built;

        public static LaunchPad Build(Transform parent, Vector3 worldPos, float apex = LaunchPadRules.DefaultApexMeters, Vector3 localDir = default, float speed = 0f, float cooldown = LaunchPadRules.DefaultCooldown)
        {
            var go = new GameObject("LaunchPad");
            if (parent != null)
                go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;
            var pad = go.AddComponent<LaunchPad>();
            pad.apexHeight = apex;
            pad.horizontalDir = localDir;
            pad.horizontalSpeed = speed;
            pad.cooldown = cooldown;
            pad.EnsurePad();
            return pad;
        }

        /// <summary>
        /// Nearest pad on the chase line, inside 16 m. The landing uses the pad's
        /// horizontal when it has one, otherwise the speed the pawn is carrying.
        /// </summary>
        public static bool QueryChase(Vector3 pawn, Vector3 velocity, Vector3 target, float gravity, float fallGravity, out Vector3 aim, out float distance, out bool helps)
        {
            aim = Vector3.zero;
            distance = 999f;
            helps = false;
            Vector3 toTarget = target - pawn;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.25f) return false;
            Vector3 aimN = toTarget.normalized;

            LaunchPad best = null;
            float bestDist = 999f;
            for (int i = 0; i < Active.Count; i++)
            {
                LaunchPad pad = Active[i];
                if (pad == null || !pad.isActiveAndEnabled) continue;
                Vector3 toPad = pad.transform.position - pawn;
                toPad.y = 0f;
                float dist = toPad.magnitude;
                if (dist < 0.45f || dist > 16f || dist >= bestDist) continue;
                if (Vector3.Dot(toPad / dist, aimN) < 0.35f) continue;
                best = pad;
                bestDist = dist;
            }
            if (best == null) return false;

            bool setH = best.TryHorizontal(out Vector3 horiz);
            Vector3 landing = LaunchPadRules.Landing(best.transform.position, velocity, horiz, setH, best.apexHeight, gravity, fallGravity);
            aim = best.transform.position - pawn;
            aim.y = 0f;
            distance = bestDist;
            helps = LaunchPadRules.LandingHelps(pawn, landing, target);
            return true;
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        void Awake()
        {
            EnsurePad();
        }

        void Update()
        {
            Pulse();
            PollSteps();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            Consider(other.GetComponentInParent<PlayerMotor>());
        }

        void OnTriggerExit(Collider other)
        {
            if (other == null) return;
            PlayerMotor motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null) return;
            _touching.Remove(motor.GetInstanceID());
        }

        /// <summary>Pad-local direction flattened to XZ. False when the pad does not set horizontal.</summary>
        public bool TryHorizontal(out Vector3 worldVelocity)
        {
            worldVelocity = Vector3.zero;
            if (horizontalSpeed <= 0.001f) return false;
            Vector3 dir = transform.TransformDirection(horizontalDir);
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-8f) return false;
            worldVelocity = dir.normalized * horizontalSpeed;
            return true;
        }

        void Consider(PlayerMotor motor)
        {
            if (motor == null) return;
            int id = motor.GetInstanceID();
            if (!_touching.Add(id)) return;
            float now = Time.time;
            if (_readyAt.TryGetValue(id, out float ready) && !LaunchPadRules.CooldownOpen(now, ready))
                return;
            TryHorizontal(out Vector3 horiz);
            bool setH = horizontalSpeed > 0.001f && horiz.sqrMagnitude > 1e-8f;
            if (!motor.QueueLaunch(apexHeight, horiz, setH, cooldown))
                return;
            _readyAt[id] = LaunchPadRules.ArmCooldown(now, cooldown);
        }

        void PollSteps()
        {
            if (_trigger == null) return;
            _seen.Clear();
            Vector3 center = _trigger.bounds.center;
            Vector3 ext = _trigger.bounds.extents;
            int n = Physics.OverlapBoxNonAlloc(center, ext, _hits, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;
                PlayerMotor motor = hit.GetComponentInParent<PlayerMotor>();
                if (motor == null) continue;
                _seen.Add(motor.GetInstanceID());
                Consider(motor);
            }
            _drop.Clear();
            foreach (int id in _touching)
            {
                if (!_seen.Contains(id))
                    _drop.Add(id);
            }
            for (int i = 0; i < _drop.Count; i++)
                _touching.Remove(_drop[i]);
        }

        void EnsurePad()
        {
            if (_built) return;
            _built = true;
            if (transform.Find("LaunchPadSlab") == null)
                BuildGraybox();
            EnsureTrigger();
        }

        void BuildGraybox()
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "LaunchPadSlab";
            slab.transform.SetParent(transform, false);
            slab.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            slab.transform.localScale = new Vector3(1.6f, 0.12f, 1.6f);
            Collider solid = slab.GetComponent<Collider>();
            if (solid != null)
                DestroyImmediate(solid);
            Tint(slab, new Color(0.45f, 0.48f, 0.52f), false);

            var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = "LaunchPadGlow";
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            glow.transform.localScale = new Vector3(1.15f, 0.04f, 1.15f);
            Collider glowCol = glow.GetComponent<Collider>();
            if (glowCol != null)
                DestroyImmediate(glowCol);
            _glowHot = new Color(1f, 0.55f, 0.15f);
            _glowDim = new Color(0.35f, 0.22f, 0.08f);
            _glow = glow.GetComponent<Renderer>();
            _glowMat = Tint(glow, _glowHot, true);

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.06f, 0f);
            box.size = new Vector3(1.6f, 0.12f, 1.6f);
        }

        void EnsureTrigger()
        {
            if (_trigger != null) return;
            BoxCollider[] boxes = GetComponents<BoxCollider>();
            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] != null && boxes[i].isTrigger)
                {
                    _trigger = boxes[i];
                    return;
                }
            }
            _trigger = gameObject.AddComponent<BoxCollider>();
            _trigger.isTrigger = true;
            _trigger.center = new Vector3(0f, 0.5f, 0f);
            _trigger.size = new Vector3(1.35f, 0.9f, 1.35f);
        }

        void Pulse()
        {
            if (_glowMat == null) return;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * PulseHz * Mathf.PI * 2f);
            Color c = Color.Lerp(_glowDim, _glowHot, 0.35f + 0.65f * wave);
            if (_glowMat.HasProperty("_EmissionColor"))
                _glowMat.SetColor("_EmissionColor", c * (1.2f + wave * 1.4f));
            if (_glowMat.HasProperty("_BaseColor"))
                _glowMat.SetColor("_BaseColor", c);
            if (_glowMat.HasProperty("_Color"))
                _glowMat.SetColor("_Color", c);
        }

        static Material Tint(GameObject go, Color c, bool emissive)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) return null;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            if (shader == null) return null;
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (emissive && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 1.6f);
            }
            mr.sharedMaterial = m;
            return m;
        }
    }
}
