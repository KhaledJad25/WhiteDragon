using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Test effect that records which hooks ran and with how many stacks.</summary>
    public class RecordingEffect : ShotEffect
    {
        public int ModifyCalls, SpawnCalls, UpdateCalls, HitCalls;
        public int LastStacks;

        class State { public int Updates; }

        public override object CreateState() => new State();

        public override void ModifyRecipe(ShotRecipe recipe, int stacks)
        {
            ModifyCalls++;
            LastStacks = stacks;
        }

        public override void OnSpawn(ShotEffectInstance shot) => SpawnCalls++;

        public override void OnUpdate(ShotEffectInstance shot, float dt)
        {
            ((State)shot.State).Updates++;
            UpdateCalls++;
        }

        public override void OnHit(ShotEffectInstance shot, IDamageable target, Vector3 point) => HitCalls++;
    }
}
