using UnityEngine;

public interface IGravityAffected {
    void OnGravityFieldEnter(float force);
    void OnGravityFieldExit();
    void OnGravityFieldUpdate(Vector3 force);
}