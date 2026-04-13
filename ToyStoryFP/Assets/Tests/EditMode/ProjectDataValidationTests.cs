using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ProjectDataValidationTests
{
    private const string WeaponCatalogAssetPath = "Assets/Project/Data/Player/DefaultWeaponCatalog.asset";
    private const string ProjectAudioCatalogAssetPath = "Assets/Project/Data/Audio/ProjectAudioCatalog.asset";
    private const string WaveDialogueCatalogAssetPath = "Assets/Project/Data/Dialogue/WaveDialogueCatalog.asset";
    private const string PlayerAudioProfileAssetPath = "Assets/Project/Data/Player/DefaultPlayerAudioProfile.asset";
    private const string UiAudioProfileAssetPath = "Assets/Project/Data/UI/DefaultUiAudioProfile.asset";
    private const string CreditsProfileAssetPath = "Assets/Project/Data/UI/DefaultCreditsPresentationProfile.asset";
    private const string EnemyAudioProfileAssetPath = "Assets/Project/Data/Enemy/DefaultEnemyAudioProfile.asset";
    private const string DefaultUIButtonFxProfileAssetPath = "Assets/Project/Data/FX/DefaultUIButtonFxProfile.asset";
    private const string DefaultUIPanelFxProfileAssetPath = "Assets/Project/Data/FX/DefaultUIPanelFxProfile.asset";

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

            if (assetPath.Contains("Original ConfigPrefab"))
            {
                continue;
            }

            WeaponStatsProfile profile = AssetDatabase.LoadAssetAtPath<WeaponStatsProfile>(assetPath);
            Assert.That(profile, Is.Not.Null, $"No se pudo cargar WeaponStatsProfile en {assetPath}.");
            Assert.That(profile.MaxRange, Is.GreaterThan(0f), $"{assetPath} necesita MaxRange positivo.");
            Assert.That(profile.FireRate, Is.GreaterThan(0f), $"{assetPath} necesita FireRate positivo.");
            Assert.That(profile.MagazineSize, Is.GreaterThan(0), $"{assetPath} necesita MagazineSize positivo.");
            Assert.That(profile.DamagePerShot, Is.GreaterThan(0), $"{assetPath} necesita DamagePerShot positivo.");
            Assert.That(profile.ReloadDuration, Is.GreaterThan(0f), $"{assetPath} necesita ReloadDuration positiva.");
            Assert.That(profile.PresentationProfile, Is.Not.Null, $"{assetPath} necesita WeaponPresentationProfile.");
        }
    }

    [Test]
    public void ProjectAudioCatalog_OnlyContainsGlobalAudioOwnership()
    {
        string assetText = ReadAssetText(ProjectAudioCatalogAssetPath);
        Assert.That(assetText, Does.Contain("music:"));
        Assert.That(assetText, Does.Contain("waves:"));
        Assert.That(assetText, Does.Not.Contain("\n  player:"));
        Assert.That(assetText, Does.Not.Contain("\n  enemy:"));
        Assert.That(assetText, Does.Not.Contain("\n  weapons:"));
        Assert.That(assetText, Does.Not.Contain("\n  ui:"));
        Assert.That(assetText, Does.Not.Contain("\n  credits:"));
    }

    [Test]
    public void DefaultPlayerAudioProfile_HasRequiredClips()
    {
        PlayerAudioProfile profile = AssetDatabase.LoadAssetAtPath<PlayerAudioProfile>(PlayerAudioProfileAssetPath);
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.JumpClip, Is.Not.Null);
        Assert.That(profile.FootstepClips, Is.Not.Null);
        Assert.That(profile.FootstepClips.Length, Is.GreaterThan(0));
        Assert.That(profile.WeaponSwitchClip, Is.Not.Null);
        Assert.That(profile.CoinPickupClip, Is.Not.Null);
        Assert.That(profile.KillConfirmClip, Is.Not.Null);
        Assert.That(profile.HitmarkerClip, Is.Not.Null);
        Assert.That(profile.HitmarkerVolume, Is.InRange(0f, 1f));
        Assert.That(profile.HurtClip, Is.Not.Null);
        Assert.That(profile.HurtVolume, Is.InRange(0f, 1f));
        Assert.That(profile.DeathClip, Is.Not.Null);
        Assert.That(profile.DeathVolume, Is.InRange(0f, 1f));
    }

    [Test]
    public void DefaultUiAudioProfile_HasRequiredClips()
    {
        UiAudioProfile profile = AssetDatabase.LoadAssetAtPath<UiAudioProfile>(UiAudioProfileAssetPath);
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.ClickClip, Is.Not.Null);
        Assert.That(profile.ClickVolume, Is.InRange(0f, 1f));
        Assert.That(profile.HoverClip, Is.Not.Null);
        Assert.That(profile.HoverVolume, Is.InRange(0f, 1f));
        Assert.That(profile.PanelOpenClip, Is.Not.Null);
        Assert.That(profile.PanelOpenVolume, Is.InRange(0f, 1f));
        Assert.That(profile.PanelCloseClip, Is.Not.Null);
        Assert.That(profile.PanelCloseVolume, Is.InRange(0f, 1f));
        Assert.That(profile.ShopPurchaseSuccessClip, Is.Not.Null);
        Assert.That(profile.ShopPurchaseSuccessVolume, Is.InRange(0f, 1f));
        Assert.That(profile.ShopPurchaseFailedClip, Is.Not.Null);
        Assert.That(profile.ShopPurchaseFailedVolume, Is.InRange(0f, 1f));
    }

    [Test]
    public void DefaultEnemyAudioProfile_HasRequiredAlertClip()
    {
        EnemyAudioProfile profile = AssetDatabase.LoadAssetAtPath<EnemyAudioProfile>(EnemyAudioProfileAssetPath);
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.AlertClip, Is.Not.Null);
    }

    [Test]
    public void DefaultCreditsPresentationProfile_HasRequiredCreditsClips_AndNoFallbackFlag()
    {
        CreditsPresentationProfile profile = AssetDatabase.LoadAssetAtPath<CreditsPresentationProfile>(CreditsProfileAssetPath);
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.IntroWhooshClip, Is.Not.Null);
        Assert.That(profile.IntroWhooshVolume, Is.InRange(0f, 1f));
        Assert.That(profile.NameHitClip, Is.Not.Null);
        Assert.That(profile.NameHitVolume, Is.InRange(0f, 1f));
        Assert.That(profile.NameTickClip, Is.Not.Null);
        Assert.That(profile.NameTickVolume, Is.InRange(0f, 1f));
        Assert.That(profile.FinalStingClip, Is.Not.Null);
        Assert.That(profile.FinalStingVolume, Is.InRange(0f, 1f));
        Assert.That(profile.OutroSwishClip, Is.Not.Null);
        Assert.That(profile.OutroSwishVolume, Is.InRange(0f, 1f));

        string assetText = ReadAssetText(CreditsProfileAssetPath);
        Assert.That(assetText, Does.Not.Contain("useAudioManagerFallback:"));
    }

    [Test]
    public void ProjectAudioCatalog_HasRequiredClips_AndVolumes()
    {
        ProjectAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<ProjectAudioCatalog>(ProjectAudioCatalogAssetPath);
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog.Music.MainMenuAudio.Clip, Is.Not.Null);
        Assert.That(catalog.Music.MainMenuAudio.Volume, Is.InRange(0f, 1f));
        Assert.That(catalog.Music.GameplayAudio.Clip, Is.Not.Null);
        Assert.That(catalog.Music.GameplayAudio.Volume, Is.InRange(0f, 1f));
        Assert.That(catalog.Music.ShopAudio.Clip, Is.Not.Null);
        Assert.That(catalog.Music.ShopAudio.Volume, Is.InRange(0f, 1f));
        Assert.That(catalog.Music.EndMenuAudio.Clip, Is.Not.Null);
        Assert.That(catalog.Music.EndMenuAudio.Volume, Is.InRange(0f, 1f));
        Assert.That(catalog.Waves.AnnouncementAudio.Clip, Is.Not.Null);
        Assert.That(catalog.Waves.AnnouncementAudio.Volume, Is.InRange(0f, 1f));
    }

    [Test]
    public void WaveDialogueCatalog_Exists_AsCentralDialogueSource()
    {
        WaveDialogueCatalog catalog = AssetDatabase.LoadAssetAtPath<WaveDialogueCatalog>(WaveDialogueCatalogAssetPath);
        Assert.That(catalog, Is.Not.Null, "WaveDialogueCatalog.asset debe existir como fuente central de dialogos.");
    }

    [Test]
    public void UIButtonFxProfile_And_UIPanelFxProfile_DoNotSerializeLegacyAudioFields()
    {
        string buttonProfileText = ReadAssetText(DefaultUIButtonFxProfileAssetPath);
        Assert.That(buttonProfileText, Does.Not.Contain("hoverClip:"));
        Assert.That(buttonProfileText, Does.Not.Contain("clickClip:"));
        Assert.That(buttonProfileText, Does.Not.Contain("useAudioManagerFallback:"));

        string panelProfileText = ReadAssetText(DefaultUIPanelFxProfileAssetPath);
        Assert.That(panelProfileText, Does.Not.Contain("openClip:"));
        Assert.That(panelProfileText, Does.Not.Contain("closeClip:"));
        Assert.That(panelProfileText, Does.Not.Contain("useAudioManagerFallback:"));
    }

    [Test]
    public void WeaponStatsProfiles_DoNotSerializeLegacyFx()
    {
        string[] assetPaths =
        {
            "Assets/Project/Data/Weapons/LowPoly1323PlayerWeaponStatsProfile.asset",
            "Assets/Project/Data/Weapons/AssaultRiflePlayerWeaponStatsProfile.asset",
            "Assets/Project/Data/Weapons/AssaultRifleEnemyWeaponStatsProfile.asset",
            "Assets/Project/Data/Weapons/TacticalRiflePlayerWeaponStatsProfile.asset",
            "Assets/Project/Data/Weapons/TacticalRifleEnemyWeaponStatsProfile.asset"
        };

        for (int i = 0; i < assetPaths.Length; i++)
        {
            string assetText = ReadAssetText(assetPaths[i]);
            Assert.That(assetText, Does.Contain("presentationProfile:"));
            Assert.That(assetText, Does.Not.Contain("muzzleFlashPrefab:"));
            Assert.That(assetText, Does.Not.Contain("fireSound:"));
            Assert.That(assetText, Does.Not.Contain("dryFireSound:"));
            Assert.That(assetText, Does.Not.Contain("reloadSound:"));
            Assert.That(assetText, Does.Not.Contain("fireVolume:"));
            Assert.That(assetText, Does.Not.Contain("dryFireVolume:"));
            Assert.That(assetText, Does.Not.Contain("reloadVolume:"));
            Assert.That(assetText, Does.Not.Contain("firePitchRandomness:"));
            Assert.That(assetText, Does.Not.Contain("dryFirePitchRandomness:"));
            Assert.That(assetText, Does.Not.Contain("reloadPitchRandomness:"));
        }
    }

    private static string ReadAssetText(string assetPath)
    {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        Assert.That(File.Exists(fullPath), Is.True, $"El asset '{assetPath}' debe existir.");
        return File.ReadAllText(fullPath);
    }
}
