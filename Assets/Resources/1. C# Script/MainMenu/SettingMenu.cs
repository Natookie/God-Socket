using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI")]
    public Slider sensitivitySlider;
    public Slider audioSlider;
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;
    public Toggle motionBlurToggle;

    [Header("Motion Blur")]
    public GameObject motionBlurObject;

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions = new List<Resolution>();

    private void Start()
    {
        SetupSensitivity();
        SetupAudio();
        SetupResolutionDropdown();
        SetupFullscreen();
        SetupMotionBlur();

        RegisterEvents();

        // Apply saved settings when menu starts
        SetAudio(audioSlider.value);
        SetMotionBlur(motionBlurToggle.isOn);
        ApplyResolutionNow();
    }

    private void SetupSensitivity()
    {
        sensitivitySlider.minValue = 0.5f;
        sensitivitySlider.maxValue = 10f;

        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        sensitivitySlider.SetValueWithoutNotify(savedSensitivity);
    }

    private void SetupAudio()
    {
        audioSlider.minValue = 0f;
        audioSlider.maxValue = 1f;

        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        audioSlider.SetValueWithoutNotify(savedVolume);
    }

    private void SetupFullscreen()
    {
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 0) == 1;

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        }
    }

    private void SetupMotionBlur()
    {
        bool motionBlurOn = PlayerPrefs.GetInt("MotionBlur", 1) == 1;

        if (motionBlurToggle != null)
        {
            motionBlurToggle.SetIsOnWithoutNotify(motionBlurOn);
        }

        if (motionBlurObject != null)
        {
            motionBlurObject.SetActive(motionBlurOn);
        }
    }

    private void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        filteredResolutions.Clear();

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution resolution = resolutions[i];

            bool alreadyAdded = false;

            for (int j = 0; j < filteredResolutions.Count; j++)
            {
                if (filteredResolutions[j].width == resolution.width &&
                    filteredResolutions[j].height == resolution.height)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
            {
                filteredResolutions.Add(resolution);

                string option = resolution.width + " x " + resolution.height;
                options.Add(option);

                if (resolution.width == Screen.currentResolution.width &&
                    resolution.height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);

        int savedResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);

        if (savedResolutionIndex >= 0 && savedResolutionIndex < filteredResolutions.Count)
        {
            resolutionDropdown.SetValueWithoutNotify(savedResolutionIndex);
        }
        else
        {
            resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
        }

        resolutionDropdown.RefreshShownValue();
    }

    private void RegisterEvents()
    {
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        audioSlider.onValueChanged.AddListener(SetAudio);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (motionBlurToggle != null)
        {
            motionBlurToggle.onValueChanged.AddListener(SetMotionBlur);
        }
    }

    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();

        PlayerRotation playerRotation = FindFirstObjectByType<PlayerRotation>();

        if (playerRotation != null)
        {
            playerRotation.LoadSensitivity();
        }

        Debug.Log("Sensitivity changed to: " + value);
    }

    public void SetAudio(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();

        AudioListener.volume = value;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBGMVolume(value);
        }

        Debug.Log("Audio changed to: " + value);
    }

    public void SetResolution(int resolutionIndex)
    {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.Save();

        ApplyResolutionNow();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        ApplyResolutionNow();
    }

    private void ApplyResolutionNow()
    {
        int resolutionIndex = resolutionDropdown.value;

        if (resolutionIndex < 0 || resolutionIndex >= filteredResolutions.Count)
        {
            Debug.LogWarning("Invalid resolution index.");
            return;
        }

        Resolution resolution = filteredResolutions[resolutionIndex];

        bool fullscreen = false;

        if (fullscreenToggle != null)
        {
            fullscreen = fullscreenToggle.isOn;
        }

        FullScreenMode screenMode = fullscreen
            ? FullScreenMode.ExclusiveFullScreen
            : FullScreenMode.Windowed;

        Screen.SetResolution(
            resolution.width,
            resolution.height,
            screenMode
        );

        Debug.Log("Resolution changed to: " + resolution.width + " x " + resolution.height);
        Debug.Log("Fullscreen: " + fullscreen);
    }

    public void SetMotionBlur(bool isOn)
    {
        PlayerPrefs.SetInt("MotionBlur", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (motionBlurObject != null)
        {
            motionBlurObject.SetActive(isOn);
        }

        Debug.Log("Motion Blur: " + isOn);
    }
}