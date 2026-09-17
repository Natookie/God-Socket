using UnityEngine;
using Nova;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EnergyUI : MonoBehaviour
{
    [SerializeField] private TextBlock playerEnergyText;
    [Space(10)]
    [SerializeField] private UIBlock2D playerEnergyBlock;
    [SerializeField] private UIBlock2D droneEnergyBlock;
    [Space(10)]
    [SerializeField] private DroneLogic drone;
    [SerializeField] private EnergySystem player;

    [Header("SHUTDOWN")]
    [SerializeField] private UIBlock2D shutdownVignette;
    [SerializeField] private Volume globalVolume;
    private ColorAdjustments colorAdjustments;

    private float playerEnergy;
    private float droneEnergy;
    private string playerTextCache;
    private string droneTextCache;
    private float updateTimer;
    private const float UPDATE_INTERVAL = 0.1f;

    private const float SATURATION = -10f;
    private const float TARGET_SATURATION = -100f;

    private const float OPACITY = 0f;
    private const float TARGET_OPACITY = 25f;

    void Start(){
        UpdateTexts();
        UpdateBlock();
    }

    void Update(){
        updateTimer += Time.deltaTime;
        if(updateTimer >= UPDATE_INTERVAL){
            updateTimer = 0f;
            UpdateTexts();
            UpdateBlock();
        }
    }

    public void UpdateEnergyUI(float ratio){
        float threshold = .4f;
        float effectRatio = 0f;
        
        if(ratio < threshold){
            effectRatio = 1f - (ratio / threshold);
            effectRatio = Mathf.Clamp01(effectRatio);
            effectRatio = effectRatio * effectRatio * (3f - 2f * effectRatio);
        }
        
        if(globalVolume != null && globalVolume.profile.TryGet(out colorAdjustments)){
            colorAdjustments.saturation.value = Mathf.Lerp(SATURATION, TARGET_SATURATION, effectRatio);
        }
        
        if(shutdownVignette != null){
            float opacity = Mathf.Lerp(OPACITY, TARGET_OPACITY, effectRatio);
            Color color = shutdownVignette.Color;
            color.a = opacity/100f;
            shutdownVignette.Color = color;
        }
    }

    void UpdateTexts(){
        if(player != null){
            float newPlayerEnergy = player.GetCurrentEnergy();
            if(!Mathf.Approximately(playerEnergy, newPlayerEnergy)){
                playerEnergy = newPlayerEnergy;
                string newText = $"Player Energy: {playerEnergy:F1}";
                if(playerTextCache != newText){
                    playerTextCache = newText;
                    if(playerEnergyText != null) playerEnergyText.Text = playerTextCache;
                }
            }
        }

        if(drone != null){
            float newDroneEnergy = drone.GetCurrentEnergy();
            if(!Mathf.Approximately(droneEnergy, newDroneEnergy)){
                droneEnergy = newDroneEnergy;
                string newText = $"Drone Energy: {droneEnergy:F1}";
                if(droneTextCache != newText){
                    droneTextCache = newText;
                }
            }
        }
    }

    void UpdateBlock(){
        if(playerEnergyBlock != null && player != null){
            float playerRatio = player.GetCurrentEnergy() / 100f;
            playerEnergyBlock.Size.X.Percent = Mathf.Clamp01(playerRatio);
        }

        if(droneEnergyBlock != null && drone != null){
            float droneRatio = drone.GetCurrentEnergy() / 200f;
            droneEnergyBlock.Size.X.Percent = Mathf.Clamp01(droneRatio);
        }
    }
}