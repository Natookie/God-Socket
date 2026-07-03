using UnityEngine;

public interface ITargetable {
    Vector3 GetPosition();
    bool IsTargetable();
    float GetTargetPriority();
}