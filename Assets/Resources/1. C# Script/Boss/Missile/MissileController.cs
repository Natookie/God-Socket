using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MissileController : MonoBehaviour
{
    [Header("MOVEMENT")]
    public float speed = 20f;
    public float rotationSpeed = 200f;
    
    [Header("COMBAT")]
    public float damage = 30f;
    public float explosionRadius = 4f;
    public float maxLifeTime = 5f;
    public Transform target;
    
    [Header("VISUAL")]
    public GameObject visualModel;
    public ParticleSystem explosionEffect;
    
    [Header("RIDING")]
    public bool isBeingRidden = false;
    public Transform ridePosition;
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
    
    void Awake(){
        rb = GetComponent<Rigidbody>();
        if(rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        missileCollider = GetComponent<BoxCollider>();
        enemyLayerMask = ~LayerMask.GetMask("Enemy");
        
        isActive = true;
        isExploded = false;
        
        if(ridePosition == null){
            ridePosition = new GameObject("RidePosition").transform;
            ridePosition.parent = transform;
            ridePosition.localPosition = new Vector3(0, 0.5f, -0.5f);
        }
        
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
        
        target = building.transform;
        isActive = true;
        isExploded = false;
        lifeTimer = 0f;
        isPlayerControlled = false;
        isGoingStraight = false;
        
        if(visualModel != null) visualModel.SetActive(true);
        if(missileCollider != null) missileCollider.enabled = true;
    }
    
    public Building GetTargetBuilding(){
        return targetedBuilding;
    }
    
    void Update(){
        if(!isActive || isExploded) return;
        
        lifeTimer += Time.deltaTime;
        if(lifeTimer >= maxLifeTime){
            Explode();
            return;
        }
        
        if(!hasRider){
            CheckForNearbyPlayer();
        }
        
        if(isPlayerControlled && hasRider){
            UpdatePlayerControlledMovement();
            CheckImpact();
            
            if(Input.GetKeyDown(KeyCode.Space)){
                DismountPlayer();
            }
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
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, rideDetectionRadius);
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
        if(playerRb != null){
            playerRb.isKinematic = false;
            playerRb.linearVelocity = transform.forward * dismountLaunchForce + Vector3.up * dismountLaunchUpForce;
        }
        
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
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        if(distanceToTarget < 1.5f){
            Explode();
            return;
        }
        
        RaycastHit hit;
        if(Physics.Raycast(transform.position, transform.forward, out hit, speed * Time.deltaTime + 0.5f, enemyLayerMask)){
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if(damageable != null){
                Explode();
                return;
            }
            
            if((hasRider || isGoingStraight) && hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy")){
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
        
        if(explosionEffect != null){
            ParticleSystem explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            explosion.Play();
            Destroy(explosion.gameObject, explosion.main.duration + 1f);
        }
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach(Collider col in colliders){
            IDamageable damageable = col.GetComponent<IDamageable>();
            if(damageable != null){
                Vector3 closestPoint = col.ClosestPoint(transform.position);
                float distance = Vector3.Distance(transform.position, closestPoint);
                float damageMultiplier = Mathf.Clamp01(1f - (distance / explosionRadius));
                float actualDamage = damage * Mathf.Max(damageMultiplier, 0.1f);
                damageable.TakeDamage(actualDamage);
            }
        }
        
        if(visualModel != null) visualModel.SetActive(false);
        if(missileCollider != null) missileCollider.enabled = false;
        
        gameObject.SetActive(false);
    }

    void OnCollisionEnter(Collision collision){
        if(isExploded || !isActive) return;
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if(damageable != null) Explode();
    }
    void OnTriggerEnter(Collider other){
        if(isExploded || !isActive) return;
        IDamageable damageable = other.GetComponent<IDamageable>();
        if(damageable != null) Explode();
    }
    
    public bool IsExploded() => isExploded;
    public bool HasRider() => hasRider;
    public bool IsPlayerControlled() => isPlayerControlled;
    
    public void ManualDismount(){
        if(hasRider) DismountPlayer();
    }
    
    void OnDisable(){
        isActive = false;
        StopAllCoroutines();
        
        if(hasRider) DismountPlayer();
        if(targetedBuilding != null && !isExploded) BuildingManager.Instance.SetTargeted(targetedBuilding, false);
    }
}