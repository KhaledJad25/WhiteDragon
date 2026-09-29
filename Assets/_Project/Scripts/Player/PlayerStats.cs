using UnityEngine;

// Owns the player's StatBlock. Everything else reads from here.
public class PlayerStats : MonoBehaviour
{
    public StatBlock Stats { get; private set; } = new StatBlock();

    [Header("Base values (character data will replace these later)")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpHeight = 1.2f;   // meters
    [SerializeField] float characterSize = 1f;  // 1 = 1.8m tall
    [SerializeField] float fireRate = 2f;       // throws per second
    [SerializeField] float damage = 3.5f;
    [SerializeField] float projectileSpeed = 18f;
    [SerializeField] float range = 20f;
    [SerializeField] float luck = 0f;

    void Awake() => ApplyBase();

    [ContextMenu("Re-apply base stats")]
    public void ApplyBase()
    {
        Stats.SetBase(StatType.MoveSpeed, moveSpeed);
        Stats.SetBase(StatType.JumpHeight, jumpHeight);
        Stats.SetBase(StatType.CharacterSize, characterSize);
        Stats.SetBase(StatType.FireRate, fireRate);
        Stats.SetBase(StatType.Damage, damage);
        Stats.SetBase(StatType.ProjectileSpeed, projectileSpeed);
        Stats.SetBase(StatType.Range, range);
        Stats.SetBase(StatType.Luck, luck);
    }
}