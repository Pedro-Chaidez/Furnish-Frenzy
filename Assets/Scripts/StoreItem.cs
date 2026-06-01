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
 
        GameObject pickupRoot = FindScenePickupRoot();
        GameObject pickedPrefab = itemData.FindVariant(pickupRoot) 
            ?? itemData.FindVariant(gameObject) 
            ?? FindVariantFromParentsOrChildren();

        bool isBig = IsBigItem();
        bool added = cartInventory.AddItem(itemData, isBig, pickedPrefab, pickupRoot);
 
        if (added)
        {
            CartUI.Instance?.RefreshUI();

            // ✅ DON'T call SetActive(false) — ShoppingCart3D will parent and show it
            // Just disable physics and interaction so it doesn't float away
            // before RefreshCartVisuals runs
            Rigidbody rb = pickupRoot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
            }

            // Disable colliders so player doesn't keep interacting with it
            foreach (var col in pickupRoot.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            // Disable StoreItem so it can't be picked up again
            this.enabled = false;
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

    private GameObject FindScenePickupRoot()
    {
        if (itemData == null) return gameObject;

        Transform current = transform;
        while (current != null)
        {
            if (itemData.FindVariant(current.gameObject) != null && 
                !current.GetComponent<ShoppingCart3D>())
                return current.gameObject;

            current = current.parent;
        }

        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (itemData.FindVariant(child.gameObject) != null)
                return child.gameObject;
        }

        return gameObject;
    }
}