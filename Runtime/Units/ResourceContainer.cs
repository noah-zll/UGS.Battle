using System;
using System.Collections.Generic;
using UGS.Battle.Attributes;
using UnityEngine;

namespace UGS.Battle.Units
{
    /// <summary>
    /// 通用资源池。按属性 Id 索引，包内不认识"生命""法力"这类具体概念。
    /// 未显式初始化的资源视为"满值"。
    /// </summary>
    public sealed class ResourceContainer
    {
        private readonly AttributeContainer _attributes;

        private float[] _current;
        private bool[] _initialized;

        /// <summary>资源变化事件（可选，默认不订阅）。</summary>
        public event Action<int, float, float> Changed;

        public ResourceContainer(AttributeContainer attributes)
        {
            _attributes = attributes ?? throw new ArgumentNullException(nameof(attributes));
            Allocate(Mathf.Max(AttributeRegistry.Capacity, 16));
        }

        public float GetMax(int resourceAttributeId)
        {
            return _attributes.Get(resourceAttributeId);
        }

        public float GetCurrent(int resourceAttributeId)
        {
            EnsureCapacity();
            if ((uint)resourceAttributeId >= (uint)_current.Length) return 0f;

            return _initialized[resourceAttributeId]
                ? _current[resourceAttributeId]
                : GetMax(resourceAttributeId);
        }

        public float GetRatio(int resourceAttributeId)
        {
            float max = GetMax(resourceAttributeId);
            if (max <= 0f) return 0f;

            return Mathf.Clamp01(GetCurrent(resourceAttributeId) / max);
        }

        public bool IsDepleted(int resourceAttributeId)
        {
            return GetCurrent(resourceAttributeId) <= 0f;
        }

        public void SetCurrent(int resourceAttributeId, float value)
        {
            EnsureCapacity();
            if ((uint)resourceAttributeId >= (uint)_current.Length) return;

            float max = GetMax(resourceAttributeId);
            float clamped = max > 0f ? Mathf.Clamp(value, 0f, max) : Mathf.Max(0f, value);
            float old = GetCurrent(resourceAttributeId);

            _current[resourceAttributeId] = clamped;
            _initialized[resourceAttributeId] = true;

            if (!Mathf.Approximately(old, clamped)) Changed?.Invoke(resourceAttributeId, old, clamped);
        }

        /// <summary>增减并钳制到 [0, Max]，返回实际变化量（用于吸血/溢出统计）。</summary>
        public float Change(int resourceAttributeId, float delta)
        {
            if (Mathf.Approximately(delta, 0f)) return 0f;

            float old = GetCurrent(resourceAttributeId);
            SetCurrent(resourceAttributeId, old + delta);
            return GetCurrent(resourceAttributeId) - old;
        }

        public void InitToMax(int resourceAttributeId)
        {
            SetCurrent(resourceAttributeId, GetMax(resourceAttributeId));
        }

        /// <summary>把所有资源型属性初始化为满值。</summary>
        public void InitAllToMax()
        {
            IReadOnlyList<int> ids = AttributeRegistry.ResourceAttributes;
            for (int i = 0; i < ids.Count; i++) InitToMax(ids[i]);
        }

        /// <summary>上限变化后把当前值重新钳制（如 Buff 降低最大生命）。</summary>
        public void ClampToMax(int resourceAttributeId)
        {
            EnsureCapacity();
            if ((uint)resourceAttributeId >= (uint)_current.Length) return;
            if (!_initialized[resourceAttributeId]) return;

            SetCurrent(resourceAttributeId, _current[resourceAttributeId]);
        }

        private void EnsureCapacity()
        {
            int required = AttributeRegistry.Capacity;
            if (required <= _current.Length) return;

            Allocate(Mathf.Max(required, _current.Length * 2));
        }

        private void Allocate(int capacity)
        {
            float[] newCurrent = new float[capacity];
            bool[] newInitialized = new bool[capacity];

            if (_current != null)
            {
                int copy = Mathf.Min(_current.Length, capacity);
                Array.Copy(_current, newCurrent, copy);
                Array.Copy(_initialized, newInitialized, copy);
            }

            _current = newCurrent;
            _initialized = newInitialized;
        }
    }

    /// <summary>通用资源消耗描述：任意资源型属性都可作为消耗。</summary>
    public readonly struct ResourceCost
    {
        public readonly int ResourceAttributeId;
        public readonly float Amount;

        public ResourceCost(int resourceAttributeId, float amount)
        {
            ResourceAttributeId = resourceAttributeId;
            Amount = amount;
        }

        public override string ToString() => $"Resource({ResourceAttributeId}) -{Amount}";
    }
}
