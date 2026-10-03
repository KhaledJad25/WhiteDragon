namespace WhiteDragon
{
    /// <summary>Test only: a behavior without [EnemyBehaviorInfo], for the validator's description rule.</summary>
    public class UndescribedBehavior : EnemyBehavior<UndescribedBehavior.State>
    {
        public class State { }

        protected override bool Tick(EnemyContext ctx, State s, float dt) => false;
    }
}
