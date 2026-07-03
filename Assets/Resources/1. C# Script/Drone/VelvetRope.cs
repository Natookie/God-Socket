using UnityEngine;
using System.Collections.Generic;

public class VerletRope : MonoBehaviour
{
    [Header("ENDPOINTS")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("PHYSICS")]
    public int segments = 20;
    public float segmentLength = 0.5f;
    public float gravity = -9.81f;
    public float damping = 0.98f;
    public int constraintIterations = 10;
    public float maxStretchRatio = 1.3f;
    public float stiffness = 0.5f;

    [Header("RENDERING")]
    public LineRenderer lineRenderer;

    [Header("COLLISION")]
    public bool enableCollision = true;
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.1f;
    public float collisionPushForce = 0.5f;
    
    [Header("COLLIDER OPTIMIZATION")]
    public bool enableCapsuleColliders = true;
    public float capsuleRadius = 0.1f;
    public bool capsuleIsTrigger = true;
    public int colliderSubdivisions = 2;
    public bool preventSelfCollision = true;

    private List<VerletPoint> points;
    private Vector3[] renderPositions;
    private float totalLength;
    private bool initialized;
    private bool ropeVisible;
    private bool isFirstFrame = true;
    
    private List<CapsuleCollider> segmentColliders;
    private List<Vector3> segmentPositions;
    private List<Quaternion> segmentRotations;
    private List<float> segmentHeights;
    private Transform poolParent;
    private int activeSegments;

    struct VerletPoint
    {
        public Vector3 position;
        public Vector3 previousPosition;
    }

    void Awake(){
        InitializeRope();
        if(enableCapsuleColliders) InitializeColliders();
    }

