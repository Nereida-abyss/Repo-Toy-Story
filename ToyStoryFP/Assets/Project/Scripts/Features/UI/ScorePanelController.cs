using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScorePanelController : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private Transform endMenuCanvasRoot;
    [SerializeField] private GameObject previousPanelToHide;
    [SerializeField] private GameObject panelScoreRoot;
    [SerializeField] private Button scoreButton;
    [SerializeField] private Button closeButton;

    [Header("Score Texts")]
    [SerializeField] private TMP_Text bestCoinsText;
    [SerializeField] private TMP_Text bestWaveText;
    [SerializeField] private TMP_Text bestBotsText;

    [Header("Behavior")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private bool listenersBound;

    // Inicializa referencias antes de usar el componente.
    private void Awake()
    {
        BindListeners();
        ClosePanelImmediate();
    }

    // Activa listeners y estado al habilitar el objeto.
    private void OnEnable()
    {
        BindListeners();
    }

    // Libera listeners y estado al deshabilitar el objeto.
    private void OnDisable()
    {
        UnbindListeners();
    }

    // Actualiza la logica en cada frame.
    private void Update()
    {
        if (panelScoreRoot == null || !panelScoreRoot.activeSelf)
        {
            return;
        }

        if (ProjectInput.WasUiClosePressed(closeKey))
        {
            ClosePanel();
        }
    }

    // Configura si needed.
    public void ConfigureIfNeeded(GameObject previousPanel, GameObject existingScorePanel)
    {
        if (previousPanelToHide == null)
        {
            previousPanelToHide = previousPanel;
        }

        if (panelScoreRoot == null)
        {
            panelScoreRoot = existingScorePanel;
        }

        BindListeners();
        ClosePanelImmediate();
    }

    // Abre panel.
    public void OpenPanel()
    {
        if (panelScoreRoot == null)
        {
            GameDebug.Advertencia("Score", "No se puede abrir SCORE porque falta PanelScore en la escena.", this);
            return;
        }

        RefreshBestStats();

        if (previousPanelToHide != null)
        {
            UIFxUtility.SetPanelActive(previousPanelToHide, false);
        }

        UIFxUtility.SetPanelActive(panelScoreRoot, true);
    }

    // Cierra panel.
    public void ClosePanel()
    {
        if (panelScoreRoot != null)
        {
            UIFxUtility.SetPanelActive(panelScoreRoot, false);
        }

        if (previousPanelToHide != null)
        {
            UIFxUtility.SetPanelActive(previousPanelToHide, true);
        }
    }

    // Cierra panel inmediato.
    private void ClosePanelImmediate()
    {
        if (panelScoreRoot != null)
        {
            UIFxUtility.HideImmediate(panelScoreRoot);
        }
    }

    // Refresca best estadisticas.
    private void RefreshBestStats()
    {
        RunStatsStore.GetLastRunStats(out int coins, out int wave, out int bots);

        if (bestCoinsText != null)
        {
            bestCoinsText.text = $"MAX COINS: {coins}";
        }

        if (bestWaveText != null)
        {
            bestWaveText.text = $"LAST WAVE: {wave}";
        }

        if (bestBotsText != null)
        {
            bestBotsText.text = $"LAST BOTS: {bots}";
        }
    }

    // Conecta listeners.
    private void BindListeners()
    {
        if (listenersBound)
        {
            return;
        }

        if (scoreButton != null)
        {
            scoreButton.onClick.RemoveListener(OpenPanel);
            scoreButton.onClick.AddListener(OpenPanel);
        }
        else
        {
            GameDebug.Advertencia("Score", "ScorePanelController no tiene ScoreButton asignado en Inspector.", this);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
        else
        {
            GameDebug.Advertencia("Score", "ScorePanelController no tiene BackButton asignado en Inspector.", this);
        }

        listenersBound = true;
    }

    // Desconecta listeners.
    private void UnbindListeners()
    {
        if (scoreButton != null)
        {
            scoreButton.onClick.RemoveListener(OpenPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        listenersBound = false;
    }
}
