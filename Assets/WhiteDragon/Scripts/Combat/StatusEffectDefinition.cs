using UnityEngine;

namespace WhiteDragon
{
    /// <summary>A status (burn, poison, slow...). Pure data: create an asset, no code.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Status Effect", fileName = "Status")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique lowercase id, e.g. \"burn\".")]
        public string id;
        [Tooltip("Name shown in tools and the debug panel.")]
        public string displayName;

        [Header("Timing")]
        [Tooltip("Seconds the status lasts after it is applied.")]
        [Min(0f)]
        public float duration = 3f;
        [Tooltip("Seconds between damage ticks. Only matters when Damage Per Second is above 0.")]
        [Min(0.05f)]
        public float tickInterval = 0.5f;

        [Header("Effect")]
        [Tooltip("Damage per second for each stack. 0 = no damage (e.g. a pure slow).")]
        [Min(0f)]
        public float damagePerSecond;
        [Tooltip("Movement speed multiplier. 1 = no change, 0.5 = half speed, 0 = frozen.")]
        [Range(0f, 2f)]
        public float speedMultiplier = 1f;
        [Tooltip("Color the target is tinted while affected.")]
        public Color tint = Color.white;

        [Header("Stacking")]
        [Tooltip("Applying it again: RefreshDuration resets the timer, StackIntensity adds stacks (more damage), Ignore does nothing.")]
        public StatusStacking stacking = StatusStacking.RefreshDuration;
        [Tooltip("Most stacks a target can have. Damage scales with stacks.")]
        [Min(1)]
        public int maxStacks = 1;
    }
}
