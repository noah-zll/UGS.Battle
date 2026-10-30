using System.Collections.Generic;
using UGS.Battle.Units;

namespace UGS.Battle.Skills
{
    /// <summary>技能动作。多个动作按定义顺序执行，共享 <see cref="SkillCastContext.Targets"/>。</summary>
    public interface ISkillAction
    {
        void Execute(SkillCastContext context);
    }

    /// <summary>目标选择器。通过 key 注册到 BattleServiceRegistry，配置表只存 key。</summary>
    public interface ITargetSelector
    {
        string Key { get; }

        void Select(SkillCastContext context, List<ICombatUnit> results);
    }

    /// <summary>自动触发条件（被动 / 触发技）。</summary>
    public interface ISkillTrigger
    {
        string Key { get; }

        bool ShouldTrigger(ISkillState state, ISkillDefinition definition, SkillCastContext probe);
    }
}
