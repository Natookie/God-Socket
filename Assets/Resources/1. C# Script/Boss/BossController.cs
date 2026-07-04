using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class BossController : MonoBehaviour
{
    [Header("STATE TIMING")]
    public float idleDuration = 2f;
    public float ufoDuration = 5f;
    public float missileDuration = 8f;
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
    public int missilePoolSize = 5;
    public int missileCount = 3;
    public float missileSpawnDelay = 1f;
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
    
    private enum BossState { Idle, UFO, Missile, Gravity }
    private BossState currentState = BossState.Idle;
    private float stateTimer = 0f;
    private bool isDefeated = false;
    
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
        
        if(weakpointManager != null){
            weakpointManager.OnAllWeakPointsDestroyed += OnAllWeakPointsDestroyed;
        }
        
        if(shieldCollider != null) shieldCollider.enabled  = false;
        
        currentState = BossState.Idle;
        stateTimer = 0f;
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

        stateTimer += Time.deltaTime;
        CheckStateExit();
        CleanupDeadEntities();
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
                if(stateTimer >= ufoDuration || activeUFOs.Count == 0){
                    ExitState();
                    EnterState(BossState.Idle);
                }
                break;
                
            case BossState.Missile:
                if(stateTimer >= missileDuration || activeMissiles.Count == 0){
                    ExitState();
                    EnterState(BossState.Idle);
                }
                break;
                
            case BossState.Gravity:
                break;
        }
    }

    BossState GetNextState(){
        int choice = Random.Range(0, 3);
        
        if(choice == 1 && !HasAvailableBuildings()){
            choice = Random.Range(0, 2);
            if(choice == 1) choice = 0;
        }
        
        switch(choice){
            case 0: return BossState.UFO;
            case 1: return BossState.Missile;
            case 2: return BossState.Gravity;
            default: return BossState.UFO;
        }
    }

    void EnterState(BossState newState){
        if(isDefeated) return;
        
        currentState = newState;
        stateTimer = 0f;
        
        switch(newState){
            case BossState.Idle:
                DeactivateShield();
                break;
                
            case BossState.UFO:
                ActivateShield();
                currentCoroutine = StartCoroutine(UFOCoroutine());
                break;
                
            case BossState.Missile:
                ActivateShield();
                currentCoroutine = StartCoroutine(MissileCoroutine());
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
                foreach(GameObject ufo in activeUFOs){
                    if(ufo != null) ufo.SetActive(false);
                }
                activeUFOs.Clear();
                DeactivateShield();
                break;
                
            case BossState.Missile:
                foreach(MissileController missile in activeMissiles){
                    if(missile != null) missile.gameObject.SetActive(false);
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
        int spawned = 0;
        
        for(int i = 0; i < missileCount; i++){
            if(isDefeated) yield break;
            
            Building target = GetClosestAvailableBuilding();
            if(target == null) break;
            
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
                    spawned++;
                }
            }
            
            yield return new WaitForSeconds(missileSpawnDelay);
        }
        
        if(spawned == 0){
            ExitState();
            EnterState(BossState.Idle);
        }
    }

    IEnumerator GravityCoroutine(){
        ActivateGravity();
        
        float chargeTimer = 0f;
        while(chargeTimer < chargeTime && !isDefeated){
            chargeTimer += Time.deltaTime;
            yield return null;
        }
        
        if(!isDefeated){
            ActivateLasers();
            
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
            EnterState(BossState.Idle);
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
    
    Building GetClosestAvailableBuilding(){
        if(BuildingManager.Instance == null) return null;
        
        Building closest = null;
        float closestDistance = float.MaxValue;
        
        foreach(var kvp in BuildingManager.Instance.GetBuildingStates()){
            Building building = kvp.Key;
            BuildingState state = kvp.Value;
            
            if(building == null) continue;
            if(!building.IsAlive()) continue;
            if(state == BuildingState.Destroyed) continue;
            
            float distance = Vector3.Distance(transform.position, building.transform.position);
            if(distance < closestDistance){
                closestDistance = distance;
                closest = building;
            }
        }
        
        return closest;
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
        activeUFOs.RemoveAll(u => u == null || !u.activeInHierarchy);
        activeMissiles.RemoveAll(m => m == null || m.IsExploded());
        
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
    
    void ActivateLasers(){
        isLaserActive = true;
        if(laserFieldController != null && weakpointManager != null){
            List<Vector3> origins = weakpointManager.GetLaserOrigins();
            laserFieldController.ActivateLasers(origins);
        }
    }
    
    void DeactivateLasers(){
        isLaserActive = false;
        if(laserFieldController != null) laserFieldController.DeactivateLasers();
    }
    
    void ActivateShield(){
        isShieldActive = true;
        if(shieldCollider != null) shieldCollider.enabled = true;;
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
            EnterState(BossState.Idle);
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
        
        foreach(GameObject ufo in activeUFOs){
            if(ufo != null) ufo.SetActive(false);
        }
        activeUFOs.Clear();
        
        foreach(MissileController missile in activeMissiles){
            if(missile != null) missile.gameObject.SetActive(false);
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