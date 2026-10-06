using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// What a pickup does when collected. Stateless and shared (like ShotEffect): Inspector numbers only.
    /// New effects: one small class that extends this, plus an asset.
    /// </summary>
    public abstract class PickupEffect : ScriptableObject
    {
        /// <summary>Apply the effect. Return false if it cannot be taken now (for example health already full);
        /// a pickup whose effects all return false stays on the floor.</summary>
        public abstract bool Collect(PickupContext context);

        /// <summary>Color of the placeholder shape when the pickup has no model and no tint of its own.</summary>
        public virtual Color PlaceholderTint => new Color(0.7f, 0.7f, 0.7f);
    }
}
