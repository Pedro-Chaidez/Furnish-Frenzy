using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTeleport : Interactable
{
    protected override void Interact()
    {
        SceneManager.LoadScene("Store");
    }
}