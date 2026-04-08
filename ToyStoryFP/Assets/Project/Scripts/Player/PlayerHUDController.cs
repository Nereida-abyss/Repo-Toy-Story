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

    [Header("Damage Feedback")]
    [SerializeField] private Image damageFlashImage;
    [SerializeField] private Color damageFlashColor = new Color(0.95f, 0.08f, 0.08f, 0.38f);
    [SerializeField] private float damageFlashFadeIn = 0.04f;
    [SerializeField] private float damageFlashHold = 0.05f;
    [SerializeField] private float damageFlashFadeOut = 0.2f;
    [SerializeField] private float damageFeedbackMinInterval = 0.04f;
    [SerializeField] private float healthPulseScale = 1.12f;
    [SerializeField] private float healthPulseDuration = 0.2f;

    private PlayerHealthScript playerHealth;
    private PlayerCurrencyController playerCurrency;
    private PlayerAudioController playerAudio;
    private WeaponLoadoutScript weaponLoadout;
    private WeaponScript observedWeapon;

    private float displayedHealthNormalized = 1f;
    private float damageFeedbackCooldownTimer;
    private float damageFlashTimer;
    private float healthPulseTimer;
    private int lastKnownHealth = -1;
    private Vector3 healthFillBaseScale = Vector3.one;
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
        ResolveUiReferences();
        BindEvents();
        RefreshAllImmediate();
        InitializeDamageFeedbackVisuals();
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
        UpdateDamageFeedbackAnimation();
        RefreshAmmo();
    }

    // Resuelve referencias.
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
            playerAudio = playerController.Audio ?? playerController.GetComponent<PlayerAudioController>();
        }
    }

    // Resuelve UI referencias.
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

        if (damageFlashImage == null)
        {
            damageFlashImage = FindComponentInChildrenByName<Image>("PanelUIPlayer");
        }
    }

    // Gestiona warn si UI referencias are faltante.
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
        GameDebug.Advertencia("HUD", $"PlayerHUDController tiene referencias faltantes bajo {name}: {missingReferences}", this);
    }

    // Conecta eventos.
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

    // Desconecta eventos.
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

    // Gestiona vida cambios.
    private void HandleHealthChanged(PlayerHealthScript health)
    {
        if (health == null)
        {
            RefreshHealthText();
            return;
        }

        int previousHealth = lastKnownHealth >= 0 ? lastKnownHealth : health.CurrentHealth;
        int currentHealth = health.CurrentHealth;
        int damageApplied = previousHealth - currentHealth;

        if (damageApplied > 0)
        {
            PlayDamageFeedback(damageApplied);
        }

        lastKnownHealth = currentHealth;
        RefreshHealthText();
    }

    // Gestiona actual arma cambios.
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

    // Gestiona monedas cambios.
    private void HandleCoinsChanged(PlayerCurrencyController currency)
    {
        RefreshCoins();
    }

    // Gestiona arma estado cambios.
    private void HandleWeaponStateChanged(WeaponScript weapon)
    {
        RefreshAmmo();
    }

    // Refresca todos inmediato.
    private void RefreshAllImmediate()
    {
        displayedHealthNormalized = playerHealth != null ? playerHealth.HealthNormalized : 1f;
        lastKnownHealth = playerHealth != null ? playerHealth.CurrentHealth : -1;

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = displayedHealthNormalized;
            healthFillBaseScale = healthFillImage.rectTransform.localScale;
        }

        RefreshHealthText();
        RefreshAmmo();
        RefreshCoins();
    }

    // Actualiza vida animación.
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

    // Refresca vida texto.
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

    // Refresca ammo.
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

    // Refresca monedas.
    private void RefreshCoins()
    {
        if (coinsText == null)
        {
            return;
        }

        int currentCoins = playerCurrency != null ? playerCurrency.CurrentCoins : 0;
        coinsText.text = $"COINS {currentCoins}";
    }

    // Inicializa da�o feedback visuals.
    private void InitializeDamageFeedbackVisuals()
    {
        damageFeedbackCooldownTimer = 0f;
        damageFlashTimer = 0f;
        healthPulseTimer = 0f;

        SetDamageFlashAlpha(0f);

        if (healthFillImage != null)
        {
            healthFillBaseScale = healthFillImage.rectTransform.localScale;
        }
    }

    // Actualiza da�o feedback animación.
    private void UpdateDamageFeedbackAnimation()
    {
        if (damageFeedbackCooldownTimer > 0f)
        {
            damageFeedbackCooldownTimer -= Time.deltaTime;
        }

        UpdateDamageFlash();
        UpdateHealthPulse();
    }

    // Actualiza da�o flash.
    private void UpdateDamageFlash()
    {
        if (damageFlashImage == null)
        {
            return;
        }

        if (damageFlashTimer <= 0f)
        {
            SetDamageFlashAlpha(0f);
            return;
        }

        float totalDuration = GetDamageFlashTotalDuration();
        damageFlashTimer = Mathf.Max(0f, damageFlashTimer - Time.deltaTime);
        float elapsed = totalDuration - damageFlashTimer;

        float fadeIn = Mathf.Max(0f, damageFlashFadeIn);
        float hold = Mathf.Max(0f, damageFlashHold);
        float fadeOut = Mathf.Max(0.001f, damageFlashFadeOut);
        float alpha01;

        if (elapsed <= fadeIn && fadeIn > 0.001f)
        {
            alpha01 = Mathf.Clamp01(elapsed / fadeIn);
        }
        else if (elapsed <= fadeIn + hold)
        {
            alpha01 = 1f;
        }
        else
        {
            float fadeOutElapsed = elapsed - fadeIn - hold;
            alpha01 = 1f - Mathf.Clamp01(fadeOutElapsed / fadeOut);
        }

        SetDamageFlashAlpha(alpha01);
    }

    // Actualiza vida pulse.
    private void UpdateHealthPulse()
    {
        if (healthFillImage == null)
        {
            return;
        }

        if (healthPulseTimer <= 0f || healthPulseDuration <= 0.001f)
        {
            healthFillImage.rectTransform.localScale = healthFillBaseScale;
            return;
        }

        healthPulseTimer = Mathf.Max(0f, healthPulseTimer - Time.deltaTime);
        float normalizedProgress = 1f - (healthPulseTimer / healthPulseDuration);
        float pulse = Mathf.Sin(Mathf.Clamp01(normalizedProgress) * Mathf.PI);
        float scaleMultiplier = Mathf.Lerp(1f, Mathf.Max(1f, healthPulseScale), pulse);
        healthFillImage.rectTransform.localScale = healthFillBaseScale * scaleMultiplier;
    }

    // Reproduce daño feedback.
    private void PlayDamageFeedback(int damageApplied)
    {
        if (damageApplied <= 0)
        {
            return;
        }

        if (damageFeedbackCooldownTimer <= 0f)
        {
            healthPulseTimer = Mathf.Max(0.01f, healthPulseDuration);
            damageFeedbackCooldownTimer = Mathf.Max(0f, damageFeedbackMinInterval);
        }

        damageFlashTimer = GetDamageFlashTotalDuration();
        playerAudio?.PlayHurt();
    }

    // Obtiene da�o flash total duración.
    private float GetDamageFlashTotalDuration()
    {
        return Mathf.Max(0.05f, Mathf.Max(0f, damageFlashFadeIn) + Mathf.Max(0f, damageFlashHold) + Mathf.Max(0.001f, damageFlashFadeOut));
    }

    // Actualiza da�o flash alpha.
    private void SetDamageFlashAlpha(float alpha01)
    {
        if (damageFlashImage == null)
        {
            return;
        }

        Color color = damageFlashColor;
        color.a *= Mathf.Clamp01(alpha01);
        damageFlashImage.color = color;
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
