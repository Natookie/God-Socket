using UnityEngine;

public interface IHealth {
    float GetHealth();
    float GetMaxHealth();
    void SetHealth(float health);
    void OnDeath();
}