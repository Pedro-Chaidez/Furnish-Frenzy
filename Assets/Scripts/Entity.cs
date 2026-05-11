using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    public float health = 100f;
    public ushort teamID = 2; // 0 for Player, 1 for Enemy, 2 for Neutral

    // Added 'source' so the player can check if the object hitting them is in the parry box
    public virtual void TakeDamage(float amount, GameObject source = null)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}