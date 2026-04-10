using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerHealthScript : MonoBehaviour, IDamageable
{

    [FormerlySerializedAs("health")]
    [SerializeField] private int maxHealth = 100;
    [Header("Death Drop")]
    [SerializeField] private bool dropCoinOnDeath = true;
    [SerializeField] private int coinValue = 1;
    [SerializeField] private Vector3 coinDropOffset = new Vector3(0f, 0.5f, 0f);

    private int currentHealth;

    public event Action<PlayerHealthScript> HealthChanged;
    public event Action<PlayerHealthScript> Died;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public float HealthNormalized => maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
    public bool IsAlive => currentHealth > 0;

    void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    // Gestiona take da�o.
    public DamageResult TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0)
        {
            return DamageResult.None;
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        NotifyHealthChanged();

        int damageApplied = previousHealth - currentHealth;
        bool wasKilled = currentHealth <= 0;

        if (wasKilled)
        {
            HandleDeath();

            if (!BelongsToPlayer())
            {
                Destroy(gameObject);
            }
        }

        return new DamageResult(damageApplied > 0, wasKilled, damageApplied);
    }

    // Cura vida actual sin superar el maximo.
    public bool Heal(int amount)
    {
        if (!IsAlive || amount <= 0 || currentHealth >= maxHealth)
        {
            return false;
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);

        if (currentHealth == previousHealth)
        {
            return false;
        }

        NotifyHealthChanged();
        return true;
    }

    // Actualiza maximo vida.
    public void SetMaxHealth(int newMaxHealth, bool restoreCurrentHealthToMax)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        currentHealth = restoreCurrentHealthToMax
            ? maxHealth
            : Mathf.Clamp(currentHealth, 0, maxHealth);
        NotifyHealthChanged();
    }

    // Notifica vida cambios.
    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(this);
    }

    // Gestiona death.
    private void HandleDeath()
    {
        Died?.Invoke(this);

        if (!ShouldDropCoin())
        {
            return;
        }

        CoinPickup.Spawn(transform.position + coinDropOffset, coinValue);
    }

    // Comprueba si soltar moneda.
    private bool ShouldDropCoin()
    {
        if (!dropCoinOnDeath || coinValue <= 0)
        {
            return false;
        }

        return !BelongsToPlayer();
    }

    // Gestiona belongs a jugador.
    private bool BelongsToPlayer()
    {
        return GetComponentInParent<PlayerController>() != null;
    }
}
