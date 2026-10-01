using System;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Small popup asking for a name, with an optional choice list. Enter creates, Escape cancels.</summary>
    public class NamePromptWindow : EditorWindow
    {
        string value;
        string choiceLabel;
        string[] choices;
        int choice;
        Action<string, int> onCreate;
        bool focusRequested;

        public static void Show(string title, string defaultName, Action<string> onCreate) =>
            Show(title, defaultName, null, null, (name, _) => onCreate(name));

        public static void Show(string title, string defaultName, string choiceLabel, string[] choices, Action<string, int> onCreate)
        {
            var w = CreateInstance<NamePromptWindow>();
            w.titleContent = new GUIContent(title);
            w.value = defaultName;
            w.choiceLabel = choiceLabel;
            w.choices = choices;
            w.onCreate = onCreate;
            var size = new Vector2(360f, choices != null ? 96f : 74f);
            var main = EditorGUIUtility.GetMainWindowPosition();
            w.position = new Rect(main.center - size * 0.5f, size);
            w.minSize = w.maxSize = size;
            w.ShowUtility();
        }

        void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)) { Submit(); e.Use(); return; }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { Close(); e.Use(); return; }

            EditorGUILayout.Space(4f);
            if (choices != null)
                choice = EditorGUILayout.Popup(choiceLabel, choice, choices);
            GUI.SetNextControlName("NameField");
            value = EditorGUILayout.TextField("Name", value);
            if (!focusRequested)
            {
                focusRequested = true;
                EditorGUI.FocusTextInControl("NameField");
            }

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(80f))) Close();
                if (GUILayout.Button("Create", GUILayout.Width(80f))) Submit();
            }
        }

        void Submit()
        {
            var callback = onCreate;
            string name = value?.Trim();
            int picked = choice;
            Close();
            callback?.Invoke(name, picked);
        }
    }
}
