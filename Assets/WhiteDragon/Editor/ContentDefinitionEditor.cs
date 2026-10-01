using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Shared inspector: one-line summary at the top, an inline warning if this asset's id is empty
    /// or used by another asset of the same type, then the default Inspector.
    /// </summary>
    public abstract class ContentDefinitionEditor<T> : Editor where T : ScriptableObject
    {
        string checkedId;
        string duplicateNames;

        protected abstract string GetId(T asset);
        protected abstract string GetSummary(T asset);

        public override void OnInspectorGUI()
        {
            var asset = (T)target;
            string id = GetId(asset);

            EditorGUILayout.HelpBox(GetSummary(asset), MessageType.None);
            if (string.IsNullOrWhiteSpace(id))
            {
                EditorGUILayout.HelpBox("Id is empty. Give it a unique lowercase id, e.g. \"iron_tooth\".", MessageType.Warning);
            }
            else
            {
                if (id != checkedId) FindDuplicates(asset, id);
                if (duplicateNames.Length > 0)
                    EditorGUILayout.HelpBox($"Id \"{id}\" is also used by: {duplicateNames}. Ids must be unique.", MessageType.Error);
            }
            EditorGUILayout.Space(2f);

            DrawDefaultInspector();
        }

        void FindDuplicates(T asset, string id)
        {
            checkedId = id;
            duplicateNames = string.Join(", ", ContentCreator.FindAll<T>()
                .Where(a => a != asset && string.Equals(GetId(a)?.Trim(), id.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(a => a.name));
        }
    }
}
