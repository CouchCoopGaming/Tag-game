namespace Tag.Art
{
    /// <summary>
    /// Visual recovery that plays as a verb ends. None is idle.
    /// Roll and RollAbsorb are the terminal landing. The motor is not a verb here.
    /// </summary>
    public enum VerbExitId
    {
        None = 0,
        WallRun,
        WallJump,
        ClimbTopOut,
        ClingDrop,
        Vault,
        Mantle,
        Slide,
        AirDash,
        Punch,
        Lunge,
        ZipDrop,
        LaunchLand,
        GrappleArrive,
        GrappleRelease,
        Stagger,
        TagBackEnd,
        SoftLand,
        Roll,
        RollAbsorb
    }
}
