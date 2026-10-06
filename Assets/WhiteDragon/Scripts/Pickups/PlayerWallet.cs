using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// The player's currencies by id, clamped to each currency's max. Resets to the start amounts when a run starts.
    /// Empty currencies list = every CurrencyDefinition in Resources/Currencies (sorted by id).
    /// </summary>
    public class PlayerWallet : MonoBehaviour
    {
        [Tooltip("Currencies this wallet holds. Empty = every currency asset in Data/Resources/Currencies.")]
        public List<CurrencyDefinition> currencies = new List<CurrencyDefinition>();

        int[] amounts;
        bool initialized;

        /// <summary>A currency amount changed (the currency id).</summary>
        public event Action<string> Changed;

        public IReadOnlyList<CurrencyDefinition> Currencies { get { Initialize(); return currencies; } }

        void Awake() => Initialize();
        void OnEnable() => RunSession.RunStarted += ResetToStart;
        void OnDisable() => RunSession.RunStarted -= ResetToStart;

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (currencies.Count == 0)
            {
                currencies.AddRange(Resources.LoadAll<CurrencyDefinition>("Currencies"));
                currencies.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            }
            currencies.RemoveAll(c => c == null);
            amounts = new int[currencies.Count];
            for (int i = 0; i < amounts.Length; i++) amounts[i] = currencies[i].Clamp(currencies[i].startAmount);
        }

        /// <summary>Every currency back to its start amount.</summary>
        public void ResetToStart()
        {
            Initialize();
            for (int i = 0; i < amounts.Length; i++)
            {
                int start = currencies[i].Clamp(currencies[i].startAmount);
                if (amounts[i] == start) continue;
                amounts[i] = start;
                Changed?.Invoke(currencies[i].id);
            }
        }

        public bool Has(string currencyId) => IndexOf(currencyId) >= 0;

        public int Get(string currencyId)
        {
            int i = IndexOf(currencyId);
            return i >= 0 ? amounts[i] : 0;
        }

        /// <summary>The definition for an id, or null.</summary>
        public CurrencyDefinition Find(string currencyId)
        {
            int i = IndexOf(currencyId);
            return i >= 0 ? currencies[i] : null;
        }

        /// <summary>True when one more would be clamped away (at max).</summary>
        public bool IsFull(string currencyId)
        {
            int i = IndexOf(currencyId);
            return i >= 0 && currencies[i].maxAmount > 0 && amounts[i] >= currencies[i].maxAmount;
        }

        /// <summary>Adds (or removes, if negative) and clamps. Returns how much actually changed.</summary>
        public int Add(string currencyId, int amount)
        {
            int i = IndexOf(currencyId);
            if (i < 0 || amount == 0) return 0;
            int before = amounts[i];
            amounts[i] = currencies[i].Clamp(before + amount);
            int changed = amounts[i] - before;
            if (changed != 0) Changed?.Invoke(currencies[i].id);
            return changed;
        }

        /// <summary>Spends the amount only if the wallet holds all of it.</summary>
        public bool TrySpend(string currencyId, int amount)
        {
            int i = IndexOf(currencyId);
            if (i < 0 || amount < 0 || amounts[i] < amount) return false;
            if (amount == 0) return true;
            amounts[i] -= amount;
            Changed?.Invoke(currencies[i].id);
            return true;
        }

        int IndexOf(string currencyId)
        {
            Initialize();
            if (string.IsNullOrEmpty(currencyId)) return -1;
            for (int i = 0; i < currencies.Count; i++)
                if (string.Equals(currencies[i].id, currencyId, StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
