using UnityEngine;
using Tag.Audio;

namespace TagArena.Movement
{
    /// <summary>
    /// Rigidbody motor: Apex parkour grafted onto Tribes skiing.
    /// Velocity is sacred. States decorate velocity; they do not overwrite it
    /// except for authored cinematic windows (mantle warp, super-glide launch).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(SurfaceProbe))]
    public class PlayerMotor : MonoBehaviour
    {
        public MovementConfig cfg;
        public Transform cam;
        public Animator animator;
        public TagRole tagRole;

        public MoveState State { get; private set; } = MoveState.Idle;
        public Vector3 Velocity => _rb.linearVelocity;
        public float Energy { get; private set; }
        public float HorizSpeed => WishAccel.HorizSpeed(_rb.linearVelocity);
        public GroundInfo Ground => _probe.Ground;
        public bool Skiing { get; private set; }
        public bool Jetting { get; private set; }
        public float ClimbHeightUsed { get; private set; }
        public float SuperGlideT { get; private set; } = -1f;
        public Vector3 WallNormal => _probe.Wall.normal;
        public bool WallLeft => _probe.Wall.left;
        public bool IsMotorLocked => _motorLocked;
        public bool IsSliding => State == MoveState.Slide;
        public bool IsWallRunning => State == MoveState.WallRun;
        public bool IsVaulting => State == MoveState.Mantle;
        public bool IsAirDodgeLocked => State == MoveState.Jet && Jetting;
        public bool IsAirDashing => _airDashT > 0f;
        public bool HasAirDodgeIFrames => IsAirDodgeLocked || _airDashIFramesT > 0f;
        /// <summary>1 at air-dash start, 0 at end (TP whip).</summary>
        public float AirDashProgress =>
            _airDashT > 0f && cfg != null
                ? Mathf.Clamp01(_airDashT / Mathf.Max(0.01f, cfg.airDashDuration))
                : 0f;
        /// <summary>Seconds left before air dash is usable again (0 = ready).</summary>
        public float AirDashCooldownRemaining => Mathf.Max(0f, _airDashCd);
        public bool IsGrounded => _probe != null && _probe.Ground.grounded;
        public float HorizontalSpeed => HorizSpeed;
        public bool IsLunging => _lungeT > 0f;
        /// <summary>1 at lunge start, 0 at end (TP whip->settle).</summary>
        public float LungeProgress =>
            _lungeT > 0f && cfg != null
                ? Mathf.Clamp01(_lungeT / Mathf.Max(0.01f, cfg.taggerLungeDuration))
                : 0f;
        /// <summary>0 at mantle start, 1 at finish (TP pull-up then plant).</summary>
        public float MantleProgress =>
            State == MoveState.Mantle && cfg != null
                ? Mathf.Clamp01(_mantleT / Mathf.Max(0.01f, cfg.mantleDuration))
                : 0f;
        public float SprintSpeed => cfg != null ? cfg.sprintSpeed : 12f;
        /// <summary>Downward speed (m/s) latched on the most recent ground contact.</summary>
        public float LastLandImpactSpeed => _lastLandImpactSpeed;

        Rigidbody _rb;
        CapsuleCollider _cap;
        PlayerInputReader _in;
        SurfaceProbe _probe;

        float _height;
        float _coyote;
        float _jumpBuf;
        float _lastLanded;
        float _slideT;
        float _slideStartSpeed;
        float _climbT;
        float _climbStartY;
        float _wallRunT;
        float _wallContactLostT;
        bool _wallRunBlocked;
        bool _climbBlocked;
        const float WallReattachDelay = 0.15f;
        float _mantleT;
        Vector3 _mantleFrom;
        Vector3 _mantleTo;
        Vector3 _mantleFwd;
        float _energyRegenDelay;
        float _tapCd;
        float _lungeCd;
        float _lungeT;
        float _airDashT;
        float _airDashIFramesT;
        float _airDashCd;
        Vector3 _airDashDir;
        float _landStunT;
        float _lastLandImpactSpeed;
        bool _wasProbeGrounded = true;
        bool _jumpFatigued;
        int _airJumpsFromFatigue;
        bool _motorLocked;
        bool _slideBlocked;
        float _punchMoveScale = 1f;
        float _speedBoostMul = 1f;
        float _speedBoostT;

        public event System.Action<MoveState, MoveState> OnStateChanged;
        public event System.Action OnJumped;
        public event System.Action OnSlid;
        public event System.Action OnAirDashed;
        public event System.Action OnWallBounced;
        public event System.Action OnSuperGlide;
        public event System.Action OnMantle;
        public event System.Action OnTaggedSomeone;
        public event System.Action OnBecameIt;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _cap = GetComponent<CapsuleCollider>();
            _in = GetComponent<PlayerInputReader>();
            _probe = GetComponent<SurfaceProbe>();

            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.useGravity = false;
            _rb.mass = 80f;

            if (cfg != null)
            {
                _height = cfg.standingHeight;
                Energy = cfg.jetEnergyMax;
                ApplyCapsule();
                _probe.Init(cfg, transform, _cap);
            }
        }

        void Start()
        {
            if (cfg != null && _probe != null && _height < 0.1f)
            {
                _height = cfg.standingHeight;
                Energy = cfg.jetEnergyMax;
                ApplyCapsule();
                _probe.Init(cfg, transform, _cap);
            }
        }

        void Update()
        {
            if (_in == null || cfg == null) return;
            _in.Read();
            if (_in.JumpPressed) _jumpBuf = cfg.jumpBuffer;
        }

