using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
 
public class BustedScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject bustedPanel;
    public TextMeshProUGUI bustedText;
    public UnityEngine.UI.Button restartButton;
    public UnityEngine.UI.Button mainMenuButton;
 
    [Header("Settings")]
    public float showDelay = 0.5f;
    public string mainMenuSceneName = "Main Menu";
 
    private bool hasBeenBusted = false;
 
    private void Start()
    {
        if (bustedPanel != null)
        {
            bustedPanel.SetActive(false);
        }
 
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartLevel);
        }
 
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
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
 
        yield return new WaitForSecondsRealtime(showDelay);
 
        if (bustedPanel != null)
        {
            bustedPanel.SetActive(true);
        }
 
        Debug.Log("BUSTED!");
    }
 
    private void RestartLevel()
    {
        Time.timeScale = 1f;
        hasBeenBusted = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
 
    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}