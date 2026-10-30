using System;
using System.Collections.Generic;
using UGS.Battle.Units;

namespace UGS.Battle.Skills
{
    /// <summary>技能标签。</summary>
    [Flags]
    public enum SkillTag
    {
        None = 0,
        Active = 1 << 0,
        Passive = 1 << 1,
        Toggle = 1 << 2,
        Channel = 1 << 3,
        Ultimate = 1 << 4,
    }

    /// <summary>
    /// 冷却策略：决定一次施法是否消耗充能并开始冷却。
    /// </summary>
    /// <remarks>
    /// 默认 <see cref="OnCast"/>，与最初实现一致。
    /// <list type="bullet">
    /// <item><see cref="OnCast"/>：每次成功施法都消耗一次充能并进入冷却；</item>
    /// <item><see cref="OnDeactivate"/>：开关类技能"打开"不消耗，"关闭"才消耗并进入冷却；
    /// 非开关类技能退回 <see cref="OnCast"/>；</item>
    /// <item><see cref="Manual"/>：施法时不自动消耗充能/进冷却，由宿主调用
    /// <see cref="SkillStateMachine.StartCooldown"/> 决定时机（读条完成、蓄力释放等）。</item>
    /// </list>
    /// </remarks>
    public enum SkillCooldownMode
    {
        OnCast = 0,
        OnDeactivate = 1,
        Manual = 2,
    }

    /// <summary>
    /// 包内默认的 <see cref="ISkillDefinition"/> 实现（测试、Samples、Editor 用）。
    /// 游戏侧不继承它：配置行自己实现契约，避免"配置的再一次翻译"。
    /// </summary>
    public sealed class SimpleSkillDefinition : ISkillDefinition
    {
        private static readonly IReadOnlyList<ResourceCost> NoCosts = Array.Empty<ResourceCost>();
        private static readonly IReadOnlyList<ISkillTrigger> NoTriggers = Array.Empty<ISkillTrigger>();

        public int Id { get; set; }

        /// <summary>可选的人类可读键；仅包外调试用。</summary>
        public string Key { get; set; }

        public SkillTag Tags { get; set; }

        /// <summary>冷却（秒）；实际冷却 = 本值 × 实例倍率。</summary>
        public float Cooldown { get; set; }

        public SkillCooldownMode CooldownMode { get; set; } = SkillCooldownMode.OnCast;

        public int MaxCharges { get; set; } = 1;

        public List<ResourceCost> Costs { get; set; } = new List<ResourceCost>(1);

        public float CastTime { get; set; }

        public float Range { get; set; }

        public ITargetSelector Selector { get; set; }

        public List<ISkillAction> Actions { get; set; } = new List<ISkillAction>(2);

        public List<ISkillTrigger> Triggers { get; set; } = new List<ISkillTrigger>(0);

        /// <summary>上层扩展载荷；包内不解释。</summary>
        public object Payload { get; set; }

        IReadOnlyList<ResourceCost> ISkillDefinition.Costs => Costs ?? NoCosts;

        IReadOnlyList<ISkillAction> ISkillDefinition.Actions => Actions ?? (IReadOnlyList<ISkillAction>)Array.Empty<ISkillAction>();

        IReadOnlyList<ISkillTrigger> ISkillDefinition.Triggers => Triggers ?? NoTriggers;
    }
}
