using UnityEngine;

public class Building : MonoBehaviour, IDamageable
{
    [Header("STATUS")]
    public float health = 20f;

    [Header("DESTRUCTION")]
    public float explosionForce = 100f;
    public float explosionRadius = 25f;
    public float upwardsModifier = 5f;
    [Space(10)]
    public bool isTargeted = false;

    private BoxCollider col;
    private Renderer fullMesh;
    private bool destroyed;

    void Start(){
        fullMesh = GetComponent<Renderer>();
        for(int i = 1; i < transform.childCount; i++){
            Transform piece = transform.GetChild(i);

            piece.gameObject.SetActive(false);

            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if(rb != null) rb.isKinematic = true;
        }

        col ??= GetComponent<BoxCollider>();
    }

    void Update(){
        //if(Input.GetKeyDown(KeyCode.R)) DestroyBuilding();
    }


    public void TakeDamage(float damage){
        health -= damage;
        if(health <= 0){
            BuildingManager.Instance.SetDestroyed(this);
            CameraController.Instance.ShakeCamera(7f, 1.5f, CameraController.ShakePriority.Critical);
            DestroyBuilding();
            Destroy(gameObject, 30f);
        }
    }

    public void DestroyBuilding(){
        if(destroyed) return;
        col.enabled = false;
        fullMesh.enabled = false;
        destroyed = true;
        if(this.transform.childCount > 0) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(GameSFX.BuildingDestroyed);
        }

        Vector3 explosionOrigin = transform.position + Vector3.up * 2f;
        for(int i = 1; i < transform.childCount; i++){
            Transform piece = transform.GetChild(i);

            piece.gameObject.SetActive(true);
            Rigidbody rb = piece.GetComponent<Rigidbody>();

            if(rb != null){
                rb.isKinematic = false;
                rb.AddExplosionForce(
                    explosionForce,
                    explosionOrigin,
                    explosionRadius,
                    upwardsModifier,
                    ForceMode.Impulse
                );
            }
        }
    }

    public bool IsAlive() => !destroyed;
}