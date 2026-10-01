using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Bonus that turns on while the player holds requiredCount or more items with the tag.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Synergy", fileName = "Synergy")]
    public class SynergyDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public string tag;
        public int requiredCount = 2;

        [Header("Effects")]
        public StatModifier[] statModifiers = new StatModifier[0];
        public RecipeEdits recipeEdits = RecipeEdits.Default;
        public List<ShotEffect> effects = new List<ShotEffect>();
    }
}
