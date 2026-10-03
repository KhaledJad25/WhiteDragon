using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Everything the validator looks at. Filled from the project by ContentValidator, or by hand in tests.</summary>
    public class ContentSet
    {
        public readonly List<ItemDefinition> Items = new List<ItemDefinition>();
        public readonly List<SynergyDefinition> Synergies = new List<SynergyDefinition>();
        public readonly List<ShotEffect> Effects = new List<ShotEffect>();
        public readonly List<StatusEffectDefinition> Statuses = new List<StatusEffectDefinition>();
        public readonly List<EnemyDefinition> Enemies = new List<EnemyDefinition>();
        public readonly List<CharacterDefinition> Characters = new List<CharacterDefinition>();
        public readonly List<EnemyBrainDefinition> Brains = new List<EnemyBrainDefinition>();
        public readonly List<EnemyVariant> Variants = new List<EnemyVariant>();
        public readonly List<EnemyBehavior> Behaviors = new List<EnemyBehavior>();
        /// <summary>Enemy prefabs (an Enemy component pointing at a definition) found in the project.</summary>
        public readonly List<GameObject> EnemyPrefabs = new List<GameObject>();
        /// <summary>Enemy components in open scenes.</summary>
        public readonly List<Enemy> SceneEnemies = new List<Enemy>();

        /// <summary>Content asset paths that reference a script that no longer exists (deleted or renamed class).</summary>
        public readonly List<string> MissingScriptAssets = new List<string>();

        /// <summary>Asset path per asset. Assets without a path skip the folder check.</summary>
        public readonly Dictionary<Object, string> Paths = new Dictionary<Object, string>();

        /// <summary>Pool IDs used by pedestals in open scenes, with the pedestal to select.</summary>
        public readonly List<(string Pool, Object Source)> PedestalPools = new List<(string, Object)>();
    }
}
