using System;
using System.Collections.Generic;

namespace UGS.Battle.Core
{
    /// <summary>
    /// 临时集合复用池。热路径禁止 new List，统一从这里租借并归还。
    /// </summary>
    public sealed class BattleBufferPool
    {
        private const int MaxRetainedPerType = 8;

        private readonly Dictionary<Type, Stack<object>> _pools = new Dictionary<Type, Stack<object>>();

        public List<T> RentList<T>()
        {
            if (_pools.TryGetValue(typeof(List<T>), out var stack) && stack.Count > 0)
            {
                return (List<T>)stack.Pop();
            }

            return new List<T>(16);
        }

        public void ReturnList<T>(List<T> list)
        {
            if (list == null) return;

            list.Clear();
            if (!_pools.TryGetValue(typeof(List<T>), out var stack))
            {
                stack = new Stack<object>();
                _pools[typeof(List<T>)] = stack;
            }

            if (stack.Count < MaxRetainedPerType) stack.Push(list);
        }

        public void Clear()
        {
            _pools.Clear();
        }
    }
}
