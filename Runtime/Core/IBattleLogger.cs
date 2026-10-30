namespace UGS.Battle.Core
{
    /// <summary>日志抽象。包内不依赖任何具体日志实现，由上层注入。</summary>
    public interface IBattleLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }

    /// <summary>默认日志实现：不做任何输出。</summary>
    public sealed class NullBattleLogger : IBattleLogger
    {
        public static readonly NullBattleLogger Instance = new NullBattleLogger();

        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }
}
