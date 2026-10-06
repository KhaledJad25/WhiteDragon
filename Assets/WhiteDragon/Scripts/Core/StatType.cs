namespace WhiteDragon
{
    /// <summary>Player stats. APPEND ONLY: never reorder or renumber (values are serialized in assets).</summary>
    public enum StatType
    {
        MoveSpeed,
        JumpHeight,
        CharacterSize,
        FireRate,
        Damage,
        ProjectileSpeed,
        Range,
        Luck,
        /// <summary>Multiplies enemy drop chances (base 1; a PercentAdd of 0.25 = +25%).</summary>
        DropRate,
    }
}
