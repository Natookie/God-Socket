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
    private Renderer[] renderers;
    private Material currentMaterial;
    private Material deactivatedMaterial;
    
    void Awake(){
        if(visualObject != null){
            renderers = visualObject.GetComponentsInChildren<Renderer>();
        }
    }
    
    void Start(){
        isActive = false;
        isDestroyed = false;
    }
    
    public void Activate(){
        isActive = true;
        isDestroyed = false;
    }
    
    public void Deactivate(){
        isActive = false;
        SetMaterial(deactivatedMaterial);
    }
    
    public void SetMaterial(Material material){
        if(renderers == null || renderers.Length == 0) return;
        if(material == null) return;
        
        currentMaterial = material;
        foreach(Renderer renderer in renderers){
            if(renderer != null) renderer.material = material;
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
            SetMaterial(deactivatedMaterial);
            OnWeakPointDestroyed?.Invoke(this);
        }
    }
    
    public Vector3 GetLaserOrigin() => laserOrigin != null ? laserOrigin.position : transform.position;
    public bool IsAlive() => isActive && !isDestroyed;
    
    public void ResetHealth(){
        health = 30f;
        isDestroyed = false;
    }
}