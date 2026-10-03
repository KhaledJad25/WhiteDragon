namespace WhiteDragon
{
    /// <summary>What an actor (player or enemy) is doing, for animation hooks. APPEND ONLY.</summary>
    public enum ActorState
    {
        Idle,
        Move,
        Attack,
        Hit,
        Die,
        /// <summary>Attack wind-up (an enemy's telegraph). Without its own mapping, AnimatorStateDriver plays Attack.</summary>
        Windup,
    }
}
