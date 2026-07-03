using UnityEngine;
using System.Collections.Generic;

public enum DroneState
{
    Observing,
    Repositioning,
    ResupplyPreparing,
    ResupplyTransferring,
    ResupplyRetracting,
    RegainingEnergy,
    Searching
}

public enum ObjectivePriority
{
    Emergency,
    RegainEnergy,
    Resupply,
    Reposition,
    Observe
}

public class DroneLogic : MonoBehaviour
{
    [Header("TARGET")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private EnergySystem playerEnergy;

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
    [SerializeField] private float minDroneEnergyToFunction = 20f;
    [SerializeField] private float maxDroneEnergyToRecharge = 80f;
    [SerializeField] private float playerEnergyCriticalThreshold = 20f;
    [SerializeField] private float playerEnergyLowThreshold = 40f;
    [SerializeField] private float playerEnergySafeThreshold = 70f;

    [Header("OBSERVATION")]
    [SerializeField] private float repositionCooldown = 5f;
    [SerializeField] private float repositionScoreGap = 0.5f;
    [SerializeField] private float lostContactTime = 4f;
    [SerializeField] private float maxSearchTime = 10f;
    [SerializeField] private float evaluationInterval = 0.25f;

    [Header("SCORING WEIGHTS")]
    [SerializeField] private float weightLineOfSight = 1.5f;
    [SerializeField] private float weightAltitude = 1.0f;
    [SerializeField] private float weightDistance = 0.8f;

    [Header("MOVEMENT")]
    [SerializeField] private float observationSpeed = 10f;
    [SerializeField] private float repositionSpeed = 12f;
    [SerializeField] private float resupplySpeed = 9f;
    [SerializeField] private float arrivalRadius = 5f;
    [SerializeField] private float stoppingRadius = 0.8f;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("RESUPPLY")]
    [SerializeField] private float resupplyDistance = 5f;
    [SerializeField] private float energyPerSecondToPlayer = 15f;
    [SerializeField] private float transferDuration = 3f;
    [SerializeField] private float autoCancelEnergyThreshold = 0.8f;
    [SerializeField] private bool debugRope;

    [Header("CABLE")]
    [SerializeField] private VerletRope verletRope;

    [Header("REFERENCES")]
    [SerializeField] private InputHandler input;

    private DroneState currentState = DroneState.Observing;
    private ObjectivePriority activePriority = ObjectivePriority.Observe;
    private DroneState previousState = DroneState.Observing;

    private Vector3 bestPosition;
    private float lastRepositionTime = -999f;
    private float currentScore;
    private float bestScore;

    private float stateTimer;
    private float lostContactTimer;
    private float searchTimer;
    private float rechargeTimer;
    private bool isRechargingAtAltitude;

    private bool manualResupplyRequested;
    private bool previousManualRequest;
    private bool isAutoResupply;
    private float resupplyStartTime;

    private Rigidbody rb;

    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownPosition;

    private float evaluationTimer;

    void Awake(){
        InitializeComponents();
        InitializeState();
    }

