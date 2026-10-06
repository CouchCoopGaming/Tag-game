using Tag.Core;
using Tag.Gameplay;
using Tag.Level;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Local
{
    /// <summary>
    /// OOB / void kill box from the ParkArena registry. Arena 0 is Mega Park,
    /// 1 is Pocket Park, 2 is Stack Yard. Each box is that park's fence plus the
    /// shared margin, and the kill height is the shared plane. Human and AI pawns.
    /// </summary>
    public class VoidRespawn : MonoBehaviour
    {
        [SerializeField] float killY = -20f;
        [SerializeField] float punchInvulnAfterTeleport = 1f;
        /// <summary>Meters past the playable edge before a respawn. The grass collar is 3 m.</summary>
        [SerializeField] float xzMargin = 4f;

        Rigidbody _rb;
        PlayerMotor _motor;
        PlayerRagdoll _ragdoll;
        ItController _it;

        void Awake()
        {
            if (xzMargin < SessionRules.Margin) xzMargin = SessionRules.Margin;
            _rb = GetComponent<Rigidbody>();
            _motor = GetComponent<PlayerMotor>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            _it = GetComponent<ItController>();
        }

        void FixedUpdate()
        {
            SessionRules.ArenaBox box = SessionRules.Bounds(ParkArena.Id);
            if (box.KillY != MegaParkP1Layout.KillPlaneY)
                box.KillY = MegaParkP1Layout.KillPlaneY;
            killY = box.KillY;
            Vector3 p = transform.position;
            if (!SessionRules.Outside(box, p.x, p.y, p.z)) return;
            RespawnToNearestPad(box);
        }

        void RespawnToNearestPad(SessionRules.ArenaBox box)
        {
            Vector3 from = transform.position;
            bool hasIt = TryOtherIt(out Vector3 itPos);
            ParkArena.PickRespawn(
                from.x, from.z,
                hasIt ? itPos.x : from.x,
                hasIt ? itPos.z : from.z,
                hasIt,
                out float x, out float y, out float z);
            if (SessionRules.Outside(box, x, y, z))
            {
                x = (box.MinX + box.MaxX) * 0.5f;
                z = (box.MinZ + box.MaxZ) * 0.5f;
                y = MegaParkP1Layout.SpawnY;
            }
            Vector3 pad = new Vector3(x, y, z);

            if (_ragdoll != null)
                _ragdoll.ForceRecover();
            if (_motor != null)
            {
                _motor.ClearStun();
                _motor.Place(pad);
            }
            else
            {
                if (_rb != null)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.useGravity = false;
                    _rb.position = pad;
                }
                transform.position = pad;
            }
            Physics.SyncTransforms();

            // Brief punch i-frames so spawn-camp / mid-void teleports aren't free tags.
            // PunchHitbox already gates via ItController.CanBeTagged → HasIFrames.
            if (_it != null && punchInvulnAfterTeleport > 0f)
                _it.ApplySpawnIFrames(punchInvulnAfterTeleport);
        }

        bool TryOtherIt(out Vector3 pos)
        {
            pos = default;
            ItController[] all = FindObjectsByType<ItController>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                ItController it = all[i];
                if (it == null || it.gameObject == gameObject) continue;
                if (!it.IsIt || !it.IsAlive) continue;
                pos = it.transform.position;
                return true;
            }
            return false;
        }
    }
}
