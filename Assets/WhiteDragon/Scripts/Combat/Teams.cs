using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Team rules, in one place. Player shots hit Enemy and Neutral targets, never the Player.
    /// Enemy attacks (contact, projectiles, area damage) hit ONLY the Player. Neutral never attacks.
    /// Walls block every shot; that is decided by the projectile, not here.
    /// </summary>
    public static class Teams
    {
        public static bool CanDamage(Team attacker, Team target)
        {
            switch (attacker)
            {
                case Team.Player: return target == Team.Enemy || target == Team.Neutral;
                case Team.Enemy: return target == Team.Player;
                default: return false;
            }
        }

        /// <summary>
        /// Damage dealt TO the player is counted in half hearts: an enemy's damage number is half hearts.
        /// Any positive amount deals at least one half heart; fractions round to the nearest half heart.
        /// The only conversion point: PlayerHealth.TakeDamage uses it for contact, projectiles and area damage.
        /// </summary>
        public static int ToHalfHearts(float amount) => amount > 0f ? Mathf.Max(1, Mathf.RoundToInt(amount)) : 0;
    }
}
