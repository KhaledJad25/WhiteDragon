using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Everything one throw needs. Built fresh on every throw.</summary>
    public class ShotRecipe
    {
        public const int MinCount = 1, MaxCount = 12;
        public const int MinPierce = 0, MaxPierce = 10;
        public const float MinSize = 0.3f, MaxSize = 4f;
        public const float MinSpeed = 2f, MaxSpeed = 80f;
        public const float MinRange = 2f, MaxRange = 100f;
        public const float MinSpread = 0f, MaxSpread = 120f;
        public const float MinSpreadPerExtraProjectile = 4f;

        public float Damage;
        public float Speed;
        public float Range;
        public float SizeScale = 1f;
        public int Count = 1;
        public float SpreadDegrees;
        public int Pierce;
        public DamageType DamageType = DamageType.Physical;
        public readonly HashSet<string> Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Each effect appears once, with how many sources granted it.</summary>
        public readonly List<EffectStack> Effects = new List<EffectStack>();

        public static ShotRecipe FromStats(StatBlock stats)
        {
            return new ShotRecipe
            {
                Damage = stats.Get(StatType.Damage),
                Speed = stats.Get(StatType.ProjectileSpeed),
                Range = stats.Get(StatType.Range),
            };
        }

        public void AddEffect(ShotEffect effect, int stacks = 1)
        {
            if (effect == null || stacks <= 0) return;
            foreach (var e in Effects)
            {
                if (e.Effect != effect) continue;
                e.Stacks += stacks;
                return;
            }
            Effects.Add(new EffectStack(effect, stacks));
        }

        public int GetStacks(ShotEffect effect)
        {
            foreach (var e in Effects)
                if (e.Effect == effect) return e.Stacks;
            return 0;
        }

        public ShotRecipe Clone()
        {
            var copy = new ShotRecipe
            {
                Damage = Damage,
                Speed = Speed,
                Range = Range,
                SizeScale = SizeScale,
                Count = Count,
                SpreadDegrees = SpreadDegrees,
                Pierce = Pierce,
                DamageType = DamageType,
            };
            copy.Tags.UnionWith(Tags);
            foreach (var e in Effects) copy.Effects.Add(new EffectStack(e.Effect, e.Stacks));
            return copy;
        }

        public void Clamp()
        {
            Count = Mathf.Clamp(Count, MinCount, MaxCount);
            Pierce = Mathf.Clamp(Pierce, MinPierce, MaxPierce);
            SizeScale = Mathf.Clamp(SizeScale, MinSize, MaxSize);
            Speed = Mathf.Clamp(Speed, MinSpeed, MaxSpeed);
            Range = Mathf.Clamp(Range, MinRange, MaxRange);
            SpreadDegrees = Mathf.Clamp(SpreadDegrees, MinSpread, MaxSpread);
            if (Count > 1)
                SpreadDegrees = Mathf.Max(SpreadDegrees, MinSpreadPerExtraProjectile * (Count - 1));
        }

        /// <summary>Yaw offset of projectile index in a fan of count projectiles across spread degrees.</summary>
        public static float FanAngle(int index, int count, float spread)
        {
            if (count <= 1) return 0f;
            return -spread * 0.5f + spread * index / (count - 1);
        }
    }
}
