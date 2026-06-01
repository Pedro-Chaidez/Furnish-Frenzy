using UnityEngine;
using System.Collections.Generic;

public class ShoppingCart : Item
{
    [Header("Equip Settings")]
    public Vector3 equippedPositionOffset = new Vector3(0f, 0f, 1.5f);
    public Vector3 equippedRotationOffset = Vector3.zero;

    [Header("Drop Settings")]
    public float dropDistance = 2.5f;

    [Header("Floor Drop Settings")]
    public float floorDropHeight = 0.5f; // Height above floor to spawn items
    public float spreadDistance = 2f;   // How far apart items are spread

    private Transform cachedPlayerTransform;

    public override void useItem()
    {
        Debug.Log("Used " + itemName);
        EmptyCartToFloor();
    }

    public override void OnEquipCustom(Transform playerTransform)
    {
        cachedPlayerTransform = playerTransform;

        // 1. Turn off physics completely while held
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // 2. Disable the hovering physics script
        if (physicsController != null) physicsController.enabled = false;

        // 3. Parent to player, ground it, and lock rotation using our offsets
        transform.SetParent(playerTransform);
        transform.localPosition = equippedPositionOffset;
        transform.localRotation = Quaternion.Euler(equippedRotationOffset);
    }

    public override void OnUnequipCustom()
    {
        // Detach from the player
        transform.SetParent(null);

        // Turn physics and gravity back on
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        if (physicsController != null) physicsController.enabled = true;
    }

    /// <summary>
    /// Empties the cart by dropping all child items onto the house floor
    /// </summary>
    public void EmptyCartToFloor()
    {
        if (transform.childCount == 0)
        {
            Debug.Log("Shopping cart is empty!");
            return;
        }

        Debug.Log($"Emptying shopping cart with {transform.childCount} items");

        // Collect all children first (to avoid issues with modifying hierarchy while iterating)
        List<Transform> childrenToEmpty = new List<Transform>();
        foreach (Transform child in transform)
        {
            childrenToEmpty.Add(child);
        }

        // Drop each child item onto the floor
        for (int i = 0; i < childrenToEmpty.Count; i++)
        {
            Transform child = childrenToEmpty[i];
            Item itemInCart = child.GetComponent<Item>();

            if (itemInCart != null)
            {
                // Detach from cart
                child.SetParent(null);

                // Calculate spread position around the cart
                Vector3 spreadPos = transform.position + (transform.forward * (i * spreadDistance));
                spreadPos.y = floorDropHeight;
                child.position = spreadPos;

                // Enable physics
                Rigidbody itemRb = child.GetComponent<Rigidbody>();
                if (itemRb != null)
                {
                    itemRb.isKinematic = false;
                }

                // Re-enable physics controller if it has one
                HeldItemPhysics physicsCtrl = child.GetComponent<HeldItemPhysics>();
                if (physicsCtrl != null)
                {
                    physicsCtrl.enabled = true;
                }

                Debug.Log($"Dropped {itemInCart.itemName} onto floor");
            }
        }

        Debug.Log("Shopping cart emptied!");
    }

    /// <summary>
    /// Adds an item to the shopping cart (as a child)
    /// </summary>
    public void AddItemToCart(Item itemToAdd)
    {
        if (itemToAdd != null)
        {
            // Parent the item to the cart
            itemToAdd.transform.SetParent(transform);
            itemToAdd.transform.localPosition = Vector3.zero;

            // Disable physics while in cart
            Rigidbody itemRb = itemToAdd.GetComponent<Rigidbody>();
            if (itemRb != null)
            {
                itemRb.isKinematic = true;
            }

            HeldItemPhysics physicsCtrl = itemToAdd.GetComponent<HeldItemPhysics>();
            if (physicsCtrl != null)
            {
                physicsCtrl.enabled = false;
            }

            Debug.Log($"Added {itemToAdd.itemName} to shopping cart");
        }
    }

    public override void OnDrop(float force, Vector3 direction)
    {
        if (cachedPlayerTransform != null)
        {
            Vector3 newDropPos = cachedPlayerTransform.position + (cachedPlayerTransform.forward * dropDistance);
            newDropPos.y = cachedPlayerTransform.position.y;
            transform.position = newDropPos;

            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);

            cachedPlayerTransform = null;
        }

        direction.y = 0;
        direction.Normalize();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && force > 0)
        {
            // --- THE FIX: Flag the cart as thrown so the base class knows to deal damage ---
            isThrown = true;
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }
}