using UnityEngine;

public class BulletController : MonoBehaviour
{
    [Header("SETTINGS")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private ParticleSystem hitEffectPrefab;

    void OnCollisionEnter(Collision collision){
        IDamageable damageable = collision.collider.GetComponent<IDamageable>();
        if(damageable != null) damageable.TakeDamage(damage);

        if(hitEffectPrefab != null){
            ContactPoint contact = collision.GetContact(0);
            Instantiate(hitEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
        }

        gameObject.SetActive(false);
    }
}