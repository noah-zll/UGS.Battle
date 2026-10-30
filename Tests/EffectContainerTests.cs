using NUnit.Framework;
using UGS.Battle.Attributes;
using UGS.Battle.Core;
using UGS.Battle.Effects;
using UGS.Battle.Effects.Behaviors;
using UGS.Battle.Sessions;

namespace UGS.Battle.Tests
{
    public class EffectContainerTests
    {
        private const string CounterKey = "counter";

        private int _hp;
        private BattleSession _session;
        private TestUnit _unit;

        private sealed class CounterBehavior : EffectBehaviorBase
        {
            public int Periods;

            public override void OnPeriod(EffectContext context) => Periods++;
        }

        [SetUp]
        public void SetUp()
        {
            AttributeRegistry.Clear();
            BattleServiceRegistry.Default.Clear();
            BattleServiceRegistry.Default.RegisterEffectBehavior(CounterKey, _ => new CounterBehavior());

            _hp = AttributeRegistry.Define("hp", 100f, 0f, isResource: true).Id;

            _session = new BattleSession(new BattleRules { RandomSeed = 1UL });
            _unit = new TestUnit(1, 1, _hp);
            _unit.Bind(_session);
            _session.RegisterUnit(1, _unit);
        }

        [TearDown]
        public void TearDown()
        {
            BattleServiceRegistry.Default.Clear();
        }

        [Test]
        public void StackRuleAccumulatesStacks()
        {
            var def = new SimpleEffectDefinition
            {
                Id = 101,
                Key = "stackable",
                Duration = 5f,
                MaxStacks = 3,
                StackRule = StackRule.Stack,
            };

            _unit.Effects.Apply(def, null);
            _unit.Effects.Apply(def, null);
            _unit.Effects.Apply(def, null);
            _unit.Effects.Apply(def, null);

            Assert.AreEqual(1, _unit.Effects.Count);
            Assert.AreEqual(3, _unit.Effects.GetStacks(def.Id));
        }

        [Test]
        public void ExpiresAfterDuration()
        {
            var def = new SimpleEffectDefinition { Id = 102, Key = "timed", Duration = 1f };
            _unit.Effects.Apply(def, null);

            Assert.AreEqual(1, _unit.Effects.Count);

            _unit.Effects.Tick(0.6f);
            Assert.AreEqual(1, _unit.Effects.Count);

            _unit.Effects.Tick(0.6f);
            Assert.AreEqual(0, _unit.Effects.Count);
        }

        [Test]
        public void PermanentEffectNeverExpires()
        {
            var def = new SimpleEffectDefinition { Id = 103, Key = "aura" };
            _unit.Effects.Apply(def, null);

            _unit.Effects.Tick(1000f);

            Assert.AreEqual(1, _unit.Effects.Count);
        }

        [Test]
        public void RefreshResetsDuration()
        {
            var def = new SimpleEffectDefinition
            {
                Id = 104,
                Key = "refresh",
                Duration = 2f,
                StackRule = StackRule.Refresh,
            };

            _unit.Effects.Apply(def, null);
            _unit.Effects.Tick(1.5f);
            _unit.Effects.Apply(def, null);

            Assert.AreEqual(1, _unit.Effects.Count);

            _unit.Effects.Tick(1.5f);
            Assert.AreEqual(1, _unit.Effects.Count, "刷新后应重新计时");

            _unit.Effects.Tick(1f);
            Assert.AreEqual(0, _unit.Effects.Count);
        }

        [Test]
        public void PeriodicBehaviorTriggersAtInterval()
        {
            var def = new SimpleEffectDefinition
            {
                Id = 105,
                Key = "dot",
                Duration = 3f,
                Period = 0.5f,
            };
            def.BehaviorKeys.Add(CounterKey);

            _unit.Effects.Apply(def, null);
            _unit.Effects.Tick(0.5f);

            var behavior = _unit.Effects.GetFirstBehavior<CounterBehavior>();
            Assert.IsNotNull(behavior);
            Assert.AreEqual(1, behavior.Periods);

            _unit.Effects.Tick(1f);
            Assert.AreEqual(3, behavior.Periods);
        }

        [Test]
        public void RemoveByTagDispels()
        {
            var def = new SimpleEffectDefinition
            {
                Id = 106,
                Key = "debuff",
                Duration = 10f,
                Tags = EffectTag.Debuff | EffectTag.Dispellable,
            };

            _unit.Effects.Apply(def, null);
            int removed = _unit.Effects.RemoveByTag(EffectTag.Debuff);

            Assert.AreEqual(1, removed);
            Assert.AreEqual(0, _unit.Effects.Count);
        }

        [Test]
        public void AttributeModifierBehaviorAppliesOnStack()
        {
            int attack = AttributeRegistry.Define("attack", 10f, 0f).Id;

            var def = new SimpleEffectDefinition
            {
                Id = 107,
                Key = "buff",
                Duration = 5f,
                MaxStacks = 2,
                StackRule = StackRule.Stack,
                BehaviorKeys = { "attr" },
                Payload = new AttributeModifierPayload
                {
                    Op = ModifierOp.Add,
                    AttributeIds = new[] { attack },
                    ValuesPerStack = new[] { 5f },
                },
            };

            BattleServiceRegistry.Default.RegisterEffectBehavior("attr", d => new AttributeModifierBehavior(d));

            _unit.Effects.Apply(def, null);
            Assert.AreEqual(15f, _unit.Attributes.Get(attack), 0.001f);

            _unit.Effects.Apply(def, null);
            Assert.AreEqual(20f, _unit.Attributes.Get(attack), 0.001f);

            _unit.Effects.Remove(def.Id, EffectRemoveReason.Manual);
            Assert.AreEqual(10f, _unit.Attributes.Get(attack), 0.001f);
        }
    }
}
