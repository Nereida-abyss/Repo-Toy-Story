using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

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
            SceneManager.LoadScene("EndMenu");
            Destroy(gameObject,2f);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        return new DamageResult(damageApplied > 0, wasKilled, damageApplied);
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(this);
    }

    private void HandleDeath()
    {
        Died?.Invoke(this);

        if (!ShouldDropCoin())
        {
            return;
        }

        CoinPickup.Spawn(transform.position + coinDropOffset, coinValue);
    }

    private bool ShouldDropCoin()
    {
        if (!dropCoinOnDeath || coinValue <= 0)
        {
            return false;
        }

        return GetComponentInParent<PlayerController>() == null;
    }
}
