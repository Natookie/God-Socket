using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraAutoAim : MonoBehaviour
{
    [Header("AUTO AIM")]
    public bool enableAutoAim = true;
    public float autoAimRange = 50f;
    public LayerMask enemyLayer = 1 << 6;
    public float maxAimAngle = 60f;
    public float minAimDistance = 2f;
    public float obstructionCheckRadius = 0.5f;

    [Header("TARGET SELECTION")]
    public float angleWeight = 0.7f;
    public float distanceWeight = 0.3f;
    public float targetSwitchHysteresis = 0.5f;

    [Header("REFERENCES")]
    public Transform cameraTransform;
    public PlayerRotation playerRotation;
    public InputHandler input;
    public float cameraHeightOffset = 3f;

    private Transform currentTarget;
    private float targetLockTimer;
    private float targetLockDuration = 0.5f;

    private bool isAutoAiming = false;
    private bool cacheIsAutoAiming = false;
    
    private List<EnemyCandidate> enemyCache = new List<EnemyCandidate>();
    private float cacheTimer;
    private float cacheRefreshRate = 0.25f;

    private class EnemyCandidate
    {
        public Transform transform;
        public float distance;
        public float angle;
        public float score;
        
        public EnemyCandidate(Transform t, float dist, float ang){
            transform = t;
            distance = dist;
            angle = ang;
            score = 0f;
        }
    }

    public event System.Action<bool> OnAutoAimStateChanged;

    void Start(){
        if(cameraTransform == null){
            Camera mainCam = Camera.main;
            if(mainCam != null) cameraTransform = mainCam.transform;
        }

        if(input == null) input = FindFirstObjectByType<InputHandler>();
    }

    public bool aiming => input != null && input.AimHeld;

    void Update(){
        if(!enableAutoAim) return;

        if(isAutoAiming != cacheIsAutoAiming){
            OnAutoAimStateChanged?.Invoke(isAutoAiming);
            cacheIsAutoAiming = isAutoAiming;
        }

        if(enableAutoAim && aiming) UpdateAutoAim();
        else ResetAutoAim();
    }

    void UpdateAutoAim(){
        cacheTimer += Time.deltaTime;
        if(cacheTimer >= cacheRefreshRate){
            RefreshEnemyCache();
            cacheTimer = 0f;
        }

        EnemyCandidate bestCandidate = GetBestCandidate();
        
        if(bestCandidate != null && IsTargetVisible(bestCandidate.transform)){
            if(currentTarget != null && currentTarget != bestCandidate.transform){
                float currentScore = GetCandidateScore(currentTarget);
                float newScore = bestCandidate.score;

                if(newScore > currentScore + targetSwitchHysteresis) SwitchTarget(bestCandidate.transform);
                else targetLockTimer = targetLockDuration;
            }
            else if(currentTarget == null){
                SwitchTarget(bestCandidate.transform);
            }
            else{
                targetLockTimer = targetLockDuration;
            }

            if(isAutoAiming && playerRotation != null){
                Vector3 enemyCenter = GetEnemyCenter(currentTarget);
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
            if(targetLockTimer <= 0f) ClearTarget();
        }
    }

    void SwitchTarget(Transform newTarget){
        currentTarget = newTarget;
        targetLockTimer = targetLockDuration;
        isAutoAiming = true;
    }

    void ClearTarget(){
        currentTarget = null;
        targetLockTimer = 0f;
        isAutoAiming = false;
        enemyCache.Clear();
        
        if(playerRotation != null){
            playerRotation.SetInputDisabled(false);
            playerRotation.ClearAutoAim();
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
                if((enemyLayer.value & (1 << hit.collider.gameObject.layer)) != 0) return true;
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
        
        Vector3 cameraPos = GetCameraPosition();
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        
        foreach(Collider col in colliders){
            Transform t = col.transform;
            Vector3 directionToEnemy = (t.position - cameraPos).normalized;
            float angle = Vector3.Angle(forward, directionToEnemy);
            
            if(angle <= maxAimAngle){
                float distance = Vector3.Distance(cameraPos, t.position);
                if(distance >= minAimDistance && distance <= autoAimRange){
                    EnemyCandidate candidate = new EnemyCandidate(t, distance, angle);
                    float normalizedAngle = angle / maxAimAngle;
                    float normalizedDistance = distance / autoAimRange;
                    candidate.score = (1f - normalizedAngle) * angleWeight + (1f - normalizedDistance) * distanceWeight;
                    enemyCache.Add(candidate);
                }
            }
        }
        
        enemyCache.Sort((a, b) => b.score.CompareTo(a.score));
    }

    EnemyCandidate GetBestCandidate(){
        if(enemyCache.Count == 0) return null;
        
        foreach(EnemyCandidate candidate in enemyCache){
            if(candidate.transform != null && candidate.transform.gameObject.activeInHierarchy){
                float distance = Vector3.Distance(GetCameraPosition(), candidate.transform.position);
                if(distance <= autoAimRange && distance >= minAimDistance) return candidate;
            }
        }
        return null;
    }

    float GetCandidateScore(Transform target){
        if(target == null) return 0f;
        
        Vector3 cameraPos = GetCameraPosition();
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 directionToEnemy = (target.position - cameraPos).normalized;
        float angle = Vector3.Angle(forward, directionToEnemy);
        float distance = Vector3.Distance(cameraPos, target.position);
        
        float normalizedAngle = Mathf.Clamp01(angle / maxAimAngle);
        float normalizedDistance = Mathf.Clamp01(distance / autoAimRange);
        
        return (1f - normalizedAngle) * angleWeight + (1f - normalizedDistance) * distanceWeight;
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

    public void ResetAutoAim(){
        if(isAutoAiming){
            isAutoAiming = false;
            
            if(playerRotation != null){
                playerRotation.SetInputDisabled(false);
                playerRotation.ClearAutoAim();
            }
        }
        
        ClearTarget();
    }

    public void SetAutoAimEnabled(bool enabled){
        enableAutoAim = enabled;
        if(!enabled){
            ResetAutoAim();
        }
    }

    public Transform GetCurrentTarget() => currentTarget;
    public bool HasTarget() => currentTarget != null && targetLockTimer > 0f;
    public bool IsAutoAiming() => isAutoAiming;

    #if UNITY_EDITOR
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
        
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Quaternion leftRotation = Quaternion.AngleAxis(-maxAimAngle, Vector3.up);
        Quaternion rightRotation = Quaternion.AngleAxis(maxAimAngle, Vector3.up);
        
        Vector3 leftDir = leftRotation * forward;
        Vector3 rightDir = rightRotation * forward;
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, leftDir * autoAimRange);
        Gizmos.DrawRay(transform.position, rightDir * autoAimRange);

        if(enemyCache.Count > 0){
            Gizmos.color = Color.cyan;
            foreach(EnemyCandidate candidate in enemyCache){
                if(candidate.transform != null){
                    Gizmos.DrawWireSphere(candidate.transform.position, 0.2f);
                    UnityEditor.Handles.Label(candidate.transform.position + Vector3.up * 0.5f, $"Score: {candidate.score:F2}");
                }
            }
        }
    }
    #endif
}