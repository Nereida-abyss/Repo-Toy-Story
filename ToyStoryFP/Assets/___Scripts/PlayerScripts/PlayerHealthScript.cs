using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerHealthScript : MonoBehaviour, IDamageable
{
    [FormerlySerializedAs("health")]
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public event Action<PlayerHealthScript> HealthChanged;

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

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        NotifyHealthChanged();

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(this);
    }
}
