namespace UGS.Battle.Factions
{
    /// <summary>阵营关系。未显式设置的关系取 <see cref="Sessions.BattleRules.DefaultRelation"/>。</summary>
    public enum FactionRelation
    {
        /// <summary>同一阵营。</summary>
        Same = 0,

        /// <summary>盟友：不可攻击，可治疗，共享敌视目标。</summary>
        Allied = 1,

        /// <summary>中立：不主动攻击（除非被攻击，由上层决定）。</summary>
        Neutral = 2,

        /// <summary>敌对。</summary>
        Hostile = 3,
    }
}
