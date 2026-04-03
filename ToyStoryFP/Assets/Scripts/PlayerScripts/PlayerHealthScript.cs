using UnityEngine;

public class PlayerHealthScript : MonoBehaviour
{
    public int health;

    public void TakeDamage(int _damage)
    {
        health -= _damage;


        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
