using UnityEngine;
using System.Collections.Generic;

public enum DroneState
{
    Observing,
    Charging,
    Resupplying,
    RechargingSelf
}

public class DroneLogic : MonoBehaviour
{
    [Header("TARGET")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private EnergySystem playerEnergy;
    [SerializeField] private Transform model;

    [Header("DRONE SETTINGS")]
    [SerializeField] private float followHeightAbovePlayer = 10f;
    [SerializeField] private float idealHorizontalDistance = 15f;
    [SerializeField] private float minSafeDistance = 8f;
    [SerializeField] private float maxHeightAboveTerrain = 100f;

    [Header("DRONE ENERGY")]
    [SerializeField] private float droneMaxEnergy = 100f;
    [SerializeField] private float droneCurrentEnergy;
    [SerializeField] private float droneEnergyDrainPerSecond = 15f;
    [SerializeField] private float droneRechargeRate = 20f;
    [SerializeField] private float droneRechargeAltitude = 80f;
    
    [Header("RECHARGE THRESHOLDS")]
    [SerializeField] private float minDroneEnergyToFunction = 30f;
    [SerializeField] private float playerEnergyCriticalThreshold = 10f;
    [SerializeField] private float autoResupplyThreshold = 30f;
    [SerializeField] private float autoResupplyCooldown = 5f;

    [Header("OBSERVATION")]
    [SerializeField] private float repositionCooldown = 5f;
    [SerializeField] private float repositionScoreGap = 0.5f;
    [SerializeField] private float evaluationInterval = 0.25f;

    [Header("SCORING WEIGHTS")]
    [SerializeField] private float weightLineOfSight = 1.5f;
    [SerializeField] private float weightAltitude = 1.0f;
    [SerializeField] private float weightDistance = 0.8f;

    [Header("MOVEMENT")]
    [SerializeField] private float observationSpeed = 10f;
    [SerializeField] private float resupplySpeed = 9f;
    [SerializeField] private float arrivalRadius = 5f;
    [SerializeField] private float stoppingRadius = 0.8f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("RESUPPLY")]
    [SerializeField] private float resupplyDistance = 5f;
    [SerializeField] private float energyPerSecondToPlayer = 15f;

    [Header("CABLE")]
    [SerializeField] private SimpleCable cable;

    [Header("REFERENCES")]
    [SerializeField] private InputHandler input;

    private DroneState currentState = DroneState.Observing;

    private Vector3 bestPosition;
    private float lastRepositionTime = -999f;
    private float currentScore;
    private float bestScore;

    private float stateTimer;
    private float rechargeTimer;
    private bool isRechargingAtAltitude;
    private bool isManualCharge;
    private bool isManualResupply;
    private bool isAutoResupply;
    private float lastAutoResupplyTime = -999f;
    private bool wasManualChargeInterrupted;

    private Rigidbody rb;
    private float evaluationTimer;

