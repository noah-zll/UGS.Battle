using System.Collections.Generic;
using UGS.Battle.Attributes;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Sessions
{
    /// <summary>单个单位的快照。</summary>
    public struct UnitSnapshot
    {
        public int UnitId;
        public int FactionId;
        public bool IsAlive;
        public Vector2 Position;
        public List<ResourceSnapshot> Resources;
    }

    /// <summary>单个资源的快照。</summary>
    public struct ResourceSnapshot
    {
        public int AttributeId;
        public float Current;
    }

    /// <summary>
    /// 战斗快照。用于后台战斗补算、进程重启续跑与回放。
    /// 仅覆盖逻辑状态（资源 / 时钟 / 随机），表现状态由上层自行处理。
    /// </summary>
    public sealed class BattleSnapshot
    {
        public int SessionId;
        public BattlePhase Phase;
        public float Elapsed;
        public ulong RandomState;
        public List<UnitSnapshot> Units = new List<UnitSnapshot>(32);

        public static BattleSnapshot Capture(BattleSession session)
        {
            var snapshot = new BattleSnapshot
            {
                SessionId = session.SessionId,
                Phase = session.Phase,
                Elapsed = session.Clock.Elapsed,
                RandomState = session.Random.State,
            };

            IReadOnlyList<ICombatUnit> units = session.Factions.AllUnits;
            IReadOnlyList<int> resourceIds = AttributeRegistry.ResourceAttributes;

            for (int i = 0; i < units.Count; i++)
            {
                ICombatUnit unit = units[i];
                if (unit == null) continue;

                var resources = new List<ResourceSnapshot>(resourceIds.Count);
                for (int r = 0; r < resourceIds.Count; r++)
                {
                    int id = resourceIds[r];
                    resources.Add(new ResourceSnapshot
                    {
                        AttributeId = id,
                        Current = unit.Resources.GetCurrent(id),
                    });
                }

                snapshot.Units.Add(new UnitSnapshot
                {
                    UnitId = unit.UnitId,
                    FactionId = unit.FactionId,
                    IsAlive = unit.IsAlive,
                    Position = unit.Position,
                    Resources = resources,
                });
            }

            return snapshot;
        }

        /// <summary>把快照恢复到会话（按 UnitId 匹配，找不到的单位忽略）。</summary>
        public void RestoreTo(BattleSession session)
        {
            if (session == null) return;

            session.Clock.Restore(Elapsed);
            session.Random.Restore(RandomState);

            IReadOnlyList<ICombatUnit> units = session.Factions.AllUnits;

            for (int i = 0; i < Units.Count; i++)
            {
                UnitSnapshot snapshot = Units[i];

                for (int u = 0; u < units.Count; u++)
                {
                    ICombatUnit unit = units[u];
                    if (unit == null || unit.UnitId != snapshot.UnitId) continue;

                    List<ResourceSnapshot> resources = snapshot.Resources;
                    if (resources != null)
                    {
                        for (int r = 0; r < resources.Count; r++)
                        {
                            unit.Resources.SetCurrent(resources[r].AttributeId, resources[r].Current);
                        }
                    }

                    break;
                }
            }
        }
    }
}
