namespace UGS.Battle.Skills
{
    /// <summary>
    /// 技能实例状态（每拥有者一份）。只放"无法由配置推导"的变化量：充能、剩余冷却、开关、施法次数。
    /// </summary>
    /// <remarks>
    /// 刻意不放 <see cref="ISkillDefinition"/> / 施法者：Tick 与施法时由调用方传入，
    /// 这样存档里不会出现配置行与单位引用（见 <see cref="SkillStateMachine"/>）。
    /// <para>
    /// 写入只走方法：<see cref="SetCharges"/> / <see cref="SetCooldown"/> / <see cref="SetToggled"/> /
    /// <see cref="AddCastCount"/>，保证 <see cref="SkillStateMachine"/> 是唯一修改者。
    /// </para>
    /// </remarks>
    public interface ISkillState
    {
        int Charges { get; }

        float CooldownRemaining { get; }

        bool ToggledOn { get; }

        int CastCount { get; }

        /// <summary>冷却缩减系数（1 = 无缩减）。由实例提供，配置不被改写。</summary>
        float CooldownMultiplier { get; }

        void SetCharges(int charges);

        void SetCooldown(float remaining);

        void SetToggled(bool on);

        void AddCastCount(int delta);
    }
}
