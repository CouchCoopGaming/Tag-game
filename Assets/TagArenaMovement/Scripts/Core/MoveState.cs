namespace TagArena.Movement
{
    /// <summary>
    /// Discrete locomotion states. The animator and camera read these.
    /// Transitions are owned by PlayerMotor — never set these from animation events
    /// except through Motor hooks (OnMantlePeak, OnLandCommit).
    /// </summary>
    public enum MoveState
    {
        Idle,
        Walk,
        Sprint,
        Crouch,
        Slide,
        Ski,
        Air,
        Jet,
        WallClimb,
        Mantle,
        WallRun,
        LandStun
    }

    /// <summary>
    /// Sole owner of player velocity. Presentation <see cref="MoveState"/> is derived from this.
    /// Illegal pairs cannot both be true.
    /// </summary>
    public enum Locomotion
    {
        Ground,
        Slide,
        Air,
        AirDash,
        Climb,
        WallRun,
        Ski,
        Vault,
        LandStun,
        Ragdoll
    }
}
