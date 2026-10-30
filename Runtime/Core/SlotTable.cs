using System;
using System.Collections.Generic;

namespace UGS.Battle.Core
{
    /// <summary>
    /// Slot + generation 的单位索引表。
    /// 提供连续数组遍历（对 CPU cache 友好）与失效句柄检测。
    /// </summary>
    public sealed class SlotTable<T> where T : class
    {
        private const int DefaultCapacity = 16;

        private T[] _items;
        private int[] _generation;
        private readonly Stack<int> _freeSlots = new Stack<int>();
        private int _count;
        private int _highWater;

        public SlotTable(int capacity = DefaultCapacity)
        {
            capacity = capacity < 1 ? DefaultCapacity : capacity;
            _items = new T[capacity];
            _generation = new int[capacity];
        }

        /// <summary>已占用槽位数量（含"空洞"之外的项）。</summary>
        public int Count => _count;

        /// <summary>底层数组的容量。</summary>
        public int Capacity => _items.Length;

        /// <summary>底层数组（含 null 空洞）。遍历时需跳过 null。</summary>
        public T[] Raw => _items;

        /// <summary>占用槽位数为 0 时返回 true（用于空早退）。</summary>
        public bool IsEmpty => _count == 0;

        public int Add(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            int slot;
            if (_freeSlots.Count > 0)
            {
                slot = _freeSlots.Pop();
            }
            else
            {
                if (_highWater >= _items.Length) Grow();
                slot = _highWater++;
            }

            _items[slot] = item;
            _count++;
            return slot;
        }

        public bool TryGet(int slot, out T item)
        {
            if ((uint)slot >= (uint)_items.Length)
            {
                item = null;
                return false;
            }

            item = _items[slot];
            return item != null;
        }

        public T Get(int slot)
        {
            return (uint)slot < (uint)_items.Length ? _items[slot] : null;
        }

        public int GetGeneration(int slot)
        {
            return (uint)slot < (uint)_generation.Length ? _generation[slot] : -1;
        }

        public bool IsValid(int slot, int generation)
        {
            return (uint)slot < (uint)_items.Length
                   && _items[slot] != null
                   && _generation[slot] == generation;
        }

        public bool Remove(int slot)
        {
            if ((uint)slot >= (uint)_items.Length || _items[slot] == null) return false;

            _items[slot] = null;
            _generation[slot]++;
            _freeSlots.Push(slot);
            _count--;
            return true;
        }

        public void Clear()
        {
            for (int i = 0; i < _highWater; i++)
            {
                _items[i] = null;
                _generation[i]++;
            }

            _freeSlots.Clear();
            _count = 0;
            _highWater = 0;
        }

        private void Grow()
        {
            int newCapacity = _items.Length * 2;
            Array.Resize(ref _items, newCapacity);
            Array.Resize(ref _generation, newCapacity);
        }
    }

    /// <summary>槽位 + 代数的句柄，用于安全引用 SlotTable 中的项。</summary>
    public readonly struct SlotHandle : IEquatable<SlotHandle>
    {
        public readonly int Slot;
        public readonly int Generation;

        public SlotHandle(int slot, int generation)
        {
            Slot = slot;
            Generation = generation;
        }

        public static readonly SlotHandle Invalid = new SlotHandle(-1, -1);

        public bool IsValid => Slot >= 0;

        public bool Equals(SlotHandle other) => Slot == other.Slot && Generation == other.Generation;

        public override bool Equals(object obj) => obj is SlotHandle other && Equals(other);

        public override int GetHashCode() => (Slot * 397) ^ Generation;

        public override string ToString() => $"Slot({Slot}:{Generation})";
    }
}
