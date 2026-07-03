using UnityEngine;

public class BossHealthSystem : MonoBehaviour, IDamageable
{
    [Header("BOSS STATS")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("REFERENCES")]
    public BossUI bossUI;
    public AudioSource audioSource;
    public AudioClip damageSound;

    public System.Action OnDeath;

    void Start(){
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage){
        if(currentHealth <= 0f) return;
        
        currentHealth -= damage;
        bossUI.UpdateUI(currentHealth/maxHealth);

        if(audioSource != null && damageSound != null) audioSource.PlayOneShot(damageSound);
        if(currentHealth <= 0f) OnDeath?.Invoke();
    }

    public bool IsAlive() => currentHealth > 0f;
    public float GetHealthPercent() => currentHealth / maxHealth;
    public void ResetHealth() => currentHealth = maxHealth;
}