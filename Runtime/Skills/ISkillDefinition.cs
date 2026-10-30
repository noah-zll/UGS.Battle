using System.Collections.Generic;
using UGS.Battle.Units;

namespace UGS.Battle.Skills
{
    /// <summary>
    /// 技能定义（只读契约）。配置表的一行就满足它：一个 <c>configId</c> 一个实例，被所有单位共享。
    /// </summary>
    /// <remarks>
    /// 只有 getter 与只读集合：包内无法在运行时改写配置（冷却、消耗、标签都来自表）。
    /// 实例相关的数值（当前冷却、充能、开关）属于 <see cref="ISkillState"/>。
    /// 动作 / 选择器必须是**无状态**的：施法者、目标、效果组一律从 <see cref="SkillCastContext"/> 取。
    /// </remarks>
    public interface ISkillDefinition
    {
        int Id { get; }

        SkillTag Tags { get; }

        /// <summary>冷却（秒，未计冷却缩减）；缩减由 <see cref="ISkillState.CooldownMultiplier"/> 提供。</summary>
        float Cooldown { get; }

        SkillCooldownMode CooldownMode { get; }

        int MaxCharges { get; }

        /// <summary>资源消耗（任意资源型属性）。</summary>
        IReadOnlyList<ResourceCost> Costs { get; }

        /// <summary>吟唱时间（秒）；仅作为数据，由表现层解释。</summary>
        float CastTime { get; }

        /// <summary>施法距离；&lt;=0 表示不校验。</summary>
        float Range { get; }

        ITargetSelector Selector { get; }

        IReadOnlyList<ISkillAction> Actions { get; }

        IReadOnlyList<ISkillTrigger> Triggers { get; }
    }

    /// <summary><see cref="ISkillDefinition"/> 的便捷判定（避免接口塞入派生属性）。</summary>
    public static class SkillDefinitionExtensions
    {
        public static bool IsToggle(this ISkillDefinition definition) =>
            definition != null && (definition.Tags & SkillTag.Toggle) != 0;

        public static bool IsPassive(this ISkillDefinition definition) =>
            definition != null && (definition.Tags & SkillTag.Passive) != 0;

        public static bool HasTag(this ISkillDefinition definition, SkillTag tag) =>
            definition != null && (definition.Tags & tag) == tag;
    }
}
