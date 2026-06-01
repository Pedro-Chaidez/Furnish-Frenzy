using UnityEngine;

public class SodaCan : Item
{
    [Header("Consumable Settings")]
    public float healAmount = 25f;

    public override void useItem()
    {
        // Find the player in the scene
        Player player = FindAnyObjectByType<Player>();

        if (player != null)
        {
            // Assuming you add a Heal() method to your Entity/Player class
            player.Heal(healAmount); 
            Debug.Log($"{itemName} used! Restored {healAmount} health.");
        }
    }
}