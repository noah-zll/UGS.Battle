using System;
using System.Collections.Generic;
using UGS.Battle.Attributes;
using UGS.Battle.Effects;
using UGS.Battle.Skills;
using UGS.Battle.Units;

namespace UGS.Battle.Core
{
    /// <summary>
    /// 扩展点注册表。所有"需要游戏侧注入"的实现集中在此，便于集中查看与整体替换。
    /// </summary>
    public sealed class BattleServiceRegistry
    {
        private readonly Dictionary<int, List<IAttributeFormula>> _attributeFormulas =
            new Dictionary<int, List<IAttributeFormula>>();

        private readonly Dictionary<string, Func<IEffectDefinition, IEffectBehavior>> _effectBehaviors =
            new Dictionary<string, Func<IEffectDefinition, IEffectBehavior>>(StringComparer.Ordinal);

        private readonly Dictionary<string, ITargetSelector> _targetSelectors =
            new Dictionary<string, ITargetSelector>(StringComparer.Ordinal);

        /// <summary>全局默认注册表。</summary>
        public static BattleServiceRegistry Default { get; } = new BattleServiceRegistry();

        public IBattleLogger Logger { get; set; } = NullBattleLogger.Instance;

        /// <summary>召唤物生成器（游戏侧实现）。</summary>
        public IUnitSpawner Spawner { get; set; }

        // ---------------- 属性公式 ----------------

        public void RegisterAttributeFormula(int attributeId, IAttributeFormula formula)
        {
            if (formula == null) throw new ArgumentNullException(nameof(formula));

            if (!_attributeFormulas.TryGetValue(attributeId, out var list))
            {
                list = new List<IAttributeFormula>(2);
                _attributeFormulas[attributeId] = list;
            }

            list.Add(formula);
            // 注册期排序：运行期只遍历
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public bool TryGetAttributeFormulas(int attributeId, out List<IAttributeFormula> formulas)
        {
            return _attributeFormulas.TryGetValue(attributeId, out formulas);
        }

        // ---------------- 效果行为 ----------------

        public void RegisterEffectBehavior(string key, Func<IEffectDefinition, IEffectBehavior> factory)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key is null or empty", nameof(key));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _effectBehaviors[key] = factory;
        }

        public bool TryGetEffectBehaviorFactory(string key,
            out Func<IEffectDefinition, IEffectBehavior> factory)
        {
            return _effectBehaviors.TryGetValue(key, out factory);
        }

        // ---------------- 目标选择器 ----------------

        public void RegisterTargetSelector(string key, ITargetSelector selector)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key is null or empty", nameof(key));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            _targetSelectors[key] = selector;
        }

        public bool TryGetTargetSelector(string key, out ITargetSelector selector)
        {
            return _targetSelectors.TryGetValue(key, out selector);
        }

        public void Clear()
        {
            _attributeFormulas.Clear();
            _effectBehaviors.Clear();
            _targetSelectors.Clear();
            Spawner = null;
            Logger = NullBattleLogger.Instance;
        }
    }
}
