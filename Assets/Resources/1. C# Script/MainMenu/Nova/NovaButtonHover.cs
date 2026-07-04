using Nova;
using UnityEngine;

[RequireComponent(typeof(UIBlock2D))]
[RequireComponent(typeof(Interactable))]
public class NovaButtonHover : MonoBehaviour
{
    [Header("Color")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Text Color")]
    public TextBlock textBlock;
    public Color normalTextColor = Color.black;
    public Color hoverTextColor = Color.black;

    [Header("Scale")]
    public float hoverScale = 1.08f;
    public float animationSpeed = 12f;

    private UIBlock2D uiBlock;
    private Vector3 normalScale;
    private Vector3 targetScale;
    private Color targetColor;
    private Color targetTextColor;

    private void Awake()
    {
        uiBlock = GetComponent<UIBlock2D>();

        normalScale = transform.localScale;
        targetScale = normalScale;

        normalColor = uiBlock.Color;
        targetColor = normalColor;

        if (textBlock != null)
        {
            normalTextColor = textBlock.Color;
            targetTextColor = normalTextColor;
        }
    }

    private void OnEnable()
    {
        uiBlock.AddGestureHandler<Gesture.OnHover>(HandleHover);
        uiBlock.AddGestureHandler<Gesture.OnUnhover>(HandleUnhover);
    }

    private void OnDisable()
    {
        uiBlock.RemoveGestureHandler<Gesture.OnHover>(HandleHover);
        uiBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleUnhover);
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );

        uiBlock.Color = Color.Lerp(
            uiBlock.Color,
            targetColor,
            Time.unscaledDeltaTime * animationSpeed
        );

        if (textBlock != null)
        {
            textBlock.Color = Color.Lerp(
                textBlock.Color,
                targetTextColor,
                Time.unscaledDeltaTime * animationSpeed
            );
        }
    }

    private void HandleHover(Gesture.OnHover evt)
    {
        targetScale = normalScale * hoverScale;
        targetColor = hoverColor;
        targetTextColor = hoverTextColor;
    }

    private void HandleUnhover(Gesture.OnUnhover evt)
    {
        targetScale = normalScale;
        targetColor = normalColor;
        targetTextColor = normalTextColor;
    }
}