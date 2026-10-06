using Tag.Core;
using Tag.Local;
using Tag.Modes;
using Tag.Practice;
using Tag.Settings;
using UnityEngine;

namespace Tag.Front
{
    /// <summary>
    /// Applies the saved setup to the live match and totals RoundFlow-style
    /// fields across rounds. The mode still decides when a round ends.
    /// </summary>
    public static class FrontLive
    {
        const int Cap = 8;

        static readonly string[] Ids = new string[Cap];
        static readonly float[] TimeAsIt = new float[Cap];
        static readonly int[] Tags = new int[Cap];
        static readonly System.Text.StringBuilder Sb = new System.Text.StringBuilder(256);

        static int _n;
        static int _rounds = 1;
        static int _played;
        static float _longest;
        static bool _continue;
        static string _winner = "";
        static string _card = "";

        public static string Card()
        {
            return _card ?? "";
        }

        public static void Reset()
        {
            _continue = false;
            _rounds = 1;
            Clear();
        }

        public static void SetMatch(int rounds)
        {
            if (rounds < GameSettings.RoundsMin) rounds = GameSettings.RoundsMin;
            if (rounds > GameSettings.RoundsMax) rounds = GameSettings.RoundsMax;
            _rounds = rounds;
        }

        public static void Apply()
        {
            GameSettings s = GameSettings.Current ?? GameSettings.Defaults();
            GameSettings.Current = s;
            s.Clamp();
            int opponents = s.AiOpponents;
            bool passive = false;
            if (PracticeSession.Active)
            {
                opponents = PracticeSession.AiCount;
                passive = PracticeSession.Dummy;
            }
            if (!LocalPlayerRoster.IsCouch)
            {
                LocalPlayerSpawner spawn = Object.FindFirstObjectByType<LocalPlayerSpawner>();
                if (spawn != null) spawn.ApplyOpponents(opponents);
            }
            float diff = s.DifficultyValue();
            DummyPatrol[] patrols = Object.FindObjectsByType<DummyPatrol>(FindObjectsSortMode.None);
            for (int i = 0; i < patrols.Length; i++)
            {
                if (patrols[i] == null) continue;
                patrols[i].Passive = passive;
                patrols[i].ApplyDifficulty(diff);
            }
            TagModeController mode = TagModeController.Instance;
            if (mode != null) mode.ApplyRound(s.RoundSeconds(), s.RoundsPerMatch);
            SetMatch(s.RoundsPerMatch);
        }

        public static void OnRoundStarted()
        {
            if (_continue)
            {
                _continue = false;
                return;
            }
            Clear();
        }

        public static bool KeepGoing(string[] ids, float[] times, int[] tags, int count, float longest)
        {
            if (count < 0) count = 0;
            if (count > Cap) count = Cap;
            for (int i = 0; i < count; i++)
            {
                string id = "Player";
                if (ids != null && i < ids.Length && !string.IsNullOrEmpty(ids[i]))
                    id = ids[i];
                int slot = Slot(id);
                if (times != null && i < times.Length) TimeAsIt[slot] += times[i];
                if (tags != null && i < tags.Length) Tags[slot] += tags[i];
            }
            if (longest > _longest) _longest = longest;
            _played++;
            if (_played < _rounds)
            {
                _continue = true;
                return true;
            }
            Pick();
            Build();
            return false;
        }

        public static void ReleaseScene()
        {
            Retire(LocalPlayerSpawner.OpponentPawnName + "_2");
            Retire(LocalPlayerSpawner.OpponentPawnName + "_3");
        }

        static void Clear()
        {
            _played = 0;
            _n = 0;
            _longest = 0f;
            _winner = "";
            _card = "";
            for (int i = 0; i < Cap; i++)
            {
                Ids[i] = null;
                TimeAsIt[i] = 0f;
                Tags[i] = 0;
            }
        }

        static int Slot(string id)
        {
            for (int i = 0; i < _n; i++)
            {
                if (Ids[i] == id) return i;
            }
            if (_n >= Cap) return Cap - 1;
            Ids[_n] = id;
            _n++;
            return _n - 1;
        }

        static void Pick()
        {
            if (_n <= 0)
            {
                _winner = "nobody";
                return;
            }
            float best = TimeAsIt[0];
            int wins = 1;
            for (int i = 1; i < _n; i++)
            {
                if (TimeAsIt[i] < best - 0.0001f)
                {
                    best = TimeAsIt[i];
                    wins = 1;
                }
                else if (TimeAsIt[i] <= best + 0.0001f)
                    wins++;
            }
            if (wins != 1)
            {
                _winner = "tie";
                return;
            }
            for (int i = 0; i < _n; i++)
            {
                if (TimeAsIt[i] <= best + 0.0001f)
                {
                    _winner = Ids[i];
                    return;
                }
            }
            _winner = Ids[0];
        }

        static void Build()
        {
            Sb.Clear();
            Sb.Append("Least time as It wins");
            for (int i = 0; i < _n; i++)
            {
                Sb.Append('\n');
                Sb.Append(Ids[i]);
                Sb.Append("  ");
                Sb.Append(HudDigits.Tenth0(TimeAsIt[i]));
                Sb.Append("s as It  tags ");
                Sb.Append(HudDigits.Whole0(Tags[i]));
            }
            Sb.Append("\nLongest survival  ");
            Sb.Append(HudDigits.Tenth0(_longest));
            Sb.Append("s\nWinner  ");
            Sb.Append(_winner);
            Sb.Append("\n\n1-3 or Left / Right    Enter or South\nR rematch");
            _card = Sb.ToString();
        }

        static void Retire(string name)
        {
            GameObject go = GameObject.Find(name);
            if (go == null) return;
            go.SetActive(false);
            Object.Destroy(go);
        }
    }
}
