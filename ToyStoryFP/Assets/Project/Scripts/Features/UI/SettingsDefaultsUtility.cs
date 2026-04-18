using UnityEngine;

internal static class SettingsDefaultsUtility
{
    private static SettingsDefaultsProfile activeDefaultsProfile;
    private static SettingsDefaultsProfile runtimeFallbackDefaultsProfile;
    private static bool hasLoggedRuntimeFallbackWarning;

    public static void RegisterDefaultsProfile(SettingsDefaultsProfile profile)
    {
        if (profile != null)
        {
            activeDefaultsProfile = profile;
        }
    }

    public static float GetDefaultVolume()
    {
        return Mathf.Clamp01(ResolveActiveDefaultsProfile().DefaultVolume);
    }

    public static float GetDefaultLookSensitivity()
    {
        return ResolveActiveDefaultsProfile().DefaultLookSensitivity;
    }

    public static float GetMinLookSensitivity()
    {
        return ResolveActiveDefaultsProfile().MinLookSensitivity;
    }

    public static float GetMaxLookSensitivity()
    {
        SettingsDefaultsProfile profile = ResolveActiveDefaultsProfile();
        return Mathf.Max(GetMinLookSensitivity(), profile.MaxLookSensitivity);
    }

    public static int GetDefaultWindowedWidth()
    {
        return Mathf.Max(1, ResolveActiveDefaultsProfile().DefaultWindowedWidth);
    }

    public static int GetDefaultWindowedHeight()
    {
        return Mathf.Max(1, ResolveActiveDefaultsProfile().DefaultWindowedHeight);
    }

    public static int GetMinimumWindowedDimension()
    {
        return Mathf.Max(1, ResolveActiveDefaultsProfile().MinimumWindowedDimension);
    }

    private static SettingsDefaultsProfile ResolveActiveDefaultsProfile()
    {
        if (activeDefaultsProfile != null)
        {
            return activeDefaultsProfile;
        }

        runtimeFallbackDefaultsProfile ??= ScriptableObject.CreateInstance<SettingsDefaultsProfile>();

        if (!hasLoggedRuntimeFallbackWarning)
        {
            hasLoggedRuntimeFallbackWarning = true;
            GameDebug.Advertencia("Settings", "No hay SettingsDefaultsProfile registrado. Se usara un perfil temporal de seguridad hasta que la escena registre uno.");
        }

        return runtimeFallbackDefaultsProfile;
    }
}