        void FixedUpdate()
        {
            if (cfg == null) return;
            if (_motorLocked) return;
            float dt = Time.fixedDeltaTime;
            TickTimers(dt);

            _probe.Refresh(_height, _rb.linearVelocity);
            if (_probe.Ground.grounded) _coyote = cfg.coyoteTime;
            LatchLandImpact();
            TickWallContactGates(dt);

            Vector3 wish = WishAccel.CameraWish(cam ? cam : transform, _in.Move);
            Vector3 v = _rb.linearVelocity;

            bool grounded = _probe.Ground.grounded && State != MoveState.Mantle && State != MoveState.WallClimb && State != MoveState.WallRun;

            TickEnergy(dt);
            TickHeight(dt);

            switch (State)
            {
                case MoveState.Mantle:
                    v = TickMantle(dt, v);
                    break;
                case MoveState.WallClimb:
                    v = TickClimb(dt, v, wish);
                    break;
                case MoveState.WallRun:
                    v = TickWallRun(dt, v, wish);
                    break;
                case MoveState.LandStun:
                    v = TickLandStun(dt, v);
                    break;
                default:
                    v = TickLocomotion(dt, v, wish, grounded);
                    break;
            }

            v = ClampAndDrag(v, dt);
            // Punch speed buff is walk/sprint. Never multiply a slide Ã¢â‚¬â€ entry speed only decays.
            if (_speedBoostMul > 1.001f && State != MoveState.LandStun && State != MoveState.Slide)
            {
                Vector3 hv = WishAccel.Horizontal(v) * _speedBoostMul;
                v = WishAccel.SetHoriz(v, hv);
            }
            if (_punchMoveScale < 0.999f)
            {
                Vector3 hv = WishAccel.Horizontal(v) * _punchMoveScale;
                v = WishAccel.SetHoriz(v, hv);
            }
            if (State == MoveState.Slide)
            {
                Vector3 sh = WishAccel.Horizontal(v);
                float scap = Mathf.Max(0.01f, _slideStartSpeed);
                if (sh.magnitude > scap)
                    v = WishAccel.SetHoriz(v, sh * (scap / sh.magnitude));
            }
            if (_slideBlocked && State == MoveState.Slide)
                SetState(MoveState.Crouch);
            _rb.linearVelocity = v;

            if (tagRole != null && tagRole.IsIt)
                TryTag();

            DriveAnimator();
        }

        Vector3 TickLocomotion(float dt, Vector3 v, Vector3 wish, bool grounded)
        {
            Skiing = WantsSki(grounded);
            Jetting = WantsJet();

            if (TryEnterClimb(v, grounded)) return _rb.linearVelocity;
            if (TryEnterWallRun(v, grounded, wish)) return _rb.linearVelocity;
            if (TryAirDash(ref v, wish, dt, grounded)) return v;
            if (TryLunge(ref v, wish, dt)) return v;

            if (grounded && !Skiing)
                v = GroundMove(dt, v, wish);
            else if (Skiing && _probe.Ground.grounded)
                v = SkiMove(dt, v, wish);
            else
                v = AirMove(dt, v, wish);

            if (Jetting)
                v = ApplyJet(dt, v, wish);

            TryJump(ref v, grounded);
            UpdateLocomotionState(grounded, v);
            return v;
        }

        #region Ground / Slide / Crouch

        Vector3 GroundMove(float dt, Vector3 v, Vector3 wish)
        {
            bool sliding = State == MoveState.Slide;
            bool wantCrouch = _in.CrouchHeld;
            float hSpeed = WishAccel.HorizSpeed(v);

            if (!sliding && wantCrouch && hSpeed >= cfg.slideEntrySpeed && _probe.Ground.walkable)
            {
                EnterSlide(ref v);
                sliding = true;
            }

            if (sliding)
                return SlideMove(dt, v, wish);

            Vector3 hv = WishAccel.Horizontal(v);
            float max = wantCrouch ? cfg.crouchSpeed
                      : (_in.SprintHeld || _in.Move.y > 0.4f) ? cfg.sprintSpeed
                      : cfg.walkSpeed;

            if (tagRole != null && tagRole.IsIt)
                max += cfg.taggerSprintBonus;

            if (wish.sqrMagnitude > 0.01f)
                hv = WishAccel.Accelerate(hv, wish, max, cfg.groundAccel / Mathf.Max(max, 1f), dt);
            else
                hv = WishAccel.Friction(hv, cfg.groundDecel / Mathf.Max(hv.magnitude, 1f), dt);

            // Stick to slope without launching
            if (_probe.Ground.walkable)
            {
                Vector3 along = Vector3.ProjectOnPlane(hv, _probe.Ground.normal);
                if (along.sqrMagnitude > 0.001f)
                    along = along.normalized * hv.magnitude;
                v = new Vector3(along.x, along.y, along.z);
                if (v.y > 0.4f && _probe.Ground.slopeAngle < 12f) v.y = 0f;
            }
            else
            {
                // Too steep to walk ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â start an involuntary ski if pointing downhill
                v = WishAccel.SetHoriz(v, hv);
                v.y -= cfg.gravity * dt;
            }

            SetHeight(wantCrouch ? cfg.crouchHeight : cfg.standingHeight);
            return v;
        }

        void EnterSlide(ref Vector3 v)
        {
            // Carry existing planar speed only Ã¢â‚¬â€ no enter impulse / boost.
            Vector3 hv = WishAccel.Horizontal(v);
            if (hv.sqrMagnitude < 0.05f)
                hv = transform.forward * Mathf.Max(hv.magnitude, cfg.slideEntrySpeed * 0.85f);
            // Ignore legacy slideBoost so slides never punch speed on enter.
            v = WishAccel.SetHoriz(v, hv);
            _slideT = 0f;
            _slideStartSpeed = hv.magnitude;
            SetState(MoveState.Slide);
            SetHeight(cfg.crouchHeight);
            OnSlid?.Invoke();
        }

