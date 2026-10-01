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
        [TextArea] public string description;

        /// <summary>Called once per throw before clamping. stacks = how many sources carry this effect.</summary>
        public virtual void ModifyRecipe(ShotRecipe recipe, int stacks) { }

        /// <summary>Per-shot state object, or null if the effect needs none.</summary>
        public virtual object CreateState() => null;

        public virtual void OnSpawn(ShotEffectInstance shot) { }
        public virtual void OnUpdate(ShotEffectInstance shot, float dt) { }

        /// <summary>Called after the shot damaged target.</summary>
        public virtual void OnHit(ShotEffectInstance shot, IDamageable target, Vector3 point) { }
    }
}
