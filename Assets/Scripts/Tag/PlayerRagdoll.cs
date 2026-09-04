using System.Collections;
using UnityEngine;
using Tag.Movement;

namespace Tag.Gameplay
{
    /// <summary>
    /// Kinematic stun / ragdoll proxy: CC off → Rigidbody impulse → CC on.
    /// Full bone ragdoll can replace this later. No punch while down (motor locked).
    /// </summary>
    public class PlayerRagdoll : MonoBehaviour
    {
        [SerializeField] Rigidbody bodyRb;
        CharacterController _cc;
        PlayerMotor _motor;
        bool _ragdolling;
        Coroutine _routine;

        public bool IsRagdolling => _ragdolling;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
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
            bodyRb.isKinematic = true;
            bodyRb.useGravity = false;
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
            if (_motor != null) _motor.SetMotorLocked(true);
            if (_cc != null) _cc.enabled = false;

            EnsureBodyRb();
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
            bodyRb.isKinematic = true;
            bodyRb.useGravity = false;
            transform.SetPositionAndRotation(pos, rot);

            if (_cc != null)
            {
                _cc.enabled = true;
                Physics.SyncTransforms();
            }
            if (_motor != null) _motor.SetMotorLocked(false);
            _ragdolling = false;
            _routine = null;
        }
    }
}
