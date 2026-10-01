using System.Collections.Generic;
using NUnit.Framework;

namespace WhiteDragon
{
    public class StatBlockTests
    {
        [Test]
        public void Defaults_MatchDesign()
        {
            var s = new StatBlock();
            Assert.AreEqual(5f, s.Get(StatType.MoveSpeed));
            Assert.AreEqual(1.2f, s.Get(StatType.JumpHeight), 1e-5f);
            Assert.AreEqual(1f, s.Get(StatType.CharacterSize));
            Assert.AreEqual(2f, s.Get(StatType.FireRate));
            Assert.AreEqual(3.5f, s.Get(StatType.Damage));
            Assert.AreEqual(18f, s.Get(StatType.ProjectileSpeed));
            Assert.AreEqual(20f, s.Get(StatType.Range));
            Assert.AreEqual(0f, s.Get(StatType.Luck));
        }

        [Test]
        public void Formula_FlatThenPercentThenMultiply()
        {
            var s = new StatBlock();
            object a = new object(), b = new object();
            s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.Flat, 1.5f), a);
            s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.Flat, 1f), b);
            s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.PercentAdd, 0.25f), a);
            s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.PercentAdd, 0.25f), b);
            s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.Multiply, 2f), a);
            s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.Multiply, 0.5f), b);
            // (3.5 + 2.5) x (1 + 0.5) x (2 x 0.5) = 9
            Assert.AreEqual(9f, s.Get(StatType.Damage), 1e-4f);
        }

        [Test]
        public void Multiply_Stacks()
        {
            var s = new StatBlock();
            object a = new object();
            s.AddModifier(new StatModifier(StatType.FireRate, ModifierKind.Multiply, 2f), a);
            s.AddModifier(new StatModifier(StatType.FireRate, ModifierKind.Multiply, 1.5f), a);
            Assert.AreEqual(6f, s.Get(StatType.FireRate), 1e-4f);
        }

        [Test]
        public void RemoveBySource_RestoresAndLeavesOthers()
        {
            var s = new StatBlock();
            object a = new object(), b = new object();
            s.AddModifier(new StatModifier(StatType.MoveSpeed, ModifierKind.Flat, 2f), a);
            s.AddModifier(new StatModifier(StatType.MoveSpeed, ModifierKind.Flat, 1f), b);
            Assert.AreEqual(1, s.RemoveModifiersFromSource(a));
            Assert.AreEqual(6f, s.Get(StatType.MoveSpeed), 1e-4f);
            s.RemoveModifiersFromSource(b);
            Assert.AreEqual(5f, s.Get(StatType.MoveSpeed), 1e-4f);
            Assert.AreEqual(0, s.ModifierCount);
            Assert.AreEqual(0, s.RemoveModifiersFromSource(new object()));
        }

        [Test]
        public void ModifiersOnlyAffectTheirStat()
        {
            var s = new StatBlock();
            s.AddModifier(new StatModifier(StatType.Luck, ModifierKind.Flat, 3f), this);
            Assert.AreEqual(3f, s.Get(StatType.Luck));
            Assert.AreEqual(3.5f, s.Get(StatType.Damage));
        }

        [Test]
        public void Changed_FiresPerAffectedStat()
        {
            var s = new StatBlock();
            var fired = new List<StatType>();
            s.Changed += fired.Add;
            object a = new object();
            s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.Flat, 1f), a);
            s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.PercentAdd, 1f), a);
            s.AddModifier(new StatModifier(StatType.Range, ModifierKind.Flat, 1f), a);
            CollectionAssert.AreEqual(new[] { StatType.Damage, StatType.Damage, StatType.Range }, fired);

            fired.Clear();
            s.RemoveModifiersFromSource(a);
            CollectionAssert.AreEquivalent(new[] { StatType.Damage, StatType.Range }, fired);

            fired.Clear();
            s.SetBase(StatType.Luck, 2f);
            CollectionAssert.AreEqual(new[] { StatType.Luck }, fired);
            Assert.AreEqual(2f, s.Get(StatType.Luck));
        }

        [Test]
        public void NullSource_Throws()
        {
            var s = new StatBlock();
            Assert.Throws<System.ArgumentNullException>(() =>
                s.AddModifier(new StatModifier(StatType.Damage, ModifierKind.Flat, 1f), null));
        }
    }
}
