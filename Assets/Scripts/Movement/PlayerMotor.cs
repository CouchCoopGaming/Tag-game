using UnityEngine;
using Tag.Input;

namespace Tag.Movement
{
    /// <summary>
    /// CharacterController motor: walk/sprint/jump/coyote/buffer/air control are live.
    /// Slide, wall run, wall jump, vault have first-pass / stub implementations (see TODOs).
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

        // External freeze (ragdoll)
        bool _motorLocked;

        // Punch integration
        float _punchMoveScale = 1f;
        bool _slideBlocked;

        public bool IsGrounded => _cc != null && _cc.isGrounded;
        public bool IsSliding => _sliding;
        public bool IsWallRunning => _wallRunning;
        public bool IsMotorLocked => _motorLocked;
        public Vector3 Velocity => _velocity;
        public float HorizontalSpeed => new Vector3(_velocity.x, 0f, _velocity.z).magnitude;
        public MovementTuning Tuning => tuning;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _input = GetComponent<PlayerInputReader>();
            if (tuning == null)
                tuning = MovementTuning.CreateRuntimeDefaults();

            _standHeight = _cc.height > 0.1f ? _cc.height : tuning.standHeight;
            _standCenterY = _cc.center.y;
            _yaw = transform.eulerAngles.y;

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

            if (_vaulting)
            {
                TickVault(dt);
                return;
            }

            if (_wallRunning)
            {
                TickWallRun(dt);
                return;
            }

            if (_sliding)
            {
                TickSlide(dt);
                return;
            }

            TickLocomotion(dt);
            TryStartSlide();
            TryAttachWallRun();
            TryVault();
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

            if (_speedBoostTimer > 0f)
            {
                _speedBoostTimer -= dt;
                if (_speedBoostTimer <= 0f) _speedMul = 1f;
            }
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
            horiz = Vector3.MoveTowards(horiz, desired, targetSpeed * accel * dt);

            // Facing turn rate (cosmetic yaw already from look; this clamps velocity turn feel)
            float turnRate = _sprintHeld ? tuning.turnRateSprint : tuning.turnRateWalk;
            if (wish.sqrMagnitude > 0.01f && IsGrounded)
            {
                Quaternion to = Quaternion.LookRotation(wish, Vector3.up);
                // yaw already camera-driven; keep velocity aligned
                horiz = Vector3.RotateTowards(horiz, desired.sqrMagnitude > 0.01f ? desired : horiz,
                    turnRate * Mathf.Deg2Rad * dt, 0f);
            }

            _velocity.x = horiz.x;
            _velocity.z = horiz.z;

            // Gravity + jump
            if (IsGrounded && _velocity.y < 0f)
                _velocity.y = -2f;

            bool canJump = _coyoteTimer > 0f && _jumpBufferTimer > 0f;
            if (canJump)
            {
                float launch = tuning.jumpLaunchSpeed;
                // Prefer apex-derived if launch unset mismatch: v = sqrt(2gh)
                float derived = Mathf.Sqrt(2f * tuning.gravity * tuning.jumpApexHeight);
                if (Mathf.Abs(launch - derived) > 0.5f)
                    launch = derived;
                _velocity.y = launch;

                if (_sprintHeld)
                {
                    Vector3 h = new Vector3(_velocity.x, 0f, _velocity.z);
                    h *= tuning.sprintJumpHorizRetain;
                    _velocity.x = h.x;
                    _velocity.z = h.z;
                }

                _coyoteTimer = 0f;
                _jumpBufferTimer = 0f;
            }
            else if (!IsGrounded)
            {
                _velocity.y -= tuning.gravity * dt;
            }

            // Hard-land detect
            Vector3 before = transform.position;
            CollisionFlags flags = _cc.Move(_velocity * dt);
            if ((flags & CollisionFlags.Below) != 0 && _velocity.y < 0f)
            {
                float fallDist = before.y - transform.position.y;
                // approximate via velocity peak: if descending hard
                float hardThresh = tuning.jumpApexHeight * tuning.hardLandFallMultiple;
                if (-_velocity.y > Mathf.Sqrt(2f * tuning.gravity * hardThresh) * 0.85f)
                    _hardLandTimer = tuning.hardLandPenaltyDuration;
                _velocity.y = -2f;
            }
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

            _cc.height = tuning.slideHeight;
            _cc.center = new Vector3(0f, tuning.slideHeight * 0.5f, 0f);
        }

        void TickSlide(float dt)
        {
            _slideTimer -= dt;
            float t = 1f - Mathf.Clamp01(_slideTimer / tuning.slideDuration);
            float startSpd = Mathf.Max(HorizontalSpeed, tuning.slideSpeedGate);
            // punch window keeps speed, then decay to end percent
            float endSpd = startSpd * tuning.slideEndSpeedPercent;
            float spd = t < (tuning.slidePunchDuration / tuning.slideDuration)
                ? startSpd
                : Mathf.Lerp(startSpd, endSpd, t);

            _velocity = _slideDir * spd;
            _velocity.y = -2f;
            _cc.Move(_velocity * dt);

            // Jump-from-slide
            if (_input != null && _input.JumpPressed)
            {
                ExitSlide(toSprint: true);
                _velocity = _slideDir * (spd * (1f + tuning.slideJumpHorizBonus));
                _velocity.y = Mathf.Sqrt(2f * tuning.gravity * tuning.jumpApexHeight);
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
                // loose first-pass gate
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

            // stick to wall lightly
            _cc.Move(-_wallNormal * 2f * dt);

            Vector2 move = _input != null ? _input.Move : Vector2.zero;
            // opposite = away from wall
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
            // wall jump: out 6.5 + up 5.5, steer ±20%, along 50%
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
            _sameWallCd = 0f; // opposite OK after wall jump
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
            if (IsGrounded == false && !_sliding) return; // first-pass: only ground/slide approach
            // Placeholder: short forward obstacle ray
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
            // lip jump window
            float remaining = _vaultTimer / Mathf.Max(0.01f, _vaultLock);
            if (remaining <= tuning.vaultLipJumpWindow && _input != null && _input.JumpPressed)
            {
                _vaulting = false;
                _velocity = _vaultVel;
                _velocity.y = Mathf.Sqrt(2f * tuning.gravity * tuning.jumpApexHeight * 0.6f);
                return;
            }
            if (_vaultTimer <= 0f)
            {
                _vaulting = false;
                _velocity = _vaultVel;
                _velocity.y = 0f;
            }
        }

        public void ApplySpeedBoost(float percent, float duration)
        {
            // No stack: set absolute multiplier; refresh timer on hit
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
            if (t != null) tuning = t;
        }
    }
}
