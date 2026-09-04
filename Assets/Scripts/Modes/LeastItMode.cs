using System.Collections.Generic;
using Tag.Gameplay;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>Timed round; lowest TimeAsIt wins.</summary>
    public class LeastItMode : ITagMode
    {
        readonly LeastItTuning _tuning;
        bool _ended;

        public TagModeId Id => TagModeId.LeastIt;

        public LeastItMode(LeastItTuning tuning)
        {
            _tuning = tuning != null ? tuning : LeastItTuning.CreateRuntimeDefaults();
        }

        public void OnRoundStart(TagModeContext ctx)
        {
            _ended = false;
            ctx.RemainingTime = _tuning.roundDuration;
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                p.Revive();
                p.ResetScore();
                p.SetIt(false);
            }
        }

        public void Tick(TagModeContext ctx, float dt)
        {
            if (_ended) return;
            ctx.RemainingTime -= dt;
            if (ctx.RemainingTime < 0f) ctx.RemainingTime = 0f;
        }

        public void OnPunchTransfer(TagModeContext ctx, ItController from, ItController to)
        {
            // Shared transfer handled by controller; nothing mode-specific.
        }

        public void OnPlayerEliminated(TagModeContext ctx, ItController player) { }

        public bool ShouldEndRound(TagModeContext ctx)
        {
            if (_ended) return true;
            if (ctx.RemainingTime <= 0f)
            {
                _ended = true;
                return true;
            }
            return false;
        }

        public IReadOnlyList<string> GetWinnerIds(TagModeContext ctx)
        {
            float best = float.MaxValue;
            var winners = new List<string>();
            foreach (var p in ctx.Players)
            {
                if (p == null || !p.IsAlive) continue;
                if (p.TimeAsIt < best - 0.0001f)
                {
                    best = p.TimeAsIt;
                    winners.Clear();
                    winners.Add(p.PlayerId);
                }
                else if (Mathf.Abs(p.TimeAsIt - best) <= 0.0001f)
                {
                    winners.Add(p.PlayerId);
                }
            }
            return winners;
        }

        public string GetHud(TagModeContext ctx)
        {
            string it = ctx.CurrentIt != null ? ctx.CurrentIt.PlayerId : "-";
            var sb = new System.Text.StringBuilder();
            sb.Append($"LeastIt | Time {ctx.RemainingTime:0.0}s | It: {it}\n");
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                sb.Append($"{p.PlayerId}: {p.TimeAsIt:0.00}s{(p.IsIt ? " *" : "")}\n");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
