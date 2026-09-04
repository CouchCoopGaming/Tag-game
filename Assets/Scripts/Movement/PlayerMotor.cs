using UnityEngine;
using Tag.Audio;
using Tag.Input;
using Tag.Gameplay;

namespace Tag.Movement
{
    /// <summary>
    /// CharacterController motor: walk/sprint/jump/coyote/buffer/air control,
    /// slide, wall run, wall jump, vault, air dodge (Systems Tag v1).
    /// Same kit for It and runner.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] MovementTuning tuning;
        [SerializeField] Transform cameraPivot;
        [SerializeField] float lookSensitivity = 2.0f;
        [SerializeField] float pitchMin = -80f;
        [SerializeField] float pitchMax = 80f;

        CharacterController _cc;
        PlayerInputReader _input;
        PunchHitbox _punch;
        PlayerRagdoll _ragdoll;
        Vector3 _velocity;
        float _yaw;
        float _pitch;
        float _coyoteTimer;
        float _jumpBufferTimer;
        float _hardLandTimer;
        float _speedMul = 1f;
        float _speedBoostTimer;
        bool _sprintHeld;

        // Slide state
        bool _sliding;
        float _slideTimer;
        float _slideCooldownTimer;
        float _standHeight;
        float _standCenterY;
        Vector3 _slideDir;

        // Wall run state (first-pass)
        bool _wallRunning;
        float _wallRunTimer;
        Vector3 _wallNormal;
        Collider _wallCollider;
        float _sameWallCd;
        int _wallsSinceGround;
        float _oppositeHold;

        // Vault (stub/first-pass)
        bool _vaulting;
        float _vaultTimer;
        float _vaultLock;
        Vector3 _vaultVel;

        // Air dodge (Systems Tag v1)
        int _airDodgeChargesLeft;
        float _airDodgeBufferTimer;
        float _airDodgeLockTimer;
        float _airDodgeIFrameTimer;
        float _airDodgeGroundTravel;
        int _airDodgeGroundSteps;
        Vector3 _lastGroundPos;
        bool _wasGrounded;

        // External freeze (ragdoll)
        bool _motorLocked;

        // Punch integration
        float _punchMoveScale = 1f;
        bool _slideBlocked;

        public bool IsGrounded => _cc != null && _cc.isGrounded;
        public bool IsSliding => _sliding;
        public bool IsWallRunning => _wallRunning;
        public bool IsVaulting => _vaulting;
        public bool IsMotorLocked => _motorLocked;
        public bool IsAirDodgeLocked => _airDodgeLockTimer > 0f;
        /// <summary>True while air-dodge punch i-frames are active (PunchHitbox should ignore).</summary>
        public bool HasAirDodgeIFrames => _airDodgeIFrameTimer > 0f;
        public Vector3 Velocity => _velocity;
        public float HorizontalSpeed => new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
        public MovementTuning Tuning => tuning;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
            _punch = GetComponent<PunchHitbox>();
            _ragdoll = GetComponent<PlayerRagdoll>();
            if (tuning == null)
                tuning = MovementTuning.CreateRuntimeDefaults();

            _standHeight = _cc.height > 0.1f ? _cc.height : tuning.standHeight;
            _standCenterY = _cc.center.y;
            _yaw = transform.eulerAngles.y;
            _airDodgeChargesLeft = Mathf.Max(1, tuning.airDodgeCharges);
            _lastGroundPos = transform.position;

