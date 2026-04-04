using UnityEngine;

[DisallowMultipleComponent]
public class WeaponLoadoutScript : MonoBehaviour
{
    [SerializeField] private Camera weaponCamera;

    private WeaponScript[] weapons = System.Array.Empty<WeaponScript>();
    private int currentWeaponIndex = -1;

    public WeaponScript CurrentWeapon =>
        currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Length
            ? weapons[currentWeaponIndex]
            : null;

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
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            SelectNextWeapon(1);
        }
        else if (scroll < 0f)
        {
            SelectNextWeapon(-1);
        }
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

            if (weapons[i].gameObject.activeSelf)
            {
                activeIndex = i;
            }
        }

        SetWeaponIndex(activeIndex);
    }

    private void SelectNextWeapon(int direction)
    {
        if (weapons.Length <= 1)
        {
            return;
        }

        int nextIndex = currentWeaponIndex;

        do
        {
            nextIndex = (nextIndex + direction + weapons.Length) % weapons.Length;
        }
        while (nextIndex == currentWeaponIndex);

        SetWeaponIndex(nextIndex);
    }

    private void SetWeaponIndex(int index)
    {
        if (weapons.Length == 0)
        {
            currentWeaponIndex = -1;
            return;
        }

        currentWeaponIndex = Mathf.Clamp(index, 0, weapons.Length - 1);

        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(i == currentWeaponIndex);
        }
    }
}
