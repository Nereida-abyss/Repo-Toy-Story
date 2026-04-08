using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public static event Action<bool> PauseStateChanged;
    public static bool IsGamePaused => Instance != null && Instance.IsPaused;

    [Header("Paneles")]
    public GameObject panelPause;
    public GameObject panelUI;
    public GameObject settingsPanel;

    public bool IsPaused =>
        (panelPause != null && panelPause.activeSelf) ||
        (settingsPanel != null && settingsPanel.activeSelf);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            GameDebug.Advertencia("UI", "Se detecto un UIManager duplicado. Se destruirá la instancia mas nueva.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureEventSystem();
    }

    void Start()
    {
        ApplyPauseState(IsPaused);
    }

    // Gestiona abrir pausa.
    public void AbrirPausa()
    {
        ApplyPauseState(true);
    }

    // Gestiona cerrar pausa.
    public void CerrarPausa()
    {
        ApplyPauseState(false);
    }

    void Update()
    {
        if (!CanTogglePause())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePausa();
        }
    }

    void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        Instance = null;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PauseStateChanged?.Invoke(false);
    }

    // Alterna pausa.
    private void TogglePausa()
    {
        ApplyPauseState(!IsPaused);
    }

    // Aplica pausa estado.
    private void ApplyPauseState(bool paused)
    {
        bool previousPauseState = IsPaused;

        if (panelPause != null)
        {
            UIFxUtility.SetPanelActive(panelPause, paused);
        }

        if (settingsPanel != null)
        {
            UIFxUtility.SetPanelActive(settingsPanel, false);
        }

        if (panelUI != null)
        {
            panelUI.SetActive(!paused);
        }

        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;

        bool currentPauseState = IsPaused;

        if (currentPauseState != previousPauseState)
        {
            PauseStateChanged?.Invoke(currentPauseState);
        }
    }

    // Comprueba si alternar pausa.
    private bool CanTogglePause()
    {
        return panelPause != null || panelUI != null;
    }

    // Asegura event system.
    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
