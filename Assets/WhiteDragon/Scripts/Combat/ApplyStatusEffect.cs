using UnityEngine;

namespace WhiteDragon
{
    /// <summary>On hit, applies a status asset to the target. Intensity = stacks x intensityPerStack.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Effects/Apply Status", fileName = "ApplyStatus")]
    public class ApplyStatusEffect : ShotEffect
    {
        public StatusEffectDefinition status;
        public int intensityPerStack = 1;

        public override void OnHit(ShotEffectInstance shot, IDamageable target, Vector3 point)
        {
            if (status == null || !(target is Component c) || c == null) return;
            var receiver = c.GetComponent<StatusReceiver>();
            if (receiver != null) receiver.Apply(status, shot.Stacks * intensityPerStack);
        }
    }
}
