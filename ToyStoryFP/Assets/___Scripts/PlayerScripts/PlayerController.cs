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
        Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        movementScript.SetMoveInput(moveInput);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            movementScript.RequestJump();
        }

        if (Input.GetButton("Fire1"))
        {
            weaponLoadout?.CurrentWeapon?.TryFire();
        }
    }
}
