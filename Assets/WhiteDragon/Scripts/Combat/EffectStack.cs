namespace WhiteDragon
{
    /// <summary>An effect in a recipe with how many sources granted it.</summary>
    public class EffectStack
    {
        public ShotEffect Effect;
        public int Stacks;

        public EffectStack(ShotEffect effect, int stacks)
        {
            Effect = effect;
            Stacks = stacks;
        }
    }
}
