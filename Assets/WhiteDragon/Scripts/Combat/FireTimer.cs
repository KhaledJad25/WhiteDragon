namespace WhiteDragon
{
    /// <summary>
    /// Fixed-rate repeat timer that carries leftover time forward, so the rate is the same at any frame
    /// rate, and fires more than once in a frame when needed. Holding after a pause fires at once
    /// without banking the missed shots.
    /// </summary>
    public class FireTimer
    {
        public const int MaxPerFrame = 8;

        float next = float.NegativeInfinity;
        bool wasHeld;

        /// <summary>How many shots to fire this frame.</summary>
        public int Tick(float now, bool held, float rate)
        {
            if (!held || rate <= 0f)
            {
                wasHeld = false;
                return 0;
            }
            float interval = 1f / rate;
            // A fresh press fires now (no banked shots); while held, leftover time is always carried.
            if (!wasHeld && next < now) next = now;
            wasHeld = true;
            int shots = 0;
            while (now >= next && shots < MaxPerFrame)
            {
                next += interval;
                shots++;
            }
            return shots;
        }
    }
}