    void InitializeRope(){
        if(points == null) points = new List<VerletPoint>();
        else points.Clear();

        totalLength = segments * segmentLength;

        Vector3 start = startPoint ? startPoint.position : transform.position;
        Vector3 end = endPoint ? endPoint.position : start + Vector3.down * totalLength;

        for(int i = 0; i <= segments; i++){
            float t = (float)i / segments;
            Vector3 pos = Vector3.Lerp(start, end, t);
            points.Add(new VerletPoint{
                position = pos,
                previousPosition = pos
            });
        }

        renderPositions = new Vector3[points.Count];
        initialized = true;
        isFirstFrame = true;

        if(!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        if(lineRenderer) lineRenderer.positionCount = points.Count;
    }

    void InitializeColliders(){
        if(segmentColliders == null){
            segmentColliders = new List<CapsuleCollider>();
            segmentPositions = new List<Vector3>();
            segmentRotations = new List<Quaternion>();
            segmentHeights = new List<float>();
        }
        
        if(poolParent == null){
            poolParent = new GameObject("RopeColliderPool").transform;
            poolParent.SetParent(transform);
        }
        
        int totalSegments = segments;
        int colliderCount = Mathf.CeilToInt((float)totalSegments / colliderSubdivisions);
        
        for(int i = 0; i < colliderCount; i++){
            CapsuleCollider collider = CreateCollider();
            segmentColliders.Add(collider);
            segmentPositions.Add(Vector3.zero);
            segmentRotations.Add(Quaternion.identity);
            segmentHeights.Add(segmentLength * colliderSubdivisions);
        }
        
        if(preventSelfCollision){
            for(int i = 0; i < segmentColliders.Count; i++){
                for(int j = i + 1; j < segmentColliders.Count; j++){
                    if(segmentColliders[i] != null && segmentColliders[j] != null){
                        Physics.IgnoreCollision(segmentColliders[i], segmentColliders[j], true);
                    }
                }
            }
        }
        
        activeSegments = colliderCount;
    }

    CapsuleCollider CreateCollider(){
        GameObject colliderObj = new GameObject($"RopeCollider_{segmentColliders.Count}");
        colliderObj.transform.SetParent(poolParent);
        
        CapsuleCollider capsule = colliderObj.AddComponent<CapsuleCollider>();
        capsule.radius = capsuleRadius;
        capsule.height = 1f;
        capsule.direction = 2;
        capsule.isTrigger = capsuleIsTrigger;
        
        Rigidbody rb = colliderObj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        
        return capsule;
    }

    void FixedUpdate(){
        if(!initialized || points == null || points.Count < 2) return;
        if(!ropeVisible) return;

        UpdateSimulation(Time.fixedDeltaTime);
        UpdateRenderer();
        if(enableCapsuleColliders) UpdateColliders();
    }

    void UpdateSimulation(float deltaTime){
        Vector3 startPos = startPoint ? startPoint.position : transform.position;
        Vector3 endPos = endPoint ? endPoint.position : (points[points.Count - 1].position);

        VerletPoint first = points[0];
        Vector3 startDelta = startPos - first.position;
        first.previousPosition = first.position;
        first.position = startPos;
        points[0] = first;

        VerletPoint last = points[points.Count - 1];
        Vector3 endDelta = endPos - last.position;
        last.previousPosition = last.position;
        last.position = endPos;
        points[points.Count - 1] = last;

        float endDistance = Vector3.Distance(startPos, endPos);
        float maxLength = totalLength * maxStretchRatio;
        if(endDistance > maxLength){
            Vector3 midPoint = (startPos + endPos) * 0.5f;
            Vector3 dirA = (midPoint - startPos).normalized;
            Vector3 dirB = (midPoint - endPos).normalized;
            float halfMax = maxLength * 0.5f;

            if(!startPoint){
                first.position = midPoint - dirA * halfMax;
                first.previousPosition = first.position;
                points[0] = first;
            }
            if(!endPoint){
                last.position = midPoint - dirB * halfMax;
                last.previousPosition = last.position;
                points[points.Count - 1] = last;
            }
        }

        if(!isFirstFrame){
            for(int i = 1; i < points.Count - 1; i++){
                VerletPoint point = points[i];
                Vector3 velocity = (point.position - point.previousPosition) * damping;
                Vector3 newPosition = point.position + velocity;
                newPosition.y += gravity * deltaTime * deltaTime;

                point.previousPosition = point.position;
                point.position = newPosition;
                points[i] = point;
            }
        }
        else{
            isFirstFrame = false;
        }

        for(int iter = 0; iter < constraintIterations; iter++){
            for(int i = 0; i < points.Count - 1; i++){
                VerletPoint a = points[i];
                VerletPoint b = points[i + 1];

                Vector3 dir = b.position - a.position;
                float dist = dir.magnitude;
                if(dist < 0.0001f) continue;

                float correctionFactor = 0.5f * stiffness;
                Vector3 correction = dir * ((dist - segmentLength) / dist) * correctionFactor;

                if(i > 0){
                    a.position += correction;
                    points[i] = a;
                }
                if(i + 1 < points.Count - 1){
                    b.position -= correction;
                    points[i + 1] = b;
                }
            }
        }

        if(enableCollision){
            for(int i = 1; i < points.Count - 1; i++){
                VerletPoint point = points[i];
                Collider[] colliders = Physics.OverlapSphere(point.position, collisionRadius, collisionMask);
                foreach(Collider col in colliders){
                    Vector3 closest = col.ClosestPoint(point.position);
                    float penetration = collisionRadius - Vector3.Distance(point.position, closest);
                    if(penetration > 0f){
                        point.position += (point.position - closest).normalized * penetration * collisionPushForce;
                    }
                }
                points[i] = point;
            }
        }
    }

    void UpdateRenderer(){
        if(!lineRenderer) return;
        for(int i = 0; i < points.Count; i++)
            renderPositions[i] = points[i].position;
        lineRenderer.SetPositions(renderPositions);
    }

    void UpdateColliders(){
        if(segmentColliders == null || segmentColliders.Count == 0 || points == null || points.Count < 2) return;
        
        int colliderIndex = 0;
        int pointIndex = 0;
        
        while(pointIndex < points.Count - 1 && colliderIndex < segmentColliders.Count){
            int endIndex = Mathf.Min(pointIndex + colliderSubdivisions, points.Count - 1);
            
            Vector3 startPos = points[pointIndex].position;
            Vector3 endPos = points[endIndex].position;
            Vector3 midPoint = (startPos + endPos) * 0.5f;
            Vector3 direction = (endPos - startPos).normalized;
            float distance = Vector3.Distance(startPos, endPos);
            
            if(distance < 0.001f){
                pointIndex = endIndex;
                continue;
            }
            
            segmentPositions[colliderIndex] = midPoint;
            segmentRotations[colliderIndex] = Quaternion.LookRotation(direction);
            segmentHeights[colliderIndex] = distance;
            
            pointIndex = endIndex;
            colliderIndex++;
        }
        
        activeSegments = colliderIndex;
        
        for(int i = 0; i < segmentColliders.Count; i++){
            if(i < activeSegments){
                CapsuleCollider collider = segmentColliders[i];
                collider.transform.position = segmentPositions[i];
                collider.transform.rotation = segmentRotations[i];
                collider.height = segmentHeights[i] + capsuleRadius * 2f;
                collider.enabled = ropeVisible;
            }
            else{
                segmentColliders[i].enabled = false;
            }
        }
    }

    public void ResetRope(){
        if(!initialized || points == null) return;
        
        Vector3 start = startPoint ? startPoint.position : transform.position;
        Vector3 end = endPoint ? endPoint.position : start + Vector3.down * totalLength;
        
        for(int i = 0; i <= segments; i++){
            float t = (float)i / segments;
            Vector3 pos = Vector3.Lerp(start, end, t);
            VerletPoint point = points[i];
            point.position = pos;
            point.previousPosition = pos;
            points[i] = point;
        }
        
        isFirstFrame = true;
    }

    public float GetTotalLength() => totalLength;
    public bool IsInitialized() => initialized;
    
    public void SetVisible(bool visible){
        ropeVisible = visible;
        if(lineRenderer) lineRenderer.enabled = visible;
        enabled = visible;
        
        if(visible && initialized){
            ResetRope();
        }
        
        if(enableCapsuleColliders && segmentColliders != null){
            foreach(CapsuleCollider collider in segmentColliders){
                if(collider != null) collider.enabled = visible;
            }
        }
    }

    public List<CapsuleCollider> GetColliders(){
        return segmentColliders;
    }

    public void RefreshColliders(){
        if(!enableCapsuleColliders) return;
        
        if(segmentColliders != null){
            foreach(CapsuleCollider collider in segmentColliders){
                if(collider != null) Destroy(collider.gameObject);
            }
            segmentColliders.Clear();
            segmentPositions.Clear();
            segmentRotations.Clear();
            segmentHeights.Clear();
        }
        
        InitializeColliders();
    }

    public void SetCapsuleRadius(float radius){
        capsuleRadius = radius;
        if(segmentColliders != null){
            foreach(CapsuleCollider collider in segmentColliders){
                if(collider != null) collider.radius = radius;
            }
        }
    }

    public void SetCapsuleTrigger(bool trigger){
        capsuleIsTrigger = trigger;
        if(segmentColliders != null){
            foreach(CapsuleCollider collider in segmentColliders){
                if(collider != null) collider.isTrigger = trigger;
            }
        }
    }

    public bool IsPointOnRope(Vector3 point, float checkRadius = -1f){
        if(checkRadius < 0) checkRadius = capsuleRadius;
        if(points == null || points.Count < 2) return false;
        
        for(int i = 0; i < points.Count - 1; i++){
            Vector3 start = points[i].position;
            Vector3 end = points[i + 1].position;
            Vector3 closest = ClosestPointOnSegment(point, start, end);
            if(Vector3.Distance(point, closest) < checkRadius) return true;
        }
        return false;
    }

    Vector3 ClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end){
        Vector3 direction = (end - start).normalized;
        float length = Vector3.Distance(start, end);
        float t = Vector3.Dot(point - start, direction);
        t = Mathf.Clamp(t, 0f, length);
        return start + direction * t;
    }

