// Contrato de objetos que pueden recibir Daño.
public interface IDamageable
{
    // Aplica Daño y devuelve el resultado final.
    DamageResult TakeDamage(int damage);
}
