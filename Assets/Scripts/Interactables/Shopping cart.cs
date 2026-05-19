using UnityEngine;

public class ShoppingCart : Item
{
    [Header("Equip Settings")]
    public Vector3 equippedPositionOffset = new Vector3(0f, 0f, 1.5f);
    public Vector3 equippedRotationOffset = Vector3.zero;

    [Header("Drop Settings")]
    public float dropDistance = 2.5f;

    private Transform cachedPlayerTransform;

    public override void useItem()
    {
        Debug.Log("Used " + itemName);
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