using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Bonus that turns on while the player holds requiredCount or more items with the tag.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Synergy", fileName = "Synergy")]
    public class SynergyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique lowercase id, e.g. \"pyromaniac\". Never change it once in use.")]
        public string id;
        [Tooltip("Name for tools and future UI.")]
        public string displayName;
        [Tooltip("What the synergy does, in one short line.")]
        [TextArea] public string description;

        [Header("When it turns on")]
        [Tooltip("Item tag to count, e.g. \"fire\". Must match the tags on items (case is ignored).")]
        public string tag;
        [Tooltip("How many held items must carry the tag. 2 = any two fire items.")]
        [Min(1)]
        public int requiredCount = 2;

        [Header("Effects")]
        [Tooltip("Stat changes while active. Flat adds, PercentAdd adds percent (0.25 = +25%), Multiply multiplies.")]
        public StatModifier[] statModifiers = new StatModifier[0];
        [Tooltip("Changes to every throw while active: extra rocks, spread, pierce, size, damage type.")]
        public RecipeEdits recipeEdits = RecipeEdits.Default;
        [Tooltip("Shot effect assets added while active. No empty slots.")]
        public List<ShotEffect> effects = new List<ShotEffect>();

        [Header("Drops")]
        [Tooltip("While active, multiply the drop weight of pickups with a tag (\"nothing\" = chance of no drop).")]
        public DropModifier[] dropModifiers = new DropModifier[0];

        /// <summary>One line describing when it activates and what it does, for tools and debugging.</summary>
        public string Summary()
        {
            string text = ItemDefinition.DescribeEffects(statModifiers, recipeEdits, effects);
            return $"{requiredCount}x '{tag}': " + (text.Length > 0 ? text : "no effect");
        }
    }
}
