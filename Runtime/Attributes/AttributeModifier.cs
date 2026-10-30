using System.Collections.Generic;
using UGS.Battle.Units;

namespace UGS.Battle.Attributes
{
    /// <summary>属性查询的附加上下文，供自定义修正器读取外部因素（等级、装备等）。</summary>
    public readonly struct AttributeQueryContext
    {
        public readonly ICombatUnit Owner;
        public readonly int AttributeId;

        /// <summary>上层自定义的额外数据（可为 null）。</summary>
        public readonly object Extra;

        public AttributeQueryContext(ICombatUnit owner, int attributeId, object extra = null)
        {
            Owner = owner;
            AttributeId = attributeId;
            Extra = extra;
        }
    }

    /// <summary>
    /// 属性修正来源。一个来源（装备 / Buff / 天赋）可同时修正多个属性。
    /// </summary>
    public interface IAttributeModifier
    {
        /// <summary>修正来源，用于按来源批量移除（如 Buff 过期）。</summary>
        object Source { get; }

        ModifierOp Op { get; }

        /// <summary>该修正影响的属性 id 集合。</summary>
        IEnumerable<int> AffectedAttributes { get; }

        /// <summary>取本修正对指定属性的值。</summary>
        float GetValue(int attributeId, in AttributeQueryContext context);
    }

    /// <summary>最常用的修正器：固定值 / 百分比，可同时作用于多个属性。</summary>
    public sealed class AttributeModifier : IAttributeModifier
    {
        private readonly int[] _targets;

        public object Source { get; }
        public ModifierOp Op { get; }
        public float Value { get; }

        public AttributeModifier(object source, ModifierOp op, float value, params int[] attributeIds)
        {
            Source = source;
            Op = op;
            Value = value;
            _targets = attributeIds ?? System.Array.Empty<int>();
        }

        public IEnumerable<int> AffectedAttributes => _targets;

        public float GetValue(int attributeId, in AttributeQueryContext context)
        {
            int[] targets = _targets;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == attributeId) return Value;
            }

            return 0f;
        }
    }
}
