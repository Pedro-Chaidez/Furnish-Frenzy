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
 
        // CartInventory.AddItem now handles PickVariant internally
        bool isBig = IsBigItem();
        bool added = cartInventory.AddItem(itemData, isBig);
 
        if (added)
        {
            CartUI.Instance?.RefreshUI();
            gameObject.SetActive(false);
        }
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
}
 