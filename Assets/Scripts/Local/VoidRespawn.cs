using Tag.Gameplay;
using Tag.Level;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Local
{
    /// <summary>
    /// Lightweight OOB / void killplane: below Y threshold OR outside mega-park XZ AABB,
    /// teleport to nearest LocalPlayerSpawner pad. Works for human and AI pawns.
    /// </summary>
    public class VoidRespawn : MonoBehaviour
    {
        [SerializeField] float killY = -20f;
        [SerializeField] float punchInvulnAfterTeleport = 1f;
        /// <summary>World-space margin past floor edge (MapW/MapD * WorldScale).</summary>
        [SerializeField] float xzMargin = 20f;

        Rigidbody _rb;
        PlayerMotor _motor;
        PlayerRagdoll _ragdoll;
        ItController _it;

        // Derived from CutArenaBootstrap: origin = SW corner, +X east, +Z north.
        float _minX, _maxX, _minZ, _maxZ;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _motor = GetComponent<PlayerMotor>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            _it = GetComponent<ItController>();

            float worldW = CutArenaBootstrap.MapW * CutArenaBootstrap.WorldScale;
            float worldD = CutArenaBootstrap.MapD * CutArenaBootstrap.WorldScale;
            _minX = -xzMargin;
            _maxX = worldW + xzMargin;
            _minZ = -xzMargin;
            _maxZ = worldD + xzMargin;
        }

        void FixedUpdate()
        {
            Vector3 p = transform.position;
            bool oobY = p.y < killY;
            bool oobXZ = p.x < _minX || p.x > _maxX || p.z < _minZ || p.z > _maxZ;
            if (!oobY && !oobXZ) return;
            RespawnToNearestPad();
        }

        void RespawnToNearestPad()
        {
            Vector3 pad = NearestPad(transform.position);

            if (_ragdoll != null)
                _ragdoll.ForceRecover();
            if (_motor != null)
                _motor.ClearStun();

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                // Motor owns gravity; ragdoll may have flipped this on.
                _rb.useGravity = false;
                _rb.position = pad;
            }

            transform.position = pad;
            Physics.SyncTransforms();

            // Brief punch i-frames so spawn-camp / mid-void teleports aren't free tags.
            // PunchHitbox already gates via ItController.CanBeTagged → HasIFrames.
            if (_it != null && punchInvulnAfterTeleport > 0f)
                _it.ApplySpawnIFrames(punchInvulnAfterTeleport);
        }

        static Vector3 NearestPad(Vector3 from)
        {
            var pads = LocalPlayerSpawner.Spawns;
            Vector3 best = pads[0];
            float bestSq = float.MaxValue;
            for (int i = 0; i < pads.Length; i++)
            {
                float dx = pads[i].x - from.x;
                float dz = pads[i].z - from.z;
                float sq = dx * dx + dz * dz;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = pads[i];
                }
            }
            return best;
        }
    }
}
