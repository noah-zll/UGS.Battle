using System;
using System.Collections.Generic;
using UGS.Battle.Core;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Attributes
{
    /// <summary>
    /// 属性容器：数组化存储 + 脏标记缓存。
    /// 计算式：value = (Base + ΣAdd) * Π(1 + Mul) 或 Override → 公式链 → Clamp。
    /// </summary>
    public sealed class AttributeContainer
    {
        private readonly ICombatUnit _owner;
        private readonly BattleServiceRegistry _services;

        private float[] _base;
        private bool[] _hasBase;
        private float[] _cache;
        private bool[] _dirty;
        private List<IAttributeModifier>[] _modifiers;

        /// <summary>属性值变化事件。默认关闭（热路径零分配要求），需要时手动开启。</summary>
        public event Action<int, float, float> Changed;

        /// <summary>是否抛出 <see cref="Changed"/>。默认 false。</summary>
        public bool RaiseChanged { get; set; }

        public ICombatUnit Owner => _owner;

        public AttributeContainer(ICombatUnit owner, BattleServiceRegistry services = null)
        {
            _owner = owner;
            _services = services ?? BattleServiceRegistry.Default;

            int capacity = Mathf.Max(AttributeRegistry.Capacity, 16);
            Allocate(capacity);
        }

        // ---------------- 基础值 ----------------

        public bool HasBase(int attributeId)
        {
            EnsureCapacity();
            return (uint)attributeId < (uint)_hasBase.Length && _hasBase[attributeId];
        }

        public float GetBase(int attributeId)
        {
            EnsureCapacity();
            if ((uint)attributeId >= (uint)_base.Length) return 0f;
            if (_hasBase[attributeId]) return _base[attributeId];
            return AttributeRegistry.TryGet(attributeId, out AttributeDefinition def) ? def.DefaultValue : 0f;
        }

        public void SetBase(int attributeId, float value)
        {
            EnsureCapacity();
            if ((uint)attributeId >= (uint)_base.Length) return;

            _base[attributeId] = value;
            _hasBase[attributeId] = true;
            MarkDirty(attributeId);
        }

        // ---------------- 修正器 ----------------

        public void AddModifier(IAttributeModifier modifier)
        {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));

            EnsureCapacity();
            foreach (int id in modifier.AffectedAttributes)
            {
                if ((uint)id >= (uint)_modifiers.Length) continue;

                List<IAttributeModifier> list = _modifiers[id];
                if (list == null)
                {
                    list = new List<IAttributeModifier>(4);
                    _modifiers[id] = list;
                }

                list.Add(modifier);
                MarkDirty(id);
            }
        }

        public void RemoveModifier(IAttributeModifier modifier)
        {
            if (modifier == null) return;

            EnsureCapacity();
            foreach (int id in modifier.AffectedAttributes)
            {
                if ((uint)id >= (uint)_modifiers.Length) continue;

                List<IAttributeModifier> list = _modifiers[id];
                if (list == null) continue;

                if (list.Remove(modifier)) MarkDirty(id);
            }
        }

        /// <summary>按来源批量移除（Buff 过期、装备卸下）。返回移除数量。</summary>
        public int RemoveModifiersFrom(object source)
        {
            if (source == null) return 0;

            EnsureCapacity();
            int removed = 0;
            for (int id = 0; id < _modifiers.Length; id++)
            {
                List<IAttributeModifier> list = _modifiers[id];
                if (list == null || list.Count == 0) continue;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(list[i].Source, source))
                    {
                        list.RemoveAt(i);
                        MarkDirty(id);
                        removed++;
                    }
                }
            }

            return removed;
        }

        public bool HasModifiersFrom(object source)
        {
            if (source == null) return false;

            for (int id = 0; id < _modifiers.Length; id++)
            {
                List<IAttributeModifier> list = _modifiers[id];
                if (list == null) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    if (ReferenceEquals(list[i].Source, source)) return true;
                }
            }

            return false;
        }

        // ---------------- 取值 ----------------

        public float Get(int attributeId)
        {
            if (attributeId <= 0) return 0f;

            EnsureCapacity();
            if ((uint)attributeId >= (uint)_cache.Length) return 0f;

            if (!_dirty[attributeId]) return _cache[attributeId];

            float old = _cache[attributeId];
            float value = Compute(attributeId);

            _cache[attributeId] = value;
            _dirty[attributeId] = false;

            if (RaiseChanged && !Mathf.Approximately(old, value))
            {
                Changed?.Invoke(attributeId, old, value);
            }

            return value;
        }

        public void MarkDirty(int attributeId)
        {
            if ((uint)attributeId >= (uint)_dirty.Length) return;
            if (_dirty[attributeId]) return;

            _dirty[attributeId] = true;

            int[] dependents = AttributeRegistry.GetDependents(attributeId);
            if (dependents == null) return;

            for (int i = 0; i < dependents.Length; i++) MarkDirty(dependents[i]);
        }

        public void MarkAllDirty()
        {
            for (int i = 0; i < _dirty.Length; i++) _dirty[i] = true;
        }

        // ---------------- 内部 ----------------

        private float Compute(int attributeId)
        {
            AttributeRegistry.TryGet(attributeId, out AttributeDefinition def);

            float baseValue = _hasBase[attributeId]
                ? _base[attributeId]
                : (def != null ? def.DefaultValue : 0f);

            float add = 0f;
            float multiply = 1f;
            float overrideValue = 0f;
            bool hasOverride = false;

            List<IAttributeModifier> modifiers = _modifiers[attributeId];
            if (modifiers != null && modifiers.Count > 0)
            {
                var context = new AttributeQueryContext(_owner, attributeId);
                for (int i = 0; i < modifiers.Count; i++)
                {
                    IAttributeModifier modifier = modifiers[i];
                    float value = modifier.GetValue(attributeId, context);

                    switch (modifier.Op)
                    {
                        case ModifierOp.Add:
                            add += value;
                            break;
                        case ModifierOp.Multiply:
                            multiply *= 1f + value;
                            break;
                        default:
                            hasOverride = true;
                            overrideValue = value;
                            break;
                    }
                }
            }

            float result = hasOverride ? overrideValue : (baseValue + add) * multiply;

            if (_services.TryGetAttributeFormulas(attributeId, out List<IAttributeFormula> formulas))
            {
                for (int i = 0; i < formulas.Count; i++)
                {
                    result = formulas[i].Apply(this, attributeId, result);
                }
            }

            if (def != null) result = Mathf.Clamp(result, def.Min, def.Max);

            return result;
        }

        private void EnsureCapacity()
        {
            int required = AttributeRegistry.Capacity;
            if (required <= _base.Length) return;

            int capacity = Mathf.Max(required, _base.Length * 2);
            Allocate(capacity, true);
        }

        private void Allocate(int capacity, bool preserve = false)
        {
            float[] newBase = new float[capacity];
            bool[] newHasBase = new bool[capacity];
            float[] newCache = new float[capacity];
            bool[] newDirty = new bool[capacity];
            var newModifiers = new List<IAttributeModifier>[capacity];

            if (preserve && _base != null)
            {
                int copy = Mathf.Min(_base.Length, capacity);
                Array.Copy(_base, newBase, copy);
                Array.Copy(_hasBase, newHasBase, copy);
                Array.Copy(_cache, newCache, copy);
                Array.Copy(_dirty, newDirty, copy);
                Array.Copy(_modifiers, newModifiers, copy);
            }

            for (int i = 0; i < capacity; i++)
            {
                if (!preserve || i >= (_dirty?.Length ?? 0)) newDirty[i] = true;
            }

            _base = newBase;
            _hasBase = newHasBase;
            _cache = newCache;
            _dirty = newDirty;
            _modifiers = newModifiers;
        }
    }
}
