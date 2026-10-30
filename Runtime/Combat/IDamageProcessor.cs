namespace UGS.Battle.Combat
{
    /// <summary>伤害管线的单个处理器。Order 小者先执行。</summary>
    public interface IDamageProcessor
    {
        int Order { get; }

        bool Enabled { get; }

        string Name { get; }

        void Process(DamageContext context);
    }
}
