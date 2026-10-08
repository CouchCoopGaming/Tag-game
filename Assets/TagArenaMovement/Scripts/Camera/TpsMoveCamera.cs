using Tag.Art;
using Tag.Settings;
using UnityEngine;

namespace TagArena.Movement
{
    /// <summary>
    /// Third-person orbit/follow. Mouse look drives player yaw + camera pitch boom.
    /// Sets motor.cam for wish-direction; keeps the full body visible (no eye CamRig).
    /// Soft sphere-cast keeps the boom from clipping through world geometry.
    /// FOV + slight look-ahead track HorizSpeed / MoveState for readable speed feel.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TpsMoveCamera : MonoBehaviour
    {
        public PlayerMotor motor;
        public MovementConfig cfg;
        public Transform pitchPivot;
        public Camera cam;

        public float sensitivity = 1.8f;
        public float minPitch = -25f;
        public float maxPitch = 55f;
        // Slightly above-shoulder, ~5.2m back — readable third-person framing
        public Vector3 boomOffset = new Vector3(0.4f, 0.45f, -5.2f);
        public float pivotHeight = 1.4f;
        public float follow = 18f;
        public float lookAtHeight = 1.25f;
        public float collisionRadius = 0.28f;
        public float collisionMinDistance = 0.55f;
        public LayerMask collisionMask = ~0;
        // Look-ahead / speed FOV — keep small for TP readability (not FPS tunnel)
        public float lookAheadMax = 0.9f;
        public float lookAheadSpeedLo = 4.4f;
        public float lookAheadSpeedHi = 22f;
        public float speedFovBoostMax = 3f;

        float _yaw;
        float _pitch = 12f;
        float _fov;
        float _tilt;
        float _boomDist;
        float _lookAhead;
        float _lookH = 1.25f;
        float _lookHVel;
        float _wallLook;
        float _wallLookVel;
        Vector3 _wallLookDir;
        float _catchT;
        bool _wasLunging;
        MoveState _prevState = MoveState.Idle;
        Vector3 _aheadSmoothed;
        Vector3 _kick;
        Vector3 _kickFrom;
        Vector3 _kickTo;
        float _kickIn = 1f;
        int _lookFrame = -1;

        PlayerInputReader _in;
        DummyLocomotor _loco;

        void Awake()
        {
            if (!motor) motor = GetComponentInParent<PlayerMotor>();
            if (!cfg && motor) cfg = motor.cfg;
            _in = motor != null ? motor.GetComponent<PlayerInputReader>() : null;
            _yaw = motor != null ? motor.transform.eulerAngles.y : transform.root.eulerAngles.y;
            _fov = cfg != null ? cfg.fovIdle : 70f;
            _lookH = lookAtHeight;
            _boomDist = Mathf.Abs(boomOffset.z);
            LookSensitivity.Load();
            sensitivity = LookSensitivity.Current;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Start()
        {
            if (motor != null && _in == null) _in = motor.GetComponent<PlayerInputReader>();
            if (motor != null) _loco = motor.GetComponentInChildren<DummyLocomotor>(true);
        }

        void Update()
        {
            ApplyLook();
        }

        /// <summary>
        /// This frame's look, after the reader and before the motor.
        /// LateUpdate keeps the boom and does not add look again.
        /// </summary>
        void ApplyLook()
        {
            if (motor == null) return;
            if (_lookFrame == Time.frameCount) return;
            _lookFrame = Time.frameCount;
            if (_in == null) BindRig();
            if (_in != null && !ResumeInputGate.Blocking)
            {
                bool padLook = _in.LookFromGamepad;
                LookFeel.Deltas(_in.Look.x, _in.Look.y, padLook, out float yaw, out float pitch);
                _yaw += yaw;
                _pitch -= pitch;
            }
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            motor.transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        void LateUpdate()
        {
            if (motor == null) return;
            if (_in == null || _loco == null) ResolveRig();
            float dt = Time.deltaTime;

            MoveState state = motor.State;
            bool enteredSlide = state == MoveState.Slide && _prevState != MoveState.Slide;
            bool wallToAir = state == MoveState.Air && (_prevState == MoveState.WallRun || _prevState == MoveState.WallClimb);
            bool enteredMantle = state == MoveState.Mantle && _prevState != MoveState.Mantle;
            bool lungeBurst = motor.IsLunging && !_wasLunging;
            _wasLunging = motor.IsLunging;
            bool becomeIt = false;
            bool aim = false;
            // This pawn's visual only. A scene search would let a couch or DummyRunner pose move the solo rig.
            DummyLocomotor loco = _loco;
            if (loco != null)
            {
                becomeIt = loco.ConsumeBecomeCatch();
                aim = loco.ConsumeAimCatch();
            }
            ChaseCam.Arm(ref _catchT, enteredSlide, wallToAir, enteredMantle, becomeIt, lungeBurst, aim, motor.gameObject.name);
            _prevState = state;

            // CamRig stays at player root; pivot at chest/shoulder height
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            if (pitchPivot != null)
            {
                pitchPivot.localPosition = new Vector3(0f, pivotHeight, 0f) + _kick;
                pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, _tilt);
            }

            if (cam != null)
            {
                ApplyBoomWithCollision();

                // Look toward upper chest + slight velocity look-ahead (readable speed).
                // A slide ducks the wedge, so the look point drops with it.
                // A climb reaches up, so the look point rises with the hands. Mouse look is unchanged.
                float wantLookH = lookAtHeight;
                if (motor.State == MoveState.Slide)
                    wantLookH = lookAtHeight - 0.32f;
                else if (motor.State == MoveState.WallClimb)
                    wantLookH = lookAtHeight + 0.28f;
                float lookRate = ChaseCam.LookRateFor(_catchT);
                // Height and the wall offset ease. The catch rate still owns look-ahead direction.
                _lookH = SmoothMotion.Smooth(_lookH, wantLookH, ref _lookHVel, SmoothMotion.SettleSeconds, dt);
                Vector3 lookAt = motor.transform.position + Vector3.up * _lookH;
                float wantWall = 0f;
                Vector3 wallDir = _wallLookDir.sqrMagnitude > 0.001f ? _wallLookDir : motor.transform.forward;
                if (motor.State == MoveState.WallRun && motor.WallNormal.sqrMagnitude > 0.01f)
                {
                    Vector3 wallInto = Vector3.ProjectOnPlane(-motor.WallNormal, Vector3.up);
                    if (wallInto.sqrMagnitude > 0.01f)
                    {
                        wantWall = 0.42f;
                        wallDir = wallInto.normalized;
                    }
                }
                if (_wallLookDir.sqrMagnitude < 0.001f)
                    _wallLookDir = wallDir;
                _wallLook = SmoothMotion.Smooth(_wallLook, wantWall, ref _wallLookVel, SmoothMotion.YawSeconds, dt);
                float wallU = 1f - Mathf.Exp(-SmoothMotion.Rate(SmoothMotion.YawSeconds) * dt);
                _wallLookDir = Vector3.Slerp(_wallLookDir, wallDir, wallU);
                if (_wallLook > 0.001f && _wallLookDir.sqrMagnitude > 0.001f)
                    lookAt += _wallLookDir.normalized * _wallLook;
                float lo = cfg != null ? cfg.walkSpeed : lookAheadSpeedLo;
                float hi = lookAheadSpeedHi;
                float speedT = Mathf.InverseLerp(lo, hi, motor.HorizSpeed);
                float wantAhead = lookAheadMax * speedT;
                switch (motor.State)
                {
                    case MoveState.Sprint: wantAhead *= 1.05f; break;
                    case MoveState.Slide: wantAhead *= 1.15f; break;
                    case MoveState.Ski: wantAhead *= 1.25f; break;
                    case MoveState.Jet: wantAhead *= 1.1f; break;
                    case MoveState.Air: wantAhead *= 1.08f; break;
                    case MoveState.WallRun: wantAhead *= 0.7f; break;
                    case MoveState.Crouch:
                    case MoveState.Idle: wantAhead *= 0.35f; break;
                }
                _lookAhead = Mathf.Lerp(_lookAhead, wantAhead, 1f - Mathf.Exp(-lookRate * dt));
                Vector3 hv = motor.Velocity; hv.y = 0f;
                // Direction is smoothed. An instant velocity flip was yawing the look-at
                // point 180° in one frame (the distance lerp was already smooth).
                // A pose gate uses its short window so the look arrives with the body.
                // A normal turn leaves the slow slew alone.
                Vector3 rawAhead = hv.sqrMagnitude > 1f ? hv.normalized : motor.transform.forward;
                if (_aheadSmoothed.sqrMagnitude < 0.001f) _aheadSmoothed = rawAhead;
                float aheadRate = ChaseCam.AheadRateFor(_catchT);
                _aheadSmoothed = Vector3.Slerp(_aheadSmoothed, rawAhead, 1f - Mathf.Exp(-aheadRate * dt));
                if (_aheadSmoothed.sqrMagnitude > 0.001f)
                    lookAt += _aheadSmoothed.normalized * _lookAhead;

                Vector3 to = lookAt - cam.transform.position;
                if (to.sqrMagnitude > 0.001f)
                {
                    // Mouse yaw is already on the rig this frame. A small look-point step eases.
                    // A flick, or the boom pulling in, still snaps so the horizon does not trail the mouse.
                    Vector3 dir = to.normalized;
                    Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
                    float align = Vector3.Dot(cam.transform.forward, dir);
                    if (align > 0.990f && align < 0.9998f)
                        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, want, 1f - Mathf.Exp(-18f * dt));
                    else
                        cam.transform.rotation = want;
                }

                // Mirror FpsMoveCamera cfg.fov* by MoveState (+ tiny continuous speed boost)
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
                    targetFov += speedFovBoostMax * speedT;
                }
                targetFov = LookFeel.ScaleFov(targetFov);
                _fov = Mathf.Lerp(_fov, targetFov, 1f - Mathf.Exp(-6f * dt));
                cam.fieldOfView = _fov;
            }

            float side = Vector3.Dot(motor.Velocity, motor.transform.right);
            float tiltMax = cfg != null ? cfg.tiltMax : 6f;
            // Strafe roll used to orbit the boom, then LookRotation snapped the lens back.
            float wantTilt = Mathf.Clamp(-side * 0.05f, -tiltMax, tiltMax);
            if (motor.State == MoveState.WallRun)
                wantTilt = motor.WallLeft ? tiltMax * 0.6f : -tiltMax * 0.6f;
            _tilt = Mathf.Lerp(_tilt, wantTilt, 1f - Mathf.Exp(-8f * dt));

            if (_kickIn < 1f)
            {
                _kickIn = Mathf.MoveTowards(_kickIn, 1f, dt / SmoothMotion.ResponsiveSeconds);
                float u = _kickIn;
                u = u * u * (3f - 2f * u);
                _kick = Vector3.Lerp(_kickFrom, _kickTo, u);
            }
            else
                _kick = Vector3.Lerp(_kick, Vector3.zero, 1f - Mathf.Exp(-12f * dt));
            if (_catchT > 0f)
                _catchT = Mathf.Max(0f, _catchT - dt);

            if (motor.cam == null && cam != null)
                motor.cam = cam.transform;
        }

