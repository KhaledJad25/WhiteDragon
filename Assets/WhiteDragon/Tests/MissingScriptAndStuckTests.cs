using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhiteDragon
{
    public class MissingScriptAndStuckTests
    {
        const string TempFolder = "Assets/WhiteDragon/Data/Resources/Enemies/ZzMissingScriptTest";
        readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.DeleteAsset(TempFolder);
            EnemyCatalog.Reload();
        }

        /// <summary>
        /// A real asset whose script was deleted, written the way Unity saves it: a MonoBehaviour block pointing at a
        /// script GUID that no longer exists (exactly what a deleted behavior class leaves behind).
        /// </summary>
        static string WriteBrokenAsset()
        {
            Directory.CreateDirectory(TempFolder);
            string path = TempFolder + "/Broken.asset";
            File.WriteAllText(path,
                "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n" +
                "  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n" +
                "  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n" +
                "  m_Script: {fileID: 11500000, guid: 0123456789abcdef0123456789abcdef, type: 3}\n" +
                "  m_Name: Broken\n  m_EditorClassIdentifier: WhiteDragon.Runtime::WhiteDragon.DeletedBehavior\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return path;
        }

        [Test]
        public void ContentAssetWithMissingScript_IsError()
        {
            string path = WriteBrokenAsset();
            var issues = ContentValidator.Collect().Where(i => i.Code == "asset.missingscript").ToList();
            Assert.AreEqual(1, issues.Count, "exactly the broken asset");
            Assert.AreEqual(IssueSeverity.Error, issues[0].Severity);
            StringAssert.Contains(path, issues[0].Message);
        }

        [Test]
        public void ShippedContent_HasNoMissingScripts()
        {
            CollectionAssert.IsEmpty(ContentValidator.Collect().Where(i => i.Code == "asset.missingscript").Select(i => i.Message));
        }

        // ---------- Smoke test: stuck enemies ----------

        EnemyDefinition Definition(string id, StateMachineBrain brain)
        {
            var d = ScriptableObject.CreateInstance<EnemyDefinition>();
            d.id = id;
            d.displayName = id;
            d.brain = brain;
            cleanup.Add(d);
            return d;
        }

        StateMachineBrain IdleBrain(bool terminal)
        {
            var b = ScriptableObject.CreateInstance<StateMachineBrain>();
            b.states.Add(new BrainState { name = "Idle", terminal = terminal });
            cleanup.Add(b);
            return b;
        }

        [Test]
        public void SmokeTest_ReportsAStuckEnemy_ButNotAnIntentionallyIdleOne()
        {
            var stuck = Definition("stuck", IdleBrain(terminal: false));
            var idle = Definition("statue", IdleBrain(terminal: true));
            var scene = SceneManager.GetActiveScene();
            var report = EnemySmokeTest.RunAll(() => scene, _ => { },
                new[] { (stuck, (EnemyVariant)null), (idle, (EnemyVariant)null) });

            Assert.AreEqual(2, report.Runs.Count);
            Assert.IsFalse(report.Runs[0].Passed, "never moved and never changed state");
            StringAssert.Contains("never moved and never changed state", string.Join(";", report.Runs[0].Problems));
            Assert.IsTrue(report.Runs[1].Passed, "its start state is marked Terminal: idle on purpose");
            Assert.IsFalse(report.Passed);
        }
    }
}
