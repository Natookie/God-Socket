using Nova;
using UnityEngine;

public class NovaMouseInput : MonoBehaviour
{
    private const uint MousePointerControlID = 1;

    private void Update()
    {
        if (!Input.mousePresent)
        {
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError("No Main Camera found. Make sure your camera tag is MainCamera.");
            return;
        }

        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        Interaction.Update update = new Interaction.Update(mouseRay, MousePointerControlID);

        bool mousePressed = Input.GetMouseButton(0);

        Interaction.Point(update, mousePressed);
    }
}