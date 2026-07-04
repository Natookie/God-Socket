using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovementController : MonoBehaviour
{
    [Header("SPEED")]
    public float normalMaxSpeed = 10f;
    public float boostSpeedMultiplier = 2f;
    public float aimMovementMultiplier = 0.6f;

    [Header("ACCELERATION")]
    public float forwardAcceleration = 25f;
    public float strafeAcceleration = 15f;
    public float verticalAcceleration = 12f;
    public float brakingAcceleration = 35f;
    public float boostAccelerationMultiplier = 2.5f;

    [Header("MOVEMENT FEEL")]
    public float idleDamping = 2f;
    public float drag = 0.2f;

    [Header("REFERENCES")]
    public Transform cameraTransform;
    public InputHandler input;
    public EnergySystem energy;

    private Rigidbody rb;

    private bool isOverheated;
    private bool boostActive;
    private bool inputDisabled;
    private bool wasAiming;
    private bool isRiding = false;
    private MissileController ridingMissile;
    private Vector3 lastWorldMoveDirection;

    public Vector3 WorldMoveDirection { get; private set; }

    public void SetOverheated(bool overheated){
        isOverheated = overheated;
        if(overheated) rb.linearVelocity = Vector3.zero;
    }

    public void SetBoostActive(bool active){
        boostActive = active;
        if(!active && wasAiming && input != null && input.AimHeld){
            wasAiming = false;
        }
    }

    public void SetInputDisabled(bool disabled) => inputDisabled = disabled;
    public float GetCurrentSpeed() => rb.linearVelocity.magnitude;
    public bool IsRiding() => isRiding;
    public bool IsOverheated() => isOverheated;

    public void SetRiding(bool riding){
        isRiding = riding;
        if(riding){
            inputDisabled = true;
            rb.isKinematic = true;
        }
        else{
            inputDisabled = false;
            rb.isKinematic = false;
        }
    }

    public void SetRidingMissile(MissileController missile){
        ridingMissile = missile;
    }

    public Vector3 GetWorldMoveDirection(){
        if(isRiding && ridingMissile != null){
            Vector3 moveInput = input.MoveDirection;
            if(moveInput.sqrMagnitude > 0.01f){
                Vector3 camForward = cameraTransform.forward;
                Vector3 camRight = cameraTransform.right;
                
                Vector3 forward = camForward.normalized;
                Vector3 right = Vector3.ProjectOnPlane(camRight, Vector3.up).normalized;
                Vector3 up = Vector3.up;
                
                lastWorldMoveDirection = forward * moveInput.z + right * moveInput.x + up * moveInput.y;
                return lastWorldMoveDirection.normalized;
            }
            return lastWorldMoveDirection;
        }
        return WorldMoveDirection;
    }

    public void DismountMissile(){
        if(isRiding && ridingMissile != null){
            ridingMissile.ManualDismount();
        }
    }

    void Awake(){
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.linearDamping = drag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void FixedUpdate(){
        WorldMoveDirection = Vector3.zero;

        if(isOverheated || inputDisabled) 
        {
        UpdateFlyingWindSound();
        return;
        }

        HandleMovement();
        UpdateFlyingWindSound();

        bool isAiming = input != null && input.AimHeld;

        if(isAiming != wasAiming){
            wasAiming = isAiming;
            if(!isAiming) boostActive = false;
        }

        if(boostActive && !isOverheated && !isAiming){
            float currentMaxSpeed = boostActive ? normalMaxSpeed * boostSpeedMultiplier : normalMaxSpeed;
            if(isAiming) currentMaxSpeed *= aimMovementMultiplier;
            
            float speedRatio = rb.linearVelocity.magnitude / currentMaxSpeed;
            float boostThreshold = boostActive ? 0.4f : 0.5f;
            
            if(speedRatio > boostThreshold) energy.DrainBoost(Time.fixedDeltaTime);
        }
    }

    void HandleMovement(){
        if(isRiding) return;
        
        Vector3 moveInput = input.MoveDirection;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        Vector3 forward = camForward.normalized;
        Vector3 right = Vector3.ProjectOnPlane(camRight, Vector3.up).normalized;
        Vector3 up = Vector3.up;

        float currentForwardAccel = forwardAcceleration;
        float currentStrafeAccel = strafeAcceleration;
        float currentVerticalAccel = verticalAcceleration;

        if(boostActive){
            currentForwardAccel *= boostAccelerationMultiplier;
            currentStrafeAccel *= boostAccelerationMultiplier;
            currentVerticalAccel *= boostAccelerationMultiplier;
        }

        Vector3 desiredAcceleration = forward * moveInput.z * currentForwardAccel + 
                                       right * moveInput.x * currentStrafeAccel + 
                                       up * moveInput.y * currentVerticalAccel;

        WorldMoveDirection = desiredAcceleration.normalized;

        float currentMaxSpeed = boostActive ? normalMaxSpeed * boostSpeedMultiplier : normalMaxSpeed;

        if(input.AimHeld) currentMaxSpeed *= aimMovementMultiplier;

        if(moveInput.sqrMagnitude > 0.01f) rb.AddForce(desiredAcceleration, ForceMode.Acceleration);
        else rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, Vector3.zero, idleDamping * Time.fixedDeltaTime);

        Vector3 velocity = rb.linearVelocity;

        if(moveInput.sqrMagnitude > 0.01f && velocity.sqrMagnitude > 0.01f){
            Vector3 desiredDirection = desiredAcceleration.normalized;
            float alignment = Vector3.Dot(velocity.normalized, desiredDirection);
            if(alignment < -0.3f) rb.linearVelocity = Vector3.MoveTowards(velocity, Vector3.zero, brakingAcceleration * Time.fixedDeltaTime);
        }

        if(rb.linearVelocity.magnitude > currentMaxSpeed) rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
    }

    void UpdateFlyingWindSound()
    {
        if (AudioManager.Instance == null) return;

        bool isAiming = CameraController.Instance != null && CameraController.Instance.aiming;

        bool shouldPlayWind =
            boostActive &&
            !isOverheated &&
            !inputDisabled &&
            !isAiming &&
            rb.linearVelocity.magnitude > 0.5f;

        AudioManager.Instance.SetFlyingWind(shouldPlayWind);
    }
}