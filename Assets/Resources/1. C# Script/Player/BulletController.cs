using UnityEngine;
using System.Collections.Generic;

public class BulletController : MonoBehaviour
{
    [Header("SETTINGS")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private ParticleSystem hitEffectPrefab;

    [Header("POOLING")]
    [SerializeField] private int hitEffectPoolSize = 10;
    [SerializeField] private float hitEffectLifetime = 2f;

    [Header("TRAIL")]
    [SerializeField] private TrailRenderer trailRenderer;

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
        if(trailRenderer != null){
            trailRenderer.Clear();
            trailRenderer.enabled = true;
        }
    }

    void Update(){
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

    void OnCollisionEnter(Collision collision){
        IDamageable damageable = collision.collider.GetComponent<IDamageable>();
        if(damageable != null) damageable.TakeDamage(damage);

        if(trailRenderer != null) trailRenderer.enabled = false;
        if(hitEffectPrefab != null){
            ContactPoint contact = collision.GetContact(0);
            PlayHitEffect(contact.point, contact.normal);
        }

        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other){
        IDamageable damageable = other.GetComponent<IDamageable>();
        if(damageable != null) damageable.TakeDamage(damage);

        if(trailRenderer != null) trailRenderer.enabled = false;
        if(hitEffectPrefab != null){
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).normalized;
            PlayHitEffect(hitPoint, hitNormal);
        }

        gameObject.SetActive(false);
    }

    void OnDisable(){
        if(trailRenderer != null) trailRenderer.enabled = false;
    }

    void PlayHitEffect(Vector3 position, Vector3 normal){
        ParticleSystem ps = GetPooledHitEffect();
        if(ps != null){
            ps.transform.position = position;
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
}