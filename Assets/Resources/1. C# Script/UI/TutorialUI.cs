using UnityEngine;
using Nova;

public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance; 

    [SerializeField] private GameObject panel;
    [SerializeField] private UIBlock2D icon;
    [SerializeField] private TextBlock text;
    [SerializeField] private Sprite[] iconPreset;

    //0 = MISSILE
    //1 = UFO
    //2 = WEAK

    void Awake(){
        if(Instance == null) Instance = this;
        else{
            Destroy(gameObject);
            return;
        }
    }

    void Start() => HideTutorial();
    public void HideTutorial() => panel.SetActive(false);

    public void showTutorial(int type, string message){
        panel.SetActive(true);
        text.Text = message;
        
        if(type == -1){
            icon.gameObject.SetActive(false);
            return;
        }
        else icon.gameObject.SetActive(true);
        icon.SetImage(iconPreset[type]);
    }
}