using Tag.Audio;
using Tag.Experimental;
using Tag.FX;
using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Plays a verb exit after DummyLocomotor has written the next pose.
    /// The exit peels off on jump, slide, punch, dash, or lunge.
    /// No second CharacterController move. No root motion on the motor.
    /// </summary>
    [DefaultExecutionOrder(8000)]
    public sealed class VerbExitRider : MonoBehaviour
    {
        const float GrappleCatch = 1.35f;

        PlayerMotor _motor;
        PlayerInputReader _input;
        PunchHitbox _punch;
        ExperimentalGrapple _grapple;
        ItController _it;
        FxBurstPool _fx;
        int _vaultCol;
        int _vaultSurf;

        Transform _hips, _spine, _head;
        Transform _uaL, _uaR, _laL, _laR;
        Transform _ulL, _ulR, _llL, _llR;
        Transform _ftL, _ftR;
        Quaternion _hips0, _spine0, _head0;
        Quaternion _uaL0, _uaR0, _laL0, _laR0;
        Quaternion _ulL0, _ulR0, _llL0, _llR0;
        Quaternion _ftL0, _ftR0;
        bool _bones;

        VerbExitId _id;
        float _age;
        float _cancel;
        float _fallScale;
        bool _stepDown;
        bool _shoulderLeft;
        float _travelYaw;
        bool _dusted;
        VerbExitSample _shown;
        VerbExitSample _from;
        float _blendAge;
        bool _chain;
        int _alternate;
        bool _onLeftWall;
        bool _trailLeft;
        bool _fromMantle;
        bool _wallHold;
        float _wallHoldAge;
        float _exitVy;
        float _ropeElev;
        bool _ropeHave;
        DummyLocomotor _loco;
        Transform _handL, _handR;
        bool _pinHandL, _pinHandR, _pinFootL, _pinFootR;
        Vector3 _anchorHandL, _anchorHandR, _anchorFootL, _anchorFootR;
        RaycastHit _plantHit;

        bool _primed;
        MoveState _prevState;
        bool _prevGrounded;
        bool _prevDash;
        bool _prevLunge;
        bool _prevZip;
        bool _prevLaunch;
        bool _prevStagger;
        bool _prevPull;
        bool _prevPunch;
        float _prevTagBack;
        bool _mantleFromClimb;
        float _mantlePlanar;
        bool _launchPending;
        bool _arriveSent;

        public static void Ensure(GameObject host)
        {
            if (host == null) return;
            if (host.GetComponent<VerbExitRider>() != null) return;
            host.AddComponent<VerbExitRider>();
        }

        /// <summary>Exit currently showing. None when the gait owns the body.</summary>
        public VerbExitId Active => _id;

        void Awake()
        {
            CacheBones();
            _motor = GetComponentInParent<PlayerMotor>();
            _input = GetComponentInParent<PlayerInputReader>();
            _punch = GetComponentInParent<PunchHitbox>();
            _grapple = GetComponentInParent<ExperimentalGrapple>();
            _it = GetComponentInParent<ItController>();
            Transform host = _motor != null ? _motor.transform : transform;
            _fx = FxBurstPool.Ensure(host);
            _loco = GetComponentInParent<DummyLocomotor>();
            _cancel = -1f;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt < 0f) dt = 0f;
            if (!_primed)
            {
                Remember();
                _primed = true;
                return;
            }

            VerbExitId before = _id;
            VerbExitId next = Detect();
            if (next == VerbExitId.WallJump)
            {
                _wallHold = true;
                _wallHoldAge = 0f;
                next = VerbExitId.None;
            }
            if (_wallHold)
            {
                _wallHoldAge += dt;
                float span = WallJumpPose.BeatSeconds + WallJumpPose.EaseSeconds;
                if (next != VerbExitId.None)
                    _wallHold = false;
                else if (_wallHoldAge >= span)
                {
                    _wallHold = false;
                    next = VerbExitId.WallJump;
                }
            }
            if (next != VerbExitId.None)
                Begin(next);

            bool jump = _input != null && _input.JumpPressed;
            bool slide = _input != null && _input.CrouchPressed;
            bool punch = _input != null && _input.PunchPressed;
            bool dash = _input != null && _input.AirDashPressed;
            bool lunge = _input != null && _input.LungePressed;
            if (VerbExitPick.Cancels(_id, jump, slide, punch, dash, lunge))
                ArmCancel();
            if (before == _id && VerbExitPick.CancelsState(_id, EnteredSlide(), EnteredWall(), EnteredMantle(), EnteredDash(), EnteredLunge(), EnteredPunch()))
                ArmCancel();

            if (_id != VerbExitId.None)
            {
                _age += dt;
                if (_cancel >= 0f) _cancel += dt;
                float dur = VerbExitClock.Duration(_id);
                float w = VerbExitClock.Weight(_age, dur, _cancel);
                bool done = w <= 0.001f && (_age >= dur || _cancel >= VerbExitClock.CancelSeconds);
                if (done)
                    Clear();
                else
                    Apply(w, dt);
            }

            Remember();
        }

        VerbExitId Detect()
        {
            if (_motor == null) return VerbExitId.None;
            MoveState st = _motor.State;
            bool grounded = _motor.IsGrounded;
            float vy = _motor.Velocity.y;
            VerbExitId pick = VerbExitId.None;
            bool mantleExit = false;

            if (st == MoveState.WallRun || st == MoveState.WallClimb)
                _onLeftWall = _motor.WallLeft;

            bool wasWall = _prevState == MoveState.WallRun || _prevState == MoveState.WallClimb;
            if (wasWall && st != _prevState)
            {
                VerbExitId wall = VerbExitPick.WallLeave(
                    _prevState == MoveState.WallRun,
                    _prevState == MoveState.WallClimb,
                    st == MoveState.Mantle,
                    st == MoveState.Air || st == MoveState.Jet,
                    vy);
                if (wall != VerbExitId.None)
                {
                    _stepDown = VerbExitPick.StepDown(grounded, vy);
                    _shoulderLeft = _onLeftWall;
                    pick = wall;
                }
            }

            if (st == MoveState.Mantle && _prevState != MoveState.Mantle)
            {
                _mantleFromClimb = _prevState == MoveState.WallClimb;
                _mantlePlanar = _motor.HorizontalSpeed;
                _trailLeft = NextLead();
                if (_fx != null)
                {
                    int surf = DustContact.Read(_motor.Ground.collider, ref _vaultCol, ref _vaultSurf);
                    DustLook.Puff puff = DustLook.At(surf, _motor.HorizSpeed, (int)DustLook.Kick.None);
                    Vector3 hand = _motor.transform.position + Vector3.up * 1.15f;
                    _fx.PlayShaped(FxBurstKind.VaultPuff, hand, puff);
                }
            }
            if (_prevState == MoveState.Mantle && st != MoveState.Mantle)
            {
                _shoulderLeft = _trailLeft;
                mantleExit = true;
                pick = VerbExitPick.MantleLeave(_mantleFromClimb, _mantlePlanar);
            }

            if (_prevState == MoveState.Slide && st != MoveState.Slide)
            {
                bool intoAir = st == MoveState.Air || st == MoveState.Jet;
                if (VerbExitPick.PlaySlideExit(intoAir, vy))
                {
                    _shoulderLeft = NextLead();
                    pick = VerbExitId.Slide;
                }
            }

            if (_prevDash && !_motor.IsAirDashing)
                pick = VerbExitId.AirDash;
            if (_prevLunge && !_motor.IsLunging)
                pick = VerbExitId.Lunge;
            if (_prevPunch && !Punching())
                pick = VerbExitId.Punch;
            if (_prevZip && !_motor.ZipRiding)
                pick = VerbExitId.ZipDrop;
            if (_prevStagger && !_motor.IsPunchStaggered)
                pick = VerbExitId.Stagger;

            float tagBack = _it != null ? _it.TagBackRemaining : 0f;
            if (_prevTagBack > 0.001f && tagBack <= 0.001f)
                pick = VerbExitId.TagBackEnd;

            NoteGrapple(ref pick);

            if (_prevLaunch && !_motor.LaunchArc)
                _launchPending = true;

            if (grounded && !_prevGrounded)
            {
                float impact = _motor.LastLandImpactSpeed;
                float planar = Planar();
                if (LandingRollPose.Triggered(impact))
                {
                    bool still = LandingRollPose.Stationary(planar);
                    _shoulderLeft = !still && NextLead();
                    _travelYaw = still ? 0f : TravelYaw();
                    _fallScale = 1f;
                    _stepDown = false;
                    pick = still ? VerbExitId.RollAbsorb : VerbExitId.Roll;
                    _launchPending = false;
                }
                else if (_launchPending)
                {
                    _fallScale = 1f;
                    _travelYaw = 0f;
                    _shoulderLeft = false;
                    pick = VerbExitId.LaunchLand;
                    _launchPending = false;
                }
                else if (impact >= LandingRollPose.SoftFloor)
                {
                    _fallScale = LandingRollPose.TierScale(impact);
                    _travelYaw = 0f;
                    _shoulderLeft = false;
                    pick = VerbExitId.SoftLand;
                }
                else
                    _launchPending = false;
            }

            if (pick == VerbExitId.ClimbTopOut || pick == VerbExitId.Vault || pick == VerbExitId.Mantle)
                _fromMantle = mantleExit;
            return pick;
        }

        void NoteGrapple(ref VerbExitId pick)
        {
            if (_grapple == null) return;
            bool pulling = _grapple.Pulling;
            bool attached = _grapple.IsPulling;
            if (pulling && !_arriveSent && CloseToAnchor())
            {
                pick = VerbExitId.GrappleArrive;
                _arriveSent = true;
            }
            if (_prevPull && !attached)
            {
                pick = VerbExitId.GrappleRelease;
                _arriveSent = false;
            }
            if (!attached)
                _arriveSent = false;
        }

        bool CloseToAnchor()
        {
            if (_motor == null || _grapple == null) return false;
            Vector3 anchor;
            float length;
            float slack;
            if (!_grapple.TryGetRope(out anchor, out length, out slack)) return false;
            Vector3 d = anchor - _motor.transform.position;
            return d.sqrMagnitude <= GrappleCatch * GrappleCatch;
        }

        void Begin(VerbExitId id)
        {
            if (id == VerbExitId.None) return;
            bool chaining = _id != VerbExitId.None;
            if (chaining)
            {
                _from = _shown;
                _blendAge = 0f;
                _chain = true;
            }
            else
                _chain = false;
            _id = id;
            _age = 0f;
            _cancel = -1f;
            _dusted = false;
            if (id != VerbExitId.SoftLand && id != VerbExitId.Roll && id != VerbExitId.RollAbsorb)
                _fallScale = 1f;
            bool keepSide = id == VerbExitId.Roll || id == VerbExitId.WallRun || id == VerbExitId.WallJump
                || id == VerbExitId.Vault || id == VerbExitId.Slide || id == VerbExitId.ClimbTopOut;
            if (!keepSide)
                _shoulderLeft = false;
            if (id != VerbExitId.Roll)
                _travelYaw = 0f;
            if (_motor != null) _exitVy = _motor.Velocity.y;
        }

        void ArmCancel()
        {
            if (_id == VerbExitId.None) return;
            if (_cancel < 0f) _cancel = 0f;
        }

        void Clear()
        {
            _id = VerbExitId.None;
            _age = 0f;
            _cancel = -1f;
            _dusted = false;
            _fromMantle = false;
            ReleasePins();
        }

        void Apply(float w, float dt)
        {
            if (w <= 0.001f)
            {
                ReleasePins();
                return;
            }
            float show = w;
            if (!_chain)
                show *= VerbExitEase.Enter(_id, _age);
            if (show <= 0.001f)
            {
                ReleasePins();
                return;
            }
            float unit = Unit(_age);
            VerbExitSample target = VerbExitClips.At(_id, unit, _fallScale, _stepDown, _shoulderLeft);
            float phase = _loco != null ? _loco.SurfPhase : 0f;
            float gait = _loco != null ? _loco.GaitCycle : 0f;
            Facing(out float fwd, out float side);
            target = VerbExitFit.Apply(
                target, _id, unit, _shoulderLeft, _fromMantle, phase, fwd, side,
                _exitVy, gait, _ropeElev, _ropeHave);
            VerbExitSample s = target;
            if (_chain)
            {
                _blendAge += dt;
                s = VerbExitChain.Blend(_from, target, _blendAge);
                if (_blendAge >= VerbExitChain.BlendSeconds)
                    _chain = false;
            }
            _shown = s;
            Blend(_hips, _hips0, s.Hip, s.HipYaw, s.HipRoll, show);
            Blend(_spine, _spine0, s.Spine, s.SpineYaw, s.SpineRoll, show);
            Blend(_head, _head0, s.Head, s.HeadYaw, 0f, show);
            Blend(_uaL, _uaL0, s.ArmPitchL, s.ArmYawL, s.ArmRollL, show);
            Blend(_uaR, _uaR0, s.ArmPitchR, s.ArmYawR, s.ArmRollR, show);
            Blend(_laL, _laL0, s.ElbowL, 0f, 0f, show);
            Blend(_laR, _laR0, s.ElbowR, 0f, 0f, show);
            Blend(_ulL, _ulL0, s.ThighL, s.ThighYawL, 0f, show);
            Blend(_ulR, _ulR0, s.ThighR, s.ThighYawR, 0f, show);
            Blend(_llL, _llL0, s.KneeL, 0f, 0f, show);
            Blend(_llR, _llR0, s.KneeR, 0f, 0f, show);
            Blend(_ftL, _ftL0, s.FootL, 0f, 0f, show);
            Blend(_ftR, _ftR0, s.FootR, 0f, 0f, show);
            // EaseFacing already wrote the yaw this frame. The exit sits on top of it.
            if (_id == VerbExitId.Roll)
            {
                float spin = s.RootSpin * show;
                Vector3 axis = LandingRollPose.Axis(_shoulderLeft);
                Quaternion q = Quaternion.AngleAxis(spin, axis);
                transform.localRotation = transform.localRotation
                    * Quaternion.Euler(0f, _travelYaw * show, 0f)
                    * q;
                Vector3 p = transform.localPosition;
                p += LandingRollPose.OrbitDelta(LandingRollPose.Pivot(_shoulderLeft), axis, spin);
                p.y += LandingRollPose.FloorShift(s, spin, _shoulderLeft);
                transform.localPosition = p;
            }
            else
            {
                transform.localRotation = transform.localRotation
                    * Quaternion.Euler(s.RootPitch * show, 0f, s.RootRoll * show);
                if (s.Drop > 0f)
                {
                    Vector3 p = transform.localPosition;
                    p.y -= s.Drop * show;
                    transform.localPosition = p;
                }
            }
            MaybeDust(s);
            if (_cancel >= 0f)
                ReleasePins();
            else
                PlantContact(unit);
        }

        void MaybeDust(VerbExitSample s)
        {
            if (_dusted) return;
            if (_id != VerbExitId.Roll && _id != VerbExitId.RollAbsorb) return;
            if (_age < LandingRollPose.DustAt) return;
            _dusted = true;
            Vector3 pos = _motor != null ? _motor.transform.position : transform.position;
            pos.y += 0.08f;
            if (_fx != null)
                _fx.Play(FxBurstKind.Roll, pos);
            AudioBus.Raise(AudioBus.Hook.LandingRoll, pos);
        }

        float Unit(float age)
        {
            float dur = VerbExitClock.Duration(_id);
            if (dur <= 0.0001f) return 1f;
            float u = age / dur;
            if (u < 0f) return 0f;
            if (u > 1f) return 1f;
            return u;
        }

        static void Blend(Transform bone, Quaternion rest, float pitch, float yaw, float roll, float w)
        {
            if (bone == null) return;
            Quaternion goal = rest * Quaternion.Euler(pitch, yaw, roll);
            bone.localRotation = Quaternion.Slerp(bone.localRotation, goal, w);
        }

        void NoteRope()
        {
            if (_grapple == null || _motor == null || !_grapple.IsPulling) return;
            Vector3 anchor;
            float length;
            float slack;
            if (!_grapple.TryGetRope(out anchor, out length, out slack)) return;
            Vector3 local = _motor.transform.InverseTransformDirection(anchor - _motor.transform.position);
            _ropeElev = GrapplePose.ElevDegrees(local.y, local.x, local.z);
            _ropeHave = true;
        }

        void Remember()
        {
            if (_motor == null) return;
            _prevState = _motor.State;
            _prevGrounded = _motor.IsGrounded;
            _prevDash = _motor.IsAirDashing;
            _prevLunge = _motor.IsLunging;
            _prevZip = _motor.ZipRiding;
            _prevLaunch = _motor.LaunchArc;
            NoteRope();
            _prevStagger = _motor.IsPunchStaggered;
            _prevPull = _grapple != null && _grapple.IsPulling;
            _prevPunch = Punching();
            _prevTagBack = _it != null ? _it.TagBackRemaining : 0f;
        }

        bool Punching()
        {
            return _punch != null && _punch.Phase != PunchPhase.Idle;
        }

        bool EnteredSlide()
        {
            return _motor != null && _motor.State == MoveState.Slide && _prevState != MoveState.Slide;
        }

        bool EnteredWall()
        {
            if (_motor == null) return false;
            MoveState st = _motor.State;
            bool wall = st == MoveState.WallRun || st == MoveState.WallClimb;
            bool was = _prevState == MoveState.WallRun || _prevState == MoveState.WallClimb;
            return wall && !was;
        }

        bool EnteredMantle()
        {
            return _motor != null && _motor.State == MoveState.Mantle && _prevState != MoveState.Mantle;
        }

        bool EnteredDash()
        {
            return _motor != null && _motor.IsAirDashing && !_prevDash;
        }

        bool EnteredLunge()
        {
            return _motor != null && _motor.IsLunging && !_prevLunge;
        }

        bool EnteredPunch()
        {
            return Punching() && !_prevPunch;
        }

        float Planar()
        {
            Vector3 v = _motor.Velocity;
            return Mathf.Sqrt(v.x * v.x + v.z * v.z);
        }

        float TravelYaw()
        {
            Vector3 v = _motor.Velocity;
            v.y = 0f;
            if (v.sqrMagnitude < 0.04f) return 0f;
            Vector3 f = _motor.transform.forward;
            f.y = 0f;
            float fl = Mathf.Sqrt(f.x * f.x + f.z * f.z);
            float vl = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            if (fl < 0.0001f || vl < 0.0001f) return 0f;
            float dot = (f.x * v.x + f.z * v.z) / (fl * vl);
            float cross = (f.z * v.x - f.x * v.z) / (fl * vl);
            return Mathf.Atan2(cross, dot) * Mathf.Rad2Deg;
        }

        bool NextLead()
        {
            float lateral = 0f;
            if (_motor != null)
            {
                Vector3 v = _motor.Velocity;
                Vector3 r = _motor.transform.right;
                lateral = v.x * r.x + v.z * r.z;
            }
            if (lateral >= -0.45f && lateral <= 0.45f)
                _alternate = _alternate == 0 ? 1 : 0;
            return LandingRollPose.LeadLeft(lateral, _alternate);
        }

        void CacheBones()
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            _hips = Find(all, "Hips", "Pelvis");
            _spine = Find(all, "Spine", "Torso", "Spine1", "Chest");
            _head = Find(all, "Head");
            _uaL = Find(all, "UpperArm_L", "LeftArm", "LeftUpperArm", "Arm_L");
            _uaR = Find(all, "UpperArm_R", "RightArm", "RightUpperArm", "Arm_R");
            _laL = Find(all, "LowerArm_L", "LeftForeArm", "LeftLowerArm", "ForeArm_L");
            _laR = Find(all, "LowerArm_R", "RightForeArm", "RightLowerArm", "ForeArm_R");
            _ulL = Find(all, "UpperLeg_L", "LeftUpLeg", "LeftUpperLeg", "Thigh_L");
            _ulR = Find(all, "UpperLeg_R", "RightUpLeg", "RightUpperLeg", "Thigh_R");
            _llL = Find(all, "LowerLeg_L", "LeftLeg", "LeftLowerLeg", "Calf_L");
            _llR = Find(all, "LowerLeg_R", "RightLeg", "RightLowerLeg", "Calf_R");
            _ftL = Find(all, "Foot_L", "LeftFoot");
            _ftR = Find(all, "Foot_R", "RightFoot");
            _handL = Find(all, "Hand_L", "Hand.L", "LeftHand", "mixamorig:LeftHand", "hand_l");
            _handR = Find(all, "Hand_R", "Hand.R", "RightHand", "mixamorig:RightHand", "hand_r");
            _bones = _hips != null || _uaL != null || _ulL != null;
            if (!_bones) return;
            if (_hips) _hips0 = _hips.localRotation;
            if (_spine) _spine0 = _spine.localRotation;
            if (_head) _head0 = _head.localRotation;
            if (_uaL) _uaL0 = _uaL.localRotation;
            if (_uaR) _uaR0 = _uaR.localRotation;
            if (_laL) _laL0 = _laL.localRotation;
            if (_laR) _laR0 = _laR.localRotation;
            if (_ulL) _ulL0 = _ulL.localRotation;
            if (_ulR) _ulR0 = _ulR.localRotation;
            if (_llL) _llL0 = _llL.localRotation;
            if (_llR) _llR0 = _llR.localRotation;
            if (_ftL) _ftL0 = _ftL.localRotation;
            if (_ftR) _ftR0 = _ftR.localRotation;
        }

        static Transform Find(Transform[] all, params string[] names)
        {
            if (all == null || names == null) return null;
            for (int n = 0; n < names.Length; n++)
            {
                string want = names[n];
                if (string.IsNullOrEmpty(want)) continue;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == want) return all[i];
                }
            }
            return null;
        }

        void Facing(out float fwd, out float side)
        {
            fwd = 1f;
            side = 0f;
            if (_motor == null) return;
            Vector3 v = _motor.Velocity;
            Vector3 f = _motor.transform.forward;
            Vector3 r = _motor.transform.right;
            fwd = v.x * f.x + v.z * f.z;
            side = v.x * r.x + v.z * r.z;
        }

        void PlantContact(float u)
        {
            float lip = VerbExitFit.LipWeight(_id, u);
            float hand = lip > 0.02f ? lip : VerbExitFit.HandGroundWeight(_id, u, _fallScale);
            bool onLip = lip > 0.02f;
            if (hand > 0.02f)
            {
                MoveHand(_handL, ref _pinHandL, ref _anchorHandL, hand, onLip);
                MoveHand(_handR, ref _pinHandR, ref _anchorHandR, hand, onLip);
            }
            else
            {
                _pinHandL = false;
                _pinHandR = false;
            }
            float foot = VerbExitFit.FootWeight(_id, u);
            if (foot > 0.02f)
            {
                MoveFoot(_ftL, ref _pinFootL, ref _anchorFootL, foot);
                MoveFoot(_ftR, ref _pinFootR, ref _anchorFootR, foot);
            }
            else
            {
                _pinFootL = false;
                _pinFootR = false;
            }
        }

        void ReleasePins()
        {
            _pinHandL = false;
            _pinHandR = false;
            _pinFootL = false;
            _pinFootR = false;
        }

        void MoveHand(Transform bone, ref bool pin, ref Vector3 anchor, float weight, bool lip)
        {
            if (bone == null) return;
            Vector3 posed = bone.position;
            Vector3 hit;
            if (lip)
            {
                if (!TryLip(posed, out hit))
                {
                    pin = false;
                    return;
                }
            }
            else if (!TryGround(posed, out hit))
            {
                pin = false;
                return;
            }
            Stick(bone, posed, hit, ref pin, ref anchor, weight);
        }

        void MoveFoot(Transform bone, ref bool pin, ref Vector3 anchor, float weight)
        {
            if (bone == null) return;
            Vector3 posed = bone.position;
            Vector3 hit;
            if (!TryGround(posed, out hit))
            {
                pin = false;
                return;
            }
            Stick(bone, posed, hit, ref pin, ref anchor, weight);
        }

        void Stick(Transform bone, Vector3 posed, Vector3 hit, ref bool pin, ref Vector3 anchor, float weight)
        {
            if (!pin)
            {
                anchor = hit;
                pin = true;
            }
            else
                anchor.y = hit.y;
            Vector3 delta = anchor - posed;
            float mag = delta.magnitude;
            float cap = ClimbContact.Palm(mag);
            if (mag > 0.0001f) delta *= cap / mag;
            bone.position = posed + delta * weight;
        }

        bool TryLip(Vector3 hand, out Vector3 lip)
        {
            lip = hand;
            if (_motor == null) return false;
            Vector3 n = _motor.WallNormal;
            if (n.sqrMagnitude < 0.0001f) n = _motor.transform.forward;
            else n.Normalize();
            Vector3 origin = hand + Vector3.up * 0.55f - n * 0.12f;
            if (Physics.Raycast(origin, Vector3.down, out _plantHit, 1.15f) && !Own(_plantHit.transform))
            {
                lip = _plantHit.point + Vector3.up * 0.02f;
                return true;
            }
            if (_motor.LedgeHit)
            {
                Vector3 side = Vector3.Cross(Vector3.up, n);
                if (side.sqrMagnitude < 0.0001f) side = _motor.transform.right;
                else side.Normalize();
                float along = Vector3.Dot(hand - _motor.LedgeStand, side);
                if (along > 0.28f) along = 0.28f;
                if (along < -0.28f) along = -0.28f;
                lip = _motor.LedgeStand - n * 0.08f + side * along;
                lip.y = _motor.LedgeStand.y;
                return true;
            }
            return false;
        }

        bool TryGround(Vector3 bone, out Vector3 ground)
        {
            ground = bone;
            Vector3 origin = bone + Vector3.up * 0.45f;
            if (!Physics.Raycast(origin, Vector3.down, out _plantHit, 1.25f) || Own(_plantHit.transform))
                return false;
            ground = _plantHit.point + Vector3.up * 0.02f;
            return true;
        }

        bool Own(Transform hit)
        {
            if (hit == null || _motor == null) return false;
            Transform root = _motor.transform;
            return hit == root || hit.IsChildOf(root);
        }
    }
}
