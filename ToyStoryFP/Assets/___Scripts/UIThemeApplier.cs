using UnityEngine;

[DisallowMultipleComponent]
public class UIThemeApplier : MonoBehaviour
{
    [SerializeField] private bool applyOnEnable;
    [SerializeField] private UIThemeCanvasBinder[] canvasBinders;

    private void OnEnable()
    {
        if (!applyOnEnable)
        {
            return;
        }

        ApplyThemeNow();
    }

    [ContextMenu("Apply Theme Now")]
    public void ApplyThemeNow()
    {
        ResolveBindersIfNeeded();

        if (canvasBinders == null || canvasBinders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < canvasBinders.Length; i++)
        {
            UIThemeCanvasBinder binder = canvasBinders[i];

            if (binder == null)
            {
                continue;
            }

            binder.ApplyThemeNow();
        }
    }

    [ContextMenu("Reset To Theme Defaults")]
    public void ResetToThemeDefaults()
    {
        ResolveBindersIfNeeded();

        if (canvasBinders == null || canvasBinders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < canvasBinders.Length; i++)
        {
            UIThemeCanvasBinder binder = canvasBinders[i];

            if (binder == null)
            {
                continue;
            }

            binder.ResetToThemeDefaults();
        }
    }

    private void ResolveBindersIfNeeded()
    {
        if (canvasBinders != null && canvasBinders.Length > 0)
        {
            return;
        }

        canvasBinders = GetComponentsInChildren<UIThemeCanvasBinder>(true);
    }
}
