using System;
using System.Collections.Generic;
using UGS.Battle.Units;
using UnityEngine;

namespace UGS.Battle.Core
{
    /// <summary>
    /// 2D 均匀网格空间索引。格边长建议取 ≈ 2× 最大攻击范围。
    /// </summary>
    public sealed class UniformGridSpatialIndex : ISpatialIndex
    {
        private readonly float _cellSize;
        private readonly float _invCellSize;
        private readonly Dictionary<long, List<ICombatUnit>> _cells = new Dictionary<long, List<ICombatUnit>>();

        private IReadOnlyList<ICombatUnit> _units;
        private int _count;

        public UniformGridSpatialIndex(float cellSize = 12f)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));

            _cellSize = cellSize;
            _invCellSize = 1f / cellSize;
        }

        public int Count => _count;

        public void Rebuild(IReadOnlyList<ICombatUnit> units)
        {
            Clear();
            _units = units;

            if (units == null) return;

            for (int i = 0; i < units.Count; i++)
            {
                ICombatUnit unit = units[i];
                if (unit == null || !unit.IsAlive) continue;

                long key = CellKey(CellX(unit.Position.x), CellY(unit.Position.y));
                if (!_cells.TryGetValue(key, out var list))
                {
                    list = new List<ICombatUnit>(8);
                    _cells[key] = list;
                }

                list.Add(unit);
                _count++;
            }
        }

        public void Clear()
        {
            _cells.Clear();
            _units = null;
            _count = 0;
        }

        public int Query(Vector2 center, float radius, int factionId, List<ICombatUnit> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            results.Clear();
            if (_count == 0 || radius <= 0f) return 0;

            float radiusSqr = radius * radius;

            int minX = CellX(center.x - radius);
            int maxX = CellX(center.x + radius);
            int minY = CellY(center.y - radius);
            int maxY = CellY(center.y + radius);

            for (int cx = minX; cx <= maxX; cx++)
            {
                for (int cy = minY; cy <= maxY; cy++)
                {
                    if (!_cells.TryGetValue(CellKey(cx, cy), out var list)) continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        ICombatUnit unit = list[i];
                        if (unit == null || !unit.IsAlive) continue;
                        if (factionId >= 0 && unit.FactionId != factionId) continue;

                        if ((unit.Position - center).sqrMagnitude <= radiusSqr)
                        {
                            results.Add(unit);
                        }
                    }
                }
            }

            return results.Count;
        }

        public ICombatUnit FindNearest(Vector2 from, int factionId, float maxRadius)
        {
            if (_count == 0 || maxRadius <= 0f) return null;

            ICombatUnit best = null;
            float bestSqr = maxRadius * maxRadius;

            int maxRing = Mathf.CeilToInt(maxRadius * _invCellSize) + 1;
            int centerX = CellX(from.x);
            int centerY = CellY(from.y);

            // 由内向外环形扩展，找到后多扩一圈再收敛
            for (int ring = 0; ring <= maxRing; ring++)
            {
                bool visitedAny = false;

                for (int cx = centerX - ring; cx <= centerX + ring; cx++)
                {
                    for (int cy = centerY - ring; cy <= centerY + ring; cy++)
                    {
                        // 只取当前环上的格子
                        if (ring > 0
                            && cx > centerX - ring && cx < centerX + ring
                            && cy > centerY - ring && cy < centerY + ring)
                        {
                            continue;
                        }

                        if (!_cells.TryGetValue(CellKey(cx, cy), out var list)) continue;

                        visitedAny = true;
                        for (int i = 0; i < list.Count; i++)
                        {
                            ICombatUnit unit = list[i];
                            if (unit == null || !unit.IsAlive) continue;
                            if (factionId >= 0 && unit.FactionId != factionId) continue;

                            float sqr = (unit.Position - from).sqrMagnitude;
                            if (sqr < bestSqr)
                            {
                                bestSqr = sqr;
                                best = unit;
                            }
                        }
                    }
                }

                // 已找到候选且后续环的最小可能距离都更大时收敛
                if (best != null && !visitedAny) break;
                if (best != null)
                {
                    float ringMinDistance = (ring + 1) * _cellSize;
                    if (ringMinDistance * ringMinDistance > bestSqr) break;
                }
            }

            return best;
        }

        private int CellX(float x) => Mathf.FloorToInt(x * _invCellSize);

        private int CellY(float y) => Mathf.FloorToInt(y * _invCellSize);

        private static long CellKey(int cx, int cy) => ((long)cx << 32) ^ (uint)cy;
    }
}
