using System.Collections.Generic;
using UGS.Battle.Sessions;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Skills
{
    /// <summary>施法失败原因。</summary>
    public enum SkillCastFailure
    {
        None = 0,
        OnCooldown,
        NoCharges,
        NotEnoughResource,
        OutOfRange,
        NoValidTarget,
        Silenced,
        Dead,
    }

    /// <summary>施法结果。</summary>
    public struct SkillCastResult
    {
        public bool Success;
        public SkillCastFailure Failure;
        public int TargetCount;
    }

    /// <summary>
    /// 施法上下文。由调用方创建（可池化），承载目标、基础数值与扩展载荷，
    /// 避免动作接口参数爆炸。
    /// </summary>
    public sealed class SkillCastContext
    {
        public BattleSession Session;
        public ICombatUnit Caster;
        public ICombatUnit Target;

        /// <summary>筛选后的目标集合（由 Selector 填充）。</summary>
        public readonly List<ICombatUnit> Targets = new List<ICombatUnit>(8);

        /// <summary>
        /// 候选单位集合（由上层按自身规则填充，选择器只能从其中挑）。
        /// 用于"候选来源在游戏侧、挑选规则在包内"的场景（如按 EffectRange 分组选目标）。
        /// </summary>
        public readonly List<ICombatUnit> Candidates = new List<ICombatUnit>(16);

        /// <summary>本次施法的效果半径（候选筛选用；&lt;= 0 时选择器可回退到 <see cref="ISkillDefinition.Range"/>）。</summary>
        public float Radius;

        public Vector2 TargetPosition;

        public ISkillDefinition Skill;

        /// <summary>基础数值（伤害/治疗量），由上层按自身属性算出后填入。</summary>
        public float BaseAmount;

        /// <summary>上层扩展载荷（常用来承载 <see cref="Combat.IDamagePayload"/>）。</summary>
        public object Payload;

        /// <summary>本次施法是否翻转了开关状态。</summary>
        public bool ToggledOn;

        public void Reset(BattleSession session, ICombatUnit caster, ICombatUnit target,
            ISkillDefinition skill, Vector2 targetPosition, float baseAmount = 0f, object payload = null)
        {
            Session = session;
            Caster = caster;
            Target = target;
            Skill = skill;
            TargetPosition = targetPosition;
            BaseAmount = baseAmount;
            Payload = payload;
            ToggledOn = false;
            Radius = 0f;
            Targets.Clear();
            Candidates.Clear();
        }
    }
}
