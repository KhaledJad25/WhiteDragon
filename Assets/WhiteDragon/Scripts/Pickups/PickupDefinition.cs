using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// One pickup (heart, coin, key, ...) as data: what it does (effects), how it behaves on the floor, and
    /// optional art. Create an asset in Data/Resources/Pickups. Every visual field is optional.
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Pickup", fileName = "Pickup")]
    public class PickupDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique lowercase id, e.g. \"half_heart\".")]
        public string id;
        [Tooltip("Name for tools and debugging.")]
        public string displayName;
        [Tooltip("Lowercase tags, e.g. \"heart\", \"coin\". Drop modifiers change the weight of pickups by tag.")]
        public string[] tags = new string[0];

        [Header("Effects")]
        [Tooltip("Applied in order when collected. A pickup is taken if at least one effect applies; effects that refuse are lost.")]
        public List<PickupEffect> effects = new List<PickupEffect>();

        [Header("Art (optional; empty = placeholder)")]
        [Tooltip("Model on the floor, about 0.3 m across, centered on its pivot. Empty = a small tinted shape.")]
        public GameObject worldPrefab;
        [Tooltip("Icon (stored only; no UI shows it yet).")]
        public Sprite icon;
        [Tooltip("Sound played when collected. Empty = silent.")]
        public AudioClip collectSound;
        [Tooltip("Effect spawned where it was collected (removed after 2 s). Empty = a small tinted burst.")]
        public GameObject collectVfx;
        [Tooltip("Placeholder and burst color. Fully transparent (the default) = the first effect's color.")]
        public Color tint = Color.clear;

        [Header("On the floor")]
        [Tooltip("Within this distance (m) the pickup flies to the player. 0 = no magnet.")]
        [Min(0f)]
        public float magnetRange = 3f;
        [Tooltip("Magnet flight speed (m/s).")]
        [Min(0f)]
        public float magnetSpeed = 8f;
        [Tooltip("Collected when the player is this close (m).")]
        [Min(0.05f)]
        public float collectRadius = 0.7f;
        [Tooltip("Seconds before it disappears (it blinks first). 0 = stays forever.")]
        [Min(0f)]
        public float lifetimeSeconds;

        [Header("Availability")]
        [Tooltip("Empty = always. Otherwise it only spawns once this unlock is granted (hidden progression).")]
        public string requiredUnlockId = "";
        [Tooltip("How it is used. Only ApplyImmediately exists today (held-in-slot comes later).")]
        public PickupCollectMode collectMode = PickupCollectMode.ApplyImmediately;

        /// <summary>The tint to use: tint if set, else the first effect's placeholder color.</summary>
        public Color EffectiveTint
        {
            get
            {
                if (tint.a > 0f) return tint;
                if (effects != null)
                    foreach (var e in effects)
                        if (e != null) return e.PlaceholderTint;
                return new Color(0.7f, 0.7f, 0.7f);
            }
        }

        public bool IsUnlocked => Unlocks.Has(requiredUnlockId);

        public bool HasTag(string tag)
        {
            if (tags == null || string.IsNullOrEmpty(tag)) return false;
            foreach (var t in tags)
                if (string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
