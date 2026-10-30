using UGS.Battle.Combat;
using UGS.Battle.Sessions;

namespace UGS.Battle.Effects.Behaviors
{
    /// <summary>
    /// 周期伤害行为（DoT）。
    /// 参数键：<c>amount</c>（每层每次伤害）。
    /// </summary>
    public sealed class PeriodicDamageBehavior : EffectBehaviorBase
    {
        private readonly float _amountPerStack;
        private readonly DamageKind _kind;

        public PeriodicDamageBehavior(IEffectDefinition definition)
        {
            _amountPerStack = definition.GetParameter("amount");
            _kind = definition.GetParameter("kind") >= 1f ? DamageKind.Magic : DamageKind.Physical;
        }

        public override void OnPeriod(EffectContext context)
        {
            BattleSession session = context.Session;
            if (session == null) return;

            float amount = _amountPerStack * context.Stacks;
            if (amount <= 0f) return;

            session.Damage.Process(new DamageRequest
            {
                Attacker = context.Source,
                Target = context.Owner,
                Amount = amount,
                Kind = _kind,
                Flags = DamageFlags.Periodic | DamageFlags.CannotCrit,
                SkillId = -1,
                SourceTag = context.Definition,
            }, session);
        }
    }
}
