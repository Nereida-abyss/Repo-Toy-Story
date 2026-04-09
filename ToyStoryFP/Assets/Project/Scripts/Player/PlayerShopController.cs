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

    private PlayerController playerController;
    private PlayerCurrencyController currencyController;
    private PlayerHealthScript playerHealth;
    private MovementScript movementScript;
    private WeaponLoadoutScript weaponLoadout;
    private WaveManager waveManager;

    private GameObject panelShop;
    private TMP_Text moneyText;
    private TMP_Text speedStatText;
    private TMP_Text jumpStatText;
    private Button ammoButton;
    private Button healButton;
    private Button speedButton;
    private Button jumpButton;
    private Button m16Button;
    private Button akButton;

    private int speedLevel = 1;
    private int jumpLevel = 1;
    private bool buttonListenersBound;

    private bool IsShopOpen => panelShop != null && panelShop.activeSelf;

    void Awake()
    {
        ResolveGameplayReferences();
        ResolveUiReferences();
        ConfigureWeaponShopPrices();
        BindButtonListeners();
        ApplyMovementUpgradeLevels();
        CloseShopImmediate();
        RefreshUi();
    }

    void Update()
    {
        ResolveGameplayReferences();
        ResolveUiReferences();
        ConfigureWeaponShopPrices();
        BindButtonListeners();

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

    void OnDestroy()
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
        ResolveUiReferences();
        ConfigureWeaponShopPrices();

        if (panelShop == null)
        {
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
        waveManager ??= FindFirstObjectByType<WaveManager>();
        return waveManager != null && waveManager.CurrentState == WaveManager.WaveRuntimeState.Intermission;
    }

    private void HandleAmmoPurchase()
    {
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
        ResolveUiReferences();

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
        WeaponScript currentWeapon = weaponLoadout != null ? weaponLoadout.CurrentWeapon : null;
        return currentWeapon != null
            && currentWeapon.IsPlayerOwnedWeapon
            && currentWeapon.UsesFiniteReserve
            && currentWeapon.MissingTotalAmmo > 0;
    }

    private bool CanPurchaseHeal()
    {
        return playerHealth != null && playerHealth.IsAlive && playerHealth.CurrentHealth < playerHealth.MaxHealth;
    }

    private bool CanPurchaseWeapon(string weaponId, int currentCoins)
    {
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
        playerHealth ??= playerController != null ? playerController.Health : GetComponent<PlayerHealthScript>();
        currencyController ??= playerController != null ? playerController.Currency : GetComponent<PlayerCurrencyController>();
        movementScript ??= GetComponent<MovementScript>();
        weaponLoadout ??= playerController != null ? playerController.WeaponLoadout : GetComponentInChildren<WeaponLoadoutScript>(true);
        waveManager ??= FindFirstObjectByType<WaveManager>();
    }

    private void ResolveUiReferences()
    {
        panelShop ??= FindObjectByExactName("PanelShop");
        moneyText ??= FindComponentInChildrenByExactName<TMP_Text>("Money");
        speedStatText ??= FindComponentInChildrenByExactName<TMP_Text>("SpeedStat");
        jumpStatText ??= FindComponentInChildrenByExactName<TMP_Text>("JumpStat");
        ammoButton ??= FindComponentInChildrenByAnyName<Button>("ButtonMunicion", "ButtonMunici\u00F3n");
        healButton ??= FindComponentInChildrenByAnyName<Button>("ButtonCuracion", "ButtonCuraci\u00F3n");
        speedButton ??= FindComponentInChildrenByExactName<Button>("ButtonBoostVelocity");
        jumpButton ??= FindComponentInChildrenByExactName<Button>("ButtonBoostJump");
        m16Button ??= FindComponentInChildrenByExactName<Button>("ButtonM16");
        akButton ??= FindComponentInChildrenByExactName<Button>("ButtonAk");
    }

    private void BindButtonListeners()
    {
        if (buttonListenersBound)
        {
            return;
        }

        ResolveUiReferences();

        if (ammoButton == null
            || healButton == null
            || speedButton == null
            || jumpButton == null
            || m16Button == null
            || akButton == null)
        {
            return;
        }

        ammoButton?.onClick.AddListener(HandleAmmoPurchase);
        healButton?.onClick.AddListener(HandleHealPurchase);
        speedButton?.onClick.AddListener(HandleSpeedPurchase);
        jumpButton?.onClick.AddListener(HandleJumpPurchase);
        m16Button?.onClick.AddListener(HandleM16Purchase);
        akButton?.onClick.AddListener(HandleAkPurchase);
        buttonListenersBound = true;
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

    private GameObject FindObjectByExactName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        Transform[] transforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == targetName)
            {
                return transforms[i].gameObject;
            }
        }

        return null;
    }

    private T FindComponentInChildrenByExactName<T>(string targetName) where T : Component
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        T[] components = GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].gameObject.name == targetName)
            {
                return components[i];
            }
        }

        return null;
    }

    private T FindComponentInChildrenByAnyName<T>(params string[] targetNames) where T : Component
    {
        if (targetNames == null || targetNames.Length == 0)
        {
            return null;
        }

        T[] components = GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];

            if (component == null)
            {
                continue;
            }

            for (int nameIndex = 0; nameIndex < targetNames.Length; nameIndex++)
            {
                if (component.gameObject.name == targetNames[nameIndex])
                {
                    return component;
                }
            }
        }

        return null;
    }
}
