using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class BustedScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bustedPanel;
    public TextMeshProUGUI bustedText;
    public UnityEngine.UI.Button mainMenuButton;

    [Header("Settings")]
    public float showDelay = 0.5f;
    public string mainMenuSceneName = "Main Menu";

    private bool hasBeenBusted = false;
    private InputManager inputManager;

    private void Start()
    {
        inputManager = FindAnyObjectByType<InputManager>();

        if (mainMenuButton == null)
            mainMenuButton = bustedPanel.transform.Find("MainMenuButton")
                .GetComponent<UnityEngine.UI.Button>();

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        if (bustedPanel != null)
            bustedPanel.SetActive(false);
    }

    public void ShowBusted()
    {
        if (hasBeenBusted) return;
        hasBeenBusted = true;
        StartCoroutine(ShowBustedRoutine());
    }

    private IEnumerator ShowBustedRoutine()
    {
        Time.timeScale = 0f;
        if (inputManager != null) inputManager.SetPlayerControls(false);

        yield return new WaitForSecondsRealtime(showDelay);

        if (bustedPanel != null)
            bustedPanel.SetActive(true);

        Debug.Log("BUSTED!");
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}