using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Local
{
    /// <summary>
    /// Lightweight void killplane: below Y threshold, teleport to nearest LocalPlayerSpawner pad.
    /// Works for human and AI pawns (any Rigidbody + optional motor/ragdoll).
    /// </summary>
    public class VoidRespawn : MonoBehaviour
    {
        [SerializeField] float killY = -20f;
        [SerializeField] float punchInvulnAfterTeleport = 1f;

        Rigidbody _rb;
        PlayerMotor _motor;
        PlayerRagdoll _ragdoll;
        ItController _it;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _motor = GetComponent<PlayerMotor>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            _it = GetComponent<ItController>();
        }

        void FixedUpdate()
        {
            if (transform.position.y >= killY) return;
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
