using System.Collections.Generic;
using Tag.Gameplay;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Timed score: least TimeAsIt wins. Time accrues always while It (incl ragdoll/spawn i-frames).
    /// TieBreak = NextPunch (wait for a transfer after timer to break ties).
    /// </summary>
    public class LeastItMode : ITagMode
    {
        readonly LeastItTuning _tuning;
        bool _timerDone;
        bool _ended;
        bool _awaitingTieBreak;
        string _pendingWinner;

        public TagModeId Id => TagModeId.LeastIt;

        public LeastItMode(LeastItTuning tuning)
        {
            _tuning = tuning != null ? tuning : LeastItTuning.CreateRuntimeDefaults();
        }

        public void OnRoundStart(TagModeContext ctx)
        {
            _timerDone = false;
            _ended = false;
            _awaitingTieBreak = false;
            _pendingWinner = null;
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
            if (_awaitingTieBreak) return;

            ctx.RemainingTime -= dt;
            if (ctx.RemainingTime > 0f) return;
            ctx.RemainingTime = 0f;
            _timerDone = true;
            ResolveOrTieBreak(ctx);
        }

        void ResolveOrTieBreak(TagModeContext ctx)
        {
            var ranked = RankByLeastIt(ctx);
            if (ranked.Count == 0)
            {
                _ended = true;
                return;
            }

            float best = RoundScore(ranked[0].TimeAsIt);
            var tied = new List<ItController>();
            foreach (var p in ranked)
            {
                if (Mathf.Abs(RoundScore(p.TimeAsIt) - best) <= 0.0001f)
                    tied.Add(p);
                else break;
            }

            if (tied.Count <= 1 || _tuning.tieBreak != LeastItTieBreak.NextPunch)
            {
                _pendingWinner = tied[0].PlayerId;
                _ended = true;
                return;
            }

            _awaitingTieBreak = true;
            Debug.Log($"[LeastIt] Tie at {best:0.0}s — NextPunch tiebreak among {tied.Count}");
        }

        float RoundScore(float t)
        {
            float prec = Mathf.Max(0.01f, _tuning.scorePrecision);
            return Mathf.Round(t / prec) * prec;
        }

        List<ItController> RankByLeastIt(TagModeContext ctx)
        {
            var list = new List<ItController>();
            foreach (var p in ctx.Players)
            {
                if (p != null && p.IsAlive) list.Add(p);
            }
            list.Sort((a, b) => a.TimeAsIt.CompareTo(b.TimeAsIt));
            return list;
        }

        public void OnPunchTransfer(TagModeContext ctx, ItController from, ItController to)
        {
            if (!_awaitingTieBreak || _ended) return;
            // NextPunch: the player who just became It loses the tie (got punched = was runner with tied low time?
            // Sheet: TieBreak=NextPunch — simplest readable: the transfer itself breaks the tie;
            // winner = player who successfully dumped It (puncher / from), i.e. least-It intent.
            if (from != null)
            {
                _pendingWinner = from.PlayerId;
                _awaitingTieBreak = false;
                _ended = true;
            }
        }

        public void OnPlayerEliminated(TagModeContext ctx, ItController player) { }

        public bool ShouldEndRound(TagModeContext ctx) => _ended;

        public IReadOnlyList<string> GetWinnerIds(TagModeContext ctx)
        {
            if (!string.IsNullOrEmpty(_pendingWinner))
                return new List<string> { _pendingWinner };

            var winners = new List<string>();
            float best = float.MaxValue;
            foreach (var p in ctx.Players)
            {
                if (p == null || !p.IsAlive) continue;
                float s = RoundScore(p.TimeAsIt);
                if (s < best - 0.0001f)
                {
                    best = s;
                    winners.Clear();
                    winners.Add(p.PlayerId);
                }
                else if (Mathf.Abs(s - best) <= 0.0001f)
                {
                    winners.Add(p.PlayerId);
                }
            }
            return winners;
        }

        public string GetHud(TagModeContext ctx)
        {
            string it = ctx.CurrentIt != null ? ctx.CurrentIt.PlayerId : "-";
            string extra = _awaitingTieBreak ? " | TIEBREAK: next punch" : "";
            var sb = new System.Text.StringBuilder();
            sb.Append($"LeastIt | Time {ctx.RemainingTime:0.0}s | It: {it}{extra}\n");
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                sb.Append($"{p.PlayerId}: {p.TimeAsIt:0.0}s{(p.IsIt ? " *" : "")}\n");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
