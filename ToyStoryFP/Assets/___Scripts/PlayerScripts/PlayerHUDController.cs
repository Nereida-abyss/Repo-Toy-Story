using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHUDController : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private float healthAnimationSpeed = 2.5f;

    [Header("Ammo UI")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text reloadText;

    [Header("Currency UI")]
    [SerializeField] private TMP_Text coinsText;

    private PlayerHealthScript playerHealth;
    private PlayerCurrencyController playerCurrency;
    private WeaponLoadoutScript weaponLoadout;
    private WeaponScript observedWeapon;

    private float displayedHealthNormalized = 1f;
    private bool loggedMissingUiReferences;

    void Awake()
    {
        ResolveReferences();
        ResolveUiReferences();
        WarnIfUiReferencesAreMissing();
        BindEvents();
        RefreshAllImmediate();
    }

    void OnEnable()
    {
        ResolveReferences();
        BindEvents();
        RefreshAllImmediate();
    }

    void OnDisable()
    {
        UnbindEvents();
    }

    void OnValidate()
    {
        ResolveUiReferences();
    }

    void Update()
    {
        UpdateHealthAnimation();
        RefreshAmmo();
    }

    private void ResolveReferences()
    {
        playerHealth = GetComponentInParent<PlayerHealthScript>();
        PlayerController playerController = GetComponentInParent<PlayerController>();

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerController != null)
        {
            weaponLoadout = playerController.GetComponentInChildren<WeaponLoadoutScript>(true);
            playerCurrency = playerController.Currency ?? playerController.GetComponent<PlayerCurrencyController>();
        }
    }

    private void ResolveUiReferences()
    {
        if (healthFillImage == null)
        {
            healthFillImage = FindComponentInChildrenByName<Image>("HealthBarFill");
        }

        if (healthText == null)
        {
            healthText = FindComponentInChildrenByName<TMP_Text>("HealthText");
        }

        if (ammoText == null)
        {
            ammoText = FindComponentInChildrenByName<TMP_Text>("AmmoText");
        }

        if (reloadText == null)
        {
            reloadText = FindComponentInChildrenByName<TMP_Text>("ReloadText");
        }

        if (coinsText == null)
        {
            coinsText = FindComponentInChildrenByName<TMP_Text>("CoinsText");
        }
    }

    private void WarnIfUiReferencesAreMissing()
    {
        if (loggedMissingUiReferences)
        {
            return;
        }

        string missingReferences = string.Empty;

        if (healthFillImage == null)
        {
            missingReferences += "HealthRoot/HealthBarBackground/HealthBarFill, ";
        }

        if (healthText == null)
        {
            missingReferences += "HealthRoot/HealthText, ";
        }

        if (ammoText == null)
        {
            missingReferences += "AmmoText, ";
        }

        if (reloadText == null)
        {
            missingReferences += "ReloadText, ";
        }

        if (coinsText == null)
        {
            missingReferences += "CoinsText, ";
        }

        if (string.IsNullOrEmpty(missingReferences))
        {
            return;
        }

        loggedMissingUiReferences = true;
        missingReferences = missingReferences.TrimEnd(' ', ',');
        Debug.LogWarning($"PlayerHUDController is missing HUD references under {name}: {missingReferences}", this);
    }

    private void BindEvents()
    {
        UnbindEvents();

        if (playerHealth != null)
        {
            playerHealth.HealthChanged += HandleHealthChanged;
        }

        if (playerCurrency != null)
        {
            playerCurrency.CoinsChanged += HandleCoinsChanged;
        }

        if (weaponLoadout != null)
        {
            weaponLoadout.CurrentWeaponChanged += HandleCurrentWeaponChanged;
            observedWeapon = weaponLoadout.CurrentWeapon;

            if (observedWeapon != null)
            {
                observedWeapon.StateChanged += HandleWeaponStateChanged;
            }
        }
    }

    private void UnbindEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
        }

        if (playerCurrency != null)
        {
            playerCurrency.CoinsChanged -= HandleCoinsChanged;
        }

        if (weaponLoadout != null)
        {
            weaponLoadout.CurrentWeaponChanged -= HandleCurrentWeaponChanged;
        }

        if (observedWeapon != null)
        {
            observedWeapon.StateChanged -= HandleWeaponStateChanged;
            observedWeapon = null;
        }
    }

    private void HandleHealthChanged(PlayerHealthScript health)
    {
        RefreshHealthText();
    }

    private void HandleCurrentWeaponChanged(WeaponScript newWeapon)
    {
        if (observedWeapon != null)
        {
            observedWeapon.StateChanged -= HandleWeaponStateChanged;
        }

        observedWeapon = newWeapon;

        if (observedWeapon != null)
        {
            observedWeapon.StateChanged += HandleWeaponStateChanged;
        }

        RefreshAmmo();
    }

    private void HandleCoinsChanged(PlayerCurrencyController currency)
    {
        RefreshCoins();
    }

    private void HandleWeaponStateChanged(WeaponScript weapon)
    {
        RefreshAmmo();
    }

    private void RefreshAllImmediate()
    {
        displayedHealthNormalized = playerHealth != null ? playerHealth.HealthNormalized : 1f;

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = displayedHealthNormalized;
        }

        RefreshHealthText();
        RefreshAmmo();
        RefreshCoins();
    }

    private void UpdateHealthAnimation()
    {
        if (playerHealth == null || healthFillImage == null)
        {
            return;
        }

        displayedHealthNormalized = Mathf.MoveTowards(
            displayedHealthNormalized,
            playerHealth.HealthNormalized,
            healthAnimationSpeed * Time.deltaTime);

        healthFillImage.fillAmount = displayedHealthNormalized;
    }

    private void RefreshHealthText()
    {
        if (healthText == null)
        {
            return;
        }

        if (playerHealth == null)
        {
            healthText.text = "HP --";
            return;
        }

        healthText.text = $"HP {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";
    }

    private void RefreshAmmo()
    {
        if (ammoText == null || reloadText == null)
        {
            return;
        }

        if (weaponLoadout == null)
        {
            ammoText.text = "-- / --";
            reloadText.text = string.Empty;
            return;
        }

        if (weaponLoadout.IsSwitchingWeapon)
        {
            ammoText.text = "-- / --";
            reloadText.text = "EQUIPPING";
            return;
        }

        WeaponScript currentWeapon = weaponLoadout.CurrentWeapon;

        if (currentWeapon == null || !currentWeapon.IsPlayerOwnedWeapon)
        {
            ammoText.text = "-- / --";
            reloadText.text = string.Empty;
            return;
        }

        string reserveText = currentWeapon.HasInfiniteReserve
            ? "INF"
            : currentWeapon.ReserveAmmo.ToString();

        ammoText.text = $"{currentWeapon.CurrentAmmoInMagazine} / {reserveText}";
        reloadText.text = currentWeapon.IsReloading ? "RELOADING" : string.Empty;
    }

    private void RefreshCoins()
    {
        if (coinsText == null)
        {
            return;
        }

        int currentCoins = playerCurrency != null ? playerCurrency.CurrentCoins : 0;
        coinsText.text = $"COINS {currentCoins}";
    }

    private T FindComponentInChildrenByName<T>(string targetName) where T : Component
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
}
