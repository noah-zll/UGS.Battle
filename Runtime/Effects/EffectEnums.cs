using System;

namespace UGS.Battle.Effects
{
    /// <summary>效果标签，用于驱散、UI 分类与筛选。</summary>
    [Flags]
    public enum EffectTag
    {
        None = 0,
        Buff = 1 << 0,
        Debuff = 1 << 1,
        Control = 1 << 2,
        DamageOverTime = 1 << 3,
        HealOverTime = 1 << 4,
        Shield = 1 << 5,
        Dispellable = 1 << 6,
        Undispellable = 1 << 7,
        Aura = 1 << 8,
    }

    /// <summary>叠加规则。</summary>
    public enum StackRule
    {
        /// <summary>允许多个实例共存，各自独立计时。</summary>
        Independent = 0,

        /// <summary>复用实例并重置时长，层数不变。</summary>
        Refresh = 1,

        /// <summary>复用实例，层数累加（上限 MaxStacks），时长重置。</summary>
        Stack = 2,

        /// <summary>复用实例，延长时长。</summary>
        Extend = 3,

        /// <summary>已存在则忽略新的施加。</summary>
        Ignore = 4,
    }

    /// <summary>效果移除原因。</summary>
    public enum EffectRemoveReason
    {
        Expired = 0,
        Dispelled = 1,
        Replaced = 2,
        OwnerDied = 3,
        Manual = 4,
    }
}
