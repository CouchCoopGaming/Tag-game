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

        Rigidbody _rb;
        PlayerMotor _motor;
        PlayerRagdoll _ragdoll;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _motor = GetComponent<PlayerMotor>();
            _ragdoll = GetComponent<PlayerRagdoll>();
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
