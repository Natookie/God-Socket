using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameSceneSettingsApplier : MonoBehaviour
{
    [Header("Audio")]
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

    private void Start()
    {
        ApplySettings();
    }

    public void ApplySettings()
    {
        ApplyBGMVolume();
        ApplyScreenSettings();
        ApplyMotionBlur();

        Debug.Log("GameScene settings applied.");
    }

    private void ApplyBGMVolume()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = GameSettings.BGMVolume;
            Debug.Log("GameScene BGM Volume: " + GameSettings.BGMVolume);
        }
    }

    private void ApplyScreenSettings()
    {
        Screen.fullScreen = GameSettings.Fullscreen;

        int index = GameSettings.ResolutionIndex;
        index = Mathf.Clamp(index, 0, resolutions.Length - 1);

        Vector2Int resolution = resolutions[index];

        Screen.SetResolution(
            resolution.x,
            resolution.y,
            GameSettings.Fullscreen
        );

        Debug.Log("GameScene Fullscreen: " + GameSettings.Fullscreen);
        Debug.Log("GameScene Resolution: " + resolution.x + "x" + resolution.y);
    }

    private void ApplyMotionBlur()
    {
        if (globalVolume == null)
        {
            Debug.LogWarning("GameScene Global Volume is missing.");
            return;
        }

        if (globalVolume.profile.TryGet(out MotionBlur motionBlur))
        {
            motionBlur.active = GameSettings.MotionBlur;
            Debug.Log("GameScene Motion Blur: " + motionBlur.active);
        }
        else
        {
            Debug.LogWarning("Motion Blur not found inside GameScene Global Volume profile.");
        }
    }
}