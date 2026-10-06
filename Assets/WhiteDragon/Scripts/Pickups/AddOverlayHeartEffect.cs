using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Adds soul or dark hearts on top of red health. (There is no cap on overlay hearts yet.)</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Pickup Effects/Add Overlay Heart", fileName = "AddOverlayHeartEffect")]
    public class AddOverlayHeartEffect : PickupEffect
    {
        [Tooltip("Soul (Fellowship) or Dark (Corruption).")]
        public HeartKind kind = HeartKind.Soul;
        [Tooltip("How much, in half hearts (2 = one full heart).")]
        [Min(1)]
        public int halves = 2;

        public override bool Collect(PickupContext context)
        {
            var health = context.Health;
            if (health == null || health.State.IsDead) return false;
            if (kind == HeartKind.Dark) health.State.AddDark(halves);
            else health.State.AddSoul(halves);
            return true;
        }

        public override Color PlaceholderTint => kind == HeartKind.Dark ? new Color(0.3f, 0.08f, 0.4f) : new Color(0.3f, 0.5f, 1f);
    }
}
