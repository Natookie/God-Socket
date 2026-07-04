using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class Building : MonoBehaviour, IDamageable
{
    [Header("STATUS")]
    public float health = 20f;

    [Header("DESTRUCTION")]
    [ReadOnly] public float explosionForce = 25f;
    [ReadOnly] public float explosionRadius = 25f;
    [ReadOnly] public float upwardsModifier = 5f;
    [Space(10)]
    public bool isTargeted = false;
    private bool hasChild => transform.childCount > 0;

    [Header("EXPLOSION EFFECT")]
    public ParticleSystem explosionParticlePrefab;
    public float explosionParticleLifetime = 2f;

    private BoxCollider col;
    private Renderer fullMesh;
    private bool destroyed;
    private List<ParticleSystem> explosionParticlePool;
    private int currentPoolIndex = 0;
    private Dictionary<ParticleSystem, float> particleTimers = new Dictionary<ParticleSystem, float>();

    void Awake(){
        fullMesh = GetComponent<Renderer>();
        col = GetComponent<BoxCollider>();
        
        for(int i = 0; i < transform.childCount; i++){
            Transform piece = transform.GetChild(i);
            piece.gameObject.SetActive(false);
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if(rb != null){
                rb.isKinematic = true;
                rb.mass = 3f;
            }
        }

        if(explosionParticlePrefab != null){
            explosionParticlePool = new List<ParticleSystem>();
            for(int i = 0; i < 5; i++){
                ParticleSystem ps = Instantiate(explosionParticlePrefab);
                ps.gameObject.SetActive(false);
                explosionParticlePool.Add(ps);
            }
        }
    }

    void Update(){
        if(particleTimers.Count > 0){
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

    public void TakeDamage(float damage){
        health -= damage;
        if(health <= 0){
            BuildingManager.Instance.SetDestroyed(this);
            CameraController.Instance.ShakeCamera(1.5f, 1f, CameraController.ShakePriority.Critical);
            DestroyBuilding();
            PlayExplosionParticles();
        }
    }

    public void DestroyBuilding(){
        if(destroyed) return;
        col.enabled = false;
        fullMesh.enabled = false;
        destroyed = true;
        if(!hasChild) return;

        Vector3 explosionOrigin = transform.position + Vector3.up * 2f;
        for(int i = 0; i < transform.childCount; i++){
            Transform piece = transform.GetChild(i);
            piece.gameObject.SetActive(true);
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if(rb != null){
                rb.isKinematic = false;
                rb.AddExplosionForce(
                    explosionForce,
                    explosionOrigin,
                    explosionRadius,
                    upwardsModifier,
                    ForceMode.Impulse
                );
            }
        }
    }

    void PlayExplosionParticles(){
        if(explosionParticlePrefab == null) return;

        Bounds bounds = GetBuildingBounds();
        Vector3 bottomCenter = new Vector3(
            bounds.center.x,
            bounds.min.y,
            bounds.center.z
        );

        ParticleSystem ps = GetPooledExplosionParticle();
        if(ps != null){
            ps.transform.position = bottomCenter;
            ps.transform.rotation = Quaternion.identity;
            ps.gameObject.SetActive(true);
            ps.Play();
            particleTimers[ps] = Time.time + explosionParticleLifetime;
        }
    }

    ParticleSystem GetPooledExplosionParticle(){
        for(int i = 0; i < explosionParticlePool.Count; i++){
            int index = (currentPoolIndex + i) % explosionParticlePool.Count;
            if(!explosionParticlePool[index].gameObject.activeSelf){
                currentPoolIndex = (index + 1) % explosionParticlePool.Count;
                return explosionParticlePool[index];
            }
        }

        ParticleSystem ps = Instantiate(explosionParticlePrefab);
        ps.gameObject.SetActive(false);
        explosionParticlePool.Add(ps);
        return ps;
    }

    Bounds GetBuildingBounds(){
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach(Renderer renderer in renderers){
            if(renderer != null && renderer.enabled){
                if(bounds.size == Vector3.zero) bounds = renderer.bounds;
                else bounds.Encapsulate(renderer.bounds);
            }
        }
        return bounds;
    }

    public void SetupBuilding(Material ditherMaterial){
        if(fullMesh == null) fullMesh = GetComponent<Renderer>();
        fullMesh.gameObject.layer = LayerMask.NameToLayer("Building");

        if(col == null){
            col = gameObject.GetComponent<BoxCollider>();
            if(col == null) col = gameObject.AddComponent<BoxCollider>();
        }

        for(int i = 0; i < transform.childCount; i++){
            Transform piece = transform.GetChild(i);
            
            MeshCollider meshCol = piece.GetComponent<MeshCollider>();
            if(meshCol == null) meshCol = piece.gameObject.AddComponent<MeshCollider>();
            meshCol.convex = true;
            
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if(rb == null) rb = piece.gameObject.AddComponent<Rigidbody>();
            if(rb != null) rb.isKinematic = true;
            
            piece.gameObject.SetActive(false);
        }
    }

    public bool IsAlive() => !destroyed;
}