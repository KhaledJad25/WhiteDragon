using UnityEditor;

namespace WhiteDragon
{
    [CustomEditor(typeof(SynergyDefinition))]
    public class SynergyDefinitionEditor : ContentDefinitionEditor<SynergyDefinition>
    {
        protected override string GetId(SynergyDefinition asset) => asset.id;
        protected override string GetSummary(SynergyDefinition asset) => asset.Summary();
    }
}
