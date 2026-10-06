using System.Collections.Generic;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Level
{
    /// <summary>
    /// Reusable map zip line. Drop it on an empty object, or call <see cref="Build"/>.
    /// Awake builds a cable and a grab volume between the two ends.
    /// Mega Park placement is left to the map lane. This component does not retune jump.
    ///
    /// Fields:
    /// <list type="bullet">
    /// <item><see cref="pointA"/> — first end. A level line rides from here toward <see cref="pointB"/>.</item>
    /// <item><see cref="pointB"/> — second end. When the ends differ in height, the ride goes downhill.</item>
    /// <item><see cref="rideSpeed"/> — meters per second along the cable. Default 14. Entry speed does not change it.</item>
    /// <item><see cref="regrabCooldown"/> — seconds before this pawn can grab this line again. Default 0.3.</item>
    /// </list>
    /// The pawn grabs by holding cling (the move stick) while touching the grab volume,
    /// in the air or from a ledge. Releasing cling falls with the ride velocity.
    /// Jump drops with jumpSpeed and keeps the ride's horizontal. The end auto-drops
    /// with that same carry. The step is a velocity set inside the one kinematic
    /// CharacterController.Move. No rigidbody, no physics joint, and no root motion.
    /// A punch stagger drops the grab. A tag still connects. The same-wall ban is
    /// not cleared by the ride. Landing still clears it.
    /// DummyRunner takes the line only when the exit is closer to the chase target.
    /// </summary>
    [DisallowMultipleComponent]
    public class ZipLine : MonoBehaviour
    {
        [Tooltip("First end. A level cable rides from pointA toward pointB.")]
        public Transform pointA;

        [Tooltip("Second end. A sloped cable rides toward the lower end.")]
        public Transform pointB;

        [Tooltip("Meters per second along the cable. Default 14. Entry speed is ignored.")]
        public float rideSpeed = ZipLineRules.DefaultRideSpeed;

        [Tooltip("Seconds before this pawn can grab this same line again.")]
        public float regrabCooldown = ZipLineRules.DefaultRegrabCooldown;

        static readonly List<ZipLine> Active = new List<ZipLine>();

        readonly HashSet<int> _riders = new HashSet<int>();

        Transform _cable;
        Transform _grab;
        BoxCollider _trigger;
        Material _cableMat;
        Color _idle;
        Color _hot;
        bool _built;

        public Vector3 WorldA => pointA != null ? pointA.position : transform.TransformPoint(new Vector3(0f, 4f, 0f));
        public Vector3 WorldB => pointB != null ? pointB.position : transform.TransformPoint(new Vector3(14f, 2.2f, 0f));
        public float Speed => rideSpeed > 0.01f ? rideSpeed : ZipLineRules.DefaultRideSpeed;
        public float Cooldown => regrabCooldown > 0f ? regrabCooldown : 0f;
        public bool Held => _riders.Count > 0;
        public Vector3 EntryWorld => ZipLineRules.EntryPoint(WorldA, WorldB);
        public Vector3 ExitWorld => ZipLineRules.ExitPoint(WorldA, WorldB);

        public static ZipLine Build(Transform parent, Vector3 worldA, Vector3 worldB, float speed = ZipLineRules.DefaultRideSpeed, float cooldown = ZipLineRules.DefaultRegrabCooldown)
        {
            var go = new GameObject("ZipLine");
            if (parent != null)
                go.transform.SetParent(parent, true);
            go.transform.position = worldA;
            go.transform.rotation = Quaternion.identity;
            var line = go.AddComponent<ZipLine>();
            line.rideSpeed = speed;
            line.regrabCooldown = cooldown;
            line.EnsurePoints();
            if (line.pointA != null) line.pointA.position = worldA;
            if (line.pointB != null) line.pointB.position = worldB;
            line.EnsureVisual();
            return line;
        }

        /// <summary>
        /// Nearest line on the chase line, inside 16 m, or one the pawn is already under.
        /// The exit helps only when it is closer to the target than staying off the line.
        /// </summary>
        public static bool QueryChase(Vector3 pawn, Vector3 target, out Vector3 aim, out float distance, out bool helps)
        {
            aim = Vector3.zero;
            distance = 999f;
            helps = false;
            Vector3 toTarget = target - pawn;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.25f) return false;
            Vector3 aimN = toTarget.normalized;

            ZipLine best = null;
            float bestDist = 999f;
            bool bestOn = false;
            for (int i = 0; i < Active.Count; i++)
            {
                ZipLine line = Active[i];
                if (line == null || !line.isActiveAndEnabled) continue;
                Vector3 entry = line.EntryWorld;
                float cable = ZipLineRules.DistanceToSegment(pawn, line.WorldA, line.WorldB);
                float along = ZipLineRules.AlongMeters(pawn, entry, line.ExitWorld);
                float span = (line.ExitWorld - entry).magnitude;
                bool on = cable <= 2.6f && along > -0.4f && along < span + 0.4f;
                if (on)
                {
                    if (bestOn && cable >= bestDist) continue;
                    best = line;
                    bestDist = cable;
                    bestOn = true;
                    continue;
                }
                if (bestOn) continue;
                Vector3 toEntry = entry - pawn;
                toEntry.y = 0f;
                float dist = toEntry.magnitude;
                if (dist < 0.45f || dist > 16f || dist >= bestDist) continue;
                if (Vector3.Dot(toEntry * (1f / dist), aimN) < 0.35f) continue;
                float dy = entry.y - pawn.y;
                if (dy > 8f || dy < -3f) continue;
                best = line;
                bestDist = dist;
            }
            if (best == null) return false;

            if (bestOn)
            {
                aim = ZipLineRules.RideDirection(best.WorldA, best.WorldB);
                aim.y = 0f;
            }
            else
            {
                aim = best.EntryWorld - pawn;
                aim.y = 0f;
            }
            distance = bestDist;
            helps = ZipLineRules.ExitHelps(pawn, best.ExitWorld, target);
            return true;
        }

        public static bool TryGrab(PlayerMotor motor)
        {
            if (motor == null) return false;
            Vector3 pawn = motor.transform.position;
            for (int i = 0; i < Active.Count; i++)
            {
                ZipLine line = Active[i];
                if (line == null || !line.isActiveAndEnabled) continue;
                if (!line.Contains(pawn)) continue;
                if (motor.TryBeginZip(line))
                    return true;
            }
            return false;
        }

        public bool Contains(Vector3 world)
        {
            return ZipLineRules.InGrabVolume(world, WorldA, WorldB);
        }

        public Vector3 CurrentRideVelocity()
        {
            return ZipLineRules.RideVelocity(Vector3.zero, ZipLineRules.RideDirection(WorldA, WorldB), Speed);
        }

        public Vector3 RideVelocityWithHang(Vector3 pawn, float dt)
        {
            return ZipLineRules.HangVelocity(pawn, EntryWorld, ExitWorld, Speed, dt);
        }

        public bool AtExit(Vector3 pawn, float dt)
        {
            return ZipLineRules.ReachedEnd(pawn, EntryWorld, ExitWorld, Speed, dt);
        }

        public void SetRider(int motorId, bool riding)
        {
            if (riding) _riders.Add(motorId);
            else _riders.Remove(motorId);
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
            _riders.Clear();
        }

        void Awake()
        {
            EnsurePoints();
            EnsureVisual();
        }

        void Update()
        {
            RefreshVisual();
            Pulse();
        }

        void EnsurePoints()
        {
            if (pointA == null)
            {
                var a = new GameObject("PointA");
                a.transform.SetParent(transform, false);
                a.transform.localPosition = new Vector3(0f, 4f, 0f);
                pointA = a.transform;
            }
            if (pointB == null)
            {
                var b = new GameObject("PointB");
                b.transform.SetParent(transform, false);
                b.transform.localPosition = new Vector3(14f, 2.2f, 0f);
                pointB = b.transform;
            }
        }

        void EnsureVisual()
        {
            if (_built) return;
            _built = true;
            if (transform.Find("ZipCable") == null)
            {
                var cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cable.name = "ZipCable";
                cable.transform.SetParent(transform, true);
                Collider solid = cable.GetComponent<Collider>();
                if (solid != null)
                    DestroyImmediate(solid);
                _idle = new Color(0.12f, 0.38f, 0.46f);
                _hot = new Color(0.55f, 0.95f, 1f);
                _cableMat = Tint(cable, _idle, true);
                _cable = cable.transform;
            }
            else
            {
                _cable = transform.Find("ZipCable");
            }

            Transform grab = transform.Find("ZipGrab");
            if (grab == null)
            {
                var go = new GameObject("ZipGrab");
                go.transform.SetParent(transform, true);
                grab = go.transform;
            }
            _grab = grab;
            _trigger = _grab.GetComponent<BoxCollider>();
            if (_trigger == null)
                _trigger = _grab.gameObject.AddComponent<BoxCollider>();
            _trigger.isTrigger = true;
            RefreshVisual();
        }

        void RefreshVisual()
        {
            if (_cable == null || _grab == null || _trigger == null) return;
            Vector3 a = WorldA;
            Vector3 b = WorldB;
            Vector3 span = b - a;
            float len = span.magnitude;
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = len > 0.05f ? span / len : Vector3.forward;
            _cable.position = mid;
            _cable.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            _cable.localScale = new Vector3(0.07f, Mathf.Max(0.05f, len * 0.5f), 0.07f);

            float down = ZipLineRules.HangDrop + ZipLineRules.GrabBelow;
            float up = ZipLineRules.GrabAbove;
            float height = up + down;
            _grab.position = mid;
            _grab.rotation = Quaternion.LookRotation(dir, Vector3.up);
            _trigger.isTrigger = true;
            _trigger.size = new Vector3(ZipLineRules.GrabRadius * 2f, height, Mathf.Max(0.4f, len));
            _trigger.center = new Vector3(0f, (up - down) * 0.5f, 0f);
        }

        void Pulse()
        {
            if (_cableMat == null) return;
            bool held = Held;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * (held ? 3.2f : 1.4f) * Mathf.PI * 2f);
            Color c = held ? Color.Lerp(_idle, _hot, 0.55f + 0.45f * wave) : Color.Lerp(_idle, _hot, 0.18f + 0.12f * wave);
            if (_cableMat.HasProperty("_EmissionColor"))
                _cableMat.SetColor("_EmissionColor", c * (held ? 2.4f : 0.6f));
            if (_cableMat.HasProperty("_BaseColor"))
                _cableMat.SetColor("_BaseColor", c);
            if (_cableMat.HasProperty("_Color"))
                _cableMat.SetColor("_Color", c);
        }

        void OnDrawGizmos()
        {
            Vector3 a = WorldA;
            Vector3 b = WorldB;
            Gizmos.color = new Color(0.35f, 0.85f, 0.95f, 0.9f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawWireSphere(a, 0.12f);
            Gizmos.DrawWireSphere(b, 0.12f);
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
                m.SetColor("_EmissionColor", c * 0.8f);
            }
            mr.sharedMaterial = m;
            return m;
        }
    }
}
