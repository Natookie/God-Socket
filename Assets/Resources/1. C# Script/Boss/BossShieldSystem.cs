using UnityEngine;
using System.Collections;

public class BossShieldSystem : MonoBehaviour, IDamageable
{
    [Header("SHIELD STATS")]
    public float DamageMultiplier = 0f;
    public enum DamageType
    {
        Bullet,
        Missile
    }

    [Header("FADE SETTINGS")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float minFade = 0f;
    [SerializeField] private float maxFade = 1f;

    [Header("REFERENCES")]
    public SphereCollider shieldCollider;
    public BossHealthSystem healthSystem;
    public Renderer shieldRenderer;
    public AudioSource audioSource;
    public AudioClip damageSound;

    private static readonly int OpacityID = Shader.PropertyToID("_Opacity");
    private MaterialPropertyBlock mpb;
    private Coroutine fadeCoroutine;
    private float currentFade;

    void Awake(){
        mpb = new MaterialPropertyBlock();
        if(shieldRenderer == null) shieldRenderer = GetComponent<Renderer>();
        
        currentFade = minFade;
        SetFade(currentFade);
        shieldCollider.enabled = false;
    }

    public void TakeDamage(float damage){
        float actualDamage = damage * DamageMultiplier;
        healthSystem.TakeDamage(actualDamage);
        if(audioSource != null && damageSound != null) audioSource.PlayOneShot(damageSound);
    }

    public void TakeDamage(float damage, DamageType type){
        float actualDamage = (type == DamageType.Bullet) ? damage * DamageMultiplier : damage;
        healthSystem.TakeDamage(actualDamage);
        if(audioSource != null && damageSound != null) audioSource.PlayOneShot(damageSound);
    }

    public void SetDamageMultiplier(float reduction) => DamageMultiplier = Mathf.Clamp01(reduction);
    public bool IsAlive() => true;

    public void ShowShield(){
        if(fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        shieldCollider.enabled = true;
        fadeCoroutine = StartCoroutine(FadeCoroutine(maxFade));
    }

    public void HideShield(){
        if(fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCoroutine(minFade, true));
    }

    public void InstantShowShield(){
        if(fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        shieldCollider.enabled = true;
        SetFade(maxFade);
    }

    public void InstantHideShield(){
        if(fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        SetFade(minFade);
        shieldCollider.enabled = false;
    }

    void SetFade(float fade){
        currentFade = Mathf.Clamp(fade, minFade, maxFade);
        mpb.SetFloat(OpacityID, currentFade);
        shieldRenderer.SetPropertyBlock(mpb);
    }

    float GetCurrentFade(){
        return currentFade;
    }

    IEnumerator FadeCoroutine(float targetFade, bool deactivateOnComplete = false){
        float startFade = GetCurrentFade();
        float elapsed = 0f;

        while(elapsed < fadeDuration){
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            t = t * t * (3f - 2f * t);
            float currentFade = Mathf.Lerp(startFade, targetFade, t);
            SetFade(currentFade);
            yield return null;
        }

        SetFade(targetFade);

        if(deactivateOnComplete) shieldCollider.enabled = false;
        fadeCoroutine = null;
    }
}