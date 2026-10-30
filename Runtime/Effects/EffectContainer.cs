using System;
using System.Collections.Generic;
using UGS.Battle.Core;
using UGS.Battle.Sessions;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Effects
{
    /// <summary>
    /// 效果容器：单位身上的 Buff / Debuff 集合。
    /// 使用"到期最小堆"推进，复杂度 O(活跃效果)；空容器直接早退。
    /// </summary>
    public sealed class EffectContainer
    {
        private struct ExpiryEntry
        {
            public EffectInstance Instance;
            public float ExpireAt;
        }

        private readonly ICombatUnit _owner;
        private readonly List<EffectInstance> _active = new List<EffectInstance>(8);
        private readonly Dictionary<int, List<EffectInstance>> _byDefinition =
            new Dictionary<int, List<EffectInstance>>(8);
        private readonly List<ExpiryEntry> _expiryHeap = new List<ExpiryEntry>(16);

        private long _applyOrderSeed;
        private float _localElapsed;

        public event Action<EffectInstance> Applied;
        public event Action<EffectInstance, EffectRemoveReason> Removed;
        public event Action<EffectInstance, int, int> StackChanged;

        public IReadOnlyList<EffectInstance> Effects => _active;

        public int Count => _active.Count;

        /// <summary>空容器早退标志。</summary>
        public bool IsEmpty => _active.Count == 0;

        public EffectContainer(ICombatUnit owner)
        {
            _owner = owner;
        }

        /// <summary>容器自身的累计时间。只由 <see cref="Tick"/> 推进，保证与是否接入会话无关。</summary>
        private float Elapsed => _localElapsed;

        private BattleServiceRegistry Services
        {
            get
            {
                BattleSession session = _owner?.Session;
                return session != null ? session.Services : BattleServiceRegistry.Default;
            }
        }

        // ---------------- 施加 ----------------

        public EffectInstance Apply(IEffectDefinition definition, ICombatUnit source,
            int stacks = 1, float? durationOverride = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (stacks < 1) stacks = 1;

            float elapsed = Elapsed;
            float duration = durationOverride ?? definition.Duration;

            if (definition.StackRule != StackRule.Independent)
            {
                EffectInstance existing = FindInstance(definition.Id);
                if (existing != null)
                {
                    return ApplyToExisting(existing, definition, stacks, duration, elapsed);
                }
            }

            EffectInstance instance = new EffectInstance(definition, _owner, source)
            {
                Stacks = Mathf.Clamp(stacks, 1, Mathf.Max(1, definition.MaxStacks)),
                Remaining = definition.IsPermanent() ? float.PositiveInfinity : duration,
                ExpireAt = definition.IsPermanent() ? float.PositiveInfinity : elapsed + duration,
                ApplyOrder = ++_applyOrderSeed,
                Behaviors = CreateBehaviors(definition),
            };

            _active.Add(instance);
            GetOrCreateList(definition.Id).Add(instance);
            PushExpiry(instance);

            EffectContext context = EffectContext.Create(instance);
            for (int i = 0; i < instance.Behaviors.Length; i++) instance.Behaviors[i]?.OnApply(context);

            Applied?.Invoke(instance);
            return instance;
        }

        private EffectInstance ApplyToExisting(EffectInstance existing, IEffectDefinition definition,
            int stacks, float duration, float elapsed)
        {
            EffectContext context = EffectContext.Create(existing);

            switch (definition.StackRule)
            {
                case StackRule.Ignore:
                    return existing;

                case StackRule.Extend:
                    if (!definition.IsPermanent())
                    {
                        existing.Remaining += duration;
                        existing.ExpireAt = elapsed + existing.Remaining;
                        PushExpiry(existing);
                    }

                    return existing;

                case StackRule.Refresh:
                    ResetDuration(existing, definition, duration, elapsed);
                    return existing;

                case StackRule.Stack:
                    {
                        int oldStacks = existing.Stacks;
                        int maxStacks = Mathf.Max(1, definition.MaxStacks);
                        int newStacks = Mathf.Min(maxStacks, oldStacks + stacks);

                        ResetDuration(existing, definition, duration, elapsed);

                        if (newStacks != oldStacks)
                        {
                            existing.Stacks = newStacks;
                            for (int i = 0; i < existing.Behaviors.Length; i++)
                            {
                                existing.Behaviors[i]?.OnStackChanged(context, oldStacks, newStacks);
                            }

                            StackChanged?.Invoke(existing, oldStacks, newStacks);
                        }

                        return existing;
                    }

                default:
                    ResetDuration(existing, definition, duration, elapsed);
                    return existing;
            }
        }

        private void ResetDuration(EffectInstance instance, IEffectDefinition definition,
            float duration, float elapsed)
        {
            if (definition.IsPermanent()) return;

            instance.Remaining = duration;
            instance.ExpireAt = elapsed + duration;
            PushExpiry(instance);
        }

        private IEffectBehavior[] CreateBehaviors(IEffectDefinition definition)
        {
            IReadOnlyList<string> keys = definition.BehaviorKeys;
            if (keys == null || keys.Count == 0) return Array.Empty<IEffectBehavior>();

            BattleServiceRegistry services = Services;
            IEffectBehavior[] behaviors = new IEffectBehavior[keys.Count];

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (services != null && services.TryGetEffectBehaviorFactory(key, out var factory))
                {
                    behaviors[i] = factory(definition);
                }
                else
                {
                    services?.Logger?.Warning($"[UGS.Battle] effect behavior not registered: {key}");
                }
            }

            return behaviors;
        }

        // ---------------- 移除 ----------------

        public bool Remove(int definitionId, EffectRemoveReason reason, int stacks = 1)
        {
            EffectInstance instance = FindInstance(definitionId);
            if (instance == null) return false;

            if (stacks >= instance.Stacks)
            {
                RemoveInstance(instance, reason);
                return true;
            }

            int oldStacks = instance.Stacks;
            instance.Stacks -= stacks;
            EffectContext context = EffectContext.Create(instance);
            for (int i = 0; i < instance.Behaviors.Length; i++)
            {
                instance.Behaviors[i]?.OnStackChanged(context, oldStacks, instance.Stacks);
            }

            StackChanged?.Invoke(instance, oldStacks, instance.Stacks);
            return true;
        }

        public int RemoveByTag(EffectTag tag, int maxCount = int.MaxValue)
        {
            int removed = 0;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (removed >= maxCount) break;

                EffectInstance instance = _active[i];
                if (instance.IsRemoved) continue;
                if (!instance.Definition.HasTag(tag)) continue;

                RemoveInstance(instance, EffectRemoveReason.Dispelled);
                removed++;
            }

            return removed;
        }

        /// <summary>移除来源为指定对象的全部效果。</summary>
        public int RemoveFrom(object source)
        {
            if (source == null) return 0;

            int removed = 0;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                EffectInstance instance = _active[i];
                if (instance.IsRemoved) continue;
                if (!ReferenceEquals(instance.Source, source)) continue;

                RemoveInstance(instance, EffectRemoveReason.Dispelled);
                removed++;
            }

            return removed;
        }

        public void Clear(EffectRemoveReason reason)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                RemoveInstance(_active[i], reason);
            }

            _active.Clear();
            _byDefinition.Clear();
            _expiryHeap.Clear();
        }

        // ---------------- 查询 ----------------

        public EffectInstance FindInstance(int definitionId)
        {
            if (!_byDefinition.TryGetValue(definitionId, out List<EffectInstance> list)) return null;

            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].IsRemoved) return list[i];
            }

            return null;
        }

        public bool Has(int definitionId) => FindInstance(definitionId) != null;

        public int GetStacks(int definitionId)
        {
            EffectInstance instance = FindInstance(definitionId);
            return instance?.Stacks ?? 0;
        }

        public bool HasTag(EffectTag tag)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (!_active[i].IsRemoved && _active[i].Definition.HasTag(tag)) return true;
            }

            return false;
        }

        /// <summary>取第一个匹配类型的行为（如护盾池）。</summary>
        public T GetFirstBehavior<T>() where T : class
        {
            for (int i = 0; i < _active.Count; i++)
            {
                EffectInstance instance = _active[i];
                if (instance.IsRemoved) continue;

                IEffectBehavior[] behaviors = instance.Behaviors;
                for (int b = 0; b < behaviors.Length; b++)
                {
                    if (behaviors[b] is T typed) return typed;
                }
            }

            return null;
        }

        // ---------------- Tick ----------------

        public void Tick(float deltaTime)
        {
            if (_active.Count == 0 || deltaTime <= 0f) return;

            _localElapsed += deltaTime;
            float elapsed = Elapsed;

            for (int i = 0; i < _active.Count; i++)
            {
                EffectInstance instance = _active[i];
                if (instance.IsRemoved) continue;

                IEffectDefinition definition = instance.Definition;

                if (!definition.IsPermanent())
                {
                    instance.Remaining -= deltaTime;
                    if (instance.Remaining < 0f) instance.Remaining = 0f;
                }

                EffectContext context = EffectContext.Create(instance);

                IEffectBehavior[] behaviors = instance.Behaviors;
                for (int b = 0; b < behaviors.Length; b++) behaviors[b]?.OnTick(context, deltaTime);

                if (definition.IsPeriodic())
                {
                    instance.PeriodAccumulator += deltaTime;
                    int guard = 0;
                    while (instance.PeriodAccumulator >= definition.Period && guard < 8)
                    {
                        instance.PeriodAccumulator -= definition.Period;
                        for (int b = 0; b < behaviors.Length; b++) behaviors[b]?.OnPeriod(context);
                        guard++;
                    }
                }
            }

            ExpireDue(elapsed);
        }

        private void ExpireDue(float elapsed)
        {
            while (_expiryHeap.Count > 0)
            {
                ExpiryEntry entry = _expiryHeap[0];
                if (entry.ExpireAt > elapsed) break;

                PopHeap();

                EffectInstance instance = entry.Instance;
                if (instance == null || instance.IsRemoved) continue;

                // 已续期（堆中存在旧条目）→ 跳过
                if (instance.ExpireAt > entry.ExpireAt) continue;

                RemoveInstance(instance, EffectRemoveReason.Expired);
            }

            if (_active.Count == 0) _expiryHeap.Clear();
        }

        private void RemoveInstance(EffectInstance instance, EffectRemoveReason reason)
        {
            if (instance == null || instance.IsRemoved) return;

            instance.IsRemoved = true;

            EffectContext context = EffectContext.Create(instance);
            IEffectBehavior[] behaviors = instance.Behaviors;

            for (int i = 0; i < behaviors.Length; i++)
            {
                if (behaviors[i] == null) continue;

                if (reason == EffectRemoveReason.Expired) behaviors[i].OnExpire(context);
                behaviors[i].OnRemoved(context, reason);
            }

            _active.Remove(instance);

            if (_byDefinition.TryGetValue(instance.Definition.Id, out List<EffectInstance> list))
            {
                list.Remove(instance);
                if (list.Count == 0) _byDefinition.Remove(instance.Definition.Id);
            }

            Removed?.Invoke(instance, reason);
        }

        private List<EffectInstance> GetOrCreateList(int definitionId)
        {
            if (!_byDefinition.TryGetValue(definitionId, out List<EffectInstance> list))
            {
                list = new List<EffectInstance>(2);
                _byDefinition[definitionId] = list;
            }

            return list;
        }

        // ---------------- 到期最小堆 ----------------

        private void PushExpiry(EffectInstance instance)
        {
            if (instance.Definition.IsPermanent()) return;

            _expiryHeap.Add(new ExpiryEntry { Instance = instance, ExpireAt = instance.ExpireAt });

            int index = _expiryHeap.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (_expiryHeap[parent].ExpireAt <= _expiryHeap[index].ExpireAt) break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void PopHeap()
        {
            int last = _expiryHeap.Count - 1;
            _expiryHeap[0] = _expiryHeap[last];
            _expiryHeap.RemoveAt(last);

            int index = 0;
            int count = _expiryHeap.Count;

            while (true)
            {
                int left = (index << 1) + 1;
                if (left >= count) break;

                int right = left + 1;
                int smallest = (right < count && _expiryHeap[right].ExpireAt < _expiryHeap[left].ExpireAt)
                    ? right
                    : left;

                if (_expiryHeap[index].ExpireAt <= _expiryHeap[smallest].ExpireAt) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            ExpiryEntry temp = _expiryHeap[a];
            _expiryHeap[a] = _expiryHeap[b];
            _expiryHeap[b] = temp;
        }
    }
}
