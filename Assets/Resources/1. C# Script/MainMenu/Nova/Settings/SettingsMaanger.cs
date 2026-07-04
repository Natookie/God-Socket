using NovaSamples.UIControls;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsManager : MonoBehaviour
{
    private bool isLoadingSettings;

    [Header("Nova Sliders")]
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Slider sensSlider;

    [Header("Audio Sources")]
    public AudioSource bgmAudioSource;

    [Header("Post Processing")]
    public Volume globalVolume;

    [Header("Resolution Options")]
    public Vector2Int[] resolutions =
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1366, 768),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080)
    };

    private MotionBlur motionBlur;

    private void Start()
    {
        ApplyAllSettings();
    }

    public void LoadSettingsToUI()
    {
        isLoadingSettings = true;

        if (bgmSlider != null)
        {
            bgmSlider.Value = GameSettings.BGMVolume;
        }

        if (sfxSlider != null)
        {
            sfxSlider.Value = GameSettings.SFXVolume;
        }

        if (sensSlider != null)
        {
            sensSlider.Value = GameSettings.Sensitivity;
        }

        Debug.Log("Loaded BGM Volume: " + GameSettings.BGMVolume);
        Debug.Log("Loaded SFX Volume: " + GameSettings.SFXVolume);
        Debug.Log("Loaded Sensitivity: " + GameSettings.Sensitivity);

        isLoadingSettings = false;
    }

    public void SetBGMVolume(float value)
    {
        if (isLoadingSettings)
        {
            return;
        }

        value = Mathf.Clamp01(value);

        GameSettings.BGMVolume = value;

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = value;
        }

        GameSettings.Save();

        Debug.Log("Saved BGM Volume: " + value);
    }

    public void SetSFXVolume(float value)
    {
        if (isLoadingSettings)
        {
            return;
        }

        value = Mathf.Clamp01(value);

        GameSettings.SFXVolume = value;
        GameSettings.Save();

        Debug.Log("Saved SFX Volume: " + value);
    }

    public void SetSensitivity(float value)
    {
        if (isLoadingSettings)
        {
            return;
        }

        GameSettings.Sensitivity = value;
        GameSettings.Save();

        Debug.Log("Saved Sensitivity: " + value);
    }

    public void SetFullscreen(bool value)
    {
        if (isLoadingSettings)
        {
            return;
        }

        GameSettings.Fullscreen = value;
        Screen.fullScreen = value;
        GameSettings.Save();

        Debug.Log("Saved Fullscreen: " + value);
    }

    public void SetMotionBlur(bool value)
    {
        if (isLoadingSettings)
        {
            return;
        }

        GameSettings.MotionBlur = value;
        ApplyMotionBlur(value);
        GameSettings.Save();

        Debug.Log("Saved Motion Blur: " + value);
    }

    public void SetResolution(int index)
    {
        if (isLoadingSettings)
        {
            return;
        }

        if (index < 0 || index >= resolutions.Length)
        {
            Debug.LogWarning("Invalid resolution index: " + index);
            return;
        }

        GameSettings.ResolutionIndex = index;

        Vector2Int resolution = resolutions[index];

        Screen.SetResolution(
            resolution.x,
            resolution.y,
            GameSettings.Fullscreen
        );

        GameSettings.Save();

        Debug.Log("Saved Resolution: " + resolution.x + "x" + resolution.y);
    }

    public void SetResolutionFromDropdown(string resolutionText)
    {
        if (isLoadingSettings)
        {
            return;
        }

        int index = 3;

        switch (resolutionText)
        {
            case "1280 x 720":
                index = 0;
                break;

            case "1366 x 768":
                index = 1;
                break;

            case "1600 x 900":
                index = 2;
                break;

            case "1920 x 1080":
                index = 3;
                break;

            default:
                Debug.LogWarning("Unknown resolution option: " + resolutionText);
                return;
        }

        SetResolution(index);
    }

    public void ApplyAllSettings()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = GameSettings.BGMVolume;
        }

        Screen.fullScreen = GameSettings.Fullscreen;

        ApplyResolution(GameSettings.ResolutionIndex, false);

        ApplyMotionBlur(GameSettings.MotionBlur);
    }

    private void ApplyResolution(int index, bool save)
    {
        if (index < 0 || index >= resolutions.Length)
        {
            Debug.LogWarning("Invalid resolution index: " + index);
            return;
        }

        Vector2Int resolution = resolutions[index];

        Screen.SetResolution(
            resolution.x,
            resolution.y,
            GameSettings.Fullscreen
        );

        if (save)
        {
            GameSettings.ResolutionIndex = index;
            GameSettings.Save();

            Debug.Log("Saved Resolution: " + resolution.x + "x" + resolution.y);
        }
    }

    private void ApplyMotionBlur(bool enabled)
    {
        if (globalVolume == null)
        {
            Debug.LogWarning("Global Volume is missing.");
            return;
        }

        if (globalVolume.profile.TryGet(out motionBlur))
        {
            motionBlur.active = enabled;
        }
        else
        {
            Debug.LogWarning("Motion Blur not found inside Global Volume profile.");
        }
    }
}