            if (cameraPivot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraPivot = cam.transform;
            }
        }

        void Update()
        {
            if (_motorLocked || tuning == null) return;

            float dt = Time.deltaTime;
            TickTimers(dt);
            ReadLook(dt);
            TickAirDodgeBuffer(dt);
            TickAirDodgeRecharge(dt);

            if (_vaulting)
            {
                TickVault(dt);
                return;
            }

            if (_wallRunning)
            {
                TickWallRun(dt);
                // wall-run attached: air dodge gated off
                return;
            }

            if (_sliding)
            {
                TickSlide(dt);
                return;
            }

            if (_airDodgeLockTimer > 0f)
            {
                TickAirDodgeLock(dt);
                return;
            }

            TickLocomotion(dt);
            TryStartSlide();
            TryAttachWallRun();
            TryVault();
            TryAirDodge();
        }

        void TickTimers(float dt)
        {
            if (IsGrounded)
            {
                _coyoteTimer = tuning.coyoteTime;
                _wallsSinceGround = 0;
            }
            else
                _coyoteTimer -= dt;

            if (_input != null && _input.JumpPressed)
                _jumpBufferTimer = tuning.jumpBuffer;
            else
                _jumpBufferTimer -= dt;

            if (_hardLandTimer > 0f) _hardLandTimer -= dt;
            if (_slideCooldownTimer > 0f) _slideCooldownTimer -= dt;
            if (_sameWallCd > 0f) _sameWallCd -= dt;
            if (_airDodgeLockTimer > 0f) _airDodgeLockTimer -= dt;
            if (_airDodgeIFrameTimer > 0f) _airDodgeIFrameTimer -= dt;

            if (_speedBoostTimer > 0f)
            {
                _speedBoostTimer -= dt;
                if (_speedBoostTimer <= 0f) _speedMul = 1f;
            }
        }

        void TickAirDodgeBuffer(float dt)
        {
            if (_input != null && _input.AirDodgePressed)
                _airDodgeBufferTimer = tuning.airDodgeBuffer;
            else
                _airDodgeBufferTimer = Mathf.Max(0f, _airDodgeBufferTimer - dt);
        }

        void TickAirDodgeRecharge(float dt)
        {
            bool grounded = IsGrounded;
            if (grounded)
            {
                if (!_wasGrounded)
                {
                    _lastGroundPos = transform.position;
                    _airDodgeGroundSteps++;
                }

                Vector3 flat = transform.position - _lastGroundPos;
                flat.y = 0f;
                float moved = flat.magnitude;
                if (moved > 0.001f)
                {
                    _airDodgeGroundTravel += moved;
                    _lastGroundPos = transform.position;
                }

                int maxCharges = Mathf.Max(1, tuning.airDodgeCharges);
                if (_airDodgeChargesLeft < maxCharges)
                {
                    // Prefer travel fallback (Systems Tag v1: 1.8 m); steps as secondary.
                    bool travelReady = _airDodgeGroundTravel >= tuning.airDodgeRechargeTravel;
                    bool stepsReady = _airDodgeGroundSteps >= tuning.airDodgeRechargeSteps;
                    if (travelReady || stepsReady)
                    {
                        _airDodgeChargesLeft = maxCharges;
                        _airDodgeGroundTravel = 0f;
                        _airDodgeGroundSteps = 0;
                    }
                }
            }
            else if (_wasGrounded)
            {
                // left ground — keep travel progress until recharge fires next ground stint
            }

            _wasGrounded = grounded;
        }

        void ReadLook(float dt)
        {
            if (_input == null) return;
            Vector2 look = _input.LookDelta;
            _yaw += look.x * lookSensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y * lookSensitivity, pitchMin, pitchMax);
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraPivot != null)
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        void TickLocomotion(float dt)
        {
            Vector2 move = _input != null ? _input.Move : Vector2.zero;
            _sprintHeld = _input != null && _input.SprintHeld && move.sqrMagnitude > 0.01f;

            float targetSpeed = (_sprintHeld ? tuning.sprintSpeed : tuning.walkSpeed) * _speedMul * _punchMoveScale;
            if (_hardLandTimer > 0f)
                targetSpeed *= (1f - tuning.hardLandHorizPenalty);

            Vector3 wish = transform.right * move.x + transform.forward * move.y;
            if (wish.sqrMagnitude > 1f) wish.Normalize();

            Vector3 horiz = new Vector3(_velocity.x, 0f, _velocity.z);
            float accel = IsGrounded
                ? (wish.sqrMagnitude > 0.01f ? (1f / Mathf.Max(0.01f, tuning.accelTime)) : (1f / Mathf.Max(0.01f, tuning.brakeTime)))
                : (1f / Mathf.Max(0.01f, tuning.accelTime)) * tuning.airControlPercent;

            Vector3 desired = wish * targetSpeed;

            // Airborne: preserve carried planar momentum (Systems: airMomentumPreserve).
            if (!IsGrounded && wish.sqrMagnitude > 0.01f)
            {
                float floor = Mathf.Max(desired.magnitude, horiz.magnitude * tuning.airMomentumPreserve);
                if (desired.sqrMagnitude > 0.01f)
                    desired = desired.normalized * floor;
            }

            horiz = Vector3.MoveTowards(horiz, desired, Mathf.Max(targetSpeed, 0.01f) * accel * dt);

            float turnRate = _sprintHeld ? tuning.turnRateSprint : tuning.turnRateWalk;
            if (wish.sqrMagnitude > 0.01f && IsGrounded)
            {
                horiz = Vector3.RotateTowards(horiz, desired.sqrMagnitude > 0.01f ? desired : horiz,
                    turnRate * Mathf.Deg2Rad * dt, 0f);
            }

            _velocity.x = horiz.x;
            _velocity.z = horiz.z;

            if (IsGrounded && _velocity.y < 0f)
                _velocity.y = -2f;

            bool canJump = _coyoteTimer > 0f && _jumpBufferTimer > 0f;
            if (canJump)
            {
                ApplyJumpTakeoff();
            }
            else if (!IsGrounded)
            {
                _velocity.y -= tuning.gravity * dt;
            }

            // Hard-land detect: fall > 1.5× jump apex → ×0.85 horiz for 0.1s; never zero
            Vector3 before = transform.position;
            float vyBefore = _velocity.y;
            CollisionFlags flags = _cc.Move(_velocity * dt);
            if ((flags & CollisionFlags.Below) != 0 && vyBefore < 0f)
            {
                float hardThresh = tuning.jumpApexHeight * tuning.hardLandFallMultiple;
                float hardSpeed = Mathf.Sqrt(2f * tuning.gravity * hardThresh);
                if (-vyBefore > hardSpeed * 0.85f)
                {
                    _hardLandTimer = tuning.hardLandPenaltyDuration;
                    float keep = 1f - tuning.hardLandHorizPenalty; // 0.85
                    _velocity.x *= keep;
                    _velocity.z *= keep;
                }
                _velocity.y = -2f;
            }
        }

        void ApplyJumpTakeoff()
        {
            float launch = tuning.jumpLaunchSpeed;
            float derived = Mathf.Sqrt(2f * tuning.gravity * tuning.jumpApexHeight);
            if (Mathf.Abs(launch - derived) > 0.5f)
                launch = derived;
            _velocity.y = launch;

            // Systems Tag v1: JumpHorizRetain on ALL takeoffs (not just sprint).
            Vector3 h = new Vector3(_velocity.x, 0f, _velocity.z);
            h *= tuning.jumpHorizRetain;
            if (_sprintHeld)
                h *= tuning.sprintJumpHorizRetain;
            _velocity.x = h.x;
            _velocity.z = h.z;

            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }

        void TryStartSlide()
        {
            if (_slideBlocked) return;
            if (_input == null || !_input.SlidePressed) return;
            if (!IsGrounded || _sliding || _slideCooldownTimer > 0f) return;
            if (HorizontalSpeed < tuning.slideSpeedGate) return;

            _sliding = true;
            _slideTimer = tuning.slideDuration;
            _slideDir = new Vector3(_velocity.x, 0f, _velocity.z).normalized;
            if (_slideDir.sqrMagnitude < 0.01f)
                _slideDir = transform.forward;

            // Systems: SlideEnterWipe = false → keep current horiz (gate still ≥5.5).
            if (tuning.slideEnterWipe)
            {
                float spd = tuning.slideSpeedGate;
                _velocity.x = _slideDir.x * spd;
                _velocity.z = _slideDir.z * spd;
            }

            _cc.height = tuning.slideHeight;
            _cc.center = new Vector3(0f, tuning.slideHeight * 0.5f, 0f);
        }

        void TickSlide(float dt)
        {
            _slideTimer -= dt;
            float t = 1f - Mathf.Clamp01(_slideTimer / tuning.slideDuration);
            float startSpd = Mathf.Max(HorizontalSpeed, tuning.slideSpeedGate);
            float endSpd = startSpd * tuning.slideEndSpeedPercent;
            float spd = t < (tuning.slidePunchDuration / tuning.slideDuration)
                ? startSpd
                : Mathf.Lerp(startSpd, endSpd, t);

            _velocity = _slideDir * spd;
            _velocity.y = -2f;
            _cc.Move(_velocity * dt);

            if (_input != null && _input.JumpPressed)
            {
                ExitSlide(toSprint: true);
                _velocity = _slideDir * (spd * (1f + tuning.slideJumpHorizBonus));
                _velocity.y = Mathf.Sqrt(2f * tuning.gravity * tuning.jumpApexHeight);
                // retain on slide-exit jump
                Vector3 h = new Vector3(_velocity.x, 0f, _velocity.z);
                h *= tuning.jumpHorizRetain;
                _velocity.x = h.x;
                _velocity.z = h.z;
                _coyoteTimer = 0f;
                _jumpBufferTimer = 0f;
                return;
            }

            if (_slideTimer <= 0f || !IsGrounded)
                ExitSlide(toSprint: true);
        }

        void ExitSlide(bool toSprint)
        {
            _sliding = false;
            _slideCooldownTimer = tuning.slideCooldown;
            _cc.height = _standHeight;
            _cc.center = new Vector3(0f, _standCenterY, 0f);
            if (toSprint)
                _sprintHeld = true;
        }

        void TryAttachWallRun()
        {
            // TODO: full face/vel angle gates, same-wall CD, chain cap 6.2 attach.
            if (IsGrounded || _wallRunning || HorizontalSpeed < tuning.wallRunAttachSpeed) return;
            if (_wallsSinceGround >= tuning.wallChainCap && HorizontalSpeed < tuning.wallChainAttachMinAfterCap)
                return;

            Vector3 origin = transform.position + Vector3.up * (_cc.height * 0.5f);
            float probe = _cc.radius + 0.35f;
            if (Physics.Raycast(origin, transform.right, out RaycastHit rh, probe) ||
                Physics.Raycast(origin, -transform.right, out rh, probe))
            {
                if (_sameWallCd > 0f && rh.collider == _wallCollider) return;

                float faceAng = Vector3.Angle(-rh.normal, transform.forward);
                if (faceAng > tuning.wallRunFaceAngleMax) return;

                _wallRunning = true;
                _wallRunTimer = tuning.wallRunMaxDuration;
                _wallNormal = rh.normal;
                _wallCollider = rh.collider;
                _wallsSinceGround++;
                _velocity.y = Mathf.Min(_velocity.y, 0f);
            }
        }

        void TickWallRun(float dt)
        {
            // TODO: clamp to sprint, opposite-input detach >100ms, proper along-wall velocity.
            _wallRunTimer -= dt;
            Vector3 along = Vector3.Cross(_wallNormal, Vector3.up).normalized;
            if (Vector3.Dot(along, transform.forward) < 0f) along = -along;

            float spd = Mathf.Min(HorizontalSpeed, tuning.sprintSpeed);
            if (spd < 0.1f) spd = tuning.sprintSpeed * 0.85f;

            _velocity = along * spd;
            _velocity.y -= tuning.gravity * tuning.wallRunGravityScale * dt;
            _cc.Move(_velocity * dt);

            _cc.Move(-_wallNormal * 2f * dt);

            Vector2 move = _input != null ? _input.Move : Vector2.zero;
            Vector3 away = _wallNormal;
            float awayInput = Vector3.Dot(transform.right * move.x + transform.forward * move.y, away);
            if (awayInput > 0.4f) _oppositeHold += dt;
            else _oppositeHold = 0f;

            bool jump = _input != null && _input.JumpPressed;
            if (jump)
            {
                DoWallJump();
                return;
            }

            if (_wallRunTimer <= 0f || spd < tuning.wallRunMinSpeed || _oppositeHold >= tuning.wallRunDetachOppositeHold)
                DetachWallRun();
        }

        void DoWallJump()
        {
            // wall jump: out 6.5 + up 5.5, steer ±20%, along 50% — carries speed into follow-up jump/slide
            Vector3 along = Vector3.Cross(_wallNormal, Vector3.up).normalized;
            if (Vector3.Dot(along, transform.forward) < 0f) along = -along;

            Vector2 move = _input != null ? _input.Move : Vector2.zero;
            Vector3 steer = (transform.right * move.x + transform.forward * move.y);
            steer.y = 0f;
            if (steer.sqrMagnitude > 1f) steer.Normalize();

            Vector3 outDir = (_wallNormal + along * tuning.wallJumpAlongPercent).normalized;
            outDir = (outDir + steer * tuning.wallJumpSteerPercent).normalized;

            _velocity = outDir * tuning.wallJumpOutSpeed;
            _velocity.y = tuning.wallJumpUpSpeed;

            // TODO: falloff ×0.85 within 0.8s floor ×0.55
            DetachWallRun(clearSameWallCd: false);
            _sameWallCd = 0f;
            _jumpBufferTimer = 0f;
        }

        void DetachWallRun(bool clearSameWallCd = true)
        {
            _wallRunning = false;
            _oppositeHold = 0f;
            if (clearSameWallCd)
                _sameWallCd = tuning.sameWallCooldown;
        }

        void TryVault()
        {
            // TODO: cone auto-detect 35°, low/high bands, lip jump last 30% of lock.
            if (_input == null || !_input.JumpPressed) return;
            if (IsGrounded == false && !_sliding) return;
            Vector3 origin = transform.position + Vector3.up * 0.3f;
            if (!Physics.Raycast(origin, transform.forward, out RaycastHit hit, 1.2f)) return;
            float h = hit.collider.bounds.max.y - transform.position.y;
            bool low = h >= tuning.vaultLowMin && h <= tuning.vaultLowMax && HorizontalSpeed >= tuning.vaultLowSpeedGate;
            bool high = h > tuning.vaultLowMax && h <= tuning.vaultHighMax && HorizontalSpeed >= tuning.vaultHighSpeedGate;
            if (!low && !high) return;

            _vaulting = true;
            _vaultLock = low ? tuning.vaultLowLock : tuning.vaultHighLock;
            _vaultTimer = _vaultLock;
            float retain = low ? tuning.vaultLowRetain : tuning.vaultHighRetain;
            Vector3 dir = transform.forward;
            _vaultVel = dir * (HorizontalSpeed * retain);
            _vaultVel.y = (h + 0.2f) / Mathf.Max(0.05f, _vaultLock);
            _jumpBufferTimer = 0f;
        }

        void TickVault(float dt)
        {
            _vaultTimer -= dt;
            _cc.Move(_vaultVel * dt);
            float remaining = _vaultTimer / Mathf.Max(0.01f, _vaultLock);
            if (remaining <= tuning.vaultLipJumpWindow && _input != null && _input.JumpPressed)
            {
                // Vault exit may chain into jump at carried speed
                _vaulting = false;
                _velocity = _vaultVel;
                _velocity.y = Mathf.Sqrt(2f * tuning.gravity * tuning.jumpApexHeight * 0.6f);
                Vector3 h = new Vector3(_velocity.x, 0f, _velocity.z);
                h *= tuning.jumpHorizRetain;
                _velocity.x = h.x;
                _velocity.z = h.z;
                return;
            }
            if (_vaultTimer <= 0f)
            {
                _vaulting = false;
                _velocity = _vaultVel;
                _velocity.y = 0f;
            }
        }

        bool CanAirDodge()
        {
            if (_airDodgeBufferTimer <= 0f) return false;
            if (_airDodgeChargesLeft <= 0) return false;
            if (IsGrounded) return false;
            if (_wallRunning) return false;
            if (_vaulting) return false;
            if (_motorLocked) return false;
            if (_ragdoll != null && _ragdoll.IsRagdolling) return false;
            if (_airDodgeLockTimer > 0f) return false;

            // Block during punch windup / active / miss-recover (HitRecover allowed).
            if (_punch != null)
            {
                var phase = _punch.Phase;
                if (phase == PunchPhase.Windup
                    || phase == PunchPhase.Active
                    || phase == PunchPhase.MissRecover)
                    return false;
            }

            return true;
        }

        void TryAirDodge()
        {
            if (!CanAirDodge()) return;

            Vector2 move = _input != null ? _input.Move : Vector2.zero;
            Vector3 dir = transform.right * move.x + transform.forward * move.y;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                dir = transform.forward;
            else
                dir.Normalize();

            float keepY = _velocity.y;
            float dodgeSpeed = tuning.airDodgeSpeed;
            // Soft clamp effective distance (speed × lock) to airDodgeMaxDistance when > 0.
            if (tuning.airDodgeMaxDistance > 0f && tuning.airDodgeLock > 0.0001f)
            {
                float maxSpeed = tuning.airDodgeMaxDistance / tuning.airDodgeLock;
                if (dodgeSpeed > maxSpeed)
                    dodgeSpeed = maxSpeed;
            }
            _velocity = dir * dodgeSpeed;
            _velocity.y = keepY;

            _airDodgeChargesLeft = Mathf.Max(0, _airDodgeChargesLeft - 1);
            _airDodgeBufferTimer = 0f;
            _airDodgeLockTimer = tuning.airDodgeLock;
            _airDodgeIFrameTimer = tuning.airDodgeIFrames;
            _airDodgeGroundTravel = 0f;
            _airDodgeGroundSteps = 0;
        }

        void TickAirDodgeLock(float dt)
        {
            // No air control during lock — gravity + carry dodge velocity only.
            if (!IsGrounded)
                _velocity.y -= tuning.gravity * dt;
            else if (_velocity.y < 0f)
                _velocity.y = -2f;

            CollisionFlags flags = _cc.Move(_velocity * dt);
            if ((flags & CollisionFlags.Below) != 0 && _velocity.y < 0f)
                _velocity.y = -2f;

            // Still accept buffered dodge only after lock ends (handled next frame).
        }

        public void ApplySpeedBoost(float percent, float duration)
        {
            _speedMul = 1f + percent;
            _speedBoostTimer = duration;
        }

        public void ClearSpeedBoost()
        {
            _speedMul = 1f;
            _speedBoostTimer = 0f;
        }

        public void SetPunchMoveScale(float scale)
        {
            _punchMoveScale = Mathf.Clamp(scale, 0f, 1.5f);
        }

        public void SetSlideBlocked(bool blocked)
        {
            _slideBlocked = blocked;
            if (blocked && _sliding)
            {
                _sliding = false;
                _cc.height = _standHeight;
                _cc.center = new Vector3(0f, _standCenterY, 0f);
                _slideCooldownTimer = tuning != null ? tuning.slideCooldown : 0.08f;
            }
        }

        public void SetMotorLocked(bool locked)
        {
            _motorLocked = locked;
            if (locked)
            {
                _velocity = Vector3.zero;
                _sliding = false;
                _wallRunning = false;
                _vaulting = false;
                _airDodgeLockTimer = 0f;
                _airDodgeIFrameTimer = 0f;
            }
        }

        /// <summary>Kinematic stun proxy if ragdoll component missing.</summary>
        public void BeginStunProxy(float duration, Vector3 knock)
        {
            StopCoroutine(nameof(StunProxyRoutine));
            StartCoroutine(StunProxyRoutine(duration, knock));
        }

        System.Collections.IEnumerator StunProxyRoutine(float duration, Vector3 knock)
        {
            SetMotorLocked(true);
            if (_cc != null) _cc.enabled = true;
            _velocity = knock;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                if (_cc != null && _cc.enabled)
                {
                    _velocity.y -= (tuning != null ? tuning.gravity : 28f) * Time.deltaTime;
                    _cc.Move(_velocity * Time.deltaTime);
                    Vector3 h = new Vector3(_velocity.x, 0f, _velocity.z);
                    h = Vector3.MoveTowards(h, Vector3.zero, 6f * Time.deltaTime);
                    _velocity.x = h.x;
                    _velocity.z = h.z;
                }
                yield return null;
            }
            _velocity = Vector3.zero;
            SetMotorLocked(false);
        }

        public void SetTuning(MovementTuning t)
        {
            if (t != null)
            {
                tuning = t;
                _airDodgeChargesLeft = Mathf.Max(1, tuning.airDodgeCharges);
            }
        }
    }
}
