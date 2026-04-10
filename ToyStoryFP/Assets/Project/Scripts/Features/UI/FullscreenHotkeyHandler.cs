using UnityEngine;

[DisallowMultipleComponent]
public class FullscreenHotkeyHandler : MonoBehaviour
{
    [SerializeField] private SettingsPanelController settingsPanelController;
    [SerializeField] private bool applySavedSettingsOnAwake = true;

    private void Awake()
    {
        if (applySavedSettingsOnAwake)
        {
            if (settingsPanelController != null)
            {
                settingsPanelController.ApplySavedSettingsFromProfileOrDefaults();
            }
            else
            {
                SettingsPanelController.ApplySavedSettings();
            }
        }

        if (settingsPanelController == null)
        {
            GameDebug.Advertencia(
                "Settings",
                "FullscreenHotkeyHandler necesita una referencia explicita a SettingsPanelController.",
                this);
        }
    }

    private void Update()
    {
        if (settingsPanelController == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F11))
        {
            settingsPanelController.ToggleFullscreenFromHotkey();
        }
    }
}
