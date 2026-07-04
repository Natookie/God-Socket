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

    void Start(){
        if(visualObject != null) visualObject.SetActive(false);
        isActive = false;
    }
    
    public void Activate(){
        isActive = true;
        isDestroyed = false;
        if(visualObject != null) visualObject.SetActive(true);
    }
    
    public void Deactivate(){
        isActive = false;
        if(visualObject != null) visualObject.SetActive(false);
    }
    
    public void TakeDamage(float damage){
        if(isDestroyed || !isActive) return;
        
        health -= damage;
        if(health <= 0){
            isDestroyed = true;
            isActive = false;
            if(visualObject != null) visualObject.SetActive(false);
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