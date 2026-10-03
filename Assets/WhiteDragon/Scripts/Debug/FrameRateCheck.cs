using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace WhiteDragon
{
    /// <summary>
    /// Debug-only check that gameplay results match at 30 and 60 fps: fire rate, burn damage, status
    /// duration, invincibility, homing, rock range and enemy speed. Reports numbers; changes nothing.
    /// </summary>
    public class FrameRateCheck : MonoBehaviour
    {
        public bool Running { get; private set; }
        public string LastReport { get; private set; } = "";

        readonly List<string> keys = new List<string>();
        readonly Dictionary<string, float[]> values = new Dictionary<string, float[]>();
        readonly List<GameObject> temp = new List<GameObject>();

        void Put(string key, int fpsIndex, float value)
        {
            if (!values.TryGetValue(key, out var v))
            {
                values[key] = v = new[] { float.NaN, float.NaN };
                keys.Add(key);
            }
            v[fpsIndex] = value;
        }

        public IEnumerator Run()
        {
            if (!StressTest.Allowed || Running) yield break;
            Running = true;
            keys.Clear();
            values.Clear();
            var playerCtrl = FindAnyObjectByType<PlayerController>();
            var thrower = playerCtrl.GetComponent<RockThrower>();
            var stats = playerCtrl.GetComponent<PlayerStats>().Stats;
            var dummies = FindObjectsByType<TargetDummy>();
            foreach (var d in dummies) d.gameObject.SetActive(false);

            int[] rates = { 30, 60 };
            for (int idx = 0; idx < rates.Length; idx++)
            {
                StressTest.SetFrameCap(rates[idx]);
                yield return new WaitForSecondsRealtime(1.5f);
                int frames = 0;
                float t0 = Time.unscaledTime;
                while (Time.unscaledTime - t0 < 1f) { frames++; yield return null; }
                Put("actual fps", idx, frames / (Time.unscaledTime - t0));

                foreach (float rate in new[] { 2f, 6f, 10f })
                    yield return FireRate(thrower, stats, rate, idx);
                yield return StatusTiming("Statuses/Burn", "burn", idx);
                yield return StatusTiming("Statuses/Slow", "slow", idx);
                yield return Invincibility(idx);
                yield return Homing(idx);
                yield return Range(idx);
                yield return EnemySpeed(idx);
            }

            StressTest.SetFrameCap(-1);
            foreach (var d in dummies) if (d != null) d.gameObject.SetActive(true);
            foreach (var go in temp) if (go != null) Destroy(go);
            temp.Clear();

            var sb = new StringBuilder();
            sb.AppendLine("[FrameCheck] " + StressTest.Hardware());
            foreach (var k in keys)
            {
                var v = values[k];
                float diff = Mathf.Abs(v[0]) > 1e-6f ? (v[1] - v[0]) / v[0] * 100f : 0f;
                sb.AppendLine($"[FrameCheck] {k,-44} 30fps {v[0],9:0.###}   60fps {v[1],9:0.###}   diff {diff,6:0.0}%");
            }
            LastReport = sb.ToString();
            Debug.Log(LastReport);
            Running = false;
        }

        IEnumerator FireRate(RockThrower thrower, StatBlock stats, float rate, int idx)
        {
            stats.SetBase(StatType.FireRate, rate);
            int count = 0;
            Action<ShotRecipe> onShot = _ => count++;
            thrower.ShotBuilt += onShot;
            CursorState.SetLocked(true);
            if (Mouse.current != null)
                InputSystem.QueueStateEvent(Mouse.current, new MouseState().WithButton(MouseButton.Left, true));
            yield return new WaitForSeconds(0.5f);
            count = 0;
            float t0 = Time.time;
            while (Time.time - t0 < 5f) yield return null;
            float duration = Time.time - t0;
            if (Mouse.current != null) InputSystem.QueueStateEvent(Mouse.current, new MouseState());
            thrower.ShotBuilt -= onShot;
            stats.SetBase(StatType.FireRate, StatBlock.DefaultBase(StatType.FireRate));
            Put($"throws/s at FireRate {rate} (expect {rate})", idx, count / duration);
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator StatusTiming(string path, string label, int idx)
        {
            var status = Resources.Load<StatusEffectDefinition>(path);
            var go = new GameObject("FrameCheckTarget");
            temp.Add(go);
            go.transform.position = new Vector3(-15f, 30f, 15f);
            var target = go.AddComponent<TestDamageable>();
            var receiver = go.AddComponent<StatusReceiver>();
            receiver.Apply(status, 1);
            float t0 = Time.time;
            while (receiver.Active.Count > 0 && Time.time - t0 < 10f) yield return null;
            float duration = Time.time - t0;
            Put($"{label} duration s (expect {status.duration})", idx, duration);
            if (status.damagePerSecond > 0f)
            {
                Put($"{label} total damage (expect {status.damagePerSecond * status.duration})", idx, target.TotalDamage);
                Put($"{label} damage per second (expect {status.damagePerSecond})", idx, target.TotalDamage / status.duration);
            }
            Destroy(go);
        }

        IEnumerator Invincibility(int idx)
        {
            // A separate health object, so the real player's state is not involved.
            var go = new GameObject("FrameCheckHealth");
            temp.Add(go);
            go.transform.position = new Vector3(-15f, 30f, 15f);
            var health = go.AddComponent<PlayerHealth>();
            health.State.AddSoul(40);
            float last = -1f, sum = 0f;
            int hits = 0;
            float t0 = Time.time;
            while (Time.time - t0 < 4.5f)
            {
                if (health.Damage(1))
                {
                    if (last >= 0f) { sum += Time.time - last; hits++; }
                    last = Time.time;
                }
                yield return null;
            }
            Put($"invincibility interval s (expect {health.State.InvincibilitySeconds})", idx, hits > 0 ? sum / hits : float.NaN);
            Destroy(go);
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Homing(int idx)
        {
            var targetGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.Add(targetGo);
            targetGo.transform.position = new Vector3(6f, 14f, 0f);
            var target = targetGo.AddComponent<TestDamageable>();
            var recipe = new ShotRecipe { Damage = 1f, Speed = 18f, Range = 100f };
            recipe.AddEffect(Resources.Load<ShotEffect>("Effects/Homing"));
            yield return null;
            var rock = Projectile.Spawn(recipe, new Vector3(0f, 14f, -8f), Vector3.forward, null);
            float t0 = Time.time;
            float heading = float.NaN;
            while (target.Hits == 0 && Time.time - t0 < 3f)
            {
                if (float.IsNaN(heading) && Time.time - t0 >= 0.25f && rock != null && !rock.IsDespawned)
                    heading = Vector3.Angle(Vector3.forward, rock.Velocity);
                yield return null;
            }
            Put("homing heading change after 0.25 s (deg)", idx, heading);
            Put("homing time to hit s", idx, target.Hits > 0 ? Time.time - t0 : float.NaN);
            Destroy(targetGo);
        }

        IEnumerator Range(int idx)
        {
            var recipe = new ShotRecipe { Damage = 1f, Speed = 18f, Range = 20f };
            Vector3 start = new Vector3(0f, 18f, -8f);
            var rock = Projectile.Spawn(recipe, start, Vector3.right, null);
            rock.lifetime = 6f;
            Vector3 last = start;
            float t0 = Time.time;
            while (rock != null && Time.time - t0 < 6f)
            {
                last = rock.transform.position;
                if (rock.IsDespawned) break;
                yield return null;
            }
            Put("rock range: distance at despawn m (expect 20)", idx, Vector3.Distance(start, last));
            yield return null;
        }

        IEnumerator EnemySpeed(int idx)
        {
            var def = Instantiate(EnemyCatalog.Find("ghoul"));
            def.maxHealth = 1e9f;
            var go = new GameObject("FrameCheckEnemy");
            temp.Add(go);
            go.SetActive(false);
            go.transform.position = new Vector3(15f, 0.05f, -16f);
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            var enemy = go.AddComponent<Enemy>();
            enemy.definition = def;
            go.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            Vector3 a = go.transform.position;
            float t0 = Time.time;
            yield return new WaitForSeconds(2f);
            Vector3 b = go.transform.position;
            a.y = b.y = 0f;
            Put($"enemy chase speed m/s (expect {def.moveSpeed})", idx, Vector3.Distance(a, b) / (Time.time - t0));
            Destroy(go);
        }
    }
}
