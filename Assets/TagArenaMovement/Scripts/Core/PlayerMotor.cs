using System.Collections.Generic;
using UnityEngine;
using Tag.Audio;
using Tag.Core;
using Tag.Experimental;
using Tag.Gameplay;
using Tag.Level;

namespace TagArena.Movement
{
    /// <summary>
    /// Kinematic motor. One CharacterController.Move per Update.
    /// Gravity, jump, slide friction, and cling live here. The rigidbody is the ragdoll window only.
    /// A launch pad queues a velocity set that this Update writes before that Move.
    /// A zip line replaces that velocity with a fixed ride along the cable while cling is held.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(SurfaceProbe))]
    public class PlayerMotor : MonoBehaviour
    {
        public MovementConfig cfg;
        public Transform cam;
        public Animator animator;
        public TagRole tagRole;

        public MoveState State { get; private set; } = MoveState.Idle;
        public Locomotion Mode => _mode;
        public Vector3 Velocity => _mode == Locomotion.Ragdoll && _rb != null ? _rb.linearVelocity : _velocity;
        public float Energy { get; private set; }
        public float HorizSpeed => WishAccel.HorizSpeed(Velocity);
        public GroundInfo Ground => _probe.Ground;
        public bool Skiing { get; private set; }
        public bool Jetting { get; private set; }
        public float ClimbHeightUsed { get; private set; }
        public float SuperGlideT { get; private set; } = -1f;
        public Vector3 WallNormal => _probe.Wall.normal;
        public bool WallLeft => _probe.Wall.left;
        /// <summary>Visual lip only. The mantle timer and the stand point write are unchanged.</summary>
        public bool LedgeHit => _probe != null && _probe.Ledge.hit;
        public Vector3 LedgeStand => _probe != null ? _probe.Ledge.standPoint : Vector3.zero;
        /// <summary>Seconds of cling-release grace still running. The pose reads this. The timer is not written here.</summary>
        public float ClingGraceRemaining => _clingGrace;
        /// <summary>True when a cling into the face just left is refused. The pose reads this.</summary>
        public bool ClingRefused => _clingRefused;
        /// <summary>True from a pad's velocity set until the next landing. The rise pose reads this.</summary>
        public bool LaunchArc => _launchArc;
        /// <summary>True while cling is holding a zip cable. The hang pose reads this.</summary>
        public bool ZipRiding => _zipRiding;

        /// <summary>Chase reads this so it does not steer a re-cling into the face just left.</summary>
        public bool WouldRefuseCling(int colliderId, Vector3 normal, Vector3 point)
        {
            return SameWallLimit.Blocks(_wallBan, SameWallLimit.Make(colliderId, normal, point));
        }
        /// <summary>True while the wall probe is in contact. The pose reads this.</summary>
        public bool WallContact => _probe != null && _probe.Wall.hit;
        public bool IsMotorLocked => _motorLocked;
        public bool IsSliding => State == MoveState.Slide;
        public bool IsWallRunning => State == MoveState.WallRun;
        public bool IsVaulting => State == MoveState.Mantle;
        public bool IsAirDodgeLocked => State == MoveState.Jet && Jetting;
        public bool IsAirDashing => _mode == Locomotion.AirDash;
        public bool HasAirDodgeIFrames => IsAirDodgeLocked || _airDashIFramesT > 0f;
        /// <summary>1 at air-dash start, 0 at end (TP whip).</summary>
        public float AirDashProgress =>
            _airDashT > 0f && cfg != null
                ? Mathf.Clamp01(_airDashT / Mathf.Max(0.01f, cfg.airDashDuration))
                : 0f;
        /// <summary>Seconds left before air dash is usable again (0 = ready).</summary>
        public float AirDashCooldownRemaining => Mathf.Max(0f, _airDashCd);
        /// <summary>Planar direction of the current or most recent air dash. Read by the tell only.</summary>
        public Vector3 AirDashDirection => _airDashDir;
        public bool IsGrounded => _probe != null && _stableFeet;
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
        public float SprintSpeed => cfg != null ? cfg.sprintSpeed : 13.8f;
        /// <summary>Downward speed (m/s) latched on the most recent ground contact.</summary>
        public float LastLandImpactSpeed => _lastLandImpactSpeed;

        Rigidbody _rb;
        CapsuleCollider _cap;
        CharacterController _cc;
        PlayerInputReader _in;
        SurfaceProbe _probe;
        Locomotion _mode = Locomotion.Ground;
        Vector3 _velocity;

        float _height;
        float _coyote;
        float _jumpSlot;
        float _wallJumpSlot;
        float _clingGrace;
        bool _wallJumpFromClimb;
        float _lastLanded;
        float _slideT;
        float _slideLoopT;
        float _zipLoopT;
        float _slideStartSpeed;
        float _climbT;
        float _climbStartY;
        float _wallRunT;
        bool _wallRunBlocked;
        bool _climbBlocked;
        SameWallLimit.Ban _wallBan;
        SameWallLimit.Face _attachedFace;
        bool _clingRefused;
        float _mantleT;
        float _mantleEntryPlanar;
        Vector3 _mantleFrom;
        Vector3 _mantleTo;
        Vector3 _mantleFwd;
        float _energyRegenDelay;
        float _tapCd;
        float _lungeCd;
        float _lungeT;
        /// <summary>AI It flag. Human lunge still requires TagRole. Not a new button.</summary>
        bool _externalTagger;
        float _airDashT;
        float _airDashIFramesT;
        float _airDashCd;
        Vector3 _airDashDir;
        float _landStunT;
        float _lastLandImpactSpeed;
        bool _wasProbeGrounded = true;
        bool _rawFeetPrev;
        bool _stableFeet = true;
        bool _jumpFatigued;
        bool _motorLocked;
        bool _slideBlocked;
        float _punchMoveScale = 1f;
        PunchStagger.Clock _stagger;
        float _speedBoostMul = 1f;
        float _speedBoostT;
        bool _grappleSearched;
        ExperimentalGrapple _grapple;
        bool _grappleYieldDash;
        bool _launchQueued;
        bool _launchSetHoriz;
        bool _launchArc;
        float _launchApex;
        float _launchCooldown;
        float _launchReadyAt;
        Vector3 _launchHoriz;
        bool _zipRiding;
        bool _zipChaseTake;
        int _zipLineId;
        float _zipReadyAt;
        float _zipCooldown;
        Vector3 _zipVelocity;
        ZipLine _zipLine;

        public event System.Action<MoveState, MoveState> OnStateChanged;
        public event System.Action OnJumped;
        public event System.Action OnSlid;
        public event System.Action OnAirDashed;
        public event System.Action OnWallBounced;
        public event System.Action OnSuperGlide;
        public event System.Action OnMantle;
        /// <summary>Kill-box and practice restart. 0 hides the mesh. The pad position is already final.</summary>
        public float VisualBlinkAge = 10f;
        public event System.Action OnTaggedSomeone;
        public event System.Action OnBecameIt;

        static readonly Dictionary<int, PlayerMotor> ColliderIndex = new Dictionary<int, PlayerMotor>();

        public static void ResetColliderIndex()
        {
            ColliderIndex.Clear();
        }

        public static PlayerMotor FromCollider(Collider c)
        {
            if (c == null) return null;
            PlayerMotor motor;
            if (ColliderIndex.TryGetValue(c.GetInstanceID(), out motor) && motor != null)
                return motor;
            motor = c.GetComponentInParent<PlayerMotor>();
            if (motor != null)
                ColliderIndex[c.GetInstanceID()] = motor;
            return motor;
        }

        void OnEnable()
        {
            IndexColliders();
        }

        void OnDisable()
        {
            DropColliders();
        }

