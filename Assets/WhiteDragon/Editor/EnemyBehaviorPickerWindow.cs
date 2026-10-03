using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Searchable chooser of behavior types (with their [EnemyBehaviorInfo] descriptions) that creates an asset.</summary>
    public class EnemyBehaviorPickerWindow : EditorWindow
    {
        Type[] types = new Type[0];
        string search = "";
        int selected = -1;
        string assetName = "";
        string family = "shared";
        Vector2 scroll;

        public static void Open()
        {
            var w = GetWindow<EnemyBehaviorPickerWindow>(true, "New Enemy Behavior Asset");
            w.minSize = new Vector2(460f, 380f);
            w.types = EnemyContentCreator.BehaviorTypes();
        }

        void OnGUI()
        {
            search = EditorGUILayout.TextField("Search", search);
            EditorGUILayout.LabelField("Pick a behavior type (name, description or category):", EditorStyles.miniLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            string q = search.Trim().ToLowerInvariant();
            for (int i = 0; i < types.Length; i++)
            {
                var info = EnemyContentCreator.Info(types[i]);
                string description = info?.Description ?? "(no description)";
                string category = info?.Category ?? "-";
                string text = $"{types[i].Name} {description} {category}".ToLowerInvariant();
                if (q.Length > 0 && !text.Contains(q)) continue;

                var rect = EditorGUILayout.GetControlRect(false, 32f);
                if (i == selected) EditorGUI.DrawRect(rect, new Color(0.24f, 0.37f, 0.59f, 0.5f));
                EditorGUI.LabelField(new Rect(rect.x + 4f, rect.y, rect.width - 8f, 16f), $"{ObjectNames.NicifyVariableName(types[i].Name)}   [{category}]", EditorStyles.boldLabel);
                EditorGUI.LabelField(new Rect(rect.x + 4f, rect.y + 15f, rect.width - 8f, 16f), description, EditorStyles.miniLabel);
                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    selected = i;
                    if (string.IsNullOrEmpty(assetName)) assetName = types[i].Name.Replace("Behavior", "");
                    Event.current.Use();
                    Repaint();
                }
            }
            EditorGUILayout.EndScrollView();

            assetName = EditorGUILayout.TextField("Asset name", assetName);
            family = EditorGUILayout.TextField(new GUIContent("Family folder", "Data/Resources/Enemies/<Family>/. Use \"shared\" for behaviors several families use."), family);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(80f))) Close();
                using (new EditorGUI.DisabledScope(selected < 0))
                    if (GUILayout.Button("Create", GUILayout.Width(80f)))
                    {
                        var asset = EnemyContentCreator.CreateBehaviorAsset(types[selected], assetName, family);
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                        Close();
                    }
            }
        }
    }
}
