using UnityEngine;
using System.Collections;
using NaughtyAttributes;

public class WeakpointManager : MonoBehaviour
{
    [Header("WEAK POINTS")]
    public WeakpointController[] weakPoints;
    
    [Header("MATERIALS")]
    public Material threatMaterial;
    public Material deactivatedMaterial;
    public Material weakpointMaterial;
    
    public event System.Action OnAllWeakPointsDestroyed;
    
    private int activeCount;
    
    void Start(){
        DeactivateAll();
        for(int i = 0; i < weakPoints.Length; i++) weakPoints[i].SetDeactivatedMaterial(deactivatedMaterial);
    }
    
    void OnDestroy(){
        for(int i = 0; i < weakPoints.Length; i++){
            WeakpointController wp = weakPoints[i];
            if(wp != null) wp.OnWeakPointDestroyed -= OnWeakPointDestroyed;
        }
    }
    
    void AssignMaterial(WeakpointController wp, int index){
        if(wp == null) return;
        
        if(index == 0){
            wp.SetMaterial(threatMaterial);
            return;
        }
        wp.SetMaterial(weakpointMaterial);
        Debug.Log(weakpointMaterial);
    }
    
    public void ActivateAll(){
        activeCount = 0;
        for(int i = 0; i < weakPoints.Length; i++){
            WeakpointController wp = weakPoints[i];
            if(wp != null){
                wp.ResetHealth();
                wp.Activate();
                activeCount++;
                AssignMaterial(wp, i);
            }
        }
    }
    
    public void DeactivateAll(){
        for(int i = 0; i < weakPoints.Length; i++){
            WeakpointController wp = weakPoints[i];
            if(wp != null) wp.Deactivate();
        }
        activeCount = 0;
    }
    
    void OnWeakPointDestroyed(WeakpointController wp){
        if(wp != null) wp.SetMaterial(deactivatedMaterial);
        activeCount--;
        if(activeCount <= 0) OnAllWeakPointsDestroyed?.Invoke();
    }
    
    public Vector3[] GetLaserOrigins(){
        Vector3[] origins = new Vector3[weakPoints.Length];
        int count = 0;
        for(int i = 0; i < weakPoints.Length; i++){
            WeakpointController wp = weakPoints[i];
            if(wp != null && wp.IsAlive()){
                origins[count] = wp.GetLaserOrigin();
                count++;
            }
        }
        System.Array.Resize(ref origins, count);
        return origins;
    }
    
    public bool HasActiveWeakPoints() => activeCount > 0;
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected(){
        Gizmos.color = Color.magenta;
        for(int i = 0; i < weakPoints.Length; i++){
            WeakpointController wp = weakPoints[i];
            if(wp != null && wp.IsAlive()) Gizmos.DrawWireSphere(wp.GetLaserOrigin(), 0.5f);
        }
    }
    #endif
}