using UnityEngine;

namespace WhiteDragon
{
    /// <summary>The player's held items. The logic lives in ItemLoadout.</summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerInventory : MonoBehaviour
    {
        ItemLoadout loadout;

        public ItemLoadout Loadout => loadout ??= new ItemLoadout(
            GetComponent<PlayerStats>().Stats,
            Resources.LoadAll<SynergyDefinition>("Synergies"));

        public bool Add(ItemDefinition item) => Loadout.Add(item);
        public bool RemoveLast() => Loadout.RemoveLast();
        public void Clear() => Loadout.Clear();
    }
}
