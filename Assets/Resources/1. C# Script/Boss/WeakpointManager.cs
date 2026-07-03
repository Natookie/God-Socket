using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class WeakpointManager : MonoBehaviour
{
    [Header("WEAK POINTS")]
    public List<WeakpointController> weakPoints = new List<WeakpointController>();
    
    public event System.Action OnAllWeakPointsDestroyed;
    
    private int activeCount = 0;
    
    void Start(){
        foreach(WeakpointController wp in weakPoints){
            if(wp != null){
                wp.OnWeakPointDestroyed += OnWeakPointDestroyed;
                wp.Deactivate();
            }
        }
    }
    
    void OnDestroy(){
        foreach(WeakpointController wp in weakPoints){
            if(wp != null){
                wp.OnWeakPointDestroyed -= OnWeakPointDestroyed;
            }
        }
    }
    
    public void ActivateAll(){
        activeCount = 0;
        foreach(WeakpointController wp in weakPoints){
            if(wp != null){
                wp.ResetHealth();
                wp.Activate();
                activeCount++;
            }
        }
    }
    
    public void DeactivateAll(){
        foreach(WeakpointController wp in weakPoints){
            if(wp != null){
                wp.Deactivate();
            }
        }
        activeCount = 0;
    }
    
    void OnWeakPointDestroyed(WeakpointController wp){
        activeCount--;
        if(activeCount <= 0){
            OnAllWeakPointsDestroyed?.Invoke();
        }
    }
    
    public List<Vector3> GetLaserOrigins(){
        List<Vector3> origins = new List<Vector3>();
        foreach(WeakpointController wp in weakPoints){
            if(wp != null && wp.IsActive()){
                origins.Add(wp.GetLaserOrigin());
            }
        }
        return origins;
    }
    
    public bool HasActiveWeakPoints(){
        return activeCount > 0;
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected(){
        Gizmos.color = Color.magenta;
        foreach(WeakpointController wp in weakPoints){
            if(wp != null && wp.IsActive()){
                Gizmos.DrawWireSphere(wp.GetLaserOrigin(), 0.5f);
            }
        }
    }
    #endif
}