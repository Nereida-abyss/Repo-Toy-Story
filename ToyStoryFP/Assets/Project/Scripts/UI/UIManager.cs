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
    [SerializeField] private GameObject panelPause;
    [SerializeField] private GameObject panelUI;
    [SerializeField] private GameObject settingsPanel;

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

    // Abre la pausa usando el flujo común para no duplicar reglas.
    public void AbrirPausa()
    {
        ApplyPauseState(true);
    }

    // Cierra la pausa usando el mismo punto centralizado.
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

    // Cambia entre pausa y juego activo según el estado actual.
    private void TogglePausa()
    {
        ApplyPauseState(!IsPaused);
    }

    // Este es el punto que realmente cambia el estado de pausa.
    // Ajusta tiempo, paneles y evento global para que todo el juego reaccione de forma consistente.
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

    // Decide si ahora mismo está permitido abrir o cerrar la pausa.
    private bool CanTogglePause()
    {
        return panelPause != null || panelUI != null;
    }

    // Garantiza que exista un EventSystem para que la UI pueda recibir foco e input.
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
