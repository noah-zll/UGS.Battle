using System;

namespace UGS.Battle.Core
{
    /// <summary>战斗框架内部异常。仅用于不可恢复的配置/使用错误。</summary>
    public sealed class BattleException : Exception
    {
        public BattleException(string message) : base(message) { }

        public BattleException(string message, Exception inner) : base(message, inner) { }
    }
}
