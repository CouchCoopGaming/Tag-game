using System.Collections.Generic;
using Tag.Gameplay;

namespace Tag.Modes
{
    /// <summary>Pluggable round rules for HotPotato / LeastIt / TrailTag.</summary>
    public interface ITagMode
    {
        TagModeId Id { get; }
        void OnRoundStart(TagModeContext ctx);
        void Tick(TagModeContext ctx, float dt);
        void OnPunchTransfer(TagModeContext ctx, ItController from, ItController to);
        void OnPlayerEliminated(TagModeContext ctx, ItController player);
        bool ShouldEndRound(TagModeContext ctx);
        IReadOnlyList<string> GetWinnerIds(TagModeContext ctx);
        string GetHud(TagModeContext ctx);
    }
}
