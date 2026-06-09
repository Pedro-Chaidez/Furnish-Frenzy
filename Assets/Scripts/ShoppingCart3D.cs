using System.Collections.Generic;
using UnityEngine;

public class ShoppingCart3D : MonoBehaviour
{
    public static ShoppingCart3D Instance { get; private set; }

    [Header("Cart Follow")]
    public Transform playerTransform;

    [Tooltip("X = side offset, Y = height, Z = distance in front/behind player")]
    public Vector3 offset = new Vector3(0f, 0.6f, 2.2f);

    [Tooltip("Lower = less buffer, higher = floatier")]
    public float positionSmoothTime = 0.08f;

    [Tooltip("Lower = turns faster, higher = smoother turning")]
    public float forwardSmoothTime = 0.06f;

    [Tooltip("How fast the cart rotates to face the player direction")]
    public float rotationSpeed = 18f;

    [Tooltip("Set this to Y = 180 if your cart faces backward")]
    public Vector3 modelRotationOffset = Vector3.zero;

    public bool addColliderIfMissing = true;

    [Header("Item Slots")]
    public Transform[] slotPositions = new Transform[6];

    [Header("Preview Scale")]
    public Vector3 bigItemScale = Vector3.one * 0.4f;
    public Vector3 smallItemScale = Vector3.one * 0.25f;

    [Header("Slot Local Offset")]
    [Tooltip("Local offset applied inside each slot. Negative Y sinks items into basket.")]
    public Vector3 slotLocalPositionOffset = new Vector3(0f, -0.5f, 0f);

    private Vector3 _posVelocity = Vector3.zero;
    private Vector3 _smoothForward = Vector3.forward;
    private Vector3 _fwdVelocity = Vector3.zero;

    private readonly List<GameObject> managedPreviews = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (addColliderIfMissing && GetComponent<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
        }
    }

    void OnEnable()
    {
        CartInventory.InventoryChanged += RefreshCartVisuals;
    }

    void OnDisable()
    {
        CartInventory.InventoryChanged -= RefreshCartVisuals;
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            Vector3 flatForward = GetFlatForward(playerTransform);
            _smoothForward = flatForward;

            Vector3 startRight = Vector3.Cross(Vector3.up, _smoothForward).normalized;

            transform.position = playerTransform.position
                               + _smoothForward * offset.z
                               + startRight * offset.x
                               + Vector3.up * offset.y;

            transform.rotation = Quaternion.LookRotation(_smoothForward, Vector3.up)
                               * Quaternion.Euler(modelRotationOffset);
        }

        RefreshCartVisuals();
    }

    void LateUpdate()
{
    if (playerTransform == null)
        return;

    Vector3 desiredForward = GetFlatForward(playerTransform);

    // Smooth only the direction, not the position
    _smoothForward = Vector3.SmoothDamp(
        _smoothForward,
        desiredForward,
        ref _fwdVelocity,
        forwardSmoothTime,
        Mathf.Infinity,
        Time.deltaTime
    );

    if (_smoothForward.sqrMagnitude < 0.001f)
        _smoothForward = desiredForward;

    _smoothForward.Normalize();

    Vector3 right = Vector3.Cross(Vector3.up, _smoothForward).normalized;

    Vector3 targetPosition = playerTransform.position
                           + _smoothForward * offset.z
                           + right * offset.x
                           + Vector3.up * offset.y;

    // IMPORTANT:
    // Direct position follow removes forward-movement delay/buffer.
    transform.position = targetPosition;

    Quaternion targetRotation = Quaternion.LookRotation(_smoothForward, Vector3.up)
                              * Quaternion.Euler(modelRotationOffset);

    transform.rotation = Quaternion.Slerp(
        transform.rotation,
        targetRotation,
        1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
    );
}

    private Vector3 GetFlatForward(Transform target)
    {
        Vector3 forward = target.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = transform.forward;

        forward.Normalize();
        return forward;
    }

    public void RefreshCartVisuals()
    {
        foreach (GameObject obj in managedPreviews)
        {
            if (obj != null)
                Destroy(obj);
        }

        managedPreviews.Clear();

        if (CartInventory.Instance == null)
            return;

        GameObject bigPrefab = CartInventory.Instance.GetBigItemPrefab();
        List<FurnitureItem> smallItems = CartInventory.Instance.GetCartItems();

        int nextSlot = 0;

        if (bigPrefab != null && slotPositions.Length > 0 && slotPositions[0] != null)
        {
            SpawnPreviewClone(bigPrefab, slotPositions[0], bigItemScale);
            nextSlot = 1;
        }

        for (int i = 0; i < smallItems.Count; i++)
        {
            if (nextSlot >= slotPositions.Length)
                break;

            Transform slot = slotPositions[nextSlot];

            if (slot != null)
            {
                GameObject prefab = CartInventory.Instance.GetCartPrefab(i);

                if (prefab != null)
                    SpawnPreviewClone(prefab, slot, smallItemScale);
            }

            nextSlot++;
        }
    }

    private void SpawnPreviewClone(GameObject prefab, Transform slot, Vector3 scale)
    {
        if (prefab == null || slot == null)
            return;

        GameObject preview = Instantiate(prefab, slot);

        preview.transform.localPosition = slotLocalPositionOffset;
        preview.transform.localRotation = Quaternion.identity;
        preview.transform.localScale = scale;

        foreach (Rigidbody rb in preview.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif

            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        foreach (Collider col in preview.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        foreach (MonoBehaviour mb in preview.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null)
                continue;

            if (mb is ShoppingCart3D)
                continue;

            mb.enabled = false;
        }

        managedPreviews.Add(preview);
    }
}