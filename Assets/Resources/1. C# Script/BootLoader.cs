using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    void Awake(){
        SceneManager.LoadScene("ManagerScene", LoadSceneMode.Additive);
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
    }
}