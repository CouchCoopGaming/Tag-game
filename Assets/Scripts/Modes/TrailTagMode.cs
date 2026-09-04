using System.Collections.Generic;
using Tag.Gameplay;
using Tag.Trail;
using UnityEngine;
using Tag.Audio;

namespace Tag.Modes
{
    /// <summary>
    /// Trail collision eliminates; last standing wins.
    /// Emitters All (default) vs ItOnly. MatchTimeCap → sudden death (self-grace halved).
    /// Punch/It stay. Dodge i-frames do NOT ignore trails.
    /// </summary>
    public class TrailTagMode : ITagMode
    {
        readonly TrailTagTuning _tuning;
        bool _ended;
        bool _suddenDeath;

        public TagModeId Id => TagModeId.TrailTag;
        public TrailTagTuning Tuning => _tuning;

        public TrailTagMode(TrailTagTuning tuning)
        {
            _tuning = tuning != null ? tuning : TrailTagTuning.CreateRuntimeDefaults();
        }

        public void OnRoundStart(TagModeContext ctx)
        {
            _ended = false;
            _suddenDeath = false;
            ctx.SuddenDeath = false;
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
                emitter.SetSuddenDeath(false);
                emitter.BeginSpawnDelay(_tuning.spawnTrailDelay);
                emitter.SetItEmphasis(false, _tuning.itTrailBrightness);
            }
            RefreshEmitterGates(ctx);
        }

        void OnTrailHit(ItController victim, ItController owner)
        {
            var controller = TagModeController.Instance;
            if (controller == null || !controller.RoundActive) return;
            if (victim == null || !victim.IsAlive) return;
            AudioCuePlayer.Ensure()?.TrailElim(victim.transform.position);
            controller.EliminatePlayer(victim, $"trail from {owner?.PlayerId ?? "?"}");
        }

        public void Tick(TagModeContext ctx, float dt)
        {
            if (_ended) return;
            RefreshEmitterGates(ctx);
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null)
                    e.SetItEmphasis(p.IsIt && p.IsAlive, _tuning.itTrailBrightness);
            }

            if (_tuning.matchTimeCap > 0f && !_suddenDeath)
            {
                ctx.RemainingTime -= dt;
                if (ctx.RemainingTime <= 0f)
                {
                    ctx.RemainingTime = 0f;
                    EnterSuddenDeath(ctx);
                }
            }

            if (!_ended && ctx.LivingCount() <= 1)
            {
                _ended = true;
                StopEmitters(ctx);
            }
        }

        void EnterSuddenDeath(TagModeContext ctx)
        {
            _suddenDeath = true;
            ctx.SuddenDeath = true;
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e != null) e.SetSuddenDeath(true);
            }
            Debug.Log("[TrailTag] Sudden death — next trail hit eliminates (self-grace halved)");
        }

        void RefreshEmitterGates(TagModeContext ctx)
        {
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                var e = p.GetComponent<PlayerTrailEmitter>();
                if (e == null) continue;
                if (!p.IsAlive) { e.SetEmitting(false); continue; }
                bool should = _tuning.emitters == TrailEmitterMode.All
                    || (_tuning.emitters == TrailEmitterMode.ItOnly && p.IsIt);
                e.SetEmitting(should);
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

        public void OnPunchTransfer(TagModeContext ctx, ItController from, ItController to)
        {
            if (_tuning.emitters != TrailEmitterMode.ItOnly) return;
            if (from != null)
            {
                var fe = from.GetComponent<PlayerTrailEmitter>();
                if (fe != null) fe.SetEmitting(false);
            }
            if (to != null)
            {
                var te = to.GetComponent<PlayerTrailEmitter>();
                if (te != null)
                {
                    te.BeginSpawnDelay(_tuning.spawnTrailDelay);
                    te.SetEmitting(true);
                }
            }
        }

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
            foreach (var p in ctx.LivingPlayers()) winners.Add(p.PlayerId);
            return winners;
        }

                public string GetHud(TagModeContext ctx)
        {
            string it = ctx.CurrentIt != null ? ctx.CurrentIt.PlayerId : "-";
            string timer = _suddenDeath ? "SUDDEN DEATH"
                : (_tuning.matchTimeCap > 0f ? $"Time {ctx.RemainingTime:0.0}s" : "No cap");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"TrailTag | {timer} | Alive {ctx.LivingCount()} | Emit:{_tuning.emitters} | It:{it}");
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                sb.AppendLine($"{p.PlayerId}: {(p.IsAlive ? "alive" : "OUT")}{(p.IsIt ? " *" : "")}");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
