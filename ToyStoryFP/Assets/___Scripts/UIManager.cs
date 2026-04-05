using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    public static bool IsGamePaused => Instance != null && Instance.IsPaused;

    [Header("Paneles")]
    public GameObject panelPause;
    public GameObject panelUI;

    public bool IsPaused => panelPause != null && panelPause.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate UIManager detected. Destroying the newest instance.", this);
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

    public void AbrirPausa()
    {
        ApplyPauseState(true);
    }

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
    }

    private void TogglePausa()
    {
        ApplyPauseState(!IsPaused);
    }

    private void ApplyPauseState(bool paused)
    {
        if (panelPause != null)
        {
            panelPause.SetActive(paused);
        }

        if (panelUI != null)
        {
            panelUI.SetActive(!paused);
        }

        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    private bool CanTogglePause()
    {
        return panelPause != null || panelUI != null;
    }

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
