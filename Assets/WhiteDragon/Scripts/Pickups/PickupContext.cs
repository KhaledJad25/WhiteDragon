using UnityEngine;

namespace WhiteDragon
{
    /// <summary>What a PickupEffect may touch when the player collects a pickup. Any field can be null.</summary>
    public class PickupContext
    {
        public GameObject Player;
        public PlayerHealth Health;
        public PlayerWallet Wallet;
        public PlayerStats Stats;
        public PickupDefinition Pickup;
        public Vector3 Position;

        /// <summary>Fills the player fields from a player object (null clears them).</summary>
        public void SetPlayer(GameObject player)
        {
            Player = player;
            Health = player != null ? player.GetComponent<PlayerHealth>() : null;
            Wallet = player != null ? player.GetComponent<PlayerWallet>() : null;
            Stats = player != null ? player.GetComponent<PlayerStats>() : null;
        }
    }
}
