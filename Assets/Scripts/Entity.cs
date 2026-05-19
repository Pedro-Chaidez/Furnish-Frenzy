using UnityEngine;
using System;
 
public abstract class Entity : MonoBehaviour
{
    public float maxHealth = 100f;
    public float health = 100f;
    public ushort teamID = 2;
 

    public event Action<float, float> OnHealthChanged;
 
    public virtual void TakeDamage(float amount, GameObject source = null)
    {
        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth);
 
        OnHealthChanged?.Invoke(health, maxHealth);
 
        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(health, maxHealth);
    }
 
    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
 