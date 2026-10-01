using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Decides when hit-stop (brief slow motion) runs. Overlapping requests extend the current stop, but
    /// never past MaxDuration from its start, and a new stop cannot start within Cooldown of the last one
    /// ending, so constant hits cannot keep the game in slow motion. Times are real (unscaled) seconds.
    /// </summary>
    public class HitStopGate
    {
        public float MaxDuration = 0.15f;
        public float Cooldown = 0.1f;

        float start, until, blockedUntil = float.NegativeInfinity;

        public bool Active { get; private set; }

        /// <summary>True if time should be slowed from now on.</summary>
        public bool Request(float now, float seconds)
        {
            if (seconds <= 0f) return false;
            if (!Active)
            {
                if (now < blockedUntil) return false;
                Active = true;
                start = now;
                until = now + Mathf.Min(seconds, MaxDuration);
                return true;
            }
            until = Mathf.Min(Mathf.Max(until, now + seconds), start + MaxDuration);
            return true;
        }

        public void Reset()
        {
            Active = false;
            start = until = 0f;
            blockedUntil = float.NegativeInfinity;
        }

        /// <summary>True on the frame the stop ends (restore normal time then).</summary>
        public bool Update(float now)
        {
            if (!Active || now < until) return false;
            Active = false;
            blockedUntil = now + Cooldown;
            return true;
        }
    }
}
