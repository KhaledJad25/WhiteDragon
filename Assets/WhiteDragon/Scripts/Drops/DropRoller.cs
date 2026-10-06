using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Rolls drop tables. Plain C#, no scene access, so it can be unit tested. Every roll uses the generator it is
    /// given (each source derives its own), so the same seed, source and modifiers always give the same drop.
    /// Weight of an entry = its weight x the player's multiplier for each tag on its pickup.
    /// Nothing weight = nothingWeight x max(0.2, 1 - 0.05 x Luck) x the "nothing" multiplier.
    /// </summary>
    public static class DropRoller
    {
        public const RandomStream Stream = RandomStream.Rewards;

        /// <summary>
        /// One drop: with probability dropChance (1 or more = always, no chance roll), table.rolls picks of one
        /// entry or nothing. Returns what dropped, in roll order (empty = nothing).
        /// </summary>
        public static List<(PickupDefinition pickup, int count)> Roll(DropTableDefinition table, float dropChance, RunRandom rng, DropContext context)
        {
            var result = new List<(PickupDefinition, int)>();
            if (table == null || rng == null || dropChance <= 0f) return result;
            context ??= new DropContext();
            if (dropChance < 1f && rng.Value(Stream) >= dropChance) return result;

            int count = table.entries != null ? table.entries.Count : 0;
            var weights = new float[count + 1];
            for (int i = 0; i < count; i++) weights[i] = EffectiveWeight(table.entries[i], context);
            weights[count] = EffectiveNothingWeight(table, context);

            for (int r = 0; r < Mathf.Max(1, table.rolls); r++)
            {
                int pick = rng.PickWeighted(Stream, weights);
                if (pick < 0 || pick == count) continue;
                var entry = table.entries[pick];
                int min = Mathf.Max(1, entry.minCount);
                int max = Mathf.Max(min, entry.maxCount);
                result.Add((entry.pickup, rng.Range(Stream, min, max + 1)));
            }
            return result;
        }

        /// <summary>The entry's weight after modifiers; 0 if it cannot drop now (no pickup, locked, or its condition fails).</summary>
        public static float EffectiveWeight(DropEntry entry, DropContext context)
        {
            if (entry == null || entry.pickup == null || !(entry.weight > 0f)) return 0f;
            if (!entry.pickup.IsUnlocked) return 0f;
            if (entry.condition == DropCondition.PlayerHurt && !context.PlayerHurt) return 0f;
            float w = entry.weight;
            if (entry.pickup.tags != null)
                foreach (var tag in entry.pickup.tags) w *= context.Multiplier(tag);
            return w;
        }

        public static float EffectiveNothingWeight(DropTableDefinition table, DropContext context) =>
            table.nothingWeight * LuckFactor(context.Luck) * context.Multiplier(DropModifier.NothingTag);

        /// <summary>How luck scales the nothing weight: 5% less per point, at most 80% less.</summary>
        public static float LuckFactor(float luck) => Mathf.Max(0.2f, 1f - 0.05f * luck);

        /// <summary>An enemy's chance to drop: definition chance x variant multiplier x the DropRate stat, within 0..1.</summary>
        public static float EnemyDropChance(EnemyDefinition definition, EnemyVariant variant, float dropRate)
        {
            if (definition == null) return 0f;
            float multiplier = variant != null ? variant.dropChanceMultiplier : 1f;
            return Mathf.Clamp01(definition.dropChance * multiplier * dropRate);
        }

        /// <summary>The table an enemy rolls: the variant's override, else the definition's.</summary>
        public static DropTableDefinition EnemyTable(EnemyDefinition definition, EnemyVariant variant) =>
            variant != null && variant.dropTable != null ? variant.dropTable : definition != null ? definition.dropTable : null;

        // ---- Keys: each source derives its generator from its own stable key, never from a shared stream. ----

        /// <summary>"&lt;enemyKey&gt;:drop" (enemyKey = "&lt;roomId&gt;#i" or "debug:N"). Repeat rolls add "#n".</summary>
        public static string EnemyKey(string spawnKey, int repeat = 0) => WithRepeat(spawnKey + ":drop", repeat);

        /// <summary>"&lt;roomId&gt;:reward". Repeat rolls add "#n".</summary>
        public static string RoomKey(string roomId, int repeat = 0) => WithRepeat(roomId + ":reward", repeat);

        static string WithRepeat(string key, int repeat) => repeat > 0 ? key + "#" + repeat : key;

        /// <summary>A source's own generator: the run generator derived with the source key.</summary>
        public static RunRandom SourceRandom(string key) => (RunSession.Rng ?? new RunRandom(RunSession.Seed)).Derive(key);
    }
}
