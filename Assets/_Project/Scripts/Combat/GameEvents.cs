using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action<ShotRecipe> ShotBuilt;
    public static event Action<Vector3, float, ShotRecipe> EnemyHit;
    public static event Action<Vector3> EnemyKilled;

    public static void RaiseShotBuilt(ShotRecipe recipe)
    {
        ShotBuilt?.Invoke(recipe);
    }

    public static void RaiseEnemyHit(Vector3 point, float damage, ShotRecipe recipe)
    {
        EnemyHit?.Invoke(point, damage, recipe);
    }

    public static void RaiseEnemyKilled(Vector3 point)
    {
        EnemyKilled?.Invoke(point);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        ShotBuilt = null;
        EnemyHit = null;
        EnemyKilled = null;
    }
}