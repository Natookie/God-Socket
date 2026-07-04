using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameSFX
{
    PlayerGun,
    UfoGun,
    BuildingDestroyed,
    UfoDestroyed
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "GameScene";

    [Header("BGM")]
    public AudioSource bgmSource;
    public AudioClip mainBGM;
    public AudioClip gameBGM;

    [Header("SFX Clips")]
    public AudioClip projectileGunPlayer;
    public AudioClip projectileGunUfo;
    public AudioClip buildingDestroyed;
    public AudioClip ufoDestroyed;

    [Header("Loop SFX")]
    public AudioSource flyingWindSource;
    public AudioClip robotFlyingWind;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float defaultVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float flyingWindVolume = 0.7f;

    [Header("SFX Pool")]
    public int sfxSourceCount = 10;

    private readonly List<AudioSource> sfxSources = new List<AudioSource>();

    private const string MASTER_VOLUME_KEY = "MasterVolume";

    private void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupBGMSource();
        SetupFlyingWindSource();
        SetupSFXSources();

        float savedVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaultVolume);
        SetMasterVolume(savedVolume);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start(){
        PlayBGMByCurrentScene();
    }

    private void OnDestroy(){
        if(Instance == this){
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode){
        PlayBGMBySceneName(scene.name);
        StopFlyingWind();
    }

    private void SetupBGMSource(){
        if(bgmSource == null){
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;
    }

    private void SetupFlyingWindSource(){
        if(flyingWindSource == null){
            flyingWindSource = gameObject.AddComponent<AudioSource>();
        }

        flyingWindSource.loop = true;
        flyingWindSource.playOnAwake = false;
        flyingWindSource.volume = flyingWindVolume;
    }

    private void SetupSFXSources(){
        for (int i = 0; i < sfxSourceCount; i++){
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.volume = sfxVolume;

            sfxSources.Add(source);
        }
    }

    private void PlayBGMByCurrentScene(){
        Scene currentScene = SceneManager.GetActiveScene();
        PlayBGMBySceneName(currentScene.name);
    }

    private void PlayBGMBySceneName(string sceneName){
        if(sceneName == mainMenuSceneName){
            PlayBGM(mainBGM);
        }
        else if(sceneName == gameSceneName){
            PlayBGM(gameBGM);
        }
    }

    public void PlayBGM(AudioClip clip){
        if(clip == null){
            Debug.LogWarning("No BGM clip assigned.");
            return;
        }

        if(bgmSource.clip == clip && bgmSource.isPlaying){
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();

        Debug.Log("Playing BGM: " + clip.name);
    }

    public void StopBGM(){
        if(bgmSource != null){
            bgmSource.Stop();
        }
    }

    public void PlaySFX(GameSFX sfx){
        AudioClip clip = GetSFXClip(sfx);

        if(clip == null){
            Debug.LogWarning("Missing SFX clip: " + sfx);
            return;
        }

        AudioSource source = GetAvailableSFXSource();
        source.volume = sfxVolume;
        source.PlayOneShot(clip);
    }

    private AudioClip GetSFXClip(GameSFX sfx){
        switch (sfx){
            case GameSFX.PlayerGun: return projectileGunPlayer;
            case GameSFX.UfoGun: return projectileGunUfo;
            case GameSFX.BuildingDestroyed: return buildingDestroyed;
            case GameSFX.UfoDestroyed: return ufoDestroyed;
            default: return null;
        }
    }

    private AudioSource GetAvailableSFXSource(){
        foreach (AudioSource source in sfxSources){
            if(!source.isPlaying) return source;
        }

        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.loop = false;
        newSource.playOnAwake = false;
        newSource.volume = sfxVolume;

        sfxSources.Add(newSource);
        return newSource;
    }

    public void SetFlyingWind(bool isFlying){
        if(robotFlyingWind == null) return;
        if(isFlying) PlayFlyingWind();
        else StopFlyingWind();
    }

    public void PlayFlyingWind(){
        if(robotFlyingWind == null){
            Debug.LogWarning("Robot flying wind clip is missing.");
            return;
        }

        if(flyingWindSource.isPlaying) return;

        flyingWindSource.clip = robotFlyingWind;
        flyingWindSource.volume = flyingWindVolume;
        flyingWindSource.Play();
    }

    public void StopFlyingWind(){
        if(flyingWindSource != null && flyingWindSource.isPlaying) flyingWindSource.Stop();
    }

    public void SetMasterVolume(float volume){
        volume = Mathf.Clamp01(volume);

        AudioListener.volume = volume;

        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volume);
        PlayerPrefs.Save();

        Debug.Log("Master Volume changed to: " + volume);
    }

    public void SetBGMVolume(float volume) => SetMasterVolume(volume);
    public float GetMasterVolume() => PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaultVolume);
}