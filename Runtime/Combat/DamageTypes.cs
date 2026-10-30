using System;
using System.Collections.Generic;
using UGS.Battle.Units;

namespace UGS.Battle.Combat
{
    /// <summary>伤害类型。上层可扩展语义（如"毒""真元"），管线只按 Kind 分发抑制逻辑。</summary>
    public enum DamageKind
    {
        Physical = 0,
        Magic = 1,
        True = 2,
    }

    [Flags]
    public enum DamageFlags
    {
        None = 0,
        CannotCrit = 1 << 0,
        CannotBeBlocked = 1 << 1,
        IgnoreShield = 1 << 2,
        IgnoreMitigation = 1 << 3,
        Periodic = 1 << 4,
        Reflected = 1 << 5,
    }

    /// <summary>
    /// 伤害载荷契约。由上层实现，承载命中率 / 暴击 / 减伤等玩法参数，
    /// 使包内管线无需认识任何具体属性。
    /// </summary>
    public interface IDamagePayload
    {
        /// <summary>最终命中率 [0,1]；&lt;0 表示跳过命中判定。</summary>
        float HitChance { get; }

        /// <summary>暴击率 [0,1]；&lt;0 表示跳过暴击判定。</summary>
        float CriticalChance { get; }

        /// <summary>暴击倍率（1 表示无加成）。</summary>
        float CriticalMultiplier { get; }

        /// <summary>目标侧减伤率 [0,1]，由上层根据护甲/穿透/部位计算。</summary>
        float MitigationRate { get; }
    }

    /// <summary>一次伤害请求。</summary>
    public struct DamageRequest
    {
        public ICombatUnit Attacker;
        public ICombatUnit Target;

        /// <summary>基础伤害值。</summary>
        public float Amount;

        public DamageKind Kind;
        public DamageFlags Flags;

        /// <summary>来源技能 Id；&lt;0 表示普通攻击。</summary>
        public int SkillId;

        /// <summary>上层扩展载荷（实现 <see cref="IDamagePayload"/> 才会参与命中/暴击/减伤）。</summary>
        public object Payload;

        /// <summary>用于事件溯源的来源标记（技能定义、效果定义等）。</summary>
        public object SourceTag;
    }

    /// <summary>管线中的一步记录（仅诊断编译下填充）。</summary>
    public struct DamageStep
    {
        public string Processor;
        public float Before;
        public float After;
        public string Note;
    }

    /// <summary>伤害结算结果。</summary>
    public struct DamageResult
    {
        public float FinalAmount;

        /// <summary>被护盾/格挡吸收的量。</summary>
        public float Absorbed;

        /// <summary>是否完全被挡下（含命中失败）。</summary>
        public bool Blocked;

        public bool Missed;

        public bool Critical;

        public bool Killed;

        /// <summary>管线步骤；仅在 BATTLE_DIAGNOSTICS 宏下非 null。</summary>
        public List<DamageStep> Steps;
    }
}
