
using UnityEngine;
 
public class StoreItem : Interactable
{
    [Header("Item Data")]
    public FurnitureItem itemData;
 
    [Header("Interaction")]
    public float interactRadius = 4f;
 
    private string[] bigItemCategories =
    {
        "Bed", "Sofa", "Table", "Closet",
        "Kitchen", "Bathroom", "Drawer"
    };
 
    private void Awake()
    {
        if (string.IsNullOrEmpty(promptMessage))
            promptMessage = "Press E to add to cart";
    }
 
    protected override void Interact()
    {
        TryAddToCart();
    }
 
    private void TryAddToCart()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{gameObject.name} has no FurnitureItem assigned!");
            return;
        }
 
        CartInventory cartInventory = CartInventory.Instance
            ?? Object.FindAnyObjectByType<CartInventory>();
 
        if (cartInventory == null)
        {
            Debug.LogWarning($"{gameObject.name}: No CartInventory found at runtime.");
            return;
        }
 
        bool isBig = IsBigItem();
 
        // Try to match a prefab variant from the scene object name
        GameObject pickedPrefab = itemData.FindVariant(gameObject)
            ?? FindVariantFromParentsOrChildren();
 
        // If still null, just pick a random one — better than nothing
        if (pickedPrefab == null)
        {
            pickedPrefab = itemData.GetRandomVariant();
            Debug.Log($"{gameObject.name}: Could not match exact variant, using random: {pickedPrefab?.name}");
        }
        else
        {
            Debug.Log($"{gameObject.name}: Matched variant: {pickedPrefab.name}");
        }
 
        bool added = cartInventory.AddItem(itemData, isBig, pickedPrefab, null);
 
        if (added)
        {
            CartUI.Instance?.RefreshUI();
            GameObject rootToHide = FindStoreObjectRoot();
            rootToHide.SetActive(false);
        }
    }
 
    private GameObject FindStoreObjectRoot()
    {
        Transform current = transform;
 
        while (current.parent != null)
        {
            if (current.parent.GetComponent<ShoppingCart3D>() != null)
                break;
            if (current.parent.GetComponentsInChildren<StoreItem>().Length > 1)
                break;
            current = current.parent;
        }
 
        return current.gameObject;
    }
 
    private bool IsBigItem()
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.itemName))
            return false;
 
        foreach (string category in bigItemCategories)
        {
            if (itemData.itemName.Contains(category))
                return true;
        }
 
        return false;
    }
 
    private GameObject FindVariantFromParentsOrChildren()
    {
        if (itemData == null) return null;
 
        Transform current = transform.parent;
        while (current != null)
        {
            GameObject match = itemData.FindVariant(current.gameObject);
            if (match != null) return match;
            current = current.parent;
        }
 
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            GameObject match = itemData.FindVariant(child.gameObject);
            if (match != null) return match;
        }
 
        return null;
    }
}