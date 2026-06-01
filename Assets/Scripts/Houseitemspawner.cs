using UnityEngine;
using System.Collections.Generic;

public class HouseItemSpawner : MonoBehaviour
{
    [Header("Item Registry")]
    [Tooltip("Drag all FurnitureItem ScriptableObjects here.")]
    public List<FurnitureItem> allItems = new List<FurnitureItem>();

    [Header("Spawn Point")]
    public Transform spawnPoint;
    public float spreadDistance = 1.5f;

    void Start()
    {
        SpawnPendingItems();
    }

    void SpawnPendingItems()
    {
        int cartCount = PlayerPrefs.GetInt("PendingCartCount", 0);
        int handCount = PlayerPrefs.GetInt("PendingHandCount", 0);

        Debug.Log($"HouseItemSpawner: cartCount={cartCount} handCount={handCount}");

        if (cartCount == 0 && handCount == 0)
        {
            Debug.Log("HouseItemSpawner: Nothing to spawn.");
            return;
        }

        int totalSpawned = 0;

        for (int i = 0; i < cartCount; i++)
        {
            string itemName   = PlayerPrefs.GetString($"Cart_{i}_Name", "");
            string prefabName = PlayerPrefs.GetString($"Cart_{i}_Prefab", "");
            Debug.Log($"HouseItemSpawner: cart[{i}] itemName='{itemName}' prefabName='{prefabName}'");

            if (SpawnItem(itemName, prefabName, GetSpawnPos(totalSpawned)))
                totalSpawned++;

            PlayerPrefs.DeleteKey($"Cart_{i}_Name");
            PlayerPrefs.DeleteKey($"Cart_{i}_Prefab");
            PlayerPrefs.DeleteKey($"Cart_{i}_X");
            PlayerPrefs.DeleteKey($"Cart_{i}_Y");
            PlayerPrefs.DeleteKey($"Cart_{i}_Z");
        }

        for (int i = 0; i < handCount; i++)
        {
            string itemName   = PlayerPrefs.GetString($"Hand_{i}_Name", "");
            string prefabName = PlayerPrefs.GetString($"Hand_{i}_Prefab", "");
            Debug.Log($"HouseItemSpawner: hand[{i}] itemName='{itemName}' prefabName='{prefabName}'");

            if (SpawnItem(itemName, prefabName, GetSpawnPos(totalSpawned)))
                totalSpawned++;

            PlayerPrefs.DeleteKey($"Hand_{i}_Name");
            PlayerPrefs.DeleteKey($"Hand_{i}_Prefab");
            PlayerPrefs.DeleteKey($"Hand_{i}_X");
            PlayerPrefs.DeleteKey($"Hand_{i}_Y");
            PlayerPrefs.DeleteKey($"Hand_{i}_Z");
        }

        PlayerPrefs.DeleteKey("PendingCartCount");
        PlayerPrefs.DeleteKey("PendingHandCount");
        PlayerPrefs.Save();

        Debug.Log($"HouseItemSpawner: Spawned {totalSpawned} items.");
    }

    bool SpawnItem(string itemName, string prefabName, Vector3 pos)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning("HouseItemSpawner: Empty item name, skipping.");
            return false;
        }

        // Find ScriptableObject by itemName
        FurnitureItem match = allItems.Find(f => f != null && f.itemName == itemName);

        if (match == null)
        {
            // Print ALL available item names so we can see the mismatch
            var available = new System.Text.StringBuilder();
            foreach (var f in allItems)
                if (f != null) available.Append($"'{f.itemName}' ");
            Debug.LogError($"HouseItemSpawner: No SO found for itemName='{itemName}'. " +
                           $"Available: [{available}]");
            return false;
        }

        // Try to find the exact prefab by name
        GameObject prefab = null;
        if (!string.IsNullOrEmpty(prefabName) && match.prefabVariants != null)
        {
            // Print all variant names so we can see the mismatch
            var variantNames = new System.Text.StringBuilder();
            foreach (var v in match.prefabVariants)
                if (v != null) variantNames.Append($"'{v.name}' ");

            Debug.Log($"HouseItemSpawner: Looking for prefab '{prefabName}' in '{itemName}'. " +
                      $"Available variants: [{variantNames}]");

            foreach (var v in match.prefabVariants)
            {
                if (v != null && v.name == prefabName)
                {
                    prefab = v;
                    break;
                }
            }
        }

        if (prefab == null)
        {
            Debug.LogWarning($"HouseItemSpawner: Exact prefab '{prefabName}' not found — " +
                             $"falling back to random for '{itemName}'.");
            prefab = match.GetRandomVariant();
        }

        if (prefab == null)
        {
            Debug.LogError($"HouseItemSpawner: '{itemName}' has no prefab variants at all!");
            return false;
        }

        GameObject spawned = Instantiate(prefab, pos, Quaternion.identity);
        Debug.Log($"HouseItemSpawner: Spawned '{prefab.name}' at {pos}");

        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        return true;
    }

    Vector3 GetSpawnPos(int index)
    {
        Vector3 origin = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        return origin + Vector3.right * (index * spreadDistance);
    }
}