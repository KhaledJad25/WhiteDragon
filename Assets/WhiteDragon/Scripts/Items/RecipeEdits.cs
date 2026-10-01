using System;

namespace WhiteDragon
{
    /// <summary>Shot changes shared by items and synergies, so both can do the same things.</summary>
    [Serializable]
    public struct RecipeEdits
    {
        public int projectileCountAdd;
        public float spreadAddDegrees;
        public int pierceAdd;
        public float sizeMultiplier;
        public bool overrideDamageType;
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
