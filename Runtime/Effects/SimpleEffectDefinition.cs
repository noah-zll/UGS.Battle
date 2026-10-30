using System.Collections.Generic;

namespace UGS.Battle.Effects
{
    /// <summary>
    /// 包内默认的 <see cref="IEffectDefinition"/> 实现（测试、Samples、Editor，以及宿主的合成定义，
    /// 例如护盾持有者 / 计时器 —— 它们没有配置表行可依托）。
    /// </summary>
    public sealed class SimpleEffectDefinition : IEffectDefinition
    {
        public int Id { get; set; }

        public string Key { get; set; }

        /// <summary>持续时长（秒）；&lt;0 表示永久。</summary>
        public float Duration { get; set; } = -1f;

        /// <summary>周期触发间隔（秒）；&lt;=0 表示非周期效果。</summary>
        public float Period { get; set; }

        public int MaxStacks { get; set; } = 1;

        public StackRule StackRule { get; set; } = StackRule.Refresh;

        public EffectTag Tags { get; set; }

        public int Priority { get; set; }

        /// <summary>行为 key 列表，由 <see cref="Core.BattleServiceRegistry"/> 解析为运行时行为实例。</summary>
        public List<string> BehaviorKeys { get; set; } = new List<string>(1);

        /// <summary>类型化的扩展载荷，由上层 Builder 填充、具体行为自行强转（如属性修正配置）。</summary>
        public object Payload { get; set; }

        /// <summary>行为参数（固定值、倍率等），由行为自行解释。</summary>
        public Dictionary<string, float> Parameters { get; set; } = new Dictionary<string, float>(4);

        IReadOnlyList<string> IEffectDefinition.BehaviorKeys => BehaviorKeys;

        public float GetParameter(string key, float defaultValue = 0f)
        {
            return Parameters != null && Parameters.TryGetValue(key, out float value) ? value : defaultValue;
        }
    }
}
