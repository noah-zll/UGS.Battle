using UGS.Battle.Combat;
using UGS.Battle.Effects;
using UGS.Battle.Sessions;
using UGS.Battle.Units;

namespace UGS.Battle.Events
{
    /// <summary>事件派发级别。</summary>
    public enum BattleEventLevel
    {
        /// <summary>诊断级：默认不派发（属性变化、步骤日志等）。</summary>
        Diagnostic = 0,

        /// <summary>普通级：帧末批量派发（伤害、治疗、效果变化）。</summary>
        Normal = 1,

        /// <summary>关键级：立即派发（阶段变更、死亡、战斗结束）。</summary>
        Critical = 2,
    }

    /// <summary>战斗事件基类。SessionId / Time / Sequence 由事件总线自动填充。</summary>
    public abstract class BattleEvent
    {
        public int SessionId;
        public float Time;
        public long Sequence;
        public BattleEventLevel Level = BattleEventLevel.Normal;
    }

    public sealed class BattlePhaseChangedEvent : BattleEvent
    {
        public BattlePhase From;
        public BattlePhase To;

        public BattlePhaseChangedEvent()
        {
            Level = BattleEventLevel.Critical;
        }
    }

    public sealed class BattleEndedEvent : BattleEvent
    {
        public BattleEndReason Reason;
        public int WinnerFactionId;

        public BattleEndedEvent()
        {
            Level = BattleEventLevel.Critical;
        }
    }

    public sealed class DamageDealtEvent : BattleEvent
    {
        public ICombatUnit Attacker;
        public ICombatUnit Target;
        public DamageRequest Request;
        public DamageResult Result;
    }

    public sealed class UnitDiedEvent : BattleEvent
    {
        public ICombatUnit Unit;
        public ICombatUnit Killer;
        public object SourceTag;

        public UnitDiedEvent()
        {
            Level = BattleEventLevel.Critical;
        }
    }

    public sealed class UnitHealedEvent : BattleEvent
    {
        public ICombatUnit Unit;
        public ICombatUnit Healer;
        public float Amount;
    }

    public sealed class EffectAppliedEvent : BattleEvent
    {
        public ICombatUnit Unit;
        public EffectInstance Instance;
    }

    public sealed class EffectRemovedEvent : BattleEvent
    {
        public ICombatUnit Unit;
        public EffectInstance Instance;
        public EffectRemoveReason Reason;
    }

    public sealed class SkillCastEvent : BattleEvent
    {
        public ICombatUnit Caster;
        public int SkillId;
        public bool ToggledOn;
    }

    public sealed class AttributeChangedEvent : BattleEvent
    {
        public ICombatUnit Unit;
        public int AttributeId;
        public float OldValue;
        public float NewValue;

        public AttributeChangedEvent()
        {
            Level = BattleEventLevel.Diagnostic;
        }
    }
}
