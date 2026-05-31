using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTeleport : Interactable
{
private void Awake()
{
int interactableLayer = LayerMask.NameToLayer("Interactable");

    if (gameObject.layer != interactableLayer)
    {
        Debug.LogWarning(gameObject.name + " was not on the Interactable layer. Setting it automatically.");
        gameObject.layer = interactableLayer;
    }
}

protected override void Interact()
{
    if (LevelManager.Instance != null)
    {
        LevelManager.Instance.LoadNextStore();
    }
    else
    {
        Debug.LogError("LevelManager is missing from the scene.");
    }
}
}