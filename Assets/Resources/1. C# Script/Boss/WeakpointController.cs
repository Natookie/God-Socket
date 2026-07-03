using UnityEngine;
using System.Collections;

public class WeakPointController : MonoBehaviour
{
    [Header("HEALTH")]
    public float maxHealth = 50f;
    public float currentHealth;
    
    [Header("VISUAL")]
    public GameObject visualModel;
    public Material normalMaterial;
    public Material damagedMaterial;
    public Material destroyedMaterial;
    public MeshRenderer visualRenderer;
    public ParticleSystem shieldParticles;
    public ParticleSystem damageParticles;
    public ParticleSystem destroyParticles;
    public Light glowLight;
    public float glowIntensity = 2f;
    public float pulseSpeed = 2f;
    
    [Header("AUDIO")]
    public AudioSource audioSource;
    public AudioClip damageSound;
    public AudioClip destroySound;
    public AudioClip shieldSound;
    public AudioClip respawnSound;
    
    [Header("ANIMATION")]
    public float rotationSpeed = 30f;
    public float bobAmplitude = 0.2f;
    public float bobFrequency = 1f;
    
    [Header("RESPAWN")]
    public bool canRespawn = false;
    public float respawnDelay = 10f;
    public bool respawnOnNewState = true;
    
    [Header("OPTIMIZATION")]
    public bool usePooling = true;
    public float updateInterval = 0.1f;
    
    private Transform bossTransform;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private float bobPhase;
    private float pulsePhase;
    private bool isActive;
    private bool isDestroyed;
    private bool isInitialized;
    private System.Action onDestroyedCallback;
    private float updateTimer;
    private Color originalGlowColor;
    private float originalGlowIntensity;
    
    void Awake(){
        InitializeComponents();
    }
    
