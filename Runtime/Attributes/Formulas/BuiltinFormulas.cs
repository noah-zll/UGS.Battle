using UnityEngine;

namespace UGS.Battle.Attributes.Formulas
{
    /// <summary>线性公式：value * k + b。</summary>
    public sealed class LinearAttributeFormula : IAttributeFormula
    {
        private readonly float _k;
        private readonly float _b;

        public int Order { get; }

        public LinearAttributeFormula(float k = 1f, float b = 0f, int order = 100)
        {
            _k = k;
            _b = b;
            Order = order;
        }

        public float Apply(AttributeContainer container, int attributeId, float value)
        {
            return value * _k + _b;
        }
    }

    /// <summary>钳制公式：把结果限制在 [min,max]。</summary>
    public sealed class ClampAttributeFormula : IAttributeFormula
    {
        private readonly float _min;
        private readonly float _max;

        public int Order { get; }

        public ClampAttributeFormula(float min, float max, int order = 900)
        {
            _min = min;
            _max = max;
            Order = order;
        }

        public float Apply(AttributeContainer container, int attributeId, float value)
        {
            return Mathf.Clamp(value, _min, _max);
        }
    }
}
