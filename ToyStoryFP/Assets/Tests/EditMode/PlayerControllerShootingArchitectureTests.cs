using System.IO;
using NUnit.Framework;

public class PlayerControllerShootingArchitectureTests
{
    [Test]
    public void PlayerController_QueuesPrimaryFire_AndSkipsItWhenWeaponStateChanged()
    {
        string source = ReadSource("Assets/Project/Scripts/Features/Player/PlayerController.cs");
        Assert.That(source, Does.Contain("private bool queuedPrimaryFire;"));
        Assert.That(source, Does.Contain("private bool shouldSkipQueuedPrimaryFire;"));
        Assert.That(source, Does.Contain("ResetQueuedWeaponActions();"));
        Assert.That(source, Does.Contain("shouldSkipQueuedPrimaryFire = weaponLoadout.TryCycleWeapon(1);"));
        Assert.That(source, Does.Contain("shouldSkipQueuedPrimaryFire = weaponLoadout.TryCycleWeapon(-1);"));
        Assert.That(source, Does.Contain("shouldSkipQueuedPrimaryFire |= reloadStarted;"));
        Assert.That(source, Does.Contain("!shouldSkipQueuedPrimaryFire &&"));
        Assert.That(source, Does.Contain("!weaponLoadout.IsSwitchingWeapon;"));
    }

    private static string ReadSource(string relativePath)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        Assert.That(File.Exists(fullPath), Is.True, $"El archivo '{relativePath}' debe existir.");
        return File.ReadAllText(fullPath);
    }
}
