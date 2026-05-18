using UnityEngine;
using TMPro; // Using TextMeshPro for modern Unity UI. Change to UnityEngine.UI if using standard Text.

// This forces Unity to add a BoxCollider when you attach this script
[RequireComponent(typeof(BoxCollider))]
public abstract class RectangleZone : MonoBehaviour
{
    [Header("Zone Dimensions")]
    public float length = 10f; // Z-axis
    public float width = 10f;  // X-axis
    public float height = 5f;  // Y-axis

    [Header("Zone Information")]
    public string zoneName;
    public TextMeshProUGUI zoneUIText; // Drag your UI Text component here in the inspector

    [Header("Spawning Settings")]
    public GameObject[] possiblePrefabs;
    public int numberOfPrefabsToSpawn = 5;

    private BoxCollider zoneCollider;

    // Virtual allows child classes to override or extend this method
    protected virtual void Awake()
    {
        // Automatically scale the collider to match your variables
        zoneCollider = GetComponent<BoxCollider>();
        zoneCollider.isTrigger = true;
        zoneCollider.size = new Vector3(width, height, length);
    }

    protected virtual void Start()
    {
        SpawnPrefabs();

        // Ensure UI text is empty/hidden on startup
        if (zoneUIText != null)
        {
            zoneUIText.text = "";
        }
    }

    private void SpawnPrefabs()
    {
        // Don't run if the array is empty
        if (possiblePrefabs == null || possiblePrefabs.Length == 0) return;

        for (int i = 0; i < numberOfPrefabsToSpawn; i++)
        {
            // 1. Pick a random prefab from the array
            int randomIndex = Random.Range(0, possiblePrefabs.Length);
            GameObject prefabToSpawn = possiblePrefabs[randomIndex];

            // 2. Calculate a random local position inside the rectangular prism
            Vector3 randomLocalPos = new Vector3(
                Random.Range(-width / 2f, width / 2f),
                Random.Range(-height / 2f, height / 2f),
                Random.Range(-length / 2f, length / 2f)
            );

            // 3. Convert local position to world space (so it accounts for the Zone's rotation/position)
            Vector3 spawnPosition = transform.TransformPoint(randomLocalPos);

            // 4. Instantiate the prefab as a child of this zone
            Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, transform);
        }
    }

    // Triggered when another collider enters this zone's collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the zone is the player
        if (other.CompareTag("Player"))
        {
            if (zoneUIText != null)
            {
                zoneUIText.text = "Current Zone: " + zoneName;
            }
        }
    }

    // Optional: Clear the UI text when the player leaves the zone
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (zoneUIText != null)
            {
                zoneUIText.text = "";
            }
        }
    }

    // This helps you visualize the rectangular prism in the Unity Editor!
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f); // Transparent green
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, new Vector3(width, height, length));
    }
}