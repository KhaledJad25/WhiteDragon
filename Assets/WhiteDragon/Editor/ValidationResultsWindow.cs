using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Lists validation results. Click a row to select and ping the asset.</summary>
    public class ValidationResultsWindow : EditorWindow
    {
        List<ContentIssue> issues = new List<ContentIssue>();
        Vector2 scroll;
        GUIContent errorIcon, warningIcon;

        public static void ShowResults(List<ContentIssue> results)
        {
            var w = GetWindow<ValidationResultsWindow>("Content Validation");
            w.issues = results.OrderBy(i => i.Severity).ToList();
            w.Repaint();
        }

        void OnGUI()
        {
            errorIcon ??= EditorGUIUtility.IconContent("console.erroricon.sml");
            warningIcon ??= EditorGUIUtility.IconContent("console.warnicon.sml");

            int errors = issues.Count(i => i.Severity == IssueSeverity.Error);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label($"{errors} error(s), {issues.Count - errors} warning(s)");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Validate again", EditorStyles.toolbarButton)) ContentValidator.Run();
            }
            if (issues.Count == 0) EditorGUILayout.HelpBox("No problems found.", MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var issue in issues)
            {
                var rect = EditorGUILayout.GetControlRect(false, 20f);
                if (issue.Asset != null && Selection.activeObject == issue.Asset)
                    EditorGUI.DrawRect(rect, new Color(0.24f, 0.37f, 0.59f, 0.5f));
                var icon = issue.Severity == IssueSeverity.Error ? errorIcon : warningIcon;
                GUI.Label(new Rect(rect.x, rect.y, 20f, rect.height), icon);
                EditorGUI.LabelField(new Rect(rect.x + 22f, rect.y, rect.width - 22f, rect.height), issue.Message);

                var e = Event.current;
                if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && issue.Asset != null)
                {
                    Selection.activeObject = issue.Asset;
                    EditorGUIUtility.PingObject(issue.Asset);
                    e.Use();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