    void InitializeComponents(){
        rb = GetComponent<Rigidbody>();
        if(rb != null){
            rb.useGravity = false;
            rb.linearDamping = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if(verletRope != null) verletRope.SetVisible(false);
        if(playerRb == null && player != null) playerRb = player.GetComponent<Rigidbody>();
        if(playerEnergy == null && player != null) playerEnergy = player.GetComponent<EnergySystem>();
        if(input == null) input = FindFirstObjectByType<InputHandler>();
    }

    void InitializeState(){
        droneCurrentEnergy = droneMaxEnergy;
        bestPosition = transform.position;
        lastKnownPlayerPosition = player != null ? player.position : transform.position;
        hasLastKnownPosition = player != null;
        isRechargingAtAltitude = false;
        EvaluateBestPosition();
        UpdateCableVisibility();
    }

    void Update(){
        if(player == null) return;

        HandleManualResupplyInput();
        UpdatePriorities();
        UpdateStateMachine();
        UpdateCableVisibility();
    }

    void HandleManualResupplyInput(){
        bool currentRequest = input != null ? input.ResupplyPressed : Input.GetKeyDown(KeyCode.Tab);
        
        if(currentRequest && !previousManualRequest){
            if(droneCurrentEnergy > 0 && currentState != DroneState.RegainingEnergy){
                manualResupplyRequested = !manualResupplyRequested;
                isAutoResupply = false;
                
                if(!manualResupplyRequested && IsResupplyState()){
                    CancelResupply("Manual toggle off");
                }
            }
            else{
                Debug.Log($"Cannot resupply - Drone energy: {droneCurrentEnergy:F0}, State: {currentState}");
                manualResupplyRequested = false;
            }
        }
        previousManualRequest = currentRequest;
    }

    void UpdatePriorities(){
        if(player == null) return;

        bool hasVisual = HasLineOfSight();

        if(!hasVisual){
            lostContactTimer += Time.deltaTime;
            if(lostContactTimer > lostContactTime){
                activePriority = ObjectivePriority.Emergency;
                lastKnownPlayerPosition = player.position;
                hasLastKnownPosition = true;
                return;
            }
        }
        else{
            lostContactTimer = 0f;
            lastKnownPlayerPosition = player.position;
            hasLastKnownPosition = true;
        }

        float playerEnergyPercent = playerEnergy != null ? (playerEnergy.GetCurrentEnergy() / playerEnergy.MaxEnergy) * 100f : 100f;
        float droneEnergyPercent = (droneCurrentEnergy / droneMaxEnergy) * 100f;
        
        float targetDroneEnergy = CalculateTargetDroneEnergy(playerEnergyPercent);
        bool needsRecharge = droneCurrentEnergy < targetDroneEnergy;
        
        bool droneCriticallyLow = droneCurrentEnergy < minDroneEnergyToFunction;
        
        bool shouldSmartRecharge = needsRecharge && 
                                   playerEnergyPercent > playerEnergyLowThreshold &&
                                   !manualResupplyRequested &&
                                   currentState != DroneState.RegainingEnergy;

        if(droneCriticallyLow || shouldSmartRecharge){
            if(droneCriticallyLow || !IsResupplyState()){
                activePriority = ObjectivePriority.RegainEnergy;
                if(IsResupplyState())
                    CancelResupply("Drone needs recharge");
                return;
            }
        }

        bool playerNeedsEnergy = playerEnergy != null && playerEnergy.GetCurrentEnergy() <= playerEnergyLowThreshold;
        bool playerCriticallyNeedsEnergy = playerEnergy != null && playerEnergy.GetCurrentEnergy() <= playerEnergyCriticalThreshold;
        
        if(manualResupplyRequested || playerNeedsEnergy){
            if(droneCurrentEnergy > 0 || playerCriticallyNeedsEnergy){
                bool playerCanAcceptEnergy = playerEnergy != null && 
                                            playerEnergy.GetCurrentEnergy() < playerEnergy.MaxEnergy;
                
                if(playerCanAcceptEnergy || manualResupplyRequested){
                    activePriority = ObjectivePriority.Resupply;
                    if(playerNeedsEnergy && !manualResupplyRequested)
                        isAutoResupply = true;
                    return;
                }
                else if(playerEnergy != null && playerEnergy.GetCurrentEnergy() >= playerEnergy.MaxEnergy){
                    if(!manualResupplyRequested)
                        isAutoResupply = false;
                }
            }
            else{
                if(manualResupplyRequested){
                    manualResupplyRequested = false;
                    Debug.Log("Cannot resupply - drone energy depleted");
                }
            }
        }

        evaluationTimer += Time.deltaTime;
        if(evaluationTimer >= evaluationInterval){
            evaluationTimer = 0f;
            EvaluateBestPosition();
            
            if(Time.time - lastRepositionTime > repositionCooldown &&
                bestScore - currentScore > repositionScoreGap){
                activePriority = ObjectivePriority.Reposition;
                return;
            }
        }

        activePriority = ObjectivePriority.Observe;
    }

    float CalculateTargetDroneEnergy(float playerEnergyPercent){
        if(playerEnergyPercent >= playerEnergySafeThreshold)
            return maxDroneEnergyToRecharge;
        
        if(playerEnergyPercent <= playerEnergyLowThreshold)
            return minDroneEnergyToFunction;
        
        float t = Mathf.InverseLerp(playerEnergyLowThreshold, playerEnergySafeThreshold, playerEnergyPercent);
        return Mathf.Lerp(minDroneEnergyToFunction, maxDroneEnergyToRecharge, t);
    }

    void UpdateStateMachine(){
        stateTimer += Time.deltaTime;

        DroneState newState = DetermineNextState();
        
        if(newState != currentState){
            previousState = currentState;
            currentState = newState;
            stateTimer = 0f;
            OnStateEnter(currentState);
        }

        ExecuteStateLogic();
    }

    DroneState DetermineNextState(){
        switch (activePriority){
            case ObjectivePriority.Emergency:
                return DroneState.Searching;

            case ObjectivePriority.RegainEnergy:
                return DroneState.RegainingEnergy;

            case ObjectivePriority.Resupply:
                return DetermineResupplyState();

            case ObjectivePriority.Reposition:
                return DroneState.Repositioning;

            case ObjectivePriority.Observe:
            default:
                return DroneState.Observing;
        }
    }

    DroneState DetermineResupplyState(){
        switch (currentState){
            case DroneState.ResupplyPreparing:
                if(ArrivedAtResupplyPosition())
                    return DroneState.ResupplyTransferring;
                break;

            case DroneState.ResupplyTransferring:
                bool transferComplete = stateTimer >= transferDuration ||
                                       (playerEnergy != null && playerEnergy.GetCurrentEnergy() >= playerEnergy.MaxEnergy) ||
                                       droneCurrentEnergy <= 0f;
                
                if(transferComplete)
                    return DroneState.ResupplyRetracting;
                break;

            case DroneState.ResupplyRetracting:
                if(Vector3.Distance(transform.position, bestPosition) < stoppingRadius * 1.5f)
                    return DroneState.Observing;
                break;

            default:
                return DroneState.ResupplyPreparing;
        }

        return currentState;
    }

    void OnStateEnter(DroneState state){
        switch (state){
            case DroneState.ResupplyPreparing:
                resupplyStartTime = Time.time;
                break;

            case DroneState.ResupplyTransferring:
                break;

            case DroneState.ResupplyRetracting:
                break;

            case DroneState.Searching:
                searchTimer = 0f;
                break;

            case DroneState.RegainingEnergy:
                isRechargingAtAltitude = false;
                rechargeTimer = 0f;
                if(IsResupplyState())
                    CancelResupply("Entering recharge state");
                
                float targetEnergy = CalculateTargetDroneEnergy(
                    playerEnergy != null ? (playerEnergy.GetCurrentEnergy() / playerEnergy.MaxEnergy) * 100f : 100f
                );
                Debug.Log($"Starting recharge - Current: {droneCurrentEnergy:F0}, Target: {targetEnergy:F0}, Player Energy: {(playerEnergy != null ? playerEnergy.GetCurrentEnergy() : 0):F0}");
                break;
        }
        
        UpdateCableVisibility();
    }

    void ExecuteStateLogic(){
        switch (currentState){
            case DroneState.Observing:
                ExecuteObserving();
                break;

            case DroneState.Repositioning:
                ExecuteRepositioning();
                break;

            case DroneState.ResupplyPreparing:
                ExecuteResupplyPreparing();
                break;

            case DroneState.ResupplyTransferring:
                ExecuteResupplyTransferring();
                break;

            case DroneState.ResupplyRetracting:
                ExecuteResupplyRetracting();
                break;

            case DroneState.RegainingEnergy:
                ExecuteRegainingEnergy();
                break;

            case DroneState.Searching:
                ExecuteSearching();
                break;
        }
    }

    void ExecuteObserving(){
        if(Vector3.Distance(transform.position, bestPosition) > stoppingRadius * 1.5f){
            activePriority = ObjectivePriority.Reposition;
        }
    }

    void ExecuteRepositioning(){
        if(Vector3.Distance(transform.position, bestPosition) < stoppingRadius){
            lastRepositionTime = Time.time;
        }
    }

    void ExecuteResupplyPreparing(){
        if(ShouldCancelResupply()){
            CancelResupply("Cancellation condition met");
        }
    }

    void ExecuteResupplyTransferring(){
        if(ShouldCancelResupply()){
            CancelResupply("Cancellation condition met");
            return;
        }

        if(!debugRope) TransferEnergy();
    }

    void ExecuteResupplyRetracting(){
    }

    void ExecuteRegainingEnergy(){
        Vector3 rechargeTarget = transform.position;
        rechargeTarget.y = Mathf.Min(droneRechargeAltitude, maxHeightAboveTerrain);
        
        float distanceToTarget = Vector3.Distance(transform.position, rechargeTarget);
        float rechargeStartThreshold = stoppingRadius * 3f;
        
        if(distanceToTarget < rechargeStartThreshold){
            if(!isRechargingAtAltitude){
                isRechargingAtAltitude = true;
                rechargeTimer = 0f;
                Debug.Log($"Drone started recharging at altitude {transform.position.y:F1}");
            }
            
            float playerEnergyPercent = playerEnergy != null ? (playerEnergy.GetCurrentEnergy() / playerEnergy.MaxEnergy) * 100f : 100f;
            float targetEnergy = CalculateTargetDroneEnergy(playerEnergyPercent);
            
            float rechargeAmount = droneRechargeRate * Time.deltaTime;
            droneCurrentEnergy = Mathf.Min(droneCurrentEnergy + rechargeAmount, targetEnergy);
            rechargeTimer += Time.deltaTime;
            
            if(droneCurrentEnergy >= targetEnergy - 0.5f){
                Debug.Log($"Drone reached target energy: {droneCurrentEnergy:F0}/{targetEnergy:F0} (Player Energy: {playerEnergyPercent:F0}%)");
                currentState = DroneState.Observing;
                EvaluateBestPosition();
                UpdateCableVisibility();
            }
        }
        else isRechargingAtAltitude = false;
    }

    void ExecuteSearching(){
        searchTimer += Time.deltaTime;

        if(HasLineOfSight()){
            currentState = DroneState.Observing;
            lostContactTimer = 0f;
            EvaluateBestPosition();
            UpdateCableVisibility();
            return;
        }

        if(searchTimer > maxSearchTime){
            if(hasLastKnownPosition){
                bestPosition = lastKnownPlayerPosition + Vector3.up * followHeightAbovePlayer;
                currentState = DroneState.Repositioning;
                searchTimer = 0f;
                UpdateCableVisibility();
            }
        }
    }

    bool ShouldCancelResupply(){
        if(droneCurrentEnergy <= 0) return true;
        if(playerEnergy == null || player == null) return true;

        if(!manualResupplyRequested && isAutoResupply){
            if(playerEnergy != null && 
                playerEnergy.GetCurrentEnergy() >= playerEnergy.MaxEnergy * autoCancelEnergyThreshold){
                return true;
            }
        }

        return false;
    }

    void UpdateCableVisibility(){
        if(verletRope == null) return;
        
        bool shouldBeVisible = currentState == DroneState.ResupplyTransferring;
        verletRope.SetVisible(shouldBeVisible);
    }

    void FixedUpdate(){
        if(player == null) return;

        Vector3 targetPosition = CalculateTargetPosition();
        float speed = CalculateMovementSpeed();

        MoveDrone(targetPosition, speed);
        FacePlayer();
    }

    Vector3 CalculateTargetPosition(){
        switch (currentState){
            case DroneState.Observing:
            case DroneState.Repositioning:
            case DroneState.ResupplyRetracting:
                return bestPosition;

            case DroneState.ResupplyPreparing:
            case DroneState.ResupplyTransferring:
                return GetResupplyTarget();

            case DroneState.RegainingEnergy:
                Vector3 rechargeTarget = transform.position;
                rechargeTarget.y = Mathf.Min(droneRechargeAltitude, maxHeightAboveTerrain);
                return rechargeTarget;

            case DroneState.Searching:
                return GetSearchTarget();

            default:
                return transform.position;
        }
    }

    float CalculateMovementSpeed(){
        switch (currentState){
            case DroneState.Observing:
                return observationSpeed;
            case DroneState.Repositioning:
                return repositionSpeed;
            case DroneState.ResupplyPreparing:
            case DroneState.ResupplyTransferring:
            case DroneState.ResupplyRetracting:
                return resupplySpeed;
            case DroneState.RegainingEnergy:
            case DroneState.Searching:
                return repositionSpeed;
            default:
                return observationSpeed;
        }
    }

    void MoveDrone(Vector3 targetPosition, float speed){
        Vector3 toTarget = targetPosition - transform.position;
        float dist = toTarget.magnitude;

        if(dist > stoppingRadius){
            float arrivalFactor = Mathf.Clamp01((dist - stoppingRadius) / (arrivalRadius - stoppingRadius));
            float desiredSpeed = Mathf.Lerp(0, speed, arrivalFactor);
            
            Vector3 desiredVelocity = toTarget.normalized * desiredSpeed;
            rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, desiredVelocity, speed * Time.fixedDeltaTime);
        }
        else rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, speed * Time.fixedDeltaTime);
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

