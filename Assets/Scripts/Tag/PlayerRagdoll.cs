using System.Collections;
using UnityEngine;
using Tag.Audio;
using TagArena.Movement;

namespace Tag.Gameplay
{
    /// <summary>
    /// Stun / ragdoll proxy on Rigidbody motor: lock motor, impulse, unlock.
    /// Full bone ragdoll can replace this later. No punch while down (motor locked).
    /// </summary>
    public class PlayerRagdoll : MonoBehaviour
    {
        [SerializeField] Rigidbody bodyRb;
        PlayerMotor _motor;
        bool _ragdolling;
        Coroutine _routine;

        public bool IsRagdolling => _ragdolling;

        void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            EnsureBodyRb();
        }

        void EnsureBodyRb()
        {
            if (bodyRb == null)
                bodyRb = GetComponent<Rigidbody>();
            if (bodyRb == null)
            {
                bodyRb = gameObject.AddComponent<Rigidbody>();
                bodyRb.mass = 80f;
                bodyRb.interpolation = RigidbodyInterpolation.Interpolate;
                bodyRb.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }


        public void ForceRecover()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            EnsureBodyRb();
            if (bodyRb != null)
            {
                bodyRb.linearVelocity = Vector3.zero;
                bodyRb.angularVelocity = Vector3.zero;
                bodyRb.useGravity = false;
                Quaternion rot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                transform.rotation = rot;
            }

            if (_motor != null) _motor.SetMotorLocked(false);
            _ragdolling = false;
        }

        public void TriggerRagdoll(float duration)
        {
            TriggerRagdoll(duration, Vector3.up * 2f);
        }

        public void TriggerRagdoll(float duration, Vector3 knockVelocity)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(RagdollRoutine(duration, knockVelocity));
        }

        IEnumerator RagdollRoutine(float duration, Vector3 knockVelocity)
        {
            _ragdolling = true;
            AudioCuePlayer.Ensure()?.Ragdoll(transform.position);
            if (_motor != null) _motor.SetMotorLocked(true);

            EnsureBodyRb();
            // Motor already owns non-kinematic RB; keep gravity on during stun.
            bodyRb.isKinematic = false;
            bodyRb.useGravity = true;
            bodyRb.linearVelocity = Vector3.zero;
            bodyRb.angularVelocity = Vector3.zero;
            bodyRb.AddForce(knockVelocity, ForceMode.VelocityChange);

            yield return new WaitForSeconds(duration);

            Vector3 pos = transform.position;
            Quaternion rot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            bodyRb.linearVelocity = Vector3.zero;
            bodyRb.angularVelocity = Vector3.zero;
            // Keep non-kinematic for TagArena motor; just settle yaw.
            transform.SetPositionAndRotation(pos, rot);
            Physics.SyncTransforms();

            if (_motor != null) _motor.SetMotorLocked(false);
            _ragdolling = false;
            _routine = null;
        }
    }
}
