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
    public Vector3 bigItemScale  = Vector3.one * 0.3f;
    public Vector3 smallItemScale = Vector3.one * 0.15f;

    [Header("Basket Preview Anchors")]
    public Vector3 basketCenterLocal = new Vector3(0f, 0.28f, 0f);
    public Vector3 smallSlotSpacing = new Vector3(0.22f, 0f, 0.18f);

    private Transform contentsRoot;
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
        FollowCartContents();
    }

    void LateUpdate()
    {
        FollowCartContents();
    }

    public void RefreshCartVisuals()
    {
        EnsureContentsRoot();

        foreach (var obj in previewObjects)
            if (obj != null) Destroy(obj);
        previewObjects.Clear();

        if (CartInventory.Instance == null) return;

        GameObject bigPrefab   = CartInventory.Instance.GetBigItemPrefab();
        List<FurnitureItem> smallItems = CartInventory.Instance.GetCartItems();

        if (bigPrefab != null)
        {
            Transform anchor = CreatePreviewAnchor("BigItemAnchor", Vector3.zero);
            SpawnPreview(bigPrefab, anchor, bigItemScale);
        }

        int startSlot = bigPrefab != null ? 5 : 0;
        int maxSmall  = bigPrefab != null ? 1 : 6;

        for (int i = 0; i < Mathf.Min(smallItems.Count, maxSmall); i++)
        {
            int slotIndex = startSlot + i;
            Transform slot = CreatePreviewAnchor($"SmallItemAnchor_{slotIndex}", GetBasketSlotLocalPosition(slotIndex));
            GameObject prefab = CartInventory.Instance.GetCartPrefab(i);

            if (slot != null && prefab != null)
                SpawnPreview(prefab, slot, smallItemScale);
        }
    }

    // Parent previews to slots so they always move with the cart.
    void SpawnPreview(GameObject prefab, Transform slot, Vector3 scale)
    {
        if (prefab == null || slot == null) return;

        GameObject preview = Instantiate(prefab, slot);
        SetupPreviewObject(preview, slot, scale);
        previewObjects.Add(preview);
    }

    void SetupPreviewObject(GameObject preview, Transform slot, Vector3 scale)
    {
        preview.transform.SetParent(slot, false);
        preview.transform.localPosition = Vector3.zero;
        preview.transform.localRotation = Quaternion.identity;
        preview.transform.localScale    = scale;
        GroundPreviewOnSlot(preview, slot);
        DisablePreviewBehaviours(preview);

        foreach (var col in preview.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var rb in preview.GetComponentsInChildren<Rigidbody>(true))
        {
            bool wasKinematic = rb.isKinematic;
            if (wasKinematic)
                rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    Transform CreatePreviewAnchor(string anchorName, Vector3 localPosition)
    {
        var anchor = new GameObject(anchorName);
        anchor.transform.SetParent(EnsureContentsRoot(), false);
        anchor.transform.localPosition = localPosition;
        anchor.transform.localRotation = Quaternion.identity;
        anchor.transform.localScale = Vector3.one;
        previewObjects.Add(anchor);
        return anchor.transform;
    }

    Transform EnsureContentsRoot()
    {
        if (contentsRoot == null)
        {
            GameObject root = new GameObject("CartContentsRoot");
            contentsRoot = root.transform;
        }

        FollowCartContents();
        return contentsRoot;
    }

    void FollowCartContents()
    {
        if (contentsRoot == null) return;

        if (contentsRoot.parent != transform)
            contentsRoot.SetParent(transform, true);

        contentsRoot.SetPositionAndRotation(GetBasketCenterWorld(), GetContentRotation());
        contentsRoot.localScale = GetInverseCartScale();
    }

    void GroundPreviewOnSlot(GameObject preview, Transform slot)
    {
        if (preview == null || slot == null) return;

        Renderer[] renderers = preview.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        bool hasBounds = false;
        Bounds localBounds = new Bounds();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            Bounds rendererBounds = renderer.bounds;

            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 worldCorner = rendererBounds.center + Vector3.Scale(rendererBounds.extents, new Vector3(x, y, z));
                Vector3 localCorner = slot.InverseTransformPoint(worldCorner);

                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        if (!hasBounds) return;

        preview.transform.localPosition -= new Vector3(
            localBounds.center.x,
            localBounds.min.y,
            localBounds.center.z);
    }

    void DisablePreviewBehaviours(GameObject preview)
    {
        foreach (var behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
    }

    Vector3 GetBasketSlotLocalPosition(int index)
    {
        if (index == 5)
            return new Vector3(0f, 0f, -smallSlotSpacing.z);

        switch (index)
        {
            case 0: return new Vector3(-smallSlotSpacing.x, 0f,  smallSlotSpacing.z);
            case 1: return new Vector3( smallSlotSpacing.x, 0f,  smallSlotSpacing.z);
            case 2: return new Vector3(-smallSlotSpacing.x, 0f,  0f);
            case 3: return new Vector3( smallSlotSpacing.x, 0f,  0f);
            case 4: return new Vector3(0f, 0f, -smallSlotSpacing.z);
            default: return Vector3.zero;
        }
    }

    Vector3 GetBasketCenterWorld()
    {
        if (slotPositions != null)
        {
            Vector3 center = Vector3.zero;
            int count = 0;

            foreach (Transform slot in slotPositions)
            {
                if (slot == null) continue;
                center += slot.position;
                count++;
            }

            if (count > 0)
                return center / count;
        }

        return transform.TransformPoint(basketCenterLocal);
    }

    Quaternion GetContentRotation()
    {
        if (playerTransform != null)
            return Quaternion.LookRotation(playerTransform.forward, Vector3.up);

        return transform.rotation;
    }

    Vector3 GetInverseCartScale()
    {
        Vector3 scale = transform.lossyScale;

        return new Vector3(
            Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
            Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y,
            Mathf.Approximately(scale.z, 0f) ? 1f : 1f / scale.z);
    }
}
