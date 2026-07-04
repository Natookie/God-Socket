using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class LaserFieldController : MonoBehaviour
{
    [Header("LASER SETTINGS")]
    public float laserDamage = 10f;
    public float laserWidth = 0.5f;
    public float laserExpandSpeed = 20f;
    public float maxLaserLength = 50f;
    public LayerMask buildingLayer;
    public Color laserStartColor = Color.red;
    public Color laserEndColor = new Color(1f, 0.5f, 0f, 1f);
    public Material laserMaterial;
    
    private List<LaserBeam> activeLasers = new List<LaserBeam>();
    private bool isActive = false;
    private BossAimer bossAimer;

    private class LaserBeam
    {
        public GameObject gameObject;
        public LineRenderer lineRenderer;
        public Vector3 startPosition;
        public Vector3 direction;
        public float currentLength;
        public float maxLength;
        public bool isExpanding;
        public List<IDamageable> damagedTargets = new List<IDamageable>();
        public Vector3 hitPoint;
        public bool hasHit;
        public RaycastHit hitInfo;
        
        public LaserBeam(GameObject obj, Vector3 start, Vector3 dir, float maxLen){
            gameObject = obj;
            lineRenderer = obj.GetComponent<LineRenderer>();
            startPosition = start;
            direction = dir.normalized;
            maxLength = maxLen;
            currentLength = 0.1f;
            isExpanding = true;
            damagedTargets.Clear();
            hitPoint = start;
            hasHit = false;
        }
    }
    
    void Start(){
        bossAimer = GetComponentInParent<BossAimer>();
        
        if(laserMaterial == null){
            laserMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if(laserMaterial == null){
                laserMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            if(laserMaterial != null){
                laserMaterial.color = laserStartColor;
            }
        }
    }
    
    public void ActivateLasers(List<Vector3> origins){
        isActive = true;
        SpawnLasers(origins);
    }
    
    public void DeactivateLasers(){
        isActive = false;
        ClearLasers();
    }
    
    void SpawnLasers(List<Vector3> origins){
        ClearLasers();
        
        if(bossAimer == null) return;
        
        Vector3 direction = bossAimer.GetAimDirection();
        
        foreach(Vector3 spawnPos in origins){
            GameObject laserObj = new GameObject($"Laser_{activeLasers.Count}");
            laserObj.transform.position = spawnPos;
            laserObj.transform.parent = transform;
            
            LineRenderer line = laserObj.AddComponent<LineRenderer>();
            line.startWidth = laserWidth;
            line.endWidth = laserWidth;
            
            if(laserMaterial != null){
                line.material = laserMaterial;
            }
            else{
                line.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if(line.material == null){
                    line.material = new Material(Shader.Find("Sprites/Default"));
                }
            }
            
            line.startColor = laserStartColor;
            line.endColor = laserEndColor;
            line.positionCount = 2;
            line.useWorldSpace = true;
            
            Vector3 initialEnd = spawnPos + direction * 0.1f;
            line.SetPosition(0, spawnPos);
            line.SetPosition(1, initialEnd);
            
            LaserBeam beam = new LaserBeam(laserObj, spawnPos, direction, maxLaserLength);
            beam.currentLength = 0.1f;
            activeLasers.Add(beam);
        }
    }
    
    void ClearLasers(){
        foreach(LaserBeam beam in activeLasers){
            if(beam.gameObject != null){
                Destroy(beam.gameObject);
            }
        }
        activeLasers.Clear();
    }
    
    void Update(){
        if(!isActive) return;
        
        Vector3 direction = bossAimer != null ? bossAimer.GetAimDirection() : Vector3.forward;
        
        foreach(LaserBeam beam in activeLasers){
            if(beam.gameObject == null) continue;
            
            beam.direction = direction;
            
            if(beam.isExpanding){
                beam.currentLength += laserExpandSpeed * Time.deltaTime;
                
                RaycastHit hit;
                if(Physics.Raycast(
                    beam.startPosition,
                    beam.direction,
                    out hit,
                    beam.currentLength,
                    buildingLayer
                )){
                    beam.hasHit = true;
                    beam.hitPoint = hit.point;
                    beam.hitInfo = hit;
                    beam.currentLength = hit.distance;
                    beam.isExpanding = false;
                    
                    IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                    if(damageable != null){
                        damageable.TakeDamage(laserDamage * Time.deltaTime);
                        if(!beam.damagedTargets.Contains(damageable)){
                            beam.damagedTargets.Add(damageable);
                        }
                    }
                    
                    UpdateLaserVisual(beam, beam.currentLength);
                }
                else if(beam.currentLength >= beam.maxLength){
                    beam.currentLength = beam.maxLength;
                    beam.isExpanding = false;
                    UpdateLaserVisual(beam, beam.currentLength);
                }
                else{
                    UpdateLaserVisual(beam, beam.currentLength);
                }
            }
            else{
                RaycastHit hit;
                if(Physics.Raycast(
                    beam.startPosition,
                    beam.direction,
                    out hit,
                    beam.currentLength,
                    buildingLayer
                )){
                    beam.hasHit = true;
                    beam.hitPoint = hit.point;
                    beam.hitInfo = hit;
                    beam.currentLength = hit.distance;
                    
                    IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                    if(damageable != null){
                        damageable.TakeDamage(laserDamage * Time.deltaTime);
                        if(!beam.damagedTargets.Contains(damageable)){
                            beam.damagedTargets.Add(damageable);
                        }
                    }
                    
                    UpdateLaserVisual(beam, beam.currentLength);
                }
                else{
                    beam.currentLength = Mathf.Min(beam.currentLength + laserExpandSpeed * Time.deltaTime, beam.maxLength);
                    UpdateLaserVisual(beam, beam.currentLength);
                }
            }
        }
    }
    
    void UpdateLaserVisual(LaserBeam beam, float length){
        if(beam.lineRenderer == null) return;
        
        Vector3 endPoint = beam.startPosition + beam.direction * length;
        beam.lineRenderer.SetPosition(0, beam.startPosition);
        beam.lineRenderer.SetPosition(1, endPoint);
        
        beam.currentLength = length;
    }
    
    void OnDisable(){
        ClearLasers();
        isActive = false;
    }
}