        void IndexColliders()
        {
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null) continue;
                ColliderIndex[cols[i].GetInstanceID()] = this;
            }
        }

        void DropColliders()
        {
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null) continue;
                int id = cols[i].GetInstanceID();
                PlayerMotor owner;
                if (ColliderIndex.TryGetValue(id, out owner) && owner == this)
                    ColliderIndex.Remove(id);
            }
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _cap = GetComponent<CapsuleCollider>();
            _cc = GetComponent<CharacterController>();
            if (_cc == null) _cc = gameObject.AddComponent<CharacterController>();
            _in = GetComponent<PlayerInputReader>();
            _probe = GetComponent<SurfaceProbe>();

            // Solver does not integrate this body. Ragdoll turns it dynamic for the stun window.
            if (_rb != null)
            {
                _rb.interpolation = RigidbodyInterpolation.None;
                _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                _rb.constraints = RigidbodyConstraints.FreezeRotation;
                _rb.isKinematic = true;
                _rb.useGravity = false;
                _rb.detectCollisions = false;
                _rb.mass = 80f;
            }
            if (_cap != null) _cap.enabled = false;

            if (cfg != null)
            {
                _height = cfg.standingHeight;
                Energy = cfg.jetEnergyMax;
                ApplyCapsule();
                _probe.Init(cfg, transform, _cap);
            }
            if (animator != null) animator.applyRootMotion = false;
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
            Tag.Settings.PadRumbleOutput.Tick(Time.unscaledDeltaTime);
            _clingRefused = false;
            if (_in == null || cfg == null) return;
            _in.Read();
            // Next frame's stick gesture reads this. A return before the write below leaves it clear.
            _in.EvasionGrounded = false;
            // Edges are latched in this same Update, then the move consumes them.
            // A frozen pause keeps cling grace, the jump buffer, and every verb timer.
            // An unlocked cursor (results, resume gate) still drops the slots so a card click cannot hop.
            if (SessionRules.TimeFrozen(Time.timeScale))
                return;
            // Countdown and results keep timeScale at 1. A ride, a pad, and a
            // stumble do not start there. Pause returns above and keeps the ride.
            if (!SessionRules.RoundPlay)
            {
                if (_zipRiding) ReleaseZip();
                _launchQueued = false;
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked || ResumeInputGate.Blocking)
            {
                _jumpSlot = 0f;
                _wallJumpSlot = 0f;
                _clingGrace = 0f;
                ReleaseGrapple();
                return;
            }
            if (_motorLocked)
            {
                ReleaseGrapple();
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (VisualBlinkAge < Tag.Art.RespawnBlink.Seconds)
                VisualBlinkAge += dt;
            _grappleYieldDash = false;
            ResolveGrapple();
            TickTimers(dt);

            _probe.Refresh(_height, _velocity);
            // Raw contact refreshes coyote. One missed sample does not flap locomotion into air.
            bool rawFeet = _probe.Ground.grounded;
            bool feet = KinematicStep.StableGround(rawFeet, _rawFeetPrev, _velocity.y);
            _rawFeetPrev = rawFeet;
            _stableFeet = feet;
            // A zip ride and a pad arc are not ground. They must not refill coyote.
            if (rawFeet && !_zipRiding && !_launchArc
                && _mode != Locomotion.Climb && _mode != Locomotion.WallRun && _mode != Locomotion.Vault)
                _coyote = cfg.coyoteTime;
            LatchLandImpact();
            TickZipAttach();
            TickWallContactGates();
            Vector3 wish = WishAccel.CameraWish(cam ? cam : transform, _in.Move);
            if (!_probe.Wall.hit)
            {
                _clingGrace = 0f;
                _wallJumpSlot = 0f;
            }
            else if ((_mode == Locomotion.Climb || _mode == Locomotion.WallRun) && ClingHeld(wish))
                _clingGrace = Mathf.Max(0.01f, cfg.clingReleaseGrace);
            else if (_clingGrace > 0f)
                _clingGrace -= dt;
            LatchSlots();

            Vector3 v = _velocity;

            bool grounded = feet && _mode != Locomotion.Vault && _mode != Locomotion.Climb && _mode != Locomotion.WallRun;

            TickEnergy(dt);
            TickHeight(dt);

            if (_zipRiding)
            {
                // The ride velocity is written once, in ApplyZipRide, before the single Move.
                SetState(MoveState.Air);
            }
            else switch (_mode)
            {
                case Locomotion.Vault:
                    v = TickMantle(dt, v);
                    break;
                case Locomotion.Climb:
                    v = TickClimb(dt, v, wish);
                    break;
                case Locomotion.WallRun:
                    v = TickWallRun(dt, v, wish);
                    break;
                case Locomotion.LandStun:
                    v = TickLandStun(dt, v);
                    break;
                case Locomotion.Ragdoll:
                    ReleaseGrapple();
                    return;
                default:
                    v = TickLocomotion(dt, v, wish, grounded);
                    break;
            }

            _clingRefused = EvaluateClingRefused(wish);
            v = ClampAndDrag(v, dt);
            if (_speedBoostMul > 1.001f && _mode != Locomotion.LandStun && _mode != Locomotion.Slide
                && _mode != Locomotion.Vault)
            {
                Vector3 hv = WishAccel.Horizontal(v) * _speedBoostMul;
                v = WishAccel.SetHoriz(v, hv);
            }
            if (_punchMoveScale < 0.999f)
            {
                Vector3 hv = WishAccel.Horizontal(v) * _punchMoveScale;
                v = WishAccel.SetHoriz(v, hv);
            }
            if (_mode == Locomotion.Slide)
            {
                Vector3 sh = WishAccel.Horizontal(v);
                float scap = Mathf.Max(0.01f, _slideStartSpeed);
                if (sh.magnitude > scap)
                    v = WishAccel.SetHoriz(v, sh * (scap / sh.magnitude));
            }
            if (_slideBlocked && _mode == Locomotion.Slide)
                SetState(MoveState.Crouch);
            // After TryJump. That write is v.y = jumpSpeed (24.7 when not fatigued).
            // A zip or a queued pad owns the velocity, so the rope yields first.
            // The one Move below consumes whatever is left.
            if (VerbIntegration.GrappleYields(_zipRiding, _launchQueued, _launchArc))
                ReleaseGrapple();
            v = ApplyGrappleHorizontal(v);
            v = ApplyGroundRead(v, dt);
            v = ApplyQueuedLaunch(v);
            v = ApplyZipRide(v, dt);

            _velocity = VerbIntegration.FiniteOrZero(v);
            // The reader already sampled the stick this frame, using last frame's grounded flag.
            _in.EvasionGrounded = grounded;
            _in.EvasionId = GetInstanceID();
            float evasionX = _velocity.x;
            float evasionZ = _velocity.z;
            _in.EvasionPlanarSpeed = Mathf.Sqrt(evasionX * evasionX + evasionZ * evasionZ);
            // Evasion is off unless the flag is set. Vertical stays the jump the button wrote.
            if (Tag.Gameplay.EvasionMoves.Enabled)
                _velocity = Tag.Gameplay.EvasionMoves.Gate(
                    GetInstanceID(), _velocity, dt, cfg.groundAccel, cfg.sprintSpeed, transform.forward, transform.right);
            FitController(v.y, CeilingClose());
            CollisionFlags flags = CollisionFlags.None;
            if (_cc != null && _cc.enabled)
            {
                flags = _cc.Move(_velocity * dt);
                FrameMeter.AddMove(FrameMeter.MoveOps);
            }
            _velocity.y = KinematicStep.CeilingBlockedVy(_velocity.y, (flags & CollisionFlags.Above) != 0);
            if (VerbIntegration.EndLaunchArc(_launchArc, _stableFeet, _velocity.y))
            {
                _launchArc = false;
                ClearWallBan();
            }

            if (tagRole != null && tagRole.IsIt)
                TryTag();

            DriveAnimator();
            PawnAudio.Step(this, dt);
        }

        ExperimentalGrapple GrappleOrNull()
        {
            if (_grappleSearched) return _grapple;
            _grappleSearched = true;
            _grapple = GetComponent<ExperimentalGrapple>();
            return _grapple;
        }

        void ResolveGrapple()
        {
            ExperimentalGrapple grapple = GrappleOrNull();
            if (grapple != null) grapple.ResolveAttach();
        }

        void ReleaseGrapple()
        {
            ExperimentalGrapple grapple = GrappleOrNull();
            if (grapple != null) grapple.Release();
        }

        /// <summary>
        /// One horizontal write. WishAccel.SetHoriz keeps v.y, which TryJump set from jumpSpeed.
        /// Air dash already returned its own horizontal for this Move. Climb, wall run, vault,
        /// land stun, and ragdoll keep the velocity they wrote.
        /// </summary>
        Vector3 ApplyGrappleHorizontal(Vector3 v)
        {
            if (_grappleYieldDash) return v;
            if (_mode == Locomotion.Climb || _mode == Locomotion.WallRun || _mode == Locomotion.Vault
                || _mode == Locomotion.Ragdoll || _mode == Locomotion.LandStun)
                return v;
            ExperimentalGrapple grapple = GrappleOrNull();
            if (grapple == null || !grapple.TryGetRope(out Vector3 anchor, out float length, out float slack))
                return v;
            Vector3 hv = KinematicStep.GrappleHorizontal(WishAccel.Horizontal(v), transform.position, anchor, length, slack);
            if (grapple.Pulling)
            {
                Vector3 to = anchor - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.16f)
                    hv = to.normalized * ExperimentalGrapple.PullSpeed;
            }
            return WishAccel.SetHoriz(v, hv);
        }

        /// <summary>
        /// One slot each. The latest press replaces that slot. A wall press does not become a ground jump.
        /// </summary>
        void LatchSlots()
        {
            if (_in.JumpPressed)
            {
                bool onWall = _probe != null && _probe.Wall.hit
                    && (_mode == Locomotion.Climb || _mode == Locomotion.WallRun || _clingGrace > 0f);
                if (onWall)
                {
                    _wallJumpSlot = cfg.jumpBuffer;
                    _jumpSlot = 0f;
                }
                else
                {
                    _jumpSlot = cfg.jumpBuffer;
                    _wallJumpSlot = 0f;
                }
            }
            if (_probe == null || !_probe.Wall.hit)
                _wallJumpSlot = 0f;
        }

        Vector3 TickLocomotion(float dt, Vector3 v, Vector3 wish, bool grounded)
        {
            Skiing = WantsSki(grounded);
            Jetting = WantsJet();

            if (TryEnterClimb(v, grounded, wish)) return _velocity;
            if (TryEnterWallRun(v, grounded, wish)) return _velocity;
            if (!_launchQueued && _clingGrace > 0f && _probe.Wall.hit && _wallJumpSlot > 0f
                && _mode != Locomotion.Climb && _mode != Locomotion.WallRun)
            {
                if (_wallJumpFromClimb) DoWallBounce(ref v);
                else DoWallRunJump(ref v);
                return v;
            }
            if (TryAirDash(ref v, wish, dt, grounded))
            {
                // This frame's Move keeps airDashSpeed. The rope does not retune that window.
                _grappleYieldDash = true;
                return v;
            }
            if (TryLunge(ref v, wish, dt)) return v;

            // Pad rise. Gravity is the rise curve. Crouch does not scale it.
            // Strafe still adds horizontal through the same air steer.
            if (_launchArc && v.y > 0f)
            {
                v = AirMoveLaunch(dt, v, wish);
                UpdateLocomotionState(false, v);
                return v;
            }

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
            bool sprint = PunchStagger.SprintHeld(_in.SprintHeld, _stagger.Stagger);
            float max = KinematicStep.GaitCap(wantCrouch, sprint, _in.Move.y, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);

            if (tagRole != null && tagRole.IsIt && sprint)
                max += cfg.taggerSprintBonus;

            // A jump this frame leaves before friction, which is how a landing hop keeps air speed.
            bool hop = _jumpSlot > 0f;
            hv = KinematicStep.GroundSteer(hv, wish, max, cfg.groundAccel, cfg.groundDecel, dt, hop);

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
                // Too steep to walk - start an involuntary ski if pointing downhill
                v = WishAccel.SetHoriz(v, hv);
                v.y -= cfg.gravity * dt;
            }

            SetHeight(wantCrouch ? cfg.crouchHeight : cfg.standingHeight);
            return v;
        }

        void EnterSlide(ref Vector3 v)
        {
            // Carry existing planar speed only - no enter impulse / boost.
            Vector3 hv = WishAccel.Horizontal(v);
            if (hv.sqrMagnitude < 0.05f)
                hv = transform.forward * Mathf.Max(hv.magnitude, cfg.slideEntrySpeed * 0.85f);
            // Ignore legacy slideBoost so slides never punch speed on enter.
            v = WishAccel.SetHoriz(v, hv);
            _slideT = 0f;
            _slideStartSpeed = hv.magnitude;
            SetState(MoveState.Slide);
            SetHeight(cfg.crouchHeight);
            _slideLoopT = 0.45f;
            AudioBus.Raise(AudioBus.Hook.SlideStart, transform.position);
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

            // Carry entry speed only - never accelerate above slide-entry planar speed.
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
                // Steer only - cap at current speed (no +1.5 boost).
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
                else if (_probe.Ground.grounded)
                    SetState(_stagger.Stagger <= 0f && hNow > cfg.walkSpeed + 0.4f ? MoveState.Sprint : MoveState.Walk);
                else SetState(MoveState.Air);
            }
            else
            {
                _slideLoopT -= dt;
                if (_slideLoopT <= 0f)
                {
                    _slideLoopT = 0.45f;
                    AudioBus.Raise(AudioBus.Hook.SlideLoop, transform.position);
                }
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
            // Flat / mild slope: do NOT ice-skate at jog speeds - prefer run/sprint.
            // Only allow flat ski to preserve already-high momentum (Tribes crest carry).
            if (grounded && _probe.Ground.slopeAngle < cfg.skiMinSlope)
                return HorizSpeed >= cfg.sprintSpeed * 1.15f;
            return true;
        }

        Vector3 SkiMove(float dt, Vector3 v, Vector3 wish)
        {
            Vector3 n = _probe.Ground.normal;
            // Gravity along the plane - this is the entire Tribes engine in one line.
            v += Physics.gravity.normalized * (cfg.gravity * cfg.skiGravityScale * dt);
            // Outward vs ground normal - crest-launch fuel (was killed by a zero factor).
            float leave = Vector3.Dot(v, n);
            // Hug the plane for friction/edging; restore leave later with a sane factor.
            Vector3 planeVel = Vector3.ProjectOnPlane(v, n);

            // Tiny friction - never zero, matching real T1 insight: decay by slope.
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
            float g = KinematicStep.AirGravity(v.y, cfg.gravity, cfg.fallGravityMult);
            if (Jetting) g *= cfg.gravityWhileJetting;
            // Air crouch = dive: ~2x fall rate while crouch held and falling/rising into dive.
            if (!Jetting && _in.CrouchHeld)
                g *= Mathf.Max(1f, cfg.airCrouchFallMult);
            float fallCap = cfg.maxFallSpeed * (_in.CrouchHeld && !Jetting ? Mathf.Max(1f, cfg.airCrouchFallMult) : 1f);
            v.y = KinematicStep.IntegrateVertical(v.y, g, dt, fallCap);

            Vector3 hv = WishAccel.Horizontal(v);
            // Wish speed is the gait of the keys held, not max(airSpeedCap, current speed).
            // A straight run keeps its speed. A turned strafe can add speed past sprint.
            if (wish.sqrMagnitude > 0.01f)
            {
                float wishSpeed = KinematicStep.GaitCap(_in.CrouchHeld, PunchStagger.SprintHeld(_in.SprintHeld, _stagger.Stagger), _in.Move.y, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
                float accel = cfg.airAccel * (_in.Move.x != 0f && Mathf.Abs(_in.Move.y) < 0.2f ? cfg.airStrafeBonus : 1f);
                hv = KinematicStep.AirSteer(hv, wish, wishSpeed, accel, dt);
            }

            // Tap-strafe: a forward pulse (W or a stick flick) while holding a side key
            // redirects a slice of speed into the current wish. Impulse and cooldown are unchanged.
            // The redirect keeps the speed you already have. It does not add a side boost.
            if (cfg.enableTapStrafe && _in.TapForwardPulse && _tapCd <= 0f && Mathf.Abs(_in.Move.x) > 0.4f)
            {
                float kept = hv.magnitude;
                Vector3 side = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.right * Mathf.Sign(_in.Move.x);
                float donate = Mathf.Min(cfg.tapStrafeImpulse, hv.magnitude);
                hv += side * donate * 0.65f;
                hv -= Vector3.Project(hv, transform.forward) * 0.25f;
                hv = WishAccel.ClampPlanarSpeed(hv, kept);
                _tapCd = cfg.tapStrafeCooldown;
                _in.ConsumeTapPulse();
            }

            v = WishAccel.SetHoriz(v, hv);
            return v;
        }

        /// <summary>
        /// Launch rise. Vertical is gravity only. Horizontal is the existing air steer,
        /// so a strafe can add speed and a held key cannot change the apex.
        /// </summary>
        Vector3 AirMoveLaunch(float dt, Vector3 v, Vector3 wish)
        {
            float g = KinematicStep.AirGravity(v.y, cfg.gravity, cfg.fallGravityMult);
            v.y = KinematicStep.IntegrateVertical(v.y, g, dt, cfg.maxFallSpeed);
            v = WishAccel.SetHoriz(v, LaunchAirHorizontal(v, wish, dt));
            return v;
        }

        Vector3 LaunchAirHorizontal(Vector3 v, Vector3 wish, float dt)
        {
            Vector3 hv = WishAccel.Horizontal(v);
            if (wish.sqrMagnitude > 0.01f)
            {
                float wishSpeed = KinematicStep.GaitCap(_in.CrouchHeld, PunchStagger.SprintHeld(_in.SprintHeld, _stagger.Stagger), _in.Move.y, cfg.crouchSpeed, cfg.sprintSpeed, cfg.walkSpeed);
                float accel = cfg.airAccel * (_in.Move.x != 0f && Mathf.Abs(_in.Move.y) < 0.2f ? cfg.airStrafeBonus : 1f);
                hv = KinematicStep.AirSteer(hv, wish, wishSpeed, accel, dt);
            }

            if (cfg.enableTapStrafe && _in.TapForwardPulse && _tapCd <= 0f && Mathf.Abs(_in.Move.x) > 0.4f)
            {
                float kept = hv.magnitude;
                Vector3 side = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.right * Mathf.Sign(_in.Move.x);
                float donate = Mathf.Min(cfg.tapStrafeImpulse, hv.magnitude);
                hv += side * donate * 0.65f;
                hv -= Vector3.Project(hv, transform.forward) * 0.25f;
                hv = WishAccel.ClampPlanarSpeed(hv, kept);
                _tapCd = cfg.tapStrafeCooldown;
                _in.ConsumeTapPulse();
            }

            return hv;
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
            // A pad arc, or a pad queued for this frame, is already the vertical.
            // The buffered jump must not write jumpSpeed on top of it.
            if (_launchArc || _launchQueued) return;
            if (_jumpSlot <= 0f) return;

            // Super-glide: jump at mantle peak (crouch optional; height follows CrouchHeld)
            if (State == MoveState.Mantle && SuperGlideT >= 0f && SuperGlideT <= cfg.superGlideWindow)
            {
                DoSuperGlide(ref v);
                return;
            }

            if (_mode == Locomotion.Climb || _mode == Locomotion.WallRun)
            {
                // Wall jump is its own slot. A ground slot must not bounce, and this slot must not hop in air.
                return;
            }

            // Coyote is jump-only - walk-off should fall immediately (Apex snappy, not air-walk).
            if (!KinematicStep.CoyoteJumpAllowed(grounded, _coyote)) return;

            float h = JumpHeightNow();
            bool fromSlide = State == MoveState.Slide && _slideT <= cfg.slideJumpWindow && HorizSpeed <= cfg.slideJumpSpeedCap;

            // Vertical impulse only. Walk and sprint keep the horizontal speed they already
            // have, so a faster run jumps farther. Height is not a speed bonus.
            // A slide hop still multiplies by slideHopRetain. That spends slide speed. It is not a boost.
            v.y = h;
            if (fromSlide)
            {
                Vector3 hv = WishAccel.Horizontal(v);
                v = WishAccel.SetHoriz(v, hv * cfg.slideHopRetain);
            }

            _jumpSlot = 0f;
            _wallJumpSlot = 0f;
            _coyote = 0f;
            _jumpFatigued = true;
            _lastLanded = Time.time;
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
            WallJumpCount++;
            float along = _climbT;
            bool green = along >= cfg.wallBounceGreenMin && along <= cfg.wallBounceGreenMax;
            Vector3 away = _probe.Wall.hit ? _probe.Wall.normal : -transform.forward;
            Vector3 look = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : transform.forward;

            BanLeftWall();
            float up = green ? cfg.wallBounceUp : cfg.wallBounceUp * 0.55f;
            float outSpeed = green ? cfg.wallBounceSpeed : cfg.wallBounceSpeed * 0.65f;

            Vector3 hv = Vector3.ProjectOnPlane(v, Vector3.up);
            // Bias off-wall so TP reads a Tribes kick, not a plain jump-away.
            Vector3 launch = (away * 0.85f + look * 0.4f).normalized * outSpeed + Vector3.up * up;
            // Keep some inbound speed so bounce is a redirect, not a reset.
            launch += hv * 0.4f;

            v = launch;
            _launchArc = false;
            _jumpSlot = 0f;
            _wallJumpSlot = 0f;
            _clingGrace = 0f;
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
            _jumpSlot = 0f;
            _wallJumpSlot = 0f;
            SuperGlideT = -1f;
            SetState(MoveState.Air);
            SetHeight(_in.CrouchHeld ? cfg.crouchHeight : cfg.standingHeight);
            OnSuperGlide?.Invoke();
        }

        void DoWallRunJump(ref Vector3 v)
        {
            WallJumpCount++;
            BanLeftWall();
            Vector3 away = _probe.Wall.hit ? _probe.Wall.normal : -transform.right;
            Vector3 look = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : transform.forward;
            v = away * cfg.wallRunJumpOut + Vector3.up * cfg.wallRunJumpUp + look * 3.5f;
            _launchArc = false;
            _jumpSlot = 0f;
            _wallJumpSlot = 0f;
            _clingGrace = 0f;
            SetState(MoveState.Air);
            OnWallBounced?.Invoke();
        }

        #endregion

        #region Climb / Mantle / WallRun

        bool TryEnterClimb(Vector3 v, bool grounded, Vector3 wish)
        {
            if (_launchQueued) return false;
            if (!_probe.Wall.hit) return false;
            if (State == MoveState.WallClimb || State == MoveState.Mantle) return false;

            float face = Vector3.Angle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), -Vector3.ProjectOnPlane(_probe.Wall.normal, Vector3.up));
            // Mantle entry stays the old gate. JumpHeld is not climb cling.
            bool holdingIn = _in.Move.y > 0.2f || _in.JumpHeld;
            bool mantleOk = holdingIn && face <= cfg.climbAttachAngle && !(grounded && !_in.JumpHeld && !_in.JumpPressed);
            if (mantleOk && _probe.Ledge.hit && _probe.Ledge.height < cfg.mantleMaxLedgeHeight && v.y > -8f)
            {
                BeginMantle();
                return true;
            }

            // Climb stick is move-into-wall. A grounded walk that is not into the wall does not grab.
            if (!ClingHeld(wish)) return false;
            if (face > cfg.climbAttachAngle) return false;

            // The face just left stays closed. A different face, or the ground, opens it.
            if (WallReentryBlocked()) return false;
            if (ClimbHeightUsed >= cfg.climbMaxHeight) return false;

            _climbT = 0f;
            _climbStartY = transform.position.y;
            _wallJumpFromClimb = true;
            GrabWallFace();
            SetState(MoveState.WallClimb);
            AudioBus.Raise(AudioBus.Hook.ClingGrab, transform.position);
            return true;
        }

        Vector3 TickClimb(float dt, Vector3 v, Vector3 wish)
        {
            if (!_probe.Wall.hit)
            {
                BanLeftWall();
                _clingGrace = 0f;
                _wallJumpSlot = 0f;
                SetState(MoveState.Air);
                return v;
            }

            TrackAttachedFace();
            _climbT += dt;
            ClimbHeightUsed = transform.position.y - _climbStartY;

            if (_probe.Ledge.hit && ClimbHeightUsed > 0.2f)
            {
                BeginMantle();
                return v;
            }

            // Release does not keep the climb. Grace only covers the jump, and only while this probe is still true.
            if (_wallJumpSlot > 0f && !_launchQueued && (ClingHeld(wish) || _clingGrace > 0f))
            {
                _wallJumpFromClimb = true;
                DoWallBounce(ref v);
                return v;
            }

            bool timeOut = cfg.climbMaxTime > 0f && _climbT >= cfg.climbMaxTime;
            bool heightOut = ClimbHeightUsed >= cfg.climbMaxHeight;
            if (timeOut || heightOut || !ClingHeld(wish))
            {
                BanLeftWall();
                _wallJumpFromClimb = true;
                if (timeOut || heightOut || !_probe.Wall.hit)
                {
                    _clingGrace = 0f;
                    _wallJumpSlot = 0f;
                }
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
            Vector3 tangent = WallTangent();
            float sideAmt = Vector3.Dot(wish, tangent);
            Vector3 side = tangent * sideAmt * cfg.climbSideSpeed;

            v = up + side + stick;
            SetHeight(cfg.standingHeight);
            return v;
        }

        void BeginMantle()
        {
            _mantleT = 0f;
            _mantleEntryPlanar = HorizSpeed;
            _mantleFrom = transform.position;
            // Nudge onto the deck along wall normal - 5cm clipped Mega_/rail colliders.
            Vector3 n = Vector3.ProjectOnPlane(_probe.Ledge.wallNormal, Vector3.up);
            if (n.sqrMagnitude < 0.01f) n = _probe.Ledge.wallNormal;
            n.Normalize();
            _mantleFwd = -n;
            _mantleTo = _probe.Ledge.standPoint + n * Mathf.Max(0.12f, cfg.radius * 0.35f) + Vector3.up * 0.03f;
            SuperGlideT = -1f;
            ClimbHeightUsed = 0f;
            _jumpFatigued = false;
            _launchArc = false;
            SetState(MoveState.Mantle);
            OnMantle?.Invoke();
        }

        Vector3 TickMantle(float dt, Vector3 v)
        {
            _mantleT += dt;
            float u = Mathf.Clamp01(_mantleT / cfg.mantleDuration);
            // Fast pull then settle - Apex mantle is front-loaded.
            float s = u < 0.55f ? Mathf.SmoothStep(0f, 1f, u / 0.55f) : 1f;
            Vector3 fwd = _mantleFwd.sqrMagnitude > 0.01f ? _mantleFwd : transform.forward;
            // Arc follows actual ledge rise (not max knob) so short rails do not sky-vault.
            float rise = Mathf.Max(0.15f, _mantleTo.y - _mantleFrom.y);
            Vector3 mid = _mantleFrom + Vector3.up * (rise * 0.55f);
            Vector3 pullTarget = mid + (_mantleTo - _mantleFrom) * 0.4f;
            // The arc ends on the stand point. The old settle overshot, then the
            // last frame wrote 0.25 of that push and the mesh popped backward.
            // Duration and the exit speed are unchanged.
            Vector3 stand = _mantleTo + fwd * cfg.mantleForward * 0.25f;
            Vector3 settleTarget = stand;
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
            if (_jumpSlot > 0f && SuperGlideT >= 0f)
            {
                DoSuperGlide(ref v);
                return v;
            }

            if (u >= 1f)
            {
                transform.position = stand;
                // Keep the speed you had when the vault started. The chase
                // velocity along the way is the animation, not a launch.
                v = fwd * Mathf.Max(cfg.walkSpeed, _mantleEntryPlanar);
                SetState(MoveState.Idle);
            }
            return v;
        }

        bool TryEnterWallRun(Vector3 v, bool grounded, Vector3 wish)
        {
            if (_launchQueued) return false;
            if (!cfg.enableWallRun) return false;
            if (WallReentryBlocked()) return false;
            if (grounded) return false;
            if (!_probe.Wall.hit) return false;
            if (!ClingHeld(wish)) return false;
            if (State == MoveState.WallClimb || State == MoveState.Mantle) return false;
            if (WishAccel.HorizSpeed(v) < cfg.wallRunMinSpeed) return false;

            float face = Vector3.Angle(WishAccel.Horizontal(v), -Vector3.ProjectOnPlane(_probe.Wall.normal, Vector3.up));
            // Must glance, not slam face-first (face-first is climb)
            if (face < cfg.wallRunAttachAngle || face > 90f) return false;
            if (_in.Move.y < 0.1f && Mathf.Abs(_in.Move.x) < 0.1f) return false;

            _wallRunT = 0f;
            _wallJumpFromClimb = false;
            GrabWallFace();
            SetState(MoveState.WallRun);
            AudioBus.Raise(AudioBus.Hook.ClingGrab, transform.position);
            return true;
        }

        Vector3 TickWallRun(float dt, Vector3 v, Vector3 wish)
        {
            if (_probe.Wall.hit && !_launchQueued && _wallJumpSlot > 0f && (ClingHeld(wish) || _clingGrace > 0f))
            {
                _wallJumpFromClimb = false;
                DoWallRunJump(ref v);
                return v;
            }

            if (!_probe.Wall.hit || _wallRunT > cfg.wallRunMaxTime)
            {
                // The face stays closed after the run ends. Air accel must not restart it.
                BanLeftWall();
                _wallJumpFromClimb = false;
                if (!_probe.Wall.hit)
                {
                    _clingGrace = 0f;
                    _wallJumpSlot = 0f;
                }
                if (_wallRunT > cfg.wallRunMaxTime)
                    v.y = Mathf.Min(v.y, -4.5f);
                SetState(MoveState.Air);
                return v;
            }

            // Release drops the run the same tick. Grace does not keep the run alive.
            if (!ClingHeld(wish))
            {
                BanLeftWall();
                _wallJumpFromClimb = false;
                SetState(MoveState.Air);
                return v;
            }
            TrackAttachedFace();
            _wallRunT += dt;

            Vector3 along = WallTangent();
            float wishAlong = Vector3.Dot(wish, along);
            float velAlong = Vector3.Dot(WishAccel.Horizontal(v), along);
            float sideSign = Mathf.Abs(wishAlong) > 0.2f
                ? Mathf.Sign(wishAlong)
                : (Mathf.Abs(velAlong) > 0.05f ? Mathf.Sign(velAlong) : 1f);
            along *= sideSign;

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

            // Held cling plus the wall-jump slot. A release already returned above.
            if (ClingHeld(wish) && !_launchQueued && _wallJumpSlot > 0f) DoWallRunJump(ref v);
            return v;
        }

        /// <summary>
        /// Systems lock. ClingHeld is the Move wish into the wall: dot(wishDir, -wallNormal) greater than 0.25.
        /// Face-on climb is forward while the body faces the wall. A sideways run keeps the same dot
        /// with an into-wall strafe or a forward-strafe diagonal. JumpHeld is not cling. No Cling action.
        /// </summary>
        const float ClingIntoWall = 0.25f;

        bool ClingHeld(Vector3 wish)
        {
            if (!_probe.Wall.hit) return false;
            if (wish.sqrMagnitude < 0.0001f) return false;
            Vector3 wishDir = wish.normalized;
            return Vector3.Dot(wishDir, -_probe.Wall.normal) > ClingIntoWall;
        }

        Vector3 WallTangent()
        {
            Vector3 tangent = Vector3.Cross(_probe.Wall.normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f) tangent = transform.right;
            return tangent.normalized;
        }

        #endregion

        #region Lunge / land stun / tag

        bool TryAirDash(ref Vector3 v, Vector3 wish, float dt, bool grounded)
        {
            if (_airDashT > 0f)
            {
                // A pad rise owns this frame. The dash must not skip its gravity.
                if (_launchArc || _launchQueued)
                {
                    _airDashT = 0f;
                    if (_mode == Locomotion.AirDash)
                        _mode = Locomotion.Air;
                    return false;
                }
                _airDashT -= dt;
                v = WishAccel.SetHoriz(v, _airDashDir * cfg.airDashSpeed);
                // Keep vertical - burst, not hover/jet.
                if (_airDashT <= 0f && State != MoveState.Slide)
                {
                    _mode = grounded ? Locomotion.Ground : Locomotion.Air;
                    SetState(grounded ? MoveState.Sprint : MoveState.Air);
                }
                return true;
            }

            if (cfg == null || !cfg.enableAirDash) return false;
            if (_launchArc || _launchQueued) return false;
            if (grounded) return false;
            if (!SessionRules.AirDashAllowed(_stagger.Stagger)) return false;
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
            _mode = Locomotion.AirDash;
            SetState(MoveState.Air);
            AudioBus.Raise(AudioBus.Hook.AirDash, transform.position);
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
                // Stagger cancels the burst. A pad rise is not a lunge.
                if (_stagger.Stagger > 0f || _launchArc || _launchQueued)
                {
                    _lungeT = 0f;
                    return false;
                }
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

            // Ground It burst only - airborne MMB is consumed by TryAirDash.
            if (_stagger.Stagger > 0f || _launchArc || _launchQueued) return false;
            if (!_probe.Ground.grounded) return false;
            if (!CanTaggerLunge()) return false;
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

        /// <summary>
        /// Human MMB still needs TagRole. The campus opponent has no TagRole;
        /// PunchHitbox owns the tag, and this flag only arms the existing lunge.
        /// </summary>
        public void SetExternalTagger(bool isIt) => _externalTagger = isIt;

        bool CanTaggerLunge()
        {
            if (tagRole != null && tagRole.IsIt) return true;
            return _in != null && _in.ExternalControl && _externalTagger;
        }

        Vector3 TickLandStun(float dt, Vector3 v)
        {
            _landStunT -= dt;
            Vector3 hv = WishAccel.Friction(WishAccel.Horizontal(v), 14f, dt);
            v = WishAccel.SetHoriz(v, hv);
            v.y -= cfg.gravity * dt;
            if (_landStunT <= 0f || _jumpSlot > 0f)
            {
                SetState(_probe.Ground.grounded ? MoveState.Idle : MoveState.Air);
                if (_jumpSlot > 0f) TryJump(ref v, true);
            }
            return v;
        }

        readonly Collider[] _tagOverlap = new Collider[16];

        void TryTag()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.9f, cfg.tagRadius, _tagOverlap);
            for (int i = 0; i < count; i++)
            {
                Collider c = _tagOverlap[i];
                _tagOverlap[i] = null;
                if (c == null || c.transform == transform) continue;
                var other = TagRole.FromCollider(c);
                if (other == null || other.IsIt) continue;
                if (!tagRole.Tag(other)) continue;
                OnTaggedSomeone?.Invoke();
                if (other.Motor != null) other.Motor.NotifyBecameIt();
                break;
            }
        }

        public void NotifyBecameIt() => OnBecameIt?.Invoke();

        public void SetMotorLocked(bool locked)
        {
            bool rising = locked && !_motorLocked;
            _motorLocked = locked;
            if (rising)
            {
                DropCarrierVerbs();
                _velocity = Vector3.zero;
            }
            if (!locked)
                CloseRagdollBody();
        }

        /// <summary>Ragdoll window only. CharacterController is off until the stun ends.</summary>
        public void OpenRagdollBody()
        {
            DropCarrierVerbs();
            _mode = Locomotion.Ragdoll;
            _velocity = Vector3.zero;
            _clingGrace = 0f;
            _wallJumpSlot = 0f;
            _jumpSlot = 0f;
            if (_cc != null) _cc.enabled = false;
            if (_cap != null) _cap.enabled = true;
            if (_rb != null)
            {
                _rb.detectCollisions = true;
                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }

        void CloseRagdollBody()
        {
            if (_rb != null)
            {
                ClearDynamicVelocity(_rb);
                _rb.useGravity = false;
                _rb.isKinematic = true;
                _rb.detectCollisions = false;
            }
            if (_cap != null) _cap.enabled = false;
            if (_cc != null) _cc.enabled = true;
            _velocity = Vector3.zero;
            bool feet = _probe != null && _probe.Ground.grounded;
            _mode = feet ? Locomotion.Ground : Locomotion.Air;
            State = feet ? MoveState.Idle : MoveState.Air;
        }

        public void Halt()
        {
            DropCarrierVerbs();
            _velocity = Vector3.zero;
            _jumpSlot = 0f;
            _wallJumpSlot = 0f;
            _clingGrace = 0f;
            _stagger = default;
            _wallRunT = 0f;
            _slideT = 0f;
            _climbT = 0f;
            if (State == MoveState.WallRun || State == MoveState.WallClimb || State == MoveState.Slide
                || State == MoveState.Mantle || _mode == Locomotion.AirDash)
                SetState(MoveState.Idle);
        }

        /// <summary>Zip, pad arc, lunge, and air dash drop. Ground velocity is left for the caller.</summary>
        public void ReleaseCarriers()
        {
            DropCarrierVerbs();
        }

        /// <summary>
        /// Zip, pad, grapple, lunge, and air dash all drop. The caller sets the mode.
        /// One velocity owner remains, and it is not a stuck carrier.
        /// </summary>
        void DropCarrierVerbs()
        {
            ReleaseZip();
            ReleaseGrapple();
            _launchArc = false;
            _launchQueued = false;
            _lungeT = 0f;
            _airDashT = 0f;
            if (_mode == Locomotion.AirDash)
                _mode = Locomotion.Air;
        }

        /// <summary>
        /// Snap the pawn. CharacterController ignores a transform write while it is enabled,
        /// so the capsule is toggled around the move. Velocity and both jump slots die.
        /// </summary>
        public void Place(Vector3 worldPos) => Place(worldPos, "snap");

        /// <summary>
        /// Snap the pawn and say why. Kill-plane falls are the only surprise
        /// return. Spawn, arena, and practice restarts are intentional.
        /// </summary>
        public void Place(Vector3 worldPos, string reason)
        {
            Halt();
            if (reason == "kill-plane" || reason == "practice-restart")
                VisualBlinkAge = 0f;
            if (_cc != null) _cc.enabled = false;
            transform.position = worldPos;
            Debug.Log("snap " + (string.IsNullOrEmpty(reason) ? "snap" : reason) + " " + worldPos.ToString("F1"));
            if (_rb != null)
            {
                ClearDynamicVelocity(_rb);
                _rb.useGravity = false;
                _rb.isKinematic = true;
                _rb.detectCollisions = false;
                _rb.position = worldPos;
            }
            if (_cc != null) _cc.enabled = _mode != Locomotion.Ragdoll;
        }

        /// <summary>
        /// Kinematic bodies reject velocity writes and log. A dynamic ragdoll still stops before the kinematic swap.
        /// </summary>
        static void ClearDynamicVelocity(Rigidbody rb)
        {
            if (rb == null || rb.isKinematic) return;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        public bool IsPunchStaggered => _stagger.Stagger > 0f;
        public float StaggerRemaining => _stagger.Stagger;
        /// <summary>Climb, wall-run, or a zip cling. The verb HUD reads this. It does not steer.</summary>
        public bool ClingHeldActive =>
            State == MoveState.WallClimb || State == MoveState.WallRun || _zipRiding;

        /// <summary>Wall jumps already taken. Prompts read the edge. It does not change the bounce.</summary>
        public int WallJumpCount { get; private set; }

        /// <summary>
        /// Airborne, facing a close wall, and not already climbing or riding.
        /// Distance is the probe hit. The prompt decides how close is close.
        /// </summary>
        public bool AirborneClingFace(out float distance, out Vector3 point)
        {
            distance = 99f;
            point = transform.position;
            if (_probe == null || !_probe.Wall.hit) return false;
            if (IsGrounded || ZipRiding || LaunchArc) return false;
            if (State == MoveState.WallClimb || State == MoveState.WallRun) return false;
            distance = _probe.Wall.distance;
            point = _probe.Wall.point;
            return Vector3.Dot(transform.forward, -_probe.Wall.normal) > 0.25f;
        }

        /// <summary>
        /// Non-tag punch connect. Sprint drops. Vertical velocity is not written.
        /// A hit during the stumble or the immunity after it does not refresh.
        /// </summary>
        public bool BeginPunchStagger()
        {
            if (!SessionRules.StaggerStarts()) return false;
            if (_motorLocked) return false;
            if (!PunchStagger.TryStart(ref _stagger)) return false;
            AudioBus.Raise(AudioBus.Hook.Stagger, transform.position);
            if (State == MoveState.Sprint)
                SetState(MoveState.Walk);
            _lungeT = 0f;
            _airDashT = 0f;
            if (_mode == Locomotion.AirDash)
                _mode = Locomotion.Air;
            if (_zipRiding)
                ReleaseZip();
            return true;
        }

        /// <summary>Dummy chase sets this when the zip exit helps. A human grab ignores it.</summary>
        public void SetZipChase(bool take)
        {
            _zipChaseTake = take;
        }

        /// <summary>
        /// Cling is the move stick, the same hold as a wall. Jump is not cling.
        /// An external pawn grabs only when the chase asked for this line.
        /// </summary>
        bool ZipClingHeld()
        {
            if (_in == null) return false;
            return ZipLineRules.ClingHeld(_in.Move);
        }

        bool ZipGrabAllowed()
        {
            if (!ZipClingHeld()) return false;
            if (_in.JumpPressed) return false;
            if (_in.ExternalControl && !_zipChaseTake) return false;
            return true;
        }

        void TickZipAttach()
        {
            if (_zipRiding || _in == null) return;
            if (!ZipGrabAllowed()) return;
            if (VerbIntegration.ZipGrabBlocked(
                _stagger.Stagger > 0f, _launchArc, _launchQueued, _motorLocked,
                _mode == Locomotion.Vault, _mode == Locomotion.Ragdoll, _mode == Locomotion.LandStun))
                return;
            ZipLine.TryGrab(this);
        }

        /// <summary>
        /// One line. Rejected while this pawn's regrab cooldown on this line is running,
        /// while a stagger, a pad arc, a vault, or a ragdoll owns the body.
        /// An air dash is cancelled by the grab. The jump buffer is left for the drop.
        /// </summary>
        public bool TryBeginZip(ZipLine line)
        {
            if (!SessionRules.NewCarrierAllowed()) return false;
            if (line == null || _zipRiding) return false;
            if (VerbIntegration.ZipGrabBlocked(
                _stagger.Stagger > 0f, _launchArc, _launchQueued, _motorLocked,
                _mode == Locomotion.Vault, _mode == Locomotion.Ragdoll, _mode == Locomotion.LandStun))
                return false;
            if (!ZipGrabAllowed()) return false;
            int id = line.GetInstanceID();
            if (ZipLineRules.RegrabBlocked(id, _zipLineId, Time.time, _zipReadyAt))
                return false;
            if (State == MoveState.WallClimb || State == MoveState.WallRun)
                BanLeftWall();
            _zipRiding = true;
            _zipLine = line;
            _zipLineId = id;
            _zipCooldown = line.Cooldown;
            _zipVelocity = line.CurrentRideVelocity();
            _wallJumpSlot = 0f;
            _airDashT = 0f;
            _lungeT = 0f;
            if (_mode == Locomotion.AirDash)
                _mode = Locomotion.Air;
            ReleaseGrapple();
            line.SetRider(GetInstanceID(), true);
            _zipLoopT = 0.45f;
            AudioBus.Raise(AudioBus.Hook.ZipGrab, transform.position);
            return true;
        }

        void ReleaseZip()
        {
            bool was = _zipRiding;
            if (_zipLine != null)
                _zipLine.SetRider(GetInstanceID(), false);
            _zipRiding = false;
            _zipLine = null;
            if (!was) return;
            AudioBus.Raise(AudioBus.Hook.ZipDrop, transform.position);
            float cd = _zipCooldown > 0f ? _zipCooldown : ZipLineRules.DefaultRegrabCooldown;
            _zipReadyAt = ZipLineRules.ArmCooldown(Time.time, cd);
        }

        /// <summary>
        /// Fixed ride speed along the cable, plus a pull onto the hang.
        /// Jump writes jumpSpeed and keeps horizontal. Release and the end keep the ride.
        /// Does not clear the same-wall ban.
        /// </summary>
        Vector3 ApplyZipRide(Vector3 v, float dt)
        {
            if (!_zipRiding)
                return v;
            if (_zipLine == null || !_zipLine.isActiveAndEnabled)
            {
                VerbIntegration.ZipLeave dropped = VerbIntegration.LeaveZip(
                    _zipVelocity, 0f, false, false, _coyote, _jumpSlot, _wallJumpSlot);
                v = _zipVelocity.sqrMagnitude > 1e-8f ? dropped.Velocity : v;
                _coyote = dropped.Coyote;
                _jumpSlot = dropped.JumpSlot;
                _wallJumpSlot = dropped.WallJumpSlot;
                ReleaseZip();
                SetState(MoveState.Air);
                return v;
            }
            if (_stagger.Stagger > 0f || _motorLocked)
            {
                VerbIntegration.ZipLeave dropped = VerbIntegration.LeaveZip(
                    _zipLine.CurrentRideVelocity(), 0f, false, false, _coyote, _jumpSlot, _wallJumpSlot);
                v = dropped.Velocity;
                _coyote = dropped.Coyote;
                _jumpSlot = dropped.JumpSlot;
                _wallJumpSlot = dropped.WallJumpSlot;
                ReleaseZip();
                SetState(MoveState.Air);
                return v;
            }

            Vector3 ride = _zipLine.CurrentRideVelocity();
            _zipVelocity = ride;
            _launchArc = false;
            bool end = _zipLine.AtExit(transform.position, dt);
            bool release = !ZipClingHeld();
            bool jump = _in != null && _in.JumpPressed;
            if (end || release || jump)
            {
                float jumpSpeed = cfg != null ? cfg.jumpSpeed : 24.7f;
                VerbIntegration.ZipLeave leave = VerbIntegration.LeaveZip(
                    ride, jumpSpeed, jump, end, _coyote, _jumpSlot, _wallJumpSlot);
                v = leave.Velocity;
                _coyote = leave.Coyote;
                _jumpSlot = leave.JumpSlot;
                _wallJumpSlot = leave.WallJumpSlot;
                ReleaseZip();
                if (leave.Jumped)
                {
                    _jumpFatigued = true;
                    _lastLanded = Time.time;
                    OnJumped?.Invoke();
                }
                SetState(MoveState.Air);
                return v;
            }

            SetState(MoveState.Air);
            _zipLoopT -= dt;
            if (_zipLoopT <= 0f)
            {
                _zipLoopT = 0.45f;
                AudioBus.Raise(AudioBus.Hook.ZipLoop, transform.position);
            }
            return _zipLine.RideVelocityWithHang(transform.position, dt);
        }

        /// <summary>
        /// One pad step. Consumed as a velocity set before the single Move.
        /// Rejected while this pawn's cooldown is still running.
        /// </summary>
        public bool QueueLaunch(float apexHeight, Vector3 horizontalVelocity, bool setHorizontal, float cooldownSeconds)
        {
            if (!SessionRules.NewCarrierAllowed()) return false;
            if (!LaunchPadRules.CooldownOpen(Time.time, _launchReadyAt))
                return false;
            if (apexHeight <= 0.001f) return false;
            _launchQueued = true;
            _launchApex = apexHeight;
            _launchHoriz = horizontalVelocity;
            _launchSetHoriz = setHorizontal;
            _launchCooldown = cooldownSeconds > 0f ? cooldownSeconds : LaunchPadRules.DefaultCooldown;
            return true;
        }

        /// <summary>
        /// Replaces vertical with the pad apex. Horizontal is replaced only when the pad sets it.
        /// A cling leave is banned. The flight itself does not clear that ban. Landing still does.
        /// A zip in progress is dropped so the pad is the only velocity.
        /// </summary>
        Vector3 ApplyQueuedLaunch(Vector3 v)
        {
            if (!_launchQueued) return v;
            _launchQueued = false;
            if (cfg == null) return v;
            if (_mode == Locomotion.Ragdoll || _mode == Locomotion.Vault || _mode == Locomotion.LandStun)
                return v;
            float vy = LaunchPadRules.VerticalSpeed(_launchApex, cfg.gravity);
            if (vy <= 0.01f) return v;
            if (_zipRiding)
                ReleaseZip();
            if (VerbIntegration.PadLeavesWall(
                State == MoveState.WallClimb || _mode == Locomotion.Climb,
                State == MoveState.WallRun || _mode == Locomotion.WallRun))
                BanLeftWall();
            _lungeT = 0f;
            _airDashT = 0f;
            if (_mode == Locomotion.AirDash)
                _mode = Locomotion.Air;
            ReleaseGrapple();
            v = VerbIntegration.ApplyPadVelocity(v, _launchApex, cfg.gravity, _launchHoriz, _launchSetHoriz);
            _jumpSlot = 0f;
            _wallJumpSlot = 0f;
            _coyote = 0f;
            _launchArc = true;
            _launchReadyAt = LaunchPadRules.ArmCooldown(Time.time, _launchCooldown);
            SetHeight(cfg.standingHeight);
            SetState(MoveState.Air);
            AudioBus.Raise(AudioBus.Hook.PadLaunch, transform.position);
            Tag.Settings.PadRumble.PulseId(gameObject.GetInstanceID(), Tag.Settings.PadRumble.PadLaunch);
            return v;
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
            OpenRagdollBody();
            if (_rb != null)
                _rb.AddForce(knock, ForceMode.VelocityChange);
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
        /// The face left by a wall jump, a release, a slip, or a spent grace stays closed.
        /// Ground opens it. Touching a different face opens it. A timer does not.
        /// Cling grace for the face you are still on is not written here.
        /// </summary>
        void TickWallContactGates()
        {
            bool onWallState = State == MoveState.WallClimb || State == MoveState.WallRun;
            // A zip ride is not a wall and not a landing. Pad flight is not a landing either.
            // The ban stays until the feet plant and the arc is over.
            if (VerbIntegration.ClearsWallBan(_probe.Ground.grounded, onWallState, _zipRiding, _launchArc))
            {
                ClearWallBan();
                ClimbHeightUsed = 0f;
                return;
            }

            if (onWallState || !_probe.Wall.hit || !_wallBan.Active)
                return;

            SameWallLimit.NoteTouch(ref _wallBan, ProbeFace());
            if (!_wallBan.Active)
            {
                _wallRunBlocked = false;
                _climbBlocked = false;
            }
        }

        bool WallReentryBlocked()
        {
            if (_probe != null && _probe.Wall.hit && SameWallLimit.Blocks(_wallBan, ProbeFace()))
            {
                _climbBlocked = true;
                _wallRunBlocked = true;
                return true;
            }
            return _climbBlocked || _wallRunBlocked;
        }

        bool EvaluateClingRefused(Vector3 wish)
        {
            if (_clingGrace > 0f) return false;
            if (State == MoveState.WallClimb || State == MoveState.WallRun || State == MoveState.Mantle)
                return false;
            if (_probe == null || !_probe.Wall.hit) return false;
            if (!SameWallLimit.Blocks(_wallBan, ProbeFace())) return false;
            return ClingHeld(wish);
        }

        SameWallLimit.Face ProbeFace()
        {
            int id = _probe.Wall.collider != null ? _probe.Wall.collider.GetInstanceID() : 0;
            return SameWallLimit.Make(id, _probe.Wall.normal, _probe.Wall.point);
        }

        void GrabWallFace()
        {
            _attachedFace = ProbeFace();
            _wallBan = default;
            _climbBlocked = false;
            _wallRunBlocked = false;
        }

        void TrackAttachedFace()
        {
            if (_probe == null || !_probe.Wall.hit) return;
            SameWallLimit.Face face = ProbeFace();
            if (!_attachedFace.Valid || SameWallLimit.SameFace(_attachedFace, face))
                _attachedFace = face;
        }

        void BanLeftWall()
        {
            SameWallLimit.Face face = _attachedFace;
            if (_probe != null && _probe.Wall.hit)
            {
                SameWallLimit.Face now = ProbeFace();
                if (!face.Valid || SameWallLimit.SameFace(face, now))
                    face = now;
            }
            SameWallLimit.NoteLeave(ref _wallBan, face);
            _climbBlocked = true;
            _wallRunBlocked = true;
        }

        void ClearWallBan()
        {
            _wallBan = default;
            _attachedFace = default;
            _climbBlocked = false;
            _wallRunBlocked = false;
        }

        void LatchLandImpact()
        {
            bool g = _probe.Ground.grounded;
            if (g && !_wasProbeGrounded)
            {
                _launchArc = false;
                float impact = Mathf.Max(0f, -_velocity.y);
                _lastLandImpactSpeed = impact;
                _lastLanded = Time.time;
                // Air dash uses time cooldown (not land refresh).

                // Landing shock (Apex) - only from true air/jet, not ski kisses.
                // Harder impacts hold stun a touch longer (clamped).
                if ((State == MoveState.Air || State == MoveState.Jet) && impact >= cfg.landStunSpeed && !_zipRiding)
                {
                    float over = Mathf.InverseLerp(cfg.landStunSpeed, cfg.maxFallSpeed, impact);
                    _landStunT = cfg.landStunDuration * Mathf.Lerp(1f, 1.35f, over);
                    SetState(MoveState.LandStun);
                }
                else if (impact >= 5f)
                {
                    // Hard land already thuds via MoveAnimDriver on LandStun. This is the step-down.
                    AudioBus.Raise(AudioBus.Hook.LandSoft, transform.position);
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
            else if (_stagger.Stagger <= 0f && (_in.SprintHeld || hs > cfg.sprintSpeed * 0.82f)) SetState(MoveState.Sprint);
            else SetState(MoveState.Walk);
        }

        void SetState(MoveState next)
        {
            if (_mode != Locomotion.AirDash || next != MoveState.Air)
                _mode = ModeFor(next);
            if (State == next) return;
            var prev = State;
            State = next;
            if (prev == MoveState.Slide && next != MoveState.Slide)
                AudioBus.Raise(AudioBus.Hook.SlideEnd, transform.position);
            if (next == MoveState.Ski && prev != MoveState.Ski)
            {
                var src = TagSfx.EnsureSource(gameObject);
                if (src != null) TagSfx.SkiStart(src);
            }
            OnStateChanged?.Invoke(prev, next);
        }

        static Locomotion ModeFor(MoveState next)
        {
            switch (next)
            {
                case MoveState.Slide: return Locomotion.Slide;
                case MoveState.Ski: return Locomotion.Ski;
                case MoveState.Air:
                case MoveState.Jet: return Locomotion.Air;
                case MoveState.WallClimb: return Locomotion.Climb;
                case MoveState.Mantle: return Locomotion.Vault;
                case MoveState.WallRun: return Locomotion.WallRun;
                case MoveState.LandStun: return Locomotion.LandStun;
                default: return Locomotion.Ground;
            }
        }

        void TickTimers(float dt)
        {
            if (_speedBoostT > 0f)
            {
                _speedBoostT -= dt;
                if (_speedBoostT <= 0f) ClearSpeedBoost();
            }
            _coyote = KinematicStep.DecayCoyote(_coyote, dt);
            if (_jumpSlot > 0f) _jumpSlot -= dt;
            if (_wallJumpSlot > 0f) _wallJumpSlot -= dt;
            if (_tapCd > 0f) _tapCd -= dt;
            if (_lungeCd > 0f) _lungeCd -= dt;
            if (_airDashCd > 0f) _airDashCd -= dt;
            if (_airDashIFramesT > 0f) _airDashIFramesT -= dt;
            if (_energyRegenDelay > 0f) _energyRegenDelay -= dt;
            PunchStagger.Tick(ref _stagger, dt);

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
            bool skiJetOrSlide = State == MoveState.Ski || State == MoveState.Jet || State == MoveState.Slide;
            float cap = KinematicStep.LocomotionPlanarCap(cfg.skiMaxSpeed, skiJetOrSlide);
            hv = WishAccel.ClampPlanarSpeed(hv, cap);

            if (!_stableFeet && !Jetting)
                hv = Vector3.Lerp(hv, hv.normalized * Mathf.Min(hv.magnitude, cfg.skiMaxSpeed), cfg.skiAirDrag * dt);

            return WishAccel.SetHoriz(v, hv);
        }

        void TickHeight(float dt)
        {
            if (_cap == null) return;
            _cap.height = Mathf.MoveTowards(_cap.height, _height, 8f * dt);
            _cap.center = new Vector3(0f, _cap.height * 0.5f, 0f);
            if (_cc == null) return;
            _cc.height = _cap.height;
            _cc.radius = _cap.radius;
            _cc.center = _cap.center;
        }

        /// <summary>
        /// Plant stick, bury ease, and ceiling kiss. All of them rewrite velocity for the
        /// one Move. None of them write the transform.
        /// </summary>
        Vector3 ApplyGroundRead(Vector3 v, float dt)
        {
            if (cfg == null) return v;
            bool feet = _stableFeet;
            bool walkable = _probe != null && _probe.Ground.walkable && _probe.Ground.normal.sqrMagnitude > 0.01f;
            bool stickMode = _mode == Locomotion.Ground || _mode == Locomotion.Slide || _mode == Locomotion.Ski
                || _mode == Locomotion.LandStun;
            if (stickMode && feet && walkable)
                v.y = KinematicStep.SteepLandStick(v.y, Vector3.Dot(v, _probe.Ground.normal));
            if ((_mode == Locomotion.Ground || _mode == Locomotion.Slide || _mode == Locomotion.Ski)
                && feet && v.y > -0.5f && v.y < KinematicStep.LaunchVy)
                v.y = KinematicStep.PlantStickVy;

            if (_probe != null && _probe.Ground.grounded && _probe.Ground.walkable)
                v.y = KinematicStep.BuriedEaseVy(v.y, _probe.Ground.feetGap, cfg.skin, dt);

            float skin = Mathf.Max(0.02f, cfg.skin);
            float rise = v.y > 0f ? v.y * dt : 0f;
            float gap = _probe != null ? _probe.CeilingGap : KinematicStep.OpenCeiling;
            float kissed = KinematicStep.CeilingKissRise(rise, gap, skin);
            if (kissed < rise - 0.0001f || kissed < -0.0001f)
            {
                float vy = dt > 1e-6f ? kissed / dt : 0f;
                v.y = KinematicStep.CapKissVy(vy);
            }
            return v;
        }

        bool CeilingClose()
        {
            if (cfg == null) return false;
            float skin = Mathf.Max(0.02f, cfg.skin);
            float height = _cc != null && _cc.height > 0.05f ? _cc.height : cfg.standingHeight;
            float radius = _cc != null && _cc.radius > 0.01f ? _cc.radius : cfg.radius;
            float gap = _probe != null ? _probe.CeilingGap : KinematicStep.OpenCeiling;
            float step = KinematicStep.StepOffset(height, radius, skin, false);
            return KinematicStep.CeilingSuppressStep(gap, skin, step);
        }

        /// <summary>
        /// One writer for skin, slope, and step. A launch, a low ceiling, or a wall contact
        /// drops the step so the controller cannot snap the jump, kiss the ceiling, or hop
        /// up the face. On the ground, slope follows the walkable angle. Climb and wall-run
        /// keep 90° so their into-wall stick is not rejected. Skin stays the config value.
        /// </summary>
        void FitController(float verticalVelocity, bool ceilingClose)
        {
            if (_cc == null || cfg == null) return;
            float skin = Mathf.Max(0.02f, cfg.skin);
            float height = _cc.height > 0.05f ? _cc.height : cfg.standingHeight;
            float radius = _cc.radius > 0.01f ? _cc.radius : cfg.radius;
            bool wall = _mode == Locomotion.Climb || _mode == Locomotion.WallRun || _mode == Locomotion.Vault;
            bool rising = verticalVelocity > KinematicStep.LaunchVy || ceilingClose;
            _cc.skinWidth = skin;
            _cc.slopeLimit = KinematicStep.SlopeLimit(cfg.maxWalkableAngle, wall);
            _cc.minMoveDistance = 0f;
            _cc.stepOffset = KinematicStep.StepOffset(height, radius, skin, rising || wall);
        }

        void SetHeight(float h) => _height = h;

        void ApplyCapsule()
        {
            _cap.height = cfg.standingHeight;
            _cap.radius = cfg.radius;
            _cap.center = new Vector3(0f, cfg.standingHeight * 0.5f, 0f);
            _cap.direction = 1;
            _cap.enabled = _mode == Locomotion.Ragdoll;
            if (_cc == null) return;
            _cc.height = _cap.height;
            _cc.radius = _cap.radius;
            _cc.center = _cap.center;
            FitController(0f, false);
        }

        void DriveAnimator()
        {
            if (!animator) return;
            animator.applyRootMotion = false;
            animator.SetInteger(AnimIds.State, (int)State);
            animator.SetFloat(AnimIds.Speed, HorizSpeed);
            animator.SetFloat(AnimIds.VertSpeed, _velocity.y);
            animator.SetBool(AnimIds.Grounded, _stableFeet);
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

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_mode != Locomotion.Ski && _mode != Locomotion.Slide) return;
            if (hit == null || hit.normal.sqrMagnitude < 0.01f) return;
            if (Vector3.Angle(hit.normal, Vector3.up) < 55f) return;
            Vector3 hv = WishAccel.Horizontal(_velocity);
            if (hv.sqrMagnitude < 0.01f) return;
            if (Vector3.Dot(hv.normalized, -hit.normal) <= 0.72f) return;
            Vector3 n = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
            if (n.sqrMagnitude < 0.01f) return;
            Vector3 bounced = Vector3.Reflect(hv, n.normalized);
            _velocity = WishAccel.SetHoriz(_velocity, bounced * 0.45f);
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
