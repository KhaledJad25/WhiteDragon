using System;
using UnityEngine;

[CreateAssetMenu(menuName = "WhiteDragon/Item")]
public class ItemDefinition : ScriptableObject
{
    [Serializable]
    public struct StatEntry
    {
        public StatType stat;
        public ModifierKind kind;
        public float value;
    }

    public string id;
    public string displayName;
    [TextArea] public string description;
    public string[] tags = new string[0];

    [Header("Stat modifiers")]
    public StatEntry[] statModifiers = new StatEntry[0];

    [Header("Shot recipe edits")]
    public int projectileCountAdd;
    public float spreadAddDegrees;
    public int pierceAdd;
    public float shotSizeMultiplier = 1f;
    public float homingAdd;
    public float burnDpsAdd;
    public float burnDurationAdd;
    public bool overrideDamageType;
    public DamageType damageType;

    public void EditRecipe(ShotRecipe r)
    {
        r.Count += projectileCountAdd;
        r.SpreadDegrees += spreadAddDegrees;
        r.Pierce += pierceAdd;
        r.SizeScale *= shotSizeMultiplier;
        r.Homing += homingAdd;
        r.BurnDps += burnDpsAdd;
        r.BurnDuration += burnDurationAdd;

        if (overrideDamageType)
        {
            r.DamageType = damageType;
        }

        for (int i = 0; i < tags.Length; i++)
        {
            r.Tags.Add(tags[i]);
        }
    }
}