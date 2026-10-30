using UGS.Battle.Attributes;

namespace UGS.Battle.Effects.Behaviors
{
    /// <summary>属性修正行为的配置载荷。</summary>
    public sealed class AttributeModifierPayload
    {
        public ModifierOp Op = ModifierOp.Add;

        /// <summary>被修正的属性 id。</summary>
        public int[] AttributeIds;

        /// <summary>每层数值（与 <see cref="AttributeIds"/> 一一对应）。</summary>
        public float[] ValuesPerStack;
    }

    /// <summary>
    /// 属性修正行为：按层数把 <see cref="AttributeModifier"/> 挂到宿主属性容器上，移除时自动摘除。
    /// </summary>
    public sealed class AttributeModifierBehavior : EffectBehaviorBase
    {
        private readonly AttributeModifierPayload _payload;
        private AttributeModifier[] _applied;
        private object _sourceToken;

        public AttributeModifierBehavior(IEffectDefinition definition)
        {
            _payload = definition.Payload as AttributeModifierPayload;
        }

        public override void OnApply(EffectContext context)
        {
            _sourceToken = context.Instance;
            Rebuild(context);
        }

        public override void OnStackChanged(EffectContext context, int oldStacks, int newStacks)
        {
            Rebuild(context);
        }

        public override void OnRemoved(EffectContext context, EffectRemoveReason reason)
        {
            Detach(context);
        }

        private void Rebuild(EffectContext context)
        {
            Detach(context);

            if (_payload?.AttributeIds == null || _payload.ValuesPerStack == null) return;

            int count = System.Math.Min(_payload.AttributeIds.Length, _payload.ValuesPerStack.Length);
            if (count <= 0) return;

            AttributeContainer attributes = context.Owner?.Attributes;
            if (attributes == null) return;

            _applied = new AttributeModifier[count];
            int stacks = context.Stacks;

            for (int i = 0; i < count; i++)
            {
                float value = _payload.ValuesPerStack[i] * stacks;
                _applied[i] = new AttributeModifier(_sourceToken, _payload.Op, value, _payload.AttributeIds[i]);
                attributes.AddModifier(_applied[i]);
            }
        }

        private void Detach(EffectContext context)
        {
            if (_applied == null) return;

            AttributeContainer attributes = context.Owner?.Attributes;
            if (attributes != null)
            {
                for (int i = 0; i < _applied.Length; i++)
                {
                    if (_applied[i] != null) attributes.RemoveModifier(_applied[i]);
                }
            }

            _applied = null;
        }
    }
}
