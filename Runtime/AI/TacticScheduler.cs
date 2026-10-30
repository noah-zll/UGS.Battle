using System;
using System.Collections.Generic;
using UGS.Battle.Sessions;

namespace UGS.Battle.AI
{
    /// <summary>
    /// 战术调度器。Register 时按 Priority 降序排序，Tick 内不再排序。
    /// </summary>
    public sealed class TacticScheduler
    {
        private sealed class Output : ITacticOutput
        {
            private List<UnitCommand> _results;
            private readonly HashSet<int> _claimed = new HashSet<int>();

            public void Begin(List<UnitCommand> results)
            {
                _results = results;
                _claimed.Clear();
            }

            public void Issue(UnitCommand command)
            {
                if (command == null) return;
                if (!_claimed.Add(command.UnitId)) return;

                _results.Add(command);
            }

            public bool IsClaimed(int unitId) => _claimed.Contains(unitId);

            public void End() => _results = null;
        }

        private readonly List<ITactic> _tactics = new List<ITactic>(8);
        private readonly Output _output = new Output();

        public IReadOnlyList<ITactic> Tactics => _tactics;

        public void Register(ITactic tactic)
        {
            if (tactic == null) throw new ArgumentNullException(nameof(tactic));
            if (_tactics.Contains(tactic)) return;

            int index = _tactics.Count;
            for (int i = 0; i < _tactics.Count; i++)
            {
                if (_tactics[i].Priority < tactic.Priority)
                {
                    index = i;
                    break;
                }
            }

            _tactics.Insert(index, tactic);
        }

        public bool Unregister(ITactic tactic) => _tactics.Remove(tactic);

        public bool UnregisterFaction(int factionId)
        {
            bool removed = false;
            for (int i = _tactics.Count - 1; i >= 0; i--)
            {
                if (_tactics[i].FactionId != factionId) continue;

                _tactics.RemoveAt(i);
                removed = true;
            }

            return removed;
        }

        public void Clear() => _tactics.Clear();

        public void Tick(BattleSession session, float deltaTime, List<UnitCommand> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            _output.Begin(results);
            try
            {
                for (int i = 0; i < _tactics.Count; i++)
                {
                    ITactic tactic = _tactics[i];
                    if (tactic == null) continue;

                    tactic.Execute(session, deltaTime, _output);
                }
            }
            finally
            {
                _output.End();
            }
        }
    }
}
