using System;
using System.Reflection;
using NUnit.Framework;

public class RunSessionTests
{
    [Test]
    public void StartRunZero_TwiceInARow_GivesTwoDifferentSeeds()
    {
        RunSession.StartRun(0);
        int seed1 = RunSession.Seed;

        RunSession.StartRun(0);
        int seed2 = RunSession.Seed;

        Assert.AreNotEqual(seed1, seed2, "Two consecutive StartRun(0) calls in the same frame must generate distinct seeds.");
    }

    [Test]
    public void StartRunWithSameN_ReplaysSameItemsSourceSequence()
    {
        const int fixedSeed = 88421;
        RunSession.StartRun(fixedSeed);
        float[] seq1 = new float[20];
        for (int i = 0; i < seq1.Length; i++)
        {
            seq1[i] = RunSession.Rng.Value(RandomStream.Items);
        }

        RunSession.StartRun(fixedSeed);
        float[] seq2 = new float[20];
        for (int i = 0; i < seq2.Length; i++)
        {
            seq2[i] = RunSession.Rng.Value(RandomStream.Items);
        }

        CollectionAssert.AreEqual(seq1, seq2, "Same seed must produce the identical Items stream sequence.");
    }

    [Test]
    public void CallingStartRun_ResetsPickedItemIds()
    {
        RunSession.StartRun(100);
        RunSession.PickItem("swift_fingers");
        RunSession.PickItem("iron_grip");
        Assert.AreEqual(2, RunSession.PickedItemIds.Count);

        RunSession.StartRun(101);
        Assert.AreEqual(0, RunSession.PickedItemIds.Count, "StartRun must clear PickedItemIds.");
    }

    [Test]
    public void ResetStatics_ClearsEventSubscriptions()
    {
        int runStartedInvocations = 0;
        Action<int> handler = _ => runStartedInvocations++;

        RunSession.RunStarted += handler;

        // Invoke private static ResetStatics() via reflection
        MethodInfo resetMethod = typeof(RunSession).GetMethod(
            "ResetStatics",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(resetMethod, "ResetStatics method must exist on RunSession.");
        resetMethod.Invoke(null, null);

        // Reset invocation counter from the StartRun(0) that ResetStatics internally executes
        runStartedInvocations = 0;

        // A new StartRun should not trigger the cleared subscriber
        RunSession.StartRun(500);
        Assert.AreEqual(0, runStartedInvocations, "Subscribers before ResetStatics must have been detached.");
    }
}
