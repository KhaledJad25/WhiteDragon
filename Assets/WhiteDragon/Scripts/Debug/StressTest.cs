using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Debug-only stress harness (editor and debug builds, runtime check). Fires rocks, spawns enemies,
    /// caps the frame rate, and measures frame time, script time, garbage per frame and live counts.
    /// Run with "-wdStress" on the command line to run the baseline suite and the frame-rate check, then quit.
    /// </summary>
    public class StressTest : MonoBehaviour
    {
        public const int Window = 120;

        [Header("Live settings")]
        public float rocksPerSecond;
        [Tooltip("If above 0, spawn whatever is needed each frame to keep this many rocks alive (replaces rocks that hit things).")]
        public int keepRocksAlive;
        public float spreadDegrees = 60f;
        [Tooltip("Degrees above horizontal the rocks are aimed. Steep (80) keeps homing rocks searching without finding ground enemies.")]
        public float aimPitch = 45f;
        public float rockLifetime = 3f;
        public int enemyCount;
        public bool homingAndBurn;

        public static bool Allowed => Application.isEditor || Debug.isDebugBuild;

        // GPU frame time from FrameTimingManager (needs Player Settings > Frame Timing Stats; else "n/a").
        static readonly FrameTiming[] frameTimings = new FrameTiming[1];

        public float AverageMs { get; private set; }
        public float WorstMs { get; private set; }
        public float ScriptMs { get; private set; }
        public float MainThreadMs { get; private set; }
        public long GcBytes { get; private set; }
        public long Batches { get; private set; }
        public bool SuiteRunning { get; private set; }
        public string LastReport { get; private set; } = "";

        readonly float[] frameMs = new float[Window];
        int frameIndex, frameFilled;
        ProfilerRecorder gcRecorder, updateRecorder, lateRecorder, batchesRecorder, mainThreadRecorder;

        // Built-in profiler markers used to rank costs (time and call count per frame).
        static readonly (string category, string marker, string label)[] BreakdownMarkers =
        {
            ("Physics", "Physics.SphereCastAll", "SphereCastAll"),
            ("Physics", "Physics.OverlapSphere", "OverlapSphere"),
            ("Other", "Destroy", "Destroy"),
            ("Memory", "GC.Alloc", "GC.Alloc"),
        };
        ProfilerRecorder[] breakdown;

        float spawnCarry;
        int spawnIndex;
        ShotRecipe plainRecipe, effectRecipe;
        readonly List<Enemy> enemies = new List<Enemy>();
        EnemyDefinition enemyDef;
        StatusEffectDefinition burn;
        float burnTimer;
        Transform player;
        PlayerHealth playerHealth;

        public static int ActiveEnemies()
        {
            int n = 0;
            foreach (var e in Enemy.Live)
                if (e != null && !e.IsDead && !e.IsDormant) n++;
            return n;
        }

        void Awake()
        {
            if (!Allowed)
            {
                enabled = false;
                return;
            }
            gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            updateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "BehaviourUpdate");
            lateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "LateBehaviourUpdate");
            batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            breakdown = new ProfilerRecorder[BreakdownMarkers.Length];
            for (int i = 0; i < breakdown.Length; i++)
                breakdown[i] = ProfilerRecorder.StartNew(new ProfilerCategory(BreakdownMarkers[i].category), BreakdownMarkers[i].marker);

            plainRecipe = new ShotRecipe { Damage = 1f, Speed = 10f, Range = 100f };
            effectRecipe = plainRecipe.Clone();
            effectRecipe.AddEffect(Resources.Load<ShotEffect>("Effects/Homing"));
            effectRecipe.AddEffect(Resources.Load<ShotEffect>("Effects/ApplyBurn"));
            burn = Resources.Load<StatusEffectDefinition>("Statuses/Burn");
            var ghoul = EnemyCatalog.Find("ghoul");
            if (ghoul != null)
            {
                enemyDef = Instantiate(ghoul);
                enemyDef.maxHealth = 1e9f;
            }
        }

        void Start()
        {
            // "-wdNoOutline": outline renderer feature off for this run, to measure what it costs.
            if (Allowed && Array.IndexOf(Environment.GetCommandLineArgs(), "-wdNoOutline") >= 0)
            {
                var rig = FindAnyObjectByType<LookRig>();
                if (rig != null && rig.outline != null)
                {
                    rig.outline.SetActive(false);
                    Debug.Log($"[Stress] -wdNoOutline: outline feature active = {rig.outline.isActive}");
                    rig.outline = null;
                }
                else Debug.LogWarning("[Stress] -wdNoOutline: no LookRig outline found");
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-wdStress") >= 0)
                StartCoroutine(RunAll(quitAfter: true));
            else if (Array.IndexOf(Environment.GetCommandLineArgs(), "-wdProfile") >= 0)
                StartCoroutine(RunProfileCaptures(quitAfter: true));
        }

        void OnDestroy()
        {
            gcRecorder.Dispose();
            updateRecorder.Dispose();
            lateRecorder.Dispose();
            batchesRecorder.Dispose();
            mainThreadRecorder.Dispose();
            if (breakdown != null)
                foreach (var r in breakdown) r.Dispose();
        }

        void Update()
        {
            RecordFrame();
            if (player == null)
            {
                var pc = FindAnyObjectByType<PlayerController>();
                if (pc == null) return;
                player = pc.transform;
                playerHealth = pc.GetComponent<PlayerHealth>();
            }
            SpawnRocks(Time.unscaledDeltaTime);
            MaintainEnemies(Time.unscaledDeltaTime);
        }

        // ---------- Measurement ----------

        void RecordFrame()
        {
            frameMs[frameIndex] = Time.unscaledDeltaTime * 1000f;
            frameIndex = (frameIndex + 1) % Window;
            if (frameFilled < Window) frameFilled++;
            float sum = 0f, worst = 0f;
            for (int i = 0; i < frameFilled; i++)
            {
                sum += frameMs[i];
                if (frameMs[i] > worst) worst = frameMs[i];
            }
            AverageMs = sum / frameFilled;
            WorstMs = worst;
            GcBytes = gcRecorder.Valid ? gcRecorder.LastValue : -1;
            ScriptMs = ((updateRecorder.Valid ? updateRecorder.LastValue : 0) + (lateRecorder.Valid ? lateRecorder.LastValue : 0)) / 1e6f;
            MainThreadMs = mainThreadRecorder.Valid ? mainThreadRecorder.LastValue / 1e6f : -1f;
            Batches = batchesRecorder.Valid ? batchesRecorder.LastValue : -1;
        }

        public static void SetFrameCap(int fps)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
        }

        // ---------- Load ----------

        void SpawnRocks(float dt)
        {
            int n;
            if (keepRocksAlive > 0)
            {
                n = Mathf.Min(40, keepRocksAlive - Projectile.LiveCount);
            }
            else if (rocksPerSecond > 0f)
            {
                spawnCarry += rocksPerSecond * dt;
                n = (int)spawnCarry;
                spawnCarry -= n;
            }
            else
            {
                spawnCarry = 0f;
                return;
            }
            if (n <= 0) return;
            var recipe = homingAndBurn ? effectRecipe : plainRecipe;
            // Steep aim launches from above the enemies crowding the player, so rocks are not hit on spawn.
            Vector3 origin = player.position + Vector3.up * (aimPitch > 60f ? 4f : 1.6f) + player.forward * 0.6f;
            Quaternion aim = Quaternion.LookRotation(player.forward) * Quaternion.Euler(-aimPitch, 0f, 0f);
            for (int k = 0; k < n; k++)
            {
                // Golden-angle spiral inside the spread cone: deterministic, no randomness.
                int i = spawnIndex++;
                float angle = i * 137.508f * Mathf.Deg2Rad;
                float radius = Mathf.Sqrt(((i % 64) + 0.5f) / 64f) * spreadDegrees * 0.5f;
                Vector3 dir = aim * Quaternion.Euler(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, 0f) * Vector3.forward;
                Projectile.Spawn(recipe, origin, dir, player).lifetime = rockLifetime;
            }
        }

        void MaintainEnemies(float dt)
        {
            enemies.RemoveAll(e => e == null);
            if (enemyDef == null) return;
            while (enemies.Count < enemyCount) enemies.Add(SpawnEnemy(enemies.Count));
            while (enemies.Count > enemyCount)
            {
                Destroy(enemies[enemies.Count - 1].gameObject);
                enemies.RemoveAt(enemies.Count - 1);
            }

            // Keep the player alive under load without touching invincibility.
            if (playerHealth != null && enemies.Count > 0 && playerHealth.State.Soul < 10)
                playerHealth.State.AddSoul(20);

            if (!homingAndBurn || burn == null || enemies.Count == 0) return;
            burnTimer -= dt;
            if (burnTimer > 0f) return;
            burnTimer = 1f;
            foreach (var e in enemies) e.GetComponent<StatusReceiver>().Apply(burn, 1);
        }

        Enemy SpawnEnemy(int index)
        {
            var go = new GameObject("StressEnemy");
            go.SetActive(false);
            float a = index * 137.508f * Mathf.Deg2Rad;
            go.transform.position = player.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 10f + Vector3.up * 0.05f;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            AddShape(go.transform, PrimitiveType.Capsule, new Vector3(0f, 0.7f, 0f), new Vector3(0.55f, 0.7f, 0.55f));
            AddShape(go.transform, PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0.1f), Vector3.one * 0.8f);
            var enemy = go.AddComponent<Enemy>();
            enemy.definition = enemyDef;
            go.SetActive(true);
            return enemy;
        }

        static void AddShape(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale)
        {
            var s = GameObject.CreatePrimitive(type);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(parent, false);
            s.transform.localPosition = pos;
            s.transform.localScale = scale;
            s.GetComponent<Renderer>().sharedMaterial = PlaceholderMaterials.Lit(new Color(0.4f, 0.36f, 0.32f));
        }

        public void StopAll()
        {
            rocksPerSecond = 0f;
            keepRocksAlive = 0;
            enemyCount = 0;
            homingAndBurn = false;
        }

        // ---------- Suite ----------

        public IEnumerator RunAll(bool quitAfter)
        {
            yield return RunSuite();
            var check = GetComponent<FrameRateCheck>();
            if (check != null) yield return check.Run();
            if (!Application.isEditor)
                File.WriteAllText(Path.Combine(Application.dataPath, "..", "wd_stress_results.txt"),
                    LastReport + "\n" + (check != null ? check.LastReport : ""));
            if (quitAfter && !Application.isEditor) Application.Quit();
        }

        /// <summary>
        /// Records profiler captures (.raw, next to the exe) of the 100-rock case and the steady heavy case,
        /// for loading in the Profiler window. Run with "-wdProfile".
        /// </summary>
        public IEnumerator RunProfileCaptures(bool quitAfter)
        {
            SetFrameCap(-1);
            string dir = Path.Combine(Application.dataPath, "..");
            yield return ProfileScenario(Path.Combine(dir, "profile_100rocks"), 100, 0, false, false, 3f);
            yield return ProfileScenario(Path.Combine(dir, "profile_heavy"), 600, 20, true, true, 10f);
            if (quitAfter && !Application.isEditor) Application.Quit();
        }

        IEnumerator ProfileScenario(string file, int rocks, int enemies, bool effects, bool steep, float seconds)
        {
            keepRocksAlive = rocks;
            enemyCount = enemies;
            homingAndBurn = effects;
            aimPitch = steep ? 80f : 45f;
            spreadDegrees = steep ? 15f : 60f;
            yield return new WaitForSecondsRealtime(rockLifetime + 1.5f);

            UnityEngine.Profiling.Profiler.maxUsedMemory = 512 * 1024 * 1024;
            UnityEngine.Profiling.Profiler.logFile = file;
            UnityEngine.Profiling.Profiler.enableBinaryLog = true;
            UnityEngine.Profiling.Profiler.enabled = true;
            yield return new WaitForSecondsRealtime(seconds);
            UnityEngine.Profiling.Profiler.enabled = false;
            UnityEngine.Profiling.Profiler.enableBinaryLog = false;
            UnityEngine.Profiling.Profiler.logFile = "";

            StopAll();
            aimPitch = 45f;
            spreadDegrees = 60f;
            yield return new WaitForSecondsRealtime(rockLifetime + 0.5f);
        }

        public static string Hardware() =>
            $"{SystemInfo.processorType} ({SystemInfo.processorCount} threads), {SystemInfo.systemMemorySize} MB RAM, " +
            $"{SystemInfo.graphicsDeviceName} ({SystemInfo.graphicsDeviceType}), {Screen.width}x{Screen.height}, " +
            $"Unity {Application.unityVersion}, {(Application.isEditor ? "Editor" : Debug.isDebugBuild ? "Development build" : "Release build")}";

        public IEnumerator RunSuite()
        {
            SuiteRunning = true;
            var sb = new StringBuilder();
            sb.AppendLine("[Stress] " + Hardware());
            sb.AppendLine($"[Stress] recorders valid: gc={gcRecorder.Valid} update={updateRecorder.Valid} late={lateRecorder.Valid} batches={batchesRecorder.Valid} mainThread={mainThreadRecorder.Valid}");
            SetFrameCap(-1);
            // rocks = live rocks to keep alive; rate > 0 instead fires that many per second (rocks then = 0).
            // steep: aim almost straight up so 600 homing rocks stay alive and keep searching (the steady heaviest case).
            // noGui: OnGUI components off, to show how much garbage IMGUI itself makes.
            var scenarios = new (int rocks, float rate, int enemies, bool effects, bool steep, bool noGui)[]
            {
                (100, 0f, 0, false, false, false), (300, 0f, 0, false, false, false), (600, 0f, 0, false, false, false),
                (300, 0f, 20, false, false, false), (300, 0f, 20, true, false, false), (600, 0f, 20, true, false, false),
                (0, 200f, 20, true, false, false),
                (150, 0f, 20, true, true, false), // realistic heavy tier
                (600, 0f, 20, true, true, false), // extreme ceiling tier
                (600, 0f, 20, true, true, true),
            };
            var samples = new float[20000];
            var gui = new List<Behaviour>();
            foreach (var mb in FindObjectsByType<MonoBehaviour>())
                if (mb is Crosshair || mb is HeartsHUD || mb is DeathScreen || mb is DebugPanel) gui.Add(mb);
            foreach (var s in scenarios)
            {
                keepRocksAlive = s.rocks;
                rocksPerSecond = s.rate;
                enemyCount = s.enemies;
                homingAndBurn = s.effects;
                aimPitch = s.steep ? 80f : 45f;
                spreadDegrees = s.steep ? 15f : 60f;
                foreach (var g in gui) g.enabled = !s.noGui;
                yield return new WaitForSecondsRealtime(rockLifetime + 1.5f);

                int frames = 0;
                double sumMs = 0, sumScript = 0, sumGc = 0, sumMain = 0, sumBatches = 0, sumRocks = 0, sumEnemies = 0;
                double sumBursts = 0, sumNumbers = 0, sumGpu = 0;
                int gpuFrames = 0;
                var markerMs = new double[breakdown.Length];
                var markerCalls = new double[breakdown.Length];
                float worst = 0f;
                float end = Time.unscaledTime + 5f;
                while (Time.unscaledTime < end)
                {
                    yield return null;
                    float ms = Time.unscaledDeltaTime * 1000f;
                    if (frames < samples.Length) samples[frames] = ms;
                    frames++;
                    sumMs += ms;
                    worst = Mathf.Max(worst, ms);
                    sumScript += ScriptMs;
                    sumGc += Mathf.Max(0, GcBytes);
                    sumMain += MainThreadMs;
                    sumBatches += Batches;
                    sumRocks += Projectile.LiveCount;
                    sumEnemies += ActiveEnemies();
                    sumBursts += GameFeel.ParticlesAlive;
                    sumNumbers += DamageNumber.LiveCount;
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, frameTimings) > 0 && frameTimings[0].gpuFrameTime > 0)
                    {
                        sumGpu += frameTimings[0].gpuFrameTime;
                        gpuFrames++;
                    }
                    for (int m = 0; m < breakdown.Length; m++)
                    {
                        if (!breakdown[m].Valid || breakdown[m].Count == 0) continue;
                        var sample = breakdown[m].GetSample(breakdown[m].Count - 1);
                        markerMs[m] += sample.Value / 1e6;
                        markerCalls[m] += sample.Count;
                    }
                }
                var parts = new StringBuilder();
                for (int m = 0; m < breakdown.Length; m++)
                    parts.Append(breakdown[m].Valid
                        ? $" {BreakdownMarkers[m].label} {markerMs[m] / frames:0.00} ms x{markerCalls[m] / frames:0}"
                        : $" {BreakdownMarkers[m].label} n/a");
                float avg = (float)(sumMs / frames);
                int over33 = 0;
                for (int i = 0; i < Mathf.Min(frames, samples.Length); i++) if (samples[i] > 33.3f) over33++;
                string line =
                    $"[Stress] {(s.rate > 0f ? $"fire {s.rate:0}/s" : $"keep {s.rocks,3} alive")} enemies={s.enemies,2} homing+burn={(s.effects ? "yes" : "no ")}{(s.steep ? " aimed up" : "")}{(s.noGui ? " OnGUI off" : "")} | " +
                    $"live rocks {sumRocks / frames:0} enemies {sumEnemies / frames:0} | avg {avg:0.00} ms ({1000f / avg:0} fps) " +
                    $"worst {worst:0.00} ms, >33ms: {over33} | main thread {sumMain / frames:0.00} ms | scripts {sumScript / frames:0.00} ms | " +
                    $"gpu {(gpuFrames > 0 ? $"{sumGpu / gpuFrames:0.00} ms" : "n/a")} | GC {sumGc / frames:0} B/frame | frames {frames}\n" +
                    $"[Stress]     per frame:{parts} | burst particles alive {sumBursts / frames:0}, damage numbers alive {sumNumbers / frames:0}, recycled by cap {Projectile.RecycledByCap}";
                Debug.Log(line);
                sb.AppendLine(line);
            }
            StopAll();
            aimPitch = 45f;
            spreadDegrees = 60f;
            foreach (var g in gui) if (g != null) g.enabled = true;
            yield return new WaitForSecondsRealtime(rockLifetime + 0.5f);
            LastReport = sb.ToString();
            SuiteRunning = false;
        }

        // ---------- Panel ----------

        public void DrawGui()
        {
            GUILayout.Label($"Frame avg {AverageMs:0.00} ms ({(AverageMs > 0f ? 1000f / AverageMs : 0f):0} fps), worst {WorstMs:0.00} ms (last {Window})");
            GUILayout.Label($"Main thread {MainThreadMs:0.00} ms, scripts {ScriptMs:0.00} ms, GC {GcBytes} B/frame, batches {Batches}");
            GUILayout.Label($"Live: rocks {Projectile.LiveCount}, damage numbers {DamageNumber.LiveCount}, burst particles {GameFeel.ParticlesAlive}, active enemies {ActiveEnemies()}, recycled by cap {Projectile.RecycledByCap}");

            GUILayout.Label($"Rocks per second: {rocksPerSecond:0}   (or keep alive: {keepRocksAlive})");
            rocksPerSecond = Mathf.Round(GUILayout.HorizontalSlider(rocksPerSecond, 0f, 400f));
            keepRocksAlive = Mathf.RoundToInt(GUILayout.HorizontalSlider(keepRocksAlive, 0f, 1200f) / 50f) * 50;
            GUILayout.Label($"Spread: {spreadDegrees:0} deg   Lifetime: {rockLifetime:0.0} s");
            spreadDegrees = Mathf.Round(GUILayout.HorizontalSlider(spreadDegrees, 0f, 120f));
            rockLifetime = Mathf.Round(GUILayout.HorizontalSlider(rockLifetime, 0.5f, 6f) * 2f) / 2f;
            GUILayout.Label($"Enemies: {enemyCount}");
            enemyCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(enemyCount, 0f, 40f));
            homingAndBurn = GUILayout.Toggle(homingAndBurn, "Rocks home and enemies burn");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("30 fps")) SetFrameCap(30);
            if (GUILayout.Button("60 fps")) SetFrameCap(60);
            if (GUILayout.Button("Uncapped")) SetFrameCap(-1);
            if (GUILayout.Button("Stop load")) StopAll();
            GUILayout.EndHorizontal();

            var check = GetComponent<FrameRateCheck>();
            GUILayout.BeginHorizontal();
            GUI.enabled = !SuiteRunning && (check == null || !check.Running);
            if (GUILayout.Button("Run baseline suite")) StartCoroutine(RunSuite());
            if (check != null && GUILayout.Button("Run frame-rate check")) StartCoroutine(check.Run());
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            if (LastReport.Length > 0) GUILayout.Label(LastReport);
            if (check != null && check.LastReport.Length > 0) GUILayout.Label(check.LastReport);
        }
    }
}
