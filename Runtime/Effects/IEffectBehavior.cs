using UGS.Battle.Core;
using UGS.Battle.Sessions;
using UGS.Battle.Units;

namespace UGS.Battle.Effects
{
    /// <summary>效果行为执行上下文。</summary>
    public sealed class EffectContext
    {
        public EffectInstance Instance;

        /// <summary>上层载荷（宿主一次性执行时携带施法上下文；常驻实例为 null）。</summary>
        public object Payload;

        public ICombatUnit Owner => Instance.Owner;
        public ICombatUnit Source => Instance.Source;
        public int Stacks => Instance.Stacks;
        public BattleSession Session => Instance.Owner?.Session;
        public BattleRandom Random => Session?.Random;
        public IEffectDefinition Definition => Instance.Definition;

        public static EffectContext Create(EffectInstance instance)
        {
            return new EffectContext { Instance = instance };
        }

        /// <summary>
        /// 构造一次性执行上下文（不进 <see cref="EffectContainer"/>）。
        /// 供宿主"施法时立即生效"的效果复用同一套 <see cref="IEffectBehavior"/> 实现。
        /// </summary>
        public static EffectContext CreateTransient(IEffectDefinition definition, ICombatUnit owner,
            ICombatUnit source, int stacks, object payload)
        {
            var instance = new EffectInstance(definition, owner, source) { Stacks = stacks };
            return new EffectContext { Instance = instance, Payload = payload };
        }
    }

    /// <summary>
    /// 效果行为接口。由上层注册到 <see cref="BattleServiceRegistry"/>，包内不实现任何具体玩法效果。
    /// 每个效果实例独享一份行为对象，可自由保存实例级状态。
    /// </summary>
    public interface IEffectBehavior
    {
        void OnApply(EffectContext context);

        void OnStackChanged(EffectContext context, int oldStacks, int newStacks);

        void OnTick(EffectContext context, float deltaTime);

        void OnPeriod(EffectContext context);

        void OnExpire(EffectContext context);

        void OnRemoved(EffectContext context, EffectRemoveReason reason);
    }

    /// <summary>空实现基类，减少样板代码。</summary>
    public abstract class EffectBehaviorBase : IEffectBehavior
    {
        public virtual void OnApply(EffectContext context) { }

        public virtual void OnStackChanged(EffectContext context, int oldStacks, int newStacks) { }

        public virtual void OnTick(EffectContext context, float deltaTime) { }

        public virtual void OnPeriod(EffectContext context) { }

        public virtual void OnExpire(EffectContext context) { }

        public virtual void OnRemoved(EffectContext context, EffectRemoveReason reason) { }
    }
}
