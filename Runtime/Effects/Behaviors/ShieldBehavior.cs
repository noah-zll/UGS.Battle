using UnityEngine;

namespace UGS.Battle.Effects.Behaviors
{
    /// <summary>护盾提供者。伤害管线通过它吸收伤害。</summary>
    public interface IShieldProvider
    {
        /// <summary>剩余护盾量。</summary>
        float Remaining { get; }

        /// <summary>吸收伤害，返回实际吸收量。</summary>
        float Absorb(float amount);
    }

    /// <summary>
    /// 护盾行为：按层数提供可吸收伤害的护盾池。
    /// 参数键：<c>amount</c>（每层护盾量）。
    /// </summary>
    public sealed class ShieldBehavior : EffectBehaviorBase, IShieldProvider
    {
        private readonly float _perStack;
        private float _pool;

        public ShieldBehavior(IEffectDefinition definition)
        {
            _perStack = definition.GetParameter("amount");
        }

        public float Remaining => _pool;

        public override void OnApply(EffectContext context)
        {
            Rebuild(context);
        }

        public override void OnStackChanged(EffectContext context, int oldStacks, int newStacks)
        {
            Rebuild(context);
        }

        public float Absorb(float amount)
        {
            if (amount <= 0f || _pool <= 0f) return 0f;

            float absorbed = Mathf.Min(_pool, amount);
            _pool -= absorbed;
            return absorbed;
        }

        private void Rebuild(EffectContext context)
        {
            _pool = _perStack * context.Stacks;
        }
    }
}
