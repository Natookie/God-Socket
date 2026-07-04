using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GameSettings
{
    public static float BGMVolume
    {
        get => PlayerPrefs.GetFloat("BGMVolume", 1f);
        set => PlayerPrefs.SetFloat("BGMVolume", Mathf.Clamp01(value));
    }

    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat("SFXVolume", 1f);
        set => PlayerPrefs.SetFloat("SFXVolume", Mathf.Clamp01(value));
    }

    public static float Sensitivity
    {
        get => PlayerPrefs.GetFloat("Sensitivity", 1f);
        set => PlayerPrefs.SetFloat("Sensitivity", Mathf.Clamp(value, 0.1f, 5f));
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        set => PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
    }

    public static bool MotionBlur
    {
        get => PlayerPrefs.GetInt("MotionBlur", 1) == 1;
        set => PlayerPrefs.SetInt("MotionBlur", value ? 1 : 0);
    }

    public static int ResolutionIndex
    {
        get => PlayerPrefs.GetInt("ResolutionIndex", 0);
        set => PlayerPrefs.SetInt("ResolutionIndex", value);
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }
}