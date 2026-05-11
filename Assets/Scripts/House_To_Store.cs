using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTeleport : MonoBehaviour
{
    [Header("Teleport Settings")]
    public string sceneToLoad = "Store";

    [Header("UI Prompt")]
    public GameObject interactPrompt;

    [Header("Detection")]
    public float detectionRadius = 2f; // tweak this in Inspector

    private bool playerInRange = false;

    void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void Update()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius
        );

        playerInRange = false;
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                playerInRange = true;
                break;
            }
        }

        if (interactPrompt != null)
            interactPrompt.SetActive(playerInRange);

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
            SceneManager.LoadScene(sceneToLoad);
    }

    // Visualize detection radius in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}