using System;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponLoadoutScript : MonoBehaviour
{
    [SerializeField] private Camera weaponCamera;
    [SerializeField] private float weaponSwitchDuration = 0.35f;

    private WeaponScript[] weapons = System.Array.Empty<WeaponScript>();
    private int currentWeaponIndex = -1;
    private int pendingWeaponIndex = -1;
    private float weaponSwitchTimer;

    public event Action<WeaponScript> CurrentWeaponChanged;

    public WeaponScript CurrentWeapon =>
        currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Length
            ? weapons[currentWeaponIndex]
            : null;

    public bool IsSwitchingWeapon => weaponSwitchTimer > 0f;

    void Awake()
    {
        if (weaponCamera == null)
        {
            weaponCamera = GetComponent<Camera>();
        }

        RefreshWeapons();
    }

    void Update()
    {
        if (!IsSwitchingWeapon)
        {
            return;
        }

        weaponSwitchTimer -= Time.deltaTime;

        if (weaponSwitchTimer > 0f)
        {
            return;
        }

        CompleteWeaponSwitch();
    }

    public void RefreshWeapons()
    {
        weapons = GetComponentsInChildren<WeaponScript>(true);

        if (weapons.Length == 0)
        {
            currentWeaponIndex = -1;
            return;
        }

        int activeIndex = 0;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weaponCamera != null)
            {
                weapons[i]._camera = weaponCamera;
            }

            weapons[i].SetPlayerOwned(true);

            if (weapons[i].gameObject.activeSelf)
            {
                activeIndex = i;
            }
        }

        SetWeaponIndexImmediate(activeIndex);
    }

    public bool TryCycleWeapon(int direction)
    {
        if (weapons.Length <= 1 || IsSwitchingWeapon)
        {
            return false;
        }

        int nextIndex = currentWeaponIndex;

        do
        {
            nextIndex = (nextIndex + direction + weapons.Length) % weapons.Length;
        }
        while (nextIndex == currentWeaponIndex);

        StartWeaponSwitch(nextIndex);
        return true;
    }

    private void StartWeaponSwitch(int nextIndex)
    {
        pendingWeaponIndex = Mathf.Clamp(nextIndex, 0, weapons.Length - 1);
        weaponSwitchTimer = Mathf.Max(0.01f, weaponSwitchDuration);

        CurrentWeapon?.CancelReload();
        SetAllWeaponsActive(false);
        currentWeaponIndex = -1;
        CurrentWeaponChanged?.Invoke(null);
    }

    private void CompleteWeaponSwitch()
    {
        weaponSwitchTimer = 0f;

        if (pendingWeaponIndex < 0)
        {
            return;
        }

        SetWeaponIndexImmediate(pendingWeaponIndex);
        pendingWeaponIndex = -1;
    }

    private void SetWeaponIndexImmediate(int index)
    {
        if (weapons.Length == 0)
        {
            currentWeaponIndex = -1;
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        currentWeaponIndex = Mathf.Clamp(index, 0, weapons.Length - 1);
        SetAllWeaponsActive(false);

        WeaponScript currentWeapon = weapons[currentWeaponIndex];
        currentWeapon.gameObject.SetActive(true);
        currentWeapon.NotifyEquipped();
        CurrentWeaponChanged?.Invoke(currentWeapon);
    }

    private void SetAllWeaponsActive(bool active)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(active);
        }
    }
}
