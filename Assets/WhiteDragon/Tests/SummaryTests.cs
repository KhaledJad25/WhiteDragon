using NUnit.Framework;
using UnityEngine;

namespace WhiteDragon
{
    public class SummaryTests
    {
        [Test]
        public void ItemSummary_DescribesStatsEditsEffectsAndUnlock()
        {
            var effect = ScriptableObject.CreateInstance<HomingEffect>();
            effect.name = "Homing";
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.statModifiers = new[]
            {
                new StatModifier(StatType.Damage, ModifierKind.Flat, 1f),
                new StatModifier(StatType.FireRate, ModifierKind.PercentAdd, 0.25f),
                new StatModifier(StatType.Damage, ModifierKind.Multiply, 1.5f),
            };
            item.recipeEdits.projectileCountAdd = 2;
            item.recipeEdits.overrideDamageType = true;
            item.recipeEdits.damageType = DamageType.Fire;
            item.effects.Add(effect);
            item.effects.Add(null);
            item.requiredUnlockId = "beat_boss";

            Assert.AreEqual("+1 Damage, +25% FireRate, x1.5 Damage, +2 rocks, Fire damage, Homing, (missing effect), needs unlock 'beat_boss'",
                item.Summary());

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(effect);
        }

        [Test]
        public void EmptyItem_And_Synergy_Summaries()
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            Assert.AreEqual("no effect", item.Summary());

            var syn = ScriptableObject.CreateInstance<SynergyDefinition>();
            syn.tag = "rock";
            syn.requiredCount = 3;
            syn.recipeEdits.pierceAdd = 1;
            Assert.AreEqual("3x 'rock': +1 pierce", syn.Summary());

            Object.DestroyImmediate(item);
            Object.DestroyImmediate(syn);
        }
    }
}
