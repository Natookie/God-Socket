using Nova;
using UnityEngine;

[RequireComponent(typeof(UIBlock2D))]
[RequireComponent(typeof(Interactable))]
public class NovaTutorialScroll : MonoBehaviour
{
    [Header("Scroll Content")]
    public UIBlock scrollContent;

    [Header("Scroll Settings")]
    public float scrollSpeed = 80f;
    public float maxScrollY = 550f;

    private Interactable interactable;
    private bool isHovering;
    private float currentScrollY;
    private Vector3 startLocalPosition;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
    }

    private void Start()
    {
        if (scrollContent != null)
        {
            startLocalPosition = scrollContent.transform.localPosition;
        }
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            return;
        }

        interactable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleHover);
        interactable.UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleUnhover);
    }

    private void OnDisable()
    {
        if (interactable == null)
        {
            return;
        }

        interactable.UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleHover);
        interactable.UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleUnhover);
    }

    private void Update()
    {
        if (!isHovering || scrollContent == null)
        {
            return;
        }

        float wheel = Input.mouseScrollDelta.y;

        if (Mathf.Abs(wheel) < 0.01f)
        {
            return;
        }

        currentScrollY -= wheel * scrollSpeed;
        currentScrollY = Mathf.Clamp(currentScrollY, 0f, maxScrollY);

        Vector3 newPosition = startLocalPosition;
        newPosition.y += currentScrollY;

        scrollContent.TrySetLocalPosition(newPosition);
    }

    private void HandleHover(Gesture.OnHover evt)
    {
        isHovering = true;
    }

    private void HandleUnhover(Gesture.OnUnhover evt)
    {
        isHovering = false;
    }
}