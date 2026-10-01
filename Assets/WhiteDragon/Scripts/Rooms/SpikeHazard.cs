using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Trigger that hurts the player while they stand in it (invincibility frames limit the rate).</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SpikeHazard : MonoBehaviour
    {
        [Tooltip("In half hearts.")]
        public int damage = 1;

        void Awake() => GetComponent<BoxCollider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.Damage(damage);
        }
    }
}
