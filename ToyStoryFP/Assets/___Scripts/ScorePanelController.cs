using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ScorePanelController : MonoBehaviour
{
    [Header("Wiring")]
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

    private void Awake()
    {
        ResolveUiReferences();
        BindListeners();
        ClosePanelImmediate();
    }

    private void OnEnable()
    {
        BindListeners();
    }

    private void OnDisable()
    {
        UnbindListeners();
    }

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

    public void OpenPanel()
    {
        ResolveUiReferences();

        if (panelScoreRoot == null)
        {
            Debug.LogWarning("ScorePanelController could not open panel because PanelScore is missing in scene.", this);
            return;
        }

        RefreshBestStats();

        if (previousPanelToHide != null)
        {
            previousPanelToHide.SetActive(false);
        }

        panelScoreRoot.SetActive(true);
    }

    public void ClosePanel()
    {
        if (panelScoreRoot != null)
        {
            panelScoreRoot.SetActive(false);
        }

        if (previousPanelToHide != null)
        {
            previousPanelToHide.SetActive(true);
        }
    }

    private void ClosePanelImmediate()
    {
        if (panelScoreRoot != null)
        {
            panelScoreRoot.SetActive(false);
        }
    }

    private void RefreshBestStats()
    {
        RunStatsStore.GetBestStats(out int maxCoins, out int maxWave, out int maxBotsKilled);

        if (bestCoinsText != null)
        {
            bestCoinsText.text = $"MAX COINS: {maxCoins}";
        }

        if (bestWaveText != null)
        {
            bestWaveText.text = $"MAX WAVE: {maxWave}";
        }

        if (bestBotsText != null)
        {
            bestBotsText.text = $"MAX BOTS: {maxBotsKilled}";
        }
    }

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
            Debug.LogWarning("ScorePanelController could not find ScoreButton in PanelButtons.", this);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
        else
        {
            Debug.LogWarning("ScorePanelController could not find BackButton in PanelScore.", this);
        }

        listenersBound = true;
    }

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

    private void ResolveUiReferences()
    {
        if (previousPanelToHide == null)
        {
            Transform panelButtonsTransform = transform.Find("PanelButtons");

            if (panelButtonsTransform == null)
            {
                panelButtonsTransform = transform.parent != null ? transform.parent.Find("PanelButtons") : null;
            }

            previousPanelToHide = panelButtonsTransform != null ? panelButtonsTransform.gameObject : null;
        }

        if (scoreButton == null && previousPanelToHide != null)
        {
            Transform scoreButtonTransform = previousPanelToHide.transform.Find("ScoreButton");
            scoreButton = scoreButtonTransform != null ? scoreButtonTransform.GetComponent<Button>() : null;
        }

        if (panelScoreRoot == null)
        {
            Transform scorePanelTransform = transform.Find("PanelScore");

            if (scorePanelTransform == null)
            {
                scorePanelTransform = transform.parent != null ? transform.parent.Find("PanelScore") : null;
            }

            panelScoreRoot = scorePanelTransform != null ? scorePanelTransform.gameObject : null;
        }

        if (panelScoreRoot == null)
        {
            Debug.LogWarning("ScorePanelController requires an existing PanelScore under EndMenu_Canvas.", this);
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
            Transform closeButtonTransform = panelScoreRoot.transform.Find("BackButton");
            closeButton = closeButtonTransform != null ? closeButtonTransform.GetComponent<Button>() : null;
        }
    }

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
}
