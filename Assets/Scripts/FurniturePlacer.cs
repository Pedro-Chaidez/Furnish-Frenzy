// FurniturePlacer.cs
// Attach to the Player in the House scene.
// Reads items saved by DoorExit (via PlayerPrefs) and lets the player
// place each one: left-click to confirm, right-click to skip.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FurniturePlacer : MonoBehaviour
{
    [Header("Item Registry")]
    [Tooltip("All FurnitureItem ScriptableObjects — same list as HouseItemSpawner.")]
    public List<FurnitureItem> allItems = new List<FurnitureItem>();

    [Header("Placement")]
    public LayerMask floorLayerMask;
    public Material ghostMaterial;
    public KeyCode placeKey  = KeyCode.Mouse0;
    public KeyCode cancelKey = KeyCode.Mouse1;

    [Header("Ghost")]
    public float maxRayDistance = 20f;

    // ── Runtime state ──────────────────────────────────────────────────────

    private struct PendingItem
    {
        public FurnitureItem furnitureItem;
        public GameObject    exactPrefab;    // the specific variant from the store
    }

    private List<PendingItem> itemsToPlace = new List<PendingItem>();
    private int         currentIndex = 0;
    private GameObject  ghostObject;
    private bool        isPlacing = false;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    void Start()
    {
        LoadFromPlayerPrefs();

        if (itemsToPlace.Count > 0)
            BeginPlacing(0);
        else
            Debug.Log("FurniturePlacer: No items to place.");
    }

    void Update()
    {
        if (!isPlacing || itemsToPlace.Count == 0) return;

        MoveGhost();

        if (Input.GetKeyDown(placeKey) && !IsPointerOverUI())
            ConfirmPlacement();

        if (Input.GetKeyDown(cancelKey))
            SkipCurrentItem();
    }

    // ── Load saved items from PlayerPrefs ──────────────────────────────────

    void LoadFromPlayerPrefs()
    {
        int cartCount = PlayerPrefs.GetInt("PendingCartCount", 0);
        int handCount = PlayerPrefs.GetInt("PendingHandCount", 0);

        for (int i = 0; i < cartCount; i++)
        {
            PendingItem? item = BuildPendingItem(
                PlayerPrefs.GetString($"Cart_{i}_Name", ""),
                PlayerPrefs.GetString($"Cart_{i}_Prefab", ""));

            if (item.HasValue) itemsToPlace.Add(item.Value);

            PlayerPrefs.DeleteKey($"Cart_{i}_Name");
            PlayerPrefs.DeleteKey($"Cart_{i}_Prefab");
            PlayerPrefs.DeleteKey($"Cart_{i}_X");
            PlayerPrefs.DeleteKey($"Cart_{i}_Y");
            PlayerPrefs.DeleteKey($"Cart_{i}_Z");
        }

        for (int i = 0; i < handCount; i++)
        {
            PendingItem? item = BuildPendingItem(
                PlayerPrefs.GetString($"Hand_{i}_Name", ""),
                PlayerPrefs.GetString($"Hand_{i}_Prefab", ""));

            if (item.HasValue) itemsToPlace.Add(item.Value);

            PlayerPrefs.DeleteKey($"Hand_{i}_Name");
            PlayerPrefs.DeleteKey($"Hand_{i}_Prefab");
            PlayerPrefs.DeleteKey($"Hand_{i}_X");
            PlayerPrefs.DeleteKey($"Hand_{i}_Y");
            PlayerPrefs.DeleteKey($"Hand_{i}_Z");
        }

        PlayerPrefs.DeleteKey("PendingCartCount");
        PlayerPrefs.DeleteKey("PendingHandCount");
        PlayerPrefs.Save();

        Debug.Log($"FurniturePlacer: Loaded {itemsToPlace.Count} items to place.");
    }

    PendingItem? BuildPendingItem(string itemName, string prefabName)
    {
        if (string.IsNullOrEmpty(itemName)) return null;

        // Find the ScriptableObject by category name
        FurnitureItem match = allItems.Find(f =>
            f != null && f.itemName == itemName);

        if (match == null)
        {
            Debug.LogWarning($"FurniturePlacer: No FurnitureItem found for '{itemName}'.");
            return null;
        }

        // Find the exact prefab by name
        GameObject exactPrefab = null;
        if (!string.IsNullOrEmpty(prefabName) && match.prefabVariants != null)
        {
            foreach (var variant in match.prefabVariants)
            {
                if (variant != null && variant.name == prefabName)
                {
                    exactPrefab = variant;
                    break;
                }
            }
        }

        // Fall back to random if the exact prefab wasn't found
        if (exactPrefab == null)
        {
            Debug.LogWarning($"FurniturePlacer: Couldn't find prefab '{prefabName}' " +
                             $"for '{itemName}', falling back to random.");
            exactPrefab = match.GetRandomVariant();
        }

        if (exactPrefab == null)
        {
            Debug.LogWarning($"FurniturePlacer: '{itemName}' has no prefab variants.");
            return null;
        }

        return new PendingItem { furnitureItem = match, exactPrefab = exactPrefab };
    }

    // ── Ghost preview ──────────────────────────────────────────────────────

    void BeginPlacing(int index)
    {
        if (index >= itemsToPlace.Count) { FinishPlacing(); return; }

        currentIndex = index;
        isPlacing    = true;

        DestroyGhost();

        PendingItem pending = itemsToPlace[currentIndex];
        ghostObject = Instantiate(pending.exactPrefab);
        ApplyGhostMaterial(ghostObject);

        foreach (var col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        Rigidbody rb = ghostObject.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.constraints = RigidbodyConstraints.FreezeAll; }

        Debug.Log($"Placing: {pending.furnitureItem.itemName} " +
                  $"[{pending.exactPrefab.name}]  " +
                  $"({currentIndex + 1}/{itemsToPlace.Count})");
    }

    void MoveGhost()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, floorLayerMask))
        {
            if (ghostObject != null)
            {
                FurnitureItem item = itemsToPlace[currentIndex].furnitureItem;
                ghostObject.transform.position = hit.point + item.defaultPlacementOffset;
                ghostObject.transform.up       = hit.normal;
            }
        }
    }

    void ConfirmPlacement()
    {
        if (ghostObject == null) return;

        PendingItem pending = itemsToPlace[currentIndex];

        // Spawn the exact same prefab at the ghost's position
        GameObject placed = Instantiate(
            pending.exactPrefab,
            ghostObject.transform.position,
            ghostObject.transform.rotation);

        Rigidbody rb = placed.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.constraints = RigidbodyConstraints.FreezeAll; }

        DestroyGhost();
        isPlacing = false;

        itemsToPlace.RemoveAt(currentIndex);

        if (itemsToPlace.Count > 0)
            BeginPlacing(0);
        else
            FinishPlacing();
    }

    void SkipCurrentItem()
    {
        DestroyGhost();
        isPlacing = false;

        int next = (currentIndex + 1) % itemsToPlace.Count;
        BeginPlacing(next);
    }

    void FinishPlacing()
    {
        isPlacing = false;
        DestroyGhost();
        Debug.Log("FurniturePlacer: All items placed!");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    void DestroyGhost()
    {
        if (ghostObject != null) Destroy(ghostObject);
        ghostObject = null;
    }

    void ApplyGhostMaterial(GameObject go)
    {
        if (ghostMaterial == null) return;
        foreach (var rend in go.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
            rend.materials = mats;
        }
    }

    bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}