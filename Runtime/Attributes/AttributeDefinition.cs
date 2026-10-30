namespace UGS.Battle.Attributes
{
    /// <summary>属性修正的聚合方式。</summary>
    public enum ModifierOp
    {
        /// <summary>加法：与基础值直接相加。</summary>
        Add = 0,

        /// <summary>乘法：按 (1 + value) 连乘。</summary>
        Multiply = 1,

        /// <summary>覆盖：最后一个 Override 生效。</summary>
        Override = 2,
    }

    /// <summary>属性定义。包内不预设任何具体属性，全部由上层 <see cref="AttributeRegistry.Define"/> 声明。</summary>
    public sealed class AttributeDefinition
    {
        public int Id;
        public string Key;
        public float DefaultValue;
        public float Min = float.MinValue;
        public float Max = float.MaxValue;

        /// <summary>
        /// 是否为"资源型"属性：有独立的当前值，可被消耗/恢复
        /// （生命、法力、体力、怒气…均由上层自行定义）。
        /// </summary>
        public bool IsResource;

        public override string ToString() => $"{Key}({Id})";
    }
}
