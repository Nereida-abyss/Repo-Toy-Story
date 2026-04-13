using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerShopController : MonoBehaviour
{
    private const string ShopInputGateOwner = "PlayerShopController.Shop";
    private const float AvailableButtonAlpha = 1f;
    private const float UnaffordableButtonAlpha = 0.6f;
    private const float DisabledButtonAlpha = 0.4f;
    private const string NotEnoughCoinsFailReason = "Not enough coins.";
    private const string M16WeaponId = "TacticalRifle";
    private const string AkWeaponId = "AssaultRifle";

    public static bool IsInputBlocked => GameplayInputGate.IsBlocked;

    [Header("Scene References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private ShopBalanceProfile shopBalanceProfile;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private UiAudioProfile uiAudioProfile;
    [SerializeField] private AudioSource uiAudioSource;

    [Header("Shop UI")]
    [SerializeField] private GameObject panelShop;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text speedStatText;
    [SerializeField] private TMP_Text jumpStatText;
    [SerializeField] private Button ammoButton;
    [SerializeField] private Button healButton;
    [SerializeField] private Button speedButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button m16Button;
    [SerializeField] private Button akButton;

    private PlayerController playerController;
    private PlayerCurrencyController currencyController;
    private PlayerHealthScript playerHealth;
    private MovementScript movementScript;
    private WeaponLoadoutScript weaponLoadout;

    private int speedLevel = 1;
    private int jumpLevel = 1;
    private bool buttonListenersBound;
    private bool hasLoggedMissingReferences;
    private bool hasLoggedMissingShopBalanceProfile;
    private bool hasAcquiredInputGate;
    private static ShopBalanceProfile runtimeFallbackShopBalanceProfile;
    private readonly Dictionary<Button, CanvasGroup> buttonCanvasGroups = new Dictionary<Button, CanvasGroup>();

    private bool IsShopOpen => panelShop != null && panelShop.activeSelf;

    private void Awake()
    {
        InitializeShopController();
    }

    private void OnEnable()
    {
        RefreshShopSetup();
    }

    private void Update()
    {
        RefreshOpenShopUi();

        if (ShouldForceShopClosed())
        {
            CloseShopIfNeeded();
            return;
        }

        HandleShopToggleInput();
    }

    private void OnDestroy()
    {
        UnbindButtonListeners();
        ReleaseShopInputGateIfNeeded();
    }

    private void ToggleShop()
    {
        if (IsShopOpen)
        {
            CloseShop();
            return;
        }

        OpenShop();
    }

    private void InitializeShopController()
    {
        RefreshShopSetup();
        ApplyMovementUpgradeLevels();
        CloseShopImmediate();
    }

    private void RefreshShopSetup()
    {
        WarnIfMissingShopBalanceProfile();
        ResolveGameplayReferences();
        BindButtonListeners();
        RefreshUi();
        WarnIfReferencesAreMissing();
    }

    public void CloseShopFromButton()
    {
        CloseShop();
    }

    private void OpenShop()
    {
        ResolveGameplayReferences();

        if (panelShop == null)
        {
            WarnIfReferencesAreMissing();
            return;
        }

        UIFxUtility.SetPanelActive(panelShop, true);
        BeginShopSession();
        RefreshUi();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(speedButton != null ? speedButton.gameObject : null);
        }
    }

    private void CloseShop()
    {
        bool wasShopOpen = IsShopOpen;

        if (panelShop != null)
        {
            UIFxUtility.SetPanelActive(panelShop, false);
        }

        if (wasShopOpen)
        {
            ProjectInput.ConsumePauseToggleForCurrentFrame();
            EndShopAudioSession();
        }

        EndShopInputSession();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void CloseShopImmediate()
    {
        if (panelShop != null && panelShop.activeSelf)
        {
            panelShop.SetActive(false);
        }

        ReleaseShopInputGateIfNeeded();
    }

    private void RefreshOpenShopUi()
    {
        if (IsShopOpen)
        {
            RefreshUi();
        }
    }

    private bool ShouldForceShopClosed()
    {
        return UIManager.IsGamePaused || !IsIntermissionActive();
    }

    private void CloseShopIfNeeded()
    {
        if (IsShopOpen)
        {
            CloseShop();
        }
    }

    private void HandleShopToggleInput()
    {
        if (IsShopOpen)
        {
            HandleOpenShopInput();
            return;
        }

        if (ProjectInput.WasShopTogglePressed())
        {
            ToggleShop();
        }
    }

    private void HandleOpenShopInput()
    {
        if (ProjectInput.WasUiBackPressed() || ProjectInput.WasShopTogglePressed())
        {
            CloseShop();
        }
    }

    private void BeginShopSession()
    {
        PlayShopMusic();
        AcquireShopInputGateIfNeeded();
    }

    private void EndShopAudioSession()
    {
        RestoreGameplayMusic();
    }

    private void EndShopInputSession()
    {
        ReleaseShopInputGateIfNeeded();
    }

    private bool IsIntermissionActive()
    {
        return waveManager != null && waveManager.CurrentState == WaveManager.WaveRuntimeState.Intermission;
    }

    private void HandleAmmoPurchase()
    {
        ResolveGameplayReferences();

        if (currencyController == null || weaponLoadout == null)
        {
            return;
        }

        if (!CanPurchaseAmmo())
        {
            return;
        }

        if (!TrySpendSharedUpgradePrice())
        {
            return;
        }

        if (weaponLoadout.AddAmmoToCurrentWeaponByMagazines(GetAmmoPurchaseMagazineCount()))
        {
            PlayShopPurchaseSuccessAudio();
            RefreshUi();
        }
    }

    private void HandleHealPurchase()
    {
        ResolveGameplayReferences();

        if (currencyController == null || playerHealth == null)
        {
            return;
        }

        if (!CanPurchaseHeal())
        {
            return;
        }

        if (!TrySpendSharedUpgradePrice())
        {
            return;
        }

        int healAmount = Mathf.CeilToInt(playerHealth.MaxHealth * GetHealFraction());
        playerHealth.Heal(healAmount);
        PlayShopPurchaseSuccessAudio();
        RefreshUi();
    }

    private void HandleSpeedPurchase()
    {
        ResolveGameplayReferences();

        if (currencyController == null || movementScript == null)
        {
            return;
        }

        if (!TrySpendSharedUpgradePrice())
        {
            return;
        }

        speedLevel++;
        ApplyMovementUpgradeLevels();
        PlayShopPurchaseSuccessAudio();
        RefreshUi();
    }

    private void HandleJumpPurchase()
    {
        ResolveGameplayReferences();

        if (currencyController == null || movementScript == null)
        {
            return;
        }

        if (!TrySpendSharedUpgradePrice())
        {
            return;
        }

        jumpLevel++;
        ApplyMovementUpgradeLevels();
        PlayShopPurchaseSuccessAudio();
        RefreshUi();
    }

    private bool TrySpendSharedUpgradePrice()
    {
        if (currencyController == null)
        {
            return false;
        }

        if (currencyController.TrySpendCoins(GetSharedUpgradePrice()))
        {
            return true;
        }

        PlayShopPurchaseFailedAudio();
        return false;
    }

    private void HandleM16Purchase()
    {
        TryPurchaseWeapon(M16WeaponId);
    }

    private void HandleAkPurchase()
    {
        TryPurchaseWeapon(AkWeaponId);
    }

    private void TryPurchaseWeapon(string weaponId)
    {
        ResolveGameplayReferences();

        if (currencyController == null || weaponLoadout == null)
        {
            return;
        }

        bool wasUnlocked = weaponLoadout.IsWeaponUnlocked(weaponId);
        bool purchaseSucceeded = weaponLoadout.TryPurchaseWeapon(weaponId, currencyController, true, out string failReason);

        if (!purchaseSucceeded)
        {
            if (failReason == NotEnoughCoinsFailReason)
            {
                PlayShopPurchaseFailedAudio();
            }

            RefreshUi();
            return;
        }

        if (!wasUnlocked)
        {
            PlayShopPurchaseSuccessAudio();
        }

        RefreshUi();
    }

    private void ApplyMovementUpgradeLevels()
    {
        if (movementScript == null)
        {
            return;
        }

        movementScript.ApplyShopUpgradeLevels(speedLevel, jumpLevel, GetUpgradeStepMultiplier());
    }

    private void RefreshUi()
    {
        ResolveGameplayReferences();

        if (moneyText != null)
        {
            int currentCoins = currencyController != null ? currencyController.CurrentCoins : 0;
            moneyText.text = $"${currentCoins}";
        }

        if (speedStatText != null)
        {
            speedStatText.text = $"SPEED: lvl {speedLevel}";
        }

        if (jumpStatText != null)
        {
            jumpStatText.text = $"JUMP: lvl {jumpLevel}";
        }

        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        int currentCoins = currencyController != null ? currencyController.CurrentCoins : 0;
        int sharedUpgradePrice = GetSharedUpgradePrice();
        bool canPurchaseAmmo = CanPurchaseAmmo();
        bool canPurchaseHeal = CanPurchaseHeal();

        SetButtonPurchaseState(speedButton, true, currentCoins >= sharedUpgradePrice);
        SetButtonPurchaseState(jumpButton, true, currentCoins >= sharedUpgradePrice);
        SetButtonPurchaseState(ammoButton, canPurchaseAmmo, currentCoins >= sharedUpgradePrice);
        SetButtonPurchaseState(healButton, canPurchaseHeal, currentCoins >= sharedUpgradePrice);
        SetButtonPurchaseState(m16Button, CanAttemptWeaponPurchase(M16WeaponId), CanAffordWeapon(M16WeaponId, currentCoins));
        SetButtonPurchaseState(akButton, CanAttemptWeaponPurchase(AkWeaponId), CanAffordWeapon(AkWeaponId, currentCoins));
    }

    private int GetSharedUpgradePrice()
    {
        return Mathf.Max(0, ResolveShopBalanceProfile().SharedUpgradePrice);
    }

    private int GetAmmoPurchaseMagazineCount()
    {
        return Mathf.Max(1, ResolveShopBalanceProfile().AmmoPurchaseMagazineCount);
    }

    private float GetUpgradeStepMultiplier()
    {
        return Mathf.Max(0f, ResolveShopBalanceProfile().UpgradeStepMultiplier);
    }

    private float GetHealFraction()
    {
        return Mathf.Max(0f, ResolveShopBalanceProfile().HealFraction);
    }

    private void WarnIfMissingShopBalanceProfile()
    {
        if (shopBalanceProfile != null)
        {
            hasLoggedMissingShopBalanceProfile = false;
            return;
        }

        if (hasLoggedMissingShopBalanceProfile)
        {
            return;
        }

        hasLoggedMissingShopBalanceProfile = true;
        GameDebug.Advertencia("Shop", "PlayerShopController no tiene ShopBalanceProfile asignado. Se usara un perfil temporal de seguridad hasta asignar uno en escena.", this);
    }

    private bool CanPurchaseAmmo()
    {
        ResolveGameplayReferences();

        WeaponScript currentWeapon = weaponLoadout != null ? weaponLoadout.CurrentWeapon : null;
        return currentWeapon != null
            && currentWeapon.IsPlayerOwnedWeapon
            && currentWeapon.UsesFiniteReserve
            && currentWeapon.MissingTotalAmmo > 0;
    }

    private bool CanPurchaseHeal()
    {
        ResolveGameplayReferences();
        return playerHealth != null && playerHealth.IsAlive && playerHealth.CurrentHealth < playerHealth.MaxHealth;
    }

    private bool CanAttemptWeaponPurchase(string weaponId)
    {
        ResolveGameplayReferences();

        if (weaponLoadout == null)
        {
            return false;
        }

        if (!weaponLoadout.TryGetShopEntry(weaponId, out WeaponLoadoutScript.WeaponShopEntry entry))
        {
            return false;
        }

        return !entry.IsUnlocked;
    }

    private bool CanAffordWeapon(string weaponId, int currentCoins)
    {
        ResolveGameplayReferences();

        if (weaponLoadout == null)
        {
            return false;
        }

        if (!weaponLoadout.TryGetShopEntry(weaponId, out WeaponLoadoutScript.WeaponShopEntry entry))
        {
            return false;
        }

        return !entry.IsUnlocked && currentCoins >= entry.Price;
    }

    private void SetButtonPurchaseState(Button button, bool canAttemptPurchase, bool canAffordPurchase)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = canAttemptPurchase;
        SetButtonVisualAlpha(button, ResolveButtonAlpha(canAttemptPurchase, canAffordPurchase));
    }

    private void SetButtonVisualAlpha(Button button, float alpha)
    {
        CanvasGroup group = ResolveButtonCanvasGroup(button);

        if (group == null)
        {
            return;
        }

        group.alpha = Mathf.Clamp01(alpha);
    }

    private CanvasGroup ResolveButtonCanvasGroup(Button button)
    {
        if (button == null)
        {
            return null;
        }

        if (buttonCanvasGroups.TryGetValue(button, out CanvasGroup cachedGroup) && cachedGroup != null)
        {
            return cachedGroup;
        }

        CanvasGroup group = button.GetComponent<CanvasGroup>();

        if (group == null)
        {
            group = button.gameObject.AddComponent<CanvasGroup>();
        }

        buttonCanvasGroups[button] = group;
        return group;
    }

    private float ResolveButtonAlpha(bool canAttemptPurchase, bool canAffordPurchase)
    {
        if (!canAttemptPurchase)
        {
            return DisabledButtonAlpha;
        }

        return canAffordPurchase ? AvailableButtonAlpha : UnaffordableButtonAlpha;
    }

    private void PlayShopMusic()
    {
        ResolveAudioManager()?.PlayShopMusic();
    }

    private void RestoreGameplayMusic()
    {
        ResolveAudioManager()?.RestoreGameplayMusic();
    }

    private void ResolveGameplayReferences()
    {
        playerController ??= GetComponent<PlayerController>();
        playerHealth ??= GetComponent<PlayerHealthScript>();
        currencyController ??= GetComponent<PlayerCurrencyController>();
        movementScript ??= GetComponent<MovementScript>();
        weaponLoadout ??= GetComponentInChildren<WeaponLoadoutScript>(true);

        if (playerHealth == null && playerController != null)
        {
            playerHealth = playerController.Health;
        }

        if (currencyController == null && playerController != null)
        {
            currencyController = playerController.Currency;
        }

        if (weaponLoadout == null && playerController != null)
        {
            weaponLoadout = playerController.WeaponLoadout;
        }
    }

    private void BindButtonListeners()
    {
        if (buttonListenersBound)
        {
            return;
        }

        BindButtonListener(ammoButton, HandleAmmoPurchase);
        BindButtonListener(healButton, HandleHealPurchase);
        BindButtonListener(speedButton, HandleSpeedPurchase);
        BindButtonListener(jumpButton, HandleJumpPurchase);
        BindButtonListener(m16Button, HandleM16Purchase);
        BindButtonListener(akButton, HandleAkPurchase);
        buttonListenersBound = true;
    }

    private void BindButtonListener(Button button, UnityEngine.Events.UnityAction listener)
    {
        if (button == null || listener == null)
        {
            return;
        }

        button.onClick.RemoveListener(listener);
        button.onClick.AddListener(listener);
    }

    private void UnbindButtonListeners()
    {
        if (!buttonListenersBound)
        {
            return;
        }

        ammoButton?.onClick.RemoveListener(HandleAmmoPurchase);
        healButton?.onClick.RemoveListener(HandleHealPurchase);
        speedButton?.onClick.RemoveListener(HandleSpeedPurchase);
        jumpButton?.onClick.RemoveListener(HandleJumpPurchase);
        m16Button?.onClick.RemoveListener(HandleM16Purchase);
        akButton?.onClick.RemoveListener(HandleAkPurchase);
        buttonListenersBound = false;
    }

    private void WarnIfReferencesAreMissing()
    {
        if (hasLoggedMissingReferences)
        {
            return;
        }

        string missing = string.Empty;

        if (waveManager == null)
        {
            missing += "WaveManager, ";
        }

        if (currencyController == null)
        {
            missing += "PlayerCurrencyController, ";
        }

        if (playerHealth == null)
        {
            missing += "PlayerHealthScript, ";
        }

        if (movementScript == null)
        {
            missing += "MovementScript, ";
        }

        if (weaponLoadout == null)
        {
            missing += "WeaponLoadoutScript, ";
        }

        if (panelShop == null)
        {
            missing += "PanelShop, ";
        }

        if (moneyText == null)
        {
            missing += "MoneyText, ";
        }

        if (speedStatText == null)
        {
            missing += "SpeedStatText, ";
        }

        if (jumpStatText == null)
        {
            missing += "JumpStatText, ";
        }

        if (ammoButton == null)
        {
            missing += "AmmoButton, ";
        }

        if (healButton == null)
        {
            missing += "HealButton, ";
        }

        if (speedButton == null)
        {
            missing += "SpeedButton, ";
        }

        if (jumpButton == null)
        {
            missing += "JumpButton, ";
        }

        if (m16Button == null)
        {
            missing += "M16Button, ";
        }

        if (akButton == null)
        {
            missing += "AkButton, ";
        }

        if (string.IsNullOrEmpty(missing))
        {
            return;
        }

        hasLoggedMissingReferences = true;
        missing = missing.TrimEnd(' ', ',');
        GameDebug.Advertencia("Shop", $"PlayerShopController tiene referencias sin asignar: {missing}", this);
    }

    private void PlayShopPurchaseSuccessAudio()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        AudioClip clip = profile != null ? profile.ShopPurchaseSuccessClip : ResolveAudioManager()?.GetUiShopPurchaseSuccessClip();
        float volume = profile != null ? profile.ShopPurchaseSuccessVolume : (ResolveAudioManager() != null ? ResolveAudioManager().GetUiShopPurchaseSuccessVolume() : 0f);
        PlayUiAudio(clip, volume);
    }

    private void PlayShopPurchaseFailedAudio()
    {
        UiAudioProfile profile = ResolveUiAudioProfile();
        AudioClip clip = profile != null ? profile.ShopPurchaseFailedClip : ResolveAudioManager()?.GetUiShopPurchaseFailedClip();
        float volume = profile != null ? profile.ShopPurchaseFailedVolume : (ResolveAudioManager() != null ? ResolveAudioManager().GetUiShopPurchaseFailedVolume() : 0f);
        PlayUiAudio(clip, volume);
    }

    private void PlayUiAudio(AudioClip clip, float volume)
    {
        AudioSource sharedSfxSource = ResolveUiAudioSource();

        if (sharedSfxSource == null || clip == null || volume <= 0f)
        {
            return;
        }

        sharedSfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private AudioManager ResolveAudioManager()
    {
        if (audioManager != null)
        {
            return audioManager;
        }

        audioManager = AudioManager.Instance;
        return audioManager;
    }

    private UiAudioProfile ResolveUiAudioProfile()
    {
        return uiAudioProfile;
    }

    private ShopBalanceProfile ResolveShopBalanceProfile()
    {
        if (shopBalanceProfile != null)
        {
            return shopBalanceProfile;
        }

        runtimeFallbackShopBalanceProfile ??= ScriptableObject.CreateInstance<ShopBalanceProfile>();
        return runtimeFallbackShopBalanceProfile;
    }

    private AudioSource ResolveUiAudioSource()
    {
        if (uiAudioSource != null)
        {
            return uiAudioSource;
        }

        AudioManager resolvedAudioManager = ResolveAudioManager();
        return resolvedAudioManager != null ? resolvedAudioManager.SharedSfxSource : null;
    }

    private void AcquireShopInputGateIfNeeded()
    {
        if (hasAcquiredInputGate)
        {
            return;
        }

        GameplayInputGate.Acquire(ShopInputGateOwner);
        hasAcquiredInputGate = true;
    }

    private void ReleaseShopInputGateIfNeeded()
    {
        if (!hasAcquiredInputGate)
        {
            return;
        }

        GameplayInputGate.Release(ShopInputGateOwner);
        hasAcquiredInputGate = false;
    }
}
