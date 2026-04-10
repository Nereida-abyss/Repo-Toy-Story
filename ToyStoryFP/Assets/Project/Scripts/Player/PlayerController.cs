using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovementScript))]
[RequireComponent(typeof(PlayerHealthScript))]
[RequireComponent(typeof(PlayerCurrencyController))]
[RequireComponent(typeof(PlayerAudioController))]
[RequireComponent(typeof(PlayerShopController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [SerializeField] private MovementScript movementScript;
    [SerializeField] private PlayerHealthScript healthScript;
    [SerializeField] private PlayerCurrencyController currencyController;
    [SerializeField] private PlayerAudioController audioController;
    [SerializeField] private WeaponLoadoutScript weaponLoadout;
    [SerializeField] private PlayerShopController shopController;

    private bool hasLoggedMissingDependencies;

    public PlayerHealthScript Health => healthScript;
    public PlayerCurrencyController Currency => currencyController;
    public PlayerAudioController Audio => audioController;
    public WeaponLoadoutScript WeaponLoadout => weaponLoadout;
    public PlayerShopController Shop => shopController;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            GameDebug.Advertencia("Jugador", "Se detecto un PlayerController duplicado. Se destruira la instancia mas nueva.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ValidateDependencies();
        RunStatsStore.BeginRun();
        weaponLoadout?.BeginRunLoadout();
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
        if (movementScript == null)
        {
            ValidateDependencies();
            return;
        }

        if (UIManager.IsGamePaused || PlayerShopController.IsInputBlocked)
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
            ValidateDependencies();
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
            weaponLoadout.CurrentWeapon?.TryFire();
        }
    }

    private void ValidateDependencies()
    {
        if (movementScript != null
            && healthScript != null
            && currencyController != null
            && audioController != null
            && weaponLoadout != null
            && shopController != null)
        {
            return;
        }

        if (hasLoggedMissingDependencies)
        {
            return;
        }

        hasLoggedMissingDependencies = true;
        GameDebug.Advertencia(
            "Jugador",
            "PlayerController necesita referencias serializadas a Movement, Health, Currency, Audio, WeaponLoadout y Shop.",
            this);
    }
}
