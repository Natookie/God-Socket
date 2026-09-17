using Nova;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Menu,
        Running,
        GameOver
    }

    public GameState currentState = GameState.Menu;

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextBlock gameOverMessage;

    [Header("DEBUG")]
    [SerializeField] private bool autoRun;
    public BossController bossController;

    void Awake(){
        if(Instance == null) Instance = this;
        else{
            Destroy(gameObject);
            return;
        }
    }

    void Start(){
        gameOverPanel.SetActive(false);
        if(autoRun){
            bossController.CallDelay();
            currentState = GameState.Running;
        }
    }

    public void GameOver(string message){
        currentState = GameState.GameOver;
        if(gameOverMessage != null) gameOverMessage.Text = message;
        if(gameOverPanel != null) gameOverPanel.SetActive(true);
        gameOverPanel.SetActive(true);
        
        StartCoroutine(ReloadSceneAfterDelay(2f));
    }

    IEnumerator ReloadSceneAfterDelay(float delay){
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame(){
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}