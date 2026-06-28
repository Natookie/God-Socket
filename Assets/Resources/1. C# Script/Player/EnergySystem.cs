using UnityEngine;
using System;

public class EnergySystem : MonoBehaviour
{
    [Header("SETTINGS")]
    public float maxEnergy = 100f;
    public float boostDrainPerSecond = 15f;
    public float shotCost = 8f;
    public float overheatCooldown = 3f;

    [Header("CURRENT STATE")]
    [SerializeField] private float currentEnergy;

    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;
    public bool IsDepleted => currentEnergy <= 0f;

    public event Action<float, float> OnEnergyChanged;
    public event Action OnEnergyDepleted;
    public event Action OnEnergyRestored;

    void Awake(){
        currentEnergy = maxEnergy;
    }

    public bool CanAfford(float amount) => currentEnergy >= amount;
    public bool Consume(float amount){
        if(amount <= 0f) return true;
        if(currentEnergy < amount) return false;

        currentEnergy -= amount;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        if(IsDepleted) OnEnergyDepleted?.Invoke();

        return true;
    }

    public void Restore(float amount){
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    public void FullRestore(){
        currentEnergy = maxEnergy;
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        OnEnergyRestored?.Invoke();
    }

    public void DrainBoost(float deltaTime) => Consume(boostDrainPerSecond * deltaTime);
}