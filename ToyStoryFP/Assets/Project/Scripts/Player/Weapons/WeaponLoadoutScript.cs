using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponLoadoutScript : MonoBehaviour
{
    [Serializable]
    public class WeaponUnlockDefinition
    {
        public string weaponId;
        public WeaponScript weapon;
        public int price;
        public bool unlockedByDefault;
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
    [SerializeField] private float weaponSwitchDuration = 0.35f;
    [SerializeField] private float weaponSwitchLowerRatio = 0.45f;
    [SerializeField] private float weaponSwitchRaiseRatio = 0.45f;
    [SerializeField] private string defaultStartingWeaponId = "Low_Poly_1323";
    [SerializeField] private List<WeaponUnlockDefinition> weaponUnlockDefinitions = new List<WeaponUnlockDefinition>();

    private WeaponScript[] allWeapons = Array.Empty<WeaponScript>();
    private WeaponScript[] unlockedWeapons = Array.Empty<WeaponScript>();
    private int equippedWeaponIndex = -1;
    private int targetWeaponIndex = -1;
    private float switchPhaseTimer;
    private WeaponSwitchState switchState = WeaponSwitchState.Idle;
    private WeaponScript visibleWeapon;
    private PlayerAudioController playerAudio;
    private bool initialLoadoutResolved;
    private bool runLoadoutInitialized;

    private readonly Dictionary<string, WeaponUnlockDefinition> definitionsById =
        new Dictionary<string, WeaponUnlockDefinition>(StringComparer.Ordinal);
    private readonly Dictionary<WeaponScript, string> weaponIdByWeapon = new Dictionary<WeaponScript, string>();
    private readonly HashSet<string> unlockedWeaponIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly List<WeaponShopEntry> shopEntries = new List<WeaponShopEntry>();
    private readonly HashSet<string> knownShopIds = new HashSet<string>(StringComparer.Ordinal);

    public event Action<WeaponScript> CurrentWeaponChanged;

    public WeaponScript CurrentWeapon =>
        equippedWeaponIndex >= 0 && equippedWeaponIndex < unlockedWeapons.Length
            ? unlockedWeapons[equippedWeaponIndex]
            : null;

    public bool IsSwitchingWeapon => switchState != WeaponSwitchState.Idle;

    void Awake()
    {
        if (weaponCamera == null)
        {
            weaponCamera = GetComponent<Camera>();
        }

        playerAudio = GetComponentInParent<PlayerAudioController>();
        RefreshWeapons();
        BeginRunLoadout();
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

    // Refresca armas.
    public void RefreshWeapons()
    {
        allWeapons = CollectDirectChildWeapons();
        BuildDefinitionMaps();

        if (allWeapons.Length == 0)
        {
            unlockedWeapons = Array.Empty<WeaponScript>();
            equippedWeaponIndex = -1;
            targetWeaponIndex = -1;
            visibleWeapon = null;
            switchState = WeaponSwitchState.Idle;
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        for (int i = 0; i < allWeapons.Length; i++)
        {
            if (weaponCamera != null)
            {
                allWeapons[i]._camera = weaponCamera;
            }

            allWeapons[i].SetPlayerOwned(true);
        }

        if (!runLoadoutInitialized)
        {
            unlockedWeapons = allWeapons;
            int activeIndex = ResolveActiveIndex(unlockedWeapons);
            bool initializeSilently = !initialLoadoutResolved;
            SetWeaponIndexImmediate(activeIndex, initializeSilently);
            initialLoadoutResolved = true;
            return;
        }

        RebuildUnlockedWeapons();

        if (unlockedWeapons.Length == 0)
        {
            DeactivateAllWeapons();
            equippedWeaponIndex = -1;
            targetWeaponIndex = -1;
            visibleWeapon = null;
            switchState = WeaponSwitchState.Idle;
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= unlockedWeapons.Length)
        {
            equippedWeaponIndex = 0;
        }

        SetWeaponIndexImmediate(equippedWeaponIndex);
    }

    // Inicia ejecucion equipamiento.
    public void BeginRunLoadout()
    {
        if (allWeapons.Length == 0)
        {
            RefreshWeapons();
        }

        unlockedWeaponIds.Clear();

        for (int i = 0; i < weaponUnlockDefinitions.Count; i++)
        {
            WeaponUnlockDefinition definition = weaponUnlockDefinitions[i];
            if (definition == null || !definition.unlockedByDefault)
            {
                continue;
            }

            string weaponId = NormalizeWeaponId(definition.weaponId);
            if (!string.IsNullOrEmpty(weaponId))
            {
                unlockedWeaponIds.Add(weaponId);
            }
        }

        runLoadoutInitialized = true;
        RebuildUnlockedWeapons();

        if (unlockedWeaponIds.Count == 0 && allWeapons.Length > 0)
        {
            string fallbackWeaponId = ResolveFallbackWeaponId();
            if (!string.IsNullOrEmpty(fallbackWeaponId))
            {
                unlockedWeaponIds.Add(fallbackWeaponId);
                RebuildUnlockedWeapons();
            }
        }

        if (unlockedWeapons.Length == 0)
        {
            DeactivateAllWeapons();
            equippedWeaponIndex = -1;
            targetWeaponIndex = -1;
            visibleWeapon = null;
            switchState = WeaponSwitchState.Idle;
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        int startingIndex = ResolveStartingWeaponIndex();
        bool initializeSilently = !initialLoadoutResolved;
        SetWeaponIndexImmediate(startingIndex, initializeSilently);
        initialLoadoutResolved = true;
    }

    // Intenta compra arma.
    public bool TryPurchaseWeapon(string weaponId, PlayerCurrencyController currency, bool autoEquip, out string failReason)
    {
        failReason = string.Empty;

        string normalizedWeaponId = NormalizeWeaponId(weaponId);
        if (string.IsNullOrEmpty(normalizedWeaponId))
        {
            failReason = "Invalid weapon id.";
            return false;
        }

        if (!definitionsById.TryGetValue(normalizedWeaponId, out WeaponUnlockDefinition definition) || definition == null)
        {
            failReason = $"Weapon '{normalizedWeaponId}' is not registered.";
            return false;
        }

        if (IsWeaponUnlocked(normalizedWeaponId))
        {
            return true;
        }

        int price = Mathf.Max(0, definition.price);
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

    // Comprueba si arma desbloqueada.
    public bool IsWeaponUnlocked(string weaponId)
    {
        string normalizedWeaponId = NormalizeWeaponId(weaponId);
        return !string.IsNullOrEmpty(normalizedWeaponId) && unlockedWeaponIds.Contains(normalizedWeaponId);
    }

    // Obtiene arma tienda entradas.
    public IReadOnlyList<WeaponShopEntry> GetWeaponShopEntries()
    {
        shopEntries.Clear();
        knownShopIds.Clear();

        for (int i = 0; i < weaponUnlockDefinitions.Count; i++)
        {
            WeaponUnlockDefinition definition = weaponUnlockDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            string weaponId = NormalizeWeaponId(definition.weaponId);
            if (string.IsNullOrEmpty(weaponId) || !knownShopIds.Add(weaponId))
            {
                continue;
            }

            shopEntries.Add(new WeaponShopEntry(weaponId, Mathf.Max(0, definition.price), IsWeaponUnlocked(weaponId)));
        }

        return shopEntries;
    }

    // Intenta ciclo arma.
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

    // Inicia arma cambio.
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

        if (playerAudio == null)
        {
            playerAudio = GetComponentInParent<PlayerAudioController>();
        }

        visibleWeapon?.CancelReload();
        playerAudio?.PlayWeaponSwitch();
        CurrentWeaponChanged?.Invoke(null);

        if (switchPhaseTimer <= 0f)
        {
            CompleteLowering();
        }
    }

    // Actualiza bajada.
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

    // Completa bajada.
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

    // Actualiza subida.
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

    // Finaliza arma cambio.
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

    // Gestiona la cancelacion del cambio de arma.
    private void CancelWeaponSwitch()
    {
        switchState = WeaponSwitchState.Idle;
        switchPhaseTimer = 0f;
        targetWeaponIndex = -1;
        EnsureEquippedWeaponVisible();
        visibleWeapon = CurrentWeapon;
        CurrentWeaponChanged?.Invoke(visibleWeapon);
    }

    // Actualiza arma indice inmediato.
    private void SetWeaponIndexImmediate(int index, bool initializeSilently = false)
    {
        if (unlockedWeapons.Length == 0)
        {
            equippedWeaponIndex = -1;
            targetWeaponIndex = -1;
            visibleWeapon = null;
            switchState = WeaponSwitchState.Idle;
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

    // Asegura que el arma equipada este visible.
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

    // Gestiona activate only.
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
    }

    // Obtiene lower duracion.
    private float GetLowerDuration()
    {
        float totalRatio = Mathf.Max(0.01f, Mathf.Clamp01(weaponSwitchLowerRatio) + Mathf.Clamp01(weaponSwitchRaiseRatio));
        return Mathf.Max(0f, weaponSwitchDuration) * (Mathf.Clamp01(weaponSwitchLowerRatio) / totalRatio);
    }

    // Obtiene raise duracion.
    private float GetRaiseDuration()
    {
        float totalRatio = Mathf.Max(0.01f, Mathf.Clamp01(weaponSwitchLowerRatio) + Mathf.Clamp01(weaponSwitchRaiseRatio));
        return Mathf.Max(0f, weaponSwitchDuration) * (Mathf.Clamp01(weaponSwitchRaiseRatio) / totalRatio);
    }

    // Gestiona smooth phase.
    private float SmoothPhase(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    // Recopila direct hijo armas.
    private WeaponScript[] CollectDirectChildWeapons()
    {
        if (weaponCamera == null)
        {
            return Array.Empty<WeaponScript>();
        }

        Transform cameraTransform = weaponCamera.transform;
        List<WeaponScript> directChildWeapons = new List<WeaponScript>(cameraTransform.childCount);

        for (int i = 0; i < cameraTransform.childCount; i++)
        {
            Transform child = cameraTransform.GetChild(i);
            WeaponScript weapon = child.GetComponent<WeaponScript>();

            if (weapon != null)
            {
                directChildWeapons.Add(weapon);
            }
        }

        return directChildWeapons.ToArray();
    }

    // Construye definition maps.
    private void BuildDefinitionMaps()
    {
        definitionsById.Clear();
        weaponIdByWeapon.Clear();

        if (weaponUnlockDefinitions == null)
        {
            weaponUnlockDefinitions = new List<WeaponUnlockDefinition>();
        }

        for (int i = 0; i < weaponUnlockDefinitions.Count; i++)
        {
            WeaponUnlockDefinition definition = weaponUnlockDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            definition.weaponId = NormalizeWeaponId(definition.weaponId);
            if (string.IsNullOrEmpty(definition.weaponId))
            {
                GameDebug.Advertencia("Armas", "WeaponLoadoutScript encontro una definicion de arma sin id.", this);
                continue;
            }

            if (definition.weapon == null)
            {
                definition.weapon = FindWeaponByName(definition.weaponId);
            }

            if (definitionsById.ContainsKey(definition.weaponId))
            {
                GameDebug.Advertencia("Armas", $"Id de arma duplicado '{definition.weaponId}' en WeaponLoadoutScript. Se mantiene la primera ocurrencia.", this);
                continue;
            }

            definitionsById.Add(definition.weaponId, definition);

            if (definition.weapon != null && !weaponIdByWeapon.ContainsKey(definition.weapon))
            {
                weaponIdByWeapon.Add(definition.weapon, definition.weaponId);
            }
        }

        for (int i = 0; i < allWeapons.Length; i++)
        {
            WeaponScript weapon = allWeapons[i];
            if (weapon == null || weaponIdByWeapon.ContainsKey(weapon))
            {
                continue;
            }

            string fallbackId = NormalizeWeaponId(weapon.name);
            if (definitionsById.ContainsKey(fallbackId))
            {
                weaponIdByWeapon[weapon] = fallbackId;
                continue;
            }

            WeaponUnlockDefinition runtimeDefinition = new WeaponUnlockDefinition
            {
                weaponId = fallbackId,
                weapon = weapon,
                price = 0,
                unlockedByDefault = false
            };

            weaponUnlockDefinitions.Add(runtimeDefinition);
            definitionsById.Add(fallbackId, runtimeDefinition);
            weaponIdByWeapon.Add(weapon, fallbackId);
            GameDebug.Advertencia("Armas", $"Se autoregistro el arma '{fallbackId}'. Configurala en Inspector para controlar desbloqueo.", this);
        }
    }

    // Gestiona rebuild desbloqueada armas.
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

    // Resuelve activo indice.
    private int ResolveActiveIndex(WeaponScript[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return -1;
        }

        int activeIndex = -1;
        int activeWeaponCount = 0;

        for (int i = 0; i < pool.Length; i++)
        {
            if (!pool[i].gameObject.activeSelf)
            {
                continue;
            }

            activeWeaponCount++;
            activeIndex = i;
        }

        if (activeWeaponCount == 1)
        {
            return activeIndex;
        }

        return equippedWeaponIndex >= 0 && equippedWeaponIndex < pool.Length ? equippedWeaponIndex : 0;
    }

    // Resuelve arma id.
    private string ResolveWeaponId(WeaponScript weapon)
    {
        if (weapon == null)
        {
            return string.Empty;
        }

        if (weaponIdByWeapon.TryGetValue(weapon, out string mappedId) && !string.IsNullOrEmpty(mappedId))
        {
            return mappedId;
        }

        string fallbackId = NormalizeWeaponId(weapon.name);
        if (!string.IsNullOrEmpty(fallbackId))
        {
            weaponIdByWeapon[weapon] = fallbackId;
        }

        return fallbackId;
    }

    // Resuelve starting arma indice.
    private int ResolveStartingWeaponIndex()
    {
        string preferredId = NormalizeWeaponId(defaultStartingWeaponId);
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

    // Busca desbloqueada arma indice.
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

    // Resuelve respaldo arma id.
    private string ResolveFallbackWeaponId()
    {
        string preferredId = NormalizeWeaponId(defaultStartingWeaponId);
        if (!string.IsNullOrEmpty(preferredId))
        {
            if (definitionsById.ContainsKey(preferredId) || FindWeaponByName(preferredId) != null)
            {
                return preferredId;
            }
        }

        if (allWeapons.Length > 0)
        {
            return ResolveWeaponId(allWeapons[0]);
        }

        return string.Empty;
    }

    // Busca arma por nombre.
    private WeaponScript FindWeaponByName(string weaponName)
    {
        string normalizedName = NormalizeWeaponId(weaponName);
        if (string.IsNullOrEmpty(normalizedName))
        {
            return null;
        }

        for (int i = 0; i < allWeapons.Length; i++)
        {
            WeaponScript weapon = allWeapons[i];
            if (weapon != null && NormalizeWeaponId(weapon.name) == normalizedName)
            {
                return weapon;
            }
        }

        return null;
    }

    // Gestiona normalize arma id.
    private string NormalizeWeaponId(string rawWeaponId)
    {
        return string.IsNullOrWhiteSpace(rawWeaponId) ? string.Empty : rawWeaponId.Trim();
    }

    // Gestiona deactivate todos armas.
    private void DeactivateAllWeapons()
    {
        for (int i = 0; i < allWeapons.Length; i++)
        {
            if (allWeapons[i] != null && allWeapons[i].gameObject.activeSelf)
            {
                allWeapons[i].gameObject.SetActive(false);
            }
        }
    }
}
