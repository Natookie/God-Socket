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
        
        if(playerTransform == null){
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if(player != null) playerTransform = player.transform;
        }
    }

    void Update(){
        if(isOverheated || inputDisabled) return;
        if(input.FireHeld && Time.time >= nextFireTime) TryFire();
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
            CameraController.Instance.ShakeCamera(1f, 0.2f, CameraController.ShakePriority.Low);
            
            Rigidbody projRb = proj.GetComponent<Rigidbody>();
            if(projRb) projRb.linearVelocity = aimDirection * projectileSpeed;
        }

        nextFireTime = Time.time + fireRate;
        AudioManager.Instance.PlaySFX(GameSFX.PlayerGun);
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