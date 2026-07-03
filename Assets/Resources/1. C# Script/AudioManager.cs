using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM")]
    public AudioSource bgmSource;
    public AudioClip mainBGM;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float defaultVolume = 1f;

    private void Awake()
    {
        // Singleton, so only one AudioManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupBGM();
    }

    private void Start()
    {
        AudioListener.volume = 1f;

        if (bgmSource != null)
        {
            bgmSource.volume = 1f;
        }

        PlayBGM(mainBGM);
    }

    private void SetupBGM()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", defaultVolume);
        bgmSource.volume = savedVolume;
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("No BGM clip assigned.");
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();

        Debug.Log("Playing BGM: " + clip.name);
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

   public void SetBGMVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (bgmSource != null)
        {
            bgmSource.volume = volume;
        }

        AudioListener.volume = volume;

        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();

        Debug.Log("BGM Volume changed to: " + volume);
    }
}