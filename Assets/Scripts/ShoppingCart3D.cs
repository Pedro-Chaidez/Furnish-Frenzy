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
    public Transform smallItemSlot;
 
    [Header("Preview Scale")]
    public Vector3 bigItemScale  = Vector3.one * 0.5f;
    public Vector3 smallItemScale = Vector3.one * 0.25f;
 
    private List<GameObject> previewObjects = new List<GameObject>();
 
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
 
        transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followSpeed);
 
        Quaternion targetRot = Quaternion.LookRotation(playerTransform.forward, Vector3.up)
                             * Quaternion.Euler(modelRotationOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }
 
    public void RefreshCartVisuals()
    {
        foreach (var obj in previewObjects)
            if (obj != null) Destroy(obj);
        previewObjects.Clear();
 
        if (CartInventory.Instance == null) return;
 
        // Use the prefab stored in CartInventory — the exact one picked in the store
        GameObject bigPrefab = CartInventory.Instance.GetBigItemPrefab();
        List<FurnitureItem> smallItems = CartInventory.Instance.GetCartItems();
 
        if (bigPrefab != null)
        {
            Transform anchor = bigItemSlot ?? GetBigItemCenter();
            SpawnPreview(bigPrefab, anchor.position, anchor.rotation, bigItemScale);
        }
 
        if (smallItems.Count > 0)
        {
            Transform slot = smallItemSlot ?? GetSmallItemAnchor(bigPrefab != null);
            if (slot != null)
            {
                // Use the stored prefab for slot 0
                GameObject smallPrefab = CartInventory.Instance.GetCartPrefab(0);
                if (smallPrefab != null)
                    SpawnPreview(smallPrefab, slot.position, slot.rotation, smallItemScale, slot);
            }
        }
    }
 
    void SpawnPreview(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale, Transform parent = null)
    {
        GameObject preview = Instantiate(prefab, pos, rot, parent != null ? parent : transform);
        preview.transform.localPosition = parent != null ? Vector3.zero : preview.transform.localPosition;
        preview.transform.localRotation = parent != null ? Quaternion.identity : preview.transform.localRotation;
        preview.transform.localScale    = scale;
 
        foreach (var col in preview.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var rb in preview.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
 
        previewObjects.Add(preview);
    }
 
    Transform GetBigItemCenter()
    {
        if (slotPositions == null || slotPositions.Length == 0) return transform;
        Vector3 center = Vector3.zero;
        int count = 0;
        for (int i = 0; i < Mathf.Min(5, slotPositions.Length); i++)
        {
            if (slotPositions[i] == null) continue;
            center += slotPositions[i].position;
            count++;
        }
        if (count == 0) return transform;
 
        var pivot = new GameObject("_BigItemPivot");
        pivot.transform.SetParent(transform);
        pivot.transform.position = center / count;
        previewObjects.Add(pivot);
        return pivot.transform;
    }
 
    Transform GetSmallItemAnchor(bool hasBigItem)
    {
        if (smallItemSlot != null) return smallItemSlot;
        if (slotPositions != null && slotPositions.Length > 0)
        {
            if (hasBigItem && slotPositions.Length > 5) return slotPositions[5];
            if (slotPositions.Length > 1) return slotPositions[1];
            return slotPositions[0];
        }
        return transform;
    }
}