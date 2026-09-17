using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public Animator animator;
    public MovementController mc;

    void Update(){
        bool isFastFlying = Input.GetKey(KeyCode.LeftShift);
        bool isShooting = Input.GetMouseButton(0);
        bool isMounting = mc.IsRiding();
        bool isShutdown = mc.IsOverheated();

        animator.SetBool("isFastFlying", isFastFlying);
        animator.SetBool("isShooting", isShooting);
        animator.SetBool("isMounting", isMounting);
        animator.SetBool("isShutdown", isShutdown);
    }
}