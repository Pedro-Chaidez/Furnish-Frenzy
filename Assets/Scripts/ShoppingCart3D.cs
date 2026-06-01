using System.Collections.Generic;
using UnityEngine;

public class ShoppingCart3D : MonoBehaviour
{
    public static ShoppingCart3D Instance { get; private set; }

    [Header("Cart Follow")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0f, 0.6f, 2.2f);
    public float followSpeed = 8f;
    public float rotationSpeed = 12f;
    public Vector3 modelRotationOffset = Vector3.zero;
    public bool addColliderIfMissing = true;

    [Header("Item Slots (6 child Transforms inside cart basket)")]
    public Transform[] slotPositions = new Transform[6];
    public Transform bigItemSlot;

    [Header("Preview Scale (world-space units)")]
    public Vector3 bigItemScale   = Vector3.one * 0.4f;
    public Vector3 smallItemScale = Vector3.one * 0.25f;

    // Maps each docked scene object to its target slot
    private Dictionary<GameObject, Transform> dockedItems = new Dictionary<GameObject, Transform>();
    private List<GameObject> managedPreviews = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (addColliderIfMissing && GetComponent<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
        }
    }

    void OnEnable()  => CartInventory.InventoryChanged += RefreshCartVisuals;
    void OnDisable() => CartInventory.InventoryChanged -= RefreshCartVisuals;

    void Start()
    {
        if (playerTransform == null)
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        RefreshCartVisuals();
    }

    void Update()
    {
        if (playerTransform == null) return;

        Vector3 target = playerTransform.position
                       + playerTransform.forward * offset.z
                       + playerTransform.right   * offset.x
                       + playerTransform.up      * offset.y;

        transform.position = Vector3.Lerp(transform.position, target,
                                          Time.deltaTime * followSpeed);

        Quaternion targetRot = Quaternion.LookRotation(playerTransform.forward, Vector3.up)
                             * Quaternion.Euler(modelRotationOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                                              Time.deltaTime * rotationSpeed);
    }

    void LateUpdate()
    {
        // Every frame, force all docked items to sit exactly at their slot's world position
        // This works even if slots are outside the cart's hierarchy
        foreach (var kvp in dockedItems)
        {
            GameObject obj = kvp.Key;
            Transform slot = kvp.Value;

            if (obj == null || slot == null) continue;

            obj.transform.position = slot.position;
            obj.transform.rotation = slot.rotation;
        }
    }

    public void RefreshCartVisuals()
    {
        // Clean up old previews
        foreach (var obj in managedPreviews)
            if (obj != null) Destroy(obj);
        managedPreviews.Clear();
        dockedItems.Clear();

        if (CartInventory.Instance == null) return;

        GameObject bigPrefab           = CartInventory.Instance.GetBigItemPrefab();
        GameObject bigSceneObj         = CartInventory.Instance.GetBigItemSceneObject();
        List<FurnitureItem> smallItems = CartInventory.Instance.GetCartItems();

        // Big item
        if (bigPrefab != null)
        {
            Transform anchor = bigItemSlot != null ? bigItemSlot
                             : (slotPositions.Length > 0 ? slotPositions[0] : transform);

            if (bigSceneObj != null)
                DockSceneObject(bigSceneObj, anchor, bigItemScale);
            else
                SpawnPreviewClone(bigPrefab, anchor, bigItemScale);
        }

        // Small items
        int startSlot = bigPrefab != null ? 5 : 0;
        int maxSmall  = bigPrefab != null ? 1 : slotPositions.Length;

        for (int i = 0; i < Mathf.Min(smallItems.Count, maxSmall); i++)
        {
            int slotIndex = Mathf.Min(startSlot + i, slotPositions.Length - 1);
            Transform slot = slotPositions[slotIndex];
            if (slot == null) continue;

            GameObject sceneObj = CartInventory.Instance.GetCartSceneObject(i);
            GameObject prefab   = CartInventory.Instance.GetCartPrefab(i);

            if (sceneObj != null)
                DockSceneObject(sceneObj, slot, smallItemScale);
            else if (prefab != null)
                SpawnPreviewClone(prefab, slot, smallItemScale);
        }
    }

    void DockSceneObject(GameObject obj, Transform slot, Vector3 desiredWorldScale)
    {
        if (obj == null || slot == null) return;

        obj.SetActive(true);

        // Unparent so world-space positioning works cleanly
        obj.transform.SetParent(null);
        obj.transform.position = slot.position;
        obj.transform.rotation = slot.rotation;

        // Scale compensating for nothing (object is now in world space)
        obj.transform.localScale = desiredWorldScale;

        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic      = true;
            rb.linearVelocity   = Vector3.zero;
            rb.angularVelocity  = Vector3.zero;
            rb.useGravity       = false;
            rb.detectCollisions = false;
            rb.constraints      = RigidbodyConstraints.FreezeAll;
        }

        foreach (var col in obj.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var mb in obj.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null || mb is ShoppingCart3D) continue;
            mb.enabled = false;
        }

        // Register in dictionary so LateUpdate keeps it locked to the slot
        dockedItems[obj] = slot;
    }

    void SpawnPreviewClone(GameObject prefab, Transform slot, Vector3 desiredWorldScale)
    {
        if (prefab == null || slot == null) return;

        GameObject preview = Instantiate(prefab);
        preview.transform.position   = slot.position;
        preview.transform.rotation   = slot.rotation;
        preview.transform.localScale = desiredWorldScale;

        foreach (var rb in preview.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic      = true;
            rb.linearVelocity   = Vector3.zero;
            rb.angularVelocity  = Vector3.zero;
            rb.useGravity       = false;
            rb.detectCollisions = false;
            rb.constraints      = RigidbodyConstraints.FreezeAll;
        }

        foreach (var col in preview.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var mb in preview.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            mb.enabled = false;
        }

        // Register clone in dictionary too so LateUpdate moves it with the slot
        dockedItems[preview] = slot;
        managedPreviews.Add(preview);
    }
}