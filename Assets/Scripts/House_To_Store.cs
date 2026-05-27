using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTeleport : Interactable
{
    private void Awake()
    {
        // Get the integer ID of the layer
        int interactableLayer = LayerMask.NameToLayer("Interactable");

        // Enforce the requirement: Check if the layer matches
        if (gameObject.layer != interactableLayer)
        {
            Debug.LogWarning($"[{gameObject.name}] was not on the 'Interactable' layer. Setting it automatically.");
            gameObject.layer = interactableLayer;
        }
    }

    protected override void Interact()
    {
        SceneManager.LoadScene("Store");
    }
}