    void Awake(){
        rb = GetComponent<Rigidbody>();
        if(rb != null){
            rb.useGravity = false;
            rb.linearDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if(cable != null) cable.SetVisible(false);
        if(playerRb == null && player != null) playerRb = player.GetComponent<Rigidbody>();
        if(playerEnergy == null && player != null) playerEnergy = player.GetComponent<EnergySystem>();
        if(input == null) input = FindFirstObjectByType<InputHandler>();

        droneCurrentEnergy = droneMaxEnergy;
        bestPosition = transform.position;
        isRechargingAtAltitude = false;
        wasManualChargeInterrupted = false;
        EvaluateBestPosition();
        UpdateCableVisibility();
    }

    void Update(){
        if(player == null) return;

        HandleInput();
        UpdateState();
        UpdateCable();
    }

    void DrainEnergy(){
        if(currentState == DroneState.Charging || 
           currentState == DroneState.Resupplying || 
           currentState == DroneState.RechargingSelf){
            return;
        }

        droneCurrentEnergy -= droneEnergyDrainPerSecond * Time.deltaTime;
        droneCurrentEnergy = Mathf.Max(0, droneCurrentEnergy);
    }

    void HandleInput(){
        bool chargePressed = input != null ? input.ChargePressed : Input.GetKeyDown(KeyCode.C);
        bool resupplyPressed = input != null ? input.ResupplyPressed : Input.GetKeyDown(KeyCode.F);

        if(chargePressed){
            if(currentState == DroneState.Charging && isManualCharge){
                isManualCharge = false;
                wasManualChargeInterrupted = false;
                currentState = DroneState.Observing;
                EvaluateBestPosition();
                UpdateCableVisibility();
                return;
            }

            if(currentState != DroneState.Resupplying && currentState != DroneState.RechargingSelf){
                isManualCharge = true;
                isManualResupply = false;
                wasManualChargeInterrupted = false;
                currentState = DroneState.Charging;
                rechargeTimer = 0f;
                isRechargingAtAltitude = false;
                UpdateCableVisibility();
            }
        }

        if(resupplyPressed){
            if(currentState == DroneState.Resupplying && isManualResupply){
                isManualResupply = false;
                currentState = DroneState.Observing;
                EvaluateBestPosition();
                UpdateCableVisibility();
                return;
            }

            if(currentState != DroneState.Resupplying && currentState != DroneState.RechargingSelf){
                if(droneCurrentEnergy <= 0){
                    return;
                }

                isManualResupply = true;
                isManualCharge = false;
                currentState = DroneState.Resupplying;
                isAutoResupply = false;
                UpdateCableVisibility();
            }
        }
    }

    void UpdateState(){
        stateTimer += Time.deltaTime;

        if(playerEnergy == null) return;

        float playerPercent = (playerEnergy.GetCurrentEnergy() / playerEnergy.MaxEnergy) * 100f;
        float dronePercent = (droneCurrentEnergy / droneMaxEnergy) * 100f;

        bool playerCriticallyLow = playerPercent <= playerEnergyCriticalThreshold;
        bool shouldAutoResupply = playerPercent > playerEnergyCriticalThreshold && 
                                  playerPercent - dronePercent >= autoResupplyThreshold &&
                                  Time.time - lastAutoResupplyTime > autoResupplyCooldown;

        if(currentState == DroneState.Resupplying && isManualResupply){
            ExecuteResupplying();
            return;
        }

        if(currentState == DroneState.Charging && isManualCharge){
            if(playerCriticallyLow){
                wasManualChargeInterrupted = true;
                isManualCharge = false;
                currentState = DroneState.Resupplying;
                isAutoResupply = true;
                UpdateCableVisibility();
                return;
            }

            if(droneCurrentEnergy >= droneMaxEnergy){
                if(wasManualChargeInterrupted){
                    wasManualChargeInterrupted = false;
                    isManualCharge = false;
                    currentState = DroneState.Resupplying;
                    isAutoResupply = true;
                    UpdateCableVisibility();
                    return;
                }

                isManualCharge = false;
                currentState = DroneState.Observing;
                EvaluateBestPosition();
                UpdateCableVisibility();
                return;
            }

            if(stateTimer > 5f && !isRechargingAtAltitude){
                isRechargingAtAltitude = true;
                rechargeTimer = 0f;
            }

            ExecuteCharging();
            return;
        }

        if(playerCriticallyLow){
            if(currentState != DroneState.Resupplying){
                if(droneCurrentEnergy <= 0){
                    currentState = DroneState.RechargingSelf;
                    isRechargingAtAltitude = false;
                    rechargeTimer = 0f;
                    UpdateCableVisibility();
                    return;
                }

                currentState = DroneState.Resupplying;
                isAutoResupply = true;
                UpdateCableVisibility();
            }
            
            ExecuteResupplying();
            return;
        }

        if(shouldAutoResupply){
            if(currentState != DroneState.Resupplying && currentState != DroneState.RechargingSelf){
                currentState = DroneState.Resupplying;
                isAutoResupply = true;
                UpdateCableVisibility();
            }
            
            ExecuteResupplying();
            return;
        }

        if(currentState == DroneState.RechargingSelf){
            ExecuteRechargingSelf();
            return;
        }

        if(wasManualChargeInterrupted){
            if(droneCurrentEnergy >= droneMaxEnergy){
                wasManualChargeInterrupted = false;
                currentState = DroneState.Resupplying;
                isAutoResupply = true;
                UpdateCableVisibility();
                return;
            }

            ExecuteCharging();
            return;
        }

        evaluationTimer += Time.deltaTime;
        if(evaluationTimer >= evaluationInterval){
            evaluationTimer = 0f;
            EvaluateBestPosition();
            
            if(Time.time - lastRepositionTime > repositionCooldown &&
                bestScore - currentScore > repositionScoreGap){
                currentState = DroneState.Observing;
                return;
            }
        }

        if(currentState == DroneState.Observing){
            ExecuteObserving();
        }
    }

    void ExecuteCharging(){
        Vector3 rechargeTarget = transform.position;
        rechargeTarget.y = Mathf.Min(droneRechargeAltitude, maxHeightAboveTerrain);
        
        float distanceToTarget = Vector3.Distance(transform.position, rechargeTarget);
        float rechargeStartThreshold = stoppingRadius * 3f;
        
        if(distanceToTarget < rechargeStartThreshold || isRechargingAtAltitude){
            if(!isRechargingAtAltitude){
                isRechargingAtAltitude = true;
                rechargeTimer = 0f;
            }
            
            float rechargeAmount = droneRechargeRate * Time.deltaTime;
            droneCurrentEnergy = Mathf.Min(droneCurrentEnergy + rechargeAmount, droneMaxEnergy);
            rechargeTimer += Time.deltaTime;
            
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, observationSpeed * Time.fixedDeltaTime);
        }
        else{
            isRechargingAtAltitude = false;
            Vector3 toTarget = rechargeTarget - transform.position;
            float dist = toTarget.magnitude;
            
            if(dist > stoppingRadius){
                float arrivalFactor = Mathf.Clamp01((dist - stoppingRadius) / (arrivalRadius - stoppingRadius));
                float desiredSpeed = Mathf.Lerp(0, observationSpeed, arrivalFactor);
                Vector3 desiredVelocity = toTarget.normalized * desiredSpeed;
                rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, desiredVelocity, observationSpeed * Time.fixedDeltaTime);
            }
        }

