using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("CAMERAS")]
    public CinemachineCamera normalCamera;
    public CinemachineCamera aimCamera;

    [Header("REFERENCES")]
    public InputHandler input;
    public PlayerRotation playerRotation;

    [Header("PRIORITIES")]
    public int activePriority = 20;
    public int inactivePriority = 10;

    [Header("DUTCH")]
    public float maxDutch = 5f;
    public float yawRateForMaxDutch = 360f;
    public float dutchSmoothSpeed = 8f;

    [Header("AUTO AIM")]
    public bool enableAutoAim = true;
    public float autoAimRange = 50f;
    public LayerMask enemyLayer = 1 << 6;
    public float maxAimAngle = 60f;
    public float minAimDistance = 2f;
    public float obstructionCheckRadius = 0.5f;

    [Header("CAMERA OFFSET")]
    public Transform cameraTransform;
    public float cameraHeightOffset = 3f;

    private float currentDutch;
    private Transform currentTarget;
    private float targetLockTimer;
    private float targetLockDuration = 0.5f;

    private bool isAutoAiming = false;
    private bool cacheIsAutoAiming = true;
    
    private List<Transform> enemyCache = new List<Transform>();
    private float cacheTimer;
    private float cacheRefreshRate = 0.25f;

    void Awake(){
        if(Instance != null){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start(){
        if(cameraTransform == null){
            Camera mainCam = Camera.main;
            if(mainCam != null) cameraTransform = mainCam.transform;
        }
    }

    public bool aiming => input.AimHeld;
    void Update(){
        if(!normalCamera || !aimCamera || !input) return;

        if(isAutoAiming != cacheIsAutoAiming){
            CrosshairUI.Instance.ChangeColor(!isAutoAiming);
            cacheIsAutoAiming = isAutoAiming;
        }

        UpdateCameraPriority(aiming);
        UpdateDutch(aiming);
        
        if(enableAutoAim && aiming) UpdateAutoAim();
        else ResetAutoAim();
    }

    #region POLISH
    void UpdateCameraPriority(bool aiming){
        if(aiming){
            aimCamera.Priority = activePriority;
            normalCamera.Priority = inactivePriority;
        }
        else{
            normalCamera.Priority = activePriority;
            aimCamera.Priority = inactivePriority;
        }
    }

    void UpdateDutch(bool aiming){
        float targetDutch = 0f;

        if(!aiming && playerRotation){
            float normalizedYawRate = Mathf.Clamp(
                playerRotation.CurrentYawRate / yawRateForMaxDutch,
                -1f,
                1f);
            targetDutch = -normalizedYawRate * maxDutch;
        }

        currentDutch = Mathf.Lerp(
            currentDutch,
            targetDutch,
            Time.deltaTime * dutchSmoothSpeed);

        normalCamera.Lens.Dutch = currentDutch;
        aimCamera.Lens.Dutch = 0f;
    }

    public void ShakeCamera(){
        
    }

    public void SpeedZoom(){
        
    }
    #endregion

    #region AUTO AIM LOGIC
    void UpdateAutoAim(){
        cacheTimer += Time.deltaTime;
        if(cacheTimer >= cacheRefreshRate){
            RefreshEnemyCache();
            cacheTimer = 0f;
        }

        Transform closestEnemy = FindClosestEnemy();
        
        if(closestEnemy != null && IsTargetVisible(closestEnemy)){
            currentTarget = closestEnemy;
            targetLockTimer = targetLockDuration;
            isAutoAiming = true;

            if(playerRotation != null){
                Vector3 enemyCenter = GetEnemyCenter(closestEnemy);
                Vector3 cameraPos = GetCameraPosition();
                
                Vector3 directionToEnemy = (enemyCenter - cameraPos).normalized;
                
                if(directionToEnemy.sqrMagnitude > 0.01f){
                    Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);
                    playerRotation.SetAutoAimTarget(targetRotation);
                }
            }
        }
        else{
            targetLockTimer -= Time.deltaTime;
            if(targetLockTimer <= 0f){
                currentTarget = null;
                isAutoAiming = false;
                
                if(playerRotation != null){
                    playerRotation.SetInputDisabled(false);
                    playerRotation.ClearAutoAim();
                }
            }
        }
    }

    bool IsTargetVisible(Transform target){
        Vector3 enemyCenter = GetEnemyCenter(target);
        Vector3 cameraPos = GetCameraPosition();
        
        Vector3 direction = (enemyCenter - cameraPos).normalized;
        float distance = Vector3.Distance(cameraPos, enemyCenter);
        
        RaycastHit hit;
        if(Physics.SphereCast(cameraPos, obstructionCheckRadius, direction, out hit, distance)){
            if(hit.collider != null){
                Transform hitTransform = hit.collider.transform;
                
                if(hitTransform == target || hitTransform.IsChildOf(target)) return true;
                if(hit.collider.gameObject.layer == enemyLayer) return true;
                if(hit.collider.GetComponent<IDamageable>() != null) return true;
                return false;
            }
        }
        
        return true;
    }

    void RefreshEnemyCache(){
        enemyCache.Clear();
        
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            autoAimRange,
            enemyLayer
        );
        
        foreach(Collider col in colliders){
            Transform t = col.transform;
            Vector3 directionToEnemy = (t.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToEnemy);
            
            if(angle <= maxAimAngle){
                enemyCache.Add(t);
            }
        }
        
        enemyCache.Sort((a, b) => 
            Vector3.Distance(transform.position, a.position)
            .CompareTo(Vector3.Distance(transform.position, b.position))
        );
    }

    Transform FindClosestEnemy(){
        if(enemyCache.Count == 0) return null;
        
        foreach(Transform enemy in enemyCache){
            if(enemy != null && enemy.gameObject.activeInHierarchy){
                float distance = Vector3.Distance(transform.position, enemy.position);
                if(distance <= autoAimRange && distance >= minAimDistance)
                    return enemy;
            }
        }
        return null;
    }

    Vector3 GetEnemyCenter(Transform enemy){
        Renderer renderer = enemy.GetComponentInChildren<Renderer>();
        if(renderer != null) return renderer.bounds.center;
        
        Collider collider = enemy.GetComponentInChildren<Collider>();
        if(collider != null) return collider.bounds.center;
        
        return enemy.position + Vector3.up * 1f;
    }

    Vector3 GetCameraPosition(){
        if(cameraTransform != null) return cameraTransform.position;
        if(playerRotation != null) return playerRotation.transform.position + Vector3.up * cameraHeightOffset;
        return transform.position + Vector3.up * cameraHeightOffset;
    }

    void ResetAutoAim(){
        if(isAutoAiming){
            isAutoAiming = false;
            
            if(playerRotation != null){
                playerRotation.SetInputDisabled(false);
                playerRotation.ClearAutoAim();
            }
        }
        currentTarget = null;
    }

    public void SetAutoAimEnabled(bool enabled){
        enableAutoAim = enabled;
        if(!enabled){
            ResetAutoAim();
        }
    }
    #endregion

    public Transform GetCurrentTarget() => currentTarget;
    public bool HasTarget() => currentTarget != null && targetLockTimer > 0f;
    public bool IsAutoAiming() => isAutoAiming;

    #region DEBUG LOGIC
    void OnDrawGizmosSelected(){
        if(!enableAutoAim) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, autoAimRange);
        
        if(currentTarget != null){
            Vector3 enemyCenter = GetEnemyCenter(currentTarget);
            Vector3 cameraPos = GetCameraPosition();
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(cameraPos, enemyCenter);
            Gizmos.DrawWireSphere(enemyCenter, 0.5f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(cameraPos, 0.3f);
            
            Gizmos.color = Color.cyan;
            Vector3 direction = (enemyCenter - cameraPos).normalized;
            Gizmos.DrawWireSphere(cameraPos + direction * Vector3.Distance(cameraPos, enemyCenter) * 0.5f, obstructionCheckRadius);
        }
        
        Vector3 forward = transform.forward;
        Quaternion leftRotation = Quaternion.AngleAxis(-maxAimAngle, Vector3.up);
        Quaternion rightRotation = Quaternion.AngleAxis(maxAimAngle, Vector3.up);
        
        Vector3 leftDir = leftRotation * forward;
        Vector3 rightDir = rightRotation * forward;
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, leftDir * autoAimRange);
        Gizmos.DrawRay(transform.position, rightDir * autoAimRange);
    }
    #endregion
}