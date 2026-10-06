using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Adds an amount of a currency to the wallet. Cannot be taken while that currency is at its max.</summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Pickup Effects/Currency", fileName = "CurrencyEffect")]
    public class CurrencyEffect : PickupEffect
    {
        [Tooltip("Which currency (coins, keys, ...).")]
        public CurrencyDefinition currency;
        [Tooltip("How much one pickup is worth.")]
        [Min(1)]
        public int amount = 1;

        public override bool Collect(PickupContext context)
        {
            var wallet = context.Wallet;
            if (wallet == null || currency == null || !wallet.Has(currency.id) || wallet.IsFull(currency.id)) return false;
            wallet.Add(currency.id, amount);
            return true;
        }

        public override Color PlaceholderTint => new Color(0.85f, 0.68f, 0.2f);
    }
}
