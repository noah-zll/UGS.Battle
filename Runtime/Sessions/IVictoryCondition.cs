using System.Collections.Generic;

namespace UGS.Battle.Sessions
{
    /// <summary>胜利条件。由上层实现（全灭、占点、护送、限时…）。</summary>
    public interface IVictoryCondition
    {
        /// <summary>
        /// 评估胜负。
        /// </summary>
        /// <param name="session">战斗会话。</param>
        /// <param name="winnerFactionId">胜方阵营；-1 表示平局/无胜方。</param>
        /// <param name="reason">结束原因。</param>
        /// <returns>战斗是否应结束。</returns>
        bool Evaluate(BattleSession session, out int winnerFactionId, out BattleEndReason reason);
    }

    /// <summary>默认条件：仅剩一个存活阵营（或全部阵亡判平局）。</summary>
    public sealed class LastFactionStandingCondition : IVictoryCondition
    {
        private readonly List<int> _aliveBuffer = new List<int>(8);

        public bool Evaluate(BattleSession session, out int winnerFactionId, out BattleEndReason reason)
        {
            winnerFactionId = -1;
            reason = BattleEndReason.Victory;

            Factions.FactionSystem factions = session.Factions;
            _aliveBuffer.Clear();

            foreach (int factionId in factions.AliveFactionIds)
            {
                _aliveBuffer.Add(factionId);
            }

            if (_aliveBuffer.Count > 1) return false;

            if (_aliveBuffer.Count == 1)
            {
                winnerFactionId = _aliveBuffer[0];
                reason = BattleEndReason.Victory;
            }
            else
            {
                reason = BattleEndReason.AllDefeated;
            }

            return true;
        }
    }
}
