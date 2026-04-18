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
    [SerializeField] private MouseLookScript mouseLook;
    [SerializeField] private WeaponLoadoutScript weaponLoadout;
    [SerializeField] private PlayerShopController shopController;

    private bool hasLoggedMissingDependencies;
    private bool queuedPrimaryFire;
    private bool shouldSkipQueuedPrimaryFire;

    public PlayerHealthScript Health => healthScript;
    public PlayerCurrencyController Currency => currencyController;
    public PlayerAudioController Audio => audioController;
    public MouseLookScript MouseLook => mouseLook;
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
        ResolveDependencies();
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
        ResetQueuedWeaponActions();

        if (movementScript == null)
        {
            ResolveDependencies();
            ValidateDependencies();
            return;
        }

        if (GameplayInputGate.IsBlocked)
        {
            movementScript.SetMoveInput(Vector2.zero);
            return;
        }

        Vector2 moveInput = ProjectInput.GetMoveInput().normalized;
        movementScript.SetMoveInput(moveInput);

        if (ProjectInput.WasDashPressed())
        {
            movementScript.TryStartDash(moveInput);
        }

        if (ProjectInput.WasJumpPressed())
        {
            movementScript.RequestJump();
        }

        HandleWeaponInput();
    }

    void LateUpdate()
    {
        ProcessQueuedWeaponInput();
    }

    private void HandleWeaponInput()
    {
        if (weaponLoadout == null)
        {
            ResolveDependencies();
            ValidateDependencies();
            return;
        }

        float scroll = ProjectInput.GetWeaponCycleScroll();

        if (scroll > 0f)
        {
            shouldSkipQueuedPrimaryFire = weaponLoadout.TryCycleWeapon(1);
        }
        else if (scroll < 0f)
        {
            shouldSkipQueuedPrimaryFire = weaponLoadout.TryCycleWeapon(-1);
        }

        if (ProjectInput.WasReloadPressed())
        {
            bool reloadStarted = weaponLoadout.CurrentWeapon?.TryReload() ?? false;
            shouldSkipQueuedPrimaryFire |= reloadStarted;
        }

        if (weaponLoadout.IsSwitchingWeapon)
        {
            return;
        }

        if (ProjectInput.IsPrimaryFireHeld())
        {
            queuedPrimaryFire = true;
        }
    }

    private void ProcessQueuedWeaponInput()
    {
        if (!queuedPrimaryFire)
        {
            return;
        }

        bool canProcessQueuedShot =
            !shouldSkipQueuedPrimaryFire &&
            !GameplayInputGate.IsBlocked &&
            weaponLoadout != null &&
            !weaponLoadout.IsSwitchingWeapon;

        if (canProcessQueuedShot)
        {
            weaponLoadout.CurrentWeapon?.TryFire();
        }

        ResetQueuedWeaponActions();
    }

    private void ResetQueuedWeaponActions()
    {
        queuedPrimaryFire = false;
        shouldSkipQueuedPrimaryFire = false;
    }

    private void ResolveDependencies()
    {
        movementScript ??= GetComponent<MovementScript>();
        healthScript ??= GetComponent<PlayerHealthScript>();
        currencyController ??= GetComponent<PlayerCurrencyController>();
        audioController ??= GetComponent<PlayerAudioController>();
        mouseLook ??= GetComponentInChildren<MouseLookScript>(true);
        weaponLoadout ??= GetComponentInChildren<WeaponLoadoutScript>(true);
        shopController ??= GetComponent<PlayerShopController>();
    }

    private void ValidateDependencies()
    {
        ResolveDependencies();

        if (movementScript != null
            && healthScript != null
            && currencyController != null
            && audioController != null
            && mouseLook != null
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
        string missingDependencies = string.Empty;

        if (movementScript == null)
        {
            missingDependencies += "MovementScript, ";
        }

        if (healthScript == null)
        {
            missingDependencies += "PlayerHealthScript, ";
        }

        if (currencyController == null)
        {
            missingDependencies += "PlayerCurrencyController, ";
        }

        if (audioController == null)
        {
            missingDependencies += "PlayerAudioController, ";
        }

        if (mouseLook == null)
        {
            missingDependencies += "MouseLookScript, ";
        }

        if (weaponLoadout == null)
        {
            missingDependencies += "WeaponLoadoutScript, ";
        }

        if (shopController == null)
        {
            missingDependencies += "PlayerShopController, ";
        }

        missingDependencies = missingDependencies.TrimEnd(' ', ',');
        GameDebug.Advertencia(
            "Jugador",
            $"PlayerController no pudo resolver estas dependencias: {missingDependencies}.",
            this);
    }
}