        Vector3 SlideMove(float dt, Vector3 v, Vector3 wish)
        {
            _slideT += dt;
            Vector3 n = _probe.Ground.grounded ? _probe.Ground.normal : Vector3.up;
            Vector3 along = Vector3.ProjectOnPlane(v, n);

            float slope = _probe.Ground.slopeAngle;
            bool downhill = _probe.Ground.fallLine.sqrMagnitude > 0f &&
                            Vector3.Dot(along, _probe.Ground.fallLine) > 0f;

            // Carry entry speed only Ã¢â‚¬â€ never accelerate above slide-entry planar speed.
            // Downhill softens friction (sustains longer) but cannot add speed.
            if (slope > 8f && !downhill)
                along = WishAccel.Friction(along, cfg.slideUphillBrake / Mathf.Max(along.magnitude, 1f), dt);
            else
            {
                float fric = (downhill && slope > 4f)
                    ? cfg.slideFlatFriction * 0.35f
                    : cfg.slideFlatFriction;
                along = WishAccel.Friction(along, fric / Mathf.Max(along.magnitude, 1f), dt);
            }

            if (wish.sqrMagnitude > 0.01f)
            {
                Vector3 steer = Vector3.ProjectOnPlane(wish, n);
                // Steer only Ã¢â‚¬â€ cap at current speed (no +1.5 boost).
                along = WishAccel.Accelerate(along, steer, along.magnitude, cfg.slideSteer / 10f, dt);
            }

            float cap = Mathf.Max(0.01f, _slideStartSpeed);
            float spd = along.magnitude;
            if (spd > cap)
                along *= cap / spd;

            v = along;

            // Slide only while crouch is held with momentum. Short commit avoids crouch-edge flicker on enter.
            float hNow = WishAccel.HorizSpeed(v);
            float commit = Mathf.Max(0.04f, cfg.slideMinDuration);
            bool inCommit = _slideT < commit;
            bool keepCrouch = _in.CrouchHeld || inCommit;
            bool keepSpeed = hNow >= cfg.slideStaySpeed || inCommit;
            if (!keepCrouch || !keepSpeed || !_probe.Ground.grounded)
            {
                if (_in.CrouchHeld && _probe.Ground.grounded) SetState(MoveState.Crouch);
                else if (_probe.Ground.grounded) SetState(hNow > cfg.walkSpeed + 0.4f ? MoveState.Sprint : MoveState.Walk);
                else SetState(MoveState.Air);
            }

            SetHeight(cfg.crouchHeight);
            return v;
        }

        #endregion

        #region Ski (Tribes)

        bool WantsSki(bool grounded)
        {
            if (!_in.SkiHeld) return false;
            if (State == MoveState.Mantle || State == MoveState.WallClimb) return false;
            // Flat / mild slope: do NOT ice-skate at jog speeds Ã¢â‚¬â€ prefer run/sprint.
            // Only allow flat ski to preserve already-high momentum (Tribes crest carry).
            if (grounded && _probe.Ground.slopeAngle < cfg.skiMinSlope)
                return HorizSpeed >= cfg.sprintSpeed * 1.15f;
            return true;
        }

        Vector3 SkiMove(float dt, Vector3 v, Vector3 wish)
        {
            Vector3 n = _probe.Ground.normal;
            // Gravity along the plane ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â this is the entire Tribes engine in one line.
            v += Physics.gravity.normalized * (cfg.gravity * cfg.skiGravityScale * dt);
            // Outward vs ground normal Ã¢â‚¬â€ crest-launch fuel (was killed by a zero factor).
            float leave = Vector3.Dot(v, n);
            // Hug the plane for friction/edging; restore leave later with a sane factor.
            Vector3 planeVel = Vector3.ProjectOnPlane(v, n);

            // Tiny friction ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¢ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€šÃ‚Â never zero, matching real T1 insight: decay by slope.
            float fric = cfg.skiFriction * Mathf.Lerp(1f, 0.25f, Mathf.InverseLerp(cfg.skiMinSlope, 50f, _probe.Ground.slopeAngle));
            planeVel = WishAccel.Friction(planeVel, fric, dt);

            // Edging: wishdir on the plane, weaker at high speed (Tribes carve).
            if (wish.sqrMagnitude > 0.01f)
            {
                Vector3 edge = Vector3.ProjectOnPlane(wish, n).normalized;
                float falloff = Mathf.Lerp(1f, 0.22f, Mathf.InverseLerp(cfg.sprintSpeed, cfg.highSpeedSteerFalloff, planeVel.magnitude));
                planeVel = WishAccel.Accelerate(planeVel, edge, planeVel.magnitude + 4f, (cfg.skiSteer * falloff) / 10f, dt);
            }

            // Crest launch: restore outward component (factor 1.0 = full Tribes preserve; threshold = skiLaunchLeaveDot).
            const float skiLaunchFactor = 1.0f;
            v = planeVel + n * Mathf.Max(0f, leave) * skiLaunchFactor;
            if (v.sqrMagnitude > 1e-6f && Vector3.Dot(v.normalized, n) > cfg.skiLaunchLeaveDot && _probe.Ground.slopeAngle > 8f)
            {
                _coyote = 0f;
                SetState(MoveState.Air);
            }
            else
            {
                v = planeVel; // stay glued when not launching
                SetState(MoveState.Ski);
            }

            SetHeight(cfg.standingHeight);
            return v;
        }

        #endregion

        #region Air / Jet / Tap-strafe

        Vector3 AirMove(float dt, Vector3 v, Vector3 wish)
        {
            float g = cfg.gravity * (v.y < 0f ? cfg.fallGravityMult : 1f);
            if (Jetting) g *= cfg.gravityWhileJetting;
            // Air crouch = dive: ~2x fall rate while crouch held and falling/rising into dive.
            if (!Jetting && _in.CrouchHeld)
                g *= Mathf.Max(1f, cfg.airCrouchFallMult);
            v.y -= g * dt;
            float fallCap = cfg.maxFallSpeed * (_in.CrouchHeld && !Jetting ? Mathf.Max(1f, cfg.airCrouchFallMult) : 1f);
            if (v.y < -fallCap) v.y = -fallCap;

            Vector3 hv = WishAccel.Horizontal(v);
            if (wish.sqrMagnitude > 0.01f)
            {
                float cap = Mathf.Max(cfg.airSpeedCap, hv.magnitude);
                float accel = cfg.airAccel * (_in.Move.x != 0f && Mathf.Abs(_in.Move.y) < 0.2f ? cfg.airStrafeBonus : 1f);
                hv = WishAccel.Accelerate(hv, wish, cap, accel / Mathf.Max(cap, 1f), dt);
            }

            // Tap-strafe analog: a discrete forward pulse while holding a side key
            // redirects a slice of speed into the current wish. MnK skill ceiling.
            if (cfg.enableTapStrafe && _in.TapForwardPulse && _tapCd <= 0f && Mathf.Abs(_in.Move.x) > 0.4f)
            {
                Vector3 side = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.right * Mathf.Sign(_in.Move.x);
                float donate = Mathf.Min(cfg.tapStrafeImpulse, hv.magnitude);
                hv += side * donate * 0.65f;
                hv -= Vector3.Project(hv, transform.forward) * 0.25f;
                _tapCd = cfg.tapStrafeCooldown;
                _in.ConsumeTapPulse();
            }

            v = WishAccel.SetHoriz(v, hv);
            return v;
        }

