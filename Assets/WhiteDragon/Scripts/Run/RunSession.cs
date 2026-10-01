using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>The current run: seed, seeded randomness, picked items, and run events.</summary>
    public static class RunSession
    {
        static long freshSeedCounter;

        public static int Seed { get; private set; }
        public static RunRandom Rng { get; private set; }
        public static bool IsRunning { get; private set; }
        public static int RoomsCleared { get; private set; }
        public static readonly List<string> PickedItemIds = new List<string>();

        public static event Action RunStarted;
        public static event Action RunEnded;
        public static event Action<ItemDefinition> ItemPicked;
        public static event Action<string> RoomEntered;
        public static event Action<string> RoomCleared;

        /// <summary>Starts a run. seed 0 means a fresh random seed.</summary>
        public static void StartRun(int seed = 0)
        {
            if (seed == 0) seed = FreshSeed();
            Seed = seed;
            Rng = new RunRandom(seed);
            PickedItemIds.Clear();
            RoomsCleared = 0;
            IsRunning = true;
            Debug.Log($"[Run] Seed {seed}");
            RunStarted?.Invoke();
        }

        public static void EndRun()
        {
            if (!IsRunning) return;
            IsRunning = false;
            RunEnded?.Invoke();
        }

        public static void NotifyItemPicked(ItemDefinition item)
        {
            if (item == null) return;
            PickedItemIds.Add(item.id);
            ItemPicked?.Invoke(item);
        }

        public static void NotifyRoomEntered(string roomId) => RoomEntered?.Invoke(roomId);

        public static void NotifyRoomCleared(string roomId)
        {
            RoomsCleared++;
            RoomCleared?.Invoke(roomId);
        }

        /// <summary>Positive non-zero seed from the clock mixed with an atomic counter, so same-frame calls differ.</summary>
        public static int FreshSeed()
        {
            long n = Interlocked.Increment(ref freshSeedCounter);
            ulong mixed = RunRandom.Mix((ulong)DateTime.UtcNow.Ticks ^ RunRandom.Mix((ulong)n));
            int seed = (int)(mixed & 0x7FFFFFFF);
            return seed == 0 ? 1 : seed;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Seed = 0;
            Rng = null;
            IsRunning = false;
            RoomsCleared = 0;
            PickedItemIds.Clear();
            RunStarted = null;
            RunEnded = null;
            ItemPicked = null;
            RoomEntered = null;
            RoomCleared = null;
        }
    }
}
