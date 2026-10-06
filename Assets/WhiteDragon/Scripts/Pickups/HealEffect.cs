using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Heals red health. Cannot be taken at full red health.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Pickup Effects/Heal", fileName = "HealEffect")]
    public class HealEffect : PickupEffect
    {
        [Tooltip("Red health restored, in half hearts (2 = one full heart).")]
        [Min(1)]
        public int halves = 2;

        public override bool Collect(PickupContext context)
        {
            var health = context.Health;
            if (health == null || health.State.IsDead || health.State.Red >= health.State.MaxRed) return false;
            health.State.Heal(halves);
            return true;
        }

        public override Color PlaceholderTint => new Color(0.8f, 0.05f, 0.08f);
    }
}
