using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// The player's drop modifiers: the character's, then held items' (pickup order), then active synergies', multiplied
    /// per tag and clamped to 0.1..10. Rebuilt only when the loadout or character changes (lazily, on the next read).
    /// </summary>
    public class PlayerDropModifiers : MonoBehaviour
    {
        public const float MinMultiplier = 0.1f;
        public const float MaxMultiplier = 10f;

        readonly Dictionary<string, float> multipliers = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        CharacterDefinition character;
        ItemLoadout subscribed;
        bool dirty = true;

        /// <summary>Tag to multiplier (missing tag = 1).</summary>
        public IReadOnlyDictionary<string, float> Multipliers
        {
            get
            {
                Refresh();
                return multipliers;
            }
        }

        public CharacterDefinition Character => character;

        /// <summary>CharacterApplier calls this with the character being played.</summary>
        public void SetCharacter(CharacterDefinition value)
        {
            character = value;
            dirty = true;
        }

        public float Multiplier(string tag) => Multipliers.TryGetValue(tag ?? "", out float m) ? m : 1f;

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            if (subscribed != null) subscribed.Changed -= MarkDirty;
            subscribed = null;
        }

        void MarkDirty() => dirty = true;

        void Subscribe()
        {
            var inventory = GetComponent<PlayerInventory>();
            var loadout = inventory != null ? inventory.Loadout : null;
            if (loadout == subscribed) return;
            if (subscribed != null) subscribed.Changed -= MarkDirty;
            subscribed = loadout;
            if (subscribed != null) subscribed.Changed += MarkDirty;
            dirty = true;
        }

        void Refresh()
        {
            Subscribe();
            if (!dirty) return;
            dirty = false;
            Aggregate(character, subscribed?.Items, subscribed?.ActiveSynergies, multipliers);
        }

        /// <summary>Multiplies every modifier per tag in a fixed order (character, items, synergies), then clamps each tag to 0.1..10.</summary>
        public static void Aggregate(CharacterDefinition character, IEnumerable<ItemDefinition> items, IEnumerable<SynergyDefinition> synergies,
            Dictionary<string, float> into)
        {
            into.Clear();
            if (character != null) Add(character.dropModifiers, into);
            if (items != null)
                foreach (var item in items)
                    if (item != null) Add(item.dropModifiers, into);
            if (synergies != null)
                foreach (var synergy in synergies)
                    if (synergy != null) Add(synergy.dropModifiers, into);
            var tags = new List<string>(into.Keys);
            foreach (var tag in tags) into[tag] = Mathf.Clamp(into[tag], MinMultiplier, MaxMultiplier);
        }

        static void Add(DropModifier[] modifiers, Dictionary<string, float> into)
        {
            if (modifiers == null) return;
            foreach (var m in modifiers)
            {
                if (m == null || string.IsNullOrEmpty(m.tag)) continue;
                into[m.tag] = (into.TryGetValue(m.tag, out float current) ? current : 1f) * m.weightMultiplier;
            }
        }
    }
}
