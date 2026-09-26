using System.Collections.Generic;
using Tag.Gameplay;

namespace Tag.Modes
{
    /// <summary>
    /// Sandbox: punch still transfers It, nobody is eliminated by a timer.
    /// </summary>
    public class FreePlayMode : ITagMode
    {
        public TagModeId Id => TagModeId.FreePlay;

        public void OnRoundStart(TagModeContext ctx)
        {
            ctx.RemainingTime = 0f;
            ctx.SuddenDeath = false;
        }

        public void Tick(TagModeContext ctx, float dt) { }

        public void OnPunchTransfer(TagModeContext ctx, ItController from, ItController to) { }

        public void OnPlayerEliminated(TagModeContext ctx, ItController player) { }

        public bool ShouldEndRound(TagModeContext ctx) => false;

        public IReadOnlyList<string> GetWinnerIds(TagModeContext ctx) => new List<string>();

        public string GetHud(TagModeContext ctx)
        {
            return "Free play\nPunch transfers It. No round timer.\nF1-F3 scored modes; F4 stays free.";
        }
    }
}
