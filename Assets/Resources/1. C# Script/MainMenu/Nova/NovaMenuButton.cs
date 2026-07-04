using System.Collections;
using Nova;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NovaMenuButton : MonoBehaviour
{
    public enum ButtonType
    {
        Play,
        Settings,
        Tutorial,
        Credit,
        Back,
        Quit
    }

    [Header("Button")]
    public ButtonType buttonType;

    [Header("References")]
    public MainMenuManager mainMenuManager;

    [Header("Scene")]
    public string gameSceneName = "GameScene";

    [Header("Click Animation")]
    public float pressedScale = 0.9f;
    public float popScale = 1.08f;
    public float animationSpeed = 18f;

    private Interactable interactable;
    private Vector3 normalScale;
    private bool isClicked;

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

        if (isClicked)
        {
            return;
        }

        StartCoroutine(ClickAnimationThenAction());
    }

    private IEnumerator ClickAnimationThenAction()
    {
        isClicked = true;

        yield return ScaleTo(normalScale * pressedScale, 0.05f);
        yield return ScaleTo(normalScale * popScale, 0.07f);
        yield return ScaleTo(normalScale, 0.06f);

        ExecuteButtonAction();

        isClicked = false;
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

    private void ExecuteButtonAction()
    {
        switch (buttonType)
        {
            case ButtonType.Play:
                Debug.Log("Play clicked");
                SceneManager.LoadScene(gameSceneName);
                break;

            case ButtonType.Settings:
                Debug.Log("Settings clicked");

                if (mainMenuManager != null)
                {
                    mainMenuManager.ShowSettings();
                }

                break;

            // case ButtonType.Tutorial:
            //     Debug.Log("Tutorial clicked");

            //     if (mainMenuManager != null)
            //     {
            //         mainMenuManager.ShowTutorial();
            //     }

            //     break;

            case ButtonType.Back:
                Debug.Log("Back clicked");

                if (mainMenuManager != null)
                {
                    mainMenuManager.ShowMainMenu();
                }

                break;

            case ButtonType.Quit:
                Debug.Log("Quit clicked");

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }
}