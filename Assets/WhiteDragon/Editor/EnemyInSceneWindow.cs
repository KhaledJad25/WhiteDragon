using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Builds a whole enemy GameObject from a definition (and optional variant) in the open scene.</summary>
    public class EnemyInSceneWindow : EditorWindow
    {
        const string RoomTooltip =
            "Adds the enemy to the room selected in the Hierarchy. It is APPENDED to the end of the room's enemy list, " +
            "never inserted: each enemy's random key is room id + its index in that list, so inserting would change the " +
            "other enemies' behavior.";

        EnemyDefinition[] definitions = new EnemyDefinition[0];
        EnemyVariant[] variants = new EnemyVariant[0];
        int definition;
        int variant;
        bool addToRoom = true;

        public static void Open()
        {
            var w = GetWindow<EnemyInSceneWindow>(true, "New Enemy in Scene");
            w.minSize = w.maxSize = new Vector2(380f, 150f);
            w.definitions = EnemyContentCreator.EnemyDefinitions();
            w.variants = ContentCreator.FindAll<EnemyVariant>().OrderBy(v => v.name).ToArray();
        }

        void OnSelectionChange() => Repaint();

        void OnGUI()
        {
            if (definitions.Length == 0)
            {
                EditorGUILayout.HelpBox("No enemy definitions yet. Use Tools/WhiteDragon/New/Enemy first.", MessageType.Info);
                return;
            }
            definition = EditorGUILayout.Popup("Enemy", definition, definitions.Select(EnemyContentCreator.Label).ToArray());
            var def = definitions[Mathf.Clamp(definition, 0, definitions.Length - 1)];
            var usable = variants.Where(v => v.baseEnemy == null || v.baseEnemy == def).ToArray();
            variant = EditorGUILayout.Popup("Variant", Mathf.Min(variant, usable.Length), new[] { "(none)" }.Concat(usable.Select(v => v.name)).ToArray());

            var room = SelectedRoom();
            using (new EditorGUI.DisabledScope(room == null))
                addToRoom = EditorGUILayout.Toggle(new GUIContent(room != null ? $"Append to {room.name}" : "Append to room (select one)", RoomTooltip), addToRoom);
            EditorGUILayout.LabelField(room != null && addToRoom ? "Placed at the room's centre." : "Placed at the Scene view's centre.", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(80f))) Close();
                if (GUILayout.Button("Create", GUILayout.Width(80f)))
                {
                    var target = room != null && addToRoom ? room : null;
                    Vector3 at = target != null ? target.transform.position
                        : SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
                    var chosen = variant > 0 && variant <= usable.Length ? usable[variant - 1] : null;
                    var enemy = EnemyContentCreator.BuildInScene(def, chosen, at, target);
                    Selection.activeGameObject = enemy.gameObject;
                    Close();
                }
            }
        }

        static RoomController SelectedRoom() =>
            Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<RoomController>() : null;
    }
}
