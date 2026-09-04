using System.Collections.Generic;
using UnityEngine;
using Tag.Core;

namespace Tag.Gameplay
{
    /// <summary>
    /// Timed round (~90–120s). Score = least time-as-It. Transfers It via punch only.
    /// </summary>
    public class TagRoundController : MonoBehaviour
    {
        [SerializeField] float roundDuration = 105f;
        [SerializeField] bool autoFindPlayers = true;
        [SerializeField] List<ItController> players = new List<ItController>();

        float _remaining;
        bool _running;
        ItController _currentIt;

        public float Remaining => _remaining;
        public bool IsRunning => _running;
        public bool RoundActive => _running;
        public ItController CurrentIt => _currentIt;

        void Start()
        {
            if (autoFindPlayers)
                RefreshPlayers();
            if (FindFirstObjectByType<GameFlow>() == null)
                StartRound();
        }

        public void RefreshPlayers()
        {
            players.Clear();
            players.AddRange(FindObjectsByType<ItController>(FindObjectsSortMode.None));
        }

        public void RegisterPlayer(ItController p)
        {
            if (p != null && !players.Contains(p))
                players.Add(p);
        }

        public void StartRound()
        {
            RefreshPlayers();
            foreach (var p in players)
            {
                p.ResetScore();
                p.SetIt(false);
            }

            _remaining = roundDuration;
            _running = true;

            if (players.Count > 0)
            {
                int idx = Random.Range(0, players.Count);
                TransferIt(null, players[idx]);
            }

            Debug.Log($"[TagRound] Started — {players.Count} players, {roundDuration:0}s.");
        }

        public void Rematch() => StartRound();

        void Update()
        {
            if (!_running) return;
            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
                EndRound();
        }

        public void OnSuccessfulPunch(ItController puncher, ItController target)
        {
            if (!_running || puncher == null || target == null) return;
            if (!puncher.IsIt) return;
            if (!target.CanBeTagged) return;
            TransferIt(puncher, target);
        }

        public void TransferIt(ItController from, ItController to)
        {
            if (from != null) from.SetIt(false);
            if (to != null)
            {
                to.SetIt(true);
                _currentIt = to;
                Debug.Log($"[TagRound] It → {to.PlayerId}");
            }
        }

        void EndRound()
        {
            _running = false;
            _remaining = 0f;

            ItController winner = null;
            float best = float.MaxValue;
            foreach (var p in players)
            {
                if (p.TimeAsIt < best)
                {
                    best = p.TimeAsIt;
                    winner = p;
                }
            }

            string msg = winner != null
                ? $"Winner {winner.PlayerId} with {best:0.00}s as It"
                : "No players";
            Debug.Log($"[TagRound] END — {msg}");

            var flow = GameFlow.Instance != null ? GameFlow.Instance : FindFirstObjectByType<GameFlow>();
            if (flow != null)
                flow.OnRoundEnded();
        }

        void OnGUI()
        {
            string itName = _currentIt != null ? _currentIt.PlayerId : "-";
            GUI.Label(new Rect(20, Screen.height - 70, 520, 24),
                $"Time {_remaining:0.0}s | It: {itName} | R=Rematch when ended");
            float y = Screen.height - 50;
            foreach (var p in players)
            {
                GUI.Label(new Rect(20, y, 480, 20),
                    $"{p.PlayerId}: {p.TimeAsIt:0.00}s as It{(p.IsIt ? " *" : "")}");
                y += 18;
            }
        }
    }
}