        bool WantsJet()
        {
            if (cfg == null || !cfg.enableJet) return false;
            return _in.JetHeld && Energy > cfg.jetMinEnergy && State != MoveState.Mantle && State != MoveState.WallClimb;
        }

        Vector3 ApplyJet(float dt, Vector3 v, Vector3 wish)
        {
            Energy -= cfg.jetDrain * dt;
            _energyRegenDelay = cfg.jetRegenDelay;

            v.y += cfg.jetUpForce * dt;
            if (v.y > 11f)
                v.y = Mathf.Lerp(v.y, 8.5f, cfg.jetHoverDamp * dt);

            if (wish.sqrMagnitude > 0.01f)
            {
                Vector3 hv = WishAccel.Horizontal(v);
                float wishSpd = Mathf.Max(hv.magnitude, cfg.sprintSpeed + 2f);
                float jAccel = cfg.jetWishForce / 10f;
                // Low-speed jet still reads as thrust, not a hover-in-place.
                if (hv.magnitude < cfg.sprintSpeed)
                    jAccel *= 1.35f;
                hv = WishAccel.Accelerate(hv, wish, wishSpd, jAccel, dt);
                v = WishAccel.SetHoriz(v, hv);
            }

            if (State != MoveState.Slide && State != MoveState.WallRun)
                SetState(MoveState.Jet);
            return v;
        }

        void TickEnergy(float dt)
        {
            float max = cfg.jetEnergyMax + (tagRole != null && !tagRole.IsIt ? cfg.runnerJetEnergyBonus : 0f);
            if (_energyRegenDelay > 0f) return;
            if (_in.JetHeld) return;
            Energy = Mathf.Min(max, Energy + cfg.jetEnergyRegen * dt);
        }

        #endregion

        #region Jump / fatigue / bounce

        void TryJump(ref Vector3 v, bool grounded)
        {
            if (_jumpBuf <= 0f) return;

            // Super-glide: jump at mantle peak (crouch optional; height follows CrouchHeld)
            if (State == MoveState.Mantle && SuperGlideT >= 0f && SuperGlideT <= cfg.superGlideWindow)
            {
                DoSuperGlide(ref v);
                return;
            }

            if (State == MoveState.WallClimb)
            {
                DoWallBounce(ref v);
                return;
            }

            if (State == MoveState.WallRun)
            {
                DoWallRunJump(ref v);
                return;
            }

            // Coyote is jump-only Ã¢â‚¬â€ walk-off should fall immediately (Apex snappy, not air-walk).
            if (!grounded && _coyote <= 0f) return;

            float h = JumpHeightNow();
            bool fromSlide = State == MoveState.Slide && _slideT <= cfg.slideJumpWindow && HorizSpeed <= cfg.slideJumpSpeedCap;

            // Fixed launch: not additive with residual up from run/sprint/ski slopes.
            v.y = h;
            if (fromSlide)
            {
                Vector3 hv = WishAccel.Horizontal(v);
                v = WishAccel.SetHoriz(v, hv * cfg.slideHopRetain);
            }

            _jumpBuf = 0f;
            _coyote = 0f;
            _jumpFatigued = true;
            _lastLanded = Time.time;
            _airJumpsFromFatigue++;
            SetState(MoveState.Air);
            SetHeight(cfg.standingHeight);
            OnJumped?.Invoke();
        }

        float JumpHeightNow()
        {
            if (!_jumpFatigued) return cfg.jumpSpeed;
            float since = Time.time - _lastLanded;
            if (since <= cfg.jumpFatigueFullAt) return cfg.jumpFatigueMin;
            if (since >= cfg.jumpFatigueWindow) return cfg.jumpSpeed;
            float t = Mathf.InverseLerp(cfg.jumpFatigueFullAt, cfg.jumpFatigueWindow, since);
            return Mathf.Lerp(cfg.jumpFatigueMin, cfg.jumpSpeed, t);
        }

        void DoWallBounce(ref Vector3 v)
        {
            float along = _climbT;
            bool green = along >= cfg.wallBounceGreenMin && along <= cfg.wallBounceGreenMax;
            Vector3 away = _probe.Wall.hit ? _probe.Wall.normal : -transform.forward;
            Vector3 look = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : transform.forward;

            _climbBlocked = true;
            float up = green ? cfg.wallBounceUp : cfg.wallBounceUp * 0.55f;
            float outSpeed = green ? cfg.wallBounceSpeed : cfg.wallBounceSpeed * 0.65f;

            Vector3 hv = Vector3.ProjectOnPlane(v, Vector3.up);
            // Bias off-wall so TP reads a Tribes kick, not a plain jump-away.
            Vector3 launch = (away * 0.85f + look * 0.4f).normalized * outSpeed + Vector3.up * up;
            // Keep some inbound speed so bounce is a redirect, not a reset.
            launch += hv * 0.4f;

            v = launch;
            _jumpBuf = 0f;
            ClimbHeightUsed = 0f;
            _jumpFatigued = false; // climb clears fatigue, matching Apex
            SetState(MoveState.Air);
            OnWallBounced?.Invoke();
        }

        void DoSuperGlide(ref Vector3 v)
        {
            Vector3 dir = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : transform.forward;
            if (_in.Move.sqrMagnitude > 0.1f)
                dir = WishAccel.CameraWish(cam ? cam : transform, _in.Move);
            v = dir * cfg.superGlideSpeed + Vector3.up * 1.85f;
            _jumpBuf = 0f;
            SuperGlideT = -1f;
            SetState(MoveState.Air);
            SetHeight(_in.CrouchHeld ? cfg.crouchHeight : cfg.standingHeight);
            OnSuperGlide?.Invoke();
        }

