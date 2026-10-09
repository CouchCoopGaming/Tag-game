using System.Collections.Generic;
using Tag.Gameplay;
using Tag.Settings;
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
        readonly Dictionary<string, int> _roundWins = new Dictionary<string, int>();
        readonly List<string> _matchWinners = new List<string>();
        int _roundIndex = 1;
        bool _ended;
        bool _awaitingTieBreak;
        float _tieBreakTimer;
        string _pendingWinner;
        readonly HashSet<string> _tieBreakEligible = new HashSet<string>();

        public TagModeId Id => TagModeId.LeastIt;

        public LeastItMode(LeastItTuning tuning)
        {
            _tuning = tuning != null ? tuning : LeastItTuning.CreateRuntimeDefaults();
        }

        public void OnRoundStart(TagModeContext ctx)
        {
            _ended = false;
            _awaitingTieBreak = false;
            _tieBreakTimer = 0f;
            _pendingWinner = null;
            _tieBreakEligible.Clear();
            if (_roundWins.Count == 0)
            {
                _roundIndex = 1;
                _matchWinners.Clear();
                foreach (var p in ctx.Players)
                {
                    if (p == null || string.IsNullOrEmpty(p.PlayerId)) continue;
                    _roundWins[p.PlayerId] = 0;
                }
            }
            ctx.RemainingTime = _tuning.roundDuration;
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                p.Revive();
                p.ResetScore();
                p.SetIt(false);
            }
        }

        /// <summary>1-based round. Display only.</summary>
        public int RoundIndex => _roundIndex < 1 ? 1 : _roundIndex;

        /// <summary>Rounds this player has already won. A resolved round counts.</summary>
        public int RoundWins(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return 0;
            int wins;
            return _roundWins.TryGetValue(playerId, out wins) ? wins : 0;
        }

        public void Tick(TagModeContext ctx, float dt)
        {
            if (_ended) return;
            if (_awaitingTieBreak)
            {
                ClampTieBreakTimerForDummyIt(ctx);
                _tieBreakTimer -= dt;
                if (_tieBreakTimer <= 0f)
                    ResolveTieByCurrentItTime(ctx);
                return;
            }

            ctx.RemainingTime -= dt;
            if (ctx.RemainingTime > 0f) return;
            ctx.RemainingTime = 0f;
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
                FinishRound(ctx, tied[0].PlayerId);
                return;
            }

            _tieBreakEligible.Clear();
            foreach (var p in tied)
                _tieBreakEligible.Add(p.PlayerId);

            _awaitingTieBreak = true;
            _tieBreakTimer = EffectiveNextPunchTimeout(ctx);
            Debug.Log($"[LeastIt] Tie at {best:0.0}s — NextPunch tiebreak among {tied.Count} (timeout {_tieBreakTimer:0.#}s)");
        }

        float EffectiveNextPunchTimeout(TagModeContext ctx)
        {
            float full = Mathf.Max(0.1f, _tuning.nextPunchTimeoutSec);
            float dummy = Mathf.Max(0.1f, _tuning.dummyNoPunchTimeoutSec);
            bool currentItIsDummy = ctx.CurrentIt != null && ctx.CurrentIt.GetComponent<DummyPatrol>() != null;
            if (currentItIsDummy || OnlyDummyOrNoPunchFoes(ctx))
                return Mathf.Min(full, dummy);
            return full;
        }

        void ClampTieBreakTimerForDummyIt(TagModeContext ctx)
        {
            if (ctx.CurrentIt == null || ctx.CurrentIt.GetComponent<DummyPatrol>() == null)
                return;
            float dummy = Mathf.Max(0.1f, _tuning.dummyNoPunchTimeoutSec);
            if (_tieBreakTimer > dummy)
                _tieBreakTimer = dummy;
        }

        /// <summary>
        /// True when every living player is DummyPatrol or lacks PlayerInputReader
        /// (no human input present who could land the NextPunch).
        /// </summary>
        bool OnlyDummyOrNoPunchFoes(TagModeContext ctx)
        {
            bool anyLiving = false;
            foreach (var p in ctx.Players)
            {
                if (p == null || !p.IsAlive) continue;
                anyLiving = true;
                bool isDummy = p.GetComponent<DummyPatrol>() != null;
                bool hasInput = p.GetComponent<TagArena.Movement.PlayerInputReader>() != null;
                if (hasInput && !isDummy)
                    return false;
            }
            return anyLiving;
        }

        void ResolveTieByCurrentItTime(TagModeContext ctx)
        {
            // 20s no punch → least current It-time among ORIGINAL tied set (shared if still equal)
            var ranked = new List<ItController>();
            foreach (var p in RankByLeastIt(ctx))
            {
                if (_tieBreakEligible.Count == 0 || _tieBreakEligible.Contains(p.PlayerId))
                    ranked.Add(p);
            }
            if (ranked.Count == 0) { _ended = true; _awaitingTieBreak = false; return; }
            float best = RoundScore(ranked[0].TimeAsIt);
            var winners = new List<string>();
            foreach (var p in ranked)
            {
                if (Mathf.Abs(RoundScore(p.TimeAsIt) - best) <= 0.0001f)
                    winners.Add(p.PlayerId);
                else break;
            }
            _awaitingTieBreak = false;
            if (winners.Count == 1)
                _pendingWinner = winners[0];
            else
                _pendingWinner = null;
            FinishRound(ctx, winners);
            Debug.Log($"[LeastIt] NextPunch timeout — resolve by TimeAsIt among original tied ({winners.Count} winners)");
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
            // NextPunch: winner = puncher who dumps It, but only if they were in the original tied set.
            if (from != null && (_tieBreakEligible.Count == 0 || _tieBreakEligible.Contains(from.PlayerId)))
            {
                _pendingWinner = from.PlayerId;
                _awaitingTieBreak = false;
                FinishRound(ctx, from.PlayerId);
            }
        }

        public void OnPlayerEliminated(TagModeContext ctx, ItController player) { }

        void FinishRound(TagModeContext ctx, string winnerId)
        {
            var ids = new List<string>();
            if (!string.IsNullOrEmpty(winnerId)) ids.Add(winnerId);
            FinishRound(ctx, ids);
        }

        /// <summary>
        /// One resolved round. Each named winner gains a win. The match ends
        /// at the win target or the round cap. Otherwise the clock starts over.
        /// </summary>
        void FinishRound(TagModeContext ctx, List<string> winnerIds)
        {
            if (winnerIds != null)
            {
                for (int i = 0; i < winnerIds.Count; i++)
                {
                    string id = winnerIds[i];
                    if (string.IsNullOrEmpty(id)) continue;
                    int have;
                    _roundWins.TryGetValue(id, out have);
                    _roundWins[id] = have + 1;
                }
            }
            _matchWinners.Clear();
            int need = WinsNeeded();
            foreach (var kv in _roundWins)
            {
                if (kv.Value >= need) _matchWinners.Add(kv.Key);
            }
            int cap = RoundCap();
            if (_matchWinners.Count == 0 && _roundIndex >= cap)
            {
                int best = 0;
                foreach (var kv in _roundWins)
                    if (kv.Value > best) best = kv.Value;
                if (best > 0)
                {
                    foreach (var kv in _roundWins)
                        if (kv.Value == best) _matchWinners.Add(kv.Key);
                }
            }
            if (_matchWinners.Count > 0 || _roundIndex >= cap)
            {
                _ended = true;
                _awaitingTieBreak = false;
                if (ctx.CurrentIt != null) ctx.CurrentIt.SetIt(false);
                ctx.CurrentIt = null;
                return;
            }
            NextRound(ctx);
        }

        int RoundCap()
        {
            int n = _tuning != null ? _tuning.roundCount : 1;
            if (n < 1) n = 1;
            return n;
        }

        int WinsNeeded()
        {
            int cap = RoundCap();
            int need = 1;
            GameSettings menu = GameSettings.Current;
            if (menu != null && menu.WinTarget >= 1)
                need = menu.WinTarget;
            if (need > cap) need = cap;
            if (need < 1) need = 1;
            return need;
        }

        void NextRound(TagModeContext ctx)
        {
            _ended = false;
            _awaitingTieBreak = false;
            _pendingWinner = null;
            _tieBreakEligible.Clear();
            _roundIndex++;
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                p.ResetScore();
                p.SetIt(false);
            }
            ctx.CurrentIt = null;
            ctx.RemainingTime = _tuning.roundDuration;
            PickNextIt(ctx);
            float post = ctx.MatchTuning != null ? ctx.MatchTuning.postRoundSec : 4f;
            if (ctx.EnterPostRound != null) ctx.EnterPostRound(post);
        }

        void PickNextIt(TagModeContext ctx)
        {
            var living = new List<ItController>();
            foreach (var p in ctx.Players)
            {
                if (p != null && p.IsAlive) living.Add(p);
            }
            if (living.Count == 0) return;
            ItController pick = living[Random.Range(0, living.Count)];
            foreach (var p in ctx.Players)
                if (p != null) p.SetIt(false);
            pick.SetIt(true);
            ctx.CurrentIt = pick;
        }

        public bool ShouldEndRound(TagModeContext ctx) => _ended;

        public IReadOnlyList<string> GetWinnerIds(TagModeContext ctx)
        {
            if (_ended)
                return _matchWinners;
            if (!string.IsNullOrEmpty(_pendingWinner))
                return new List<string> { _pendingWinner };

            var winners = new List<string>();
            float best = float.MaxValue;
            foreach (var p in ctx.Players)
            {
                if (p == null || !p.IsAlive) continue;
                if (_tieBreakEligible.Count > 0 && !_tieBreakEligible.Contains(p.PlayerId)) continue;
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
            sb.Append($"TAG / Least It   R{_roundIndex}/{RoundCap()}   {ctx.RemainingTime:0}s left   It: {it}{extra}\n");
            sb.Append("Least time-as-It wins the round. Punch transfers It.\n");
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                sb.Append($"{p.PlayerId}: {p.TimeAsIt:0.0}s as It  wins {RoundWins(p.PlayerId)}{(p.IsIt ? "  << IT" : "")}\n");
            }
            return sb.ToString().TrimEnd();
        }
    }
}
