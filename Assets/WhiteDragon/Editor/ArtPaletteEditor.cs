using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Editing the ArtPalette re-applies it to the environment materials and to any LookRig in the open scene.</summary>
    [CustomEditor(typeof(ArtPalette))]
    public class ArtPaletteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();
            if (GUILayout.Button("Apply to materials and scene")) changed = true;
            if (changed) Apply((ArtPalette)target);
        }

        public static void Apply(ArtPalette palette)
        {
            palette.ApplyToMaterials();
            foreach (var t in palette.environment)
                if (t != null && t.material != null) EditorUtility.SetDirty(t.material);
            foreach (var rig in Object.FindObjectsByType<LookRig>()) rig.Apply();
        }
    }
}
