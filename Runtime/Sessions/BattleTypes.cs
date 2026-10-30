using UGS.Battle.Factions;

namespace UGS.Battle.Sessions
{
    /// <summary>战斗阶段。合法转换见设计文档第 10 章。</summary>
    public enum BattlePhase
    {
        Created = 0,
        Preparation = 1,
        InProgress = 2,
        Resolving = 3,
        Ended = 4,
    }

    /// <summary>推进模式。</summary>
    public enum BattleTickMode
    {
        /// <summary>每帧推进，配合表现层（场景内实时战斗）。</summary>
        Realtime = 0,

        /// <summary>按固定间隔批量推进（大世界后台模拟）。</summary>
        RoundBased = 1,
    }

    /// <summary>战斗结束原因。</summary>
    public enum BattleEndReason
    {
        Victory = 0,
        Draw = 1,
        Timeout = 2,
        Aborted = 3,
        AllDefeated = 4,
    }

    /// <summary>
    /// 战斗规则。所有玩法相关开关集中在此，便于按场景切换。
    /// </summary>
    public sealed class BattleRules
    {
        public BattleTickMode TickMode = BattleTickMode.Realtime;

        /// <summary>RoundBased 每回合推进时长（秒）。</summary>
        public float RoundInterval = 1f;

        /// <summary>战术决策间隔（秒）。</summary>
        public float TacticInterval = 0.25f;

        /// <summary>战斗最长时长（秒）；&lt;=0 表示不限。</summary>
        public float MaxDuration;

        /// <summary>未显式配置时的默认关系。</summary>
        public FactionRelation DefaultRelation = FactionRelation.Hostile;

        /// <summary>单次伤害下限（命中后）。</summary>
        public float MinDamage = 1f;

        /// <summary>单次伤害上限；&lt;=0 表示不限。</summary>
        public float MaxDamage;

        /// <summary>随机种子；0 表示按会话 Id 派生。</summary>
        public ulong RandomSeed;

        public IVictoryCondition VictoryCondition = new LastFactionStandingCondition();

        /// <summary>空间索引（可选）。未设置时查询退化为线性扫描。</summary>
        public Core.ISpatialIndex SpatialIndex;

        /// <summary>是否注册包内默认伤害处理器。</summary>
        public bool SkipDefaultProcessors;
    }
}
