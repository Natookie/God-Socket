using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class BossController : MonoBehaviour
{
    [Header("STATE TIMING")]
    public float idleDuration = 2f;
    public float initialDelay = 5f;
    public float ufoDuration = 10f;
    public float missileDuration = 10f;
    public float gravityDuration = 15f;
    
    [Header("UFO")]
    public GameObject ufoPrefab;
    public Transform ufoParent;
    public int ufoPoolSize = 10;
    public int ufoSpawnCount = 3;
    public float ufoSpawnDelay = 0.5f;
    public Transform[] ufoSpawnPoints;
    
    [Header("MISSILE")]
    public GameObject missilePrefab;
    public Transform missileParent;
    public int missilePoolSize = 10;
    public int totalMissilesToSpawn = 7;
    public int minMissilesPerWave = 1;
    public int maxMissilesPerWave = 3;
    public float missileSpawnDelay = 1f;
    public float missileWaveDelay = 2f;
    public Transform missileSpawnPoint;
    
    [Header("GRAVITY FIELD")]
    public GravityFieldController gravityFieldController;
    public LaserFieldController laserFieldController;
    public WeakpointManager weakpointManager;
    public BossAimer bossAimer;
    public float chargeTime = 10f;
    public float laserDuration = 5f;
    
    [Header("REFERENCES")]
    public Transform player;
    public BossHealthSystem healthSystem;
    public BossShieldSystem shieldSystem;
    public ParticleSystem deathEffect;
    public AudioSource audioSource;
    public AudioClip deathSound;

    [Header("SHIELD")]
    public Collider shieldCollider;
    public float shieldDamageMultiplier = 0.5f;

    [Header("DEBUG")]
    [SerializeField] private bool disableLogic;
    [SerializeField] private bool enableDebugInput;
    public enum BossState { Idle, UFO, Missile, Gravity }
    [ReadOnly] public BossState currentState = BossState.Idle;
    
    private float stateTimer = 0f;
    private bool isDefeated = false;
    private int missilesSpawnedThisPhase = 0;
    private int stateCounter = 0;
    private bool hasStarted = false;
    
    private List<GameObject> ufoPool = new List<GameObject>();
    private List<GameObject> missilePool = new List<GameObject>();
    private List<GameObject> activeUFOs = new List<GameObject>();
    private List<MissileController> activeMissiles = new List<MissileController>();
    
    private Coroutine currentCoroutine = null;
    [ReadOnly] public bool isGravityActive = false;
    [ReadOnly] public bool isLaserActive = false;
    [ReadOnly] public bool isShieldActive = false;

    void Start(){
        CreatePools();
        
        if(player == null){
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if(p != null) player = p.transform;
        }

        if(healthSystem == null) healthSystem = GetComponent<BossHealthSystem>();
        if(healthSystem != null) healthSystem.OnDeath += Die;
        if(gravityFieldController == null) gravityFieldController = GetComponent<GravityFieldController>();
        if(laserFieldController == null) laserFieldController = GetComponent<LaserFieldController>();
        if(weakpointManager == null) weakpointManager = GetComponent<WeakpointManager>();
        if(bossAimer == null) bossAimer = GetComponent<BossAimer>();
        
        if(weakpointManager != null) weakpointManager.OnAllWeakPointsDestroyed += OnAllWeakPointsDestroyed;
        if(shieldCollider != null) shieldCollider.enabled = false;
        
        currentState = BossState.Idle;
        bossAimer.SetIdle(true);
        stateTimer = 0f;
        stateCounter = 0;
        hasStarted = false;
        
        StartCoroutine(InitialDelayCoroutine());
    }
    
    IEnumerator InitialDelayCoroutine(){
        yield return new WaitForSeconds(initialDelay);
        hasStarted = true;
        EnterState(GetNextState());
    }
    
    void OnDestroy(){
        if(healthSystem != null) healthSystem.OnDeath -= Die;
        if(weakpointManager != null) weakpointManager.OnAllWeakPointsDestroyed -= OnAllWeakPointsDestroyed;
    }
    
    void CreatePools(){
        if(ufoParent == null){
            ufoParent = new GameObject("UFOPool").transform;
            ufoParent.SetParent(transform);
        }
        for(int i = 0; i < ufoPoolSize; i++){
            GameObject obj = Instantiate(ufoPrefab);
            obj.SetActive(false);
            obj.transform.SetParent(ufoParent);
            ufoPool.Add(obj);
        }
        
        if(missileParent == null){
            missileParent = new GameObject("MissilePool").transform;
            missileParent.SetParent(transform);
        }
        for(int i = 0; i < missilePoolSize; i++){
            GameObject obj = Instantiate(missilePrefab);
            obj.SetActive(false);
            obj.transform.SetParent(missileParent);
            missilePool.Add(obj);
        }
    }
    
    void Update(){
        if(isDefeated || player == null) return;
        if(enableDebugInput) HandleDebugInput();
        if(disableLogic) return;

        if(!hasStarted) return;
        
        stateTimer += Time.deltaTime;
        CleanupDeadEntities();
        CheckStateExit();
        UpdateBuildingTargets();
    }

    void HandleDebugInput(){
        if(Input.GetKeyDown(KeyCode.Alpha1)){
            DebugUFO();
        }
        else if(Input.GetKeyDown(KeyCode.Alpha2)){
            DebugMissile();
        }
        else if(Input.GetKeyDown(KeyCode.Alpha3)){
            DebugGravity();
        }
    }

    void CheckStateExit(){
        switch(currentState){
            case BossState.Idle:
                if(stateTimer >= idleDuration){
                    ExitState();
                    EnterState(GetNextState());
                }
                break;
                
            case BossState.UFO:
                if(stateTimer >= ufoDuration && AllUFOsInactive()){
                    ExitState();
                    EnterState(GetNextState());
                }
                break;
                
            case BossState.Missile:
                if(missilesSpawnedThisPhase >= totalMissilesToSpawn && AllMissilesInactive()){
                    ExitState();
                    EnterState(GetNextState());
                }
                break;
                
            case BossState.Gravity:
                if(stateTimer >= gravityDuration){
                    ExitState();
                    EnterState(GetNextState());
                }
                break;
        }
    }

    bool AllUFOsInactive(){
        if(ufoParent == null) return true;
        
        for(int i = 0; i < ufoParent.childCount; i++){
            Transform child = ufoParent.GetChild(i);
            if(child.gameObject.activeSelf){
                return false;
            }
        }
        return true;
    }

    bool AllMissilesInactive(){
        if(missileParent == null) return true;
        
        for(int i = 0; i < missileParent.childCount; i++){
            Transform child = missileParent.GetChild(i);
            if(child.gameObject.activeSelf){
                return false;
            }
        }
        return true;
    }

    BossState GetNextState(){
        stateCounter++;
        
        if(stateCounter % 3 == 0){
            return BossState.Gravity;
        }
        
        int choice = Random.Range(0, 2);
        
        if(choice == 0){
            return BossState.UFO;
        }
        else{
            if(HasAvailableBuildings()){
                return BossState.Missile;
            }
            else{
                return BossState.UFO;
            }
        }
    }

    void EnterState(BossState newState){
        if(isDefeated) return;
        
        currentState = newState;
        stateTimer = 0f;
        
        switch(newState){
            case BossState.Idle:
                DeactivateShield();
                bossAimer.SetIdle(true);
                break;
                
            case BossState.UFO:
                ActivateShield();
                currentCoroutine = StartCoroutine(UFOCoroutine());
                bossAimer.SetIdle(false);
                break;
                
            case BossState.Missile:
                missilesSpawnedThisPhase = 0;
                ActivateShield();
                currentCoroutine = StartCoroutine(MissileCoroutine());
                bossAimer.SetIdle(false);
                break;
                
            case BossState.Gravity:
                DeactivateShield();
                currentCoroutine = StartCoroutine(GravityCoroutine());
                break;
        }
    }

    void ExitState(){
        if(currentCoroutine != null){
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
        
        switch(currentState){
            case BossState.UFO:
                for(int i = 0; i < ufoParent.childCount; i++){
                    Transform child = ufoParent.GetChild(i);
                    if(child.gameObject.activeSelf){
                        child.gameObject.SetActive(false);
                    }
                }
                activeUFOs.Clear();
                DeactivateShield();
                break;
                
            case BossState.Missile:
                for(int i = 0; i < missileParent.childCount; i++){
                    Transform child = missileParent.GetChild(i);
                    if(child.gameObject.activeSelf){
                        child.gameObject.SetActive(false);
                    }
                }
                activeMissiles.Clear();
                DeactivateShield();
                break;
                
            case BossState.Gravity:
                DeactivateGravity();
                DeactivateLasers();
                DeactivateShield();
                break;
        }
    }

    IEnumerator UFOCoroutine(){
        for(int i = 0; i < ufoSpawnCount; i++){
            if(isDefeated) yield break;
            
            GameObject ufo = GetPooled(ufoPool);
            if(ufo != null){
                Transform spawnPoint = GetRandomUFOSpawnPoint();
                if(spawnPoint != null){
                    ufo.transform.position = spawnPoint.position;
                    ufo.transform.rotation = spawnPoint.rotation;
                }
                else{
                    ufo.transform.position = transform.position + Random.insideUnitSphere * 5f;
                }
                ufo.SetActive(true);
                activeUFOs.Add(ufo);
                
                UFOController summoner = ufo.GetComponent<UFOController>();
                if(summoner != null) summoner.Initialize(player);
            }
            yield return new WaitForSeconds(ufoSpawnDelay);
        }
    }

    IEnumerator MissileCoroutine(){
        while(missilesSpawnedThisPhase < totalMissilesToSpawn && !isDefeated){
            if(!HasAvailableBuildings()){
                yield break;
            }
            
            int missilesInWave = Random.Range(minMissilesPerWave, maxMissilesPerWave + 1);
            missilesInWave = Mathf.Min(missilesInWave, totalMissilesToSpawn - missilesSpawnedThisPhase);
            
            for(int i = 0; i < missilesInWave; i++){
                if(isDefeated) yield break;
                
                Building target = GetRandomAvailableBuilding();
                if(target == null){
                    yield break;
                }
                
                GameObject missileObj = GetPooled(missilePool);
                if(missileObj != null){
                    if(missileSpawnPoint != null){
                        missileObj.transform.position = missileSpawnPoint.position;
                        missileObj.transform.rotation = missileSpawnPoint.rotation;
                    }
                    else{
                        missileObj.transform.position = transform.position + Vector3.up * 3f + Random.insideUnitSphere * 2f;
                    }
                    missileObj.layer = LayerMask.NameToLayer("EnemyProjectile");
                    missileObj.SetActive(true);
                    
                    MissileController missile = missileObj.GetComponent<MissileController>();
                    if(missile != null){
                        missile.Initialize(target);
                        activeMissiles.Add(missile);
                    }
                }
                
                missilesSpawnedThisPhase++;
                yield return new WaitForSeconds(missileSpawnDelay);
            }
            
            yield return new WaitForSeconds(missileWaveDelay);
        }
    }

    IEnumerator GravityCoroutine(){
        ActivateGravity();
        yield return new WaitForEndOfFrame();
        bossAimer.AimGravity();
        
        float chargeTimer = 0f;
        while(chargeTimer < chargeTime && !isDefeated){
            chargeTimer += Time.deltaTime;
            yield return null;
        }
        
        if(!isDefeated){
            WeakpointController chargingWeakpoint = weakpointManager.weakPoints[0];
            Vector3 origin = chargingWeakpoint.GetLaserOrigin();
            Vector3 direction = chargingWeakpoint.transform.forward;
            ActivateLaserFromWeakpoint(origin, direction);
            
            float laserTimer = 0f;
            while(laserTimer < laserDuration && !isDefeated){
                laserTimer += Time.deltaTime;
                yield return null;
            }
            
            DeactivateLasers();
        }
        
        DeactivateGravity();
        
        if(!isDefeated){
            ExitState();
            EnterState(GetNextState());
        }
    }

    void ActivateLaserFromWeakpoint(Vector3 origin, Vector3 direction){
        isLaserActive = true;
        if(laserFieldController != null){
            laserFieldController.ActivateSingleLaser(origin, direction, null);
        }
    }

    Transform GetRandomUFOSpawnPoint(){
        if(ufoSpawnPoints == null || ufoSpawnPoints.Length == 0) return null;
        
        List<Transform> availablePoints = new List<Transform>();
        foreach(Transform point in ufoSpawnPoints){
            if(point != null) availablePoints.Add(point);
        }
        
        if(availablePoints.Count == 0) return null;
        return availablePoints[Random.Range(0, availablePoints.Count)];
    }

    bool HasAvailableBuildings(){
        if(BuildingManager.Instance == null) return false;
        
        foreach(var kvp in BuildingManager.Instance.GetBuildingStates()){
            Building building = kvp.Key;
            BuildingState state = kvp.Value;
            if(building != null && building.IsAlive() && state != BuildingState.Destroyed){
                return true;
            }
        }
        return false;
    }
    
    Building GetRandomAvailableBuilding(){
        if(BuildingManager.Instance == null) return null;
        
        List<Building> availableBuildings = new List<Building>();
        
        foreach(var kvp in BuildingManager.Instance.GetBuildingStates()){
            Building building = kvp.Key;
            BuildingState state = kvp.Value;
            
            if(building == null) continue;
            if(!building.IsAlive()) continue;
            if(state == BuildingState.Destroyed) continue;
            
            availableBuildings.Add(building);
        }
        
        if(availableBuildings.Count == 0) return null;
        
        int randomIndex = Random.Range(0, availableBuildings.Count);
        return availableBuildings[randomIndex];
    }
    
    void UpdateBuildingTargets(){
        foreach(var kvp in BuildingManager.Instance.GetBuildingStates()){
            Building building = kvp.Key;
            if(building != null && building.isTargeted){
                bool isTargeted = false;
                foreach(MissileController missile in activeMissiles){
                    if(missile != null && !missile.IsExploded() && missile.GetTargetBuilding() == building){
                        isTargeted = true;
                        break;
                    }
                }
                
                if(!isTargeted){
                    BuildingManager.Instance.SetTargeted(building, false);
                }
            }
        }
    }
    
    void CleanupDeadEntities(){
        List<GameObject> ufoToRemove = new List<GameObject>();
        foreach(GameObject ufo in activeUFOs){
            if(ufo == null || !ufo.activeInHierarchy){
                ufoToRemove.Add(ufo);
            }
        }
        foreach(GameObject ufo in ufoToRemove){
            activeUFOs.Remove(ufo);
        }
        
        List<MissileController> missileToRemove = new List<MissileController>();
        foreach(MissileController missile in activeMissiles){
            if(missile == null || missile.IsExploded() || !missile.gameObject.activeInHierarchy){
                missileToRemove.Add(missile);
            }
        }
        foreach(MissileController missile in missileToRemove){
            activeMissiles.Remove(missile);
        }
        
        ufoPool.RemoveAll(obj => obj == null);
        missilePool.RemoveAll(obj => obj == null);
    }
    
    void ActivateGravity(){
        isGravityActive = true;
        if(gravityFieldController != null) gravityFieldController.Activate();
        if(weakpointManager != null) weakpointManager.ActivateAll();
    }
    
    void DeactivateGravity(){
        isGravityActive = false;
        if(gravityFieldController != null) gravityFieldController.Deactivate();
        if(weakpointManager != null) weakpointManager.DeactivateAll();
    }
    
    void DeactivateLasers(){
        isLaserActive = false;
        if(laserFieldController != null) laserFieldController.DeactivateLasers();
    }
    
    void ActivateShield(){
        isShieldActive = true;
        if(shieldCollider != null) shieldCollider.enabled = true;
        if(shieldSystem != null){
            shieldSystem.ShowShield();
            shieldSystem.SetDamageMultiplier(shieldDamageMultiplier);
        }
    }
    
    void DeactivateShield(){
        isShieldActive = false;
        if(shieldCollider != null) shieldCollider.enabled = false;
        if(shieldSystem != null){
            shieldSystem.HideShield();
            shieldSystem.SetDamageMultiplier(1f);
        }
    }
    
    void OnAllWeakPointsDestroyed(){
        if(currentState == BossState.Gravity){
            ExitState();
            EnterState(GetNextState());
        }
    }
    
    GameObject GetPooled(List<GameObject> pool){
        pool.RemoveAll(obj => obj == null);
        
        foreach(GameObject obj in pool){
            if(obj != null && !obj.activeInHierarchy){
                return obj;
            }
        }
        return null;
    }
    
    void Die(){
        if(isDefeated) return;
        isDefeated = true;
        
        DeactivateShield();
        
        if(deathEffect != null) deathEffect.Play();
        if(audioSource != null && deathSound != null) audioSource.PlayOneShot(deathSound);
        
        if(currentCoroutine != null){
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
        
        for(int i = 0; i < ufoParent.childCount; i++){
            Transform child = ufoParent.GetChild(i);
            if(child.gameObject.activeSelf){
                child.gameObject.SetActive(false);
            }
        }
        activeUFOs.Clear();
        
        for(int i = 0; i < missileParent.childCount; i++){
            Transform child = missileParent.GetChild(i);
            if(child.gameObject.activeSelf){
                child.gameObject.SetActive(false);
            }
        }
        activeMissiles.Clear();
        
        DeactivateLasers();
        if(weakpointManager != null) weakpointManager.DeactivateAll();
        
        foreach(var kvp in BuildingManager.Instance.GetBuildingStates()){
            if(kvp.Key != null && kvp.Key.isTargeted){
                BuildingManager.Instance.SetTargeted(kvp.Key, false);
            }
        }
        
        Destroy(gameObject, 3f);
    }
    
    #if UNITY_EDITOR
    [Button("Force UFO")] void DebugUFO(){ if(!isDefeated){ ExitState(); EnterState(BossState.UFO); } }
    [Button("Force Missile")] void DebugMissile(){ if(!isDefeated){ ExitState(); EnterState(BossState.Missile); } }
    [Button("Force Gravity")] void DebugGravity(){ if(!isDefeated){ ExitState(); EnterState(BossState.Gravity); } }
    [Button("Force Idle")] void DebugIdle(){ if(!isDefeated){ ExitState(); EnterState(BossState.Idle); } }
    [Button("Damage 50")] void DebugDamage(){ if(healthSystem != null) healthSystem.TakeDamage(50f); }
    #endif
}