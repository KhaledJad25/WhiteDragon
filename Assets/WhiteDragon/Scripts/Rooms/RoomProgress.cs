namespace WhiteDragon
{
    /// <summary>Room state machine: Waiting, then Fighting on first entry, then Cleared when no enemy is alive.</summary>
    public class RoomProgress
    {
        public enum Phase
        {
            Waiting,
            Fighting,
            Cleared,
        }

        public Phase Current { get; private set; } = Phase.Waiting;

        /// <summary>True only on the first entry.</summary>
        public bool Enter()
        {
            if (Current != Phase.Waiting) return false;
            Current = Phase.Fighting;
            return true;
        }

        /// <summary>True exactly once: the moment a fighting room has no living enemies.</summary>
        public bool Update(int aliveEnemies)
        {
            if (Current != Phase.Fighting || aliveEnemies > 0) return false;
            Current = Phase.Cleared;
            return true;
        }
    }
}
