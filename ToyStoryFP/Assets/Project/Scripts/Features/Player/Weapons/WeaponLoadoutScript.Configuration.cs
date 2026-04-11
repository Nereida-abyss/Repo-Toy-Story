using System;
using System.Collections.Generic;
using UnityEngine;

public partial class WeaponLoadoutScript
{
    public bool TryPurchaseWeapon(string weaponId, PlayerCurrencyController currency, bool autoEquip, out string failReason)
    {
        failReason = string.Empty;
        EnsureConfigurationIsReady();

        string normalizedWeaponId = NormalizeWeaponId(weaponId);
        if (string.IsNullOrEmpty(normalizedWeaponId))
        {
            failReason = "Invalid weapon id.";
            return false;
        }

        if (!definitionsById.TryGetValue(normalizedWeaponId, out ConfiguredWeaponDefinition definition) || definition == null)
        {
            failReason = $"Weapon '{normalizedWeaponId}' is not registered.";
            return false;
        }

        if (definition.Weapon == null)
        {
            failReason = $"Weapon '{normalizedWeaponId}' has no runtime WeaponScript assigned.";
            return false;
        }

        if (IsWeaponUnlocked(normalizedWeaponId))
        {
            return true;
        }

        int price = Mathf.Max(0, definition.Price);
        if (currency == null)
        {
            failReason = "Currency controller is missing.";
            return false;
        }

        if (!currency.TrySpendCoins(price))
        {
            failReason = "Not enough coins.";
            return false;
        }

        unlockedWeaponIds.Add(normalizedWeaponId);
        RebuildUnlockedWeapons();

        if (unlockedWeapons.Length == 0)
        {
            failReason = $"Weapon '{normalizedWeaponId}' was unlocked but cannot be equipped.";
            return true;
        }

        if (autoEquip)
        {
            int unlockedIndex = FindUnlockedWeaponIndex(normalizedWeaponId);
            if (unlockedIndex >= 0)
            {
                SetWeaponIndexImmediate(unlockedIndex);
            }
        }
        else if (equippedWeaponIndex < 0 || equippedWeaponIndex >= unlockedWeapons.Length)
        {
            SetWeaponIndexImmediate(0);
        }
        else
        {
            EnsureEquippedWeaponVisible();
        }

        return true;
    }

    public bool IsWeaponUnlocked(string weaponId)
    {
        string normalizedWeaponId = NormalizeWeaponId(weaponId);
        return !string.IsNullOrEmpty(normalizedWeaponId) && unlockedWeaponIds.Contains(normalizedWeaponId);
    }

    public IReadOnlyList<WeaponShopEntry> GetWeaponShopEntries()
    {
        EnsureConfigurationIsReady();
        shopEntries.Clear();

        for (int i = 0; i < configuredDefinitions.Count; i++)
        {
            ConfiguredWeaponDefinition definition = configuredDefinitions[i];
            if (definition == null || definition.Weapon == null || string.IsNullOrEmpty(definition.WeaponId))
            {
                continue;
            }

            shopEntries.Add(new WeaponShopEntry(definition.WeaponId, Mathf.Max(0, definition.Price), IsWeaponUnlocked(definition.WeaponId)));
        }

        return shopEntries;
    }

    public bool TryGetShopEntry(string weaponId, out WeaponShopEntry entry)
    {
        EnsureConfigurationIsReady();

        string normalizedWeaponId = NormalizeWeaponId(weaponId);
        if (string.IsNullOrEmpty(normalizedWeaponId))
        {
            entry = default(WeaponShopEntry);
            return false;
        }

        if (!definitionsById.TryGetValue(normalizedWeaponId, out ConfiguredWeaponDefinition definition) || definition == null)
        {
            entry = default(WeaponShopEntry);
            return false;
        }

        if (definition.Weapon == null)
        {
            entry = default(WeaponShopEntry);
            return false;
        }

        entry = new WeaponShopEntry(normalizedWeaponId, Mathf.Max(0, definition.Price), IsWeaponUnlocked(normalizedWeaponId));
        return true;
    }

