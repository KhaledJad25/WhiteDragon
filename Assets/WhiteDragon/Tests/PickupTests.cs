using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using Is = UnityEngine.TestTools.Constraints.Is;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Object = UnityEngine.Object;

namespace WhiteDragon
{
    public class PickupTests
    {
        static readonly Vector3 Arena = new Vector3(7000f, 0f, -7000f);
        const float Dt = 1f / 60f;
        readonly List<Object> cleanup = new List<Object>();
        EffectsQuality qualityBefore;

        [SetUp]
        public void SetUp()
        {
            qualityBefore = GameFeel.Quality;
            PickupManager.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            PickupManager.ClearAll();
            GameFeelSettings.Current = null;
            GameFeel.Quality = qualityBefore;
            Unlocks.Clear();
            foreach (var o in cleanup)
                if (o != null) Object.DestroyImmediate(o);
            cleanup.Clear();
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            cleanup.Add(o);
            return o;
        }

        CurrencyDefinition Currency(string id, int max, int start = 0)
        {
            var c = Make<CurrencyDefinition>();
            c.id = id;
            c.displayName = char.ToUpperInvariant(id[0]) + id.Substring(1);
            c.maxAmount = max;
            c.startAmount = start;
            return c;
        }

        PickupDefinition Pickup(string id, params PickupEffect[] effects)
        {
            var p = Make<PickupDefinition>();
            p.id = id;
            p.effects = new List<PickupEffect>(effects);
            return p;
        }

        PickupDefinition Heart(int halves = 2)
        {
            var heal = Make<HealEffect>();
            heal.halves = halves;
            return Pickup("heart", heal);
        }

        PickupDefinition Coin(CurrencyDefinition coins, int amount = 1)
        {
            var e = Make<CurrencyEffect>();
            e.currency = coins;
            e.amount = amount;
            return Pickup("coin", e);
        }

        GameObject Player(Vector3 at, params CurrencyDefinition[] currencies)
        {
            var go = new GameObject("PickupTestPlayer");
            cleanup.Add(go);
            go.transform.position = at;
            go.AddComponent<PlayerHealth>();
            var wallet = go.AddComponent<PlayerWallet>();
            wallet.currencies.AddRange(currencies);
            PickupManager.SetPlayer(go);
            return go;
        }

