using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Optional rock models per damage type. Lives at Data/Resources/ProjectileVisuals.asset.
    /// Empty (or missing asset) = the placeholder sphere tinted by damage type, exactly as before.
    /// </summary>
    [CreateAssetMenu(menuName = "WhiteDragon/Projectile Visuals", fileName = "ProjectileVisuals")]
    public class ProjectileVisuals : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Damage type this model is used for.")]
            public DamageType damageType;
            [Tooltip("Rock model. Author it at the base rock size (about 0.24 m across); it is scaled by the shot's size. Colliders on it are removed.")]
            public GameObject prefab;
        }

        [Tooltip("Model for any damage type without its own entry. Empty = placeholder sphere.")]
        public GameObject defaultPrefab;
        [Tooltip("Optional model per damage type.")]
        public List<Entry> entries = new List<Entry>();

        static ProjectileVisuals current;
        static bool loaded;

        /// <summary>The asset in Resources (loaded once). Tests may assign one; null reloads.</summary>
        public static ProjectileVisuals Current
        {
            get
            {
                if (!loaded)
                {
                    current = Resources.Load<ProjectileVisuals>("ProjectileVisuals");
                    loaded = true;
                }
                return current;
            }
            set
            {
                current = value;
                loaded = value != null;
            }
        }

        /// <summary>The model for this damage type, or null for the placeholder sphere.</summary>
        public static GameObject Find(DamageType type) => Current != null ? Current.Get(type) : null;

        public GameObject Get(DamageType type)
        {
            foreach (var e in entries)
                if (e.damageType == type && e.prefab != null) return e.prefab;
            return defaultPrefab;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            current = null;
            loaded = false;
        }
    }
}
