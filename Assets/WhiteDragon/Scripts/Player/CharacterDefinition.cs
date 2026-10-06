using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>A playable character as data: base stats, starting hearts and starting items.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Character", fileName = "Character")]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique lowercase id, e.g. \"rock_thrower\".")]
        public string id;
        [Tooltip("Name for tools and a future character select screen.")]
        public string displayName;
        [Tooltip("One or two lines about how this character plays.")]
        [TextArea] public string description;

        [Header("Stats")]
        [Tooltip("Base stat values for this character. Stats not listed keep the game defaults. Items modify on top of these.")]
        public StatOverride[] statOverrides = new StatOverride[0];

        [Header("Start of run")]
        [Tooltip("Items the character starts with, in pickup order. No empty slots.")]
        public List<ItemDefinition> startingItems = new List<ItemDefinition>();
        [Tooltip("Red heart containers, all filled. 3 = the default.")]
        [Min(0)]
        public int startingRedContainers = 3;
        [Tooltip("Soul (Fellowship) hearts, in half hearts. 2 = one full soul heart.")]
        [Min(0)]
        public int startingSoulHearts;
        [Tooltip("Dark (Corruption) hearts, in half hearts. 2 = one full dark heart.")]
        [Min(0)]
        public int startingDarkHearts;

        [Header("Look")]
        [Tooltip("Placeholder color for this character (not shown yet: the player is first-person with no body).")]
        public Color tint = new Color(0.8f, 0.75f, 0.7f);

        [Header("Drops")]
        [Tooltip("For the whole run, multiply the drop weight of pickups with a tag (\"nothing\" = chance of no drop). Change the drop chance itself through a DropRate stat override.")]
        public DropModifier[] dropModifiers = new DropModifier[0];

        /// <summary>One line describing the character, for tools and debugging.</summary>
        public string Summary()
        {
            string text = $"{startingRedContainers} hearts";
            if (startingSoulHearts > 0) text += $", {startingSoulHearts} soul";
            if (startingDarkHearts > 0) text += $", {startingDarkHearts} dark";
            if (statOverrides != null && statOverrides.Length > 0) text += $", {statOverrides.Length} stat override(s)";
            if (startingItems != null && startingItems.Count > 0)
            {
                var names = new List<string>();
                foreach (var i in startingItems) names.Add(i != null ? i.displayName : "(missing item)");
                text += ", starts with " + string.Join(", ", names);
            }
            return text;
        }
    }
}
