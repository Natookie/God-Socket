using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Managers")]
    public SettingsManager settingsManager;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject tutorialPanel;
    public GameObject creditPanel;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
        }
    }

    public void ShowSettings()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
        }

        if (settingsManager != null)
        {
            settingsManager.LoadSettingsToUI();
        }
    }

    public void ShowTutorial()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        if (creditPanel != null)
        {
            creditPanel.SetActive(false);
        }
    }

    public void ShowCredit()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        if (creditPanel != null)
        {
            creditPanel.SetActive(true);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}