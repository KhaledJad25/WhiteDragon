using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Shot changes shared by items and synergies, so both can do the same things.</summary>
    [Serializable]
    public struct RecipeEdits
    {
        [Tooltip("Extra rocks per throw. 2 = three rocks. Total is capped at 12.")]
        public int projectileCountAdd;
        [Tooltip("Extra fan width in degrees when throwing several rocks.")]
        public float spreadAddDegrees;
        [Tooltip("How many extra enemies each rock passes through. Capped at 10.")]
        public int pierceAdd;
        [Tooltip("Rock size multiplier. 1 = no change, 2 = twice as big.")]
        public float sizeMultiplier;
        [Tooltip("Tick to replace the rock's damage type (and color) with the one below.")]
        public bool overrideDamageType;
        [Tooltip("Damage type used when Override Damage Type is ticked.")]
        public DamageType damageType;

        /// <summary>No change. Use as the field initializer so new assets start with sizeMultiplier 1.</summary>
        public static RecipeEdits Default => new RecipeEdits { sizeMultiplier = 1f };

        public void ApplyTo(ShotRecipe recipe)
        {
            recipe.Count += projectileCountAdd;
            recipe.SpreadDegrees += spreadAddDegrees;
            recipe.Pierce += pierceAdd;
            recipe.SizeScale *= sizeMultiplier;
            if (overrideDamageType) recipe.DamageType = damageType;
        }
    }
}
