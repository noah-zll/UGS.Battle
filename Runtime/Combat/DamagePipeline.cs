using System;
using System.Collections.Generic;
using UGS.Battle.Events;
using UGS.Battle.Sessions;
using UGS.Battle.Units;

namespace UGS.Battle.Combat
{
    /// <summary>
    /// 伤害管线：按 Order 升序执行处理器。
    /// **注册期插入有序**，Process 内只做 for 循环，禁止运行期排序与 LINQ。
    /// </summary>
    public sealed class DamagePipeline
    {
        private readonly List<IDamageProcessor> _processors = new List<IDamageProcessor>(8);
        private readonly Stack<DamageContext> _pool = new Stack<DamageContext>(4);

        public IReadOnlyList<IDamageProcessor> Processors => _processors;

        /// <summary>按 Order 插入到有序位置。</summary>
        public void Add(IDamageProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));

            int index = _processors.Count;
            for (int i = 0; i < _processors.Count; i++)
            {
                if (_processors[i].Order > processor.Order)
                {
                    index = i;
                    break;
                }
            }

            _processors.Insert(index, processor);
        }

        public bool Remove(IDamageProcessor processor) => _processors.Remove(processor);

        public void RemoveAll<T>() where T : IDamageProcessor
        {
            for (int i = _processors.Count - 1; i >= 0; i--)
            {
                if (_processors[i] is T) _processors.RemoveAt(i);
            }
        }

        public void Clear() => _processors.Clear();

        /// <summary>执行一次伤害结算。</summary>
        public DamageResult Process(in DamageRequest request, BattleSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            DamageContext context = _pool.Count > 0 ? _pool.Pop() : new DamageContext();
            context.Reset(session, request);

            try
            {
                ICombatUnit target = request.Target;

                // 伤害重定向（如"转移到坐骑"）：由目标自行决定是否接管
                if (target != null && target.TryRedirectDamage(request))
                {
                    context.Amount = 0f;
                    context.Blocked = true;
                    return context.ToResult();
                }

                int count = _processors.Count;
                for (int i = 0; i < count; i++)
                {
                    IDamageProcessor processor = _processors[i];
                    if (!processor.Enabled) continue;

                    processor.Process(context);
                    if (context.Halted) break;
                }

                DamageResult result = context.ToResult();
                Publish(session, request, result);
                return result;
            }
            finally
            {
                context.Session = null;
                context.Request = default;
                context.Killed = false;
                _pool.Push(context);
            }
        }

        private static void Publish(BattleSession session, in DamageRequest request, in DamageResult result)
        {
            session.Events.Publish(new DamageDealtEvent
            {
                Attacker = request.Attacker,
                Target = request.Target,
                Request = request,
                Result = result,
            });

            if (result.Killed && request.Target != null)
            {
                session.Events.Publish(new UnitDiedEvent
                {
                    Unit = request.Target,
                    Killer = request.Attacker,
                    SourceTag = request.SourceTag,
                });
            }
        }
    }
}
