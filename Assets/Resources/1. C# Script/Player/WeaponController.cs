using UnityEngine;
using System.Collections.Generic;

public class WeaponController : MonoBehaviour
{
    [Header("SHOOTING")]
    public float fireRate = 0.2f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 50f;

    [Header("POOLING")]
    public Transform poolParent;
    public int poolSize = 20;

    [Header("REFERENCES")]
    public InputHandler input;
    public EnergySystem energy;
    public Camera playerCamera;

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
    }

    void Update(){
        if(isOverheated || inputDisabled) return;
        if(input.FireHeld && Time.time >= nextFireTime) TryFire();
    }

    void TryFire(){
        if(!energy.CanAfford(energy.shotCost)) return;

        energy.Consume(energy.shotCost);
        if(projectilePrefab && firePoint && playerCamera){
            GameObject proj = GetPooledProjectile();
            proj.transform.position = firePoint.position;
            proj.transform.rotation = Quaternion.LookRotation(playerCamera.transform.forward);
            proj.SetActive(true);
            Rigidbody projRb = proj.GetComponent<Rigidbody>();
            if(projRb) projRb.linearVelocity = playerCamera.transform.forward * projectileSpeed;
        }

        nextFireTime = Time.time + fireRate;
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