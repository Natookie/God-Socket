using UnityEngine;
using System.Collections;

public class Projectile : MonoBehaviour
{
    [Header("MOVEMENT")]
    public float speed = 20f;
    public float maxLifeTime = 5f;
    public bool useGravity = false;
    public float gravityScale = 1f;
    
    [Header("DAMAGE")]
    public float damage = 10f;
    public float explosionRadius = 0f;
    public float explosionForce = 0f;
    public string targetTag = "Player";
    public bool damageOnCollision = true;
    public bool destroyOnCollision = true;
    public bool destroyOnHit = true;
    
    [Header("HOMING")]
    public bool isHoming = false;
    public float homingStrength = 2f;
    public float homingRange = 50f;
    public float maxHomingAngle = 60f;
    public Transform homingTarget;
    
    [Header("VISUAL")]
    public GameObject visualModel;
    public ParticleSystem trailParticles;
    public ParticleSystem impactParticles;
    public Light glowLight;
    public GameObject trailObject;
    
    [Header("AUDIO")]
    public AudioSource audioSource;
    public AudioClip launchSound;
    public AudioClip impactSound;
    public AudioClip flySound;
    
    [Header("POOLING")]
    public bool usePooling = true;
    public string poolTag = "Projectile";
    
    private Vector3 velocity;
    private Vector3 targetPosition;
    private float lifeTimer;
    private bool isActive;
    private bool isDestroyed;
    private Rigidbody rb;
    private Collider projectileCollider;
    private Vector3 startPosition;
    private Quaternion startRotation;
    
    void Awake(){
        InitializeComponents();
    }
    
