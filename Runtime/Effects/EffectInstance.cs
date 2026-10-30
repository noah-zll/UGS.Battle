using UGS.Battle.Units;

namespace UGS.Battle.Effects
{
    /// <summary>效果运行时实例。所有可变状态（层数、剩余时长）都在实例上，定义可共享。</summary>
    public sealed class EffectInstance
    {
        public IEffectDefinition Definition { get; }

        public ICombatUnit Owner { get; }

        public ICombatUnit Source { get; }

        /// <summary>层数。</summary>
        public int Stacks { get; internal set; }

        /// <summary>剩余时长（秒）；永久效果为 <see cref="float.PositiveInfinity"/>。</summary>
        public float Remaining { get; internal set; }

        /// <summary>周期累积计时。</summary>
        public float PeriodAccumulator { get; internal set; }

        /// <summary>绝对到期时间（供到期最小堆排序使用）。永久效果为 +∞。</summary>
        public float ExpireAt { get; internal set; }

        /// <summary>施加顺序，保证同帧处理稳定。</summary>
        public long ApplyOrder { get; internal set; }

        /// <summary>是否已被移除。</summary>
        public bool IsRemoved { get; internal set; }

        /// <summary>行为实例（每个效果实例独享，便于保存实例级状态，如护盾池）。</summary>
        public IEffectBehavior[] Behaviors { get; internal set; }

        public EffectInstance(IEffectDefinition definition, ICombatUnit owner, ICombatUnit source)
        {
            Definition = definition;
            Owner = owner;
            Source = source;
            Behaviors = System.Array.Empty<IEffectBehavior>();
        }

        public bool IsExpired => !Definition.IsPermanent() && Remaining <= 0f;

        public float Elapsed => Definition.IsPermanent() ? 0f : Definition.Duration - Remaining;

        public override string ToString()
        {
            return $"{Definition.Key} x{Stacks} ({(Definition.IsPermanent() ? "∞" : Remaining.ToString("F1"))})";
        }
    }
}
