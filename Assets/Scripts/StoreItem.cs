using UnityEngine;

public class StoreItem : Interactable
{
    [Header("Item Data")]
    public FurnitureItem itemData;

    [Header("Inventory Reference")]
    public CartInventory cartInventory;

    [Header("Interaction")]
    public float interactRadius = 4f;

    [Header("Carry Size Override")]
    [Tooltip("Turn this on only for furniture that should take the player's hands instead of a cart slot.")]
    public bool forceBigItem = false;

    [Tooltip("Turn this on for objects that should always go into the cart, even if their category name normally sounds large.")]
    public bool forceSmallItem = false;

    // Only these should block the player's hands by default.
    // Cushions, pillows, lamps, plates, cups, jars, plants, small decor, etc. should NOT be here.
    private readonly string[] defaultBigItemCategories =
    {
        "Bed", "Sofa", "Couch", "Table", "Closet", "Wardrobe", "Drawer", "Dresser", "Cabinet"
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
            Debug.LogWarning($"{gameObject.name}: CartInventory reference is missing.");
            return;
        }

        bool isBig = IsBigItem();

        // Pass this scene object so CartInventory/FurnitureItem can try to keep the exact variant.
        bool added = cartInventory.AddItem(itemData, isBig, gameObject, gameObject);

        if (added)
        {
            Debug.Log($"Added {itemData.itemName} from {gameObject.name} to {(isBig ? "hands" : "cart")}");
            CartUI.Instance?.RefreshUI();
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"Could not add {itemData.itemName} from {gameObject.name}. Cart/hands may be full.");
        }
    }

    private void FindCartInventoryIfMissing()
    {
        if (cartInventory != null) return;

        cartInventory = CartInventory.Instance;
        if (cartInventory != null) return;

        cartInventory = Object.FindAnyObjectByType<CartInventory>();
        if (cartInventory != null) return;

        GameObject obj = GameObject.Find("CartInventorySystem");
        if (obj != null) cartInventory = obj.GetComponent<CartInventory>();

        if (cartInventory != null) return;

        obj = GameObject.Find("CartInventory");
        if (obj != null) cartInventory = obj.GetComponent<CartInventory>();
    }

    private bool IsBigItem()
    {
        if (forceSmallItem) return false;
        if (forceBigItem) return true;

        if (itemData == null || string.IsNullOrEmpty(itemData.itemName))
        {
            return false;
        }

        foreach (string category in defaultBigItemCategories)
        {
            if (itemData.itemName.Contains(category))
            {
                return true;
            }
        }

        return false;
    }
}
