using System;
using System.Collections.Generic;

namespace UGS.Battle.Attributes
{
    /// <summary>
    /// 属性注册表。属性 key 由字符串定义，运行时映射为连续小整数 id，
    /// 便于 <see cref="AttributeContainer"/> 使用数组而非字典存储。
    /// </summary>
    public static class AttributeRegistry
    {
        private static readonly Dictionary<string, AttributeDefinition> ByKey =
            new Dictionary<string, AttributeDefinition>(StringComparer.Ordinal);

        private static readonly List<AttributeDefinition> ById = new List<AttributeDefinition>(64)
        {
            null, // id 0 保留
        };

        private static readonly List<List<int>> Dependents = new List<List<int>>(64) { null };
        private static readonly List<int[]> DependentCache = new List<int[]>(64) { null };

        private static readonly List<int> ResourceIds = new List<int>(8);

        /// <summary>下一个可用 id（= 已定义属性数量 + 1）。</summary>
        public static int Capacity => ById.Count;

        /// <summary>所有已定义属性。</summary>
        public static IReadOnlyList<AttributeDefinition> All => ById;

        /// <summary>所有资源型属性 id。</summary>
        public static IReadOnlyList<int> ResourceAttributes => ResourceIds;

        /// <summary>
        /// 定义一个属性（幂等）。返回既有定义或新定义。
        /// </summary>
        public static AttributeDefinition Define(string key, float defaultValue = 0f,
            float min = float.MinValue, float max = float.MaxValue, bool isResource = false)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key is null or empty", nameof(key));

            if (ByKey.TryGetValue(key, out AttributeDefinition exist)) return exist;

            var def = new AttributeDefinition
            {
                Id = ById.Count,
                Key = key,
                DefaultValue = defaultValue,
                Min = min,
                Max = max,
                IsResource = isResource,
            };

            ByKey[key] = def;
            ById.Add(def);
            Dependents.Add(null);
            DependentCache.Add(null);

            if (isResource) ResourceIds.Add(def.Id);

            return def;
        }

        public static AttributeDefinition Get(string key)
        {
            if (ByKey.TryGetValue(key, out AttributeDefinition def)) return def;
            throw new BattleAttributesException($"attribute not defined: {key}");
        }

        public static AttributeDefinition Get(int id)
        {
            if ((uint)id < (uint)ById.Count && ById[id] != null) return ById[id];
            throw new BattleAttributesException($"attribute id not defined: {id}");
        }

        public static bool TryGet(int id, out AttributeDefinition def)
        {
            if ((uint)id < (uint)ById.Count && ById[id] != null)
            {
                def = ById[id];
                return true;
            }

            def = null;
            return false;
        }

        public static bool TryGetId(string key, out int id)
        {
            if (ByKey.TryGetValue(key, out AttributeDefinition def))
            {
                id = def.Id;
                return true;
            }

            id = 0;
            return false;
        }

        /// <summary>
        /// 声明依赖：<paramref name="sourceId"/> 变化时，<paramref name="dependentId"/> 也需要重算。
        /// （例如"全局减伤 → 各部位减伤"）
        /// </summary>
        public static void AddDependency(int sourceId, int dependentId)
        {
            if (sourceId == dependentId) return;
            if ((uint)sourceId >= (uint)ById.Count) throw new ArgumentOutOfRangeException(nameof(sourceId));
            if ((uint)dependentId >= (uint)ById.Count) throw new ArgumentOutOfRangeException(nameof(dependentId));

            List<int> list = Dependents[sourceId];
            if (list == null)
            {
                list = new List<int>(2);
                Dependents[sourceId] = list;
            }

            if (!list.Contains(dependentId))
            {
                list.Add(dependentId);
                DependentCache[sourceId] = list.ToArray();
            }
        }

        /// <summary>获取依赖该属性的下游属性数组（零分配）。</summary>
        public static int[] GetDependents(int sourceId)
        {
            if ((uint)sourceId >= (uint)DependentCache.Count) return null;
            return DependentCache[sourceId];
        }

        /// <summary>清空注册表（仅测试/热重载使用）。</summary>
        public static void Clear()
        {
            ByKey.Clear();
            ById.Clear();
            ById.Add(null);
            Dependents.Clear();
            Dependents.Add(null);
            DependentCache.Clear();
            DependentCache.Add(null);
            ResourceIds.Clear();
        }
    }

    /// <summary>属性系统异常。</summary>
    public sealed class BattleAttributesException : Exception
    {
        public BattleAttributesException(string message) : base(message) { }
    }
}
