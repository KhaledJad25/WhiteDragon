using UnityEngine;

[CreateAssetMenu(menuName = "WhiteDragon/Synergy")]
public class SynergyDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Trigger: N or more held items with this tag")]
    public string tag;
    public int requiredCount = 2;

    [Header("Bonus")]
    public ItemDefinition.StatEntry[] statModifiers = new ItemDefinition.StatEntry[0];
    public int projectileCountAdd;
    public int pierceAdd;
    public float shotSizeMultiplier = 1f;
    public float homingAdd;
    public float burnDpsAdd;
    public float burnDurationAdd;

    public void EditRecipe(ShotRecipe r)
    {
        r.Count += projectileCountAdd;
        r.Pierce += pierceAdd;
        r.SizeScale *= shotSizeMultiplier;
        r.Homing += homingAdd;
        r.BurnDps += burnDpsAdd;
        r.BurnDuration += burnDurationAdd;
    }
}