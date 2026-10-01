using UnityEditor;

namespace WhiteDragon
{
    [CustomEditor(typeof(ItemDefinition))]
    public class ItemDefinitionEditor : ContentDefinitionEditor<ItemDefinition>
    {
        protected override string GetId(ItemDefinition asset) => asset.id;
        protected override string GetSummary(ItemDefinition asset) => asset.Summary();
    }
}
