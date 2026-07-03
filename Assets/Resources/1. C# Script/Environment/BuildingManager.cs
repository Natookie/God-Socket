using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("BUILDING LIST")]
    public List<Building> buildings = new List<Building>();
    private Dictionary<Building, BuildingState> buildingStates = new Dictionary<Building, BuildingState>();

    [Header("AUTO POPULATE")]
    public bool autoPopulateOnStart = true;

    void Awake(){
        if(Instance != null){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start(){
        if(autoPopulateOnStart) PopulateList();
    }

    [Button("Populate Building List", enabledMode: EButtonEnableMode.Editor)]
    void PopulateList(){
        buildings.Clear();
        buildingStates.Clear();
        
        Building[] found = FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach(Building b in found){
            if(b != null){
                buildings.Add(b);
                buildingStates[b] = BuildingState.Available;
            }
        }
    }

    public void AddBuilding(Building b){
        if(b == null || buildingStates.ContainsKey(b)) return;
        buildings.Add(b);
        buildingStates[b] = BuildingState.Available;
    }

    public void RemoveBuilding(Building b){
        if(b == null || !buildingStates.ContainsKey(b)) return;
        buildings.Remove(b);
        buildingStates.Remove(b);
    }

    public void SetTargeted(Building b, bool targeted){
        if(b == null || !buildingStates.ContainsKey(b)) return;
        if(buildingStates[b] == BuildingState.Destroyed) return;
        
        buildingStates[b] = targeted ? BuildingState.Targeted : BuildingState.Available;
        b.isTargeted = targeted;
    }

    public void SetDestroyed(Building b){
        if(b == null || !buildingStates.ContainsKey(b)) return;
        buildingStates[b] = BuildingState.Destroyed;
        b.isTargeted = false;
    }

    public Building GetUntargeted(){
        foreach(var kvp in buildingStates){
            if(kvp.Value == BuildingState.Available) return kvp.Key;
        }
        return null;
    }

    public bool HasUntargeted(){
        foreach(var kvp in buildingStates){
            if(kvp.Value == BuildingState.Available) return true;
        }
        return false;
    }

    public BuildingState GetState(Building b){
        if(b == null || !buildingStates.ContainsKey(b)) return BuildingState.Destroyed;
        return buildingStates[b];
    }

    public void Clear(){
        buildings.Clear();
        buildingStates.Clear();
    }
}

public enum BuildingState
{
    Available,
    Targeted,
    Destroyed
}