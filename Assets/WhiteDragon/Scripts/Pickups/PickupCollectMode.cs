namespace WhiteDragon
{
    /// <summary>
    /// How a pickup is used. APPEND ONLY: never reorder or renumber (serialized in assets).
    /// Only ApplyImmediately is implemented. A held-in-slot mode (cards, pills) comes in a later task;
    /// until then any other value logs a warning and behaves as ApplyImmediately.
    /// </summary>
    public enum PickupCollectMode
    {
        ApplyImmediately,
    }
}
