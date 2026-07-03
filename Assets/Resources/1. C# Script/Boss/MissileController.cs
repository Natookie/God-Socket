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
    
    private bool isActive;
    private bool isExploded;
    private float lifeTimer;
    private Rigidbody rb;
    private BoxCollider missileCollider;
    private Building targetedBuilding;
    private int enemyLayerMask;
    
    void Awake(){
        rb = GetComponent<Rigidbody>();
        if(rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        missileCollider = GetComponent<BoxCollider>();
        enemyLayerMask = ~LayerMask.GetMask("Enemy");
        
        isActive = true;
        isExploded = false;
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
        
        if(visualModel != null) visualModel.SetActive(true);
        if(missileCollider != null) missileCollider.enabled = true;
    }
    
    void Update(){
        if(!isActive || isExploded) return;
        
        lifeTimer += Time.deltaTime;
        if(lifeTimer >= maxLifeTime){
            Explode();
            return;
        }
        
        if(target == null){
            Explode();
            return;
        }
        
        UpdateMovement();
        CheckImpact();
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
        
        if(targetedBuilding != null){
            BuildingManager.Instance.SetTargeted(targetedBuilding, false);
        }
        
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
        Debug.Log("dldd");
        if(collision.gameObject.layer == LayerMask.NameToLayer("Enemy")) return;
        
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if(damageable != null) Explode();
    }
    
    public bool IsExploded() => isExploded;
    
    void OnDisable(){
        isActive = false;
        StopAllCoroutines();
    }
}