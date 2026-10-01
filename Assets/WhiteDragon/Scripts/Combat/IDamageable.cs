using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Anything a rock (or a status tick) can hurt.</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint);
    }
}
