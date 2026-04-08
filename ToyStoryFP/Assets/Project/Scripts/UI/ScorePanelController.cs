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
        ResolveUiReferences();
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

    // Actualiza la lógica en cada frame.
    private void Update()
    {
        if (panelScoreRoot == null || !panelScoreRoot.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(closeKey))
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

        ResolveUiReferences();
        BindListeners();
        ClosePanelImmediate();
    }

    // Abre panel.
    public void OpenPanel()
    {
        ResolveUiReferences();

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

    // Refresca best estadísticas.
    private void RefreshBestStats()
    {
        RunStatsStore.GetLastRunStats(out int coins, out int wave, out int bots);

        if (bestCoinsText != null)
        {
            bestCoinsText.text = $"LAST COINS: {coins}";
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

        ResolveUiReferences();

        if (scoreButton != null)
        {
            scoreButton.onClick.RemoveListener(OpenPanel);
            scoreButton.onClick.AddListener(OpenPanel);
        }
        else
        {
            GameDebug.Advertencia("Score", "No se encontró ScoreButton dentro de PanelButtons.", this);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
        else
        {
            GameDebug.Advertencia("Score", "No se encontró BackButton dentro de PanelScore.", this);
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

    // Resuelve UI referencias.
    private void ResolveUiReferences()
    {
        Transform canvasRoot = ResolveCanvasRoot();

        if (previousPanelToHide == null)
        {
            Transform panelButtonsTransform = FindChildByName(canvasRoot, "PanelButtons");
            previousPanelToHide = panelButtonsTransform != null ? panelButtonsTransform.gameObject : null;
        }

        if (scoreButton == null && previousPanelToHide != null)
        {
            Transform scoreButtonTransform = FindChildByName(previousPanelToHide.transform, "ScoreButton");
            scoreButton = scoreButtonTransform != null ? scoreButtonTransform.GetComponent<Button>() : null;
        }

        if (panelScoreRoot == null)
        {
            Transform scorePanelTransform = FindChildByName(canvasRoot, "PanelScore");
            panelScoreRoot = scorePanelTransform != null ? scorePanelTransform.gameObject : null;
        }

        if (panelScoreRoot == null)
        {
            GameDebug.Advertencia("Score", "ScorePanelController necesita un PanelScore existente bajo EndMenu_Canvas.", this);
            return;
        }

        if (bestCoinsText == null)
        {
            bestCoinsText = FindTextUnderPanel("BestCoinsText");
        }

        if (bestWaveText == null)
        {
            bestWaveText = FindTextUnderPanel("BestWaveText");
        }

        if (bestBotsText == null)
        {
            bestBotsText = FindTextUnderPanel("BestBotsText");
        }

        if (closeButton == null)
        {
            Transform closeButtonTransform = FindChildByName(panelScoreRoot.transform, "BackButton");
            closeButton = closeButtonTransform != null ? closeButtonTransform.GetComponent<Button>() : null;
        }
    }

    // Busca texto under panel.
    private TMP_Text FindTextUnderPanel(string nameToFind)
    {
        if (panelScoreRoot == null || string.IsNullOrWhiteSpace(nameToFind))
        {
            return null;
        }

        TMP_Text[] texts = panelScoreRoot.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text != null && text.name == nameToFind)
            {
                return text;
            }
        }

        return null;
    }

    // Resuelve canvas raíz.
    private Transform ResolveCanvasRoot()
    {
        if (endMenuCanvasRoot != null)
        {
            return endMenuCanvasRoot;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>(true);

        if (parentCanvas != null)
        {
            endMenuCanvasRoot = parentCanvas.transform;
            return endMenuCanvasRoot;
        }

        if (transform.parent != null)
        {
            endMenuCanvasRoot = transform.parent;
            return endMenuCanvasRoot;
        }

        endMenuCanvasRoot = transform;
        return endMenuCanvasRoot;
    }

    // Busca hijo por nombre.
    private Transform FindChildByName(Transform root, string nameToFind)
    {
        if (root == null || string.IsNullOrWhiteSpace(nameToFind))
        {
            return null;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate != null && candidate.name == nameToFind)
            {
                return candidate;
            }
        }

        return null;
    }
}
