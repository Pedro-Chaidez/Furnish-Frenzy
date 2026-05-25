using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

[System.Serializable]
public class ItemSaveData
{
    public string itemID;
    public Vector3 position;
    public Quaternion rotation;
    public string parentName; // Used to try and re-parent on load
}

[System.Serializable]
public class SceneSaveData
{
    public List<ItemSaveData> savedItems = new List<ItemSaveData>();
}

public class PersistentStateManager : MonoBehaviour
{
    public static PersistentStateManager Instance;

    [Header("Prefab Database")]
    [Tooltip("Drag all placeable Item prefabs here. The script will use these to rebuild the scene.")]
    public List<Item> placeablePrefabs;

    private SceneSaveData currentSceneData = new SceneSaveData();

    // Dynamically gets the key based on whatever scene we are currently in
    private string SaveKey => "SceneData_" + SceneManager.GetActiveScene().name;

    private void Awake()
    {
        // Singleton pattern with persistence
        if (Instance == null)
        {
            Instance = this;

            // Tell Unity not to destroy this when loading a new scene
            DontDestroyOnLoad(gameObject);

            // Listen for scene changes to automatically load data for the new level
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Load data for the initial startup scene
            LoadSceneData();
        }
        else
        {
            // If we reload a scene that already has a manager, destroy the duplicate
            Destroy(gameObject);
            return;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Automatically load the saved items whenever a new level finishes loading
        LoadSceneData();
    }

    private void OnDestroy()
    {
        // Clean up the event listener if the game closes to prevent memory leaks
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void SavePlacedItem(string id, Transform itemTransform)
    {
        ItemSaveData data = new ItemSaveData
        {
            itemID = id,
            position = itemTransform.position,
            rotation = itemTransform.rotation,
            parentName = itemTransform.parent != null ? itemTransform.parent.name : ""
        };

        // Check if updating an existing item; otherwise, add a new one
        int index = currentSceneData.savedItems.FindIndex(i => i.itemID == id);
        if (index >= 0)
        {
            currentSceneData.savedItems[index] = data;
        }
        else
        {
            currentSceneData.savedItems.Add(data);
        }

        CommitSave();
    }

    public void RemovePlacedItem(string id)
    {
        // Remove the item from the tracking list
        currentSceneData.savedItems.RemoveAll(i => i.itemID.StartsWith(id));
        CommitSave();
    }

    private void CommitSave()
    {
        // Serialize the list to JSON and save it in PlayerPrefs
        string json = JsonUtility.ToJson(currentSceneData);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void LoadSceneData()
    {
        currentSceneData = new SceneSaveData(); // Reset current data for the new scene

        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey);
            currentSceneData = JsonUtility.FromJson<SceneSaveData>(json);

            foreach (var itemData in currentSceneData.savedItems)
            {
                // Extract the base name (e.g., "ShoppingCart" from "ShoppingCart_123456")
                string baseItemName = itemData.itemID.Split('_')[0];

                // Find the matching prefab in the registry
                Item prefabToSpawn = placeablePrefabs.Find(p => p.itemName == baseItemName);

                if (prefabToSpawn != null)
                {
                    // Instantiate the object from the prefab
                    Item spawnedItem = Instantiate(prefabToSpawn, itemData.position, itemData.rotation);

                    // Handle physics: If it was attached to a parent object, lock it in place
                    Rigidbody rb = spawnedItem.GetComponent<Rigidbody>();
                    if (rb != null && !string.IsNullOrEmpty(itemData.parentName))
                    {
                        rb.isKinematic = true;

                        // Try to find the parent object in the scene to re-attach it
                        GameObject parentObj = GameObject.Find(itemData.parentName);
                        if (parentObj != null)
                        {
                            spawnedItem.transform.SetParent(parentObj.transform);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Could not find a prefab for: {baseItemName}. Ensure it is added to the placeablePrefabs list on the PersistentStateManager.");
                }
            }
        }
    }
}