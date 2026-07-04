using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class BossAimer : MonoBehaviour
{
    [Header("TARGETING")]
    public float rotationSpeed = 90f;
    public float idleRotationSpeed = 30f;
    
    [Header("GRAVITY AIM")]
    public Vector3 gravityAimRotation = new Vector3(-95, 0f, 0f);
    
    [Header("DEBUG")]
    public bool showDebugLogs = true;
    public GameObject debugTargetObject;
    
    private Transform target;
    private Building targetedBuilding;
    private bool hasTarget = false;
    private float idleAngle = 0f;
    [ReadOnly] public bool isIdle = true;
    private Quaternion targetRotation;
    private bool isGravityAiming = false;

    private const float DEFAULT_X_ROT = -90f;
    
    void Start(){
        targetRotation = transform.rotation;
        FindNearestTarget();
    }
    
    void Update(){
        if(isIdle){
            idleAngle += idleRotationSpeed * Time.deltaTime;
            if(idleAngle > 360f) idleAngle -= 360f;
            
            Quaternion idleRot = Quaternion.Euler(DEFAULT_X_ROT, idleAngle, 0f);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                idleRot,
                idleRotationSpeed * Time.deltaTime
            );
            return;
        }
        
        if(isGravityAiming){
            Quaternion gravityRot = Quaternion.Euler(gravityAimRotation);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                gravityRot,
                rotationSpeed * Time.deltaTime
            );
            return;
        }
        
        if(hasTarget && target != null){
            Building building = target.GetComponent<Building>();
            
            if(building == null || !building.IsAlive()){
                if(showDebugLogs) Debug.Log($"BossAimer: Target {target.name} is no longer valid!");
                ClearTarget();
                FindNearestTarget();
                return;
            }
            
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
            
            debugTargetObject = target.gameObject;
            return;
        }
        
        if(!hasTarget || target == null) FindNearestTarget();
    }
    
    public void AimGravity(){
        ClearTarget();
        isIdle = false;
        isGravityAiming = true;
        if(showDebugLogs) Debug.Log($"BossAimer: Aiming gravity rotation to {gravityAimRotation}");
    }
    
    void CalculateTargetRotation(Building building){
        Vector3 direction = building.transform.position - transform.position;
        
        if(direction.magnitude > 0.1f){
            Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
            Vector3 euler = lookRotation.eulerAngles;
            float xOffset = Mathf.DeltaAngle(DEFAULT_X_ROT, euler.x);
            euler.x = DEFAULT_X_ROT + Mathf.Clamp(xOffset, -10f, 10f);
            euler.z = 0f;
            targetRotation = Quaternion.Euler(euler);
        }
    }
    
    void FindNearestTarget(){
        if(BuildingManager.Instance == null){
            if(showDebugLogs) Debug.LogError("BossAimer: BuildingManager.Instance is null!");
            return;
        }
        
        if(BuildingManager.Instance.buildings == null || BuildingManager.Instance.buildings.Count == 0){
            if(showDebugLogs) Debug.LogWarning("BossAimer: No buildings found in BuildingManager!");
            return;
        }
        
        Building nearest = null;
        float nearestDistance = float.MaxValue;
        int availableCount = 0;
        
        foreach(Building building in BuildingManager.Instance.buildings){
            if(building == null) continue;
            
            if(!building.IsAlive()){
                if(showDebugLogs) Debug.Log($"BossAimer: Building {building.name} is destroyed, skipping...");
                continue;
            }
            
            if(building.isTargeted){
                if(showDebugLogs) Debug.Log($"BossAimer: Building {building.name} is already targeted, skipping...");
                continue;
            }
            
            availableCount++;
            float distance = Vector3.Distance(transform.position, building.transform.position);
            if(distance < nearestDistance){
                nearestDistance = distance;
                nearest = building;
            }
        }
        
        if(showDebugLogs) Debug.Log($"BossAimer: Found {availableCount} available buildings.");
        
        if(nearest != null){
            target = nearest.transform;
            targetedBuilding = nearest;
            hasTarget = true;
            isIdle = false;
            isGravityAiming = false;
            
            CalculateTargetRotation(nearest);
            BuildingManager.Instance.SetTargeted(nearest, true);
            if(showDebugLogs) Debug.Log($"BossAimer: NOW Targeting {nearest.name} at distance {nearestDistance}");
        }
        else{
            if(showDebugLogs) Debug.LogWarning("BossAimer: No available buildings found to target!");
            ClearTarget();
            isIdle = true;
        }
    }
    
    void ClearTarget(){
        if(targetedBuilding != null){
            BuildingManager.Instance.SetTargeted(targetedBuilding, false);
            if(showDebugLogs) Debug.Log($"BossAimer: Unmarked {targetedBuilding.name} as targeted");
        }
        
        target = null;
        targetedBuilding = null;
        hasTarget = false;
        debugTargetObject = null;
        isGravityAiming = false;
        targetRotation = transform.rotation;
    }
    
    public void SetIdle(bool idle){
        isIdle = idle;
        isGravityAiming = false;
        if(idle) ClearTarget();
        else FindNearestTarget();
    }
    
    public Vector3 GetAimDirection() => transform.forward;
    public Transform GetTarget() => target;
    public bool HasTarget() => hasTarget && target != null;
    public bool IsIdle() => isIdle;
    
    public void ResetAiming(){
        ClearTarget();
        isIdle = true;
        isGravityAiming = false;
        targetRotation = transform.rotation;
        if(showDebugLogs) Debug.Log("BossAimer: Aiming reset");
    }
    
    void OnDrawGizmosSelected(){
        if(target != null){
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position, 0.5f);
            
            UnityEditor.Handles.Label(target.position + Vector3.up * 1f, $"Target: {target.name}");
        }
        else{
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "NO TARGET");
        }
        
        if(BuildingManager.Instance != null){
            foreach(Building building in BuildingManager.Instance.buildings){
                if(building == null) continue;
                
                if(building.IsAlive()){
                    if(building.isTargeted){
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(building.transform.position, 0.5f);
                        UnityEditor.Handles.Label(building.transform.position + Vector3.up * 0.5f, $"TARGETED: {building.name}");
                    }
                    else{
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireSphere(building.transform.position, 0.3f);
                    }
                }
                else{
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireSphere(building.transform.position, 0.2f);
                    UnityEditor.Handles.Label(building.transform.position + Vector3.up * 0.5f, $"DESTROYED: {building.name}");
                }
            }
        }
    }
}