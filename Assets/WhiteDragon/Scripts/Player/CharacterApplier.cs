using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Applies a CharacterDefinition to the player. Runs before every other player script so stats
    /// and hearts are set before anything reads them. No character assigned = game defaults.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(PlayerStats), typeof(PlayerHealth), typeof(PlayerInventory))]
    public class CharacterApplier : MonoBehaviour
    {
        [Tooltip("The character to play. Empty = default stats, 3 hearts, no items.")]
        public CharacterDefinition character;

        void Awake() => Apply();

        public void Apply()
        {
            if (character == null) return;

            var dropModifiers = GetComponent<PlayerDropModifiers>();
            if (dropModifiers != null) dropModifiers.SetCharacter(character);

            var stats = GetComponent<PlayerStats>().Stats;
            if (character.statOverrides != null)
                foreach (var o in character.statOverrides)
                    stats.SetBase(o.stat, o.value);

            var health = GetComponent<PlayerHealth>();
            health.startingContainers = character.startingRedContainers;
            health.State.AddSoul(character.startingSoulHearts);
            health.State.AddDark(character.startingDarkHearts);

            var inventory = GetComponent<PlayerInventory>();
            if (character.startingItems != null)
                foreach (var item in character.startingItems)
                    if (item != null) inventory.Add(item);
        }
    }
}
