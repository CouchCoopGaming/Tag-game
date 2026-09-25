using Tag.Experimental;
using Tag.Gameplay;
using TagArena.Movement;
using UnityEngine;

namespace Tag.Art
{
    /// <summary>
    /// Procedural parkour body driven by TagArena MoveState (Apex-Tribes).
    /// No AnimationClips required - readable limb tells for third-person views.
    /// </summary>
    public class DummyLocomotor : MonoBehaviour
    {
        PlayerMotor _motor;
        PlayerInputReader _input;
        PunchHitbox _punch;
        ExperimentalGrapple _grapple;

        Transform _hips, _spine, _head;
        Transform _upperArmL, _upperArmR, _lowerArmL, _lowerArmR;
        Transform _upperLegL, _upperLegR, _lowerLegL, _lowerLegR;
        Quaternion _hips0, _spine0, _head0;
        Quaternion _uaL0, _uaR0, _laL0, _laR0;
        Quaternion _ulL0, _ulR0, _llL0, _llR0;
        Vector3 _root0;
        bool _bound;
        bool _loggedBindFail;
        float _cycle;
        float _landSquash;
        float _landHold;
        float _landHard;
        float _punchTelegraph;
        bool _wasGrounded = true;
        float _prevSpeed;
        float _pushOff;
        bool _pushLeft;
        bool _jumpFromStill;
        float _jumpFromStillIn;
        Quaternion _stillJumpUaL, _stillJumpUaR, _stillJumpLaL, _stillJumpLaR;
        Quaternion _stillJumpUlL, _stillJumpUlR, _stillJumpLlL, _stillJumpLlR;
        Quaternion _stillJumpSp, _stillJumpHp, _stillJumpHd;
        bool _jumpFromCrouchWalk;
        float _jumpFromCrouchWalkIn;
        Quaternion _crouchWalkJumpUaL, _crouchWalkJumpUaR, _crouchWalkJumpLaL, _crouchWalkJumpLaR;
        Quaternion _crouchWalkJumpUlL, _crouchWalkJumpUlR, _crouchWalkJumpLlL, _crouchWalkJumpLlR;
        Quaternion _crouchWalkJumpSp, _crouchWalkJumpHp, _crouchWalkJumpHd;
        bool _jumpFromWalk;
        float _jumpFromWalkIn;
        Quaternion _walkJumpUaL, _walkJumpUaR, _walkJumpLaL, _walkJumpLaR;
        Quaternion _walkJumpUlL, _walkJumpUlR, _walkJumpLlL, _walkJumpLlR;
        Quaternion _walkJumpSp, _walkJumpHp, _walkJumpHd;
        bool _jumpFromSki;
        bool _jumpFromSlide;
        float _jumpFromSlideIn;
        Quaternion _slideJumpUaL, _slideJumpUaR, _slideJumpLaL, _slideJumpLaR;
        Quaternion _slideJumpUlL, _slideJumpUlR, _slideJumpLlL, _slideJumpLlR;
        Quaternion _slideJumpSp, _slideJumpHp, _slideJumpHd;
        bool _jumpFromDash;
        bool _jumpFromClimb;
        bool _jumpFromWall;
        bool _jumpFromAirCrouch;
        bool _jumpFromAirCrouchStride;
        bool _jumpFromSoftLand;
        bool _jumpFromHardLand;
        bool _jumpFromMiss;
        float _missR;
        bool _jumpFromTag;
        float _tagSettle;
        bool _tagFromClaim;
        bool _jumpFromClaim;
        bool _jumpFromGrapple;
        bool _jumpFromReady;
        bool _jumpFromPunch;
        float _jumpFromPunchIn;
        Quaternion _punchJumpUaL, _punchJumpUaR, _punchJumpLaL, _punchJumpLaR;
        Quaternion _punchJumpUlL, _punchJumpUlR, _punchJumpLlL, _punchJumpLlR;
        Quaternion _punchJumpSp, _punchJumpHp, _punchJumpHd;
        bool _punchFromJump;
        float _punchFromJumpIn;
        bool _punchFromSoft;
        float _punchFromSoftIn;
        Quaternion _landPunchUaL, _landPunchUaR, _landPunchLaL, _landPunchLaR;
        Quaternion _landPunchUlL, _landPunchUlR, _landPunchLlL, _landPunchLlR;
        Quaternion _landPunchSp, _landPunchHp, _landPunchHd;
        bool _punchFromHard;
        float _punchFromHardIn;
        Quaternion _hardPunchUaL, _hardPunchUaR, _hardPunchLaL, _hardPunchLaR;
        Quaternion _hardPunchUlL, _hardPunchUlR, _hardPunchLlL, _hardPunchLlR;
        Quaternion _hardPunchSp, _hardPunchHp, _hardPunchHd;
        bool _punchFromSki;
        float _punchFromSkiIn;
        Quaternion _skiPunchUaL, _skiPunchUaR, _skiPunchLaL, _skiPunchLaR;
        Quaternion _skiPunchUlL, _skiPunchUlR, _skiPunchLlL, _skiPunchLlR;
        Quaternion _skiPunchSp, _skiPunchHp, _skiPunchHd;
        bool _punchFromSlide;
        float _punchFromSlideIn;
        Quaternion _slidePunchUaL, _slidePunchUaR, _slidePunchLaL, _slidePunchLaR;
        Quaternion _slidePunchUlL, _slidePunchUlR, _slidePunchLlL, _slidePunchLlR;
        Quaternion _slidePunchSp, _slidePunchHp, _slidePunchHd;
        bool _punchFromClimb;
        float _punchFromClimbIn;
        Quaternion _climbPunchUaL, _climbPunchUaR, _climbPunchLaL, _climbPunchLaR;
        Quaternion _climbPunchUlL, _climbPunchUlR, _climbPunchLlL, _climbPunchLlR;
        Quaternion _climbPunchSp, _climbPunchHp, _climbPunchHd;
        bool _punchFromWall;
        float _punchFromWallIn;
        Quaternion _wallPunchUaL, _wallPunchUaR, _wallPunchLaL, _wallPunchLaR;
        Quaternion _wallPunchUlL, _wallPunchUlR, _wallPunchLlL, _wallPunchLlR;
        Quaternion _wallPunchSp, _wallPunchHp, _wallPunchHd;
        bool _punchFromDart;
        float _punchFromDartIn;
        Quaternion _dartPunchUaL, _dartPunchUaR, _dartPunchLaL, _dartPunchLaR;
        Quaternion _dartPunchUlL, _dartPunchUlR, _dartPunchLlL, _dartPunchLlR;
        Quaternion _dartPunchSp, _dartPunchHp, _dartPunchHd;
        bool _punchFromClaim;
        float _punchFromClaimIn;
        Quaternion _claimPunchUaL, _claimPunchUaR, _claimPunchLaL, _claimPunchLaR;
        Quaternion _claimPunchUlL, _claimPunchUlR, _claimPunchLlL, _claimPunchLlR;
        Quaternion _claimPunchSp, _claimPunchHp, _claimPunchHd;
        bool _punchFromGrapple;
        float _punchFromGrappleIn;
        Quaternion _grapplePunchUaL, _grapplePunchUaR, _grapplePunchLaL, _grapplePunchLaR;
        Quaternion _grapplePunchUlL, _grapplePunchUlR, _grapplePunchLlL, _grapplePunchLlR;
        Quaternion _grapplePunchSp, _grapplePunchHp, _grapplePunchHd;
        bool _punchFromReady;
        float _punchFromReadyIn;
        Quaternion _readyPunchUaL, _readyPunchUaR, _readyPunchLaL, _readyPunchLaR;
        Quaternion _readyPunchUlL, _readyPunchUlR, _readyPunchLlL, _readyPunchLlR;
        Quaternion _readyPunchSp, _readyPunchHp, _readyPunchHd;
        bool _tagPunchHold;
        bool _punchFromTag;
        float _punchFromTagIn;
        Quaternion _tagPunchUaL, _tagPunchUaR, _tagPunchLaL, _tagPunchLaR;
        Quaternion _tagPunchUlL, _tagPunchUlR, _tagPunchLlL, _tagPunchLlR;
        Quaternion _tagPunchSp, _tagPunchHp, _tagPunchHd;
        bool _punchFromCrouch;
        float _punchFromCrouchIn;
        Quaternion _crouchPunchUaL, _crouchPunchUaR, _crouchPunchLaL, _crouchPunchLaR;
        Quaternion _crouchPunchUlL, _crouchPunchUlR, _crouchPunchLlL, _crouchPunchLlR;
        Quaternion _crouchPunchSp, _crouchPunchHp, _crouchPunchHd;
        bool _punchFromWalk;
        float _punchFromWalkIn;
        Quaternion _walkPunchUaL, _walkPunchUaR, _walkPunchLaL, _walkPunchLaR;
        Quaternion _walkPunchUlL, _walkPunchUlR, _walkPunchLlL, _walkPunchLlR;
        Quaternion _walkPunchSp, _walkPunchHp, _walkPunchHd;
        bool _punchFromSprint;
        float _punchFromSprintIn;
        Quaternion _sprintPunchUaL, _sprintPunchUaR, _sprintPunchLaL, _sprintPunchLaR;
        Quaternion _sprintPunchUlL, _sprintPunchUlR, _sprintPunchLlL, _sprintPunchLlR;
        Quaternion _sprintPunchSp, _sprintPunchHp, _sprintPunchHd;
        bool _punchFromCrouchWalk;
        float _punchFromCrouchWalkIn;
        Quaternion _crouchWalkPunchUaL, _crouchWalkPunchUaR, _crouchWalkPunchLaL, _crouchWalkPunchLaR;
        Quaternion _crouchWalkPunchUlL, _crouchWalkPunchUlR, _crouchWalkPunchLlL, _crouchWalkPunchLlR;
        Quaternion _crouchWalkPunchSp, _crouchWalkPunchHp, _crouchWalkPunchHd;
        bool _tagFromCrouch;
        float _tagFromCrouchIn;
        Quaternion _crouchTagUaL, _crouchTagUaR, _crouchTagLaL, _crouchTagLaR;
        Quaternion _crouchTagUlL, _crouchTagUlR, _crouchTagLlL, _crouchTagLlR;
        Quaternion _crouchTagSp, _crouchTagHp, _crouchTagHd;
        bool _tagFromWalk;
        float _tagFromWalkIn;
        Quaternion _walkTagUaL, _walkTagUaR, _walkTagLaL, _walkTagLaR;
        Quaternion _walkTagUlL, _walkTagUlR, _walkTagLlL, _walkTagLlR;
        Quaternion _walkTagSp, _walkTagHp, _walkTagHd;
        bool _tagFromSprint;
        float _tagFromSprintIn;
        Quaternion _sprintTagUaL, _sprintTagUaR, _sprintTagLaL, _sprintTagLaR;
        Quaternion _sprintTagUlL, _sprintTagUlR, _sprintTagLlL, _sprintTagLlR;
        Quaternion _sprintTagSp, _sprintTagHp, _sprintTagHd;
        bool _tagFromCrouchWalk;
        float _tagFromCrouchWalkIn;
        Quaternion _crouchWalkTagUaL, _crouchWalkTagUaR, _crouchWalkTagLaL, _crouchWalkTagLaR;
        Quaternion _crouchWalkTagUlL, _crouchWalkTagUlR, _crouchWalkTagLlL, _crouchWalkTagLlR;
        Quaternion _crouchWalkTagSp, _crouchWalkTagHp, _crouchWalkTagHd;
        bool _punchFromDash;
        float _punchFromDashIn;
        Quaternion _dashPunchUaL, _dashPunchUaR, _dashPunchLaL, _dashPunchLaR;
        Quaternion _dashPunchUlL, _dashPunchUlR, _dashPunchLlL, _dashPunchLlR;
        Quaternion _dashPunchSp, _dashPunchHp, _dashPunchHd;
        bool _tagFromDash;
        float _tagFromDashIn;
        Quaternion _dashTagUaL, _dashTagUaR, _dashTagLaL, _dashTagLaR;
        Quaternion _dashTagUlL, _dashTagUlR, _dashTagLlL, _dashTagLlR;
        Quaternion _dashTagSp, _dashTagHp, _dashTagHd;
        bool _punchWindWas;
        bool _tagFromJump;
        float _tagFromJumpIn;
        bool _tagHitWas;
        Quaternion _tagJumpUaL, _tagJumpUaR, _tagJumpLaL, _tagJumpLaR;
        Quaternion _tagJumpSp, _tagJumpHp, _tagJumpHd;
        Quaternion _tagJumpUlL, _tagJumpUlR, _tagJumpLlL, _tagJumpLlR;
        bool _tagFromSoft;
        float _tagFromSoftIn;
        Quaternion _softTagUaL, _softTagUaR, _softTagLaL, _softTagLaR;
        Quaternion _softTagUlL, _softTagUlR, _softTagLlL, _softTagLlR;
        Quaternion _softTagSp, _softTagHp, _softTagHd;
        bool _tagFromHard;
        float _tagFromHardIn;
        Quaternion _hardTagUaL, _hardTagUaR, _hardTagLaL, _hardTagLaR;
        Quaternion _hardTagUlL, _hardTagUlR, _hardTagLlL, _hardTagLlR;
        Quaternion _hardTagSp, _hardTagHp, _hardTagHd;
        bool _tagFromSki;
        float _tagFromSkiIn;
        Quaternion _skiTagUaL, _skiTagUaR, _skiTagLaL, _skiTagLaR;
        Quaternion _skiTagUlL, _skiTagUlR, _skiTagLlL, _skiTagLlR;
        Quaternion _skiTagSp, _skiTagHp, _skiTagHd;
        bool _tagFromSlide;
        float _tagFromSlideIn;
        Quaternion _slideTagUaL, _slideTagUaR, _slideTagLaL, _slideTagLaR;
        Quaternion _slideTagUlL, _slideTagUlR, _slideTagLlL, _slideTagLlR;
        Quaternion _slideTagSp, _slideTagHp, _slideTagHd;
        bool _tagFromClimb;
        float _tagFromClimbIn;
        Quaternion _climbTagUaL, _climbTagUaR, _climbTagLaL, _climbTagLaR;
        Quaternion _climbTagUlL, _climbTagUlR, _climbTagLlL, _climbTagLlR;
        Quaternion _climbTagSp, _climbTagHp, _climbTagHd;
        bool _tagFromWall;
        float _tagFromWallIn;
        Quaternion _wallTagUaL, _wallTagUaR, _wallTagLaL, _wallTagLaR;
        Quaternion _wallTagUlL, _wallTagUlR, _wallTagLlL, _wallTagLlR;
        Quaternion _wallTagSp, _wallTagHp, _wallTagHd;
        bool _tagFromDart;
        float _tagFromDartIn;
        Quaternion _dartTagUaL, _dartTagUaR, _dartTagLaL, _dartTagLaR;
        Quaternion _dartTagUlL, _dartTagUlR, _dartTagLlL, _dartTagLlR;
        Quaternion _dartTagSp, _dartTagHp, _dartTagHd;
        bool _tagFromItClaim;
        float _tagFromItClaimIn;
        Quaternion _claimTagUaL, _claimTagUaR, _claimTagLaL, _claimTagLaR;
        Quaternion _claimTagUlL, _claimTagUlR, _claimTagLlL, _claimTagLlR;
        Quaternion _claimTagSp, _claimTagHp, _claimTagHd;
        bool _tagFromGrapple;
        float _tagFromGrappleIn;
        Quaternion _grappleTagUaL, _grappleTagUaR, _grappleTagLaL, _grappleTagLaR;
        Quaternion _grappleTagUlL, _grappleTagUlR, _grappleTagLlL, _grappleTagLlR;
        Quaternion _grappleTagSp, _grappleTagHp, _grappleTagHd;
        bool _tagFromReady;
        float _tagFromReadyIn;
        Quaternion _readyTagUaL, _readyTagUaR, _readyTagLaL, _readyTagLaR;
        Quaternion _readyTagUlL, _readyTagUlR, _readyTagLlL, _readyTagLlR;
        Quaternion _readyTagSp, _readyTagHp, _readyTagHd;
        bool _tagFromPunch;
        float _tagFromPunchIn;
        Quaternion _punchTagUaL, _punchTagUaR, _punchTagLaL, _punchTagLaR;
        Quaternion _punchTagUlL, _punchTagUlR, _punchTagLlL, _punchTagLlR;
        Quaternion _punchTagSp, _punchTagHp, _punchTagHd;
        bool _tagFromMiss;
        float _tagFromMissIn;
        Quaternion _missTagUaL, _missTagUaR, _missTagLaL, _missTagLaR;
        Quaternion _missTagUlL, _missTagUlR, _missTagLlL, _missTagLlR;
        Quaternion _missTagSp, _missTagHp, _missTagHd;
        PunchPhase _punchPhaseWas;
        bool _landedFromJump;
        Quaternion _punchUaL, _punchUaR, _punchLaL, _punchLaR;
        Quaternion _punchSp, _punchHp, _punchHd;
        Quaternion _punchUlL, _punchUlR, _punchLlL, _punchLlR;
        Quaternion _readyUaL, _readyUaR, _readyLaL, _readyLaR;
        Quaternion _readySp, _readyHp, _readyHd;
        Quaternion _readyUlL, _readyUlR, _readyLlL, _readyLlR;
        float _prevVy;
        bool _crouchWalkArmed;
        float _airArmIn = 1f;
        float _bouncePulse;
        bool _bounceWallLeft;
        float _glidePulse;
        float _dashPulse;
        float _dashRecover;
        float _armRecover;
        bool _airDashArms;
        bool _dashFromJump;
        float _dashFromJumpIn;
        Quaternion _jumpDashUaL, _jumpDashUaR, _jumpDashLaL, _jumpDashLaR;
        Quaternion _jumpDashUlL, _jumpDashUlR, _jumpDashLlL, _jumpDashLlR;
        Quaternion _jumpDashSp, _jumpDashHp, _jumpDashHd;
        bool _dashFromCrouch;
        float _dashFromCrouchIn;
        Quaternion _crouchDashUaL, _crouchDashUaR, _crouchDashLaL, _crouchDashLaR;
        Quaternion _crouchDashUlL, _crouchDashUlR, _crouchDashLlL, _crouchDashLlR;
        Quaternion _crouchDashSp, _crouchDashHp, _crouchDashHd;
        bool _dashFromPunch;
        float _dashFromPunchIn;
        Quaternion _punchDashUaL, _punchDashUaR, _punchDashLaL, _punchDashLaR;
        Quaternion _punchDashUlL, _punchDashUlR, _punchDashLlL, _punchDashLlR;
        Quaternion _punchDashSp, _punchDashHp, _punchDashHd;
        bool _dashFromCrouchWalk;
        float _dashFromCrouchWalkIn;
        Quaternion _crouchWalkDashUaL, _crouchWalkDashUaR, _crouchWalkDashLaL, _crouchWalkDashLaR;
        Quaternion _crouchWalkDashUlL, _crouchWalkDashUlR, _crouchWalkDashLlL, _crouchWalkDashLlR;
        Quaternion _crouchWalkDashSp, _crouchWalkDashHp, _crouchWalkDashHd;
        bool _dashFromWalk;
        float _dashFromWalkIn;
        Quaternion _walkDashUaL, _walkDashUaR, _walkDashLaL, _walkDashLaR;
        Quaternion _walkDashUlL, _walkDashUlR, _walkDashLlL, _walkDashLlR;
        Quaternion _walkDashSp, _walkDashHp, _walkDashHd;
        bool _dashFromReady;
        float _dashFromReadyIn;
        Quaternion _readyDashUaL, _readyDashUaR, _readyDashLaL, _readyDashLaR;
        Quaternion _readyDashUlL, _readyDashUlR, _readyDashLlL, _readyDashLlR;
        Quaternion _readyDashSp, _readyDashHp, _readyDashHd;
        bool _dashFromDart;
        float _dashFromDartIn;
        bool _dashFromDartStride;
        bool _dashFromSki;
        float _dashFromSkiIn;
        float _skiSin;
        bool _dashFromSlide;
        float _dashFromSlideIn;
        bool _slideLeadLeft;
        Quaternion _slideDashUaL, _slideDashUaR, _slideDashLaL, _slideDashLaR;
        Quaternion _slideDashUlL, _slideDashUlR, _slideDashLlL, _slideDashLlR;
        Quaternion _slideDashSp, _slideDashHp, _slideDashHd;
        bool _dashFromClimb;
        float _dashFromClimbIn;
        Quaternion _climbUaL, _climbUaR, _climbLaL, _climbLaR;
        Quaternion _climbUlL, _climbUlR, _climbLlL, _climbLlR;
        Quaternion _climbSp, _climbHp, _climbHd;
        bool _dashFromWall;
        float _dashFromWallIn;
        Quaternion _wallUaL, _wallUaR, _wallLaL, _wallLaR;
        Quaternion _wallUlL, _wallUlR, _wallLlL, _wallLlR;
        Quaternion _wallSp, _wallHp, _wallHd;
        bool _skiFromDash;
        float _skiFromDashIn;
        Quaternion _dashSkiUaL, _dashSkiUaR, _dashSkiLaL, _dashSkiLaR;
        Quaternion _dashSkiUlL, _dashSkiUlR, _dashSkiLlL, _dashSkiLlR;
        Quaternion _dashSkiSp, _dashSkiHp, _dashSkiHd;
        bool _skiFromPunch;
        float _skiFromPunchIn;
        Quaternion _punchSkiUaL, _punchSkiUaR, _punchSkiLaL, _punchSkiLaR;
        Quaternion _punchSkiUlL, _punchSkiUlR, _punchSkiLlL, _punchSkiLlR;
        Quaternion _punchSkiSp, _punchSkiHp, _punchSkiHd;
        bool _skiFromTag;
        float _skiFromTagIn;
        Quaternion _tagSkiUaL, _tagSkiUaR, _tagSkiLaL, _tagSkiLaR;
        Quaternion _tagSkiUlL, _tagSkiUlR, _tagSkiLlL, _tagSkiLlR;
        Quaternion _tagSkiSp, _tagSkiHp, _tagSkiHd;
        bool _skiFromMiss;
        float _skiFromMissIn;
        Quaternion _missSkiUaL, _missSkiUaR, _missSkiLaL, _missSkiLaR;
        Quaternion _missSkiUlL, _missSkiUlR, _missSkiLlL, _missSkiLlR;
        Quaternion _missSkiSp, _missSkiHp, _missSkiHd;
        bool _skiFromSoft;
        float _skiFromSoftIn;
        Quaternion _softSkiUaL, _softSkiUaR, _softSkiLaL, _softSkiLaR;
        Quaternion _softSkiUlL, _softSkiUlR, _softSkiLlL, _softSkiLlR;
        Quaternion _softSkiSp, _softSkiHp, _softSkiHd;
        bool _skiFromHard;
        float _skiFromHardIn;
        Quaternion _hardSkiUaL, _hardSkiUaR, _hardSkiLaL, _hardSkiLaR;
        Quaternion _hardSkiUlL, _hardSkiUlR, _hardSkiLlL, _hardSkiLlR;
        Quaternion _hardSkiSp, _hardSkiHp, _hardSkiHd;
        bool _skiFromDart;
        float _skiFromDartIn;
        Quaternion _dartSkiUaL, _dartSkiUaR, _dartSkiLaL, _dartSkiLaR;
        Quaternion _dartSkiUlL, _dartSkiUlR, _dartSkiLlL, _dartSkiLlR;
        Quaternion _dartSkiSp, _dartSkiHp, _dartSkiHd;
        bool _skiFromGrapple;
        float _skiFromGrappleIn;
        Quaternion _grappleSkiUaL, _grappleSkiUaR, _grappleSkiLaL, _grappleSkiLaR;
        Quaternion _grappleSkiUlL, _grappleSkiUlR, _grappleSkiLlL, _grappleSkiLlR;
        Quaternion _grappleSkiSp, _grappleSkiHp, _grappleSkiHd;
        bool _skiFromClaim;
        float _skiFromClaimIn;
        Quaternion _claimSkiUaL, _claimSkiUaR, _claimSkiLaL, _claimSkiLaR;
        Quaternion _claimSkiUlL, _claimSkiUlR, _claimSkiLlL, _claimSkiLlR;
        Quaternion _claimSkiSp, _claimSkiHp, _claimSkiHd;
        bool _skiFromReady;
        float _skiFromReadyIn;
        Quaternion _readySkiUaL, _readySkiUaR, _readySkiLaL, _readySkiLaR;
        Quaternion _readySkiUlL, _readySkiUlR, _readySkiLlL, _readySkiLlR;
        Quaternion _readySkiSp, _readySkiHp, _readySkiHd;
        bool _skiFromClimb;
        float _skiFromClimbIn;
        Quaternion _climbSkiUaL, _climbSkiUaR, _climbSkiLaL, _climbSkiLaR;
        Quaternion _climbSkiUlL, _climbSkiUlR, _climbSkiLlL, _climbSkiLlR;
        Quaternion _climbSkiSp, _climbSkiHp, _climbSkiHd;
        bool _skiFromWall;
        float _skiFromWallIn;
        Quaternion _wallSkiUaL, _wallSkiUaR, _wallSkiLaL, _wallSkiLaR;
        Quaternion _wallSkiUlL, _wallSkiUlR, _wallSkiLlL, _wallSkiLlR;
        Quaternion _wallSkiSp, _wallSkiHp, _wallSkiHd;
        bool _slideFromDash;
        float _slideFromDashIn;
        bool _slideFromPunch;
        float _slideFromPunchIn;
        Quaternion _punchSlideUaL, _punchSlideUaR, _punchSlideLaL, _punchSlideLaR;
        Quaternion _punchSlideUlL, _punchSlideUlR, _punchSlideLlL, _punchSlideLlR;
        Quaternion _punchSlideSp, _punchSlideHp, _punchSlideHd;
        bool _slideFromTag;
        float _slideFromTagIn;
        Quaternion _tagSlideUaL, _tagSlideUaR, _tagSlideLaL, _tagSlideLaR;
        Quaternion _tagSlideUlL, _tagSlideUlR, _tagSlideLlL, _tagSlideLlR;
        Quaternion _tagSlideSp, _tagSlideHp, _tagSlideHd;
        bool _slideFromSoft;
        float _slideFromSoftIn;
        Quaternion _softSlideUaL, _softSlideUaR, _softSlideLaL, _softSlideLaR;
        Quaternion _softSlideUlL, _softSlideUlR, _softSlideLlL, _softSlideLlR;
        Quaternion _softSlideSp, _softSlideHp, _softSlideHd;
        bool _slideFromClimb;
        float _slideFromClimbIn;
        Quaternion _climbSlideUaL, _climbSlideUaR, _climbSlideLaL, _climbSlideLaR;
        Quaternion _climbSlideUlL, _climbSlideUlR, _climbSlideLlL, _climbSlideLlR;
        Quaternion _climbSlideSp, _climbSlideHp, _climbSlideHd;
        bool _slideFromWall;
        float _slideFromWallIn;
        Quaternion _wallSlideUaL, _wallSlideUaR, _wallSlideLaL, _wallSlideLaR;
        Quaternion _wallSlideUlL, _wallSlideUlR, _wallSlideLlL, _wallSlideLlR;
        Quaternion _wallSlideSp, _wallSlideHp, _wallSlideHd;
        bool _slideFromGrapple;
        float _slideFromGrappleIn;
        Quaternion _grappleSlideUaL, _grappleSlideUaR, _grappleSlideLaL, _grappleSlideLaR;
        Quaternion _grappleSlideUlL, _grappleSlideUlR, _grappleSlideLlL, _grappleSlideLlR;
        Quaternion _grappleSlideSp, _grappleSlideHp, _grappleSlideHd;
        bool _slideFromClaim;
        float _slideFromClaimIn;
        Quaternion _claimSlideUaL, _claimSlideUaR, _claimSlideLaL, _claimSlideLaR;
        Quaternion _claimSlideUlL, _claimSlideUlR, _claimSlideLlL, _claimSlideLlR;
        Quaternion _claimSlideSp, _claimSlideHp, _claimSlideHd;
        bool _slideFromReady;
        float _slideFromReadyIn;
        Quaternion _readySlideUaL, _readySlideUaR, _readySlideLaL, _readySlideLaR;
        Quaternion _readySlideUlL, _readySlideUlR, _readySlideLlL, _readySlideLlR;
        Quaternion _readySlideSp, _readySlideHp, _readySlideHd;
        bool _slideFromMiss;
        float _slideFromMissIn;
        Quaternion _missSlideUaL, _missSlideUaR, _missSlideLaL, _missSlideLaR;
        Quaternion _missSlideUlL, _missSlideUlR, _missSlideLlL, _missSlideLlR;
        Quaternion _missSlideSp, _missSlideHp, _missSlideHd;
        bool _slideFromCrouch;
        float _slideFromCrouchIn;
        Quaternion _crouchSlideUaL, _crouchSlideUaR, _crouchSlideLaL, _crouchSlideLaR;
        Quaternion _crouchSlideUlL, _crouchSlideUlR, _crouchSlideLlL, _crouchSlideLlR;
        Quaternion _crouchSlideSp, _crouchSlideHp, _crouchSlideHd;
        bool _slideFromSki;
        float _slideFromSkiIn;
        Quaternion _skiSlideUaL, _skiSlideUaR, _skiSlideLaL, _skiSlideLaR;
        Quaternion _skiSlideUlL, _skiSlideUlR, _skiSlideLlL, _skiSlideLlR;
        Quaternion _skiSlideSp, _skiSlideHp, _skiSlideHd;
        bool _slideFromSprint;
        float _slideFromSprintIn;
        Quaternion _sprintSlideUaL, _sprintSlideUaR, _sprintSlideLaL, _sprintSlideLaR;
        Quaternion _sprintSlideUlL, _sprintSlideUlR, _sprintSlideLlL, _sprintSlideLlR;
        Quaternion _sprintSlideSp, _sprintSlideHp, _sprintSlideHd;
        bool _slideFromWalk;
        float _slideFromWalkIn;
        Quaternion _walkSlideUaL, _walkSlideUaR, _walkSlideLaL, _walkSlideLaR;
        Quaternion _walkSlideUlL, _walkSlideUlR, _walkSlideLlL, _walkSlideLlR;
        Quaternion _walkSlideSp, _walkSlideHp, _walkSlideHd;
        bool _climbFromDash;
        float _climbFromDashIn;
        bool _wallFromDash;
        float _wallFromDashIn;
        bool _dashFromMiss;
        float _dashFromMissIn;
        Quaternion _missUaL, _missUaR, _missLaL, _missLaR;
        Quaternion _missUlL, _missUlR, _missLlL, _missLlR;
        Quaternion _missSp, _missHp, _missHd;
        bool _dashFromTag;
        float _dashFromTagIn;
        Quaternion _tagUaL, _tagUaR, _tagLaL, _tagLaR;
        Quaternion _tagUlL, _tagUlR, _tagLlL, _tagLlR;
        Quaternion _tagSp, _tagHp, _tagHd;
        bool _dashFromClaim;
        float _dashFromClaimIn;
        Quaternion _claimUaL, _claimUaR, _claimLaL, _claimLaR;
        Quaternion _claimUlL, _claimUlR, _claimLlL, _claimLlR;
        Quaternion _claimSp, _claimHp, _claimHd;
        bool _dashFromGrapple;
        float _dashFromGrappleIn;
        Quaternion _grappleUaL, _grappleUaR, _grappleLaL, _grappleLaR;
        Quaternion _grappleUlL, _grappleUlR, _grappleLlL, _grappleLlR;
        Quaternion _grappleSp, _grappleHp, _grappleHd;
        bool _dashFromSoft;
        float _dashFromSoftIn;
        Quaternion _softUaL, _softUaR, _softLaL, _softLaR;
        Quaternion _softUlL, _softUlR, _softLlL, _softLlR;
        Quaternion _softSp, _softHp, _softHd;
        bool _dashFromHard;
        float _dashFromHardIn;
        Quaternion _hardUaL, _hardUaR, _hardLaL, _hardLaR;
        Quaternion _hardUlL, _hardUlR, _hardLlL, _hardLlR;
        Quaternion _hardSp, _hardHp, _hardHd;
        float _dartStepL;
        float _dartStepR;
        bool _dartFromDash;
        float _dartFromDashIn;
        bool _airDashPoseWas;
        float _dashTrailT;
        float _dashReady;
        float _dashCdWas;
        bool _dashCdSeen;
        float _tagFlinch;
        float _itClaim;
        float _skiBlend;
        bool _skiFromWalk;
        float _skiFromWalkIn;
        Quaternion _walkSkiUaL, _walkSkiUaR, _walkSkiLaL, _walkSkiLaR;
        Quaternion _walkSkiUlL, _walkSkiUlR, _walkSkiLlL, _walkSkiLlR;
        Quaternion _walkSkiSp, _walkSkiHp, _walkSkiHd;
        bool _skiFromSprint;
        float _skiFromSprintIn;
        Quaternion _sprintSkiUaL, _sprintSkiUaR, _sprintSkiLaL, _sprintSkiLaR;
        Quaternion _sprintSkiUlL, _sprintSkiUlR, _sprintSkiLlL, _sprintSkiLlR;
        Quaternion _sprintSkiSp, _sprintSkiHp, _sprintSkiHd;
        bool _skiFromCrouch;
        float _skiFromCrouchIn;
        bool _stillSkiSnap;
        Quaternion _stillSkiUaL, _stillSkiUaR, _stillSkiLaL, _stillSkiLaR;
        Quaternion _stillSkiUlL, _stillSkiUlR, _stillSkiLlL, _stillSkiLlR;
        Quaternion _stillSkiSp, _stillSkiHp, _stillSkiHd;
        bool _skiFromCrouchWalk;
        float _skiFromCrouchWalkIn;
        bool _crouchWalkSkiSnap;
        Quaternion _crouchWalkSkiUaL, _crouchWalkSkiUaR, _crouchWalkSkiLaL, _crouchWalkSkiLaR;
        Quaternion _crouchWalkSkiUlL, _crouchWalkSkiUlR, _crouchWalkSkiLlL, _crouchWalkSkiLlR;
        Quaternion _crouchWalkSkiSp, _crouchWalkSkiHp, _crouchWalkSkiHd;
        float _stopGait;
        float _stopRun;
        float _stopPlant;
        bool _stopPlanted;
        bool _stopPlantLeft;
        float _runVis;
        float _swayVis;
        float _idlePhase;
        bool _swayIdle;
        float _stepIn;
        float _sprintIn = 1f;
        float _prevRunAmt;
        bool _sprintFromTurn;
        bool _sprintOutLeft;
        float _dropVis;
        bool _dropSlide;
        float _slideToCrouch;
        float _slideToCrouchWalk;
        float _crouchToSlide;
        float _crouchWalkToSlide;
        float _skiToSlide;
        float _jumpToSlide;
        bool _jumpToSlideLand;
        bool _slideToSki;
        bool _skiFromSlide;
        float _skiFromSlideIn;
        Quaternion _slideSkiUaL, _slideSkiUaR, _slideSkiLaL, _slideSkiLaR;
        Quaternion _slideSkiUlL, _slideSkiUlR, _slideSkiLlL, _slideSkiLlR;
        Quaternion _slideSkiSp, _slideSkiHp, _slideSkiHd;
        bool _skiFromJump;
        bool _skiFromJumpLand;
        bool _crouchFromStand;
        bool _crouchFromWalk;
        bool _stillCrouchWas;
        bool _crouchFromJump;
        float _crouchFromJumpIn;
        Quaternion _jumpCrouchUaL, _jumpCrouchUaR, _jumpCrouchLaL, _jumpCrouchLaR;
        Quaternion _jumpCrouchUlL, _jumpCrouchUlR, _jumpCrouchLlL, _jumpCrouchLlR;
        Quaternion _jumpCrouchSp, _jumpCrouchHp, _jumpCrouchHd;
        bool _dashCrouchWas;
        bool _crouchFromDash;
        float _crouchFromDashIn;
        Quaternion _dashCrouchUaL, _dashCrouchUaR, _dashCrouchLaL, _dashCrouchLaR;
        Quaternion _dashCrouchUlL, _dashCrouchUlR, _dashCrouchLlL, _dashCrouchLlR;
        Quaternion _dashCrouchSp, _dashCrouchHp, _dashCrouchHd;
        bool _stillFromWalk;
        float _stillFromWalkIn;
        Quaternion _walkGuardUaL, _walkGuardUaR, _walkGuardLaL, _walkGuardLaR;
        Quaternion _walkGuardUlL, _walkGuardUlR, _walkGuardLlL, _walkGuardLlR;
        Quaternion _walkGuardSp, _walkGuardHp, _walkGuardHd;
        bool _stillFromSprint;
        float _stillFromSprintIn;
        Quaternion _sprintGuardUaL, _sprintGuardUaR, _sprintGuardLaL, _sprintGuardLaR;
        Quaternion _sprintGuardUlL, _sprintGuardUlR, _sprintGuardLlL, _sprintGuardLlR;
        Quaternion _sprintGuardSp, _sprintGuardHp, _sprintGuardHd;
        bool _stillFromSki;
        float _stillFromSkiIn;
        Quaternion _skiGuardUaL, _skiGuardUaR, _skiGuardLaL, _skiGuardLaR;
        Quaternion _skiGuardUlL, _skiGuardUlR, _skiGuardLlL, _skiGuardLlR;
        Quaternion _skiGuardSp, _skiGuardHp, _skiGuardHd;
        bool _stillFromSlide;
        float _stillFromSlideIn;
        Quaternion _slideGuardUaL, _slideGuardUaR, _slideGuardLaL, _slideGuardLaR;
        Quaternion _slideGuardUlL, _slideGuardUlR, _slideGuardLlL, _slideGuardLlR;
        Quaternion _slideGuardSp, _slideGuardHp, _slideGuardHd;
        bool _stillFromPunch;
        float _stillFromPunchIn;
        Quaternion _punchGuardUaL, _punchGuardUaR, _punchGuardLaL, _punchGuardLaR;
        Quaternion _punchGuardUlL, _punchGuardUlR, _punchGuardLlL, _punchGuardLlR;
        Quaternion _punchGuardSp, _punchGuardHp, _punchGuardHd;
        bool _stillFromTag;
        float _stillFromTagIn;
        Quaternion _tagGuardUaL, _tagGuardUaR, _tagGuardLaL, _tagGuardLaR;
        Quaternion _tagGuardUlL, _tagGuardUlR, _tagGuardLlL, _tagGuardLlR;
        Quaternion _tagGuardSp, _tagGuardHp, _tagGuardHd;
        bool _stillFromMiss;
        float _stillFromMissIn;
        Quaternion _missGuardUaL, _missGuardUaR, _missGuardLaL, _missGuardLaR;
        Quaternion _missGuardUlL, _missGuardUlR, _missGuardLlL, _missGuardLlR;
        Quaternion _missGuardSp, _missGuardHp, _missGuardHd;
        bool _stillFromClaim;
        float _stillFromClaimIn;
        bool _claimWas;
        bool _claimStillHeld;
        Quaternion _claimGuardUaL, _claimGuardUaR, _claimGuardLaL, _claimGuardLaR;
        Quaternion _claimGuardUlL, _claimGuardUlR, _claimGuardLlL, _claimGuardLlR;
        Quaternion _claimGuardSp, _claimGuardHp, _claimGuardHd;
        bool _stillFromReady;
        float _stillFromReadyIn;
        bool _readyWas;
        bool _readyStillHeld;
        Quaternion _readyGuardUaL, _readyGuardUaR, _readyGuardLaL, _readyGuardLaR;
        Quaternion _readyGuardUlL, _readyGuardUlR, _readyGuardLlL, _readyGuardLlR;
        Quaternion _readyGuardSp, _readyGuardHp, _readyGuardHd;
        bool _stillFromGrapple;
        float _stillFromGrappleIn;
        bool _grappleReleaseWas;
        bool _grappleStillHeld;
        Quaternion _grappleGuardUaL, _grappleGuardUaR, _grappleGuardLaL, _grappleGuardLaR;
        Quaternion _grappleGuardUlL, _grappleGuardUlR, _grappleGuardLlL, _grappleGuardLlR;
        Quaternion _grappleGuardSp, _grappleGuardHp, _grappleGuardHd;
        bool _stillFromHard;
        float _stillFromHardIn;
        bool _hardLandWas;
        bool _hardStillHeld;
        Quaternion _hardGuardUaL, _hardGuardUaR, _hardGuardLaL, _hardGuardLaR;
        Quaternion _hardGuardUlL, _hardGuardUlR, _hardGuardLlL, _hardGuardLlR;
        Quaternion _hardGuardSp, _hardGuardHp, _hardGuardHd;
        bool _stillFromSoft;
        float _stillFromSoftIn;
        bool _softLandWas;
        bool _softStillHeld;
        Quaternion _softGuardUaL, _softGuardUaR, _softGuardLaL, _softGuardLaR;
        Quaternion _softGuardUlL, _softGuardUlR, _softGuardLlL, _softGuardLlR;
        Quaternion _softGuardSp, _softGuardHp, _softGuardHd;
        bool _stillFromDart;
        float _stillFromDartIn;
        bool _dartStillWas;
        bool _dartStillHeld;
        Quaternion _dartGuardUaL, _dartGuardUaR, _dartGuardLaL, _dartGuardLaR;
        Quaternion _dartGuardUlL, _dartGuardUlR, _dartGuardLlL, _dartGuardLlR;
        Quaternion _dartGuardSp, _dartGuardHp, _dartGuardHd;
        bool _stillFromClimb;
        float _stillFromClimbIn;
        bool _climbStillHeld;
        Quaternion _climbGuardUaL, _climbGuardUaR, _climbGuardLaL, _climbGuardLaR;
        Quaternion _climbGuardUlL, _climbGuardUlR, _climbGuardLlL, _climbGuardLlR;
        Quaternion _climbGuardSp, _climbGuardHp, _climbGuardHd;
        bool _stillFromWall;
        float _stillFromWallIn;
        bool _wallStillHeld;
        Quaternion _wallGuardUaL, _wallGuardUaR, _wallGuardLaL, _wallGuardLaR;
        Quaternion _wallGuardUlL, _wallGuardUlR, _wallGuardLlL, _wallGuardLlR;
        Quaternion _wallGuardSp, _wallGuardHp, _wallGuardHd;
        bool _stillFromCrouchWalk;
        float _stillFromCrouchWalkIn;
        bool _crouchWalkStillHeld;
        Quaternion _crouchWalkGuardUaL, _crouchWalkGuardUaR, _crouchWalkGuardLaL, _crouchWalkGuardLaR;
        Quaternion _crouchWalkGuardUlL, _crouchWalkGuardUlR, _crouchWalkGuardLlL, _crouchWalkGuardLlR;
        Quaternion _crouchWalkGuardSp, _crouchWalkGuardHp, _crouchWalkGuardHd;
        bool _crouchWalkFromSki;
        float _crouchWalkFromSkiIn;
        bool _skiWalkHeld;
        Quaternion _skiWalkUaL, _skiWalkUaR, _skiWalkLaL, _skiWalkLaR;
        Quaternion _skiWalkUlL, _skiWalkUlR, _skiWalkLlL, _skiWalkLlR;
        Quaternion _skiWalkSp, _skiWalkHp, _skiWalkHd;
        bool _crouchWalkFromSlide;
        float _crouchWalkFromSlideIn;
        Quaternion _slideWalkUaL, _slideWalkUaR, _slideWalkLaL, _slideWalkLaR;
        Quaternion _slideWalkUlL, _slideWalkUlR, _slideWalkLlL, _slideWalkLlR;
        Quaternion _slideWalkSp, _slideWalkHp, _slideWalkHd;
        bool _crouchWalkFromWalk;
        float _crouchWalkFromWalkIn;
        bool _walkCrouchHeld;
        Quaternion _walkCrouchUaL, _walkCrouchUaR, _walkCrouchLaL, _walkCrouchLaR;
        Quaternion _walkCrouchUlL, _walkCrouchUlR, _walkCrouchLlL, _walkCrouchLlR;
        Quaternion _walkCrouchSp, _walkCrouchHp, _walkCrouchHd;
        bool _crouchWalkFromSprint;
        float _crouchWalkFromSprintIn;
        bool _sprintCrouchHeld;
        Quaternion _sprintCrouchUaL, _sprintCrouchUaR, _sprintCrouchLaL, _sprintCrouchLaR;
        Quaternion _sprintCrouchUlL, _sprintCrouchUlR, _sprintCrouchLlL, _sprintCrouchLlR;
        Quaternion _sprintCrouchSp, _sprintCrouchHp, _sprintCrouchHd;
        bool _crouchWalkFromStill;
        float _crouchWalkFromStillIn;
        bool _stillStrideHeld;
        Quaternion _stillStrideUaL, _stillStrideUaR, _stillStrideLaL, _stillStrideLaR;
        Quaternion _stillStrideUlL, _stillStrideUlR, _stillStrideLlL, _stillStrideLlR;
        Quaternion _stillStrideSp, _stillStrideHp, _stillStrideHd;
        bool _walkFromSki;
        float _walkFromSkiIn;
        bool _walkFromSkiHeld;
        Quaternion _glideWalkUaL, _glideWalkUaR, _glideWalkLaL, _glideWalkLaR;
        Quaternion _glideWalkUlL, _glideWalkUlR, _glideWalkLlL, _glideWalkLlR;
        Quaternion _glideWalkSp, _glideWalkHp, _glideWalkHd;
        bool _runFromSki;
        float _runFromSkiIn;
        bool _runFromSkiHeld;
        Quaternion _glideRunUaL, _glideRunUaR, _glideRunLaL, _glideRunLaR;
        Quaternion _glideRunUlL, _glideRunUlR, _glideRunLlL, _glideRunLlR;
        Quaternion _glideRunSp, _glideRunHp, _glideRunHd;
        bool _idleFromSki;
        float _idleFromSkiIn;
        bool _idleFromSkiHeld;
        Quaternion _glideIdleUaL, _glideIdleUaR, _glideIdleLaL, _glideIdleLaR;
        Quaternion _glideIdleUlL, _glideIdleUlR, _glideIdleLlL, _glideIdleLlR;
        Quaternion _glideIdleSp, _glideIdleHp, _glideIdleHd;
        bool _walkFromCrouchWalk;
        float _walkFromCrouchWalkIn;
        bool _walkUpHeld;
        Quaternion _walkUpUaL, _walkUpUaR, _walkUpLaL, _walkUpLaR;
        Quaternion _walkUpUlL, _walkUpUlR, _walkUpLlL, _walkUpLlR;
        Quaternion _walkUpSp, _walkUpHp, _walkUpHd;
        bool _runFromCrouchWalk;
        float _runFromCrouchWalkIn;
        bool _runUpHeld;
        Quaternion _runUpUaL, _runUpUaR, _runUpLaL, _runUpLaR;
        Quaternion _runUpUlL, _runUpUlR, _runUpLlL, _runUpLlR;
        Quaternion _runUpSp, _runUpHp, _runUpHd;
        bool _walkFromStill;
        float _walkFromStillIn;
        bool _stillWalkHeld;
        Quaternion _stillWalkUaL, _stillWalkUaR, _stillWalkLaL, _stillWalkLaR;
        Quaternion _stillWalkUlL, _stillWalkUlR, _stillWalkLlL, _stillWalkLlR;
        Quaternion _stillWalkSp, _stillWalkHp, _stillWalkHd;
        bool _runFromStill;
        float _runFromStillIn;
        bool _stillRunHeld;
        Quaternion _stillRunUaL, _stillRunUaR, _stillRunLaL, _stillRunLaR;
        Quaternion _stillRunUlL, _stillRunUlR, _stillRunLlL, _stillRunLlR;
        Quaternion _stillRunSp, _stillRunHp, _stillRunHd;
        float _diveVis;
        bool _diveFromJump;
        float _surfPhase;
        float _surfIn;
        bool _wasSurf;
        bool _surfFromCrouch;
        bool _surfFromJump;
        bool _surfFromJumpPush;
        bool _surfFromJumpWall;
        bool _surfFromJumpWallPush;
        float _prevYaw;
        float _turnVis;
        bool _hasYaw;
        float _lookArmVis;
        float _grapplePose;
        float _wallExit;
        bool _exitFromWall;
        bool _exitIntoWalk;
        bool _exitIntoSprint;
        bool _exitIntoCrouch;
        bool _exitIntoCrouchWalk;
        bool _exitLeadLeft;
        Quaternion _exitUaL, _exitUaR, _exitLaL, _exitLaR;
        Quaternion _exitUlL, _exitUlR, _exitLlL, _exitLlR;
        Quaternion _exitSpine, _exitHips;
        bool _wasLunging;
        bool _wasAirDashing;
        bool _wasJetting;
        PlayerMotor _bounceHooked;
        TrailRenderer _dashTrail;

        Quaternion _spineT, _hipsT, _headT;
        Quaternion _uaLT, _uaRT, _laLT, _laRT;
        Quaternion _ulLT, _ulRT, _llLT, _llRT;

        public void Bind(Transform visualRoot, PlayerMotor motor, PunchHitbox punch, CharacterController ccIgnored = null)
        {
            _motor = motor;
            HookBounce();
            _punch = punch;
            _root0 = transform.localPosition;
            Cache(visualRoot);
            if (!_bound && !_loggedBindFail)
            {
                _loggedBindFail = true;
                Debug.LogWarning($"[DummyLocomotor] Bone bind failed on '{(visualRoot != null ? visualRoot.name : "null")}' - no hierarchical UpperArm/UpperLeg.");
            }
        }

        /// <summary>
        /// True when the visual has a hierarchical limb rig (LowerArm under UpperArm).
        /// Flat sibling mesh mannequins (HiPoly FBX / Dummy_Runner) return false.
        /// </summary>
        public static bool HasBindableBones(Transform root)
        {
            if (root == null) return false;
            var upperArm = FindBone(root, "UpperArm_L", "UpperArm.L", "LeftArm", "LeftUpperArm", "mixamorig:LeftArm", "Arm_L", "upperarm_l", "Upper_Arm_L");
            var lowerArm = FindBone(root, "LowerArm_L", "LowerArm.L", "LeftForeArm", "LeftLowerArm", "mixamorig:LeftForeArm", "ForeArm_L", "lowerarm_l", "Lower_Arm_L");
            var upperLeg = FindBone(root, "UpperLeg_L", "UpperLeg.L", "LeftUpLeg", "LeftUpperLeg", "mixamorig:LeftUpLeg", "Thigh_L", "upperleg_l", "Upper_Leg_L");
            if (upperArm == null || upperLeg == null) return false;
            // Require hierarchy so procedural swing actually moves the distal limb
            if (lowerArm != null && lowerArm.IsChildOf(upperArm) && lowerArm != upperArm)
                return true;
            // Or explicit Hips/Spine empties with arms as descendants (primitive factory)
            var hips = FindBone(root, "Hips", "Pelvis", "mixamorig:Hips", "hip", "Root");
            var spine = FindBone(root, "Spine", "Torso", "Spine1", "mixamorig:Spine", "Chest");
            if (hips != null && upperLeg.IsChildOf(hips))
                return true;
            if (spine != null && upperArm.IsChildOf(spine) && upperArm != spine)
                return true;
            return false;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            _punchTelegraph = Mathf.MoveTowards(_punchTelegraph, 0f, dt);
            if (_motor == null) _motor = GetComponentInParent<PlayerMotor>();
            if (_input == null) _input = GetComponentInParent<PlayerInputReader>();
            if (_grapple == null) _grapple = GetComponentInParent<ExperimentalGrapple>();
            HookBounce();
            // Cyan dash tell must run even when the limb rig failed to bind.
            TickAirDashTell(dt);
            if (!_bound) Cache(transform);
            if (!_bound) return;
            if (_punch == null) _punch = GetComponentInParent<PunchHitbox>();

            float speed = _motor != null ? _motor.HorizontalSpeed : 0f;
            bool grounded = _motor == null || _motor.IsGrounded;
            var st = _motor != null ? _motor.State : MoveState.Idle;
            bool sliding = st == MoveState.Slide;
            bool jet = st == MoveState.Jet || (_motor != null && _motor.Jetting);
            bool wallRun = st == MoveState.WallRun;
            bool climb = st == MoveState.WallClimb;
            bool mantle = st == MoveState.Mantle;
            bool air = st == MoveState.Air || (!grounded && !climb && !wallRun && !mantle);
            bool onSurf = wallRun || climb;
            bool leavingSurf = _wasSurf && !onSurf;
            if (onSurf && !_wasSurf)
            {
                _surfPhase = 0f;
                // A still crouch meets the wall in the guard, then the climb or the run.
                // A jump meets the climb in the contact, then the grab.
                // A jump meets the wall run in the contact, then the attach.
                // A normal entry is unchanged. The meet time is unchanged.
                _surfFromCrouch = _crouchFromStand && _dropVis > 0.2f && !_dropSlide;
                float meetVy = _motor != null ? _motor.Velocity.y : 0f;
                _surfFromJump = climb && !_surfFromCrouch && (meetVy > 1.5f || _prevVy > 1.5f || _pushOff > 0.02f);
                _surfFromJumpPush = _surfFromJump && _pushOff > 0.02f;
                _surfFromJumpWall = wallRun && !_surfFromCrouch && !_surfFromJump && (meetVy > 1.5f || _prevVy > 1.5f || _pushOff > 0.02f);
                _surfFromJumpWallPush = _surfFromJumpWall && _pushOff > 0.02f;
            }
            else if (!onSurf)
            {
                _surfFromCrouch = false;
                _surfFromJump = false;
                _surfFromJumpPush = false;
                _surfFromJumpWall = false;
                _surfFromJumpWallPush = false;
            }
            if (onSurf)
            {
                // Hand meets the surface, then the swing starts. A clock sine pops the arm.
                _surfIn = Mathf.MoveTowards(_surfIn, 1f, dt / 0.1f);
                _surfPhase += dt * (climb ? 7.5f : 9.5f);
            }
            else
                _surfIn = 0f;
            _wasSurf = onSurf;
            if (leavingSurf)
                _cycle = _exitLeadLeft ? Mathf.PI * 0.5f : Mathf.PI * 1.5f;
            // Jump holds a reach while rising. Fall trails the arms once drop speed builds.
            // The jet branch is separate and is not used here.
            float airRise = 0f;
            float airFall = 0f;
            float diveAmt = 0f;
            if (air && _motor != null)
            {
                float vy = _motor.Velocity.y;
                // Full tuck on a normal leave, full trail once the drop is clearly down.
                // The quiet band around zero is the apex hang. Jump height is unchanged.
                airRise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3.2f, 9f, vy));
                airFall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-3.2f, -9f, vy));
                // Visual only. airCrouchFallMult stays 2. The dart starts as soon as the drop is readable.
                bool airCrouch = !jet && _input != null && _input.CrouchHeld;
                if (airCrouch)
                    diveAmt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-1.2f, -6.5f, vy));
            }
            // The crouch arrives with the fall. Leaving it eases, so the arms do not pop on the land.
            // A jump eases the apex and the descent into that dart. Fall speed stays doubled.
            float diveStep = diveAmt >= _diveVis ? 1f : dt / 0.16f;
            if (_diveFromJump && !_jumpFromAirCrouch && diveAmt > _diveVis)
                diveStep = dt / 0.16f;
            _diveVis = Mathf.MoveTowards(_diveVis, diveAmt, diveStep);
            bool crouch = st == MoveState.Crouch;
            // Drop into the guard or the wedge, then rise back out. Speed is unchanged.
            _dropVis = Mathf.MoveTowards(_dropVis, sliding || crouch ? 1f : 0f, dt / 0.16f);
            // A still crouch into a slide eases the guard into the wedge.
            // A crouch walk into a slide eases the low stride into the wedge.
            // A ski into a slide eases the glide into the wedge. Ski speed is unchanged.
            // A jump eases the glide or the landing into the wedge.
            // A punch eases the cock or the strike into the wedge. A tag eases the connect into the wedge.
            // A punch miss eases the whiff into the wedge. Whiff time is unchanged.
            // A still crouch eases the guard into the wedge. A crouch walk keeps its ease.
            // A soft landing eases the absorb into the wedge. A hard landing keeps the jump entry.
            // A climb eases the grab into the wedge. A wall run eases the leave into the wedge.
            // A grapple release eases the line into the wedge. The gate stays off.
            // Becoming It eases the claim into the wedge. Claim time is unchanged.
            // A dash coming off cooldown eases the pulse into the wedge. Duration and cooldown are unchanged.
            // slideBoost stays 0. Exit time is unchanged.
            // The land numbers are written later this frame. Read the same impact here
            // so a soft touchdown is not filed as a jump.
            float slideRecoverT = _landHard;
            if (grounded && !_wasGrounded)
            {
                float impact = _motor != null ? _motor.LastLandImpactSpeed : 10f;
                float softEdge = 5f;
                float hardEdge = 24f;
                if (_motor != null && _motor.cfg != null)
                    hardEdge = Mathf.Max(softEdge + 1f, _motor.cfg.landStunSpeed);
                slideRecoverT = Mathf.Clamp01(Mathf.InverseLerp(softEdge, hardEdge, impact));
            }
            bool softSlideRecover = grounded && slideRecoverT < 0.4f && ((grounded && !_wasGrounded) || _landSquash > 0.08f);
            bool wallLeaveSlide = !wallRun && _exitFromWall && (leavingSurf || _wallExit > 0.2f);
            bool fromWallSlide = sliding && !_dropSlide && wallLeaveSlide && !jet && !_airDashPoseWas && !_jumpFromWall
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool climbLeaveSlide = !wallRun && !_exitFromWall && (leavingSurf || _wallExit > 0.2f);
            bool fromClimbSlide = sliding && !_dropSlide && !fromWallSlide && climbLeaveSlide && !jet && !_airDashPoseWas && !_jumpFromClimb
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool grappleReleaseSlide = _grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f;
            bool fromGrappleSlide = sliding && !_dropSlide && !fromWallSlide && !fromClimbSlide && grappleReleaseSlide && !jet && !_airDashPoseWas && !_jumpFromGrapple
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool fromClaimSlide = sliding && !_dropSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && _itClaim > 0.2f && !_jumpFromClaim && !jet && !_airDashPoseWas
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool fromReadySlide = sliding && !_dropSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide
                && _dashReady > 0.2f && _grapplePose <= 0.04f && !_jumpFromReady
                && !softSlideRecover && grounded && _wasGrounded && _landSquash <= 0.08f
                && !jet && !crouch && !_airDashPoseWas
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover
                && _punchPhaseWas != PunchPhase.MissRecover
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool fromSoftSlide = sliding && !_dropSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && softSlideRecover && !jet && !_airDashPoseWas
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool jumpIntoSlide = sliding && !_dropSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && (!grounded || !_wasGrounded || _landSquash > 0.08f);
            bool fromPunchSlide = sliding && !_dropSlide && !jumpIntoSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && !_airDashPoseWas
                && (_punchPhaseWas == PunchPhase.Windup || _punchPhaseWas == PunchPhase.Active)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool fromTagSlide = sliding && !_dropSlide && !jumpIntoSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && !fromPunchSlide && !_airDashPoseWas && !_jumpFromTag
                && _punchPhaseWas == PunchPhase.HitRecover
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool fromMissSlide = sliding && !_dropSlide && !jumpIntoSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && !fromPunchSlide && !fromTagSlide && !_airDashPoseWas && !_jumpFromMiss && !jet && !crouch
                && _punchPhaseWas == PunchPhase.MissRecover
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool fromCrouchSlide = sliding && !_dropSlide && !jumpIntoSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && !fromPunchSlide && !fromTagSlide && !fromMissSlide
                && !_airDashPoseWas && !jet && _crouchFromStand && !_crouchWalkArmed && speed <= 0.35f && _dropVis > 0.2f
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            bool fromSkiSlide = sliding && !_dropSlide && !jumpIntoSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && !fromPunchSlide && !fromTagSlide && !fromMissSlide && !fromCrouchSlide
                && !_airDashPoseWas && !jet && !(_crouchFromWalk && _dropVis > 0.2f) && _skiBlend > 0.2f
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (jumpIntoSlide)
            {
                _jumpToSlide = 1f;
                _jumpToSlideLand = grounded;
            }
            else if (!fromPunchSlide && !fromTagSlide && !fromMissSlide && !fromCrouchSlide && !fromSkiSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide)
            {
                if (sliding && !_dropSlide && _crouchFromStand && _dropVis > 0.2f)
                    _crouchToSlide = 1f;
                if (sliding && !_dropSlide && _crouchFromWalk && _dropVis > 0.2f)
                    _crouchWalkToSlide = 1f;
                if (sliding && !_dropSlide && _skiBlend > 0.2f)
                    _skiToSlide = _skiBlend;
            }
            if (fromPunchSlide && !_slideFromPunch)
            {
                // The cock or the strike eases into the wedge. A punch into a ski keeps its ease.
                // A crouch into a slide keeps its ease. A ski into a slide keeps its ease.
                // A jump into a slide keeps its ease. slideBoost stays 0. Windup time is unchanged.
                _slideFromPunch = true;
                _slideFromPunchIn = 0f;
                _punchSlideUaL = _upperArmL.localRotation;
                _punchSlideUaR = _upperArmR.localRotation;
                _punchSlideLaL = _lowerArmL.localRotation;
                _punchSlideLaR = _lowerArmR.localRotation;
                _punchSlideUlL = _upperLegL.localRotation;
                _punchSlideUlR = _upperLegR.localRotation;
                _punchSlideLlL = _lowerLegL.localRotation;
                _punchSlideLlR = _lowerLegR.localRotation;
                _punchSlideSp = _spine.localRotation;
                _punchSlideHp = _hips.localRotation;
                _punchSlideHd = _head.localRotation;
            }
            if (fromTagSlide && !_slideFromTag)
            {
                // The connect eases into the wedge. A tag into a ski keeps its ease.
                // A punch into a slide keeps its ease. A slide into a tag keeps its ease.
                // A crouch into a slide keeps its ease. A ski into a slide keeps its ease.
                // A jump into a slide keeps its ease. slideBoost stays 0. Connect time is unchanged.
                _slideFromTag = true;
                _slideFromTagIn = 0f;
                _tagSlideUaL = _upperArmL.localRotation;
                _tagSlideUaR = _upperArmR.localRotation;
                _tagSlideLaL = _lowerArmL.localRotation;
                _tagSlideLaR = _lowerArmR.localRotation;
                _tagSlideUlL = _upperLegL.localRotation;
                _tagSlideUlR = _upperLegR.localRotation;
                _tagSlideLlL = _lowerLegL.localRotation;
                _tagSlideLlR = _lowerLegR.localRotation;
                _tagSlideSp = _spine.localRotation;
                _tagSlideHp = _hips.localRotation;
                _tagSlideHd = _head.localRotation;
            }
            if (fromMissSlide && !_slideFromMiss)
            {
                // The whiff eases into the wedge. A punch miss into a ski keeps its ease.
                // A punch into a slide keeps its ease. A punch miss into a tag keeps its ease.
                // A punch miss into a jump keeps its push. A punch miss into an air dash keeps its ease.
                // slideBoost stays 0. Whiff time is unchanged.
                _slideFromMiss = true;
                _slideFromMissIn = 0f;
                _missSlideUaL = _upperArmL.localRotation;
                _missSlideUaR = _upperArmR.localRotation;
                _missSlideLaL = _lowerArmL.localRotation;
                _missSlideLaR = _lowerArmR.localRotation;
                _missSlideUlL = _upperLegL.localRotation;
                _missSlideUlR = _upperLegR.localRotation;
                _missSlideLlL = _lowerLegL.localRotation;
                _missSlideLlR = _lowerLegR.localRotation;
                _missSlideSp = _spine.localRotation;
                _missSlideHp = _hips.localRotation;
                _missSlideHd = _head.localRotation;
            }
            if (fromCrouchSlide && !_slideFromCrouch)
            {
                // The guard eases into the wedge. A punch miss into a slide keeps its ease.
                // A still crouch into a punch keeps its ease. A still crouch into a tag keeps its ease.
                // A still crouch into a ski has its own ease. A crouch walk into a slide keeps its ease.
                // slideBoost stays 0.
                _slideFromCrouch = true;
                _slideFromCrouchIn = 0f;
                _crouchSlideUaL = _upperArmL.localRotation;
                _crouchSlideUaR = _upperArmR.localRotation;
                _crouchSlideLaL = _lowerArmL.localRotation;
                _crouchSlideLaR = _lowerArmR.localRotation;
                _crouchSlideUlL = _upperLegL.localRotation;
                _crouchSlideUlR = _upperLegR.localRotation;
                _crouchSlideLlL = _lowerLegL.localRotation;
                _crouchSlideLlR = _lowerLegR.localRotation;
                _crouchSlideSp = _spine.localRotation;
                _crouchSlideHp = _hips.localRotation;
                _crouchSlideHd = _head.localRotation;
            }
            bool fromSprintSlide = sliding && !_dropSlide && !jumpIntoSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && !fromPunchSlide && !fromTagSlide && !fromMissSlide && !fromCrouchSlide && !fromSkiSlide
                && grounded && _wasGrounded && speed > 5.5f && _landSquash <= 0.08f && !_airDashPoseWas && !jet && !crouch
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (fromSprintSlide && !_slideFromSprint)
            {
                // The run eases into the wedge. A run into a ski keeps its ease.
                // A ski into a slide keeps its ease. A still crouch into a slide keeps its ease.
                // slideBoost stays 0.
                _slideFromSprint = true;
                _slideFromSprintIn = 0f;
                _sprintSlideUaL = _upperArmL.localRotation;
                _sprintSlideUaR = _upperArmR.localRotation;
                _sprintSlideLaL = _lowerArmL.localRotation;
                _sprintSlideLaR = _lowerArmR.localRotation;
                _sprintSlideUlL = _upperLegL.localRotation;
                _sprintSlideUlR = _upperLegR.localRotation;
                _sprintSlideLlL = _lowerLegL.localRotation;
                _sprintSlideLlR = _lowerLegR.localRotation;
                _sprintSlideSp = _spine.localRotation;
                _sprintSlideHp = _hips.localRotation;
                _sprintSlideHd = _head.localRotation;
            }
            bool fromWalkSlide = sliding && !_dropSlide && !fromSprintSlide && !jumpIntoSlide && !fromSoftSlide && !fromWallSlide && !fromClimbSlide && !fromGrappleSlide && !fromClaimSlide && !fromReadySlide && !fromPunchSlide && !fromTagSlide && !fromMissSlide && !fromCrouchSlide && !fromSkiSlide
                && grounded && _wasGrounded && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint
                && !_crouchFromWalk && !_crouchWalkArmed && !_crouchFromStand
                && _landSquash <= 0.08f && !_airDashPoseWas && !jet && !crouch
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (fromWalkSlide && !_slideFromWalk)
            {
                // The walk eases into the wedge. A run into a slide keeps its ease.
                // A crouch walk into a slide keeps its ease. A still crouch into a slide keeps its ease.
                // A ski into a slide keeps its ease. An air dash into a still crouch keeps its ease.
                // slideBoost stays 0.
                _slideFromWalk = true;
                _slideFromWalkIn = 0f;
                _walkSlideUaL = _upperArmL.localRotation;
                _walkSlideUaR = _upperArmR.localRotation;
                _walkSlideLaL = _lowerArmL.localRotation;
                _walkSlideLaR = _lowerArmR.localRotation;
                _walkSlideUlL = _upperLegL.localRotation;
                _walkSlideUlR = _upperLegR.localRotation;
                _walkSlideLlL = _lowerLegL.localRotation;
                _walkSlideLlR = _lowerLegR.localRotation;
                _walkSlideSp = _spine.localRotation;
                _walkSlideHp = _hips.localRotation;
                _walkSlideHd = _head.localRotation;
            }
            if (fromSkiSlide && !_slideFromSki)
            {
                // The glide eases into the wedge. A still crouch into a slide keeps its ease.
                // A crouch walk into a ski has its own ease. A crouch walk into a slide keeps its ease.
                // A slide into a ski has its own ease. slideBoost stays 0. Ski speed is unchanged.
                _slideFromSki = true;
                _slideFromSkiIn = 0f;
                _skiSlideUaL = _upperArmL.localRotation;
                _skiSlideUaR = _upperArmR.localRotation;
                _skiSlideLaL = _lowerArmL.localRotation;
                _skiSlideLaR = _lowerArmR.localRotation;
                _skiSlideUlL = _upperLegL.localRotation;
                _skiSlideUlR = _upperLegR.localRotation;
                _skiSlideLlL = _lowerLegL.localRotation;
                _skiSlideLlR = _lowerLegR.localRotation;
                _skiSlideSp = _spine.localRotation;
                _skiSlideHp = _hips.localRotation;
                _skiSlideHd = _head.localRotation;
            }
            if (fromSoftSlide && !_slideFromSoft)
            {
                // The absorb eases into the wedge. A soft landing into a ski keeps its ease.
                // A hard landing into a slide keeps its ease. A jump into a slide keeps its ease.
                // A punch into a slide keeps its ease. A crouch into a slide keeps its ease.
                // A ski into a slide keeps its ease. slideBoost stays 0. Land time is unchanged.
                _slideFromSoft = true;
                _slideFromSoftIn = 0f;
                _softSlideUaL = _upperArmL.localRotation;
                _softSlideUaR = _upperArmR.localRotation;
                _softSlideLaL = _lowerArmL.localRotation;
                _softSlideLaR = _lowerArmR.localRotation;
                _softSlideUlL = _upperLegL.localRotation;
                _softSlideUlR = _upperLegR.localRotation;
                _softSlideLlL = _lowerLegL.localRotation;
                _softSlideLlR = _lowerLegR.localRotation;
                _softSlideSp = _spine.localRotation;
                _softSlideHp = _hips.localRotation;
                _softSlideHd = _head.localRotation;
            }
            if (fromWallSlide && !_slideFromWall)
            {
                // The leave eases into the wedge. A wall run into a ski keeps its ease.
                // A climb into a slide keeps its ease. A wall run into a jump keeps its push.
                // A soft landing into a slide keeps its ease. A jump into a slide keeps its ease.
                // slideBoost stays 0. Exit time is unchanged.
                _slideFromWall = true;
                _slideFromWallIn = 0f;
                _wallSlideUaL = _upperArmL.localRotation;
                _wallSlideUaR = _upperArmR.localRotation;
                _wallSlideLaL = _lowerArmL.localRotation;
                _wallSlideLaR = _lowerArmR.localRotation;
                _wallSlideUlL = _upperLegL.localRotation;
                _wallSlideUlR = _upperLegR.localRotation;
                _wallSlideLlL = _lowerLegL.localRotation;
                _wallSlideLlR = _lowerLegR.localRotation;
                _wallSlideSp = _spine.localRotation;
                _wallSlideHp = _hips.localRotation;
                _wallSlideHd = _head.localRotation;
            }
            if (fromGrappleSlide && !_slideFromGrapple)
            {
                // The line eases into the wedge. A grapple release into a ski keeps its ease.
                // A grapple release into a jump keeps its push. A wall run into a slide keeps its ease.
                // A soft landing into a slide keeps its ease. slideBoost stays 0. The gate stays off.
                _slideFromGrapple = true;
                _slideFromGrappleIn = 0f;
                _grappleSlideUaL = _upperArmL.localRotation;
                _grappleSlideUaR = _upperArmR.localRotation;
                _grappleSlideLaL = _lowerArmL.localRotation;
                _grappleSlideLaR = _lowerArmR.localRotation;
                _grappleSlideUlL = _upperLegL.localRotation;
                _grappleSlideUlR = _upperLegR.localRotation;
                _grappleSlideLlL = _lowerLegL.localRotation;
                _grappleSlideLlR = _lowerLegR.localRotation;
                _grappleSlideSp = _spine.localRotation;
                _grappleSlideHp = _hips.localRotation;
                _grappleSlideHd = _head.localRotation;
            }
            if (fromClaimSlide && !_slideFromClaim)
            {
                // The claim eases into the wedge. Becoming It into a ski keeps its ease.
                // Becoming It into a punch keeps its ease. Becoming It into a jump keeps its push.
                // A grapple release into a slide keeps its ease. slideBoost stays 0. Claim time is unchanged.
                _slideFromClaim = true;
                _slideFromClaimIn = 0f;
                _claimSlideUaL = _upperArmL.localRotation;
                _claimSlideUaR = _upperArmR.localRotation;
                _claimSlideLaL = _lowerArmL.localRotation;
                _claimSlideLaR = _lowerArmR.localRotation;
                _claimSlideUlL = _upperLegL.localRotation;
                _claimSlideUlR = _upperLegR.localRotation;
                _claimSlideLlL = _lowerLegL.localRotation;
                _claimSlideLlR = _lowerLegR.localRotation;
                _claimSlideSp = _spine.localRotation;
                _claimSlideHp = _hips.localRotation;
                _claimSlideHd = _head.localRotation;
            }
            if (fromReadySlide && !_slideFromReady)
            {
                // The pulse eases into the wedge. A dash coming off cooldown into a ski keeps its ease.
                // A dash coming off cooldown into a punch keeps its ease.
                // A dash coming off cooldown into a tag keeps its ease.
                // A dash coming off cooldown into a jump keeps its push.
                // Becoming It into a slide keeps its ease. slideBoost stays 0. Duration and cooldown are unchanged.
                _slideFromReady = true;
                _slideFromReadyIn = 0f;
                _readySlideUaL = _upperArmL.localRotation;
                _readySlideUaR = _upperArmR.localRotation;
                _readySlideLaL = _lowerArmL.localRotation;
                _readySlideLaR = _lowerArmR.localRotation;
                _readySlideUlL = _upperLegL.localRotation;
                _readySlideUlR = _upperLegR.localRotation;
                _readySlideLlL = _lowerLegL.localRotation;
                _readySlideLlR = _lowerLegR.localRotation;
                _readySlideSp = _spine.localRotation;
                _readySlideHp = _hips.localRotation;
                _readySlideHd = _head.localRotation;
            }
            if (fromClimbSlide && !_slideFromClimb)
            {
                // The grab eases into the wedge. A climb into a ski keeps its ease.
                // A wall run into a slide keeps its own ease. A climb into a jump keeps its push.
                // A soft landing into a slide keeps its ease. A jump into a slide keeps its ease.
                // slideBoost stays 0. Exit time is unchanged.
                _slideFromClimb = true;
                _slideFromClimbIn = 0f;
                _climbSlideUaL = _upperArmL.localRotation;
                _climbSlideUaR = _upperArmR.localRotation;
                _climbSlideLaL = _lowerArmL.localRotation;
                _climbSlideLaR = _lowerArmR.localRotation;
                _climbSlideUlL = _upperLegL.localRotation;
                _climbSlideUlR = _upperLegR.localRotation;
                _climbSlideLlL = _lowerLegL.localRotation;
                _climbSlideLlR = _lowerLegR.localRotation;
                _climbSlideSp = _spine.localRotation;
                _climbSlideHp = _hips.localRotation;
                _climbSlideHd = _head.localRotation;
            }
            if (sliding)
                _dropSlide = true;
            else if (crouch)
            {
                // A slide that dies into a still crouch snapshots the wedge.
                // A slide that dies into a crouch walk eases the wedge into the low stride.
                // slideBoost stays 0.
                if (_dropSlide && speed <= 0.35f)
                {
                    _slideToCrouch = 1f;
                    if (!_stillFromWalk && !_stillFromSprint && !_stillFromSki
                        && !_crouchFromJump && !_crouchFromDash && !_airDashPoseWas && !_airDashArms
                        && !jet && !wallRun && !climb
                        && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active
                        && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                        && _itClaim <= 0.2f && _dashReady <= 0.2f
                        && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                        && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                    {
                        // The wedge eases into the guard. A ski into a still crouch keeps its ease.
                        // A run into a still crouch keeps its ease. slideBoost stays 0.
                        _stillFromSlide = true;
                        _stillFromSlideIn = 0f;
                        _slideGuardUaL = _upperArmL.localRotation;
                        _slideGuardUaR = _upperArmR.localRotation;
                        _slideGuardLaL = _lowerArmL.localRotation;
                        _slideGuardLaR = _lowerArmR.localRotation;
                        _slideGuardUlL = _upperLegL.localRotation;
                        _slideGuardUlR = _upperLegR.localRotation;
                        _slideGuardLlL = _lowerLegL.localRotation;
                        _slideGuardLlR = _lowerLegR.localRotation;
                        _slideGuardSp = _spine.localRotation;
                        _slideGuardHp = _hips.localRotation;
                        _slideGuardHd = _head.localRotation;
                    }
                }
                else if (_dropSlide && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint)
                {
                    _slideToCrouchWalk = 1f;
                    if (!_stillFromSlide && !_crouchFromJump && !_crouchFromDash && !_airDashPoseWas && !_airDashArms
                        && !jet && !wallRun && !climb
                        && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active
                        && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                        && _itClaim <= 0.2f && _dashReady <= 0.2f
                        && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                        && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                    {
                        // The wedge eases into the low stride. A slide into a still crouch keeps its ease.
                        // A ski into a crouch walk keeps its ease. slideBoost stays 0.
                        _crouchWalkFromSlide = true;
                        _crouchWalkFromSlideIn = 0f;
                        _slideWalkUaL = _upperArmL.localRotation;
                        _slideWalkUaR = _upperArmR.localRotation;
                        _slideWalkLaL = _lowerArmL.localRotation;
                        _slideWalkLaR = _lowerArmR.localRotation;
                        _slideWalkUlL = _upperLegL.localRotation;
                        _slideWalkUlR = _upperLegR.localRotation;
                        _slideWalkLlL = _lowerLegL.localRotation;
                        _slideWalkLlR = _lowerLegR.localRotation;
                        _slideWalkSp = _spine.localRotation;
                        _slideWalkHp = _hips.localRotation;
                        _slideWalkHd = _head.localRotation;
                    }
                }
                _dropSlide = false;
            }
            else if (_dropVis <= 0.001f)
                _dropSlide = false;
            if (sliding || !crouch || speed > 0.35f)
                _slideToCrouch = 0f;
            else if (_slideToCrouch > 0f)
                _slideToCrouch = Mathf.MoveTowards(_slideToCrouch, 0f, dt / 0.16f);
            if (sliding || !crouch || speed <= 0.35f || speed > 5.5f || st == MoveState.Sprint)
            {
                _slideToCrouchWalk = 0f;
                _crouchWalkFromSlide = false;
            }
            else if (_slideToCrouchWalk > 0f)
                _slideToCrouchWalk = Mathf.MoveTowards(_slideToCrouchWalk, 0f, dt / 0.16f);
            if (_crouchWalkFromSlide)
                _crouchWalkFromSlideIn = Mathf.MoveTowards(_crouchWalkFromSlideIn, 1f, dt / 0.04f);
            if (!sliding)
                _crouchToSlide = 0f;
            else if (_crouchToSlide > 0f)
                _crouchToSlide = Mathf.MoveTowards(_crouchToSlide, 0f, dt / 0.16f);
            if (!sliding)
                _crouchWalkToSlide = 0f;
            else if (_crouchWalkToSlide > 0f)
                _crouchWalkToSlide = Mathf.MoveTowards(_crouchWalkToSlide, 0f, dt / 0.16f);
            if (!sliding)
                _skiToSlide = 0f;
            else if (_skiToSlide > 0f)
                _skiToSlide = Mathf.MoveTowards(_skiToSlide, 0f, dt / 0.22f);
            if (!sliding)
            {
                _jumpToSlide = 0f;
                _jumpToSlideLand = false;
                _slideFromPunch = false;
                _slideFromTag = false;
                _slideFromSoft = false;
                _slideFromClimb = false;
                _slideFromWall = false;
                _slideFromGrapple = false;
                _slideFromClaim = false;
                _slideFromReady = false;
                _slideFromMiss = false;
                _slideFromCrouch = false;
                _slideFromSki = false;
                _slideFromSprint = false;
                _slideFromWalk = false;
            }
            else if (_jumpToSlide > 0f)
                _jumpToSlide = Mathf.MoveTowards(_jumpToSlide, 0f, dt / 0.16f);
            if (_slideFromPunch && sliding)
                _slideFromPunchIn = Mathf.MoveTowards(_slideFromPunchIn, 1f, dt / 0.04f);
            if (_slideFromTag && sliding)
                _slideFromTagIn = Mathf.MoveTowards(_slideFromTagIn, 1f, dt / 0.04f);
            if (_slideFromSoft && sliding)
                _slideFromSoftIn = Mathf.MoveTowards(_slideFromSoftIn, 1f, dt / 0.04f);
            if (_slideFromClimb && sliding)
                _slideFromClimbIn = Mathf.MoveTowards(_slideFromClimbIn, 1f, dt / 0.04f);
            if (_slideFromWall && sliding)
                _slideFromWallIn = Mathf.MoveTowards(_slideFromWallIn, 1f, dt / 0.04f);
            if (_slideFromGrapple && sliding)
                _slideFromGrappleIn = Mathf.MoveTowards(_slideFromGrappleIn, 1f, dt / 0.04f);
            if (_slideFromClaim && sliding)
                _slideFromClaimIn = Mathf.MoveTowards(_slideFromClaimIn, 1f, dt / 0.04f);
            if (_slideFromReady && sliding)
                _slideFromReadyIn = Mathf.MoveTowards(_slideFromReadyIn, 1f, dt / 0.04f);
            if (_slideFromMiss && sliding)
                _slideFromMissIn = Mathf.MoveTowards(_slideFromMissIn, 1f, dt / 0.04f);
            if (_slideFromCrouch && sliding)
                _slideFromCrouchIn = Mathf.MoveTowards(_slideFromCrouchIn, 1f, dt / 0.04f);
            if (_slideFromSki && sliding)
                _slideFromSkiIn = Mathf.MoveTowards(_slideFromSkiIn, 1f, dt / 0.04f);
            if (_slideFromSprint && sliding)
                _slideFromSprintIn = Mathf.MoveTowards(_slideFromSprintIn, 1f, dt / 0.04f);
            if (_slideFromWalk && sliding)
                _slideFromWalkIn = Mathf.MoveTowards(_slideFromWalkIn, 1f, dt / 0.04f);
            // Read before the clear below. An air dash raises speed and leaves crouch the same frame.
            bool crouchWalkPose = _crouchFromWalk && _dropVis > 0.2f && !sliding;
            bool standWalk = _wasGrounded && _prevSpeed > 0.35f && _prevSpeed <= 5.5f
                && !_crouchFromStand && !crouchWalkPose && !_crouchWalkArmed
                && !_dropSlide && _skiBlend <= 0.2f && _landSquash <= 0.08f;
            bool standRun = _wasGrounded && _prevSpeed > 5.5f
                && !_crouchFromStand && !crouchWalkPose && !_crouchWalkArmed
                && !_dropSlide && _skiBlend <= 0.2f && _landSquash <= 0.08f;
            // Read before the clear below. Starting to move leaves the planted guard in one frame.
            bool stillWasPlanted = _crouchFromStand && _stillCrouchWas && _dropVis > 0.2f && _skiBlend <= 0.2f;
            if (crouch && !sliding && speed <= 0.35f)
                _crouchFromStand = true;
            else if ((crouch && speed > 0.35f) || sliding || _dropVis <= 0.001f)
                _crouchFromStand = false;
            bool dashPoseNow = (_motor != null && _motor.IsAirDashing) || _airDashPoseWas || _airDashArms;
            if (stillWasPlanted && crouch && grounded && !sliding && !jet
                && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint && !_dropSlide
                && !_stillStrideHeld
                && !_crouchWalkFromWalk && !_crouchWalkFromSprint && !_crouchWalkFromSki && !_crouchWalkFromSlide
                && !dashPoseNow && !wallRun && !climb
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active
                && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The planted guard eases into the low stride. A walk into a crouch walk keeps its ease.
                // A run into a crouch walk keeps its ease. A crouch walk into a still crouch keeps its ease.
                _crouchWalkFromStill = true;
                _crouchWalkFromStillIn = 0f;
                _stillStrideHeld = true;
                _stillStrideUaL = _upperArmL.localRotation;
                _stillStrideUaR = _upperArmR.localRotation;
                _stillStrideLaL = _lowerArmL.localRotation;
                _stillStrideLaR = _lowerArmR.localRotation;
                _stillStrideUlL = _upperLegL.localRotation;
                _stillStrideUlR = _upperLegR.localRotation;
                _stillStrideLlL = _lowerLegL.localRotation;
                _stillStrideLlR = _lowerLegR.localRotation;
                _stillStrideSp = _spine.localRotation;
                _stillStrideHp = _hips.localRotation;
                _stillStrideHd = _head.localRotation;
            }
            bool jumpStill = !sliding && !jet && speed <= 0.35f && !dashPoseNow && !_jumpFromStill && !_jumpFromCrouchWalk
                && _diveFromJump
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active
                && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                && ((grounded && crouch) || (!grounded && _input != null && _input.CrouchHeld && _diveVis <= 0.02f));
            if (jumpStill && !_stillCrouchWas && !_crouchFromJump && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The jump eases into the guard. A jump into a ski keeps its ease.
                // A jump into a slide keeps its ease. Jump height is unchanged.
                _crouchFromJump = true;
                _crouchFromJumpIn = 0f;
                _jumpCrouchUaL = _upperArmL.localRotation;
                _jumpCrouchUaR = _upperArmR.localRotation;
                _jumpCrouchLaL = _lowerArmL.localRotation;
                _jumpCrouchLaR = _lowerArmR.localRotation;
                _jumpCrouchUlL = _upperLegL.localRotation;
                _jumpCrouchUlR = _upperLegR.localRotation;
                _jumpCrouchLlL = _lowerLegL.localRotation;
                _jumpCrouchLlR = _lowerLegR.localRotation;
                _jumpCrouchSp = _spine.localRotation;
                _jumpCrouchHp = _hips.localRotation;
                _jumpCrouchHd = _head.localRotation;
            }
            else if (jumpStill && !_stillCrouchWas && !_crouchFromJump)
            {
                _crouchFromJump = true;
                _crouchFromJumpIn = 1f;
            }
            bool inStill = !sliding && !jet && speed <= 0.35f
                && ((grounded && crouch) || (!grounded && _input != null && _input.CrouchHeld));
            if (!inStill)
                _crouchFromJump = false;
            else if (_crouchFromJump)
                _crouchFromJumpIn = Mathf.MoveTowards(_crouchFromJumpIn, 1f, dt / 0.04f);
            bool walkIntoStill = inStill && !_stillCrouchWas && standWalk && !_stillFromSlide
                && !_crouchFromJump && !_crouchFromDash && !dashPoseNow && !_diveFromJump
                && !sliding && !jet && !wallRun && !climb && !leavingSurf
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active
                && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (walkIntoStill)
            {
                // The walk eases into the guard. A soft landing into a punch keeps its ease.
                // A soft landing into a tag keeps its ease. An air crouch into a punch keeps its ease.
                _stillFromWalk = true;
                _stillFromWalkIn = 0f;
                _walkGuardUaL = _upperArmL.localRotation;
                _walkGuardUaR = _upperArmR.localRotation;
                _walkGuardLaL = _lowerArmL.localRotation;
                _walkGuardLaR = _lowerArmR.localRotation;
                _walkGuardUlL = _upperLegL.localRotation;
                _walkGuardUlR = _upperLegR.localRotation;
                _walkGuardLlL = _lowerLegL.localRotation;
                _walkGuardLlR = _lowerLegR.localRotation;
                _walkGuardSp = _spine.localRotation;
                _walkGuardHp = _hips.localRotation;
                _walkGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromWalk = false;
            else if (_stillFromWalk)
                _stillFromWalkIn = Mathf.MoveTowards(_stillFromWalkIn, 1f, dt / 0.04f);
            bool sprintIntoStill = inStill && !_stillCrouchWas && standRun && !standWalk
                && !_stillFromWalk && !_stillFromSlide && !_crouchFromJump && !_crouchFromDash && !dashPoseNow && !_diveFromJump
                && !sliding && !jet && !wallRun && !climb && !leavingSurf
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active
                && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (sprintIntoStill)
            {
                // The run eases into the guard. A walk into a still crouch keeps its ease.
                // A jump into a still crouch keeps its ease. An air dash into a still crouch keeps its ease.
                _stillFromSprint = true;
                _stillFromSprintIn = 0f;
                _sprintGuardUaL = _upperArmL.localRotation;
                _sprintGuardUaR = _upperArmR.localRotation;
                _sprintGuardLaL = _lowerArmL.localRotation;
                _sprintGuardLaR = _lowerArmR.localRotation;
                _sprintGuardUlL = _upperLegL.localRotation;
                _sprintGuardUlR = _upperLegR.localRotation;
                _sprintGuardLlL = _lowerLegL.localRotation;
                _sprintGuardLlR = _lowerLegR.localRotation;
                _sprintGuardSp = _spine.localRotation;
                _sprintGuardHp = _hips.localRotation;
                _sprintGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromSprint = false;
            else if (_stillFromSprint)
                _stillFromSprintIn = Mathf.MoveTowards(_stillFromSprintIn, 1f, dt / 0.04f);
            bool skiIntoStill = inStill && grounded && !_stillCrouchWas && _skiBlend > 0.2f && !_dropSlide
                && !_stillFromWalk && !_stillFromSprint && !_stillFromSlide && !_crouchFromJump && !_crouchFromDash
                && !dashPoseNow && !_diveFromJump && !sliding && !jet && !wallRun && !climb && !leavingSurf
                && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active
                && _punchPhaseWas != PunchPhase.HitRecover && _punchPhaseWas != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (skiIntoStill)
            {
                // The glide eases into the guard. A run into a still crouch keeps its ease.
                // A walk into a still crouch keeps its ease. Ski speed is unchanged.
                _stillFromSki = true;
                _stillFromSkiIn = 0f;
                _skiGuardUaL = _upperArmL.localRotation;
                _skiGuardUaR = _upperArmR.localRotation;
                _skiGuardLaL = _lowerArmL.localRotation;
                _skiGuardLaR = _lowerArmR.localRotation;
                _skiGuardUlL = _upperLegL.localRotation;
                _skiGuardUlR = _upperLegR.localRotation;
                _skiGuardLlL = _lowerLegL.localRotation;
                _skiGuardLlR = _lowerLegR.localRotation;
                _skiGuardSp = _spine.localRotation;
                _skiGuardHp = _hips.localRotation;
                _skiGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromSki = false;
            else if (_stillFromSki)
                _stillFromSkiIn = Mathf.MoveTowards(_stillFromSkiIn, 1f, dt / 0.04f);
            if (!inStill)
                _stillFromSlide = false;
            else if (_stillFromSlide)
                _stillFromSlideIn = Mathf.MoveTowards(_stillFromSlideIn, 1f, dt / 0.04f);
            bool enterStill = inStill && !_stillCrouchWas;
            _stillCrouchWas = inStill;
            if (crouch && !sliding && speed > 0.35f && speed <= 5.5f && !_dropSlide)
                _crouchFromWalk = true;
            else if (!crouch || sliding || speed <= 0.35f || speed > 5.5f || _dropVis <= 0.001f)
                _crouchFromWalk = false;
            bool crouchWalkWasArmed = _crouchWalkArmed;
            if (crouch && !sliding && speed > 0.35f && speed <= 5.5f && !_dropSlide)
                _crouchWalkArmed = true;
            else if ((crouch && speed <= 0.35f) || sliding || speed > 5.5f || _dropVis <= 0.001f)
                _crouchWalkArmed = false;
            // Stand-up from a slide: the feet enter the stride while the hips are still low.
            // slideBoost stays 0. The speed you already have carries.
            // A still crouch eases into the idle breath. The hips do not pop flat.
            bool slideExit = _dropSlide && !sliding && !crouch && st != MoveState.Ski;
            // A slide that dies into a stand rises into the idle breath.
            // A slide that dies into a walk rises into the stride.
            // A slide that dies into a sprint rises into the long stride.
            bool slideIdleExit = slideExit && speed <= 0.35f;
            bool slideWalkExit = slideExit && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint;
            bool slideSprintExit = slideExit && !slideIdleExit && !slideWalkExit;
            // A still crouch eases into the idle breath. A still crouch into a sprint rises into the long stride.
            bool crouchStandSprint = _crouchFromStand && !_dropSlide && !sliding && !crouch && st == MoveState.Sprint;
            bool skiing = st == MoveState.Ski;
            // A walk eases into the glide. A sprint closes the long stride into it.
            // A still crouch eases the guard into the glide. A crouch walk eases the low stride into it.
            // A slide eases the wedge into the glide. A jump eases the glide or the landing into it.
            // A punch eases the cock or the strike into the glide. A tag eases the connect into the glide.
            // A punch miss eases the whiff into the glide. Whiff time is unchanged.
            // A soft landing eases the absorb into the glide. A hard landing eases the deeper absorb into it.
            // An air crouch eases the dart into the glide. Fall speed is unchanged.
            // A grapple release eases the line into the glide. The gate stays off.
            // Becoming It eases the claim into the glide. Claim time is unchanged.
            // A dash coming off cooldown eases the pulse into the glide. Duration and cooldown are unchanged.
            // A climb eases the grab into the glide. A wall run eases the leave into the glide.
            // Ski speed is unchanged. Exit time is unchanged.
            if (skiing && _skiBlend <= 0.02f)
            {
                bool fromSlide = _dropSlide;
                // The land numbers are written later this frame. Read the same impact here
                // so a soft touchdown is not filed as a jump.
                float recoverT = _landHard;
                if (grounded && !_wasGrounded)
                {
                    float impact = _motor != null ? _motor.LastLandImpactSpeed : 10f;
                    float softEdge = 5f;
                    float hardEdge = 24f;
                    if (_motor != null && _motor.cfg != null)
                        hardEdge = Mathf.Max(softEdge + 1f, _motor.cfg.landStunSpeed);
                    recoverT = Mathf.Clamp01(Mathf.InverseLerp(softEdge, hardEdge, impact));
                }
                bool softRecover = grounded && recoverT < 0.4f && ((grounded && !_wasGrounded) || _landSquash > 0.08f);
                bool hardRecover = grounded && recoverT >= 0.4f && ((grounded && !_wasGrounded) || _landSquash > 0.08f);
                bool wallLeaveNow = !wallRun && _exitFromWall && (leavingSurf || _wallExit > 0.2f);
                bool fromWall = !fromSlide && wallLeaveNow && !jet && !crouch && !_airDashPoseWas && !_jumpFromWall
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool climbLeaveNow = !wallRun && !_exitFromWall && (leavingSurf || _wallExit > 0.2f);
                bool fromClimb = !fromSlide && !fromWall && climbLeaveNow && !jet && !crouch && !_airDashPoseWas && !_jumpFromClimb
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool fromDart = !fromSlide && !fromWall && !fromClimb && _diveVis > 0.2f && !jet && !crouch && !_airDashPoseWas && !_jumpFromAirCrouch
                    && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool grappleRelease = _grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f;
                bool fromGrapple = !fromSlide && !fromWall && !fromClimb && !fromDart && grappleRelease && !jet && !crouch && !_airDashPoseWas && !_jumpFromGrapple
                    && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool fromClaim = !fromSlide && !fromWall && !fromClimb && !fromDart && !fromGrapple && _itClaim > 0.2f && !_jumpFromClaim && !jet && !crouch && !_airDashPoseWas
                    && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool fromReady = !fromSlide && !fromWall && !fromClimb && !fromDart && !fromGrapple && !fromClaim
                    && _dashReady > 0.2f && _grapplePose <= 0.04f && !_jumpFromReady
                    && !hardRecover && !softRecover && grounded && _wasGrounded && _landSquash <= 0.08f
                    && !jet && !crouch && !_airDashPoseWas
                    && _punchPhaseWas != PunchPhase.Windup && _punchPhaseWas != PunchPhase.Active && _punchPhaseWas != PunchPhase.HitRecover
                    && _punchPhaseWas != PunchPhase.MissRecover
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool fromHard = !fromSlide && !fromWall && !fromClimb && !fromDart && !fromGrapple && !fromClaim && !fromReady && hardRecover && !crouch && !jet && !_airDashPoseWas
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool fromSoft = !fromSlide && !fromWall && !fromClimb && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromHard && softRecover && !crouch && !jet && !_airDashPoseWas
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool fromJump = !fromSlide && !fromSoft && !fromHard && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromWall && !fromClimb && !_airDashPoseWas && (!grounded || !_wasGrounded || _landSquash > 0.08f);
                bool fromCrouchWalk = !fromSlide && !fromJump && !fromSoft && !fromHard && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromWall && !fromClimb && !_airDashPoseWas && !_dropSlide && speed > 0.35f && speed <= 5.5f && _dropVis > 0.2f;
                bool fromPunch = !fromSlide && !fromJump && !fromSoft && !fromHard && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromWall && !fromClimb && !crouch && !_airDashPoseWas
                    && (_punchPhaseWas == PunchPhase.Windup || _punchPhaseWas == PunchPhase.Active)
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool fromTag = !fromSlide && !fromJump && !fromSoft && !fromHard && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromWall && !fromClimb && !fromPunch && !crouch && !_airDashPoseWas && !_jumpFromTag
                    && _punchPhaseWas == PunchPhase.HitRecover
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                bool fromMiss = !fromSlide && !fromJump && !fromSoft && !fromHard && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromWall && !fromClimb && !fromPunch && !fromTag && !crouch && !_airDashPoseWas && !_jumpFromMiss
                    && _punchPhaseWas == PunchPhase.MissRecover
                    && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
                _skiFromWalk = !fromSlide && !fromJump && !fromSoft && !fromHard && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromWall && !fromClimb && !fromPunch && !fromTag && !fromMiss && !_airDashPoseWas && grounded && _wasGrounded && speed > 0.35f && speed <= 5.5f && !fromCrouchWalk;
                _skiFromSprint = !fromSlide && !fromJump && !fromSoft && !fromHard && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromWall && !fromClimb && !fromPunch && !fromTag && !fromMiss && !_airDashPoseWas && speed > 5.5f;
                if (_skiFromWalk && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The walk eases into the glide. A run into a ski keeps its ease.
                    // A crouch walk into a ski has its own ease. A still crouch into a ski has its own ease.
                    // A walk into a slide keeps its ease. Ski speed is unchanged.
                    _skiFromWalkIn = 0f;
                    _walkSkiUaL = _upperArmL.localRotation;
                    _walkSkiUaR = _upperArmR.localRotation;
                    _walkSkiLaL = _lowerArmL.localRotation;
                    _walkSkiLaR = _lowerArmR.localRotation;
                    _walkSkiUlL = _upperLegL.localRotation;
                    _walkSkiUlR = _upperLegR.localRotation;
                    _walkSkiLlL = _lowerLegL.localRotation;
                    _walkSkiLlR = _lowerLegR.localRotation;
                    _walkSkiSp = _spine.localRotation;
                    _walkSkiHp = _hips.localRotation;
                    _walkSkiHd = _head.localRotation;
                }
                else if (_skiFromWalk)
                    _skiFromWalkIn = 1f;
                if (_skiFromSprint && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The run eases into the glide. A walk into a ski keeps its ease.
                    // Ski speed is unchanged.
                    _skiFromSprintIn = 0f;
                    _sprintSkiUaL = _upperArmL.localRotation;
                    _sprintSkiUaR = _upperArmR.localRotation;
                    _sprintSkiLaL = _lowerArmL.localRotation;
                    _sprintSkiLaR = _lowerArmR.localRotation;
                    _sprintSkiUlL = _upperLegL.localRotation;
                    _sprintSkiUlR = _upperLegR.localRotation;
                    _sprintSkiLlL = _lowerLegL.localRotation;
                    _sprintSkiLlR = _lowerLegR.localRotation;
                    _sprintSkiSp = _spine.localRotation;
                    _sprintSkiHp = _hips.localRotation;
                    _sprintSkiHd = _head.localRotation;
                }
                else if (_skiFromSprint)
                    _skiFromSprintIn = 1f;
                if (fromSlide && !_skiFromSlide && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The wedge eases into the glide. A ski into a slide keeps its ease.
                    // A walk into a ski keeps its ease. A run into a ski keeps its ease.
                    // Ski speed is unchanged. slideBoost stays 0.
                    _skiFromSlide = true;
                    _skiFromSlideIn = 0f;
                    _slideSkiUaL = _upperArmL.localRotation;
                    _slideSkiUaR = _upperArmR.localRotation;
                    _slideSkiLaL = _lowerArmL.localRotation;
                    _slideSkiLaR = _lowerArmR.localRotation;
                    _slideSkiUlL = _upperLegL.localRotation;
                    _slideSkiUlR = _upperLegR.localRotation;
                    _slideSkiLlL = _lowerLegL.localRotation;
                    _slideSkiLlR = _lowerLegR.localRotation;
                    _slideSkiSp = _spine.localRotation;
                    _slideSkiHp = _hips.localRotation;
                    _slideSkiHd = _head.localRotation;
                }
                else if (fromSlide && !_skiFromSlide)
                    _skiFromSlideIn = 1f;
                bool stillCrouchSki = !fromSlide && !fromJump && !fromSoft && !fromHard && !fromDart && !fromGrapple && !fromClaim && !fromReady && !fromWall && !fromClimb && !fromPunch && !fromTag && !fromMiss && !_airDashPoseWas && !_dropSlide && speed <= 0.35f && _dropVis > 0.2f;
                if (stillCrouchSki && !_skiFromCrouch && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The planted guard eases into the glide. A crouch walk into a ski has its own ease.
                    // A walk into a ski keeps its ease. A slide into a ski has its own ease.
                    // Ski speed is unchanged.
                    _skiFromCrouchIn = 0f;
                    _stillSkiSnap = true;
                    _stillSkiUaL = _upperArmL.localRotation;
                    _stillSkiUaR = _upperArmR.localRotation;
                    _stillSkiLaL = _lowerArmL.localRotation;
                    _stillSkiLaR = _lowerArmR.localRotation;
                    _stillSkiUlL = _upperLegL.localRotation;
                    _stillSkiUlR = _upperLegR.localRotation;
                    _stillSkiLlL = _lowerLegL.localRotation;
                    _stillSkiLlR = _lowerLegR.localRotation;
                    _stillSkiSp = _spine.localRotation;
                    _stillSkiHp = _hips.localRotation;
                    _stillSkiHd = _head.localRotation;
                }
                else if (stillCrouchSki && !_skiFromCrouch)
                    _skiFromCrouchIn = 1f;
                _skiFromCrouch = stillCrouchSki;
                bool crouchWalkSki = fromCrouchWalk && !fromPunch && !fromTag && !fromMiss;
                if (crouchWalkSki && !_skiFromCrouchWalk && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The low stride eases into the glide. A still crouch into a ski has its own ease.
                    // A walk into a ski keeps its ease. A slide into a ski has its own ease.
                    // Ski speed is unchanged.
                    _skiFromCrouchWalkIn = 0f;
                    _crouchWalkSkiSnap = true;
                    _crouchWalkSkiUaL = _upperArmL.localRotation;
                    _crouchWalkSkiUaR = _upperArmR.localRotation;
                    _crouchWalkSkiLaL = _lowerArmL.localRotation;
                    _crouchWalkSkiLaR = _lowerArmR.localRotation;
                    _crouchWalkSkiUlL = _upperLegL.localRotation;
                    _crouchWalkSkiUlR = _upperLegR.localRotation;
                    _crouchWalkSkiLlL = _lowerLegL.localRotation;
                    _crouchWalkSkiLlR = _lowerLegR.localRotation;
                    _crouchWalkSkiSp = _spine.localRotation;
                    _crouchWalkSkiHp = _hips.localRotation;
                    _crouchWalkSkiHd = _head.localRotation;
                }
                else if (crouchWalkSki && !_skiFromCrouchWalk)
                    _skiFromCrouchWalkIn = 1f;
                _skiFromCrouchWalk = crouchWalkSki;
                _slideToSki = fromSlide;
                _skiFromJump = fromJump;
                _skiFromJumpLand = fromJump && grounded;
                if (fromPunch && !_skiFromPunch)
                {
                    // The cock or the strike eases into the glide. A ski into a punch keeps its ease.
                    // A slide into a ski has its own ease. A jump into a ski keeps its ease.
                    // Ski speed is unchanged. Windup time is unchanged.
                    _skiFromPunch = true;
                    _skiFromPunchIn = 0f;
                    _punchSkiUaL = _upperArmL.localRotation;
                    _punchSkiUaR = _upperArmR.localRotation;
                    _punchSkiLaL = _lowerArmL.localRotation;
                    _punchSkiLaR = _lowerArmR.localRotation;
                    _punchSkiUlL = _upperLegL.localRotation;
                    _punchSkiUlR = _upperLegR.localRotation;
                    _punchSkiLlL = _lowerLegL.localRotation;
                    _punchSkiLlR = _lowerLegR.localRotation;
                    _punchSkiSp = _spine.localRotation;
                    _punchSkiHp = _hips.localRotation;
                    _punchSkiHd = _head.localRotation;
                }
                if (fromTag && !_skiFromTag)
                {
                    // The connect eases into the glide. A punch into a ski keeps its ease.
                    // A punch into a slide keeps its ease. A tag into a jump keeps its push.
                    // A slide into a ski has its own ease. Ski speed is unchanged. Connect time is unchanged.
                    _skiFromTag = true;
                    _skiFromTagIn = 0f;
                    _tagSkiUaL = _upperArmL.localRotation;
                    _tagSkiUaR = _upperArmR.localRotation;
                    _tagSkiLaL = _lowerArmL.localRotation;
                    _tagSkiLaR = _lowerArmR.localRotation;
                    _tagSkiUlL = _upperLegL.localRotation;
                    _tagSkiUlR = _upperLegR.localRotation;
                    _tagSkiLlL = _lowerLegL.localRotation;
                    _tagSkiLlR = _lowerLegR.localRotation;
                    _tagSkiSp = _spine.localRotation;
                    _tagSkiHp = _hips.localRotation;
                    _tagSkiHd = _head.localRotation;
                }
                if (fromMiss && !_skiFromMiss)
                {
                    // The whiff eases into the glide. A punch into a ski keeps its ease.
                    // A punch miss into a tag keeps its ease. A punch miss into a jump keeps its push.
                    // A punch miss into an air dash keeps its ease. A dash coming off cooldown into a slide keeps its ease.
                    // Whiff time is unchanged. Ski speed is unchanged.
                    _skiFromMiss = true;
                    _skiFromMissIn = 0f;
                    _missSkiUaL = _upperArmL.localRotation;
                    _missSkiUaR = _upperArmR.localRotation;
                    _missSkiLaL = _lowerArmL.localRotation;
                    _missSkiLaR = _lowerArmR.localRotation;
                    _missSkiUlL = _upperLegL.localRotation;
                    _missSkiUlR = _upperLegR.localRotation;
                    _missSkiLlL = _lowerLegL.localRotation;
                    _missSkiLlR = _lowerLegR.localRotation;
                    _missSkiSp = _spine.localRotation;
                    _missSkiHp = _hips.localRotation;
                    _missSkiHd = _head.localRotation;
                }
                if (fromSoft && !_skiFromSoft)
                {
                    // The absorb eases into the glide. A hard landing into a ski keeps its own ease.
                    // A jump into a ski keeps its ease. A punch into a ski keeps its ease.
                    // A slide into a ski has its own ease. Ski speed is unchanged. Land time is unchanged.
                    _skiFromSoft = true;
                    _skiFromSoftIn = 0f;
                    _softSkiUaL = _upperArmL.localRotation;
                    _softSkiUaR = _upperArmR.localRotation;
                    _softSkiLaL = _lowerArmL.localRotation;
                    _softSkiLaR = _lowerArmR.localRotation;
                    _softSkiUlL = _upperLegL.localRotation;
                    _softSkiUlR = _upperLegR.localRotation;
                    _softSkiLlL = _lowerLegL.localRotation;
                    _softSkiLlR = _lowerLegR.localRotation;
                    _softSkiSp = _spine.localRotation;
                    _softSkiHp = _hips.localRotation;
                    _softSkiHd = _head.localRotation;
                }
                if (fromHard && !_skiFromHard)
                {
                    // The deeper absorb eases into the glide. A soft landing into a ski keeps its ease.
                    // A jump into a ski keeps its ease. An air dash into a ski keeps its ease.
                    // A hard landing into a slide keeps its ease. Ski speed is unchanged. Land time is unchanged.
                    _skiFromHard = true;
                    _skiFromHardIn = 0f;
                    _hardSkiUaL = _upperArmL.localRotation;
                    _hardSkiUaR = _upperArmR.localRotation;
                    _hardSkiLaL = _lowerArmL.localRotation;
                    _hardSkiLaR = _lowerArmR.localRotation;
                    _hardSkiUlL = _upperLegL.localRotation;
                    _hardSkiUlR = _upperLegR.localRotation;
                    _hardSkiLlL = _lowerLegL.localRotation;
                    _hardSkiLlR = _lowerLegR.localRotation;
                    _hardSkiSp = _spine.localRotation;
                    _hardSkiHp = _hips.localRotation;
                    _hardSkiHd = _head.localRotation;
                }
                if (fromDart && !_skiFromDart)
                {
                    // The dart eases into the glide. A hard landing into a ski keeps its ease.
                    // An air dash into a ski keeps its ease. A jump into a ski keeps its ease.
                    // An air crouch into a slide keeps its ease. Fall speed is unchanged. Ski speed is unchanged.
                    _skiFromDart = true;
                    _skiFromDartIn = 0f;
                    _dartSkiUaL = _upperArmL.localRotation;
                    _dartSkiUaR = _upperArmR.localRotation;
                    _dartSkiLaL = _lowerArmL.localRotation;
                    _dartSkiLaR = _lowerArmR.localRotation;
                    _dartSkiUlL = _upperLegL.localRotation;
                    _dartSkiUlR = _upperLegR.localRotation;
                    _dartSkiLlL = _lowerLegL.localRotation;
                    _dartSkiLlR = _lowerLegR.localRotation;
                    _dartSkiSp = _spine.localRotation;
                    _dartSkiHp = _hips.localRotation;
                    _dartSkiHd = _head.localRotation;
                }
                if (fromGrapple && !_skiFromGrapple)
                {
                    // The line eases into the glide. An air crouch into a ski keeps its ease.
                    // A grapple release into a jump keeps its push. A grapple release into a punch keeps its ease.
                    // An air dash into a ski keeps its ease. The gate stays off. Ski speed is unchanged.
                    _skiFromGrapple = true;
                    _skiFromGrappleIn = 0f;
                    _grappleSkiUaL = _upperArmL.localRotation;
                    _grappleSkiUaR = _upperArmR.localRotation;
                    _grappleSkiLaL = _lowerArmL.localRotation;
                    _grappleSkiLaR = _lowerArmR.localRotation;
                    _grappleSkiUlL = _upperLegL.localRotation;
                    _grappleSkiUlR = _upperLegR.localRotation;
                    _grappleSkiLlL = _lowerLegL.localRotation;
                    _grappleSkiLlR = _lowerLegR.localRotation;
                    _grappleSkiSp = _spine.localRotation;
                    _grappleSkiHp = _hips.localRotation;
                    _grappleSkiHd = _head.localRotation;
                }
                if (fromClaim && !_skiFromClaim)
                {
                    // The claim eases into the glide. A grapple release into a ski keeps its ease.
                    // Becoming It into a punch keeps its ease. Becoming It into a jump keeps its push.
                    // A grapple release into a slide keeps its ease. Claim time is unchanged. Ski speed is unchanged.
                    _skiFromClaim = true;
                    _skiFromClaimIn = 0f;
                    _claimSkiUaL = _upperArmL.localRotation;
                    _claimSkiUaR = _upperArmR.localRotation;
                    _claimSkiLaL = _lowerArmL.localRotation;
                    _claimSkiLaR = _lowerArmR.localRotation;
                    _claimSkiUlL = _upperLegL.localRotation;
                    _claimSkiUlR = _upperLegR.localRotation;
                    _claimSkiLlL = _lowerLegL.localRotation;
                    _claimSkiLlR = _lowerLegR.localRotation;
                    _claimSkiSp = _spine.localRotation;
                    _claimSkiHp = _hips.localRotation;
                    _claimSkiHd = _head.localRotation;
                }
                if (fromReady && !_skiFromReady)
                {
                    // The pulse eases into the glide. Becoming It into a ski keeps its ease.
                    // A dash coming off cooldown into a punch keeps its ease.
                    // A dash coming off cooldown into a tag keeps its ease.
                    // A dash coming off cooldown into a jump keeps its push.
                    // Becoming It into a slide keeps its ease. Duration and cooldown are unchanged. Ski speed is unchanged.
                    _skiFromReady = true;
                    _skiFromReadyIn = 0f;
                    _readySkiUaL = _upperArmL.localRotation;
                    _readySkiUaR = _upperArmR.localRotation;
                    _readySkiLaL = _lowerArmL.localRotation;
                    _readySkiLaR = _lowerArmR.localRotation;
                    _readySkiUlL = _upperLegL.localRotation;
                    _readySkiUlR = _upperLegR.localRotation;
                    _readySkiLlL = _lowerLegL.localRotation;
                    _readySkiLlR = _lowerLegR.localRotation;
                    _readySkiSp = _spine.localRotation;
                    _readySkiHp = _hips.localRotation;
                    _readySkiHd = _head.localRotation;
                }
                if (fromWall && !_skiFromWall)
                {
                    // The leave eases into the glide. A climb into a ski keeps its ease.
                    // A wall run into a jump keeps its push. A soft landing into a ski keeps its ease.
                    // A jump into a ski keeps its ease. Ski speed is unchanged. Exit time is unchanged.
                    _skiFromWall = true;
                    _skiFromWallIn = 0f;
                    _wallSkiUaL = _upperArmL.localRotation;
                    _wallSkiUaR = _upperArmR.localRotation;
                    _wallSkiLaL = _lowerArmL.localRotation;
                    _wallSkiLaR = _lowerArmR.localRotation;
                    _wallSkiUlL = _upperLegL.localRotation;
                    _wallSkiUlR = _upperLegR.localRotation;
                    _wallSkiLlL = _lowerLegL.localRotation;
                    _wallSkiLlR = _lowerLegR.localRotation;
                    _wallSkiSp = _spine.localRotation;
                    _wallSkiHp = _hips.localRotation;
                    _wallSkiHd = _head.localRotation;
                }
                if (fromClimb && !_skiFromClimb)
                {
                    // The grab eases into the glide. A wall run into a ski keeps its own ease.
                    // A climb into a jump keeps its push. A climb into a punch keeps its ease.
                    // A jump into a ski keeps its ease. Ski speed is unchanged. Exit time is unchanged.
                    _skiFromClimb = true;
                    _skiFromClimbIn = 0f;
                    _climbSkiUaL = _upperArmL.localRotation;
                    _climbSkiUaR = _upperArmR.localRotation;
                    _climbSkiLaL = _lowerArmL.localRotation;
                    _climbSkiLaR = _lowerArmR.localRotation;
                    _climbSkiUlL = _upperLegL.localRotation;
                    _climbSkiUlR = _upperLegR.localRotation;
                    _climbSkiLlL = _lowerLegL.localRotation;
                    _climbSkiLlR = _lowerLegR.localRotation;
                    _climbSkiSp = _spine.localRotation;
                    _climbSkiHp = _hips.localRotation;
                    _climbSkiHd = _head.localRotation;
                }
            }
            else if (!skiing)
            {
                _skiFromWalk = false;
                _skiFromSprint = false;
                _skiFromCrouch = false;
                _stillSkiSnap = false;
                _skiFromCrouchWalk = false;
                _crouchWalkSkiSnap = false;
                _slideToSki = false;
                _skiFromSlide = false;
                _skiFromJump = false;
                _skiFromJumpLand = false;
                _skiFromPunch = false;
                _skiFromTag = false;
                _skiFromMiss = false;
                _skiFromSoft = false;
                _skiFromHard = false;
                _skiFromDart = false;
                _skiFromGrapple = false;
                _skiFromClaim = false;
                _skiFromReady = false;
                _skiFromClimb = false;
                _skiFromWall = false;
            }
            bool crouchIdleExit = !_dropSlide && !sliding && !crouch && speed <= 0.35f && !crouchStandSprint && !_skiFromCrouch;
            // A crouch walk stands into the stride. The feet step while the hips are still rising.
            bool crouchWalkExit = !_dropSlide && !sliding && !crouch && speed > 0.35f && !crouchStandSprint && !_skiFromCrouchWalk;
            // A crouch walk into a sprint opens the step as the hips rise. Speed is unchanged.
            bool crouchSprintExit = crouchWalkExit && st == MoveState.Sprint;
            float footDrop = (slideExit || crouchIdleExit || crouchWalkExit || crouchStandSprint) ? _dropVis * _dropVis : _dropVis;
            float hipDrop = (slideExit || crouchIdleExit || crouchWalkExit || crouchStandSprint) ? Mathf.SmoothStep(0f, 1f, _dropVis) : _dropVis;
            _skiBlend = Mathf.MoveTowards(_skiBlend, skiing ? 1f : 0f, dt / 0.22f);
            if (_skiFromWalk && skiing)
                _skiFromWalkIn = Mathf.MoveTowards(_skiFromWalkIn, 1f, dt / 0.04f);
            if (_skiFromSprint && skiing)
                _skiFromSprintIn = Mathf.MoveTowards(_skiFromSprintIn, 1f, dt / 0.04f);
            if (_skiFromSlide && skiing)
                _skiFromSlideIn = Mathf.MoveTowards(_skiFromSlideIn, 1f, dt / 0.04f);
            if (_skiFromCrouchWalk && skiing)
                _skiFromCrouchWalkIn = Mathf.MoveTowards(_skiFromCrouchWalkIn, 1f, dt / 0.04f);
            if (_skiFromCrouch && skiing)
                _skiFromCrouchIn = Mathf.MoveTowards(_skiFromCrouchIn, 1f, dt / 0.04f);
            if (_skiFromPunch && skiing)
                _skiFromPunchIn = Mathf.MoveTowards(_skiFromPunchIn, 1f, dt / 0.04f);
            if (_skiFromTag && skiing)
                _skiFromTagIn = Mathf.MoveTowards(_skiFromTagIn, 1f, dt / 0.04f);
            if (_skiFromMiss && skiing)
                _skiFromMissIn = Mathf.MoveTowards(_skiFromMissIn, 1f, dt / 0.04f);
            if (_skiFromSoft && skiing)
                _skiFromSoftIn = Mathf.MoveTowards(_skiFromSoftIn, 1f, dt / 0.04f);
            if (_skiFromHard && skiing)
                _skiFromHardIn = Mathf.MoveTowards(_skiFromHardIn, 1f, dt / 0.04f);
            if (_skiFromDart && skiing)
                _skiFromDartIn = Mathf.MoveTowards(_skiFromDartIn, 1f, dt / 0.04f);
            if (_skiFromGrapple && skiing)
                _skiFromGrappleIn = Mathf.MoveTowards(_skiFromGrappleIn, 1f, dt / 0.04f);
            if (_skiFromClaim && skiing)
                _skiFromClaimIn = Mathf.MoveTowards(_skiFromClaimIn, 1f, dt / 0.04f);
            if (_skiFromReady && skiing)
                _skiFromReadyIn = Mathf.MoveTowards(_skiFromReadyIn, 1f, dt / 0.04f);
            if (_skiFromClimb && skiing)
                _skiFromClimbIn = Mathf.MoveTowards(_skiFromClimbIn, 1f, dt / 0.04f);
            if (_skiFromWall && skiing)
                _skiFromWallIn = Mathf.MoveTowards(_skiFromWallIn, 1f, dt / 0.04f);
            if (_slideToSki && _skiBlend >= 0.98f)
                _slideToSki = false;
            if (_skiFromJump && _skiBlend >= 0.98f)
            {
                _skiFromJump = false;
                _skiFromJumpLand = false;
            }
            // Feet stay in the short glide while the hips are still pitched, then the run opens under them.
            // A ski into a walk has its own ease. A ski into a run has its own ease.
            // Ski speed is unchanged. Jet stays off.
            // A still crouch takes the glide into the guard. A crouch walk takes it into the low stride.
            float footSki = _skiBlend * (2f - _skiBlend);
            bool skiCrouchExit = !skiing && grounded && crouch && speed <= 0.35f && _skiBlend > 0.02f && !_dropSlide;
            bool skiCrouchWalk = !skiing && grounded && crouch && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint && _skiBlend > 0.02f && !_dropSlide;
            float walkSki = !skiing && grounded && !skiCrouchExit && !skiCrouchWalk
                ? Mathf.Clamp01(speed / 5.5f) * (1f - Mathf.InverseLerp(5.5f, 11.5f, speed))
                : 0f;
            bool sprintExit = !skiing && grounded && speed > 5.5f;
            float legSki = walkSki > 0.02f || sprintExit || _skiFromWalk || _skiFromSprint ? footSki * footSki : footSki;
            bool punching = _punch != null && _punch.IsPunching;
            bool lunging = _motor != null && _motor.IsLunging;
            var phase = _punch != null ? _punch.Phase : PunchPhase.Idle;
            if (!skiCrouchWalk)
                _skiWalkHeld = false;
            bool skiIntoCrouchWalk = skiCrouchWalk && !_skiWalkHeld && !_skiFromCrouchWalk
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (skiIntoCrouchWalk)
            {
                // The glide eases into the low stride. A ski into a still crouch keeps its ease.
                // A crouch walk into a ski has its own ease. Ski speed is unchanged.
                _crouchWalkFromSki = true;
                _crouchWalkFromSkiIn = 0f;
                _skiWalkHeld = true;
                _skiWalkUaL = _upperArmL.localRotation;
                _skiWalkUaR = _upperArmR.localRotation;
                _skiWalkLaL = _lowerArmL.localRotation;
                _skiWalkLaR = _lowerArmR.localRotation;
                _skiWalkUlL = _upperLegL.localRotation;
                _skiWalkUlR = _upperLegR.localRotation;
                _skiWalkLlL = _lowerLegL.localRotation;
                _skiWalkLlR = _lowerLegR.localRotation;
                _skiWalkSp = _spine.localRotation;
                _skiWalkHp = _hips.localRotation;
                _skiWalkHd = _head.localRotation;
            }
            if (!skiCrouchWalk)
                _crouchWalkFromSki = false;
            else if (_crouchWalkFromSki)
                _crouchWalkFromSkiIn = Mathf.MoveTowards(_crouchWalkFromSkiIn, 1f, dt / 0.04f);
            bool skiToWalk = !skiing && grounded && !crouch && !jet && speed > 0.35f && speed <= 5.5f
                && st != MoveState.Sprint && !_dropSlide && _skiBlend > 0.02f;
            bool skiIntoWalk = skiToWalk && _skiBlend > 0.2f && !_walkFromSkiHeld
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (skiIntoWalk)
            {
                // The glide eases into the walk. A ski into a run has its own ease.
                // A walk into a ski keeps its ease. Ski speed is unchanged.
                _walkFromSki = true;
                _walkFromSkiIn = 0f;
                _walkFromSkiHeld = true;
                _glideWalkUaL = _upperArmL.localRotation;
                _glideWalkUaR = _upperArmR.localRotation;
                _glideWalkLaL = _lowerArmL.localRotation;
                _glideWalkLaR = _lowerArmR.localRotation;
                _glideWalkUlL = _upperLegL.localRotation;
                _glideWalkUlR = _upperLegR.localRotation;
                _glideWalkLlL = _lowerLegL.localRotation;
                _glideWalkLlR = _lowerLegR.localRotation;
                _glideWalkSp = _spine.localRotation;
                _glideWalkHp = _hips.localRotation;
                _glideWalkHd = _head.localRotation;
            }
            if (!skiToWalk)
            {
                _walkFromSki = false;
                _walkFromSkiHeld = false;
            }
            else if (_walkFromSki)
                _walkFromSkiIn = Mathf.MoveTowards(_walkFromSkiIn, 1f, dt / 0.04f);
            bool skiToRun = !skiing && grounded && !crouch && !jet && !_dropSlide && _skiBlend > 0.02f
                && (speed > 5.5f || st == MoveState.Sprint);
            bool skiIntoRun = skiToRun && _skiBlend > 0.2f && !_runFromSkiHeld && !_walkFromSki
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (skiIntoRun)
            {
                // The glide eases into the run. A ski into a walk has its own ease.
                // A run into a ski keeps its ease. Ski speed is unchanged.
                _runFromSki = true;
                _runFromSkiIn = 0f;
                _runFromSkiHeld = true;
                _glideRunUaL = _upperArmL.localRotation;
                _glideRunUaR = _upperArmR.localRotation;
                _glideRunLaL = _lowerArmL.localRotation;
                _glideRunLaR = _lowerArmR.localRotation;
                _glideRunUlL = _upperLegL.localRotation;
                _glideRunUlR = _upperLegR.localRotation;
                _glideRunLlL = _lowerLegL.localRotation;
                _glideRunLlR = _lowerLegR.localRotation;
                _glideRunSp = _spine.localRotation;
                _glideRunHp = _hips.localRotation;
                _glideRunHd = _head.localRotation;
            }
            if (!skiToRun)
            {
                _runFromSki = false;
                _runFromSkiHeld = false;
            }
            else if (_runFromSki)
                _runFromSkiIn = Mathf.MoveTowards(_runFromSkiIn, 1f, dt / 0.04f);
            bool skiToIdle = !skiing && grounded && !crouch && !jet && !_dropSlide && _skiBlend > 0.02f
                && speed <= 0.35f && st != MoveState.Sprint;
            bool skiIntoIdle = skiToIdle && _skiBlend > 0.2f && !_idleFromSkiHeld && !_walkFromSki && !_runFromSki
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (skiIntoIdle)
            {
                // The glide eases into the idle. A ski into a walk has its own ease.
                // A ski into a run has its own ease. A ski into a still crouch keeps its ease.
                // Ski speed is unchanged.
                _idleFromSki = true;
                _idleFromSkiIn = 0f;
                _idleFromSkiHeld = true;
                _glideIdleUaL = _upperArmL.localRotation;
                _glideIdleUaR = _upperArmR.localRotation;
                _glideIdleLaL = _lowerArmL.localRotation;
                _glideIdleLaR = _lowerArmR.localRotation;
                _glideIdleUlL = _upperLegL.localRotation;
                _glideIdleUlR = _upperLegR.localRotation;
                _glideIdleLlL = _lowerLegL.localRotation;
                _glideIdleLlR = _lowerLegR.localRotation;
                _glideIdleSp = _spine.localRotation;
                _glideIdleHp = _hips.localRotation;
                _glideIdleHd = _head.localRotation;
            }
            if (!skiToIdle)
            {
                _idleFromSki = false;
                _idleFromSkiHeld = false;
            }
            else if (_idleFromSki)
                _idleFromSkiIn = Mathf.MoveTowards(_idleFromSkiIn, 1f, dt / 0.04f);
            // A walk that drops into a crouch walk. The drop timer stays dt/0.16.
            bool walkCrouch = crouch && grounded && !sliding && !jet && speed > 0.35f && speed <= 5.5f
                && st != MoveState.Sprint && !_dropSlide;
            if (!walkCrouch)
                _walkCrouchHeld = false;
            bool walkIntoCrouchWalk = walkCrouch && standWalk && !_walkCrouchHeld
                && !_crouchWalkFromSki && !_crouchWalkFromSlide && !_crouchWalkFromSprint && !_crouchWalkFromStill
                && !_stillFromWalk && !_stillFromSprint && !_stillFromSki && !_stillFromSlide
                && !_crouchFromJump && !_crouchFromDash && !dashPoseNow && !wallRun && !climb
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (walkIntoCrouchWalk)
            {
                // The walk eases into the low stride. A slide into a crouch walk keeps its ease.
                // A walk into a still crouch keeps its ease. The drop timer stays dt/0.16.
                _crouchWalkFromWalk = true;
                _crouchWalkFromWalkIn = 0f;
                _walkCrouchHeld = true;
                _walkCrouchUaL = _upperArmL.localRotation;
                _walkCrouchUaR = _upperArmR.localRotation;
                _walkCrouchLaL = _lowerArmL.localRotation;
                _walkCrouchLaR = _lowerArmR.localRotation;
                _walkCrouchUlL = _upperLegL.localRotation;
                _walkCrouchUlR = _upperLegR.localRotation;
                _walkCrouchLlL = _lowerLegL.localRotation;
                _walkCrouchLlR = _lowerLegR.localRotation;
                _walkCrouchSp = _spine.localRotation;
                _walkCrouchHp = _hips.localRotation;
                _walkCrouchHd = _head.localRotation;
            }
            if (!walkCrouch)
                _crouchWalkFromWalk = false;
            else if (_crouchWalkFromWalk)
                _crouchWalkFromWalkIn = Mathf.MoveTowards(_crouchWalkFromWalkIn, 1f, dt / 0.04f);
            // A run that drops into a crouch walk. Speed can still be above the walk band.
            // The drop timer stays dt/0.16. A walk into a crouch walk keeps its ease.
            bool sprintCrouch = crouch && grounded && !sliding && !jet && speed > 0.35f && !_dropSlide;
            if (!sprintCrouch)
                _sprintCrouchHeld = false;
            bool sprintIntoCrouchWalk = sprintCrouch && standRun && !standWalk && !_sprintCrouchHeld
                && !_crouchWalkFromWalk && !_crouchWalkFromSki && !_crouchWalkFromSlide && !_crouchWalkFromStill
                && !_stillFromWalk && !_stillFromSprint && !_stillFromSki && !_stillFromSlide
                && !_crouchFromJump && !_crouchFromDash && !dashPoseNow && !wallRun && !climb
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (sprintIntoCrouchWalk)
            {
                // The run eases into the low stride. A walk into a crouch walk keeps its ease.
                // A run into a still crouch keeps its ease. The drop timer stays dt/0.16.
                _crouchWalkFromSprint = true;
                _crouchWalkFromSprintIn = 0f;
                _sprintCrouchHeld = true;
                _sprintCrouchUaL = _upperArmL.localRotation;
                _sprintCrouchUaR = _upperArmR.localRotation;
                _sprintCrouchLaL = _lowerArmL.localRotation;
                _sprintCrouchLaR = _lowerArmR.localRotation;
                _sprintCrouchUlL = _upperLegL.localRotation;
                _sprintCrouchUlR = _upperLegR.localRotation;
                _sprintCrouchLlL = _lowerLegL.localRotation;
                _sprintCrouchLlR = _lowerLegR.localRotation;
                _sprintCrouchSp = _spine.localRotation;
                _sprintCrouchHp = _hips.localRotation;
                _sprintCrouchHd = _head.localRotation;
            }
            if (!sprintCrouch)
                _crouchWalkFromSprint = false;
            else if (_crouchWalkFromSprint)
                _crouchWalkFromSprintIn = Mathf.MoveTowards(_crouchWalkFromSprintIn, 1f, dt / 0.04f);
            if (!walkCrouch)
            {
                _crouchWalkFromStill = false;
                _stillStrideHeld = false;
            }
            else if (_crouchWalkFromStill)
                _crouchWalkFromStillIn = Mathf.MoveTowards(_crouchWalkFromStillIn, 1f, dt / 0.04f);
            // A crouch walk that stands into the walk. The drop timer stays dt/0.16.
            // A crouch walk into a run has its own ease. A still crouch into a walk has its own ease.
            bool walkStand = crouchWalkExit && !crouchSprintExit && speed <= 5.5f && st != MoveState.Sprint;
            bool walkUpFromCrouch = walkStand && _crouchWalkArmed && _dropVis > 0.2f && !_walkUpHeld
                && !dashPoseNow && !wallRun && !climb && !jet && !sliding
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (walkUpFromCrouch)
            {
                // The low stride eases into the walk, then the walk holds.
                // A crouch walk into a run has its own ease. A still crouch into a walk has its own ease.
                // The drop timer stays dt/0.16.
                _walkFromCrouchWalk = true;
                _walkFromCrouchWalkIn = 0f;
                _walkUpHeld = true;
                _walkUpUaL = _upperArmL.localRotation;
                _walkUpUaR = _upperArmR.localRotation;
                _walkUpLaL = _lowerArmL.localRotation;
                _walkUpLaR = _lowerArmR.localRotation;
                _walkUpUlL = _upperLegL.localRotation;
                _walkUpUlR = _upperLegR.localRotation;
                _walkUpLlL = _lowerLegL.localRotation;
                _walkUpLlR = _lowerLegR.localRotation;
                _walkUpSp = _spine.localRotation;
                _walkUpHp = _hips.localRotation;
                _walkUpHd = _head.localRotation;
            }
            if (!walkStand || _dropVis <= 0.02f)
            {
                _walkFromCrouchWalk = false;
                _walkUpHeld = false;
            }
            else if (_walkFromCrouchWalk)
                _walkFromCrouchWalkIn = Mathf.MoveTowards(_walkFromCrouchWalkIn, 1f, dt / 0.04f);
            // A crouch walk that stands into a run. The drop timer stays dt/0.16.
            // A crouch walk into a walk keeps its ease. A still crouch into a run has its own ease.
            bool runStand = crouchSprintExit && !crouchStandSprint;
            bool runUpFromCrouch = runStand && crouchWalkWasArmed && _dropVis > 0.2f && !_runUpHeld
                && !_walkFromCrouchWalk
                && !dashPoseNow && !wallRun && !climb && !jet && !sliding
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (runUpFromCrouch)
            {
                // The low stride eases into the run, then the run holds.
                // A crouch walk into a walk keeps its ease. A still crouch into a run has its own ease.
                // The drop timer stays dt/0.16.
                _runFromCrouchWalk = true;
                _runFromCrouchWalkIn = 0f;
                _runUpHeld = true;
                _runUpUaL = _upperArmL.localRotation;
                _runUpUaR = _upperArmR.localRotation;
                _runUpLaL = _lowerArmL.localRotation;
                _runUpLaR = _lowerArmR.localRotation;
                _runUpUlL = _upperLegL.localRotation;
                _runUpUlR = _upperLegR.localRotation;
                _runUpLlL = _lowerLegL.localRotation;
                _runUpLlR = _lowerLegR.localRotation;
                _runUpSp = _spine.localRotation;
                _runUpHp = _hips.localRotation;
                _runUpHd = _head.localRotation;
            }
            if (!runStand || _dropVis <= 0.02f)
            {
                _runFromCrouchWalk = false;
                _runUpHeld = false;
            }
            else if (_runFromCrouchWalk)
                _runFromCrouchWalkIn = Mathf.MoveTowards(_runFromCrouchWalkIn, 1f, dt / 0.04f);
            // A still crouch that stands into the walk. The drop timer stays dt/0.16.
            // A crouch walk into a walk keeps its ease. A still crouch into a run has its own ease.
            bool stillWalk = walkStand && _crouchFromStand && !crouchWalkWasArmed;
            bool walkUpFromStill = stillWalk && _dropVis > 0.2f && !_stillWalkHeld
                && !_walkFromCrouchWalk && !_runFromCrouchWalk
                && !dashPoseNow && !wallRun && !climb && !jet && !sliding
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (walkUpFromStill)
            {
                // The planted guard eases into the walk, then the walk holds.
                // A crouch walk into a walk keeps its ease. A still crouch into a run has its own ease.
                // The drop timer stays dt/0.16.
                _walkFromStill = true;
                _walkFromStillIn = 0f;
                _stillWalkHeld = true;
                _stillWalkUaL = _upperArmL.localRotation;
                _stillWalkUaR = _upperArmR.localRotation;
                _stillWalkLaL = _lowerArmL.localRotation;
                _stillWalkLaR = _lowerArmR.localRotation;
                _stillWalkUlL = _upperLegL.localRotation;
                _stillWalkUlR = _upperLegR.localRotation;
                _stillWalkLlL = _lowerLegL.localRotation;
                _stillWalkLlR = _lowerLegR.localRotation;
                _stillWalkSp = _spine.localRotation;
                _stillWalkHp = _hips.localRotation;
                _stillWalkHd = _head.localRotation;
            }
            if (!stillWalk || _dropVis <= 0.02f)
            {
                _walkFromStill = false;
                _stillWalkHeld = false;
            }
            else if (_walkFromStill)
                _walkFromStillIn = Mathf.MoveTowards(_walkFromStillIn, 1f, dt / 0.04f);
            // A still crouch that stands into a run. The drop timer stays dt/0.16.
            // A still crouch into a walk keeps its ease. A crouch walk into a run keeps its ease.
            bool stillRun = crouchStandSprint && !crouchWalkWasArmed;
            bool runUpFromStill = stillRun && _dropVis > 0.2f && !_stillRunHeld
                && !_walkFromStill && !_walkFromCrouchWalk && !_runFromCrouchWalk
                && !dashPoseNow && !wallRun && !climb && !jet && !sliding
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (runUpFromStill)
            {
                // The planted guard eases into the run, then the run holds.
                // A still crouch into a walk keeps its ease. A crouch walk into a run keeps its ease.
                // The drop timer stays dt/0.16.
                _runFromStill = true;
                _runFromStillIn = 0f;
                _stillRunHeld = true;
                _stillRunUaL = _upperArmL.localRotation;
                _stillRunUaR = _upperArmR.localRotation;
                _stillRunLaL = _lowerArmL.localRotation;
                _stillRunLaR = _lowerArmR.localRotation;
                _stillRunUlL = _upperLegL.localRotation;
                _stillRunUlR = _upperLegR.localRotation;
                _stillRunLlL = _lowerLegL.localRotation;
                _stillRunLlR = _lowerLegR.localRotation;
                _stillRunSp = _spine.localRotation;
                _stillRunHp = _hips.localRotation;
                _stillRunHd = _head.localRotation;
            }
            if (!stillRun || _dropVis <= 0.02f)
            {
                _runFromStill = false;
                _stillRunHeld = false;
            }
            else if (_runFromStill)
                _runFromStillIn = Mathf.MoveTowards(_runFromStillIn, 1f, dt / 0.04f);

            if (grounded && !_wasGrounded)
            {
                // Soft landings = mild squash; hard (near landStunSpeed) = punchier. Clamped.
                float impact = _motor != null ? _motor.LastLandImpactSpeed : 10f;
                float soft = 5f;
                float hard = 24f;
                if (_motor != null && _motor.cfg != null)
                    hard = Mathf.Max(soft + 1f, _motor.cfg.landStunSpeed);
                float t = Mathf.Clamp01(Mathf.InverseLerp(soft, hard, impact));
                // Ease-in so mid falls stay readable but terminal velocity punches.
                // Slightly stronger mid-band so a park hop-off reads without waiting for stun speed.
                _landSquash = Mathf.Clamp(Mathf.Lerp(0.55f, 1.35f, t * t), 0.55f, 1.35f);
                // Brief absorb, then the pose eases into the run instead of popping off.
                _landHold = Mathf.Lerp(0.05f, 0.11f, t);
                _landHard = t;
            }
            if (!grounded && _motor != null && _motor.Velocity.y > 1.5f && (_wasGrounded || _prevVy <= 1.5f))
                _diveFromJump = true;
            if (grounded && _diveFromJump)
                _landedFromJump = true;
            if (grounded)
                _diveFromJump = false;
            if (!grounded || _landSquash <= 0.02f)
                _landedFromJump = false;
            if (!grounded && _wasGrounded && _motor != null && _motor.Velocity.y > 1.5f)
            {
                // Push off the foot that was down. Jump height is unchanged.
                _pushLeft = Mathf.Cos(_cycle) < 0f;
                _pushOff = 1f;
                // Arms leave the stride into the air pose. A ledge step does not restart this.
                // A still crouch eases the guard into that push. A crouch walk eases the low stride into it.
                // A ski eases the glide into it. A slide eases the wedge into it.
                // slideBoost stays 0. Jump height is unchanged.
                _airArmIn = 0f;
                _jumpFromStill = _crouchFromStand && speed <= 0.35f;
                if (_jumpFromStill && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The guard eases into the jump. A crouch walk into a jump keeps its ease.
                    // A walk into an air dash keeps its ease. A jump into a still crouch keeps its ease.
                    // Jump height is unchanged.
                    _jumpFromStillIn = 0f;
                    _stillJumpUaL = _upperArmL.localRotation;
                    _stillJumpUaR = _upperArmR.localRotation;
                    _stillJumpLaL = _lowerArmL.localRotation;
                    _stillJumpLaR = _lowerArmR.localRotation;
                    _stillJumpUlL = _upperLegL.localRotation;
                    _stillJumpUlR = _upperLegR.localRotation;
                    _stillJumpLlL = _lowerLegL.localRotation;
                    _stillJumpLlR = _lowerLegR.localRotation;
                    _stillJumpSp = _spine.localRotation;
                    _stillJumpHp = _hips.localRotation;
                    _stillJumpHd = _head.localRotation;
                }
                else if (_jumpFromStill)
                    _jumpFromStillIn = 1f;
                _jumpFromCrouchWalk = !_jumpFromStill && _crouchWalkArmed && speed > 0.35f && speed <= 5.5f;
                if (_jumpFromCrouchWalk && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The low stride eases into the jump. A still crouch into a jump keeps its push.
                    // A walk into a ski keeps its ease. A crouch walk into an air dash keeps its ease.
                    // Jump height is unchanged.
                    _jumpFromCrouchWalkIn = 0f;
                    _crouchWalkJumpUaL = _upperArmL.localRotation;
                    _crouchWalkJumpUaR = _upperArmR.localRotation;
                    _crouchWalkJumpLaL = _lowerArmL.localRotation;
                    _crouchWalkJumpLaR = _lowerArmR.localRotation;
                    _crouchWalkJumpUlL = _upperLegL.localRotation;
                    _crouchWalkJumpUlR = _upperLegR.localRotation;
                    _crouchWalkJumpLlL = _lowerLegL.localRotation;
                    _crouchWalkJumpLlR = _lowerLegR.localRotation;
                    _crouchWalkJumpSp = _spine.localRotation;
                    _crouchWalkJumpHp = _hips.localRotation;
                    _crouchWalkJumpHd = _head.localRotation;
                }
                else if (_jumpFromCrouchWalk)
                    _jumpFromCrouchWalkIn = 1f;
                _jumpFromSki = !_jumpFromStill && !_jumpFromCrouchWalk && !_dropSlide && _skiBlend > 0.2f;
                _jumpFromSlide = !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && _dropSlide && _dropVis > 0.2f;
                if (_jumpFromSlide && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The wedge eases into the jump. A ski into a jump keeps its ease.
                    // slideBoost stays 0. Jump height is unchanged.
                    _jumpFromSlideIn = 0f;
                    _slideJumpUaL = _upperArmL.localRotation;
                    _slideJumpUaR = _upperArmR.localRotation;
                    _slideJumpLaL = _lowerArmL.localRotation;
                    _slideJumpLaR = _lowerArmR.localRotation;
                    _slideJumpUlL = _upperLegL.localRotation;
                    _slideJumpUlR = _upperLegR.localRotation;
                    _slideJumpLlL = _lowerLegL.localRotation;
                    _slideJumpLlR = _lowerLegR.localRotation;
                    _slideJumpSp = _spine.localRotation;
                    _slideJumpHp = _hips.localRotation;
                    _slideJumpHd = _head.localRotation;
                }
                else if (_jumpFromSlide)
                    _jumpFromSlideIn = 1f;
                bool dashPose = _airDashArms || _motor.IsAirDashing;
                _jumpFromDash = dashPose && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide;
                _jumpFromWall = _exitFromWall && _wallExit > 0.2f && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide && !_jumpFromDash && !_jumpFromClimb;
                // A soft landing eases the absorb into this push. A hard landing keeps its jump.
                // Land time is unchanged when you stay down. Jump height is unchanged.
                _jumpFromSoftLand = !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch
                    && _landHard < 0.4f && _landSquash > 0.08f;
                // A hard landing eases the deeper absorb into this push. A soft landing keeps its jump.
                // Land time is unchanged when you stay down. Jump height is unchanged.
                _jumpFromHardLand = !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch && !_jumpFromSoftLand
                    && _landHard >= 0.4f && _landSquash > 0.08f;
                // A punch miss eases the whiff into this push. A crouch miss keeps its jump.
                // A soft landing and a hard landing keep their jump. Jump height is unchanged.
                _jumpFromMiss = phase == PunchPhase.MissRecover
                    && !_crouchFromStand && !_crouchWalkArmed
                    && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch
                    && !_jumpFromSoftLand && !_jumpFromHardLand;
                if (_jumpFromMiss && _punch != null)
                {
                    float missProg = _punch.PhaseProgress;
                    _missR = Mathf.Lerp(0.62f, 0.02f, missProg * missProg * missProg);
                }
                // A tag eases the connect into this push. A crouch tag keeps its jump.
                // A punch miss keeps its jump. Jump height is unchanged.
                _jumpFromTag = phase == PunchPhase.HitRecover
                    && !_crouchFromStand && !_crouchWalkArmed
                    && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch
                    && !_jumpFromSoftLand && !_jumpFromHardLand && !_jumpFromMiss;
                if (_jumpFromTag && _punch != null)
                {
                    float tagProg = _punch.PhaseProgress;
                    _tagSettle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, tagProg));
                    _tagFromClaim = _itClaim > 0.04f;
                }
                // The claim eases into this push. A crouch claim keeps its jump.
                // A tag keeps its jump. Jump height is unchanged.
                _jumpFromClaim = _itClaim > 0.2f
                    && phase != PunchPhase.HitRecover
                    && !_crouchFromStand && !_crouchWalkArmed
                    && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch
                    && !_jumpFromSoftLand && !_jumpFromHardLand && !_jumpFromMiss && !_jumpFromTag;
                // A grapple release eases the line into this push. A crouch release keeps its jump.
                // The gate stays off. Jump height is unchanged.
                bool grappleRelease = _grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f;
                _jumpFromGrapple = grappleRelease
                    && !_crouchFromStand && !_crouchWalkArmed
                    && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch
                    && !_jumpFromSoftLand && !_jumpFromHardLand && !_jumpFromMiss && !_jumpFromTag && !_jumpFromClaim;
                // A dash ready eases the pulse into this push. A crouch ready keeps its jump.
                // A grapple release keeps its jump. Duration and cooldown are unchanged.
                bool readyTell = _dashReady > 0.2f && !punching && _grapplePose <= 0.04f
                    && _upperArmL != null && _spine != null && _upperLegL != null;
                _jumpFromReady = readyTell
                    && !_crouchFromStand && !_crouchWalkArmed
                    && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch
                    && !_jumpFromSoftLand && !_jumpFromHardLand && !_jumpFromMiss && !_jumpFromTag
                    && !_jumpFromClaim && !_jumpFromGrapple;
                if (_jumpFromReady)
                {
                    _readyUaL = _upperArmL.localRotation;
                    _readyUaR = _upperArmR.localRotation;
                    _readyLaL = _lowerArmL.localRotation;
                    _readyLaR = _lowerArmR.localRotation;
                    _readySp = _spine.localRotation;
                    _readyHp = _hips.localRotation;
                    _readyHd = _head.localRotation;
                    _readyUlL = _upperLegL.localRotation;
                    _readyUlR = _upperLegR.localRotation;
                    _readyLlL = _lowerLegL.localRotation;
                    _readyLlR = _lowerLegR.localRotation;
                    _dashReady = 0f;
                }
                // A punch eases into this push. A miss, a tag, and the claim keep their jump.
                // A jump into a punch keeps its windup. Windup time is unchanged. Jump height is unchanged.
                bool punchIntoJump = punching && (phase == PunchPhase.Active || (phase == PunchPhase.Windup && _punchWindWas));
                _jumpFromPunch = punchIntoJump
                    && !_crouchFromStand && !_crouchWalkArmed
                    && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch
                    && !_jumpFromSoftLand && !_jumpFromHardLand && !_jumpFromMiss && !_jumpFromTag
                    && !_jumpFromClaim && !_jumpFromGrapple && !_jumpFromReady
                    && !_punchFromJump;
                if (_jumpFromPunch && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    _jumpFromPunchIn = 0f;
                    _punchJumpUaL = _upperArmL.localRotation;
                    _punchJumpUaR = _upperArmR.localRotation;
                    _punchJumpLaL = _lowerArmL.localRotation;
                    _punchJumpLaR = _lowerArmR.localRotation;
                    _punchJumpUlL = _upperLegL.localRotation;
                    _punchJumpUlR = _upperLegR.localRotation;
                    _punchJumpLlL = _lowerLegL.localRotation;
                    _punchJumpLlR = _lowerLegR.localRotation;
                    _punchJumpSp = _spine.localRotation;
                    _punchJumpHp = _hips.localRotation;
                    _punchJumpHd = _head.localRotation;
                }
                else if (_jumpFromPunch)
                    _jumpFromPunchIn = 1f;
                _jumpFromWalk = !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                    && !_jumpFromDash && !_jumpFromWall && !_jumpFromClimb && !_jumpFromAirCrouch
                    && !_jumpFromSoftLand && !_jumpFromHardLand && !_jumpFromMiss && !_jumpFromTag
                    && !_jumpFromClaim && !_jumpFromGrapple && !_jumpFromReady && !_jumpFromPunch
                    && !_crouchFromStand && !_crouchWalkArmed && !_airDashPoseWas && !jet
                    && _landSquash <= 0.08f && _skiBlend <= 0.2f && !_dropSlide
                    && speed > 0.35f && speed <= 5.5f
                    && phase != PunchPhase.Windup && phase != PunchPhase.Active
                    && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover;
                if (_jumpFromWalk && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    // The walk eases into the jump. A crouch walk into a jump keeps its ease.
                    // A still crouch into a jump keeps its push. A walk into a ski keeps its ease.
                    // Jump height is unchanged.
                    _jumpFromWalkIn = 0f;
                    _walkJumpUaL = _upperArmL.localRotation;
                    _walkJumpUaR = _upperArmR.localRotation;
                    _walkJumpLaL = _lowerArmL.localRotation;
                    _walkJumpLaR = _lowerArmR.localRotation;
                    _walkJumpUlL = _upperLegL.localRotation;
                    _walkJumpUlR = _upperLegR.localRotation;
                    _walkJumpLlL = _lowerLegL.localRotation;
                    _walkJumpLlR = _lowerLegR.localRotation;
                    _walkJumpSp = _spine.localRotation;
                    _walkJumpHp = _hips.localRotation;
                    _walkJumpHd = _head.localRotation;
                }
                else if (_jumpFromWalk)
                    _jumpFromWalkIn = 1f;
            }
            else if (!grounded && _motor != null && _motor.Velocity.y > 1.5f && _prevVy <= 1.5f
                && (_airDashArms || _motor.IsAirDashing)
                && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide)
            {
                // A jump during the burst eases that burst into the push. Duration and cooldown are unchanged.
                // Jump height is unchanged.
                _pushLeft = Mathf.Cos(_cycle) < 0f;
                _pushOff = 1f;
                _airArmIn = 0f;
                _jumpFromDash = true;
            }
            else if (!grounded && _motor != null && _motor.Velocity.y > 1.5f && _prevVy <= 1.5f
                && _exitFromWall && _wallExit > 0.2f
                && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide && !_jumpFromDash && !_jumpFromClimb)
            {
                // A wall run eases into the push. A climb keeps its own leave.
                // Exit time is unchanged. Jump height is unchanged.
                _pushLeft = !_exitLeadLeft;
                _pushOff = 1f;
                _airArmIn = 0f;
                _jumpFromWall = true;
            }
            else if (!grounded && _motor != null && _motor.Velocity.y > 1.5f && _prevVy <= 1.5f
                && _diveVis > 0.2f && _input != null && _input.CrouchHeld && !jet
                && !_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromSki && !_jumpFromSlide
                && !_jumpFromDash && !_jumpFromClimb && !_jumpFromWall)
            {
                // An air crouch eases the dart into the push. A moving fall uses the low stride.
                // Fall speed stays doubled. Jump height is unchanged.
                _pushLeft = Mathf.Cos(_cycle) < 0f;
                _pushOff = 1f;
                _airArmIn = 0f;
                _jumpFromAirCrouch = true;
                float runNow = Mathf.InverseLerp(5.5f, 11.5f, speed);
                _jumpFromAirCrouchStride = speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint && runNow <= 0.4f
                    && !(_airDashArms || _armRecover > 0f);
            }
            else if (!grounded)
                _pushOff = Mathf.MoveTowards(_pushOff, 0f, dt / 0.12f);
            else
                _pushOff = 0f;
            _prevVy = _motor != null ? _motor.Velocity.y : 0f;
            if (grounded || _pushOff <= 0.02f)
            {
                _jumpFromStill = false;
                _jumpFromCrouchWalk = false;
                _jumpFromWalk = false;
                _jumpFromSki = false;
                _jumpFromSlide = false;
                _jumpFromDash = false;
                _jumpFromClimb = false;
                _jumpFromWall = false;
                _jumpFromAirCrouch = false;
                _jumpFromAirCrouchStride = false;
                _jumpFromSoftLand = false;
                _jumpFromHardLand = false;
                _jumpFromMiss = false;
                _jumpFromTag = false;
                _jumpFromClaim = false;
                _jumpFromGrapple = false;
                _jumpFromReady = false;
                _jumpFromPunch = false;
            }
            if (_jumpFromStill && _pushOff > 0.02f)
                _jumpFromStillIn = Mathf.MoveTowards(_jumpFromStillIn, 1f, dt / 0.04f);
            if (_jumpFromCrouchWalk && _pushOff > 0.02f)
                _jumpFromCrouchWalkIn = Mathf.MoveTowards(_jumpFromCrouchWalkIn, 1f, dt / 0.04f);
            if (_jumpFromWalk && _pushOff > 0.02f)
                _jumpFromWalkIn = Mathf.MoveTowards(_jumpFromWalkIn, 1f, dt / 0.04f);
            if (_jumpFromSlide && _pushOff > 0.02f)
                _jumpFromSlideIn = Mathf.MoveTowards(_jumpFromSlideIn, 1f, dt / 0.04f);
            if (_jumpFromPunch && _pushOff > 0.02f)
                _jumpFromPunchIn = Mathf.MoveTowards(_jumpFromPunchIn, 1f, dt / 0.04f);
            bool dashingAir = _motor != null && _motor.IsAirDashing;
            if (dashingAir && !_airDashPoseWas && _diveFromJump && !_jumpFromDash)
            {
                // The jump eases into the burst. The burst still holds.
                // A slide into an air dash keeps its ease. A ski into an air dash keeps its ease.
                // Jump height is unchanged. Duration and cooldown are unchanged.
                _dashFromJump = true;
                _dashFromJumpIn = 0f;
                if (_upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
                {
                    _jumpDashUaL = _upperArmL.localRotation;
                    _jumpDashUaR = _upperArmR.localRotation;
                    _jumpDashLaL = _lowerArmL.localRotation;
                    _jumpDashLaR = _lowerArmR.localRotation;
                    _jumpDashUlL = _upperLegL.localRotation;
                    _jumpDashUlR = _upperLegR.localRotation;
                    _jumpDashLlL = _lowerLegL.localRotation;
                    _jumpDashLlR = _lowerLegR.localRotation;
                    _jumpDashSp = _spine.localRotation;
                    _jumpDashHp = _hips.localRotation;
                    _jumpDashHd = _head.localRotation;
                }
                else
                    _dashFromJumpIn = 1f;
            }
            if (dashingAir && _dashFromJump)
                _dashFromJumpIn = Mathf.MoveTowards(_dashFromJumpIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromJump = false;
            bool dartAir = _diveVis > 0.2f && _input != null && _input.CrouchHeld && !jet;
            if (dashingAir && !_airDashPoseWas && dartAir && !_jumpFromDash)
            {
                // The dart eases into the burst. A moving fall uses the low stride.
                // The burst still holds. Fall speed stays doubled. Duration and cooldown are unchanged.
                _dashFromDart = true;
                _dashFromDartIn = 0f;
                float runNow = Mathf.InverseLerp(5.5f, 11.5f, speed);
                _dashFromDartStride = speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint && runNow <= 0.4f
                    && !(_airDashArms || _armRecover > 0f);
                float dartSin = Mathf.Sin(_cycle);
                _dartStepL = Mathf.Max(0f, dartSin);
                _dartStepR = Mathf.Max(0f, -dartSin);
            }
            if (dashingAir && _dashFromDart)
                _dashFromDartIn = Mathf.MoveTowards(_dashFromDartIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromDart = false;
            bool skiGlide = _skiBlend > 0.2f && !dartAir && !jet;
            if (dashingAir && !_airDashPoseWas && skiGlide && !_jumpFromDash && !_dashFromDart)
            {
                // The glide eases into the burst. The burst still holds.
                // An air crouch into a dash keeps its ease. Ski speed is unchanged.
                // Duration and cooldown are unchanged.
                _dashFromSki = true;
                _dashFromSkiIn = 0f;
                _skiSin = Mathf.Sin(_cycle);
            }
            if (dashingAir && _dashFromSki)
                _dashFromSkiIn = Mathf.MoveTowards(_dashFromSkiIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromSki = false;
            bool slideWedge = _dropSlide && _dropVis > 0.2f && !dartAir && !jet;
            if (dashingAir && !_airDashPoseWas && slideWedge && !_jumpFromDash && !_dashFromDart && !_dashFromSki
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The wedge eases into the burst. The burst still holds.
                // A ski into a dash keeps its ease. A ski into a slide keeps its ease.
                // slideBoost stays 0. Duration and cooldown are unchanged.
                _dashFromSlide = true;
                _dashFromSlideIn = 0f;
                _slideLeadLeft = Mathf.Sin(_cycle) >= 0f;
                _slideDashUaL = _upperArmL.localRotation;
                _slideDashUaR = _upperArmR.localRotation;
                _slideDashLaL = _lowerArmL.localRotation;
                _slideDashLaR = _lowerArmR.localRotation;
                _slideDashUlL = _upperLegL.localRotation;
                _slideDashUlR = _upperLegR.localRotation;
                _slideDashLlL = _lowerLegL.localRotation;
                _slideDashLlR = _lowerLegR.localRotation;
                _slideDashSp = _spine.localRotation;
                _slideDashHp = _hips.localRotation;
                _slideDashHd = _head.localRotation;
            }
            if (dashingAir && _dashFromSlide)
                _dashFromSlideIn = Mathf.MoveTowards(_dashFromSlideIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromSlide = false;
            bool climbLeave = !wallRun && !_exitFromWall && (leavingSurf || _wallExit > 0.2f);
            if (dashingAir && !_airDashPoseWas && climbLeave && !jet && !_jumpFromDash && !_dashFromDart && !_dashFromSki && !_dashFromSlide
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The climb eases into the burst. The burst still holds.
                // An air dash into a slide keeps its ease. A ski and a slide into a dash keep their ease.
                // Exit time is unchanged. Duration and cooldown are unchanged.
                _dashFromClimb = true;
                _dashFromClimbIn = 0f;
                _climbUaL = _upperArmL.localRotation;
                _climbUaR = _upperArmR.localRotation;
                _climbLaL = _lowerArmL.localRotation;
                _climbLaR = _lowerArmR.localRotation;
                _climbUlL = _upperLegL.localRotation;
                _climbUlR = _upperLegR.localRotation;
                _climbLlL = _lowerLegL.localRotation;
                _climbLlR = _lowerLegR.localRotation;
                _climbSp = _spine.localRotation;
                _climbHp = _hips.localRotation;
                _climbHd = _head.localRotation;
            }
            if (dashingAir && _dashFromClimb)
                _dashFromClimbIn = Mathf.MoveTowards(_dashFromClimbIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromClimb = false;
            bool wallLeave = _exitFromWall && (wallRun || leavingSurf || _wallExit > 0.2f);
            if (dashingAir && !_airDashPoseWas && wallLeave && !jet && !_jumpFromDash && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The wall exit eases into the burst. The burst still holds.
                // A climb into a dash keeps its ease.
                // Exit time is unchanged. Duration and cooldown are unchanged.
                _dashFromWall = true;
                _dashFromWallIn = 0f;
                _wallUaL = _upperArmL.localRotation;
                _wallUaR = _upperArmR.localRotation;
                _wallLaL = _lowerArmL.localRotation;
                _wallLaR = _lowerArmR.localRotation;
                _wallUlL = _upperLegL.localRotation;
                _wallUlR = _upperLegR.localRotation;
                _wallLlL = _lowerLegL.localRotation;
                _wallLlR = _lowerLegR.localRotation;
                _wallSp = _spine.localRotation;
                _wallHp = _hips.localRotation;
                _wallHd = _head.localRotation;
            }
            if (dashingAir && _dashFromWall)
                _dashFromWallIn = Mathf.MoveTowards(_dashFromWallIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromWall = false;
            bool stillGuard = _crouchFromStand && !_crouchWalkArmed && _dropVis > 0.2f && !sliding && !dartAir
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover;
            if (dashingAir && !_airDashPoseWas && stillGuard && !jet && !_diveFromJump && !_jumpFromDash
                && !_dashFromJump && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The guard eases into the burst. The burst still holds.
                // A jump into a dash keeps its ease. An air crouch into a dash keeps its ease.
                // A slide into a dash keeps its ease. Duration and cooldown are unchanged.
                _dashFromCrouch = true;
                _dashFromCrouchIn = 0f;
                _crouchDashUaL = _upperArmL.localRotation;
                _crouchDashUaR = _upperArmR.localRotation;
                _crouchDashLaL = _lowerArmL.localRotation;
                _crouchDashLaR = _lowerArmR.localRotation;
                _crouchDashUlL = _upperLegL.localRotation;
                _crouchDashUlR = _upperLegR.localRotation;
                _crouchDashLlL = _lowerLegL.localRotation;
                _crouchDashLlR = _lowerLegR.localRotation;
                _crouchDashSp = _spine.localRotation;
                _crouchDashHp = _hips.localRotation;
                _crouchDashHd = _head.localRotation;
            }
            if (dashingAir && _dashFromCrouch)
                _dashFromCrouchIn = Mathf.MoveTowards(_dashFromCrouchIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromCrouch = false;
            bool punchStrike = punching && (phase == PunchPhase.Windup || phase == PunchPhase.Active);
            if (dashingAir && !_airDashPoseWas && punchStrike && !jet
                && !_jumpFromDash && !_diveFromJump && !_dashFromJump && !_dashFromDart && !_dashFromSki
                && !_dashFromSlide && !_dashFromClimb && !_dashFromWall && !_dashFromCrouch
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The punch eases into the burst. The burst still holds.
                // A punch miss into a dash keeps its ease. A tag into a dash keeps its ease.
                // A still crouch into a dash keeps its ease. Windup time is unchanged.
                // Duration and cooldown are unchanged.
                _dashFromPunch = true;
                _dashFromPunchIn = 0f;
                _punchDashUaL = _upperArmL.localRotation;
                _punchDashUaR = _upperArmR.localRotation;
                _punchDashLaL = _lowerArmL.localRotation;
                _punchDashLaR = _lowerArmR.localRotation;
                _punchDashUlL = _upperLegL.localRotation;
                _punchDashUlR = _upperLegR.localRotation;
                _punchDashLlL = _lowerLegL.localRotation;
                _punchDashLlR = _lowerLegR.localRotation;
                _punchDashSp = _spine.localRotation;
                _punchDashHp = _hips.localRotation;
                _punchDashHd = _head.localRotation;
            }
            if (dashingAir && _dashFromPunch)
                _dashFromPunchIn = Mathf.MoveTowards(_dashFromPunchIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromPunch = false;
            if (dashingAir && !_airDashPoseWas && crouchWalkPose && !dartAir && !jet
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !_diveFromJump && !_jumpFromDash && !_dashFromJump && !_dashFromDart && !_dashFromSki
                && !_dashFromSlide && !_dashFromClimb && !_dashFromWall && !_dashFromCrouch && !_dashFromPunch
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The low stride eases into the burst. The burst still holds.
                // A still crouch into a dash keeps its ease. A punch into a dash keeps its ease.
                // A hard landing into a jump keeps its ease. A soft landing into a jump keeps its ease.
                // Duration and cooldown are unchanged.
                _dashFromCrouchWalk = true;
                _dashFromCrouchWalkIn = 0f;
                _crouchWalkDashUaL = _upperArmL.localRotation;
                _crouchWalkDashUaR = _upperArmR.localRotation;
                _crouchWalkDashLaL = _lowerArmL.localRotation;
                _crouchWalkDashLaR = _lowerArmR.localRotation;
                _crouchWalkDashUlL = _upperLegL.localRotation;
                _crouchWalkDashUlR = _upperLegR.localRotation;
                _crouchWalkDashLlL = _lowerLegL.localRotation;
                _crouchWalkDashLlR = _lowerLegR.localRotation;
                _crouchWalkDashSp = _spine.localRotation;
                _crouchWalkDashHp = _hips.localRotation;
                _crouchWalkDashHd = _head.localRotation;
            }
            if (dashingAir && _dashFromCrouchWalk)
                _dashFromCrouchWalkIn = Mathf.MoveTowards(_dashFromCrouchWalkIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromCrouchWalk = false;
            bool readyPulse = _dashReady > 0.2f && !punching && _grapplePose <= 0.04f && !dartAir
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover;
            if (dashingAir && !_airDashPoseWas && readyPulse && !jet
                && !_diveFromJump && !_jumpFromDash && !_dashFromJump && !_dashFromDart && !_dashFromSki
                && !_dashFromSlide && !_dashFromClimb && !_dashFromWall && !_dashFromCrouch && !_dashFromPunch
                && !_dashFromCrouchWalk
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The pulse eases into the burst. The burst still holds.
                // A crouch walk into a dash keeps its ease. A punch into a dash keeps its ease.
                // A dash coming off cooldown into a jump keeps its push.
                // Duration and cooldown are unchanged.
                _dashFromReady = true;
                _dashFromReadyIn = 0f;
                _readyDashUaL = _upperArmL.localRotation;
                _readyDashUaR = _upperArmR.localRotation;
                _readyDashLaL = _lowerArmL.localRotation;
                _readyDashLaR = _lowerArmR.localRotation;
                _readyDashUlL = _upperLegL.localRotation;
                _readyDashUlR = _upperLegR.localRotation;
                _readyDashLlL = _lowerLegL.localRotation;
                _readyDashLlR = _lowerLegR.localRotation;
                _readyDashSp = _spine.localRotation;
                _readyDashHp = _hips.localRotation;
                _readyDashHd = _head.localRotation;
                _dashReady = 0f;
            }
            if (dashingAir && _dashFromReady)
                _dashFromReadyIn = Mathf.MoveTowards(_dashFromReadyIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromReady = false;
            bool missWhiff = phase == PunchPhase.MissRecover && !_jumpFromMiss;
            if (dashingAir && !_airDashPoseWas && missWhiff && !jet
                && !_jumpFromDash && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The whiff eases into the burst. The burst still holds.
                // A punch miss into a jump keeps its push. A crouch miss keeps its pose.
                // An air dash into a wall or a climb keeps its ease.
                // Duration and cooldown are unchanged.
                _dashFromMiss = true;
                _dashFromMissIn = 0f;
                _missUaL = _upperArmL.localRotation;
                _missUaR = _upperArmR.localRotation;
                _missLaL = _lowerArmL.localRotation;
                _missLaR = _lowerArmR.localRotation;
                _missUlL = _upperLegL.localRotation;
                _missUlR = _upperLegR.localRotation;
                _missLlL = _lowerLegL.localRotation;
                _missLlR = _lowerLegR.localRotation;
                _missSp = _spine.localRotation;
                _missHp = _hips.localRotation;
                _missHd = _head.localRotation;
            }
            if (dashingAir && _dashFromMiss)
                _dashFromMissIn = Mathf.MoveTowards(_dashFromMissIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromMiss = false;
            bool tagConnect = phase == PunchPhase.HitRecover && !_jumpFromTag;
            if (dashingAir && !_airDashPoseWas && tagConnect && !jet && !_dashFromMiss
                && !_jumpFromDash && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The connect eases into the burst. The burst still holds.
                // A tag into a jump keeps its push. A crouch tag keeps its pose.
                // A punch miss into a dash keeps its ease.
                // Duration and cooldown are unchanged.
                _dashFromTag = true;
                _dashFromTagIn = 0f;
                _tagUaL = _upperArmL.localRotation;
                _tagUaR = _upperArmR.localRotation;
                _tagLaL = _lowerArmL.localRotation;
                _tagLaR = _lowerArmR.localRotation;
                _tagUlL = _upperLegL.localRotation;
                _tagUlR = _upperLegR.localRotation;
                _tagLlL = _lowerLegL.localRotation;
                _tagLlR = _lowerLegR.localRotation;
                _tagSp = _spine.localRotation;
                _tagHp = _hips.localRotation;
                _tagHd = _head.localRotation;
            }
            if (dashingAir && _dashFromTag)
                _dashFromTagIn = Mathf.MoveTowards(_dashFromTagIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromTag = false;
            bool claimPose = _itClaim > 0.2f && !_jumpFromClaim && phase != PunchPhase.HitRecover;
            if (dashingAir && !_airDashPoseWas && claimPose && !jet && !_dashFromTag && !_dashFromMiss
                && !_jumpFromDash && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The claim eases into the burst. The burst still holds.
                // Becoming It into a jump keeps its push. A crouch claim keeps its pose.
                // A tag into a dash keeps its ease.
                // Duration and cooldown are unchanged.
                _dashFromClaim = true;
                _dashFromClaimIn = 0f;
                _claimUaL = _upperArmL.localRotation;
                _claimUaR = _upperArmR.localRotation;
                _claimLaL = _lowerArmL.localRotation;
                _claimLaR = _lowerArmR.localRotation;
                _claimUlL = _upperLegL.localRotation;
                _claimUlR = _upperLegR.localRotation;
                _claimLlL = _lowerLegL.localRotation;
                _claimLlR = _lowerLegR.localRotation;
                _claimSp = _spine.localRotation;
                _claimHp = _hips.localRotation;
                _claimHd = _head.localRotation;
            }
            if (dashingAir && _dashFromClaim)
                _dashFromClaimIn = Mathf.MoveTowards(_dashFromClaimIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromClaim = false;
            bool grappleLine = _grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f && !_jumpFromGrapple;
            if (dashingAir && !_airDashPoseWas && grappleLine && !jet && !_dashFromClaim && !_dashFromTag && !_dashFromMiss
                && !_jumpFromDash && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The line eases into the burst. The burst still holds.
                // A grapple release into a jump keeps its push. A crouch release keeps its pose.
                // Becoming It into a dash keeps its ease. The gate stays off.
                // Duration and cooldown are unchanged.
                _dashFromGrapple = true;
                _dashFromGrappleIn = 0f;
                _grappleUaL = _upperArmL.localRotation;
                _grappleUaR = _upperArmR.localRotation;
                _grappleLaL = _lowerArmL.localRotation;
                _grappleLaR = _lowerArmR.localRotation;
                _grappleUlL = _upperLegL.localRotation;
                _grappleUlR = _upperLegR.localRotation;
                _grappleLlL = _lowerLegL.localRotation;
                _grappleLlR = _lowerLegR.localRotation;
                _grappleSp = _spine.localRotation;
                _grappleHp = _hips.localRotation;
                _grappleHd = _head.localRotation;
            }
            if (dashingAir && _dashFromGrapple)
                _dashFromGrappleIn = Mathf.MoveTowards(_dashFromGrappleIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromGrapple = false;
            bool softAbsorb = _landHard < 0.4f && _landSquash > 0.08f && !_jumpFromSoftLand;
            if (dashingAir && !_airDashPoseWas && softAbsorb && !jet && !_dashFromGrapple && !_dashFromClaim && !_dashFromTag && !_dashFromMiss
                && !_jumpFromDash && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The absorb eases into the burst. The burst still holds.
                // A soft landing into a jump keeps its push. A hard landing keeps its pose.
                // An air dash into a tag keeps its ease. Land time is unchanged.
                // Duration and cooldown are unchanged.
                _dashFromSoft = true;
                _dashFromSoftIn = 0f;
                _softUaL = _upperArmL.localRotation;
                _softUaR = _upperArmR.localRotation;
                _softLaL = _lowerArmL.localRotation;
                _softLaR = _lowerArmR.localRotation;
                _softUlL = _upperLegL.localRotation;
                _softUlR = _upperLegR.localRotation;
                _softLlL = _lowerLegL.localRotation;
                _softLlR = _lowerLegR.localRotation;
                _softSp = _spine.localRotation;
                _softHp = _hips.localRotation;
                _softHd = _head.localRotation;
            }
            if (dashingAir && _dashFromSoft)
                _dashFromSoftIn = Mathf.MoveTowards(_dashFromSoftIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromSoft = false;
            bool hardAbsorb = _landHard >= 0.4f && _landSquash > 0.08f && !_jumpFromHardLand;
            if (dashingAir && !_airDashPoseWas && hardAbsorb && !jet && !_dashFromSoft && !_dashFromGrapple && !_dashFromClaim && !_dashFromTag && !_dashFromMiss
                && !_jumpFromDash && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The deeper absorb eases into the burst. The burst still holds.
                // A soft landing into a dash keeps its ease. A hard landing into a jump keeps its push.
                // Land time is unchanged. Duration and cooldown are unchanged.
                _dashFromHard = true;
                _dashFromHardIn = 0f;
                _hardUaL = _upperArmL.localRotation;
                _hardUaR = _upperArmR.localRotation;
                _hardLaL = _lowerArmL.localRotation;
                _hardLaR = _lowerArmR.localRotation;
                _hardUlL = _upperLegL.localRotation;
                _hardUlR = _upperLegR.localRotation;
                _hardLlL = _lowerLegL.localRotation;
                _hardLlR = _lowerLegR.localRotation;
                _hardSp = _spine.localRotation;
                _hardHp = _hips.localRotation;
                _hardHd = _head.localRotation;
            }
            if (dashingAir && _dashFromHard)
                _dashFromHardIn = Mathf.MoveTowards(_dashFromHardIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromHard = false;
            bool walkStride = _wasGrounded && _prevSpeed > 0.35f && _prevSpeed <= 5.5f
                && !_crouchFromStand && !_crouchWalkArmed && !crouchWalkPose
                && !_diveFromJump && !dartAir && !jet
                && _skiBlend <= 0.2f && !_dropSlide && _landSquash <= 0.08f
                && _dashReady <= 0.2f && _itClaim <= 0.2f && _grapplePose <= 0.04f
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover;
            if (dashingAir && !_airDashPoseWas && walkStride && !jet
                && !_jumpFromDash && !_dashFromJump && !_dashFromDart && !_dashFromSki && !_dashFromSlide
                && !_dashFromClimb && !_dashFromWall && !_dashFromCrouch && !_dashFromPunch && !_dashFromCrouchWalk
                && !_dashFromReady && !_dashFromMiss && !_dashFromTag && !_dashFromClaim && !_dashFromGrapple
                && !_dashFromSoft && !_dashFromHard
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The walk eases into the burst. The burst still holds.
                // A walk into a jump keeps its ease. A crouch walk into a dash keeps its ease.
                // A still crouch into a dash keeps its ease. Duration and cooldown are unchanged.
                _dashFromWalk = true;
                _dashFromWalkIn = 0f;
                _walkDashUaL = _upperArmL.localRotation;
                _walkDashUaR = _upperArmR.localRotation;
                _walkDashLaL = _lowerArmL.localRotation;
                _walkDashLaR = _lowerArmR.localRotation;
                _walkDashUlL = _upperLegL.localRotation;
                _walkDashUlR = _upperLegR.localRotation;
                _walkDashLlL = _lowerLegL.localRotation;
                _walkDashLlR = _lowerLegR.localRotation;
                _walkDashSp = _spine.localRotation;
                _walkDashHp = _hips.localRotation;
                _walkDashHd = _head.localRotation;
            }
            if (dashingAir && _dashFromWalk)
                _dashFromWalkIn = Mathf.MoveTowards(_dashFromWalkIn, 1f, dt / 0.04f);
            else if (!dashingAir)
                _dashFromWalk = false;
            if (!dashingAir && _airDashPoseWas && !jet && air && _input != null && _input.CrouchHeld)
            {
                // The burst eases into the dart. An air crouch into a dash keeps its ease.
                // Fall speed stays doubled. Duration and cooldown are unchanged.
                _dartFromDash = true;
                _dartFromDashIn = 0f;
            }
            bool dashTell = _dashPulse > 0.04f || lunging || dashingAir;
            if (_dartFromDash && !dashingAir && !jet && air && _input != null && _input.CrouchHeld)
            {
                _dartFromDashIn = Mathf.MoveTowards(_dartFromDashIn, 1f, dt / 0.16f);
                if (_dartFromDashIn >= 0.98f && !dashTell)
                    _dartFromDash = false;
            }
            else
                _dartFromDash = false;
            if (!dashingAir && _airDashPoseWas && skiing && !jet && !crouch
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The burst eases into the glide. A wall run into a ski keeps its ease.
                // A climb into a ski keeps its ease. An air dash into a slide keeps its ease.
                // A ski into a dash keeps its ease. Ski speed is unchanged.
                // Duration and cooldown are unchanged.
                _skiFromDash = true;
                _skiFromDashIn = 0f;
                _dashSkiUaL = _upperArmL.localRotation;
                _dashSkiUaR = _upperArmR.localRotation;
                _dashSkiLaL = _lowerArmL.localRotation;
                _dashSkiLaR = _lowerArmR.localRotation;
                _dashSkiUlL = _upperLegL.localRotation;
                _dashSkiUlR = _upperLegR.localRotation;
                _dashSkiLlL = _lowerLegL.localRotation;
                _dashSkiLlR = _lowerLegR.localRotation;
                _dashSkiSp = _spine.localRotation;
                _dashSkiHp = _hips.localRotation;
                _dashSkiHd = _head.localRotation;
            }
            if (_skiFromDash && !dashingAir && skiing && !jet && !crouch)
            {
                _skiFromDashIn = Mathf.MoveTowards(_skiFromDashIn, 1f, dt / 0.04f);
                if (_skiFromDashIn >= 0.98f && !dashTell)
                    _skiFromDash = false;
            }
            else
                _skiFromDash = false;
            if (!dashingAir && _airDashPoseWas && sliding && !jet && !skiing)
            {
                // The burst eases into the wedge. An air dash into a ski keeps its ease.
                // A ski into a dash keeps its ease. A slide into a dash keeps its ease.
                // slideBoost stays 0. Duration and cooldown are unchanged.
                _slideFromDash = true;
                _slideFromDashIn = 0f;
            }
            if (_slideFromDash && !dashingAir && sliding && !jet && !skiing)
            {
                _slideFromDashIn = Mathf.MoveTowards(_slideFromDashIn, 1f, dt / 0.16f);
                if (_slideFromDashIn >= 0.98f && !dashTell)
                    _slideFromDash = false;
            }
            else
                _slideFromDash = false;
            if (!dashingAir && _airDashPoseWas && climb && !jet && !wallRun)
            {
                // The burst eases into the grab. A climb into a dash keeps its ease.
                // A wall exit into a dash keeps its ease.
                // Exit time is unchanged. Duration and cooldown are unchanged.
                _climbFromDash = true;
                _climbFromDashIn = 0f;
            }
            if (_climbFromDash && !dashingAir && climb && !jet && !wallRun)
            {
                _climbFromDashIn = Mathf.MoveTowards(_climbFromDashIn, 1f, dt / 0.16f);
                if (_climbFromDashIn >= 0.98f && !dashTell)
                    _climbFromDash = false;
            }
            else
                _climbFromDash = false;
            if (!dashingAir && _airDashPoseWas && wallRun && !jet && !climb)
            {
                // The burst eases into the attach. An air dash into a climb keeps its ease.
                // A climb into a dash keeps its ease. A wall exit into a dash keeps its ease.
                // Exit time is unchanged. Duration and cooldown are unchanged.
                _wallFromDash = true;
                _wallFromDashIn = 0f;
            }
            if (_wallFromDash && !dashingAir && wallRun && !jet && !climb)
            {
                _wallFromDashIn = Mathf.MoveTowards(_wallFromDashIn, 1f, dt / 0.16f);
                if (_wallFromDashIn >= 0.98f && !dashTell)
                    _wallFromDash = false;
            }
            else
                _wallFromDash = false;
            bool burstPunch = punching && phase == PunchPhase.Windup;
            bool jumpPunch = (!grounded && _diveFromJump) || (_landedFromJump && _landSquash > 0.08f);
            bool windupOnTail = burstPunch && !_punchWindWas && !dashingAir && _airDashArms && _dashPulse > 0.04f;
            if (!jumpPunch && (( !dashingAir && _airDashPoseWas && burstPunch) || windupOnTail)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The burst eases into the cock. A slide into a punch keeps its ease.
                // A ski into a tag keeps its ease. A slide into a tag keeps its ease.
                // Windup time is unchanged. Duration and cooldown are unchanged.
                _punchFromDash = true;
                _punchFromDashIn = 0f;
                _dashPunchUaL = _upperArmL.localRotation;
                _dashPunchUaR = _upperArmR.localRotation;
                _dashPunchLaL = _lowerArmL.localRotation;
                _dashPunchLaR = _lowerArmR.localRotation;
                _dashPunchUlL = _upperLegL.localRotation;
                _dashPunchUlR = _upperLegR.localRotation;
                _dashPunchLlL = _lowerLegL.localRotation;
                _dashPunchLlR = _lowerLegR.localRotation;
                _dashPunchSp = _spine.localRotation;
                _dashPunchHp = _hips.localRotation;
                _dashPunchHd = _head.localRotation;
            }
            if (_punchFromDash && !dashingAir && burstPunch && !jumpPunch)
            {
                _punchFromDashIn = Mathf.MoveTowards(_punchFromDashIn, 1f, dt / 0.04f);
                if (_punchFromDashIn >= 0.98f && !dashTell)
                    _punchFromDash = false;
            }
            else
                _punchFromDash = false;
            bool burstTag = punching && phase == PunchPhase.HitRecover && !crouch && !_jumpFromTag;
            bool tagOnTail = burstTag && !_tagHitWas && !dashingAir && _airDashArms && _dashPulse > 0.04f;
            if (!jumpPunch && !_punchFromDash && ((!dashingAir && _airDashPoseWas && burstTag) || tagOnTail)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The burst eases into the connect. An air dash into a punch keeps its ease.
                // A climb into a punch keeps its ease. A jump into a tag keeps its ease.
                // Connect time is unchanged. Duration and cooldown are unchanged.
                _tagFromDash = true;
                _tagFromDashIn = 0f;
                _dashTagUaL = _upperArmL.localRotation;
                _dashTagUaR = _upperArmR.localRotation;
                _dashTagLaL = _lowerArmL.localRotation;
                _dashTagLaR = _lowerArmR.localRotation;
                _dashTagUlL = _upperLegL.localRotation;
                _dashTagUlR = _upperLegR.localRotation;
                _dashTagLlL = _lowerLegL.localRotation;
                _dashTagLlR = _lowerLegR.localRotation;
                _dashTagSp = _spine.localRotation;
                _dashTagHp = _hips.localRotation;
                _dashTagHd = _head.localRotation;
            }
            if (_tagFromDash && !dashingAir && burstTag && !jumpPunch && !_punchFromDash)
            {
                _tagFromDashIn = Mathf.MoveTowards(_tagFromDashIn, 1f, dt / 0.04f);
                if (_tagFromDashIn >= 0.98f && !dashTell)
                    _tagFromDash = false;
            }
            else
                _tagFromDash = false;
            _airDashPoseWas = dashingAir;
            bool windupNow = punching && phase == PunchPhase.Windup;
            bool fromJumpPose = (!grounded && _diveFromJump) || (_landedFromJump && _landSquash > 0.08f);
            if (windupNow && !_punchWindWas && fromJumpPose
                && _upperArmL != null && _spine != null && _upperLegL != null && _head != null)
            {
                // The apex or the landing eases into the cock. A punch from the ground keeps its windup.
                // Windup time is unchanged. Jump height is unchanged.
                _punchFromJump = true;
                _punchFromJumpIn = 0f;
                _punchUaL = _upperArmL.localRotation;
                _punchUaR = _upperArmR.localRotation;
                _punchLaL = _lowerArmL.localRotation;
                _punchLaR = _lowerArmR.localRotation;
                _punchSp = _spine.localRotation;
                _punchHp = _hips.localRotation;
                _punchHd = _head.localRotation;
                _punchUlL = _upperLegL.localRotation;
                _punchUlR = _upperLegR.localRotation;
                _punchLlL = _lowerLegL.localRotation;
                _punchLlR = _lowerLegR.localRotation;
            }
            if (windupNow && _punchFromJump)
                _punchFromJumpIn = Mathf.MoveTowards(_punchFromJumpIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromJump = false;
            bool softPunch = windupNow && !_punchWindWas && !fromJumpPose && !_punchFromJump && !_punchFromDash
                && !_jumpFromSoftLand && _landHard < 0.4f && _landSquash > 0.08f
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (softPunch)
            {
                // The absorb eases into the cock. A jump into a punch keeps its ease.
                // A soft landing into a jump keeps its push. A hard landing keeps its pose.
                // Windup time is unchanged. Land time is unchanged.
                _punchFromSoft = true;
                _punchFromSoftIn = 0f;
                _landPunchUaL = _upperArmL.localRotation;
                _landPunchUaR = _upperArmR.localRotation;
                _landPunchLaL = _lowerArmL.localRotation;
                _landPunchLaR = _lowerArmR.localRotation;
                _landPunchUlL = _upperLegL.localRotation;
                _landPunchUlR = _upperLegR.localRotation;
                _landPunchLlL = _lowerLegL.localRotation;
                _landPunchLlR = _lowerLegR.localRotation;
                _landPunchSp = _spine.localRotation;
                _landPunchHp = _hips.localRotation;
                _landPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromSoft)
                _punchFromSoftIn = Mathf.MoveTowards(_punchFromSoftIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromSoft = false;
            bool hardPunch = windupNow && !_punchWindWas && !fromJumpPose && !_punchFromJump && !_punchFromDash && !_punchFromSoft
                && !_jumpFromHardLand && _landHard >= 0.4f && _landSquash > 0.08f
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (hardPunch)
            {
                // The deeper absorb eases into the cock. A soft landing into a punch keeps its ease.
                // A hard landing into a jump keeps its push. A jump into a punch keeps its ease.
                // Windup time is unchanged. Land time is unchanged.
                _punchFromHard = true;
                _punchFromHardIn = 0f;
                _hardPunchUaL = _upperArmL.localRotation;
                _hardPunchUaR = _upperArmR.localRotation;
                _hardPunchLaL = _lowerArmL.localRotation;
                _hardPunchLaR = _lowerArmR.localRotation;
                _hardPunchUlL = _upperLegL.localRotation;
                _hardPunchUlR = _upperLegR.localRotation;
                _hardPunchLlL = _lowerLegL.localRotation;
                _hardPunchLlR = _lowerLegR.localRotation;
                _hardPunchSp = _spine.localRotation;
                _hardPunchHp = _hips.localRotation;
                _hardPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromHard)
                _punchFromHardIn = Mathf.MoveTowards(_punchFromHardIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromHard = false;
            bool skiPunch = windupNow && !_punchWindWas && !fromJumpPose && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard
                && !_jumpFromSki && _skiBlend > 0.2f && !dartAir && !jet && !_dropSlide
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (skiPunch)
            {
                // The glide eases into the cock. A ski into a jump keeps its push.
                // A ski into an air dash keeps its ease. A hard landing into a tag keeps its ease.
                // Windup time is unchanged. Ski speed is unchanged.
                _punchFromSki = true;
                _punchFromSkiIn = 0f;
                _skiPunchUaL = _upperArmL.localRotation;
                _skiPunchUaR = _upperArmR.localRotation;
                _skiPunchLaL = _lowerArmL.localRotation;
                _skiPunchLaR = _lowerArmR.localRotation;
                _skiPunchUlL = _upperLegL.localRotation;
                _skiPunchUlR = _upperLegR.localRotation;
                _skiPunchLlL = _lowerLegL.localRotation;
                _skiPunchLlR = _lowerLegR.localRotation;
                _skiPunchSp = _spine.localRotation;
                _skiPunchHp = _hips.localRotation;
                _skiPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromSki)
                _punchFromSkiIn = Mathf.MoveTowards(_punchFromSkiIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromSki = false;
            bool slidePunch = windupNow && !_punchWindWas && !fromJumpPose && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard && !_punchFromSki
                && !_jumpFromSlide && _dropSlide && _dropVis > 0.2f && !dartAir && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (slidePunch)
            {
                // The wedge eases into the cock. A ski into a punch keeps its ease.
                // A slide into a jump keeps its push. A slide into an air dash keeps its ease.
                // slideBoost stays 0. Windup time is unchanged.
                _punchFromSlide = true;
                _punchFromSlideIn = 0f;
                _slidePunchUaL = _upperArmL.localRotation;
                _slidePunchUaR = _upperArmR.localRotation;
                _slidePunchLaL = _lowerArmL.localRotation;
                _slidePunchLaR = _lowerArmR.localRotation;
                _slidePunchUlL = _upperLegL.localRotation;
                _slidePunchUlR = _upperLegR.localRotation;
                _slidePunchLlL = _lowerLegL.localRotation;
                _slidePunchLlR = _lowerLegR.localRotation;
                _slidePunchSp = _spine.localRotation;
                _slidePunchHp = _hips.localRotation;
                _slidePunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromSlide)
                _punchFromSlideIn = Mathf.MoveTowards(_punchFromSlideIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromSlide = false;
            bool climbPunch = windupNow && !_punchWindWas && !fromJumpPose && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard && !_punchFromSki && !_punchFromSlide
                && !_jumpFromClimb && (climb || climbLeave) && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (climbPunch)
            {
                // The grab eases into the cock. A slide into a tag keeps its ease.
                // A climb into an air dash keeps its ease. A climb into a jump keeps its push.
                // A wall run keeps its pose. Windup time is unchanged. Exit time is unchanged.
                _punchFromClimb = true;
                _punchFromClimbIn = 0f;
                _climbPunchUaL = _upperArmL.localRotation;
                _climbPunchUaR = _upperArmR.localRotation;
                _climbPunchLaL = _lowerArmL.localRotation;
                _climbPunchLaR = _lowerArmR.localRotation;
                _climbPunchUlL = _upperLegL.localRotation;
                _climbPunchUlR = _upperLegR.localRotation;
                _climbPunchLlL = _lowerLegL.localRotation;
                _climbPunchLlR = _lowerLegR.localRotation;
                _climbPunchSp = _spine.localRotation;
                _climbPunchHp = _hips.localRotation;
                _climbPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromClimb)
                _punchFromClimbIn = Mathf.MoveTowards(_punchFromClimbIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromClimb = false;
            bool wallPunch = windupNow && !_punchWindWas && !fromJumpPose && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard && !_punchFromSki && !_punchFromSlide && !_punchFromClimb
                && !_jumpFromWall && wallLeave && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (wallPunch)
            {
                // The wall exit eases into the cock. A climb into a punch keeps its ease.
                // A wall exit into an air dash keeps its ease. A wall exit into a jump keeps its push.
                // A climb into a tag keeps its ease. Windup time is unchanged. Exit time is unchanged.
                _punchFromWall = true;
                _punchFromWallIn = 0f;
                _wallPunchUaL = _upperArmL.localRotation;
                _wallPunchUaR = _upperArmR.localRotation;
                _wallPunchLaL = _lowerArmL.localRotation;
                _wallPunchLaR = _lowerArmR.localRotation;
                _wallPunchUlL = _upperLegL.localRotation;
                _wallPunchUlR = _upperLegR.localRotation;
                _wallPunchLlL = _lowerLegL.localRotation;
                _wallPunchLlR = _lowerLegR.localRotation;
                _wallPunchSp = _spine.localRotation;
                _wallPunchHp = _hips.localRotation;
                _wallPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromWall)
                _punchFromWallIn = Mathf.MoveTowards(_punchFromWallIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromWall = false;
            bool dartPunch = windupNow && !_punchWindWas && !fromJumpPose && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall
                && !_jumpFromAirCrouch && dartAir
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (dartPunch)
            {
                // The dart eases into the cock. A wall exit into a tag keeps its ease.
                // An air crouch into an air dash keeps its ease. An air crouch into a jump keeps its push.
                // A jump into a punch keeps its ease. Windup time is unchanged. Fall speed is unchanged.
                _punchFromDart = true;
                _punchFromDartIn = 0f;
                _dartPunchUaL = _upperArmL.localRotation;
                _dartPunchUaR = _upperArmR.localRotation;
                _dartPunchLaL = _lowerArmL.localRotation;
                _dartPunchLaR = _lowerArmR.localRotation;
                _dartPunchUlL = _upperLegL.localRotation;
                _dartPunchUlR = _upperLegR.localRotation;
                _dartPunchLlL = _lowerLegL.localRotation;
                _dartPunchLlR = _lowerLegR.localRotation;
                _dartPunchSp = _spine.localRotation;
                _dartPunchHp = _hips.localRotation;
                _dartPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromDart)
                _punchFromDartIn = Mathf.MoveTowards(_punchFromDartIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromDart = false;
            bool claimPunch = windupNow && !_punchWindWas && !fromJumpPose && !crouch && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall && !_punchFromDart
                && !_jumpFromClaim && _itClaim > 0.2f && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (claimPunch)
            {
                // The claim eases into the cock. An air crouch into a tag keeps its ease.
                // Becoming It into an air dash keeps its ease. Becoming It into a jump keeps its push.
                // A crouch claim keeps its pose. Windup time is unchanged. Claim time is unchanged.
                _punchFromClaim = true;
                _punchFromClaimIn = 0f;
                _claimPunchUaL = _upperArmL.localRotation;
                _claimPunchUaR = _upperArmR.localRotation;
                _claimPunchLaL = _lowerArmL.localRotation;
                _claimPunchLaR = _lowerArmR.localRotation;
                _claimPunchUlL = _upperLegL.localRotation;
                _claimPunchUlR = _upperLegR.localRotation;
                _claimPunchLlL = _lowerLegL.localRotation;
                _claimPunchLlR = _lowerLegR.localRotation;
                _claimPunchSp = _spine.localRotation;
                _claimPunchHp = _hips.localRotation;
                _claimPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromClaim)
                _punchFromClaimIn = Mathf.MoveTowards(_punchFromClaimIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromClaim = false;
            bool grapplePunch = windupNow && !_punchWindWas && !fromJumpPose && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall && !_punchFromDart && !_punchFromClaim
                && _grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f && !_jumpFromGrapple && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (grapplePunch)
            {
                // The line eases into the cock. Becoming It into a tag keeps its ease.
                // A grapple release into an air dash keeps its ease. A grapple release into a jump keeps its push.
                // A crouch release keeps its pose. Windup time is unchanged. The gate stays off.
                _punchFromGrapple = true;
                _punchFromGrappleIn = 0f;
                _grapplePunchUaL = _upperArmL.localRotation;
                _grapplePunchUaR = _upperArmR.localRotation;
                _grapplePunchLaL = _lowerArmL.localRotation;
                _grapplePunchLaR = _lowerArmR.localRotation;
                _grapplePunchUlL = _upperLegL.localRotation;
                _grapplePunchUlR = _upperLegR.localRotation;
                _grapplePunchLlL = _lowerLegL.localRotation;
                _grapplePunchLlR = _lowerLegR.localRotation;
                _grapplePunchSp = _spine.localRotation;
                _grapplePunchHp = _hips.localRotation;
                _grapplePunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromGrapple)
                _punchFromGrappleIn = Mathf.MoveTowards(_punchFromGrappleIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromGrapple = false;
            bool readyPunch = windupNow && !_punchWindWas && !fromJumpPose && !crouch && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall && !_punchFromDart && !_punchFromClaim && !_punchFromGrapple
                && _dashReady > 0.2f && _grapplePose <= 0.04f && !_jumpFromReady && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (readyPunch)
            {
                // The pulse eases into the cock. A grapple release into a tag keeps its ease.
                // A dash coming off cooldown into a jump keeps its push. A crouch ready keeps its pose.
                // Windup time is unchanged. Duration and cooldown are unchanged.
                _punchFromReady = true;
                _punchFromReadyIn = 0f;
                _readyPunchUaL = _upperArmL.localRotation;
                _readyPunchUaR = _upperArmR.localRotation;
                _readyPunchLaL = _lowerArmL.localRotation;
                _readyPunchLaR = _lowerArmR.localRotation;
                _readyPunchUlL = _upperLegL.localRotation;
                _readyPunchUlR = _upperLegR.localRotation;
                _readyPunchLlL = _lowerLegL.localRotation;
                _readyPunchLlR = _lowerLegR.localRotation;
                _readyPunchSp = _spine.localRotation;
                _readyPunchHp = _hips.localRotation;
                _readyPunchHd = _head.localRotation;
                _dashReady = 0f;
            }
            if (windupNow && _punchFromReady)
                _punchFromReadyIn = Mathf.MoveTowards(_punchFromReadyIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromReady = false;
            bool tagPunch = windupNow && !_punchWindWas && !fromJumpPose && !crouch && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall && !_punchFromDart && !_punchFromClaim && !_punchFromGrapple && !_punchFromReady
                && _tagPunchHold && !_jumpFromTag && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (tagPunch)
            {
                // The connect eases into the cock. A punch into a tag keeps its ease.
                // A dash coming off cooldown into a punch keeps its ease. A crouch tag keeps its pose.
                // A tag into a jump keeps its push. Windup time is unchanged. Connect time is unchanged.
                _punchFromTag = true;
                _punchFromTagIn = 0f;
                _tagPunchHold = false;
            }
            if (windupNow && _punchFromTag)
                _punchFromTagIn = Mathf.MoveTowards(_punchFromTagIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromTag = false;
            bool stillCrouchPunch = windupNow && !_punchWindWas && !fromJumpPose && grounded && !sliding && !_dropSlide
                && !dartAir && !jet && !_airDashPoseWas
                && _crouchFromStand && !_crouchWalkArmed && speed <= 0.35f && _dropVis > 0.2f
                && _itClaim <= 0.2f && _dashReady <= 0.2f && !_tagPunchHold
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard
                && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall && !_punchFromDart
                && !_punchFromClaim && !_punchFromGrapple && !_punchFromReady && !_punchFromTag
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (stillCrouchPunch)
            {
                // The guard eases into the cock. A crouch walk into a punch keeps its windup.
                // An air crouch into a punch keeps its ease. A slide into a punch keeps its ease.
                // A crouch into a slide keeps its ease. A crouch claim keeps its pose.
                // A crouch tag keeps its pose. Windup time is unchanged.
                _punchFromCrouch = true;
                _punchFromCrouchIn = 0f;
                _crouchPunchUaL = _upperArmL.localRotation;
                _crouchPunchUaR = _upperArmR.localRotation;
                _crouchPunchLaL = _lowerArmL.localRotation;
                _crouchPunchLaR = _lowerArmR.localRotation;
                _crouchPunchUlL = _upperLegL.localRotation;
                _crouchPunchUlR = _upperLegR.localRotation;
                _crouchPunchLlL = _lowerLegL.localRotation;
                _crouchPunchLlR = _lowerLegR.localRotation;
                _crouchPunchSp = _spine.localRotation;
                _crouchPunchHp = _hips.localRotation;
                _crouchPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromCrouch)
                _punchFromCrouchIn = Mathf.MoveTowards(_punchFromCrouchIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromCrouch = false;
            bool walkPunch = windupNow && !_punchWindWas && !fromJumpPose && grounded && _wasGrounded && !sliding && !crouch && !wallRun && !climb
                && !_dropSlide && !dartAir && !jet && !_airDashPoseWas
                && !_crouchFromStand && !_crouchWalkArmed && !_crouchFromWalk
                && speed > 0.35f && speed <= 5.5f && _skiBlend <= 0.2f && _landSquash <= 0.08f
                && _itClaim <= 0.2f && _dashReady <= 0.2f && !_tagPunchHold
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard
                && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall && !_punchFromDart
                && !_punchFromClaim && !_punchFromGrapple && !_punchFromReady && !_punchFromTag && !_punchFromCrouch
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (walkPunch)
            {
                // The walk eases into the cock. A still crouch into a punch keeps its ease.
                // A jump into a punch keeps its ease. A still crouch into a jump keeps its ease.
                // Windup time is unchanged.
                _punchFromWalk = true;
                _punchFromWalkIn = 0f;
                _walkPunchUaL = _upperArmL.localRotation;
                _walkPunchUaR = _upperArmR.localRotation;
                _walkPunchLaL = _lowerArmL.localRotation;
                _walkPunchLaR = _lowerArmR.localRotation;
                _walkPunchUlL = _upperLegL.localRotation;
                _walkPunchUlR = _upperLegR.localRotation;
                _walkPunchLlL = _lowerLegL.localRotation;
                _walkPunchLlR = _lowerLegR.localRotation;
                _walkPunchSp = _spine.localRotation;
                _walkPunchHp = _hips.localRotation;
                _walkPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromWalk)
                _punchFromWalkIn = Mathf.MoveTowards(_punchFromWalkIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromWalk = false;
            bool sprintIntoPunch = windupNow && !_punchWindWas && !fromJumpPose && grounded && _wasGrounded && !sliding && !crouch && !wallRun && !climb
                && !_dropSlide && !dartAir && !jet && !_airDashPoseWas
                && !_crouchFromStand && !_crouchWalkArmed && !_crouchFromWalk
                && speed > 5.5f && _skiBlend <= 0.2f && _landSquash <= 0.08f
                && _itClaim <= 0.2f && _dashReady <= 0.2f && !_tagPunchHold
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard
                && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall && !_punchFromDart
                && !_punchFromClaim && !_punchFromGrapple && !_punchFromReady && !_punchFromTag && !_punchFromCrouch && !_punchFromWalk
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (sprintIntoPunch)
            {
                // The run eases into the cock. A walk into a punch keeps its ease.
                // A jump into a punch keeps its ease. A walk into a tag keeps its ease.
                // Windup time is unchanged.
                _punchFromSprint = true;
                _punchFromSprintIn = 0f;
                _sprintPunchUaL = _upperArmL.localRotation;
                _sprintPunchUaR = _upperArmR.localRotation;
                _sprintPunchLaL = _lowerArmL.localRotation;
                _sprintPunchLaR = _lowerArmR.localRotation;
                _sprintPunchUlL = _upperLegL.localRotation;
                _sprintPunchUlR = _upperLegR.localRotation;
                _sprintPunchLlL = _lowerLegL.localRotation;
                _sprintPunchLlR = _lowerLegR.localRotation;
                _sprintPunchSp = _spine.localRotation;
                _sprintPunchHp = _hips.localRotation;
                _sprintPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromSprint)
                _punchFromSprintIn = Mathf.MoveTowards(_punchFromSprintIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromSprint = false;
            bool crouchWalkIntoPunch = windupNow && !_punchWindWas && !fromJumpPose && grounded && _wasGrounded
                && !sliding && !wallRun && !climb && !_dropSlide
                && !dartAir && !jet && !_airDashPoseWas && crouchWalkPose
                && !_crouchFromStand
                && speed > 0.35f && speed <= 5.5f && _skiBlend <= 0.2f && _landSquash <= 0.08f
                && _itClaim <= 0.2f && _dashReady <= 0.2f && !_tagPunchHold
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && !_punchFromJump && !_punchFromDash && !_punchFromSoft && !_punchFromHard
                && !_punchFromSki && !_punchFromSlide && !_punchFromClimb && !_punchFromWall && !_punchFromDart
                && !_punchFromClaim && !_punchFromGrapple && !_punchFromReady && !_punchFromTag && !_punchFromCrouch && !_punchFromWalk && !_punchFromSprint
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (crouchWalkIntoPunch)
            {
                // The low stride eases into the cock. A still crouch into a punch keeps its ease.
                // A walk into a punch keeps its ease. A run into a tag keeps its ease.
                // Windup time is unchanged.
                _punchFromCrouchWalk = true;
                _punchFromCrouchWalkIn = 0f;
                _crouchWalkPunchUaL = _upperArmL.localRotation;
                _crouchWalkPunchUaR = _upperArmR.localRotation;
                _crouchWalkPunchLaL = _lowerArmL.localRotation;
                _crouchWalkPunchLaR = _lowerArmR.localRotation;
                _crouchWalkPunchUlL = _upperLegL.localRotation;
                _crouchWalkPunchUlR = _upperLegR.localRotation;
                _crouchWalkPunchLlL = _lowerLegL.localRotation;
                _crouchWalkPunchLlR = _lowerLegR.localRotation;
                _crouchWalkPunchSp = _spine.localRotation;
                _crouchWalkPunchHp = _hips.localRotation;
                _crouchWalkPunchHd = _head.localRotation;
            }
            if (windupNow && _punchFromCrouchWalk)
                _punchFromCrouchWalkIn = Mathf.MoveTowards(_punchFromCrouchWalkIn, 1f, dt / 0.04f);
            else if (!windupNow)
                _punchFromCrouchWalk = false;
            _punchWindWas = windupNow;
            bool hitNow = punching && phase == PunchPhase.HitRecover;
            if (hitNow && !_tagHitWas && fromJumpPose && !crouch && !_jumpFromTag
                && _upperArmL != null && _spine != null && _upperLegL != null && _head != null)
            {
                // The apex or the landing eases into the connect. A crouch tag keeps its pose.
                // A tag into a jump keeps its push. Jump height is unchanged.
                _tagFromJump = true;
                _tagFromJumpIn = 0f;
                _tagJumpUaL = _upperArmL.localRotation;
                _tagJumpUaR = _upperArmR.localRotation;
                _tagJumpLaL = _lowerArmL.localRotation;
                _tagJumpLaR = _lowerArmR.localRotation;
                _tagJumpSp = _spine.localRotation;
                _tagJumpHp = _hips.localRotation;
                _tagJumpHd = _head.localRotation;
                _tagJumpUlL = _upperLegL.localRotation;
                _tagJumpUlR = _upperLegR.localRotation;
                _tagJumpLlL = _lowerLegL.localRotation;
                _tagJumpLlR = _lowerLegR.localRotation;
            }
            if (hitNow && _tagFromJump)
                _tagFromJumpIn = Mathf.MoveTowards(_tagFromJumpIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromJump = false;
            bool softTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash
                && !_jumpFromSoftLand && _landHard < 0.4f && _landSquash > 0.08f
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (softTag)
            {
                // The absorb eases into the connect. A jump into a tag keeps its ease.
                // A soft landing into a jump keeps its push. A hard landing keeps its pose.
                // A crouch tag keeps its pose. Connect time is unchanged. Land time is unchanged.
                _tagFromSoft = true;
                _tagFromSoftIn = 0f;
                _softTagUaL = _upperArmL.localRotation;
                _softTagUaR = _upperArmR.localRotation;
                _softTagLaL = _lowerArmL.localRotation;
                _softTagLaR = _lowerArmR.localRotation;
                _softTagUlL = _upperLegL.localRotation;
                _softTagUlR = _upperLegR.localRotation;
                _softTagLlL = _lowerLegL.localRotation;
                _softTagLlR = _lowerLegR.localRotation;
                _softTagSp = _spine.localRotation;
                _softTagHp = _hips.localRotation;
                _softTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromSoft)
                _tagFromSoftIn = Mathf.MoveTowards(_tagFromSoftIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromSoft = false;
            bool hardTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft
                && !_jumpFromHardLand && _landHard >= 0.4f && _landSquash > 0.08f
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (hardTag)
            {
                // The deeper absorb eases into the connect. A soft landing into a tag keeps its ease.
                // A hard landing into a jump keeps its push. A jump into a tag keeps its ease.
                // A crouch tag keeps its pose. Connect time is unchanged. Land time is unchanged.
                _tagFromHard = true;
                _tagFromHardIn = 0f;
                _hardTagUaL = _upperArmL.localRotation;
                _hardTagUaR = _upperArmR.localRotation;
                _hardTagLaL = _lowerArmL.localRotation;
                _hardTagLaR = _lowerArmR.localRotation;
                _hardTagUlL = _upperLegL.localRotation;
                _hardTagUlR = _upperLegR.localRotation;
                _hardTagLlL = _lowerLegL.localRotation;
                _hardTagLlR = _lowerLegR.localRotation;
                _hardTagSp = _spine.localRotation;
                _hardTagHp = _hips.localRotation;
                _hardTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromHard)
                _tagFromHardIn = Mathf.MoveTowards(_tagFromHardIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromHard = false;
            bool skiTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard
                && !_jumpFromSki && _skiBlend > 0.2f && !dartAir && !jet && !_dropSlide
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (skiTag)
            {
                // The glide eases into the connect. A ski into a punch keeps its ease.
                // A ski into a jump keeps its push. A slide into a punch keeps its ease.
                // A jump into a tag keeps its ease. A crouch tag keeps its pose.
                // Connect time is unchanged. Ski speed is unchanged.
                _tagFromSki = true;
                _tagFromSkiIn = 0f;
                _skiTagUaL = _upperArmL.localRotation;
                _skiTagUaR = _upperArmR.localRotation;
                _skiTagLaL = _lowerArmL.localRotation;
                _skiTagLaR = _lowerArmR.localRotation;
                _skiTagUlL = _upperLegL.localRotation;
                _skiTagUlR = _upperLegR.localRotation;
                _skiTagLlL = _lowerLegL.localRotation;
                _skiTagLlR = _lowerLegR.localRotation;
                _skiTagSp = _spine.localRotation;
                _skiTagHp = _hips.localRotation;
                _skiTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromSki)
                _tagFromSkiIn = Mathf.MoveTowards(_tagFromSkiIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromSki = false;
            bool slideTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki
                && !_jumpFromSlide && _dropSlide && _dropVis > 0.2f && !dartAir && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (slideTag)
            {
                // The wedge eases into the connect. A ski into a tag keeps its ease.
                // A slide into a punch keeps its ease. A slide into a jump keeps its push.
                // A jump into a tag keeps its ease. A crouch tag keeps its pose.
                // slideBoost stays 0. Connect time is unchanged.
                _tagFromSlide = true;
                _tagFromSlideIn = 0f;
                _slideTagUaL = _upperArmL.localRotation;
                _slideTagUaR = _upperArmR.localRotation;
                _slideTagLaL = _lowerArmL.localRotation;
                _slideTagLaR = _lowerArmR.localRotation;
                _slideTagUlL = _upperLegL.localRotation;
                _slideTagUlR = _upperLegR.localRotation;
                _slideTagLlL = _lowerLegL.localRotation;
                _slideTagLlR = _lowerLegR.localRotation;
                _slideTagSp = _spine.localRotation;
                _slideTagHp = _hips.localRotation;
                _slideTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromSlide)
                _tagFromSlideIn = Mathf.MoveTowards(_tagFromSlideIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromSlide = false;
            bool climbTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki && !_tagFromSlide
                && !_jumpFromClimb && (climb || climbLeave) && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (climbTag)
            {
                // The grab eases into the connect. A climb into a punch keeps its ease.
                // A climb into an air dash keeps its ease. A climb into a jump keeps its push.
                // A wall run keeps its pose. A crouch tag keeps its pose.
                // Connect time is unchanged. Exit time is unchanged.
                _tagFromClimb = true;
                _tagFromClimbIn = 0f;
                _climbTagUaL = _upperArmL.localRotation;
                _climbTagUaR = _upperArmR.localRotation;
                _climbTagLaL = _lowerArmL.localRotation;
                _climbTagLaR = _lowerArmR.localRotation;
                _climbTagUlL = _upperLegL.localRotation;
                _climbTagUlR = _upperLegR.localRotation;
                _climbTagLlL = _lowerLegL.localRotation;
                _climbTagLlR = _lowerLegR.localRotation;
                _climbTagSp = _spine.localRotation;
                _climbTagHp = _hips.localRotation;
                _climbTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromClimb)
                _tagFromClimbIn = Mathf.MoveTowards(_tagFromClimbIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromClimb = false;
            bool wallTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki && !_tagFromSlide && !_tagFromClimb
                && !_jumpFromWall && wallLeave && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (wallTag)
            {
                // The wall exit eases into the connect. A wall exit into a punch keeps its ease.
                // A wall exit into an air dash keeps its ease. A wall exit into a jump keeps its push.
                // A climb into a tag keeps its ease. A crouch tag keeps its pose.
                // Connect time is unchanged. Exit time is unchanged.
                _tagFromWall = true;
                _tagFromWallIn = 0f;
                _wallTagUaL = _upperArmL.localRotation;
                _wallTagUaR = _upperArmR.localRotation;
                _wallTagLaL = _lowerArmL.localRotation;
                _wallTagLaR = _lowerArmR.localRotation;
                _wallTagUlL = _upperLegL.localRotation;
                _wallTagUlR = _upperLegR.localRotation;
                _wallTagLlL = _lowerLegL.localRotation;
                _wallTagLlR = _lowerLegR.localRotation;
                _wallTagSp = _spine.localRotation;
                _wallTagHp = _hips.localRotation;
                _wallTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromWall)
                _tagFromWallIn = Mathf.MoveTowards(_tagFromWallIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromWall = false;
            bool dartTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall
                && !_jumpFromAirCrouch && dartAir
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (dartTag)
            {
                // The dart eases into the connect. An air crouch into a punch keeps its ease.
                // An air crouch into an air dash keeps its ease. An air crouch into a jump keeps its push.
                // A jump into a tag keeps its ease. A crouch tag keeps its pose.
                // Connect time is unchanged. Fall speed is unchanged.
                _tagFromDart = true;
                _tagFromDartIn = 0f;
                _dartTagUaL = _upperArmL.localRotation;
                _dartTagUaR = _upperArmR.localRotation;
                _dartTagLaL = _lowerArmL.localRotation;
                _dartTagLaR = _lowerArmR.localRotation;
                _dartTagUlL = _upperLegL.localRotation;
                _dartTagUlR = _upperLegR.localRotation;
                _dartTagLlL = _lowerLegL.localRotation;
                _dartTagLlR = _lowerLegR.localRotation;
                _dartTagSp = _spine.localRotation;
                _dartTagHp = _hips.localRotation;
                _dartTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromDart)
                _tagFromDartIn = Mathf.MoveTowards(_tagFromDartIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromDart = false;
            bool claimTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart
                && !_jumpFromClaim && _itClaim > 0.2f && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (claimTag)
            {
                // The claim eases into the connect. Becoming It into a punch keeps its ease.
                // Becoming It into an air dash keeps its ease. Becoming It into a jump keeps its push.
                // An air crouch into a tag keeps its ease. A crouch claim keeps its pose.
                // Connect time is unchanged. Claim time is unchanged.
                _tagFromItClaim = true;
                _tagFromItClaimIn = 0f;
                _claimTagUaL = _upperArmL.localRotation;
                _claimTagUaR = _upperArmR.localRotation;
                _claimTagLaL = _lowerArmL.localRotation;
                _claimTagLaR = _lowerArmR.localRotation;
                _claimTagUlL = _upperLegL.localRotation;
                _claimTagUlR = _upperLegR.localRotation;
                _claimTagLlL = _lowerLegL.localRotation;
                _claimTagLlR = _lowerLegR.localRotation;
                _claimTagSp = _spine.localRotation;
                _claimTagHp = _hips.localRotation;
                _claimTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromItClaim)
                _tagFromItClaimIn = Mathf.MoveTowards(_tagFromItClaimIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromItClaim = false;
            bool grappleTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart && !_tagFromItClaim
                && _grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f && !_jumpFromGrapple && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (grappleTag)
            {
                // The line eases into the connect. A grapple release into a punch keeps its ease.
                // A grapple release into an air dash keeps its ease. A grapple release into a jump keeps its push.
                // Becoming It into a tag keeps its ease. A crouch release keeps its pose.
                // Connect time is unchanged. The gate stays off.
                _tagFromGrapple = true;
                _tagFromGrappleIn = 0f;
                _grappleTagUaL = _upperArmL.localRotation;
                _grappleTagUaR = _upperArmR.localRotation;
                _grappleTagLaL = _lowerArmL.localRotation;
                _grappleTagLaR = _lowerArmR.localRotation;
                _grappleTagUlL = _upperLegL.localRotation;
                _grappleTagUlR = _upperLegR.localRotation;
                _grappleTagLlL = _lowerLegL.localRotation;
                _grappleTagLlR = _lowerLegR.localRotation;
                _grappleTagSp = _spine.localRotation;
                _grappleTagHp = _hips.localRotation;
                _grappleTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromGrapple)
                _tagFromGrappleIn = Mathf.MoveTowards(_tagFromGrappleIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromGrapple = false;
            bool readyTag = hitNow && !_tagHitWas && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart && !_tagFromItClaim && !_tagFromGrapple
                && _dashReady > 0.2f && _grapplePose <= 0.04f && !_jumpFromReady && !jet
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (readyTag)
            {
                // The pulse eases into the connect. A dash coming off cooldown into a punch keeps its ease.
                // A dash coming off cooldown into a jump keeps its push. A crouch ready keeps its pose.
                // A grapple release into a tag keeps its ease. Connect time is unchanged.
                // Duration and cooldown are unchanged.
                _tagFromReady = true;
                _tagFromReadyIn = 0f;
                _readyTagUaL = _upperArmL.localRotation;
                _readyTagUaR = _upperArmR.localRotation;
                _readyTagLaL = _lowerArmL.localRotation;
                _readyTagLaR = _lowerArmR.localRotation;
                _readyTagUlL = _upperLegL.localRotation;
                _readyTagUlR = _upperLegR.localRotation;
                _readyTagLlL = _lowerLegL.localRotation;
                _readyTagLlR = _lowerLegR.localRotation;
                _readyTagSp = _spine.localRotation;
                _readyTagHp = _hips.localRotation;
                _readyTagHd = _head.localRotation;
                _dashReady = 0f;
            }
            if (hitNow && _tagFromReady)
                _tagFromReadyIn = Mathf.MoveTowards(_tagFromReadyIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromReady = false;
            bool fromPunchPose = _punchPhaseWas == PunchPhase.Windup || _punchPhaseWas == PunchPhase.Active;
            bool punchTag = hitNow && !_tagHitWas && fromPunchPose && !fromJumpPose && !crouch && !_jumpFromTag && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart && !_tagFromItClaim && !_tagFromGrapple && !_tagFromReady
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (punchTag)
            {
                // The cock or the strike eases into the connect. A dash coming off cooldown into a tag keeps its ease.
                // A dash coming off cooldown into a punch keeps its ease. A crouch tag keeps its pose.
                // A tag into a jump keeps its push. Connect time is unchanged. Windup time is unchanged.
                _tagFromPunch = true;
                _tagFromPunchIn = 0f;
                _punchTagUaL = _upperArmL.localRotation;
                _punchTagUaR = _upperArmR.localRotation;
                _punchTagLaL = _lowerArmL.localRotation;
                _punchTagLaR = _lowerArmR.localRotation;
                _punchTagUlL = _upperLegL.localRotation;
                _punchTagUlR = _upperLegR.localRotation;
                _punchTagLlL = _lowerLegL.localRotation;
                _punchTagLlR = _lowerLegR.localRotation;
                _punchTagSp = _spine.localRotation;
                _punchTagHp = _hips.localRotation;
                _punchTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromPunch)
                _tagFromPunchIn = Mathf.MoveTowards(_tagFromPunchIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromPunch = false;
            bool missTag = hitNow && !_tagHitWas && _punchPhaseWas == PunchPhase.MissRecover && !fromJumpPose && !crouch && !_jumpFromTag && !_jumpFromMiss && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart && !_tagFromItClaim && !_tagFromGrapple && !_tagFromReady && !_tagFromPunch
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (missTag)
            {
                // The whiff eases into the connect. A tag into a punch keeps its ease.
                // A punch into a tag keeps its ease. A punch miss into a jump keeps its push.
                // A punch miss into an air dash keeps its ease. A crouch miss keeps its pose.
                // Connect time is unchanged.
                _tagFromMiss = true;
                _tagFromMissIn = 0f;
                _missTagUaL = _upperArmL.localRotation;
                _missTagUaR = _upperArmR.localRotation;
                _missTagLaL = _lowerArmL.localRotation;
                _missTagLaR = _lowerArmR.localRotation;
                _missTagUlL = _upperLegL.localRotation;
                _missTagUlR = _upperLegR.localRotation;
                _missTagLlL = _lowerLegL.localRotation;
                _missTagLlR = _lowerLegR.localRotation;
                _missTagSp = _spine.localRotation;
                _missTagHp = _hips.localRotation;
                _missTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromMiss)
                _tagFromMissIn = Mathf.MoveTowards(_tagFromMissIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromMiss = false;
            bool stillCrouchTag = hitNow && !_tagHitWas && !fromJumpPose && crouch && grounded && !sliding && !_dropSlide
                && !dartAir && !jet && !_airDashPoseWas && !_jumpFromTag
                && _crouchFromStand && !_crouchWalkArmed && speed <= 0.35f && _dropVis > 0.2f
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && _punchPhaseWas != PunchPhase.MissRecover
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard
                && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart
                && !_tagFromItClaim && !_tagFromGrapple && !_tagFromReady && !_tagFromPunch && !_tagFromMiss
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (stillCrouchTag)
            {
                // The guard eases into the connect. A still crouch into a punch keeps its ease.
                // A crouch walk into a tag keeps its pose. An air crouch into a tag keeps its ease.
                // A slide into a tag keeps its ease. A crouch claim keeps its pose.
                // A punch into a tag keeps its ease. Connect time is unchanged.
                _tagFromCrouch = true;
                _tagFromCrouchIn = 0f;
                _crouchTagUaL = _upperArmL.localRotation;
                _crouchTagUaR = _upperArmR.localRotation;
                _crouchTagLaL = _lowerArmL.localRotation;
                _crouchTagLaR = _lowerArmR.localRotation;
                _crouchTagUlL = _upperLegL.localRotation;
                _crouchTagUlR = _upperLegR.localRotation;
                _crouchTagLlL = _lowerLegL.localRotation;
                _crouchTagLlR = _lowerLegR.localRotation;
                _crouchTagSp = _spine.localRotation;
                _crouchTagHp = _hips.localRotation;
                _crouchTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromCrouch)
                _tagFromCrouchIn = Mathf.MoveTowards(_tagFromCrouchIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromCrouch = false;
            bool walkIntoTag = hitNow && !_tagHitWas && !fromJumpPose && !fromPunchPose && grounded && _wasGrounded
                && !crouch && !sliding && !wallRun && !climb && !_dropSlide
                && !dartAir && !jet && !_airDashPoseWas && !_jumpFromTag
                && !_crouchFromStand && !_crouchWalkArmed && !_crouchFromWalk
                && speed > 0.35f && speed <= 5.5f && _skiBlend <= 0.2f && _landSquash <= 0.08f
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && _punchPhaseWas != PunchPhase.MissRecover
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard
                && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart
                && !_tagFromItClaim && !_tagFromGrapple && !_tagFromReady && !_tagFromPunch && !_tagFromMiss && !_tagFromCrouch
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (walkIntoTag)
            {
                // The walk eases into the connect. A walk into a punch keeps its ease.
                // A jump into a tag keeps its ease. A still crouch into a tag keeps its ease.
                // Connect time is unchanged.
                _tagFromWalk = true;
                _tagFromWalkIn = 0f;
                _walkTagUaL = _upperArmL.localRotation;
                _walkTagUaR = _upperArmR.localRotation;
                _walkTagLaL = _lowerArmL.localRotation;
                _walkTagLaR = _lowerArmR.localRotation;
                _walkTagUlL = _upperLegL.localRotation;
                _walkTagUlR = _upperLegR.localRotation;
                _walkTagLlL = _lowerLegL.localRotation;
                _walkTagLlR = _lowerLegR.localRotation;
                _walkTagSp = _spine.localRotation;
                _walkTagHp = _hips.localRotation;
                _walkTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromWalk)
                _tagFromWalkIn = Mathf.MoveTowards(_tagFromWalkIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromWalk = false;
            bool sprintIntoTag = hitNow && !_tagHitWas && !fromJumpPose && !fromPunchPose && grounded && _wasGrounded
                && !crouch && !sliding && !wallRun && !climb && !_dropSlide
                && !dartAir && !jet && !_airDashPoseWas && !_jumpFromTag
                && !_crouchFromStand && !_crouchWalkArmed && !_crouchFromWalk
                && speed > 5.5f && _skiBlend <= 0.2f && _landSquash <= 0.08f
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && _punchPhaseWas != PunchPhase.MissRecover
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard
                && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart
                && !_tagFromItClaim && !_tagFromGrapple && !_tagFromReady && !_tagFromPunch && !_tagFromMiss && !_tagFromCrouch && !_tagFromWalk
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (sprintIntoTag)
            {
                // The run eases into the connect. A run into a punch keeps its ease.
                // A walk into a tag keeps its ease. A jump into a tag keeps its ease.
                // Connect time is unchanged.
                _tagFromSprint = true;
                _tagFromSprintIn = 0f;
                _sprintTagUaL = _upperArmL.localRotation;
                _sprintTagUaR = _upperArmR.localRotation;
                _sprintTagLaL = _lowerArmL.localRotation;
                _sprintTagLaR = _lowerArmR.localRotation;
                _sprintTagUlL = _upperLegL.localRotation;
                _sprintTagUlR = _upperLegR.localRotation;
                _sprintTagLlL = _lowerLegL.localRotation;
                _sprintTagLlR = _lowerLegR.localRotation;
                _sprintTagSp = _spine.localRotation;
                _sprintTagHp = _hips.localRotation;
                _sprintTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromSprint)
                _tagFromSprintIn = Mathf.MoveTowards(_tagFromSprintIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromSprint = false;
            bool crouchWalkIntoTag = hitNow && !_tagHitWas && !fromJumpPose && !fromPunchPose && grounded && _wasGrounded
                && !sliding && !wallRun && !climb && !_dropSlide
                && !dartAir && !jet && !_airDashPoseWas && crouchWalkPose && !_crouchFromStand
                && speed > 0.35f && speed <= 5.5f && _skiBlend <= 0.2f && _landSquash <= 0.08f
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && _punchPhaseWas != PunchPhase.MissRecover
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && !_tagFromJump && !_tagFromDash && !_tagFromSoft && !_tagFromHard
                && !_tagFromSki && !_tagFromSlide && !_tagFromClimb && !_tagFromWall && !_tagFromDart
                && !_tagFromItClaim && !_tagFromGrapple && !_tagFromReady && !_tagFromPunch && !_tagFromMiss && !_tagFromCrouch && !_tagFromWalk && !_tagFromSprint
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (crouchWalkIntoTag)
            {
                // The low stride eases into the connect. A crouch walk into a punch keeps its ease.
                // A run into a tag keeps its ease. A still crouch into a tag keeps its ease.
                // Connect time is unchanged.
                _tagFromCrouchWalk = true;
                _tagFromCrouchWalkIn = 0f;
                _crouchWalkTagUaL = _upperArmL.localRotation;
                _crouchWalkTagUaR = _upperArmR.localRotation;
                _crouchWalkTagLaL = _lowerArmL.localRotation;
                _crouchWalkTagLaR = _lowerArmR.localRotation;
                _crouchWalkTagUlL = _upperLegL.localRotation;
                _crouchWalkTagUlR = _upperLegR.localRotation;
                _crouchWalkTagLlL = _lowerLegL.localRotation;
                _crouchWalkTagLlR = _lowerLegR.localRotation;
                _crouchWalkTagSp = _spine.localRotation;
                _crouchWalkTagHp = _hips.localRotation;
                _crouchWalkTagHd = _head.localRotation;
            }
            if (hitNow && _tagFromCrouchWalk)
                _tagFromCrouchWalkIn = Mathf.MoveTowards(_tagFromCrouchWalkIn, 1f, dt / 0.04f);
            else if (!hitNow)
                _tagFromCrouchWalk = false;
            bool tagFell = _tagHitWas && !hitNow && !crouch && !_jumpFromTag
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (tagFell)
            {
                // The next cock can ease out of this connect. A punch into a tag keeps its ease.
                // A crouch tag keeps its pose. A tag into a jump keeps its push.
                _tagPunchHold = true;
                _tagPunchUaL = _upperArmL.localRotation;
                _tagPunchUaR = _upperArmR.localRotation;
                _tagPunchLaL = _lowerArmL.localRotation;
                _tagPunchLaR = _lowerArmR.localRotation;
                _tagPunchUlL = _upperLegL.localRotation;
                _tagPunchUlR = _upperLegR.localRotation;
                _tagPunchLlL = _lowerLegL.localRotation;
                _tagPunchLlR = _lowerLegR.localRotation;
                _tagPunchSp = _spine.localRotation;
                _tagPunchHp = _hips.localRotation;
                _tagPunchHd = _head.localRotation;
            }
            else if (!windupNow)
                _tagPunchHold = false;
            bool punchIntoStill = inStill && fromPunchPose
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !_crouchFromJump && !_crouchFromDash && !_diveFromJump
                && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (punchIntoStill)
            {
                // The punch eases into the guard. A slide into a still crouch keeps its ease.
                // A ski into a still crouch keeps its ease. A tag into a still crouch keeps its ease.
                // Windup time is unchanged.
                _stillFromPunch = true;
                _stillFromPunchIn = 0f;
                _punchGuardUaL = _upperArmL.localRotation;
                _punchGuardUaR = _upperArmR.localRotation;
                _punchGuardLaL = _lowerArmL.localRotation;
                _punchGuardLaR = _lowerArmR.localRotation;
                _punchGuardUlL = _upperLegL.localRotation;
                _punchGuardUlR = _upperLegR.localRotation;
                _punchGuardLlL = _lowerLegL.localRotation;
                _punchGuardLlR = _lowerLegR.localRotation;
                _punchGuardSp = _spine.localRotation;
                _punchGuardHp = _hips.localRotation;
                _punchGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromPunch = false;
            else if (_stillFromPunch)
                _stillFromPunchIn = Mathf.MoveTowards(_stillFromPunchIn, 1f, dt / 0.04f);
            bool tagIntoStill = inStill && _punchPhaseWas == PunchPhase.HitRecover
                && phase != PunchPhase.HitRecover
                && phase != PunchPhase.Windup && phase != PunchPhase.Active && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (tagIntoStill)
            {
                // The connect eases into the guard. A punch into a still crouch keeps its ease.
                // A slide into a still crouch keeps its ease. A whiff into a still crouch keeps its ease.
                // Connect time is unchanged.
                _stillFromTag = true;
                _stillFromTagIn = 0f;
                _tagGuardUaL = _upperArmL.localRotation;
                _tagGuardUaR = _upperArmR.localRotation;
                _tagGuardLaL = _lowerArmL.localRotation;
                _tagGuardLaR = _lowerArmR.localRotation;
                _tagGuardUlL = _upperLegL.localRotation;
                _tagGuardUlR = _upperLegR.localRotation;
                _tagGuardLlL = _lowerLegL.localRotation;
                _tagGuardLlR = _lowerLegR.localRotation;
                _tagGuardSp = _spine.localRotation;
                _tagGuardHp = _hips.localRotation;
                _tagGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromTag = false;
            else if (_stillFromTag)
                _stillFromTagIn = Mathf.MoveTowards(_stillFromTagIn, 1f, dt / 0.04f);
            bool missIntoStill = inStill && _punchPhaseWas == PunchPhase.MissRecover
                && phase != PunchPhase.MissRecover
                && phase != PunchPhase.Windup && phase != PunchPhase.Active && phase != PunchPhase.HitRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (missIntoStill)
            {
                // The whiff eases into the guard. A tag into a still crouch keeps its ease.
                // A punch into a still crouch keeps its ease. A crouch walk keeps its slow blend.
                // Whiff time is unchanged.
                _stillFromMiss = true;
                _stillFromMissIn = 0f;
                _missGuardUaL = _upperArmL.localRotation;
                _missGuardUaR = _upperArmR.localRotation;
                _missGuardLaL = _lowerArmL.localRotation;
                _missGuardLaR = _lowerArmR.localRotation;
                _missGuardUlL = _upperLegL.localRotation;
                _missGuardUlR = _upperLegR.localRotation;
                _missGuardLlL = _lowerLegL.localRotation;
                _missGuardLlR = _lowerLegR.localRotation;
                _missGuardSp = _spine.localRotation;
                _missGuardHp = _hips.localRotation;
                _missGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromMiss = false;
            else if (_stillFromMiss)
                _stillFromMissIn = Mathf.MoveTowards(_stillFromMissIn, 1f, dt / 0.04f);
            if (!_claimWas || !inStill)
                _claimStillHeld = false;
            bool claimIntoStill = inStill && _claimWas && !_claimStillHeld && !_jumpFromClaim
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (claimIntoStill)
            {
                // The claim eases into the guard. A whiff into a still crouch keeps its ease.
                // A tag into a still crouch keeps its ease. A crouch walk keeps its slow blend.
                // Claim time is unchanged.
                _stillFromClaim = true;
                _stillFromClaimIn = 0f;
                _claimStillHeld = true;
                _claimGuardUaL = _upperArmL.localRotation;
                _claimGuardUaR = _upperArmR.localRotation;
                _claimGuardLaL = _lowerArmL.localRotation;
                _claimGuardLaR = _lowerArmR.localRotation;
                _claimGuardUlL = _upperLegL.localRotation;
                _claimGuardUlR = _upperLegR.localRotation;
                _claimGuardLlL = _lowerLegL.localRotation;
                _claimGuardLlR = _lowerLegR.localRotation;
                _claimGuardSp = _spine.localRotation;
                _claimGuardHp = _hips.localRotation;
                _claimGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromClaim = false;
            else if (_stillFromClaim)
                _stillFromClaimIn = Mathf.MoveTowards(_stillFromClaimIn, 1f, dt / 0.04f);
            if (!_readyWas || !inStill)
                _readyStillHeld = false;
            bool readyIntoStill = inStill && _readyWas && !_readyStillHeld
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (readyIntoStill)
            {
                // The pulse eases into the guard. An It claim into a still crouch keeps its ease.
                // A whiff into a still crouch keeps its ease. A crouch walk keeps its pulse.
                // Duration and cooldown are unchanged.
                _stillFromReady = true;
                _stillFromReadyIn = 0f;
                _readyStillHeld = true;
                _readyGuardUaL = _upperArmL.localRotation;
                _readyGuardUaR = _upperArmR.localRotation;
                _readyGuardLaL = _lowerArmL.localRotation;
                _readyGuardLaR = _lowerArmR.localRotation;
                _readyGuardUlL = _upperLegL.localRotation;
                _readyGuardUlR = _upperLegR.localRotation;
                _readyGuardLlL = _lowerLegL.localRotation;
                _readyGuardLlR = _lowerLegR.localRotation;
                _readyGuardSp = _spine.localRotation;
                _readyGuardHp = _hips.localRotation;
                _readyGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromReady = false;
            else if (_stillFromReady)
                _stillFromReadyIn = Mathf.MoveTowards(_stillFromReadyIn, 1f, dt / 0.04f);
            if (!_grappleReleaseWas || !inStill)
                _grappleStillHeld = false;
            bool grappleIntoStill = inStill && _grappleReleaseWas && !_grappleStillHeld && !_jumpFromGrapple
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (grappleIntoStill)
            {
                // The line eases into the guard. A dash ready into a still crouch keeps its ease.
                // An It claim into a still crouch keeps its ease. A crouch walk keeps its slow leave.
                // The gate stays off.
                _stillFromGrapple = true;
                _stillFromGrappleIn = 0f;
                _grappleStillHeld = true;
                _grappleGuardUaL = _upperArmL.localRotation;
                _grappleGuardUaR = _upperArmR.localRotation;
                _grappleGuardLaL = _lowerArmL.localRotation;
                _grappleGuardLaR = _lowerArmR.localRotation;
                _grappleGuardUlL = _upperLegL.localRotation;
                _grappleGuardUlR = _upperLegR.localRotation;
                _grappleGuardLlL = _lowerLegL.localRotation;
                _grappleGuardLlR = _lowerLegR.localRotation;
                _grappleGuardSp = _spine.localRotation;
                _grappleGuardHp = _hips.localRotation;
                _grappleGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromGrapple = false;
            else if (_stillFromGrapple)
                _stillFromGrappleIn = Mathf.MoveTowards(_stillFromGrappleIn, 1f, dt / 0.04f);
            if (!_hardLandWas || !inStill)
                _hardStillHeld = false;
            bool hardIntoStill = inStill && _hardLandWas && !_hardStillHeld && !_stillFromDart
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (hardIntoStill)
            {
                // The absorb eases into the guard. A grapple release into a still crouch keeps its ease.
                // A dash ready into a still crouch keeps its ease. A soft landing keeps its ease.
                // Land time is unchanged.
                _stillFromHard = true;
                _stillFromHardIn = 0f;
                _hardStillHeld = true;
                _hardGuardUaL = _upperArmL.localRotation;
                _hardGuardUaR = _upperArmR.localRotation;
                _hardGuardLaL = _lowerArmL.localRotation;
                _hardGuardLaR = _lowerArmR.localRotation;
                _hardGuardUlL = _upperLegL.localRotation;
                _hardGuardUlR = _upperLegR.localRotation;
                _hardGuardLlL = _lowerLegL.localRotation;
                _hardGuardLlR = _lowerLegR.localRotation;
                _hardGuardSp = _spine.localRotation;
                _hardGuardHp = _hips.localRotation;
                _hardGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromHard = false;
            else if (_stillFromHard)
                _stillFromHardIn = Mathf.MoveTowards(_stillFromHardIn, 1f, dt / 0.04f);
            if (!_softLandWas || !inStill)
                _softStillHeld = false;
            bool softIntoStill = inStill && _softLandWas && !_softStillHeld && !_stillFromDart
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (softIntoStill)
            {
                // The absorb eases into the guard. A hard land into a still crouch keeps its ease.
                // A grapple release into a still crouch keeps its ease. A crouch walk keeps its absorb.
                // Land time is unchanged.
                _stillFromSoft = true;
                _stillFromSoftIn = 0f;
                _softStillHeld = true;
                _softGuardUaL = _upperArmL.localRotation;
                _softGuardUaR = _upperArmR.localRotation;
                _softGuardLaL = _lowerArmL.localRotation;
                _softGuardLaR = _lowerArmR.localRotation;
                _softGuardUlL = _upperLegL.localRotation;
                _softGuardUlR = _upperLegR.localRotation;
                _softGuardLlL = _lowerLegL.localRotation;
                _softGuardLlR = _lowerLegR.localRotation;
                _softGuardSp = _spine.localRotation;
                _softGuardHp = _hips.localRotation;
                _softGuardHd = _head.localRotation;
            }
            if (!inStill)
                _stillFromSoft = false;
            else if (_stillFromSoft)
                _stillFromSoftIn = Mathf.MoveTowards(_stillFromSoftIn, 1f, dt / 0.04f);
            if (!_dartStillWas || !inStill || !grounded)
                _dartStillHeld = false;
            bool dartIntoStill = inStill && grounded && _dartStillWas && !_dartStillHeld
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow && !wallRun && !climb
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (dartIntoStill)
            {
                // The dart eases into the guard. A soft land into a still crouch keeps its ease.
                // A hard land into a still crouch keeps its ease. A moving air crouch keeps the flare.
                // Fall speed is unchanged.
                _stillFromDart = true;
                _stillFromDartIn = 0f;
                _dartStillHeld = true;
                _dartGuardUaL = _upperArmL.localRotation;
                _dartGuardUaR = _upperArmR.localRotation;
                _dartGuardLaL = _lowerArmL.localRotation;
                _dartGuardLaR = _lowerArmR.localRotation;
                _dartGuardUlL = _upperLegL.localRotation;
                _dartGuardUlR = _upperLegR.localRotation;
                _dartGuardLlL = _lowerLegL.localRotation;
                _dartGuardLlR = _lowerLegR.localRotation;
                _dartGuardSp = _spine.localRotation;
                _dartGuardHp = _hips.localRotation;
                _dartGuardHd = _head.localRotation;
            }
            if (!inStill || !grounded)
                _stillFromDart = false;
            else if (_stillFromDart)
                _stillFromDartIn = Mathf.MoveTowards(_stillFromDartIn, 1f, dt / 0.04f);
            if (!leavingSurf || !inStill)
                _climbStillHeld = false;
            bool climbIntoStill = inStill && leavingSurf && !_exitFromWall && !_climbStillHeld
                && !wallRun && !climb && !skiing
                && !_jumpFromClimb && !_dashFromClimb && !_skiFromClimb && !_slideFromClimb
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (climbIntoStill)
            {
                // The grab eases into the guard. An air crouch into a still crouch keeps its ease.
                // A wall run into a still crouch keeps its leave. A crouch walk keeps its low stride.
                // Exit time is unchanged.
                _stillFromClimb = true;
                _stillFromClimbIn = 0f;
                _climbStillHeld = true;
                _climbGuardUaL = _upperArmL.localRotation;
                _climbGuardUaR = _upperArmR.localRotation;
                _climbGuardLaL = _lowerArmL.localRotation;
                _climbGuardLaR = _lowerArmR.localRotation;
                _climbGuardUlL = _upperLegL.localRotation;
                _climbGuardUlR = _upperLegR.localRotation;
                _climbGuardLlL = _lowerLegL.localRotation;
                _climbGuardLlR = _lowerLegR.localRotation;
                _climbGuardSp = _spine.localRotation;
                _climbGuardHp = _hips.localRotation;
                _climbGuardHd = _head.localRotation;
            }
            if (!inStill || (air && _diveVis > 0.2f) || _jumpFromClimb)
                _stillFromClimb = false;
            else if (_stillFromClimb)
                _stillFromClimbIn = Mathf.MoveTowards(_stillFromClimbIn, 1f, dt / 0.04f);
            if (!leavingSurf || !inStill)
                _wallStillHeld = false;
            bool wallIntoStill = inStill && leavingSurf && _exitFromWall && !_wallStillHeld
                && !wallRun && !climb && !skiing
                && !_jumpFromWall && !_dashFromWall && !_skiFromWall && !_slideFromWall
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (wallIntoStill)
            {
                // The leave eases into the guard. A climb into a still crouch keeps its ease.
                // An air crouch into a still crouch keeps its ease. A crouch walk keeps its low stride.
                // Exit time is unchanged.
                _stillFromWall = true;
                _stillFromWallIn = 0f;
                _wallStillHeld = true;
                _wallGuardUaL = _upperArmL.localRotation;
                _wallGuardUaR = _upperArmR.localRotation;
                _wallGuardLaL = _lowerArmL.localRotation;
                _wallGuardLaR = _lowerArmR.localRotation;
                _wallGuardUlL = _upperLegL.localRotation;
                _wallGuardUlR = _upperLegR.localRotation;
                _wallGuardLlL = _lowerLegL.localRotation;
                _wallGuardLlR = _lowerLegR.localRotation;
                _wallGuardSp = _spine.localRotation;
                _wallGuardHp = _hips.localRotation;
                _wallGuardHd = _head.localRotation;
            }
            if (!inStill || (air && _diveVis > 0.2f) || _jumpFromWall)
                _stillFromWall = false;
            else if (_stillFromWall)
                _stillFromWallIn = Mathf.MoveTowards(_stillFromWallIn, 1f, dt / 0.04f);
            if (!inStill || !crouchWalkPose)
                _crouchWalkStillHeld = false;
            bool crouchWalkIntoStill = enterStill && crouchWalkPose && !_crouchWalkStillHeld
                && !skiing && !wallRun && !climb && !leavingSurf
                && !_jumpFromCrouchWalk && !_dashFromCrouchWalk && !_skiFromCrouchWalk
                && !_punchFromCrouchWalk && !_tagFromCrouchWalk
                && phase != PunchPhase.Windup && phase != PunchPhase.Active
                && phase != PunchPhase.HitRecover && phase != PunchPhase.MissRecover
                && !dashPoseNow
                && _itClaim <= 0.2f && _dashReady <= 0.2f
                && !(_grapple != null && !_grapple.IsPulling && _grapplePose > 0.2f)
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null;
            if (crouchWalkIntoStill)
            {
                // The low stride eases into the guard. A wall exit into a still crouch keeps its ease.
                // A climb into a still crouch keeps its ease. The stride is unchanged.
                _stillFromCrouchWalk = true;
                _stillFromCrouchWalkIn = 0f;
                _crouchWalkStillHeld = true;
                _crouchWalkGuardUaL = _upperArmL.localRotation;
                _crouchWalkGuardUaR = _upperArmR.localRotation;
                _crouchWalkGuardLaL = _lowerArmL.localRotation;
                _crouchWalkGuardLaR = _lowerArmR.localRotation;
                _crouchWalkGuardUlL = _upperLegL.localRotation;
                _crouchWalkGuardUlR = _upperLegR.localRotation;
                _crouchWalkGuardLlL = _lowerLegL.localRotation;
                _crouchWalkGuardLlR = _lowerLegR.localRotation;
                _crouchWalkGuardSp = _spine.localRotation;
                _crouchWalkGuardHp = _hips.localRotation;
                _crouchWalkGuardHd = _head.localRotation;
            }
            if (!inStill || (air && _diveVis > 0.2f))
                _stillFromCrouchWalk = false;
            else if (_stillFromCrouchWalk)
                _stillFromCrouchWalkIn = Mathf.MoveTowards(_stillFromCrouchWalkIn, 1f, dt / 0.04f);
            _punchPhaseWas = phase;
            _tagHitWas = hitNow;
            // Find the tuck, then the look trail. Look speed is unchanged.
            if (air)
                _airArmIn = Mathf.MoveTowards(_airArmIn, 1f, dt / 0.18f);
            else
                _airArmIn = 1f;
            _wasGrounded = grounded;
            _prevSpeed = speed;
            if (_landHold > 0f)
                _landHold = Mathf.Max(0f, _landHold - dt);
            else
            {
                // ~0.4s from a full buckle back to the stride.
                _landSquash = Mathf.MoveTowards(_landSquash, 0f, dt * 3.1f);
            }
            // Bible WallBounce ~0.22s kick flash - brief TP limb tell after OnWallBounced.
            _bouncePulse = Mathf.MoveTowards(_bouncePulse, 0f, dt / 0.22f);
            bool bouncing = _bouncePulse > 0.04f;
            float bounceAmt = Mathf.Clamp01(_bouncePulse);
            // Bible SuperGlide ~0.28s flat body + crouch hips - TP launch tell.
            _glidePulse = Mathf.MoveTowards(_glidePulse, 0f, dt / 0.28f);
            bool gliding = _glidePulse > 0.04f;
            float glideAmt = Mathf.Clamp01(_glidePulse);

            bool airDashing = _motor != null && _motor.IsAirDashing;
            _tagFlinch = Mathf.MoveTowards(_tagFlinch, 0f, dt / 0.45f);
            _itClaim = Mathf.MoveTowards(_itClaim, 0f, dt / 0.52f);
            _claimWas = _itClaim > 0.2f;
            bool dashing = _dashPulse > 0.04f || lunging || airDashing;
            float dashAmt = Mathf.Max(
                Mathf.Clamp01(_dashPulse),
                lunging && _motor != null ? _motor.LungeProgress : 0f,
                airDashing && _motor != null ? _motor.AirDashProgress : 0f);
            float flinchAmt = Mathf.Clamp01(_tagFlinch);
            float claimAmt = Mathf.Clamp01(_itClaim);

            float walkAmt = Mathf.Clamp01(speed / 5.5f);
            float runAmt = Mathf.InverseLerp(5.5f, 11.5f, speed);
            // A still crouch in the air uses the guard. The fall dart stays as it is.
            // A moving crouch eases that fall into the low stride. A still crouch keeps the dart.
            // After an air dash, a still crouch keeps that guard. Jump height is unchanged.
            bool airStillCrouch = air && !jet && !airDashing && !_crouchFromJump
                && speed <= 0.35f && _diveVis <= 0.02f
                && _input != null && _input.CrouchHeld;
            bool airCrouchWalk = air && !jet && !airDashing && (_airDashArms || _armRecover > 0f)
                && speed > 0.35f && st != MoveState.Sprint && runAmt <= 0.4f && _diveVis <= 0.02f
                && _input != null && _input.CrouchHeld;
            bool airDartWalk = air && !jet && !airDashing && !(_airDashArms || _armRecover > 0f)
                && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint && runAmt <= 0.4f
                && _diveVis > 0.02f
                && _input != null && _input.CrouchHeld;
            // Keep a soft air/vault cycle so limbs stay energetic off the ground.
            // Walk and sprint ease length and tempo. The cycle keeps advancing, so a plant does not freeze.
            if (airDashing)
            {
                // The burst leads with the left thigh. Hold the stride there so the
                // landing does not skate onto the other foot. Dash time is unchanged.
                _runVis = runAmt;
                _cycle = Mathf.PI * 0.5f;
                _stopGait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                _stopRun = _runVis;
            }
            else if (grounded && speed > 0.35f && !sliding && !crouch)
            {
                // A sprint into a walk closes the stride with the step. Snapping the length skates.
                // Opening into a sprint stays on the shorter ease. Speed is unchanged.
                float runStep = runAmt < _runVis ? dt / 0.32f : dt / 0.2f;
                _runVis = Mathf.MoveTowards(_runVis, runAmt, runStep);
                float cadence = Mathf.Lerp(7.2f, 11.2f, _runVis);
                // A ski into a walk keeps the walk step. A ski into a run keeps the run step.
                float rate = Mathf.Lerp(cadence, 5.2f, (_walkFromSki || _runFromSki) ? 0f : legSki);
                _cycle += dt * rate;
                _stopGait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                _stopRun = _runVis;
            }
            else if (sliding)
            {
                // Keep the stride that entered the slide. Closing it skates the exit.
                // slideBoost stays 0. Speed is unchanged.
                _stopGait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                _stopRun = _runVis;
            }
            else if (air && !jet)
            {
                if (_armRecover > 0f)
                {
                    // Stay on the dash lead until the feet are back on the ground.
                    _runVis = runAmt;
                    _cycle = Mathf.PI * 0.5f;
                }
                else
                {
                    _runVis = runAmt;
                    _cycle += dt * Mathf.Lerp(5.5f, 9f, runAmt);
                }
            }
            else if (crouch && grounded && speed > 0.35f)
            {
                // Short shuffle under the hips. The guard stays low. Speed is unchanged.
                _cycle += dt * 6.2f;
                _runVis = runAmt;
            }
            else if (!jet)
            {
                // Close onto a stride where the sine is 0. Rounding to an integer left a leg stuck out,
                // which read as a skate stop. Speed is unchanged.
                float plant = Mathf.PI * Mathf.Round(_cycle / Mathf.PI);
                _cycle = Mathf.MoveTowards(_cycle, plant, dt * 4.2f);
                if (!sliding && !crouch)
                {
                    _stopGait = Mathf.MoveTowards(_stopGait, 0f, dt / 0.28f);
                    _runVis = Mathf.MoveTowards(_runVis, 0f, dt / 0.28f);
                    _stopRun = _runVis;
                }
                else
                    _runVis = runAmt;
            }
            else
                _runVis = runAmt;

            bool stepping = grounded && speed > 0.35f && !sliding && !crouch;
            // Walk into a sprint pushes off the back foot, then the stride opens.
            // Speed is unchanged. An idle start still uses its own plant.
            if (stepping && !air && !dashing && !_airDashArms && runAmt > 0.4f && _prevRunAmt < 0.2f && _runVis < 0.35f)
            {
                _sprintIn = 0f;
                // A walk turn keeps the outside foot down, then the sprint opens.
                _sprintFromTurn = Mathf.Abs(_turnVis) > 0.18f;
                _sprintOutLeft = _turnVis > 0f;
            }
            _prevRunAmt = runAmt;
            if (_sprintIn < 1f)
                _sprintIn = Mathf.MoveTowards(_sprintIn, 1f, dt / 0.32f);
            if (air)
                _stepIn = 1f;
            else if (stepping)
                _stepIn = Mathf.MoveTowards(_stepIn, 1f, dt / 0.32f);
            else if (crouch)
            {
                float remain = Mathf.Abs(_cycle - Mathf.PI * Mathf.Round(_cycle / Mathf.PI));
                if (remain < 0.25f)
                    _stepIn = Mathf.MoveTowards(_stepIn, 0f, dt / 0.12f);
            }
            else if (speed <= 0.35f)
            {
                float remain = Mathf.Abs(_cycle - Mathf.PI * Mathf.Round(_cycle / Mathf.PI));
                if (remain < 0.25f)
                    _stepIn = Mathf.MoveTowards(_stepIn, 0f, dt / 0.12f);
            }

            // Hold the plant and the lift, then cross zero faster - a sine reads as skating.
            // The first step uses the raw sine so it pushes off the plant instead of skating.
            float sinRaw = Mathf.Sin(_cycle);
            float sinShaped = Mathf.Sign(sinRaw) * Mathf.Pow(Mathf.Abs(sinRaw), 0.40f);
            float sinC = Mathf.Lerp(sinRaw, sinShaped, stepping ? Mathf.SmoothStep(0f, 1f, _stepIn) : 1f);
            float breath = Mathf.Sin(Time.time * 2.1f) * 2.4f;
            float punchProg = _punch != null ? _punch.PhaseProgress : 0f;

            // Spine / hips lean by state - jet reads clearly in TP
            float leanX = lunging || dashing ? Mathf.Lerp(28f, 48f, dashAmt) : jet ? -22f : wallRun ? 22f : climb ? -16f : mantle ? Mathf.Lerp(42f, 22f, _motor != null ? _motor.MantleProgress : 0.5f) : air ? 18f : breath;
            float leanZ = wallRun ? (_motor != null && _motor.WallLeft ? 32f : -32f) : 0f;
            float idleW = 0f;
            bool atRest = grounded && !dashing && !sliding && !crouch && !jet && !wallRun && !climb && !mantle && !air && !lunging && flinchAmt < 0.04f && claimAmt < 0.04f;
            if (atRest)
                idleW = 1f - Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
            // After the feet close, the last hip roll eases into the idle sway.
            // A clock sine pops the hips. Holding them flat until the sway starts reads as a freeze.
            float strideRemain = Mathf.Abs(_cycle - Mathf.PI * Mathf.Round(_cycle / Mathf.PI));
            bool stopping = atRest && speed <= 0.35f;
            if (!stopping)
            {
                _stopPlanted = false;
                _stopPlant = 0f;
            }
            else if (!_stopPlanted)
            {
                // The back foot is the one that stays. The front foot finishes the close.
                _stopPlanted = true;
                _stopPlantLeft = sinC < 0f;
            }
            if (stopping)
                _stopPlant = Mathf.MoveTowards(_stopPlant, 1f, dt / 0.12f);
            float closeRoll = 0f;
            if (stopping && !_swayIdle)
                closeRoll = sinC * Mathf.Lerp(3.2f, 5.5f, _stopRun) * Mathf.Max(_stopGait, Mathf.Clamp01(strideRemain / 0.55f));
            if (!stopping)
                _swayIdle = false;
            else if (!_swayIdle && strideRemain < 0.22f)
            {
                _swayIdle = true;
                float n = Mathf.Clamp(_swayVis / 5f, -1f, 1f);
                float a = Mathf.Asin(n);
                // Leave a peak toward center so the idle sway continues the settle.
                _idlePhase = n >= 0f ? Mathf.PI - a : a;
            }
            if (_swayIdle)
                _idlePhase += dt * 0.8f;
            float swayTarget = !atRest ? 0f : _swayIdle ? Mathf.Sin(_idlePhase) * 5f : closeRoll;
            _swayVis = Mathf.MoveTowards(_swayVis, swayTarget, dt * 28f);
            if (idleW > 0.02f)
                leanX = breath * (1f + idleW);
            if (atRest && (stopping || idleW > 0.02f || Mathf.Abs(_swayVis) > 0.2f))
                leanZ = _swayVis;
            if (_skiBlend > 0.02f && !dashing && !sliding && !jet && !_walkFromSki && !_runFromSki && !_idleFromSki)
                leanX = Mathf.Lerp(leanX, 26f, _skiBlend);
            if (bouncing)
            {
                // Kick wall: spine opens opposite the wall normal (WallLeft = wall on left).
                leanX = Mathf.Lerp(leanX, 28f, bounceAmt);
                leanZ = Mathf.Lerp(leanZ, _bounceWallLeft ? -38f : 38f, bounceAmt);
            }
            if (gliding)
            {
                // Flat launch silhouette - hips read a crouch even if capsule stands.
                leanX = Mathf.Lerp(leanX, 42f, glideAmt);
                leanZ = Mathf.Lerp(leanZ, 0f, glideAmt);
            }
            _spineT = _spine0 * Quaternion.Euler(leanX, 0f, leanZ);
            float mantleAmt = mantle && _motor != null ? _motor.MantleProgress : 0f;
            _hipsT = _hips0 * Quaternion.Euler(lunging || dashing ? Mathf.Lerp(18f, 28f, dashAmt) : gliding ? Mathf.Lerp(8f, 22f, glideAmt) : bouncing ? 14f : mantle ? Mathf.Lerp(18f, 8f, mantleAmt) : jet ? -10f : climb ? 12f : air ? 8f : 0f, 0f, -leanZ * 0.55f);
            if (_skiBlend > 0.02f && !dashing && !sliding && !jet && !_walkFromSki && !_runFromSki && !_idleFromSki)
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(14f, 0f, 0f), _skiBlend);
            _headT = _head0 * Quaternion.Euler(lunging || dashing ? Mathf.Lerp(16f, 22f, dashAmt) : gliding ? Mathf.Lerp(-4f, 8f, glideAmt) : bouncing ? 10f : jet ? -8f : air ? -6f : -breath * 0.4f, 0f, 0f);
            float swayFade = Mathf.Max(idleW, atRest ? Mathf.Clamp01(Mathf.Abs(_swayVis) / 5f) : 0f);
            if (swayFade > 0.02f)
                _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-breath * 0.5f, 0f, -_swayVis * 0.35f), swayFade);

            Transform yawSrc = _motor != null ? _motor.transform : transform;
            float yawNow = yawSrc.eulerAngles.y;
            if (!_hasYaw)
            {
                _prevYaw = yawNow;
                _hasYaw = true;
            }
            float yawRate = dt > 0.0001f ? Mathf.DeltaAngle(_prevYaw, yawNow) / dt : 0f;
            _prevYaw = yawNow;
            // Positive yaw is a right turn. Visual only. Look speed is unchanged.
            bool canTurn = grounded && !air && !sliding && !crouch && !dashing && !jet && !wallRun && !climb && !mantle;
            float turnTarget = canTurn ? Mathf.Clamp(yawRate / 280f, -1f, 1f) : 0f;
            _turnVis = Mathf.MoveTowards(_turnVis, turnTarget, dt / 0.1f);
            if (Mathf.Abs(_turnVis) > 0.12f && canTurn)
            {
                // Same roll on the chest and the hips. A counter-roll reads as a twist at the waist.
                // A sprint leans a little more so the pair still reads through the long stride.
                float sprint = Mathf.Clamp01(_runVis);
                float leanDeg = Mathf.Lerp(4.5f, 10f, sprint);
                float w = Mathf.Clamp01(Mathf.Abs(_turnVis) * Mathf.Lerp(1f, 1.7f, sprint));
                Quaternion lean = Quaternion.Euler(0f, 0f, _turnVis * leanDeg);
                _spineT = Quaternion.Slerp(_spineT, _spineT * lean, w);
                _hipsT = Quaternion.Slerp(_hipsT, _hipsT * lean, w);
            }

            // Arms - slight outward A-pose only (large +Z was V-ing hands into the butt)
            float armZ = Mathf.Lerp(4f, 8f, _runVis);
            // Camera pitch only. The look gate and the sensitivity stay on the camera.
            float lookPitch = 0f;
            Transform lookCam = _motor != null ? _motor.cam : null;
            if (lookCam != null && lookCam.parent != null)
            {
                float x = lookCam.parent.localEulerAngles.x;
                if (x > 180f) x -= 360f;
                lookPitch = Mathf.Clamp(x, -25f, 55f);
            }
            _lookArmVis = Mathf.MoveTowards(_lookArmVis, lookPitch, dt * 240f);
            float lungeAmt = lunging && _motor != null ? _motor.LungeProgress : 0f;
            // A walk leaves the burst into the stride. A sprint leaves it into the long stride.
            // A still crouch leaves it into the guard. A stand keeps the old leave.
            // Duration and cooldown are unchanged.
            bool dashWalk = _airDashArms && !airDashing && !lunging && speed > 0.35f && st != MoveState.Sprint && runAmt <= 0.4f;
            bool dashCrouchWalk = dashWalk && _input != null && _input.CrouchHeld;
            bool dashSprint = _airDashArms && !airDashing && !lunging && !dashWalk && (st == MoveState.Sprint || runAmt > 0.4f || speed > 5.5f);
            bool dashCrouch = _airDashArms && !airDashing && !lunging && !dashWalk && !dashSprint
                && speed <= 0.35f && _input != null && _input.CrouchHeld;
            if (dashCrouch && !_dashCrouchWas && !_crouchFromJump && !_crouchFromDash
                && _upperArmL != null && _spine != null && _hips != null && _upperLegL != null && _head != null)
            {
                // The burst eases into the guard. A jump into a still crouch keeps its ease.
                // A still crouch into an air dash keeps its ease. Duration and cooldown are unchanged.
                _crouchFromDash = true;
                _crouchFromDashIn = 0f;
                _dashCrouchUaL = _upperArmL.localRotation;
                _dashCrouchUaR = _upperArmR.localRotation;
                _dashCrouchLaL = _lowerArmL.localRotation;
                _dashCrouchLaR = _lowerArmR.localRotation;
                _dashCrouchUlL = _upperLegL.localRotation;
                _dashCrouchUlR = _upperLegR.localRotation;
                _dashCrouchLlL = _lowerLegL.localRotation;
                _dashCrouchLlR = _lowerLegR.localRotation;
                _dashCrouchSp = _spine.localRotation;
                _dashCrouchHp = _hips.localRotation;
                _dashCrouchHd = _head.localRotation;
            }
            else if (dashCrouch && !_dashCrouchWas && !_crouchFromJump && !_crouchFromDash)
            {
                _crouchFromDash = true;
                _crouchFromDashIn = 1f;
            }
            if (!dashCrouch)
                _crouchFromDash = false;
            else if (_crouchFromDash)
                _crouchFromDashIn = Mathf.MoveTowards(_crouchFromDashIn, 1f, dt / 0.04f);
            _dashCrouchWas = dashCrouch;
            // 1 at the start of an air dash or lunge, 0 at the end. The pulse tail keeps easing after the burst.
            float dashStretchPose = 1f;
            if (lunging || dashing)
            {
                if (airDashing && _motor != null)
                {
                    // Hold the whip for the whole burst. A curve that dies early never
                    // reaches the chest and arms inside 0.1s. Duration and cooldown are unchanged.
                    dashStretchPose = 1f;
                    _dashRecover = 1f;
                    _airDashArms = true;
                    _armRecover = 0.28f;
                }
                else if (lunging)
                {
                    float raw = lungeAmt;
                    dashStretchPose = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.InverseLerp(0.08f, 0.62f, raw)));
                    _dashRecover = dashStretchPose;
                }
                else if (_airDashArms)
                {
                    // The burst already settled toward a hang. The leftover pulse is still high,
                    // and feeding it back in throws the arms into a second whip.
                    _dashRecover = Mathf.MoveTowards(_dashRecover, 0f, dt / 0.12f);
                    dashStretchPose = _dashRecover;
                }
                else
                {
                    float raw = Mathf.Clamp01(_dashPulse);
                    dashStretchPose = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.InverseLerp(0.08f, 0.62f, raw)));
                }
                // Pitch only. Extra roll on the Hier A-pose folds the hands into the pelvis.
                if (airDashing || (_airDashArms && !lunging))
                {
                    // Wide and back, clear of the fall trail, so the short burst reads from behind.
                    // The same pose eases out after the burst. It does not whip a second time.
                    // A soft landing after the burst keeps the arms in the stride. The knees still absorb.
                    float pose = dashStretchPose;
                    float intoStride = 0f;
                    if (!dashWalk && !dashSprint && !dashCrouch && grounded && !airDashing && _landSquash > 0.08f)
                    {
                        float hardS = Mathf.SmoothStep(0f, 1f, _landHard);
                        pose *= hardS;
                        intoStride = 1f - hardS;
                    }
                    if (dashCrouchWalk)
                    {
                        // The burst ends in the low stride. A still crouch keeps the guard.
                        _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-36f, 16f, armZ), _uaL0 * Quaternion.Euler(108f, 32f, armZ), pose);
                        _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-36f, -16f, -armZ), _uaR0 * Quaternion.Euler(108f, -32f, -armZ), pose);
                        _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-72f, 0f, 0f), _laL0 * Quaternion.Euler(-22f, 0f, 0f), pose);
                        _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-72f, 0f, 0f), _laR0 * Quaternion.Euler(-22f, 0f, 0f), pose);
                        _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(10f, 0f, 0f), _spine0 * Quaternion.Euler(58f, 0f, 0f), pose);
                        _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(22f, 0f, 0f), pose);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-6f, 0f, 0f), 1f - pose);
                    }
                    else if (dashWalk)
                    {
                        // The burst ends in the walk. It does not come to a stop.
                        float gait = Mathf.Max(Mathf.Clamp01(walkAmt), 0.65f);
                        float idle = 0f;
                        float amp = Mathf.Lerp(36f, 64f, gait);
                        float outY = Mathf.Lerp(12f, 8f, gait);
                        float roll = Mathf.Lerp(0f, armZ, gait);
                        float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                        float turnOut = Mathf.Abs(_turnVis) * 5f;
                        float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                        float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                        float armBreath = breath * 0.55f * idle;
                        float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                        float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                        float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                        float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                        _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll), _uaL0 * Quaternion.Euler(108f, 32f, armZ), pose);
                        _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll), _uaR0 * Quaternion.Euler(108f, -32f, -armZ), pose);
                        _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _laL0 * Quaternion.Euler(-22f, 0f, 0f), pose);
                        _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _laR0 * Quaternion.Euler(-22f, 0f, 0f), pose);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(58f, 0f, 0f), pose);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), pose);
                    }
                    else if (dashSprint)
                    {
                        // The burst ends in the long stride. It does not come to a stop.
                        float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                        float amp = Mathf.Lerp(36f, 64f, gait);
                        float outY = Mathf.Lerp(12f, 8f, gait);
                        float roll = Mathf.Lerp(0f, armZ, gait);
                        float reachY = outY + 6f;
                        float turnOut = Mathf.Abs(_turnVis) * 5f;
                        float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                        float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                        float elbowL = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(sinC) * gait);
                        float elbowR = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(-sinC) * gait);
                        _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp), yL, roll), _uaL0 * Quaternion.Euler(108f, 32f, armZ), pose);
                        _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp), -yR, -roll), _uaR0 * Quaternion.Euler(108f, -32f, -armZ), pose);
                        _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _laL0 * Quaternion.Euler(-22f, 0f, 0f), pose);
                        _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _laR0 * Quaternion.Euler(-22f, 0f, 0f), pose);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(58f, 0f, 0f), pose);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), pose);
                    }
                    else if (dashCrouch && !_crouchFromDash)
                    {
                        // The burst ends in the guard. A crouch walk ends in the low stride.
                        _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-36f, 16f, armZ), _uaL0 * Quaternion.Euler(108f, 32f, armZ), pose);
                        _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-36f, -16f, -armZ), _uaR0 * Quaternion.Euler(108f, -32f, -armZ), pose);
                        _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-72f, 0f, 0f), _laL0 * Quaternion.Euler(-22f, 0f, 0f), pose);
                        _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-72f, 0f, 0f), _laR0 * Quaternion.Euler(-22f, 0f, 0f), pose);
                        _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(10f, 0f, 0f), _spine0 * Quaternion.Euler(58f, 0f, 0f), pose);
                        _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(22f, 0f, 0f), pose);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-6f, 0f, 0f), 1f - pose);
                    }
                    else
                    {
                    _uaLT = Quaternion.Slerp(
                        _uaL0 * Quaternion.Euler(-16f, 0f, armZ),
                        _uaL0 * Quaternion.Euler(108f, 32f, armZ),
                        pose);
                    _uaRT = Quaternion.Slerp(
                        _uaR0 * Quaternion.Euler(-12f, 0f, -armZ),
                        _uaR0 * Quaternion.Euler(108f, -32f, -armZ),
                        pose);
                    _laLT = Quaternion.Slerp(
                        _laL0 * Quaternion.Euler(-14f, 0f, 0f),
                        _laL0 * Quaternion.Euler(-22f, 0f, 0f),
                        pose);
                    _laRT = Quaternion.Slerp(
                        _laR0 * Quaternion.Euler(-12f, 0f, 0f),
                        _laR0 * Quaternion.Euler(-22f, 0f, 0f),
                        pose);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(58f, 0f, 0f), pose);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), pose);
                    if (intoStride > 0.02f)
                    {
                        float gait = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                        float idle = 1f - gait;
                        float amp = Mathf.Lerp(36f, 64f, gait);
                        float outY = Mathf.Lerp(12f, 8f, gait);
                        float roll = Mathf.Lerp(0f, armZ, gait);
                        float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                        float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                        float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                        float armBreath = breath * 0.55f * idle;
                        float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                        float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll), intoStride);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll), intoStride);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait), 0f, 0f), intoStride);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait), 0f, 0f), intoStride);
                    }
                    }
                }
                else
                {
                    // Early frames hold the whip. The back half settles toward a hang so the run does not pop in.
                    _uaLT = Quaternion.Slerp(
                        _uaL0 * Quaternion.Euler(-16f, 0f, armZ),
                        _uaL0 * Quaternion.Euler(96f, -6f, armZ),
                        dashStretchPose);
                    _uaRT = Quaternion.Slerp(
                        _uaR0 * Quaternion.Euler(-12f, 0f, -armZ),
                        _uaR0 * Quaternion.Euler(70f, 6f, -armZ),
                        dashStretchPose);
                    _laLT = Quaternion.Slerp(
                        _laL0 * Quaternion.Euler(-14f, 0f, 0f),
                        _laL0 * Quaternion.Euler(-58f, 0f, 0f),
                        dashStretchPose);
                    _laRT = Quaternion.Slerp(
                        _laR0 * Quaternion.Euler(-12f, 0f, 0f),
                        _laR0 * Quaternion.Euler(-46f, 0f, 0f),
                        dashStretchPose);
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(14f, 0f, 0f), _spineT, Mathf.Lerp(0.4f, 1f, dashStretchPose));
                    _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(8f, 0f, 0f), _hipsT, Mathf.Lerp(0.4f, 1f, dashStretchPose));
                }
            }
            else if (jet)
            {
                // Jet pack tell + forward reach so thrust reads in TP (no invisible grapple)
                float throb = 0.65f + 0.35f * Mathf.Sin(Time.time * 18f);
                _uaLT = _uaL0 * Quaternion.Euler(18f * throb, 16f, 62f);
                _uaRT = _uaR0 * Quaternion.Euler(-48f * throb, -18f, -58f);
                _laLT = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(-48f, 0f, 0f);
            }
            else if (climb)
            {
                // One hand meets the surface, then they trade. The swing eases in
                // so the grab does not pop. Pitch and the mild A flare only.
                float climbLive = Mathf.Sin(_surfPhase);
                float upLive = (climbLive + 1f) * 0.5f;
                float up = Mathf.Lerp(0.8f, upLive, _surfIn);
                float down = 1f - up;
                _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-52f, -118f, up), Mathf.Lerp(12f, 18f, up), armZ);
                _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-52f, -118f, down), Mathf.Lerp(-12f, -18f, down), -armZ);
                _laLT = _laL0 * Quaternion.Euler(Mathf.Lerp(-18f, -8f, up), 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(Mathf.Lerp(-18f, -8f, down), 0f, 0f);
            }
            else if (mantle)
            {
                // Progress pull-up - plant: syncs with motor mantle arc (not free Time.sin).
                float m = _motor != null ? _motor.MantleProgress : 0.5f;
                float reach = Mathf.Lerp(-155f, -78f, m);
                float flare = Mathf.Lerp(32f, 14f, m);
                _uaLT = _uaL0 * Quaternion.Euler(reach, Mathf.Lerp(20f, 8f, m), flare);
                _uaRT = _uaR0 * Quaternion.Euler(reach - 4f, Mathf.Lerp(-20f, -8f, m), -flare);
                _laLT = _laL0 * Quaternion.Euler(Mathf.Lerp(-62f, -28f, m), 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(Mathf.Lerp(-62f, -28f, m), 0f, 0f);
            }
            else if (wallRun)
            {
                // Wall hand presses into the surface, then travels with the stride.
                // The outer arm stays a long line. Pitch and the mild A flare only.
                bool left = _motor != null && _motor.WallLeft;
                float wallLive = Mathf.Sin(_surfPhase);
                float pressLive = (wallLive + 1f) * 0.5f;
                float press = Mathf.Lerp(0.55f, pressLive, _surfIn);
                float outerFwd = 1f - press;
                float yaw = Mathf.Lerp(18f, 34f, _surfIn);
                float wallPitch = Mathf.Lerp(-48f, -72f, press);
                float wallElbow = Mathf.Lerp(-16f, -8f, press);
                float outerArm = Mathf.Lerp(-36f, Mathf.Lerp(-28f, -84f, 1f - pressLive), _surfIn);
                float outerElbow = Mathf.Lerp(-16f, -10f, outerFwd);
                if (left)
                {
                    _uaLT = _uaL0 * Quaternion.Euler(wallPitch, yaw, armZ);
                    _uaRT = _uaR0 * Quaternion.Euler(outerArm, -10f, -armZ);
                    _laLT = _laL0 * Quaternion.Euler(wallElbow, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(outerElbow, 0f, 0f);
                }
                else
                {
                    _uaRT = _uaR0 * Quaternion.Euler(wallPitch, -yaw, -armZ);
                    _uaLT = _uaL0 * Quaternion.Euler(outerArm, 10f, armZ);
                    _laRT = _laR0 * Quaternion.Euler(wallElbow, 0f, 0f);
                    _laLT = _laL0 * Quaternion.Euler(outerElbow, 0f, 0f);
                }
            }
            else if (gliding)
            {
                // Flat forward reach - reads as mantle-glide launch, not air flail
                float g = glideAmt;
                _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-20f, -72f, g), 12f * g, Mathf.Lerp(14f, 38f, g));
                _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-20f, -72f, g), -12f * g, Mathf.Lerp(-14f, -38f, g));
                _laLT = _laL0 * Quaternion.Euler(Mathf.Lerp(-18f, -36f, g), 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(Mathf.Lerp(-18f, -36f, g), 0f, 0f);
            }
            else if (bouncing)
            {
                // Brief push-off: wall-side arm plants/kicks, outer flings open
                float k = bounceAmt;
                if (_bounceWallLeft)
                {
                    _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-20f, -78f, k), 22f * k, 42f * k);
                    _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-20f, -48f, k), -18f * k, -36f * k);
                    _laLT = _laL0 * Quaternion.Euler(-52f * k, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(-24f * k, 0f, 0f);
                }
                else
                {
                    _uaLT = _uaL0 * Quaternion.Euler(Mathf.Lerp(-20f, -48f, k), 18f * k, 36f * k);
                    _uaRT = _uaR0 * Quaternion.Euler(Mathf.Lerp(-20f, -78f, k), -22f * k, -42f * k);
                    _laLT = _laL0 * Quaternion.Euler(-24f * k, 0f, 0f);
                    _laRT = _laR0 * Quaternion.Euler(-52f * k, 0f, 0f);
                }
            }
            else if (punching && !((_skiFromMiss || _slideFromMiss) && phase == PunchPhase.MissRecover) && !_jumpFromPunch)
            {
                // Clear windup -> connect pose (beyond HitRecover) so tags read in TP
                _uaLT = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                _laLT = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                if (phase == PunchPhase.Windup)
                {
                    // The cock arrives in the first beat and holds, so the tell reads before the strike.
                    // Yaw carries the elbow out. Roll stays the mild A. Timing stays the authored 0.12s windup.
                    float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                    _uaRT = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                    _laRT = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                    _hipsT = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f); // clearer windup hip twist in TP
                    _spineT = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ); // clearer windup spine twist in TP
                }
                else if (phase == PunchPhase.Active)
                {
                    float e = Mathf.Lerp(0.8f, 1f, punchProg);
                    // Long line in front of the chest. A bent elbow disappears at chase distance.
                    // Pitch stays above -150 so the fist does not wrap through the torso.
                    _uaRT = _uaR0 * Quaternion.Euler(-118f, 58f * e, -22f);
                    _laRT = _laR0 * Quaternion.Euler(-18f * e, 0f, 0f);
                    _hipsT = _hips0 * Quaternion.Euler(18f, 22f * e, 0f);
                    _spineT = _spine0 * Quaternion.Euler(leanX + 16f, 42f * e, leanZ);
                }
                else if (phase == PunchPhase.HitRecover)
                {
                    // Hold the connect, then ease onward so the fist does not snap when the phase ends.
                    // A tag eases into the new It's claim. A clean hit still eases into the run.
                    // Timing is unchanged. Pitch on the connect stays above a torso wrap.
                    float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, punchProg));
                    Quaternion connectR = _uaR0 * Quaternion.Euler(-118f, 52f, -22f);
                    Quaternion connectL = _uaL0 * Quaternion.Euler(-32f, 18f, armZ);
                    if (claimAmt > 0.04f)
                    {
                        _uaRT = Quaternion.Slerp(connectR, _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), settle);
                        _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-18f, 0f, 0f), _laR0 * Quaternion.Euler(-12f, 0f, 0f), settle);
                        _uaLT = Quaternion.Slerp(connectL, _uaL0 * Quaternion.Euler(-128f, 8f, armZ), settle);
                        _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-22f, 0f, 0f), _laL0 * Quaternion.Euler(-10f, 0f, 0f), settle);
                        _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spine0 * Quaternion.Euler(-22f, -16f, 0f), settle);
                        _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hips0 * Quaternion.Euler(4f, 0f, 0f), settle);
                    }
                    else
                    {
                        // The arm opposite the front knee goes back to the stride while the
                        // fist is still out. The hips leave the punch twist with that arm.
                        // Windup time is unchanged.
                        float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
                        float idle = 1f - gait;
                        float amp = Mathf.Lerp(36f, 64f, gait);
                        float outY = Mathf.Lerp(12f, 8f, gait);
                        float roll = Mathf.Lerp(0f, armZ, gait);
                        float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                        float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                        float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                        float armBreath = breath * 0.55f * idle;
                        Quaternion runL = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll);
                        Quaternion runR = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll);
                        float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                        float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                        float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                        float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                        float plant = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg));
                        _uaRT = Quaternion.Slerp(connectR, runR, settle);
                        _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-18f, 0f, 0f), _laR0 * Quaternion.Euler(elbowR, 0f, 0f), settle);
                        _uaLT = Quaternion.Slerp(connectL, runL, plant);
                        _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-22f, 0f, 0f), _laL0 * Quaternion.Euler(elbowL, 0f, 0f), plant);
                        _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spineT, plant);
                        _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hipsT, plant);
                    }
                }
                else // MissRecover - limp whiff: less extension, quicker drop vs HitRecover hold
                {
                    // Cubed ease + soft shoulder sag so a whiff drops faster vs HitRecover hold.
                    float r = Mathf.Lerp(0.62f, 0.02f, punchProg * punchProg * punchProg); // fuller limp drop at end of MissRecover
                    _uaRT = _uaR0 * Quaternion.Euler(-18f - 40f * r, 10f * r, -8f); // softer limp shoulder so whiff reads in TP
                    _laRT = _laR0 * Quaternion.Euler(-14f * r, 0f, 0f);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX + 6f * r, 0f, leanZ), 0.35f);
                    // Standing, both fists ease into the idle hang so they do not freeze and then pop.
                    // A walk returns them to the stride. A sprint returns them to the long stride.
                    // A still crouch keeps the whiff for the snapshot ease. A crouch walk keeps that guard and the low stride.
                    // Whiff time is unchanged.
                    float moving = grounded ? Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)) : 0f;
                    float standing = grounded ? 1f - moving : 0f;
                    bool crouchMiss = grounded && crouch && speed <= 0.35f;
                    bool crouchWalkMiss = grounded && crouch && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint;
                    float walkMiss = grounded ? Mathf.Clamp01(walkAmt) * (1f - Mathf.Clamp01(runAmt)) : 0f;
                    float sprintMiss = grounded && (st == MoveState.Sprint || runAmt > 0.4f) ? 1f : 0f;
                    if (sprintMiss > 0.02f || crouchMiss || crouchWalkMiss)
                        walkMiss = 0f;
                    if (crouchMiss || crouchWalkMiss)
                        sprintMiss = 0f;
                    float missEase = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg));
                    float intoIdle = crouchMiss || crouchWalkMiss || walkMiss > 0.02f || sprintMiss > 0.02f ? 0f : missEase * standing;
                    if (crouchWalkMiss)
                    {
                        // The whiff eases into the guard. A still crouch keeps its snapshot. The feet stay on the low stride.
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), missEase);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), missEase);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-72f, 0f, 0f), missEase);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-72f, 0f, 0f), missEase);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(10f, 0f, 0f), missEase);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), missEase);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-6f, 0f, 0f), missEase);
                    }
                    else if (sprintMiss > 0.02f)
                    {
                        float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                        float amp = Mathf.Lerp(36f, 64f, gait);
                        float outY = Mathf.Lerp(12f, 8f, gait);
                        float roll = Mathf.Lerp(0f, armZ, gait);
                        float reachY = outY + 6f;
                        float turnOut = Mathf.Abs(_turnVis) * 5f;
                        float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                        float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                        float pitchL = RunArmPitch(-sinC, amp);
                        float pitchR = RunArmPitch(sinC, amp);
                        float elbowL = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(sinC) * gait);
                        float elbowR = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(-sinC) * gait);
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(pitchL, yL, roll), missEase);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(pitchR, -yR, -roll), missEase);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(elbowL, 0f, 0f), missEase);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(elbowR, 0f, 0f), missEase);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX, 0f, leanZ), missEase);
                    }
                    else if (walkMiss > 0.02f)
                    {
                        float gait = Mathf.Max(Mathf.Clamp01(walkAmt), Mathf.Max(_stopGait, _runVis));
                        float idle = 1f - gait;
                        float amp = Mathf.Lerp(36f, 64f, gait);
                        float outY = Mathf.Lerp(12f, 8f, gait);
                        float roll = Mathf.Lerp(0f, armZ, gait);
                        float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                        float turnOut = Mathf.Abs(_turnVis) * 5f;
                        float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                        float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                        float armBreath = breath * 0.55f * idle;
                        float pitchL = RunArmPitch(-sinC, amp) - 12f * idle + armBreath;
                        float pitchR = RunArmPitch(sinC, amp) - 12f * idle + armBreath;
                        float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                        float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                        float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                        float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(pitchL, yL, roll), missEase);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(pitchR, -yR, -roll), missEase);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(elbowL, 0f, 0f), missEase);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(elbowR, 0f, 0f), missEase);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX, 0f, leanZ), missEase);
                    }
                    if (intoIdle > 0.02f)
                    {
                        float armBreath = breath * 0.55f;
                        float y = 12f + Mathf.Abs(_turnVis) * 5f;
                        float elbowIdle = Mathf.Lerp(-10f, -6f, _runVis);
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-12f + armBreath, y, 0f), intoIdle);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-12f + armBreath, -y, 0f), intoIdle);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(elbowIdle, 0f, 0f), intoIdle);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(elbowIdle, 0f, 0f), intoIdle);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX, 0f, leanZ), intoIdle);
                    }
                }
            }
            else if (air)
            {
                // Rise: a long line up and out. Fall: both arms trail back, wide of the torso.
                // The tuck pitch is unchanged once the arms have settled. Look speed is unchanged.
                // Apex hangs out to the sides so the top reads before the trail. Mild A only.
                float airW = Mathf.Clamp01(airRise + airFall);
                float riseShare = airW > 0.001f ? airRise / (airRise + airFall) : 0f;
                float lookUp = Mathf.Clamp(-_lookArmVis, 0f, 25f);
                float lookDown = Mathf.Clamp(_lookArmVis, 0f, 55f);
                // Look lives on the fall only, and it arrives with the settle so the trail does not pop.
                // An air dash keeps the burst pose. Jump height is unchanged.
                float armIn = _airDashArms ? 1f : Mathf.SmoothStep(0f, 1f, _airArmIn);
                float fallPitch = 72f + (lookDown * 0.05f - lookUp * 0.2f) * armIn;
                float fallYaw = 16f + lookDown * 0.08f * armIn;
                Quaternion upL = _uaL0 * Quaternion.Euler(-112f, 16f, armZ);
                Quaternion upR = _uaR0 * Quaternion.Euler(-112f, -16f, -armZ);
                Quaternion downL = _uaL0 * Quaternion.Euler(fallPitch, fallYaw, armZ);
                Quaternion downR = _uaR0 * Quaternion.Euler(fallPitch, -fallYaw, -armZ);
                Quaternion hangL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion hangR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                _uaLT = Quaternion.Slerp(hangL, Quaternion.Slerp(downL, upL, riseShare), airW);
                _uaRT = Quaternion.Slerp(hangR, Quaternion.Slerp(downR, upR, riseShare), airW);
                Quaternion elbowUp = Quaternion.Euler(-12f, 0f, 0f);
                Quaternion elbowDown = Quaternion.Euler(-16f, 0f, 0f);
                Quaternion elbowHang = Quaternion.Euler(-12f, 0f, 0f);
                Quaternion elbow = Quaternion.Slerp(elbowHang, Quaternion.Slerp(elbowDown, elbowUp, riseShare), airW);
                _laLT = _laL0 * elbow;
                _laRT = _laR0 * elbow;
                if (_airArmIn < 0.98f && !_airDashArms)
                {
                    // Short reach off the stride, then the tuck, the hang, or the look trail.
                    Quaternion takeL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                    Quaternion takeR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                    _uaLT = Quaternion.Slerp(takeL, _uaLT, armIn);
                    _uaRT = Quaternion.Slerp(takeR, _uaRT, armIn);
                    Quaternion elbowTake = Quaternion.Euler(-14f, 0f, 0f);
                    _laLT = Quaternion.Slerp(_laL0 * elbowTake, _laLT, armIn);
                    _laRT = Quaternion.Slerp(_laR0 * elbowTake, _laRT, armIn);
                }
                _spineT = Quaternion.Slerp(
                    _spine0 * Quaternion.Euler(-6f, 0f, 0f),
                    Quaternion.Slerp(_spine0 * Quaternion.Euler(26f, 0f, 0f), _spine0 * Quaternion.Euler(-8f, 0f, 0f), riseShare),
                    airW);
                _hipsT = Quaternion.Slerp(
                    _hips0 * Quaternion.Euler(6f, 0f, 0f),
                    Quaternion.Slerp(_hips0 * Quaternion.Euler(8f, 0f, 0f), _hips0 * Quaternion.Euler(4f, 0f, 0f), riseShare),
                    airW);
                if (_diveVis > 0.02f)
                {
                    // Air crouch: knees up and arms in, short of the jump tuck and the ground guard.
                    // A moving fall eases into the low stride. A still crouch keeps the dart.
                    // The chest stays nose-down on that dart. Fall speed is unchanged.
                    float d = _diveVis;
                    if (airDartWalk)
                    {
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), d);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), d);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-72f, 0f, 0f), d);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-72f, 0f, 0f), d);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(10f, 0f, 0f), d);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), d);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-6f, 0f, 0f), d);
                    }
                    else
                    {
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-16f, 12f, armZ), d);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-16f, -12f, -armZ), d);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-58f, 0f, 0f), d);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-58f, 0f, 0f), d);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(46f, 0f, 0f), d);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(24f, 0f, 0f), d);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(12f, 0f, 0f), d);
                    }
                }
                if (airStillCrouch && !_jumpFromStill)
                {
                    // The tuck leaves into the guard. The push still reads, then the crouch.
                    float g = 1f - Mathf.Clamp01(_pushOff);
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), g);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), g);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-72f, 0f, 0f), g);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-72f, 0f, 0f), g);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(10f, 0f, 0f), g);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), g);
                    _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-6f, 0f, 0f), g);
                }
                if (airCrouchWalk && !_jumpFromCrouchWalk && !_jumpFromWalk)
                {
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), 1f);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), 1f);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-72f, 0f, 0f), 1f);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-72f, 0f, 0f), 1f);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(10f, 0f, 0f), 1f);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), 1f);
                    _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-6f, 0f, 0f), 1f);
                }
                if (_jumpFromSki && _pushOff > 0.02f && _diveVis < 0.2f)
                {
                    // The glide eases into the push, then the air pose. A still crouch and a crouch walk keep their push.
                    // A standing jump keeps the old push. Jump height is unchanged.
                    float t = 1f - Mathf.Clamp01(_pushOff);
                    float intoPush = Mathf.Clamp01(t * 2f);
                    float leave = Mathf.Clamp01(t * 2f - 1f);
                    float skateL = RunArmPitch(-sinC, 16f);
                    float skateR = RunArmPitch(sinC, 16f);
                    Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                    Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                    Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                    Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                    Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                    Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                    Quaternion pushElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                    Quaternion pushElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                    _uaLT = Quaternion.Slerp(Quaternion.Slerp(skiL, pushL, intoPush), _uaLT, leave);
                    _uaRT = Quaternion.Slerp(Quaternion.Slerp(skiR, pushR, intoPush), _uaRT, leave);
                    _laLT = Quaternion.Slerp(Quaternion.Slerp(skiElL, pushElL, intoPush), _laLT, leave);
                    _laRT = Quaternion.Slerp(Quaternion.Slerp(skiElR, pushElR, intoPush), _laRT, leave);
                    Quaternion skiSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                    Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                    Quaternion skiHp = _hips0 * Quaternion.Euler(14f, 0f, 0f);
                    Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                    Quaternion skiHd = _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f);
                    Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                    _spineT = Quaternion.Slerp(Quaternion.Slerp(skiSp, pushSp, intoPush), _spineT, leave);
                    _hipsT = Quaternion.Slerp(Quaternion.Slerp(skiHp, pushHp, intoPush), _hipsT, leave);
                    _headT = Quaternion.Slerp(Quaternion.Slerp(skiHd, pushHd, intoPush), _headT, leave);
                }
                if (_jumpFromSlide && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromSlideIn < 0.98f)
                {
                    // The wedge eases into the jump, then the jump holds.
                    // A ski into a jump keeps its ease. A still crouch and a crouch walk keep their push.
                    // A standing jump keeps the old push. slideBoost stays 0. Jump height is unchanged.
                    float intoJump = _jumpFromSlideIn;
                    _uaLT = Quaternion.Slerp(_slideJumpUaL, _uaLT, intoJump);
                    _uaRT = Quaternion.Slerp(_slideJumpUaR, _uaRT, intoJump);
                    _laLT = Quaternion.Slerp(_slideJumpLaL, _laLT, intoJump);
                    _laRT = Quaternion.Slerp(_slideJumpLaR, _laRT, intoJump);
                    _spineT = Quaternion.Slerp(_slideJumpSp, _spineT, intoJump);
                    _hipsT = Quaternion.Slerp(_slideJumpHp, _hipsT, intoJump);
                    _headT = Quaternion.Slerp(_slideJumpHd, _headT, intoJump);
                }
                if (_jumpFromPunch && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromPunchIn < 0.98f)
                {
                    // The punch eases into the jump, then the jump holds.
                    // Becoming It into a jump keeps its ease. A tag into a jump keeps its ease.
                    // A punch miss into a jump keeps its ease. Windup time is unchanged. Jump height is unchanged.
                    float intoJump = _jumpFromPunchIn;
                    _uaLT = Quaternion.Slerp(_punchJumpUaL, _uaLT, intoJump);
                    _uaRT = Quaternion.Slerp(_punchJumpUaR, _uaRT, intoJump);
                    _laLT = Quaternion.Slerp(_punchJumpLaL, _laLT, intoJump);
                    _laRT = Quaternion.Slerp(_punchJumpLaR, _laRT, intoJump);
                    _spineT = Quaternion.Slerp(_punchJumpSp, _spineT, intoJump);
                    _hipsT = Quaternion.Slerp(_punchJumpHp, _hipsT, intoJump);
                    _headT = Quaternion.Slerp(_punchJumpHd, _headT, intoJump);
                }
                if (_jumpFromCrouchWalk && !_jumpFromStill && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromCrouchWalkIn < 0.98f)
                {
                    // The low stride eases into the jump, then the jump holds.
                    // A still crouch into a jump keeps its push. A walk into a ski keeps its ease.
                    // A crouch walk into an air dash keeps its ease. Jump height is unchanged.
                    float intoJump = _jumpFromCrouchWalkIn;
                    _uaLT = Quaternion.Slerp(_crouchWalkJumpUaL, _uaLT, intoJump);
                    _uaRT = Quaternion.Slerp(_crouchWalkJumpUaR, _uaRT, intoJump);
                    _laLT = Quaternion.Slerp(_crouchWalkJumpLaL, _laLT, intoJump);
                    _laRT = Quaternion.Slerp(_crouchWalkJumpLaR, _laRT, intoJump);
                    _spineT = Quaternion.Slerp(_crouchWalkJumpSp, _spineT, intoJump);
                    _hipsT = Quaternion.Slerp(_crouchWalkJumpHp, _hipsT, intoJump);
                    _headT = Quaternion.Slerp(_crouchWalkJumpHd, _headT, intoJump);
                }
                if (_jumpFromWalk && !_jumpFromCrouchWalk && !_jumpFromStill && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromWalkIn < 0.98f)
                {
                    // The walk eases into the jump, then the jump holds.
                    // A crouch walk into a jump keeps its ease. A still crouch into a jump keeps its push.
                    // A walk into a ski keeps its ease. Jump height is unchanged.
                    float intoJump = _jumpFromWalkIn;
                    _uaLT = Quaternion.Slerp(_walkJumpUaL, _uaLT, intoJump);
                    _uaRT = Quaternion.Slerp(_walkJumpUaR, _uaRT, intoJump);
                    _laLT = Quaternion.Slerp(_walkJumpLaL, _laLT, intoJump);
                    _laRT = Quaternion.Slerp(_walkJumpLaR, _laRT, intoJump);
                    _spineT = Quaternion.Slerp(_walkJumpSp, _spineT, intoJump);
                    _hipsT = Quaternion.Slerp(_walkJumpHp, _hipsT, intoJump);
                    _headT = Quaternion.Slerp(_walkJumpHd, _headT, intoJump);
                }
                if (_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromWalk && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromStillIn < 0.98f)
                {
                    // The guard eases into the jump, then the jump holds.
                    // A crouch walk into a jump keeps its ease. A walk into an air dash keeps its ease.
                    // A jump into a still crouch keeps its ease. Jump height is unchanged.
                    float intoJump = _jumpFromStillIn;
                    _uaLT = Quaternion.Slerp(_stillJumpUaL, _uaLT, intoJump);
                    _uaRT = Quaternion.Slerp(_stillJumpUaR, _uaRT, intoJump);
                    _laLT = Quaternion.Slerp(_stillJumpLaL, _laLT, intoJump);
                    _laRT = Quaternion.Slerp(_stillJumpLaR, _laRT, intoJump);
                    _spineT = Quaternion.Slerp(_stillJumpSp, _spineT, intoJump);
                    _hipsT = Quaternion.Slerp(_stillJumpHp, _hipsT, intoJump);
                    _headT = Quaternion.Slerp(_stillJumpHd, _headT, intoJump);
                }
            }
            else
            {
                // Opposite the legs. sinC>0 puts the left thigh forward, so the right arm reaches
                // and the left arm stays back. Same-side swing reads as a skate from the chase cam.
                // Rearward travel stays short so the hands do not fold into the pelvis. No extra roll.
                // _stopGait holds the last stride while the feet close, so a brake does not pop the arms idle.
                // _runVis keeps the sprint swing while it eases into the walk, so the arms do not snap.
                float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_stopGait, _runVis));
                float idle = 1f - gait;
                float amp = Mathf.Lerp(36f, 64f, gait);
                // Idle hang sits slightly forward and out. The outward yaw stays on through the
                // stride so the hands do not drop into the hips as the walk starts.
                // Roll stays 0 at rest and only picks up the mild A once the stride is moving.
                float outY = Mathf.Lerp(12f, 8f, gait);
                float roll = Mathf.Lerp(0f, armZ, gait);
                // The reach opposite the front knee opens a little wider. The back arm keeps the
                // shorter yaw so the hand stays out of the hip. Rearward pitch stays short.
                float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                // A turn opens both hands a little more. No extra roll, so they stay off the hips.
                float turnOut = Mathf.Abs(_turnVis) * 5f;
                yL += turnOut;
                yR += turnOut;
                // The reach follows the look. Look up lifts it. Look down stays short and out,
                // so the hand does not enter the hip. The back arm stays the plant.
                float lookUp = Mathf.Clamp(-_lookArmVis, 0f, 25f);
                float lookDown = Mathf.Clamp(_lookArmVis, 0f, 55f);
                float lookAdd = lookDown * 0.1f - lookUp * 0.5f;
                float lookOut = lookDown * 0.1f;
                float reachL = Mathf.Clamp01(-sinC) * gait;
                float reachR = Mathf.Clamp01(sinC) * gait;
                yL += lookOut * reachL;
                yR += lookOut * reachR;
                // Both hands rise a little with the breath. Yaw stays out, and roll stays 0 at rest,
                // so the sway does not fold the hands into the hips.
                float armBreath = breath * 0.55f * idle;
                float pitchL = RunArmPitch(-sinC, amp) - 12f * idle + armBreath + lookAdd * reachL;
                float pitchR = RunArmPitch(sinC, amp) - 12f * idle + armBreath + lookAdd * reachR;
                // Stop and the first step. Hands stay forward and out so they do not drift into the hips.
                float stopBlend = (!stepping && !air && !sliding && !crouch) ? _stopGait : 0f;
                float startBlend = (stepping && !air)
                    ? (1f - Mathf.SmoothStep(0f, 1f, _stepIn)) * Mathf.Clamp01(walkAmt + runAmt)
                    : 0f;
                float armHold = Mathf.Clamp01(Mathf.Max(stopBlend, startBlend));
                if (armHold > 0.02f)
                {
                    pitchL = Mathf.Lerp(pitchL, Mathf.Min(pitchL, -6f), armHold);
                    pitchR = Mathf.Lerp(pitchR, Mathf.Min(pitchR, -6f), armHold);
                    yL += 5f * armHold;
                    yR += 5f * armHold;
                }
                _uaLT = _uaL0 * Quaternion.Euler(pitchL, yL, roll);
                _uaRT = _uaR0 * Quaternion.Euler(pitchR, -yR, -roll);
                // Long line on the reach. The elbow fold sits on the back arm, short of the hip.
                // The trail knee is unchanged and stays straight.
                float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                if (armHold > 0.02f)
                {
                    elbowL = Mathf.Lerp(elbowL, Mathf.Max(elbowL, -12f), armHold);
                    elbowR = Mathf.Lerp(elbowR, Mathf.Max(elbowR, -12f), armHold);
                }
                _laLT = _laL0 * Quaternion.Euler(elbowL, 0f, 0f);
                _laRT = _laR0 * Quaternion.Euler(elbowR, 0f, 0f);
                if (footSki > 0.001f)
                {
                    // Short swing while the feet are still in the glide, so the arms do not skate past the hips.
                    float skateL = RunArmPitch(-sinC, 16f);
                    float skateR = RunArmPitch(sinC, 16f);
                    float skateW = (_walkFromSki || _runFromSki || _idleFromSki) ? 0f : legSki;
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ), skateW);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ), skateW);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-12f, 0f, 0f), skateW);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-12f, 0f, 0f), skateW);
                }
                if (_dropVis > 0.02f)
                {
                    // The stride drops into the wedge or the guard. A slide stand-up
                    // brings the arms back with the feet, under the hips.
                    float d = footDrop;
                    if (_dropSlide)
                    {
                        float lineL = -70f;
                        float lineR = -64f;
                        float lineYaw = 28f;
                        float lineRoll = armZ;
                        float lineElbL = -8f;
                        float lineElbR = -6f;
                        float lineYawR = lineYaw;
                        if (slideIdleExit)
                        {
                            // Hands leave the long line into the idle hang. They do not pop.
                            float up = 1f - _dropVis;
                            float hang = -12f + breath * 0.55f;
                            lineL = Mathf.Lerp(-70f, hang, up);
                            lineR = Mathf.Lerp(-64f, hang, up);
                            lineYaw = Mathf.Lerp(28f, 12f, up);
                            lineYawR = lineYaw;
                            lineRoll = Mathf.Lerp(armZ, 0f, up);
                            lineElbL = Mathf.Lerp(-8f, -10f, up);
                            lineElbR = Mathf.Lerp(-6f, -10f, up);
                        }
                        else if (slideWalkExit)
                        {
                            // Hands leave the long line into the walk. They do not stay in the line.
                            float up = 1f - _dropVis;
                            lineL = Mathf.Lerp(-70f, pitchL, up);
                            lineR = Mathf.Lerp(-64f, pitchR, up);
                            lineYaw = Mathf.Lerp(28f, yL, up);
                            lineYawR = Mathf.Lerp(28f, yR, up);
                            lineRoll = Mathf.Lerp(armZ, roll, up);
                            lineElbL = Mathf.Lerp(-8f, elbowL, up);
                            lineElbR = Mathf.Lerp(-6f, elbowR, up);
                        }
                        else if (slideSprintExit)
                        {
                            // Hands leave the long line into the open stride. They do not stay in the line.
                            float up = 1f - _dropVis;
                            float openGait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                            float openAmp = Mathf.Lerp(36f, 64f, openGait);
                            float openOut = Mathf.Lerp(12f, 8f, openGait);
                            float openRoll = Mathf.Lerp(0f, armZ, openGait);
                            float openReachY = openOut + 6f;
                            float openTurn = Mathf.Abs(_turnVis) * 5f;
                            float openYL = Mathf.Lerp(openOut, openReachY, Mathf.Clamp01(-sinC) * openGait) + openTurn;
                            float openYR = Mathf.Lerp(openOut, openReachY, Mathf.Clamp01(sinC) * openGait) + openTurn;
                            lineL = Mathf.Lerp(-70f, RunArmPitch(-sinC, openAmp), up);
                            lineR = Mathf.Lerp(-64f, RunArmPitch(sinC, openAmp), up);
                            lineYaw = Mathf.Lerp(28f, openYL, up);
                            lineYawR = Mathf.Lerp(28f, openYR, up);
                            lineRoll = Mathf.Lerp(armZ, openRoll, up);
                            lineElbL = Mathf.Lerp(-8f, Mathf.Lerp(-6f, -30f, Mathf.Clamp01(sinC) * openGait), up);
                            lineElbR = Mathf.Lerp(-6f, Mathf.Lerp(-6f, -30f, Mathf.Clamp01(-sinC) * openGait), up);
                        }
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(lineL, lineYaw, lineRoll), d);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(lineR, -lineYawR, -lineRoll), d);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(lineElbL, 0f, 0f), d);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(lineElbR, 0f, 0f), d);
                    }
                    else if (crouchStandSprint)
                    {
                        // The guard opens into the long stride as the hips rise.
                        // A still crouch into a run leaves the live sprint arms. The drop timer stays dt/0.16.
                        if (!_runFromStill)
                        {
                        float up = 1f - _dropVis;
                        float openGait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                        float openAmp = Mathf.Lerp(36f, 64f, openGait);
                        float openOut = Mathf.Lerp(12f, 8f, openGait);
                        float openRoll = Mathf.Lerp(0f, armZ, openGait);
                        float openReachY = openOut + 6f;
                        float openTurn = Mathf.Abs(_turnVis) * 5f;
                        float openYL = Mathf.Lerp(openOut, openReachY, Mathf.Clamp01(-sinC) * openGait) + openTurn;
                        float openYR = Mathf.Lerp(openOut, openReachY, Mathf.Clamp01(sinC) * openGait) + openTurn;
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(Mathf.Lerp(-36f, RunArmPitch(-sinC, openAmp), up), Mathf.Lerp(16f, openYL, up), Mathf.Lerp(armZ, openRoll, up)), hipDrop);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(Mathf.Lerp(-36f, RunArmPitch(sinC, openAmp), up), -Mathf.Lerp(16f, openYR, up), -Mathf.Lerp(armZ, openRoll, up)), hipDrop);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(Mathf.Lerp(-72f, Mathf.Lerp(-6f, -30f, Mathf.Clamp01(sinC) * openGait), up), 0f, 0f), hipDrop);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(Mathf.Lerp(-72f, Mathf.Lerp(-6f, -30f, Mathf.Clamp01(-sinC) * openGait), up), 0f, 0f), hipDrop);
                        }
                    }
                    else
                    {
                        // A sprint leaves the guard as the hips rise, so the arms do not stay folded.
                        // A crouch walk or a still crouch into a walk or a run leaves the live arms. The drop timer stays dt/0.16.
                        float armD = (_walkFromCrouchWalk || _runFromCrouchWalk || _walkFromStill) ? 0f : ((_crouchWalkFromWalk || _crouchWalkFromSprint || _crouchWalkFromStill) ? 1f : (crouchSprintExit ? hipDrop : d));
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), armD);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), armD);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-72f, 0f, 0f), armD);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-72f, 0f, 0f), armD);
                    }
                }
                if (((_skiFromCrouch && !_stillSkiSnap) || (_skiFromCrouchWalk && !_crouchWalkSkiSnap)) && _skiBlend < 0.98f)
                {
                    // The guard eases into the glide. A crouch walk into a ski has its own ease. A still crouch into a ski has its own ease.
                    float intoGlide = _skiBlend;
                    float glideL = RunArmPitch(-sinC, 16f);
                    float glideR = RunArmPitch(sinC, 16f);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-36f, 16f, armZ), _uaL0 * Quaternion.Euler(-18f + glideL, 22f, armZ), intoGlide);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-36f, -16f, -armZ), _uaR0 * Quaternion.Euler(-18f + glideR, -22f, -armZ), intoGlide);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-72f, 0f, 0f), _laL0 * Quaternion.Euler(-12f, 0f, 0f), intoGlide);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-72f, 0f, 0f), _laR0 * Quaternion.Euler(-12f, 0f, 0f), intoGlide);
                }
                if ((skiCrouchExit && !_stillFromSki) || (skiCrouchWalk && !_crouchWalkFromSki))
                {
                    // The glide eases into the guard. A crouch walk keeps these arms. A still crouch keeps its snapshot.
                    float intoGuard = 1f - _skiBlend;
                    float glideL = RunArmPitch(-sinC, 16f);
                    float glideR = RunArmPitch(sinC, 16f);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-18f + glideL, 22f, armZ), _uaL0 * Quaternion.Euler(-36f, 16f, armZ), intoGuard);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-18f + glideR, -22f, -armZ), _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), intoGuard);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-12f, 0f, 0f), _laL0 * Quaternion.Euler(-72f, 0f, 0f), intoGuard);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-12f, 0f, 0f), _laR0 * Quaternion.Euler(-72f, 0f, 0f), intoGuard);
                }
                if ((_slideToCrouch > 0.02f && !_stillFromSlide) || (_slideToCrouchWalk > 0.02f && !_crouchWalkFromSlide))
                {
                    // The wedge eases into the guard. A crouch walk keeps these arms. A still crouch keeps its snapshot.
                    float intoGuard = 1f - (_slideToCrouch > 0.02f ? _slideToCrouch : _slideToCrouchWalk);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-70f, 28f, armZ), _uaL0 * Quaternion.Euler(-36f, 16f, armZ), intoGuard);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-64f, -28f, -armZ), _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), intoGuard);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-8f, 0f, 0f), _laL0 * Quaternion.Euler(-72f, 0f, 0f), intoGuard);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-6f, 0f, 0f), _laR0 * Quaternion.Euler(-72f, 0f, 0f), intoGuard);
                }
                if (_crouchToSlide > 0.02f || _crouchWalkToSlide > 0.02f)
                {
                    // The guard eases into the wedge. A crouch walk uses the same arms.
                    float intoWedge = 1f - (_crouchToSlide > 0.02f ? _crouchToSlide : _crouchWalkToSlide);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-36f, 16f, armZ), _uaL0 * Quaternion.Euler(-70f, 28f, armZ), intoWedge);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-36f, -16f, -armZ), _uaR0 * Quaternion.Euler(-64f, -28f, -armZ), intoWedge);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-72f, 0f, 0f), _laL0 * Quaternion.Euler(-8f, 0f, 0f), intoWedge);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-72f, 0f, 0f), _laR0 * Quaternion.Euler(-6f, 0f, 0f), intoWedge);
                }
            }

            if (_surfFromCrouch && onSurf && _surfIn < 0.98f)
            {
                // The guard eases onto the wall. It does not snap into the climb or the run.
                float intoSurf = _surfIn;
                _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-36f, 16f, armZ), _uaLT, intoSurf);
                _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-36f, -16f, -armZ), _uaRT, intoSurf);
                _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-72f, 0f, 0f), _laLT, intoSurf);
                _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-72f, 0f, 0f), _laRT, intoSurf);
            }
            if (_surfFromJump && climb && _surfIn < 0.98f)
            {
                // The jump eases into the grab. A crouch onto the wall is unchanged.
                // A wall run is unchanged. The meet time is unchanged.
                float intoGrab = _surfIn;
                Quaternion fromL;
                Quaternion fromR;
                Quaternion fromElL;
                Quaternion fromElR;
                if (_surfFromJumpPush)
                {
                    fromL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                }
                else
                {
                    fromL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                }
                _uaLT = Quaternion.Slerp(fromL, _uaLT, intoGrab);
                _uaRT = Quaternion.Slerp(fromR, _uaRT, intoGrab);
                _laLT = Quaternion.Slerp(fromElL, _laLT, intoGrab);
                _laRT = Quaternion.Slerp(fromElR, _laRT, intoGrab);
            }
            if (_surfFromJumpWall && wallRun && _surfIn < 0.98f)
            {
                // The jump eases into the wall-run attach. A jump into a climb is unchanged.
                // A crouch onto the wall is unchanged. The meet time is unchanged.
                float intoAttach = _surfIn;
                Quaternion fromL;
                Quaternion fromR;
                Quaternion fromElL;
                Quaternion fromElR;
                if (_surfFromJumpWallPush)
                {
                    fromL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                }
                else
                {
                    fromL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                }
                _uaLT = Quaternion.Slerp(fromL, _uaLT, intoAttach);
                _uaRT = Quaternion.Slerp(fromR, _uaRT, intoAttach);
                _laLT = Quaternion.Slerp(fromElL, _laLT, intoAttach);
                _laRT = Quaternion.Slerp(fromElR, _laRT, intoAttach);
            }

            if (_punchTelegraph > 0.02f && !punching)
            {
                // Dummy It cocks before QueuePunch. Same pose as the windup, clear of the chest.
                // The real windup is still only 0.12s; this is the hold pose before QueuePunch.
                float k = Mathf.Clamp01(_punchTelegraph / 0.2f);
                _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-58f, 46f, -armZ), k);
                _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-68f, 0f, 0f), k);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(14f + 8f, -30f, 0f), k); // match windup hip twist
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(leanX + 12f, -36f, leanZ), k); // match windup spine twist
            }

            // Legs
            if (lunging || dashing)
            {
                _ulLT = Quaternion.Slerp(
                    _ulL0 * Quaternion.Euler(16f, 0f, 0f),
                    _ulL0 * Quaternion.Euler(72f, 0f, 0f),
                    dashStretchPose);
                _ulRT = Quaternion.Slerp(
                    _ulR0 * Quaternion.Euler(-6f, 0f, 0f),
                    _ulR0 * Quaternion.Euler(-34f, 0f, 0f),
                    dashStretchPose);
                _llLT = Quaternion.Slerp(
                    _llL0 * Quaternion.Euler(-14f, 0f, 0f),
                    _llL0 * Quaternion.Euler(-62f, 0f, 0f),
                    dashStretchPose);
                _llRT = Quaternion.Slerp(
                    _llR0 * Quaternion.Euler(-8f, 0f, 0f),
                    _llR0 * Quaternion.Euler(-18f, 0f, 0f),
                    dashStretchPose);
                if (_airDashArms && !airDashing && !lunging)
                {
                    // The burst is over. The same lead foot reaches into the stride
                    // under the hips. Collapsing the split reads as a skate.
                    // A walk keeps that stride. A sprint opens the long stride.
                    // A still crouch ends in the guard. A stand keeps the old leave.
                    float w = 1f - Mathf.Clamp01(dashStretchPose);
                    if (dashCrouch && !_crouchFromDash)
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(56f, 0f, 0f), w);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(56f, 0f, 0f), w);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-68f, 0f, 0f), w);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-68f, 0f, 0f), w);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), w);
                    }
                    else if (dashCrouchWalk)
                    {
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), w);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), w);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), w);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), w);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(22f, 0f, 0f), w);
                    }
                    else
                    {
                    float openGait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                    float stride = Mathf.Lerp(0.96f, 1.16f, dashSprint ? openGait : _runVis);
                    float reachGait = dashSprint
                        ? openGait
                        : dashWalk
                            ? Mathf.Max(Mathf.Clamp01(walkAmt), 0.65f)
                            : Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                    float reach = Mathf.Lerp(34f, 58f, reachGait) * stride;
                    float kneeAmt = Mathf.Lerp(48f, 90f, dashSprint ? openGait : _runVis);
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(reach, 0f, 0f), w);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-reach * 0.58f, 0f, 0f), w);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(2f + kneeAmt), 0f, 0f), w);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-2f, 0f, 0f), w);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0, w);
                    }
                }
            }
            else if (jet)
            {
                // Knees slightly extended - hover, not a tuck
                float hover = Mathf.Sin(Time.time * 6.5f) * 5f;
                _ulLT = _ulL0 * Quaternion.Euler(14f + hover, 0f, 8f);
                _ulRT = _ulR0 * Quaternion.Euler(12f - hover, 0f, -8f);
                _llLT = _llL0 * Quaternion.Euler(-10f, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-10f, 0f, 0f);
            }
            else if (mantle)
            {
                // Tuck early, lead-leg plant late - readable vault in TP
                float m = _motor != null ? _motor.MantleProgress : 0.5f;
                _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(72f, 28f, m), 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(58f, 42f, m), 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(Mathf.Lerp(-82f, -22f, m), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(Mathf.Lerp(-64f, -38f, m), 0f, 0f);
            }
            else if (climb)
            {
                // The leg opposite the reaching hand steps up. Same phase as the hands, so the grab does not pop.
                float climbPhase = Mathf.Sin(_surfPhase);
                float up = Mathf.Lerp(0.8f, (climbPhase + 1f) * 0.5f, _surfIn);
                float kneePhase = climbPhase * _surfIn;
                _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(62f, 14f, up), 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(14f, 62f, up), 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(-(6f + Mathf.Max(0f, -kneePhase) * 72f), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(-(6f + Mathf.Max(0f, kneePhase) * 72f), 0f, 0f);
            }
            else if (wallRun)
            {
                // Outer leg steps with the same phase as the wall hand.
                bool left = _motor != null && _motor.WallLeft;
                float wallPhase = Mathf.Lerp(0f, Mathf.Sin(_surfPhase), _surfIn);
                float outerThigh = 10f + wallPhase * 38f;
                float outerKnee = -(6f + Mathf.Max(0f, wallPhase) * 68f);
                if (left)
                {
                    _ulLT = _ulL0 * Quaternion.Euler(16f, 0f, 0f);
                    _ulRT = _ulR0 * Quaternion.Euler(outerThigh, 0f, 0f);
                    _llLT = _llL0 * Quaternion.Euler(-8f, 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(outerKnee, 0f, 0f);
                }
                else
                {
                    _ulRT = _ulR0 * Quaternion.Euler(16f, 0f, 0f);
                    _ulLT = _ulL0 * Quaternion.Euler(outerThigh, 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(-8f, 0f, 0f);
                    _llLT = _llL0 * Quaternion.Euler(outerKnee, 0f, 0f);
                }
            }
            else if (gliding)
            {
                // Crouch-hip tuck in air - bible: crouch in hips even if capsule stands
                float g = glideAmt;
                _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(18f, 58f, g), 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(16f, 52f, g), 0f, 0f);
                _llLT = _llL0 * Quaternion.Euler(Mathf.Lerp(-18f, -48f, g), 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(Mathf.Lerp(-16f, -44f, g), 0f, 0f);
            }
            else if (bouncing)
            {
                // Wall-side leg kicks the face; outer tucks - readable off-wall impulse
                float k = bounceAmt;
                if (_bounceWallLeft)
                {
                    _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(18f, 62f, k), 0f, 12f * k);
                    _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(16f, 28f, k), 0f, -6f * k);
                    _llLT = _llL0 * Quaternion.Euler(Mathf.Lerp(-22f, -58f, k), 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(Mathf.Lerp(-18f, -28f, k), 0f, 0f);
                }
                else
                {
                    _ulLT = _ulL0 * Quaternion.Euler(Mathf.Lerp(16f, 28f, k), 0f, 6f * k);
                    _ulRT = _ulR0 * Quaternion.Euler(Mathf.Lerp(18f, 62f, k), 0f, -12f * k);
                    _llLT = _llL0 * Quaternion.Euler(Mathf.Lerp(-18f, -28f, k), 0f, 0f);
                    _llRT = _llR0 * Quaternion.Euler(Mathf.Lerp(-22f, -58f, k), 0f, 0f);
                }
            }
            else if (air)
            {
                // Tuck on the way up so a hop reads. Lengthen on the way down so a fall is not a skate.
                float airW = Mathf.Clamp01(airRise + airFall);
                float riseShare = airW > 0.001f ? airRise / (airRise + airFall) : 0f;
                Quaternion tuckL = _ulL0 * Quaternion.Euler(58f, 0f, 0f);
                Quaternion tuckR = _ulR0 * Quaternion.Euler(54f, 0f, 0f);
                Quaternion longL = _ulL0 * Quaternion.Euler(4f, 0f, 0f);
                Quaternion longR = _ulR0 * Quaternion.Euler(4f, 0f, 0f);
                Quaternion hangThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion hangThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                _ulLT = Quaternion.Slerp(hangThighL, Quaternion.Slerp(longL, tuckL, riseShare), airW);
                _ulRT = Quaternion.Slerp(hangThighR, Quaternion.Slerp(longR, tuckR, riseShare), airW);
                Quaternion kneeTuckL = _llL0 * Quaternion.Euler(-90f, 0f, 0f);
                Quaternion kneeTuckR = _llR0 * Quaternion.Euler(-86f, 0f, 0f);
                Quaternion kneeLongL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion kneeLongR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion kneeHangL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion kneeHangR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _llLT = Quaternion.Slerp(kneeHangL, Quaternion.Slerp(kneeLongL, kneeTuckL, riseShare), airW);
                _llRT = Quaternion.Slerp(kneeHangR, Quaternion.Slerp(kneeLongR, kneeTuckR, riseShare), airW);
                if (_diveVis > 0.02f)
                {
                    // Knees come up enough to read as a crouch. Still short of the jump tuck and the ground guard.
                    // A moving fall eases into the low stride. A still crouch keeps these knees.
                    float d = _diveVis;
                    if (airDartWalk)
                    {
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), d);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), d);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), d);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), d);
                    }
                    else
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(34f, 0f, 0f), d);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(34f, 0f, 0f), d);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-50f, 0f, 0f), d);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-50f, 0f, 0f), d);
                    }
                }
                if (_pushOff > 0.02f && _diveVis < 0.2f)
                {
                    // The planted foot pushes. The other knee comes up, then the tuck.
                    // A still crouch eases the guard into that push. A crouch walk eases the low stride into it.
                    // A ski eases the glide into it. A slide eases the wedge into it.
                    // A standing jump keeps this push. slideBoost stays 0.
                    float p = _pushOff;
                    if (_jumpFromSki)
                    {
                        float t = 1f - Mathf.Clamp01(p);
                        float intoPush = Mathf.Clamp01(t * 2f);
                        float leave = Mathf.Clamp01(t * 2f - 1f);
                        float glideL = Mathf.Max(0f, sinC);
                        float glideR = Mathf.Max(0f, -sinC);
                        Quaternion skiL = _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f);
                        Quaternion skiR = _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f);
                        Quaternion skiKl = _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f);
                        Quaternion skiKr = _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f);
                        Quaternion pushL;
                        Quaternion pushR;
                        Quaternion pushKl;
                        Quaternion pushKr;
                        if (_pushLeft)
                        {
                            pushL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                            pushKl = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                            pushR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                            pushKr = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                        }
                        else
                        {
                            pushR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                            pushKr = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                            pushL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                            pushKl = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                        }
                        _ulLT = Quaternion.Slerp(Quaternion.Slerp(skiL, pushL, intoPush), _ulLT, leave);
                        _ulRT = Quaternion.Slerp(Quaternion.Slerp(skiR, pushR, intoPush), _ulRT, leave);
                        _llLT = Quaternion.Slerp(Quaternion.Slerp(skiKl, pushKl, intoPush), _llLT, leave);
                        _llRT = Quaternion.Slerp(Quaternion.Slerp(skiKr, pushKr, intoPush), _llRT, leave);
                    }
                    else if (_pushLeft)
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(-8f, 0f, 0f), p);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-6f, 0f, 0f), p);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(48f, 0f, 0f), p);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-62f, 0f, 0f), p);
                    }
                    else
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-8f, 0f, 0f), p);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-6f, 0f, 0f), p);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(48f, 0f, 0f), p);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-62f, 0f, 0f), p);
                    }
                }
                if (_jumpFromSlide && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromSlideIn < 0.98f)
                {
                    // The wedge eases into the jump, then the jump holds.
                    // A ski into a jump keeps its ease. A still crouch and a crouch walk keep their push.
                    // A standing jump keeps the old push. slideBoost stays 0. Jump height is unchanged.
                    float intoJump = _jumpFromSlideIn;
                    _ulLT = Quaternion.Slerp(_slideJumpUlL, _ulLT, intoJump);
                    _ulRT = Quaternion.Slerp(_slideJumpUlR, _ulRT, intoJump);
                    _llLT = Quaternion.Slerp(_slideJumpLlL, _llLT, intoJump);
                    _llRT = Quaternion.Slerp(_slideJumpLlR, _llRT, intoJump);
                }
                if (_jumpFromPunch && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromPunchIn < 0.98f)
                {
                    // The punch eases into the jump, then the jump holds.
                    // Becoming It into a jump keeps its ease. A tag into a jump keeps its ease.
                    // A punch miss into a jump keeps its ease. Windup time is unchanged. Jump height is unchanged.
                    float intoJump = _jumpFromPunchIn;
                    _ulLT = Quaternion.Slerp(_punchJumpUlL, _ulLT, intoJump);
                    _ulRT = Quaternion.Slerp(_punchJumpUlR, _ulRT, intoJump);
                    _llLT = Quaternion.Slerp(_punchJumpLlL, _llLT, intoJump);
                    _llRT = Quaternion.Slerp(_punchJumpLlR, _llRT, intoJump);
                }
                if (_jumpFromCrouchWalk && !_jumpFromStill && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromCrouchWalkIn < 0.98f)
                {
                    // The low stride eases into the jump, then the jump holds.
                    // A still crouch into a jump keeps its push. A walk into a ski keeps its ease.
                    // A crouch walk into an air dash keeps its ease. Jump height is unchanged.
                    float intoJump = _jumpFromCrouchWalkIn;
                    _ulLT = Quaternion.Slerp(_crouchWalkJumpUlL, _ulLT, intoJump);
                    _ulRT = Quaternion.Slerp(_crouchWalkJumpUlR, _ulRT, intoJump);
                    _llLT = Quaternion.Slerp(_crouchWalkJumpLlL, _llLT, intoJump);
                    _llRT = Quaternion.Slerp(_crouchWalkJumpLlR, _llRT, intoJump);
                }
                if (_jumpFromWalk && !_jumpFromCrouchWalk && !_jumpFromStill && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromWalkIn < 0.98f)
                {
                    // The walk eases into the jump, then the jump holds.
                    // A crouch walk into a jump keeps its ease. A still crouch into a jump keeps its push.
                    // A walk into a ski keeps its ease. Jump height is unchanged.
                    float intoJump = _jumpFromWalkIn;
                    _ulLT = Quaternion.Slerp(_walkJumpUlL, _ulLT, intoJump);
                    _ulRT = Quaternion.Slerp(_walkJumpUlR, _ulRT, intoJump);
                    _llLT = Quaternion.Slerp(_walkJumpLlL, _llLT, intoJump);
                    _llRT = Quaternion.Slerp(_walkJumpLlR, _llRT, intoJump);
                }
                if (_jumpFromStill && !_jumpFromCrouchWalk && !_jumpFromWalk && _pushOff > 0.02f && _diveVis < 0.2f && _jumpFromStillIn < 0.98f)
                {
                    // The guard eases into the jump, then the jump holds.
                    // A crouch walk into a jump keeps its ease. A walk into an air dash keeps its ease.
                    // A jump into a still crouch keeps its ease. Jump height is unchanged.
                    float intoJump = _jumpFromStillIn;
                    _ulLT = Quaternion.Slerp(_stillJumpUlL, _ulLT, intoJump);
                    _ulRT = Quaternion.Slerp(_stillJumpUlR, _ulRT, intoJump);
                    _llLT = Quaternion.Slerp(_stillJumpLlL, _llLT, intoJump);
                    _llRT = Quaternion.Slerp(_stillJumpLlR, _llRT, intoJump);
                }
                if (airStillCrouch && !_jumpFromStill)
                {
                    float g = 1f - Mathf.Clamp01(_pushOff);
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(56f, 0f, 0f), g);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(56f, 0f, 0f), g);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-68f, 0f, 0f), g);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-68f, 0f, 0f), g);
                }
                if (airCrouchWalk && !_jumpFromCrouchWalk && !_jumpFromWalk)
                {
                    float stepL = Mathf.Max(0f, sinC);
                    float stepR = Mathf.Max(0f, -sinC);
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), 1f);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), 1f);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), 1f);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), 1f);
                }
            }
            else
            {
                // Recovery leg takes the knee. The back thigh stays shorter than the front reach
                // so the pair does not meet straight under the hips. Stance knee stays nearly straight.
                float stride = Mathf.Lerp(0.96f, 1.16f, _runVis);
                // Keep the long stride while it eases into the walk. A live snap reads as a skate stop.
                float reachGait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_stopGait, _runVis));
                float reach = Mathf.Lerp(34f, 58f, reachGait) * stride;
                float frontL = Mathf.Max(0f, sinC);
                float frontR = Mathf.Max(0f, -sinC);
                float thighL = (frontL - frontR * 0.58f) * reach;
                float thighR = (frontR - frontL * 0.58f) * reach;
                _ulLT = _ulL0 * Quaternion.Euler(thighL, 0f, 0f);
                _ulRT = _ulR0 * Quaternion.Euler(thighR, 0f, 0f);
                float kneeAmt = Mathf.Lerp(48f, 90f, _runVis);
                float kneeL = -(2f + frontL * kneeAmt);
                float kneeR = -(2f + frontR * kneeAmt);
                _llLT = _llL0 * Quaternion.Euler(kneeL, 0f, 0f);
                _llRT = _llR0 * Quaternion.Euler(kneeR, 0f, 0f);
                // First step pushes off the foot that stays down. The other leg reaches into the stride.
                float plantW = stepping ? 1f - Mathf.SmoothStep(0f, 1f, _stepIn) : 0f;
                if (plantW > 0.04f && footSki < 0.35f)
                {
                    if (Mathf.Cos(_cycle) >= 0f)
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-6f, 0f, 0f), plantW);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-4f, 0f, 0f), plantW);
                    }
                    else
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(-6f, 0f, 0f), plantW);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-4f, 0f, 0f), plantW);
                    }
                }
                float pushW = (_sprintIn < 0.98f && stepping && footSki < 0.35f) ? 1f - Mathf.SmoothStep(0f, 1f, _sprintIn) : 0f;
                if (pushW > 0.04f)
                {
                    // The back foot pushes. A walk turn plants the outside foot, then the sprint opens.
                    bool pushLeft = _sprintFromTurn ? _sprintOutLeft : sinC < 0f;
                    if (pushLeft)
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(-6f, 0f, 0f), pushW);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-4f, 0f, 0f), pushW);
                    }
                    else
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-6f, 0f, 0f), pushW);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-4f, 0f, 0f), pushW);
                    }
                }
                if (stopping && _stopPlant > 0.02f && footSki < 0.35f && _dropVis < 0.35f)
                {
                    // Last foot under the hip before the idle sway. The other foot finishes the close.
                    float p = _stopPlant;
                    if (_stopPlantLeft)
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(-4f, 0f, 0f), p);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-4f, 0f, 0f), p);
                    }
                    else
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(-4f, 0f, 0f), p);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-4f, 0f, 0f), p);
                    }
                }
                if (footSki > 0.001f)
                {
                    // Short glide under the hips. The run stride opens as the hips level.
                    float sFrontL = Mathf.Max(0f, sinC);
                    float sFrontR = Mathf.Max(0f, -sinC);
                    float sThighL = (sFrontL - sFrontR * 0.5f) * 32f;
                    float sThighR = (sFrontR - sFrontL * 0.5f) * 32f;
                    float glideW = (_walkFromSki || _runFromSki || _idleFromSki) ? 0f : legSki;
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(sThighL, 0f, 0f), glideW);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(sThighR, 0f, 0f), glideW);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(6f + sFrontL * 32f), 0f, 0f), glideW);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(6f + sFrontR * 32f), 0f, 0f), glideW);
                }
                if (_dropVis > 0.02f)
                {
                    // The stride drops into the pose. On a stand-up the feet come back
                    // under the hips while the chest is still low. The lead foot stays forward.
                    float d = footDrop;
                    if (_dropSlide)
                    {
                        bool leadLeft = sinC >= 0f;
                        float wedgeL = leadLeft ? 74f : -28f;
                        float wedgeR = leadLeft ? -28f : 74f;
                        float bendL = leadLeft ? -94f : -6f;
                        float bendR = leadLeft ? -6f : -94f;
                        float footYawL = leadLeft ? 6f : -4f;
                        float footYawR = leadLeft ? -4f : 6f;
                        if (slideIdleExit)
                        {
                            // Both feet come under the hips. The trail leg does not pop in.
                            float up = 1f - _dropVis;
                            wedgeL = Mathf.Lerp(wedgeL, 8f, up);
                            wedgeR = Mathf.Lerp(wedgeR, 8f, up);
                            bendL = Mathf.Lerp(bendL, -10f, up);
                            bendR = Mathf.Lerp(bendR, -10f, up);
                            footYawL = Mathf.Lerp(footYawL, 0f, up);
                            footYawR = Mathf.Lerp(footYawR, 0f, up);
                        }
                        else if (slideWalkExit)
                        {
                            // The wedge opens into the walk. The feet do not stay split, then pop.
                            float up = 1f - _dropVis;
                            wedgeL = Mathf.Lerp(wedgeL, thighL, up);
                            wedgeR = Mathf.Lerp(wedgeR, thighR, up);
                            bendL = Mathf.Lerp(bendL, kneeL, up);
                            bendR = Mathf.Lerp(bendR, kneeR, up);
                            footYawL = Mathf.Lerp(footYawL, 0f, up);
                            footYawR = Mathf.Lerp(footYawR, 0f, up);
                        }
                        else if (slideSprintExit)
                        {
                            // The wedge opens into the long stride. The feet do not stay split, then pop.
                            float up = 1f - _dropVis;
                            float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                            float openStride = Mathf.Lerp(0.96f, 1.16f, gait);
                            float openReach = Mathf.Lerp(34f, 58f, gait) * openStride;
                            float openFrontL = Mathf.Max(0f, sinC);
                            float openFrontR = Mathf.Max(0f, -sinC);
                            float openKnee = Mathf.Lerp(48f, 90f, gait);
                            wedgeL = Mathf.Lerp(wedgeL, (openFrontL - openFrontR * 0.58f) * openReach, up);
                            wedgeR = Mathf.Lerp(wedgeR, (openFrontR - openFrontL * 0.58f) * openReach, up);
                            bendL = Mathf.Lerp(bendL, -(2f + openFrontL * openKnee), up);
                            bendR = Mathf.Lerp(bendR, -(2f + openFrontR * openKnee), up);
                            footYawL = Mathf.Lerp(footYawL, 0f, up);
                            footYawR = Mathf.Lerp(footYawR, 0f, up);
                        }
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(wedgeL, footYawL, 0f), d);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(wedgeR, footYawR, 0f), d);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(bendL, 0f, 0f), d);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(bendR, 0f, 0f), d);
                    }
                    else if (crouchStandSprint)
                    {
                        // The guard opens into the long stride as the hips rise.
                        // A still crouch into a run leaves the live sprint stride. The drop timer stays dt/0.16.
                        if (!_runFromStill)
                        {
                        float up = 1f - _dropVis;
                        float openGait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                        float openStride = Mathf.Lerp(0.96f, 1.16f, openGait);
                        float openReach = Mathf.Lerp(34f, 58f, openGait) * openStride;
                        float openFrontL = Mathf.Max(0f, sinC);
                        float openFrontR = Mathf.Max(0f, -sinC);
                        float openKnee = Mathf.Lerp(48f, 90f, openGait);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(Mathf.Lerp(56f, (openFrontL - openFrontR * 0.58f) * openReach, up), 0f, 0f), d);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(Mathf.Lerp(56f, (openFrontR - openFrontL * 0.58f) * openReach, up), 0f, 0f), d);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(Mathf.Lerp(-68f, -(2f + openFrontL * openKnee), up), 0f, 0f), d);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(Mathf.Lerp(-68f, -(2f + openFrontR * openKnee), up), 0f, 0f), d);
                        }
                    }
                    else if ((crouch && speed > 0.35f) || crouchSprintExit)
                    {
                        // Short steps under the hips. Both knees stay bent, so it is not a run or a skate.
                        // A sprint opens that step as the hips rise. It does not plant, then pop.
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        float open = crouchSprintExit ? 1f - hipDrop : 0f;
                        float thighBase = Mathf.Lerp(46f, 20f, open);
                        float thighReach = Mathf.Lerp(12f, 34f, open);
                        float kneeBase = Mathf.Lerp(60f, 10f, open);
                        float kneeReach = Mathf.Lerp(8f, 36f, open);
                        // A walk, a run, or a still crouch into a crouch walk writes the low stride now.
                        // A crouch walk into a run leaves the live run. The drop timer stays dt/0.16.
                        float legD = _runFromCrouchWalk ? 0f : ((_crouchWalkFromWalk || _crouchWalkFromSprint || _crouchWalkFromStill) ? 1f : d);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(thighBase + stepL * thighReach - stepR * thighReach * 0.5f, 0f, 0f), legD);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(thighBase + stepR * thighReach - stepL * thighReach * 0.5f, 0f, 0f), legD);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(kneeBase + stepL * kneeReach), 0f, 0f), legD);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(kneeBase + stepR * kneeReach), 0f, 0f), legD);
                    }
                    else
                    {
                        // A crouch walk or a still crouch into a walk leaves the walk stride. The drop timer stays dt/0.16.
                        float plantD = (_walkFromCrouchWalk || _walkFromStill) ? 0f : d;
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(56f, 0f, 0f), plantD);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(56f, 0f, 0f), plantD);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-68f, 0f, 0f), plantD);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-68f, 0f, 0f), plantD);
                    }
                }
                if (Mathf.Abs(_turnVis) > 0.18f && footSki < 0.35f && !(_sprintFromTurn && _sprintIn < 0.98f))
                {
                    // Outside foot plants. Positive turn is to the right, so the left foot stays down.
                    // A walk plants at a medium turn. The old curve stayed soft until the yaw was sharp.
                    // A sprint stride is long, so that plant still arrives sooner. Look speed is unchanged.
                    float turnAbs = Mathf.Abs(_turnVis);
                    float walkW = Mathf.Clamp01((turnAbs - 0.12f) / 0.28f);
                    float sprintW = Mathf.Clamp01((turnAbs - 0.12f) / 0.22f);
                    float w = Mathf.Lerp(walkW, sprintW, Mathf.Clamp01(_runVis));
                    if (_turnVis > 0f)
                    {
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(6f, 0f, 0f), w);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-4f, 0f, 0f), w);
                    }
                    else
                    {
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(6f, 0f, 0f), w);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-4f, 0f, 0f), w);
                    }
                }
                if (_skiFromCrouch && !_stillSkiSnap && _skiBlend < 0.98f)
                {
                    // The guard eases into the short glide. The feet do not pass through a stand.
                    float intoGlide = _skiBlend;
                    float glideFrontL = Mathf.Max(0f, sinC);
                    float glideFrontR = Mathf.Max(0f, -sinC);
                    float glideThighL = (glideFrontL - glideFrontR * 0.5f) * 32f;
                    float glideThighR = (glideFrontR - glideFrontL * 0.5f) * 32f;
                    _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(56f, 0f, 0f), _ulL0 * Quaternion.Euler(glideThighL, 0f, 0f), intoGlide);
                    _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(56f, 0f, 0f), _ulR0 * Quaternion.Euler(glideThighR, 0f, 0f), intoGlide);
                    _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-68f, 0f, 0f), _llL0 * Quaternion.Euler(-(6f + glideFrontL * 32f), 0f, 0f), intoGlide);
                    _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-68f, 0f, 0f), _llR0 * Quaternion.Euler(-(6f + glideFrontR * 32f), 0f, 0f), intoGlide);
                }
                else if (_skiFromCrouchWalk && !_crouchWalkSkiSnap && _skiBlend < 0.98f)
                {
                    // The low stride eases into the short glide. The feet do not stand, then pop.
                    float intoGlide = _skiBlend;
                    float stepL = Mathf.Max(0f, sinC);
                    float stepR = Mathf.Max(0f, -sinC);
                    float glideFrontL = Mathf.Max(0f, sinC);
                    float glideFrontR = Mathf.Max(0f, -sinC);
                    float glideThighL = (glideFrontL - glideFrontR * 0.5f) * 32f;
                    float glideThighR = (glideFrontR - glideFrontL * 0.5f) * 32f;
                    _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), _ulL0 * Quaternion.Euler(glideThighL, 0f, 0f), intoGlide);
                    _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), _ulR0 * Quaternion.Euler(glideThighR, 0f, 0f), intoGlide);
                    _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), _llL0 * Quaternion.Euler(-(6f + glideFrontL * 32f), 0f, 0f), intoGlide);
                    _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), _llR0 * Quaternion.Euler(-(6f + glideFrontR * 32f), 0f, 0f), intoGlide);
                }
                if (skiCrouchExit && !_stillFromSki)
                {
                    // The glide eases into the guard. The feet do not pass through a stand. A still crouch keeps its snapshot.
                    float intoGuard = 1f - _skiBlend;
                    float glideFrontL = Mathf.Max(0f, sinC);
                    float glideFrontR = Mathf.Max(0f, -sinC);
                    float glideThighL = (glideFrontL - glideFrontR * 0.5f) * 32f;
                    float glideThighR = (glideFrontR - glideFrontL * 0.5f) * 32f;
                    _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(glideThighL, 0f, 0f), _ulL0 * Quaternion.Euler(56f, 0f, 0f), intoGuard);
                    _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(glideThighR, 0f, 0f), _ulR0 * Quaternion.Euler(56f, 0f, 0f), intoGuard);
                    _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-(6f + glideFrontL * 32f), 0f, 0f), _llL0 * Quaternion.Euler(-68f, 0f, 0f), intoGuard);
                    _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-(6f + glideFrontR * 32f), 0f, 0f), _llR0 * Quaternion.Euler(-68f, 0f, 0f), intoGuard);
                }
                else if (skiCrouchWalk && !_crouchWalkFromSki)
                {
                    // The glide eases into the low stride. It does not stand, then drop.
                    float intoStride = 1f - _skiBlend;
                    float stepL = Mathf.Max(0f, sinC);
                    float stepR = Mathf.Max(0f, -sinC);
                    float glideFrontL = Mathf.Max(0f, sinC);
                    float glideFrontR = Mathf.Max(0f, -sinC);
                    float glideThighL = (glideFrontL - glideFrontR * 0.5f) * 32f;
                    float glideThighR = (glideFrontR - glideFrontL * 0.5f) * 32f;
                    _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(glideThighL, 0f, 0f), _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), intoStride);
                    _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(glideThighR, 0f, 0f), _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), intoStride);
                    _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-(6f + glideFrontL * 32f), 0f, 0f), _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), intoStride);
                    _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-(6f + glideFrontR * 32f), 0f, 0f), _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), intoStride);
                }
                if (_slideToCrouch > 0.02f && !_stillFromSlide)
                {
                    // The wedge eases into the guard. The feet do not snap under the hips. A still crouch keeps its snapshot.
                    float intoGuard = 1f - _slideToCrouch;
                    bool leadLeft = sinC >= 0f;
                    float wedgeL = leadLeft ? 74f : -28f;
                    float wedgeR = leadLeft ? -28f : 74f;
                    float bendL = leadLeft ? -94f : -6f;
                    float bendR = leadLeft ? -6f : -94f;
                    float footYawL = leadLeft ? 6f : -4f;
                    float footYawR = leadLeft ? -4f : 6f;
                    _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(wedgeL, footYawL, 0f), _ulL0 * Quaternion.Euler(56f, 0f, 0f), intoGuard);
                    _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(wedgeR, footYawR, 0f), _ulR0 * Quaternion.Euler(56f, 0f, 0f), intoGuard);
                    _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(bendL, 0f, 0f), _llL0 * Quaternion.Euler(-68f, 0f, 0f), intoGuard);
                    _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(bendR, 0f, 0f), _llR0 * Quaternion.Euler(-68f, 0f, 0f), intoGuard);
                }
                if (_slideToCrouchWalk > 0.02f && !_crouchWalkFromSlide)
                {
                    // The wedge eases into the low stride. The feet do not snap under the hips.
                    float intoStride = 1f - _slideToCrouchWalk;
                    bool leadLeft = sinC >= 0f;
                    float wedgeL = leadLeft ? 74f : -28f;
                    float wedgeR = leadLeft ? -28f : 74f;
                    float bendL = leadLeft ? -94f : -6f;
                    float bendR = leadLeft ? -6f : -94f;
                    float footYawL = leadLeft ? 6f : -4f;
                    float footYawR = leadLeft ? -4f : 6f;
                    float stepL = Mathf.Max(0f, sinC);
                    float stepR = Mathf.Max(0f, -sinC);
                    _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(wedgeL, footYawL, 0f), _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), intoStride);
                    _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(wedgeR, footYawR, 0f), _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), intoStride);
                    _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(bendL, 0f, 0f), _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), intoStride);
                    _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(bendR, 0f, 0f), _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), intoStride);
                }
                if (_crouchToSlide > 0.02f)
                {
                    // The guard eases into the wedge. The feet do not snap apart.
                    float intoWedge = 1f - _crouchToSlide;
                    bool leadLeft = sinC >= 0f;
                    float wedgeL = leadLeft ? 74f : -28f;
                    float wedgeR = leadLeft ? -28f : 74f;
                    float bendL = leadLeft ? -94f : -6f;
                    float bendR = leadLeft ? -6f : -94f;
                    float footYawL = leadLeft ? 6f : -4f;
                    float footYawR = leadLeft ? -4f : 6f;
                    _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(56f, 0f, 0f), _ulL0 * Quaternion.Euler(wedgeL, footYawL, 0f), intoWedge);
                    _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(56f, 0f, 0f), _ulR0 * Quaternion.Euler(wedgeR, footYawR, 0f), intoWedge);
                    _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-68f, 0f, 0f), _llL0 * Quaternion.Euler(bendL, 0f, 0f), intoWedge);
                    _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-68f, 0f, 0f), _llR0 * Quaternion.Euler(bendR, 0f, 0f), intoWedge);
                }
                if (_crouchWalkToSlide > 0.02f)
                {
                    // The low stride eases into the wedge. The feet do not snap apart.
                    float intoWedge = 1f - _crouchWalkToSlide;
                    bool leadLeft = sinC >= 0f;
                    float wedgeL = leadLeft ? 74f : -28f;
                    float wedgeR = leadLeft ? -28f : 74f;
                    float bendL = leadLeft ? -94f : -6f;
                    float bendR = leadLeft ? -6f : -94f;
                    float footYawL = leadLeft ? 6f : -4f;
                    float footYawR = leadLeft ? -4f : 6f;
                    float stepL = Mathf.Max(0f, sinC);
                    float stepR = Mathf.Max(0f, -sinC);
                    _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), _ulL0 * Quaternion.Euler(wedgeL, footYawL, 0f), intoWedge);
                    _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), _ulR0 * Quaternion.Euler(wedgeR, footYawR, 0f), intoWedge);
                    _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), _llL0 * Quaternion.Euler(bendL, 0f, 0f), intoWedge);
                    _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), _llR0 * Quaternion.Euler(bendR, 0f, 0f), intoWedge);
                }
            }

            if (_surfFromCrouch && onSurf && _surfIn < 0.98f)
            {
                // The guard eases onto the wall. The feet do not snap into the step.
                float intoSurf = _surfIn;
                _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(56f, 0f, 0f), _ulLT, intoSurf);
                _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(56f, 0f, 0f), _ulRT, intoSurf);
                _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-68f, 0f, 0f), _llLT, intoSurf);
                _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-68f, 0f, 0f), _llRT, intoSurf);
            }
            if (_surfFromJump && climb && _surfIn < 0.98f)
            {
                // The jump eases into the grab. The feet do not snap onto the wall.
                float intoGrab = _surfIn;
                Quaternion fromL;
                Quaternion fromR;
                Quaternion fromKl;
                Quaternion fromKr;
                if (_surfFromJumpPush)
                {
                    if (_pushLeft)
                    {
                        fromL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                        fromKl = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                        fromR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                        fromKr = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                    }
                    else
                    {
                        fromR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                        fromKr = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                        fromL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                        fromKl = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                    }
                }
                else
                {
                    fromL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                    fromR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                    fromKl = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                    fromKr = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                }
                _ulLT = Quaternion.Slerp(fromL, _ulLT, intoGrab);
                _ulRT = Quaternion.Slerp(fromR, _ulRT, intoGrab);
                _llLT = Quaternion.Slerp(fromKl, _llLT, intoGrab);
                _llRT = Quaternion.Slerp(fromKr, _llRT, intoGrab);
            }
            if (_surfFromJumpWall && wallRun && _surfIn < 0.98f)
            {
                // The jump eases into the wall-run attach. The feet do not snap onto the wall.
                float intoAttach = _surfIn;
                Quaternion fromL;
                Quaternion fromR;
                Quaternion fromKl;
                Quaternion fromKr;
                if (_surfFromJumpWallPush)
                {
                    if (_pushLeft)
                    {
                        fromL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                        fromKl = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                        fromR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                        fromKr = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                    }
                    else
                    {
                        fromR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                        fromKr = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                        fromL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                        fromKl = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                    }
                }
                else
                {
                    fromL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                    fromR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                    fromKl = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                    fromKr = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                }
                _ulLT = Quaternion.Slerp(fromL, _ulLT, intoAttach);
                _ulRT = Quaternion.Slerp(fromR, _ulRT, intoAttach);
                _llLT = Quaternion.Slerp(fromKl, _llLT, intoAttach);
                _llRT = Quaternion.Slerp(fromKr, _llRT, intoAttach);
            }

            if (_dropVis > 0.02f && !air && !dashing && !lunging && !jet && !wallRun && !climb && !mantle && !punching)
            {
                // Chest and hips follow the drop, then rise back into the stride.
                // A slide stand-up eases the hips so they do not pop flat.
                // Letting go of a still crouch eases them into the idle breath.
                float d = (_dropSlide || crouchIdleExit || crouchWalkExit || crouchStandSprint) ? hipDrop : _dropVis;
                // A walk, a run, or a still crouch into a crouch walk writes the guard pitch now. The drop timer stays dt/0.16.
                if (_crouchWalkFromWalk || _crouchWalkFromSprint || _crouchWalkFromStill)
                    d = 1f;
                if (_walkFromCrouchWalk || _runFromCrouchWalk || _walkFromStill || _runFromStill)
                    d = 0f;
                float chest = _dropSlide ? 62f : 10f;
                float hip = _dropSlide ? 50f : 22f;
                float head = _dropSlide ? -12f : -6f;
                if (slideIdleExit)
                {
                    // Rise into the idle breath. Holding the wedge pitch pops the hips flat.
                    float up = 1f - _dropVis;
                    chest = Mathf.Lerp(62f, 0f, up);
                    hip = Mathf.Lerp(50f, 0f, up);
                    head = Mathf.Lerp(-12f, 0f, up);
                }
                else if (slideWalkExit)
                {
                    // Rise into the walk. Holding the wedge pitch pops the hips flat.
                    float up = 1f - _dropVis;
                    chest = Mathf.Lerp(62f, leanX, up);
                    hip = Mathf.Lerp(50f, 0f, up);
                    head = Mathf.Lerp(-12f, -breath * 0.4f, up);
                }
                else if (slideSprintExit)
                {
                    // Rise into the sprint. Holding the wedge pitch pops the hips flat.
                    float up = 1f - _dropVis;
                    chest = Mathf.Lerp(62f, leanX, up);
                    hip = Mathf.Lerp(50f, 0f, up);
                    head = Mathf.Lerp(-12f, -breath * 0.4f, up);
                }
                else if (crouchStandSprint)
                {
                    // Rise into the sprint. Holding the guard pitch pops the hips flat.
                    float up = 1f - _dropVis;
                    chest = Mathf.Lerp(10f, leanX, up);
                    hip = Mathf.Lerp(22f, 0f, up);
                    head = Mathf.Lerp(-6f, -breath * 0.4f, up);
                }
                _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(chest, 0f, 0f), d);
                _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(hip, 0f, 0f), d);
                _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(head, 0f, 0f), d);
            }

            if (((_skiFromCrouch && !_stillSkiSnap) || (_skiFromCrouchWalk && !_crouchWalkSkiSnap)) && _skiBlend < 0.98f && !air && !dashing && !lunging && !jet && !sliding && !punching)
            {
                // The guard pitch eases into the glide. The hips do not pop flat. A crouch walk into a ski has its own ease. A still crouch into a ski has its own ease.
                float intoGlide = _skiBlend;
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(10f, 0f, 0f), _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-6f, 0f, 0f), _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
            }
            else if (((skiCrouchExit && !_stillFromSki) || (skiCrouchWalk && !_crouchWalkFromSki)) && !air && !dashing && !lunging && !jet && !sliding && !punching)
            {
                // The glide pitch eases into the guard. The hips do not pop flat. A still crouch keeps its snapshot.
                float intoGuard = 1f - _skiBlend;
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(26f, 0f, 0f), _spine0 * Quaternion.Euler(10f, 0f, 0f), intoGuard);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(14f, 0f, 0f), _hips0 * Quaternion.Euler(22f, 0f, 0f), intoGuard);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), _head0 * Quaternion.Euler(-6f, 0f, 0f), intoGuard);
            }
            if (((_slideToCrouch > 0.02f && !_stillFromSlide) || (_slideToCrouchWalk > 0.02f && !_crouchWalkFromSlide)) && !air && !dashing && !lunging && !jet && !sliding && !punching)
            {
                // The wedge pitch eases into the guard. A crouch walk keeps this pitch. A still crouch keeps its snapshot.
                float intoGuard = 1f - (_slideToCrouch > 0.02f ? _slideToCrouch : _slideToCrouchWalk);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(62f, 0f, 0f), _spine0 * Quaternion.Euler(10f, 0f, 0f), intoGuard);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(50f, 0f, 0f), _hips0 * Quaternion.Euler(22f, 0f, 0f), intoGuard);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-12f, 0f, 0f), _head0 * Quaternion.Euler(-6f, 0f, 0f), intoGuard);
            }
            if ((_crouchToSlide > 0.02f || _crouchWalkToSlide > 0.02f) && !air && !dashing && !lunging && !jet && !punching)
            {
                // The guard pitch eases into the wedge. A crouch walk keeps this pitch.
                float intoWedge = 1f - (_crouchToSlide > 0.02f ? _crouchToSlide : _crouchWalkToSlide);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(10f, 0f, 0f), _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-6f, 0f, 0f), _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
            }
            if (_skiToSlide > 0.02f && sliding && !punching)
            {
                // The glide eases into the wedge. A walk into a ski is unchanged.
                float intoWedge = 1f - _skiToSlide;
                float glideL = RunArmPitch(-sinC, 16f);
                float glideR = RunArmPitch(sinC, 16f);
                bool leadLeft = sinC >= 0f;
                float wedgeL = leadLeft ? 74f : -28f;
                float wedgeR = leadLeft ? -28f : 74f;
                float bendL = leadLeft ? -94f : -6f;
                float bendR = leadLeft ? -6f : -94f;
                float footYawL = leadLeft ? 6f : -4f;
                float footYawR = leadLeft ? -4f : 6f;
                float glideFrontL = Mathf.Max(0f, sinC);
                float glideFrontR = Mathf.Max(0f, -sinC);
                _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-18f + glideL, 22f, armZ), _uaL0 * Quaternion.Euler(-70f, 28f, armZ), intoWedge);
                _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-18f + glideR, -22f, -armZ), _uaR0 * Quaternion.Euler(-64f, -28f, -armZ), intoWedge);
                _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-12f, 0f, 0f), _laL0 * Quaternion.Euler(-8f, 0f, 0f), intoWedge);
                _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-12f, 0f, 0f), _laR0 * Quaternion.Euler(-6f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler((glideFrontL - glideFrontR * 0.5f) * 32f, 0f, 0f), _ulL0 * Quaternion.Euler(wedgeL, footYawL, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler((glideFrontR - glideFrontL * 0.5f) * 32f, 0f, 0f), _ulR0 * Quaternion.Euler(wedgeR, footYawR, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-(6f + glideFrontL * 32f), 0f, 0f), _llL0 * Quaternion.Euler(bendL, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-(6f + glideFrontR * 32f), 0f, 0f), _llR0 * Quaternion.Euler(bendR, 0f, 0f), intoWedge);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(26f, 0f, 0f), _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(14f, 0f, 0f), _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
            }
            if (_slideToSki && !_skiFromSlide && skiing && _skiBlend < 0.98f && !punching)
            {
                // The wedge eases into the glide. A ski into a slide is unchanged.
                float intoGlide = _skiBlend;
                float glideL = RunArmPitch(-sinC, 16f);
                float glideR = RunArmPitch(sinC, 16f);
                bool leadLeft = sinC >= 0f;
                float wedgeL = leadLeft ? 74f : -28f;
                float wedgeR = leadLeft ? -28f : 74f;
                float bendL = leadLeft ? -94f : -6f;
                float bendR = leadLeft ? -6f : -94f;
                float footYawL = leadLeft ? 6f : -4f;
                float footYawR = leadLeft ? -4f : 6f;
                float glideFrontL = Mathf.Max(0f, sinC);
                float glideFrontR = Mathf.Max(0f, -sinC);
                _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-70f, 28f, armZ), _uaL0 * Quaternion.Euler(-18f + glideL, 22f, armZ), intoGlide);
                _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-64f, -28f, -armZ), _uaR0 * Quaternion.Euler(-18f + glideR, -22f, -armZ), intoGlide);
                _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-8f, 0f, 0f), _laL0 * Quaternion.Euler(-12f, 0f, 0f), intoGlide);
                _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-6f, 0f, 0f), _laR0 * Quaternion.Euler(-12f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(wedgeL, footYawL, 0f), _ulL0 * Quaternion.Euler((glideFrontL - glideFrontR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(wedgeR, footYawR, 0f), _ulR0 * Quaternion.Euler((glideFrontR - glideFrontL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(bendL, 0f, 0f), _llL0 * Quaternion.Euler(-(6f + glideFrontL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(bendR, 0f, 0f), _llR0 * Quaternion.Euler(-(6f + glideFrontR * 32f), 0f, 0f), intoGlide);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(62f, 0f, 0f), _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(50f, 0f, 0f), _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-12f, 0f, 0f), _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
            }
            if (_skiFromJump && skiing && _skiBlend < 0.98f && !punching)
            {
                // The jump eases into the ski stride. A landing uses the absorb. The air glide uses the hang.
                // A walk, a sprint, a crouch, and a slide keep their entry. Ski speed is unchanged.
                float intoGlide = _skiBlend;
                float glideL = RunArmPitch(-sinC, 16f);
                float glideR = RunArmPitch(sinC, 16f);
                float glideFrontL = Mathf.Max(0f, sinC);
                float glideFrontR = Mathf.Max(0f, -sinC);
                Quaternion fromL;
                Quaternion fromR;
                Quaternion fromElL;
                Quaternion fromElR;
                Quaternion fromThighL;
                Quaternion fromThighR;
                Quaternion fromKneeL;
                Quaternion fromKneeR;
                Quaternion fromSp;
                Quaternion fromHp;
                Quaternion fromHd;
                if (_skiFromJumpLand)
                {
                    fromL = _uaL0 * Quaternion.Euler(-40f, 18f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-40f, -18f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
                    fromThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    fromThighR = _ulR0 * Quaternion.Euler(40f, 0f, 0f);
                    fromKneeL = _llL0 * Quaternion.Euler(-78f, 0f, 0f);
                    fromKneeR = _llR0 * Quaternion.Euler(-70f, 0f, 0f);
                    fromSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                    fromHp = _hips0 * Quaternion.Euler(18f, 0f, 0f);
                    fromHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                }
                else
                {
                    fromL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                    fromThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                    fromThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                    fromKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                    fromKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                    fromSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                    fromHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                    fromHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                }
                _uaLT = Quaternion.Slerp(fromL, _uaL0 * Quaternion.Euler(-18f + glideL, 22f, armZ), intoGlide);
                _uaRT = Quaternion.Slerp(fromR, _uaR0 * Quaternion.Euler(-18f + glideR, -22f, -armZ), intoGlide);
                _laLT = Quaternion.Slerp(fromElL, _laL0 * Quaternion.Euler(-12f, 0f, 0f), intoGlide);
                _laRT = Quaternion.Slerp(fromElR, _laR0 * Quaternion.Euler(-12f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(fromThighL, _ulL0 * Quaternion.Euler((glideFrontL - glideFrontR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(fromThighR, _ulR0 * Quaternion.Euler((glideFrontR - glideFrontL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(fromKneeL, _llL0 * Quaternion.Euler(-(6f + glideFrontL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(fromKneeR, _llR0 * Quaternion.Euler(-(6f + glideFrontR * 32f), 0f, 0f), intoGlide);
                _spineT = Quaternion.Slerp(fromSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(fromHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(fromHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
            }
            if (_jumpToSlide > 0.02f && sliding && !punching)
            {
                // The jump eases into the wedge. A landing uses the absorb. The air glide uses the hang.
                // A crouch into a slide and a ski into a slide keep their entry. slideBoost stays 0.
                float intoWedge = 1f - _jumpToSlide;
                bool leadLeft = sinC >= 0f;
                Quaternion fromL;
                Quaternion fromR;
                Quaternion fromElL;
                Quaternion fromElR;
                Quaternion fromThighL;
                Quaternion fromThighR;
                Quaternion fromKneeL;
                Quaternion fromKneeR;
                Quaternion fromSp;
                Quaternion fromHp;
                Quaternion fromHd;
                if (_jumpToSlideLand)
                {
                    fromL = _uaL0 * Quaternion.Euler(-40f, 18f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-40f, -18f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
                    fromThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    fromThighR = _ulR0 * Quaternion.Euler(40f, 0f, 0f);
                    fromKneeL = _llL0 * Quaternion.Euler(-78f, 0f, 0f);
                    fromKneeR = _llR0 * Quaternion.Euler(-70f, 0f, 0f);
                    fromSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                    fromHp = _hips0 * Quaternion.Euler(18f, 0f, 0f);
                    fromHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                }
                else
                {
                    fromL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                    fromThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                    fromThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                    fromKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                    fromKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                    fromSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                    fromHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                    fromHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                }
                _uaLT = Quaternion.Slerp(fromL, _uaL0 * Quaternion.Euler(-70f, 28f, armZ), intoWedge);
                _uaRT = Quaternion.Slerp(fromR, _uaR0 * Quaternion.Euler(-64f, -28f, -armZ), intoWedge);
                _laLT = Quaternion.Slerp(fromElL, _laL0 * Quaternion.Euler(-8f, 0f, 0f), intoWedge);
                _laRT = Quaternion.Slerp(fromElR, _laR0 * Quaternion.Euler(-6f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(fromThighL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(fromThighR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(fromKneeL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(fromKneeR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
                _spineT = Quaternion.Slerp(fromSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(fromHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(fromHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
            }
            if (_jumpFromDash && _pushOff > 0.02f && !punching)
            {
                // The burst eases into the push, then the air pose.
                // A still crouch, a crouch walk, a ski, and a slide keep their jump.
                // Duration and cooldown are unchanged. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion burstL = _uaL0 * Quaternion.Euler(108f, 32f, armZ);
                Quaternion burstR = _uaR0 * Quaternion.Euler(108f, -32f, -armZ);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion burstEl = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion burstElR = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion pushEl = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion pushElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airEl = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion airElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(burstL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(burstR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(burstEl, pushEl, intoPush), airEl, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(burstElR, pushElR, intoPush), airElR, leave);
                Quaternion burstSp = _spine0 * Quaternion.Euler(58f, 0f, 0f);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion burstHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion burstHd = _head0 * Quaternion.Euler(8f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(burstSp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(burstHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(burstHd, pushHd, intoPush), airHd, leave);
                Quaternion burstThighL = _ulL0 * Quaternion.Euler(72f, 0f, 0f);
                Quaternion burstThighR = _ulR0 * Quaternion.Euler(-34f, 0f, 0f);
                Quaternion burstKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                Quaternion burstKneeR = _llR0 * Quaternion.Euler(-18f, 0f, 0f);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(burstThighL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(burstThighR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(burstKneeL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(burstKneeR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (_surfFromCrouch && onSurf && _surfIn < 0.98f && !punching)
            {
                // The guard pitch eases onto the wall. The hips do not pop flat.
                float intoSurf = _surfIn;
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(10f, 0f, 0f), _spineT, intoSurf);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hipsT, intoSurf);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-6f, 0f, 0f), _headT, intoSurf);
            }
            if (_surfFromJump && climb && _surfIn < 0.98f && !punching)
            {
                // The jump pitch eases into the grab. The hips do not pop onto the wall.
                float intoGrab = _surfIn;
                Quaternion fromSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion fromHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion fromHd = _head0 * Quaternion.Euler(_surfFromJumpPush ? 0f : -6f, 0f, 0f);
                _spineT = Quaternion.Slerp(fromSp, _spineT, intoGrab);
                _hipsT = Quaternion.Slerp(fromHp, _hipsT, intoGrab);
                _headT = Quaternion.Slerp(fromHd, _headT, intoGrab);
            }
            if (_surfFromJumpWall && wallRun && _surfIn < 0.98f && !punching)
            {
                // The jump pitch eases into the wall-run attach. The hips do not pop onto the wall.
                float intoAttach = _surfIn;
                Quaternion fromSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion fromHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion fromHd = _head0 * Quaternion.Euler(_surfFromJumpWallPush ? 0f : -6f, 0f, 0f);
                _spineT = Quaternion.Slerp(fromSp, _spineT, intoAttach);
                _hipsT = Quaternion.Slerp(fromHp, _hipsT, intoAttach);
                _headT = Quaternion.Slerp(fromHd, _headT, intoAttach);
            }

            if (wallRun || climb)
            {
                _wallExit = 1f;
                _exitFromWall = wallRun;
                _exitIntoWalk = false;
                _exitIntoSprint = false;
                _exitIntoCrouch = false;
                _exitIntoCrouchWalk = false;
                if (climb)
                {
                    float up = Mathf.Lerp(0.8f, (Mathf.Sin(_surfPhase) + 1f) * 0.5f, _surfIn);
                    _exitLeadLeft = up < 0.5f;
                }
                else
                    _exitLeadLeft = _motor == null || !_motor.WallLeft;
                _exitUaL = _uaLT;
                _exitUaR = _uaRT;
                _exitLaL = _laLT;
                _exitLaR = _laRT;
                _exitUlL = _ulLT;
                _exitUlR = _ulRT;
                _exitLlL = _llLT;
                _exitLlR = _llRT;
                _exitSpine = _spineT;
                _exitHips = _hipsT;
            }
            else if (dashing || punching || sliding || jet || mantle)
            {
                _wallExit = 0f;
                _exitIntoWalk = false;
                _exitIntoSprint = false;
                _exitIntoCrouch = false;
                _exitIntoCrouchWalk = false;
            }
            else if (_wallExit > 0f)
            {
                // Hands keep the full exit. Hips and feet ease into the stride
                // so the wall roll does not pop. Exit time is unchanged.
                // A wall run or a climb into a walk settles the hands with the feet.
                // A wall run or a climb into a sprint opens the hands into the long stride.
                // They do not stay on the surface and then hitch. A drop keeps the old leave.
                // A climb into a still crouch eases into the guard. A wall run does the same.
                // A climb into a crouch walk eases into the low stride. A wall run does the same.
                // A walk and a sprint leave are unchanged. Exit time is unchanged.
                if (leavingSurf && speed <= 0.35f && _input != null && _input.CrouchHeld)
                    _exitIntoCrouch = true;
                if (leavingSurf && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint && _input != null && _input.CrouchHeld)
                    _exitIntoCrouchWalk = true;
                if (leavingSurf && grounded && !air && !crouch && speed > 0.35f)
                {
                    if (st == MoveState.Sprint || speed > 5.5f)
                        _exitIntoSprint = true;
                    else
                        _exitIntoWalk = true;
                }
                _wallExit = Mathf.MoveTowards(_wallExit, 0f, dt / 0.18f);
                // A climb into a ski eases in its own overlay. A wall run into a ski does too.
                // The leave timer still runs.
                if (!_skiFromClimb && !_skiFromWall)
                {
                float w = _wallExit;
                float body = Mathf.SmoothStep(0f, 1f, w);
                float handW = _exitIntoWalk ? body : w;
                if (_exitIntoCrouch || _exitIntoCrouchWalk)
                {
                    // A climb into a still crouch keeps its snapshot. A wall exit into a still crouch keeps its own.
                    // A crouch walk keeps this leave. Exit time is unchanged.
                    if (!((_stillFromClimb && !_exitFromWall) || (_stillFromWall && _exitFromWall)) || _exitIntoCrouchWalk)
                    {
                    // The leave eases into the guard. A crouch walk keeps these arms and opens the low stride.
                    float intoGuard = 1f - body;
                    _uaLT = Quaternion.Slerp(_exitUaL, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), intoGuard);
                    _uaRT = Quaternion.Slerp(_exitUaR, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), intoGuard);
                    _laLT = Quaternion.Slerp(_exitLaL, _laL0 * Quaternion.Euler(-72f, 0f, 0f), intoGuard);
                    _laRT = Quaternion.Slerp(_exitLaR, _laR0 * Quaternion.Euler(-72f, 0f, 0f), intoGuard);
                    if (_exitIntoCrouchWalk)
                    {
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        _ulLT = Quaternion.Slerp(_exitUlL, _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), intoGuard);
                        _ulRT = Quaternion.Slerp(_exitUlR, _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), intoGuard);
                        _llLT = Quaternion.Slerp(_exitLlL, _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), intoGuard);
                        _llRT = Quaternion.Slerp(_exitLlR, _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), intoGuard);
                    }
                    else
                    {
                        _ulLT = Quaternion.Slerp(_exitUlL, _ulL0 * Quaternion.Euler(56f, 0f, 0f), intoGuard);
                        _ulRT = Quaternion.Slerp(_exitUlR, _ulR0 * Quaternion.Euler(56f, 0f, 0f), intoGuard);
                        _llLT = Quaternion.Slerp(_exitLlL, _llL0 * Quaternion.Euler(-68f, 0f, 0f), intoGuard);
                        _llRT = Quaternion.Slerp(_exitLlR, _llR0 * Quaternion.Euler(-68f, 0f, 0f), intoGuard);
                    }
                    _spineT = Quaternion.Slerp(_exitSpine, _spine0 * Quaternion.Euler(10f, 0f, 0f), intoGuard);
                    _hipsT = Quaternion.Slerp(_exitHips, _hips0 * Quaternion.Euler(22f, 0f, 0f), intoGuard);
                    _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-6f, 0f, 0f), intoGuard);
                    }
                }
                else if (_exitIntoSprint)
                {
                    float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), 0.85f);
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = outY + 6f;
                    float turnOut = Mathf.Abs(_turnVis) * 5f;
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                    float pitchL = RunArmPitch(-sinC, amp);
                    float pitchR = RunArmPitch(sinC, amp);
                    float elbowReach = -6f;
                    float elbowPull = -30f;
                    float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(pitchL, yL, roll), _exitUaL, body);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(pitchR, -yR, -roll), _exitUaR, body);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _exitLaL, body);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _exitLaR, body);
                }
                else
                {
                    _uaLT = Quaternion.Slerp(_uaLT, _exitUaL, handW);
                    _uaRT = Quaternion.Slerp(_uaRT, _exitUaR, handW);
                    _laLT = Quaternion.Slerp(_laLT, _exitLaL, handW);
                    _laRT = Quaternion.Slerp(_laRT, _exitLaR, handW);
                }
                if (!_exitIntoCrouch && !_exitIntoCrouchWalk)
                {
                    _ulLT = Quaternion.Slerp(_ulLT, _exitUlL, body);
                    _ulRT = Quaternion.Slerp(_ulRT, _exitUlR, body);
                    _llLT = Quaternion.Slerp(_llLT, _exitLlL, body);
                    _llRT = Quaternion.Slerp(_llRT, _exitLlR, body);
                    _spineT = Quaternion.Slerp(_spineT, _exitSpine, body);
                    _hipsT = Quaternion.Slerp(_hipsT, _exitHips, body);
                }
                }
            }

            if (_jumpFromClimb && _pushOff > 0.02f && !punching)
            {
                // The climb eases into the push, then the air pose.
                // A still crouch, a crouch walk, a ski, a slide, and an air dash keep their jump.
                // Exit time is unchanged. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion pushElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion pushElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion airElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(_exitUaL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(_exitUaR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(_exitLaL, pushElL, intoPush), airElL, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(_exitLaR, pushElR, intoPush), airElR, leave);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(_exitSpine, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(_exitHips, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(_headT, pushHd, intoPush), airHd, leave);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(_exitUlL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(_exitUlR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(_exitLlL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(_exitLlR, pushKneeR, intoPush), airKneeR, leave);
            }

            if (_jumpFromWall && _pushOff > 0.02f && !punching)
            {
                // The wall run eases into the push, then the air pose. A climb keeps its own leave.
                // Exit time is unchanged. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion pushElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion pushElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion airElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(_exitUaL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(_exitUaR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(_exitLaL, pushElL, intoPush), airElL, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(_exitLaR, pushElR, intoPush), airElR, leave);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(_exitSpine, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(_exitHips, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(_headT, pushHd, intoPush), airHd, leave);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(_exitUlL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(_exitUlR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(_exitLlL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(_exitLlR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (_jumpFromAirCrouch && _pushOff > 0.02f && !punching && !wallRun && !climb)
            {
                // The dart eases into the push, then the air pose. A moving fall uses the low stride.
                // A still crouch, a crouch walk, a ski, a slide, an air dash, a climb, and a wall run keep their jump.
                // Fall speed stays doubled. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion pushElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion pushElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion airElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion fromL;
                Quaternion fromR;
                Quaternion fromElL;
                Quaternion fromElR;
                Quaternion fromSp;
                Quaternion fromHp;
                Quaternion fromHd;
                Quaternion fromThighL;
                Quaternion fromThighR;
                Quaternion fromKneeL;
                Quaternion fromKneeR;
                if (_jumpFromAirCrouchStride)
                {
                    float stepL = Mathf.Max(0f, sinC);
                    float stepR = Mathf.Max(0f, -sinC);
                    fromL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                    fromSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                    fromHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                    fromHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                    fromThighL = _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f);
                    fromThighR = _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f);
                    fromKneeL = _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f);
                    fromKneeR = _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f);
                }
                else
                {
                    fromL = _uaL0 * Quaternion.Euler(-16f, 12f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-16f, -12f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-58f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-58f, 0f, 0f);
                    fromSp = _spine0 * Quaternion.Euler(46f, 0f, 0f);
                    fromHp = _hips0 * Quaternion.Euler(24f, 0f, 0f);
                    fromHd = _head0 * Quaternion.Euler(12f, 0f, 0f);
                    fromThighL = _ulL0 * Quaternion.Euler(34f, 0f, 0f);
                    fromThighR = _ulR0 * Quaternion.Euler(34f, 0f, 0f);
                    fromKneeL = _llL0 * Quaternion.Euler(-50f, 0f, 0f);
                    fromKneeR = _llR0 * Quaternion.Euler(-50f, 0f, 0f);
                }
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(fromL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(fromR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(fromElL, pushElL, intoPush), airElL, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(fromElR, pushElR, intoPush), airElR, leave);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(fromSp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(fromHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(fromHd, pushHd, intoPush), airHd, leave);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(fromThighL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(fromThighR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(fromKneeL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(fromKneeR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (airDashing && _dashFromJump && !_jumpFromDash && _dashFromJumpIn < 0.98f && !punching)
            {
                // The jump eases into the burst, then the burst holds.
                // A slide into an air dash keeps its ease. A ski into an air dash keeps its ease.
                // Jump height is unchanged. Duration and cooldown are unchanged.
                float intoBurst = _dashFromJumpIn;
                _uaLT = Quaternion.Slerp(_jumpDashUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_jumpDashUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_jumpDashLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_jumpDashLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_jumpDashUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_jumpDashUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_jumpDashLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_jumpDashLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_jumpDashSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_jumpDashHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_jumpDashHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromCrouch && !_dashFromJump && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall && !_jumpFromDash && _dashFromCrouchIn < 0.98f && !punching)
            {
                // The guard eases into the burst, then the burst holds.
                // A jump into a dash keeps its ease. An air crouch into a dash keeps its ease.
                // A slide into a dash keeps its ease. Duration and cooldown are unchanged.
                float intoBurst = _dashFromCrouchIn;
                _uaLT = Quaternion.Slerp(_crouchDashUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_crouchDashUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_crouchDashLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_crouchDashLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_crouchDashUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_crouchDashUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_crouchDashLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_crouchDashLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_crouchDashSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_crouchDashHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_crouchDashHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromPunch && !_dashFromCrouch && !_dashFromJump && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall && !_jumpFromDash && _dashFromPunchIn < 0.98f)
            {
                // The punch eases into the burst, then the burst holds.
                // A punch miss into a dash keeps its ease. A tag into a dash keeps its ease.
                // A still crouch into a dash keeps its ease. Windup time is unchanged.
                // Duration and cooldown are unchanged.
                float intoBurst = _dashFromPunchIn;
                _uaLT = Quaternion.Slerp(_punchDashUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_punchDashUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_punchDashLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_punchDashLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_punchDashUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_punchDashUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_punchDashLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_punchDashLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_punchDashSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_punchDashHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_punchDashHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromCrouchWalk && !_dashFromPunch && !_dashFromCrouch && !_dashFromJump && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall && !_jumpFromDash && _dashFromCrouchWalkIn < 0.98f && !punching)
            {
                // The low stride eases into the burst, then the burst holds.
                // A still crouch into a dash keeps its ease. A punch into a dash keeps its ease.
                // A hard landing into a jump keeps its ease. A soft landing into a jump keeps its ease.
                // Duration and cooldown are unchanged.
                float intoBurst = _dashFromCrouchWalkIn;
                _uaLT = Quaternion.Slerp(_crouchWalkDashUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_crouchWalkDashUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_crouchWalkDashLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_crouchWalkDashLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_crouchWalkDashUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_crouchWalkDashUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_crouchWalkDashLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_crouchWalkDashLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_crouchWalkDashSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_crouchWalkDashHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_crouchWalkDashHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromReady && !_dashFromCrouchWalk && !_dashFromPunch && !_dashFromCrouch && !_dashFromJump && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall && !_jumpFromDash && _dashFromReadyIn < 0.98f && !punching)
            {
                // The pulse eases into the burst, then the burst holds.
                // A crouch walk into a dash keeps its ease. A punch into a dash keeps its ease.
                // A dash coming off cooldown into a jump keeps its push.
                // Duration and cooldown are unchanged.
                float intoBurst = _dashFromReadyIn;
                _uaLT = Quaternion.Slerp(_readyDashUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_readyDashUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_readyDashLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_readyDashLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_readyDashUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_readyDashUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_readyDashLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_readyDashLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_readyDashSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_readyDashHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_readyDashHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromWalk && !_dashFromReady && !_dashFromCrouchWalk && !_dashFromPunch && !_dashFromCrouch && !_dashFromJump && !_dashFromDart && !_dashFromSki && !_dashFromSlide && !_dashFromClimb && !_dashFromWall && !_dashFromHard && !_dashFromSoft && !_jumpFromDash && _dashFromWalkIn < 0.98f && !punching)
            {
                // The walk eases into the burst, then the burst holds.
                // A walk into a jump keeps its ease. A crouch walk into a dash keeps its ease.
                // A still crouch into a dash keeps its ease. Duration and cooldown are unchanged.
                float intoBurst = _dashFromWalkIn;
                _uaLT = Quaternion.Slerp(_walkDashUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_walkDashUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_walkDashLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_walkDashLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_walkDashUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_walkDashUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_walkDashLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_walkDashLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_walkDashSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_walkDashHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_walkDashHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromWall && !_dashFromClimb && !_dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromWallIn < 0.98f && !punching)
            {
                // The wall exit eases into the burst, then the burst holds.
                // A climb into a dash keeps its ease.
                // Exit time is unchanged. Duration and cooldown are unchanged.
                float intoBurst = _dashFromWallIn;
                _uaLT = Quaternion.Slerp(_wallUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_wallUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_wallLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_wallLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_wallUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_wallUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_wallLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_wallLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_wallSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_wallHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_wallHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromClimb && !_dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromClimbIn < 0.98f && !punching)
            {
                // The climb eases into the burst, then the burst holds.
                // An air dash into a slide keeps its ease. A ski and a slide into a dash keep their ease.
                // Exit time is unchanged. Duration and cooldown are unchanged.
                float intoBurst = _dashFromClimbIn;
                _uaLT = Quaternion.Slerp(_climbUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_climbUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_climbLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_climbLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_climbUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_climbUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_climbLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_climbLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_climbSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_climbHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_climbHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromSlideIn < 0.98f && !punching)
            {
                // The wedge eases into the burst, then the burst holds.
                // A ski into a dash keeps its ease. A ski into a slide keeps its ease.
                // slideBoost stays 0. Duration and cooldown are unchanged.
                float intoBurst = _dashFromSlideIn;
                _uaLT = Quaternion.Slerp(_slideDashUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_slideDashUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_slideDashLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_slideDashLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_slideDashUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_slideDashUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_slideDashLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_slideDashLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_slideDashSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_slideDashHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_slideDashHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromSkiIn < 0.98f && !punching)
            {
                // The glide eases into the burst, then the burst holds.
                // An air crouch into a dash keeps its ease. Ski speed is unchanged.
                // Duration and cooldown are unchanged.
                float intoSki = _dashFromSkiIn;
                float glideL = Mathf.Max(0f, _skiSin);
                float glideR = Mathf.Max(0f, -_skiSin);
                float skateL = RunArmPitch(-_skiSin, 16f);
                float skateR = RunArmPitch(_skiSin, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                Quaternion skiHp = _hips0 * Quaternion.Euler(14f, 0f, 0f);
                Quaternion skiHd = _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f);
                Quaternion skiThighL = _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f);
                Quaternion skiThighR = _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f);
                Quaternion skiKneeL = _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f);
                Quaternion skiKneeR = _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f);
                _uaLT = Quaternion.Slerp(skiL, _uaLT, intoSki);
                _uaRT = Quaternion.Slerp(skiR, _uaRT, intoSki);
                _laLT = Quaternion.Slerp(skiElL, _laLT, intoSki);
                _laRT = Quaternion.Slerp(skiElR, _laRT, intoSki);
                _ulLT = Quaternion.Slerp(skiThighL, _ulLT, intoSki);
                _ulRT = Quaternion.Slerp(skiThighR, _ulRT, intoSki);
                _llLT = Quaternion.Slerp(skiKneeL, _llLT, intoSki);
                _llRT = Quaternion.Slerp(skiKneeR, _llRT, intoSki);
                _spineT = Quaternion.Slerp(skiSp, _spineT, intoSki);
                _hipsT = Quaternion.Slerp(skiHp, _hipsT, intoSki);
                _headT = Quaternion.Slerp(skiHd, _headT, intoSki);
            }
            if (airDashing && _dashFromDart && !_jumpFromDash && _dashFromDartIn < 0.98f && !punching)
            {
                // The dart eases into the burst, then the burst holds. A moving fall uses the low stride.
                // A jump into a dash keeps its ease. Fall speed stays doubled. Duration and cooldown are unchanged.
                float intoDart = _dashFromDartIn;
                Quaternion fromL;
                Quaternion fromR;
                Quaternion fromElL;
                Quaternion fromElR;
                Quaternion fromSp;
                Quaternion fromHp;
                Quaternion fromHd;
                Quaternion fromThighL;
                Quaternion fromThighR;
                Quaternion fromKneeL;
                Quaternion fromKneeR;
                if (_dashFromDartStride)
                {
                    fromL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                    fromSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                    fromHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                    fromHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                    fromThighL = _ulL0 * Quaternion.Euler(46f + _dartStepL * 12f - _dartStepR * 6f, 0f, 0f);
                    fromThighR = _ulR0 * Quaternion.Euler(46f + _dartStepR * 12f - _dartStepL * 6f, 0f, 0f);
                    fromKneeL = _llL0 * Quaternion.Euler(-(60f + _dartStepL * 8f), 0f, 0f);
                    fromKneeR = _llR0 * Quaternion.Euler(-(60f + _dartStepR * 8f), 0f, 0f);
                }
                else
                {
                    fromL = _uaL0 * Quaternion.Euler(-16f, 12f, armZ);
                    fromR = _uaR0 * Quaternion.Euler(-16f, -12f, -armZ);
                    fromElL = _laL0 * Quaternion.Euler(-58f, 0f, 0f);
                    fromElR = _laR0 * Quaternion.Euler(-58f, 0f, 0f);
                    fromSp = _spine0 * Quaternion.Euler(46f, 0f, 0f);
                    fromHp = _hips0 * Quaternion.Euler(24f, 0f, 0f);
                    fromHd = _head0 * Quaternion.Euler(12f, 0f, 0f);
                    fromThighL = _ulL0 * Quaternion.Euler(34f, 0f, 0f);
                    fromThighR = _ulR0 * Quaternion.Euler(34f, 0f, 0f);
                    fromKneeL = _llL0 * Quaternion.Euler(-50f, 0f, 0f);
                    fromKneeR = _llR0 * Quaternion.Euler(-50f, 0f, 0f);
                }
                _uaLT = Quaternion.Slerp(fromL, _uaLT, intoDart);
                _uaRT = Quaternion.Slerp(fromR, _uaRT, intoDart);
                _laLT = Quaternion.Slerp(fromElL, _laLT, intoDart);
                _laRT = Quaternion.Slerp(fromElR, _laRT, intoDart);
                _ulLT = Quaternion.Slerp(fromThighL, _ulLT, intoDart);
                _ulRT = Quaternion.Slerp(fromThighR, _ulRT, intoDart);
                _llLT = Quaternion.Slerp(fromKneeL, _llLT, intoDart);
                _llRT = Quaternion.Slerp(fromKneeR, _llRT, intoDart);
                _spineT = Quaternion.Slerp(fromSp, _spineT, intoDart);
                _hipsT = Quaternion.Slerp(fromHp, _hipsT, intoDart);
                _headT = Quaternion.Slerp(fromHd, _headT, intoDart);
            }
            if (_jumpFromSoftLand && _pushOff > 0.02f && !punching && !wallRun && !climb)
            {
                // The absorb eases into the push, then the air pose.
                // A hard landing keeps its jump. A still crouch, a crouch walk, a ski, a slide,
                // an air dash, a climb, and a wall run keep their jump.
                // Land time is unchanged when you stay down. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion absorbL = _uaL0 * Quaternion.Euler(-40f, 18f, armZ);
                Quaternion absorbR = _uaR0 * Quaternion.Euler(-40f, -18f, -armZ);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion absorbEl = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion pushEl = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airEl = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(absorbL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(absorbR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(absorbEl, pushEl, intoPush), airEl, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(absorbEl, pushEl, intoPush), airEl, leave);
                Quaternion absorbSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion absorbHp = _hips0 * Quaternion.Euler(18f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion absorbHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(absorbSp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(absorbHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(absorbHd, pushHd, intoPush), airHd, leave);
                Quaternion absorbThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                Quaternion absorbThighR = _ulR0 * Quaternion.Euler(40f, 0f, 0f);
                Quaternion absorbKneeL = _llL0 * Quaternion.Euler(-78f, 0f, 0f);
                Quaternion absorbKneeR = _llR0 * Quaternion.Euler(-70f, 0f, 0f);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(absorbThighL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(absorbThighR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(absorbKneeL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(absorbKneeR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (_jumpFromHardLand && _pushOff > 0.02f && !punching && !wallRun && !climb)
            {
                // The hard absorb eases into the push, then the air pose. A soft landing keeps its jump.
                // A still crouch, a crouch walk, a ski, a slide, an air dash, a climb, and a wall run keep their jump.
                // Land time is unchanged when you stay down. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion absorbL = _uaL0 * Quaternion.Euler(-40f, 18f, armZ);
                Quaternion absorbR = _uaR0 * Quaternion.Euler(-40f, -18f, -armZ);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion absorbEl = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion pushEl = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airEl = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(absorbL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(absorbR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(absorbEl, pushEl, intoPush), airEl, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(absorbEl, pushEl, intoPush), airEl, leave);
                Quaternion absorbSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion absorbHp = _hips0 * Quaternion.Euler(18f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion absorbHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(absorbSp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(absorbHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(absorbHd, pushHd, intoPush), airHd, leave);
                Quaternion absorbThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                Quaternion absorbThighR = _ulR0 * Quaternion.Euler(40f, 0f, 0f);
                Quaternion absorbKneeL = _llL0 * Quaternion.Euler(-78f, 0f, 0f);
                Quaternion absorbKneeR = _llR0 * Quaternion.Euler(-70f, 0f, 0f);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(absorbThighL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(absorbThighR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(absorbKneeL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(absorbKneeR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (_jumpFromMiss && _pushOff > 0.02f && !wallRun && !climb)
            {
                // The whiff eases into the push, then the air pose. A crouch miss keeps its jump.
                // A soft landing and a hard landing keep their jump. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                float r = _missR;
                Quaternion missL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion missR = _uaR0 * Quaternion.Euler(-18f - 40f * r, 10f * r, -8f);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion missElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion missElR = _laR0 * Quaternion.Euler(-14f * r, 0f, 0f);
                Quaternion pushEl = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airEl = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(missL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(missR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(missElL, pushEl, intoPush), airEl, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(missElR, pushEl, intoPush), airEl, leave);
                Quaternion missSp = _spine0 * Quaternion.Euler(6f * r, 0f, 0f);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion missHp = _hips0 * Quaternion.Euler(4f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion missHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(missSp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(missHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(missHd, pushHd, intoPush), airHd, leave);
                Quaternion missThighL = _ulL0 * Quaternion.Euler(14f, 0f, 0f);
                Quaternion missThighR = _ulR0 * Quaternion.Euler(12f, 0f, 0f);
                Quaternion missKneeL = _llL0 * Quaternion.Euler(-16f, 0f, 0f);
                Quaternion missKneeR = _llR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(missThighL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(missThighR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(missKneeL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(missKneeR, pushKneeR, intoPush), airKneeR, leave);
            }

            bool softAfterDash = _airDashArms && !airDashing && _landHard < 0.4f;
            // A punch from this jump eases into the windup. A tag from this jump eases into the connect.
            // Staying down still absorbs. Land time is unchanged.
            if (_landSquash > 0.08f && grounded && !sliding && (!dashing || softAfterDash) && !_punchFromJump && !_tagFromJump && !_punchFromDash && !_tagFromDash && !_punchFromSoft && !_punchFromHard && !_tagFromSoft && !_tagFromHard && !_punchFromSki && !_punchFromSlide && !_tagFromSki && !_tagFromSlide && !_punchFromClimb && !_tagFromClimb && !_punchFromWall && !_tagFromWall && !_punchFromDart && !_tagFromDart && !_punchFromClaim && !_tagFromItClaim && !_punchFromGrapple && !_tagFromGrapple && !_punchFromReady && !_tagFromReady && !_tagFromPunch && !_punchFromTag && !_tagFromMiss && !_skiFromSoft && !_skiFromHard && !_skiFromDart && !_skiFromGrapple && !_skiFromClaim && !_skiFromReady && !_skiFromMiss && !_skiFromWall && !_skiFromDash)
            {
                // A short hop bends the knees and stays in the stride. The arms-out flare
                // is for a hard landing. A sprint brings the arms into the stride under
                // the hips so they do not lock. A stand eases into the idle breath.
                // A soft landing after an air dash absorbs in the knees and keeps the arms
                // in the stride. Hold time is unchanged.
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_landSquash));
                float hard = Mathf.SmoothStep(0f, 1f, _landHard);
                float moving = Mathf.Clamp01(Mathf.Max(walkAmt, runAmt));
                float standing = 1f - moving;
                float kneeBase = Mathf.Lerp(k * 0.62f, k, hard);
                float kRelease = Mathf.Lerp(kneeBase, kneeBase * kneeBase, moving);
                float kneeStand = kneeBase * kneeBase;
                float kL = sinC >= 0f ? Mathf.Lerp(kneeBase, kneeStand, standing) : kRelease;
                float kR = sinC >= 0f ? kRelease : Mathf.Lerp(kneeBase, kneeStand, standing);
                // A soft landing into a walk keeps going. The front knee absorbs and the
                // trail leg stays in the stride, so it does not read as a stop.
                // A stand is unchanged. Hold time is unchanged.
                float walkOn = (1f - hard) * Mathf.Clamp01(walkAmt) * (1f - Mathf.Clamp01(runAmt));
                if (walkOn > 0.02f && st != MoveState.Sprint && !crouch)
                {
                    if (sinC >= 0f)
                    {
                        kL = Mathf.Lerp(kL, kL * 0.5f, walkOn);
                        kR = Mathf.Lerp(kR, kR * kR, walkOn);
                    }
                    else
                    {
                        kR = Mathf.Lerp(kR, kR * 0.5f, walkOn);
                        kL = Mathf.Lerp(kL, kL * kL, walkOn);
                    }
                }
                // A soft landing into a sprint absorbs, then the front leg opens into the stride.
                // A hard landing still absorbs. Hold time is unchanged.
                float sprintSoft = (1f - hard) * (st == MoveState.Sprint ? 1f : 0f);
                if (sprintSoft > 0.02f)
                {
                    float open = 1f - Mathf.Clamp01(k);
                    if (sinC >= 0f)
                    {
                        kL = Mathf.Lerp(kL, kL * kL, sprintSoft * open);
                        kR = Mathf.Lerp(kR, kR * kR, sprintSoft);
                    }
                    else
                    {
                        kR = Mathf.Lerp(kR, kR * kR, sprintSoft * open);
                        kL = Mathf.Lerp(kL, kL * kL, sprintSoft);
                    }
                }
                // A hard landing into a walk absorbs, then the trail leg takes the step.
                // It does not sit in the idle buckle. A sprint opens after the absorb.
                float hardWalk = hard * Mathf.Clamp01(walkAmt) * (1f - Mathf.Clamp01(runAmt));
                if (hardWalk > 0.02f && st != MoveState.Sprint && !crouch)
                {
                    float trail = kneeBase * kneeBase;
                    if (sinC >= 0f)
                    {
                        kL = Mathf.Lerp(kL, kneeBase, hardWalk);
                        kR = Mathf.Lerp(kR, trail, hardWalk);
                    }
                    else
                    {
                        kR = Mathf.Lerp(kR, kneeBase, hardWalk);
                        kL = Mathf.Lerp(kL, trail, hardWalk);
                    }
                }
                // A hard landing into a sprint absorbs, then the front leg opens into the stride.
                // A hard walk still takes one step. Hold time is unchanged.
                float sprintHard = hard * (st == MoveState.Sprint ? 1f : 0f);
                if (sprintHard > 0.02f)
                {
                    float open = 1f - Mathf.Clamp01(k);
                    if (sinC >= 0f)
                    {
                        kL = Mathf.Lerp(kL, kL * kL, sprintHard * open);
                        kR = Mathf.Lerp(kR, kR * kR, sprintHard);
                    }
                    else
                    {
                        kR = Mathf.Lerp(kR, kR * kR, sprintHard * open);
                        kL = Mathf.Lerp(kL, kL * kL, sprintHard);
                    }
                }
                float armK = Mathf.Lerp(kRelease * kRelease, kneeStand * kneeStand, standing) * hard;
                float hipMove = Mathf.Lerp(kneeBase * 0.2f, kRelease, hard);
                float hipK = hipMove * hipMove;
                // A jump into a crouch walk absorbs in the low stride.
                // A soft hop into that walk absorbs lighter. A hard landing absorbs deeper in that stride.
                // A soft landing into a still crouch absorbs in the guard.
                // A hard landing absorbs deeper in that guard. Land time is unchanged.
                bool crouchWalkSoft = crouch && speed > 0.35f && st != MoveState.Sprint && _landHard < 0.4f && _diveVis <= 0.02f;
                bool crouchWalkHard = crouch && speed > 0.35f && st != MoveState.Sprint && _landHard >= 0.4f && _diveVis <= 0.02f;
                bool crouchSoftLand = crouch && speed <= 0.35f && _landHard < 0.4f && _diveVis <= 0.02f;
                bool crouchHardLand = crouch && speed <= 0.35f && _landHard >= 0.4f && _diveVis <= 0.02f;
                if (crouchWalkSoft)
                {
                    float stepL = Mathf.Max(0f, sinC);
                    float stepR = Mathf.Max(0f, -sinC);
                    float absorb = Mathf.Clamp01(k);
                    float thighL = 46f + stepL * 12f - stepR * 6f;
                    float thighR = 46f + stepR * 12f - stepL * 6f;
                    float kneeL = 60f + stepL * 8f;
                    float kneeR = 60f + stepR * 8f;
                    if (sinC >= 0f)
                        kneeL += 8f * absorb;
                    else
                        kneeR += 8f * absorb;
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(thighL, 0f, 0f), kL);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(thighR, 0f, 0f), kR);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-kneeL, 0f, 0f), kL);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-kneeR, 0f, 0f), kR);
                }
                else if (crouchWalkHard)
                {
                    float stepL = Mathf.Max(0f, sinC);
                    float stepR = Mathf.Max(0f, -sinC);
                    float absorb = Mathf.Clamp01(k);
                    float thighL = 46f + stepL * 12f - stepR * 6f;
                    float thighR = 46f + stepR * 12f - stepL * 6f;
                    float kneeL = 60f + stepL * 8f;
                    float kneeR = 60f + stepR * 8f;
                    if (sinC >= 0f)
                    {
                        thighL += 8f * absorb;
                        kneeL += 28f * absorb;
                    }
                    else
                    {
                        thighR += 8f * absorb;
                        kneeR += 28f * absorb;
                    }
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(thighL, 0f, 0f), kL);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(thighR, 0f, 0f), kR);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-kneeL, 0f, 0f), kL);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-kneeR, 0f, 0f), kR);
                }
                else if (crouchSoftLand)
                {
                    if (!_stillFromSoft)
                    {
                        float absorb = Mathf.Clamp01(k);
                        float thigh = Mathf.Lerp(56f, 62f, absorb);
                        float knee = Mathf.Lerp(68f, 82f, absorb);
                        float w = absorb;
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(thigh, 0f, 0f), w);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(thigh, 0f, 0f), w);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-knee, 0f, 0f), w);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-knee, 0f, 0f), w);
                    }
                }
                else if (crouchHardLand)
                {
                    if (!_stillFromHard)
                    {
                        float absorb = Mathf.Clamp01(k);
                        float thigh = Mathf.Lerp(56f, 70f, absorb);
                        float knee = Mathf.Lerp(68f, 96f, absorb);
                        float w = absorb;
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(thigh, 0f, 0f), w);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(thigh, 0f, 0f), w);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-knee, 0f, 0f), w);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-knee, 0f, 0f), w);
                    }
                }
                else
                {
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(48f, 0f, 0f), kL);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(40f, 0f, 0f), kR);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-78f, 0f, 0f), kL);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-70f, 0f, 0f), kR);
                }
                if (_diveVis > 0.02f)
                {
                    // The air crouch eases into the landing. The flare comes in as the dart leaves,
                    // so a soft hop still does not flare and the arms do not pop.
                    // A still crouch lands the dart into the guard. A moving air crouch keeps the flare.
                    // A still crouch without the dart is unchanged.
                    // A soft landing opens the dart into the absorb. A hard landing keeps the flare.
                    // Fall speed and land time are unchanged.
                    bool softOpen = _landHard < 0.4f;
                    float hand = softOpen ? Mathf.SmoothStep(0f, 1f, _diveVis) : _diveVis;
                    bool dartStill = crouch && speed <= 0.35f;
                    if (dartStill)
                    {
                        // The snapshot ease owns this landing. A moving air crouch keeps the flare.
                        if (!_stillFromDart)
                        {
                            float into = 1f - hand;
                            bool hardStill = _landHard >= 0.4f;
                            float elbow = hardStill ? -80f : -72f;
                            float hips = hardStill ? 36f : 26f;
                            float spine = hardStill ? 18f : 12f;
                            float head = hardStill ? -10f : -8f;
                            float absorb = Mathf.Clamp01(k);
                            float thigh = hardStill ? Mathf.Lerp(56f, 70f, absorb) : Mathf.Lerp(56f, 62f, absorb);
                            float knee = hardStill ? Mathf.Lerp(68f, 96f, absorb) : Mathf.Lerp(68f, 82f, absorb);
                            _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-16f, 12f, armZ), hand);
                            _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-16f, -12f, -armZ), hand);
                            _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-58f, 0f, 0f), hand);
                            _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-58f, 0f, 0f), hand);
                            _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), into);
                            _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), into);
                            _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(elbow, 0f, 0f), into);
                            _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(elbow, 0f, 0f), into);
                            _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(46f, 0f, 0f), hand);
                            _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(24f, 0f, 0f), hand);
                            _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(12f, 0f, 0f), hand);
                            _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(spine, 0f, 0f), into);
                            _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(hips, 0f, 0f), into);
                            _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(head, 0f, 0f), into);
                            _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(34f, 0f, 0f), hand);
                            _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(34f, 0f, 0f), hand);
                            _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-50f, 0f, 0f), hand);
                            _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-50f, 0f, 0f), hand);
                            _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(thigh, 0f, 0f), into);
                            _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(thigh, 0f, 0f), into);
                            _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-knee, 0f, 0f), into);
                            _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-knee, 0f, 0f), into);
                        }
                    }
                    else if (softOpen && !crouch)
                    {
                        float into = 1f - hand;
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-16f, 12f, armZ), hand);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-16f, -12f, -armZ), hand);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-58f, 0f, 0f), hand);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-58f, 0f, 0f), hand);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(46f, 0f, 0f), hand);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(24f, 0f, 0f), hand);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(12f, 0f, 0f), hand);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(34f, 0f, 0f), hand);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(34f, 0f, 0f), hand);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-50f, 0f, 0f), hand);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-50f, 0f, 0f), hand);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(48f, 0f, 0f), into * kL);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(40f, 0f, 0f), into * kR);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-78f, 0f, 0f), into * kL);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-70f, 0f, 0f), into * kR);
                    }
                    else
                    {
                        float flareIn = armK * (1f - hand);
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-16f, 12f, armZ), hand);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-16f, -12f, -armZ), hand);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-58f, 0f, 0f), hand);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-58f, 0f, 0f), hand);
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-40f, 18f, armZ), flareIn);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-40f, -18f, -armZ), flareIn);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-22f, 0f, 0f), flareIn);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-22f, 0f, 0f), flareIn);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(46f, 0f, 0f), hand);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(24f, 0f, 0f), hand);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(12f, 0f, 0f), hand);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(26f, 0f, 0f), hipK * (1f - hand));
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(18f, 0f, 0f), hipK * (1f - hand));
                        if (softOpen)
                        {
                            _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(34f, 0f, 0f), hand);
                            _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(34f, 0f, 0f), hand);
                            _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-50f, 0f, 0f), hand);
                            _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-50f, 0f, 0f), hand);
                        }
                    }
                }
                else if (crouchWalkSoft)
                {
                    // A light dip in the low stride. The standing flare would pop the hips up.
                    float dip = Mathf.Clamp01(k);
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), dip);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), dip);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-72f, 0f, 0f), dip);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-72f, 0f, 0f), dip);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(24f, 0f, 0f), dip);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(12f, 0f, 0f), dip);
                    _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-8f, 0f, 0f), dip);
                }
                else if (crouchWalkHard)
                {
                    // Deeper in the low stride. The standing flare would pop the hips up.
                    float dip = Mathf.Clamp01(k);
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), dip);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), dip);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-80f, 0f, 0f), dip);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-80f, 0f, 0f), dip);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(36f, 0f, 0f), dip);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(18f, 0f, 0f), dip);
                    _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-10f, 0f, 0f), dip);
                }
                else if (crouchSoftLand)
                {
                    // Stay in the guard. A still crouch keeps its snapshot. The standing flare would pop the hips up.
                    // Land time is unchanged.
                    if (!_stillFromSoft)
                    {
                        float dip = Mathf.Clamp01(k);
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), dip);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), dip);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-72f, 0f, 0f), dip);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-72f, 0f, 0f), dip);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(26f, 0f, 0f), dip);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(12f, 0f, 0f), dip);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-8f, 0f, 0f), dip);
                    }
                }
                else if (crouchHardLand)
                {
                    // Deeper in the guard. A still crouch keeps its snapshot. The standing flare would pop the hips up.
                    // Land time is unchanged.
                    if (!_stillFromHard)
                    {
                        float dip = Mathf.Clamp01(k);
                        _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-36f, 16f, armZ), dip);
                        _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -16f, -armZ), dip);
                        _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-80f, 0f, 0f), dip);
                        _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-80f, 0f, 0f), dip);
                        _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(36f, 0f, 0f), dip);
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(18f, 0f, 0f), dip);
                        _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-10f, 0f, 0f), dip);
                    }
                }
                else
                {
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-40f, 18f, armZ), armK);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-40f, -18f, -armZ), armK);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-22f, 0f, 0f), armK);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-22f, 0f, 0f), armK);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(18f, 0f, 0f), hipK);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(26f, 0f, 0f), hipK);
                }
                _hardLandWas = crouchHardLand;
                _softLandWas = crouchSoftLand;
            }
            else
            {
                _hardLandWas = false;
                _softLandWas = false;
            }

            // Last frame's air crouch is the dart a still landing eases from.
            // A dash recover stays on the dash path. Fall speed is unchanged.
            _dartStillWas = air && !jet && !airDashing && !(_airDashArms || _armRecover > 0f)
                && _diveVis > 0.02f
                && _input != null && _input.CrouchHeld;

            bool pulling = _grapple != null && _grapple.IsPulling;
            _grapplePose = Mathf.MoveTowards(_grapplePose, pulling ? 1f : 0f, dt / 0.12f);
            if (_grapplePose > 0.04f && !punching && !_skiFromGrapple && !_slideFromGrapple)
            {
                // Experimental rope only. Both arms reach as a long line. The chest and the
                // hips settle together, with no yaw, so the line does not twist.
                // Letting go eases that line into the run or the idle. A steep drop splits
                // the hands and reads as a twist. Legs stay long so it is not a jump tuck.
                // The gate stays off unless the component is added and enableGrapple is turned on.
                float g = Mathf.SmoothStep(0f, 1f, _grapplePose);
                float outW = pulling ? g : g * g;
                // A sprint returns the hands to the long stride. A walk returns them to the walk.
                // A stand keeps the old leave. A still crouch keeps the line for the snapshot ease.
                // A crouch walk keeps that guard and eases into the low stride.
                // The pull is unchanged. The gate stays off.
                bool crouchGrapple = !pulling && grounded && crouch && speed <= 0.35f;
                bool crouchWalkGrapple = !pulling && grounded && crouch && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint;
                float walkGrapple = !pulling && grounded ? Mathf.Clamp01(walkAmt) * (1f - Mathf.Clamp01(runAmt)) : 0f;
                float sprintGrapple = !pulling && grounded && (st == MoveState.Sprint || runAmt > 0.4f) ? 1f : 0f;
                if (sprintGrapple > 0.02f || crouchGrapple || crouchWalkGrapple)
                    walkGrapple = 0f;
                if (crouchGrapple || crouchWalkGrapple)
                    sprintGrapple = 0f;
                if ((crouchGrapple && !_stillFromGrapple) || crouchWalkGrapple)
                {
                    // The line eases into the guard. A still crouch keeps its snapshot. A crouch walk keeps these arms and opens the low stride.
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-36f, 16f, armZ), _uaL0 * Quaternion.Euler(-96f, 16f, armZ), outW);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-36f, -16f, -armZ), _uaR0 * Quaternion.Euler(-96f, -16f, -armZ), outW);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-72f, 0f, 0f), _laL0 * Quaternion.Euler(-14f, 0f, 0f), outW);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-72f, 0f, 0f), _laR0 * Quaternion.Euler(-14f, 0f, 0f), outW);
                    if (crouchWalkGrapple)
                    {
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), _ulL0 * Quaternion.Euler(8f, 0f, 0f), outW);
                        _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), _ulR0 * Quaternion.Euler(6f, 0f, 0f), outW);
                        _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), _llL0 * Quaternion.Euler(-8f, 0f, 0f), outW);
                        _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), _llR0 * Quaternion.Euler(-8f, 0f, 0f), outW);
                    }
                    else
                    {
                        _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(56f, 0f, 0f), _ulL0 * Quaternion.Euler(8f, 0f, 0f), outW);
                        _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(56f, 0f, 0f), _ulR0 * Quaternion.Euler(6f, 0f, 0f), outW);
                        _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-68f, 0f, 0f), _llL0 * Quaternion.Euler(-8f, 0f, 0f), outW);
                        _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-68f, 0f, 0f), _llR0 * Quaternion.Euler(-8f, 0f, 0f), outW);
                    }
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(10f, 0f, 0f), _spine0 * Quaternion.Euler(-12f, 0f, 0f), outW);
                    _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(6f, 0f, 0f), outW);
                    _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-6f, 0f, 0f), _headT, outW);
                }
                else if (sprintGrapple > 0.02f)
                {
                    float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = outY + 6f;
                    float turnOut = Mathf.Abs(_turnVis) * 5f;
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                    float pitchL = RunArmPitch(-sinC, amp);
                    float pitchR = RunArmPitch(sinC, amp);
                    float elbowL = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(-sinC) * gait);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(pitchL, yL, roll), _uaL0 * Quaternion.Euler(-96f, 16f, armZ), outW);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(pitchR, -yR, -roll), _uaR0 * Quaternion.Euler(-96f, -16f, -armZ), outW);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _laL0 * Quaternion.Euler(-14f, 0f, 0f), outW);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _laR0 * Quaternion.Euler(-14f, 0f, 0f), outW);
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX, 0f, leanZ), _spine0 * Quaternion.Euler(-12f, 0f, 0f), outW);
                }
                else if (walkGrapple > 0.02f)
                {
                    float gait = Mathf.Max(Mathf.Clamp01(walkAmt), Mathf.Max(_stopGait, _runVis));
                    gait = Mathf.Lerp(gait, 1f, walkGrapple);
                    float idle = (1f - gait) * (1f - walkGrapple);
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                    float turnOut = Mathf.Abs(_turnVis) * 5f;
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                    float armBreath = breath * 0.55f * idle;
                    float pitchL = RunArmPitch(-sinC, amp) - 12f * idle + armBreath;
                    float pitchR = RunArmPitch(sinC, amp) - 12f * idle + armBreath;
                    float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                    float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                    float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(pitchL, yL, roll), _uaL0 * Quaternion.Euler(-96f, 16f, armZ), outW);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(pitchR, -yR, -roll), _uaR0 * Quaternion.Euler(-96f, -16f, -armZ), outW);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _laL0 * Quaternion.Euler(-14f, 0f, 0f), outW);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _laR0 * Quaternion.Euler(-14f, 0f, 0f), outW);
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX, 0f, leanZ), _spine0 * Quaternion.Euler(-12f, 0f, 0f), outW);
                }
                else
                {
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-96f, 16f, armZ), outW);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-96f, -16f, -armZ), outW);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-14f, 0f, 0f), outW);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-14f, 0f, 0f), outW);
                }
                if (!crouchGrapple && !crouchWalkGrapple)
                {
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(8f, 0f, 0f), outW);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(6f, 0f, 0f), outW);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-8f, 0f, 0f), outW);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-8f, 0f, 0f), outW);
                    if (walkGrapple <= 0.02f && sprintGrapple <= 0.02f)
                        _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(-12f, 0f, 0f), outW);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(6f, 0f, 0f), outW);
                }
            }
            _grappleReleaseWas = !pulling && _grapplePose > 0.2f && !punching && !_skiFromGrapple && !_slideFromGrapple && !_jumpFromGrapple;

            if (flinchAmt > 0f)
            {
                // Tagged runner: a long V in front of the chest. Both knees bend at the hit, so it
                // stays distinct from the new It's one-knee claim. While running, the hands, the
                // chest, and the hips leave together. Standing, they ease into the idle breath
                // so they do not freeze and then pop. Flinch time is unchanged. Mild A only.
                float f = flinchAmt;
                float moving = grounded ? Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)) : 0f;
                float standing = grounded ? 1f - moving : 0f;
                float fRelease = Mathf.Lerp(f, f * f, moving);
                float fHands = Mathf.Lerp(fRelease, f * f, standing);
                float fL = sinC >= 0f ? f : fRelease;
                float fR = sinC >= 0f ? fRelease : f;
                // A walk settles the arms into the stride. A sprint settles them into the long stride.
                // A stand still eases into the idle breath. A still crouch eases into the guard.
                // A crouch walk keeps that guard and eases into the low stride. Flinch time is unchanged.
                bool crouchTag = grounded && crouch && speed <= 0.35f;
                bool crouchWalkTag = grounded && crouch && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint;
                float walkTag = grounded ? Mathf.Clamp01(walkAmt) * (1f - Mathf.Clamp01(runAmt)) : 0f;
                float sprintTag = grounded && (st == MoveState.Sprint || runAmt > 0.4f) ? 1f : 0f;
                if (sprintTag > 0.02f || crouchTag || crouchWalkTag)
                    walkTag = 0f;
                if (crouchTag || crouchWalkTag)
                    sprintTag = 0f;
                if (crouchTag || crouchWalkTag)
                {
                    // The V eases into the guard. A crouch walk keeps these arms and opens the low stride.
                    float hold = f * f;
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-36f, 16f, armZ), _uaL0 * Quaternion.Euler(-78f, 22f, armZ), hold);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-36f, -16f, -armZ), _uaR0 * Quaternion.Euler(-78f, -22f, -armZ), hold);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-72f, 0f, 0f), _laL0 * Quaternion.Euler(-16f, 0f, 0f), hold);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-72f, 0f, 0f), _laR0 * Quaternion.Euler(-16f, 0f, 0f), hold);
                    if (crouchWalkTag)
                    {
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), _ulL0 * Quaternion.Euler(22f, 0f, 0f), hold);
                        _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), _ulR0 * Quaternion.Euler(22f, 0f, 0f), hold);
                        _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), _llL0 * Quaternion.Euler(-48f, 0f, 0f), hold);
                        _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), _llR0 * Quaternion.Euler(-48f, 0f, 0f), hold);
                    }
                    else
                    {
                        _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(56f, 0f, 0f), _ulL0 * Quaternion.Euler(22f, 0f, 0f), hold);
                        _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(56f, 0f, 0f), _ulR0 * Quaternion.Euler(22f, 0f, 0f), hold);
                        _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-68f, 0f, 0f), _llL0 * Quaternion.Euler(-48f, 0f, 0f), hold);
                        _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-68f, 0f, 0f), _llR0 * Quaternion.Euler(-48f, 0f, 0f), hold);
                    }
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(10f, 0f, 0f), _spine0 * Quaternion.Euler(22f, 0f, 0f), hold);
                    _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(8f, 0f, 0f), hold);
                    _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-6f, 0f, 0f), _headT, hold);
                }
                else if (sprintTag > 0.02f)
                {
                    float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = outY + 6f;
                    float turnOut = Mathf.Abs(_turnVis) * 5f;
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                    float pitchL = RunArmPitch(-sinC, amp);
                    float pitchR = RunArmPitch(sinC, amp);
                    float elbowL = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(-sinC) * gait);
                    float hold = f * f;
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(pitchL, yL, roll), _uaL0 * Quaternion.Euler(-78f, 22f, armZ), hold);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(pitchR, -yR, -roll), _uaR0 * Quaternion.Euler(-78f, -22f, -armZ), hold);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _laL0 * Quaternion.Euler(-16f, 0f, 0f), hold);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _laR0 * Quaternion.Euler(-16f, 0f, 0f), hold);
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX, 0f, leanZ), _spine0 * Quaternion.Euler(22f, 0f, 0f), hold);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(8f, 0f, 0f), hold);
                }
                else if (walkTag > 0.02f)
                {
                    float gait = Mathf.Max(Mathf.Clamp01(walkAmt), Mathf.Max(_stopGait, _runVis));
                    gait = Mathf.Lerp(gait, 1f, walkTag);
                    float idle = (1f - gait) * (1f - walkTag);
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                    float turnOut = Mathf.Abs(_turnVis) * 5f;
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                    float armBreath = breath * 0.55f * idle;
                    float pitchL = RunArmPitch(-sinC, amp) - 12f * idle + armBreath;
                    float pitchR = RunArmPitch(sinC, amp) - 12f * idle + armBreath;
                    float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                    float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                    float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                    float hold = Mathf.Lerp(f, f * f, walkTag);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(pitchL, yL, roll), _uaL0 * Quaternion.Euler(-78f, 22f, armZ), hold);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(pitchR, -yR, -roll), _uaR0 * Quaternion.Euler(-78f, -22f, -armZ), hold);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _laL0 * Quaternion.Euler(-16f, 0f, 0f), hold);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _laR0 * Quaternion.Euler(-16f, 0f, 0f), hold);
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX, 0f, leanZ), _spine0 * Quaternion.Euler(22f, 0f, 0f), hold);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(8f, 0f, 0f), hold);
                }
                else
                {
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-78f, 22f, armZ), fHands);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-78f, -22f, -armZ), fHands);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-16f, 0f, 0f), fHands);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-16f, 0f, 0f), fHands);
                }
                if (!crouchTag && !crouchWalkTag)
                {
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(22f, 0f, 0f), fL);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(22f, 0f, 0f), fR);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-48f, 0f, 0f), fL);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-48f, 0f, 0f), fR);
                }
                if (!crouchTag && !crouchWalkTag && walkTag <= 0.02f && sprintTag <= 0.02f)
                {
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(22f, 0f, 0f), fHands);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(8f, 0f, 0f), fHands);
                }
            }
            if (claimAmt > 0f && !_skiFromClaim && !_slideFromClaim && !_jumpFromPunch)
            {
                // New It: one arm up, the other out, chest open. Not the tagged runner's matching V.
                // While moving, the hands and the chest ease into the stride. Standing, they
                // ease into the idle breath so they do not freeze and then pop. The raised
                // knee stays on the claim. Claim time is unchanged.
                // During HitRecover the punch block eases the fist into that claim. Applying it
                // again here would snap the connect away. The knee still comes up immediately.
                float c = claimAmt;
                float moving = grounded ? Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)) : 0f;
                float standing = grounded ? 1f - moving : 0f;
                float cRelease = Mathf.Lerp(c, c * c, moving);
                float cHands = Mathf.Lerp(cRelease, c * c, standing);
                bool punchHandoff = punching && phase == PunchPhase.HitRecover;
                // A sprint settles the arms into the long stride. A walk settles them into the walk.
                // A stand still eases into the idle breath. A still crouch eases into the guard.
                // A crouch walk keeps that guard and eases into the low stride.
                // The raised knee stays on a walk or a sprint. Claim time is unchanged.
                bool crouchClaim = grounded && crouch && speed <= 0.35f;
                bool crouchWalkClaim = grounded && crouch && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint;
                float walkClaim = grounded ? Mathf.Clamp01(walkAmt) * (1f - Mathf.Clamp01(runAmt)) : 0f;
                float sprintClaim = grounded && (st == MoveState.Sprint || runAmt > 0.4f) ? 1f : 0f;
                if (sprintClaim > 0.02f || crouchClaim || crouchWalkClaim)
                    walkClaim = 0f;
                if (crouchClaim || crouchWalkClaim)
                    sprintClaim = 0f;
                if (!punchHandoff && ((crouchClaim && !_stillFromClaim) || crouchWalkClaim))
                {
                    // The claim eases into the guard. A still crouch keeps its snapshot. A crouch walk keeps these arms and opens the low stride.
                    float hold = c * c;
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(-36f, 16f, armZ), _uaL0 * Quaternion.Euler(-128f, 8f, armZ), hold);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(-36f, -16f, -armZ), _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), hold);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(-72f, 0f, 0f), _laL0 * Quaternion.Euler(-10f, 0f, 0f), hold);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(-72f, 0f, 0f), _laR0 * Quaternion.Euler(-12f, 0f, 0f), hold);
                    if (crouchWalkClaim)
                    {
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f), _ulL0 * Quaternion.Euler(10f, 0f, 0f), hold);
                        _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f), _ulR0 * Quaternion.Euler(52f, 0f, 0f), hold);
                        _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f), _llL0 * Quaternion.Euler(-6f, 0f, 0f), hold);
                        _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f), _llR0 * Quaternion.Euler(-64f, 0f, 0f), hold);
                    }
                    else
                    {
                        _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(56f, 0f, 0f), _ulL0 * Quaternion.Euler(10f, 0f, 0f), hold);
                        _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(56f, 0f, 0f), _ulR0 * Quaternion.Euler(52f, 0f, 0f), hold);
                        _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-68f, 0f, 0f), _llL0 * Quaternion.Euler(-6f, 0f, 0f), hold);
                        _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-68f, 0f, 0f), _llR0 * Quaternion.Euler(-64f, 0f, 0f), hold);
                    }
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(10f, 0f, 0f), _spine0 * Quaternion.Euler(-22f, -16f, 0f), hold);
                    _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(4f, 0f, 0f), hold);
                    _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(-6f, 0f, 0f), _headT, hold);
                }
                else if (!punchHandoff && sprintClaim > 0.02f)
                {
                    float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), Mathf.Max(_runVis, 0.85f));
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = outY + 6f;
                    float turnOut = Mathf.Abs(_turnVis) * 5f;
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                    float pitchL = RunArmPitch(-sinC, amp);
                    float pitchR = RunArmPitch(sinC, amp);
                    float elbowL = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(-6f, -30f, Mathf.Clamp01(-sinC) * gait);
                    float hold = c * c;
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(pitchL, yL, roll), _uaL0 * Quaternion.Euler(-128f, 8f, armZ), hold);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(pitchR, -yR, -roll), _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), hold);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _laL0 * Quaternion.Euler(-10f, 0f, 0f), hold);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _laR0 * Quaternion.Euler(-12f, 0f, 0f), hold);
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX, 0f, leanZ), _spine0 * Quaternion.Euler(-22f, -16f, 0f), hold);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(4f, 0f, 0f), hold);
                }
                else if (!punchHandoff && walkClaim > 0.02f)
                {
                    float gait = Mathf.Max(Mathf.Clamp01(walkAmt), Mathf.Max(_stopGait, _runVis));
                    gait = Mathf.Lerp(gait, 1f, walkClaim);
                    float idle = (1f - gait) * (1f - walkClaim);
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                    float turnOut = Mathf.Abs(_turnVis) * 5f;
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait) + turnOut;
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait) + turnOut;
                    float armBreath = breath * 0.55f * idle;
                    float pitchL = RunArmPitch(-sinC, amp) - 12f * idle + armBreath;
                    float pitchR = RunArmPitch(sinC, amp) - 12f * idle + armBreath;
                    float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                    float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                    float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                    float hold = Mathf.Lerp(c, c * c, walkClaim);
                    _uaLT = Quaternion.Slerp(_uaL0 * Quaternion.Euler(pitchL, yL, roll), _uaL0 * Quaternion.Euler(-128f, 8f, armZ), hold);
                    _uaRT = Quaternion.Slerp(_uaR0 * Quaternion.Euler(pitchR, -yR, -roll), _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), hold);
                    _laLT = Quaternion.Slerp(_laL0 * Quaternion.Euler(elbowL, 0f, 0f), _laL0 * Quaternion.Euler(-10f, 0f, 0f), hold);
                    _laRT = Quaternion.Slerp(_laR0 * Quaternion.Euler(elbowR, 0f, 0f), _laR0 * Quaternion.Euler(-12f, 0f, 0f), hold);
                    _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX, 0f, leanZ), _spine0 * Quaternion.Euler(-22f, -16f, 0f), hold);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(4f, 0f, 0f), hold);
                }
                else if (!punchHandoff)
                {
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-128f, 8f, armZ), cHands);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), cHands);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-10f, 0f, 0f), cHands);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-12f, 0f, 0f), cHands);
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(-22f, -16f, 0f), cHands);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(4f, 0f, 0f), cHands);
                }
                if (punchHandoff || (!crouchClaim && !crouchWalkClaim))
                {
                    _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(10f, 0f, 0f), c);
                    _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(52f, 0f, 0f), c);
                    _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-6f, 0f, 0f), c);
                    _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-64f, 0f, 0f), c);
                }
            }

            float cd = _motor != null ? _motor.AirDashCooldownRemaining : 0f;
            if (_dashCdSeen && _dashCdWas > 0.05f && cd <= 0.001f)
                _dashReady = 1f;
            if (_motor != null)
            {
                _dashCdWas = cd;
                _dashCdSeen = true;
            }
            bool readyBlocked = airDashing || punching || _grapplePose > 0.04f;
            if (!readyBlocked && _dashReady > 0f)
                _dashReady = Mathf.MoveTowards(_dashReady, 0f, dt / 0.28f);
            if (_dashReady > 0.02f && !readyBlocked && !_skiFromReady && !_slideFromReady)
            {
                // The cooldown just ended. A short settle on the chest and the arms,
                // then back into the stride. Standing, it is a small pulse, then the idle breath.
                // A still crouch keeps the pulse for the snapshot ease. A crouch walk pulses inside the low stride.
                // Not a second whip. Duration and cooldown are unchanged.
                float moving = grounded ? Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)) : 0f;
                float standing = grounded ? 1f - moving : 0f;
                float w = Mathf.Sin(Mathf.Clamp01(_dashReady) * Mathf.PI);
                bool crouchReady = grounded && crouch && speed <= 0.35f;
                bool crouchWalkReady = grounded && crouch && speed > 0.35f && speed <= 5.5f && st != MoveState.Sprint;
                if ((crouchReady && !_stillFromReady) || crouchWalkReady)
                {
                    // A small fold inside the guard, then back. A still crouch keeps its snapshot. A crouch walk keeps that fold and the low stride.
                    _uaLT = Quaternion.Slerp(_uaLT, _uaL0 * Quaternion.Euler(-42f, 16f, armZ), w);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaR0 * Quaternion.Euler(-42f, -16f, -armZ), w);
                    _laLT = Quaternion.Slerp(_laLT, _laL0 * Quaternion.Euler(-80f, 0f, 0f), w);
                    _laRT = Quaternion.Slerp(_laRT, _laR0 * Quaternion.Euler(-80f, 0f, 0f), w);
                    if (crouchWalkReady)
                    {
                        float stepL = Mathf.Max(0f, sinC);
                        float stepR = Mathf.Max(0f, -sinC);
                        _ulLT = Quaternion.Slerp(_ulLT, _ulL0 * Quaternion.Euler(50f + stepL * 12f - stepR * 6f, 0f, 0f), w);
                        _ulRT = Quaternion.Slerp(_ulRT, _ulR0 * Quaternion.Euler(50f + stepR * 12f - stepL * 6f, 0f, 0f), w);
                        _llLT = Quaternion.Slerp(_llLT, _llL0 * Quaternion.Euler(-(66f + stepL * 8f), 0f, 0f), w);
                        _llRT = Quaternion.Slerp(_llRT, _llR0 * Quaternion.Euler(-(66f + stepR * 8f), 0f, 0f), w);
                    }
                    _spineT = Quaternion.Slerp(_spineT, _spine0 * Quaternion.Euler(14f, 0f, 0f), w);
                    _hipsT = Quaternion.Slerp(_hipsT, _hips0 * Quaternion.Euler(26f, 0f, 0f), w);
                    _headT = Quaternion.Slerp(_headT, _head0 * Quaternion.Euler(-8f, 0f, 0f), w);
                }
                else
                {
                    float armP = Mathf.Lerp(8f, 3f, standing);
                    float armY = Mathf.Lerp(6f, 0f, standing);
                    float elb = Mathf.Lerp(4f, 1.5f, standing);
                    float chest = Mathf.Lerp(6f, 2f, standing);
                    float hip = Mathf.Lerp(3f, 1f, standing);
                    _uaLT = Quaternion.Slerp(_uaLT, _uaLT * Quaternion.Euler(armP, armY, 0f), w);
                    _uaRT = Quaternion.Slerp(_uaRT, _uaRT * Quaternion.Euler(armP, -armY, 0f), w);
                    _laLT = Quaternion.Slerp(_laLT, _laLT * Quaternion.Euler(elb, 0f, 0f), w);
                    _laRT = Quaternion.Slerp(_laRT, _laRT * Quaternion.Euler(elb, 0f, 0f), w);
                    _spineT = Quaternion.Slerp(_spineT, _spineT * Quaternion.Euler(chest, 0f, 0f), w);
                    _hipsT = Quaternion.Slerp(_hipsT, _hipsT * Quaternion.Euler(hip, 0f, 0f), w);
                }
            }
            _readyWas = _dashReady > 0.2f && !readyBlocked && !_skiFromReady && !_slideFromReady;

            if (_jumpFromTag && _pushOff > 0.02f && !wallRun && !climb)
            {
                // The connect eases into the push, then the air pose. A crouch tag keeps its jump.
                // A punch miss keeps its jump. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                float settle = _tagFromClaim ? _tagSettle : 0f;
                Quaternion connectL = _uaL0 * Quaternion.Euler(-32f, 18f, armZ);
                Quaternion connectR = _uaR0 * Quaternion.Euler(-118f, 52f, -22f);
                Quaternion claimL = _uaL0 * Quaternion.Euler(-128f, 8f, armZ);
                Quaternion claimR = _uaR0 * Quaternion.Euler(-36f, -48f, -armZ);
                Quaternion fromL = Quaternion.Slerp(connectL, claimL, settle);
                Quaternion fromR = Quaternion.Slerp(connectR, claimR, settle);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion connectElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion connectElR = _laR0 * Quaternion.Euler(-18f, 0f, 0f);
                Quaternion claimElL = _laL0 * Quaternion.Euler(-10f, 0f, 0f);
                Quaternion claimElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion fromElL = Quaternion.Slerp(connectElL, claimElL, settle);
                Quaternion fromElR = Quaternion.Slerp(connectElR, claimElR, settle);
                Quaternion pushEl = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airEl = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(fromL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(fromR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(fromElL, pushEl, intoPush), airEl, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(fromElR, pushEl, intoPush), airEl, leave);
                Quaternion connectSp = _spine0 * Quaternion.Euler(14f, 18f, 0f);
                Quaternion claimSp = _spine0 * Quaternion.Euler(-22f, -16f, 0f);
                Quaternion fromSp = Quaternion.Slerp(connectSp, claimSp, settle);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion connectHp = _hips0 * Quaternion.Euler(18f, 22f, 0f);
                Quaternion claimHp = _hips0 * Quaternion.Euler(4f, 0f, 0f);
                Quaternion fromHp = Quaternion.Slerp(connectHp, claimHp, settle);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion fromHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(fromSp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(fromHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(fromHd, pushHd, intoPush), airHd, leave);
                Quaternion connectThighL = _ulL0 * Quaternion.Euler(18f, 0f, 0f);
                Quaternion connectThighR = _ulR0 * Quaternion.Euler(16f, 0f, 0f);
                Quaternion connectKneeL = _llL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion connectKneeR = _llR0 * Quaternion.Euler(-18f, 0f, 0f);
                Quaternion claimThighL = _ulL0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion claimThighR = _ulR0 * Quaternion.Euler(52f, 0f, 0f);
                Quaternion claimKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion claimKneeR = _llR0 * Quaternion.Euler(-64f, 0f, 0f);
                Quaternion fromThighL = Quaternion.Slerp(connectThighL, claimThighL, settle);
                Quaternion fromThighR = Quaternion.Slerp(connectThighR, claimThighR, settle);
                Quaternion fromKneeL = Quaternion.Slerp(connectKneeL, claimKneeL, settle);
                Quaternion fromKneeR = Quaternion.Slerp(connectKneeR, claimKneeR, settle);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(fromThighL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(fromThighR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(fromKneeL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(fromKneeR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (_jumpFromClaim && _pushOff > 0.02f && !wallRun && !climb)
            {
                // The claim eases into the push, then the air pose. A crouch claim keeps its jump.
                // A tag keeps its jump. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion claimL = _uaL0 * Quaternion.Euler(-128f, 8f, armZ);
                Quaternion claimR = _uaR0 * Quaternion.Euler(-36f, -48f, -armZ);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion claimElL = _laL0 * Quaternion.Euler(-10f, 0f, 0f);
                Quaternion claimElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion pushEl = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airEl = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(claimL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(claimR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(claimElL, pushEl, intoPush), airEl, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(claimElR, pushEl, intoPush), airEl, leave);
                Quaternion claimSp = _spine0 * Quaternion.Euler(-22f, -16f, 0f);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion claimHp = _hips0 * Quaternion.Euler(4f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion claimHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(claimSp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(claimHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(claimHd, pushHd, intoPush), airHd, leave);
                Quaternion claimThighL = _ulL0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion claimThighR = _ulR0 * Quaternion.Euler(52f, 0f, 0f);
                Quaternion claimKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion claimKneeR = _llR0 * Quaternion.Euler(-64f, 0f, 0f);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(claimThighL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(claimThighR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(claimKneeL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(claimKneeR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (_jumpFromGrapple && _pushOff > 0.02f && !wallRun && !climb)
            {
                // The line eases into the push, then the air pose. A crouch release keeps its jump.
                // The gate stays off. Jump height is unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion lineL = _uaL0 * Quaternion.Euler(-96f, 16f, armZ);
                Quaternion lineR = _uaR0 * Quaternion.Euler(-96f, -16f, -armZ);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion lineElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion lineElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion pushElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion pushElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion airElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(lineL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(lineR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(lineElL, pushElL, intoPush), airElL, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(lineElR, pushElR, intoPush), airElR, leave);
                Quaternion lineSp = _spine0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion lineHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion lineHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(lineSp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(lineHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(lineHd, pushHd, intoPush), airHd, leave);
                Quaternion lineThighL = _ulL0 * Quaternion.Euler(8f, 0f, 0f);
                Quaternion lineThighR = _ulR0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion lineKneeL = _llL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion lineKneeR = _llR0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(lineThighL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(lineThighR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(lineKneeL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(lineKneeR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (_jumpFromReady && _pushOff > 0.02f && !wallRun && !climb)
            {
                // The pulse eases into the push, then the air pose. A crouch ready keeps its jump.
                // A grapple release keeps its jump. Duration and cooldown are unchanged.
                float t = 1f - Mathf.Clamp01(_pushOff);
                float intoPush = Mathf.Clamp01(t * 2f);
                float leave = Mathf.Clamp01(t * 2f - 1f);
                Quaternion pushL = _uaL0 * Quaternion.Euler(-36f, 14f, armZ);
                Quaternion pushR = _uaR0 * Quaternion.Euler(-36f, -14f, -armZ);
                Quaternion airL = _uaL0 * Quaternion.Euler(-52f, 22f, armZ);
                Quaternion airR = _uaR0 * Quaternion.Euler(-52f, -22f, -armZ);
                Quaternion pushElL = _laL0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion pushElR = _laR0 * Quaternion.Euler(-14f, 0f, 0f);
                Quaternion airElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion airElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(Quaternion.Slerp(_readyUaL, pushL, intoPush), airL, leave);
                _uaRT = Quaternion.Slerp(Quaternion.Slerp(_readyUaR, pushR, intoPush), airR, leave);
                _laLT = Quaternion.Slerp(Quaternion.Slerp(_readyLaL, pushElL, intoPush), airElL, leave);
                _laRT = Quaternion.Slerp(Quaternion.Slerp(_readyLaR, pushElR, intoPush), airElR, leave);
                Quaternion pushSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion airSp = _spine0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion pushHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion airHp = _hips0 * Quaternion.Euler(6f, 0f, 0f);
                Quaternion pushHd = _head0 * Quaternion.Euler(0f, 0f, 0f);
                Quaternion airHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                _spineT = Quaternion.Slerp(Quaternion.Slerp(_readySp, pushSp, intoPush), airSp, leave);
                _hipsT = Quaternion.Slerp(Quaternion.Slerp(_readyHp, pushHp, intoPush), airHp, leave);
                _headT = Quaternion.Slerp(Quaternion.Slerp(_readyHd, pushHd, intoPush), airHd, leave);
                Quaternion pushThighL;
                Quaternion pushThighR;
                Quaternion pushKneeL;
                Quaternion pushKneeR;
                if (_pushLeft)
                {
                    pushThighL = _ulL0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighR = _ulR0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                else
                {
                    pushThighR = _ulR0 * Quaternion.Euler(-8f, 0f, 0f);
                    pushKneeR = _llR0 * Quaternion.Euler(-6f, 0f, 0f);
                    pushThighL = _ulL0 * Quaternion.Euler(48f, 0f, 0f);
                    pushKneeL = _llL0 * Quaternion.Euler(-62f, 0f, 0f);
                }
                Quaternion airThighL = _ulL0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion airThighR = _ulR0 * Quaternion.Euler(20f, 0f, 0f);
                Quaternion airKneeL = _llL0 * Quaternion.Euler(-28f, 0f, 0f);
                Quaternion airKneeR = _llR0 * Quaternion.Euler(-26f, 0f, 0f);
                _ulLT = Quaternion.Slerp(Quaternion.Slerp(_readyUlL, pushThighL, intoPush), airThighL, leave);
                _ulRT = Quaternion.Slerp(Quaternion.Slerp(_readyUlR, pushThighR, intoPush), airThighR, leave);
                _llLT = Quaternion.Slerp(Quaternion.Slerp(_readyLlL, pushKneeL, intoPush), airKneeL, leave);
                _llRT = Quaternion.Slerp(Quaternion.Slerp(_readyLlR, pushKneeR, intoPush), airKneeR, leave);
            }
            if (_punchFromDash && !_punchFromJump && !_jumpFromPunch && punching && phase == PunchPhase.Windup && _punchFromDashIn < 0.98f)
            {
                // The burst eases into the cock, then the windup holds.
                // A slide into a punch keeps its ease. A ski into a tag keeps its ease.
                // A slide into a tag keeps its ease. Windup time is unchanged.
                // Duration and cooldown are unchanged.
                float intoCock = _punchFromDashIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                float windLean = dashing ? (air ? 18f : breath) : leanX;
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                _uaLT = Quaternion.Slerp(_dashPunchUaL, windL, intoCock);
                _uaRT = Quaternion.Slerp(_dashPunchUaR, windR, intoCock);
                _laLT = Quaternion.Slerp(_dashPunchLaL, windElL, intoCock);
                _laRT = Quaternion.Slerp(_dashPunchLaR, windElR, intoCock);
                _spineT = Quaternion.Slerp(_dashPunchSp, _spine0 * Quaternion.Euler(windLean + 12f * w, -36f * w, leanZ), intoCock);
                _hipsT = Quaternion.Slerp(_dashPunchHp, _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f), intoCock);
                _headT = Quaternion.Slerp(_dashPunchHd, _headT, intoCock);
                _ulLT = Quaternion.Slerp(_dashPunchUlL, _ulLT, intoCock);
                _ulRT = Quaternion.Slerp(_dashPunchUlR, _ulRT, intoCock);
                _llLT = Quaternion.Slerp(_dashPunchLlL, _llLT, intoCock);
                _llRT = Quaternion.Slerp(_dashPunchLlR, _llRT, intoCock);
            }
            if (_punchFromTag && !_punchFromReady && !_punchFromGrapple && !_punchFromClaim && !_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromTagIn < 0.98f)
            {
                // The connect eases into the cock, then the windup holds.
                // A punch into a tag keeps its ease. A dash coming off cooldown into a punch keeps its ease.
                // A crouch tag keeps its pose. A tag into a jump keeps its push.
                // Windup time is unchanged. Connect time is unchanged.
                float into = _punchFromTagIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_tagPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_tagPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_tagPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_tagPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_tagPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_tagPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_tagPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_tagPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_tagPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_tagPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_tagPunchLlR, _llRT, into);
            }
            if (_punchFromReady && !_punchFromGrapple && !_punchFromClaim && !_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromReadyIn < 0.98f)
            {
                // The pulse eases into the cock, then the windup holds.
                // A grapple release into a tag keeps its ease. A dash coming off cooldown into a jump keeps its push.
                // A crouch ready keeps its pose. Windup time is unchanged. Duration and cooldown are unchanged.
                float into = _punchFromReadyIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_readyPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_readyPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_readyPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_readyPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_readyPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_readyPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_readyPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_readyPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_readyPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_readyPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_readyPunchLlR, _llRT, into);
            }
            if (_punchFromGrapple && !_punchFromClaim && !_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromGrappleIn < 0.98f)
            {
                // The line eases into the cock, then the windup holds.
                // Becoming It into a tag keeps its ease. A grapple release into an air dash keeps its ease.
                // A grapple release into a jump keeps its push. A crouch release keeps its pose.
                // Windup time is unchanged. The gate stays off.
                float into = _punchFromGrappleIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_grapplePunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_grapplePunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_grapplePunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_grapplePunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_grapplePunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_grapplePunchSp, windSp, into);
                _headT = Quaternion.Slerp(_grapplePunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_grapplePunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_grapplePunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_grapplePunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_grapplePunchLlR, _llRT, into);
            }
            if (_punchFromClaim && !_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup)
            {
                // The claim eases into the cock, then the windup holds. The claim pose would cover it.
                // An air crouch into a tag keeps its ease. Becoming It into an air dash keeps its ease.
                // Becoming It into a jump keeps its push. A crouch claim keeps its pose.
                // Windup time is unchanged. Claim time is unchanged.
                float into = _punchFromClaimIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_claimPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_claimPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_claimPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_claimPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_claimPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_claimPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_claimPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_claimPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_claimPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_claimPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_claimPunchLlR, _llRT, into);
            }
            if (_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromDartIn < 0.98f)
            {
                // The dart eases into the cock, then the windup holds.
                // A wall exit into a tag keeps its ease. An air crouch into an air dash keeps its ease.
                // An air crouch into a jump keeps its push. A jump into a punch keeps its ease.
                // Windup time is unchanged. Fall speed is unchanged.
                float into = _punchFromDartIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_dartPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_dartPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_dartPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_dartPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_dartPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_dartPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_dartPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_dartPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_dartPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_dartPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_dartPunchLlR, _llRT, into);
            }
            if (_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup)
            {
                // The wall exit eases into the cock, then the windup holds. The wall pose would cover it.
                // A climb into a punch keeps its ease. A wall exit into an air dash keeps its ease.
                // A wall exit into a jump keeps its push. A climb into a tag keeps its ease.
                // Windup time is unchanged. Exit time is unchanged.
                float into = _punchFromWallIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_wallPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_wallPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_wallPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_wallPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_wallPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_wallPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_wallPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_wallPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_wallPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_wallPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_wallPunchLlR, _llRT, into);
            }
            if (_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup)
            {
                // The grab eases into the cock, then the windup holds. The climb pose would cover it.
                // A slide into a tag keeps its ease. A climb into an air dash keeps its ease.
                // A climb into a jump keeps its push. A wall run keeps its pose.
                // Windup time is unchanged. Exit time is unchanged.
                float into = _punchFromClimbIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_climbPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_climbPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_climbPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_climbPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_climbPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_climbPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_climbPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_climbPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_climbPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_climbPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_climbPunchLlR, _llRT, into);
            }
            if (_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromSlideIn < 0.98f)
            {
                // The wedge eases into the cock, then the windup holds.
                // A ski into a punch keeps its ease. A slide into a jump keeps its push.
                // A slide into an air dash keeps its ease. slideBoost stays 0. Windup time is unchanged.
                float into = _punchFromSlideIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_slidePunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_slidePunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_slidePunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_slidePunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_slidePunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_slidePunchSp, windSp, into);
                _headT = Quaternion.Slerp(_slidePunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_slidePunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_slidePunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_slidePunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_slidePunchLlR, _llRT, into);
            }
            if (_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromSkiIn < 0.98f)
            {
                // The glide eases into the cock, then the windup holds.
                // A ski into a jump keeps its push. A ski into an air dash keeps its ease.
                // A hard landing into a tag keeps its ease. Windup time is unchanged. Ski speed is unchanged.
                float into = _punchFromSkiIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_skiPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_skiPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_skiPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_skiPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_skiPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_skiPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_skiPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_skiPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_skiPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_skiPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_skiPunchLlR, _llRT, into);
            }
            if (_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromHardIn < 0.98f)
            {
                // The deeper absorb eases into the cock, then the windup holds.
                // A soft landing into a punch keeps its ease. A hard landing into a jump keeps its push.
                // A jump into a punch keeps its ease. Windup time is unchanged. Land time is unchanged.
                float into = _punchFromHardIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_hardPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_hardPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_hardPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_hardPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_hardPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_hardPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_hardPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_hardPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_hardPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_hardPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_hardPunchLlR, _llRT, into);
            }
            if (_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromSoftIn < 0.98f)
            {
                // The absorb eases into the cock, then the windup holds.
                // A jump into a punch keeps its ease. A soft landing into a jump keeps its push.
                // A hard landing keeps its pose. Windup time is unchanged. Land time is unchanged.
                float into = _punchFromSoftIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_landPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_landPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_landPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_landPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_landPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_landPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_landPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_landPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_landPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_landPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_landPunchLlR, _llRT, into);
            }
            if (punching && phase == PunchPhase.Windup && _punchFromJump && !_jumpFromPunch && _punchFromJumpIn < 0.98f)
            {
                // The apex or the landing eases into the cock, then the windup holds.
                // A punch from the ground keeps its windup. Windup time is unchanged.
                float into = _punchFromJumpIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_punchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_punchUaR, windR, into);
                _laLT = Quaternion.Slerp(_punchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_punchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_punchHp, windHp, into);
                _spineT = Quaternion.Slerp(_punchSp, windSp, into);
                _headT = Quaternion.Slerp(_punchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_punchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_punchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_punchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_punchLlR, _llRT, into);
            }
            if (_punchFromCrouch && !_punchFromTag && !_punchFromReady && !_punchFromGrapple && !_punchFromClaim && !_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromCrouchIn < 0.98f)
            {
                // The guard eases into the cock, then the windup holds.
                // A crouch walk into a punch keeps its windup. An air crouch into a punch keeps its ease.
                // A slide into a punch keeps its ease. A crouch into a slide keeps its ease.
                // A crouch claim keeps its pose. A crouch tag keeps its pose. Windup time is unchanged.
                float into = _punchFromCrouchIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_crouchPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_crouchPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_crouchPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_crouchPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_crouchPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_crouchPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_crouchPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_crouchPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_crouchPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_crouchPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_crouchPunchLlR, _llRT, into);
            }
            if (_punchFromWalk && !_punchFromCrouch && !_punchFromTag && !_punchFromReady && !_punchFromGrapple && !_punchFromClaim && !_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromWalkIn < 0.98f)
            {
                // The walk eases into the cock, then the windup holds.
                // A still crouch into a punch keeps its ease. A jump into a punch keeps its ease.
                // A still crouch into a jump keeps its ease. Windup time is unchanged.
                float into = _punchFromWalkIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_walkPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_walkPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_walkPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_walkPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_walkPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_walkPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_walkPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_walkPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_walkPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_walkPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_walkPunchLlR, _llRT, into);
            }
            if (_punchFromSprint && !_punchFromWalk && !_punchFromCrouch && !_punchFromTag && !_punchFromReady && !_punchFromGrapple && !_punchFromClaim && !_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromSprintIn < 0.98f)
            {
                // The run eases into the cock, then the windup holds.
                // A walk into a punch keeps its ease. A jump into a punch keeps its ease.
                // A walk into a tag keeps its ease. Windup time is unchanged.
                float into = _punchFromSprintIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_sprintPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_sprintPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_sprintPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_sprintPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_sprintPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_sprintPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_sprintPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_sprintPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_sprintPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_sprintPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_sprintPunchLlR, _llRT, into);
            }
            if (_punchFromCrouchWalk && !_punchFromSprint && !_punchFromWalk && !_punchFromCrouch && !_punchFromTag && !_punchFromReady && !_punchFromGrapple && !_punchFromClaim && !_punchFromDart && !_punchFromWall && !_punchFromClimb && !_punchFromSlide && !_punchFromSki && !_punchFromHard && !_punchFromSoft && !_punchFromJump && !_jumpFromPunch && !_punchFromDash && punching && phase == PunchPhase.Windup && _punchFromCrouchWalkIn < 0.98f)
            {
                // The low stride eases into the cock, then the windup holds.
                // A still crouch into a punch keeps its ease. A walk into a punch keeps its ease.
                // A run into a tag keeps its ease. Windup time is unchanged.
                float into = _punchFromCrouchWalkIn;
                float w = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg / 0.35f)));
                Quaternion windL = _uaL0 * Quaternion.Euler(-28f, 14f, armZ + 18f);
                Quaternion windR = _uaR0 * Quaternion.Euler(-58f * w, 46f * w, -armZ);
                Quaternion windElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion windElR = _laR0 * Quaternion.Euler(-68f * w, 0f, 0f);
                Quaternion windHp = _hips0 * Quaternion.Euler(14f + 8f * w, -30f * w, 0f);
                Quaternion windSp = _spine0 * Quaternion.Euler(leanX + 12f * w, -36f * w, leanZ);
                _uaLT = Quaternion.Slerp(_crouchWalkPunchUaL, windL, into);
                _uaRT = Quaternion.Slerp(_crouchWalkPunchUaR, windR, into);
                _laLT = Quaternion.Slerp(_crouchWalkPunchLaL, windElL, into);
                _laRT = Quaternion.Slerp(_crouchWalkPunchLaR, windElR, into);
                _hipsT = Quaternion.Slerp(_crouchWalkPunchHp, windHp, into);
                _spineT = Quaternion.Slerp(_crouchWalkPunchSp, windSp, into);
                _headT = Quaternion.Slerp(_crouchWalkPunchHd, _headT, into);
                _ulLT = Quaternion.Slerp(_crouchWalkPunchUlL, _ulLT, into);
                _ulRT = Quaternion.Slerp(_crouchWalkPunchUlR, _ulRT, into);
                _llLT = Quaternion.Slerp(_crouchWalkPunchLlL, _llLT, into);
                _llRT = Quaternion.Slerp(_crouchWalkPunchLlR, _llRT, into);
            }
            if (airDashing && _dashFromHard && !_dashFromSoft && !_dashFromGrapple && !_dashFromClaim && !_dashFromTag && !_dashFromMiss && !_dashFromWall && !_dashFromClimb && !_dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromHardIn < 0.98f && !punching)
            {
                // The deeper absorb eases into the burst, then the burst holds.
                // A soft landing into a dash keeps its ease. A hard landing into a jump keeps its push.
                // Land time is unchanged. Duration and cooldown are unchanged.
                float intoBurst = _dashFromHardIn;
                _uaLT = Quaternion.Slerp(_hardUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_hardUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_hardLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_hardLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_hardUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_hardUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_hardLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_hardLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_hardSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_hardHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_hardHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromSoft && !_dashFromGrapple && !_dashFromClaim && !_dashFromTag && !_dashFromMiss && !_dashFromWall && !_dashFromClimb && !_dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromSoftIn < 0.98f && !punching)
            {
                // The absorb eases into the burst, then the burst holds.
                // A soft landing into a jump keeps its push. A hard landing keeps its pose.
                // An air dash into a tag keeps its ease. Land time is unchanged.
                // Duration and cooldown are unchanged.
                float intoBurst = _dashFromSoftIn;
                _uaLT = Quaternion.Slerp(_softUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_softUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_softLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_softLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_softUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_softUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_softLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_softLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_softSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_softHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_softHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromGrapple && !_dashFromClaim && !_dashFromTag && !_dashFromMiss && !_dashFromWall && !_dashFromClimb && !_dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromGrappleIn < 0.98f)
            {
                // The line eases into the burst, then the burst holds.
                // A grapple release into a jump keeps its push. A crouch release keeps its pose.
                // Becoming It into a dash keeps its ease. The gate stays off.
                // Duration and cooldown are unchanged.
                float intoBurst = _dashFromGrappleIn;
                _uaLT = Quaternion.Slerp(_grappleUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_grappleUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_grappleLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_grappleLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_grappleUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_grappleUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_grappleLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_grappleLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_grappleSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_grappleHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_grappleHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromClaim && !_dashFromTag && !_dashFromMiss && !_dashFromWall && !_dashFromClimb && !_dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromClaimIn < 0.98f)
            {
                // The claim eases into the burst, then the burst holds.
                // Becoming It into a jump keeps its push. A crouch claim keeps its pose.
                // A tag into a dash keeps its ease.
                // Duration and cooldown are unchanged.
                float intoBurst = _dashFromClaimIn;
                _uaLT = Quaternion.Slerp(_claimUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_claimUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_claimLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_claimLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_claimUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_claimUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_claimLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_claimLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_claimSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_claimHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_claimHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromTag && !_dashFromMiss && !_dashFromWall && !_dashFromClimb && !_dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromTagIn < 0.98f)
            {
                // The connect eases into the burst, then the burst holds.
                // A tag into a jump keeps its push. A crouch tag keeps its pose.
                // A punch miss into a dash keeps its ease.
                // Duration and cooldown are unchanged.
                float intoBurst = _dashFromTagIn;
                _uaLT = Quaternion.Slerp(_tagUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_tagUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_tagLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_tagLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_tagUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_tagUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_tagLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_tagLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_tagSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_tagHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_tagHd, _headT, intoBurst);
            }
            if (airDashing && _dashFromMiss && !_dashFromWall && !_dashFromClimb && !_dashFromSlide && !_dashFromSki && !_dashFromDart && !_jumpFromDash && _dashFromMissIn < 0.98f)
            {
                // The whiff eases into the burst, then the burst holds.
                // A punch miss into a jump keeps its push. A crouch miss keeps its pose.
                // An air dash into a wall or a climb keeps its ease.
                // Duration and cooldown are unchanged.
                float intoBurst = _dashFromMissIn;
                _uaLT = Quaternion.Slerp(_missUaL, _uaLT, intoBurst);
                _uaRT = Quaternion.Slerp(_missUaR, _uaRT, intoBurst);
                _laLT = Quaternion.Slerp(_missLaL, _laLT, intoBurst);
                _laRT = Quaternion.Slerp(_missLaR, _laRT, intoBurst);
                _ulLT = Quaternion.Slerp(_missUlL, _ulLT, intoBurst);
                _ulRT = Quaternion.Slerp(_missUlR, _ulRT, intoBurst);
                _llLT = Quaternion.Slerp(_missLlL, _llLT, intoBurst);
                _llRT = Quaternion.Slerp(_missLlR, _llRT, intoBurst);
                _spineT = Quaternion.Slerp(_missSp, _spineT, intoBurst);
                _hipsT = Quaternion.Slerp(_missHp, _hipsT, intoBurst);
                _headT = Quaternion.Slerp(_missHd, _headT, intoBurst);
            }
            if (_wallFromDash && !airDashing && wallRun && !climb && !punching)
            {
                // The burst eases into the attach, then the attach holds.
                // An air dash into a climb keeps its ease. A climb into a dash keeps its ease.
                // A wall exit into a dash keeps its ease.
                // Exit time is unchanged. Duration and cooldown are unchanged.
                float intoAttach = _wallFromDashIn;
                bool left = _motor != null && _motor.WallLeft;
                float wallLive = Mathf.Sin(_surfPhase);
                float pressLive = (wallLive + 1f) * 0.5f;
                float press = Mathf.Lerp(0.55f, pressLive, _surfIn);
                float outerFwd = 1f - press;
                float yaw = Mathf.Lerp(18f, 34f, _surfIn);
                float wallPitch = Mathf.Lerp(-48f, -72f, press);
                float wallElbow = Mathf.Lerp(-16f, -8f, press);
                float outerArm = Mathf.Lerp(-36f, Mathf.Lerp(-28f, -84f, 1f - pressLive), _surfIn);
                float outerElbow = Mathf.Lerp(-16f, -10f, outerFwd);
                float wallPhase = Mathf.Lerp(0f, wallLive, _surfIn);
                float outerThigh = 10f + wallPhase * 38f;
                float outerKnee = -(6f + Mathf.Max(0f, wallPhase) * 68f);
                float wallLean = left ? 32f : -32f;
                Quaternion burstL = _uaL0 * Quaternion.Euler(108f, 32f, armZ);
                Quaternion burstR = _uaR0 * Quaternion.Euler(108f, -32f, -armZ);
                Quaternion burstElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion burstElR = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion attachL;
                Quaternion attachR;
                Quaternion attachElL;
                Quaternion attachElR;
                Quaternion attachThighL;
                Quaternion attachThighR;
                Quaternion attachKneeL;
                Quaternion attachKneeR;
                if (left)
                {
                    attachL = _uaL0 * Quaternion.Euler(wallPitch, yaw, armZ);
                    attachR = _uaR0 * Quaternion.Euler(outerArm, -10f, -armZ);
                    attachElL = _laL0 * Quaternion.Euler(wallElbow, 0f, 0f);
                    attachElR = _laR0 * Quaternion.Euler(outerElbow, 0f, 0f);
                    attachThighL = _ulL0 * Quaternion.Euler(16f, 0f, 0f);
                    attachThighR = _ulR0 * Quaternion.Euler(outerThigh, 0f, 0f);
                    attachKneeL = _llL0 * Quaternion.Euler(-8f, 0f, 0f);
                    attachKneeR = _llR0 * Quaternion.Euler(outerKnee, 0f, 0f);
                }
                else
                {
                    attachR = _uaR0 * Quaternion.Euler(wallPitch, -yaw, -armZ);
                    attachL = _uaL0 * Quaternion.Euler(outerArm, 10f, armZ);
                    attachElR = _laR0 * Quaternion.Euler(wallElbow, 0f, 0f);
                    attachElL = _laL0 * Quaternion.Euler(outerElbow, 0f, 0f);
                    attachThighR = _ulR0 * Quaternion.Euler(16f, 0f, 0f);
                    attachThighL = _ulL0 * Quaternion.Euler(outerThigh, 0f, 0f);
                    attachKneeR = _llR0 * Quaternion.Euler(-8f, 0f, 0f);
                    attachKneeL = _llL0 * Quaternion.Euler(outerKnee, 0f, 0f);
                }
                _uaLT = Quaternion.Slerp(burstL, attachL, intoAttach);
                _uaRT = Quaternion.Slerp(burstR, attachR, intoAttach);
                _laLT = Quaternion.Slerp(burstElL, attachElL, intoAttach);
                _laRT = Quaternion.Slerp(burstElR, attachElR, intoAttach);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(58f, 0f, 0f), _spine0 * Quaternion.Euler(22f, 0f, wallLean), intoAttach);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(0f, 0f, -wallLean * 0.55f), intoAttach);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(0f, 0f, 0f), _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoAttach);
                _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(72f, 0f, 0f), attachThighL, intoAttach);
                _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(-34f, 0f, 0f), attachThighR, intoAttach);
                _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-62f, 0f, 0f), attachKneeL, intoAttach);
                _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-18f, 0f, 0f), attachKneeR, intoAttach);
            }
            if (_climbFromDash && !airDashing && climb && !wallRun && !punching)
            {
                // The burst eases into the grab, then the grab holds.
                // A climb into a dash keeps its ease. A wall exit into a dash keeps its ease.
                // Exit time is unchanged. Duration and cooldown are unchanged.
                float intoGrab = _climbFromDashIn;
                float climbLive = Mathf.Sin(_surfPhase);
                float upLive = (climbLive + 1f) * 0.5f;
                float up = Mathf.Lerp(0.8f, upLive, _surfIn);
                float down = 1f - up;
                float kneePhase = climbLive * _surfIn;
                Quaternion burstL = _uaL0 * Quaternion.Euler(108f, 32f, armZ);
                Quaternion burstR = _uaR0 * Quaternion.Euler(108f, -32f, -armZ);
                Quaternion grabL = _uaL0 * Quaternion.Euler(Mathf.Lerp(-52f, -118f, up), Mathf.Lerp(12f, 18f, up), armZ);
                Quaternion grabR = _uaR0 * Quaternion.Euler(Mathf.Lerp(-52f, -118f, down), Mathf.Lerp(-12f, -18f, down), -armZ);
                Quaternion burstElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion burstElR = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion grabElL = _laL0 * Quaternion.Euler(Mathf.Lerp(-18f, -8f, up), 0f, 0f);
                Quaternion grabElR = _laR0 * Quaternion.Euler(Mathf.Lerp(-18f, -8f, down), 0f, 0f);
                _uaLT = Quaternion.Slerp(burstL, grabL, intoGrab);
                _uaRT = Quaternion.Slerp(burstR, grabR, intoGrab);
                _laLT = Quaternion.Slerp(burstElL, grabElL, intoGrab);
                _laRT = Quaternion.Slerp(burstElR, grabElR, intoGrab);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(58f, 0f, 0f), _spine0 * Quaternion.Euler(-16f, 0f, 0f), intoGrab);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(12f, 0f, 0f), intoGrab);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(0f, 0f, 0f), _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGrab);
                _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(72f, 0f, 0f), _ulL0 * Quaternion.Euler(Mathf.Lerp(62f, 14f, up), 0f, 0f), intoGrab);
                _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(-34f, 0f, 0f), _ulR0 * Quaternion.Euler(Mathf.Lerp(14f, 62f, up), 0f, 0f), intoGrab);
                _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-62f, 0f, 0f), _llL0 * Quaternion.Euler(-(6f + Mathf.Max(0f, -kneePhase) * 72f), 0f, 0f), intoGrab);
                _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-18f, 0f, 0f), _llR0 * Quaternion.Euler(-(6f + Mathf.Max(0f, kneePhase) * 72f), 0f, 0f), intoGrab);
            }
            if (_crouchFromJump && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The jump eases into the guard, then the guard holds.
                // A run into a slide keeps its ease. A jump into a ski keeps its ease.
                // A jump into a slide keeps its ease. Jump height is unchanged.
                float intoGuard = _crouchFromJumpIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_jumpCrouchUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_jumpCrouchUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_jumpCrouchLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_jumpCrouchLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_jumpCrouchSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_jumpCrouchHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_jumpCrouchHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_jumpCrouchUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_jumpCrouchUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_jumpCrouchLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_jumpCrouchLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_crouchFromDash && !_crouchFromJump && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The burst eases into the guard, then the guard holds.
                // A jump into a still crouch keeps its ease. A still crouch into an air dash keeps its ease.
                // An air dash into a ski keeps its ease. An air dash into a slide keeps its ease.
                // Duration and cooldown are unchanged.
                float intoGuard = _crouchFromDashIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_dashCrouchUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_dashCrouchUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_dashCrouchLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_dashCrouchLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_dashCrouchSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_dashCrouchHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_dashCrouchHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_dashCrouchUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_dashCrouchUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_dashCrouchLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_dashCrouchLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromWalk && !_crouchFromDash && !_crouchFromJump && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The walk eases into the guard, then the guard holds.
                // A soft landing into a punch keeps its ease. A soft landing into a tag keeps its ease.
                // An air crouch into a punch keeps its ease.
                float intoGuard = _stillFromWalkIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_walkGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_walkGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_walkGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_walkGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_walkGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_walkGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_walkGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_walkGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_walkGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_walkGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_walkGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromSprint && !_stillFromWalk && !_crouchFromDash && !_crouchFromJump && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The run eases into the guard, then the guard holds.
                // A walk into a still crouch keeps its ease. A jump into a still crouch keeps its ease.
                // An air dash into a still crouch keeps its ease.
                float intoGuard = _stillFromSprintIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_sprintGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_sprintGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_sprintGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_sprintGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_sprintGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_sprintGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_sprintGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_sprintGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_sprintGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_sprintGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_sprintGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromSki && !_stillFromSprint && !_stillFromWalk && !_crouchFromDash && !_crouchFromJump && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The glide eases into the guard, then the guard holds.
                // A run into a still crouch keeps its ease. A walk into a still crouch keeps its ease.
                // Ski speed is unchanged.
                float intoGuard = _stillFromSkiIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_skiGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_skiGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_skiGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_skiGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_skiGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_skiGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_skiGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_skiGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_skiGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_skiGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_skiGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromSlide && !_stillFromSki && !_stillFromSprint && !_stillFromWalk && !_crouchFromDash && !_crouchFromJump && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The wedge eases into the guard, then the guard holds.
                // A ski into a still crouch keeps its ease. A run into a still crouch keeps its ease.
                // slideBoost stays 0.
                float intoGuard = _stillFromSlideIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_slideGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_slideGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_slideGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_slideGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_slideGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_slideGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_slideGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_slideGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_slideGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_slideGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_slideGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromPunch && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The punch eases into the guard, then the guard holds.
                // A slide into a still crouch keeps its ease. A ski into a still crouch keeps its ease.
                // Windup time is unchanged.
                float intoGuard = _stillFromPunchIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_punchGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_punchGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_punchGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_punchGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_punchGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_punchGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_punchGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_punchGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_punchGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_punchGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_punchGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromTag && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The connect eases into the guard, then the guard holds.
                // A punch into a still crouch keeps its ease. A slide into a still crouch keeps its ease.
                // Connect time is unchanged.
                float intoGuard = _stillFromTagIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_tagGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_tagGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_tagGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_tagGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_tagGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_tagGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_tagGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_tagGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_tagGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_tagGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_tagGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromMiss && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The whiff eases into the guard, then the guard holds.
                // A tag into a still crouch keeps its ease. A punch into a still crouch keeps its ease.
                // Whiff time is unchanged.
                float intoGuard = _stillFromMissIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_missGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_missGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_missGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_missGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_missGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_missGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_missGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_missGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_missGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_missGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_missGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromClaim && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The claim eases into the guard, then the guard holds.
                // A whiff into a still crouch keeps its ease. A tag into a still crouch keeps its ease.
                // Claim time is unchanged.
                float intoGuard = _stillFromClaimIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_claimGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_claimGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_claimGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_claimGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_claimGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_claimGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_claimGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_claimGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_claimGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_claimGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_claimGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromReady && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The pulse eases into the guard, then the guard holds.
                // An It claim into a still crouch keeps its ease. A whiff into a still crouch keeps its ease.
                // Duration and cooldown are unchanged.
                float intoGuard = _stillFromReadyIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_readyGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_readyGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_readyGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_readyGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_readyGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_readyGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_readyGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_readyGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_readyGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_readyGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_readyGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromGrapple && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The line eases into the guard, then the guard holds.
                // A dash ready into a still crouch keeps its ease. An It claim into a still crouch keeps its ease.
                // The gate stays off.
                float intoGuard = _stillFromGrappleIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_grappleGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_grappleGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_grappleGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_grappleGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_grappleGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_grappleGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_grappleGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_grappleGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_grappleGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_grappleGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_grappleGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromHard && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The absorb eases into the guard, then the guard holds.
                // A grapple release into a still crouch keeps its ease. A dash ready into a still crouch keeps its ease.
                // Land time is unchanged.
                float intoGuard = _stillFromHardIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_hardGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_hardGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_hardGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_hardGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_hardGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_hardGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_hardGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_hardGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_hardGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_hardGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_hardGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromSoft && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The absorb eases into the guard, then the guard holds.
                // A hard land into a still crouch keeps its ease. A grapple release into a still crouch keeps its ease.
                // Land time is unchanged.
                float intoGuard = _stillFromSoftIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_softGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_softGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_softGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_softGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_softGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_softGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_softGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_softGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_softGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_softGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_softGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromDart && grounded && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The dart eases into the guard, then the guard holds.
                // A soft land into a still crouch keeps its ease. A hard land into a still crouch keeps its ease.
                // Fall speed is unchanged.
                float intoGuard = _stillFromDartIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_dartGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_dartGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_dartGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_dartGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_dartGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_dartGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_dartGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_dartGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_dartGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_dartGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_dartGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromClimb && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The grab eases into the guard, then the guard holds.
                // An air crouch into a still crouch keeps its ease. A wall exit into a still crouch keeps its own ease.
                // Exit time is unchanged.
                float intoGuard = _stillFromClimbIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_climbGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_climbGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_climbGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_climbGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_climbGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_climbGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_climbGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_climbGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_climbGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_climbGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_climbGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromWall && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The leave eases into the guard, then the guard holds.
                // A climb into a still crouch keeps its ease. An air crouch into a still crouch keeps its ease.
                // Exit time is unchanged.
                float intoGuard = _stillFromWallIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_wallGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_wallGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_wallGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_wallGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_wallGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_wallGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_wallGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_wallGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_wallGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_wallGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_wallGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_stillFromCrouchWalk && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The low stride eases into the guard, then the guard holds.
                // A wall exit into a still crouch keeps its ease. A climb into a still crouch keeps its ease.
                // The stride is unchanged.
                float intoGuard = _stillFromCrouchWalkIn;
                Quaternion guardL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion guardR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion guardElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion guardSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion guardHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion guardHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion guardThighL = _ulL0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardThighR = _ulR0 * Quaternion.Euler(56f, 0f, 0f);
                Quaternion guardKneeL = _llL0 * Quaternion.Euler(-68f, 0f, 0f);
                Quaternion guardKneeR = _llR0 * Quaternion.Euler(-68f, 0f, 0f);
                if (intoGuard < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_crouchWalkGuardUaL, guardL, intoGuard);
                    _uaRT = Quaternion.Slerp(_crouchWalkGuardUaR, guardR, intoGuard);
                    _laLT = Quaternion.Slerp(_crouchWalkGuardLaL, guardElL, intoGuard);
                    _laRT = Quaternion.Slerp(_crouchWalkGuardLaR, guardElR, intoGuard);
                    _spineT = Quaternion.Slerp(_crouchWalkGuardSp, guardSp, intoGuard);
                    _hipsT = Quaternion.Slerp(_crouchWalkGuardHp, guardHp, intoGuard);
                    _headT = Quaternion.Slerp(_crouchWalkGuardHd, guardHd, intoGuard);
                    _ulLT = Quaternion.Slerp(_crouchWalkGuardUlL, guardThighL, intoGuard);
                    _ulRT = Quaternion.Slerp(_crouchWalkGuardUlR, guardThighR, intoGuard);
                    _llLT = Quaternion.Slerp(_crouchWalkGuardLlL, guardKneeL, intoGuard);
                    _llRT = Quaternion.Slerp(_crouchWalkGuardLlR, guardKneeR, intoGuard);
                }
                else
                {
                    _uaLT = guardL;
                    _uaRT = guardR;
                    _laLT = guardElL;
                    _laRT = guardElR;
                    _spineT = guardSp;
                    _hipsT = guardHp;
                    _headT = guardHd;
                    _ulLT = guardThighL;
                    _ulRT = guardThighR;
                    _llLT = guardKneeL;
                    _llRT = guardKneeR;
                }
            }
            if (_crouchWalkFromSki && skiCrouchWalk && _crouchWalkFromSkiIn < 0.98f && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The glide eases into the low stride, then the stride holds.
                // A ski into a still crouch keeps its ease. A crouch walk into a ski has its own ease.
                // Ski speed is unchanged.
                float intoStride = _crouchWalkFromSkiIn;
                float stepL = Mathf.Max(0f, sinC);
                float stepR = Mathf.Max(0f, -sinC);
                Quaternion strideL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion strideR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion strideElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion strideHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion strideHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion strideThighL = _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f);
                Quaternion strideThighR = _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f);
                Quaternion strideKneeL = _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f);
                Quaternion strideKneeR = _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f);
                _uaLT = Quaternion.Slerp(_skiWalkUaL, strideL, intoStride);
                _uaRT = Quaternion.Slerp(_skiWalkUaR, strideR, intoStride);
                _laLT = Quaternion.Slerp(_skiWalkLaL, strideElL, intoStride);
                _laRT = Quaternion.Slerp(_skiWalkLaR, strideElR, intoStride);
                _spineT = Quaternion.Slerp(_skiWalkSp, strideSp, intoStride);
                _hipsT = Quaternion.Slerp(_skiWalkHp, strideHp, intoStride);
                _headT = Quaternion.Slerp(_skiWalkHd, strideHd, intoStride);
                _ulLT = Quaternion.Slerp(_skiWalkUlL, strideThighL, intoStride);
                _ulRT = Quaternion.Slerp(_skiWalkUlR, strideThighR, intoStride);
                _llLT = Quaternion.Slerp(_skiWalkLlL, strideKneeL, intoStride);
                _llRT = Quaternion.Slerp(_skiWalkLlR, strideKneeR, intoStride);
            }
            if (_crouchWalkFromSlide && _crouchWalkFromSlideIn < 0.98f && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The wedge eases into the low stride, then the stride holds.
                // A ski into a crouch walk keeps its ease. A slide into a still crouch keeps its ease.
                // slideBoost stays 0.
                float intoStride = _crouchWalkFromSlideIn;
                float stepL = Mathf.Max(0f, sinC);
                float stepR = Mathf.Max(0f, -sinC);
                Quaternion strideL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion strideR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion strideElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion strideHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion strideHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion strideThighL = _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f);
                Quaternion strideThighR = _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f);
                Quaternion strideKneeL = _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f);
                Quaternion strideKneeR = _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f);
                _uaLT = Quaternion.Slerp(_slideWalkUaL, strideL, intoStride);
                _uaRT = Quaternion.Slerp(_slideWalkUaR, strideR, intoStride);
                _laLT = Quaternion.Slerp(_slideWalkLaL, strideElL, intoStride);
                _laRT = Quaternion.Slerp(_slideWalkLaR, strideElR, intoStride);
                _spineT = Quaternion.Slerp(_slideWalkSp, strideSp, intoStride);
                _hipsT = Quaternion.Slerp(_slideWalkHp, strideHp, intoStride);
                _headT = Quaternion.Slerp(_slideWalkHd, strideHd, intoStride);
                _ulLT = Quaternion.Slerp(_slideWalkUlL, strideThighL, intoStride);
                _ulRT = Quaternion.Slerp(_slideWalkUlR, strideThighR, intoStride);
                _llLT = Quaternion.Slerp(_slideWalkLlL, strideKneeL, intoStride);
                _llRT = Quaternion.Slerp(_slideWalkLlR, strideKneeR, intoStride);
            }
            if (_crouchWalkFromWalk && walkCrouch && _crouchWalkFromWalkIn < 0.98f && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The walk eases into the low stride, then the stride holds.
                // A slide into a crouch walk keeps its ease. A walk into a still crouch keeps its ease.
                // The drop timer stays dt/0.16.
                float intoStride = _crouchWalkFromWalkIn;
                float stepL = Mathf.Max(0f, sinC);
                float stepR = Mathf.Max(0f, -sinC);
                Quaternion strideL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion strideR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion strideElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion strideHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion strideHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion strideThighL = _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f);
                Quaternion strideThighR = _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f);
                Quaternion strideKneeL = _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f);
                Quaternion strideKneeR = _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f);
                _uaLT = Quaternion.Slerp(_walkCrouchUaL, strideL, intoStride);
                _uaRT = Quaternion.Slerp(_walkCrouchUaR, strideR, intoStride);
                _laLT = Quaternion.Slerp(_walkCrouchLaL, strideElL, intoStride);
                _laRT = Quaternion.Slerp(_walkCrouchLaR, strideElR, intoStride);
                _spineT = Quaternion.Slerp(_walkCrouchSp, strideSp, intoStride);
                _hipsT = Quaternion.Slerp(_walkCrouchHp, strideHp, intoStride);
                _headT = Quaternion.Slerp(_walkCrouchHd, strideHd, intoStride);
                _ulLT = Quaternion.Slerp(_walkCrouchUlL, strideThighL, intoStride);
                _ulRT = Quaternion.Slerp(_walkCrouchUlR, strideThighR, intoStride);
                _llLT = Quaternion.Slerp(_walkCrouchLlL, strideKneeL, intoStride);
                _llRT = Quaternion.Slerp(_walkCrouchLlR, strideKneeR, intoStride);
            }
            if (_crouchWalkFromSprint && sprintCrouch && _crouchWalkFromSprintIn < 0.98f && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The run eases into the low stride, then the stride holds.
                // A walk into a crouch walk keeps its ease. A run into a still crouch keeps its ease.
                // The drop timer stays dt/0.16.
                float intoStride = _crouchWalkFromSprintIn;
                float stepL = Mathf.Max(0f, sinC);
                float stepR = Mathf.Max(0f, -sinC);
                Quaternion strideL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion strideR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion strideElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion strideHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion strideHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion strideThighL = _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f);
                Quaternion strideThighR = _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f);
                Quaternion strideKneeL = _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f);
                Quaternion strideKneeR = _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f);
                _uaLT = Quaternion.Slerp(_sprintCrouchUaL, strideL, intoStride);
                _uaRT = Quaternion.Slerp(_sprintCrouchUaR, strideR, intoStride);
                _laLT = Quaternion.Slerp(_sprintCrouchLaL, strideElL, intoStride);
                _laRT = Quaternion.Slerp(_sprintCrouchLaR, strideElR, intoStride);
                _spineT = Quaternion.Slerp(_sprintCrouchSp, strideSp, intoStride);
                _hipsT = Quaternion.Slerp(_sprintCrouchHp, strideHp, intoStride);
                _headT = Quaternion.Slerp(_sprintCrouchHd, strideHd, intoStride);
                _ulLT = Quaternion.Slerp(_sprintCrouchUlL, strideThighL, intoStride);
                _ulRT = Quaternion.Slerp(_sprintCrouchUlR, strideThighR, intoStride);
                _llLT = Quaternion.Slerp(_sprintCrouchLlL, strideKneeL, intoStride);
                _llRT = Quaternion.Slerp(_sprintCrouchLlR, strideKneeR, intoStride);
            }
            if (_crouchWalkFromStill && walkCrouch && _crouchWalkFromStillIn < 0.98f && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The planted guard eases into the low stride, then the stride holds.
                // A walk into a crouch walk keeps its ease. A run into a crouch walk keeps its ease.
                // A crouch walk into a still crouch keeps its ease.
                float intoStride = _crouchWalkFromStillIn;
                float stepL = Mathf.Max(0f, sinC);
                float stepR = Mathf.Max(0f, -sinC);
                Quaternion strideL = _uaL0 * Quaternion.Euler(-36f, 16f, armZ);
                Quaternion strideR = _uaR0 * Quaternion.Euler(-36f, -16f, -armZ);
                Quaternion strideElL = _laL0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideElR = _laR0 * Quaternion.Euler(-72f, 0f, 0f);
                Quaternion strideSp = _spine0 * Quaternion.Euler(10f, 0f, 0f);
                Quaternion strideHp = _hips0 * Quaternion.Euler(22f, 0f, 0f);
                Quaternion strideHd = _head0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion strideThighL = _ulL0 * Quaternion.Euler(46f + stepL * 12f - stepR * 6f, 0f, 0f);
                Quaternion strideThighR = _ulR0 * Quaternion.Euler(46f + stepR * 12f - stepL * 6f, 0f, 0f);
                Quaternion strideKneeL = _llL0 * Quaternion.Euler(-(60f + stepL * 8f), 0f, 0f);
                Quaternion strideKneeR = _llR0 * Quaternion.Euler(-(60f + stepR * 8f), 0f, 0f);
                _uaLT = Quaternion.Slerp(_stillStrideUaL, strideL, intoStride);
                _uaRT = Quaternion.Slerp(_stillStrideUaR, strideR, intoStride);
                _laLT = Quaternion.Slerp(_stillStrideLaL, strideElL, intoStride);
                _laRT = Quaternion.Slerp(_stillStrideLaR, strideElR, intoStride);
                _spineT = Quaternion.Slerp(_stillStrideSp, strideSp, intoStride);
                _hipsT = Quaternion.Slerp(_stillStrideHp, strideHp, intoStride);
                _headT = Quaternion.Slerp(_stillStrideHd, strideHd, intoStride);
                _ulLT = Quaternion.Slerp(_stillStrideUlL, strideThighL, intoStride);
                _ulRT = Quaternion.Slerp(_stillStrideUlR, strideThighR, intoStride);
                _llLT = Quaternion.Slerp(_stillStrideLlL, strideKneeL, intoStride);
                _llRT = Quaternion.Slerp(_stillStrideLlR, strideKneeR, intoStride);
            }
            if (_walkFromCrouchWalk && _walkFromCrouchWalkIn < 0.98f && !crouch && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The low stride eases into the walk, then the walk holds.
                // A crouch walk into a run has its own ease. A still crouch into a walk has its own ease.
                // The drop timer stays dt/0.16.
                float intoWalk = _walkFromCrouchWalkIn;
                _uaLT = Quaternion.Slerp(_walkUpUaL, _uaLT, intoWalk);
                _uaRT = Quaternion.Slerp(_walkUpUaR, _uaRT, intoWalk);
                _laLT = Quaternion.Slerp(_walkUpLaL, _laLT, intoWalk);
                _laRT = Quaternion.Slerp(_walkUpLaR, _laRT, intoWalk);
                _spineT = Quaternion.Slerp(_walkUpSp, _spineT, intoWalk);
                _hipsT = Quaternion.Slerp(_walkUpHp, _hipsT, intoWalk);
                _headT = Quaternion.Slerp(_walkUpHd, _headT, intoWalk);
                _ulLT = Quaternion.Slerp(_walkUpUlL, _ulLT, intoWalk);
                _ulRT = Quaternion.Slerp(_walkUpUlR, _ulRT, intoWalk);
                _llLT = Quaternion.Slerp(_walkUpLlL, _llLT, intoWalk);
                _llRT = Quaternion.Slerp(_walkUpLlR, _llRT, intoWalk);
            }
            if (_runFromCrouchWalk && _runFromCrouchWalkIn < 0.98f && !crouch && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The low stride eases into the run, then the run holds.
                // A crouch walk into a walk keeps its ease. A still crouch into a run has its own ease.
                // The drop timer stays dt/0.16.
                float intoRun = _runFromCrouchWalkIn;
                _uaLT = Quaternion.Slerp(_runUpUaL, _uaLT, intoRun);
                _uaRT = Quaternion.Slerp(_runUpUaR, _uaRT, intoRun);
                _laLT = Quaternion.Slerp(_runUpLaL, _laLT, intoRun);
                _laRT = Quaternion.Slerp(_runUpLaR, _laRT, intoRun);
                _spineT = Quaternion.Slerp(_runUpSp, _spineT, intoRun);
                _hipsT = Quaternion.Slerp(_runUpHp, _hipsT, intoRun);
                _headT = Quaternion.Slerp(_runUpHd, _headT, intoRun);
                _ulLT = Quaternion.Slerp(_runUpUlL, _ulLT, intoRun);
                _ulRT = Quaternion.Slerp(_runUpUlR, _ulRT, intoRun);
                _llLT = Quaternion.Slerp(_runUpLlL, _llLT, intoRun);
                _llRT = Quaternion.Slerp(_runUpLlR, _llRT, intoRun);
            }
            if (_walkFromStill && _walkFromStillIn < 0.98f && !crouch && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The planted guard eases into the walk, then the walk holds.
                // A crouch walk into a walk keeps its ease. A still crouch into a run has its own ease.
                // The drop timer stays dt/0.16.
                float intoWalk = _walkFromStillIn;
                _uaLT = Quaternion.Slerp(_stillWalkUaL, _uaLT, intoWalk);
                _uaRT = Quaternion.Slerp(_stillWalkUaR, _uaRT, intoWalk);
                _laLT = Quaternion.Slerp(_stillWalkLaL, _laLT, intoWalk);
                _laRT = Quaternion.Slerp(_stillWalkLaR, _laRT, intoWalk);
                _spineT = Quaternion.Slerp(_stillWalkSp, _spineT, intoWalk);
                _hipsT = Quaternion.Slerp(_stillWalkHp, _hipsT, intoWalk);
                _headT = Quaternion.Slerp(_stillWalkHd, _headT, intoWalk);
                _ulLT = Quaternion.Slerp(_stillWalkUlL, _ulLT, intoWalk);
                _ulRT = Quaternion.Slerp(_stillWalkUlR, _ulRT, intoWalk);
                _llLT = Quaternion.Slerp(_stillWalkLlL, _llLT, intoWalk);
                _llRT = Quaternion.Slerp(_stillWalkLlR, _llRT, intoWalk);
            }
            if (_runFromStill && _runFromStillIn < 0.98f && !crouch && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The planted guard eases into the run, then the run holds.
                // A still crouch into a walk keeps its ease. A crouch walk into a run keeps its ease.
                // The drop timer stays dt/0.16.
                float intoRun = _runFromStillIn;
                _uaLT = Quaternion.Slerp(_stillRunUaL, _uaLT, intoRun);
                _uaRT = Quaternion.Slerp(_stillRunUaR, _uaRT, intoRun);
                _laLT = Quaternion.Slerp(_stillRunLaL, _laLT, intoRun);
                _laRT = Quaternion.Slerp(_stillRunLaR, _laRT, intoRun);
                _spineT = Quaternion.Slerp(_stillRunSp, _spineT, intoRun);
                _hipsT = Quaternion.Slerp(_stillRunHp, _hipsT, intoRun);
                _headT = Quaternion.Slerp(_stillRunHd, _headT, intoRun);
                _ulLT = Quaternion.Slerp(_stillRunUlL, _ulLT, intoRun);
                _ulRT = Quaternion.Slerp(_stillRunUlR, _ulRT, intoRun);
                _llLT = Quaternion.Slerp(_stillRunLlL, _llLT, intoRun);
                _llRT = Quaternion.Slerp(_stillRunLlR, _llRT, intoRun);
            }
            if (_slideFromWalk && !_slideFromSprint && !_slideFromSki && !_slideFromCrouch && !_slideFromMiss && !_slideFromReady && !_slideFromClaim && !_slideFromGrapple && !_slideFromWall && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && !punching)
            {
                // The walk eases into the wedge, then the wedge holds.
                // A run into a slide keeps its ease. A crouch walk into a slide keeps its ease.
                // A still crouch into a slide keeps its ease. A ski into a slide keeps its ease.
                // An air dash into a still crouch keeps its ease. slideBoost stays 0.
                float intoWedge = _slideFromWalkIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion wedgeSp = _spine0 * Quaternion.Euler(62f, 0f, 0f);
                Quaternion wedgeHp = _hips0 * Quaternion.Euler(50f, 0f, 0f);
                Quaternion wedgeHd = _head0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion wedgeThighL = _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f);
                Quaternion wedgeThighR = _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f);
                Quaternion wedgeKneeL = _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f);
                Quaternion wedgeKneeR = _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f);
                if (intoWedge < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_walkSlideUaL, wedgeL, intoWedge);
                    _uaRT = Quaternion.Slerp(_walkSlideUaR, wedgeR, intoWedge);
                    _laLT = Quaternion.Slerp(_walkSlideLaL, wedgeElL, intoWedge);
                    _laRT = Quaternion.Slerp(_walkSlideLaR, wedgeElR, intoWedge);
                    _spineT = Quaternion.Slerp(_walkSlideSp, wedgeSp, intoWedge);
                    _hipsT = Quaternion.Slerp(_walkSlideHp, wedgeHp, intoWedge);
                    _headT = Quaternion.Slerp(_walkSlideHd, wedgeHd, intoWedge);
                    _ulLT = Quaternion.Slerp(_walkSlideUlL, wedgeThighL, intoWedge);
                    _ulRT = Quaternion.Slerp(_walkSlideUlR, wedgeThighR, intoWedge);
                    _llLT = Quaternion.Slerp(_walkSlideLlL, wedgeKneeL, intoWedge);
                    _llRT = Quaternion.Slerp(_walkSlideLlR, wedgeKneeR, intoWedge);
                }
                else
                {
                    _uaLT = wedgeL;
                    _uaRT = wedgeR;
                    _laLT = wedgeElL;
                    _laRT = wedgeElR;
                    _spineT = wedgeSp;
                    _hipsT = wedgeHp;
                    _headT = wedgeHd;
                    _ulLT = wedgeThighL;
                    _ulRT = wedgeThighR;
                    _llLT = wedgeKneeL;
                    _llRT = wedgeKneeR;
                }
            }
            if (_slideFromSprint && !_slideFromWalk && !_slideFromSki && !_slideFromCrouch && !_slideFromMiss && !_slideFromReady && !_slideFromClaim && !_slideFromGrapple && !_slideFromWall && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && !punching)
            {
                // The run eases into the wedge, then the wedge holds.
                // A run into a ski keeps its ease. A ski into a slide keeps its ease.
                // A still crouch into a slide keeps its ease. slideBoost stays 0.
                float intoWedge = _slideFromSprintIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                Quaternion wedgeSp = _spine0 * Quaternion.Euler(62f, 0f, 0f);
                Quaternion wedgeHp = _hips0 * Quaternion.Euler(50f, 0f, 0f);
                Quaternion wedgeHd = _head0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion wedgeThighL = _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f);
                Quaternion wedgeThighR = _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f);
                Quaternion wedgeKneeL = _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f);
                Quaternion wedgeKneeR = _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f);
                if (intoWedge < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_sprintSlideUaL, wedgeL, intoWedge);
                    _uaRT = Quaternion.Slerp(_sprintSlideUaR, wedgeR, intoWedge);
                    _laLT = Quaternion.Slerp(_sprintSlideLaL, wedgeElL, intoWedge);
                    _laRT = Quaternion.Slerp(_sprintSlideLaR, wedgeElR, intoWedge);
                    _spineT = Quaternion.Slerp(_sprintSlideSp, wedgeSp, intoWedge);
                    _hipsT = Quaternion.Slerp(_sprintSlideHp, wedgeHp, intoWedge);
                    _headT = Quaternion.Slerp(_sprintSlideHd, wedgeHd, intoWedge);
                    _ulLT = Quaternion.Slerp(_sprintSlideUlL, wedgeThighL, intoWedge);
                    _ulRT = Quaternion.Slerp(_sprintSlideUlR, wedgeThighR, intoWedge);
                    _llLT = Quaternion.Slerp(_sprintSlideLlL, wedgeKneeL, intoWedge);
                    _llRT = Quaternion.Slerp(_sprintSlideLlR, wedgeKneeR, intoWedge);
                }
                else
                {
                    _uaLT = wedgeL;
                    _uaRT = wedgeR;
                    _laLT = wedgeElL;
                    _laRT = wedgeElR;
                    _spineT = wedgeSp;
                    _hipsT = wedgeHp;
                    _headT = wedgeHd;
                    _ulLT = wedgeThighL;
                    _ulRT = wedgeThighR;
                    _llLT = wedgeKneeL;
                    _llRT = wedgeKneeR;
                }
            }
            if (_slideFromSki && !_slideFromCrouch && !_slideFromMiss && !_slideFromReady && !_slideFromClaim && !_slideFromGrapple && !_slideFromWall && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromSkiIn < 0.98f)
            {
                // The glide eases into the wedge, then the wedge holds.
                // A still crouch into a slide keeps its ease. A crouch walk into a ski has its own ease.
                // A crouch walk into a slide keeps its ease. A slide into a ski has its own ease.
                // slideBoost stays 0. Ski speed is unchanged.
                float intoWedge = _slideFromSkiIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_skiSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_skiSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_skiSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_skiSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_skiSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_skiSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_skiSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_skiSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_skiSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_skiSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_skiSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromCrouch && !_slideFromSki && !_slideFromMiss && !_slideFromReady && !_slideFromClaim && !_slideFromGrapple && !_slideFromWall && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromCrouchIn < 0.98f)
            {
                // The guard eases into the wedge, then the wedge holds.
                // A punch miss into a slide keeps its ease. A still crouch into a punch keeps its ease.
                // A still crouch into a tag keeps its ease. A still crouch into a ski has its own ease.
                // A crouch walk into a slide keeps its ease. slideBoost stays 0.
                float intoWedge = _slideFromCrouchIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_crouchSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_crouchSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_crouchSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_crouchSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_crouchSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_crouchSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_crouchSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_crouchSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_crouchSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_crouchSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_crouchSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromMiss && !_slideFromCrouch && !_slideFromReady && !_slideFromClaim && !_slideFromGrapple && !_slideFromWall && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromMissIn < 0.98f)
            {
                // The whiff eases into the wedge, then the wedge holds.
                // A punch miss into a ski keeps its ease. A punch into a slide keeps its ease.
                // A punch miss into a tag keeps its ease. A punch miss into a jump keeps its push.
                // A punch miss into an air dash keeps its ease. slideBoost stays 0. Whiff time is unchanged.
                float intoWedge = _slideFromMissIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_missSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_missSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_missSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_missSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_missSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_missSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_missSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_missSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_missSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_missSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_missSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromReady && !_slideFromMiss && !_slideFromClaim && !_slideFromGrapple && !_slideFromWall && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromReadyIn < 0.98f)
            {
                // The pulse eases into the wedge, then the wedge holds.
                // A dash coming off cooldown into a ski keeps its ease. A dash coming off cooldown into a punch keeps its ease.
                // A dash coming off cooldown into a tag keeps its ease. A dash coming off cooldown into a jump keeps its push.
                // Becoming It into a slide keeps its ease. slideBoost stays 0. Duration and cooldown are unchanged.
                float intoWedge = _slideFromReadyIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_readySlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_readySlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_readySlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_readySlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_readySlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_readySlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_readySlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_readySlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_readySlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_readySlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_readySlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromClaim && !_slideFromReady && !_slideFromGrapple && !_slideFromWall && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromClaimIn < 0.98f)
            {
                // The claim eases into the wedge, then the wedge holds.
                // Becoming It into a ski keeps its ease. Becoming It into a punch keeps its ease.
                // Becoming It into a jump keeps its push. A grapple release into a slide keeps its ease.
                // slideBoost stays 0. Claim time is unchanged.
                float intoWedge = _slideFromClaimIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_claimSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_claimSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_claimSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_claimSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_claimSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_claimSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_claimSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_claimSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_claimSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_claimSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_claimSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromGrapple && !_slideFromClaim && !_slideFromWall && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromGrappleIn < 0.98f)
            {
                // The line eases into the wedge, then the wedge holds.
                // A grapple release into a ski keeps its ease. A grapple release into a jump keeps its push.
                // A wall run into a slide keeps its ease. A soft landing into a slide keeps its ease.
                // slideBoost stays 0. The gate stays off.
                float intoWedge = _slideFromGrappleIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_grappleSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_grappleSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_grappleSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_grappleSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_grappleSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_grappleSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_grappleSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_grappleSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_grappleSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_grappleSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_grappleSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromWall && !_slideFromGrapple && !_slideFromClimb && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromWallIn < 0.98f)
            {
                // The leave eases into the wedge, then the wedge holds.
                // A wall run into a ski keeps its ease. A climb into a slide keeps its ease.
                // A wall run into a jump keeps its push. A soft landing into a slide keeps its ease.
                // A jump into a slide keeps its ease. slideBoost stays 0. Exit time is unchanged.
                float intoWedge = _slideFromWallIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_wallSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_wallSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_wallSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_wallSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_wallSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_wallSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_wallSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_wallSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_wallSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_wallSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_wallSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromClimb && !_slideFromWall && !_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromClimbIn < 0.98f)
            {
                // The grab eases into the wedge, then the wedge holds.
                // A climb into a ski keeps its ease. A wall run into a slide keeps its own ease.
                // A climb into a jump keeps its push. A soft landing into a slide keeps its ease.
                // A jump into a slide keeps its ease. slideBoost stays 0. Exit time is unchanged.
                float intoWedge = _slideFromClimbIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_climbSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_climbSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_climbSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_climbSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_climbSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_climbSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_climbSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_climbSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_climbSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_climbSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_climbSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromSoft && !_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromSoftIn < 0.98f)
            {
                // The absorb eases into the wedge, then the wedge holds.
                // A soft landing into a ski keeps its ease. A hard landing into a slide keeps its ease.
                // A jump into a slide keeps its ease. A punch into a slide keeps its ease.
                // A crouch into a slide keeps its ease. A ski into a slide keeps its ease.
                // slideBoost stays 0. Land time is unchanged.
                float intoWedge = _slideFromSoftIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_softSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_softSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_softSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_softSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_softSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_softSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_softSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_softSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_softSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_softSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_softSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromTag && !_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromTagIn < 0.98f)
            {
                // The connect eases into the wedge, then the wedge holds.
                // A tag into a ski keeps its ease. A punch into a slide keeps its ease.
                // A slide into a tag keeps its ease. A crouch into a slide keeps its ease.
                // A ski into a slide keeps its ease. A jump into a slide keeps its ease.
                // slideBoost stays 0. Connect time is unchanged.
                float intoWedge = _slideFromTagIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_tagSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_tagSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_tagSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_tagSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_tagSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_tagSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_tagSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_tagSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_tagSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_tagSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_tagSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromPunch && !_slideFromDash && sliding && !jet && !skiing && !wallRun && !climb && _slideFromPunchIn < 0.98f)
            {
                // The cock or the strike eases into the wedge, then the wedge holds.
                // A punch into a ski keeps its ease. A crouch into a slide keeps its ease.
                // A ski into a slide keeps its ease. A jump into a slide keeps its ease.
                // slideBoost stays 0. Windup time is unchanged.
                float intoWedge = _slideFromPunchIn;
                bool leadLeft = sinC >= 0f;
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_punchSlideUaL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(_punchSlideUaR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(_punchSlideLaL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(_punchSlideLaR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_punchSlideSp, _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_punchSlideHp, _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_punchSlideHd, _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_punchSlideUlL, _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_punchSlideUlR, _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_punchSlideLlL, _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_punchSlideLlR, _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_slideFromDash && !_skiFromDash && !airDashing && !punching && !wallRun && !climb)
            {
                // The burst eases into the wedge, then the wedge holds.
                // An air dash into a ski keeps its ease. A ski into a dash keeps its ease.
                // A slide into a dash keeps its ease. slideBoost stays 0.
                // Duration and cooldown are unchanged.
                float intoWedge = _slideFromDashIn;
                bool leadLeft = sinC >= 0f;
                Quaternion burstL = _uaL0 * Quaternion.Euler(108f, 32f, armZ);
                Quaternion burstR = _uaR0 * Quaternion.Euler(108f, -32f, -armZ);
                Quaternion wedgeL = _uaL0 * Quaternion.Euler(-70f, 28f, armZ);
                Quaternion wedgeR = _uaR0 * Quaternion.Euler(-64f, -28f, -armZ);
                Quaternion burstElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion burstElR = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion wedgeElL = _laL0 * Quaternion.Euler(-8f, 0f, 0f);
                Quaternion wedgeElR = _laR0 * Quaternion.Euler(-6f, 0f, 0f);
                _uaLT = Quaternion.Slerp(burstL, wedgeL, intoWedge);
                _uaRT = Quaternion.Slerp(burstR, wedgeR, intoWedge);
                _laLT = Quaternion.Slerp(burstElL, wedgeElL, intoWedge);
                _laRT = Quaternion.Slerp(burstElR, wedgeElR, intoWedge);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(58f, 0f, 0f), _spine0 * Quaternion.Euler(62f, 0f, 0f), intoWedge);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(50f, 0f, 0f), intoWedge);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(0f, 0f, 0f), _head0 * Quaternion.Euler(-12f, 0f, 0f), intoWedge);
                _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(72f, 0f, 0f), _ulL0 * Quaternion.Euler(leadLeft ? 74f : -28f, leadLeft ? 6f : -4f, 0f), intoWedge);
                _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(-34f, 0f, 0f), _ulR0 * Quaternion.Euler(leadLeft ? -28f : 74f, leadLeft ? -4f : 6f, 0f), intoWedge);
                _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-62f, 0f, 0f), _llL0 * Quaternion.Euler(leadLeft ? -94f : -6f, 0f, 0f), intoWedge);
                _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-18f, 0f, 0f), _llR0 * Quaternion.Euler(leadLeft ? -6f : -94f, 0f, 0f), intoWedge);
            }
            if (_skiFromWalk && !_skiFromSprint && !_skiFromCrouch && !_skiFromCrouchWalk && skiing && !jet && !crouch && !wallRun && !climb && !punching)
            {
                // The walk eases into the glide, then the glide holds.
                // A run into a ski keeps its ease. A crouch walk into a ski has its own ease.
                // A still crouch into a ski has its own ease. A walk into a slide keeps its ease.
                // Ski speed is unchanged.
                float intoGlide = _skiFromWalkIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                Quaternion skiHp = _hips0 * Quaternion.Euler(14f, 0f, 0f);
                Quaternion skiHd = _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f);
                Quaternion skiThighL = _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f);
                Quaternion skiThighR = _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f);
                Quaternion skiKneeL = _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f);
                Quaternion skiKneeR = _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f);
                if (intoGlide < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_walkSkiUaL, skiL, intoGlide);
                    _uaRT = Quaternion.Slerp(_walkSkiUaR, skiR, intoGlide);
                    _laLT = Quaternion.Slerp(_walkSkiLaL, skiElL, intoGlide);
                    _laRT = Quaternion.Slerp(_walkSkiLaR, skiElR, intoGlide);
                    _spineT = Quaternion.Slerp(_walkSkiSp, skiSp, intoGlide);
                    _hipsT = Quaternion.Slerp(_walkSkiHp, skiHp, intoGlide);
                    _headT = Quaternion.Slerp(_walkSkiHd, skiHd, intoGlide);
                    _ulLT = Quaternion.Slerp(_walkSkiUlL, skiThighL, intoGlide);
                    _ulRT = Quaternion.Slerp(_walkSkiUlR, skiThighR, intoGlide);
                    _llLT = Quaternion.Slerp(_walkSkiLlL, skiKneeL, intoGlide);
                    _llRT = Quaternion.Slerp(_walkSkiLlR, skiKneeR, intoGlide);
                }
                else
                {
                    _uaLT = skiL;
                    _uaRT = skiR;
                    _laLT = skiElL;
                    _laRT = skiElR;
                    _spineT = skiSp;
                    _hipsT = skiHp;
                    _headT = skiHd;
                    _ulLT = skiThighL;
                    _ulRT = skiThighR;
                    _llLT = skiKneeL;
                    _llRT = skiKneeR;
                }
            }
            if (_skiFromSprint && !_skiFromWalk && skiing && !jet && !crouch && !wallRun && !climb && !punching)
            {
                // The run eases into the glide, then the glide holds.
                // A walk into a ski keeps its ease. A slide into a ski has its own ease.
                // A jump into a ski keeps its ease. Ski speed is unchanged.
                float intoGlide = _skiFromSprintIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                Quaternion skiHp = _hips0 * Quaternion.Euler(14f, 0f, 0f);
                Quaternion skiHd = _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f);
                Quaternion skiThighL = _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f);
                Quaternion skiThighR = _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f);
                Quaternion skiKneeL = _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f);
                Quaternion skiKneeR = _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f);
                if (intoGlide < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_sprintSkiUaL, skiL, intoGlide);
                    _uaRT = Quaternion.Slerp(_sprintSkiUaR, skiR, intoGlide);
                    _laLT = Quaternion.Slerp(_sprintSkiLaL, skiElL, intoGlide);
                    _laRT = Quaternion.Slerp(_sprintSkiLaR, skiElR, intoGlide);
                    _spineT = Quaternion.Slerp(_sprintSkiSp, skiSp, intoGlide);
                    _hipsT = Quaternion.Slerp(_sprintSkiHp, skiHp, intoGlide);
                    _headT = Quaternion.Slerp(_sprintSkiHd, skiHd, intoGlide);
                    _ulLT = Quaternion.Slerp(_sprintSkiUlL, skiThighL, intoGlide);
                    _ulRT = Quaternion.Slerp(_sprintSkiUlR, skiThighR, intoGlide);
                    _llLT = Quaternion.Slerp(_sprintSkiLlL, skiKneeL, intoGlide);
                    _llRT = Quaternion.Slerp(_sprintSkiLlR, skiKneeR, intoGlide);
                }
                else
                {
                    _uaLT = skiL;
                    _uaRT = skiR;
                    _laLT = skiElL;
                    _laRT = skiElR;
                    _spineT = skiSp;
                    _hipsT = skiHp;
                    _headT = skiHd;
                    _ulLT = skiThighL;
                    _ulRT = skiThighR;
                    _llLT = skiKneeL;
                    _llRT = skiKneeR;
                }
            }
            if (_skiFromSlide && skiing && !jet && !crouch && !wallRun && !climb && !punching)
            {
                // The wedge eases into the glide, then the glide holds.
                // A ski into a slide keeps its ease. A walk into a ski keeps its ease.
                // A run into a ski keeps its ease. Ski speed is unchanged. slideBoost stays 0.
                float intoGlide = _skiFromSlideIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                Quaternion skiHp = _hips0 * Quaternion.Euler(14f, 0f, 0f);
                Quaternion skiHd = _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f);
                Quaternion skiThighL = _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f);
                Quaternion skiThighR = _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f);
                Quaternion skiKneeL = _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f);
                Quaternion skiKneeR = _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f);
                if (intoGlide < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_slideSkiUaL, skiL, intoGlide);
                    _uaRT = Quaternion.Slerp(_slideSkiUaR, skiR, intoGlide);
                    _laLT = Quaternion.Slerp(_slideSkiLaL, skiElL, intoGlide);
                    _laRT = Quaternion.Slerp(_slideSkiLaR, skiElR, intoGlide);
                    _spineT = Quaternion.Slerp(_slideSkiSp, skiSp, intoGlide);
                    _hipsT = Quaternion.Slerp(_slideSkiHp, skiHp, intoGlide);
                    _headT = Quaternion.Slerp(_slideSkiHd, skiHd, intoGlide);
                    _ulLT = Quaternion.Slerp(_slideSkiUlL, skiThighL, intoGlide);
                    _ulRT = Quaternion.Slerp(_slideSkiUlR, skiThighR, intoGlide);
                    _llLT = Quaternion.Slerp(_slideSkiLlL, skiKneeL, intoGlide);
                    _llRT = Quaternion.Slerp(_slideSkiLlR, skiKneeR, intoGlide);
                }
                else
                {
                    _uaLT = skiL;
                    _uaRT = skiR;
                    _laLT = skiElL;
                    _laRT = skiElR;
                    _spineT = skiSp;
                    _hipsT = skiHp;
                    _headT = skiHd;
                    _ulLT = skiThighL;
                    _ulRT = skiThighR;
                    _llLT = skiKneeL;
                    _llRT = skiKneeR;
                }
            }
            if (_crouchWalkSkiSnap && _skiFromCrouchWalk && skiing && !jet && !crouch && !wallRun && !climb && !punching)
            {
                // The low stride eases into the glide, then the glide holds.
                // A still crouch into a ski has its own ease. A walk into a ski keeps its ease.
                // A slide into a ski has its own ease. Ski speed is unchanged.
                float intoGlide = _skiFromCrouchWalkIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                Quaternion skiHp = _hips0 * Quaternion.Euler(14f, 0f, 0f);
                Quaternion skiHd = _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f);
                Quaternion skiThighL = _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f);
                Quaternion skiThighR = _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f);
                Quaternion skiKneeL = _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f);
                Quaternion skiKneeR = _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f);
                if (intoGlide < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_crouchWalkSkiUaL, skiL, intoGlide);
                    _uaRT = Quaternion.Slerp(_crouchWalkSkiUaR, skiR, intoGlide);
                    _laLT = Quaternion.Slerp(_crouchWalkSkiLaL, skiElL, intoGlide);
                    _laRT = Quaternion.Slerp(_crouchWalkSkiLaR, skiElR, intoGlide);
                    _spineT = Quaternion.Slerp(_crouchWalkSkiSp, skiSp, intoGlide);
                    _hipsT = Quaternion.Slerp(_crouchWalkSkiHp, skiHp, intoGlide);
                    _headT = Quaternion.Slerp(_crouchWalkSkiHd, skiHd, intoGlide);
                    _ulLT = Quaternion.Slerp(_crouchWalkSkiUlL, skiThighL, intoGlide);
                    _ulRT = Quaternion.Slerp(_crouchWalkSkiUlR, skiThighR, intoGlide);
                    _llLT = Quaternion.Slerp(_crouchWalkSkiLlL, skiKneeL, intoGlide);
                    _llRT = Quaternion.Slerp(_crouchWalkSkiLlR, skiKneeR, intoGlide);
                }
                else
                {
                    _uaLT = skiL;
                    _uaRT = skiR;
                    _laLT = skiElL;
                    _laRT = skiElR;
                    _spineT = skiSp;
                    _hipsT = skiHp;
                    _headT = skiHd;
                    _ulLT = skiThighL;
                    _ulRT = skiThighR;
                    _llLT = skiKneeL;
                    _llRT = skiKneeR;
                }
            }
            if (_stillSkiSnap && _skiFromCrouch && skiing && !jet && !crouch && !wallRun && !climb && !punching)
            {
                // The planted guard eases into the glide, then the glide holds.
                // A crouch walk into a ski has its own ease. A walk into a ski keeps its ease.
                // A slide into a ski has its own ease. Ski speed is unchanged.
                float intoGlide = _skiFromCrouchIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiSp = _spine0 * Quaternion.Euler(26f, 0f, 0f);
                Quaternion skiHp = _hips0 * Quaternion.Euler(14f, 0f, 0f);
                Quaternion skiHd = _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f);
                Quaternion skiThighL = _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f);
                Quaternion skiThighR = _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f);
                Quaternion skiKneeL = _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f);
                Quaternion skiKneeR = _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f);
                if (intoGlide < 0.98f)
                {
                    _uaLT = Quaternion.Slerp(_stillSkiUaL, skiL, intoGlide);
                    _uaRT = Quaternion.Slerp(_stillSkiUaR, skiR, intoGlide);
                    _laLT = Quaternion.Slerp(_stillSkiLaL, skiElL, intoGlide);
                    _laRT = Quaternion.Slerp(_stillSkiLaR, skiElR, intoGlide);
                    _spineT = Quaternion.Slerp(_stillSkiSp, skiSp, intoGlide);
                    _hipsT = Quaternion.Slerp(_stillSkiHp, skiHp, intoGlide);
                    _headT = Quaternion.Slerp(_stillSkiHd, skiHd, intoGlide);
                    _ulLT = Quaternion.Slerp(_stillSkiUlL, skiThighL, intoGlide);
                    _ulRT = Quaternion.Slerp(_stillSkiUlR, skiThighR, intoGlide);
                    _llLT = Quaternion.Slerp(_stillSkiLlL, skiKneeL, intoGlide);
                    _llRT = Quaternion.Slerp(_stillSkiLlR, skiKneeR, intoGlide);
                }
                else
                {
                    _uaLT = skiL;
                    _uaRT = skiR;
                    _laLT = skiElL;
                    _laRT = skiElR;
                    _spineT = skiSp;
                    _hipsT = skiHp;
                    _headT = skiHd;
                    _ulLT = skiThighL;
                    _ulRT = skiThighR;
                    _llLT = skiKneeL;
                    _llRT = skiKneeR;
                }
            }
            if (_walkFromSki && _walkFromSkiIn < 0.98f && !crouch && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The glide eases into the walk, then the walk holds.
                // A ski into a run has its own ease. A walk into a ski keeps its ease.
                // Ski speed is unchanged.
                float intoWalk = _walkFromSkiIn;
                _uaLT = Quaternion.Slerp(_glideWalkUaL, _uaLT, intoWalk);
                _uaRT = Quaternion.Slerp(_glideWalkUaR, _uaRT, intoWalk);
                _laLT = Quaternion.Slerp(_glideWalkLaL, _laLT, intoWalk);
                _laRT = Quaternion.Slerp(_glideWalkLaR, _laRT, intoWalk);
                _spineT = Quaternion.Slerp(_glideWalkSp, _spineT, intoWalk);
                _hipsT = Quaternion.Slerp(_glideWalkHp, _hipsT, intoWalk);
                _headT = Quaternion.Slerp(_glideWalkHd, _headT, intoWalk);
                _ulLT = Quaternion.Slerp(_glideWalkUlL, _ulLT, intoWalk);
                _ulRT = Quaternion.Slerp(_glideWalkUlR, _ulRT, intoWalk);
                _llLT = Quaternion.Slerp(_glideWalkLlL, _llLT, intoWalk);
                _llRT = Quaternion.Slerp(_glideWalkLlR, _llRT, intoWalk);
            }
            if (_runFromSki && _runFromSkiIn < 0.98f && !crouch && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The glide eases into the run, then the run holds.
                // A ski into a walk has its own ease. A run into a ski keeps its ease.
                // Ski speed is unchanged.
                float intoRun = _runFromSkiIn;
                _uaLT = Quaternion.Slerp(_glideRunUaL, _uaLT, intoRun);
                _uaRT = Quaternion.Slerp(_glideRunUaR, _uaRT, intoRun);
                _laLT = Quaternion.Slerp(_glideRunLaL, _laLT, intoRun);
                _laRT = Quaternion.Slerp(_glideRunLaR, _laRT, intoRun);
                _spineT = Quaternion.Slerp(_glideRunSp, _spineT, intoRun);
                _hipsT = Quaternion.Slerp(_glideRunHp, _hipsT, intoRun);
                _headT = Quaternion.Slerp(_glideRunHd, _headT, intoRun);
                _ulLT = Quaternion.Slerp(_glideRunUlL, _ulLT, intoRun);
                _ulRT = Quaternion.Slerp(_glideRunUlR, _ulRT, intoRun);
                _llLT = Quaternion.Slerp(_glideRunLlL, _llLT, intoRun);
                _llRT = Quaternion.Slerp(_glideRunLlR, _llRT, intoRun);
            }
            if (_idleFromSki && _idleFromSkiIn < 0.98f && !crouch && !sliding && !jet && !punching && !wallRun && !climb)
            {
                // The glide eases into the idle, then the idle holds.
                // A ski into a walk has its own ease. A ski into a run has its own ease.
                // A ski into a still crouch keeps its ease. Ski speed is unchanged.
                float intoIdle = _idleFromSkiIn;
                _uaLT = Quaternion.Slerp(_glideIdleUaL, _uaLT, intoIdle);
                _uaRT = Quaternion.Slerp(_glideIdleUaR, _uaRT, intoIdle);
                _laLT = Quaternion.Slerp(_glideIdleLaL, _laLT, intoIdle);
                _laRT = Quaternion.Slerp(_glideIdleLaR, _laRT, intoIdle);
                _spineT = Quaternion.Slerp(_glideIdleSp, _spineT, intoIdle);
                _hipsT = Quaternion.Slerp(_glideIdleHp, _hipsT, intoIdle);
                _headT = Quaternion.Slerp(_glideIdleHd, _headT, intoIdle);
                _ulLT = Quaternion.Slerp(_glideIdleUlL, _ulLT, intoIdle);
                _ulRT = Quaternion.Slerp(_glideIdleUlR, _ulRT, intoIdle);
                _llLT = Quaternion.Slerp(_glideIdleLlL, _llLT, intoIdle);
                _llRT = Quaternion.Slerp(_glideIdleLlR, _llRT, intoIdle);
            }
            if (_skiFromWall && !_skiFromClimb && !_skiFromSoft && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromWallIn < 0.98f)
            {
                // The leave eases into the glide, then the glide holds.
                // A climb into a ski keeps its ease. A wall run into a jump keeps its push.
                // A soft landing into a ski keeps its ease. A jump into a ski keeps its ease.
                // Ski speed is unchanged. Exit time is unchanged.
                float intoGlide = _skiFromWallIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_wallSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_wallSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_wallSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_wallSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_wallSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_wallSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_wallSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_wallSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_wallSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_wallSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_wallSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromClimb && !_skiFromWall && !_skiFromSoft && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromClimbIn < 0.98f)
            {
                // The grab eases into the glide, then the glide holds.
                // A wall run into a ski keeps its own ease. A climb into a jump keeps its push.
                // A climb into a punch keeps its ease. A jump into a ski keeps its ease.
                // Ski speed is unchanged. Exit time is unchanged.
                float intoGlide = _skiFromClimbIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_climbSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_climbSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_climbSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_climbSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_climbSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_climbSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_climbSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_climbSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_climbSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_climbSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_climbSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromMiss && !_skiFromReady && !_skiFromClaim && !_skiFromGrapple && !_skiFromDart && !_skiFromHard && !_skiFromSoft && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromMissIn < 0.98f)
            {
                // The whiff eases into the glide, then the glide holds.
                // A punch into a ski keeps its ease. A punch miss into a tag keeps its ease.
                // A punch miss into a jump keeps its push. A punch miss into an air dash keeps its ease.
                // A dash coming off cooldown into a slide keeps its ease. Whiff time is unchanged. Ski speed is unchanged.
                float intoGlide = _skiFromMissIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_missSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_missSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_missSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_missSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_missSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_missSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_missSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_missSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_missSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_missSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_missSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromReady && !_skiFromMiss && !_skiFromClaim && !_skiFromGrapple && !_skiFromDart && !_skiFromHard && !_skiFromSoft && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromReadyIn < 0.98f)
            {
                // The pulse eases into the glide, then the glide holds.
                // Becoming It into a ski keeps its ease. A dash coming off cooldown into a punch keeps its ease.
                // A dash coming off cooldown into a tag keeps its ease. A dash coming off cooldown into a jump keeps its push.
                // Becoming It into a slide keeps its ease. Duration and cooldown are unchanged. Ski speed is unchanged.
                float intoGlide = _skiFromReadyIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_readySkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_readySkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_readySkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_readySkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_readySkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_readySkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_readySkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_readySkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_readySkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_readySkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_readySkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromClaim && !_skiFromReady && !_skiFromGrapple && !_skiFromDart && !_skiFromHard && !_skiFromSoft && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromClaimIn < 0.98f)
            {
                // The claim eases into the glide, then the glide holds.
                // A grapple release into a ski keeps its ease. Becoming It into a punch keeps its ease.
                // Becoming It into a jump keeps its push. A grapple release into a slide keeps its ease.
                // Claim time is unchanged. Ski speed is unchanged.
                float intoGlide = _skiFromClaimIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_claimSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_claimSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_claimSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_claimSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_claimSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_claimSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_claimSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_claimSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_claimSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_claimSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_claimSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromGrapple && !_skiFromClaim && !_skiFromDart && !_skiFromHard && !_skiFromSoft && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromGrappleIn < 0.98f)
            {
                // The line eases into the glide, then the glide holds.
                // An air crouch into a ski keeps its ease. A grapple release into a jump keeps its push.
                // A grapple release into a punch keeps its ease. An air dash into a ski keeps its ease.
                // The gate stays off. Ski speed is unchanged.
                float intoGlide = _skiFromGrappleIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_grappleSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_grappleSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_grappleSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_grappleSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_grappleSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_grappleSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_grappleSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_grappleSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_grappleSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_grappleSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_grappleSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromDart && !_skiFromGrapple && !_skiFromHard && !_skiFromSoft && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromDartIn < 0.98f)
            {
                // The dart eases into the glide, then the glide holds.
                // A hard landing into a ski keeps its ease. An air dash into a ski keeps its ease.
                // A jump into a ski keeps its ease. An air crouch into a slide keeps its ease.
                // Fall speed is unchanged. Ski speed is unchanged.
                float intoGlide = _skiFromDartIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_dartSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_dartSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_dartSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_dartSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_dartSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_dartSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_dartSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_dartSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_dartSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_dartSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_dartSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromHard && !_skiFromDart && !_skiFromSoft && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromHardIn < 0.98f)
            {
                // The deeper absorb eases into the glide, then the glide holds.
                // A soft landing into a ski keeps its ease. A jump into a ski keeps its ease.
                // An air dash into a ski keeps its ease. A hard landing into a slide keeps its ease.
                // Ski speed is unchanged. Land time is unchanged.
                float intoGlide = _skiFromHardIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_hardSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_hardSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_hardSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_hardSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_hardSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_hardSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_hardSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_hardSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_hardSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_hardSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_hardSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromSoft && !_skiFromHard && !_skiFromTag && !_skiFromPunch && !_skiFromDash && !_skiFromJump && skiing && !jet && !crouch && !wallRun && !climb && _skiFromSoftIn < 0.98f)
            {
                // The absorb eases into the glide, then the glide holds.
                // A hard landing into a ski keeps its own ease. A jump into a ski keeps its ease.
                // A punch into a ski keeps its ease. A slide into a ski has its own ease.
                // Ski speed is unchanged. Land time is unchanged.
                float intoGlide = _skiFromSoftIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_softSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_softSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_softSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_softSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_softSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_softSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_softSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_softSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_softSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_softSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_softSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromTag && !_skiFromPunch && !_skiFromDash && skiing && !jet && !crouch && !wallRun && !climb && _skiFromTagIn < 0.98f)
            {
                // The connect eases into the glide, then the glide holds.
                // A punch into a ski keeps its ease. A punch into a slide keeps its ease.
                // A tag into a jump keeps its push. A slide into a ski has its own ease.
                // Ski speed is unchanged. Connect time is unchanged.
                float intoGlide = _skiFromTagIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_tagSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_tagSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_tagSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_tagSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_tagSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_tagSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_tagSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_tagSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_tagSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_tagSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_tagSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromPunch && !_skiFromDash && skiing && !jet && !crouch && !wallRun && !climb && _skiFromPunchIn < 0.98f)
            {
                // The cock or the strike eases into the glide, then the glide holds.
                // A ski into a punch keeps its ease. A slide into a ski has its own ease.
                // A jump into a ski keeps its ease. Ski speed is unchanged. Windup time is unchanged.
                float intoGlide = _skiFromPunchIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_punchSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_punchSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_punchSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_punchSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_punchSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_punchSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_punchSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_punchSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_punchSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_punchSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_punchSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_skiFromDash && !airDashing && !punching && !wallRun && !climb && skiing && !jet && !crouch && _skiFromDashIn < 0.98f)
            {
                // The burst eases into the glide, then the glide holds.
                // A wall run into a ski keeps its ease. A climb into a ski keeps its ease.
                // An air dash into a slide keeps its ease. A ski into a dash keeps its ease.
                // Ski speed is unchanged. Duration and cooldown are unchanged.
                float intoGlide = _skiFromDashIn;
                float glideL = Mathf.Max(0f, sinC);
                float glideR = Mathf.Max(0f, -sinC);
                float skateL = RunArmPitch(-sinC, 16f);
                float skateR = RunArmPitch(sinC, 16f);
                Quaternion skiL = _uaL0 * Quaternion.Euler(-18f + skateL, 22f, armZ);
                Quaternion skiR = _uaR0 * Quaternion.Euler(-18f + skateR, -22f, -armZ);
                Quaternion skiElL = _laL0 * Quaternion.Euler(-12f, 0f, 0f);
                Quaternion skiElR = _laR0 * Quaternion.Euler(-12f, 0f, 0f);
                _uaLT = Quaternion.Slerp(_dashSkiUaL, skiL, intoGlide);
                _uaRT = Quaternion.Slerp(_dashSkiUaR, skiR, intoGlide);
                _laLT = Quaternion.Slerp(_dashSkiLaL, skiElL, intoGlide);
                _laRT = Quaternion.Slerp(_dashSkiLaR, skiElR, intoGlide);
                _spineT = Quaternion.Slerp(_dashSkiSp, _spine0 * Quaternion.Euler(26f, 0f, 0f), intoGlide);
                _hipsT = Quaternion.Slerp(_dashSkiHp, _hips0 * Quaternion.Euler(14f, 0f, 0f), intoGlide);
                _headT = Quaternion.Slerp(_dashSkiHd, _head0 * Quaternion.Euler(-breath * 0.4f, 0f, 0f), intoGlide);
                _ulLT = Quaternion.Slerp(_dashSkiUlL, _ulL0 * Quaternion.Euler((glideL - glideR * 0.5f) * 32f, 0f, 0f), intoGlide);
                _ulRT = Quaternion.Slerp(_dashSkiUlR, _ulR0 * Quaternion.Euler((glideR - glideL * 0.5f) * 32f, 0f, 0f), intoGlide);
                _llLT = Quaternion.Slerp(_dashSkiLlL, _llL0 * Quaternion.Euler(-(6f + glideL * 32f), 0f, 0f), intoGlide);
                _llRT = Quaternion.Slerp(_dashSkiLlR, _llR0 * Quaternion.Euler(-(6f + glideR * 32f), 0f, 0f), intoGlide);
            }
            if (_dartFromDash && !airDashing && !punching && !wallRun && !climb)
            {
                // The burst eases into the dart, then the dart holds. An air crouch into a dash keeps its ease.
                // Fall speed stays doubled. Duration and cooldown are unchanged.
                float intoDart = _dartFromDashIn;
                Quaternion burstL = _uaL0 * Quaternion.Euler(108f, 32f, armZ);
                Quaternion burstR = _uaR0 * Quaternion.Euler(108f, -32f, -armZ);
                Quaternion dartL = _uaL0 * Quaternion.Euler(-16f, 12f, armZ);
                Quaternion dartR = _uaR0 * Quaternion.Euler(-16f, -12f, -armZ);
                Quaternion burstElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion burstElR = _laR0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion dartElL = _laL0 * Quaternion.Euler(-58f, 0f, 0f);
                Quaternion dartElR = _laR0 * Quaternion.Euler(-58f, 0f, 0f);
                _uaLT = Quaternion.Slerp(burstL, dartL, intoDart);
                _uaRT = Quaternion.Slerp(burstR, dartR, intoDart);
                _laLT = Quaternion.Slerp(burstElL, dartElL, intoDart);
                _laRT = Quaternion.Slerp(burstElR, dartElR, intoDart);
                _spineT = Quaternion.Slerp(_spine0 * Quaternion.Euler(58f, 0f, 0f), _spine0 * Quaternion.Euler(46f, 0f, 0f), intoDart);
                _hipsT = Quaternion.Slerp(_hips0 * Quaternion.Euler(22f, 0f, 0f), _hips0 * Quaternion.Euler(24f, 0f, 0f), intoDart);
                _headT = Quaternion.Slerp(_head0 * Quaternion.Euler(0f, 0f, 0f), _head0 * Quaternion.Euler(12f, 0f, 0f), intoDart);
                _ulLT = Quaternion.Slerp(_ulL0 * Quaternion.Euler(72f, 0f, 0f), _ulL0 * Quaternion.Euler(34f, 0f, 0f), intoDart);
                _ulRT = Quaternion.Slerp(_ulR0 * Quaternion.Euler(-34f, 0f, 0f), _ulR0 * Quaternion.Euler(34f, 0f, 0f), intoDart);
                _llLT = Quaternion.Slerp(_llL0 * Quaternion.Euler(-62f, 0f, 0f), _llL0 * Quaternion.Euler(-50f, 0f, 0f), intoDart);
                _llRT = Quaternion.Slerp(_llR0 * Quaternion.Euler(-18f, 0f, 0f), _llR0 * Quaternion.Euler(-50f, 0f, 0f), intoDart);
            }
            if (_tagFromCrouch && !_tagFromMiss && !_tagFromPunch && !_tagFromReady && !_tagFromGrapple && !_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && _tagFromCrouchIn < 0.98f)
            {
                // The guard eases into the connect, then the connect holds.
                // A still crouch into a punch keeps its ease. A crouch walk into a tag keeps its pose.
                // An air crouch into a tag keeps its ease. A slide into a tag keeps its ease.
                // A crouch claim keeps its pose. A punch into a tag keeps its ease. Connect time is unchanged.
                float intoTag = _tagFromCrouchIn;
                _uaLT = Quaternion.Slerp(_crouchTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_crouchTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_crouchTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_crouchTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_crouchTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_crouchTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_crouchTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_crouchTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_crouchTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_crouchTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_crouchTagLlR, _llRT, intoTag);
            }
            if (_tagFromWalk && !_tagFromCrouch && !_tagFromMiss && !_tagFromPunch && !_tagFromReady && !_tagFromGrapple && !_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromWalkIn < 0.98f)
            {
                // The walk eases into the connect, then the connect holds.
                // A walk into a punch keeps its ease. A jump into a tag keeps its ease.
                // A still crouch into a tag keeps its ease. Connect time is unchanged.
                float intoTag = _tagFromWalkIn;
                _uaLT = Quaternion.Slerp(_walkTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_walkTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_walkTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_walkTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_walkTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_walkTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_walkTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_walkTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_walkTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_walkTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_walkTagLlR, _llRT, intoTag);
            }
            if (_tagFromSprint && !_tagFromWalk && !_tagFromCrouch && !_tagFromMiss && !_tagFromPunch && !_tagFromReady && !_tagFromGrapple && !_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromSprintIn < 0.98f)
            {
                // The run eases into the connect, then the connect holds.
                // A run into a punch keeps its ease. A walk into a tag keeps its ease.
                // A jump into a tag keeps its ease. Connect time is unchanged.
                float intoTag = _tagFromSprintIn;
                _uaLT = Quaternion.Slerp(_sprintTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_sprintTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_sprintTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_sprintTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_sprintTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_sprintTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_sprintTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_sprintTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_sprintTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_sprintTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_sprintTagLlR, _llRT, intoTag);
            }
            if (_tagFromCrouchWalk && !_tagFromSprint && !_tagFromWalk && !_tagFromCrouch && !_tagFromMiss && !_tagFromPunch && !_tagFromReady && !_tagFromGrapple && !_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && _tagFromCrouchWalkIn < 0.98f)
            {
                // The low stride eases into the connect, then the connect holds.
                // A crouch walk into a punch keeps its ease. A run into a tag keeps its ease.
                // A still crouch into a tag keeps its ease. Connect time is unchanged.
                float intoTag = _tagFromCrouchWalkIn;
                _uaLT = Quaternion.Slerp(_crouchWalkTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_crouchWalkTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_crouchWalkTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_crouchWalkTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_crouchWalkTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_crouchWalkTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_crouchWalkTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_crouchWalkTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_crouchWalkTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_crouchWalkTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_crouchWalkTagLlR, _llRT, intoTag);
            }
            if (_tagFromMiss && !_tagFromPunch && !_tagFromReady && !_tagFromGrapple && !_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromMissIn < 0.98f)
            {
                // The whiff eases into the connect, then the connect holds.
                // A tag into a punch keeps its ease. A punch into a tag keeps its ease.
                // A punch miss into a jump keeps its push. A punch miss into an air dash keeps its ease.
                // A crouch miss keeps its pose. Connect time is unchanged.
                float intoTag = _tagFromMissIn;
                _uaLT = Quaternion.Slerp(_missTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_missTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_missTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_missTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_missTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_missTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_missTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_missTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_missTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_missTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_missTagLlR, _llRT, intoTag);
            }
            if (_tagFromPunch && !_tagFromReady && !_tagFromGrapple && !_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromPunchIn < 0.98f)
            {
                // The cock or the strike eases into the connect, then the connect holds.
                // A dash coming off cooldown into a tag keeps its ease. A dash coming off cooldown into a punch keeps its ease.
                // A crouch tag keeps its pose. A tag into a jump keeps its push.
                // Connect time is unchanged. Windup time is unchanged.
                float intoTag = _tagFromPunchIn;
                _uaLT = Quaternion.Slerp(_punchTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_punchTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_punchTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_punchTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_punchTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_punchTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_punchTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_punchTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_punchTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_punchTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_punchTagLlR, _llRT, intoTag);
            }
            if (_tagFromReady && !_tagFromGrapple && !_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch)
            {
                // The pulse eases into the connect, then the connect holds.
                // A dash coming off cooldown into a punch keeps its ease. A dash coming off cooldown into a jump keeps its push.
                // A crouch ready keeps its pose. A grapple release into a tag keeps its ease.
                // Connect time is unchanged. Duration and cooldown are unchanged.
                float intoTag = _tagFromReadyIn;
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, punchProg));
                Quaternion connectL = _uaL0 * Quaternion.Euler(-32f, 18f, armZ);
                Quaternion connectR = _uaR0 * Quaternion.Euler(-118f, 52f, -22f);
                Quaternion connectElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion connectElR = _laR0 * Quaternion.Euler(-18f, 0f, 0f);
                float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
                float idle = 1f - gait;
                float amp = Mathf.Lerp(36f, 64f, gait);
                float outY = Mathf.Lerp(12f, 8f, gait);
                float roll = Mathf.Lerp(0f, armZ, gait);
                float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                float armBreath = breath * 0.55f * idle;
                Quaternion runL = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll);
                Quaternion runR = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll);
                float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                float plant = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg));
                Quaternion tagL = Quaternion.Slerp(connectL, runL, plant);
                Quaternion tagR = Quaternion.Slerp(connectR, runR, settle);
                Quaternion tagElL = Quaternion.Slerp(connectElL, _laL0 * Quaternion.Euler(elbowL, 0f, 0f), plant);
                Quaternion tagElR = Quaternion.Slerp(connectElR, _laR0 * Quaternion.Euler(elbowR, 0f, 0f), settle);
                Quaternion tagSp = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spineT, plant);
                Quaternion tagHp = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hipsT, plant);
                _uaLT = Quaternion.Slerp(_readyTagUaL, tagL, intoTag);
                _uaRT = Quaternion.Slerp(_readyTagUaR, tagR, intoTag);
                _laLT = Quaternion.Slerp(_readyTagLaL, tagElL, intoTag);
                _laRT = Quaternion.Slerp(_readyTagLaR, tagElR, intoTag);
                _spineT = Quaternion.Slerp(_readyTagSp, tagSp, intoTag);
                _hipsT = Quaternion.Slerp(_readyTagHp, tagHp, intoTag);
                _headT = Quaternion.Slerp(_readyTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_readyTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_readyTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_readyTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_readyTagLlR, _llRT, intoTag);
            }
            if (_tagFromGrapple && !_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch)
            {
                // The line eases into the connect, then the connect holds.
                // A grapple release into a punch keeps its ease. A grapple release into an air dash keeps its ease.
                // A grapple release into a jump keeps its push. Becoming It into a tag keeps its ease.
                // A crouch release keeps its pose. Connect time is unchanged. The gate stays off.
                float intoTag = _tagFromGrappleIn;
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, punchProg));
                Quaternion connectL = _uaL0 * Quaternion.Euler(-32f, 18f, armZ);
                Quaternion connectR = _uaR0 * Quaternion.Euler(-118f, 52f, -22f);
                Quaternion connectElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion connectElR = _laR0 * Quaternion.Euler(-18f, 0f, 0f);
                float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
                float idle = 1f - gait;
                float amp = Mathf.Lerp(36f, 64f, gait);
                float outY = Mathf.Lerp(12f, 8f, gait);
                float roll = Mathf.Lerp(0f, armZ, gait);
                float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                float armBreath = breath * 0.55f * idle;
                Quaternion runL = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll);
                Quaternion runR = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll);
                float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                float plant = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg));
                Quaternion tagL = Quaternion.Slerp(connectL, runL, plant);
                Quaternion tagR = Quaternion.Slerp(connectR, runR, settle);
                Quaternion tagElL = Quaternion.Slerp(connectElL, _laL0 * Quaternion.Euler(elbowL, 0f, 0f), plant);
                Quaternion tagElR = Quaternion.Slerp(connectElR, _laR0 * Quaternion.Euler(elbowR, 0f, 0f), settle);
                Quaternion tagSp = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spineT, plant);
                Quaternion tagHp = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hipsT, plant);
                _uaLT = Quaternion.Slerp(_grappleTagUaL, tagL, intoTag);
                _uaRT = Quaternion.Slerp(_grappleTagUaR, tagR, intoTag);
                _laLT = Quaternion.Slerp(_grappleTagLaL, tagElL, intoTag);
                _laRT = Quaternion.Slerp(_grappleTagLaR, tagElR, intoTag);
                _spineT = Quaternion.Slerp(_grappleTagSp, tagSp, intoTag);
                _hipsT = Quaternion.Slerp(_grappleTagHp, tagHp, intoTag);
                _headT = Quaternion.Slerp(_grappleTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_grappleTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_grappleTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_grappleTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_grappleTagLlR, _llRT, intoTag);
            }
            if (_tagFromItClaim && !_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch)
            {
                // The claim eases into the connect, then the connect holds. The claim pose would fold it back.
                // Becoming It into a punch keeps its ease. Becoming It into an air dash keeps its ease.
                // Becoming It into a jump keeps its push. An air crouch into a tag keeps its ease.
                // A crouch claim keeps its pose. Connect time is unchanged. Claim time is unchanged.
                float intoTag = _tagFromItClaimIn;
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, punchProg));
                Quaternion connectL = _uaL0 * Quaternion.Euler(-32f, 18f, armZ);
                Quaternion connectR = _uaR0 * Quaternion.Euler(-118f, 52f, -22f);
                Quaternion connectElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion connectElR = _laR0 * Quaternion.Euler(-18f, 0f, 0f);
                float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
                float idle = 1f - gait;
                float amp = Mathf.Lerp(36f, 64f, gait);
                float outY = Mathf.Lerp(12f, 8f, gait);
                float roll = Mathf.Lerp(0f, armZ, gait);
                float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                float armBreath = breath * 0.55f * idle;
                Quaternion runL = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll);
                Quaternion runR = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll);
                float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                float plant = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg));
                Quaternion tagL = Quaternion.Slerp(connectL, runL, plant);
                Quaternion tagR = Quaternion.Slerp(connectR, runR, settle);
                Quaternion tagElL = Quaternion.Slerp(connectElL, _laL0 * Quaternion.Euler(elbowL, 0f, 0f), plant);
                Quaternion tagElR = Quaternion.Slerp(connectElR, _laR0 * Quaternion.Euler(elbowR, 0f, 0f), settle);
                Quaternion tagSp = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spineT, plant);
                Quaternion tagHp = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hipsT, plant);
                _uaLT = Quaternion.Slerp(_claimTagUaL, tagL, intoTag);
                _uaRT = Quaternion.Slerp(_claimTagUaR, tagR, intoTag);
                _laLT = Quaternion.Slerp(_claimTagLaL, tagElL, intoTag);
                _laRT = Quaternion.Slerp(_claimTagLaR, tagElR, intoTag);
                _spineT = Quaternion.Slerp(_claimTagSp, tagSp, intoTag);
                _hipsT = Quaternion.Slerp(_claimTagHp, tagHp, intoTag);
                _headT = Quaternion.Slerp(_claimTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_claimTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_claimTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_claimTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_claimTagLlR, _llRT, intoTag);
            }
            if (_tagFromDart && !_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromDartIn < 0.98f)
            {
                // The dart eases into the connect, then the connect holds.
                // An air crouch into a punch keeps its ease. An air crouch into an air dash keeps its ease.
                // An air crouch into a jump keeps its push. A jump into a tag keeps its ease.
                // A crouch tag keeps its pose. Connect time is unchanged. Fall speed is unchanged.
                float intoTag = _tagFromDartIn;
                _uaLT = Quaternion.Slerp(_dartTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_dartTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_dartTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_dartTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_dartTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_dartTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_dartTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_dartTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_dartTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_dartTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_dartTagLlR, _llRT, intoTag);
            }
            if (_tagFromWall && !_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch)
            {
                // The wall exit eases into the connect, then the connect holds. The wall pose would cover it.
                // A wall exit into a punch keeps its ease. A wall exit into an air dash keeps its ease.
                // A wall exit into a jump keeps its push. A climb into a tag keeps its ease.
                // A crouch tag keeps its pose. Connect time is unchanged. Exit time is unchanged.
                float intoTag = _tagFromWallIn;
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, punchProg));
                Quaternion connectL = _uaL0 * Quaternion.Euler(-32f, 18f, armZ);
                Quaternion connectR = _uaR0 * Quaternion.Euler(-118f, 52f, -22f);
                Quaternion connectElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion connectElR = _laR0 * Quaternion.Euler(-18f, 0f, 0f);
                Quaternion tagL;
                Quaternion tagR;
                Quaternion tagElL;
                Quaternion tagElR;
                Quaternion tagSp;
                Quaternion tagHp;
                if (claimAmt > 0.04f)
                {
                    tagR = Quaternion.Slerp(connectR, _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), settle);
                    tagElR = Quaternion.Slerp(connectElR, _laR0 * Quaternion.Euler(-12f, 0f, 0f), settle);
                    tagL = Quaternion.Slerp(connectL, _uaL0 * Quaternion.Euler(-128f, 8f, armZ), settle);
                    tagElL = Quaternion.Slerp(connectElL, _laL0 * Quaternion.Euler(-10f, 0f, 0f), settle);
                    tagSp = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spine0 * Quaternion.Euler(-22f, -16f, 0f), settle);
                    tagHp = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hips0 * Quaternion.Euler(4f, 0f, 0f), settle);
                }
                else
                {
                    float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
                    float idle = 1f - gait;
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                    float armBreath = breath * 0.55f * idle;
                    Quaternion runL = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll);
                    Quaternion runR = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll);
                    float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                    float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                    float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                    float plant = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg));
                    tagR = Quaternion.Slerp(connectR, runR, settle);
                    tagElR = Quaternion.Slerp(connectElR, _laR0 * Quaternion.Euler(elbowR, 0f, 0f), settle);
                    tagL = Quaternion.Slerp(connectL, runL, plant);
                    tagElL = Quaternion.Slerp(connectElL, _laL0 * Quaternion.Euler(elbowL, 0f, 0f), plant);
                    tagSp = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spineT, plant);
                    tagHp = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hipsT, plant);
                }
                _uaLT = Quaternion.Slerp(_wallTagUaL, tagL, intoTag);
                _uaRT = Quaternion.Slerp(_wallTagUaR, tagR, intoTag);
                _laLT = Quaternion.Slerp(_wallTagLaL, tagElL, intoTag);
                _laRT = Quaternion.Slerp(_wallTagLaR, tagElR, intoTag);
                _spineT = Quaternion.Slerp(_wallTagSp, tagSp, intoTag);
                _hipsT = Quaternion.Slerp(_wallTagHp, tagHp, intoTag);
                _headT = Quaternion.Slerp(_wallTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_wallTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_wallTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_wallTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_wallTagLlR, _llRT, intoTag);
            }
            if (_tagFromClimb && !_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch)
            {
                // The grab eases into the connect, then the connect holds. The climb pose would cover it.
                // A climb into a punch keeps its ease. A climb into an air dash keeps its ease.
                // A climb into a jump keeps its push. A wall run keeps its pose. A crouch tag keeps its pose.
                // Connect time is unchanged. Exit time is unchanged.
                float intoTag = _tagFromClimbIn;
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, punchProg));
                Quaternion connectL = _uaL0 * Quaternion.Euler(-32f, 18f, armZ);
                Quaternion connectR = _uaR0 * Quaternion.Euler(-118f, 52f, -22f);
                Quaternion connectElL = _laL0 * Quaternion.Euler(-22f, 0f, 0f);
                Quaternion connectElR = _laR0 * Quaternion.Euler(-18f, 0f, 0f);
                Quaternion tagL;
                Quaternion tagR;
                Quaternion tagElL;
                Quaternion tagElR;
                Quaternion tagSp;
                Quaternion tagHp;
                if (claimAmt > 0.04f)
                {
                    tagR = Quaternion.Slerp(connectR, _uaR0 * Quaternion.Euler(-36f, -48f, -armZ), settle);
                    tagElR = Quaternion.Slerp(connectElR, _laR0 * Quaternion.Euler(-12f, 0f, 0f), settle);
                    tagL = Quaternion.Slerp(connectL, _uaL0 * Quaternion.Euler(-128f, 8f, armZ), settle);
                    tagElL = Quaternion.Slerp(connectElL, _laL0 * Quaternion.Euler(-10f, 0f, 0f), settle);
                    tagSp = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spine0 * Quaternion.Euler(-22f, -16f, 0f), settle);
                    tagHp = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hips0 * Quaternion.Euler(4f, 0f, 0f), settle);
                }
                else
                {
                    float gait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
                    float idle = 1f - gait;
                    float amp = Mathf.Lerp(36f, 64f, gait);
                    float outY = Mathf.Lerp(12f, 8f, gait);
                    float roll = Mathf.Lerp(0f, armZ, gait);
                    float reachY = Mathf.Lerp(outY, outY + 6f, _runVis);
                    float yL = Mathf.Lerp(outY, reachY, Mathf.Clamp01(-sinC) * gait);
                    float yR = Mathf.Lerp(outY, reachY, Mathf.Clamp01(sinC) * gait);
                    float armBreath = breath * 0.55f * idle;
                    Quaternion runL = _uaL0 * Quaternion.Euler(RunArmPitch(-sinC, amp) - 12f * idle + armBreath, yL, roll);
                    Quaternion runR = _uaR0 * Quaternion.Euler(RunArmPitch(sinC, amp) - 12f * idle + armBreath, -yR, -roll);
                    float elbowReach = Mathf.Lerp(-10f, -6f, _runVis);
                    float elbowPull = Mathf.Lerp(-18f, -30f, _runVis);
                    float elbowL = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(sinC) * gait);
                    float elbowR = Mathf.Lerp(elbowReach, elbowPull, Mathf.Clamp01(-sinC) * gait);
                    float plant = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(punchProg));
                    tagR = Quaternion.Slerp(connectR, runR, settle);
                    tagElR = Quaternion.Slerp(connectElR, _laR0 * Quaternion.Euler(elbowR, 0f, 0f), settle);
                    tagL = Quaternion.Slerp(connectL, runL, plant);
                    tagElL = Quaternion.Slerp(connectElL, _laL0 * Quaternion.Euler(elbowL, 0f, 0f), plant);
                    tagSp = Quaternion.Slerp(_spine0 * Quaternion.Euler(leanX + 14f, 18f, leanZ), _spineT, plant);
                    tagHp = Quaternion.Slerp(_hips0 * Quaternion.Euler(18f, 22f, 0f), _hipsT, plant);
                }
                _uaLT = Quaternion.Slerp(_climbTagUaL, tagL, intoTag);
                _uaRT = Quaternion.Slerp(_climbTagUaR, tagR, intoTag);
                _laLT = Quaternion.Slerp(_climbTagLaL, tagElL, intoTag);
                _laRT = Quaternion.Slerp(_climbTagLaR, tagElR, intoTag);
                _spineT = Quaternion.Slerp(_climbTagSp, tagSp, intoTag);
                _hipsT = Quaternion.Slerp(_climbTagHp, tagHp, intoTag);
                _headT = Quaternion.Slerp(_climbTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_climbTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_climbTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_climbTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_climbTagLlR, _llRT, intoTag);
            }
            if (_tagFromSlide && !_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromSlideIn < 0.98f)
            {
                // The wedge eases into the connect, then the connect holds.
                // A ski into a tag keeps its ease. A slide into a punch keeps its ease.
                // A slide into a jump keeps its push. A jump into a tag keeps its ease.
                // A crouch tag keeps its pose. slideBoost stays 0. Connect time is unchanged.
                float intoTag = _tagFromSlideIn;
                _uaLT = Quaternion.Slerp(_slideTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_slideTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_slideTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_slideTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_slideTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_slideTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_slideTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_slideTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_slideTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_slideTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_slideTagLlR, _llRT, intoTag);
            }
            if (_tagFromSki && !_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromSkiIn < 0.98f)
            {
                // The glide eases into the connect, then the connect holds.
                // A ski into a punch keeps its ease. A ski into a jump keeps its push.
                // A slide into a punch keeps its ease. A jump into a tag keeps its ease.
                // A crouch tag keeps its pose. Connect time is unchanged. Ski speed is unchanged.
                float intoTag = _tagFromSkiIn;
                _uaLT = Quaternion.Slerp(_skiTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_skiTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_skiTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_skiTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_skiTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_skiTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_skiTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_skiTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_skiTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_skiTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_skiTagLlR, _llRT, intoTag);
            }
            if (_tagFromHard && !_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromHardIn < 0.98f)
            {
                // The deeper absorb eases into the connect, then the connect holds.
                // A soft landing into a tag keeps its ease. A hard landing into a jump keeps its push.
                // A jump into a tag keeps its ease. A crouch tag keeps its pose.
                // Connect time is unchanged. Land time is unchanged.
                float intoTag = _tagFromHardIn;
                _uaLT = Quaternion.Slerp(_hardTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_hardTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_hardTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_hardTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_hardTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_hardTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_hardTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_hardTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_hardTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_hardTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_hardTagLlR, _llRT, intoTag);
            }
            if (_tagFromSoft && !_tagFromJump && !_tagFromDash && !_jumpFromTag && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromSoftIn < 0.98f)
            {
                // The absorb eases into the connect, then the connect holds.
                // A jump into a tag keeps its ease. A soft landing into a jump keeps its push.
                // A hard landing keeps its pose. A crouch tag keeps its pose.
                // Connect time is unchanged. Land time is unchanged.
                float intoTag = _tagFromSoftIn;
                _uaLT = Quaternion.Slerp(_softTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_softTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_softTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_softTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_softTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_softTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_softTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_softTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_softTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_softTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_softTagLlR, _llRT, intoTag);
            }
            if (_tagFromDash && !_tagFromJump && !_punchFromDash && punching && phase == PunchPhase.HitRecover && !crouch && _tagFromDashIn < 0.98f)
            {
                // The burst eases into the connect, then the connect holds.
                // An air dash into a punch keeps its ease. A climb into a punch keeps its ease.
                // A jump into a tag keeps its ease. Connect time is unchanged.
                // Duration and cooldown are unchanged.
                float intoTag = _tagFromDashIn;
                _uaLT = Quaternion.Slerp(_dashTagUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_dashTagUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_dashTagLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_dashTagLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_dashTagSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_dashTagHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_dashTagHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_dashTagUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_dashTagUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_dashTagLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_dashTagLlR, _llRT, intoTag);
            }
            if (punching && phase == PunchPhase.HitRecover && _tagFromJump && !_jumpFromTag && _tagFromJumpIn < 0.98f)
            {
                // The apex or the landing eases into the connect, then the connect holds.
                // A crouch tag keeps its pose. A tag into a jump keeps its push. Jump height is unchanged.
                float intoTag = _tagFromJumpIn;
                _uaLT = Quaternion.Slerp(_tagJumpUaL, _uaLT, intoTag);
                _uaRT = Quaternion.Slerp(_tagJumpUaR, _uaRT, intoTag);
                _laLT = Quaternion.Slerp(_tagJumpLaL, _laLT, intoTag);
                _laRT = Quaternion.Slerp(_tagJumpLaR, _laRT, intoTag);
                _spineT = Quaternion.Slerp(_tagJumpSp, _spineT, intoTag);
                _hipsT = Quaternion.Slerp(_tagJumpHp, _hipsT, intoTag);
                _headT = Quaternion.Slerp(_tagJumpHd, _headT, intoTag);
                _ulLT = Quaternion.Slerp(_tagJumpUlL, _ulLT, intoTag);
                _ulRT = Quaternion.Slerp(_tagJumpUlR, _ulRT, intoTag);
                _llLT = Quaternion.Slerp(_tagJumpLlL, _llLT, intoTag);
                _llRT = Quaternion.Slerp(_tagJumpLlR, _llRT, intoTag);
            }

            float slew = bouncing || gliding || jet || punching || lunging || dashing || mantle || wallRun || climb || sliding || flinchAmt > 0.04f || claimAmt > 0.04f ? 42f : crouch ? 24f : air ? 18f : 20f;
            // 0.1s air dash never reached the whip pose at slew 42.
            bool punchWind = punching && phase == PunchPhase.Windup;
            bool handoff = flinchAmt > 0.2f || claimAmt > 0.2f;
            bool grappleTell = _grapplePose > 0.04f && !punching;
            // A hop is short. Slew 18 never reached the tuck or the trail before the landing.
            bool apexHang = air && airRise < 0.2f && airFall < 0.2f;
            bool airDive = _diveVis > 0.12f;
            bool airTell = air && (airRise > 0.12f || airFall > 0.12f || apexHang || airDive);
            float armSlewL = airDashing ? 78f : punchWind ? 90f : handoff ? 72f : grappleTell ? 36f : airTell ? 64f : (punching || lunging || dashing ? 42f : slew);
            float armSlewR = airDashing ? 78f : punchWind ? 90f : handoff ? 72f : grappleTell ? 36f : airTell ? 64f : (punching || lunging || dashing ? 46f : slew);
            // Run knees have to arrive inside one stride or the flex never shows.
            bool runCycle = grounded && !air && !sliding && !crouch && !dashing && !lunging && speed > 2f;
            // Buckle has to arrive during the short absorb, then follow the ease back into the stride.
            float legSlew = airDashing ? 78f : grappleTell ? 36f : airTell ? 64f : (_landSquash > 0.05f ? 46f : runCycle ? 44f : slew);
            float torsoSlew = airDashing ? 78f : grappleTell ? 36f : airTell ? 64f : slew;
            if (!(lunging || dashing))
                _airDashArms = false;
            if (!dashing && !lunging && _armRecover > 0f)
                _armRecover = Mathf.MoveTowards(_armRecover, 0f, dt);
            // After the burst, ease the arms into the fall or the run. Slew 64 snaps them into a second throw.
            // The legs still take the stride once the feet are on the ground.
            if (_armRecover > 0f && !airDashing && !dashing && !lunging && !punchWind && !handoff && !grappleTell)
            {
                armSlewL = 16f;
                armSlewR = 16f;
                torsoSlew = 16f;
                legSlew = grounded ? 44f : 16f;
            }
            Slew(ref _spine, _spineT, torsoSlew, dt);
            Slew(ref _hips, _hipsT, torsoSlew, dt);
            Slew(ref _head, _headT, slew, dt);
            Slew(ref _upperArmL, _uaLT, armSlewL, dt);
            Slew(ref _upperArmR, _uaRT, armSlewR, dt);
            Slew(ref _lowerArmL, _laLT, armSlewL, dt);
            Slew(ref _lowerArmR, _laRT, armSlewR, dt);
            Slew(ref _upperLegL, _ulLT, legSlew, dt);
            Slew(ref _upperLegR, _ulRT, legSlew, dt);
            Slew(ref _lowerLegL, _llLT, legSlew, dt);
            Slew(ref _lowerLegR, _llRT, legSlew, dt);


            float step = Mathf.Pow(Mathf.Abs(sinRaw), 1.7f);
            // Keep the last bounce while the feet close. Cutting it with speed freezes the hips, then the idle sway pops.
            float bobGait = Mathf.Max(Mathf.Clamp01(Mathf.Max(walkAmt, runAmt)), _stopGait);
            float bob = grounded ? step * 0.085f * bobGait : air ? step * 0.02f : 0f;
            if (_dropVis > 0.02f && !air && !jet)
                bob = Mathf.Lerp(bob, _dropSlide ? -0.32f : -0.14f, (_dropSlide || crouchIdleExit || crouchWalkExit || crouchStandSprint) ? hipDrop : _dropVis);
            else if (jet) bob = 0.05f + Mathf.Sin(Time.time * 6.5f) * 0.02f;
            if (_landSquash > 0f) bob -= 0.14f * _landSquash;
            if (dashing) bob += 0.04f * dashAmt;
            if (flinchAmt > 0.04f) bob -= 0.1f * flinchAmt;
            transform.localPosition = _root0 + new Vector3(0f, bob, 0f);
            float squash = 1f - 0.14f * _landSquash;
            // Air-dash: strong stretch then brief squash; tag flinch compresses
            float dashStretch = airDashing ? 0.32f : 0.18f;
            float dashSquash = airDashing ? 0.16f : 0.1f;
            float stretchY = 1f + dashStretch * dashAmt - 0.16f * flinchAmt + 0.06f * claimAmt;
            float stretchXZ = 1f - dashSquash * dashAmt + 0.12f * flinchAmt;
            transform.localScale = new Vector3(stretchXZ / squash, squash * stretchY, stretchXZ / squash);
        }

        /// <summary>
        /// Negative pitch is forward on the hanging arm. Rearward recovery stays short
        /// so the hands do not fold back into the hips.
        /// </summary>
        static float RunArmPitch(float phase, float amp)
        {
            float fwd = Mathf.Max(0f, phase) * amp;
            float back = Mathf.Max(0f, -phase) * amp * 0.22f;
            return -(fwd - back);
        }

        void TickAirDashTell(float dt)
        {
            bool lunging = _motor != null && _motor.IsLunging;
            bool airDashing = _motor != null && _motor.IsAirDashing;
            bool jet = _motor != null && (_motor.State == MoveState.Jet || _motor.Jetting);
            if (lunging && !_wasLunging) _dashPulse = 1f;
            if (airDashing && !_wasAirDashing)
            {
                _dashPulse = 1f;
                _dashTrailT = 0.33f; // slightly longer air-dash ribbon read
                EnsureDashTrail();
            }
            if (jet && !_wasJetting) _dashPulse = Mathf.Max(_dashPulse, 0.85f);
            _wasLunging = lunging;
            _wasAirDashing = airDashing;
            _wasJetting = jet;
            _dashPulse = Mathf.MoveTowards(_dashPulse, 0f, dt / 0.18f);
            if (_dashTrailT > 0f) _dashTrailT = Mathf.MoveTowards(_dashTrailT, 0f, dt);
            if (_dashTrail != null)
                _dashTrail.emitting = _dashTrailT > 0.01f || airDashing;
        }

        void EnsureDashTrail()
        {
            if (_dashTrail != null) return;
            var go = new GameObject("AirDashTrail");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.9f, -0.15f);
            _dashTrail = go.AddComponent<TrailRenderer>();
            _dashTrail.time = 0.33f;
            _dashTrail.minVertexDistance = 0.04f;
            _dashTrail.widthMultiplier = 0.38f; // slightly wider so air-dash ribbon reads in TP
            _dashTrail.emitting = false;
            _dashTrail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _dashTrail.receiveShadows = false;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.55f, 0.95f, 1f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.55f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            _dashTrail.colorGradient = grad;
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader);
            var c = new Color(0.45f, 0.9f, 1f, 0.9f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            _dashTrail.sharedMaterial = mat;
        }

        /// <summary>Called while the dummy is committed to a punch but has not swung yet.</summary>
        public void HoldPunchTelegraph()
        {
            _punchTelegraph = 0.2f;
        }

        public void CancelPunchTelegraph()
        {
            _punchTelegraph = 0f;
        }

        /// <summary>Tagged runner guard. The new It uses <see cref="PlayItClaim"/>.</summary>
        public void PlayTagFlinch()
        {
            _tagFlinch = 1f;
        }

        /// <summary>New It raises both arms. Separate from the tagged runner's guard.</summary>
        public void PlayItClaim()
        {
            _itClaim = 1f;
        }

        void HookBounce()
        {
            if (_motor == _bounceHooked) return;
            if (_bounceHooked != null)
            {
                _bounceHooked.OnWallBounced -= HandleWallBounced;
                _bounceHooked.OnSuperGlide -= HandleSuperGlide;
                _bounceHooked.OnAirDashed -= HandleAirDashed;
            }
            _bounceHooked = _motor;
            if (_bounceHooked != null)
            {
                _bounceHooked.OnWallBounced += HandleWallBounced;
                _bounceHooked.OnSuperGlide += HandleSuperGlide;
                _bounceHooked.OnAirDashed += HandleAirDashed;
            }
        }

        void HandleAirDashed()
        {
            _dashPulse = 1f;
            _dashTrailT = 0.22f;
            EnsureDashTrail();
            if (_dashTrail != null) _dashTrail.emitting = true;
        }

        void HandleWallBounced()
        {
            _bouncePulse = 1f;
            _bounceWallLeft = _motor != null && _motor.WallLeft;
            // The climb eases into the push. A wall run eases into its own push.
            // Exit time is unchanged. Jump height is unchanged.
            if (!_exitFromWall && _wallExit > 0.5f)
            {
                _jumpFromClimb = true;
                _pushOff = 1f;
                _pushLeft = !_exitLeadLeft;
            }
            else if (_exitFromWall && _wallExit > 0.5f)
            {
                // The wall run eases into the push. A climb keeps its own leave.
                // Exit time is unchanged. Jump height is unchanged.
                _jumpFromWall = true;
                _pushOff = 1f;
                _pushLeft = !_exitLeadLeft;
            }
        }

        void HandleSuperGlide()
        {
            _glidePulse = 1f;
        }

        void OnDisable()
        {
            if (_bounceHooked != null)
            {
                _bounceHooked.OnWallBounced -= HandleWallBounced;
                _bounceHooked.OnSuperGlide -= HandleSuperGlide;
                _bounceHooked.OnAirDashed -= HandleAirDashed;
                _bounceHooked = null;
            }
        }

        static void Slew(ref Transform t, Quaternion target, float speed, float dt)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Slerp(t.localRotation, target, 1f - Mathf.Exp(-speed * dt));
        }

        void Cache(Transform root)
        {
            if (root == null) return;
            _hips = FindBone(root, "Hips", "Pelvis", "mixamorig:Hips", "hip");
            _spine = FindBone(root, "Spine", "Torso", "Spine1", "mixamorig:Spine", "Chest");
            _head = FindBone(root, "Head", "mixamorig:Head", "head");
            _upperArmL = FindBone(root, "UpperArm_L", "UpperArm.L", "LeftArm", "LeftUpperArm", "mixamorig:LeftArm", "Arm_L", "upperarm_l", "Upper_Arm_L");
            _upperArmR = FindBone(root, "UpperArm_R", "UpperArm.R", "RightArm", "RightUpperArm", "mixamorig:RightArm", "Arm_R", "upperarm_r", "Upper_Arm_R");
            _lowerArmL = FindBone(root, "LowerArm_L", "LowerArm.L", "LeftForeArm", "LeftLowerArm", "mixamorig:LeftForeArm", "ForeArm_L", "lowerarm_l", "Lower_Arm_L");
            _lowerArmR = FindBone(root, "LowerArm_R", "LowerArm.R", "RightForeArm", "RightLowerArm", "mixamorig:RightForeArm", "ForeArm_R", "lowerarm_r", "Lower_Arm_R");
            _upperLegL = FindBone(root, "UpperLeg_L", "UpperLeg.L", "LeftUpLeg", "LeftUpperLeg", "mixamorig:LeftUpLeg", "Thigh_L", "upperleg_l", "Upper_Leg_L");
            _upperLegR = FindBone(root, "UpperLeg_R", "UpperLeg.R", "RightUpLeg", "RightUpperLeg", "mixamorig:RightUpLeg", "Thigh_R", "upperleg_r", "Upper_Leg_R");
            _lowerLegL = FindBone(root, "LowerLeg_L", "LowerLeg.L", "LeftLeg", "LeftLowerLeg", "mixamorig:LeftLeg", "Calf_L", "lowerleg_l", "Lower_Leg_L");
            _lowerLegR = FindBone(root, "LowerLeg_R", "LowerLeg.R", "RightLeg", "RightLowerLeg", "mixamorig:RightLeg", "Calf_R", "lowerleg_r", "Lower_Leg_R");
            _bound = _upperArmL != null || _upperLegL != null || _spine != null;
            if (!_bound) return;
            if (_hips) _hips0 = _hips.localRotation;
            if (_spine) _spine0 = _spine.localRotation;
            if (_head) _head0 = _head.localRotation;
            if (_upperArmL) _uaL0 = _upperArmL.localRotation;
            if (_upperArmR) _uaR0 = _upperArmR.localRotation;
            if (_lowerArmL) _laL0 = _lowerArmL.localRotation;
            if (_lowerArmR) _laR0 = _lowerArmR.localRotation;
            if (_upperLegL) _ulL0 = _upperLegL.localRotation;
            if (_upperLegR) _ulR0 = _upperLegR.localRotation;
            if (_lowerLegL) _llL0 = _lowerLegL.localRotation;
            if (_lowerLegR) _llR0 = _lowerLegR.localRotation;
            _spineT = _spine0; _hipsT = _hips0; _headT = _head0;
            _uaLT = _uaL0; _uaRT = _uaR0; _laLT = _laL0; _laRT = _laR0;
            _ulLT = _ulL0; _ulRT = _ulR0; _llLT = _llL0; _llRT = _llR0;
        }

        static Transform FindBone(Transform root, params string[] names)
        {
            if (root == null || names == null || names.Length == 0) return null;
            var all = root.GetComponentsInChildren<Transform>(true);

            // Exact match (any alias)
            for (int n = 0; n < names.Length; n++)
            {
                string want = names[n];
                if (string.IsNullOrEmpty(want)) continue;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].name == want) return all[i];
                }
            }

            // Case-insensitive exact
            for (int n = 0; n < names.Length; n++)
            {
                string want = names[n];
                if (string.IsNullOrEmpty(want)) continue;
                for (int i = 0; i < all.Length; i++)
                {
                    if (string.Equals(all[i].name, want, System.StringComparison.OrdinalIgnoreCase))
                        return all[i];
                }
            }

            // Fuzzy: strip common prefixes, then contains token
            for (int n = 0; n < names.Length; n++)
            {
                string token = NormalizeBoneToken(names[n]);
                if (token.Length < 3) continue;
                Transform best = null;
                int bestScore = int.MaxValue;
                for (int i = 0; i < all.Length; i++)
                {
                    string tn = NormalizeBoneToken(all[i].name);
                    if (tn == token) return all[i];
                    if (tn.Contains(token))
                    {
                        int score = tn.Length - token.Length;
                        if (score < bestScore) { bestScore = score; best = all[i]; }
                    }
                }
                if (best != null) return best;
            }

            return null;
        }

        static string NormalizeBoneToken(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            // strip mixamorig: / Armature / spaces
            int colon = name.LastIndexOf(':');
            if (colon >= 0 && colon + 1 < name.Length) name = name.Substring(colon + 1);
            name = name.Replace(" ", "").Replace(".", "").Replace("-", "");
            return name.ToLowerInvariant();
        }
    }
}
