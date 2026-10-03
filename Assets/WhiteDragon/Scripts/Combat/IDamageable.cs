using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Anything a rock, an enemy attack or a status tick can hurt.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint);

        /// <summary>Whose side it is on (see Teams). Anything that does not say is Neutral, like TargetDummy.</summary>
        Team Team => Team.Neutral;
    }
}
