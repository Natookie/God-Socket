using UnityEngine;
using System.Collections;

public class UFOController : MonoBehaviour, IDamageable
{
    [Header("MOVEMENT")]
    public float moveSpeed = 15f;
    public float rotationSpeed = 180f;
    public float followDistance = 10f;
    public float followHeight = 5f;
    public float stoppingDistance = 2f;
    public float damping = 2f;
    public float maxForce = 30f;
    public float hoverAmplitude = 0.5f;
    public float hoverFrequency = 2f;
    
    [Header("COMBAT")]
    public float health = 20f;
    public float fireRate = 1.5f;
    public float damage = 10f;
    public float attackRange = 25f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    [Header("VISUAL")]
    public GameObject visualModel;
    public ParticleSystem engineParticles;
    public ParticleSystem deathParticles;
    
    public Transform player;
    private float fireTimer;
    private bool isActive;
    private bool isDead;
    private Rigidbody rb;
    private Vector3 targetPosition;
    private float hoverPhase;
    private Vector3 smoothVelocity;
    private Vector3 desiredVelocity;
    
    void Awake(){
        rb = GetComponent<Rigidbody>();
        if(rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearDamping = 2f;
        rb.angularDamping = 3f;
        rb.mass = 1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        if(firePoint == null){
            firePoint = new GameObject("FirePoint").transform;
            firePoint.SetParent(transform);
            firePoint.localPosition = Vector3.forward * 1.5f;
        }
        
        isActive = true;
        isDead = false;
        targetPosition = transform.position;
        hoverPhase = Random.Range(0f, Mathf.PI * 2f);
        smoothVelocity = Vector3.zero;
        desiredVelocity = Vector3.zero;
    }
    
    public void Initialize(Transform target){
        player = target;
        isActive = true;
        isDead = false;
        fireTimer = 0f;
        
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        targetPosition = transform.position;
        smoothVelocity = Vector3.zero;
        desiredVelocity = Vector3.zero;
        
        if(visualModel != null){
            visualModel.SetActive(true);
        }
        
        if(engineParticles != null){
            engineParticles.Play();
        }
    }
    
    void FixedUpdate(){
        if(!isActive || isDead || player == null) return;
        
        UpdateMovementPhysics();
        UpdateRotationPhysics();
        ApplyHoverPhysics();
    }
    
    void Update(){
        if(!isActive || isDead || player == null) return;
        
        UpdateCombat();
        UpdateVisuals();
    }
    
    void UpdateMovementPhysics(){
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        
        if(distanceToPlayer > followDistance + stoppingDistance){
            targetPosition = player.position + directionToPlayer * followDistance;
            targetPosition.y = player.position.y + followHeight;
        }
        else if(distanceToPlayer < followDistance - stoppingDistance){
            targetPosition = player.position - directionToPlayer * followDistance;
            targetPosition.y = player.position.y + followHeight;
        }
        else{
            targetPosition = Vector3.Lerp(targetPosition, 
                transform.position + Random.insideUnitSphere * 0.1f, 
                Time.fixedDeltaTime * 0.5f
            );
            targetPosition.y = player.position.y + followHeight;
        }
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        float speedMultiplier = Mathf.Clamp01(distance / (followDistance * 0.5f));
        float targetSpeed = moveSpeed * speedMultiplier;
        
        desiredVelocity = direction * targetSpeed;
        
        Vector3 force = (desiredVelocity - rb.linearVelocity) * damping;
        force = Vector3.ClampMagnitude(force, maxForce);
        
        rb.AddForce(force, ForceMode.Force);
        
        if(rb.linearVelocity.magnitude > moveSpeed * 1.5f){
            rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed * 1.5f;
        }
    }
    
    void UpdateRotationPhysics(){
        if(rb.linearVelocity.sqrMagnitude > 0.01f){
            Quaternion targetRotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime * 0.01f);
        }
    }
    
    void ApplyHoverPhysics(){
        hoverPhase += Time.fixedDeltaTime * hoverFrequency;
        float hoverOffset = Mathf.Sin(hoverPhase) * hoverAmplitude;
        
        Vector3 hoverForce = Vector3.up * hoverOffset * 2f;
        rb.AddForce(hoverForce, ForceMode.Force);
    }
    
    void UpdateCombat(){
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if(distanceToPlayer > attackRange) return;
        
        fireTimer += Time.deltaTime;
        if(fireTimer >= fireRate){
            FireProjectile();
            fireTimer = 0f;
        }
    }
    
    void FireProjectile(){
        if(projectilePrefab == null || firePoint == null || player == null) return;
        
        Vector3 fireDirection = (player.position - firePoint.position).normalized;
        
        fireDirection = Quaternion.Euler(
            Random.Range(-3f, 3f),
            Random.Range(-3f, 3f),
            0f
        ) * fireDirection;
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
        projectile.SetActive(true);
        
        Rigidbody projRb = projectile.GetComponent<Rigidbody>();
        if(projRb != null){
            projRb.isKinematic = false;
            projRb.useGravity = false;
            projRb.linearVelocity = fireDirection * 20f;
        }
        
        Projectile proj = projectile.GetComponent<Projectile>();
        if(proj != null){
            proj.Initialize(fireDirection * 20f, damage, 5f, "Player");
        }
        
        Destroy(projectile, 5f);
    }
    
    void UpdateVisuals(){
        if(engineParticles != null){
            var emission = engineParticles.emission;
            float speedRatio = rb.linearVelocity.magnitude / moveSpeed;
            emission.rateOverTime = Mathf.Lerp(5f, 30f, speedRatio);
        }
    }
    
    public void TakeDamage(float damageAmount){
        if(isDead) return;
        
        health -= damageAmount;
        
        if(visualModel != null){
            StartCoroutine(DamageFlash());
        }
        
        Vector3 knockback = Random.insideUnitSphere * 5f;
        knockback.y = 2f;
        rb.AddForce(knockback, ForceMode.Impulse);
        
        if(health <= 0f){
            Die();
        }
    }
    
    IEnumerator DamageFlash(){
        Renderer[] renderers = visualModel.GetComponentsInChildren<Renderer>();
        Color[] originalColors = new Color[renderers.Length];
        
        for(int i = 0; i < renderers.Length; i++){
            originalColors[i] = renderers[i].material.color;
            renderers[i].material.color = Color.red;
        }
        
        yield return new WaitForSeconds(0.1f);
        
        for(int i = 0; i < renderers.Length; i++){
            renderers[i].material.color = originalColors[i];
        }
    }
    
    void Die(){
        if(isDead) return;
        isDead = true;
        isActive = false;
        
        if(deathParticles != null){
            deathParticles.transform.parent = null;
            deathParticles.Play();
            Destroy(deathParticles.gameObject, 2f);
        }
        
        if(engineParticles != null){
            engineParticles.Stop();
        }
        
        if(visualModel != null){
            visualModel.SetActive(false);
        }
        
        gameObject.SetActive(false);
    }
    
    void OnTriggerEnter(Collider other){
        if(isDead || !isActive) return;
        
        IDamageable damageable = other.GetComponent<IDamageable>();
        if(damageable != null && other.gameObject.layer != LayerMask.NameToLayer("Enemy")){
            if(other.transform == player){
                damageable.TakeDamage(damage * 0.5f);
            }
        }
    }
    
    public bool IsAlive(){
        return isActive && !isDead;
    }
    
    void OnDisable(){
        isActive = false;
        StopAllCoroutines();
    }
}