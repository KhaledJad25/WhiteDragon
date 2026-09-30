using NUnit.Framework;
using UnityEngine;

public class ItemLoadoutTests
{
    StatBlock stats;
    ItemLoadout loadout;

    [SetUp]
    public void SetUp()
    {
        stats = new StatBlock();
        stats.SetBase(StatType.Damage, 10f);
        stats.SetBase(StatType.ProjectileSpeed, 18f);
        stats.SetBase(StatType.Range, 20f);
        stats.SetBase(StatType.FireRate, 2f);
        loadout = new ItemLoadout(stats);
    }

    static ItemDefinition MakeItem(string id, params string[] tags)
    {
        ItemDefinition i = ScriptableObject.CreateInstance<ItemDefinition>();
        i.id = id;
        i.displayName = id;
        i.tags = tags;
        return i;
    }

    static ItemDefinition.StatEntry Entry(StatType stat, ModifierKind kind, float value)
    {
        ItemDefinition.StatEntry e = new ItemDefinition.StatEntry();
        e.stat = stat;
        e.kind = kind;
        e.value = value;
        return e;
    }

    static SynergyDefinition MakeSynergy(string tag, int required, float damagePercent)
    {
        SynergyDefinition s = ScriptableObject.CreateInstance<SynergyDefinition>();
        s.id = "syn_" + tag;
        s.displayName = s.id;
        s.tag = tag;
        s.requiredCount = required;
        s.statModifiers = new[] { Entry(StatType.Damage, ModifierKind.PercentAdd, damagePercent) };
        return s;
    }

    [Test]
    public void Item_StatModifier_AppliesAndRemoves()
    {
        ItemDefinition item = MakeItem("a");
        item.statModifiers = new[] { Entry(StatType.Damage, ModifierKind.PercentAdd, 0.5f) };

        loadout.Add(item);
        Assert.AreEqual(15f, stats.Get(StatType.Damage), 0.001f);

        loadout.RemoveLast();
        Assert.AreEqual(10f, stats.Get(StatType.Damage), 0.001f);
    }

    [Test]
    public void Items_StackAdditively()
    {
        for (int n = 0; n < 2; n++)
        {
            ItemDefinition item = MakeItem("a" + n);
            item.statModifiers = new[] { Entry(StatType.Damage, ModifierKind.PercentAdd, 0.25f) };
            loadout.Add(item);
        }

        Assert.AreEqual(15f, stats.Get(StatType.Damage), 0.001f);
    }

    [Test]
    public void Clear_RestoresBaseStats()
    {
        ItemDefinition item = MakeItem("a");
        item.statModifiers = new[] { Entry(StatType.Damage, ModifierKind.Multiply, 3f) };
        loadout.Add(item);
        loadout.Clear();

        Assert.AreEqual(10f, stats.Get(StatType.Damage), 0.001f);
        Assert.AreEqual(0, loadout.Count);
    }

    [Test]
    public void RecipeEdits_AddProjectiles_AndBurn()
    {
        ItemDefinition twin = MakeItem("twin");
        twin.projectileCountAdd = 1;
        ItemDefinition fire1 = MakeItem("f1");
        fire1.burnDpsAdd = 2f;
        fire1.burnDurationAdd = 3f;
        ItemDefinition fire2 = MakeItem("f2");
        fire2.burnDpsAdd = 1f;

        loadout.Add(twin);
        loadout.Add(fire1);
        loadout.Add(fire2);

        ShotRecipe r = ShotRecipe.FromStats(stats);
        loadout.ModifyRecipe(r);

        Assert.AreEqual(2, r.Count);
        Assert.AreEqual(3f, r.BurnDps, 0.001f);
        Assert.AreEqual(3f, r.BurnDuration, 0.001f);
    }

    [Test]
    public void Recipe_IsClampedToCaps()
    {
        for (int n = 0; n < 30; n++)
        {
            ItemDefinition item = MakeItem("spam" + n);
            item.projectileCountAdd = 1;
            item.pierceAdd = 1;
            item.homingAdd = 100f;
            loadout.Add(item);
        }

        ShotRecipe r = ShotRecipe.FromStats(stats);
        loadout.ModifyRecipe(r);
        r.ClampToCaps();

        Assert.AreEqual(ShotRecipe.MaxCount, r.Count);
        Assert.AreEqual(ShotRecipe.MaxPierce, r.Pierce);
        Assert.AreEqual(360f, r.Homing, 0.001f);
    }

    [Test]
    public void Synergy_Activates_AtThreshold()
    {
        loadout.SetSynergies(new[] { MakeSynergy("fire", 2, 0.25f) });

        loadout.Add(MakeItem("f1", "fire"));
        Assert.AreEqual(0, loadout.ActiveSynergyCount);
        Assert.AreEqual(10f, stats.Get(StatType.Damage), 0.001f);

        loadout.Add(MakeItem("f2", "fire"));
        Assert.AreEqual(1, loadout.ActiveSynergyCount);
        Assert.AreEqual(12.5f, stats.Get(StatType.Damage), 0.001f);
    }

    [Test]
    public void Synergy_Deactivates_WhenItemRemoved()
    {
        loadout.SetSynergies(new[] { MakeSynergy("fire", 2, 0.25f) });
        loadout.Add(MakeItem("f1", "fire"));
        loadout.Add(MakeItem("f2", "fire"));

        loadout.RemoveLast();

        Assert.AreEqual(0, loadout.ActiveSynergyCount);
        Assert.AreEqual(10f, stats.Get(StatType.Damage), 0.001f);
    }

    [Test]
    public void Synergy_IsNotDoubleApplied_WithExtraItems()
    {
        loadout.SetSynergies(new[] { MakeSynergy("fire", 2, 0.25f) });
        loadout.Add(MakeItem("f1", "fire"));
        loadout.Add(MakeItem("f2", "fire"));
        loadout.Add(MakeItem("f3", "fire"));

        Assert.AreEqual(1, loadout.ActiveSynergyCount);
        Assert.AreEqual(12.5f, stats.Get(StatType.Damage), 0.001f);
    }
}