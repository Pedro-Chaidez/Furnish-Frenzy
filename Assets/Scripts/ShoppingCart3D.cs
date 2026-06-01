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
 
    [Header("Preview Scale (world-space units)")]
    public Vector3 bigItemScale   = Vector3.one * 0.4f;
    public Vector3 smallItemScale = Vector3.one * 0.25f;
 
    [Header("Slot Offset (tune to push items into basket)")]
    [Tooltip("World-space offset applied to every item. Negative Y sinks items into the basket.")]
    public Vector3 slotPositionOffset = new Vector3(0f, -0.5f, 0f);
 
    private Dictionary<GameObject, Transform> dockedItems = new Dictionary<GameObject, Transform>();
    private List<GameObject> managedPreviews = new List<GameObject>();
 
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
 
        if (addColliderIfMissing && GetComponent<Collider>() == null)
            gameObject.AddComponent<BoxCollider>().isTrigger = false;
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
 
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followSpeed);
 
        Quaternion targetRot = Quaternion.LookRotation(playerTransform.forward, Vector3.up)
                             * Quaternion.Euler(modelRotationOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }
 
    void LateUpdate()
    {
        foreach (var kvp in dockedItems)
        {
            if (kvp.Key == null || kvp.Value == null) continue;
            kvp.Key.transform.position = kvp.Value.position + slotPositionOffset;
            kvp.Key.transform.rotation = kvp.Value.rotation;
        }
    }
 
    public void RefreshCartVisuals()
    {
        foreach (var obj in managedPreviews)
            if (obj != null) Destroy(obj);
        managedPreviews.Clear();
        dockedItems.Clear();
 
        if (CartInventory.Instance == null) return;
 
        GameObject bigPrefab           = CartInventory.Instance.GetBigItemPrefab();
        List<FurnitureItem> smallItems = CartInventory.Instance.GetCartItems();
 
        // Slot assignment:
        // Big item present  → big item at slot 0, small items at slots 1+
        // No big item       → small items fill slots 0+
        int nextSlot = 0;
 
        if (bigPrefab != null && slotPositions.Length > 0)
        {
            SpawnPreviewClone(bigPrefab, slotPositions[0], bigItemScale);
            nextSlot = 1;
        }
 
        for (int i = 0; i < smallItems.Count; i++)
        {
            if (nextSlot >= slotPositions.Length) break;
            Transform slot = slotPositions[nextSlot];
            if (slot == null) { nextSlot++; continue; }
 
            GameObject prefab = CartInventory.Instance.GetCartPrefab(i);
            if (prefab != null)
                SpawnPreviewClone(prefab, slot, smallItemScale);
 
            nextSlot++;
        }
    }
 
    void SpawnPreviewClone(GameObject prefab, Transform slot, Vector3 scale)
    {
        if (prefab == null || slot == null) return;
 
        GameObject preview = Instantiate(prefab);
        preview.transform.position   = slot.position + slotPositionOffset;
        preview.transform.rotation   = slot.rotation;
        preview.transform.localScale = scale;
 
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
            if (mb == null || mb is ShoppingCart3D) continue;
            mb.enabled = false;
        }
 
        dockedItems[preview] = slot;
        managedPreviews.Add(preview);
    }
}