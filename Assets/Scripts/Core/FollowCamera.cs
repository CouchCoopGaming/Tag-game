using UnityEngine;
using Tag.Input;

namespace Tag.Core
{
    /// <summary>Third-person follow / orbit. Optional if PlayerMotor owns look.</summary>
    public class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public PlayerInputReader input;
        public Vector3 offset = new Vector3(0f, 2.2f, -5.5f);
        public float sensitivity = 0.12f;
        public float minPitch = -25f;
        public float maxPitch = 55f;
        public float followLerp = 18f;
        public bool driveLook = false; // motor often owns yaw/pitch on pivot

        float _yaw;
        float _pitch = 12f;

        void Start()
        {
            if (target != null)
                _yaw = target.eulerAngles.y;
            if (input == null && target != null)
                input = target.GetComponentInParent<PlayerInputReader>();
        }

        void LateUpdate()
        {
            if (target == null) return;

            if (driveLook && input != null)
            {
                _yaw += input.LookDelta.x * sensitivity;
                _pitch -= input.LookDelta.y * sensitivity;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
                Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
                Vector3 desired = target.position + rot * offset;
                transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
            }
            else
            {
                // Follow behind camera pivot / target facing
                Vector3 desired = target.TransformPoint(offset);
                transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation((target.position + Vector3.up * 1.4f) - transform.position, Vector3.up),
                    1f - Mathf.Exp(-followLerp * Time.deltaTime));
            }
        }
    }
}