    void InitializeComponents(){
        if(visualRenderer == null && visualModel != null){
            visualRenderer = visualModel.GetComponent<MeshRenderer>();
        }
        
        if(audioSource == null) audioSource = GetComponent<AudioSource>();
        if(audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        if(glowLight != null){
            originalGlowColor = glowLight.color;
            originalGlowIntensity = glowLight.intensity;
        }
        
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        pulsePhase = Random.Range(0f, Mathf.PI * 2f);
        updateTimer = 0f;
        isActive = false;
        isDestroyed = false;
        isInitialized = false;
        
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }
    
    public void Initialize(float hp, System.Action destroyCallback){
        maxHealth = hp;
        currentHealth = maxHealth;
        onDestroyedCallback = destroyCallback;
        isActive = true;
        isDestroyed = false;
        isInitialized = true;
        
        bossTransform = transform.parent;
        if(bossTransform == null){
            bossTransform = transform.root;
        }
        
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        
        ResetVisuals();
        
        if(shieldParticles != null){
            shieldParticles.Play();
        }
        
        if(respawnSound != null && audioSource != null){
            audioSource.PlayOneShot(respawnSound, 0.5f);
        }
        
        StartCoroutine(WeakPointLifecycle());
    }
    
    void Update(){
        if(!isActive || isDestroyed || !isInitialized) return;
        
        updateTimer += Time.deltaTime;
        if(updateTimer >= updateInterval){
            updateTimer = 0f;
            UpdateVisuals();
            UpdateAnimation();
            CheckHealth();
        }
    }
    
    void UpdateVisuals(){
        pulsePhase += Time.deltaTime * pulseSpeed;
        float pulse = Mathf.Sin(pulsePhase) * 0.5f + 0.5f;
        
        if(glowLight != null){
            glowLight.intensity = originalGlowIntensity * (0.5f + pulse * 0.5f);
            
            float healthPercent = currentHealth / maxHealth;
            Color glowColor = originalGlowColor;
            if(healthPercent < 0.3f){
                glowColor = Color.red;
            }
            else if(healthPercent < 0.6f){
                glowColor = Color.yellow;
            }
            glowLight.color = Color.Lerp(glowColor, originalGlowColor, pulse * 0.3f);
        }
        
        if(visualRenderer != null){
            float healthPercent = currentHealth / maxHealth;
            
            if(healthPercent > 0.6f){
                visualRenderer.material = normalMaterial;
            }
            else if(healthPercent > 0.3f){
                visualRenderer.material = damagedMaterial;
                if(damageParticles != null && !damageParticles.isPlaying){
                    damageParticles.Play();
                }
            }
            else{
                visualRenderer.material = damagedMaterial;
                if(damageParticles != null){
                    var emission = damageParticles.emission;
                    emission.rateOverTime = Mathf.Lerp(5f, 20f, 1f - healthPercent);
                }
            }
        }
        
        if(shieldParticles != null){
            var emission = shieldParticles.emission;
            emission.rateOverTime = Mathf.Lerp(5f, 20f, currentHealth / maxHealth);
        }
    }
    
    void UpdateAnimation(){
        bobPhase += Time.deltaTime * bobFrequency;
        float bobOffset = Mathf.Sin(bobPhase) * bobAmplitude;
        
        transform.localPosition = initialLocalPosition + Vector3.up * bobOffset;
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    
    void CheckHealth(){
        if(currentHealth <= 0f && !isDestroyed){
            DestroyWeakPoint();
        }
    }
    
    public void TakeDamage(float damage){
        if(!isActive || isDestroyed || !isInitialized) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);
        
        if(damageSound != null && audioSource != null){
            audioSource.PlayOneShot(damageSound, 0.5f);
        }
        
        if(damageParticles != null){
            damageParticles.Emit(Mathf.RoundToInt(damage * 2f));
        }
        
        if(shieldParticles != null){
            shieldParticles.Emit(5);
        }
        
        if(currentHealth <= 0f){
            DestroyWeakPoint();
        }
    }
    
    void DestroyWeakPoint(){
        if(isDestroyed) return;
        isDestroyed = true;
        isActive = false;
        
        if(destroyParticles != null){
            destroyParticles.transform.parent = null;
            destroyParticles.Play();
            Destroy(destroyParticles.gameObject, destroyParticles.main.duration + 1f);
        }
        
        if(destroySound != null && audioSource != null){
            audioSource.PlayOneShot(destroySound, 1f);
        }
        
        if(glowLight != null){
            glowLight.enabled = false;
        }
        
        if(visualModel != null){
            visualModel.SetActive(false);
        }
        
        if(shieldParticles != null){
            shieldParticles.Stop();
        }
        
        if(onDestroyedCallback != null){
            onDestroyedCallback.Invoke();
        }
        
        if(canRespawn){
            StartCoroutine(RespawnSequence());
        }
        else{
            gameObject.SetActive(false);
        }
    }
    
    IEnumerator RespawnSequence(){
        yield return new WaitForSeconds(respawnDelay);
        
        if(respawnOnNewState || !isDestroyed){
            yield break;
        }
        
        Respawn();
    }
    
    public void Respawn(){
        if(!canRespawn) return;
        
        isDestroyed = false;
        isActive = true;
        currentHealth = maxHealth;
        
        if(visualModel != null){
            visualModel.SetActive(true);
        }
        
        if(glowLight != null){
            glowLight.enabled = true;
        }
        
        ResetVisuals();
        
        if(shieldParticles != null){
            shieldParticles.Play();
        }
        
        if(respawnSound != null && audioSource != null){
            audioSource.PlayOneShot(respawnSound, 0.5f);
        }
        
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        
        if(onDestroyedCallback != null){
            onDestroyedCallback = null;
        }
        
        StartCoroutine(WeakPointLifecycle());
    }
    
    void ResetVisuals(){
        if(visualRenderer != null){
            visualRenderer.material = normalMaterial;
        }
        
        if(damageParticles != null){
            damageParticles.Stop();
        }
        
        if(glowLight != null){
            glowLight.color = originalGlowColor;
            glowLight.intensity = originalGlowIntensity;
        }
    }
    
    IEnumerator WeakPointLifecycle(){
        while(isActive && !isDestroyed){
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    public void ForceDestroy(){
        if(!isDestroyed){
            DestroyWeakPoint();
        }
    }
    
    public void ForceRespawn(){
        if(isDestroyed && canRespawn){
            Respawn();
        }
    }
    
    public void SetActive(bool active){
        isActive = active;
        gameObject.SetActive(active);
        
        if(active && isDestroyed && canRespawn){
            Respawn();
        }
    }
    
    public bool IsDestroyed(){
        return isDestroyed;
    }
    
    public bool IsActive(){
        return isActive && !isDestroyed;
    }
    
    public float GetHealth(){
        return currentHealth;
    }
    
    public float GetMaxHealth(){
        return maxHealth;
    }
    
    public float GetHealthPercent(){
        return currentHealth / maxHealth;
    }
    
    public void SetCallback(System.Action callback){
        onDestroyedCallback = callback;
    }
    
    void OnDisable(){
        StopAllCoroutines();
    }
    
    void OnDrawGizmosSelected(){
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        if(bossTransform != null){
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, bossTransform.position);
        }
    }
}