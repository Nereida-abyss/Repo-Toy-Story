using UnityEngine;

public static class ProjectInput
{
    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const string MouseXAxis = "Mouse X";
    private const string MouseYAxis = "Mouse Y";
    private const string MouseScrollWheelAxis = "Mouse ScrollWheel";
    private const string PrimaryFireButton = "Fire1";

    public static Vector2 GetMoveInput()
    {
        return new Vector2(
            Input.GetAxisRaw(HorizontalAxis),
            Input.GetAxisRaw(VerticalAxis));
    }

    public static Vector2 GetLookDelta()
    {
        return new Vector2(
            Input.GetAxisRaw(MouseXAxis),
            Input.GetAxisRaw(MouseYAxis));
    }

    public static float GetWeaponCycleScroll()
    {
        return Input.GetAxisRaw(MouseScrollWheelAxis);
    }

    public static bool WasJumpPressed()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    public static bool WasReloadPressed()
    {
        return Input.GetKeyDown(KeyCode.R);
    }

    public static bool IsPrimaryFireHeld()
    {
        return Input.GetButton(PrimaryFireButton);
    }

    public static bool WasPauseTogglePressed()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }

    public static bool WasUiBackPressed()
    {
        return Input.GetKeyDown(KeyCode.Escape);
    }

    public static bool WasUiClosePressed(KeyCode closeKey)
    {
        return Input.GetKeyDown(closeKey);
    }

    public static bool WasShopTogglePressed()
    {
        return Input.GetKeyDown(KeyCode.T);
    }

    public static bool WasNextWavePressed()
    {
        return Input.GetKeyDown(KeyCode.Q);
    }

    public static bool WasFullscreenTogglePressed()
    {
        return Input.GetKeyDown(KeyCode.F11);
    }

    public static bool WasCreditsSkipRequested()
    {
        return WasUiBackPressed() || Input.GetMouseButtonDown(0);
    }
}
