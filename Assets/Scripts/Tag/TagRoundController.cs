using Tag.Modes;

namespace Tag.Gameplay
{
    /// <summary>
    /// Back-compat scene / GameFlow type. Logic lives in <see cref="TagModeController"/>.
    /// Scene GUID kept so Play.unity Systems component continues to bind.
    /// </summary>
    public class TagRoundController : TagModeController
    {
    }
}
