using System.Collections.Generic;
using NUnit.Framework;
using UGS.Battle.Attributes;
using UGS.Battle.Combat;
using UGS.Battle.Core;
using UGS.Battle.Factions;
using UGS.Battle.Sessions;

namespace UGS.Battle.Tests
{
    public class BattleSessionTests
    {
        private int _hp;
        private BattleSession _session;
        private TestUnit _ally;
        private TestUnit _enemy;

        [SetUp]
        public void SetUp()
        {
            AttributeRegistry.Clear();
            BattleServiceRegistry.Default.Clear();

            _hp = AttributeRegistry.Define("hp", 100f, 0f, isResource: true).Id;

            _session = new BattleSession(new BattleRules
            {
                TickMode = BattleTickMode.Realtime,
                RandomSeed = 42UL,
                MinDamage = 1f,
            });

            _session.Factions.CreateFaction(1);
            _session.Factions.CreateFaction(2);
            _session.Factions.SetRelation(1, 2, FactionRelation.Hostile);

            _ally = new TestUnit(10, 1, _hp);
            _enemy = new TestUnit(20, 2, _hp);
            _ally.Bind(_session);
            _enemy.Bind(_session);

            _session.RegisterUnit(1, _ally);
            _session.RegisterUnit(2, _enemy);
        }

        [Test]
        public void PhaseTransitionsFollowRules()
        {
            Assert.AreEqual(BattlePhase.Created, _session.Phase);

            _session.Start();
            Assert.AreEqual(BattlePhase.Preparation, _session.Phase);

            _session.SetReady();
            Assert.AreEqual(BattlePhase.InProgress, _session.Phase);
        }

        [Test]
        public void RelationsDecideHostility()
        {
            Assert.IsTrue(_session.Factions.IsHostile(1, 2));
            Assert.IsFalse(_session.Factions.IsHostile(1, 1));

            _session.Factions.SetRelation(1, 2, FactionRelation.Allied);
            Assert.IsFalse(_session.Factions.IsHostile(1, 2));
        }

        [Test]
        public void VictoryWhenEnemyFactionDefeated()
        {
            _session.Start();
            _session.SetReady();

            BattleEndReason reason = BattleEndReason.Aborted;
            int winner = 0;
            _session.Ended += (r, w) =>
            {
                reason = r;
                winner = w;
            };

            _session.Damage.Process(new DamageRequest
            {
                Attacker = _ally,
                Target = _enemy,
                Amount = 500f,
            }, _session);

            _session.Tick(0.016f);

            Assert.AreEqual(BattlePhase.Ended, _session.Phase);
            Assert.AreEqual(BattleEndReason.Victory, reason);
            Assert.AreEqual(1, winner);
        }

        [Test]
        public void SpatialQueryFiltersHostiles()
        {
            _ally.Position = new UnityEngine.Vector2(0f, 0f);
            _enemy.Position = new UnityEngine.Vector2(3f, 0f);

            var buffer = new List<Units.ICombatUnit>(4);
            int count = _session.QueryHostileInRange(1, UnityEngine.Vector2.zero, 5f, buffer);

            Assert.AreEqual(1, count);
            Assert.AreSame(_enemy, buffer[0]);

            int farCount = _session.QueryHostileInRange(1, UnityEngine.Vector2.zero, 1f, buffer);
            Assert.AreEqual(0, farCount);
        }

        [Test]
        public void SnapshotRoundTrip()
        {
            _enemy.Resources.SetCurrent(_hp, 42f);

            BattleSnapshot snapshot = _session.CaptureSnapshot();
            _enemy.Resources.SetCurrent(_hp, 10f);

            _session.RestoreSnapshot(snapshot);

            Assert.AreEqual(42f, _enemy.Resources.GetCurrent(_hp), 0.001f);
        }
    }
}
