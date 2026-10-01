using UnityEngine;

namespace WhiteDragon
{
    /// <summary>Holds the player StatBlock so other components can read and modify it.</summary>
    public class PlayerStats : MonoBehaviour
    {
        StatBlock stats;

        public StatBlock Stats => stats ??= new StatBlock();
    }
}
