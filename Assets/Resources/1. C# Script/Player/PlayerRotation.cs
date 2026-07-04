using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRotation : MonoBehaviour
{
    [Header("ROTATION")]
    public float yawSpeed = 180f;
    public float pitchSpeed = 120f;
    public float autoAimSpeed = 180f;
    public float autoAimTransitionSpeed = 360f;

    [Header("SENSITIVITY")]
    public float defaultSensitivity = 2f;
    private float mouseSensitivity;
    private float sensitivityMultiplier = 1f;

    [Header("LIMITS")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("REFERENCES")]
    public InputHandler input;

    private Rigidbody rb;
    private float yaw;
    private float pitch;
    private bool inputDisabled;
    private bool autoAimActive;
    private Quaternion autoAimTarget;
    private float autoAimStartTime;
    private Quaternion autoAimStartRotation;
    private bool isAutoAimTransitioning;

    public float CurrentYawRate { get; private set; }
    private float previousYaw;

    void Awake(){
        rb = GetComponent<Rigidbody>();

        Vector3 angles = transform.rotation.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        if(pitch > 180f) pitch -= 360f;

        previousYaw = yaw;
        autoAimActive = false;
        autoAimTarget = Quaternion.identity;
        isAutoAimTransitioning = false;
    }

    void Start(){
        LoadSensitivity();
    }

    public void LoadSensitivity(){
        sensitivityMultiplier = GameSettings.Sensitivity;
        // mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
        // sensitivityMultiplier = mouseSensitivity / defaultSensitivity;
        // Debug.Log("Loaded Mouse Sensitivity: " + mouseSensitivity);
        // Debug.Log("Sensitivity Multiplier: " + sensitivityMultiplier);
    }

    public void SetInputDisabled(bool disabled){
        inputDisabled = disabled;
    }

    public void SetAutoAimTarget(Quaternion target){
        if(!autoAimActive || Quaternion.Angle(autoAimTarget, target) > 5f){
            autoAimStartRotation = transform.rotation;
            autoAimStartTime = Time.time;
            isAutoAimTransitioning = true;
        }

        autoAimTarget = target;
        autoAimActive = true;
    }

    public void ClearAutoAim(){
        autoAimActive = false;
        autoAimTarget = Quaternion.identity;
        isAutoAimTransitioning = false;
    }

    public bool IsAutoAiming() => autoAimActive;

    void FixedUpdate(){
        if(input == null) return;
        if(input.CursorVisible) return;

        if(autoAimActive){
            UpdateAutoAim();
            return;
        }

        if(!inputDisabled) UpdateRotation();
    }

    void UpdateAutoAim(){
        Vector3 currentEuler = transform.rotation.eulerAngles;
        float currentYaw = currentEuler.y;
        float currentPitch = currentEuler.x;

        if(currentPitch > 180f) currentPitch -= 360f;

        Vector3 targetEuler = autoAimTarget.eulerAngles;
        float targetYaw = targetEuler.y;
        float targetPitch = targetEuler.x;

        if(targetPitch > 180f) targetPitch -= 360f;

        float speed;

        if(isAutoAimTransitioning){
            float transitionProgress = (Time.time - autoAimStartTime) / 0.3f;
            transitionProgress = Mathf.Clamp01(transitionProgress);

            float easedProgress = 1f - Mathf.Pow(1f - transitionProgress, 3f);

            speed = Mathf.Lerp(autoAimTransitionSpeed, autoAimSpeed, easedProgress);
            if(transitionProgress >= 1f) isAutoAimTransitioning = false;
        }
        else speed = autoAimSpeed;

        float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.fixedDeltaTime * speed);
        float newPitch = Mathf.Lerp(currentPitch, targetPitch, Time.fixedDeltaTime * speed * 0.5f);
        newPitch = Mathf.Clamp(newPitch, minPitch, maxPitch);

        Quaternion targetRotation = Quaternion.Euler(newPitch, newYaw, 0f);
        rb.MoveRotation(targetRotation);

        float deltaYaw = Mathf.DeltaAngle(previousYaw, newYaw);
        CurrentYawRate = deltaYaw / Time.fixedDeltaTime;
        previousYaw = newYaw;

        yaw = newYaw;
        pitch = newPitch;
    }

    void UpdateRotation(){
        yaw += input.MouseX * yawSpeed * sensitivityMultiplier * Time.fixedDeltaTime;
        pitch -= input.MouseY * pitchSpeed * sensitivityMultiplier * Time.fixedDeltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float deltaYaw = Mathf.DeltaAngle(previousYaw, yaw);
        CurrentYawRate = deltaYaw / Time.fixedDeltaTime;
        previousYaw = yaw;

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        rb.MoveRotation(targetRotation);
    }
}