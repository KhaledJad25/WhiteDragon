using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Plug-in shot behavior. Assets are shared and stateless: keep per-shot data in the object
    /// returned by CreateState (available as shot.State). Override only the hooks you need.
    /// To add one: write a subclass, create an asset from it, drag it into an item or synergy.
    /// </summary>
    public abstract class ShotEffect : ScriptableObject
    {
        [Tooltip("What this effect asset does, in one short line (for tools and other developers).")]
        [TextArea] public string description;

        /// <summary>Called once per throw before clamping. stacks = how many sources carry this effect.</summary>
        public virtual void ModifyRecipe(ShotRecipe recipe, int stacks) { }

        /// <summary>Per-shot state object, or null if the effect needs none.</summary>
        public virtual object CreateState() => null;

        /// <summary>
        /// Optional, for pooled projectiles: clear a used state object for a new shot and return true
        /// to reuse it (no allocation). The default returns false, so CreateState is called again.
        /// </summary>
        public virtual bool ResetState(object state) => false;

        public virtual void OnSpawn(ShotEffectInstance shot) { }
        public virtual void OnUpdate(ShotEffectInstance shot, float dt) { }

        /// <summary>Called after the shot damaged target.</summary>
        public virtual void OnHit(ShotEffectInstance shot, IDamageable target, Vector3 point) { }
    }
}