    private void CacheConfiguration()
    {
        configurationCached = true;
        definitionsById.Clear();
        configuredDefinitions.Clear();
        weaponIdByWeapon.Clear();

        WeaponScript[] detectedWeapons = weaponCamera != null
            ? weaponCamera.GetComponentsInChildren<WeaponScript>(true)
            : Array.Empty<WeaponScript>();

        if (weaponCatalog == null)
        {
            WarnIfCatalogMissing();
            allWeapons = Array.Empty<WeaponScript>();
            unexpectedWeapons = detectedWeapons;
            return;
        }

        hasLoggedMissingCatalog = false;

        IReadOnlyList<WeaponCatalog.WeaponCatalogEntry> catalogEntries = weaponCatalog.Entries;
        List<WeaponScript> configuredWeapons = new List<WeaponScript>(catalogEntries != null ? catalogEntries.Count : 0);
        HashSet<WeaponScript> registeredWeapons = new HashSet<WeaponScript>();

        if (catalogEntries != null)
        {
            for (int i = 0; i < catalogEntries.Count; i++)
            {
                WeaponCatalog.WeaponCatalogEntry entry = catalogEntries[i];
                if (entry == null)
                {
                    continue;
                }

                string weaponId = NormalizeWeaponId(entry.WeaponId);
                if (string.IsNullOrEmpty(weaponId))
                {
                    GameDebug.Advertencia("Armas", "WeaponCatalog encontro una entrada sin id.", this);
                    continue;
                }

                if (definitionsById.ContainsKey(weaponId))
                {
                    GameDebug.Advertencia("Armas", $"Id de arma duplicado '{weaponId}' en WeaponCatalog. Se mantiene la primera ocurrencia.", this);
                    continue;
                }

                WeaponScript resolvedWeapon = ResolveConfiguredWeaponInstance(weaponId, entry.Weapon, detectedWeapons);
                ConfiguredWeaponDefinition definition = new ConfiguredWeaponDefinition
                {
                    WeaponId = weaponId,
                    Weapon = resolvedWeapon,
                    Price = Mathf.Max(0, entry.Price),
                    UnlockedByDefault = entry.UnlockedByDefault
                };

                definitionsById.Add(weaponId, definition);
                configuredDefinitions.Add(definition);

                if (resolvedWeapon == null)
                {
                    GameDebug.Advertencia("Armas", $"El arma '{weaponId}' no pudo resolverse bajo la weaponCamera del jugador.", this);
                    continue;
                }

                if (!registeredWeapons.Add(resolvedWeapon))
                {
                    GameDebug.Advertencia("Armas", $"El arma '{weaponId}' comparte WeaponScript con otra entrada del catalogo. Revisa la configuracion.", this);
                    continue;
                }

                configuredWeapons.Add(resolvedWeapon);
                weaponIdByWeapon.Add(resolvedWeapon, weaponId);
                resolvedWeapon._camera = weaponCamera;
                resolvedWeapon.ConfigureAudioControllers(playerAudio, null);
                resolvedWeapon.SetPlayerOwned(true);
            }
        }

        allWeapons = configuredWeapons.ToArray();
        CacheUnexpectedWeapons(registeredWeapons, detectedWeapons);
    }

    private void RebuildUnlockedWeapons()
    {
        List<WeaponScript> unlocked = new List<WeaponScript>(allWeapons.Length);

        for (int i = 0; i < allWeapons.Length; i++)
        {
            WeaponScript weapon = allWeapons[i];
            if (weapon == null)
            {
                continue;
            }

            string weaponId = ResolveWeaponId(weapon);
            bool isUnlocked = !string.IsNullOrEmpty(weaponId) && unlockedWeaponIds.Contains(weaponId);

            if (isUnlocked)
            {
                unlocked.Add(weapon);
            }
            else if (weapon.gameObject.activeSelf)
            {
                weapon.gameObject.SetActive(false);
            }
        }

        unlockedWeapons = unlocked.ToArray();
    }

    private string ResolveWeaponId(WeaponScript weapon)
    {
        if (weapon == null)
        {
            return string.Empty;
        }

        return weaponIdByWeapon.TryGetValue(weapon, out string mappedId) ? mappedId : string.Empty;
    }

    private int ResolveStartingWeaponIndex()
    {
        string preferredId = NormalizeWeaponId(weaponCatalog != null ? weaponCatalog.DefaultStartingWeaponId : string.Empty);
        if (!string.IsNullOrEmpty(preferredId))
        {
            int preferredIndex = FindUnlockedWeaponIndex(preferredId);
            if (preferredIndex >= 0)
            {
                return preferredIndex;
            }
        }

        return 0;
    }

    private int FindUnlockedWeaponIndex(string weaponId)
    {
        string normalizedWeaponId = NormalizeWeaponId(weaponId);
        if (string.IsNullOrEmpty(normalizedWeaponId))
        {
            return -1;
        }

        for (int i = 0; i < unlockedWeapons.Length; i++)
        {
            if (ResolveWeaponId(unlockedWeapons[i]) == normalizedWeaponId)
            {
                return i;
            }
        }

        return -1;
    }