    void InitializeComponents(){
        rb = GetComponent<Rigidbody>();
        if(rb == null){
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = !useGravity;
            rb.useGravity = useGravity;
            if(useGravity){
                rb.mass = 1f;
                rb.linearDamping = 0.5f;
            }
        }
        
        projectileCollider = GetComponent<Collider>();
        if(projectileCollider == null){
            projectileCollider = gameObject.AddComponent<SphereCollider>();
            projectileCollider.isTrigger = true;
        }
        
        if(audioSource == null) audioSource = GetComponent<AudioSource>();
        if(audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        startPosition = transform.position;
        startRotation = transform.rotation;
        isActive = false;
        isDestroyed = false;
    }
    
    public void Initialize(Vector3 direction, float projectileSpeed, float projectileDamage, string tag = "Player"){
        transform.position = startPosition;
        transform.rotation = startRotation;
        
        velocity = direction.normalized * projectileSpeed;
        speed = projectileSpeed;
        damage = projectileDamage;
        targetTag = tag;
        isActive = true;
        isDestroyed = false;
        lifeTimer = 0f;
        
        if(visualModel != null){
            visualModel.SetActive(true);
        }
        
        if(projectileCollider != null){
            projectileCollider.enabled = true;
        }
        
        if(glowLight != null){
            glowLight.enabled = true;
        }
        
        if(trailParticles != null){
            trailParticles.Play();
        }
        
        if(trailObject != null){
            trailObject.SetActive(true);
        }
        
        if(launchSound != null && audioSource != null){
            audioSource.PlayOneShot(launchSound, 0.5f);
        }
        
        if(flySound != null && audioSource != null){
            audioSource.clip = flySound;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        if(useGravity && rb != null){
            rb.isKinematic = false;
            rb.linearVelocity = velocity;
        }
        
        StartCoroutine(LifecycleTimer());
        StartCoroutine(ActiveUpdate());
    }
    
    IEnumerator LifecycleTimer(){
        yield return new WaitForSeconds(maxLifeTime);
        
        if(isActive && !isDestroyed){
            DestroyProjectile();
        }
    }
    
    IEnumerator ActiveUpdate(){
        while(isActive && !isDestroyed){
            lifeTimer += Time.deltaTime;
            
            if(isHoming){
                UpdateHoming();
            }
            
            if(!useGravity){
                UpdateMovement();
            }
            
            UpdateVisuals();
            CheckCollision();
            
            yield return new WaitForSeconds(0.01f);
        }
    }
    
    void Update(){
        if(!isActive || isDestroyed) return;
        
        if(useGravity && rb != null){
            velocity = rb.linearVelocity;
            
            if(isHoming){
                UpdateHoming();
                rb.linearVelocity = velocity;
            }
        }
        
        UpdateVisuals();
    }
    
    void UpdateMovement(){
        transform.position += velocity * Time.deltaTime;
        
        if(velocity.sqrMagnitude > 0.01f){
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }
    
    void UpdateHoming(){
        if(homingTarget == null){
            FindHomingTarget();
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, homingTarget.position);
        if(distanceToTarget > homingRange){
            FindHomingTarget();
            return;
        }
        
        Vector3 directionToTarget = (homingTarget.position - transform.position).normalized;
        float angle = Vector3.Angle(velocity.normalized, directionToTarget);
        
        if(angle > maxHomingAngle){
            return;
        }
        
        Vector3 newDirection = Vector3.RotateTowards(velocity.normalized, directionToTarget, homingStrength * Mathf.Deg2Rad * Time.deltaTime, 1f);
        velocity = newDirection.normalized * speed;
        
        if(!useGravity){
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }
    
    void FindHomingTarget(){
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        if(targets == null || targets.Length == 0) return;
        
        Transform bestTarget = null;
        float bestDistance = float.MaxValue;
        
        foreach(GameObject target in targets){
            if(target == null) continue;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if(distance < homingRange && distance < bestDistance){
                Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(velocity.normalized, directionToTarget);
                
                if(angle <= maxHomingAngle){
                    bestDistance = distance;
                    bestTarget = target.transform;
                }
            }
        }
        
        if(bestTarget != null){
            homingTarget = bestTarget;
        }
    }
    
    void UpdateVisuals(){
        if(glowLight != null){
            glowLight.intensity = Mathf.Lerp(0.5f, 2f, speed / 30f);
        }
        
        if(trailParticles != null){
            var emission = trailParticles.emission;
            emission.rateOverTime = Mathf.Lerp(5f, 30f, speed / 30f);
        }
    }
    
    void CheckCollision(){
        if(!damageOnCollision) return;
        
        RaycastHit hit;
        float checkDistance = (useGravity ? velocity.magnitude * Time.deltaTime : speed * Time.deltaTime) + 0.5f;
        
        if(Physics.Raycast(transform.position, velocity.normalized, out hit, checkDistance)){
            if(hit.collider.gameObject == gameObject) return;
            
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if(damageable != null){
                if(!string.IsNullOrEmpty(targetTag) && !hit.collider.CompareTag(targetTag)){
                    return;
                }
                
                damageable.TakeDamage(damage);
                OnHit();
            }
            else if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Building")){
                OnHit();
            }
        }
    }
    
    void OnTriggerEnter(Collider other){
        if(!isActive || isDestroyed) return;
        if(other.gameObject == gameObject) return;
        
        IDamageable damageable = other.GetComponent<IDamageable>();
        if(damageable != null){
            if(!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)){
                return;
            }
            
            damageable.TakeDamage(damage);
            OnHit();
        }
        else if(other.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                other.gameObject.layer == LayerMask.NameToLayer("Building")){
            OnHit();
        }
    }
    
    void OnHit(){
        if(isDestroyed) return;
        
        if(explosionRadius > 0f){
            Explode();
        }
        else{
            if(impactParticles != null){
                ParticleSystem impact = Instantiate(impactParticles, transform.position, Quaternion.identity);
                impact.Play();
                Destroy(impact.gameObject, impact.main.duration + 1f);
            }
            
            if(impactSound != null && audioSource != null){
                audioSource.PlayOneShot(impactSound, 0.5f);
            }
            
            if(destroyOnHit){
                DestroyProjectile();
            }
        }
    }
    
    void Explode(){
        if(isDestroyed) return;
        
        if(impactParticles != null){
            ParticleSystem explosion = Instantiate(impactParticles, transform.position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * (explosionRadius / 5f);
            explosion.Play();
            Destroy(explosion.gameObject, explosion.main.duration + 1f);
        }
        
        if(impactSound != null && audioSource != null){
            audioSource.PlayOneShot(impactSound, 1f);
        }
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach(Collider col in colliders){
            IDamageable damageable = col.GetComponent<IDamageable>();
            if(damageable != null){
                float distance = Vector3.Distance(transform.position, col.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                float actualDamage = damage * Mathf.Max(damageMultiplier, 0.1f);
                damageable.TakeDamage(actualDamage);
            }
            
            Rigidbody rbTarget = col.GetComponent<Rigidbody>();
            if(rbTarget != null && !rbTarget.isKinematic && explosionForce > 0f){
                Vector3 explosionDirection = (col.transform.position - transform.position).normalized;
                float forceMultiplier = 1f - (Vector3.Distance(transform.position, col.transform.position) / explosionRadius);
                rbTarget.AddForce(explosionDirection * explosionForce * Mathf.Max(forceMultiplier, 0.1f));
            }
        }
        
        if(destroyOnHit){
            DestroyProjectile();
        }
    }
    
    void DestroyProjectile(){
        if(isDestroyed) return;
        isDestroyed = true;
        isActive = false;
        
        if(visualModel != null){
            visualModel.SetActive(false);
        }
        
        if(projectileCollider != null){
            projectileCollider.enabled = false;
        }
        
        if(glowLight != null){
            glowLight.enabled = false;
        }
        
        if(trailParticles != null){
            trailParticles.Stop();
        }
        
        if(trailObject != null){
            trailObject.SetActive(false);
        }
        
        if(flySound != null && audioSource != null){
            audioSource.Stop();
        }
        
        if(useGravity && rb != null){
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }
        
        StopAllCoroutines();
        
        if(usePooling){
            gameObject.SetActive(false);
        }
        else{
            Destroy(gameObject, 0.5f);
        }
    }
    
    public void SetTarget(Transform target){
        homingTarget = target;
        isHoming = target != null;
    }
    
    public void SetSpeed(float newSpeed){
        speed = newSpeed;
        velocity = velocity.normalized * speed;
    }
    
    public void SetDamage(float newDamage){
        damage = newDamage;
    }
    
    public bool IsActive(){
        return isActive && !isDestroyed;
    }
    
    public bool IsDestroyed(){
        return isDestroyed;
    }
    
    public float GetLifeTime(){
        return lifeTimer;
    }
    
    void OnDisable(){
        isActive = false;
        isDestroyed = true;
        StopAllCoroutines();
        
        if(flySound != null && audioSource != null){
            audioSource.Stop();
        }
    }
    
    void OnDrawGizmosSelected(){
        if(explosionRadius > 0f){
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
        
        if(isHoming && homingTarget != null){
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, homingTarget.position);
            Gizmos.DrawWireSphere(homingTarget.position, 0.5f);
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, velocity.normalized * 2f);
    }
}