using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayInputGate
{
    private static readonly HashSet<string> activeOwners = new HashSet<string>();

    public static event Action<bool> BlockStateChanged;

    public static bool IsBlocked => activeOwners.Count > 0;

    public static void Acquire(string owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            return;
        }

        bool wasBlocked = IsBlocked;
        activeOwners.Add(owner);
        NotifyStateChangedIfNeeded(wasBlocked);
    }

    public static void Release(string owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            return;
        }

        bool wasBlocked = IsBlocked;
        activeOwners.Remove(owner);
        NotifyStateChangedIfNeeded(wasBlocked);
    }

    public static void Reset()
    {
        bool wasBlocked = IsBlocked;
        activeOwners.Clear();
        NotifyStateChangedIfNeeded(wasBlocked);
    }

    public static void ApplyGameplayCursorState()
    {
        if (IsBlocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (HasGameplayPlayerControl())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void NotifyStateChangedIfNeeded(bool wasBlocked)
    {
        ApplyGameplayCursorState();

        if (IsBlocked == wasBlocked)
        {
            return;
        }

        BlockStateChanged?.Invoke(IsBlocked);
    }

    private static bool HasGameplayPlayerControl()
    {
        return SceneManager.GetActiveScene().name == "GamePlay";
    }
}
