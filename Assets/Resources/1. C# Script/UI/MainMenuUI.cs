using UnityEngine;
using Nova;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject bgContainer;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private BossController bossController;

    [Header("PLAY")]
    [SerializeField] private UIBlock2D playContainer;
    [SerializeField] private UIBlock2D playButton;
    [SerializeField] private Color playButtonHoverColor;
    [SerializeField] private Color playButtonUnhoverColor;
    [Space(10)]
    [SerializeField] private TextBlock playText;
    [SerializeField] private Color playTextHoverColor;
    [SerializeField] private Color playTextUnhoverColor;

    [Header("EXIT")]
    [SerializeField] private UIBlock2D exitContainer;
    [SerializeField] private UIBlock2D exitButton;
    [SerializeField] private Color exitButtonHoverColor;
    [SerializeField] private Color exitButtonUnhoverColor;
    [Space(10)]
    [SerializeField] private TextBlock exitText;
    [SerializeField] private Color exitTextHoverColor;
    [SerializeField] private Color exitTextUnhoverColor;

    [Header("SHARED VARIABLE")]
    [SerializeField] private float yOffset = 10f;
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;

    private Vector3 playOriginalScale;
    private Vector3 exitOriginalScale;
    private float playOriginalY;
    private float exitOriginalY;
    private Coroutine playAnimationCoroutine;
    private Coroutine exitAnimationCoroutine;

    private bool isRunning = false;

    void Awake(){
        playContainer.AddGestureHandler<Gesture.OnClick>(OnPlayClick);
        playContainer.AddGestureHandler<Gesture.OnHover>(OnPlayHover);
        playContainer.AddGestureHandler<Gesture.OnUnhover>(OnPlayUnhover);

        exitContainer.AddGestureHandler<Gesture.OnClick>(OnExitClick);
        exitContainer.AddGestureHandler<Gesture.OnHover>(OnExitHover);
        exitContainer.AddGestureHandler<Gesture.OnUnhover>(OnExitUnhover);

        playOriginalScale = playContainer.transform.localScale;
        exitOriginalScale = exitContainer.transform.localScale;
        playOriginalY = playButton.Position.Value.y;
        exitOriginalY = exitButton.Position.Value.y;
    }

    void OnPlayClick(Gesture.OnClick evt){
        GameManager.Instance.currentState = GameManager.GameState.Running;
        bgContainer.SetActive(false);
        inputHandler.HideCursor();
        bossController.CallDelay();
        AudioManager.Instance.PlayButtonClick();
    }

    void OnPlayHover(Gesture.OnHover evt){
        if(playAnimationCoroutine != null) StopCoroutine(playAnimationCoroutine);
        playAnimationCoroutine = StartCoroutine(AnimatePlayButton(true));
    }

    void OnPlayUnhover(Gesture.OnUnhover evt){
        if(playAnimationCoroutine != null) StopCoroutine(playAnimationCoroutine);
        playAnimationCoroutine = StartCoroutine(AnimatePlayButton(false));
    }

    void OnExitClick(Gesture.OnClick evt){
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void OnExitHover(Gesture.OnHover evt){
        if(exitAnimationCoroutine != null) StopCoroutine(exitAnimationCoroutine);
        exitAnimationCoroutine = StartCoroutine(AnimateExitButton(true));
    }

    void OnExitUnhover(Gesture.OnUnhover evt){
        if(exitAnimationCoroutine != null) StopCoroutine(exitAnimationCoroutine);
        exitAnimationCoroutine = StartCoroutine(AnimateExitButton(false));
    }

    IEnumerator AnimatePlayButton(bool isHovered){
        float elapsed = 0f;
        Vector3 startScale = playContainer.transform.localScale;
        Vector3 targetScale = isHovered ? playOriginalScale * scaleMultiplier : playOriginalScale;
        float startY = playButton.Position.Value.y;
        float targetY = isHovered ? playOriginalY + yOffset : playOriginalY;
        Color startButtonColor = playButton.Color;
        Color targetButtonColor = isHovered ? playButtonHoverColor : playButtonUnhoverColor;
        Color startTextColor = playText.Color;
        Color targetTextColor = isHovered ? playTextHoverColor : playTextUnhoverColor;

        while(elapsed < animationDuration){
            float t = elapsed / animationDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            playContainer.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            Length3 newPos = playButton.Position;
            newPos.Y = Mathf.Lerp(startY, targetY, t);
            playButton.Position = newPos;
            playButton.Color = Color.Lerp(startButtonColor, targetButtonColor, t);
            playText.Color = Color.Lerp(startTextColor, targetTextColor, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        playContainer.transform.localScale = targetScale;
        Length3 finalPos = playButton.Position;
        finalPos.Y = targetY;
        playButton.Position = finalPos;
        playButton.Color = targetButtonColor;
        playText.Color = targetTextColor;
    }

    IEnumerator AnimateExitButton(bool isHovered){
        float elapsed = 0f;
        Vector3 startScale = exitContainer.transform.localScale;
        Vector3 targetScale = isHovered ? exitOriginalScale * scaleMultiplier : exitOriginalScale;
        float startY = exitButton.Position.Value.y;
        float targetY = isHovered ? exitOriginalY + yOffset : exitOriginalY;
        Color startButtonColor = exitButton.Border.Color;
        Color targetButtonColor = isHovered ? exitButtonHoverColor : exitButtonUnhoverColor;
        Color startTextColor = exitText.Color;
        Color targetTextColor = isHovered ? exitTextHoverColor : exitTextUnhoverColor;

        while(elapsed < animationDuration){
            float t = elapsed / animationDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            exitContainer.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            Length3 newPos = exitButton.Position;
            newPos.Y = Mathf.Lerp(startY, targetY, t);
            exitButton.Position = newPos;
            
            exitButton.Border.Color = Color.Lerp(startButtonColor, targetButtonColor, t);
            exitText.Color = Color.Lerp(startTextColor, targetTextColor, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        exitContainer.transform.localScale = targetScale;
        Length3 finalPos = exitButton.Position;
        finalPos.Y = targetY;
        exitButton.Position = finalPos;
        
        exitButton.Border.Color = targetButtonColor;
        exitText.Color = targetTextColor;
    }
}