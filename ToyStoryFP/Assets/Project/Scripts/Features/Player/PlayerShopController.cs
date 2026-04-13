using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerShopController : MonoBehaviour
{
    private const int LegacySharedUpgradePrice = 10;
    private const int LegacyAmmoPurchaseMagazineCount = 1;
    private const float LegacyUpgradeStepMultiplier = 0.1f;
    private const float LegacyHealFraction = 0.5f;
    private const float AvailableButtonAlpha = 1f;
    private const float UnaffordableButtonAlpha = 0.6f;
    private const float DisabledButtonAlpha = 0.4f;
    private const string NotEnoughCoinsFailReason = "Not enough coins.";
    private const string M16WeaponId = "TacticalRifle";
    private const string AkWeaponId = "AssaultRifle";

    public static bool IsInputBlocked { get; private set; }

    [Header("Scene References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private ShopBalanceProfile shopBalanceProfile;

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
    private readonly Dictionary<Button, CanvasGroup> buttonCanvasGroups = new Dictionary<Button, CanvasGroup>();

    private bool IsShopOpen => panelShop != null && panelShop.activeSelf;

    private void Awake()
    {
        WarnIfMissingShopBalanceProfile();
        ResolveGameplayReferences();
        BindButtonListeners();
        ApplyMovementUpgradeLevels();
        CloseShopImmediate();
        RefreshUi();
        WarnIfReferencesAreMissing();
    }

    private void OnEnable()
    {
        WarnIfMissingShopBalanceProfile();
        ResolveGameplayReferences();
        BindButtonListeners();
        RefreshUi();
        WarnIfReferencesAreMissing();
    }

    private void Update()
    {
        if (IsShopOpen)
        {
            RefreshUi();
        }

        if (UIManager.IsGamePaused)
        {
            if (IsShopOpen)
            {
                CloseShop();
            }

            return;
        }

        if (!IsIntermissionActive())
        {
            if (IsShopOpen)
            {
                CloseShop();
            }

            return;
        }

        if (IsShopOpen)
        {
            if (ProjectInput.WasUiBackPressed() || ProjectInput.WasShopTogglePressed())
            {
                CloseShop();
            }

            return;
        }

        if (ProjectInput.WasShopTogglePressed())
        {
            ToggleShop();
        }
    }

    private void OnDestroy()
    {
        UnbindButtonListeners();

        if (IsInputBlocked)
        {
            IsInputBlocked = false;
        }
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
        PlayShopMusic();
        IsInputBlocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
            RestoreGameplayMusic();
        }

        IsInputBlocked = false;

        if (!UIManager.IsGamePaused)
        {
            RestoreGameplayCursor();
        }

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

        IsInputBlocked = false;
    }

    private void RestoreGameplayCursor()
    {
        if (MouseLookScript.instance != null)
        {
            MouseLookScript.instance.LockCursor();
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        if (!currencyController.TrySpendCoins(GetSharedUpgradePrice()))
        {
            PlayShopPurchaseFailedAudio();
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

        if (!currencyController.TrySpendCoins(GetSharedUpgradePrice()))
        {
            PlayShopPurchaseFailedAudio();
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

        if (!currencyController.TrySpendCoins(GetSharedUpgradePrice()))
        {
            PlayShopPurchaseFailedAudio();
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

        if (!currencyController.TrySpendCoins(GetSharedUpgradePrice()))
        {
            PlayShopPurchaseFailedAudio();
            return;
        }

        jumpLevel++;
        ApplyMovementUpgradeLevels();
        PlayShopPurchaseSuccessAudio();
        RefreshUi();
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
        return Mathf.Max(0, shopBalanceProfile != null ? shopBalanceProfile.SharedUpgradePrice : LegacySharedUpgradePrice);
    }

    private int GetAmmoPurchaseMagazineCount()
    {
        return Mathf.Max(1, shopBalanceProfile != null ? shopBalanceProfile.AmmoPurchaseMagazineCount : LegacyAmmoPurchaseMagazineCount);
    }

    private float GetUpgradeStepMultiplier()
    {
        return Mathf.Max(0f, shopBalanceProfile != null ? shopBalanceProfile.UpgradeStepMultiplier : LegacyUpgradeStepMultiplier);
    }

    private float GetHealFraction()
    {
        return Mathf.Max(0f, shopBalanceProfile != null ? shopBalanceProfile.HealFraction : LegacyHealFraction);
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
        GameDebug.Advertencia("Shop", "PlayerShopController no tiene ShopBalanceProfile asignado. Se usaran los valores locales legacy.", this);
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
        AudioManager.Instance?.PlayShopMusic();
    }

    private void RestoreGameplayMusic()
    {
        AudioManager.Instance?.RestoreGameplayMusic();
    }

    private void ResolveGameplayReferences()
    {
        playerController ??= GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealthScript>();
        currencyController = GetComponent<PlayerCurrencyController>();
        movementScript = GetComponent<MovementScript>();
        weaponLoadout = GetComponentInChildren<WeaponLoadoutScript>(true);

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
        PlayUiAudio(AudioManager.Instance != null ? AudioManager.Instance.GetUiShopPurchaseSuccessClip() : null, AudioManager.Instance != null ? AudioManager.Instance.GetUiShopPurchaseSuccessVolume() : 0f);
    }

    private void PlayShopPurchaseFailedAudio()
    {
        PlayUiAudio(AudioManager.Instance != null ? AudioManager.Instance.GetUiShopPurchaseFailedClip() : null, AudioManager.Instance != null ? AudioManager.Instance.GetUiShopPurchaseFailedVolume() : 0f);
    }

    private void PlayUiAudio(AudioClip clip, float volume)
    {
        AudioSource sharedSfxSource = AudioManager.Instance != null ? AudioManager.Instance.SharedSfxSource : null;

        if (sharedSfxSource == null || clip == null || volume <= 0f)
        {
            return;
        }

        sharedSfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
