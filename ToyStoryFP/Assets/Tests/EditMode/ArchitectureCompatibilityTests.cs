using System;
using System.Reflection;
using NUnit.Framework;

public class ArchitectureCompatibilityTests
{
    [Test]
    public void SceneChangeController_RetainsLegacyCompatibility()
    {
        Type legacySceneType = typeof(SceneChangeController).Assembly.GetType("CambioEscena");
        Type legacyVfxType = typeof(DestroyAfterDelay).Assembly.GetType("EliminarVFXScript");

        Assert.That(legacySceneType, Is.Not.Null);
        Assert.That(legacyVfxType, Is.Not.Null);
        Assert.That(legacySceneType.IsSubclassOf(typeof(SceneChangeController)), Is.True);
        Assert.That(legacyVfxType.IsSubclassOf(typeof(DestroyAfterDelay)), Is.True);
    }

    [Test]
    public void PanelController_And_EnemyController_KeepTheirPublicEntryPoints()
    {
        Assert.That(typeof(PanelController).GetMethod("OpenCreditsFromButton", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("ConfigureRuntimeContext", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("ApplyRoundScaling", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("NotifyDamagedByPlayer", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("NotifyAllyAlert", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(typeof(EnemyController).GetMethod("SetAvoidancePriority", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
    }
}
