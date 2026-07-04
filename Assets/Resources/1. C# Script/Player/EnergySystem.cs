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
    [SerializeField] private bool infiniteEnergy;

    [Header("REFERENCES")]
    [SerializeField] private EnergyUI energyUI;

    public float MaxEnergy => maxEnergy;
    public bool IsDepleted => currentEnergy <= 0f;
    public float Ratio => Mathf.Clamp01(currentEnergy / maxEnergy);

    public event Action<float, float> OnEnergyChanged;
    public event Action OnEnergyDepleted;
    public event Action OnEnergyRestored;

    void Awake(){
        currentEnergy = maxEnergy;
    }

    void UpdateRatio(){
        float newRatio = Mathf.Clamp01(currentEnergy / maxEnergy);
        if(energyUI != null) energyUI.UpdateEnergyUI(newRatio);
    }

    public bool CanAfford(float amount){
        if(infiniteEnergy) return true;
        return currentEnergy >= amount;
    }

    public bool Consume(float amount){
        if(amount <= 0f) return true;

        currentEnergy -= amount;
        currentEnergy = Mathf.Max(0, currentEnergy);
        UpdateRatio();
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        if(IsDepleted) OnEnergyDepleted?.Invoke();

        return true;
    }

    public void Restore(float amount){
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        UpdateRatio();
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    public void FullRestore(){
        currentEnergy = maxEnergy;
        UpdateRatio();
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        OnEnergyRestored?.Invoke();
    }

    public void DrainBoost(float deltaTime) => Consume(boostDrainPerSecond * deltaTime);
    public float GetCurrentEnergy() => currentEnergy;
}