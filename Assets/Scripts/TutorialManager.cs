using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Drag your tutorial UI panels here in order (0 is first)")]
    public List<GameObject> tutorialPanels;

    [Header("References")]
    public InputManager inputManager; // Drag your player here

    private int currentPanelIndex = 0;

    private void Start()
    {
        // Check if the player has already finished the tutorial in a previous session or scene
        if (PlayerPrefs.GetInt("TutorialCompleted", 0) == 1)
        {
            // The tutorial is already done. Make sure everything is off and let them play.
            EndTutorial();
            return;
        }

        // Otherwise, start the tutorial
        StartTutorial();
    }

    public void StartTutorial()
    {
        // 1. Tell the InputManager to disable player movement/looking and show the mouse
        if (inputManager != null)
        {
            inputManager.SetPlayerControls(false);
        }

        // 2. Show the first panel
        currentPanelIndex = 0;
        UpdatePanels();
    }

    // Call this from your "Next" UI Button
    public void NextPanel()
    {
        if (currentPanelIndex < tutorialPanels.Count - 1)
        {
            currentPanelIndex++;
            UpdatePanels();
        }
    }

    // Call this from your "Previous" UI Button
    public void PreviousPanel()
    {
        if (currentPanelIndex > 0)
        {
            currentPanelIndex--;
            UpdatePanels();
        }
    }

    // Call this from your "Close" or "Finish" UI Button on the last panel
    public void CloseTutorial()
    {
        // Save the completion state so it never shows up again
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        EndTutorial();
    }

    private void UpdatePanels()
    {
        // Turn on ONLY the current panel, turn off all others
        for (int i = 0; i < tutorialPanels.Count; i++)
        {
            tutorialPanels[i].SetActive(i == currentPanelIndex);
        }
    }

    private void EndTutorial()
    {
        // 1. Tell the InputManager to restore player movement and lock the mouse FIRST
        if (inputManager != null)
        {
            inputManager.SetPlayerControls(true);
        }

        // 2. Turn off this entire GameObject (This hides the panels AND the buttons at once)
        gameObject.SetActive(false);
    }
}