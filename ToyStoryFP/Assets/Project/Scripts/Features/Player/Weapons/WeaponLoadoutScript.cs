using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public partial class WeaponLoadoutScript : MonoBehaviour
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

}
