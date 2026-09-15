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
}
