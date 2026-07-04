using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UFOController : MonoBehaviour, IDamageable
{
    [Header("MOVEMENT")]
    public float moveSpeed = 25f;
    public float rotationSpeed = 300f;
    public float followDistance = 5f;
    public float followHeight = 3f;
    public float stoppingDistance = 1f;
    public float damping = 4f;
    public float maxForce = 50f;
    public float hoverAmplitude = 0.3f;
    public float hoverFrequency = 3f;
    
    [Header("NATURAL MOVEMENT")]
    public float wanderRadius = 1.5f;
    public float wanderChangeInterval = 1.5f;
    public float wanderSmoothness = 3f;
    public float turbulenceForce = 3f;
    public float turbulenceChangeRate = 1f;
    public float personality = 1f;
    public float avoidanceRadius = 2f;
    public float avoidanceStrength = 10f;
    
    [Header("PREDICTION")]
    public float movementPredictionTime = 1f;
    public float movementPredictionStrength = 1f;
    public float shootingPredictionTime = 0.8f;
    public float shootingPredictionStrength = 1f;
    
    [Header("COMBAT")]
    public float health = 30f;
    private float fireRate = 0.5f;
    public float damage = 15f;
    public float attackRange = 35f;
    public float aimSpeed = 10f;
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
    private Vector3 wanderOffset;
    private Vector3 targetWanderOffset;
    private float wanderTimer;
    private Vector3 turbulenceForceVector;
    private float turbulenceTimer;
    private float uniquePhaseOffset;
    private float personalitySpeedModifier;
    private float personalityHoverModifier;
    private float personalityWanderModifier;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private Vector3 spawnPosition;
    private bool isOverheatMode;
    private Building targetBuilding;
    private bool isReturningToSpawn;
    
    void Awake(){
        rb = GetComponent<Rigidbody>();
        if(rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearDamping = 1f;
        rb.angularDamping = 2f;
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
        lastPlayerPosition = Vector3.zero;
        playerVelocity = Vector3.zero;
        isOverheatMode = false;
        isReturningToSpawn = false;
        
        uniquePhaseOffset = Random.Range(0f, Mathf.PI * 2f);
        personalitySpeedModifier = Random.Range(0.9f, 1.1f);
        personalityHoverModifier = Random.Range(0.7f, 1.3f);
        personalityWanderModifier = Random.Range(0.7f, 1.3f);
        
        wanderOffset = Random.insideUnitSphere * wanderRadius;
        wanderOffset.y = 0;
        targetWanderOffset = wanderOffset;
        wanderTimer = 0f;
        
        turbulenceForceVector = Random.insideUnitSphere * turbulenceForce;
        turbulenceForceVector.y = 0;
        turbulenceTimer = 0f;
    }
    
    public void Initialize(Transform target){
        player = target;
        isActive = true;
        isDead = false;
        fireTimer = 0f;
        isOverheatMode = false;
        isReturningToSpawn = false;
        targetBuilding = null;
        spawnPosition = transform.position;
        
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        targetPosition = transform.position;
        smoothVelocity = Vector3.zero;
        desiredVelocity = Vector3.zero;
        lastPlayerPosition = player != null ? player.position : Vector3.zero;
        playerVelocity = Vector3.zero;

        if(visualModel != null){
            visualModel.SetActive(true);
            ResetVisualColors();
        }
        
        if(engineParticles != null) engineParticles.Play();
    }
    
    void ResetVisualColors(){
        Renderer[] renderers = visualModel.GetComponentsInChildren<Renderer>();
        foreach(Renderer renderer in renderers){
            if(renderer.material.HasProperty("_Color")){
                renderer.material.color = Color.white;
            }
        }
    }
    
    void FixedUpdate(){
        if(!isActive || isDead) return;
        
        if(isReturningToSpawn){
            UpdateReturnToSpawn();
            return;
        }
        
        if(isOverheatMode){
            UpdateOverheatMode();
            return;
        }
        
        if(player == null) return;
        
        UpdatePlayerVelocity();
        
        wanderTimer += Time.fixedDeltaTime;
        if(wanderTimer >= wanderChangeInterval * (1 / personalityWanderModifier)){
            wanderTimer = 0f;
            float radius = wanderRadius * (0.5f + personality * 0.5f);
            targetWanderOffset = Random.insideUnitSphere * radius;
            targetWanderOffset.y = 0;
        }
        
        wanderOffset = Vector3.Lerp(wanderOffset, targetWanderOffset, Time.fixedDeltaTime * wanderSmoothness);
        
        turbulenceTimer += Time.fixedDeltaTime;
        if(turbulenceTimer >= turbulenceChangeRate / personalitySpeedModifier){
            turbulenceTimer = 0f;
            turbulenceForceVector = Random.insideUnitSphere * turbulenceForce * personality;
            turbulenceForceVector.y = 0;
        }
        
        UpdateMovementPhysics();
        UpdateRotationPhysics();
        ApplyHoverPhysics();
        ApplyAvoidance();
        
        if(PlayerController.Instance != null && PlayerController.Instance.IsOverheat()){
            EnterOverheatMode();
        }
    }
    
    void Update(){
        if(!isActive || isDead) return;
        
        if(isReturningToSpawn) return;
        
        if(isOverheatMode){
            UpdateOverheatCombat();
            return;
        }
        
        if(player == null) return;
        
        UpdateCombat();
        UpdateVisuals();
    }
    
    void EnterOverheatMode(){
        if(isOverheatMode) return;
        
        isOverheatMode = true;
        targetBuilding = GetRandomBuilding();
        
        if(targetBuilding == null){
            isReturningToSpawn = true;
            return;
        }
        
        BuildingManager.Instance.SetTargeted(targetBuilding, true);
    }
    
    Building GetRandomBuilding(){
        if(BuildingManager.Instance == null) return null;
        
        Dictionary<Building, BuildingState> states = BuildingManager.Instance.GetBuildingStates();
        List<Building> availableBuildings = new List<Building>();
        
        foreach(var kvp in states){
            if(kvp.Value == BuildingState.Available){
                availableBuildings.Add(kvp.Key);
            }
        }
        
        if(availableBuildings.Count == 0) return null;
        
        int randomIndex = Random.Range(0, availableBuildings.Count);
        return availableBuildings[randomIndex];
    }
    
    void UpdateOverheatMode(){
        if(targetBuilding == null){
            isReturningToSpawn = true;
            return;
        }
        
        if(!targetBuilding.IsAlive()){
            targetBuilding = null;
            isReturningToSpawn = true;
            return;
        }
        
        Vector3 targetPos = targetBuilding.transform.position;
        float distanceToBuilding = Vector3.Distance(transform.position, targetPos);
        
        if(distanceToBuilding < 8f){
            isReturningToSpawn = true;
            return;
        }
        
        Vector3 directionToTarget = (targetPos - transform.position).normalized;
        Vector3 desiredPos = targetPos - directionToTarget * 8f;
        desiredPos.y = targetPos.y + followHeight;
        
        targetPosition = Vector3.Lerp(targetPosition, desiredPos, Time.fixedDeltaTime * 0.5f);
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        float targetSpeed = moveSpeed * personalitySpeedModifier;
        
        desiredVelocity = direction * targetSpeed;
        desiredVelocity += Random.insideUnitSphere * 0.3f;
        desiredVelocity.y *= 0.2f;
        
        Vector3 force = (desiredVelocity - rb.linearVelocity) * damping;
        force = Vector3.ClampMagnitude(force, maxForce * personalitySpeedModifier);
        
        rb.AddForce(force, ForceMode.Force);
        
        float maxSpeed = moveSpeed * 2f * personalitySpeedModifier;
        if(rb.linearVelocity.magnitude > maxSpeed) rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        
        ApplyHoverPhysics();
        ApplyAvoidance();
        
        if(rb.linearVelocity.sqrMagnitude > 0.01f){
            Vector3 lookDirection = rb.linearVelocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime * 0.02f);
        }
    }
    
    void UpdateOverheatCombat(){
        if(targetBuilding == null) return;
        
        if(!targetBuilding.IsAlive()){
            targetBuilding = null;
            isReturningToSpawn = true;
            return;
        }
        
        float distanceToBuilding = Vector3.Distance(transform.position, targetBuilding.transform.position);
        
        if(distanceToBuilding <= 20f){
            Vector3 directionToBuilding = (targetBuilding.transform.position - transform.position).normalized;
            
            Quaternion targetRotation = Quaternion.LookRotation(directionToBuilding);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, aimSpeed * Time.deltaTime);
            
            fireTimer += Time.deltaTime;
            if(fireTimer >= fireRate){
                FireProjectileAtBuilding();
                fireTimer = 0f;
                fireRate = Random.Range(1.5f, 3f);
            }
        }
    }
    
    void FireProjectileAtBuilding(){
        if(projectilePrefab == null || firePoint == null || targetBuilding == null) return;
        
        Vector3 fireDirection = (targetBuilding.transform.position - firePoint.position).normalized;
        
        float spread = 1f;
        fireDirection = Quaternion.Euler(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0f
        ) * fireDirection;
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
        projectile.SetActive(true);
        
        EnemyProjectile proj = projectile.GetComponent<EnemyProjectile>();
        if(proj != null){
            proj.Initialize(fireDirection, damage * 2f);
        }
        
        Destroy(projectile, 5f);
    }
    
    void UpdateReturnToSpawn(){
        float distanceToSpawn = Vector3.Distance(transform.position, spawnPosition);
        
        if(distanceToSpawn < 3f){
            Despawn();
            return;
        }
        
        Vector3 direction = (spawnPosition - transform.position).normalized;
        Vector3 desiredPos = spawnPosition;
        
        targetPosition = Vector3.Lerp(targetPosition, desiredPos, Time.fixedDeltaTime * 0.5f);
        
        float targetSpeed = moveSpeed * personalitySpeedModifier * 1.5f;
        
        desiredVelocity = direction * targetSpeed;
        desiredVelocity += Random.insideUnitSphere * 0.3f;
        desiredVelocity.y *= 0.2f;
        
        Vector3 force = (desiredVelocity - rb.linearVelocity) * damping;
        force = Vector3.ClampMagnitude(force, maxForce * personalitySpeedModifier);
        
        rb.AddForce(force, ForceMode.Force);
        
        float maxSpeed = moveSpeed * 2f * personalitySpeedModifier;
        if(rb.linearVelocity.magnitude > maxSpeed) rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        
        ApplyHoverPhysics();
        
        if(rb.linearVelocity.sqrMagnitude > 0.01f){
            Vector3 lookDirection = rb.linearVelocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime * 0.02f);
        }
    }
    
    void Despawn(){
        if(targetBuilding != null){
            BuildingManager.Instance.SetTargeted(targetBuilding, false);
            targetBuilding = null;
        }
        
        if(engineParticles != null) engineParticles.Stop();
        if(visualModel != null) visualModel.SetActive(false);
        
        gameObject.SetActive(false);
    }
    
    void UpdatePlayerVelocity(){
        if(player == null) return;
        
        Vector3 currentPlayerPos = player.position;
        
        if(lastPlayerPosition != Vector3.zero)
            playerVelocity = (currentPlayerPos - lastPlayerPosition) / Time.fixedDeltaTime;
        
        lastPlayerPosition = currentPlayerPos;
    }
    
    Vector3 GetPredictedPlayerPosition(float predictionTime, float predictionStrength){
        if(player == null) return Vector3.zero;
        
        Vector3 predictedPos = player.position;
        
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if(playerRb != null) predictedPos += playerRb.linearVelocity * predictionTime * predictionStrength;
        else predictedPos += playerVelocity * predictionTime * predictionStrength;
        
        return predictedPos;
    }
    
    void UpdateMovementPhysics(){
        Vector3 predictedTarget = GetPredictedPlayerPosition(movementPredictionTime, movementPredictionStrength);
        
        float distanceToPlayer = Vector3.Distance(transform.position, predictedTarget);
        Vector3 directionToPlayer = (predictedTarget - transform.position).normalized;
        Vector3 baseFollowPosition;
        
        if(distanceToPlayer > followDistance + stoppingDistance){
            baseFollowPosition = predictedTarget + directionToPlayer * followDistance;
            baseFollowPosition.y = predictedTarget.y + followHeight;
        }
        else if(distanceToPlayer < followDistance - stoppingDistance){
            baseFollowPosition = predictedTarget - directionToPlayer * followDistance;
            baseFollowPosition.y = predictedTarget.y + followHeight;
        }
        else{
            baseFollowPosition = Vector3.Lerp(targetPosition, 
                predictedTarget + Random.insideUnitSphere * 0.3f, 
                Time.fixedDeltaTime * 0.5f
            );
            baseFollowPosition.y = predictedTarget.y + followHeight;
        }
        
        Vector3 wanderTarget = baseFollowPosition + wanderOffset;
        Vector3 turbulenceOffset = turbulenceForceVector * Time.fixedDeltaTime * 3f;
        
        targetPosition = Vector3.Lerp(targetPosition, wanderTarget, Time.fixedDeltaTime * 1f);
        targetPosition += turbulenceOffset;
        
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        float speedMultiplier = Mathf.Clamp01(distance / (followDistance * 0.5f));
        float targetSpeed = moveSpeed * speedMultiplier * personalitySpeedModifier;
        
        desiredVelocity = direction * targetSpeed;
        
        desiredVelocity += Random.insideUnitSphere * 0.3f;
        desiredVelocity.y *= 0.2f;
        
        Vector3 force = (desiredVelocity - rb.linearVelocity) * damping;
        force = Vector3.ClampMagnitude(force, maxForce * personalitySpeedModifier);
        
        rb.AddForce(force, ForceMode.Force);
        
        float maxSpeed = moveSpeed * 2f * personalitySpeedModifier;
        if(rb.linearVelocity.magnitude > maxSpeed) rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }
    
    void ApplyAvoidance(){
        UFOController[] allUFOs = FindObjectsByType<UFOController>(FindObjectsSortMode.None);
        Vector3 avoidanceForce = Vector3.zero;
        
        foreach(UFOController other in allUFOs){
            if(other == this || !other.IsAlive()) continue;
            
            Vector3 toOther = transform.position - other.transform.position;
            float distance = toOther.magnitude;
            
            if(distance < avoidanceRadius && distance > 0.01f){
                float strength = (1f - (distance / avoidanceRadius)) * avoidanceStrength;
                avoidanceForce += toOther.normalized * strength;
            }
        }
        
        if(avoidanceForce.magnitude > 0.01f) rb.AddForce(avoidanceForce, ForceMode.Force);
    }
    
    void UpdateRotationPhysics(){
        if(rb.linearVelocity.sqrMagnitude > 0.01f){
            Vector3 lookDirection = rb.linearVelocity.normalized;
            
            float wobbleAmount = 0.03f * Mathf.Sin(Time.time * 3f + uniquePhaseOffset);
            lookDirection += Random.insideUnitSphere * wobbleAmount * 0.05f;
            lookDirection.y = Mathf.Clamp(lookDirection.y, -0.2f, 0.2f);
            
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            float rotSpeed = rotationSpeed * personalitySpeedModifier * 0.8f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSpeed * Time.fixedDeltaTime * 0.02f);
        }
        else{
            float idleWobble = Mathf.Sin(Time.time * 0.5f + uniquePhaseOffset) * 1f;
            transform.Rotate(Vector3.up, idleWobble * Time.fixedDeltaTime);
        }
    }
    
    void ApplyHoverPhysics(){
        hoverPhase += Time.fixedDeltaTime * hoverFrequency * personalityHoverModifier;
        float hoverOffset = Mathf.Sin(hoverPhase + uniquePhaseOffset) * hoverAmplitude * personalityHoverModifier;
        float secondaryHover = Mathf.Sin(hoverPhase * 1.5f + uniquePhaseOffset * 0.5f) * hoverAmplitude * 0.2f;
        hoverOffset += secondaryHover;
        
        Vector3 hoverForce = Vector3.up * hoverOffset * 3f;
        rb.AddForce(hoverForce, ForceMode.Force);
    }
    
    void UpdateCombat(){
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if(distanceToPlayer <= attackRange){
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            directionToPlayer.y = 0;
            
            if(directionToPlayer.sqrMagnitude > 0.01f){
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, aimSpeed * Time.deltaTime);
            }
            
            fireTimer += Time.deltaTime;
            if(fireTimer >= fireRate){
                FireProjectile();
                fireTimer = 0f;
                fireRate = Random.Range(2f, 4f);
            }
        }
    }
    
    void FireProjectile(){
        if(projectilePrefab == null || firePoint == null || player == null) return;
        
        Vector3 predictedTarget = player.position;
        
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if(playerRb != null){
            Vector3 playerVel = playerRb.linearVelocity;
            float timeToTarget = Vector3.Distance(transform.position, player.position) / 30f;
            predictedTarget += playerVel * timeToTarget * shootingPredictionTime * shootingPredictionStrength;
        }
        else{
            float timeToTarget = Vector3.Distance(transform.position, player.position) / 30f;
            predictedTarget += playerVelocity * timeToTarget * shootingPredictionTime * shootingPredictionStrength;
        }
        
        Vector3 fireDirection = (predictedTarget - firePoint.position).normalized;
        
        float spread = Mathf.Lerp(0.5f, 3f, rb.linearVelocity.magnitude / (moveSpeed * 2f));
        fireDirection = Quaternion.Euler(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0f
        ) * fireDirection;
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
        projectile.SetActive(true);
        
        EnemyProjectile proj = projectile.GetComponent<EnemyProjectile>();
        if(proj != null){
            proj.Initialize(fireDirection, damage);
        }
        
        Destroy(projectile, 5f);
    }
    
    void UpdateVisuals(){
        if(engineParticles != null){
            var emission = engineParticles.emission;
            float speedRatio = rb.linearVelocity.magnitude / (moveSpeed * personalitySpeedModifier);
            emission.rateOverTime = Mathf.Lerp(10f, 50f, speedRatio);
        }
    }
    
    public void TakeDamage(float damageAmount){
        if(isDead) return;
        
        health -= damageAmount;
        
        if(visualModel != null) StartCoroutine(DamageFlash());
        
        Vector3 knockback = Random.insideUnitSphere * 3f;
        knockback.y = 1f;
        rb.AddForce(knockback, ForceMode.Impulse);
        
        if(health <= 0f) Die();
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
        
        if(engineParticles != null) engineParticles.Stop();
        if(visualModel != null) visualModel.SetActive(false);
        
        gameObject.SetActive(false);
    }
    
    void OnTriggerEnter(Collider other){
        if(isDead || !isActive) return;
        
        IDamageable damageable = other.GetComponent<IDamageable>();
        if(damageable != null && other.gameObject.layer != LayerMask.NameToLayer("Enemy")){
            if(other.transform == player) damageable.TakeDamage(damage * 0.5f);
        }
    }
    
    public bool IsAlive() => isActive && !isDead;
    
    void OnDisable(){
        isActive = false;
        StopAllCoroutines();
    }
}