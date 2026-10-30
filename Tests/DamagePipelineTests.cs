using NUnit.Framework;
using UGS.Battle.Attributes;
using UGS.Battle.Combat;
using UGS.Battle.Combat.Processors;
using UGS.Battle.Core;
using UGS.Battle.Effects;
using UGS.Battle.Effects.Behaviors;
using UGS.Battle.Sessions;

namespace UGS.Battle.Tests
{
    public class DamagePipelineTests
    {
        private int _hp;
        private BattleSession _session;
        private TestUnit _attacker;
        private TestUnit _target;

        [SetUp]
        public void SetUp()
        {
            AttributeRegistry.Clear();
            BattleServiceRegistry.Default.Clear();

            _hp = AttributeRegistry.Define("hp", 100f, 0f, isResource: true).Id;

            _session = new BattleSession(new BattleRules { RandomSeed = 7UL });

            _attacker = new TestUnit(1, 1, _hp);
            _target = new TestUnit(2, 2, _hp);
            _attacker.Bind(_session);
            _target.Bind(_session);

            _session.RegisterUnit(1, _attacker);
            _session.RegisterUnit(2, _target);
            _target.Resources.SetCurrent(_hp, 100f);
        }

        [Test]
        public void BasicDamageReducesResource()
        {
            DamageResult result = _session.Damage.Process(new DamageRequest
            {
                Attacker = _attacker,
                Target = _target,
                Amount = 30f,
                Kind = DamageKind.Physical,
            }, _session);

            Assert.AreEqual(30f, result.FinalAmount, 0.001f);
            Assert.AreEqual(70f, _target.Resources.GetCurrent(_hp), 0.001f);
        }

        [Test]
        public void MissDealsNoDamage()
        {
            var payload = new TestPayload { Hit = 0f };

            DamageResult result = _session.Damage.Process(new DamageRequest
            {
                Attacker = _attacker,
                Target = _target,
                Amount = 30f,
                Payload = payload,
            }, _session);

            Assert.IsTrue(result.Missed);
            Assert.AreEqual(0f, result.FinalAmount, 0.001f);
            Assert.AreEqual(100f, _target.Resources.GetCurrent(_hp), 0.001f);
        }

        [Test]
        public void CriticalMultipliesDamage()
        {
            var payload = new TestPayload { Hit = 1f, Crit = 1f, CritMultiplier = 2f };

            DamageResult result = _session.Damage.Process(new DamageRequest
            {
                Attacker = _attacker,
                Target = _target,
                Amount = 20f,
                Payload = payload,
            }, _session);

            Assert.IsTrue(result.Critical);
            Assert.AreEqual(40f, result.FinalAmount, 0.001f);
        }

        [Test]
        public void MitigationReducesDamage()
        {
            var payload = new TestPayload { Hit = 1f, Mitigation = 0.25f };

            DamageResult result = _session.Damage.Process(new DamageRequest
            {
                Attacker = _attacker,
                Target = _target,
                Amount = 40f,
                Payload = payload,
            }, _session);

            Assert.AreEqual(30f, result.FinalAmount, 0.001f);
        }

        [Test]
        public void ShieldAbsorbsDamage()
        {
            BattleServiceRegistry.Default.RegisterEffectBehavior("shield", d => new ShieldBehavior(d));

            var shield = new SimpleEffectDefinition
            {
                Id = 900,
                Key = "shield",
                Duration = 10f,
                Tags = EffectTag.Shield,
                BehaviorKeys = { "shield" },
            };
            shield.Parameters["amount"] = 15f;

            _target.Effects.Apply(shield, _attacker);

            DamageResult result = _session.Damage.Process(new DamageRequest
            {
                Attacker = _attacker,
                Target = _target,
                Amount = 40f,
            }, _session);

            Assert.AreEqual(15f, result.Absorbed, 0.001f);
            Assert.AreEqual(25f, result.FinalAmount, 0.001f);
            Assert.AreEqual(75f, _target.Resources.GetCurrent(_hp), 0.001f);
        }

        [Test]
        public void LethalDamageKillsUnitAndRaisesEvent()
        {
            bool died = false;
            _session.Events.Subscribe<Events.UnitDiedEvent>(_ => died = true);

            _session.Damage.Process(new DamageRequest
            {
                Attacker = _attacker,
                Target = _target,
                Amount = 200f,
            }, _session);

            _session.Events.Flush();

            Assert.IsFalse(_target.IsAlive);
            Assert.IsTrue(died, "应派发 UnitDiedEvent");
        }

        [Test]
        public void ProcessorsRunInOrder()
        {
            _session.Damage.Clear();
            var recorder = new OrderRecorder();

            _session.Damage.Add(recorder.Create(50, "b"));
            _session.Damage.Add(recorder.Create(10, "a"));
            _session.Damage.Add(recorder.Create(100, "c"));

            _session.Damage.Process(new DamageRequest
            {
                Attacker = _attacker,
                Target = _target,
                Amount = 10f,
            }, _session);

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, recorder.Names);
        }

        private sealed class OrderRecorder
        {
            public readonly System.Collections.Generic.List<string> Names =
                new System.Collections.Generic.List<string>();

            public IDamageProcessor Create(int order, string name) => new Recorder(this, order, name);

            private sealed class Recorder : IDamageProcessor
            {
                private readonly OrderRecorder _owner;
                private readonly string _name;

                public Recorder(OrderRecorder owner, int order, string name)
                {
                    _owner = owner;
                    Order = order;
                    _name = name;
                }

                public int Order { get; }
                public bool Enabled => true;
                public string Name => _name;

                public void Process(DamageContext context) => _owner.Names.Add(_name);
            }
        }
    }
}
