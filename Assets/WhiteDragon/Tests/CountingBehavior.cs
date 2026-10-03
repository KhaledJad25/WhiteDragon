using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Test behavior: counts Enter, Tick and Exit per enemy and logs them; finishes after finishAfter ticks (-1 = never).</summary>
    [EnemyBehaviorInfo("Test only: counts calls.", "Test")]
    public class CountingBehavior : EnemyBehavior<CountingBehavior.State>
    {
        public class State
        {
            public int Enters, Ticks, Exits;
        }

        public static readonly List<string> Log = new List<string>();
        public int finishAfter = -1;

        /// <summary>The most recent state object this behavior ran with (tests read counts through it).</summary>
        public State Last;

        protected override void Enter(EnemyContext ctx, State s)
        {
            s.Enters++;
            s.Ticks = 0;
            Last = s;
            Log.Add("enter:" + name);
        }

        protected override bool Tick(EnemyContext ctx, State s, float dt)
        {
            s.Ticks++;
            Last = s;
            return finishAfter >= 0 && s.Ticks >= finishAfter;
        }

        protected override void Exit(EnemyContext ctx, State s)
        {
            s.Exits++;
            Log.Add("exit:" + name);
        }
    }
}
