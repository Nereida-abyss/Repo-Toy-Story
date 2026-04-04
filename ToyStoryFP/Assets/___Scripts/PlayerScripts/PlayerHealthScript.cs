using UnityEngine;

public class PlayerHealthScript : MonoBehaviour, IDamageable
{
    public int health;

    public bool IsAlive => health > 0;

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
        {
            return;
        }

        health -= damage;

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
