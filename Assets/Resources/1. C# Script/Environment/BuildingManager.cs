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

    [Header("EXPLOSION EFFECT")]
    public ParticleSystem explosionParticlePrefab;

    [Header("GUI")]
    public bool showGUI = false;

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

    void OnGUI(){
        if(!showGUI) return;

        if(buildingStates == null || buildingStates.Count == 0){
            GUI.Box(new Rect(10, 10, 300, 50), "Building States");
            GUI.Label(new Rect(20, 40, 250, 20), "No buildings in dictionary");
            return;
        }

        int height = 30 + (buildingStates.Count * 25);
        GUI.Box(new Rect(10, 10, 350, height), "Building States");
        
        int yOffset = 40;
        foreach(var kvp in buildingStates){
            string stateText = kvp.Value.ToString();
            Color originalColor = GUI.color;
            
            switch(kvp.Value){
                case BuildingState.Available:
                    GUI.color = Color.green;
                    break;
                case BuildingState.Targeted:
                    GUI.color = Color.yellow;
                    break;
                case BuildingState.Destroyed:
                    GUI.color = Color.red;
                    break;
            }
            
            string displayName = kvp.Key != null ? kvp.Key.name : "NULL";
            GUI.Label(new Rect(20, yOffset, 300, 20), $"{displayName}: {stateText}");
            
            GUI.color = originalColor;
            yOffset += 25;
        }
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
                b.SetupBuilding(null);
                
                if(explosionParticlePrefab != null) b.explosionParticlePrefab = explosionParticlePrefab;
            }
        }
    }

    public void AddBuilding(Building b){
        if(b == null || buildingStates.ContainsKey(b)) return;
        buildings.Add(b);
        buildingStates[b] = BuildingState.Available;
        b.SetupBuilding(null);
        
        if(explosionParticlePrefab != null) b.explosionParticlePrefab = explosionParticlePrefab;
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

    public Dictionary<Building, BuildingState> GetBuildingStates(){
        return buildingStates;
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