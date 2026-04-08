using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelController : MonoBehaviour
{
    private enum FullscreenChangeOrigin
    {
        Startup,
        Button,
        HotkeyF11
    }

    private const string FullscreenKey = "settings.fullscreen";
    private const string WindowedWidthKey = "settings.windowedWidth";
    private const string WindowedHeightKey = "settings.windowedHeight";
    private const string MasterVolumeKey = "settings.masterVolume";
    private const string MasterMutedKey = "settings.masterMuted";
    private const string LookSensitivityKey = "settings.lookSensitivity";
    private const float DefaultVolume = 1f;
    private const float DefaultLookSensitivity = 2f;
    private const float MinLookSensitivity = 0.5f;
    private const float MaxLookSensitivity = 5f;
    private const int DefaultWindowedWidth = 1024;
    private const int DefaultWindowedHeight = 768;
    private const int MinimumWindowedDimension = 320;
    private static readonly HashSet<SettingsPanelController> ActiveInstances = new HashSet<SettingsPanelController>();

    [SerializeField] private Button closeButton;
    [SerializeField] private Button fullscreenButton;
    [SerializeField] private TMP_Text fullscreenStateText;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button muteButton;
    [SerializeField] private TMP_Text muteButtonText;
    [SerializeField] private TMP_Text muteStateText;
    [SerializeField] private Slider lookSensitivitySlider;
    [SerializeField] private TMP_Text lookSensitivityValueText;
    [SerializeField] private GameObject previousPanelToHide;

    private float masterVolume = DefaultVolume;
    private float lookSensitivity = DefaultLookSensitivity;
    private bool masterMuted;
    private bool isFullscreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    // Arranca el listener global y aplica el fullscreen guardado antes de cargar escenas.
    private static void ApplySavedSettingsOnStartup()
    {
        FullscreenHotkeyListener.EnsureInstance();
        ApplySavedSettings();
    }

    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        ValidateReferences();
    }

    // Activa listeners y estado al habilitar el objeto.
    private void OnEnable()
    {
        ActiveInstances.Add(this);
        LoadSavedSettings();
        ApplyCurrentSettings();
        RefreshUI();
    }

    // Libera el registro al desactivar el panel.
    private void OnDisable()
    {
        ActiveInstances.Remove(this);
    }

    // Abre panel.
    public void OpenPanel()
    {
        if (gameObject.activeSelf)
        {
            RefreshUI();
            return;
        }

        if (previousPanelToHide != null)
        {
            UIFxUtility.SetPanelActive(previousPanelToHide, false);
        }

        UIFxUtility.SetPanelActive(gameObject, true);
    }

    // Cierra panel.
    public void ClosePanel()
    {
        UIFxUtility.SetPanelActive(gameObject, false);

        if (previousPanelToHide != null)
        {
            UIFxUtility.SetPanelActive(previousPanelToHide, true);
        }
    }

    // Alterna fullscreen.
    public void ToggleFullscreen()
    {
        ToggleFullscreen(FullscreenChangeOrigin.Button);
    }

    // Gestiona el evento de master volumen cambios.
    public void OnMasterVolumeChanged(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();

        ApplyAudioSettings();
        RefreshUI();
    }

    // Alterna mute.
    public void ToggleMute()
    {
        masterMuted = !masterMuted;
        PlayerPrefs.SetInt(MasterMutedKey, masterMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyAudioSettings();
        RefreshUI();
    }

    // Gestiona el evento de look sensitivity cambios.
    public void OnLookSensitivityChanged(float value)
    {
        lookSensitivity = Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity);
        PlayerPrefs.SetFloat(LookSensitivityKey, lookSensitivity);
        PlayerPrefs.Save();

        if (MouseLookScript.instance != null)
        {
            MouseLookScript.instance.SetSensitivity(lookSensitivity);
        }

        RefreshUI();
    }

    // Aplica guardado ajustes.
    public static void ApplySavedSettings()
    {
        bool fullscreen = ResolveFullscreenPreference();
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        bool muted = PlayerPrefs.GetInt(MasterMutedKey, 0) == 1;

        ApplyFullscreenState(fullscreen, FullscreenChangeOrigin.Startup, savePreference: false, logChange: true);
        AudioListener.volume = muted ? 0f : volume;
    }

    // Carga guardado ajustes.
    private void LoadSavedSettings()
    {
        isFullscreen = ResolveFullscreenPreference();
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        masterMuted = PlayerPrefs.GetInt(MasterMutedKey, 0) == 1;
        lookSensitivity = Mathf.Clamp(
            PlayerPrefs.GetFloat(LookSensitivityKey, DefaultLookSensitivity),
            MinLookSensitivity,
            MaxLookSensitivity);
    }

    // Aplica actual ajustes.
    private void ApplyCurrentSettings()
    {
        ApplyFullscreenMode(isFullscreen);
        ApplyAudioSettings();

        if (MouseLookScript.instance != null)
        {
            MouseLookScript.instance.SetSensitivity(lookSensitivity);
        }
    }

    // Aplica audio ajustes.
    private void ApplyAudioSettings()
    {
        AudioListener.volume = masterMuted ? 0f : masterVolume;
    }

    // Refresca UI.
    private void RefreshUI()
    {
        RefreshFullscreenUi();

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }

        if (muteButtonText != null)
        {
            muteButtonText.text = masterMuted ? "UNMUTE" : "MUTE";
        }

        if (muteStateText != null)
        {
            muteStateText.text = masterMuted ? "Muted" : $"{Mathf.RoundToInt(masterVolume * 100f)}%";
        }

        if (lookSensitivitySlider != null)
        {
            lookSensitivitySlider.SetValueWithoutNotify(lookSensitivity);
        }

        if (lookSensitivityValueText != null)
        {
            lookSensitivityValueText.text = lookSensitivity.ToString("0.00");
        }
    }

    // Valida referencias.
    private void ValidateReferences()
    {
        if (closeButton == null || fullscreenButton == null || fullscreenStateText == null || masterVolumeSlider == null || muteButton == null || lookSensitivitySlider == null || lookSensitivityValueText == null)
        {
            GameDebug.Advertencia("Settings", "Faltan una o mas referencias UI en SettingsPanelController.", this);
        }
    }

    // Alterna fullscreen usando el mismo flujo tanto para botón como para F11.
    private static void ToggleFullscreen(FullscreenChangeOrigin origin)
    {
        ApplyFullscreenState(!ResolveFullscreenPreference(), origin, savePreference: true, logChange: true);
    }

    // Punto de entrada para el atajo global F11.
    internal static void ToggleFullscreenFromHotkey()
    {
        ToggleFullscreen(FullscreenChangeOrigin.HotkeyF11);
    }

    // Aplica el estado real, lo guarda si toca y mantiene la UI sincronizada.
    private static void ApplyFullscreenState(bool fullscreen, FullscreenChangeOrigin origin, bool savePreference, bool logChange)
    {
        ApplyFullscreenMode(fullscreen);

        if (savePreference)
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        RefreshRegisteredFullscreenUi(fullscreen);

        if (logChange)
        {
            GameDebug.Info("Settings", $"Fullscreen {GetFullscreenStateLabel(fullscreen)} aplicado desde {GetFullscreenOriginLabel(origin)}.");
        }
    }

    // Lee la preferencia guardada y cae al estado actual si no había nada persistido.
    private static bool ResolveFullscreenPreference()
    {
        return PlayerPrefs.GetInt(FullscreenKey, ResolveCurrentFullscreenState() ? 1 : 0) == 1;
    }

    // Considera fullscreen cualquier modo que no sea ventana.
    private static bool ResolveCurrentFullscreenState()
    {
        return Screen.fullScreenMode != FullScreenMode.Windowed;
    }

    // Aplica de forma explícita el modo de pantalla correcto.
    private static void ApplyFullscreenMode(bool fullscreen)
    {
        if (fullscreen)
        {
            CaptureCurrentWindowedSizeIfNeeded();
            Resolution desktopResolution = Screen.currentResolution;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(
                Mathf.Max(DefaultWindowedWidth, desktopResolution.width),
                Mathf.Max(DefaultWindowedHeight, desktopResolution.height),
                FullScreenMode.FullScreenWindow);
            return;
        }

        Vector2Int windowedSize = ResolveWindowedSize();
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(windowedSize.x, windowedSize.y, FullScreenMode.Windowed);
    }

    // Guarda el último tamaño de ventana útil antes de entrar en fullscreen.
    private static void CaptureCurrentWindowedSizeIfNeeded()
    {
        if (ResolveCurrentFullscreenState())
        {
            return;
        }

        int currentWidth = Mathf.Max(Screen.width, 0);
        int currentHeight = Mathf.Max(Screen.height, 0);

        if (currentWidth < MinimumWindowedDimension || currentHeight < MinimumWindowedDimension)
        {
            return;
        }

        PlayerPrefs.SetInt(WindowedWidthKey, currentWidth);
        PlayerPrefs.SetInt(WindowedHeightKey, currentHeight);
        PlayerPrefs.Save();
    }

    // Recupera el tamaño de ventana guardado o usa un fallback seguro.
    private static Vector2Int ResolveWindowedSize()
    {
        int width = PlayerPrefs.GetInt(WindowedWidthKey, DefaultWindowedWidth);
        int height = PlayerPrefs.GetInt(WindowedHeightKey, DefaultWindowedHeight);

        if (width < MinimumWindowedDimension)
        {
            width = DefaultWindowedWidth;
        }

        if (height < MinimumWindowedDimension)
        {
            height = DefaultWindowedHeight;
        }

        return new Vector2Int(width, height);
    }

    // Refresca el texto local de fullscreen.
    private void RefreshFullscreenUi()
    {
        if (fullscreenStateText != null)
        {
            fullscreenStateText.text = GetFullscreenStateLabel(isFullscreen);
        }
    }

    // Sincroniza cualquier panel de ajustes abierto en ese momento.
    private static void RefreshRegisteredFullscreenUi(bool fullscreen)
    {
        if (ActiveInstances.Count == 0)
        {
            return;
        }

        List<SettingsPanelController> missingInstances = null;

        foreach (SettingsPanelController instance in ActiveInstances)
        {
            if (instance == null)
            {
                missingInstances ??= new List<SettingsPanelController>();
                missingInstances.Add(instance);
                continue;
            }

            instance.isFullscreen = fullscreen;
            instance.RefreshFullscreenUi();
        }

        if (missingInstances == null)
        {
            return;
        }

        for (int i = 0; i < missingInstances.Count; i++)
        {
            ActiveInstances.Remove(missingInstances[i]);
        }
    }

    // Devuelve el texto visible del estado de fullscreen.
    private static string GetFullscreenStateLabel(bool fullscreen)
    {
        return fullscreen ? "ON" : "OFF";
    }

    // Traduce el origen del cambio a un texto corto para debug.
    private static string GetFullscreenOriginLabel(FullscreenChangeOrigin origin)
    {
        switch (origin)
        {
            case FullscreenChangeOrigin.Button:
                return "Button";
            case FullscreenChangeOrigin.HotkeyF11:
                return "F11";
            default:
                return "Startup";
        }
    }
}

internal sealed class FullscreenHotkeyListener : MonoBehaviour
{
    private static FullscreenHotkeyListener instance;

    // Asegura una única instancia global del listener entre escenas.
    internal static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        instance = FindFirstObjectByType<FullscreenHotkeyListener>();

        if (instance != null)
        {
            return;
        }

        GameObject listenerObject = new GameObject("FullscreenHotkeyListener");
        instance = listenerObject.AddComponent<FullscreenHotkeyListener>();
    }

    // Mantiene viva una sola copia del listener durante todo el juego.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Escucha F11 y delega el cambio real al controlador de ajustes.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            SettingsPanelController.ToggleFullscreenFromHotkey();
        }
    }
}
