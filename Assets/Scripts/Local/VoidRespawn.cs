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
        /// <summary>Meters past the 160×100 playable edge before a respawn. The grass collar is 3 m.</summary>
        [SerializeField] float xzMargin = 4f;

        Rigidbody _rb;
        PlayerMotor _motor;
        PlayerRagdoll _ragdoll;
        ItController _it;

        // Mega Park origin = SW corner, +X east, +Z north. Real meters, scale 1.
        float _minX, _maxX, _minZ, _maxZ;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _motor = GetComponent<PlayerMotor>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            _it = GetComponent<ItController>();
        }

        void Start()
        {
            // After MegaParkP1Bootstrap.Awake, so a scene arenaId is already selected.
            killY = ParkArena.IsPocket ? PocketParkLayout.KillPlaneY : MegaParkP1Layout.KillPlaneY;
            float pad = xzMargin;
            _minX = -pad;
            _maxX = ParkArena.MapW + pad;
            _minZ = -pad;
            _maxZ = ParkArena.MapD + pad;
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
            Vector3 from = transform.position;
            bool hasIt = TryOtherIt(out Vector3 itPos);
            ParkArena.PickRespawn(
                from.x, from.z,
                hasIt ? itPos.x : from.x,
                hasIt ? itPos.z : from.z,
                hasIt,
                out float x, out float y, out float z);
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
