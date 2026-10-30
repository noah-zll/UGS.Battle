using NUnit.Framework;
using UGS.Battle.Attributes;
using UGS.Battle.Core;
using UGS.Battle.Skills;

namespace UGS.Battle.Tests
{
    /// <summary>
    /// 技能冷却策略测试：OnCast / OnDeactivate / Manual 与宿主控制接口。
    /// </summary>
    public class SimpleSkillStateTests
    {
        private TestUnit _unit;
        private int _hp;

        [SetUp]
        public void SetUp()
        {
            AttributeRegistry.Clear();
            BattleServiceRegistry.Default.Clear();

            _hp = AttributeRegistry.Define("hp", 100f, 0f, isResource: true).Id;
            _unit = new TestUnit(1, 1, _hp);
        }

        private static SimpleSkillDefinition NewDefinition(SkillCooldownMode mode, SkillTag tags = SkillTag.Active)
        {
            return new SimpleSkillDefinition
            {
                Id = 1,
                Key = "test.skill",
                Tags = tags,
                Cooldown = 5f,
                CooldownMode = mode,
            };
        }

        [Test]
        public void OnCast_ConsumesChargeAndBlocksUntilTick()
        {
            SimpleSkillState runtime = new SimpleSkillState(NewDefinition(SkillCooldownMode.OnCast), _unit);

            Assert.IsTrue(runtime.Cast(new SkillCastContext()).Success);
            Assert.AreEqual(0, runtime.Charges);
            Assert.AreEqual(5f, runtime.CooldownRemaining);

            SkillCastResult blocked = runtime.Cast(new SkillCastContext());
            Assert.IsFalse(blocked.Success);
            Assert.AreEqual(SkillCastFailure.OnCooldown, blocked.Failure);

            runtime.Tick(5f);
            Assert.AreEqual(1, runtime.Charges);
            Assert.IsTrue(runtime.Cast(new SkillCastContext()).Success);
        }

        [Test]
        public void OnDeactivate_OpenDoesNotStartCooldown_CloseDoes()
        {
            SimpleSkillDefinition definition = NewDefinition(SkillCooldownMode.OnDeactivate, SkillTag.Toggle);
            SimpleSkillState runtime = new SimpleSkillState(definition, _unit);

            // 打开：不消耗充能、不进冷却
            Assert.IsTrue(runtime.Cast(new SkillCastContext()).Success);
            Assert.IsTrue(runtime.ToggledOn);
            Assert.AreEqual(1, runtime.Charges);
            Assert.AreEqual(0f, runtime.CooldownRemaining);

            // 关闭：消耗充能并进冷却
            Assert.IsTrue(runtime.Cast(new SkillCastContext()).Success);
            Assert.IsFalse(runtime.ToggledOn);
            Assert.AreEqual(0, runtime.Charges);
            Assert.AreEqual(5f, runtime.CooldownRemaining);

            // 冷却中无法再次打开
            SkillCastResult blocked = runtime.Cast(new SkillCastContext());
            Assert.IsFalse(blocked.Success);
            Assert.AreEqual(SkillCastFailure.OnCooldown, blocked.Failure);

            runtime.Tick(5f);
            Assert.IsTrue(runtime.Cast(new SkillCastContext()).Success);
            Assert.IsTrue(runtime.ToggledOn);
        }

        [Test]
        public void OnDeactivate_NonToggleFallsBackToOnCast()
        {
            SimpleSkillState runtime = new SimpleSkillState(
                NewDefinition(SkillCooldownMode.OnDeactivate, SkillTag.Active), _unit);

            Assert.IsTrue(runtime.Cast(new SkillCastContext()).Success);
            Assert.AreEqual(0, runtime.Charges);
            Assert.AreEqual(5f, runtime.CooldownRemaining);
        }

        [Test]
        public void Manual_HostControlsCooldown()
        {
            SimpleSkillState runtime = new SimpleSkillState(NewDefinition(SkillCooldownMode.Manual), _unit);

            // 施法本身不消耗充能、不进冷却
            Assert.IsTrue(runtime.Cast(new SkillCastContext()).Success);
            Assert.IsTrue(runtime.Cast(new SkillCastContext()).Success);
            Assert.AreEqual(1, runtime.Charges);
            Assert.AreEqual(0f, runtime.CooldownRemaining);

            // 宿主主动进冷却
            runtime.StartCooldown(2f);
            Assert.AreEqual(0, runtime.Charges);
            Assert.AreEqual(2f, runtime.CooldownRemaining);

            Assert.IsFalse(runtime.CanCast(out SkillCastFailure failure));
            Assert.AreEqual(SkillCastFailure.OnCooldown, failure);

            runtime.Tick(2f);
            Assert.IsTrue(runtime.CanCast(out _));
        }

        [Test]
        public void SetCooldown_ChangesRemainingWithoutTouchingCharges()
        {
            SimpleSkillState runtime = new SimpleSkillState(NewDefinition(SkillCooldownMode.Manual), _unit);

            runtime.SetCooldown(3f);
            Assert.AreEqual(3f, runtime.CooldownRemaining);
            Assert.AreEqual(1, runtime.Charges);

            runtime.SetCooldown(-1f);
            Assert.AreEqual(0f, runtime.CooldownRemaining);
        }

        [Test]
        public void SetCaster_SwitchesValidationSource()
        {
            SimpleSkillState runtime = new SimpleSkillState(NewDefinition(SkillCooldownMode.OnCast), _unit);

            var deadUnit = new TestUnit(2, 1, _hp) { IsAlive = false };
            runtime.SetCaster(deadUnit);

            Assert.IsFalse(runtime.CanCast(out SkillCastFailure failure));
            Assert.AreEqual(SkillCastFailure.Dead, failure);

            runtime.SetCaster(_unit);
            Assert.IsTrue(runtime.CanCast(out _));
        }
    }
}

