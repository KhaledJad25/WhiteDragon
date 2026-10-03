using System;
using System.Collections.Generic;

namespace WhiteDragon
{
    /// <summary>
    /// Seeded randomness with one independent SplitMix64 generator per RandomStream.
    /// Drawing from one stream never changes another; the same seed always gives the same sequences.
    /// </summary>
    public class RunRandom
    {
        readonly int seed;
        ulong[] states = new ulong[0];

        public int Seed => seed;

        public RunRandom(int seed)
        {
            this.seed = seed;
        }

        /// <summary>Float in [0, 1).</summary>
        public float Value(RandomStream stream) => (Next(stream) >> 40) * (1f / 16777216f);

        /// <summary>Int in [min, maxExclusive). Returns min if the range is empty.</summary>
        public int Range(RandomStream stream, int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            ulong span = (ulong)((long)maxExclusive - min);
            return (int)(min + (long)(Next(stream) % span));
        }

        /// <summary>Float in [min, max).</summary>
        public float Range(RandomStream stream, float min, float max) => min + (max - min) * Value(stream);

        public bool Chance(RandomStream stream, float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return Value(stream) < probability;
        }

        /// <summary>Index chosen in proportion to weights; -1 if no weight is positive.</summary>
        public int PickWeighted(RandomStream stream, IReadOnlyList<float> weights)
        {
            if (weights == null) return -1;
            double total = 0;
            int last = -1;
            for (int i = 0; i < weights.Count; i++)
            {
                if (!(weights[i] > 0f)) continue;
                total += weights[i];
                last = i;
            }
            if (last < 0) return -1;

            double r = NextDouble(stream) * total;
            for (int i = 0; i < weights.Count; i++)
            {
                if (!(weights[i] > 0f)) continue;
                r -= weights[i];
                if (r < 0) return i;
            }
            return last;
        }

        public void Shuffle<T>(RandomStream stream, IList<T> list)
        {
            if (list == null) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(stream, 0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// A new, independent generator for one thing in the run (for example an enemy), from this run's seed and a
        /// stable key. Same seed and key = same sequences; drawing from it never touches this generator.
        /// </summary>
        public RunRandom Derive(string key)
        {
            ulong hash = 14695981039346656037UL; // FNV-1a: stable across runs and platforms (string.GetHashCode is not)
            if (key != null)
                foreach (char c in key)
                {
                    hash ^= c;
                    hash *= 1099511628211UL;
                }
            return new RunRandom((int)Mix((ulong)(uint)seed ^ Mix(hash)));
        }

        double NextDouble(RandomStream stream) => (Next(stream) >> 11) * (1.0 / 9007199254740992.0);

        ulong Next(RandomStream stream)
        {
            int index = (int)stream;
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(stream));
            if (index >= states.Length)
            {
                int old = states.Length;
                Array.Resize(ref states, index + 1);
                for (int i = old; i < states.Length; i++)
                    states[i] = Mix((ulong)(uint)seed * 0x9E3779B97F4A7C15UL + (ulong)(i + 1) * 0xD1B54A32D192ED03UL);
            }
            states[index] += 0x9E3779B97F4A7C15UL;
            return Mix(states[index]);
        }

        internal static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
