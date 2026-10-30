using System;
using System.Collections.Generic;
using UGS.Battle.Core;
using UGS.Battle.Sessions;
using UGS.Battle.Units;

namespace UGS.Battle.Factions
{
    /// <summary>
    /// 阵营系统：支持任意 N 方，关系可自由配置（敌对 / 中立 / 同盟）。
    /// 单位 -> 阵营 的映射在注册时建立，查询零分配（返回内部列表）。
    /// </summary>
    public sealed class FactionSystem
    {
        private struct Faction
        {
            public int Id;
            public string DisplayName;
            public List<ICombatUnit> Units;
        }

        private readonly BattleSession _session;
        private readonly Dictionary<int, Faction> _factions = new Dictionary<int, Faction>(8);
        private readonly Dictionary<long, FactionRelation> _relations = new Dictionary<long, FactionRelation>(16);
        private readonly Dictionary<int, int> _unitFaction = new Dictionary<int, int>(64);
        private readonly List<ICombatUnit> _allUnits = new List<ICombatUnit>(64);

        private readonly List<int> _aliveFactionIds = new List<int>(8);

        public event Action<int, ICombatUnit> UnitAdded;
        public event Action<int, ICombatUnit> UnitRemoved;

        public FactionSystem(BattleSession session)
        {
            _session = session;
        }

        /// <summary>全部已注册单位（含死亡）。用于会话级遍历。</summary>
        public IReadOnlyList<ICombatUnit> AllUnits => _allUnits;

        public IReadOnlyList<int> AliveFactionIds
        {
            get
            {
                _aliveFactionIds.Clear();
                foreach (KeyValuePair<int, Faction> pair in _factions)
                {
                    if (!IsDefeated(pair.Key)) _aliveFactionIds.Add(pair.Key);
                }

                return _aliveFactionIds;
            }
        }

        // ---------------- 阵营 ----------------

        public void CreateFaction(int factionId, string displayName = null)
        {
            if (_factions.ContainsKey(factionId)) return;

            _factions[factionId] = new Faction
            {
                Id = factionId,
                DisplayName = displayName,
                Units = new List<ICombatUnit>(16),
            };
        }

        public bool Contains(int factionId) => _factions.ContainsKey(factionId);

        public string GetDisplayName(int factionId)
        {
            return _factions.TryGetValue(factionId, out Faction faction) ? faction.DisplayName : null;
        }

        public void RemoveFaction(int factionId)
        {
            if (!_factions.TryGetValue(factionId, out Faction faction)) return;

            for (int i = faction.Units.Count - 1; i >= 0; i--) RemoveUnit(faction.Units[i]);

            _factions.Remove(factionId);
        }

        // ---------------- 单位 ----------------

        public void AddUnit(int factionId, ICombatUnit unit)
        {
            if (unit == null) return;

            if (!_factions.TryGetValue(factionId, out Faction faction))
            {
                CreateFaction(factionId);
                faction = _factions[factionId];
            }

            if (_unitFaction.TryGetValue(unit.UnitId, out int existFaction))
            {
                if (existFaction == factionId) return;

                RemoveUnit(unit);
                faction = _factions[factionId];
            }

            faction.Units.Add(unit);
            _unitFaction[unit.UnitId] = factionId;
            _allUnits.Add(unit);

            UnitAdded?.Invoke(factionId, unit);
        }

        public bool RemoveUnit(ICombatUnit unit)
        {
            if (unit == null) return false;
            if (!_unitFaction.TryGetValue(unit.UnitId, out int factionId)) return false;

            _unitFaction.Remove(unit.UnitId);
            _allUnits.Remove(unit);

            if (_factions.TryGetValue(factionId, out Faction faction))
            {
                faction.Units.Remove(unit);
            }

            UnitRemoved?.Invoke(factionId, unit);
            return true;
        }

        public int GetFactionOf(int unitId)
        {
            return _unitFaction.TryGetValue(unitId, out int factionId) ? factionId : 0;
        }

        // ---------------- 关系 ----------------

        public void SetRelation(int factionA, int factionB, FactionRelation relation)
        {
            if (factionA == factionB) return;

            _relations[RelationKey(factionA, factionB)] = relation;
        }

        public FactionRelation GetRelation(int factionA, int factionB)
        {
            if (factionA == factionB) return FactionRelation.Same;

            if (_relations.TryGetValue(RelationKey(factionA, factionB), out FactionRelation relation))
            {
                return relation;
            }

            return _session?.Rules?.DefaultRelation ?? FactionRelation.Hostile;
        }

        public bool IsHostile(int factionA, int factionB)
        {
            return GetRelation(factionA, factionB) == FactionRelation.Hostile;
        }

        public bool IsFriendly(int factionA, int factionB)
        {
            FactionRelation relation = GetRelation(factionA, factionB);
            return relation == FactionRelation.Same || relation == FactionRelation.Allied;
        }

        // ---------------- 查询 ----------------

        /// <summary>获取阵营全部单位（含死亡）。调用方自行判断存活。</summary>
        public IReadOnlyList<ICombatUnit> GetUnits(int factionId)
        {
            return _factions.TryGetValue(factionId, out Faction faction)
                ? faction.Units
                : (IReadOnlyList<ICombatUnit>)Array.Empty<ICombatUnit>();
        }

        /// <summary>获取阵营单位（写入调用方列表，零分配）。</summary>
        public int GetUnits(int factionId, List<ICombatUnit> results, bool aliveOnly = true)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            results.Clear();
            if (!_factions.TryGetValue(factionId, out Faction faction)) return 0;

            List<ICombatUnit> units = faction.Units;
            for (int i = 0; i < units.Count; i++)
            {
                ICombatUnit unit = units[i];
                if (unit == null) continue;
                if (aliveOnly && !unit.IsAlive) continue;

                results.Add(unit);
            }

            return results.Count;
        }

        /// <summary>
        /// 获取敌对单位（写入调用方提供的列表，零分配）。
        /// </summary>
        public int GetHostileUnits(int factionId, List<ICombatUnit> results, bool aliveOnly = true)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            results.Clear();

            foreach (KeyValuePair<int, Faction> pair in _factions)
            {
                if (pair.Key == factionId) continue;
                if (GetRelation(factionId, pair.Key) != FactionRelation.Hostile) continue;

                List<ICombatUnit> units = pair.Value.Units;
                for (int i = 0; i < units.Count; i++)
                {
                    ICombatUnit unit = units[i];
                    if (unit == null) continue;
                    if (aliveOnly && !unit.IsAlive) continue;

                    results.Add(unit);
                }
            }

            return results.Count;
        }

        public bool IsDefeated(int factionId)
        {
            if (!_factions.TryGetValue(factionId, out Faction faction)) return true;

            List<ICombatUnit> units = faction.Units;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null && units[i].IsAlive) return false;
            }

            return true;
        }

        public int CountAlive(int factionId)
        {
            if (!_factions.TryGetValue(factionId, out Faction faction)) return 0;

            int count = 0;
            List<ICombatUnit> units = faction.Units;
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] != null && units[i].IsAlive) count++;
            }

            return count;
        }

        public void Clear()
        {
            _factions.Clear();
            _relations.Clear();
            _unitFaction.Clear();
            _allUnits.Clear();
            _aliveFactionIds.Clear();
        }

        private static long RelationKey(int a, int b)
        {
            int min = a < b ? a : b;
            int max = a < b ? b : a;
            return ((long)min << 32) ^ (uint)max;
        }
    }
}
