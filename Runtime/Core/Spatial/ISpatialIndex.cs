using System.Collections.Generic;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Core
{
    /// <summary>
    /// 空间索引接口。所有范围/最近查询必须走索引，禁止全表扫描。
    /// </summary>
    public interface ISpatialIndex
    {
        /// <summary>已索引的单位数量。</summary>
        int Count { get; }

        /// <summary>重建索引（由调度器按低频调用）。</summary>
        void Rebuild(IReadOnlyList<ICombatUnit> units);

        void Clear();

        /// <summary>
        /// 查询圆形范围内的单位。
        /// </summary>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        /// <param name="factionId">阵营过滤；&lt;0 表示不过滤。</param>
        /// <param name="results">结果容器，方法内部会先 Clear。</param>
        /// <returns>结果数量。</returns>
        int Query(Vector2 center, float radius, int factionId, List<ICombatUnit> results);

        /// <summary>查找最近的单位；找不到返回 null。</summary>
        ICombatUnit FindNearest(Vector2 from, int factionId, float maxRadius);
    }
}
