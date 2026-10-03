using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace WhiteDragon
{
    /// <summary>
    /// Runs EVERY enemy definition and variant in EnemyCatalog, one at a time, for 10 simulated seconds against a
    /// scripted player, in a temporary arena scene that is removed afterwards (nothing is saved or left behind).
    /// Fails on any exception or Debug.LogError/Assert, and on an enemy that neither moves nor changes state in its
    /// whole run (unless its brain's start state is marked Terminal: idle on purpose).
    /// Reports managed allocations per frame for each enemy. New enemies are picked up automatically.
    /// Debug only (Application.isEditor || Debug.isDebugBuild).
    /// </summary>
    public static class EnemySmokeTest
    {
        public const float Seconds = 10f;
        public const float Step = 1f / 60f;
        static readonly Vector3 Arena = new Vector3(20000f, 0f, 20000f);

        public class Run
        {
            public string Name;
            public readonly List<string> Problems = new List<string>();
            public float Moved;
            public readonly List<string> States = new List<string>();
            public double BytesPerFrame;
            public bool Passed => Problems.Count == 0;
        }

        public class Report
        {
            public readonly List<Run> Runs = new List<Run>();
            public double Milliseconds;
            public bool Passed => Runs.TrueForAll(r => r.Passed);

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[Smoke] {(Passed ? "PASSED" : "FAILED")}: {Runs.Count} enemies x {Seconds:0} simulated s in {Milliseconds:0} ms");
                foreach (var r in Runs)
                    sb.AppendLine($"[Smoke]   {(r.Passed ? "ok  " : "FAIL")} {r.Name}: moved {r.Moved:0.0} m, states [{string.Join(" ", r.States)}], " +
                                  $"{(r.BytesPerFrame < 0 ? "allocations unknown (a GC ran)" : r.BytesPerFrame.ToString("0") + " B/frame")}" +
                                  $"{(r.Passed ? "" : " | " + string.Join("; ", r.Problems))}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Runs all enemies. createScene gives the arena scene and closeScene removes it (the Play mode menu makes a
        /// temporary scene). Every object the test makes is also destroyed one by one, so passing the open scene and a
        /// no-op close (as the edit-mode test does) leaves it exactly as it was. The active scene is restored.
        /// </summary>
        public static Report RunAll(Func<Scene> createScene, Action<Scene> closeScene,
            IEnumerable<(EnemyDefinition definition, EnemyVariant variant)> subjects = null)
        {
            var report = new Report();
            if (!(Application.isEditor || Debug.isDebugBuild)) return report;

            var previousActive = SceneManager.GetActiveScene();
            var arena = createScene();
            SceneManager.SetActiveScene(arena);
            var clock = Stopwatch.StartNew();
            GameObject floor = null;
            try
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "SmokeFloor";
                floor.transform.position = Arena + new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(60f, 1f, 60f);

                if (subjects != null)
                {
                    // An explicit list (tests); otherwise every definition and variant in the catalog.
                    foreach (var (definition, variant) in subjects)
                        report.Runs.Add(RunOne(definition, variant, report.Runs.Count));
                    return report;
                }
                foreach (var def in EnemyCatalog.All)
                    report.Runs.Add(RunOne(def, null, report.Runs.Count));
                foreach (var variant in EnemyCatalog.Variants)
                {
                    if (variant.baseEnemy == null)
                    {
                        var skipped = new Run { Name = variant.name };
                        skipped.Problems.Add("no base enemy, nothing to test it on");
                        report.Runs.Add(skipped);
                        continue;
                    }
                    report.Runs.Add(RunOne(variant.baseEnemy, variant, report.Runs.Count));
                }
            }
            finally
            {
                // Everything it made is removed one by one, so nothing is left even if the arena is an open scene.
                if (floor != null) Destroy(floor);
                report.Milliseconds = clock.Elapsed.TotalMilliseconds;
                if (previousActive.IsValid()) SceneManager.SetActiveScene(previousActive);
                closeScene(arena);
            }
            return report;
        }

        static Run RunOne(EnemyDefinition def, EnemyVariant variant, int index)
        {
            var run = new Run { Name = def.name + (variant != null ? " + " + variant.name : "") };
            void OnLog(string message, string stack, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    run.Problems.Add($"{type}: {message}");
            }
            Application.logMessageReceived += OnLog;
            GameObject player = null;
            Enemy enemy = null;
            try
            {
                player = new GameObject("SmokePlayer");
                player.SetActive(false);
                player.transform.position = Arena;
                var cc = player.AddComponent<CharacterController>();
                cc.radius = 0.35f;
                cc.height = 1.8f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                var health = player.AddComponent<PlayerHealth>();
                health.startingContainers = 1000;
                player.SetActive(true);

                enemy = EnemySpawner.Spawn(def, variant, Arena + new Vector3(0f, def.movement == MovementMode.Flying ? 1.5f : 0f, 8f), "smoke:" + index);
                enemy.enabled = false; // ticked here at a fixed step, not by Update
                enemy.SetTarget(health);
                Physics.SyncTransforms();

                Vector3 start = enemy.transform.position;
                string state = "";
                int frames = Mathf.RoundToInt(Seconds / Step);
                const int WarmUp = 60; // the first second includes one-time setup (brain state, pooled shots)
                long heapAtStart = 0;
                int collectionsAtStart = 0;
                for (int i = 0; i < frames && !enemy.IsDead; i++)
                {
                    if (i == WarmUp)
                    {
                        heapAtStart = Profiler.GetMonoUsedSizeLong();
                        collectionsAtStart = GC.CollectionCount(0);
                    }
                    float t = i * Step;
                    player.transform.position = Arena + new Vector3(Mathf.Cos(t * 0.5f) * 4f, 0f, Mathf.Sin(t * 0.5f) * 4f);
                    Physics.SyncTransforms();
                    enemy.Tick(Step);
                    run.Moved = Mathf.Max(run.Moved, Vector3.Distance(start, enemy.transform.position));
                    if (enemy.DebugLabel != state)
                    {
                        state = enemy.DebugLabel;
                        if (!run.States.Contains(state)) run.States.Add(state);
                    }
                }
                // Managed heap growth over the steady frames. The heap grows in blocks, so this resolves to about
                // 8 bytes per frame over 540 frames; a garbage collection in between makes it unusable (reported).
                if (GC.CollectionCount(0) == collectionsAtStart)
                    run.BytesPerFrame = (double)Math.Max(0L, Profiler.GetMonoUsedSizeLong() - heapAtStart) / Math.Max(1, frames - WarmUp);
                else
                    run.BytesPerFrame = -1;
                // Stuck: no movement and no state change for the whole run, unless its brain is idle on purpose
                // (its start state is marked Terminal).
                bool idleOnPurpose = enemy.Brain is StateMachineBrain sm && sm.states.Count > 0 && sm.states[0].terminal;
                if (run.Moved < 0.5f && run.States.Count <= 1 && !idleOnPurpose)
                    run.Problems.Add("never moved and never changed state");
            }
            catch (Exception e)
            {
                run.Problems.Add($"Exception: {e.GetType().Name}: {e.Message}");
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
                if (enemy != null)
                {
                    Projectile.DespawnOwnedBy(enemy.transform);
                    Destroy(enemy.gameObject);
                }
                if (player != null) Destroy(player);
            }
            return run;
        }

        static void Destroy(GameObject go)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
