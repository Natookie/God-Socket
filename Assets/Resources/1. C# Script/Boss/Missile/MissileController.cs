using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MissileController : MonoBehaviour, IDamageable
{
    [Header("MOVEMENT")]
    public float speed = 20f;
    public float rotationSpeed = 200f;

    [Header("HEALTH")]
    public float health = 50f;
    
    [Header("COMBAT")]
    public float damage = 30f;
    public float explosionRadius = 4f;
    public float maxLifeTime = 5f;
    public Transform target;
    public float enemyDamageMultiplier = 3f;
    
    [Header("VISUAL")]
    public GameObject visualModel;
    public ParticleSystem explosionEffect;
    
    [Header("RIDING")]
    public bool isBeingRidden = false;
    public Transform ridePosition;
    public Transform detectionPoint;
    public float rideDetectionRadius = 3f;
    public float mouseSensitivity = 2f;
    public float yawSpeed = 180f;
    public float pitchSpeed = 120f;
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public float dismountLaunchForce = 10f;
    public float dismountLaunchUpForce = 5f;
    public float mountCooldown = 3f;
    
    private bool isActive;
    private bool isExploded;
    private float lifeTimer;
    private Rigidbody rb;
    private BoxCollider missileCollider;
    private Building targetedBuilding;
    private int enemyLayerMask;
    
    private GameObject rider;
    private bool hasRider = false;
    private bool isPlayerControlled = false;
    private float lastDismountTime = -999f;
    private bool isGoingStraight = false;
    private Vector3 straightDirection;
    
    private float yaw;
    private float pitch;
    private InputHandler playerInput;
    private bool hitEnemy = false;
    
    void Awake(){
        rb = GetComponent<Rigidbody>();
        if(rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        
        missileCollider = GetComponent<BoxCollider>();
        enemyLayerMask = ~LayerMask.GetMask("Projectile", "Player");
        
        isActive = true;
        isExploded = false;
        
        if(ridePosition == null){
            ridePosition = new GameObject("RidePosition").transform;
            ridePosition.parent = transform;
            ridePosition.localPosition = new Vector3(0, 0.5f, -0.5f);
        }
        
        if(detectionPoint == null) detectionPoint = transform;
        Vector3 angles = transform.rotation.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        if(pitch > 180f) pitch -= 360f;
    }
    
    public void Initialize(Building building){
        if(building == null){
            Explode();
            return;
        }
        
        targetedBuilding = building;
        BuildingManager.Instance.SetTargeted(building, true);
        
        health = 50f;
        damage = 100f;
        target = building.transform;
        isActive = true;
        isExploded = false;
        lifeTimer = 0f;
        isPlayerControlled = false;
        isGoingStraight = false;
        hitEnemy = false;
        
        if(visualModel != null) visualModel.SetActive(true);
        if(missileCollider != null) missileCollider.enabled = true;
    }
    
    public Building GetTargetBuilding(){
        return targetedBuilding;
    }
    
    void Update(){
        if(!isActive || isExploded) return;
        if(!hasRider) CheckForNearbyPlayer();
        if(isPlayerControlled && hasRider){
            UpdatePlayerControlledMovement();
            CheckImpact();
            
            if(Input.GetKeyDown(KeyCode.Space)) DismountPlayer();
        }
        else if(isGoingStraight){
            UpdateStraightMovement();
            CheckImpact();
        }
        else if(!isBeingRidden && !hasRider){
            if(target == null){
                Explode();
                return;
            }
            UpdateMovement();
            CheckImpact();
        }
    }
    
    void CheckForNearbyPlayer(){
        if(Time.time - lastDismountTime < mountCooldown) return;
        
        Vector3 detectionPos = transform.position;
        Collider[] colliders = Physics.OverlapSphere(detectionPos, rideDetectionRadius);
        foreach(Collider col in colliders){
            if(col.CompareTag("Player")){
                MovementController playerController = col.GetComponent<MovementController>();
                if(playerController != null && !playerController.IsRiding()){
                    MountPlayer(col.gameObject);
                    break;
                }
            }
        }
    }
    
    void MountPlayer(GameObject player){
        if(hasRider) return;
        
        rider = player;
        hasRider = true;
        isBeingRidden = true;
        isPlayerControlled = true;
        isGoingStraight = false;

        if(this.gameObject.layer == 9) gameObject.layer = 6;

        Collider playerCollider = rider.GetComponent<Collider>();
        if(playerCollider != null && missileCollider != null) Physics.IgnoreCollision(playerCollider, missileCollider, true);
        
        rider.transform.SetParent(transform);
        rider.transform.localPosition = ridePosition.localPosition;
        rider.transform.localRotation = Quaternion.identity;

        Rigidbody playerRb = rider.GetComponent<Rigidbody>();
        if(playerRb != null) playerRb.isKinematic = true;
        
        MovementController playerController = rider.GetComponent<MovementController>();
        if(playerController != null){
            playerController.SetRiding(true);
            playerController.SetRidingMissile(this);
            playerInput = playerController.input;
        }
        
        Vector3 currentAngles = transform.rotation.eulerAngles;
        yaw = currentAngles.y;
        pitch = currentAngles.x;
        if(pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
    
    void UpdatePlayerControlledMovement(){
        if(rider == null){
            DismountPlayer();
            return;
        }
        
        if(playerInput == null){
            MovementController playerController = rider.GetComponent<MovementController>();
            if(playerController != null) playerInput = playerController.input;
            if(playerInput == null){
                DismountPlayer();
                return;
            }
        }
        
        if(playerInput.CursorVisible) return;
        
        yaw += playerInput.MouseX * yawSpeed * mouseSensitivity * Time.deltaTime;
        pitch -= playerInput.MouseY * pitchSpeed * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        
        Vector3 forward = transform.forward;
        Vector3 movement = forward * speed * Time.deltaTime;
        transform.position += movement;
        
        if(rider != null){
            rider.transform.localPosition = ridePosition.localPosition;
            rider.transform.localRotation = Quaternion.identity;
        }
    }
    
    void DismountPlayer(){
        if(!hasRider || rider == null) return;
        
        Collider playerCollider = rider.GetComponent<Collider>();
        if(playerCollider != null && missileCollider != null){
            Physics.IgnoreCollision(playerCollider, missileCollider, false);
        }
        
        straightDirection = transform.forward;
        isGoingStraight = true;
        gameObject.layer = LayerMask.NameToLayer("Projectile");
        
        MovementController playerController = rider.GetComponent<MovementController>();
        if(playerController != null){
            playerController.SetRiding(false);
            playerController.SetRidingMissile(null);
        }
        
        rider.transform.SetParent(null);
        
        Rigidbody playerRb = rider.GetComponent<Rigidbody>();
        if(playerRb != null) playerRb.linearVelocity = transform.forward * dismountLaunchForce + Vector3.up * dismountLaunchUpForce;
        
        lastDismountTime = Time.time;
        
        hasRider = false;
        isBeingRidden = false;
        isPlayerControlled = false;
        rider = null;
        playerInput = null;
    }
    
    void UpdateStraightMovement(){
        transform.position += straightDirection * speed * Time.deltaTime;
    }
    
    void UpdateMovement(){
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        transform.position += directionToTarget * speed * Time.deltaTime;
        
        if(directionToTarget.sqrMagnitude > 0.01f){
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void CheckImpact(){
        Vector3 detectionPos = detectionPoint != null ? detectionPoint.position : transform.position;
        float distanceToTarget = Vector3.Distance(detectionPos, target.position);
        if(distanceToTarget < .5f){
            Explode();
            return;
        }
        
        RaycastHit hit;
        if(Physics.Raycast(detectionPos, transform.forward, out hit, speed * Time.deltaTime + 0.5f, enemyLayerMask)){
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if(damageable != null){
                if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy")){
                    hitEnemy = true;
                }
                Explode();
                return;
            }
            
            if((hasRider || isGoingStraight) && hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy")){
                hitEnemy = true;
                Explode();
                return;
            }

            if(hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground") ||
               hit.collider.gameObject.layer == LayerMask.NameToLayer("Building")){
                Explode();
                return;
            }
        }
    }
    
    void Explode(){
        if(isExploded) return;
        isExploded = true;
        isActive = false;
        
        if(hasRider) DismountPlayer();
        
        isBeingRidden = false;
        if(targetedBuilding != null) BuildingManager.Instance.SetTargeted(targetedBuilding, false);
        
        Vector3 explosionCenter = detectionPoint != null ? detectionPoint.position : transform.position;
        
        if(explosionEffect != null){
            ParticleSystem explosion = Instantiate(explosionEffect, explosionCenter, Quaternion.identity);
            explosion.Play();
            Destroy(explosion.gameObject, explosion.main.duration + 1f);
        }
        
        float finalDamage = damage;
        if(hitEnemy){
            finalDamage *= enemyDamageMultiplier;
        }
        
        Collider[] colliders = Physics.OverlapSphere(explosionCenter, explosionRadius);
        foreach(Collider col in colliders){
            if(col.gameObject == gameObject) continue;
            if(col.CompareTag("Player")) continue;
            IDamageable damageable = col.GetComponent<IDamageable>();
            if(damageable != null){
                Vector3 closestPoint = col.ClosestPoint(explosionCenter);
                float distance = Vector3.Distance(explosionCenter, closestPoint);
                float damageMultiplier = Mathf.Clamp01(1f - (distance / explosionRadius));
                float actualDamage = finalDamage * Mathf.Max(damageMultiplier, 0.1f);
                damageable.TakeDamage(actualDamage);
            }
        }
        
        if(visualModel != null) visualModel.SetActive(false);
        if(missileCollider != null) missileCollider.enabled = false;
        
        CameraController.Instance.ShakeCamera(1.5f, 1f, CameraController.ShakePriority.Critical);
        gameObject.SetActive(false);
    }

    void OnCollisionEnter(Collision collision){
        if(isExploded || !isActive) return;
        if(collision.gameObject.CompareTag("Player")) return;
        
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if(damageable != null){
            if(collision.gameObject.layer == LayerMask.NameToLayer("Enemy")){
                hitEnemy = true;
            }
            Explode();
        }
    }
    
    public bool IsExploded() => isExploded;
    public bool HasRider() => hasRider;
    public bool IsPlayerControlled() => isPlayerControlled;
    
    public void ManualDismount(){
        if(hasRider) DismountPlayer();
    }

    public void TakeDamage(float amount){
        health -= amount;
        if(health <= 0){
            damage = 0f;
            Explode();
        }
    }
    public bool IsAlive() => true;
    
    void OnDisable(){
        isActive = false;
        StopAllCoroutines();
        
        if(hasRider) DismountPlayer();
        if(targetedBuilding != null && !isExploded) BuildingManager.Instance.SetTargeted(targetedBuilding, false);
    }

    void OnDrawGizmosSelected(){
        Vector3 detectionPos = detectionPoint != null ? detectionPoint.position : transform.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(detectionPos, rideDetectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionPos, explosionRadius);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(detectionPos, transform.forward * (speed * Time.deltaTime + 0.5f));
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(detectionPos, 0.5f);
    }
}