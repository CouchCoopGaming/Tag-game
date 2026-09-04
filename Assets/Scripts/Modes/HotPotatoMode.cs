using System.Collections.Generic;
using Tag.Gameplay;
using UnityEngine;

namespace Tag.Modes
{
    /// <summary>
    /// Landon-precise Hot Potato (modes-sheet v0.1):
    /// Timer 0 → current It loses the ROUND; every other player +1 round-win.
    /// Match = first to WinsToTakeMatch=2 (MaxRounds=3). NO elimination / last-standing.
    /// Ragdoll does NOT pause timer. Punch transfer stays.
    /// </summary>
    public class HotPotatoMode : ITagMode
    {
        readonly HotPotatoTuning _tuning;
        readonly Dictionary<string, int> _roundWins = new Dictionary<string, int>();
        int _roundIndex;
        bool _matchOver;
        string _lastRoundLoserId;
        readonly List<string> _matchWinners = new List<string>();
        bool _initialized;

        public TagModeId Id => TagModeId.HotPotato;

        public HotPotatoMode(HotPotatoTuning tuning)
        {
            _tuning = tuning != null ? tuning : HotPotatoTuning.CreateRuntimeDefaults();
        }

        public void OnRoundStart(TagModeContext ctx)
        {
            if (!_initialized || _matchOver)
            {
                _initialized = true;
                _matchOver = false;
                _roundIndex = 1;
                _lastRoundLoserId = null;
                _matchWinners.Clear();
                _roundWins.Clear();
                foreach (var p in ctx.Players)
                {
                    if (p == null) continue;
                    p.Revive();
                    p.ResetScore();
                    p.SetIt(false);
                    _roundWins[p.PlayerId] = 0;
                }
            }

            ctx.EnterPostRound = sec =>
            {
                var c = TagModeController.Instance;
                if (c != null) c.EnterPostRound(sec);
            };

            int n = Mathf.Clamp(Mathf.Max(2, ctx.Players.Count), 2, 4);
            ctx.RemainingTime = _tuning.DurationForPlayerCount(n);
        }

        public void Tick(TagModeContext ctx, float dt)
        {
            if (_matchOver) return;
            ctx.RemainingTime -= dt; // ragdoll does NOT pause
            if (ctx.RemainingTime > 0f) return;
            ctx.RemainingTime = 0f;
            ResolveRoundLoss(ctx);
        }

        void ResolveRoundLoss(TagModeContext ctx)
        {
            var it = ctx.CurrentIt;
            // Fuse 0 + CurrentIt null → void round: 0 wins, new random It, refill fuse, 2s beat
            if (it == null)
            {
                _lastRoundLoserId = null;
                int nVoid = Mathf.Clamp(Mathf.Max(2, ctx.Players.Count), 2, 4);
                ctx.RemainingTime = _tuning.DurationForPlayerCount(nVoid);
                PickNewIt(ctx);
                ctx.EnterPostRound?.Invoke(2f);
                Debug.Log("[HotPotato] null-It void round — no wins awarded");
                return;
            }

            _lastRoundLoserId = it.PlayerId;

            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                if (ReferenceEquals(p, it)) continue;
                if (!_roundWins.ContainsKey(p.PlayerId)) _roundWins[p.PlayerId] = 0;
                _roundWins[p.PlayerId]++;
            }

            _matchWinners.Clear();
            foreach (var kv in _roundWins)
                if (kv.Value >= _tuning.winsToTakeMatch)
                    _matchWinners.Add(kv.Key);

            bool hitMax = _roundIndex >= _tuning.maxRounds;
            if (_matchWinners.Count > 0 || hitMax)
            {
                if (_matchWinners.Count == 0)
                {
                    int best = -1;
                    foreach (var kv in _roundWins)
                        if (kv.Value > best) best = kv.Value;
                    foreach (var kv in _roundWins)
                        if (kv.Value == best) _matchWinners.Add(kv.Key);
                }
                _matchOver = true;
                if (it != null) it.SetIt(false);
                ctx.CurrentIt = null;
                return;
            }

            if (it != null) it.SetIt(false);
            ctx.CurrentIt = null;
            float post = ctx.MatchTuning != null ? ctx.MatchTuning.postRoundSec : _tuning.postRoundSec;
            _roundIndex++;
            int n = Mathf.Clamp(Mathf.Max(2, ctx.Players.Count), 2, 4);
            ctx.RemainingTime = _tuning.DurationForPlayerCount(n);
            PickNewIt(ctx);
            ctx.EnterPostRound?.Invoke(post);
        }

        void PickNewIt(TagModeContext ctx)
        {
            var candidates = new List<ItController>();
            foreach (var p in ctx.LivingPlayers())
            {
                if (_lastRoundLoserId != null && p.PlayerId == _lastRoundLoserId && ctx.LivingCount() > 1)
                    continue;
                candidates.Add(p);
            }
            if (candidates.Count == 0)
                foreach (var p in ctx.LivingPlayers()) candidates.Add(p);
            if (candidates.Count == 0) return;
            foreach (var p in ctx.Players)
                if (p != null) p.SetIt(false);
            var pick = candidates[Random.Range(0, candidates.Count)];
            pick.SetIt(true);
            ctx.CurrentIt = pick;
        }

        public void OnPunchTransfer(TagModeContext ctx, ItController from, ItController to) { }
        public void OnPlayerEliminated(TagModeContext ctx, ItController player) { }
        public bool ShouldEndRound(TagModeContext ctx) => _matchOver;
        public IReadOnlyList<string> GetWinnerIds(TagModeContext ctx) => _matchWinners;

        public string GetHud(TagModeContext ctx)
        {
            string it = ctx.CurrentIt != null ? ctx.CurrentIt.PlayerId : "-";
            string warn = (ctx.RemainingTime > 0f && ctx.RemainingTime <= _tuning.warnSec) ? " !!WARN!!" : "";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"HotPotato R{_roundIndex}/{_tuning.maxRounds} | Fuse {ctx.RemainingTime:0.0}s{warn} | It:{it}");
            sb.Append("Wins: ");
            bool first = true;
            foreach (var p in ctx.Players)
            {
                if (p == null) continue;
                int w = _roundWins.TryGetValue(p.PlayerId, out var v) ? v : 0;
                if (!first) sb.Append("  ");
                first = false;
                sb.Append($"{p.PlayerId}:{w}");
            }
            return sb.ToString();
        }
    }
}
