using UGS.Battle.Effects;
using UnityEngine;

namespace UGS.Battle.Units
{
    /// <summary>召唤请求。</summary>
    public sealed class SpawnRequest
    {
        /// <summary>召唤物类型 key（含义由上层定义）。</summary>
        public string Key;

        public int OwnerUnitId;

        public int FactionId;

        public Vector2 Position;

        /// <summary>上层附加配置（模板 Id、等级等）。</summary>
        public object Payload;

        /// <summary>来源效果（可用于把召唤物生命周期绑定到效果上）。</summary>
        public EffectInstance SourceEffect;
    }

    /// <summary>
    /// 召唤物生成器。由上层实现并注册到 <see cref="Core.BattleServiceRegistry.Spawner"/>。
    /// </summary>
    public interface IUnitSpawner
    {
        ICombatUnit Spawn(SpawnRequest request);

        void Despawn(ICombatUnit unit);
    }
}
