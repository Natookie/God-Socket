using UnityEngine;
using Nova;

public class CrosshairUI : MonoBehaviour
{
    public static CrosshairUI Instance {get; private set;}

    [SerializeField] private UIBlock2D crossHairTexture;
    [SerializeField] private Color32 defaultColor;
    [SerializeField] private Color32 autoAimColor;

    void Awake(){
        if(Instance == null) Instance = this;
        else{
            Destroy(gameObject);
            return;
        }
    }

    public void ChangeColor(bool useDefault) => crossHairTexture.Color = useDefault ? defaultColor : autoAimColor;
}
