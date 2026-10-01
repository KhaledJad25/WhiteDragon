using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugPanel : MonoBehaviour
{
    PlayerStats stats;
    PlayerInventory inventory;
    PlayerHealth health;
    ItemDefinition[] catalog;
    bool open;
    Vector2 scroll;
    int hits;
    int kills;
    string lastShot = "";
    string seedInput = "";

    readonly List<ItemPedestal> cachedPedestals = new List<ItemPedestal>();
    readonly List<string> cachedPedestalLines = new List<string>();
    readonly List<string> cachedPoolCountLines = new List<string>();
    string cachedSeedString = "";
    GUIStyle seedStyle;

    void OnEnable()
    {
        GameEvents.ShotBuilt += OnShot;
        GameEvents.EnemyHit += OnHit;
        GameEvents.EnemyKilled += OnKill;
        RunSession.RunStarted += OnRunStarted;
        RunSession.ItemPicked += OnItemPicked;
    }

    void OnDisable()
    {
        GameEvents.ShotBuilt -= OnShot;
        GameEvents.EnemyHit -= OnHit;
        GameEvents.EnemyKilled -= OnKill;
        RunSession.RunStarted -= OnRunStarted;
        RunSession.ItemPicked -= OnItemPicked;
    }

    void Start()
    {
        stats = FindAnyObjectByType<PlayerStats>();
        inventory = FindAnyObjectByType<PlayerInventory>();
        health = FindAnyObjectByType<PlayerHealth>();
        catalog = Resources.LoadAll<ItemDefinition>("Items");
        System.Array.Sort(catalog, (a, b) => string.Compare(a.displayName, b.displayName));

        cachedSeedString = "Current Seed: " + RunSession.Seed;
        RebuildPoolCounts();
        RebuildPedestals();
    }

    void OnRunStarted(int seed)
    {
        cachedSeedString = "Current Seed: " + seed;
        RebuildPoolCounts();
        RebuildPedestals();
    }

    void OnItemPicked(string itemId)
    {
        RebuildPedestals();
    }

    void RebuildPoolCounts()
    {
        cachedPoolCountLines.Clear();
        var allItems = ItemCatalog.AllItems;
        foreach (ItemPoolType poolType in System.Enum.GetValues(typeof(ItemPoolType)))
        {
            int countInPool = 0;
            if (allItems != null)
            {
                for (int i = 0; i < allItems.Count; i++)
                {
                    var item = allItems[i];
                    if (item != null && item.pools != null)
                    {
                        for (int p = 0; p < item.pools.Length; p++)
                        {
                            if (item.pools[p] == poolType)
                            {
                                countInPool++;
                                break;
                            }
                        }
                    }
                }
            }
            cachedPoolCountLines.Add(poolType + " Pool: " + countInPool + " items");
        }
    }

    void RebuildPedestals()
    {
        cachedPedestals.Clear();
        cachedPedestalLines.Clear();

        var activePedestals = FindObjectsByType<ItemPedestal>(FindObjectsInactive.Exclude);
        if (activePedestals != null && activePedestals.Length > 0)
        {
            System.Array.Sort(activePedestals, (a, b) => string.Compare(ItemPedestal.GetHierarchyPathKey(a.transform), ItemPedestal.GetHierarchyPathKey(b.transform), System.StringComparison.Ordinal));
            for (int i = 0; i < activePedestals.Length; i++)
            {
                var ped = activePedestals[i];
                if (ped == null) continue;

                cachedPedestals.Add(ped);
                string pathKey = ItemPedestal.GetHierarchyPathKey(ped.transform);
                string groupStr = string.IsNullOrEmpty(ped.GroupId) ? "standalone" : "group '" + ped.GroupId + "'";
                string itemStr = ped.IsEmpty ? "empty" : (ped.CurrentItem != null ? ped.CurrentItem.displayName : "empty");
                cachedPedestalLines.Add("[" + ped.Pool + "] " + pathKey + " (" + groupStr + ") -> " + itemStr);
            }
        }
    }

    void OnShot(ShotRecipe r)
    {
        lastShot = "dmg " + r.Damage.ToString("0.0") + "  speed " + r.Speed.ToString("0.0")
            + "  range " + r.Range.ToString("0") + "  shots x" + r.Count
            + "  pierce " + r.Pierce + "  size x" + r.SizeScale.ToString("0.00")
            + "  homing " + r.Homing.ToString("0") + "  burn " + r.BurnDps.ToString("0.0") + "/s for " + r.BurnDuration.ToString("0.0") + "s  "
            + r.DamageType;
    }

    void OnHit(Vector3 p, float d, ShotRecipe r)
    {
        hits++;
    }

    void OnKill(Vector3 p)
    {
        kills++;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            open = !open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
            if (open)
            {
                cachedSeedString = "Current Seed: " + RunSession.Seed;
                RebuildPoolCounts();
                RebuildPedestals();
            }
        }
    }

    void OnGUI()
    {
        if (!open)
        {
            GUI.Label(new Rect(10, 10, 300, 22), "F1: debug panel");
            return;
        }

        if (stats == null || inventory == null)
        {
            GUI.Label(new Rect(10, 10, 500, 22), "Debug panel: PlayerStats or PlayerInventory not found on the Player.");
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 480, Screen.height - 20), GUI.skin.box);
        GUILayout.Label("DEBUG (F1 to close)");

        GUILayout.Label("--- Run & Seed ---");
        if (seedStyle == null)
        {
            seedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }
        GUILayout.Label(cachedSeedString, seedStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("New run seed"))
        {
            RunSession.StartRun(0);
        }
        seedInput = GUILayout.TextField(seedInput, GUILayout.Width(110));
        if (GUILayout.Button("Start run with seed"))
        {
            if (int.TryParse(seedInput.Trim(), out int parsedSeed))
            {
                RunSession.StartRun(parsedSeed);
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("--- Item Pools ---");
        for (int i = 0; i < cachedPoolCountLines.Count; i++)
        {
            GUILayout.Label(cachedPoolCountLines[i]);
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("--- Pedestals in Scene ---");
        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            RebuildPedestals();
        }
        GUILayout.EndHorizontal();

        // Check if any cached pedestal was destroyed
        bool hasDestroyed = false;
        for (int i = 0; i < cachedPedestals.Count; i++)
        {
            if (cachedPedestals[i] == null)
            {
                hasDestroyed = true;
                break;
            }
        }
        if (hasDestroyed)
        {
            RebuildPedestals();
        }

        if (cachedPedestalLines.Count == 0)
        {
            GUILayout.Label("(none)");
        }
        else
        {
            for (int i = 0; i < cachedPedestalLines.Count; i++)
            {
                GUILayout.Label(cachedPedestalLines[i]);
            }
        }

        if (health != null && health.Health != null)
        {
            GUILayout.Label("--- Health ---");
            int soulCount = 0;
            int darkCount = 0;
            for (int i = 0; i < health.Health.Overlay.Count; i++)
            {
                if (health.Health.Overlay[i] == HeartType.Soul) soulCount++;
                else if (health.Health.Overlay[i] == HeartType.Dark) darkCount++;
            }

            string status = health.IsDead ? "[DEAD]" : (health.IsInvulnerable ? "[INVULNERABLE]" : "[ALIVE]");
            GUILayout.Label($"Red: {health.Health.RedCurrent}/{health.Health.RedContainers} half-hearts ({(health.Health.RedCurrent / 2f):0.#}/{(health.Health.RedContainers / 2f):0.#} full)  |  Soul: {soulCount}  Dark: {darkCount}  {status}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dmg 1/2")) health.TakeDamage(1, health.transform.position);
            if (GUILayout.Button("Dmg 1 Full")) health.TakeDamage(2, health.transform.position);
            if (GUILayout.Button("Heal 1/2")) health.Heal(1);
            if (GUILayout.Button("Heal 1 Full")) health.Heal(2);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 Container")) health.AddContainers(2);
            if (GUILayout.Button("+1 Soul")) health.AddOverlay(HeartType.Soul, 1);
            if (GUILayout.Button("+1 Dark")) health.AddOverlay(HeartType.Dark, 1);
            if (GUILayout.Button(health.IsDead ? "Revive" : "Kill"))
            {
                if (health.IsDead) health.Revive();
                else health.TakeDamage(999, health.transform.position);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Label("--- Stats ---");
        foreach (StatType t in System.Enum.GetValues(typeof(StatType)))
        {
            GUILayout.Label(t + ": " + stats.Stats.Get(t).ToString("0.00"));
        }

        GUILayout.Label("--- Last shot recipe ---");
        GUILayout.Label(lastShot == "" ? "(throw something)" : lastShot);
        GUILayout.Label("Hits: " + hits + "   Kills: " + kills);

        GUILayout.Label("--- Held items (" + inventory.Count + ") ---");
        string held = "";
        for (int i = 0; i < inventory.Count; i++)
        {
            held += inventory.GetItem(i).displayName + ", ";
        }
        GUILayout.Label(held == "" ? "(none)" : held);

        GUILayout.Label("--- Active synergies ---");
        string syn = inventory.ActiveSynergyNames();
        GUILayout.Label(syn == "" ? "(none)" : syn);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Remove last"))
        {
            inventory.RemoveLast();
        }
        if (GUILayout.Button("Clear all"))
        {
            inventory.Clear();
        }
        GUILayout.EndHorizontal();

        GUILayout.Label("--- Add item ---");
        scroll = GUILayout.BeginScrollView(scroll);
        for (int i = 0; i < catalog.Length; i++)
        {
            ItemDefinition item = catalog[i];
            if (GUILayout.Button(item.displayName + "  [" + string.Join(",", item.tags) + "]"))
            {
                inventory.Add(item);
            }
            GUILayout.Label("   " + item.description);
        }
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }
}