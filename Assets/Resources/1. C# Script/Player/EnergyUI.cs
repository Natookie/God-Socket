using UnityEngine;
using Nova;

public class EnergyUI : MonoBehaviour
{
    [SerializeField] private TextBlock playerEnergyText;
    [SerializeField] private TextBlock droneEnergyText;
    [Space(10)]
    [SerializeField] private DroneLogic drone;
    [SerializeField] private EnergySystem player;

    private float playerEnergy;
    private float droneEnergy;
    private string playerTextCache;
    private string droneTextCache;
    private float updateTimer;
    private const float UPDATE_INTERVAL = 0.1f;

    void Start(){
        UpdateTexts();
    }

    void Update(){
        updateTimer += Time.deltaTime;
        if(updateTimer >= UPDATE_INTERVAL){
            updateTimer = 0f;
            UpdateTexts();
        }
    }

    void UpdateTexts(){
        float newPlayerEnergy = player != null ? player.GetCurrentEnergy() : 0f;
        float newDroneEnergy = drone != null ? drone.GetDroneEnergy() : 0f;

        if(!Mathf.Approximately(playerEnergy, newPlayerEnergy)){
            playerEnergy = newPlayerEnergy;
            string newText = $"Player Energy: {playerEnergy:F1}";
            if(playerTextCache != newText){
                playerTextCache = newText;
                if(playerEnergyText != null) playerEnergyText.Text = playerTextCache;
            }
        }

        if(!Mathf.Approximately(droneEnergy, newDroneEnergy)){
            droneEnergy = newDroneEnergy;
            string newText = $"Drone Energy : {droneEnergy:F1}";
            if(droneTextCache != newText){
                droneTextCache = newText;
                if(droneEnergyText != null) droneEnergyText.Text = droneTextCache;
            }
        }
    }
}