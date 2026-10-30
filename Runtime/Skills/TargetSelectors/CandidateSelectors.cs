using System.Collections.Generic;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Skills.TargetSelectors
{
    /// <summary>
    /// 从上层提供的候选集合中挑选全部可被选为目标的单位。
    /// </summary>
    /// <remarks>
    /// "候选从哪来"由游戏侧决定（军队图 / 本次点击 / 传入目标），本类只负责筛选规则，
    /// 因此可以表达"己方全体""敌方全体""当前目标"等同一套规则的不同候选来源。
    /// <para>
    /// 用 <see cref="ICombatUnit.CanBeTargeted"/> 而不是 <see cref="ICombatUnit.IsAlive"/>：
    /// 后者对宿主棋子还要求"在本场细节地图且有实体"，那是阵营存亡的判据，不是能否被技能选中的判据。
    /// </para>
    /// </remarks>
    public sealed class CandidatesSelector : ITargetSelector
    {
        public string Key => "candidates";

        public void Select(SkillCastContext context, List<ICombatUnit> results)
        {
            results.Clear();
            if (context == null) return;

            List<ICombatUnit> candidates = context.Candidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                ICombatUnit unit = candidates[i];
                if (unit == null || !unit.CanBeTargeted) continue;

                results.Add(unit);
            }
        }
    }

    /// <summary>
    /// 候选集合中、距施法者指定半径内的可选单位。
    /// </summary>
    public sealed class CandidatesInRangeSelector : ITargetSelector
    {
        /// <summary>半径；&lt;= 0 时回退到上下文半径，再回退到技能范围。</summary>
        public float Radius;

        public string Key => "candidates_in_range";

        public void Select(SkillCastContext context, List<ICombatUnit> results)
        {
            results.Clear();
            if (context == null || context.Caster == null) return;

            float radius = ResolveRadius(context);
            if (radius <= 0f) return;

            float radiusSqr = radius * radius;
            Vector2 center = context.Caster.Position;

            List<ICombatUnit> candidates = context.Candidates;
            for (int i = 0; i < candidates.Count; i++)
            {
                ICombatUnit unit = candidates[i];
                if (unit == null || !unit.CanBeTargeted) continue;
                if ((unit.Position - center).sqrMagnitude > radiusSqr) continue;

                results.Add(unit);
            }
        }

        private float ResolveRadius(SkillCastContext context)
        {
            if (Radius > 0f) return Radius;
            if (context.Radius > 0f) return context.Radius;

            return context.Skill != null ? context.Skill.Range : 0f;
        }
    }
}
