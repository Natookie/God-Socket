using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class BossController : MonoBehaviour
{
    [Header("STATE TIMING")]
    public float stateDuration = 5f;
    public float stateVariance = 1.5f;
    public float idleDuration = 2f;
    
    [Header("UFO")]
    public GameObject ufoPrefab;
    public Transform ufoParent;
    public int ufoPoolSize = 10;
    public int ufoSpawnCount = 3;
    public float ufoSpawnDelay = 0.5f;
    
    [Header("MISSILE")]
    public GameObject missilePrefab;
    public Transform missileParent;
    public int missilePoolSize = 5;
    public int missileCount = 3;
    
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
    public ParticleSystem deathEffect;
    public AudioSource audioSource;
    public AudioClip deathSound;

    [Header("DEBUG")]
    [SerializeField] private bool disableLogic;
    [SerializeField] private bool enableDebugInput;
    
    private enum BossState { Idle, UFO, Missile, Gravity }
    private BossState currentState = BossState.Idle;
    private float timer;
    private bool isDefeated = false;
    
    private List<GameObject> ufoPool = new List<GameObject>();
    private List<GameObject> missilePool = new List<GameObject>();
    private List<GameObject> activeUFOs = new List<GameObject>();
    private List<GameObject> activeMissiles = new List<GameObject>();
    
    private bool isGravityActive = false;
    private bool isLaserActive = false;
    private bool isGravityStateInterruptible = false;
    private Coroutine gravityCoroutine;

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
    }
    
    void OnDestroy(){
        if(healthSystem != null) healthSystem.OnDeath -= Die;
        if(weakpointManager != null){
            weakpointManager.OnAllWeakPointsDestroyed -= OnAllWeakPointsDestroyed;
        }
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

        timer += Time.deltaTime;
        if(currentState == BossState.Idle){
            if(timer >= idleDuration){
                timer = 0f;
                PickRandomState();
            }
        }
        else{
            if(isGravityStateInterruptible && timer >= stateDuration + Random.Range(-stateVariance, stateVariance)){
                timer = 0f;
                EndCurrentState();
                currentState = BossState.Idle;
            }
        }
        
        CleanupDeadEntities();
    }

    void HandleDebugInput(){
        if(Input.GetKeyDown(KeyCode.Alpha1)){
            DebugUFO();
            Debug.Log("KD1");
        }
        else if(Input.GetKeyDown(KeyCode.Alpha2)){
            DebugMissile();
            Debug.Log("KD2");
        }
        else if(Input.GetKeyDown(KeyCode.Alpha3)){
            DebugGravity();
            Debug.Log("KD3");
        }
    }
    
    void PickRandomState(){
        int choice = Random.Range(0, 3);
        
        switch(choice){
            case 0: 
                currentState = BossState.UFO;
                isGravityStateInterruptible = true;
                StartCoroutine(SpawnUFOs());
                break;
            case 1: 
                currentState = BossState.Missile;
                isGravityStateInterruptible = true;
                StartCoroutine(SpawnMissile());
                break;
            case 2: 
                currentState = BossState.Gravity;
                isGravityStateInterruptible = false;
                gravityCoroutine = StartCoroutine(GravitySequence());
                break;
        }
    }
    
    void EndCurrentState(){
        if(isGravityActive) DeactivateGravity();
        if(isLaserActive) DeactivateLasers();
    }
    
    IEnumerator SpawnUFOs(){
        for(int i = 0; i < ufoSpawnCount; i++){
            if(isDefeated) yield break;
            
            GameObject ufo = GetPooled(ufoPool);
            if(ufo != null){
                ufo.transform.position = transform.position + Random.insideUnitSphere * 5f;
                ufo.SetActive(true);
                activeUFOs.Add(ufo);
                
                UFOController summoner = ufo.GetComponent<UFOController>();
                if(summoner != null) summoner.Initialize(player);
            }
            yield return new WaitForSeconds(ufoSpawnDelay);
        }
    }
    
    IEnumerator SpawnMissile(){
        for(int i = 0; i < missileCount; i++){
            if(isDefeated) yield break;
            
            if(!BuildingManager.Instance.HasUntargeted()) yield break;
            
            Building targetBuilding = BuildingManager.Instance.GetUntargeted();
            if(targetBuilding == null) yield break;
            
            GameObject missile = GetPooled(missilePool);
            if(missile != null){
                missile.transform.position = transform.position + Vector3.up * 3f;
                missile.SetActive(true);
                activeMissiles.Add(missile);
                
                MissileController launcher = missile.GetComponent<MissileController>();
                if(launcher != null) launcher.Initialize(targetBuilding);
            }
            yield return new WaitForSeconds(1f);
        }
    }
        
    IEnumerator GravitySequence(){
        ActivateGravity();
        
        float chargeTimer = 0f;
        while(chargeTimer < chargeTime && !isDefeated && isGravityActive){
            chargeTimer += Time.deltaTime;
            yield return null;
        }
        
        if(!isDefeated && isGravityActive){
            isLaserActive = true;
            ActivateLasers();
            
            float laserTimer = 0f;
            while(laserTimer < laserDuration && !isDefeated && isGravityActive){
                laserTimer += Time.deltaTime;
                yield return null;
            }
            
            isLaserActive = false;
            DeactivateLasers();
        }
        
        DeactivateGravity();
        currentState = BossState.Idle;
        isGravityStateInterruptible = true;
        gravityCoroutine = null;
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
        if(laserFieldController != null && weakpointManager != null){
            List<Vector3> origins = weakpointManager.GetLaserOrigins();
            laserFieldController.ActivateLasers(origins);
        }
    }
    
    void DeactivateLasers(){
        isLaserActive = false;
        if(laserFieldController != null) laserFieldController.DeactivateLasers();
    }
    
    void OnAllWeakPointsDestroyed(){
        if(isGravityActive){
            if(gravityCoroutine != null){
                StopCoroutine(gravityCoroutine);
                gravityCoroutine = null;
            }
            
            DeactivateGravity();
            DeactivateLasers();
            currentState = BossState.Idle;
            isGravityStateInterruptible = true;
        }
    }
    
    GameObject GetPooled(List<GameObject> pool){
        foreach(GameObject obj in pool){
            if(!obj.activeInHierarchy) return obj;
        }
        return null;
    }
    
    void CleanupDeadEntities(){
        activeUFOs.RemoveAll(u => u == null || !u.activeInHierarchy);
        activeMissiles.RemoveAll(m => m == null || !m.activeInHierarchy);
    }
    
    void Die(){
        if(isDefeated) return;
        isDefeated = true;
        
        if(deathEffect != null) deathEffect.Play();
        if(audioSource != null && deathSound != null) audioSource.PlayOneShot(deathSound);
        
        foreach(GameObject ufo in activeUFOs) if(ufo != null) Destroy(ufo);
        foreach(GameObject missile in activeMissiles) if(missile != null) Destroy(missile);
        DeactivateLasers();
        if(weakpointManager != null) weakpointManager.DeactivateAll();
        
        Destroy(gameObject, 3f);
    }
    
    #if UNITY_EDITOR
    [Button("Force UFO")] void DebugUFO(){ if(!isDefeated) StartCoroutine(SpawnUFOs()); }
    [Button("Force Missile")] void DebugMissile(){ if(!isDefeated) StartCoroutine(SpawnMissile()); }
    [Button("Force Gravity")] void DebugGravity(){ if(!isDefeated) StartCoroutine(GravitySequence()); }
    [Button("Damage 50")] void DebugDamage(){ if(healthSystem != null) healthSystem.TakeDamage(50f); }
    #endif
}