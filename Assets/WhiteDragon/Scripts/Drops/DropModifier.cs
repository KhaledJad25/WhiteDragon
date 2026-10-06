using System;
using UnityEngine;

namespace WhiteDragon
{
    /// <summary>
    /// Multiplies the drop weight of every pickup with this tag ("nothing" = the table's nothing weight).
    /// Items, synergies and characters list these. A class rather than a struct so a new entry starts at 1.
    /// </summary>
    [Serializable]
    public class DropModifier
    {
        /// <summary>The special tag that targets a table's nothing weight.</summary>
        public const string NothingTag = "nothing";

        [Tooltip("Lowercase pickup tag, e.g. \"coin\" or \"heart\". \"nothing\" changes the chance of no drop.")]
        public string tag = "";
        [Tooltip("1 = no change, 2 = twice as likely, 0.5 = half as likely. Stacks by multiplying; the total per tag is kept within 0.1 to 10.")]
        [Min(0f)]
        public float weightMultiplier = 1f;

        public DropModifier() { }

        public DropModifier(string tag, float weightMultiplier)
        {
            this.tag = tag;
            this.weightMultiplier = weightMultiplier;
        }
    }
}
