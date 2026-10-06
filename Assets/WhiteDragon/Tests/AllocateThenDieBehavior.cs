using System.Collections.Generic;

namespace WhiteDragon
{
    /// <summary>Test only: allocates bytesPerTick every frame (kept alive) and kills its enemy after dieAfter ticks.</summary>
    [EnemyBehaviorInfo("Test only: allocates, then dies.", "Test")]
    public class AllocateThenDieBehavior : EnemyBehavior<AllocateThenDieBehavior.State>
    {
        public class State
        {
            public int Ticks;
        }

        public static readonly List<byte[]> Kept = new List<byte[]>();
        public int bytesPerTick = 512;
        public int dieAfter = 200;

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            Kept.Add(new byte[bytesPerTick]);
            if (++s.Ticks >= dieAfter) ctx.Enemy.Kill();
            return false;
        }
    }
}
