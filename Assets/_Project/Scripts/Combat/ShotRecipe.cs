using System.Collections.Generic;
using UnityEngine;

public enum DamageType { Physical, Fire, Dark, Holy, Blood }

public class ShotRecipe
{
    public const int MaxCount = 12;
    public const int MaxPierce = 10;

    public float Damage;
    public float Speed;
    public float Range;
    public float SizeScale = 1f;
    public int Count = 1;
    public float SpreadDegrees;
    public int Pierce;
    public float Homing;
    public float BurnDps;
    public float BurnDuration;
    public DamageType DamageType = DamageType.Physical;
    public readonly HashSet<string> Tags = new HashSet<string>();
    public int Generation;

    public static ShotRecipe FromStats(StatBlock s)
    {
        ShotRecipe r = new ShotRecipe();
        r.Damage = s.Get(StatType.Damage);
        r.Speed = s.Get(StatType.ProjectileSpeed);
        r.Range = s.Get(StatType.Range);
        return r;
    }

    public void ClampToCaps()
    {
        Damage = Mathf.Max(0f, Damage);
        Speed = Mathf.Clamp(Speed, 2f, 80f);
        Range = Mathf.Clamp(Range, 2f, 100f);
        SizeScale = Mathf.Clamp(SizeScale, 0.3f, 4f);
        Count = Mathf.Clamp(Count, 1, MaxCount);
        Pierce = Mathf.Clamp(Pierce, 0, MaxPierce);
        Homing = Mathf.Clamp(Homing, 0f, 360f);
        BurnDps = Mathf.Max(0f, BurnDps);
        BurnDuration = Mathf.Clamp(BurnDuration, 0f, 10f);
        if (Count > 1)
        {
            SpreadDegrees = Mathf.Max(SpreadDegrees, 4f * (Count - 1));
        }
        SpreadDegrees = Mathf.Clamp(SpreadDegrees, 0f, 120f);
    }
}