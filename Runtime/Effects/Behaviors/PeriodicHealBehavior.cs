namespace UGS.Battle.Effects.Behaviors
{
    /// <summary>
    /// 周期治疗行为（HoT）。
    /// 参数键：<c>amount</c>（每层每次治疗量）。
    /// </summary>
    public sealed class PeriodicHealBehavior : EffectBehaviorBase
    {
        private readonly float _amountPerStack;

        public PeriodicHealBehavior(IEffectDefinition definition)
        {
            _amountPerStack = definition.GetParameter("amount");
        }

        public override void OnPeriod(EffectContext context)
        {
            float amount = _amountPerStack * context.Stacks;
            if (amount <= 0f) return;

            context.Owner?.ApplyHealing(amount);
        }
    }
}
