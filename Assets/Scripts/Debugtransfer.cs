// DebugTransfer.cs
// Temporarily attach this to any GameObject in BOTH your Store and House scenes.
// It will print exactly what is saved and what is found in PlayerPrefs.
// Delete it once everything works.
 
using UnityEngine;
using UnityEngine.SceneManagement;
 
public class DebugTransfer : MonoBehaviour
{
    void Start()
    {
        string scene = SceneManager.GetActiveScene().name;
        Debug.Log($"=== DebugTransfer: Scene = {scene} ===");
 
        int cartCount = PlayerPrefs.GetInt("PendingCartCount", -1);
        int handCount = PlayerPrefs.GetInt("PendingHandCount", -1);
 
        Debug.Log($"PendingCartCount = {cartCount}");
        Debug.Log($"PendingHandCount = {handCount}");
 
        for (int i = 0; i < Mathf.Max(cartCount, 0); i++)
        {
            string name   = PlayerPrefs.GetString($"Cart_{i}_Name",   "MISSING");
            string prefab = PlayerPrefs.GetString($"Cart_{i}_Prefab", "MISSING");
            float x = PlayerPrefs.GetFloat($"Cart_{i}_X", -9999);
            float y = PlayerPrefs.GetFloat($"Cart_{i}_Y", -9999);
            float z = PlayerPrefs.GetFloat($"Cart_{i}_Z", -9999);
            Debug.Log($"  Cart[{i}]: name='{name}' prefab='{prefab}' pos=({x},{y},{z})");
        }
 
        for (int i = 0; i < Mathf.Max(handCount, 0); i++)
        {
            string name   = PlayerPrefs.GetString($"Hand_{i}_Name",   "MISSING");
            string prefab = PlayerPrefs.GetString($"Hand_{i}_Prefab", "MISSING");
            float x = PlayerPrefs.GetFloat($"Hand_{i}_X", -9999);
            float y = PlayerPrefs.GetFloat($"Hand_{i}_Y", -9999);
            float z = PlayerPrefs.GetFloat($"Hand_{i}_Z", -9999);
            Debug.Log($"  Hand[{i}]: name='{name}' prefab='{prefab}' pos=({x},{y},{z})");
        }
 
        // If in Store scene, also dump CartInventory state
        if (scene == "Store" && CartInventory.Instance != null)
        {
            var cartItems = CartInventory.Instance.GetCartItems();
            var handItems = CartInventory.Instance.GetHandItems();
            Debug.Log($"CartInventory: {cartItems.Count} cart items, {handItems.Count} hand items");
            for (int i = 0; i < cartItems.Count; i++)
            {
                GameObject p = CartInventory.Instance.GetCartPrefab(i);
                Debug.Log($"  CartInventory cart[{i}]: '{cartItems[i].itemName}' prefab='{p?.name ?? "NULL"}'");
            }
            for (int i = 0; i < handItems.Count; i++)
            {
                GameObject p = CartInventory.Instance.GetHandPrefab(i);
                Debug.Log($"  CartInventory hand[{i}]: '{handItems[i].itemName}' prefab='{p?.name ?? "NULL"}'");
            }
        }
 
        // If in House scene, also check FurniturePlacer allItems list
        if (scene == "House")
        {
            var placer = FindAnyObjectByType<FurniturePlacer>();
            if (placer != null)
            {
                Debug.Log($"FurniturePlacer found with {placer.allItems.Count} items in registry:");
                foreach (var item in placer.allItems)
                    Debug.Log($"  Registry: '{item?.itemName}' ({item?.prefabVariants?.Length ?? 0} variants)");
            }
            else
            {
                Debug.LogError("FurniturePlacer NOT FOUND in House scene!");
            }
        }
    }
}