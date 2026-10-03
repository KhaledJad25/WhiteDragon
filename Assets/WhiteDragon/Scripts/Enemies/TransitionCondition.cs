namespace WhiteDragon
{
    /// <summary>When a StateMachineBrain transition fires. APPEND ONLY: never reorder or renumber.</summary>
    public enum TransitionCondition
    {
        /// <summary>Seconds in this state &gt;= value. Leftover time carries into the next state.</summary>
        TimeInState = 0,
        /// <summary>Horizontal distance to the player &lt; value (meters).</summary>
        DistanceToPlayerBelow = 1,
        /// <summary>Horizontal distance to the player &gt; value (meters).</summary>
        DistanceToPlayerAbove = 2,
        /// <summary>Any behavior in this state has reported Finished since the state was entered.</summary>
        BehaviorFinished = 3,
        /// <summary>Health below value percent (0-100).</summary>
        HealthBelowPercent = 4,
        /// <summary>The enemy took damage since this state was entered.</summary>
        TookDamage = 5,
        /// <summary>Nothing blocks the line from the enemy's eyes to the player.</summary>
        HasLineOfSight = 6,
        /// <summary>Always true: move on next frame.</summary>
        Always = 7,
    }
}
