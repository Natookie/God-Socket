using UnityEngine;
using NaughtyAttributes;

public class InputHandler : MonoBehaviour
{
    [Header("MOVEMENT")]
    public KeyCode forward = KeyCode.W;
    public KeyCode backward = KeyCode.S;
    public KeyCode left = KeyCode.A;
    public KeyCode right = KeyCode.D;
    public KeyCode up = KeyCode.E;
    public KeyCode down = KeyCode.Q;

    [Header("ACTIONS")]
    public KeyCode boost = KeyCode.Space;
    public KeyCode resupplyToggleKey = KeyCode.Tab;
    public int aimMouseButton = 1;
    public int fireMouseButton = 0;

    [Header("CURSOR")]
    public KeyCode cursorToggleKey = KeyCode.LeftAlt;

    public Vector3 MoveDirection { get; private set; }
    public bool BoostHeld { get; private set; }
    public bool AimHeld { get; private set; }
    public bool FireHeld { get; private set; }
    public bool FirePressed { get; private set; }

    public float MouseX { get; private set; }
    public float MouseY { get; private set; }
    public bool CursorVisible { get; private set; }
    public bool ResupplyPressed { get; private set; }

    void Start(){
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        CursorVisible = false;
    }

    void Update(){
        if(Input.GetKeyDown(cursorToggleKey)){
            CursorVisible = !CursorVisible;
            Cursor.lockState = CursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = CursorVisible;
        }
        if(Input.GetKeyDown(resupplyToggleKey)) ResupplyPressed = !ResupplyPressed;

        MouseX = CursorVisible ? 0f : Input.GetAxis("Mouse X");
        MouseY = CursorVisible ? 0f : Input.GetAxis("Mouse Y");

        float x = (Input.GetKey(right) ? 1 : 0) - (Input.GetKey(left) ? 1 : 0);
        float y = (Input.GetKey(up) ? 1 : 0) - (Input.GetKey(down) ? 1 : 0);
        float z = (Input.GetKey(forward) ? 1 : 0) - (Input.GetKey(backward) ? 1 : 0);
        MoveDirection = new Vector3(x, y, z).normalized;

        BoostHeld = Input.GetKey(boost);
        AimHeld = CursorVisible ? false : Input.GetMouseButton(aimMouseButton);
        FirePressed = CursorVisible ? false : Input.GetMouseButtonDown(fireMouseButton);
        FireHeld = CursorVisible ? false : Input.GetMouseButton(fireMouseButton);
    }
}