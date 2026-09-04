using System.Collections.Generic;
using Tag.Gameplay;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>Timed fuse (default 75s); when time hits 0, current It is eliminated / loses; others win.</summary>
    public class HotPotatoMode : ITagMode
    {
        readonly HotPotatoTuning _tuning;
        bool _ended;
        ItController _loser;

        public TagModeId Id => TagModeId.HotPotato;

        public HotPotatoMode(HotPotatoTuning tuning)
        {
            _tuning = tuning != null ? tuning : HotPotatoTuning.CreateRuntimeDefaults();
        }

        public void OnRoundStart(TagModeContext ctx)
        {
            _ended = false;
            _loser = null;
            int n = Mathf.Max(2, ctx.Players.Count);
            ctx.RemainingTime = _tuning.DurationForPlayerCount(n);
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
            if (ctx.RemainingTime > 0f) return;
            ctx.RemainingTime = 0f;
            _ended = true;
            var it = ctx.CurrentIt;
            if (it != null && it.IsAlive)
            {
                _loser = it;
                ctx.Eliminate?.Invoke(it);
            }
        }

        public void OnPunchTransfer(TagModeContext ctx, ItController from, ItController to) { }

        public void OnPlayerEliminated(TagModeContext ctx, ItController player)
        {
            if (_loser == null) _loser = player;
        }

        public bool ShouldEndRound(TagModeContext ctx) => _ended;

        public IReadOnlyList<string> GetWinnerIds(TagModeContext ctx)
        {
            var winners = new List<string>();
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                if (_loser != null && p == _loser) continue;
                winners.Add(p.PlayerId);
            }
            return winners;
        }

        public string GetHud(TagModeContext ctx)
        {
            string it = ctx.CurrentIt != null ? ctx.CurrentIt.PlayerId : "-";
            string warn = ctx.RemainingTime <= _tuning.warnSec ? " !!FUSE!!" : "";
            return $"HotPotato | Fuse {ctx.RemainingTime:0.0}s{warn} | It: {it}";
        }
    }
}
