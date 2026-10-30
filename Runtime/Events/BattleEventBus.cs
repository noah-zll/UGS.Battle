using System;
using System.Collections.Generic;
using UGS.Battle.Sessions;

namespace UGS.Battle.Events
{
    /// <summary>
    /// 事件总线。按级别派发：
    /// Diagnostic 默认丢弃；Normal 帧末批量（<see cref="Flush"/>）；Critical 立即。
    /// 处理器以委托快照数组派发，派发阶段零分配，且能正确处理批量事件的运行时类型。
    /// </summary>
    public sealed class BattleEventBus
    {
        private struct Subscription
        {
            public Delegate Original;
            public Action<BattleEvent> Invoker;
        }

        private readonly BattleSession _session;

        private readonly Dictionary<Type, List<Subscription>> _handlers =
            new Dictionary<Type, List<Subscription>>(16);

        private readonly Dictionary<Type, Action<BattleEvent>[]> _snapshots =
            new Dictionary<Type, Action<BattleEvent>[]>(16);

        private readonly List<BattleEvent> _pending = new List<BattleEvent>(64);

        private long _sequence;

        public BattleEventBus(BattleSession session)
        {
            _session = session;
        }

        /// <summary>是否派发诊断级事件（默认关闭，避免热路径开销）。</summary>
        public bool EnableDiagnostic { get; set; }

        public void Subscribe<T>(Action<T> handler) where T : BattleEvent
        {
            if (handler == null) return;

            Type type = typeof(T);
            if (!_handlers.TryGetValue(type, out List<Subscription> list))
            {
                list = new List<Subscription>(4);
                _handlers[type] = list;
            }

            list.Add(new Subscription
            {
                Original = handler,
                Invoker = evt => handler((T)evt),
            });

            _snapshots.Remove(type);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : BattleEvent
        {
            if (handler == null) return;

            Type type = typeof(T);
            if (!_handlers.TryGetValue(type, out List<Subscription> list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                if (!Equals(list[i].Original, handler)) continue;

                list.RemoveAt(i);
                _snapshots.Remove(type);
                return;
            }
        }

        /// <summary>发布事件。Normal 级入队等待 <see cref="Flush"/>。</summary>
        public void Publish<T>(T evt) where T : BattleEvent
        {
            if (evt == null) return;

            if (evt.Level == BattleEventLevel.Diagnostic && !EnableDiagnostic) return;

            evt.SessionId = _session != null ? _session.SessionId : 0;
            evt.Time = _session != null ? _session.Clock.Elapsed : 0f;
            evt.Sequence = ++_sequence;

            if (evt.Level == BattleEventLevel.Normal)
            {
                _pending.Add(evt);
                return;
            }

            Dispatch(evt);
        }

        /// <summary>批量派发待处理的 Normal 级事件（由 <see cref="BattleSession.Tick"/> 末尾调用）。</summary>
        public void Flush()
        {
            if (_pending.Count == 0) return;

            for (int i = 0; i < _pending.Count; i++)
            {
                Dispatch(_pending[i]);
            }

            _pending.Clear();
        }

        public void Clear()
        {
            _handlers.Clear();
            _snapshots.Clear();
            _pending.Clear();
        }

        private void Dispatch(BattleEvent evt)
        {
            Action<BattleEvent>[] snapshot = GetSnapshot(evt.GetType());
            if (snapshot == null) return;

            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke(evt);
            }
        }

        private Action<BattleEvent>[] GetSnapshot(Type type)
        {
            if (_snapshots.TryGetValue(type, out Action<BattleEvent>[] snapshot)) return snapshot;

            if (!_handlers.TryGetValue(type, out List<Subscription> list) || list.Count == 0) return null;

            snapshot = new Action<BattleEvent>[list.Count];
            for (int i = 0; i < list.Count; i++) snapshot[i] = list[i].Invoker;

            _snapshots[type] = snapshot;
            return snapshot;
        }
    }
}
