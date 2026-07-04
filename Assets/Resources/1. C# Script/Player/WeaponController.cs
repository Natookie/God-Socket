using UnityEngine;
using System.Collections.Generic;

public class WeaponController : MonoBehaviour
{
    [Header("SHOOTING")]
    public float fireRate = 0.2f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 50f;
    public float maxRange = 100f;

    [Header("POOLING")]
    public Transform poolParent;
    public int poolSize = 20;

    [Header("PARTICLE EFFECT")]
    public ParticleSystem muzzleFlashParticle;
    public float particleDuration = 0.5f;

    [Header("REFERENCES")]
    public InputHandler input;
    public EnergySystem energy;
    public Camera playerCamera;
    public Transform playerTransform;

    private float nextFireTime;
    private bool isOverheated = false;
    private bool inputDisabled = false;
    private List<GameObject> projectilePool;
    private int currentPoolIndex = 0;
    
    private Queue<ParticleSystem> particlePool = new Queue<ParticleSystem>();
    private Queue<float> particleActiveTimes = new Queue<float>();

    public void SetOverheated(bool overheated) => isOverheated = overheated;
    public void SetInputDisabled(bool disabled) => inputDisabled = disabled;

    void Start(){
        projectilePool = new List<GameObject>();
        for(int i = 0; i < poolSize; i++){
            GameObject proj = Instantiate(projectilePrefab);
            proj.transform.SetParent(poolParent);
            proj.SetActive(false);
            projectilePool.Add(proj);
        }
        
        if(muzzleFlashParticle != null){
            for(int i = 0; i < 2; i++){
                ParticleSystem ps = Instantiate(muzzleFlashParticle, firePoint.position, firePoint.rotation);
                ps.transform.SetParent(firePoint);
                ps.gameObject.SetActive(false);
                particlePool.Enqueue(ps);
                particleActiveTimes.Enqueue(0f);
            }
        }
        
        if(playerTransform == null){
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if(player != null) playerTransform = player.transform;
        }
    }

    void Update(){
        if(isOverheated || inputDisabled) return;
        if(input.FireHeld && Time.time >= nextFireTime) TryFire();
        
        UpdateParticles();
    }

    void TryFire(){
        if(!energy.CanAfford(energy.shotCost)) return;

        energy.Consume(energy.shotCost);
        if(projectilePrefab && firePoint && playerCamera){
            Vector3 aimDirection = GetAimDirection();
            
            GameObject proj = GetPooledProjectile();
            proj.transform.position = firePoint.position;
            proj.transform.rotation = Quaternion.LookRotation(aimDirection);
            proj.SetActive(true);
            
            Rigidbody projRb = proj.GetComponent<Rigidbody>();
            if(projRb) projRb.linearVelocity = aimDirection * projectileSpeed;
            
            PlayMuzzleFlash();
            
            CameraController.Instance.ShakeCamera(1f, 0.2f, CameraController.ShakePriority.Low);
        }

        nextFireTime = Time.time + fireRate;
        AudioManager.Instance.PlaySFX(GameSFX.PlayerGun);
    }

    void PlayMuzzleFlash(){
        if(muzzleFlashParticle == null) return;
        
        ParticleSystem ps = GetPooledParticle();
        if(ps != null){
            ps.transform.position = firePoint.position;
            ps.transform.rotation = firePoint.rotation;
            ps.gameObject.SetActive(true);
            ps.Play();
            
            float deactivateTime = Time.time + particleDuration;
            particleActiveTimes.Enqueue(deactivateTime);
        }
    }

    ParticleSystem GetPooledParticle(){
        if(particlePool.Count > 0){
            ParticleSystem ps = particlePool.Dequeue();
            if(!ps.gameObject.activeSelf) return ps;
            else{
                particlePool.Enqueue(ps);
                ParticleSystem newPS = Instantiate(muzzleFlashParticle, firePoint.position, firePoint.rotation);
                newPS.transform.SetParent(firePoint);
                newPS.gameObject.SetActive(false);
                return newPS;
            }
        }
        else{
            ParticleSystem newPS = Instantiate(muzzleFlashParticle, firePoint.position, firePoint.rotation);
            newPS.transform.SetParent(firePoint);
            newPS.gameObject.SetActive(false);
            return newPS;
        }
    }

    void UpdateParticles(){
        while(particleActiveTimes.Count > 0 && Time.time >= particleActiveTimes.Peek()){
            particleActiveTimes.Dequeue();
            
            if(particlePool.Count > 0){
                ParticleSystem ps = particlePool.Dequeue();
                if(ps != null && ps.gameObject.activeSelf){
                    ps.Stop();
                    ps.gameObject.SetActive(false);
                }
                particlePool.Enqueue(ps);
            }
        }
    }

    Vector3 GetAimDirection(){
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);
        
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, maxRange)){
            Vector3 direction = (hit.point - firePoint.position).normalized;
            return direction;
        }
        else{
            Vector3 targetPoint = ray.GetPoint(maxRange);
            Vector3 direction = (targetPoint - firePoint.position).normalized;
            return direction;
        }
    }

    GameObject GetPooledProjectile(){
        for(int i = 0; i < projectilePool.Count; i++){
            int index = (currentPoolIndex + i) % projectilePool.Count;
            if(!projectilePool[index].activeInHierarchy){
                currentPoolIndex = (index + 1) % projectilePool.Count;
                return projectilePool[index];
            }
        }

        GameObject proj = Instantiate(projectilePrefab);
        proj.SetActive(false);
        projectilePool.Add(proj);
        return proj;
    }
}