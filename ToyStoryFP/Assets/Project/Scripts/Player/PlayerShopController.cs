using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerShopController : MonoBehaviour
{
    private const int SharedUpgradePrice = 10;
    private const int M16Price = 300;
    private const int AkPrice = 50;
    private const int AmmoPurchaseMagazineCount = 1;
    private const float UpgradeStepMultiplier = 0.1f;
    private const float HealFraction = 0.5f;
    private const string M16WeaponId = "TacticalRifle";
    private const string AkWeaponId = "AssaultRifle";

    public static bool IsInputBlocked { get; private set; }

    [Header("Scene References")]
    [SerializeField] private WaveManager waveManager;

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

    private bool IsShopOpen => panelShop != null && panelShop.activeSelf;

    private void Awake()
    {
        ResolveGameplayReferences();
        ConfigureWeaponShopPrices();
        BindButtonListeners();
        ApplyMovementUpgradeLevels();
        CloseShopImmediate();
        RefreshUi();
        WarnIfReferencesAreMissing();
    }

    private void OnEnable()
    {
        ResolveGameplayReferences();
        ConfigureWeaponShopPrices();
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

        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleShop();
            return;
        }

        if (IsShopOpen && Input.GetKeyDown(KeyCode.Q))
        {
            CloseShop();
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

    private void OpenShop()
    {
        ResolveGameplayReferences();
        ConfigureWeaponShopPrices();

        if (panelShop == null)
        {
            WarnIfReferencesAreMissing();
            return;
        }

        UIFxUtility.SetPanelActive(panelShop, true);
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
        if (panelShop != null)
        {
            UIFxUtility.SetPanelActive(panelShop, false);
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

        if (!currencyController.TrySpendCoins(SharedUpgradePrice))
        {
            return;
        }

        if (weaponLoadout.AddAmmoToCurrentWeaponByMagazines(AmmoPurchaseMagazineCount))
        {
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

        if (!currencyController.TrySpendCoins(SharedUpgradePrice))
        {
            return;
        }

        int healAmount = Mathf.CeilToInt(playerHealth.MaxHealth * HealFraction);
        playerHealth.Heal(healAmount);
        RefreshUi();
    }

    private void HandleSpeedPurchase()
    {
        ResolveGameplayReferences();

        if (currencyController == null || movementScript == null)
        {
            return;
        }

        if (!currencyController.TrySpendCoins(SharedUpgradePrice))
        {
            return;
        }

        speedLevel++;
        ApplyMovementUpgradeLevels();
        RefreshUi();
    }

    private void HandleJumpPurchase()
    {
        ResolveGameplayReferences();

        if (currencyController == null || movementScript == null)
        {
            return;
        }

        if (!currencyController.TrySpendCoins(SharedUpgradePrice))
        {
            return;
        }

        jumpLevel++;
        ApplyMovementUpgradeLevels();
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
        ConfigureWeaponShopPrices();

        if (currencyController == null || weaponLoadout == null)
        {
            return;
        }

        weaponLoadout.TryPurchaseWeapon(weaponId, currencyController, true, out _);
        RefreshUi();
    }

    private void ApplyMovementUpgradeLevels()
    {
        if (movementScript == null)
        {
            return;
        }

        movementScript.ApplyShopUpgradeLevels(speedLevel, jumpLevel, UpgradeStepMultiplier);
    }

    private void ConfigureWeaponShopPrices()
    {
        if (weaponLoadout == null)
        {
            return;
        }

        weaponLoadout.SetWeaponShopPrice(M16WeaponId, M16Price);
        weaponLoadout.SetWeaponShopPrice(AkWeaponId, AkPrice);
    }

    private void RefreshUi()
    {
        ResolveGameplayReferences();
        ConfigureWeaponShopPrices();

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

        SetButtonInteractable(speedButton, currentCoins >= SharedUpgradePrice);
        SetButtonInteractable(jumpButton, currentCoins >= SharedUpgradePrice);
        SetButtonInteractable(ammoButton, currentCoins >= SharedUpgradePrice && CanPurchaseAmmo());
        SetButtonInteractable(healButton, currentCoins >= SharedUpgradePrice && CanPurchaseHeal());
        SetButtonInteractable(m16Button, CanPurchaseWeapon(M16WeaponId, currentCoins));
        SetButtonInteractable(akButton, CanPurchaseWeapon(AkWeaponId, currentCoins));
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

    private bool CanPurchaseWeapon(string weaponId, int currentCoins)
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

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = interactable;
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
}
