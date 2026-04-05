using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WeaponLoadoutScript : MonoBehaviour
{
    private enum WeaponSwitchState
    {
        Idle,
        Lowering,
        Raising
    }

    [SerializeField] private Camera weaponCamera;
    [SerializeField] private float weaponSwitchDuration = 0.35f;
    [SerializeField] private float weaponSwitchLowerRatio = 0.45f;
    [SerializeField] private float weaponSwitchRaiseRatio = 0.45f;

    private WeaponScript[] weapons = Array.Empty<WeaponScript>();
    private int equippedWeaponIndex = -1;
    private int targetWeaponIndex = -1;
    private float switchPhaseTimer;
    private WeaponSwitchState switchState = WeaponSwitchState.Idle;
    private WeaponScript visibleWeapon;
    private PlayerAudioController playerAudio;
    private bool initialLoadoutResolved;

    public event Action<WeaponScript> CurrentWeaponChanged;

    public WeaponScript CurrentWeapon =>
        equippedWeaponIndex >= 0 && equippedWeaponIndex < weapons.Length
            ? weapons[equippedWeaponIndex]
            : null;

    public bool IsSwitchingWeapon => switchState != WeaponSwitchState.Idle;

    void Awake()
    {
        if (weaponCamera == null)
        {
            weaponCamera = GetComponent<Camera>();
        }

        playerAudio = GetComponentInParent<PlayerAudioController>();
        RefreshWeapons();
    }

    void Update()
    {
        if (!IsSwitchingWeapon)
        {
            EnsureEquippedWeaponVisible();
            return;
        }

        float deltaTime = Time.deltaTime;

        switch (switchState)
        {
            case WeaponSwitchState.Lowering:
                UpdateLowering(deltaTime);
                break;
            case WeaponSwitchState.Raising:
                UpdateRaising(deltaTime);
                break;
        }
    }

    public void RefreshWeapons()
    {
        weapons = CollectDirectChildWeapons();

        if (weapons.Length == 0)
        {
            equippedWeaponIndex = -1;
            targetWeaponIndex = -1;
            visibleWeapon = null;
            switchState = WeaponSwitchState.Idle;
            return;
        }

        int activeIndex = -1;
        int activeWeaponCount = 0;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weaponCamera != null)
            {
                weapons[i]._camera = weaponCamera;
            }

            weapons[i].SetPlayerOwned(true);

            if (weapons[i].gameObject.activeSelf)
            {
                activeWeaponCount++;
                activeIndex = i;
            }
        }

        if (activeWeaponCount != 1)
        {
            activeIndex = equippedWeaponIndex >= 0 && equippedWeaponIndex < weapons.Length
                ? equippedWeaponIndex
                : 0;
        }

        if (activeIndex < 0 || activeIndex >= weapons.Length)
        {
            activeIndex = 0;
        }

        bool initializeSilently = !initialLoadoutResolved;
        SetWeaponIndexImmediate(activeIndex, initializeSilently);
        initialLoadoutResolved = true;
    }

    public bool TryCycleWeapon(int direction)
    {
        if (weapons.Length <= 1 || IsSwitchingWeapon)
        {
            return false;
        }

        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= weapons.Length)
        {
            SetWeaponIndexImmediate(0);
        }

        int nextIndex = equippedWeaponIndex;

        do
        {
            nextIndex = (nextIndex + direction + weapons.Length) % weapons.Length;
        }
        while (nextIndex == equippedWeaponIndex);

        StartWeaponSwitch(nextIndex);
        return true;
    }

    private void StartWeaponSwitch(int nextIndex)
    {
        if (nextIndex < 0 || nextIndex >= weapons.Length || nextIndex == equippedWeaponIndex)
        {
            return;
        }

        EnsureEquippedWeaponVisible();

        targetWeaponIndex = nextIndex;
        visibleWeapon = CurrentWeapon;
        switchState = WeaponSwitchState.Lowering;
        switchPhaseTimer = GetLowerDuration();

        if (playerAudio == null)
        {
            playerAudio = GetComponentInParent<PlayerAudioController>();
        }

        visibleWeapon?.CancelReload();
        playerAudio?.PlayWeaponSwitch();
        CurrentWeaponChanged?.Invoke(null);

        if (switchPhaseTimer <= 0f)
        {
            CompleteLowering();
        }
    }

    private void UpdateLowering(float deltaTime)
    {
        if (visibleWeapon == null)
        {
            EnsureEquippedWeaponVisible();
            visibleWeapon = CurrentWeapon;
        }

        float duration = GetLowerDuration();
        float progress = duration <= 0.01f
            ? 1f
            : 1f - Mathf.Clamp01(switchPhaseTimer / duration);

        visibleWeapon?.SetEquipAnimationProgress(SmoothPhase(progress), true);
        switchPhaseTimer -= deltaTime;

        if (switchPhaseTimer <= 0f)
        {
            CompleteLowering();
        }
    }

    private void CompleteLowering()
    {
        if (targetWeaponIndex < 0 || targetWeaponIndex >= weapons.Length)
        {
            CancelWeaponSwitch();
            return;
        }

        ActivateOnly(targetWeaponIndex);
        equippedWeaponIndex = targetWeaponIndex;
        targetWeaponIndex = -1;
        visibleWeapon = CurrentWeapon;
        visibleWeapon?.NotifyEquipped();
        visibleWeapon?.SetEquipAnimationProgress(0f, false);
        CurrentWeaponChanged?.Invoke(visibleWeapon);

        switchState = WeaponSwitchState.Raising;
        switchPhaseTimer = GetRaiseDuration();

        if (switchPhaseTimer <= 0f)
        {
            FinishWeaponSwitch();
        }
    }

    private void UpdateRaising(float deltaTime)
    {
        if (visibleWeapon == null)
        {
            EnsureEquippedWeaponVisible();
            visibleWeapon = CurrentWeapon;
        }

        float duration = GetRaiseDuration();
        float progress = duration <= 0.01f
            ? 1f
            : 1f - Mathf.Clamp01(switchPhaseTimer / duration);

        visibleWeapon?.SetEquipAnimationProgress(SmoothPhase(progress), false);
        switchPhaseTimer -= deltaTime;

        if (switchPhaseTimer <= 0f)
        {
            FinishWeaponSwitch();
        }
    }

    private void FinishWeaponSwitch()
    {
        switchState = WeaponSwitchState.Idle;
        switchPhaseTimer = 0f;
        targetWeaponIndex = -1;

        if (visibleWeapon == null)
        {
            EnsureEquippedWeaponVisible();
            visibleWeapon = CurrentWeapon;
        }

        visibleWeapon?.ResetEquipPose();
        visibleWeapon?.gameObject.SetActive(true);
        CurrentWeaponChanged?.Invoke(visibleWeapon);
    }

    private void CancelWeaponSwitch()
    {
        switchState = WeaponSwitchState.Idle;
        switchPhaseTimer = 0f;
        targetWeaponIndex = -1;
        EnsureEquippedWeaponVisible();
        visibleWeapon = CurrentWeapon;
        CurrentWeaponChanged?.Invoke(visibleWeapon);
    }

    private void SetWeaponIndexImmediate(int index, bool initializeSilently = false)
    {
        if (weapons.Length == 0)
        {
            equippedWeaponIndex = -1;
            targetWeaponIndex = -1;
            visibleWeapon = null;
            switchState = WeaponSwitchState.Idle;
            CurrentWeaponChanged?.Invoke(null);
            return;
        }

        equippedWeaponIndex = Mathf.Clamp(index, 0, weapons.Length - 1);
        targetWeaponIndex = -1;
        switchState = WeaponSwitchState.Idle;
        switchPhaseTimer = 0f;

        ActivateOnly(equippedWeaponIndex);

        visibleWeapon = CurrentWeapon;

        if (initializeSilently)
        {
            visibleWeapon?.InitializeAsEquippedAtSpawn();
        }
        else
        {
            visibleWeapon?.NotifyEquipped();
            visibleWeapon?.ResetEquipPose();
        }

        CurrentWeaponChanged?.Invoke(visibleWeapon);
    }

    private void EnsureEquippedWeaponVisible()
    {
        if (equippedWeaponIndex < 0 || equippedWeaponIndex >= weapons.Length)
        {
            return;
        }

        WeaponScript equippedWeapon = weapons[equippedWeaponIndex];

        if (equippedWeapon == null)
        {
            return;
        }

        int activeWeaponCount = 0;
        bool weaponMissing = !equippedWeapon.gameObject.activeSelf;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i].gameObject.activeSelf)
            {
                activeWeaponCount++;
            }

            if (i == equippedWeaponIndex)
            {
                continue;
            }

            if (weapons[i].gameObject.activeSelf)
            {
                weaponMissing = true;
            }
        }

        if (activeWeaponCount != 1)
        {
            weaponMissing = true;
        }

        if (!weaponMissing)
        {
            visibleWeapon = equippedWeapon;
            return;
        }

        ActivateOnly(equippedWeaponIndex);
        equippedWeapon.NotifyEquipped();
        equippedWeapon.ResetEquipPose();
        visibleWeapon = equippedWeapon;
    }

    private void ActivateOnly(int index)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            bool shouldBeActive = i == index;
            if (weapons[i].gameObject.activeSelf != shouldBeActive)
            {
                weapons[i].gameObject.SetActive(shouldBeActive);
            }
        }
    }

    private float GetLowerDuration()
    {
        float totalRatio = Mathf.Max(0.01f, Mathf.Clamp01(weaponSwitchLowerRatio) + Mathf.Clamp01(weaponSwitchRaiseRatio));
        return Mathf.Max(0f, weaponSwitchDuration) * (Mathf.Clamp01(weaponSwitchLowerRatio) / totalRatio);
    }

    private float GetRaiseDuration()
    {
        float totalRatio = Mathf.Max(0.01f, Mathf.Clamp01(weaponSwitchLowerRatio) + Mathf.Clamp01(weaponSwitchRaiseRatio));
        return Mathf.Max(0f, weaponSwitchDuration) * (Mathf.Clamp01(weaponSwitchRaiseRatio) / totalRatio);
    }

    private float SmoothPhase(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private WeaponScript[] CollectDirectChildWeapons()
    {
        if (weaponCamera == null)
        {
            return Array.Empty<WeaponScript>();
        }

        Transform cameraTransform = weaponCamera.transform;
        List<WeaponScript> directChildWeapons = new List<WeaponScript>(cameraTransform.childCount);

        for (int i = 0; i < cameraTransform.childCount; i++)
        {
            Transform child = cameraTransform.GetChild(i);
            WeaponScript weapon = child.GetComponent<WeaponScript>();

            if (weapon != null)
            {
                directChildWeapons.Add(weapon);
            }
        }

        return directChildWeapons.ToArray();
    }
}
