using Nova;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NovaPlayButton : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "GameScene";

    private Interactable interactable;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            Debug.LogError("Interactable is missing on " + gameObject.name);
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        Debug.Log("PLAY clicked!");

        evt.Consume();

        SceneManager.LoadScene(gameSceneName);
    }
}