    private string GetFirstConfiguredWeaponId()
    {
        for (int i = 0; i < configuredDefinitions.Count; i++)
        {
            ConfiguredWeaponDefinition definition = configuredDefinitions[i];
            if (definition != null && definition.Weapon != null && !string.IsNullOrEmpty(definition.WeaponId))
            {
                return definition.WeaponId;
            }
        }

        return string.Empty;
    }

    private void EnsureConfigurationIsReady()
    {
        if (!configurationCached)
        {
            CacheConfiguration();
        }
    }

    private string NormalizeWeaponId(string rawWeaponId)
    {
        return string.IsNullOrWhiteSpace(rawWeaponId) ? string.Empty : rawWeaponId.Trim();
    }

    private void WarnIfCatalogMissing()
    {
        if (weaponCatalog != null)
        {
            hasLoggedMissingCatalog = false;
            return;
        }

        if (hasLoggedMissingCatalog)
        {
            return;
        }

        hasLoggedMissingCatalog = true;
        GameDebug.Advertencia("Armas", "WeaponLoadoutScript no tiene WeaponCatalog asignado. El loadout quedara inactivo hasta configurarlo.", this);
    }

    private void CacheUnexpectedWeapons(HashSet<WeaponScript> configuredWeapons, WeaponScript[] detectedWeapons)
    {
        unexpectedWeapons = Array.Empty<WeaponScript>();

        if (weaponCamera == null || detectedWeapons == null || detectedWeapons.Length == 0)
        {
            return;
        }

        List<WeaponScript> extras = new List<WeaponScript>();
        HashSet<GameObject> configuredWeaponObjects = new HashSet<GameObject>();
        HashSet<string> configuredWeaponIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (WeaponScript configuredWeapon in configuredWeapons)
        {
            if (configuredWeapon != null)
            {
                configuredWeaponObjects.Add(configuredWeapon.gameObject);
            }
        }

        foreach (ConfiguredWeaponDefinition definition in configuredDefinitions)
        {
            if (definition != null && !string.IsNullOrEmpty(definition.WeaponId))
            {
                configuredWeaponIds.Add(definition.WeaponId);
            }
        }

        for (int i = 0; i < detectedWeapons.Length; i++)
        {
            WeaponScript detectedWeapon = detectedWeapons[i];
            if (detectedWeapon == null || configuredWeapons.Contains(detectedWeapon))
            {
                continue;
            }

            if (configuredWeaponObjects.Contains(detectedWeapon.gameObject))
            {
                continue;
            }

            string detectedWeaponId = NormalizeWeaponId(detectedWeapon.name);
            if (!string.IsNullOrEmpty(detectedWeaponId) && configuredWeaponIds.Contains(detectedWeaponId))
            {
                continue;
            }

            extras.Add(detectedWeapon);

            if (warnedUnexpectedWeapons.Add(detectedWeapon))
            {
                GameDebug.Advertencia(
                    "Armas",
                    $"Se detecto un WeaponScript fuera de WeaponCatalog en '{detectedWeapon.name}'. Quedara inactivo hasta que se configure en el catalogo.",
                    detectedWeapon);
            }
        }

        unexpectedWeapons = extras.ToArray();
    }

    private WeaponScript ResolveConfiguredWeaponInstance(string weaponId, WeaponScript configuredWeapon, WeaponScript[] detectedWeapons)
    {
        if (configuredWeapon != null
            && weaponCamera != null
            && configuredWeapon.transform != null
            && configuredWeapon.transform.IsChildOf(weaponCamera.transform))
        {
            return configuredWeapon;
        }

        if (string.IsNullOrEmpty(weaponId) || detectedWeapons == null)
        {
            return null;
        }

        WeaponScript matchedWeapon = null;

        for (int i = 0; i < detectedWeapons.Length; i++)
        {
            WeaponScript detectedWeapon = detectedWeapons[i];
            if (detectedWeapon == null)
            {
                continue;
            }

            if (NormalizeWeaponId(detectedWeapon.name) != weaponId)
            {
                continue;
            }

            if (matchedWeapon != null && matchedWeapon != detectedWeapon)
            {
                GameDebug.Advertencia("Armas", $"Hay varias armas llamadas '{weaponId}' bajo la weaponCamera. Revisa la jerarquia.", this);
                return null;
            }

            matchedWeapon = detectedWeapon;
        }

        return matchedWeapon;
    }

    private void DeactivateUnexpectedWeapons()
    {
        for (int i = 0; i < unexpectedWeapons.Length; i++)
        {
            if (unexpectedWeapons[i] != null && unexpectedWeapons[i].gameObject.activeSelf)
            {
                unexpectedWeapons[i].gameObject.SetActive(false);
            }
        }
    }
}
