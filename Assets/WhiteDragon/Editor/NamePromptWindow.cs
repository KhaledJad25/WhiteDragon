using System;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Small popup asking for a name, with an optional choice list or second text field. Enter creates, Escape cancels.</summary>
    public class NamePromptWindow : EditorWindow
    {
        string value;
        string choiceLabel;
        string[] choices;
        int choice;
        string secondLabel;
        string secondValue;
        string secondTooltip;
        Action<string, int> onCreate;
        Action<string, string> onCreateWithSecond;
        bool focusRequested;

        public static void Show(string title, string defaultName, Action<string> onCreate) =>
            Show(title, defaultName, null, null, (name, _) => onCreate(name));

        /// <summary>Asks for a name and a second text value (for example a family, or an optional enemy name).</summary>
        public static void Show(string title, string defaultName, string secondLabel, string secondDefault, string secondTooltip, Action<string, string> onCreate)
        {
            var w = Open(title, defaultName, 96f);
            w.secondLabel = secondLabel;
            w.secondValue = secondDefault;
            w.secondTooltip = secondTooltip;
            w.onCreateWithSecond = onCreate;
        }

        public static void Show(string title, string defaultName, string choiceLabel, string[] choices, Action<string, int> onCreate)
        {
            var w = Open(title, defaultName, choices != null ? 96f : 74f);
            w.choiceLabel = choiceLabel;
            w.choices = choices;
            w.onCreate = onCreate;
        }

        static NamePromptWindow Open(string title, string defaultName, float height)
        {
            var w = CreateInstance<NamePromptWindow>();
            w.titleContent = new GUIContent(title);
            w.value = defaultName;
            var size = new Vector2(360f, height);
            var main = EditorGUIUtility.GetMainWindowPosition();
            w.position = new Rect(main.center - size * 0.5f, size);
            w.minSize = w.maxSize = size;
            w.ShowUtility();
            return w;
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
            if (secondLabel != null)
                secondValue = EditorGUILayout.TextField(new GUIContent(secondLabel, secondTooltip), secondValue);

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
            var callbackWithSecond = onCreateWithSecond;
            string name = value?.Trim();
            string second = secondValue?.Trim() ?? "";
            int picked = choice;
            Close();
            callback?.Invoke(name, picked);
            callbackWithSecond?.Invoke(name, second);
        }
    }
}
