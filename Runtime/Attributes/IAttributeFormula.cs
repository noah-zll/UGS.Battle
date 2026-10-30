namespace UGS.Battle.Attributes
{
    /// <summary>
    /// 属性后处理公式。在"基础值 + 修正器聚合"之后执行，可读取其他属性，
    /// 用于表达武器倍率、灵根加成、部位减伤等玩法特化规则。
    /// </summary>
    public interface IAttributeFormula
    {
        /// <summary>执行顺序，小者先执行。</summary>
        int Order { get; }

        /// <summary>返回处理后的值。</summary>
        float Apply(AttributeContainer container, int attributeId, float value);
    }
}
