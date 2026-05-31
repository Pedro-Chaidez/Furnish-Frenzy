using System.Collections.Generic;
using UnityEngine;

public class CartInventory : MonoBehaviour
{
    public static CartInventory Instance { get; private set; }
    public static System.Action InventoryChanged;

    // Pair each item with the exact prefab chosen at pickup
    private List<(FurnitureItem item, GameObject prefab)> cartItems 
        = new List<(FurnitureItem, GameObject)>();
    private List<(FurnitureItem item, GameObject prefab)> handItems 
        = new List<(FurnitureItem, GameObject)>();
    private HashSet<FurnitureItem> placedItems = new HashSet<FurnitureItem>();

    private void Awake()
    {
        Instance = this;
    }

    public bool AddItem(FurnitureItem item, bool isBig)
    {
        if (item == null)
        {
            Debug.LogWarning("Tried to add a null FurnitureItem.");
            return false;
        }

        // Pick the exact prefab variant right now at pickup time
        GameObject chosenPrefab = item.PickVariant();

        if (isBig)
        {
            if (handItems.Count >= 1)
            {
                Debug.Log("Already carrying a big item! Can only carry 1.");
                return false;
            }

            handItems.Add((item, chosenPrefab));
            Debug.Log($"Added {item.itemName} [{chosenPrefab?.name}] to hands");
            InventoryChanged?.Invoke();
            return true;
        }
        else
        {
            int maxSmallItems = handItems.Count > 0 ? 1 : 6;

            if (cartItems.Count >= maxSmallItems)
            {
                Debug.Log($"Cart full! Max {maxSmallItems} small items.");
                return false;
            }

            cartItems.Add((item, chosenPrefab));
            Debug.Log($"Added {item.itemName} [{chosenPrefab?.name}] to cart ({cartItems.Count}/{maxSmallItems})");
            InventoryChanged?.Invoke();
            return true;
        }
    }

    // ── Getters ───────────────────────────────────────────────────────────────

    public List<FurnitureItem> GetCartItems()
    {
        var result = new List<FurnitureItem>();
        foreach (var entry in cartItems) result.Add(entry.item);
        return result;
    }

    public List<FurnitureItem> GetHandItems()
    {
        var result = new List<FurnitureItem>();
        foreach (var entry in handItems) result.Add(entry.item);
        return result;
    }

    // Returns the chosen prefab for a cart item at a given index
    public GameObject GetCartPrefab(int index)
    {
        if (index < 0 || index >= cartItems.Count) return null;
        return cartItems[index].prefab;
    }

    // Returns the chosen prefab for a hand item at a given index
    public GameObject GetHandPrefab(int index)
    {
        if (index < 0 || index >= handItems.Count) return null;
        return handItems[index].prefab;
    }

    public FurnitureItem GetBigItem()
    {
        return handItems.Count > 0 ? handItems[0].item : null;
    }

    public GameObject GetBigItemPrefab()
    {
        return handItems.Count > 0 ? handItems[0].prefab : null;
    }

    public int GetVisualOccupancy()
    {
        if (handItems.Count > 0)
            return Mathf.Min(5 + cartItems.Count, 6);
        return Mathf.Min(cartItems.Count, 6);
    }

    // ── Cart management ───────────────────────────────────────────────────────

    public void ClearCart()
    {
        cartItems.Clear();
        handItems.Clear();
        placedItems.Clear();
        Debug.Log("Cart cleared!");
        InventoryChanged?.Invoke();
    }

    public bool IsPlaced(FurnitureItem item)
    {
        return item != null && placedItems.Contains(item);
    }

    public void MarkPlaced(FurnitureItem item)
    {
        if (item == null) return;

        placedItems.Add(item);
        cartItems.RemoveAll(e => e.item == item);
        handItems.RemoveAll(e => e.item == item);

        InventoryChanged?.Invoke();
    }

    public bool IsFull
    {
        get
        {
            if (handItems.Count > 0)
                return cartItems.Count >= 1;
            return cartItems.Count >= 6;
        }
    }
}