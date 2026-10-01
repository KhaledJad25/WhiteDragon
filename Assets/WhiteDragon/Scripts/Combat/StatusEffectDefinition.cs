using UnityEngine;

namespace WhiteDragon
{
    /// <summary>A status (burn, poison, slow...). Pure data: create an asset, no code.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Status Effect", fileName = "Status")]
    public class StatusEffectDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public float duration = 3f;
        public float tickInterval = 0.5f;
        [Tooltip("Per stack.")]
        public float damagePerSecond;
        [Tooltip("1 = no change, 0.5 = half speed.")]
        public float speedMultiplier = 1f;
        public Color tint = Color.white;
        public StatusStacking stacking = StatusStacking.RefreshDuration;
        public int maxStacks = 1;
    }
}
