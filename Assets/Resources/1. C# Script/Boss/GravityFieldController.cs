using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class GravityFieldController : MonoBehaviour
{
    [Header("GRAVITY ELEMENTS")]
    public Transform gravityParent;
    public List<Transform> gravityElements = new List<Transform>();
    
    [Header("SETTINGS")]
    public float floatHeight = 10f;
    public float floatSpeed = 2f;
    public float damping = 5f;
    public float springStiffness = 10f;
    public float arrivalThreshold = 0.5f;
    public float resetSpeed = 3f;
    
    private bool isActive = false;
    private Dictionary<Transform, Vector3> startPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Rigidbody> elementRigidbodies = new Dictionary<Transform, Rigidbody>();
    private Dictionary<Transform, float> phaseOffsets = new Dictionary<Transform, float>();
    private Dictionary<Transform, Vector3> targetPositions = new Dictionary<Transform, Vector3>();
    
    private List<Transform> elementsToRemove = new List<Transform>();
    private bool isResetting = false;

    void Start(){
        if(gravityParent == null){
            gravityParent = new GameObject("GravityElements").transform;
            gravityParent.SetParent(transform);
        }
        PopulateList();
        CacheElements();
    }
    
    public void Activate(){
        isActive = true;
        isResetting = false;
        foreach(var kvp in elementRigidbodies){
            if(kvp.Value != null){
                kvp.Value.useGravity = false;
                kvp.Value.constraints = RigidbodyConstraints.FreezeRotation;
                kvp.Value.linearDamping = damping;
                kvp.Value.angularDamping = damping;
            }
        }
    }
    
    public void Deactivate(){
        isActive = false;
        isResetting = true;
        foreach(var kvp in elementRigidbodies){
            if(kvp.Value != null){
                kvp.Value.useGravity = true;
                kvp.Value.constraints = RigidbodyConstraints.None;
                kvp.Value.linearDamping = 0f;
                kvp.Value.angularDamping = 0.05f;
            }
        }
    }
    
    public bool AreElementsAtTarget(){
        if(!isActive || gravityElements.Count == 0) return false;
        
        foreach(Transform element in gravityElements){
            if(element == null || !startPositions.ContainsKey(element)) continue;
            
            float currentHeight = Vector3.Distance(startPositions[element], element.position);
            if(Mathf.Abs(currentHeight - floatHeight) > arrivalThreshold){
                return false;
            }
        }
        
        return true;
    }
    
    void CacheElements(){
        startPositions.Clear();
        elementRigidbodies.Clear();
        phaseOffsets.Clear();
        targetPositions.Clear();
        
        foreach(Transform element in gravityElements){
            if(element != null){
                startPositions[element] = element.position;
                phaseOffsets[element] = Random.Range(0f, Mathf.PI * 2f);
                targetPositions[element] = element.position;
                
                Rigidbody rb = element.GetComponent<Rigidbody>();
                if(rb == null){
                    rb = element.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 1f;
                    rb.linearDamping = 1f;
                    rb.angularDamping = 1f;
                }
                elementRigidbodies[element] = rb;
            }
        }
    }
    
    void FixedUpdate(){
        if(gravityElements.Count == 0) return;
        
        elementsToRemove.Clear();
        foreach(var element in gravityElements){
            if(element == null) elementsToRemove.Add(element);
        }
        foreach(var element in elementsToRemove){
            gravityElements.Remove(element);
        }
        
        if(isResetting){
            ResetElements();
            return;
        }
        
        if(!isActive) return;
        
        float time = Time.fixedTime;
        
        foreach(Transform element in gravityElements){
            if(element == null) continue;
            if(!startPositions.ContainsKey(element) || !elementRigidbodies.ContainsKey(element)) continue;
            
            Rigidbody rb = elementRigidbodies[element];
            if(rb == null) continue;
            
            Vector3 startPos = startPositions[element];
            float phase = phaseOffsets[element];
            
            float heightOffset = floatHeight + Mathf.Sin(time * floatSpeed + phase) * 0.5f;
            Vector3 targetPos = startPos + Vector3.up * heightOffset;
            targetPositions[element] = targetPos;
            
            Vector3 displacement = targetPos - rb.position;
            Vector3 springForce = displacement * springStiffness;
            Vector3 dampingForce = -rb.linearVelocity * damping;
            
            rb.AddForce(springForce + dampingForce, ForceMode.Force);
            
            Vector3 horizontalPos = new Vector3(rb.position.x, 0, rb.position.z);
            Vector3 horizontalStart = new Vector3(startPos.x, 0, startPos.z);
            if(Vector3.Distance(horizontalPos, horizontalStart) > 1f){
                Vector3 restoreForce = (horizontalStart - horizontalPos) * 2f;
                rb.AddForce(restoreForce, ForceMode.Force);
            }
        }
    }
    
    void ResetElements(){
        bool allReset = true;
        
        foreach(Transform element in gravityElements){
            if(element == null || !startPositions.ContainsKey(element)) continue;
            
            Rigidbody rb = elementRigidbodies[element];
            if(rb != null){
                Vector3 startPos = startPositions[element];
                Vector3 displacement = startPos - rb.position;
                
                if(displacement.magnitude > 0.1f){
                    allReset = false;
                    rb.AddForce(displacement * resetSpeed, ForceMode.Force);
                    rb.linearVelocity *= 0.9f;
                }
                else{
                    rb.position = startPos;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
        
        if(allReset){
            isResetting = false;
        }
    }
    
    [Button("Populate List", EButtonEnableMode.Editor)]
    void PopulateList(){
        gravityElements.Clear();
        
        if(gravityParent == null){
            Debug.LogWarning("Gravity Parent is not assigned!");
            return;
        }
        
        foreach(Transform child in gravityParent){
            gravityElements.Add(child);
        }
        
        Debug.Log($"Populated {gravityElements.Count} gravity elements");
        CacheElements();
    }
    
    [Button("Clear List", EButtonEnableMode.Editor)]
    void ClearList(){
        gravityElements.Clear();
        startPositions.Clear();
        elementRigidbodies.Clear();
        phaseOffsets.Clear();
        targetPositions.Clear();
        Debug.Log("Gravity elements list cleared");
    }
    
    [Button("Reset Positions", EButtonEnableMode.Editor)]
    void ResetPositions(){
        foreach(var kvp in startPositions){
            if(kvp.Key != null && elementRigidbodies.ContainsKey(kvp.Key)){
                Rigidbody rb = elementRigidbodies[kvp.Key];
                if(rb != null){
                    rb.position = kvp.Value;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected(){
        Gizmos.color = Color.cyan;
        foreach(Transform element in gravityElements){
            if(element != null && startPositions.ContainsKey(element)){
                Vector3 start = startPositions[element];
                Vector3 end = start + Vector3.up * floatHeight;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawWireSphere(end, 0.3f);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(element.position, 0.2f);
                Gizmos.color = Color.cyan;
            }
        }
    }
    #endif
}