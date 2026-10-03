namespace WhiteDragon
{
    /// <summary>How an enemy moves. APPEND ONLY: never reorder or renumber.</summary>
    public enum MovementMode
    {
        /// <summary>CharacterController with gravity; walks on the floor.</summary>
        Ground = 0,
        /// <summary>No gravity; behaviors such as Hover hold its height.</summary>
        Flying = 1,
    }
}
