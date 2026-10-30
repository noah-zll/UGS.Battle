using System;
using System.Collections.Generic;

namespace UGS.Battle.Core
{
    /// <summary>
    /// 分层频率调度器。
    /// 每帧只执行"本帧到期"的层，并通过 phaseOffset 错峰，避免多层同帧撞车。
    /// </summary>
    public sealed class BattleScheduler
    {
        private sealed class Layer
        {
            public string Key;
            public float Interval;
            public float Accumulator;
            public Action<float> Tick;
        }

        private readonly List<Layer> _layers = new List<Layer>(8);

        /// <summary>
        /// 注册一个调度层。
        /// </summary>
        /// <param name="key">唯一键，重复注册会被替换。</param>
        /// <param name="interval">间隔秒数；&lt;=0 表示每帧执行。</param>
        /// <param name="phaseOffsetMs">首次执行的延迟（毫秒），用于错峰。</param>
        /// <param name="tick">回调，参数为本次推进的时长。</param>
        public void Schedule(string key, float interval, int phaseOffsetMs, Action<float> tick)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key is null or empty", nameof(key));
            if (tick == null) throw new ArgumentNullException(nameof(tick));

            int index = IndexOf(key);
            if (index >= 0)
            {
                Layer existing = _layers[index];
                existing.Interval = interval;
                existing.Accumulator = -(phaseOffsetMs / 1000f);
                existing.Tick = tick;
                return;
            }

            _layers.Add(new Layer
            {
                Key = key,
                Interval = interval,
                Accumulator = -(phaseOffsetMs / 1000f),
                Tick = tick,
            });
        }

        public bool Unschedule(string key)
        {
            int index = IndexOf(key);
            if (index < 0) return false;

            _layers.RemoveAt(index);
            return true;
        }

        public void Clear()
        {
            _layers.Clear();
        }

        public void Tick(BattleClock clock)
        {
            float deltaTime = clock.DeltaTime;
            if (deltaTime <= 0f) return;

            for (int i = 0; i < _layers.Count; i++)
            {
                Layer layer = _layers[i];

                if (layer.Interval <= 0f)
                {
                    layer.Tick(deltaTime);
                    continue;
                }

                layer.Accumulator += deltaTime;
                if (layer.Accumulator < layer.Interval) continue;

                // 单帧最多推进 4 次，避免长时间卡顿后雪崩
                int guard = 0;
                while (layer.Accumulator >= layer.Interval && guard < 4)
                {
                    layer.Accumulator -= layer.Interval;
                    layer.Tick(layer.Interval);
                    guard++;
                }

                if (layer.Accumulator >= layer.Interval) layer.Accumulator = 0f;
            }
        }

        private int IndexOf(string key)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                if (string.Equals(_layers[i].Key, key, StringComparison.Ordinal)) return i;
            }

            return -1;
        }
    }
}
