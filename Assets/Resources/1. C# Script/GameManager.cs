using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}
    public enum GameState
    {
        Menu,
        Running,
        GameOver
    }
    public GameState currentState = GameState.Menu;
    
    [Header("DEBUG")]
    [SerializeField] private bool autoRun;

    void Awake(){
        if(Instance == null) Instance = this;
        else{
            Destroy(gameObject);
            return;
        }
    }

    void Start(){
        if(autoRun) currentState = GameState.Running;
    }
}
