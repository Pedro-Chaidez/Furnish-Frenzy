using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f; // NEW: Cap the health
    public ushort teamID = 2; // 0 for Player, 1 for Enemy, 2 for Neutral

    public virtual void TakeDamage(float amount, GameObject source = null)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    // --- NEW: Healing Logic ---
    public virtual void Heal(float amount)
    {
        health += amount;

        // Prevent health from going over the maximum
        if (health > maxHealth)
        {
            health = maxHealth;
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}