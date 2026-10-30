using System.Collections.Generic;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Skills.TargetSelectors
{
    /// <summary>指向施法者自身。</summary>
    public sealed class SelfSelector : ITargetSelector
    {
        public string Key => "self";

        public void Select(SkillCastContext context, List<ICombatUnit> results)
        {
            results.Clear();
            if (context.Caster != null) results.Add(context.Caster);
        }
    }

    /// <summary>指向当前锁定目标。</summary>
    public sealed class CurrentTargetSelector : ITargetSelector
    {
        public string Key => "current_target";

        public void Select(SkillCastContext context, List<ICombatUnit> results)
        {
            results.Clear();
            if (context.Target != null) results.Add(context.Target);
        }
    }

    /// <summary>范围内所有敌对单位（走空间索引）。</summary>
    public sealed class AllEnemiesInRangeSelector : ITargetSelector
    {
        public string Key => "all_enemies_in_range";

        public float Radius = 5f;

        public void Select(SkillCastContext context, List<ICombatUnit> results)
        {
            results.Clear();
            if (context.Session == null || context.Caster == null) return;

            float radius = Radius > 0f ? Radius : (context.Skill != null ? context.Skill.Range : 0f);
            if (radius <= 0f) return;

            context.Session.QueryHostileInRange(context.Caster.FactionId, context.Caster.Position, radius, results);
        }
    }

    /// <summary>范围内所有友方单位（含施法者）。</summary>
    public sealed class AllAlliesInRangeSelector : ITargetSelector
    {
        public string Key => "all_allies_in_range";

        public float Radius = 5f;

        public bool IncludeSelf = true;

        public void Select(SkillCastContext context, List<ICombatUnit> results)
        {
            results.Clear();
            if (context.Session == null || context.Caster == null) return;

            float radius = Radius > 0f ? Radius : (context.Skill != null ? context.Skill.Range : 0f);
            float radiusSqr = radius * radius;

            ICombatUnit caster = context.Caster;
            Vector2 center = caster.Position;

            IReadOnlyList<ICombatUnit> units = context.Session.Factions.GetUnits(caster.FactionId);
            for (int i = 0; i < units.Count; i++)
            {
                ICombatUnit unit = units[i];
                if (unit == null || !unit.IsAlive) continue;
                if (!IncludeSelf && ReferenceEquals(unit, caster)) continue;
                if ((unit.Position - center).sqrMagnitude > radiusSqr) continue;

                results.Add(unit);
            }
        }
    }

    /// <summary>最近的敌对单位。</summary>
    public sealed class NearestEnemySelector : ITargetSelector
    {
        public string Key => "nearest_enemy";

        public float MaxRadius;

        public void Select(SkillCastContext context, List<ICombatUnit> results)
        {
            results.Clear();
            if (context.Session == null || context.Caster == null) return;

            float radius = MaxRadius > 0f ? MaxRadius : (context.Skill != null ? context.Skill.Range : float.MaxValue);
            ICombatUnit target = context.Session.FindNearestHostile(context.Caster.FactionId, context.Caster.Position, radius);

            if (target != null) results.Add(target);
        }
    }
}
