using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRotation : MonoBehaviour
{
    [Header("ROTATION")]
    public float yawSpeed = 180f;
    public float pitchSpeed = 120f;

    [Header("LIMITS")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("REFERENCES")]
    public InputHandler input;

    private Rigidbody rb;

    private float yaw;
    private float pitch;

    private bool inputDisabled;
    public float CurrentYawRate { get; private set; }
    private float previousYaw;

    void Awake(){
        rb = GetComponent<Rigidbody>();
        Vector3 angles = transform.rotation.eulerAngles;
        yaw = angles.y;

        pitch = angles.x;
        if(pitch > 180f) pitch -= 360f;

        previousYaw = yaw;
    }
    public void SetInputDisabled(bool disabled) => inputDisabled = disabled;

    void FixedUpdate(){
        if(inputDisabled || input.CursorVisible) return;
        UpdateRotation();
    }

    void UpdateRotation(){
        yaw += input.MouseX * yawSpeed * Time.fixedDeltaTime;

        pitch -= input.MouseY * pitchSpeed * Time.fixedDeltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float deltaYaw = Mathf.DeltaAngle(previousYaw, yaw);
        CurrentYawRate = deltaYaw / Time.fixedDeltaTime;
        previousYaw = yaw;
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);

        rb.MoveRotation(targetRotation);
    }
}