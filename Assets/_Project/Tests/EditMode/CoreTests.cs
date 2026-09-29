using NUnit.Framework;

public class StatBlockTests
{
    [Test]
    public void Modifiers_Combine_InCorrectOrder()
    {
        var s = new StatBlock();
        s.SetBase(StatType.Damage, 10f);
        s.Add(new StatModifier { Stat = StatType.Damage, Kind = ModifierKind.Flat, Value = 2f });
        s.Add(new StatModifier { Stat = StatType.Damage, Kind = ModifierKind.PercentAdd, Value = 0.5f });
        s.Add(new StatModifier { Stat = StatType.Damage, Kind = ModifierKind.Multiply, Value = 2f });

        // (10 + 2) * (1 + 0.5) * 2 = 36
        Assert.AreEqual(36f, s.Get(StatType.Damage), 0.001f);
    }

    [Test]
    public void PercentAdds_AreSummed_NotCompounded()
    {
        var s = new StatBlock();
        s.SetBase(StatType.MoveSpeed, 10f);
        s.Add(new StatModifier { Stat = StatType.MoveSpeed, Kind = ModifierKind.PercentAdd, Value = 0.2f });
        s.Add(new StatModifier { Stat = StatType.MoveSpeed, Kind = ModifierKind.PercentAdd, Value = 0.3f });

        Assert.AreEqual(15f, s.Get(StatType.MoveSpeed), 0.001f);
    }

    [Test]
    public void RemoveFrom_RemovesOnlyThatSource()
    {
        var s = new StatBlock();
        var itemA = new object();
        var itemB = new object();
        s.SetBase(StatType.Damage, 10f);
        s.Add(new StatModifier { Stat = StatType.Damage, Kind = ModifierKind.Flat, Value = 5f, Source = itemA });
        s.Add(new StatModifier { Stat = StatType.Damage, Kind = ModifierKind.Flat, Value = 1f, Source = itemB });

        s.RemoveFrom(itemA);

        Assert.AreEqual(11f, s.Get(StatType.Damage), 0.001f);
    }
}

public class HealthStateTests
{
    [Test]
    public void Overlay_AbsorbsDamage_BeforeRed()
    {
        var h = new HealthState();
        h.AddContainer(6);
        h.AddOverlay(HeartType.Soul, 2);

        h.Damage(3); // 2 to soul, 1 to red

        Assert.AreEqual(0, h.Overlay.Count);
        Assert.AreEqual(5, h.RedCurrent);
    }

    [Test]
    public void DarkHeart_Break_RaisesEvent()
    {
        var h = new HealthState();
        h.AddContainer(2);
        h.AddOverlay(HeartType.Dark, 1);
        int bursts = 0;
        h.OverlayBroke += t => { if (t == HeartType.Dark) bursts++; };

        h.Damage(1);

        Assert.AreEqual(1, bursts);
        Assert.AreEqual(2, h.RedCurrent);
    }

    [Test]
    public void Heal_ClampsToContainers()
    {
        var h = new HealthState();
        h.AddContainer(4);
        h.Damage(1);
        h.Heal(99);

        Assert.AreEqual(4, h.RedCurrent);
    }
}