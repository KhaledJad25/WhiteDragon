namespace WhiteDragon
{
    /// <summary>Runtime pairing of one effect with one projectile. Reused when the projectile is pooled.</summary>
    public class ShotEffectInstance
    {
        public ShotEffect Effect { get; private set; }
        public int Stacks { get; private set; }
        public Projectile Projectile { get; private set; }
        public object State;

        public ShotEffectInstance(ShotEffect effect, int stacks, Projectile projectile)
        {
            Effect = effect;
            Stacks = stacks;
            Projectile = projectile;
            State = effect.CreateState();
        }

        /// <summary>Starts a new shot. Keeps the state object when the effect can reset it, else makes a new one.</summary>
        public void Reset(ShotEffect effect, int stacks, Projectile projectile)
        {
            bool reuse = effect == Effect && State != null && effect.ResetState(State);
            Effect = effect;
            Stacks = stacks;
            Projectile = projectile;
            if (!reuse) State = effect.CreateState();
        }
    }
}
