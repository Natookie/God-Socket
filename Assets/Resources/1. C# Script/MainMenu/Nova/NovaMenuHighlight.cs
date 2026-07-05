using Nova;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class NovaMenuHighlight : MonoBehaviour
{
    [Header("References")]
    public GameObject selectionBG;
    public TextBlock labelText;

    [Header("Text Colors")]
    public Color normalTextColor = Color.white;
    public Color hoverTextColor = Color.black;

    private Interactable interactable;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
    }

    private void OnEnable()
    {
        HideHover();

        if (interactable == null)
        {
            return;
        }

        interactable.UIBlock.AddGestureHandler<Gesture.OnHover>(HandleHover);
        interactable.UIBlock.AddGestureHandler<Gesture.OnUnhover>(HandleUnhover);
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.UIBlock.RemoveGestureHandler<Gesture.OnHover>(HandleHover);
            interactable.UIBlock.RemoveGestureHandler<Gesture.OnUnhover>(HandleUnhover);
        }

        HideHover();
    }

    private void HandleHover(Gesture.OnHover evt)
    {
        ShowHover();
    }

    private void HandleUnhover(Gesture.OnUnhover evt)
    {
        HideHover();
    }

    private void ShowHover()
    {
        if (selectionBG != null)
        {
            selectionBG.SetActive(true);
        }

        if (labelText != null)
        {
            labelText.Color = hoverTextColor;
        }
    }

    private void HideHover()
    {
        if (selectionBG != null)
        {
            selectionBG.SetActive(false);
        }

        if (labelText != null)
        {
            labelText.Color = normalTextColor;
        }
    }
}