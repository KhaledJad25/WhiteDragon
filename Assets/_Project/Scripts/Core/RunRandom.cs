using System;
using System.Collections.Generic;

public class RunRandom
{
    public int Seed { get; }
    readonly Dictionary<RandomStream, System.Random> streams = new();

    public RunRandom(int seed)
    {
        Seed = seed;
        foreach (RandomStream stream in Enum.GetValues(typeof(RandomStream)))
        {
            int streamSeed = unchecked(seed * 397 ^ ((int)stream * 7919 + 17));
            streams[stream] = new System.Random(streamSeed);
        }
    }

    public System.Random GetStream(RandomStream stream)
    {
        if (!streams.TryGetValue(stream, out var rng))
        {
            int streamSeed = unchecked(Seed * 397 ^ ((int)stream * 7919 + 17));
            rng = new System.Random(streamSeed);
            streams[stream] = rng;
        }
        return rng;
    }

    public float Value(RandomStream stream)
    {
        return (float)GetStream(stream).NextDouble();
    }

    public int Range(RandomStream stream, int minInclusive, int maxExclusive)
    {
        return GetStream(stream).Next(minInclusive, maxExclusive);
    }

    public float Range(RandomStream stream, float minInclusive, float maxInclusive)
    {
        return minInclusive + (maxInclusive - minInclusive) * Value(stream);
    }

    public int PickWeighted(RandomStream stream, IReadOnlyList<float> weights)
    {
        if (weights == null || weights.Count == 0)
        {
            return -1;
        }

        float total = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i] > 0f)
            {
                total += weights[i];
            }
        }

        if (total <= 0f)
        {
            return -1;
        }

        float roll = Value(stream) * total;
        float accum = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i] <= 0f)
            {
                continue;
            }

            accum += weights[i];
            if (roll <= accum)
            {
                return i;
            }
        }

        return weights.Count - 1;
    }

    public T PickWeighted<T>(RandomStream stream, IReadOnlyList<T> items, Func<T, float> weightSelector)
    {
        if (items == null || items.Count == 0)
        {
            return default;
        }

        float total = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            float w = weightSelector(items[i]);
            if (w > 0f)
            {
                total += w;
            }
        }

        if (total <= 0f)
        {
            return default;
        }

        float roll = Value(stream) * total;
        float accum = 0f;
        for (int i = 0; i < items.Count; i++)
        {
            float w = weightSelector(items[i]);
            if (w <= 0f)
            {
                continue;
            }

            accum += w;
            if (roll <= accum)
            {
                return items[i];
            }
        }

        return items[items.Count - 1];
    }
}
