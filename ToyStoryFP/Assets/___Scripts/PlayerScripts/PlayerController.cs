using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovementScript))]
[RequireComponent(typeof(PlayerHealthScript))]
[RequireComponent(typeof(PlayerCurrencyController))]
[RequireComponent(typeof(PlayerAudioController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    private MovementScript movementScript;
    private PlayerHealthScript healthScript;
    private PlayerCurrencyController currencyController;
    private PlayerAudioController audioController;
    private WeaponLoadoutScript weaponLoadout;

    public PlayerHealthScript Health => healthScript;
    public PlayerCurrencyController Currency => currencyController;
    public PlayerAudioController Audio => audioController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate PlayerController detected. Destroying the newest instance.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        movementScript = GetComponent<MovementScript>();
        healthScript = GetComponent<PlayerHealthScript>();
        currencyController = GetComponent<PlayerCurrencyController>();
        audioController = GetComponent<PlayerAudioController>();
        weaponLoadout = GetComponentInChildren<WeaponLoadoutScript>(true);

        RunStatsStore.BeginRun();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (UIManager.IsGamePaused)
        {
            movementScript.SetMoveInput(Vector2.zero);
            return;
        }

        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        movementScript.SetMoveInput(moveInput);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            movementScript.RequestJump();
        }

        HandleWeaponInput();
    }

    private void HandleWeaponInput()
    {
        if (weaponLoadout == null)
        {
            return;
        }

        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            weaponLoadout.TryCycleWeapon(1);
        }
        else if (scroll < 0f)
        {
            weaponLoadout.TryCycleWeapon(-1);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponLoadout.CurrentWeapon?.TryReload();
        }

        if (weaponLoadout.IsSwitchingWeapon)
        {
            return;
        }

        if (Input.GetButton("Fire1"))
        {
            weaponLoadout?.CurrentWeapon?.TryFire();
        }
    }
}
