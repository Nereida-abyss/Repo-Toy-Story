using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SettingsDefaultsTests
{
    private const string DefaultsAssetPath = "Assets/Project/Data/UI/DefaultSettingsDefaultsProfile.asset";
    private const string MasterVolumeKey = "settings.masterVolume";
    private const string MasterMutedKey = "settings.masterMuted";

    [TearDown]
    public void TearDown()
    {
        SetActiveDefaultsProfile(null);
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(MasterMutedKey);
        AudioListener.volume = 1f;
    }

    [Test]
    public void ApplySavedSettings_UsesSavedPlayerPrefsValues()
    {
        SettingsDefaultsProfile profile = AssetDatabase.LoadAssetAtPath<SettingsDefaultsProfile>(DefaultsAssetPath);
        Assert.That(profile, Is.Not.Null, "DefaultSettingsDefaultsProfile.asset debe existir.");

        SetActiveDefaultsProfile(profile);
        PlayerPrefs.SetFloat(MasterVolumeKey, 0.42f);
        PlayerPrefs.SetInt(MasterMutedKey, 0);
        PlayerPrefs.Save();

        SettingsPanelController.ApplySavedSettings();

        Assert.That(AudioListener.volume, Is.EqualTo(0.42f).Within(0.001f));
        Assert.That(PlayerPrefs.GetFloat(MasterVolumeKey, -1f), Is.EqualTo(0.42f).Within(0.001f));
    }

    [Test]
    public void ApplySavedSettings_FallsBackToProfileDefaultVolume_WhenPrefsAreMissing()
    {
        SettingsDefaultsProfile profile = AssetDatabase.LoadAssetAtPath<SettingsDefaultsProfile>(DefaultsAssetPath);
        Assert.That(profile, Is.Not.Null, "DefaultSettingsDefaultsProfile.asset debe existir.");

        SetActiveDefaultsProfile(profile);
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(MasterMutedKey);

        SettingsPanelController.ApplySavedSettings();

        Assert.That(AudioListener.volume, Is.EqualTo(profile.DefaultVolume).Within(0.001f));
    }

    private static void SetActiveDefaultsProfile(SettingsDefaultsProfile profile)
    {
        FieldInfo activeProfileField = typeof(SettingsPanelController).GetField(
            "activeDefaultsProfile",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(activeProfileField, Is.Not.Null, "No se encontro el campo activeDefaultsProfile.");
        activeProfileField.SetValue(null, profile);
    }
}
