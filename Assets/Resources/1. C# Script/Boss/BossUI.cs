using UnityEngine;
using Nova;

public class BossUI : MonoBehaviour
{
    [SerializeField] private UIBlock2D healthFill;
    public void UpdateUI(float health) => healthFill.Size.X.Percent = Mathf.Max(0, health);
}