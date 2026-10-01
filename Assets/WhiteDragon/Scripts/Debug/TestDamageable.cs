using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Debug/test target that records damage taken. Lives in the runtime assembly so tests can add it.</summary>
    public class TestDamageable : MonoBehaviour, IDamageable
    {
        public float TotalDamage;
        public int Hits;

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            TotalDamage += amount;
            Hits++;
        }
    }
}
