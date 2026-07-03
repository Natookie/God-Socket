using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class BossAimer : MonoBehaviour
{
    [Header("TARGETING")]
    public float rotationSpeed = 90f;
    public float aimOffset = 0f;
    
    [Header("DEBUG")]
    public bool showDebugLogs = true;
    public GameObject debugTargetObject;
    
    private Transform target;
    private Quaternion targetRotation;
    private Building targetedBuilding;
    private bool hasTarget = false;
    
    void Start(){
        targetRotation = transform.rotation;
        FindNearestTarget();
    }
    
    void Update(){
        if(hasTarget && target != null){
            Building building = target.GetComponent<Building>();
            
            if(building == null || !building.IsAlive()){
                if(showDebugLogs) Debug.Log($"BossAimer: Target {target.name} is no longer valid!");
                ClearTarget();
                FindNearestTarget();
                return;
            }
            
            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            
            if(direction.magnitude > 0.1f){
                targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
            
            debugTargetObject = target.gameObject;
            return;
        }
        
        if(!hasTarget || target == null){
            if(showDebugLogs) Debug.Log("BossAimer: No target, finding nearest...");
            FindNearestTarget();
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
            
            BuildingManager.Instance.SetTargeted(nearest, true);
            if(showDebugLogs) Debug.Log($"BossAimer: NOW Targeting {nearest.name} at distance {nearestDistance}");
        }
        else{
            if(showDebugLogs) Debug.LogWarning("BossAimer: No available buildings found to target!");
            ClearTarget();
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
    }
    
    public Vector3 GetAimDirection() => transform.forward;
    public Transform GetTarget() => target;
    public bool HasTarget() => hasTarget && target != null;
    
    public void ResetAiming(){
        ClearTarget();
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