        static void Tick(float seconds, float dt = Dt)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += dt) PickupManager.Tick(dt);
        }

        // ---------- Wallet ----------

        [Test]
        public void Wallet_ClampsToMax_AndTrySpendNeedsTheFullAmount()
        {
            var coins = Currency("coins", 99);
            var gold = Currency("gold", 0);
            var wallet = Player(Arena, coins, gold).GetComponent<PlayerWallet>();
            var changes = new List<string>();
            wallet.Changed += changes.Add;

            Assert.AreEqual(99, wallet.Add("coins", 150), "only up to the max");
            Assert.AreEqual(99, wallet.Get("coins"));
            Assert.AreEqual(0, wallet.Add("coins", 5), "already full: nothing changes");
            Assert.IsTrue(wallet.IsFull("coins"));
            Assert.AreEqual(1, changes.Count, "Changed fires only when the amount changes");

            Assert.IsFalse(wallet.TrySpend("coins", 100), "cannot spend more than held");
            Assert.AreEqual(99, wallet.Get("coins"), "a failed spend takes nothing");
            Assert.IsTrue(wallet.TrySpend("coins", 99));
            Assert.AreEqual(0, wallet.Get("coins"));
            Assert.AreEqual(0, wallet.Add("coins", -5), "never below zero");

            Assert.AreEqual(100000, wallet.Add("gold", 100000), "max 0 = unlimited");
            Assert.AreEqual(0, wallet.Add("missing", 3), "unknown currency");
            Assert.IsFalse(wallet.TrySpend("missing", 0));
        }

        [Test]
        public void Wallet_ResetsToStartAmounts()
        {
            var keys = Currency("keys", 9, start: 1);
            var wallet = Player(Arena, keys).GetComponent<PlayerWallet>();
            Assert.AreEqual(1, wallet.Get("keys"), "starts at its start amount");
            wallet.Add("keys", 5);
            wallet.ResetToStart();
            Assert.AreEqual(1, wallet.Get("keys"));
        }

        [Test]
        public void WalletHud_LineIsCachedUntilTheWalletChanges()
        {
            var coins = Currency("coins", 99);
            var keys = Currency("keys", 9);
            var go = Player(Arena, coins, keys);
            var wallet = go.GetComponent<PlayerWallet>();
            var hud = go.AddComponent<WalletHUD>();
            hud.wallet = wallet;
            wallet.Add("coins", 12);

            string first = hud.Line;
            Assert.AreEqual("Coins: 12   Keys: 0", first);
            Assert.AreSame(first, hud.Line, "no new string while nothing changed");
            wallet.Add("keys", 1);
            Assert.AreEqual("Coins: 12   Keys: 1", hud.Line);
        }

        // ---------- Collection ----------

        [Test]
        public void CollectReturningFalse_LeavesThePickup_UntilItCanBeTaken()
        {
            var player = Player(Arena);
            var health = player.GetComponent<PlayerHealth>();
            var heart = PickupManager.Spawn(Heart(), Arena, toss: false);
            int red = health.State.Red;

            Tick(1f);
            Assert.AreEqual(1, PickupManager.LiveCount, "full health: the heart stays");
            Assert.IsFalse(heart.IsDespawned);
            Assert.AreEqual(red, health.State.Red);

            health.State.TryDamage(2, 1000f);
            Tick(PickupManager.RetryDelay + 0.1f);
            Assert.AreEqual(0, PickupManager.LiveCount, "hurt: it is taken");
            Assert.AreEqual(red, health.State.Red, "healed back");
            Assert.AreEqual(1, PickupManager.CollectedCount);
        }

        [Test]
        public void Coins_AtMax_StayOnTheFloor()
        {
            var coins = Currency("coins", 2);
            var wallet = Player(Arena, coins).GetComponent<PlayerWallet>();
            var coin = Coin(coins);
            PickupManager.Spawn(coin, Arena, toss: false);
            PickupManager.Spawn(coin, Arena, toss: false);
            PickupManager.Spawn(coin, Arena, toss: false);
            Tick(0.1f);
            Assert.AreEqual(2, wallet.Get("coins"));
            Assert.AreEqual(1, PickupManager.LiveCount, "the third coin waits");
        }

        [Test]
        public void Magnet_PullsAPickupInRange_AndIgnoresOneOutOfRange()
        {
            var coins = Currency("coins", 99);
            var wallet = Player(Arena, coins).GetComponent<PlayerWallet>();
            var coin = Coin(coins);
            PickupManager.Spawn(coin, Arena + new Vector3(2.5f, 0f, 0f), toss: false);
            var far = PickupManager.Spawn(coin, Arena + new Vector3(0f, 0f, 6f), toss: false);
            Tick(1f);
            Assert.AreEqual(1, wallet.Get("coins"), "2.5 m: inside the 3 m magnet, flown in and collected");
            Assert.AreEqual(1, PickupManager.LiveCount);
            Assert.AreEqual(Arena + new Vector3(0f, 0f, 6f), far.transform.position, "6 m: never moves");
        }

        [Test]
        public void UnimplementedCollectMode_WarnsOnce_AndAppliesImmediately()
        {
            var coins = Currency("coins", 99);
            var wallet = Player(Arena, coins).GetComponent<PlayerWallet>();
            var coin = Coin(coins);
            coin.collectMode = (PickupCollectMode)99;
            LogAssert.Expect(LogType.Warning, new Regex("not implemented yet"));
            PickupManager.Spawn(coin, Arena, toss: false);
            PickupManager.Spawn(coin, Arena, toss: false);
            Tick(0.1f);
            Assert.AreEqual(2, wallet.Get("coins"));
        }

        // ---------- Pooling, cap, lifetime ----------

        [Test]
        public void PooledPickup_IsFullyResetOnReuse()
        {
            Player(Arena + new Vector3(0f, 0f, 50f));
            var blinking = Heart();
            blinking.lifetimeSeconds = 3f;
            blinking.tint = Color.green;
            var first = PickupManager.Spawn(blinking, Arena, toss: true);
            Tick(2.55f);
            Assert.IsFalse(first.Visible, "blinking, currently hidden (0.45 s left)");
            PickupManager.Despawn(first);

            var other = Heart();
            other.tint = Color.blue;
            var second = PickupManager.Spawn(other, Arena + Vector3.right, toss: false);
            Assert.AreSame(first, second, "taken from the pool");
            Assert.AreSame(other, second.Definition);
            Assert.AreEqual(0f, second.Age);
            Assert.IsTrue(second.Visible);
            foreach (var r in second.GetComponentsInChildren<Renderer>(true)) Assert.IsTrue(r.enabled, "visible again");
            Assert.IsFalse(second.IsDespawned);
            Assert.IsTrue(second.Landed, "no toss: resting where spawned");
            Assert.AreEqual(Arena + Vector3.right, second.transform.position);
            TestColors.AssertApprox(Color.blue, second.Visual.GetComponent<Renderer>().sharedMaterial.color);
            Tick(5f);
            Assert.AreEqual(1, PickupManager.LiveCount, "no lifetime left over from the old pickup");
        }

        [Test]
        public void Cap_RecyclesTheOldest_AndScalesWithEffectsQuality()
        {
            var settings = Make<GameFeelSettings>();
            settings.maxPickups = 5;
            GameFeelSettings.Current = settings;
            GameFeel.Quality = EffectsQuality.High;
            Player(Arena + new Vector3(0f, 0f, 50f));
            var heart = Heart();
            var serials = new List<long>();
            for (int i = 0; i < 6; i++) serials.Add(PickupManager.Spawn(heart, Arena + Vector3.right * i, toss: false).Serial);

            Assert.AreEqual(5, PickupManager.LiveCount);
            Assert.AreEqual(1, PickupManager.RecycledByCap);
            var live = new List<long>();
            foreach (var p in PickupManager.Live) live.Add(p.Serial);
            CollectionAssert.AreEquivalent(serials.GetRange(1, 5), live, "the oldest went; the five newest remain");

            settings.maxPickups = PickupManager.DefaultMaxPickups;
            Assert.AreEqual(120, PickupManager.MaxPickups);
            GameFeel.Quality = EffectsQuality.Medium;
            Assert.AreEqual(60, PickupManager.MaxPickups);
            GameFeel.Quality = EffectsQuality.Low;
            Assert.AreEqual(30, PickupManager.MaxPickups);
        }

        [Test]
        public void Lifetime_BlinksAtTheEnd_ThenDespawns_AtAnyFrameRate([Values(30f, 60f)] float fps)
        {
            Player(Arena + new Vector3(0f, 0f, 50f));
            var def = Heart();
            def.lifetimeSeconds = 5f;
            var p = PickupManager.Spawn(def, Arena, toss: false);
            float dt = 1f / fps;
            bool hiddenEarly = false, hiddenLate = false;
            float t = 0f;
            while (!p.IsDespawned && t < 10f)
            {
                PickupManager.Tick(dt);
                t += dt;
                if (p.IsDespawned) break;
                if (!p.Visible && t < 5f - PickupManager.BlinkSeconds - 1e-3f) hiddenEarly = true;
                if (!p.Visible) hiddenLate = true;
            }
            Assert.IsFalse(hiddenEarly, "solid until the last 2 seconds");
            Assert.IsTrue(hiddenLate, "blinks in the last 2 seconds");
            Assert.AreEqual(5f, t, dt + 1e-3f, "gone after 5 s");
        }

        [Test]
        public void CentralLoop_DoesNotAllocatePerFrame()
        {
            var coins = Currency("coins", 99);
            Player(Arena, coins);
            var heart = Heart();
            var blinking = Heart();
            blinking.lifetimeSeconds = 1000f;
            for (int i = 0; i < 40; i++)
            {
                // Hearts at full health: magneted in, refused, retried; some fall from a toss.
                PickupManager.Spawn(heart, Arena + new Vector3(i % 5, 0f, i / 5 * 0.3f), toss: i % 2 == 0);
                PickupManager.Spawn(blinking, Arena + new Vector3(20f + i, 0f, 0f), toss: false);
            }
            Tick(0.5f);

            // Counts GC.Alloc on this thread only (the editor's other threads allocate all the time, so heap size is noisy).
            Assert.That(() =>
            {
                for (int i = 0; i < 1000; i++) PickupManager.Tick(Dt);
            }, Is.Not.AllocatingGCMemory(), "80 pickups for 1000 frames");
            Assert.AreEqual(80, PickupManager.LiveCount, "all still there (refused hearts and far pickups)");
        }

        [Test]
        public void UnlockGatedPickup_DoesNotSpawnUntilUnlocked()
        {
            var def = Heart();
            def.requiredUnlockId = "zz_test_pickup_unlock";
            Assert.IsNull(PickupManager.Spawn(def, Arena, toss: false), "locked");
            Assert.AreEqual(0, PickupManager.LiveCount);
            Unlocks.Grant("zz_test_pickup_unlock");
            Assert.IsNotNull(PickupManager.Spawn(def, Arena, toss: false), "unlocked");
        }
    }
}