        FacePlayer();
    }

    void ExecuteResupplying(){
        float playerPercent = (playerEnergy.GetCurrentEnergy() / playerEnergy.MaxEnergy) * 100f;

        if(droneCurrentEnergy <= 0){
            currentState = DroneState.RechargingSelf;
            isRechargingAtAltitude = false;
            rechargeTimer = 0f;
            UpdateCableVisibility();
            return;
        }

        if(playerPercent >= 100f){
            currentState = DroneState.Observing;
            isAutoResupply = false;
            isManualResupply = false;
            lastAutoResupplyTime = Time.time;
            UpdateCableVisibility();
            EvaluateBestPosition();
            return;
        }

        Vector3 toPlayer = player.position - transform.position;
        float distToPlayer = toPlayer.magnitude;
        
        if(distToPlayer > 2f){
            Vector3 desiredVelocity = toPlayer.normalized * resupplySpeed;
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, desiredVelocity, resupplySpeed * Time.fixedDeltaTime);
        }
        else{
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, resupplySpeed * Time.fixedDeltaTime);
            TransferEnergy();
        }

        FacePlayer();
    }

    void ExecuteRechargingSelf(){
        Vector3 rechargeTarget = transform.position;
        rechargeTarget.y = Mathf.Min(droneRechargeAltitude, maxHeightAboveTerrain);
        
        float distanceToTarget = Vector3.Distance(transform.position, rechargeTarget);
        float rechargeStartThreshold = stoppingRadius * 3f;
        
        if(distanceToTarget < rechargeStartThreshold || isRechargingAtAltitude){
            if(!isRechargingAtAltitude){
                isRechargingAtAltitude = true;
                rechargeTimer = 0f;
            }
            
            float rechargeAmount = droneRechargeRate * Time.deltaTime;
            droneCurrentEnergy = Mathf.Min(droneCurrentEnergy + rechargeAmount, droneMaxEnergy);
            rechargeTimer += Time.deltaTime;
            
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, observationSpeed * Time.fixedDeltaTime);
            
            if(droneCurrentEnergy >= minDroneEnergyToFunction){
                float playerPercent = (playerEnergy.GetCurrentEnergy() / playerEnergy.MaxEnergy) * 100f;
                isRechargingAtAltitude = false;
                
                if(playerPercent <= playerEnergyCriticalThreshold){
                    currentState = DroneState.Resupplying;
                    isAutoResupply = true;
                    UpdateCableVisibility();
                }
                else{
                    currentState = DroneState.Observing;
                    EvaluateBestPosition();
                    UpdateCableVisibility();
                }
            }
        }
        else{
            isRechargingAtAltitude = false;
            Vector3 toTarget = rechargeTarget - transform.position;
            float dist = toTarget.magnitude;
            
            if(dist > stoppingRadius){
                float arrivalFactor = Mathf.Clamp01((dist - stoppingRadius) / (arrivalRadius - stoppingRadius));
                float desiredSpeed = Mathf.Lerp(0, observationSpeed, arrivalFactor);
                Vector3 desiredVelocity = toTarget.normalized * desiredSpeed;
                rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, desiredVelocity, observationSpeed * Time.fixedDeltaTime);
            }
        }

        FacePlayer();
    }

    void ExecuteObserving(){
        if(Vector3.Distance(transform.position, bestPosition) > stoppingRadius * 1.5f){
            Vector3 toTarget = bestPosition - transform.position;
            float dist = toTarget.magnitude;
            
            if(dist > stoppingRadius){
                float arrivalFactor = Mathf.Clamp01((dist - stoppingRadius) / (arrivalRadius - stoppingRadius));
                float desiredSpeed = Mathf.Lerp(0, observationSpeed, arrivalFactor);
                Vector3 desiredVelocity = toTarget.normalized * desiredSpeed;
                rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, desiredVelocity, observationSpeed * Time.fixedDeltaTime);
            }
            else{
                rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, observationSpeed * Time.fixedDeltaTime);
                lastRepositionTime = Time.time;
            }
        }
        else{
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, observationSpeed * Time.fixedDeltaTime);
        }

        FacePlayer();
    }

    void FacePlayer(){
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;
        
        if(lookDir.sqrMagnitude > 0.01f){
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    Vector3 GetPredictedPlayerPosition(){
        if(playerRb == null || player == null) 
            return player != null ? player.position : transform.position;
        
        return player.position + playerRb.linearVelocity * 1.5f;
    }

    List<Vector3> GenerateCandidatePositions(Vector3 around){
        List<Vector3> candidates = new List<Vector3>();
        float angleStep = 360f / 8;
        
        for (int i = 0; i < 8; i++){
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            
            Vector3 pos = around + dir * idealHorizontalDistance;
            pos.y = player.position.y + followHeightAbovePlayer;
            candidates.Add(pos);
            
            candidates.Add(pos + Vector3.up * 3f);
            candidates.Add(pos - Vector3.up * 3f);
            
            Vector3 closer = around + dir * (idealHorizontalDistance * 0.8f);
            closer.y = player.position.y + followHeightAbovePlayer;
            candidates.Add(closer);
            
            Vector3 farther = around + dir * (idealHorizontalDistance * 1.2f);
            farther.y = player.position.y + followHeightAbovePlayer;
            candidates.Add(farther);
        }
        
        return candidates;
    }

    void EvaluateBestPosition(){
        if(player == null) return;
        
        Vector3 predicted = GetPredictedPlayerPosition();
        List<Vector3> candidates = GenerateCandidatePositions(predicted);
        
        float best = float.MinValue;
        Vector3 bestPos = bestPosition;

        foreach (Vector3 pos in candidates){
            float s = ScorePosition(pos);
            if(s > best){
                best = s;
                bestPos = pos;
            }
        }
        
        bestScore = best;
        bestPosition = bestPos;
        currentScore = ScorePosition(transform.position);
    }

    float ScorePosition(Vector3 pos){
        if(player == null) return 0f;
        
        float score = 0f;

        float losScore = EvaluateLineOfSight(pos);
        score += weightLineOfSight * losScore;

        float targetY = player.position.y + followHeightAbovePlayer;
        float yDiff = Mathf.Abs(pos.y - targetY);
        score -= weightAltitude * yDiff * 0.1f;

        Vector3 flatPos = pos;
        flatPos.y = player.position.y;
        Vector3 flatPlayer = player.position;
        flatPlayer.y = player.position.y;
        float horizontalDist = Vector3.Distance(flatPos, flatPlayer);
        
        if(horizontalDist < minSafeDistance) score -= weightDistance * 3f;
        else score -= weightDistance * Mathf.Abs(horizontalDist - idealHorizontalDistance) * 0.1f;

        return score;
    }

    float EvaluateLineOfSight(Vector3 from){
        if(player == null) return 0f;
        
        Vector3 playerCenter = player.position + Vector3.up * 1f;
        Vector3 dir = (playerCenter - from).normalized;
        float dist = Vector3.Distance(from, playerCenter);
        
        if(!Physics.Raycast(from, dir, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return 1f;
        
        return 0f;
    }

    Vector3 GetResupplyTarget(){
        if(player == null) return transform.position;
        
        Vector3 dirFromPlayerToDrone = (transform.position - player.position).normalized;
        return player.position + dirFromPlayerToDrone * resupplyDistance;
    }

    void TransferEnergy(){
        if(playerEnergy == null || droneCurrentEnergy <= 0f) return;
        
        float dist = Vector3.Distance(transform.position, player.position);
        
        if(dist <= 5f){
            float amount = energyPerSecondToPlayer * Time.deltaTime;
            float spaceInPlayer = playerEnergy.MaxEnergy - playerEnergy.GetCurrentEnergy();
            float actualAmount = Mathf.Min(amount, spaceInPlayer, droneCurrentEnergy);
            
            if(actualAmount > 0){
                playerEnergy.Restore(actualAmount);
                droneCurrentEnergy -= actualAmount;
                droneCurrentEnergy = Mathf.Max(0, droneCurrentEnergy);
            }
        }
    }

    public float GetCurrentEnergy() => droneCurrentEnergy;

    void UpdateCable(){
        if(cable == null) return;
        
        bool shouldBeVisible = currentState == DroneState.Resupplying;
        
        if(shouldBeVisible){
            float dist = Vector3.Distance(transform.position, player.position);
            if(dist > 10f){
                currentState = DroneState.Observing;
                isManualResupply = false;
                isAutoResupply = false;
                shouldBeVisible = false;
                UpdateCableVisibility();
            }
        }
        
        cable.SetVisible(shouldBeVisible);
        if(shouldBeVisible) cable.SetEndpoints(transform, player);
    }

    void UpdateCableVisibility(){
        if(cable == null) return;
        
        bool shouldBeVisible = currentState == DroneState.Resupplying;
        cable.SetVisible(shouldBeVisible);
    }

    public DroneState GetCurrentState() => currentState;
    public float GetDroneEnergy() => droneCurrentEnergy;
    public float GetDroneMaxEnergy() => droneMaxEnergy;
    public bool IsResupplying() => currentState == DroneState.Resupplying;

#if UNITY_EDITOR
    void OnDrawGizmosSelected(){
        if(player == null) return;

        Vector3 targetPos = player.position + Vector3.up * followHeightAbovePlayer;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPos, 1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(bestPosition, 1.5f);

        Gizmos.color = Color.cyan;
        Vector3 rechargePos = new Vector3(player.position.x, Mathf.Min(droneRechargeAltitude, maxHeightAboveTerrain), player.position.z);
        Gizmos.DrawWireSphere(rechargePos, 2f);

        if(currentState == DroneState.Resupplying){
            Gizmos.color = Color.magenta;
            Vector3 resupplyPos = GetResupplyTarget();
            Gizmos.DrawWireSphere(resupplyPos, 1f);
            Gizmos.DrawLine(model.transform.position, resupplyPos);
        }

        float playerPercent = playerEnergy != null ? (playerEnergy.GetCurrentEnergy() / playerEnergy.MaxEnergy) * 100f : 100f;
        float dronePercent = (droneCurrentEnergy / droneMaxEnergy) * 100f;
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 5f,
            $"State: {currentState}\n" +
            $"Drone Energy: {dronePercent:F0}%\n" +
            $"Player Energy: {playerPercent:F0}%\n" +
            $"Manual Charge: {isManualCharge}\n" +
            $"Manual Resupply: {isManualResupply}\n" +
            $"Y: {transform.position.y:F1}");
    }
#endif
}