    bool HasLineOfSight() => EvaluateLineOfSight(transform.position) > 0.5f;
    bool ArrivedAtResupplyPosition() => Vector3.Distance(transform.position, GetResupplyTarget()) <= stoppingRadius * 1.2f;

    Vector3 GetResupplyTarget(){
        if(player == null) return transform.position;
        
        Vector3 dirFromPlayerToDrone = (transform.position - player.position).normalized;
        return player.position + dirFromPlayerToDrone * resupplyDistance;
    }

    void TransferEnergy(){
        if(playerEnergy == null || droneCurrentEnergy <= 0f) return;
        
        float dist = Vector3.Distance(transform.position, player.position);
        float transferRange = resupplyDistance + 1f;
        
        if(dist <= transferRange){
            float amount = energyPerSecondToPlayer * Time.deltaTime;
            float actualAmount = Mathf.Min(amount, playerEnergy.MaxEnergy - playerEnergy.GetCurrentEnergy());
            actualAmount = Mathf.Min(actualAmount, droneCurrentEnergy);
            
            if(actualAmount > 0){
                playerEnergy.Restore(actualAmount);
                droneCurrentEnergy -= actualAmount * (droneEnergyDrainPerSecond / energyPerSecondToPlayer);
                droneCurrentEnergy = Mathf.Max(0, droneCurrentEnergy);
            }
        }
    }

