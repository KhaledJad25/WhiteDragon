using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WhiteDragon
{
    /// <summary>
    /// Content validation rules. Read-only: never modifies an asset. No UnityEditor dependency,
    /// so every rule is unit tested with in-memory assets.
    /// </summary>
    public static class ContentRules
    {
        /// <summary>Types loaded with Resources.LoadAll, and the Resources subfolder they must live in.</summary>
        public static readonly Dictionary<Type, string> ResourcesFolders = new Dictionary<Type, string>
        {
            [typeof(ItemDefinition)] = "Items",
            [typeof(SynergyDefinition)] = "Synergies",
            [typeof(StatusEffectDefinition)] = "Statuses",
        };

        public static List<ContentIssue> Validate(ContentSet set)
        {
            var issues = new List<ContentIssue>();
            var items = set.Items.Where(i => i != null).ToList();
            var synergies = set.Synergies.Where(s => s != null).ToList();

            CheckIds(items, i => i.id, "Item", issues);
            CheckIds(synergies, s => s.id, "Synergy", issues);
            CheckIds(set.Statuses.Where(s => s != null), s => s.id, "Status", issues);
            CheckIds(set.Enemies.Where(e => e != null), e => e.id, "Enemy", issues);
            CheckIds(set.Characters.Where(c => c != null), c => c.id, "Character", issues);
            CheckCharacters(set.Characters.Where(c => c != null), issues);
            CheckLocations(set, issues);
            CheckItems(items, issues);
            CheckSynergies(synergies, items, issues);
            CheckEffects(set.Effects.Where(e => e != null), issues);
            CheckStatuses(set.Statuses.Where(s => s != null), issues);
            CheckLonelyTags(items, synergies, issues);
            CheckPedestalPools(set.PedestalPools, items, issues);
            CheckIds(set.Variants.Where(v => v != null), v => v.id, "Enemy variant", issues);
            CheckEnemies(set, issues);
            CheckBrains(set.Brains.OfType<StateMachineBrain>().Where(b => b != null), issues);
            CheckBehaviors(set, issues);
            foreach (string path in set.MissingScriptAssets)
                issues.Add(Error("asset.missingscript",
                    $"{path} uses a script that no longer exists (deleted or renamed class). Restore the script or delete the asset.", null));
            return issues;
        }

        /// <summary>Behaviors used by more brains than this are reported (shared by accident?).</summary>
        public const int SharedBehaviorLimit = 3;

        // ---------- Enemies ----------

        static void CheckEnemies(ContentSet set, List<ContentIssue> issues)
        {
            var withPrefab = new HashSet<EnemyDefinition>(set.EnemyPrefabs.Where(p => p != null)
                .Select(p => p.GetComponent<Enemy>()).Where(e => e != null && e.definition != null).Select(e => e.definition));
            foreach (var e in set.Enemies.Where(e => e != null))
            {
                if (e.brain == null)
                    issues.Add(Error("enemy.nobrain", $"Enemy '{e.name}' has no brain. Every enemy needs one (Tools/WhiteDragon/New/Enemy makes a starter brain).", e));
                if (e.visualPrefab == null && !withPrefab.Contains(e))
                    issues.Add(Warning("enemy.novisual", $"Enemy '{e.name}' has no visual: no visualPrefab and no enemy prefab uses it, so tools can only spawn plain placeholder shapes.", e));
                CheckMovement(e, e.brain, e.name, issues);
            }
            foreach (var v in set.Variants.Where(v => v != null && v.brain != null && v.baseEnemy != null))
                CheckMovement(v.baseEnemy, v.brain, $"{v.name} (variant of {v.baseEnemy.name})", issues);

            foreach (var e in set.SceneEnemies.Where(e => e != null))
            {
                var brain = e.variant != null && e.variant.brain != null ? e.variant.brain : e.definition != null ? e.definition.brain : null;
                if (brain == null)
                    issues.Add(Warning("enemy.builtinchase", $"Scene enemy '{e.name}' still uses the old built-in chase (no definition or no brain).", e));
            }
        }

        static void CheckMovement(EnemyDefinition enemy, EnemyBrainDefinition brain, string who, List<ContentIssue> issues)
        {
            if (!(brain is StateMachineBrain sm)) return;
            foreach (var b in sm.states.SelectMany(s => s.behaviors).Where(b => b != null).Distinct())
                if (b.RequiredMovement.HasValue && b.RequiredMovement.Value != enemy.movement)
                    issues.Add(Error("enemy.movement", $"{enemy.movement} enemy '{who}' uses '{b.name}', which only works for {b.RequiredMovement.Value} enemies.", enemy));
        }

        // ---------- Brains ----------

        static void CheckBrains(IEnumerable<StateMachineBrain> brains, List<ContentIssue> issues)
        {
            foreach (var brain in brains)
            {
                if (brain.states.Count == 0)
                {
                    issues.Add(Error("brain.nostates", $"Brain '{brain.name}' has no states.", brain));
                    continue;
                }
                var names = new HashSet<string>(brain.states.Select(s => s.name));
                foreach (var s in brain.states)
                {
                    for (int n = 0; n < s.behaviors.Count; n++)
                        if (s.behaviors[n] == null)
                            issues.Add(Error("brain.nullbehavior", $"Brain '{brain.name}' state '{s.name}' has an empty behavior slot (element {n}).", brain));
                    foreach (var t in s.transitions.Where(t => !names.Contains(t.target)))
                        issues.Add(Error("brain.badtarget", $"Brain '{brain.name}' state '{s.name}' goes to '{t.target}', which is not a state of this brain.", brain));
                    if (brain.states.Count > 1 && s.transitions.Count == 0 && !s.terminal)
                        issues.Add(Error("brain.noexit", $"Brain '{brain.name}' state '{s.name}' has no way out. Add a transition, or tick Terminal if it is meant to be final.", brain));
                }
                for (int i = 0; i < brain.states.Count; i++)
                {
                    var attack = brain.states[i].behaviors.FirstOrDefault(b => b != null && b.StartsAttack);
                    if (attack != null && !Telegraphed(brain, i, new HashSet<int>()))
                        issues.Add(Error("brain.notelegraph",
                            $"Brain '{brain.name}' state '{brain.states[i].name}' starts an attack ('{attack.name}') without a Telegraph state before it.", brain));
                }
            }
        }

        /// <summary>
        /// Every way into state i passes through a state that runs a Telegraph. States with no behaviors (pure
        /// decision states like "Check") are looked through. Entering at the start state counts as untelegraphed.
        /// </summary>
        static bool Telegraphed(StateMachineBrain brain, int i, HashSet<int> visiting)
        {
            if (i == 0 || !visiting.Add(i)) return false;
            string name = brain.states[i].name;
            for (int p = 0; p < brain.states.Count; p++)
            {
                var pred = brain.states[p];
                if (!pred.transitions.Any(t => t.target == name)) continue;
                if (pred.behaviors.Any(b => b is TelegraphBehavior)) continue;
                if (pred.behaviors.Count(b => b != null) == 0 && Telegraphed(brain, p, new HashSet<int>(visiting))) continue;
                return false;
            }
            return true;
        }

        // ---------- Behaviors ----------

        static void CheckBehaviors(ContentSet set, List<ContentIssue> issues)
        {
            var brains = set.Brains.OfType<StateMachineBrain>().Where(b => b != null).ToList();
            var enemiesByBrain = new Dictionary<EnemyBrainDefinition, List<string>>();
            foreach (var e in set.Enemies.Where(e => e != null && e.brain != null)) Users(enemiesByBrain, e.brain).Add(e.name);
            foreach (var v in set.Variants.Where(v => v != null && v.brain != null)) Users(enemiesByBrain, v.brain).Add(v.name);

            var undescribed = new HashSet<Type>();
            foreach (var b in set.Behaviors.Where(b => b != null))
            {
                var info = (EnemyBehaviorInfoAttribute)Attribute.GetCustomAttribute(b.GetType(), typeof(EnemyBehaviorInfoAttribute), false);
                if ((info == null || string.IsNullOrWhiteSpace(info.Description)) && undescribed.Add(b.GetType()))
                    issues.Add(Warning("behavior.nodescription", $"Behavior type {b.GetType().Name} has no [EnemyBehaviorInfo] description.", b));

                var usedBy = brains.Where(sm => sm.states.Any(s => s.behaviors.Contains(b))).ToList();
                if (usedBy.Count == 0)
                    issues.Add(Warning("behavior.unused", $"Behavior asset '{b.name}' is used by no brain.", b));
                else if (usedBy.Count > SharedBehaviorLimit)
                {
                    var enemies = usedBy.SelectMany(sm => enemiesByBrain.TryGetValue(sm, out var list) ? list : new List<string>()).Distinct();
                    issues.Add(Warning("behavior.shared",
                        $"Behavior asset '{b.name}' is used by {usedBy.Count} brains (shared by accident?). Enemies: {string.Join(", ", enemies)}.", b));
                }
            }
        }

        static List<string> Users(Dictionary<EnemyBrainDefinition, List<string>> map, EnemyBrainDefinition brain)
        {
            if (!map.TryGetValue(brain, out var list)) map[brain] = list = new List<string>();
            return list;
        }

        static void CheckIds<T>(IEnumerable<T> assets, Func<T, string> getId, string kind, List<ContentIssue> issues) where T : Object
        {
            var list = assets.ToList();
            foreach (var a in list.Where(a => string.IsNullOrWhiteSpace(getId(a))))
                issues.Add(Error("id.empty", $"{kind} '{a.name}' has an empty id.", a));
            foreach (var group in list.Where(a => !string.IsNullOrWhiteSpace(getId(a)))
                         .GroupBy(a => getId(a).Trim(), StringComparer.OrdinalIgnoreCase)
                         .Where(g => g.Count() > 1))
            {
                string names = string.Join(", ", group.Select(a => a.name));
                foreach (var a in group)
                    issues.Add(Error("id.duplicate", $"{kind} id '{group.Key}' is used by {group.Count()} assets: {names}.", a));
            }
        }

        static void CheckLocations(ContentSet set, List<ContentIssue> issues)
        {
            foreach (var pair in set.Paths)
            {
                if (pair.Key == null || string.IsNullOrEmpty(pair.Value)) continue;
                if (!ResourcesFolders.TryGetValue(pair.Key.GetType(), out string folder)) continue;
                string path = pair.Value.Replace('\\', '/');
                if (path.IndexOf($"/Resources/{folder}/", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                issues.Add(Error("location",
                    $"{pair.Key.GetType().Name} '{pair.Key.name}' is at {path}; it must be inside a Resources/{folder} folder or it will never load.",
                    pair.Key));
            }
        }

        static void CheckItems(List<ItemDefinition> items, List<ContentIssue> issues)
        {
            foreach (var i in items)
            {
                CheckNullEffects(i.effects, "Item", i, issues);
                var pools = (i.poolIds ?? new string[0]).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                if (pools.Count == 0)
                    issues.Add(Warning("item.nopools", $"Item '{i.name}' is in no pool, so it can never appear on a pedestal.", i));
                if (i.weightMultiplier <= 0f)
                    issues.Add(Warning("item.weight", $"Item '{i.name}' has weightMultiplier {i.weightMultiplier}, so it never rolls.", i));
                if (string.IsNullOrWhiteSpace(i.displayName))
                    issues.Add(Warning("item.noname", $"Item '{i.name}' has no displayName.", i));
                if (string.IsNullOrWhiteSpace(i.description))
                    issues.Add(Warning("item.nodescription", $"Item '{i.name}' has no description.", i));
                foreach (var tag in i.tags ?? new string[0])
                    CheckFormat(tag, "tag", "Item", i, issues);
                foreach (var pool in pools)
                    CheckFormat(pool, "pool ID", "Item", i, issues);
            }
        }

        static void CheckSynergies(List<SynergyDefinition> synergies, List<ItemDefinition> items, List<ContentIssue> issues)
        {
            foreach (var s in synergies)
            {
                CheckNullEffects(s.effects, "Synergy", s, issues);
                if (string.IsNullOrWhiteSpace(s.tag))
                {
                    issues.Add(Warning("synergy.unreachable", $"Synergy '{s.name}' has no tag, so it can never activate.", s));
                    continue;
                }
                CheckFormat(s.tag, "tag", "Synergy", s, issues);
                int carriers = items.Count(i => i.HasTag(s.tag));
                int needed = Math.Max(1, s.requiredCount);
                if (carriers < needed)
                    issues.Add(Warning("synergy.unreachable",
                        $"Synergy '{s.name}' needs {needed} items tagged '{s.tag}' but only {carriers} exist, so it can never activate.", s));
            }
        }

        static void CheckEffects(IEnumerable<ShotEffect> effects, List<ContentIssue> issues)
        {
            foreach (var e in effects)
                if (e is ApplyStatusEffect apply && apply.status == null)
                    issues.Add(Error("applystatus.nostatus", $"Apply-Status effect '{e.name}' has no status assigned.", e));
        }

        static void CheckStatuses(IEnumerable<StatusEffectDefinition> statuses, List<ContentIssue> issues)
        {
            foreach (var s in statuses)
            {
                if (s.duration <= 0f)
                    issues.Add(Warning("status.duration", $"Status '{s.name}' has duration {s.duration}, so it ends immediately.", s));
                if (s.tickInterval <= 0f && s.damagePerSecond > 0f)
                    issues.Add(Warning("status.tick", $"Status '{s.name}' deals damage but tickInterval is {s.tickInterval}.", s));
            }
        }

        static void CheckLonelyTags(List<ItemDefinition> items, List<SynergyDefinition> synergies, List<ContentIssue> issues)
        {
            var synergyTags = new HashSet<string>(synergies.Select(s => s.tag).Where(t => !string.IsNullOrWhiteSpace(t)),
                StringComparer.OrdinalIgnoreCase);
            var carriers = new Dictionary<string, List<ItemDefinition>>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in items)
                foreach (var tag in (i.tags ?? new string[0]).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!carriers.TryGetValue(tag, out var list)) carriers[tag] = list = new List<ItemDefinition>();
                    list.Add(i);
                }
            foreach (var pair in carriers.OrderBy(p => p.Key, StringComparer.Ordinal))
                if (pair.Value.Count == 1 && !synergyTags.Contains(pair.Key))
                    issues.Add(Warning("tag.single", $"Tag '{pair.Key}' is only on '{pair.Value[0].name}' and no synergy uses it (typo?).", pair.Value[0]));
        }

        static void CheckPedestalPools(List<(string Pool, Object Source)> pedestalPools, List<ItemDefinition> items, List<ContentIssue> issues)
        {
            foreach (var group in pedestalPools.GroupBy(p => p.Pool ?? "", StringComparer.OrdinalIgnoreCase))
            {
                bool eligible = items.Any(i => i.InPool(group.Key) && i.weightMultiplier > 0f && string.IsNullOrEmpty(i.requiredUnlockId));
                if (!eligible)
                    issues.Add(Warning("pool.empty",
                        $"Pool '{group.Key}' is used by {group.Count()} pedestal(s) but has no item that can appear (it falls back to 'normal').",
                        group.First().Source));
            }
        }

        static void CheckCharacters(IEnumerable<CharacterDefinition> characters, List<ContentIssue> issues)
        {
            foreach (var c in characters)
            {
                if (c.startingItems == null) continue;
                for (int n = 0; n < c.startingItems.Count; n++)
                    if (c.startingItems[n] == null)
                        issues.Add(Error("character.nullitem", $"Character '{c.name}' has an empty slot in startingItems (element {n}).", c));
            }
        }

        static void CheckNullEffects(List<ShotEffect> effects, string kind, Object owner, List<ContentIssue> issues)
        {
            if (effects == null) return;
            for (int n = 0; n < effects.Count; n++)
                if (effects[n] == null)
                    issues.Add(Error("effects.null", $"{kind} '{owner.name}' has an empty slot in effects (element {n}).", owner));
        }

        static void CheckFormat(string value, string what, string kind, Object owner, List<ContentIssue> issues)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (value == value.ToLowerInvariant() && !value.Any(char.IsWhiteSpace)) return;
            issues.Add(Warning("format", $"{kind} '{owner.name}' {what} '{value}' should be lowercase with no spaces.", owner));
        }

        static ContentIssue Error(string code, string message, Object asset) => new ContentIssue(IssueSeverity.Error, code, message, asset);
        static ContentIssue Warning(string code, string message, Object asset) => new ContentIssue(IssueSeverity.Warning, code, message, asset);
    }
}
