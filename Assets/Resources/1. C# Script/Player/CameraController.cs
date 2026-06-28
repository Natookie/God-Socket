using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("CAMERAS")]
    public CinemachineCamera normalCamera;
    public CinemachineCamera aimCamera;

    [Header("REFERENCES")]
    public InputHandler input;
    public PlayerRotation playerRotation;

    [Header("PRIORITIES")]
    public int activePriority = 20;
    public int inactivePriority = 10;

    [Header("DUTCH")]
    public float maxDutch = 5f;
    public float yawRateForMaxDutch = 360f;
    public float dutchSmoothSpeed = 8f;

    private float currentDutch;

    void Update(){
        if(!normalCamera || !aimCamera || !input)return;

        bool aiming = input.AimHeld;

        if(aiming){
            aimCamera.Priority = activePriority;
            normalCamera.Priority = inactivePriority;
        }
        else
        {
            normalCamera.Priority = activePriority;
            aimCamera.Priority = inactivePriority;
        }

        float targetDutch = 0f;

        if(!aiming && playerRotation){
            float normalizedYawRate =
                Mathf.Clamp(
                    playerRotation.CurrentYawRate /
                    yawRateForMaxDutch,
                    -1f,
                    1f);

            targetDutch =
                -normalizedYawRate *
                maxDutch;
        }

        currentDutch = Mathf.Lerp(
            currentDutch,
            targetDutch,
            Time.deltaTime * dutchSmoothSpeed);

        normalCamera.Lens.Dutch = currentDutch;
        aimCamera.Lens.Dutch = 0f;
    }
}