        void ResolveRig()
        {
            BindRig();
        }

        void BindRig()
        {
            if (motor == null) return;
            if (_in == null) _in = motor.GetComponent<PlayerInputReader>();
            if (_loco == null) _loco = motor.GetComponentInChildren<DummyLocomotor>(true);
        }

        /// <summary>Brief punch/tag camera offset. fovKick=0. Shake and slow motion stay 0.</summary>
        public void AddKick(Vector3 local)
        {
            _kickFrom = _kick;
            _kickTo = _kick + local;
            _kickIn = 0f;
        }

        void ApplyBoomWithCollision()
        {
            if (pitchPivot == null)
            {
                cam.transform.localPosition = boomOffset;
                return;
            }

            // Slight boom stretch at speed so look-ahead has room without clipping feel
            float wantDist = Mathf.Abs(boomOffset.z) + _lookAhead * 0.35f;
            Vector3 localDir = new Vector3(boomOffset.x, boomOffset.y, -wantDist);
            Vector3 worldDesired = pitchPivot.TransformPoint(localDir);
            Vector3 origin = pitchPivot.position;
            Vector3 delta = worldDesired - origin;
            float maxDist = delta.magnitude;
            float dist = maxDist;

            if (maxDist > 0.01f)
            {
                int mask = collisionMask.value != 0 ? collisionMask.value : ~0;
                if (Physics.SphereCast(origin, collisionRadius, delta.normalized, out RaycastHit hit, maxDist, mask, QueryTriggerInteraction.Ignore))
                {
                    if (motor == null || hit.transform == null || !hit.transform.IsChildOf(motor.transform))
                        dist = Mathf.Max(collisionMinDistance, hit.distance - collisionRadius * 0.15f);
                }
            }

            _boomDist = ChaseCam.BoomDistance(_boomDist, dist, Time.deltaTime, _catchT > 0f);
            float t = maxDist > 0.01f ? (_boomDist / maxDist) : 1f;
            cam.transform.position = origin + delta * t;
        }
    }
}