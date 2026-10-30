using UGS.Battle.Attributes;
using UGS.Battle.Combat;
using UGS.Battle.Effects;
using UGS.Battle.Sessions;
using UnityEngine;

namespace UGS.Battle.Units
{
    /// <summary>
    /// 可参与战斗的单位。包内不暴露 GameObject / Transform / 动画，
    /// 位置为快照值，由上层在移动后同步。
    /// </summary>
    public interface ICombatUnit
    {
        int UnitId { get; }

        string UnitName { get; }

        /// <summary>所属阵营 Id（与 <see cref="Factions.FactionSystem"/> 对应）。</summary>
        int FactionId { get; }

        /// <summary>兵种/类型标记，由上层自定义含义（包内不预设任何兵种枚举）。</summary>
        int UnitTypeId { get; }

        bool IsAlive { get; }

        /// <summary>能否行动（未死亡、未被硬控、未逃跑）。</summary>
        bool CanAct { get; }

        bool CanBeTargeted { get; }

        Vector2 Position { get; }

        float Radius { get; }

        AttributeContainer Attributes { get; }

        EffectContainer Effects { get; }

        ResourceContainer Resources { get; }

        /// <summary>主资源属性 Id（由上层定义，如 "hp"）。伤害默认扣减该资源。</summary>
        int PrimaryResourceAttributeId { get; }

        /// <summary>所属战斗会话（未注册时可为 null）。</summary>
        BattleSession Session { get; }

        /// <summary>由 <see cref="Combat.Processors"/> 的 ApplyDamageProcessor 调用；实现方只落资源与判定存活。</summary>
        void ApplyDamage(in DamageResult result);

        void ApplyHealing(float amount);

        /// <summary>
        /// 伤害重定向钩子：返回 true 表示本次伤害已被本方法接管（如"伤害转移到坐骑"）。
        /// </summary>
        bool TryRedirectDamage(in DamageRequest request);

        void OnEffectApplied(EffectInstance instance);

        void OnEffectRemoved(EffectInstance instance, EffectRemoveReason reason);
    }
}
