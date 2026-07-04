using UnityEngine;
using System.Collections.Generic;

public class EnemyProjectile : MonoBehaviour
{
    [Header("SETTINGS")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float speed = 20f;
    [SerializeField] private float maxLifeTime = 5f;
    [SerializeField] private ParticleSystem hitEffectPrefab;
    
    [Header("POOLING")]
    [SerializeField] private int hitEffectPoolSize = 10;
    [SerializeField] private float hitEffectLifetime = 2f;
    
    [Header("TRAIL")]
    [SerializeField] private TrailRenderer trailRenderer;
    
    private Vector3 direction;
    private float lifeTimer;
    private List<ParticleSystem> hitEffectPool;
    private int currentHitEffectIndex = 0;
    private Dictionary<ParticleSystem, float> particleTimers = new Dictionary<ParticleSystem, float>();
    
    void Start(){
        if(hitEffectPrefab != null){
            hitEffectPool = new List<ParticleSystem>();
            for(int i = 0; i < hitEffectPoolSize; i++){
                ParticleSystem ps = Instantiate(hitEffectPrefab);
                ps.gameObject.SetActive(false);
                hitEffectPool.Add(ps);
            }
        }
        
        if(trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
    }
    
    void OnEnable(){
        lifeTimer = 0f;
        
        if(trailRenderer != null){
            trailRenderer.Clear();
            trailRenderer.enabled = true;
        }
    }
    
    void Update(){
        lifeTimer += Time.deltaTime;
        
        if(lifeTimer >= maxLifeTime){
            ReturnToPool();
            return;
        }
        
        transform.position += direction * speed * Time.deltaTime;
        
        if(hitEffectPool != null){
            List<ParticleSystem> toRemove = new List<ParticleSystem>();
            foreach(var kvp in particleTimers){
                if(Time.time >= kvp.Value){
                    kvp.Key.Stop();
                    kvp.Key.gameObject.SetActive(false);
                    toRemove.Add(kvp.Key);
                }
            }
            foreach(var ps in toRemove) particleTimers.Remove(ps);
        }
    }
    
    public void Initialize(Vector3 fireDirection, float projectileDamage){
        direction = fireDirection.normalized;
        damage = projectileDamage;
        transform.rotation = Quaternion.LookRotation(direction);
    }
    
    void OnTriggerEnter(Collider other){
        if(other.gameObject == gameObject) return;
        
        IDamageable damageable = other.GetComponent<IDamageable>();
        if(damageable != null){
            damageable.TakeDamage(damage);
        }
        
        if(trailRenderer != null) trailRenderer.enabled = false;
        
        if(hitEffectPrefab != null){
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).normalized;
            PlayHitEffect(hitPoint, hitNormal);
        }
        
        ReturnToPool();
    }
    
    void OnCollisionEnter(Collision collision){
        IDamageable damageable = collision.collider.GetComponent<IDamageable>();
        if(damageable != null){
            damageable.TakeDamage(damage);
        }
        
        if(trailRenderer != null) trailRenderer.enabled = false;
        
        if(hitEffectPrefab != null){
            ContactPoint contact = collision.GetContact(0);
            PlayHitEffect(contact.point, contact.normal);
        }
        
        ReturnToPool();
    }
    
    void OnDisable(){
        if(trailRenderer != null) trailRenderer.enabled = false;
    }
    
    void PlayHitEffect(Vector3 position, Vector3 normal){
        ParticleSystem ps = GetPooledHitEffect();
        if(ps != null){
            ps.transform.position = position;
            ps.transform.rotation = Quaternion.LookRotation(normal);
            ps.gameObject.SetActive(true);
            ps.Play();
            particleTimers[ps] = Time.time + hitEffectLifetime;
        }
    }
    
    ParticleSystem GetPooledHitEffect(){
        for(int i = 0; i < hitEffectPool.Count; i++){
            int index = (currentHitEffectIndex + i) % hitEffectPool.Count;
            if(!hitEffectPool[index].gameObject.activeSelf){
                currentHitEffectIndex = (index + 1) % hitEffectPool.Count;
                return hitEffectPool[index];
            }
        }
        
        ParticleSystem ps = Instantiate(hitEffectPrefab);
        ps.gameObject.SetActive(false);
        hitEffectPool.Add(ps);
        return ps;
    }
    
    void ReturnToPool(){
        gameObject.SetActive(false);
    }
}