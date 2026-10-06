namespace WhiteDragon
{
    /// <summary>When a drop table entry may be rolled. APPEND ONLY: never reorder or renumber.</summary>
    public enum DropCondition
    {
        Always,
        /// <summary>Only while the player's red health is below its max.</summary>
        PlayerHurt,
    }
}
