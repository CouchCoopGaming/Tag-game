using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// First-person rig that sells speed. Does not own look inversion of movement —
    /// it only reads the motor and adds tilt / fov / landing kick.
    /// Parent this under the player, camera as child of this transform.
    /// </summary>
    public class FpsMoveCamera : MonoBehaviour
    {
        public PlayerMotor motor;
        public MovementConfig cfg;
        public Transform pitchPivot;
        public Camera cam;

        public float sensitivity = 1.8f;
        public float minPitch = -88f;
        public float maxPitch = 88f;
        public float eyeStanding = 1.62f;
        public float eyeCrouch = 0.92f;
        public float follow = 18f;

        float _yaw;
        float _pitch;
        float _fov;
        float _tilt;
        float _eye;
        Vector3 _kick;

        PlayerInputReader _in;

        void Awake()
        {
            if (!motor) motor = GetComponentInParent<PlayerMotor>();
            if (!cfg && motor) cfg = motor.cfg;
            _in = motor.GetComponent<PlayerInputReader>();
            _yaw = transform.root.eulerAngles.y;
            _fov = cfg.fovIdle;
            _eye = eyeStanding;
            LookSensitivity.Load();
            sensitivity = LookSensitivity.Current;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            _yaw += _in.Look.x * sensitivity;
            _pitch -= _in.Look.y * sensitivity;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            motor.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (pitchPivot) pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            bool low = motor.State == MoveState.Crouch || motor.State == MoveState.Slide;
            _eye = Mathf.Lerp(_eye, low ? eyeCrouch : eyeStanding, 1f - Mathf.Exp(-follow * dt));
            transform.localPosition = new Vector3(0f, _eye, 0f) + _kick;

            float targetFov = cfg.fovIdle;
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
            _fov = Mathf.Lerp(_fov, targetFov, 1f - Mathf.Exp(-6f * dt));
            if (cam) cam.fieldOfView = _fov;

            float side = Vector3.Dot(motor.Velocity, motor.transform.right);
            float wantTilt = Mathf.Clamp(-side * 0.25f, -cfg.tiltMax, cfg.tiltMax);
            if (motor.State == MoveState.WallRun)
                wantTilt = motor.WallLeft ? cfg.tiltMax : -cfg.tiltMax;
            _tilt = Mathf.Lerp(_tilt, wantTilt, 1f - Mathf.Exp(-8f * dt));

            _kick = Vector3.Lerp(_kick, Vector3.zero, 1f - Mathf.Exp(-10f * dt));

            if (pitchPivot)
            {
                var e = pitchPivot.localEulerAngles;
                pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, _tilt);
            }
        }

        public void AddKick(Vector3 local)
        {
            _kick += local;
        }
    }
}
