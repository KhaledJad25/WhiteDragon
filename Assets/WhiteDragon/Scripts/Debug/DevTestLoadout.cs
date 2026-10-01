using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Testing aid: items added when Play starts, so you can launch with a build equipped.
    /// Only works in the editor and debug builds. Empty list = no effect.
    /// </summary>
    [DefaultExecutionOrder(-999)]
    public class DevTestLoadout : MonoBehaviour
    {
        [Tooltip("Items added at the start of Play (after the character's starting items). Editor and debug builds only.")]
        public List<ItemDefinition> items = new List<ItemDefinition>();

        void Awake()
        {
            if (!(Application.isEditor || Debug.isDebugBuild) || items.Count == 0) return;
            var inventory = GetComponent<PlayerInventory>();
            if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
            if (inventory == null) return;
            foreach (var item in items)
                if (item != null) inventory.Add(item);
        }
    }
}
