using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponLoadoutScript : MonoBehaviour
{
    private sealed class ConfiguredWeaponDefinition
    {
        public string WeaponId;
        public WeaponScript Weapon;
        public int Price;
        public bool UnlockedByDefault;
    }

    public struct WeaponShopEntry
    {
        public string WeaponId { get; }
        public int Price { get; }
        public bool IsUnlocked { get; }

        public WeaponShopEntry(string weaponId, int price, bool isUnlocked)
        {
            WeaponId = weaponId;
            Price = price;
            IsUnlocked = isUnlocked;
        }
    }

    private enum WeaponSwitchState
    {
        Idle,
        Lowering,
        Raising
    }

    [SerializeField] private Camera weaponCamera;
    [SerializeField] private PlayerAudioController playerAudio;
    [SerializeField] private WeaponCatalog weaponCatalog;
    [SerializeField] private float weaponSwitchDuration = 0.35f;
    [SerializeField] private float weaponSwitchLowerRatio = 0.45f;
    [SerializeField] private float weaponSwitchRaiseRatio = 0.45f;

    private WeaponScript[] allWeapons = Array.Empty<WeaponScript>();
    private WeaponScript[] unlockedWeapons = Array.Empty<WeaponScript>();
    private int equippedWeaponIndex = -1;
    private int targetWeaponIndex = -1;
    private float switchPhaseTimer;
    private WeaponSwitchState switchState = WeaponSwitchState.Idle;
    private WeaponScript visibleWeapon;
    private bool initialLoadoutResolved;
    private bool runLoadoutInitialized;
    private bool configurationCached;
    private bool hasLoggedMissingCatalog;
    private WeaponScript[] unexpectedWeapons = Array.Empty<WeaponScript>();

    private readonly Dictionary<string, ConfiguredWeaponDefinition> definitionsById =
        new Dictionary<string, ConfiguredWeaponDefinition>(StringComparer.Ordinal);
    private readonly List<ConfiguredWeaponDefinition> configuredDefinitions = new List<ConfiguredWeaponDefinition>();
    private readonly Dictionary<WeaponScript, string> weaponIdByWeapon = new Dictionary<WeaponScript, string>();
    private readonly HashSet<string> unlockedWeaponIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<WeaponShopEntry> shopEntries = new List<WeaponShopEntry>();
    private readonly HashSet<WeaponScript> warnedUnexpectedWeapons = new HashSet<WeaponScript>();

    public event Action<WeaponScript> CurrentWeaponChanged;

    public WeaponScript CurrentWeapon =>
        equippedWeaponIndex >= 0 && equippedWeaponIndex < unlockedWeapons.Length
            ? unlockedWeapons[equippedWeaponIndex]
            : null;

    public bool IsSwitchingWeapon => switchState != WeaponSwitchState.Idle;

    void Awake()
    {
        RefreshWeapons();
        WarnIfCoreReferencesAreMissing();
    }

    void Update()
    {
        if (!IsSwitchingWeapon)
        {
            EnsureEquippedWeaponVisible();
            return;
        }

        float deltaTime = Time.deltaTime;

        switch (switchState)
        {
            case WeaponSwitchState.Lowering:
                UpdateLowering(deltaTime);
                break;
            case WeaponSwitchState.Raising:
                UpdateRaising(deltaTime);
                break;
        }
    }

    public void RefreshWeapons()
    {
        CacheConfiguration();

        if (!runLoadoutInitialized)
        {
            ResetCurrentSelection();
            DeactivateAllWeapons();
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        RebuildUnlockedWeapons();

        if (unlockedWeapons.Length == 0)
        {
            ResetCurrentSelection();
            DeactivateAllWeapons();
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= unlockedWeapons.Length)
        {
            equippedWeaponIndex = ResolveStartingWeaponIndex();
        }

        SetWeaponIndexImmediate(equippedWeaponIndex);
    }

    public void BeginRunLoadout()
    {
        CacheConfiguration();
        ResetCurrentSelection();
        DeactivateAllWeapons();
        unlockedWeaponIds.Clear();

        for (int i = 0; i < configuredDefinitions.Count; i++)
        {
            ConfiguredWeaponDefinition definition = configuredDefinitions[i];
            if (definition == null || definition.Weapon == null || !definition.UnlockedByDefault)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(definition.WeaponId))
            {
                unlockedWeaponIds.Add(definition.WeaponId);
            }
        }

        if (unlockedWeaponIds.Count == 0)
        {
            string fallbackWeaponId = GetFirstConfiguredWeaponId();
            if (!string.IsNullOrEmpty(fallbackWeaponId))
            {
                unlockedWeaponIds.Add(fallbackWeaponId);
            }
        }

        runLoadoutInitialized = true;
        RebuildUnlockedWeapons();

        if (unlockedWeapons.Length == 0)
        {
            ResetCurrentSelection();
            DeactivateAllWeapons();
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        int startingIndex = ResolveStartingWeaponIndex();
        bool initializeSilently = !initialLoadoutResolved;
        SetWeaponIndexImmediate(startingIndex, initializeSilently);
        initialLoadoutResolved = true;
    }

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

    public bool RefillCurrentWeaponAmmoToConfiguredReserve()
    {
        return CurrentWeapon != null && CurrentWeapon.RefillAmmoToConfiguredReserve();
    }

    public bool AddAmmoToCurrentWeaponByMagazines(int magazineCount)
    {
        return CurrentWeapon != null && CurrentWeapon.TryAddAmmoByMagazines(magazineCount);
    }

    public bool TryCycleWeapon(int direction)
    {
        if (unlockedWeapons.Length <= 1 || IsSwitchingWeapon)
        {
            return false;
        }

        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= unlockedWeapons.Length)
        {
            SetWeaponIndexImmediate(0);
        }

        int nextIndex = equippedWeaponIndex;

        do
        {
            nextIndex = (nextIndex + direction + unlockedWeapons.Length) % unlockedWeapons.Length;
        }
        while (nextIndex == equippedWeaponIndex);

        StartWeaponSwitch(nextIndex);
        return true;
    }

    private void StartWeaponSwitch(int nextIndex)
    {
        if (nextIndex < 0 || nextIndex >= unlockedWeapons.Length || nextIndex == equippedWeaponIndex)
        {
            return;
        }

        EnsureEquippedWeaponVisible();

        targetWeaponIndex = nextIndex;
        visibleWeapon = CurrentWeapon;
        switchState = WeaponSwitchState.Lowering;
        switchPhaseTimer = GetLowerDuration();

        visibleWeapon?.CancelReload();
        playerAudio?.PlayWeaponSwitch();
        CurrentWeaponChanged?.Invoke(null);

        if (switchPhaseTimer <= 0f)
        {
            CompleteLowering();
        }
    }

    private void UpdateLowering(float deltaTime)
    {
        if (visibleWeapon == null)
        {
            EnsureEquippedWeaponVisible();
            visibleWeapon = CurrentWeapon;
        }

        float duration = GetLowerDuration();
        float progress = duration <= 0.01f
            ? 1f
            : 1f - Mathf.Clamp01(switchPhaseTimer / duration);

        visibleWeapon?.SetEquipAnimationProgress(SmoothPhase(progress), true);
        switchPhaseTimer -= deltaTime;

        if (switchPhaseTimer <= 0f)
        {
            CompleteLowering();
        }
    }

    private void CompleteLowering()
    {
        if (targetWeaponIndex < 0 || targetWeaponIndex >= unlockedWeapons.Length)
        {
            CancelWeaponSwitch();
            return;
        }

        ActivateOnly(targetWeaponIndex);
        equippedWeaponIndex = targetWeaponIndex;
        targetWeaponIndex = -1;
        visibleWeapon = CurrentWeapon;
        visibleWeapon?.NotifyEquipped();
        visibleWeapon?.SetEquipAnimationProgress(0f, false);
        CurrentWeaponChanged?.Invoke(visibleWeapon);

        switchState = WeaponSwitchState.Raising;
        switchPhaseTimer = GetRaiseDuration();

        if (switchPhaseTimer <= 0f)
        {
            FinishWeaponSwitch();
        }
    }

    private void UpdateRaising(float deltaTime)
    {
        if (visibleWeapon == null)
        {
            EnsureEquippedWeaponVisible();
            visibleWeapon = CurrentWeapon;
        }

        float duration = GetRaiseDuration();
        float progress = duration <= 0.01f
            ? 1f
            : 1f - Mathf.Clamp01(switchPhaseTimer / duration);

        visibleWeapon?.SetEquipAnimationProgress(SmoothPhase(progress), false);
        switchPhaseTimer -= deltaTime;

        if (switchPhaseTimer <= 0f)
        {
            FinishWeaponSwitch();
        }
    }

    private void FinishWeaponSwitch()
    {
        switchState = WeaponSwitchState.Idle;
        switchPhaseTimer = 0f;
        targetWeaponIndex = -1;

        if (visibleWeapon == null)
        {
            EnsureEquippedWeaponVisible();
            visibleWeapon = CurrentWeapon;
        }

        visibleWeapon?.ResetEquipPose();
        visibleWeapon?.gameObject.SetActive(true);
        CurrentWeaponChanged?.Invoke(visibleWeapon);
    }

    private void CancelWeaponSwitch()
    {
        switchState = WeaponSwitchState.Idle;
        switchPhaseTimer = 0f;
        targetWeaponIndex = -1;
        EnsureEquippedWeaponVisible();
        visibleWeapon = CurrentWeapon;
        CurrentWeaponChanged?.Invoke(visibleWeapon);
    }

    private void SetWeaponIndexImmediate(int index, bool initializeSilently = false)
    {
        if (unlockedWeapons.Length == 0)
        {
            ResetCurrentSelection();
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        equippedWeaponIndex = Mathf.Clamp(index, 0, unlockedWeapons.Length - 1);
        targetWeaponIndex = -1;
        switchState = WeaponSwitchState.Idle;
        switchPhaseTimer = 0f;

        ActivateOnly(equippedWeaponIndex);
        visibleWeapon = CurrentWeapon;

        if (initializeSilently)
        {
            visibleWeapon?.InitializeAsEquippedAtSpawn();
        }
        else
        {
            visibleWeapon?.NotifyEquipped();
            visibleWeapon?.ResetEquipPose();
        }

        CurrentWeaponChanged?.Invoke(visibleWeapon);
    }

    private void EnsureEquippedWeaponVisible()
    {
        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= unlockedWeapons.Length)
        {
            return;
        }

        WeaponScript equippedWeapon = unlockedWeapons[equippedWeaponIndex];
        if (equippedWeapon == null)
        {
            return;
        }

        int activeWeaponCount = 0;
        bool weaponMissing = !equippedWeapon.gameObject.activeSelf;

        for (int i = 0; i < allWeapons.Length; i++)
        {
            WeaponScript weapon = allWeapons[i];
            if (weapon == null)
            {
                continue;
            }

            if (weapon.gameObject.activeSelf)
            {
                activeWeaponCount++;
            }

            if (weapon != equippedWeapon && weapon.gameObject.activeSelf)
            {
                weaponMissing = true;
            }
        }

        if (activeWeaponCount != 1)
        {
            weaponMissing = true;
        }

        if (!weaponMissing)
        {
            visibleWeapon = equippedWeapon;
            return;
        }

        ActivateOnly(equippedWeaponIndex);
        equippedWeapon.NotifyEquipped();
        equippedWeapon.ResetEquipPose();
        visibleWeapon = equippedWeapon;
    }

    private void ActivateOnly(int index)
    {
        WeaponScript activeWeapon =
            index >= 0 && index < unlockedWeapons.Length
                ? unlockedWeapons[index]
                : null;

        for (int i = 0; i < allWeapons.Length; i++)
        {
            WeaponScript weapon = allWeapons[i];
            if (weapon == null)
            {
                continue;
            }

            bool shouldBeActive = weapon == activeWeapon;
            if (weapon.gameObject.activeSelf != shouldBeActive)
            {
                weapon.gameObject.SetActive(shouldBeActive);
            }
        }

        DeactivateUnexpectedWeapons();
    }

    private float GetLowerDuration()
    {
        float totalRatio = Mathf.Max(0.01f, Mathf.Clamp01(weaponSwitchLowerRatio) + Mathf.Clamp01(weaponSwitchRaiseRatio));
        return Mathf.Max(0f, weaponSwitchDuration) * (Mathf.Clamp01(weaponSwitchLowerRatio) / totalRatio);
    }

    private float GetRaiseDuration()
    {
        float totalRatio = Mathf.Max(0.01f, Mathf.Clamp01(weaponSwitchLowerRatio) + Mathf.Clamp01(weaponSwitchRaiseRatio));
        return Mathf.Max(0f, weaponSwitchDuration) * (Mathf.Clamp01(weaponSwitchRaiseRatio) / totalRatio);
    }

    private float SmoothPhase(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
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

    private void ResetCurrentSelection()
    {
        equippedWeaponIndex = -1;
        targetWeaponIndex = -1;
        visibleWeapon = null;
        switchState = WeaponSwitchState.Idle;
        switchPhaseTimer = 0f;
    }

    private void DeactivateAllWeapons()
    {
        for (int i = 0; i < allWeapons.Length; i++)
        {
            if (allWeapons[i] != null && allWeapons[i].gameObject.activeSelf)
            {
                allWeapons[i].gameObject.SetActive(false);
            }
        }

        DeactivateUnexpectedWeapons();
    }

    private void WarnIfCoreReferencesAreMissing()
    {
        if (weaponCamera == null)
        {
            GameDebug.Advertencia("Armas", "WeaponLoadoutScript necesita una weaponCamera asignada en Inspector.", this);
        }

        if (playerAudio == null)
        {
            GameDebug.Advertencia("Armas", "WeaponLoadoutScript necesita PlayerAudioController asignado en Inspector para el sonido de cambio.", this);
        }

        WarnIfCatalogMissing();
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