    void CancelResupply(string reason = ""){
        currentState = DroneState.Observing;
        manualResupplyRequested = false;
        isAutoResupply = false;
        UpdateCableVisibility();
        
        if(!string.IsNullOrEmpty(reason))
            Debug.Log($"Resupply cancelled: {reason}");
    }

    bool IsResupplyState(){
        return currentState == DroneState.ResupplyPreparing ||
               currentState == DroneState.ResupplyTransferring ||
               currentState == DroneState.ResupplyRetracting;
    }

    Vector3 GetSearchTarget(){
        if(player == null) return transform.position;
        
        float orbitRadius = idealHorizontalDistance;
        float angle = Time.time * 30f * Mathf.Deg2Rad;
        
        Vector3 center = hasLastKnownPosition ? lastKnownPlayerPosition : player.position;
        Vector3 orbitPos = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * orbitRadius;
        orbitPos.y = center.y + followHeightAbovePlayer;
        
        return orbitPos;
    }

    public DroneState GetCurrentState() => currentState;
    public float GetDroneEnergy() => droneCurrentEnergy;
    public float GetDroneMaxEnergy() => droneMaxEnergy;
    public bool IsResupplying() => IsResupplyState();

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

        if(IsResupplyState()){
            Gizmos.color = Color.magenta;
            Vector3 resupplyPos = GetResupplyTarget();
            Gizmos.DrawWireSphere(resupplyPos, 1f);
            Gizmos.DrawLine(transform.position, resupplyPos);
        }

        float playerEnergyPercent = playerEnergy != null ? (playerEnergy.GetCurrentEnergy() / playerEnergy.MaxEnergy) * 100f : 100f;
        float targetDroneEnergy = CalculateTargetDroneEnergy(playerEnergyPercent);
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 5f,
            $"State: {currentState}\n" +
            $"Priority: {activePriority}\n" +
            $"Drone Energy: {droneCurrentEnergy:F0}/{droneMaxEnergy:F0}\n" +
            $"Target Energy: {targetDroneEnergy:F0}\n" +
            $"Player Energy: {playerEnergyPercent:F0}%\n" +
            $"Recharging: {isRechargingAtAltitude}\n" +
            $"Y: {transform.position.y:F1}");
    }
#endif
}