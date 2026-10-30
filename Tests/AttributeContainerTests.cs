using NUnit.Framework;
using UGS.Battle.Attributes;
using UGS.Battle.Attributes.Formulas;
using UGS.Battle.Core;

namespace UGS.Battle.Tests
{
    public class AttributeContainerTests
    {
        private int _attack;
        private int _hp;
        private TestUnit _unit;

        [SetUp]
        public void SetUp()
        {
            AttributeRegistry.Clear();
            BattleServiceRegistry.Default.Clear();

            _hp = AttributeRegistry.Define("hp", 100f, 0f, isResource: true).Id;
            _attack = AttributeRegistry.Define("attack", 10f, 0f).Id;

            _unit = new TestUnit(1, 1, _hp);
        }

        [Test]
        public void AddAndMultiplyModifiers()
        {
            _unit.Attributes.AddModifier(new AttributeModifier("buff", ModifierOp.Add, 5f, _attack));
            _unit.Attributes.AddModifier(new AttributeModifier("buff2", ModifierOp.Multiply, 0.5f, _attack));

            Assert.AreEqual(22.5f, _unit.Attributes.Get(_attack), 0.001f);
        }

        [Test]
        public void OverrideTakesLastValue()
        {
            _unit.Attributes.AddModifier(new AttributeModifier("a", ModifierOp.Override, 30f, _attack));

            Assert.AreEqual(30f, _unit.Attributes.Get(_attack), 0.001f);
        }

        [Test]
        public void RemoveModifiersBySource()
        {
            object source = new object();
            _unit.Attributes.AddModifier(new AttributeModifier(source, ModifierOp.Add, 5f, _attack));
            Assert.AreEqual(15f, _unit.Attributes.Get(_attack), 0.001f);

            int removed = _unit.Attributes.RemoveModifiersFrom(source);

            Assert.AreEqual(1, removed);
            Assert.AreEqual(10f, _unit.Attributes.Get(_attack), 0.001f);
        }

        [Test]
        public void FormulaAppliesAfterAggregation()
        {
            BattleServiceRegistry.Default.RegisterAttributeFormula(_attack, new LinearAttributeFormula(2f, 0f));

            Assert.AreEqual(20f, _unit.Attributes.Get(_attack), 0.001f);
        }

        [Test]
        public void DependencyPropagatesDirty()
        {
            int reduction = AttributeRegistry.Define("reduction", 0f).Id;
            int headReduction = AttributeRegistry.Define("head_reduction", 0f).Id;
            AttributeRegistry.AddDependency(reduction, headReduction);

            BattleServiceRegistry.Default.RegisterAttributeFormula(headReduction,
                new HeadReductionFormula(reduction));

            Assert.AreEqual(0f, _unit.Attributes.Get(headReduction), 0.001f);

            _unit.Attributes.SetBase(reduction, 0.4f);

            Assert.AreEqual(0.4f, _unit.Attributes.Get(headReduction), 0.001f);
        }

        [Test]
        public void ClampRespectsDefinitionRange()
        {
            int cdr = AttributeRegistry.Define("cdr", 0f, 0f, 0.8f).Id;
            _unit.Attributes.AddModifier(new AttributeModifier("buff", ModifierOp.Add, 1.5f, cdr));

            Assert.AreEqual(0.8f, _unit.Attributes.Get(cdr), 0.001f);
        }

        private sealed class HeadReductionFormula : IAttributeFormula
        {
            private readonly int _globalReductionId;

            public HeadReductionFormula(int globalReductionId)
            {
                _globalReductionId = globalReductionId;
            }

            public int Order => 100;

            public float Apply(AttributeContainer container, int attributeId, float value)
            {
                return value + container.Get(_globalReductionId);
            }
        }
    }
}
