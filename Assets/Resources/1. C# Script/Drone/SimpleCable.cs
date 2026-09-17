using UnityEngine;

public class SimpleCable : MonoBehaviour
{
    [Header("CABLE SETTINGS")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int segments = 15;
    [SerializeField] private float sagAmount = 0.5f;
    [SerializeField] private AnimationCurve sagCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    private Vector3[] positions;
    private bool isVisible;

    void Awake()
    {
        positions = new Vector3[segments + 1];
        
        if(lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if(lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.05f;
                lineRenderer.endWidth = 0.05f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = Color.cyan;
                lineRenderer.endColor = Color.cyan;
            }
        }
        
        lineRenderer.positionCount = positions.Length;
        lineRenderer.enabled = false;
        isVisible = false;
    }

    void Update()
    {
        if(!isVisible || startPoint == null || endPoint == null || lineRenderer == null) return;

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;
        
        float distance = Vector3.Distance(start, end);
        Vector3 midPoint = (start + end) * 0.5f;
        midPoint.y -= sagAmount * distance * 0.1f;

        for(int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p1 = Vector3.Lerp(start, midPoint, t);
            Vector3 p2 = Vector3.Lerp(midPoint, end, t);
            Vector3 point = Vector3.Lerp(p1, p2, t);
            point.y += sagCurve.Evaluate(t) * sagAmount;
            positions[i] = point;
        }

        lineRenderer.SetPositions(positions);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if(lineRenderer != null)
        {
            lineRenderer.enabled = visible;
            if(visible)
            {
                ForceUpdate();
            }
        }
        enabled = visible;
    }

    public void ForceUpdate()
    {
        if(!isVisible || startPoint == null || endPoint == null || lineRenderer == null) return;

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;
        
        float distance = Vector3.Distance(start, end);
        Vector3 midPoint = (start + end) * 0.5f;
        midPoint.y -= sagAmount * distance * 0.1f;

        for(int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p1 = Vector3.Lerp(start, midPoint, t);
            Vector3 p2 = Vector3.Lerp(midPoint, end, t);
            Vector3 point = Vector3.Lerp(p1, p2, t);
            point.y += sagCurve.Evaluate(t) * sagAmount;
            positions[i] = point;
        }

        lineRenderer.SetPositions(positions);
    }

    public void SetEndpoints(Transform start, Transform end)
    {
        startPoint = start;
        endPoint = end;
        if(isVisible)
        {
            ForceUpdate();
        }
    }

    public void SetStartPoint(Transform start)
    {
        startPoint = start;
        if(isVisible)
        {
            ForceUpdate();
        }
    }

    public void SetEndPoint(Transform end)
    {
        endPoint = end;
        if(isVisible)
        {
            ForceUpdate();
        }
    }

    public void SetColor(Color color)
    {
        if(lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }

    public void SetWidth(float width)
    {
        if(lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }

    public void SetSegments(int count)
    {
        if(count < 2) count = 2;
        segments = count;
        positions = new Vector3[segments + 1];
        if(lineRenderer != null)
        {
            lineRenderer.positionCount = positions.Length;
            if(isVisible) ForceUpdate();
        }
    }

    public void SetSag(float sag)
    {
        sagAmount = sag;
        if(isVisible) ForceUpdate();
    }

    public bool IsVisible() => isVisible;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if(!isVisible || startPoint == null || endPoint == null) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPoint.position, 0.1f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(endPoint.position, 0.1f);
        
        Gizmos.color = Color.yellow;
        for(int i = 0; i < positions.Length - 1; i++)
        {
            Gizmos.DrawLine(positions[i], positions[i + 1]);
        }
    }
#endif
}