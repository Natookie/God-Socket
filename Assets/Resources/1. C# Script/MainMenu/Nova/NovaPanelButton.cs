using System.Collections;
using Nova;
using UnityEngine;

public class NovaPanelButton : MonoBehaviour
{
    public enum ButtonAction
    {
        OpenSettings,
        OpenTutorial,
        OpenCredit,
        BackToMain,
        Quit
    }

    public ButtonAction buttonAction;
    public MainMenuManager menuManager;

    [Header("Click Animation")]
    public float pressedScale = 0.9f;
    public float popScale = 1.08f;

    private Interactable interactable;
    private Vector3 normalScale;
    private bool isAnimating;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        normalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            Debug.LogError("Interactable missing on " + gameObject.name);
            return;
        }

        interactable.UIBlock.AddGestureHandler<Gesture.OnClick>(HandleClick);
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.UIBlock.RemoveGestureHandler<Gesture.OnClick>(HandleClick);
        }
    }

    private void HandleClick(Gesture.OnClick evt)
    {
        evt.Consume();

        if (isAnimating)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        StartCoroutine(ClickAnimationThenAction());
}

    private IEnumerator ClickAnimationThenAction()
    {
        isAnimating = true;

        yield return ScaleTo(normalScale * pressedScale, 0.05f);
        yield return ScaleTo(normalScale * popScale, 0.07f);
        yield return ScaleTo(normalScale, 0.06f);

        ExecuteAction();

        isAnimating = false;
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void ExecuteAction()
    {
        if (menuManager == null)
        {
            Debug.LogError("MenuManager missing on " + gameObject.name);
            return;
        }

        switch (buttonAction)
        {
            case ButtonAction.OpenSettings:
                menuManager.ShowSettings();
                break;

            case ButtonAction.OpenTutorial:
                menuManager.ShowTutorial();
                break;

            case ButtonAction.OpenCredit:
                menuManager.ShowCredit();
                break;

            case ButtonAction.BackToMain:
                menuManager.ShowMainMenu();
                break;

            case ButtonAction.Quit:
                menuManager.QuitGame();
                break;
        }
    }
}