    void OnDestroy(){
        if(segmentColliders != null){
            foreach(CapsuleCollider collider in segmentColliders){
                if(collider != null) Destroy(collider.gameObject);
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate(){
        if(segments < 2) segments = 2;
        if(segmentLength < 0.01f) segmentLength = 0.01f;
        if(constraintIterations < 1) constraintIterations = 1;
        if(stiffness < 0.1f) stiffness = 0.1f;
        if(stiffness > 1f) stiffness = 1f;
        if(colliderSubdivisions < 1) colliderSubdivisions = 1;
    }

    void OnDrawGizmosSelected(){
        if(!initialized || points == null) return;
        Gizmos.color = Color.yellow;
        for(int i = 0; i < points.Count - 1; i++)
            Gizmos.DrawLine(points[i].position, points[i + 1].position);

        if(startPoint){
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.15f);
        }
        if(endPoint){
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, 0.15f);
        }
        
        if(enableCapsuleColliders && segmentColliders != null){
            Gizmos.color = Color.cyan;
            for(int i = 0; i < activeSegments && i < segmentColliders.Count; i++){
                if(segmentColliders[i] != null && segmentColliders[i].enabled){
                    Vector3 center = segmentColliders[i].transform.position;
                    float height = segmentHeights[i];
                    float radius = capsuleRadius;
                    
                    Gizmos.DrawWireSphere(center + segmentColliders[i].transform.forward * height * 0.5f, radius);
                    Gizmos.DrawWireSphere(center - segmentColliders[i].transform.forward * height * 0.5f, radius);
                    
                    Vector3 right = segmentColliders[i].transform.right * radius;
                    Vector3 up = segmentColliders[i].transform.up * radius;
                    Vector3 forward = segmentColliders[i].transform.forward * height * 0.5f;
                    
                    Gizmos.DrawLine(center + forward + right, center - forward + right);
                    Gizmos.DrawLine(center + forward - right, center - forward - right);
                    Gizmos.DrawLine(center + forward + up, center - forward + up);
                    Gizmos.DrawLine(center + forward - up, center - forward - up);
                }
            }
        }
    }
#endif
}