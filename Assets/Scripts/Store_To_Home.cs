using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
 
public class DoorExit : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Vector3 dropSpawnPoint = Vector3.zero;
    public float spreadDistance = 1.2f;
 
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
 
        if (CartInventory.Instance == null)
        {
            SceneManager.LoadScene("House");
            return;
        }
 
        List<FurnitureItem> cartItems = CartInventory.Instance.GetCartItems();
        List<FurnitureItem> handItems = CartInventory.Instance.GetHandItems();
 
        if (cartItems.Count == 0 && handItems.Count == 0)
        {
            Debug.Log("Inventory empty — not loading house.");
            return;
        }
 
        // Clear stale data
        PlayerPrefs.DeleteKey("PendingCartCount");
        PlayerPrefs.DeleteKey("PendingHandCount");
 
        // Save cart items — use the prefab stored in CartInventory, not on the ScriptableObject
        PlayerPrefs.SetInt("PendingCartCount", cartItems.Count);
        for (int i = 0; i < cartItems.Count; i++)
        {
            GameObject prefab = CartInventory.Instance.GetCartPrefab(i);
            Vector3 pos = dropSpawnPoint + Vector3.right * (i * spreadDistance);
 
            PlayerPrefs.SetString($"Cart_{i}_Name",   cartItems[i].itemName);
            PlayerPrefs.SetString($"Cart_{i}_Prefab", prefab != null ? prefab.name : "");
            PlayerPrefs.SetFloat($"Cart_{i}_X", pos.x);
            PlayerPrefs.SetFloat($"Cart_{i}_Y", pos.y);
            PlayerPrefs.SetFloat($"Cart_{i}_Z", pos.z);
 
            Debug.Log($"Saving cart[{i}]: {cartItems[i].itemName} [{prefab?.name}]");
        }
 
        // Save hand items
        PlayerPrefs.SetInt("PendingHandCount", handItems.Count);
        for (int i = 0; i < handItems.Count; i++)
        {
            GameObject prefab = CartInventory.Instance.GetHandPrefab(i);
            Vector3 pos = dropSpawnPoint + Vector3.forward * (i * spreadDistance);
 
            PlayerPrefs.SetString($"Hand_{i}_Name",   handItems[i].itemName);
            PlayerPrefs.SetString($"Hand_{i}_Prefab", prefab != null ? prefab.name : "");
            PlayerPrefs.SetFloat($"Hand_{i}_X", pos.x);
            PlayerPrefs.SetFloat($"Hand_{i}_Y", pos.y);
            PlayerPrefs.SetFloat($"Hand_{i}_Z", pos.z);
 
            Debug.Log($"Saving hand[{i}]: {handItems[i].itemName} [{prefab?.name}]");
        }
 
        PlayerPrefs.Save();
        Debug.Log($"Saved {cartItems.Count} cart + {handItems.Count} hand items. Loading house...");
        CartInventory.Instance.ClearCart();
        SceneManager.LoadScene("House");
    }
}
 