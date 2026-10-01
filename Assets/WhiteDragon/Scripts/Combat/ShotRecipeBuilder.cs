namespace WhiteDragon
{
    /// <summary>Stats, then items and active synergies in pickup order, then effect hooks, then clamp.</summary>
    public static class ShotRecipeBuilder
    {
        public static ShotRecipe Build(StatBlock stats, ItemLoadout loadout)
        {
            var recipe = ShotRecipe.FromStats(stats);
            if (loadout != null)
            {
                foreach (var item in loadout.Items)
                {
                    if (item == null) continue;
                    item.recipeEdits.ApplyTo(recipe);
                    if (item.tags != null)
                        foreach (var tag in item.tags) recipe.Tags.Add(tag);
                    if (item.effects != null)
                        foreach (var e in item.effects) recipe.AddEffect(e);
                }
                foreach (var synergy in loadout.ActiveSynergies)
                {
                    synergy.recipeEdits.ApplyTo(recipe);
                    if (synergy.effects != null)
                        foreach (var e in synergy.effects) recipe.AddEffect(e);
                }
            }

            foreach (var stack in recipe.Effects.ToArray())
                stack.Effect.ModifyRecipe(recipe, stack.Stacks);

            recipe.Clamp();
            return recipe;
        }
    }
}
