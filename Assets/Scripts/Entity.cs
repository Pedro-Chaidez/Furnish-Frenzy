using UnityEngine;
using System;

public abstract class Entity : MonoBehaviour
{
    public float maxHealth = 100f;
    public float health = 100f;
    public ushort teamID = 2; // 0 for Player, 1 for Enemy, 2 for Neutral

    // Event triggered whenever health changes (useful for updating UI)
    public event Action<float, float> OnHealthChanged;

    public virtual void TakeDamage(float amount, GameObject source = null)
    {
        health -= amount;

        // Prevent health from dropping below 0 or going above maxHealth
        health = Mathf.Clamp(health, 0, maxHealth);

        OnHealthChanged?.Invoke(health, maxHealth);

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
        health = Mathf.Clamp(health, 0, maxHealth);

        OnHealthChanged?.Invoke(health, maxHealth);
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}