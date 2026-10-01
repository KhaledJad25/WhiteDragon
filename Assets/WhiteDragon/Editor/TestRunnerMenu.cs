using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Runs the WhiteDragon EditMode tests and logs a one-line summary plus any failures.</summary>
    public static class TestRunnerMenu
    {
        static TestRunnerApi api;

        [MenuItem("Tools/WhiteDragon/Run Tests")]
        public static void RunAll()
        {
            if (api == null)
            {
                api = ScriptableObject.CreateInstance<TestRunnerApi>();
                api.hideFlags = HideFlags.HideAndDontSave;
                api.RegisterCallbacks(new Callbacks());
            }
            var filter = new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "WhiteDragon.Tests" } };
            api.Execute(new ExecutionSettings(filter));
        }

        class Callbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                var failures = new StringBuilder();
                CollectFailures(result, failures);
                string summary = $"[Tests] Passed {result.PassCount}, Failed {result.FailCount}, Skipped {result.SkipCount + result.InconclusiveCount}";
                if (result.FailCount > 0) Debug.LogError(summary + "\n" + failures);
                else Debug.Log(summary);
            }

            static void CollectFailures(ITestResultAdaptor r, StringBuilder sb)
            {
                if (r.HasChildren)
                {
                    foreach (var child in r.Children) CollectFailures(child, sb);
                    return;
                }
                if (r.TestStatus == TestStatus.Failed)
                    sb.AppendLine($"{r.FullName}: {r.Message}\n{r.StackTrace}");
            }
        }
    }
}
