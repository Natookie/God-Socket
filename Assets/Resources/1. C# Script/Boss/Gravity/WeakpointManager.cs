using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class WeakpointManager : MonoBehaviour
{
    [Header("WEAK POINTS")]
    public List<WeakpointController> weakPoints = new List<WeakpointController>();
    
    [Header("MATERIALS")]
    public Material threatMaterial;
    public Material deactivatedMaterial;
    public Material weakpointMaterial;
    
    public event System.Action OnAllWeakPointsDestroyed;
    
    private int activeCount = 0;
    
    void Start(){
        StartCoroutine(SetMat());
    }

    IEnumerator SetMat(){
        yield return new WaitForSeconds(.1f);
        for(int i = 0; i < weakPoints.Count; i++){
            WeakpointController wp = weakPoints[i];
            if(wp != null){
                wp.OnWeakPointDestroyed += OnWeakPointDestroyed;
                wp.SetDeactivatedMaterial(deactivatedMaterial);
                wp.Deactivate();
                AssignMaterial(wp, i);
            }
        }
    }
    
    void OnDestroy(){
        foreach(WeakpointController wp in weakPoints){
            if(wp != null) wp.OnWeakPointDestroyed -= OnWeakPointDestroyed;
        }
    }
    
    void AssignMaterial(WeakpointController wp, int index){
        if(wp == null) return;
        
        if(index == 0) wp.SetMaterial(threatMaterial);
        else if(index >= 1 && index <= 3) wp.SetMaterial(weakpointMaterial);
        else wp.SetMaterial(deactivatedMaterial);
    }
    
    public void ActivateAll(){
        activeCount = 0;
        for(int i = 0; i < weakPoints.Count; i++){
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
        foreach(WeakpointController wp in weakPoints){
            if(wp != null){
                wp.Deactivate();
            }
        }
        activeCount = 0;
    }
    
    void OnWeakPointDestroyed(WeakpointController wp){
        if(wp != null){
            wp.SetMaterial(deactivatedMaterial);
        }
        activeCount--;
        if(activeCount <= 0){
            OnAllWeakPointsDestroyed?.Invoke();
        }
    }
    
    public List<Vector3> GetLaserOrigins(){
        List<Vector3> origins = new List<Vector3>();
        foreach(WeakpointController wp in weakPoints){
            if(wp != null && wp.IsAlive()) origins.Add(wp.GetLaserOrigin());
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
            if(wp != null && wp.IsAlive()) Gizmos.DrawWireSphere(wp.GetLaserOrigin(), 0.5f);
        }
    }
    #endif
}