using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PedestalGroupTests
{
    static ItemDefinition MakeItem(string id, ItemRarity rarity, ItemPoolType[] pools)
    {
        ItemDefinition item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.id = id;
        item.displayName = id;
        item.rarity = rarity;
        item.pools = pools;
        item.weightMultiplier = 1f;
        return item;
    }

    [Test]
    public void PedestalGroupRoller_SameSeed_GivesSameAssignment_RegardlessOfInputOrder()
    {
        var catalog = new List<ItemDefinition>
        {
            MakeItem("item_a", ItemRarity.Common, new[] { ItemPoolType.Normal }),
            MakeItem("item_b", ItemRarity.Uncommon, new[] { ItemPoolType.Normal }),
            MakeItem("item_c", ItemRarity.Rare, new[] { ItemPoolType.Normal }),
            MakeItem("item_d", ItemRarity.Legendary, new[] { ItemPoolType.Normal }),
            MakeItem("item_e", ItemRarity.Common, new[] { ItemPoolType.Normal })
        };

        var p1 = new PedestalDescriptor("Pedestals:0/Pedestal_A:0", ItemPoolType.Normal);
        var p2 = new PedestalDescriptor("Pedestals:0/Pedestal_B:1", ItemPoolType.Normal);
        var p3 = new PedestalDescriptor("Pedestals:0/Pedestal_C:2", ItemPoolType.Normal);

        // Order 1: A, B, C
        var list1 = new List<PedestalDescriptor> { p1, p2, p3 };
        var rng1 = new RunRandom(8888);
        var assignments1 = PedestalGroupRoller.AssignItems(list1, catalog, rng1, 0f);

        // Order 2: C, A, B (different input order)
        var list2 = new List<PedestalDescriptor> { p3, p1, p2 };
        var rng2 = new RunRandom(8888);
        var assignments2 = PedestalGroupRoller.AssignItems(list2, catalog, rng2, 0f);

        // Both assignments must be identical for each key
        Assert.AreEqual(assignments1["Pedestals:0/Pedestal_A:0"].id, assignments2["Pedestals:0/Pedestal_A:0"].id);
        Assert.AreEqual(assignments1["Pedestals:0/Pedestal_B:1"].id, assignments2["Pedestals:0/Pedestal_B:1"].id);
        Assert.AreEqual(assignments1["Pedestals:0/Pedestal_C:2"].id, assignments2["Pedestals:0/Pedestal_C:2"].id);
    }
}
