using System;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public class PlayerInventory : MonoBehaviour
{
    ItemLoadout loadout;

    public event Action Changed;

    ItemLoadout Loadout
    {
        get
        {
            if (loadout == null)
            {
                loadout = new ItemLoadout(GetComponent<PlayerStats>().Stats);
                loadout.SetSynergies(Resources.LoadAll<SynergyDefinition>("Synergies"));
                loadout.Changed += OnLoadoutChanged;
            }
            return loadout;
        }
    }

    void Awake()
    {
        ItemLoadout init = Loadout;
    }

    void OnLoadoutChanged()
    {
        if (Changed != null)
        {
            Changed();
        }
    }

    public int Count
    {
        get { return Loadout.Count; }
    }

    public ItemDefinition GetItem(int index)
    {
        return Loadout.GetItem(index);
    }

    public void Add(ItemDefinition item)
    {
        Loadout.Add(item);
        Debug.Log("[Item] Added " + item.displayName);
    }

    public void RemoveLast()
    {
        Loadout.RemoveLast();
    }

    public void Clear()
    {
        Loadout.Clear();
    }

    public int CountTag(string tag)
    {
        return Loadout.CountTag(tag);
    }

    public void ModifyRecipe(ShotRecipe recipe)
    {
        Loadout.ModifyRecipe(recipe);
    }

    public string ActiveSynergyNames()
    {
        string s = "";
        for (int i = 0; i < Loadout.ActiveSynergyCount; i++)
        {
            SynergyDefinition d = Loadout.GetActiveSynergy(i);
            s += d.displayName + " (" + d.description + ")\n";
        }
        return s;
    }
}