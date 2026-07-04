using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("CAMERAS")]
    public CinemachineCamera normalCamera;
    public CinemachineCamera aimCamera;
    public CinemachineBasicMultiChannelPerlin normalCameraNoise;
    public CinemachineBasicMultiChannelPerlin aimCameraNoise;

    [Header("REFERENCES")]
    public InputHandler input;
    public PlayerRotation playerRotation;
    public MovementController movement;
    public CameraAutoAim autoAim;

    [Header("PRIORITIES")]
    public int activePriority = 20;
    public int inactivePriority = 10;

    [Header("DUTCH")]
    public float maxDutch = 5f;
    public float yawRateForMaxDutch = 360f;
    public float dutchSmoothSpeed = 8f;

    [Header("CAMERA OFFSET")]
    public Transform cameraTransform;
    public float cameraHeightOffset = 3f;

    [Header("SHAKE")]
    public NoiseSettings shakeSettings;

    [Header("SPEED ZOOM")]
    public float speedZoomMultiplier = 0.3f;
    public float speedZoomSmoothSpeed = 4f;
    public float minFOV = 60f;
    public float maxFOV = 90f;
    public float baseFOV = 70f;
    public float maxSpeedForZoom = 30f;

    public enum ShakePriority { Low, Medium, High, Critical }

    private float currentDutch;
    private float currentFOV;
    private float targetFOV;

    private Coroutine shakeCoroutine;
    private ShakeInstance currentShake;

    class ShakeInstance{
        public float targetAmplitude;
        public float duration;
        public ShakePriority priority;
        
        public ShakeInstance(float amplitude, float dur, ShakePriority p){
            targetAmplitude = amplitude;
            duration = dur;
            priority = p;
        }
    }

    void Awake(){
        if(Instance != null){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start(){
        if(cameraTransform == null){
            Camera mainCam = Camera.main;
            if(mainCam != null) cameraTransform = mainCam.transform;
        }
        
        currentFOV = baseFOV;
        targetFOV = baseFOV;

        if(normalCamera != null) normalCamera.Lens.FieldOfView = baseFOV;
        if(aimCamera != null) aimCamera.Lens.FieldOfView = baseFOV;

        if(normalCameraNoise != null){
            normalCameraNoise.AmplitudeGain = 0f;
            normalCameraNoise.FrequencyGain = 0f;
        }

        if(aimCameraNoise != null){
            aimCameraNoise.AmplitudeGain = 0f;
            aimCameraNoise.FrequencyGain = 0f;
        }

        if(shakeSettings == null){
            shakeSettings = Resources.Load<NoiseSettings>("4. Data/Shake_Setting");
            if(shakeSettings == null) Debug.LogError("Couldn't load Shake_Setting!");
        }

        if(autoAim == null) autoAim = GetComponent<CameraAutoAim>();
        if(autoAim != null) autoAim.OnAutoAimStateChanged += OnAutoAimStateChanged;
    }

    void OnDestroy(){
        if(autoAim != null) autoAim.OnAutoAimStateChanged -= OnAutoAimStateChanged;
    }

    public bool aiming => (input.AimHeld && !movement.IsOverheated() && !movement.IsRiding());

    void Update(){
        if(!normalCamera || !aimCamera || !input) return;

        UpdateCameraPriority(aiming);
        UpdateDutch(aiming);
        UpdateSpeedZoom();
    }

    void OnAutoAimStateChanged(bool isAutoAiming){
        CrosshairUI.Instance?.ChangeColor(!isAutoAiming);
    }

    #region POLISH
    void UpdateCameraPriority(bool aiming){
        if(aiming){
            aimCamera.Priority = activePriority;
            normalCamera.Priority = inactivePriority;
        }
        else{
            normalCamera.Priority = activePriority;
            aimCamera.Priority = inactivePriority;
        }
    }

    void UpdateDutch(bool aiming){
        float targetDutch = 0f;

        if(!aiming && playerRotation){
            float normalizedYawRate = Mathf.Clamp(
                playerRotation.CurrentYawRate / yawRateForMaxDutch,
                -1f,
                1f);
            targetDutch = -normalizedYawRate * maxDutch;
        }

        currentDutch = Mathf.Lerp(
            currentDutch,
            targetDutch,
            Time.deltaTime * dutchSmoothSpeed);

        normalCamera.Lens.Dutch = currentDutch;
        aimCamera.Lens.Dutch = 0f;
    }

    public void ShakeCamera(float targetAmplitude, float duration, ShakePriority priority = ShakePriority.Medium){
        if(currentShake != null && priority < currentShake.priority) return;
        
        if(shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        
        currentShake = new ShakeInstance(targetAmplitude, duration, priority);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(currentShake));
    }

    IEnumerator ShakeCoroutine(ShakeInstance shakeData){
        CinemachineBasicMultiChannelPerlin activeNoise = GetActiveNoise();
        
        if(activeNoise == null || shakeSettings == null){
            currentShake = null;
            shakeCoroutine = null;
            yield break;
        }

        activeNoise.NoiseProfile = shakeSettings;
        activeNoise.AmplitudeGain = 0f;
        activeNoise.FrequencyGain = 0f;

        float elapsed = 0f;

        while(elapsed < shakeData.duration){
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeData.duration;
            
            float amplitude = Mathf.Lerp(0f, shakeData.targetAmplitude, progress);
            
            activeNoise.AmplitudeGain = amplitude;
            activeNoise.FrequencyGain = amplitude;
            
            yield return null;
        }

        activeNoise.AmplitudeGain = shakeData.targetAmplitude;
        activeNoise.FrequencyGain = shakeData.targetAmplitude;

        float fadeDuration = 0.2f;
        float fadeElapsed = 0f;

        while(fadeElapsed < fadeDuration){
            fadeElapsed += Time.deltaTime;
            float progress = fadeElapsed / fadeDuration;
            
            float amplitude = Mathf.Lerp(shakeData.targetAmplitude, 0f, progress);
            
            activeNoise.AmplitudeGain = amplitude;
            activeNoise.FrequencyGain = amplitude;
            
            yield return null;
        }

        activeNoise.AmplitudeGain = 0f;
        activeNoise.FrequencyGain = 0f;
        activeNoise.NoiseProfile = null;

        currentShake = null;
        shakeCoroutine = null;
    }

    CinemachineBasicMultiChannelPerlin GetActiveNoise(){
        if(aiming) return aimCameraNoise;
        return normalCameraNoise;
    }

    void UpdateSpeedZoom(){
        if(movement == null) return;

        float currentSpeed = movement.GetCurrentSpeed();
        float speedRatio = Mathf.Clamp01(currentSpeed / maxSpeedForZoom);
        
        targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedRatio * speedZoomMultiplier);
        targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * speedZoomSmoothSpeed);
        
        if(normalCamera != null) normalCamera.Lens.FieldOfView = currentFOV;
        if(aimCamera != null) aimCamera.Lens.FieldOfView = currentFOV;
    }
    #endregion

    #region AUTO AIM DELEGATION
    public void SetAutoAimEnabled(bool enabled){
        if(autoAim != null) autoAim.SetAutoAimEnabled(enabled);
    }

    public Transform GetCurrentTarget(){
        if(autoAim != null) return autoAim.GetCurrentTarget();
        return null;
    }

    public bool HasTarget(){
        if(autoAim != null) return autoAim.HasTarget();
        return false;
    }

    public bool IsAutoAiming(){
        if(autoAim != null) return autoAim.IsAutoAiming();
        return false;
    }
    #endregion
}