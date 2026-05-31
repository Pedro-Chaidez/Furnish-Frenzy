using UnityEngine;

public class StoreItem : Interactable
{
    [Header("Item Data")]
    public FurnitureItem itemData;

    [Header("Inventory Reference")]
    public CartInventory cartInventory;

    [Header("Interaction")]
    public float interactRadius = 4f;

    private string[] bigItemCategories =
    {
        "Bed", "Sofa", "Table", "Chair", "Closet",
        "Kitchen", "Bathroom", "Drawer", "Cushion"
    };

    private void Awake()
    {
        if (string.IsNullOrEmpty(promptMessage))
        {
            promptMessage = "Press E to add to cart";
        }
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

        FindCartInventoryIfMissing();

        if (cartInventory == null)
        {
            Debug.LogWarning($"{gameObject.name}: CartInventory reference is missing. Drag the CartInventory object into this StoreItem.");
            return;
        }

        bool isBig = IsBigItem();
        bool added = cartInventory.AddItem(itemData, isBig);

        if (added)
        {
            Debug.Log($"Added {itemData.itemName} to {(isBig ? "hands" : "cart")}");
            CartUI.Instance?.RefreshUI();

            gameObject.SetActive(false);
        }
    }

    private void FindCartInventoryIfMissing()
    {
        if (cartInventory != null) return;

        cartInventory = CartInventory.Instance;

        if (cartInventory != null) return;

        cartInventory = Object.FindAnyObjectByType<CartInventory>();

        if (cartInventory != null) return;

        GameObject obj = GameObject.Find("CartInventory");

        if (obj != null)
        {
            cartInventory = obj.GetComponent<CartInventory>();
        }
    }

    private bool IsBigItem()
    {
        if (itemData == null || string.IsNullOrEmpty(itemData.itemName))
        {
            return false;
        }

        foreach (string category in bigItemCategories)
        {
            if (itemData.itemName.Contains(category))
            {
                return true;
            }
        }

        return false;
    }
}