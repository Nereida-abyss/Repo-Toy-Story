using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ProjectDataValidationTests
{
    private const string WeaponCatalogAssetPath = "Assets/Project/Data/Player/DefaultWeaponCatalog.asset";

    [Test]
    public void DefaultWeaponCatalog_HasUniqueIds_AndValidStartingWeapon()
    {
        WeaponCatalog catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(WeaponCatalogAssetPath);
        Assert.That(catalog, Is.Not.Null, "DefaultWeaponCatalog.asset debe existir.");
        Assert.That(catalog.Entries, Is.Not.Null);
        Assert.That(catalog.Entries.Count, Is.GreaterThan(0));

        HashSet<string> uniqueIds = new HashSet<string>();
        bool foundStartingWeapon = false;

        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            WeaponCatalog.WeaponCatalogEntry entry = catalog.Entries[i];
            Assert.That(entry, Is.Not.Null, $"La entrada {i} del WeaponCatalog no deberia ser null.");
            Assert.That(string.IsNullOrWhiteSpace(entry.WeaponId), Is.False, $"La entrada {i} del WeaponCatalog necesita WeaponId.");
            Assert.That(uniqueIds.Add(entry.WeaponId), Is.True, $"WeaponId duplicado en WeaponCatalog: {entry.WeaponId}");
            Assert.That(entry.Weapon, Is.Not.Null, $"La entrada '{entry.WeaponId}' necesita WeaponScript.");
            Assert.That(entry.Price, Is.GreaterThanOrEqualTo(0), $"La entrada '{entry.WeaponId}' no puede tener precio negativo.");

            if (entry.WeaponId == catalog.DefaultStartingWeaponId)
            {
                foundStartingWeapon = true;
            }
        }

        Assert.That(foundStartingWeapon, Is.True, "DefaultStartingWeaponId debe existir en Entries.");
    }

    [Test]
    public void WeaponStatsProfiles_HavePositiveGameplayValues()
    {
        string[] guids = AssetDatabase.FindAssets("t:WeaponStatsProfile", new[] { "Assets/Project/Data/Weapons" });
        Assert.That(guids.Length, Is.GreaterThan(0), "Se esperaba al menos un WeaponStatsProfile en Data/Weapons.");

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            WeaponStatsProfile profile = AssetDatabase.LoadAssetAtPath<WeaponStatsProfile>(assetPath);
            Assert.That(profile, Is.Not.Null, $"No se pudo cargar WeaponStatsProfile en {assetPath}.");
            Assert.That(profile.MaxRange, Is.GreaterThan(0f), $"{assetPath} necesita MaxRange positivo.");
            Assert.That(profile.FireRate, Is.GreaterThan(0f), $"{assetPath} necesita FireRate positivo.");
            Assert.That(profile.MagazineSize, Is.GreaterThan(0), $"{assetPath} necesita MagazineSize positivo.");
            Assert.That(profile.DamagePerShot, Is.GreaterThan(0), $"{assetPath} necesita DamagePerShot positivo.");
            Assert.That(profile.ReloadDuration, Is.GreaterThan(0f), $"{assetPath} necesita ReloadDuration positiva.");
        }
    }
}
