using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class RunSession
{
    public static int Seed { get; private set; }
    public static RunRandom Rng { get; private set; }
    public static readonly HashSet<string> PickedItemIds = new();

    public static event Action<int> RunStarted;
    public static event Action<string> ItemPicked;

    static int autoSeedCounter = 1;

    static RunSession()
    {
        StartRun(0);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        RunStarted = null;
        ItemPicked = null;
        StartRun(0);
    }

    public static void StartRun(int seed = 0)
    {
        if (seed == 0)
        {
            int tick = Environment.TickCount;
            int counter = Interlocked.Increment(ref autoSeedCounter);
            seed = unchecked(tick * 397 ^ counter * 7919);
            if (seed == 0)
            {
                seed = 1;
            }
        }
        Seed = seed;
        Rng = new RunRandom(seed);
        PickedItemIds.Clear();
        Debug.Log($"[Run] Seed {seed}");
        RunStarted?.Invoke(seed);
    }

    public static void PickItem(string itemId)
    {
        if (!string.IsNullOrEmpty(itemId))
        {
            PickedItemIds.Add(itemId);
        }
        ItemPicked?.Invoke(itemId);
    }
}
