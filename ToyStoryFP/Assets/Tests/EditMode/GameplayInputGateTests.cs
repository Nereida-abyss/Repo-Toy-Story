using NUnit.Framework;
using UnityEngine;

public class GameplayInputGateTests
{
    [SetUp]
    public void SetUp()
    {
        GameplayInputGate.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        GameplayInputGate.Reset();
    }

    [Test]
    public void Acquire_And_Release_AreAccumulatedPerOwner()
    {
        GameplayInputGate.Acquire("Pause");
        Assert.That(GameplayInputGate.IsBlocked, Is.True);

        GameplayInputGate.Acquire("Settings");
        Assert.That(GameplayInputGate.IsBlocked, Is.True);

        GameplayInputGate.Release("Pause");
        Assert.That(GameplayInputGate.IsBlocked, Is.True);

        GameplayInputGate.Release("Settings");
        Assert.That(GameplayInputGate.IsBlocked, Is.False);
    }

    [Test]
    public void Acquire_SameOwnerTwice_DoesNotRequireDoubleRelease()
    {
        GameplayInputGate.Acquire("Pause");
        GameplayInputGate.Acquire("Pause");
        Assert.That(GameplayInputGate.IsBlocked, Is.True);

        GameplayInputGate.Release("Pause");
        Assert.That(GameplayInputGate.IsBlocked, Is.False);
    }
}