        void DoWallRunJump(ref Vector3 v)
        {
            _wallRunBlocked = true;
            Vector3 away = _probe.Wall.hit ? _probe.Wall.normal : -transform.right;
            Vector3 look = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : transform.forward;
            v = away * cfg.wallRunJumpOut + Vector3.up * cfg.wallRunJumpUp + look * 3.5f;
            _jumpBuf = 0f;
            SetState(MoveState.Air);
            OnWallBounced?.Invoke();
        }

        #endregion

        #region Climb / Mantle / WallRun

        bool TryEnterClimb(Vector3 v, bool grounded)
        {
            if (!_probe.Wall.hit) return false;
            if (State == MoveState.WallClimb || State == MoveState.Mantle) return false;

            float face = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), -Vector3.ProjectOnPlane(_probe.Wall.normal, Vector3.up));
            bool holdingIn = _in.Move.y > 0.2f || _in.JumpHeld;
            if (!holdingIn) return false;
            if (face > cfg.climbAttachAngle) return false;
            if (grounded && !_in.JumpHeld && !_in.JumpPressed) return false;

            // Ledge grab is not a stick Ã¢â‚¬â€ still allowed after a climb has decayed off.
            if (_probe.Ledge.hit && _probe.Ledge.height < cfg.mantleMaxLedgeHeight && v.y > -8f)
            {
                BeginMantle();
                return true;
            }

            // Same-wall reattach is what made climb feel like a spider. Leave the surface first.
            if (_climbBlocked) return false;
            if (ClimbHeightUsed >= cfg.climbMaxHeight) return false;

            _climbT = 0f;
            _climbStartY = transform.position.y;
            SetState(MoveState.WallClimb);
            return true;
        }

        Vector3 TickClimb(float dt, Vector3 v, Vector3 wish)
        {
            if (!_probe.Wall.hit)
            {
                _climbBlocked = true;
                SetState(MoveState.Air);
                return v;
            }

            _climbT += dt;
            ClimbHeightUsed = transform.position.y - _climbStartY;

            if (_probe.Ledge.hit && ClimbHeightUsed > 0.2f)
            {
                BeginMantle();
                return v;
            }

            bool timeOut = cfg.climbMaxTime > 0f && _climbT >= cfg.climbMaxTime;
            bool heightOut = ClimbHeightUsed >= cfg.climbMaxHeight;
            bool released = !_in.JumpHeld && _in.Move.y < 0.1f && _climbT > 0.08f;
            if (timeOut || heightOut || released)
            {
                // Drop / slip off Ã¢â‚¬â€ stronger after a long cling so you cannot stick forever.
                _climbBlocked = true;
                v = Vector3.ProjectOnPlane(v, _probe.Wall.normal);
                float slip = cfg.climbSlipSpeed * (timeOut || heightOut ? 1.6f : 1f);
                v.y = Mathf.Min(v.y, -slip);
                SetState(MoveState.Air);
                return v;
            }

            // Up-speed decays to a real downward slide before the height cap.
            // A 0.12 floor here used to keep vy positive until climbMaxHeight fired (~0.42s)
            // and the budget never cleared, so the slide-down never played.
            float fade = 1f;
            if (cfg.climbMaxTime > 0.05f && _climbT > cfg.climbDecayStart)
            {
                float u = Mathf.InverseLerp(cfg.climbDecayStart, cfg.climbMaxTime, _climbT);
                fade = Mathf.Clamp01(1f - u * u);
            }
            float climbVy = cfg.climbSpeed * fade;
            if (fade < 0.85f)
                climbVy -= cfg.climbSlipSpeed * 3.5f * (1f - fade);
            if (_in.Move.y < -0.3f) climbVy = -cfg.climbSlipSpeed;
            Vector3 up = Vector3.up * climbVy;
            // Stronger into-wall glue so sticky probe + climb stay attached (was *0.05).
            Vector3 stick = -_probe.Wall.normal * cfg.climbStickForce * 0.09f;
            Vector3 side = Vector3.Cross(_probe.Wall.normal, Vector3.up).normalized * (_in.Move.x * cfg.climbSideSpeed);

            v = up + side + stick;
            SetHeight(cfg.standingHeight);
            return v;
        }

        void BeginMantle()
        {
            _mantleT = 0f;
            _mantleFrom = transform.position;
            // Nudge onto the deck along wall normal Ã¢â‚¬â€ 5cm clipped Mega_/rail colliders.
            Vector3 n = Vector3.ProjectOnPlane(_probe.Ledge.wallNormal, Vector3.up);
            if (n.sqrMagnitude < 0.01f) n = _probe.Ledge.wallNormal;
            n.Normalize();
            _mantleFwd = -n;
            _mantleTo = _probe.Ledge.standPoint + n * Mathf.Max(0.12f, cfg.radius * 0.35f) + Vector3.up * 0.03f;
            SuperGlideT = -1f;
            ClimbHeightUsed = 0f;
            _jumpFatigued = false;
            SetState(MoveState.Mantle);
            OnMantle?.Invoke();
        }

        Vector3 TickMantle(float dt, Vector3 v)
        {
            _mantleT += dt;
            float u = Mathf.Clamp01(_mantleT / cfg.mantleDuration);
            // Fast pull then settle Ã¢â‚¬â€ Apex mantle is front-loaded.
            float s = u < 0.55f ? Mathf.SmoothStep(0f, 1f, u / 0.55f) : 1f;
            Vector3 fwd = _mantleFwd.sqrMagnitude > 0.01f ? _mantleFwd : transform.forward;
            // Arc follows actual ledge rise (not max knob) so short rails do not sky-vault.
            float rise = Mathf.Max(0.15f, _mantleTo.y - _mantleFrom.y);
            Vector3 mid = _mantleFrom + Vector3.up * (rise * 0.55f);
            Vector3 pullTarget = mid + (_mantleTo - _mantleFrom) * 0.4f;
            Vector3 settleTarget = _mantleTo + fwd * cfg.mantleForward;
            Vector3 pos = u < 0.55f
                ? Vector3.Lerp(_mantleFrom, pullTarget, s)
                : Vector3.Lerp(pullTarget, settleTarget, (u - 0.55f) / 0.45f);

            Vector3 delta = (pos - transform.position) / dt;
            v = delta;

            // Super-glide: real seconds from mantle peak (bible u~0.62), not u-fraction.
            float peakT = cfg.mantleDuration * 0.62f;
            if (_mantleT > peakT && _mantleT <= peakT + cfg.superGlideWindow)
                SuperGlideT = _mantleT - peakT;
            else
                SuperGlideT = -1f;

            // Jump buffer so early taps still catch the window (party-fair).
            if (_jumpBuf > 0f && SuperGlideT >= 0f)
            {
                DoSuperGlide(ref v);
                return v;
            }

            if (u >= 1f)
            {
                transform.position = _mantleTo + fwd * cfg.mantleForward * 0.25f;
                v = fwd * Mathf.Max(cfg.walkSpeed, HorizSpeed * 0.4f);
                SetState(MoveState.Idle);
            }
            return v;
        }

        bool TryEnterWallRun(Vector3 v, bool grounded, Vector3 wish)
        {
            if (!cfg.enableWallRun) return false;
            if (_wallRunBlocked) return false;
            if (grounded) return false;
            if (!_probe.Wall.hit) return false;
            if (State == MoveState.WallClimb || State == MoveState.Mantle) return false;
            if (WishAccel.HorizSpeed(v) < cfg.wallRunMinSpeed) return false;

            float face = Vector3.Angle(WishAccel.Horizontal(v), -Vector3.ProjectOnPlane(_probe.Wall.normal, Vector3.up));
            // Must glance, not slam face-first (face-first is climb)
            if (face < cfg.wallRunAttachAngle || face > 90f) return false;
            if (_in.Move.y < 0.1f && Mathf.Abs(_in.Move.x) < 0.1f) return false;

            _wallRunT = 0f;
            SetState(MoveState.WallRun);
            return true;
        }

        Vector3 TickWallRun(float dt, Vector3 v, Vector3 wish)
        {
            if (!_probe.Wall.hit || _wallRunT > cfg.wallRunMaxTime)
            {
                // Block re-entry until the wall is actually left. Air accel used to
                // climb back over wallRunMinSpeed in ~2 frames and reset the timer.
                _wallRunBlocked = true;
                if (_wallRunT > cfg.wallRunMaxTime)
                    v.y = Mathf.Min(v.y, -4.5f);
                SetState(MoveState.Air);
                return v;
            }
            _wallRunT += dt;

            Vector3 along = Vector3.Cross(_probe.Wall.normal, Vector3.up);
            if (Vector3.Dot(along, WishAccel.Horizontal(v)) < 0f) along = -along;

            // Speed fades near the end of the window so the exit reads as a drop, not a glue peel.
            float tNorm = Mathf.Clamp01(_wallRunT / Mathf.Max(0.05f, cfg.wallRunMaxTime));
            float speedFade = Mathf.Lerp(1f, 0.55f, tNorm * tNorm);
            Vector3 hv = along * (cfg.wallRunSpeed * speedFade);
            float y = v.y;
            float grav = cfg.wallRunGravity * Mathf.Lerp(1f, Mathf.Max(1f, cfg.wallRunGravityEndMult), tNorm * tNorm);
            y -= grav * dt;
            // Early: soft floor. Late: allow real slide-down (no Spiderman hover).
            float minY = tNorm < 0.3f
                ? -1.5f
                : Mathf.Lerp(-2.5f, -16f, (tNorm - 0.3f) / 0.7f);
            y = Mathf.Max(y, minY);
            // Slightly stronger into-wall stick so sticky probe + run stay glued in TP
            v = hv + Vector3.up * y - _probe.Wall.normal * 2.8f;

            if (_in.JumpPressed) DoWallRunJump(ref v);
            return v;
        }

        #endregion

        #region Lunge / land stun / tag

        bool TryAirDash(ref Vector3 v, Vector3 wish, float dt, bool grounded)
        {
            if (_airDashT > 0f)
            {
                _airDashT -= dt;
                v = WishAccel.SetHoriz(v, _airDashDir * cfg.airDashSpeed);
                // Keep vertical Ã¢â‚¬â€ burst, not hover/jet.
                if (_airDashT <= 0f && State != MoveState.Slide)
                    SetState(grounded ? MoveState.Sprint : MoveState.Air);
                return true;
            }

            if (cfg == null || !cfg.enableAirDash) return false;
            if (grounded) return false;
            if (State == MoveState.Mantle || State == MoveState.WallClimb || State == MoveState.WallRun || State == MoveState.LandStun)
                return false;
            if (_airDashCd > 0f) return false;

            // Dedicated keys, or MMB/lunge press reused as air-dodge while airborne.
            bool pressed = _in.AirDashPressed || _in.LungePressed;
            if (!pressed) return false;

            Vector3 dir = DashWishDir(wish);
            _airDashDir = dir;
            _airDashT = cfg.airDashDuration;
            _airDashIFramesT = cfg.airDashIFrames;
            _airDashCd = Mathf.Max(0.01f, cfg.airDashCooldown);
            v = WishAccel.SetHoriz(v, dir * cfg.airDashSpeed);
            SetState(MoveState.Air);
            TagSfx.PlayAirDash(transform.position);
            OnAirDashed?.Invoke();
            return true;
        }

        Vector3 DashWishDir(Vector3 wish)
        {
            Vector3 dir;
            if (wish.sqrMagnitude > 0.01f)
                dir = wish;
            else if (cam != null)
                dir = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            else
                dir = transform.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
            return dir.normalized;
        }

        bool TryLunge(ref Vector3 v, Vector3 wish, float dt)
        {
            if (_lungeT > 0f)
            {
                _lungeT -= dt;
                Vector3 dir = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
                dir.Normalize();
                v = WishAccel.SetHoriz(v, dir * cfg.taggerLungeSpeed);
                v.y = Mathf.Max(v.y, -2f);
                if (_lungeT <= 0f && State != MoveState.Slide) SetState(MoveState.Sprint);
                return true;
            }

            // Ground It burst only Ã¢â‚¬â€ airborne MMB is consumed by TryAirDash.
            if (!_probe.Ground.grounded) return false;
            if (tagRole == null || !tagRole.IsIt) return false;
            if (!_in.LungePressed || _lungeCd > 0f) return false;
            _lungeT = cfg.taggerLungeDuration;
            _lungeCd = cfg.taggerLungeCooldown;
            Vector3 startDir = DashWishDir(wish);
            v = WishAccel.SetHoriz(v, startDir * cfg.taggerLungeSpeed);
            v.y = Mathf.Max(v.y, -2f);
            if (State != MoveState.Slide) SetState(MoveState.Sprint);
            TagSfx.LungeWhoosh(transform.position);
            return true;
        }

        Vector3 TickLandStun(float dt, Vector3 v)
        {
            _landStunT -= dt;
            Vector3 hv = WishAccel.Friction(WishAccel.Horizontal(v), 14f, dt);
            v = WishAccel.SetHoriz(v, hv);
            v.y -= cfg.gravity * dt;
            if (_landStunT <= 0f || _jumpBuf > 0f)
            {
                SetState(_probe.Ground.grounded ? MoveState.Idle : MoveState.Air);
                if (_jumpBuf > 0f) TryJump(ref v, true);
            }
            return v;
        }

        void TryTag()
        {
            var cols = Physics.OverlapSphere(transform.position + Vector3.up * 0.9f, cfg.tagRadius);
            foreach (var c in cols)
            {
                if (c.transform == transform) continue;
                var other = c.GetComponentInParent<TagRole>();
                if (other == null || other.IsIt) continue;
                tagRole.Tag(other);
                OnTaggedSomeone?.Invoke();
                other.GetComponent<PlayerMotor>()?.NotifyBecameIt();
                break;
            }
        }

        public void NotifyBecameIt() => OnBecameIt?.Invoke();

        public void SetMotorLocked(bool locked)
        {
            bool rising = locked && !_motorLocked;
            _motorLocked = locked;
            if (rising && _rb != null) _rb.linearVelocity = Vector3.zero;
        }

        public void SetPunchMoveScale(float scale) => _punchMoveScale = Mathf.Clamp(scale, 0.05f, 1.5f);
        public void SetSlideBlocked(bool blocked) => _slideBlocked = blocked;

        public void ApplySpeedBoost(float percent, float duration)
        {
            _speedBoostMul = 1f + Mathf.Max(0f, percent);
            _speedBoostT = Mathf.Max(_speedBoostT, duration);
        }

        public void ClearSpeedBoost()
        {
            _speedBoostMul = 1f;
            _speedBoostT = 0f;
        }

        public void BeginStunProxy(float duration, Vector3 knock)
        {
            SetMotorLocked(true);
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.AddForce(knock, ForceMode.VelocityChange);
            }
            CancelInvoke(nameof(EndStunProxy));
            Invoke(nameof(EndStunProxy), Mathf.Max(0.05f, duration));
        }

        public void ClearStun()
        {
            CancelInvoke(nameof(EndStunProxy));
            SetMotorLocked(false);
        }

        void EndStunProxy() => SetMotorLocked(false);

        #endregion

        #region State / capsule / helpers

        /// <summary>
        /// One attach per contact. Grounded, or ~0.15s with no wall hit, clears the latch
        /// so a new wall (or the same wall after you leave it) can be used again.
        /// </summary>
        void TickWallContactGates(float dt)
        {
            bool onWallState = State == MoveState.WallClimb || State == MoveState.WallRun;
            if (_probe.Ground.grounded && !onWallState)
            {
                _wallRunBlocked = false;
                _climbBlocked = false;
                ClimbHeightUsed = 0f;
                _wallContactLostT = 0f;
                return;
            }

            if (!_probe.Wall.hit)
            {
                _wallContactLostT += dt;
                if (_wallContactLostT >= WallReattachDelay)
                {
                    _wallRunBlocked = false;
                    _climbBlocked = false;
                }
            }
            else
                _wallContactLostT = 0f;
        }

        void LatchLandImpact()
        {
            bool g = _probe.Ground.grounded;
            if (g && !_wasProbeGrounded)
            {
                float impact = Mathf.Max(0f, -_rb.linearVelocity.y);
                _lastLandImpactSpeed = impact;
                _lastLanded = Time.time;
                // Air dash uses time cooldown (not land refresh).

                // Landing shock (Apex) Ã¢â‚¬â€ only from true air/jet, not ski kisses.
                // Harder impacts hold stun a touch longer (clamped).
                if ((State == MoveState.Air || State == MoveState.Jet) && impact >= cfg.landStunSpeed)
                {
                    float over = Mathf.InverseLerp(cfg.landStunSpeed, cfg.maxFallSpeed, impact);
                    _landStunT = cfg.landStunDuration * Mathf.Lerp(1f, 1.35f, over);
                    SetState(MoveState.LandStun);
                }
                else if (impact >= 5f)
                {
                    // Hard land already thuds via MoveAnimDriver on LandStun. This is the step-down.
                    TagSfx.LandAt(transform.position, Mathf.Lerp(0.18f, 0.36f, Mathf.Clamp01(impact / 12f)));
                }
            }
            _wasProbeGrounded = g;
        }

        void UpdateLocomotionState(bool grounded, Vector3 v)
        {
            if (State == MoveState.Mantle || State == MoveState.WallClimb || State == MoveState.WallRun || State == MoveState.LandStun)
                return;

            if (Jetting) { SetState(MoveState.Jet); return; }
            if (!grounded) { SetState(MoveState.Air); return; }
            if (Skiing) { SetState(MoveState.Ski); return; }
            if (State == MoveState.Slide) return;

            if (_in.CrouchHeld) { SetState(MoveState.Crouch); return; }

            float hs = WishAccel.HorizSpeed(v);
            if (hs < 0.4f) SetState(MoveState.Idle);
            else if (_in.SprintHeld || hs > cfg.sprintSpeed * 0.82f) SetState(MoveState.Sprint);
            else SetState(MoveState.Walk);
        }

        void SetState(MoveState next)
        {
            if (State == next) return;
            var prev = State;
            State = next;
            if (next == MoveState.Ski && prev != MoveState.Ski)
            {
                var src = TagSfx.EnsureSource(gameObject);
                if (src != null) TagSfx.SkiStart(src);
            }
            OnStateChanged?.Invoke(prev, next);
        }

        void TickTimers(float dt)
        {
            if (_speedBoostT > 0f)
            {
                _speedBoostT -= dt;
                if (_speedBoostT <= 0f) ClearSpeedBoost();
            }
            if (_coyote > 0f) _coyote -= dt;
            if (_jumpBuf > 0f) _jumpBuf -= dt;
            if (_tapCd > 0f) _tapCd -= dt;
            if (_lungeCd > 0f) _lungeCd -= dt;
            if (_airDashCd > 0f) _airDashCd -= dt;
            if (_airDashIFramesT > 0f) _airDashIFramesT -= dt;
            if (_energyRegenDelay > 0f) _energyRegenDelay -= dt;

            if (_probe.Ground.grounded && State != MoveState.Air && State != MoveState.Jet && State != MoveState.WallClimb)
            {
                if (_jumpFatigued && Time.time - _lastLanded > cfg.jumpFatigueWindow)
                    _jumpFatigued = false;
            }

            // Landing shock moved to LatchLandImpact (after probe Refresh).
        }

        Vector3 ClampAndDrag(Vector3 v, float dt)
        {
            Vector3 hv = WishAccel.Horizontal(v);
            float cap = State == MoveState.Ski || State == MoveState.Jet || State == MoveState.Slide
                ? cfg.skiMaxSpeed
                : cfg.skiMaxSpeed * 0.7f;
            if (hv.magnitude > cap)
                hv = hv.normalized * cap;

            if (!_probe.Ground.grounded && !Jetting)
                hv = Vector3.Lerp(hv, hv.normalized * Mathf.Min(hv.magnitude, cfg.skiMaxSpeed), cfg.skiAirDrag * dt);

            return WishAccel.SetHoriz(v, hv);
        }

        void TickHeight(float dt)
        {
            _cap.height = Mathf.MoveTowards(_cap.height, _height, 8f * dt);
            _cap.center = new Vector3(0f, _cap.height * 0.5f, 0f);
        }

        void SetHeight(float h) => _height = h;

        void ApplyCapsule()
        {
            _cap.height = cfg.standingHeight;
            _cap.radius = cfg.radius;
            _cap.center = new Vector3(0f, cfg.standingHeight * 0.5f, 0f);
            _cap.direction = 1;
        }

        void DriveAnimator()
        {
            if (!animator) return;
            animator.SetInteger(AnimIds.State, (int)State);
            animator.SetFloat(AnimIds.Speed, HorizSpeed);
            animator.SetFloat(AnimIds.VertSpeed, _rb.linearVelocity.y);
            animator.SetBool(AnimIds.Grounded, _probe.Ground.grounded);
            animator.SetBool(AnimIds.Ski, Skiing);
            animator.SetBool(AnimIds.Jet, Jetting);
            animator.SetBool(AnimIds.Slide, State == MoveState.Slide);
            animator.SetBool(AnimIds.Crouch, State == MoveState.Crouch || State == MoveState.Slide);
            animator.SetFloat(AnimIds.MoveX, _in.Move.x);
            animator.SetFloat(AnimIds.MoveY, _in.Move.y);
            animator.SetFloat(AnimIds.Energy, Energy / cfg.jetEnergyMax);
            animator.SetBool(AnimIds.WallLeft, _probe.Wall.left);
            animator.SetFloat(AnimIds.Slope, _probe.Ground.slopeAngle);
        }

        void OnCollisionStay(Collision col)
        {
            // Soft wall cancel if we slam a front wall at ski speed
            if (State != MoveState.Ski && State != MoveState.Slide) return;
            foreach (var c in col.contacts)
            {
                if (Vector3.Angle(c.normal, Vector3.up) < 55f) continue;
                if (Vector3.Dot(WishAccel.Horizontal(_rb.linearVelocity).normalized, -c.normal) > 0.72f)
                {
                    Vector3 v = _rb.linearVelocity;
                    Vector3 bounced = Vector3.Reflect(WishAccel.Horizontal(v), Vector3.ProjectOnPlane(c.normal, Vector3.up).normalized);
                    _rb.linearVelocity = WishAccel.SetHoriz(v, bounced * 0.45f);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (cfg == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, cfg.tagRadius);
        }

        #endregion
    }

    public static class AnimIds
    {
        public static readonly int State = Animator.StringToHash("State");
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int VertSpeed = Animator.StringToHash("VertSpeed");
        public static readonly int Grounded = Animator.StringToHash("Grounded");
        public static readonly int Ski = Animator.StringToHash("Ski");
        public static readonly int Jet = Animator.StringToHash("Jet");
        public static readonly int Slide = Animator.StringToHash("Slide");
        public static readonly int Crouch = Animator.StringToHash("Crouch");
        public static readonly int MoveX = Animator.StringToHash("MoveX");
        public static readonly int MoveY = Animator.StringToHash("MoveY");
        public static readonly int Energy = Animator.StringToHash("Energy");
        public static readonly int WallLeft = Animator.StringToHash("WallLeft");
        public static readonly int Slope = Animator.StringToHash("Slope");
        public static readonly int JumpTrig = Animator.StringToHash("Jump");
        public static readonly int BounceTrig = Animator.StringToHash("Bounce");
        public static readonly int MantleTrig = Animator.StringToHash("Mantle");
        public static readonly int GlideTrig = Animator.StringToHash("SuperGlide");
        public static readonly int LandTrig = Animator.StringToHash("Land");
    }
}
