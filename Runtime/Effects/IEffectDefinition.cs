using System.Collections.Generic;

namespace UGS.Battle.Effects
{
    /// <summary>
    /// 效果定义（只读契约）。配置表的一行即一份，按 <c>configId</c> 共享，被所有单位复用。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="Skills.ISkillDefinition"/> 同构：只有 getter 与只读集合，
    /// 包内不能改写配置；层数、剩余时长等运行时状态在 <see cref="EffectInstance"/> 上。
    /// 需要的具体玩法数据（位移距离、召唤配置等）由行为侧把定义强转成自己的配置类型读取，
    /// 或用 <see cref="Payload"/> 携带宿主构建的派生数据。
    /// </remarks>
    public interface IEffectDefinition
    {
        int Id { get; }

        /// <summary>调试 / 日志用的可读键；不参与玩法。</summary>
        string Key { get; }

        /// <summary>持续时长（秒）；&lt;0 表示永久。</summary>
        float Duration { get; }

        /// <summary>周期触发间隔（秒）；&lt;=0 表示非周期效果。</summary>
        float Period { get; }

        int MaxStacks { get; }

        StackRule StackRule { get; }

        EffectTag Tags { get; }

        /// <summary>覆盖优先级：数值大的优先（同属性多效果时由上层自行使用）。</summary>
        int Priority { get; }

        /// <summary>行为 key 列表，由 <see cref="Core.BattleServiceRegistry"/> 解析为运行时行为实例。</summary>
        IReadOnlyList<string> BehaviorKeys { get; }

        /// <summary>类型化的扩展载荷，由上层构建、具体行为自行强转（如属性修正配置）。</summary>
        object Payload { get; }

        /// <summary>行为参数（固定值、倍率等）；表驱动的定义通常不用。</summary>
        float GetParameter(string key, float defaultValue = 0f);
    }

    /// <summary><see cref="IEffectDefinition"/> 的便捷判定（避免接口塞入派生属性）。</summary>
    public static class EffectDefinitionExtensions
    {
        public static bool IsPermanent(this IEffectDefinition definition) =>
            definition != null && definition.Duration < 0f;

        public static bool IsPeriodic(this IEffectDefinition definition) =>
            definition != null && definition.Period > 0f;

        public static bool HasTag(this IEffectDefinition definition, EffectTag tag) =>
            definition != null && (definition.Tags & tag) == tag;
    }
}
