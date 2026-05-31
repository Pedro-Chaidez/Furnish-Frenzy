using UnityEngine;

[CreateAssetMenu(menuName = "Furniture/Furniture Item", fileName = "NewFurnitureItem")]
public class FurnitureItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public float price;
    public Vector3 defaultPlacementOffset = Vector3.zero;
    public GameObject[] prefabVariants;

    // Tracks which specific prefab was picked in the store
    // Set automatically by PickVariant() at pickup time
    [HideInInspector] public GameObject chosenVariant;

    // Call this when the player picks up the item in the store.
    // Picks a random variant and remembers it so the same one
    // can be spawned in the house.
    public GameObject PickVariant()
    {
        if (prefabVariants == null || prefabVariants.Length == 0)
        {
            chosenVariant = null;
            return null;
        }

        chosenVariant = prefabVariants[Random.Range(0, prefabVariants.Length)];
        return chosenVariant;
    }

    // Use this anywhere you just want a random prefab without locking in a choice
    // (e.g. cart 3D previews, store shelf displays)
    public GameObject GetRandomVariant()
    {
        if (prefabVariants == null || prefabVariants.Length == 0) return null;
        return prefabVariants[Random.Range(0, prefabVariants.Length)];
    }

    // Returns the chosen variant if one was picked, otherwise falls back to random
    public GameObject GetChosenVariant()
    {
        if (chosenVariant != null) return chosenVariant;
        return GetRandomVariant();
    }
}