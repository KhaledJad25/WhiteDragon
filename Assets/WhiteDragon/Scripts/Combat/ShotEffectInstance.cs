namespace WhiteDragon
{
    /// <summary>Runtime pairing of one effect with one projectile. Created per shot per effect.</summary>
    public class ShotEffectInstance
    {
        public readonly ShotEffect Effect;
        public readonly int Stacks;
        public readonly Projectile Projectile;
        public object State;

        public ShotEffectInstance(ShotEffect effect, int stacks, Projectile projectile)
        {
            Effect = effect;
            Stacks = stacks;
            Projectile = projectile;
            State = effect.CreateState();
        }
    }
}
