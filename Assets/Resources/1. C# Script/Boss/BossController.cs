using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class BossController : MonoBehaviour, IDamageable
{
    [Header("BOSS STATS")]
    public float maxHealth = 100f;
    public float currentHealth;
    
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
    public float fieldRadius = 20f;
    public float chargeTime = 2f;
    public float laserDuration = 2f;
    public float laserDamage = 10f;
    public int laserCount = 3;
    public GameObject weakPointPrefab;
    public float weakPointHealth = 30f;
    public Vector3 weakPointOffset = new Vector3(0, 4f, 6f);
    
    [Header("REFERENCES")]
    public Transform player;
    public ParticleSystem deathEffect;
    public AudioSource audioSource;
    public AudioClip damageSound;
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
    private List<GameObject> activeLasers = new List<GameObject>();
    private List<float> laserX = new List<float>();
    
    private Transform weakPoint;
    private bool isGravityActive = false;
    private bool isLaserActive = false;
    private List<float> laserXPos = new List<float>();

    void Start(){
        currentHealth = maxHealth;
        CreatePools();
        
        if(player == null){
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if(p != null) player = p.transform;
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
            if(timer >= stateDuration + Random.Range(-stateVariance, stateVariance)){
                timer = 0f;
                EndCurrentState();
                currentState = BossState.Idle;
            }
        }
        
        UpdateGravity();
        UpdateLasers();
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
                StartCoroutine(SpawnUFOs());
                break;
            case 1: 
                currentState = BossState.Missile;
                StartCoroutine(SpawnMissile());
                break;
            case 2: 
                currentState = BossState.Gravity;
                StartCoroutine(GravitySequence());
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
        while(chargeTimer < chargeTime && !isDefeated){
            chargeTimer += Time.deltaTime;
            yield return null;
        }
        
        if(!isDefeated && isGravityActive){
            isLaserActive = true;
            ActivateLasers();
            
            float laserTimer = 0f;
            while(laserTimer < laserDuration && !isDefeated){
                laserTimer += Time.deltaTime;
                yield return null;
            }
            
            isLaserActive = false;
            DeactivateLasers();
        }
        
        DeactivateGravity();
    }
    
    void ActivateGravity(){
        isGravityActive = true;
        SpawnWeakPoint();
    }
    
    void DeactivateGravity(){
        isGravityActive = false;
        DespawnWeakPoint();
    }
    
    void UpdateGravity(){
        if(!isGravityActive) return;
    }
    
    void ActivateLasers(){
        laserX.Clear();
        for(int i = 0; i < laserCount; i++){
            float x = Random.Range(-fieldRadius * 0.7f, fieldRadius * 0.7f);
            laserX.Add(x);
            
            Vector3 pos = transform.position + new Vector3(x, 0, 0);
            GameObject laser = CreateLaserLine(pos);
            activeLasers.Add(laser);
        }
    }
    
    void UpdateLasers(){
        if(!isLaserActive) return;
        
        for(int i = 0; i < laserX.Count; i++){
            Vector3 pos = transform.position + new Vector3(laserX[i], 0, 0);
            Vector3 start = pos + Vector3.down * fieldRadius;
            Vector3 end = pos + Vector3.up * fieldRadius;
            
            RaycastHit[] hits = Physics.RaycastAll(start, Vector3.up, fieldRadius * 2f);
            foreach(RaycastHit hit in hits){
                IDamageable dmg = hit.collider.GetComponent<IDamageable>();
                if(dmg != null) dmg.TakeDamage(laserDamage * Time.deltaTime);
            }
        }
    }
    
    void DeactivateLasers(){
        foreach(GameObject laser in activeLasers){
            if(laser != null) Destroy(laser);
        }
        activeLasers.Clear();
        laserX.Clear();
    }
    
    GameObject CreateLaserLine(Vector3 position){
        GameObject obj = new GameObject("Laser");
        obj.transform.position = position;
        obj.transform.parent = transform;
        
        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.startWidth = 0.5f;
        line.endWidth = 0.5f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.red;
        line.endColor = Color.red;
        
        Vector3[] positions = new Vector3[]{
            position + Vector3.down * fieldRadius,
            position + Vector3.up * fieldRadius
        };
        line.positionCount = positions.Length;
        line.SetPositions(positions);
        
        return obj;
    }
    
    void SpawnWeakPoint(){
        if(weakPointPrefab == null) return;
        
        Vector3 pos = transform.position + transform.rotation * weakPointOffset;
        GameObject obj = Instantiate(weakPointPrefab, pos, Quaternion.identity);
        obj.transform.parent = transform;
        weakPoint = obj.transform;
        
        WeakPointController wp = obj.GetComponent<WeakPointController>();
        if(wp != null) wp.Initialize(weakPointHealth, OnWeakPointDestroyed);
    }
    
    void DespawnWeakPoint(){
        if(weakPoint != null){
            Destroy(weakPoint.gameObject);
            weakPoint = null;
        }
    }
    
    void OnWeakPointDestroyed(){
        if(isGravityActive){
            DeactivateGravity();
            DeactivateLasers();
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
    
    public void TakeDamage(float damage){
        if(isDefeated) return;
        
        currentHealth -= damage;
        
        if(audioSource != null && damageSound != null){
            audioSource.PlayOneShot(damageSound);
        }
        
        if(currentHealth <= 0f) Die();
    }
    
    public bool IsAlive(){
        return !isDefeated;
    }
    
    void Die(){
        isDefeated = true;
        
        if(deathEffect != null) deathEffect.Play();
        if(audioSource != null && deathSound != null) audioSource.PlayOneShot(deathSound);
        
        foreach(GameObject ufo in activeUFOs) if(ufo != null) Destroy(ufo);
        foreach(GameObject missile in activeMissiles) if(missile != null) Destroy(missile);
        DeactivateLasers();
        DespawnWeakPoint();
        
        Destroy(gameObject, 3f);
    }
    
    #if UNITY_EDITOR
    [Button("Force UFO")] void DebugUFO(){ if(!isDefeated) StartCoroutine(SpawnUFOs()); }
    [Button("Force Missile")] void DebugMissile(){ if(!isDefeated) StartCoroutine(SpawnMissile()); }
    [Button("Force Gravity")] void DebugGravity(){ if(!isDefeated) StartCoroutine(GravitySequence()); }
    [Button("Damage 50")] void DebugDamage(){ TakeDamage(50f); }
    #endif
}