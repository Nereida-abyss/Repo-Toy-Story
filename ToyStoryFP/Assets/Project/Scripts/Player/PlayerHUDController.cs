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

    private PlayerController playerController;
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
        WarnIfUiReferencesAreMissing();
        BindEvents();
        RefreshAllImmediate();
    }

    void OnEnable()
    {
        ResolveReferences();
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
        if (healthFillImage != null)
        {
            healthFillBaseScale = healthFillImage.rectTransform.localScale;
        }
    }

    void Update()
    {
        UpdateHealthAnimation();
        UpdateDamageFeedbackAnimation();
        RefreshAmmo();
    }

    // Busca y cachea las piezas de gameplay de las que depende el HUD.
    private void ResolveReferences()
    {
        playerController ??= GetComponentInParent<PlayerController>();
        playerHealth = GetComponentInParent<PlayerHealthScript>();

        if (playerController != null)
        {
            weaponLoadout = playerController.WeaponLoadout ?? playerController.GetComponentInChildren<WeaponLoadoutScript>(true);
            playerCurrency = playerController.Currency ?? playerController.GetComponent<PlayerCurrencyController>();
            playerAudio = playerController.Audio ?? playerController.GetComponent<PlayerAudioController>();
        }
    }

    // Lanza una sola advertencia si faltan piezas importantes del HUD.
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

    // Se engancha a vida, monedas y arma actual para refrescar el HUD cuando algo cambie.
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

    // Suelta todas las suscripciones para no dejar eventos colgados al destruir o desactivar el HUD.
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

    // Cuando cambia la vida, actualiza texto y dispara feedback visual si hubo daño real.
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

    // Cambia la suscripción al arma observada y refresca la munición mostrada.
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

    // Refresca el contador de monedas cuando el valor cambia.
    private void HandleCoinsChanged(PlayerCurrencyController currency)
    {
        RefreshCoins();
    }

    // Cualquier cambio de estado del arma puede afectar a munición o texto de recarga.
    private void HandleWeaponStateChanged(WeaponScript weapon)
    {
        RefreshAmmo();
    }

    // Fuerza una foto completa del HUD sin animaciones intermedias.
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

    // Hace que la barra de vida persiga el valor real poco a poco para que no pegue saltos secos.
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

    // Actualiza el texto de vida con un fallback claro si todavía falta la referencia al jugador.
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

    // Decide qué texto de munición y recarga enseñar según arma, cambio de arma y reserva.
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

    // Actualiza el texto de monedas con el valor actual del run.
    private void RefreshCoins()
    {
        if (coinsText == null)
        {
            return;
        }

        int currentCoins = playerCurrency != null ? playerCurrency.CurrentCoins : 0;
        coinsText.text = $"COINS {currentCoins}";
    }

    // Reinicia el estado visual del feedback de daño para empezar desde cero.
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

    // Hace avanzar las dos capas de feedback de daño: flash y pulso de vida.
    private void UpdateDamageFeedbackAnimation()
    {
        if (damageFeedbackCooldownTimer > 0f)
        {
            damageFeedbackCooldownTimer -= Time.deltaTime;
        }

        UpdateDamageFlash();
        UpdateHealthPulse();
    }

    // Controla el panel rojo de daño con entrada, espera y salida.
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

    // Hace latir la barra de vida durante un momento cuando llega daño.
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

    // Activa el feedback de daño y respeta un pequeño cooldown para no saturar visual ni audio.
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

    // Suma las tres fases del flash para conocer su duración total real.
    private float GetDamageFlashTotalDuration()
    {
        return Mathf.Max(0.05f, Mathf.Max(0f, damageFlashFadeIn) + Mathf.Max(0f, damageFlashHold) + Mathf.Max(0.001f, damageFlashFadeOut));
    }

    // Aplica el alpha final del flash respetando el color base de la imagen.
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
}
