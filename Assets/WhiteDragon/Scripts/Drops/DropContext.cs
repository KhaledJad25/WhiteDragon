using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>The player's side of a drop roll. Tests build it by hand; FromPlayer reads the live player.</summary>
    public class DropContext
    {
        public float Luck;
        /// <summary>The DropRate stat (1 = normal).</summary>
        public float DropRate = 1f;
        /// <summary>Per-tag weight multipliers (missing tag = 1). Case-insensitive when built by PlayerDropModifiers.</summary>
        public IReadOnlyDictionary<string, float> TagMultipliers;
        /// <summary>Red health below max (PlayerHurt entries may drop).</summary>
        public bool PlayerHurt;

        public float Multiplier(string tag)
        {
            if (TagMultipliers == null || string.IsNullOrEmpty(tag)) return 1f;
            return TagMultipliers.TryGetValue(tag, out float m) ? m : 1f;
        }

        /// <summary>From a player object (any missing part keeps its default). Null = defaults.</summary>
        public static DropContext FromPlayer(GameObject player)
        {
            var context = new DropContext();
            if (player == null) return context;
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                context.Luck = stats.Stats.Get(StatType.Luck);
                context.DropRate = stats.Stats.Get(StatType.DropRate);
            }
            var health = player.GetComponent<PlayerHealth>();
            if (health != null) context.PlayerHurt = health.State.Red < health.State.MaxRed;
            var modifiers = player.GetComponent<PlayerDropModifiers>();
            if (modifiers != null) context.TagMultipliers = modifiers.Multipliers;
            return context;
        }

        /// <summary>From the PlayerController in the scene (the real player, never a test stand-in).</summary>
        public static DropContext FromScene()
        {
            var controller = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            return FromPlayer(controller != null ? controller.gameObject : null);
        }
    }
}
