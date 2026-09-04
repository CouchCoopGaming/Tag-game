using System.Collections.Generic;
using Tag.Gameplay;
using Tag.Trail;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// All living players emit trails; trail collision eliminates.
    /// Last alive wins, or timer expiry → survivors win.
    /// </summary>
    public class TrailTagMode : ITagMode
    {
        readonly TrailTagTuning _tuning;
        bool _ended;

        public TagModeId Id => TagModeId.TrailTag;
        public TrailTagTuning Tuning => _tuning;

        public TrailTagMode(TrailTagTuning tuning)
        {
            _tuning = tuning != null ? tuning : TrailTagTuning.CreateRuntimeDefaults();
        }

        public void OnRoundStart(TagModeContext ctx)
        {
            _ended = false;
            ctx.RemainingTime = _tuning.matchTimeCap > 0f ? _tuning.matchTimeCap : 0f;

            for (int i = 0; i < ctx.Players.Count; i++)
            {
                var p = ctx.Players[i];
                if (p == null) continue;
                p.Revive();
                p.ResetScore();
                p.SetIt(false);

                var emitter = p.GetComponent<PlayerTrailEmitter>();
                if (emitter == null)
                    emitter = p.gameObject.AddComponent<PlayerTrailEmitter>();
                emitter.Configure(_tuning, p, OnTrailHit, i);
                emitter.ClearTrail();
                emitter.SetEmitting(true);
                if (_tuning.spawnEmitDelay > 0f)
                    emitter.BeginSpawnDelay(_tuning.spawnEmitDelay);
                emitter.SetItEmphasis(false, _tuning.itTrailBrightness);
            }
        }

        void OnTrailHit(ItController victim, ItController owner)
        {
            var controller = TagModeController.Instance;
            if (controller == null || !controller.RoundActive) return;
            if (victim == null || !victim.IsAlive) return;
            controller.EliminatePlayer(victim, $"trail from {owner?.PlayerId ?? "?"}");
        }

        public void Tick(TagModeContext ctx, float dt)
        {
            if (_ended) return;

            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null)
                    e.SetItEmphasis(p.IsIt && p.IsAlive, _tuning.itTrailBrightness);
            }

            if (_tuning.matchTimeCap > 0f)
            {
                ctx.RemainingTime -= dt;
                if (ctx.RemainingTime <= 0f)
                {
                    ctx.RemainingTime = 0f;
                    _ended = true;
                    StopEmitters(ctx);
                }
            }

            if (!_ended && ctx.LivingCount() <= 1)
            {
                _ended = true;
                StopEmitters(ctx);
            }
        }

        void StopEmitters(TagModeContext ctx)
        {
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null) e.SetEmitting(false);
            }
        }

        public void OnPunchTransfer(TagModeContext ctx, ItController from, ItController to) { }

        public void OnPlayerEliminated(TagModeContext ctx, ItController player)
        {
            if (player == null) return;
            var e = player.GetComponent<PlayerTrailEmitter>();
            if (e != null) e.SetEmitting(false);
            if (ctx.LivingCount() <= 1)
            {
                _ended = true;
                StopEmitters(ctx);
            }
        }

        public bool ShouldEndRound(TagModeContext ctx) => _ended;

        public IReadOnlyList<string> GetWinnerIds(TagModeContext ctx)
        {
            var winners = new List<string>();
            foreach (var p in ctx.LivingPlayers())
                winners.Add(p.PlayerId);
            return winners;
        }

        public string GetHud(TagModeContext ctx)
        {
            string it = ctx.CurrentIt != null ? ctx.CurrentIt.PlayerId : "-";
            string timer = _tuning.matchTimeCap > 0f
                ? $"Time {ctx.RemainingTime:0.0}s"
                : "No cap";
            int alive = ctx.LivingCount();
            var sb = new System.Text.StringBuilder();
            sb.Append($"TrailTag | {timer} | Alive {alive} | It: {it}\n");
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                sb.Append($"{p.PlayerId}: {(p.IsAlive ? "alive" : "OUT")}{(p.IsIt ? " *" : "")}\n");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
