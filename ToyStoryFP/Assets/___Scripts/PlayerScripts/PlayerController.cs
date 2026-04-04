using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovementScript))]
public class PlayerController : MonoBehaviour
{
    private MovementScript movementScript;
    private WeaponLoadoutScript weaponLoadout;

    void Awake()
    {
        movementScript = GetComponent<MovementScript>();
        weaponLoadout = GetComponentInChildren<WeaponLoadoutScript>(true);
    }

    void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsPaused)
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
