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

    [Header("RENDERING")]
    public LineRenderer lineRenderer;

    [Header("COLLISION (optional)")]
    public bool enableCollision;
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.1f;
    public float collisionPushForce = 0.5f;

    private List<VerletPoint> points;
    private Vector3[] renderPositions;
    private float totalLength;
    private bool initialized;
    private bool ropeVisible;

    struct VerletPoint
    {
        public Vector3 position;
        public Vector3 previousPosition;
    }

    void Awake(){
        InitializeRope();
    }

    void InitializeRope(){
        if(points == null) points = new List<VerletPoint>();
        else points.Clear();

        totalLength = segments * segmentLength;

        Vector3 start = startPoint ? startPoint.position : transform.position;
        Vector3 end = endPoint ? endPoint.position : start + Vector3.down * totalLength;

        for(int i = 0; i <= segments; i++){
            float t = (float)i / segments;
            points.Add(new VerletPoint{
                position = Vector3.Lerp(start, end, t),
                previousPosition = Vector3.Lerp(start, end, t)
            });
        }

        renderPositions = new Vector3[points.Count];
        initialized = true;

        if(!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        if(lineRenderer) lineRenderer.positionCount = points.Count;
    }

    void FixedUpdate(){
        if(!initialized || points == null || points.Count < 2) return;
        if(!ropeVisible) return;

        UpdateSimulation(Time.fixedDeltaTime);
        UpdateRenderer();
    }

    void UpdateSimulation(float deltaTime){
        Vector3 startPos = startPoint ? startPoint.position : transform.position;
        Vector3 endPos = endPoint ? endPoint.position : (points[points.Count - 1].position);

        // --- Preserve endpoint velocities ---
        // Move first point
        VerletPoint first = points[0];
        Vector3 startDelta = startPos - first.position;
        first.previousPosition = first.position;
        first.position = startPos;
        points[0] = first;

        // Move last point
        VerletPoint last = points[points.Count - 1];
        Vector3 endDelta = endPos - last.position;
        last.previousPosition = last.position;
        last.position = endPos;
        points[points.Count - 1] = last;

        // --- Handle over‑extension ---
        float endDistance = Vector3.Distance(startPos, endPos);
        float maxLength = totalLength * maxStretchRatio;
        if(endDistance > maxLength){
            // Pull endpoints toward each other proportionally
            Vector3 midPoint = (startPos + endPos) * 0.5f;
            Vector3 dirA = (midPoint - startPos).normalized;
            Vector3 dirB = (midPoint - endPos).normalized;
            float halfMax = maxLength * 0.5f;

            if(!startPoint){  // only move if endpoint is not physics‑driven
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

        // --- Verlet integration ---
        for(int i = 1; i < points.Count - 1; i++){
            VerletPoint point = points[i];
            Vector3 velocity = (point.position - point.previousPosition) * damping;
            Vector3 newPosition = point.position + velocity;
            newPosition.y += gravity * deltaTime * deltaTime;

            point.previousPosition = point.position;
            point.position = newPosition;
            points[i] = point;
        }

        // --- Distance constraints ---
        for(int iter = 0; iter < constraintIterations; iter++){
            for(int i = 0; i < points.Count - 1; i++){
                VerletPoint a = points[i];
                VerletPoint b = points[i + 1];

                Vector3 dir = b.position - a.position;
                float dist = dir.magnitude;
                if(dist < 0.0001f) continue;

                Vector3 correction = dir * ((dist - segmentLength) / dist) * 0.5f;

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

        // --- Collision ---
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

    public float GetTotalLength() => totalLength;
    public bool IsInitialized() => initialized;
    public void SetVisible(bool visible){
        ropeVisible = visible;
        if(lineRenderer) lineRenderer.enabled = visible;
        enabled = visible;
    }

#if UNITY_EDITOR
    void OnValidate(){
        if(segments < 2) segments = 2;
        if(segmentLength < 0.01f) segmentLength = 0.01f;
        if(constraintIterations < 1) constraintIterations = 1;
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
    }
#endif
}