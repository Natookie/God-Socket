using UnityEngine;
using System.Collections;

public enum PlayerState
{
    Normal,
    Aiming,
    Boosting,
    Overheated
}

public class PlayerController : MonoBehaviour, IDamageable
{
    public static PlayerController Instance {get; private set;}

    [Header("STATE")]
    public PlayerState currentState = PlayerState.Normal;

    [Header("REFERENCES")]
    public InputHandler input;
    public EnergySystem energy;
    public MovementController movement;
    public WeaponController weapon;
    public PlayerRotation playerRotation;

    private Coroutine overheatRoutine;
    private bool cursorModeActive;

    void Awake(){
        if(Instance == null) Instance = this;
        else return;

        if(!input) input = GetComponent<InputHandler>();
        if(!energy) energy = GetComponent<EnergySystem>();
        if(!movement) movement = GetComponent<MovementController>();
        if(!weapon) weapon = GetComponent<WeaponController>();
        if(!playerRotation) playerRotation = GetComponent<PlayerRotation>();
    }

    void Start(){
        energy.OnEnergyDepleted += HandleEnergyDepleted;
        energy.OnEnergyRestored += HandleEnergyRestored;
    }

    void Update(){
        if(input.CursorVisible != cursorModeActive){
            cursorModeActive = input.CursorVisible;
            if(cursorModeActive){
                if(currentState == PlayerState.Aiming) ExitAiming();
                if(currentState == PlayerState.Boosting) ExitBoosting();

                movement.SetInputDisabled(true);
                weapon.SetInputDisabled(true);
                playerRotation.SetInputDisabled(true);
            }
            else{
                movement.SetInputDisabled(false);
                weapon.SetInputDisabled(false);
                playerRotation.SetInputDisabled(false);
            }
        }

        if(cursorModeActive) return;
        if(currentState == PlayerState.Overheated) return;

        if(input.AimHeld && currentState != PlayerState.Aiming) EnterAiming();
        else if(!input.AimHeld && currentState == PlayerState.Aiming) ExitAiming();

        if(input.BoostHeld && CanBoost() && currentState != PlayerState.Boosting) EnterBoosting();
        else if(!input.BoostHeld && currentState == PlayerState.Boosting) ExitBoosting();
    }

    bool CanBoost() => energy.GetCurrentEnergy() > 0 && currentState != PlayerState.Overheated;
    void EnterAiming() => currentState = PlayerState.Aiming;
    void ExitAiming() => currentState = PlayerState.Normal;
    void EnterBoosting(){
        currentState = PlayerState.Boosting;
        movement.SetBoostActive(true);
    }
    void ExitBoosting(){
        currentState = PlayerState.Normal;
        movement.SetBoostActive(false);
    }

    void HandleEnergyDepleted(){
        if(currentState == PlayerState.Overheated) return;

        currentState = PlayerState.Overheated;
        movement.SetOverheated(true);
        weapon.SetOverheated(true);
        movement.SetBoostActive(false);
        playerRotation.SetInputDisabled(true);

        if(overheatRoutine != null) StopCoroutine(overheatRoutine);
        overheatRoutine = StartCoroutine(OverheatCooldown());
    }
    public bool IsOverheat() => currentState == PlayerState.Overheated;

    IEnumerator OverheatCooldown(){
        yield return new WaitForSeconds(energy.overheatCooldown);
        energy.FullRestore();
    }

    void HandleEnergyRestored(){
        movement.SetOverheated(false);
        weapon.SetOverheated(false);
        playerRotation.SetInputDisabled(false);
        currentState = PlayerState.Normal;
    }

    public void TakeDamage(float damage) => energy.Consume(damage);
    public bool IsAlive() => true;

    void OnDestroy(){
        energy.OnEnergyDepleted -= HandleEnergyDepleted;
        energy.OnEnergyRestored -= HandleEnergyRestored;
    }
}