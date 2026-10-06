using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Enemy deaths roll the enemy's drop table (the variant's override, else the definition's). Listens to
    /// Enemy.AnyDied, so every death drops the same way, including an Exploder's self-destruct. Each enemy rolls from
    /// its own generator, derived from its stable spawn key ("&lt;roomId&gt;#i" or "debug:N") plus ":drop", so the
    /// same seed gives the same drops whatever the kill order. Edit mode only drops when a test asks for it.
    /// </summary>
    public static class EnemyDrops
    {
        static bool subscribed;

        /// <summary>Tests: drop in edit mode too.</summary>
        public static bool DropInEditMode;
        /// <summary>Tests: use this player context instead of the scene's player.</summary>
        public static DropContext ContextOverride;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Enable()
        {
            if (subscribed) return;
            subscribed = true;
            Enemy.AnyDied += OnEnemyDied;
        }

        public static void Disable()
        {
            Enemy.AnyDied -= OnEnemyDied;
            subscribed = false;
        }

        static void OnEnemyDied(Enemy enemy)
        {
            if (!Application.isPlaying && !DropInEditMode) return;
            Drop(enemy, ContextOverride ?? DropContext.FromScene());
        }

        /// <summary>What this enemy drops for a run generator and player context (no spawning).</summary>
        public static List<(PickupDefinition pickup, int count)> RollFor(Enemy enemy, RunRandom run, DropContext context)
        {
            var table = DropRoller.EnemyTable(enemy.definition, enemy.variant);
            if (table == null) return new List<(PickupDefinition, int)>();
            float chance = DropRoller.EnemyDropChance(enemy.definition, enemy.variant, context.DropRate);
            return DropRoller.Roll(table, chance, run.Derive(DropRoller.EnemyKey(enemy.SpawnKey)), context);
        }

        /// <summary>Rolls and spawns this enemy's drop where it stands. Returns how many pickups spawned.</summary>
        public static int Drop(Enemy enemy, DropContext context)
        {
            var drops = RollFor(enemy, RunSession.Rng ?? new RunRandom(RunSession.Seed), context);
            return SpawnAll(drops, enemy.transform.position + Vector3.up * 0.3f);
        }

        /// <summary>Spawns every rolled pickup at a point through the one spawn helper.</summary>
        public static int SpawnAll(List<(PickupDefinition pickup, int count)> drops, Vector3 position)
        {
            int spawned = 0;
            foreach (var (pickup, count) in drops)
                for (int i = 0; i < count; i++)
                    if (PickupManager.Spawn(pickup, position) != null) spawned++;
            return spawned;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            subscribed = false;
            DropInEditMode = false;
            ContextOverride = null;
        }
    }
}
