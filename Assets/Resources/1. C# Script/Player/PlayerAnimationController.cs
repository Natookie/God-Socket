using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public Animator animator;

    void Update(){
        bool isFastFlying = Input.GetKey(KeyCode.LeftShift);
        bool isShooting = Input.GetMouseButton(0);

        animator.SetBool("isFastFlying", isFastFlying);
        animator.SetBool("isShooting", isShooting);
    }
}