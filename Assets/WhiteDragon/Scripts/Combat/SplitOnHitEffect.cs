using UnityEngine;

namespace WhiteDragon
{
    /// <summary>On hit, the rock bursts into smaller fragments that fly on. Fragments do not split again.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Effects/Split On Hit", fileName = "SplitOnHit")]
    public class SplitOnHitEffect : ShotEffect
    {
        public int fragmentsPerStack = 2;
        public float spreadDegrees = 60f;
        public float damageFraction = 0.5f;
        public float sizeFraction = 0.6f;
        public float rangeFraction = 0.4f;

        public override void OnHit(ShotEffectInstance shot, IDamageable target, Vector3 point)
        {
            var parent = shot.Projectile;
            var fragment = parent.Recipe.Clone();
            fragment.Effects.RemoveAll(e => e.Effect is SplitOnHitEffect);
            fragment.Count = 1;
            fragment.Pierce = 0;
            fragment.Damage *= damageFraction;
            fragment.SizeScale *= sizeFraction;
            fragment.Range *= rangeFraction;
            fragment.Clamp();

            int count = Mathf.Max(1, fragmentsPerStack * shot.Stacks);
            Vector3 forward = parent.Velocity.sqrMagnitude > 0f ? parent.Velocity.normalized : parent.transform.forward;
            for (int i = 0; i < count; i++)
            {
                float yaw = ShotRecipe.FanAngle(i, count, spreadDegrees);
                Projectile.Spawn(fragment, point, Quaternion.AngleAxis(yaw, Vector3.up) * forward, parent.Owner)
                    .IgnoreTarget(target);
            }
        }
    }
}
