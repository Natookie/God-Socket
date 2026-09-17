using UnityEngine;
using NaughtyAttributes;

public class WeakpointController : MonoBehaviour, IDamageable
{
    [Header("WEAK POINT")]
    public float health = 30f;
    public GameObject visualObject;
    public Transform laserOrigin;
    
    public event System.Action<WeakpointController> OnWeakPointDestroyed;
    
    private bool isDestroyed = false;
    private bool isActive = false;
    private Renderer thisRenderer;
    private BoxCollider coll;
    private Material currentMaterial;
    private Material deactivatedMaterial;
    
    void Awake(){
        if(visualObject != null) thisRenderer = GetComponent<Renderer>();
    }
    
    void Start(){
        isActive = false;
        isDestroyed = false;
        coll = GetComponent<BoxCollider>();
    }
    
    public void Activate(){
        isActive = true;
        isDestroyed = false;
        if(coll) coll.enabled = true;
        if(currentMaterial != null) SetMaterial(currentMaterial);
    }
    
    public void Deactivate(){
        isActive = false;
        if(coll) coll.enabled = false;
        SetMaterial(deactivatedMaterial);
    }
    
    public void SetMaterial(Material material){
        if(material == null) return;
        
        currentMaterial = material;
        Material[] materials = new Material[1] { material };
        
        if(thisRenderer != null){
            thisRenderer.sharedMaterials = materials;
            thisRenderer.enabled = true;
        }
    }
    
    public void SetDeactivatedMaterial(Material material){
        deactivatedMaterial = material;
    }
    
    public void TakeDamage(float damage){
        if(isDestroyed || !isActive) return;
        
        health -= damage;
        if(health <= 0){
            isDestroyed = true;
            isActive = false;
            AudioManager.Instance.PlaySFX(GameSFX.UfoDestroyed);
            SetMaterial(deactivatedMaterial);
            OnWeakPointDestroyed?.Invoke(this);
        }
    }
    
    public Vector3 GetLaserOrigin() => laserOrigin != null ? laserOrigin.position : transform.position;
    public bool IsAlive() => !isDestroyed;
    
    public void ResetHealth(){
        health = 30f;
        isDestroyed = false;
    }
}