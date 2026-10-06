using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Every live pickup in ONE loop: toss arc, magnet, collection and lifetime (no per-pickup Update, no per-pickup
    /// physics query; the floor is found once when it spawns). Pickups are pooled and fully reset on reuse.
    /// Live cap = GameFeelSettings.maxPickups scaled by Effects quality; at the cap the oldest is recycled.
    /// Spawn is the one way to create a pickup (gameplay, drops and debug tools).
    /// Static caches skip destroyed objects and reset in SubsystemRegistration.
    /// </summary>
    public class PickupManager : MonoBehaviour
    {
        public const int DefaultMaxPickups = 120;
        /// <summary>A pickup with a lifetime blinks for its last BlinkSeconds.</summary>
        public const float BlinkSeconds = 2f;
        /// <summary>Visibility flips per second while blinking.</summary>
        public const float BlinkFlipsPerSecond = 8f;
        /// <summary>After a refused collection (all effects returned false), wait this long before trying again.</summary>
        public const float RetryDelay = 0.5f;
        const float Gravity = 15f;
        const float PlayerCenterHeight = 0.5f;

        static readonly List<Pickup> active = new List<Pickup>();
        static readonly Stack<Pickup> pool = new Stack<Pickup>();
        static readonly RaycastHit[] floorHits = new RaycastHit[8];
        static readonly HashSet<PickupDefinition> modeWarned = new HashSet<PickupDefinition>();
        static readonly PickupContext context = new PickupContext();
        static PickupManager instance;
        static GameObject player;
        static long serialCounter;
        static bool capReported;

        /// <summary>Live pickups (debug readout and tests).</summary>
        public static IReadOnlyList<Pickup> Live => active;
        public static int LiveCount => active.Count;
        public static int RecycledByCap { get; private set; }
        public static int CollectedCount { get; private set; }

        public static int MaxPickups
        {
            get
            {
                var settings = GameFeelSettings.Current;
                int baseCap = settings != null ? settings.maxPickups : DefaultMaxPickups;
                return Mathf.Max(1, Mathf.RoundToInt(baseCap * GameFeel.QualityScale(GameFeel.Quality)));
            }
        }

        /// <summary>
        /// Spawns a pickup at position. toss = a small random cosmetic hop. Returns null for a missing definition
        /// or one whose requiredUnlockId is not unlocked.
        /// </summary>
        public static Pickup Spawn(PickupDefinition definition, Vector3 position, bool toss = true)
        {
            if (definition == null || !definition.IsUnlocked) return null;
            if (Application.isPlaying && instance == null)
            {
                var go = new GameObject("PickupManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<PickupManager>();
            }
            while (active.Count >= MaxPickups && RecycleOldest()) { }

            var p = TakeFromPool();
            if (p == null) p = new GameObject("Pickup").AddComponent<Pickup>();
            p.gameObject.SetActive(true);
            // Cosmetic only (UnityEngine.Random is allowed for looks): which way it hops.
            Vector3 velocity = Vector3.zero;
            if (toss)
            {
                float angle = Random.value * Mathf.PI * 2f;
                float speed = Random.Range(0.8f, 1.8f);
                velocity = new Vector3(Mathf.Cos(angle) * speed, Random.Range(3.5f, 4.5f), Mathf.Sin(angle) * speed);
            }
            p.Begin(definition, position, velocity, FloorBelow(position), ++serialCounter);
            p.activeIndex = active.Count;
            active.Add(p);
            return p;
        }

        /// <summary>The player that collects pickups (null = find the PlayerController in the scene when needed).</summary>
        public static void SetPlayer(GameObject playerObject)
        {
            player = playerObject;
            context.SetPlayer(playerObject);
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>One frame for every live pickup (Update calls this; tests call it directly).</summary>
        public static void Tick(float dt)
        {
            if (active.Count == 0) return;
            if (player == null)
            {
                var controller = FindAnyObjectByType<PlayerController>();
                SetPlayer(controller != null ? controller.gameObject : null);
            }
            bool hasPlayer = player != null;
            Vector3 target = hasPlayer ? player.transform.position + Vector3.up * PlayerCenterHeight : Vector3.zero;

            // Backwards with swap-removal: a pickup removed at i is replaced by one already handled this frame.
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p == null)
                {
                    RemoveAtSwap(i);
                    continue;
                }
                var def = p.Definition;
                if (def == null)
                {
                    Despawn(p);
                    continue;
                }
                p.Age += dt;
                if (def.lifetimeSeconds > 0f)
                {
                    float left = def.lifetimeSeconds - p.Age;
                    if (left <= 0f)
                    {
                        Despawn(p);
                        continue;
                    }
                    p.SetVisible(left > BlinkSeconds || Mathf.FloorToInt(left * BlinkFlipsPerSecond) % 2 == 0);
                }

                Vector3 start = p.transform.position;
                Vector3 pos = start;
                if (!p.Landed)
                {
                    p.velocity.y -= Gravity * dt;
                    pos += p.velocity * dt;
                    if (pos.y <= p.groundY && p.velocity.y <= 0f)
                    {
                        pos.y = p.groundY;
                        p.velocity = Vector3.zero;
                        p.Landed = true;
                    }
                }

                if (hasPlayer && p.Age >= p.retryAt)
                {
                    Vector3 to = target - pos;
                    float sq = to.sqrMagnitude;
                    if (sq <= def.collectRadius * def.collectRadius)
                    {
                        if (TryCollect(p, pos)) continue;
                        p.retryAt = p.Age + RetryDelay;
                    }
                    else if (def.magnetRange > 0f && sq <= def.magnetRange * def.magnetRange)
                    {
                        float distance = Mathf.Sqrt(sq);
                        pos += to * (Mathf.Min(def.magnetSpeed * dt, distance) / distance);
                        // Falls back to the floor if the player walks out of range.
                        p.velocity = Vector3.zero;
                        p.Landed = false;
                    }
                }
                if (pos != start) p.transform.position = pos;
            }
        }

        static bool TryCollect(Pickup p, Vector3 position)
        {
            var def = p.Definition;
            if (def.collectMode != PickupCollectMode.ApplyImmediately && modeWarned.Add(def))
                Debug.LogWarning($"[Pickup] '{def.id}' uses collect mode {def.collectMode}, which is not implemented yet; applying it immediately.", def);
            context.Pickup = def;
            context.Position = position;
            bool taken = false;
            if (def.effects != null)
                foreach (var effect in def.effects)
                    if (effect != null && effect.Collect(context)) taken = true;
            context.Pickup = null;
            if (!taken) return false;

            CollectedCount++;
            if (Application.isPlaying)
            {
                if (def.collectSound != null) AudioSource.PlayClipAtPoint(def.collectSound, position, GameFeel.SoundVolume);
                if (def.collectVfx != null) Destroy(Instantiate(def.collectVfx, position, Quaternion.identity), 2f);
                else GameFeel.Burst(position + Vector3.up * 0.2f, def.EffectiveTint, 10, 2f);
            }
            Despawn(p);
            return true;
        }

        /// <summary>Removes a pickup from the world and returns it to the pool.</summary>
        public static void Despawn(Pickup p)
        {
            if (p == null || p.IsDespawned) return;
            p.IsDespawned = true;
            RemoveFromActive(p);
            p.gameObject.SetActive(false);
            pool.Push(p);
        }

        static void RemoveFromActive(Pickup p)
        {
            int i = p.activeIndex;
            p.activeIndex = -1;
            if (i < 0 || i >= active.Count || active[i] != p) return;
            RemoveAtSwap(i);
        }

        static void RemoveAtSwap(int i)
        {
            int last = active.Count - 1;
            var moved = active[last];
            active[i] = moved;
            if (moved != null) moved.activeIndex = i;
            active.RemoveAt(last);
        }

        /// <summary>Recycles the oldest live pickup. False when there is none.</summary>
        static bool RecycleOldest()
        {
            Pickup oldest = null;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p == null)
                {
                    RemoveAtSwap(i);
                    continue;
                }
                if (oldest == null || p.Serial < oldest.Serial) oldest = p;
            }
            if (oldest == null) return false;
            Despawn(oldest);
            RecycledByCap++;
            if (!capReported)
            {
                capReported = true;
                Debug.Log($"[Pickup] Live pickup cap ({MaxPickups}) reached; recycling the oldest pickups. See GameFeelSettings.maxPickups.");
            }
            return true;
        }

        static Pickup TakeFromPool()
        {
            while (pool.Count > 0)
            {
                var p = pool.Pop();
                if (p != null) return p;
            }
            return null;
        }

        /// <summary>Height of the floor under a point (one query at spawn). Characters are ignored. None = the point's own height.</summary>
        static float FloorBelow(Vector3 position)
        {
            int count = Physics.RaycastNonAlloc(position + Vector3.up * 0.5f, Vector3.down, floorHits, 50f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var c = floorHits[i].collider;
                if (c is CharacterController) continue;
                float y = floorHits[i].point.y;
                if (y <= position.y + 0.5f && y > best) best = y;
            }
            return float.IsNegativeInfinity(best) ? position.y : best;
        }

        /// <summary>Destroys live and pooled pickups and forgets them (tests and scene cleanup).</summary>
        public static void ClearAll()
        {
            foreach (var p in active.ToArray()) if (p != null) Pickup.DestroySafe(p.gameObject);
            while (pool.Count > 0)
            {
                var p = pool.Pop();
                if (p != null) Pickup.DestroySafe(p.gameObject);
            }
            active.Clear();
            RecycledByCap = 0;
            CollectedCount = 0;
            capReported = false;
            SetPlayer(null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            pool.Clear();
            modeWarned.Clear();
            instance = null;
            serialCounter = 0;
            RecycledByCap = 0;
            CollectedCount = 0;
            capReported = false;
            SetPlayer(null);
        }
    }
}
