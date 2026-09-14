using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Third-person orbit/follow. Mouse look drives player yaw + camera pitch boom.
    /// Sets motor.cam for wish-direction; keeps the full body visible (no eye CamRig).
    /// </summary>
    public class TpsMoveCamera : MonoBehaviour
    {
        public PlayerMotor motor;
        public MovementConfig cfg;
        public Transform pitchPivot;
        public Camera cam;

        public float sensitivity = 1.8f;
        public float minPitch = -25f;
        public float maxPitch = 55f;
        public Vector3 boomOffset = new Vector3(0f, 0.55f, -5.5f);
        public float pivotHeight = 1.35f;
        public float follow = 18f;
        public float lookAtHeight = 1.25f;

        float _yaw;
        float _pitch = 12f;
        float _fov;
        float _tilt;

        PlayerInputReader _in;

        void Awake()
        {
            if (!motor) motor = GetComponentInParent<PlayerMotor>();
            if (!cfg && motor) cfg = motor.cfg;
            _in = motor != null ? motor.GetComponent<PlayerInputReader>() : null;
            _yaw = motor != null ? motor.transform.eulerAngles.y : transform.root.eulerAngles.y;
            _fov = cfg != null ? cfg.fovIdle : 70f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            if (motor == null) return;
            if (_in == null) _in = motor.GetComponent<PlayerInputReader>();
            float dt = Time.deltaTime;

            if (_in != null)
            {
                _yaw += _in.Look.x * sensitivity;
                _pitch -= _in.Look.y * sensitivity;
            }
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            // Body yaw only — camera boom owns pitch
            motor.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // CamRig stays at player root; pivot at chest height
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            if (pitchPivot != null)
            {
                pitchPivot.localPosition = new Vector3(0f, pivotHeight, 0f);
                pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, _tilt);
            }

            if (cam != null)
            {
                cam.transform.localPosition = boomOffset;
                // Look toward upper chest so framing stays readable
                Vector3 lookAt = motor.transform.position + Vector3.up * lookAtHeight;
                Vector3 to = lookAt - cam.transform.position;
                if (to.sqrMagnitude > 0.001f)
                    cam.transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);

                float targetFov = cfg != null ? cfg.fovIdle : 70f;
                if (cfg != null)
                {
                    switch (motor.State)
                    {
                        case MoveState.Sprint: targetFov = cfg.fovSprint; break;
                        case MoveState.Slide: targetFov = cfg.fovSlide; break;
                        case MoveState.Ski: targetFov = cfg.fovSki; break;
                        case MoveState.Jet: targetFov = cfg.fovJet; break;
                        case MoveState.Air:
                            targetFov = Mathf.Lerp(cfg.fovIdle, cfg.fovSki, Mathf.InverseLerp(8f, 24f, motor.HorizSpeed));
                            break;
                    }
                }
                _fov = Mathf.Lerp(_fov, targetFov, 1f - Mathf.Exp(-6f * dt));
                cam.fieldOfView = _fov;
            }

            float side = Vector3.Dot(motor.Velocity, motor.transform.right);
            float tiltMax = cfg != null ? cfg.tiltMax : 6f;
            float wantTilt = Mathf.Clamp(-side * 0.15f, -tiltMax, tiltMax);
            if (motor.State == MoveState.WallRun)
                wantTilt = motor.WallLeft ? tiltMax * 0.6f : -tiltMax * 0.6f;
            _tilt = Mathf.Lerp(_tilt, wantTilt, 1f - Mathf.Exp(-8f * dt));

            if (motor.cam == null && cam != null)
                motor.cam = cam.transform;
        }
    }
}
