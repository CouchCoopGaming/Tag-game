using System;
using System.Collections.Generic;
using Tag.Gameplay;

namespace Tag.Modes
{
    /// <summary>Shared mutable round state handed to the active ITagMode.</summary>
    public class TagModeContext
    {
        public readonly List<ItController> Players = new List<ItController>();
        public ItController CurrentIt;
        public float RemainingTime;
        public float Elapsed;
        public bool RoundRunning;
        public bool SuddenDeath;
        public MatchTuning MatchTuning;
        public Action<ItController> Eliminate;
        public Action<float> EnterPostRound;

        public IEnumerable<ItController> LivingPlayers()
        {
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p != null && p.IsAlive)
                    yield return p;
            }
        }

        public int LivingCount()
        {
            int n = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                var p = Players[i];
                if (p != null && p.IsAlive) n++;
            }
            return n;
        }
    }
}
